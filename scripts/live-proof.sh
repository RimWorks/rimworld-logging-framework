#!/usr/bin/env bash
set -uo pipefail

root=$(pwd)
probe="$root/Probe"
if [[ ! -f "$probe/About/About.xml" || ! -f "$root/RimWorks.RimLogging.slnx" ]]; then
    echo "live-proof: run this from the repo root" >&2
    exit 2
fi
for tool in gamecrate jq make; do
    command -v "$tool" >/dev/null || { echo "live-proof: $tool is not on PATH" >&2; exit 2; }
done

work=$(mktemp -d -t rimlogging-live-proof.XXXXXX)
settings_name="Mod_RimWorks.RimLogging_LoggingMod.xml"
rolling_rel="config/config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/RimLogging"
failed=0

pass() { echo "pass  $1"; }
fail() { echo "FAIL  $1"; failed=1; }
stop() { echo "live-proof: $1" >&2; exit 2; }

line_of() { grep -n -m1 -F -- "$2" "$1" | cut -d: -f1; }

frames_after() {
    awk -v n="$2" 'NR > n { if ($0 ~ /^at /) c++; else exit } END { print c + 0 }' "$1"
}

has_prefix() { sed -n "${2}p" "$1" | grep -qE '[[/]ProbeLines(\.cs)?:[0-9]+\]'; }

excerpt() {
    local n
    n=$(line_of "$1" "$2")
    if [[ -z "$n" ]]; then
        echo "      ($2 is not in $(basename "$1"))"
        return
    fi
    sed -n "${n},$((n + 6))p" "$1" | cut -c1-200 | sed 's/^/      | /'
}

newest_after() {
    find "$1" -maxdepth 1 -name "$2" -newer "$3" -printf '%T@ %p\n' 2>/dev/null | sort -n | tail -1 | cut -d' ' -f2-
}

ndjson_field() {
    jq -cR --arg tag "$2" "fromjson? | select(.msg | contains(\$tag)) | $3" "$1" 2>/dev/null | head -1
}

echo "building 1.6 and 1.7 (log: $work/build.log)"
make build >"$work/build.log" 2>&1 || stop "make build failed, see $work/build.log"
make build GameVersion=1.7 >>"$work/build.log" 2>&1 || stop "make build GameVersion=1.7 failed, see $work/build.log"

