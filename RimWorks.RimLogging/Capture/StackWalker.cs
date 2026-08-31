using System;

namespace RimWorks.RimLogging.Capture;

/// <summary>
/// Runtime fallback that resolves the originating <see cref="SourceLocation"/> by
/// walking the managed stack when compile-time caller attributes are unavailable.
/// </summary>
public static class StackWalker
{
    private static readonly System.Text.RegularExpressions.Regex _pathStrip = new(
        @"^.*?(RimworldCosmere[\\/]RimworldCosmere[\\/]|RimWorld[\\/]Mods[\\/])+[\\/]*",
        System.Text.RegularExpressions.RegexOptions.Compiled);
    private static readonly System.Text.RegularExpressions.Regex _dupDir = new(
        @"^(\w+)[\\/]\1[\\/]",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<System.Reflection.Assembly, AssemblyHint> _assemblyHints =
        new System.Collections.Concurrent.ConcurrentDictionary<System.Reflection.Assembly, AssemblyHint>();

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _normalizedPaths =
        new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

    /// <summary>
    /// Per-assembly hint used by <see cref="NormalizePath(string, System.Type)"/> to anchor
    /// embedded source paths. <see cref="AssemblyName"/> matches a path segment in the source
    /// path; <see cref="ModFolder"/> is parsed from <see cref="System.Reflection.Assembly.Location"/>
    /// and prepended to the result so the user sees the in-game mod folder name rather than the
    /// developer's project layout.
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
    /// Walks the current stack and returns the first frame outside the
    /// <c>RimWorks.RimLogging.</c> namespace, normalising its file path so the
    /// resulting <see cref="SourceLocation"/> is stable across machines and layouts.
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
            // Bug fix: vanilla Verse/Unity frames have no PDB, so GetFileName() returns null.
            // Previously we bailed here, which meant the real user-code frame underneath was
            // never visited even when its PDB was loaded. Keep walking instead.
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
    /// Formats a <see cref="System.Diagnostics.StackTrace"/> into a multi-line
    /// string of <c>at Type.Method (file:line)</c> entries for every frame
    /// outside the <c>RimWorks.RimLogging.</c> namespace. Paths are
    /// normalised the same way <see cref="FirstCallerFrame"/> normalises them.
    /// </summary>
    /// <param name="st">A pre-captured stack trace to format.</param>
    /// <returns>A formatted trace string; empty when no qualifying frames exist.</returns>
    public static string FormatTrace(System.Diagnostics.StackTrace st)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < st.FrameCount; i++)
        {
            System.Diagnostics.StackFrame? frame = st.GetFrame(i);
            if (frame == null) continue;
            System.Reflection.MethodBase? method = frame.GetMethod();
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
        return sb.ToString();
    }

    /// <summary>
    /// Normalises an absolute source-file path into a short, stable form. When
    /// <paramref name="declaringType"/> is supplied, the assembly's simple name is
    /// used as a stable anchor segment in the path and the mod folder (parsed from
    /// the assembly's load location) is used as the display prefix. Falls back to
    /// stripping common RimWorld layout prefixes via regex, collapsing duplicated
    /// sibling directories, and removing the trailing <c>.cs</c> extension.
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
    /// Computes the normalised path for a given input. Tries assembly-anchored resolution
    /// first (using the declaring type's assembly to find a stable anchor segment in the
    /// source path), then falls back to the legacy regex-based prefix stripping.
    /// </summary>
    /// <summary>
    /// Computes the normalised path along with a "stable" flag indicating whether the result
    /// can be safely cached. A hint is unstable when its <see cref="AssemblyHint.ModFolder"/>
    /// is still null but the folder provider exists -- meaning the folder may resolve on a
    /// subsequent call once more mods have loaded.
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

