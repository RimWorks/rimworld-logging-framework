using System;

namespace RimWorks.RimLogging.Capture;

/// <summary>
/// Runtime fallback that resolves the originating <see cref="SourceLocation"/> by
/// walking the managed stack when compile-time caller attributes are unavailable.
/// </summary>
public static class StackWalker
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private static readonly System.Text.RegularExpressions.Regex _pathStrip = new(
        @"^.*?(RimworldCosmere[\\/]RimworldCosmere[\\/]|RimWorld[\\/]Mods[\\/])+[\\/]*",
        System.Text.RegularExpressions.RegexOptions.Compiled,
        RegexTimeout);
    private static readonly System.Text.RegularExpressions.Regex _dupDir = new(
        @"^(\w+)[\\/]\1[\\/]",
        System.Text.RegularExpressions.RegexOptions.Compiled,
        RegexTimeout);

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<System.Reflection.Assembly, AssemblyHint> _assemblyHints =
        new System.Collections.Concurrent.ConcurrentDictionary<System.Reflection.Assembly, AssemblyHint>();

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _normalizedPaths =
        new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

    /// <summary>
    /// Anchors an embedded source path to a mod folder, so the user sees the in-game folder
    /// name rather than the developer's project layout.
    /// </summary>
    private readonly struct AssemblyHint
    {
        public readonly string? AssemblyName;
        public readonly string? ModFolder;

        public AssemblyHint(string? assemblyName, string? modFolder)
        {
            AssemblyName = assemblyName;
            ModFolder = modFolder;
        }
    }

    /// <summary>
    /// Walks the current stack and returns the first frame outside the RimWorks.RimLogging. namespace,
    /// normalising its file path so the resulting  is stable across machines and layouts.
    /// </summary>
    /// <returns>
    /// A populated <see cref="SourceLocation"/> when a usable frame is found, or
    /// <see cref="SourceLocation.Empty"/> when no caller frame carries file info.
    /// </returns>

    public static SourceLocation WalkOnce()
    {
        System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(1, true);
        return FirstCallerFrame(st);
    }

    /// <summary>
    /// Returns the first frame in <paramref name="st"/> that lives outside the
    /// <c>RimWorks.RimLogging.</c> namespace, with a normalised file path.
    /// </summary>
    /// <param name="st">A pre-captured stack trace to scan.</param>
    /// <returns>The resolved <see cref="SourceLocation"/>, or <see cref="SourceLocation.Empty"/>.</returns>
    public static SourceLocation FirstCallerFrame(System.Diagnostics.StackTrace st)
    {
        for (int i = 0; i < st.FrameCount; i++)
        {
            System.Diagnostics.StackFrame? frame = st.GetFrame(i);
            System.Reflection.MethodBase? method = frame?.GetMethod();
            System.Type? declaringType = method?.DeclaringType;
            string? declaring = declaringType?.FullName;
            string? assembly = declaringType?.Assembly.GetName().Name;
            // Skip framework infrastructure (RimLogging, Harmony stubs, MonoMod, dynamic methods).
            if (CallerFrameClassifier.IsInternalFrame(declaring, assembly)) continue;
            string? file = frame?.GetFileName();
            // vanilla frames have no PDB, so keep walking rather than giving up on the
            // user-code frame underneath
            if (file == null) continue;
            string clean = NormalizePath(file, declaringType);
            return new SourceLocation(clean, frame!.GetFileLineNumber(), method?.Name);
        }
        return SourceLocation.Empty;
    }


    /// <summary>
    /// Returns the declaring <see cref="System.Type"/> of the first non-framework frame in
    /// <paramref name="st"/>, or <c>null</c> when no such frame exists. Cheaper than
    /// <see cref="FirstCallerFrame"/> because it does not touch file/line metadata, so the
    /// caller can build the trace with <c>fNeedFileInfo: false</c>.
    /// </summary>
    public static System.Type? FirstCallerType(System.Diagnostics.StackTrace st)
    {
        for (int i = 0; i < st.FrameCount; i++)
        {
            System.Diagnostics.StackFrame? frame = st.GetFrame(i);
            System.Reflection.MethodBase? method = frame?.GetMethod();
            System.Type? declaringType = method?.DeclaringType;
            string? declaring = declaringType?.FullName;
            string? assembly = declaringType?.Assembly.GetName().Name;
            if (CallerFrameClassifier.IsInternalFrame(declaring, assembly)) continue;
            return declaringType;
        }
        return null;
    }

    /// <summary>
    /// Formats a  into a multi-line string of at Type.Method (file:line) entries for every frame outside the
    /// RimWorks.RimLogging. namespace. Paths are normalised the same way  normalises them.
    /// </summary>
    /// <param name="st">A pre-captured stack trace to format.</param>
    /// <returns>A formatted trace string; empty when no qualifying frames exist.</returns>
    public static string FormatTrace(System.Diagnostics.StackTrace st) => FormatTrace(st, out _);

    /// <summary>Same as <see cref="FormatTrace(System.Diagnostics.StackTrace)"/>, plus the
    /// distinct patch-owner ids found across every frame of the same walk.</summary>
    public static string FormatTrace(System.Diagnostics.StackTrace st, out System.Collections.Generic.IReadOnlyList<string>? patchedBy)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        System.Collections.Generic.List<string>? owners = null;
        bool attributionEnabled = Logging.AttributionProvider != null;
        bool attributionUnavailable = false;
        for (int i = 0; i < st.FrameCount; i++)
        {
            System.Diagnostics.StackFrame? frame = st.GetFrame(i);
            if (frame == null) continue;
            System.Reflection.MethodBase? method = frame.GetMethod();

            // StackFrame, not MethodBase: Mono nulls GetMethod() on a Harmony replacement frame, and only the frame lets Harmony's native-address fallback resolve it.
            if (attributionEnabled)
            {
                System.Collections.Generic.IReadOnlyList<string>? frameOwners =
                    Patching.PatchAttributionGuard.OwnersFor(frame);
                if (frameOwners == null)
                {
                    // flag only: this frame still belongs in the formatted trace
                    attributionUnavailable = true;
                }
                else
                {
                    foreach (string owner in frameOwners)
                    {
                        owners ??= new System.Collections.Generic.List<string>();
                        if (!owners.Contains(owner)) owners.Add(owner);
                    }
                }
            }

            System.Type? declaringType = method?.DeclaringType;
            string? declaring = declaringType?.FullName;
            string? assembly = declaringType?.Assembly.GetName().Name;
            if (CallerFrameClassifier.IsInternalFrame(declaring, assembly)) continue;
            string typeName = declaring ?? "<unknown>";
            string methodName = method?.Name ?? "<unknown>";
            string? file = frame.GetFileName();
            int line = frame.GetFileLineNumber();
            sb.Append("at ").Append(typeName).Append('.').Append(methodName);
            if (!string.IsNullOrEmpty(file))
            {
                sb.Append(" (").Append(NormalizePath(file, declaringType));
                if (line > 0) sb.Append(':').Append(line);
                sb.Append(')');
            }
            sb.Append('\n');
        }
        if (sb.Length > 0 && sb[sb.Length - 1] == '\n') sb.Length--;
        // one unanswerable frame makes the entry unanswerable: claiming the owners we did find
        // are the whole story would be a confident half-answer
        patchedBy = attributionUnavailable
            ? null
            : (System.Collections.Generic.IReadOnlyList<string>?)owners ?? System.Array.Empty<string>();
        return sb.ToString();
    }

    /// <summary>
    /// Shortens an absolute source path. With a declaring type it anchors on the assembly name
    /// and prefixes the mod folder; otherwise it strips known RimWorld layout prefixes.
    /// </summary>
    /// <param name="file">Raw file path from <c>StackFrame.GetFileName()</c>.</param>
    /// <param name="declaringType">
    /// Optional declaring type of the frame whose file path is being normalised.
    /// When provided, enables assembly-anchored resolution.
    /// </param>
    /// <returns>Short normalised path, never <c>null</c>.</returns>
    internal static string NormalizePath(string file, System.Type? declaringType = null)
    {
        if (string.IsNullOrEmpty(file)) return string.Empty;
        if (_normalizedPaths.TryGetValue(file, out string? cached)) return cached;
        (string computed, bool stable) = ComputeNormalizedPath(file, declaringType);
        if (stable) _normalizedPaths.TryAdd(file, computed);
        return computed;
    }


    /// <summary>
    /// Normalises a path, anchoring on the declaring type's assembly name when it can and
    /// falling back to regex prefix stripping when it cannot.
    /// </summary>
    /// <summary>
    /// Normalises the path and reports whether it is safe to cache. Unstable while the mod
    /// folder is unresolved but a provider exists, since a later call may resolve it.
    /// </summary>
    private static (string Path, bool Stable) ComputeNormalizedPath(string file, System.Type? declaringType)
    {
        if (declaringType != null)
        {
            AssemblyHint hint = GetHint(declaringType.Assembly);
            string? anchored = TryAnchor(file, hint);
            if (anchored != null) return (anchored, IsHintStable(hint));
        }

        foreach (System.Reflection.Assembly asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            AssemblyHint hint = GetHint(asm);
            string? anchored = TryAnchor(file, hint);
            if (anchored != null) return (anchored, IsHintStable(hint));
        }

        // no assembly anchor, so strip the /Mods/ prefix by regex and normalise Source/ the
        // same way, giving "Profiling/X" rather than "Dubs-Performance-Analyzer/Source/Profiling/X"
        string clean = _pathStrip.Replace(file, string.Empty);
        clean = _dupDir.Replace(clean, "$1\\");
        clean = clean.TrimStart('\\', '/');
        clean = StripLeadingSourceDir(clean);
        return (ToOsSeparators(StripCsExtension(clean)), true);
    }

    private static bool IsHintStable(AssemblyHint hint)
    {
        // Since TryAnchor no longer uses ModFolder, the result depends only on AssemblyName --
        // which is fixed for an assembly. Always safe to cache.
        _ = hint;
        return true;
    }

    private static string? TryAnchor(string file, AssemblyHint hint)
    {
        if (hint.AssemblyName == null) return null;
        string? rel = TryAnchorByAssembly(file, hint.AssemblyName);
        if (rel == null) return null;
        // relative to the anchor only: the channel column already names the mod, and a leading
        // "Source/" is a developer convention with nothing in it for the reader
        rel = StripLeadingSourceDir(rel);
        return ToOsSeparators(StripCsExtension(rel));
    }

    /// <summary>
    /// Drops a leading or second-segment "Source/" so paths read as "Foo/Bar.cs".
    /// The second-segment case covers sub-project containers like "Framework/Source/...".
    /// </summary>
    private static string StripLeadingSourceDir(string rel)
    {
        // Try Source/ stripping first.
        string? stripped = TryStripSourcePattern(rel, '/');
        if (stripped != null) return stripped;
        stripped = TryStripSourcePattern(rel, '\\');
        if (stripped != null) return stripped;
        // collapse a repeated "<X>/<X>/", which happens when the anchor lands on an outer mod
        // folder that nests the same name inside
        stripped = TryStripDuplicatePrefix(rel, '/');
        if (stripped != null) return stripped;
        stripped = TryStripDuplicatePrefix(rel, '\\');
        if (stripped != null) return stripped;
        return rel;
    }

    private static string? TryStripDuplicatePrefix(string rel, char sep)
    {
        int firstSep = rel.IndexOf(sep);
        if (firstSep <= 0) return null;
        int after = firstSep + 1;
        int segLen = firstSep;
        if (after + segLen + 1 > rel.Length) return null;
        if (string.CompareOrdinal(rel, 0, rel, after, segLen) != 0) return null;
        if (rel[after + segLen] != sep) return null;
        return rel.Substring(after + segLen + 1);
    }

    private static string? TryStripSourcePattern(string rel, char sep)
    {
        string sourceSeg = "Source" + sep;
        if (rel.StartsWith(sourceSeg, StringComparison.Ordinal))
            return rel.Substring(sourceSeg.Length);
        int firstSep = rel.IndexOf(sep);
        if (firstSep <= 0) return null;
        int after = firstSep + 1;
        if (after + sourceSeg.Length <= rel.Length &&
            string.CompareOrdinal(rel, after, sourceSeg, 0, sourceSeg.Length) == 0)
        {
            return rel.Substring(after + sourceSeg.Length);
        }
        return null;
    }

    private static string ToOsSeparators(string path)
    {
        char target = System.IO.Path.DirectorySeparatorChar;
        char other = target == '/' ? '\\' : '/';
        return path.IndexOf(other) >= 0 ? path.Replace(other, target) : path;
    }

    /// <summary>
    /// Searches  for the first occurrence of a path segment whose name equals  (matching either separator
    /// style) and returns the substring after that segment, or null if no such anchor exists.
    /// </summary>
    private static string? TryAnchorByAssembly(string file, string asmName)
    {
        // a "Foo.Library" folder builds assembly "Foo", so accept prefix segments too
        string? rel = TryAnchorSegment(file, asmName, '/');
        rel ??= TryAnchorSegment(file, asmName, '\\');
        return rel;
    }

    private static string? TryAnchorSegment(string file, string asmName, char sep)
    {
        string sepStr = sep.ToString();
        string exact = sepStr + asmName + sepStr;
        int idx = file.IndexOf(exact, StringComparison.Ordinal);
        if (idx >= 0) return file.Substring(idx + exact.Length);

        string prefix = sepStr + asmName + ".";
        idx = file.IndexOf(prefix, StringComparison.Ordinal);
        if (idx >= 0)
        {
            int afterPrefix = idx + prefix.Length;
            int nextSep = file.IndexOf(sep, afterPrefix);
            if (nextSep > 0) return file.Substring(nextSep + 1);
        }
        return null;
    }

    private static string StripCsExtension(string s)
    {
        if (s.EndsWith(".cs", StringComparison.Ordinal)) return s.Substring(0, s.Length - 3);
        return s;
    }

    /// <summary>
    /// Builds a hint from an assembly: its simple name anchors the source path, and the mod
    /// folder becomes the display prefix when it loaded from under <c>Mods/</c>.
    /// </summary>
    private static AssemblyHint ComputeAssemblyHint(System.Reflection.Assembly asm)
    {
        string? asmName = asm.GetName().Name;
        // prefer the Verse-supplied folder: it survives an empty Assembly.Location, which
        // happens when a mod is loaded from bytes
        string? modFolder = ModNameCache.FolderForAssembly(asm);
        if (modFolder == null)
        {
            string? location = TryGetAssemblyLocation(asm);
            modFolder = ParseModFolder(location);
        }
        return new AssemblyHint(asmName, modFolder);
    }


    /// <summary>
    /// Cached hint for the assembly, recomputed while the mod folder is unresolved so an
    /// early-load miss does not get cached forever.
    /// </summary>
    private static AssemblyHint GetHint(System.Reflection.Assembly asm)
    {
        if (_assemblyHints.TryGetValue(asm, out AssemblyHint cached) && cached.ModFolder != null)
            return cached;
        AssemblyHint fresh = ComputeAssemblyHint(asm);
        if (fresh.ModFolder != null) _assemblyHints[asm] = fresh;
        return fresh;
    }

    private static string? TryGetAssemblyLocation(System.Reflection.Assembly asm)
    {
        try
        {
            string location = asm.Location;
            return string.IsNullOrEmpty(location) ? null : location;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns the segment after <c>/Mods/</c>, so ".../Mods/RimObs/Assemblies/x.dll" gives
    /// "RimObs". Null outside a Mods directory, which is normal for tests and tooling.
    /// </summary>
    private static string? ParseModFolder(string? path)
    {
        if (path == null) return null;
        string normalized = path.Replace('\\', '/');
        int idx = normalized.IndexOf("/Mods/", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        int start = idx + "/Mods/".Length;
        int end = normalized.IndexOf('/', start);
        if (end < 0) return null;
        return normalized.Substring(start, end - start);
    }
}