prove() {
    local profile=$1 version=$2 kind=$3
    local -a flags=(--mod RimWorks.RimLogging.Probe --use "RimWorks.RimLogging.Probe=$probe"
        --mode headless --marker '[rimlogging-probe] done' --timeout 300 --no-replace)
    local -a game_args=()
    [[ "$version" == "1.7" ]] && game_args=(-- -DisableLogStackTraces -NoFileLogCallstacks)

    echo "== $profile ($version)"
    local plan
    plan=$(gamecrate run "$profile" "${flags[@]}" --print-plan --json "${game_args[@]}" 2>"$work/$profile.plan.err") \
        || stop "gamecrate could not plan $profile, see $work/$profile.plan.err"

    local profile_dir data_dir logs_dir
    profile_dir=$(jq -r .profileDir <<<"$plan")
    data_dir=$(jq -r .dataDirHost <<<"$plan")
    logs_dir=$(jq -r .logsDirHost <<<"$plan")

    local id dir
    for id in RimWorks.RimLogging RimWorks.RimLogging.Probe; do
        dir=$(jq -r --arg id "$id" '.mods[] | select(.packageId == $id) | .hostDir' <<<"$plan")
        [[ "$dir" == "$root" || "$dir" == "$root/"* ]] || stop "$profile resolves $id to '${dir:-nothing}', not inside $root"
    done

    local settings="$data_dir/Config/$settings_name" stamp="$work/$profile.stamp" out="$work/$profile.out" code tries=0
    while :; do
        if [[ -f "$settings" ]]; then
            local saved
            saved=$(grep -oE '<logDirectory>[^<]+' "$settings" | head -1 | cut -d'>' -f2)
            [[ -z "$saved" || "$saved" == *"/${rolling_rel#config/}" ]] \
                || stop "$settings sets logDirectory to $saved, so the rolling files land somewhere this script cannot read. clear it in the settings window and run again"
            if [[ "$kind" == "plain" ]]; then
                local level
                level=$(grep -oE '<printStackAt>[^<]*' "$settings" | head -1 | cut -d'>' -f2)
                [[ -z "$level" || "$level" == "Warn" ]] \
                    || stop "$settings saves printStackAt as $level. checks 1 and 2 expect Warn. set it back to Warn in the settings window and run again"
            fi
        fi

        touch "$stamp"
        gamecrate run "$profile" "${flags[@]}" "${game_args[@]}" >"$out" 2>&1
        code=$?
        [[ $code -ne 7 ]] && break
        tries=$((tries + 1))
        [[ $tries -ge 30 ]] && stop "gamecrate refused $profile 30 times in a row (exit 7). see $out"
        echo "   gamecrate refused the run (exit 7), most likely a window open on this instance. trying again in 60s"
        sleep 60
    done
    echo "   gamecrate exit $code, output in $out"

    local run_dir game_log
    run_dir=$(find "$logs_dir/runs" -mindepth 1 -maxdepth 1 -type d -newer "$stamp" 2>/dev/null | sort | tail -1)
    if [[ -z "$run_dir" ]]; then
        fail "$profile: no run directory under $logs_dir/runs"
        return
    fi
    if [[ "$version" == "1.7" ]]; then game_log="$run_dir/stdout.log"; else game_log="$run_dir/Player.log"; fi
    if [[ ! -f "$game_log" ]]; then
        fail "$profile: no game log at $game_log"
        return
    fi

    local log="$work/$profile.game.log"
    if [[ "$version" == "1.7" ]]; then
        sed -E 's/\x1b\[[0-9;]*m//g; s/\r$//; s/^[0-9]{2}:[0-9]{2}:[0-9]{2} +[A-Z]+ //' "$game_log" >"$log"
    else
        sed -E 's/\r$//' "$game_log" >"$log"
    fi

    if grep -qF '[rimlogging-probe] done' "$log"; then
        pass "$profile: the run reached the marker"
    else
        fail "$profile: the run did not reach the marker (game log $game_log)"
        return
    fi

    local found
    found=$(grep -oE -m1 'RimWorld [0-9]+\.[0-9]+\.[0-9]+ rev[0-9]+' "$log")
    if [[ "$found" == "RimWorld $version."* ]]; then
        pass "$profile: the game is $found"
    else
        fail "$profile: expected RimWorld $version, the game log says '${found:-nothing}'"
    fi

    local l1 l2 l3 l4 l5 l6
    l1=$(line_of "$log" "[rimlogging-probe] line1-message")
    l2=$(line_of "$log" "[rimlogging-probe] line2-warning")
    l3=$(line_of "$log" "[rimlogging-probe] line3-error")
    l4=$(line_of "$log" "[rimlogging-probe] line4-warning-once")
    l5=$(line_of "$log" "[rimlogging-probe] line5-warn-exception")
    l6=$(line_of "$log" "[rimlogging-probe] line6-no-capture")

    if [[ "$kind" == "trace" ]]; then
        if ! grep -qF '<printStackAt>Trace</printStackAt>' "$settings" 2>/dev/null; then
            fail "$profile: $settings does not save printStackAt as Trace, so the gamecrate on PATH did not write the profile's modSettings. the fix is the gamecrate commit 'fix(cli): keep a profile's mod settings files through resolution'"
            return
        fi
        if [[ -n "$l1" && $(frames_after "$log" "$l1") -gt 0 ]]; then
            pass "$profile: line 1 has frames under it in the game log"
        else
            fail "$profile: line 1 has no frames under it in the game log"
            excerpt "$log" "[rimlogging-probe] line1-message"
        fi
        return
    fi

    local rolling="$profile_dir/$rolling_rel" text json
    text=$(newest_after "$rolling" 'RimLogging-*.log' "$stamp")
    json=$(newest_after "$rolling" 'RimLogging-*.ndjson' "$stamp")

    if [[ -n "$l1" ]] && has_prefix "$log" "$l1" && [[ $(frames_after "$log" "$l1") -eq 0 ]]; then
        pass "$profile: check 1, line 1 has a ProbeLines prefix and no frames in the game log"
    else
        fail "$profile: check 1, line 1 needs a ProbeLines prefix and no frames in the game log"
        excerpt "$log" "[rimlogging-probe] line1-message"
    fi

    if [[ -n "$l2" && -n "$l3" && $(frames_after "$log" "$l2") -gt 0 && $(frames_after "$log" "$l3") -gt 0 ]]; then
        pass "$profile: check 2, lines 2 and 3 have frames in the game log"
    else
        fail "$profile: check 2, lines 2 and 3 need frames in the game log"
        excerpt "$log" "[rimlogging-probe] line2-warning"
        excerpt "$log" "[rimlogging-probe] line3-error"
    fi

    if [[ -n "$l5" ]] && grep -qF 'InvalidOperationException: [rimlogging-probe] line5-exception-text' "$log"; then
        pass "$profile: check 3, line 5 prints its exception in the game log"
    else
        fail "$profile: check 3, line 5 needs its exception in the game log"
        excerpt "$log" "[rimlogging-probe] line5-warn-exception"
    fi

    local t1=""
    [[ -n "$text" ]] && t1=$(line_of "$text" "[rimlogging-probe] line1-message")
    if [[ -n "$t1" && $(frames_after "$text" "$t1") -gt 0 ]]; then
        pass "$profile: check 4, line 1 has frames in the rolling text file"
    elif [[ -z "$text" ]]; then
        fail "$profile: check 4, no rolling text file written in this run under $rolling"
    else
        fail "$profile: check 4, line 1 needs frames in $text"
        excerpt "$text" "[rimlogging-probe] line1-message"
    fi

    local stack6="" patched6=""
    if [[ -n "$json" ]]; then
        stack6=$(ndjson_field "$json" "[rimlogging-probe] line6-no-capture" .stack)
        patched6=$(ndjson_field "$json" "[rimlogging-probe] line6-no-capture" .patched)
    fi
    if [[ -n "$l6" ]] && has_prefix "$log" "$l6" && [[ "$stack6" == "null" && "$patched6" == "null" ]]; then
        pass "$profile: check 5, line 6 has a ProbeLines prefix, and a null stack and patched in NDJSON"
    else
        fail "$profile: check 5, line 6 needs a ProbeLines prefix, and a null stack and patched in NDJSON (stack=${stack6:-missing} patched=${patched6:-missing} file=${json:-none})"
        excerpt "$log" "[rimlogging-probe] line6-no-capture"
    fi

    if [[ -n "$l4" ]] && has_prefix "$log" "$l4"; then
        pass "$profile: check 6, line 4 has a ProbeLines prefix"
    else
        fail "$profile: check 6, line 4 needs a ProbeLines prefix"
        excerpt "$log" "[rimlogging-probe] line4-warning-once"
    fi

    local stack1=""
    [[ -n "$json" ]] && stack1=$(ndjson_field "$json" "[rimlogging-probe] line1-message" .stack)
    if [[ "$stack1" == *'at '* ]]; then
        pass "$profile: check 7, line 1 has a stack with frames in NDJSON"
    else
        fail "$profile: check 7, line 1 needs a stack with frames in NDJSON (stack=${stack1:-missing} file=${json:-none})"
    fi
}

prove rimlogging 1.6 plain
prove rimlogging-harmony 1.6 plain
prove rimlogging-17 1.7 plain
prove rimlogging-17-harmony 1.7 plain
prove rimlogging-17-trace 1.7 trace

echo "work files: $work"
if [[ $failed -ne 0 ]]; then
    echo "live-proof: FAILED"
    exit 1
fi
echo "live-proof: all checks passed"