        // Legacy fallback: no asm anchored, so use the /Mods/ regex prefix-strip. After stripping
        // we apply the same Source/ pattern normalisation as the asm-anchored branch so paths like
        // /Mods/Dubs-Performance-Analyzer/Source/Profiling/X.cs collapse to "Profiling/X" instead
        // of "Dubs-Performance-Analyzer/Source/Profiling/X".
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
        // Show only the path relative to the asm anchor (no modFolder/asm prefix). The mod is
        // already identified by the channel column, so duplicating it here is noise. Also drop a
        // leading "Source/" segment -- a common project layout (Dubs, Lightweave subprojects, etc.)
        // -- because it's purely a developer-side convention with no signal for the reader.
        rel = StripLeadingSourceDir(rel);
        return ToOsSeparators(StripCsExtension(rel));
    }

    /// <summary>
    /// Normalises a path that's already relative to a project / mod root. Strips developer-side
    /// "Source/" conventions so the rendered path shows only the meaningful code location:
    ///   - "Source/Foo/Bar.cs"            -> "Foo/Bar.cs"           (Source/ as the leading segment)
    ///   - "Sub/Source/Foo/Bar.cs"        -> "Foo/Bar.cs"           (Source/ as the second segment;
    ///                                                              "Sub" is a sub-project container
    ///                                                              like Lightweave's "Framework" or
    ///                                                              Dubs' "Dubs-Performance-Analyzer")
    /// </summary>
    private static string StripLeadingSourceDir(string rel)
    {
        // Try Source/ stripping first.
        string? stripped = TryStripSourcePattern(rel, '/');
        if (stripped != null) return stripped;
        stripped = TryStripSourcePattern(rel, '\\');
        if (stripped != null) return stripped;
        // Collapse a leading "<X>/<X>/" duplication (common when the asm anchor lands on the
        // outer mod folder of a layout that nests the same name inside, e.g. LightweaveRimBridge
        // /LightweaveRimBridge/Bridge.cs). Without this collapse the rendered path repeats the
        // mod name redundantly.
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
    /// Searches <paramref name="file"/> for the first occurrence of a path segment whose
    /// name equals <paramref name="asmName"/> (matching either separator style) and returns
    /// the substring after that segment, or <c>null</c> if no such anchor exists.
    /// </summary>
    private static string? TryAnchorByAssembly(string file, string asmName)
    {
        // A common .NET project convention: project folder named "Foo.Library" or "Foo.Core"
        // produces assembly "Foo". Accept both exact segments (.../Foo/...) and prefix segments
        // (.../Foo.Library/...) so the runtime asm name still anchors the embedded source path.
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
    /// Builds an <see cref="AssemblyHint"/> from a runtime <see cref="System.Reflection.Assembly"/>:
    /// the assembly's simple name (used as the source-path anchor) and, when the assembly is loaded
    /// from a path under a <c>Mods/</c> directory, the mod folder name (used as the display prefix).
    /// </summary>
    private static AssemblyHint ComputeAssemblyHint(System.Reflection.Assembly asm)
    {
        string? asmName = asm.GetName().Name;
        // Prefer the Verse-supplied folder: it's the real /Mods/<folder>/ directory name and
        // is stable for RimWorld-loaded assemblies even when Assembly.Location is empty (which
        // happens when mods are loaded from byte[] or when running under degraded conditions).
        string? modFolder = ModNameCache.FolderForAssembly(asm);
        if (modFolder == null)
        {
            string? location = TryGetAssemblyLocation(asm);
            modFolder = ParseModFolder(location);
        }
        return new AssemblyHint(asmName, modFolder);
    }


    /// <summary>
    /// Returns the cached <see cref="AssemblyHint"/> for <paramref name="asm"/>, recomputing
    /// when the cached hint has no resolved <see cref="AssemblyHint.ModFolder"/>. This avoids
    /// permanently caching an under-resolved hint that was computed before
    /// <see cref="ModNameCache.FolderMap"/> finished populating (typical during early mod load).
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
    /// Parses the segment immediately after <c>/Mods/</c> from an assembly location path
    /// (e.g. <c>.../RimWorld/Mods/RimObs/Assemblies/RimObs.Library.dll</c> -> <c>RimObs</c>).
    /// Returns <c>null</c> when the path does not live under a <c>Mods/</c> directory, which is
    /// the common case for unit tests and tooling assemblies.
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
