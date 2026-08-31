using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Concord;
using Verse;

namespace RimWorks.RimLogging.Bootstrap;

/// <summary>
/// Late-loads the optional Lightweave log viewer companion assembly when <c>Cosmere.Lightweave</c> is
/// active. The viewer DLL ships outside any <c>Assemblies/</c> folder so RimWorld never auto-validates
/// it at mod-load time (its base types live in Lightweave, which loads after RimLogging). Loading it
/// here, at <see cref="StaticConstructorOnStartup"/>, runs after every mod's assemblies are present, so
/// all Lightweave references resolve. This type holds no compile-time reference to the viewer or to
/// Lightweave; it drives them purely through reflection and Concord.
/// </summary>
[StaticConstructorOnStartup]
internal static class LightweaveViewerLoader
{
    private const string LightweavePackageId = "cosmere.lightweave";
    private const string SelfPackageId = "rimworks.rimlogging";
    private const string ViewerFolder = "LightweaveViewer";
    private const string ViewerDll = "RimWorks.RimLogging.LightweaveViewer.dll";
    private const string BootTypeName = "RimWorks.RimLogging.LightweaveViewer.LogViewerBoot";

    static LightweaveViewerLoader()
    {
        try
        {
            if (ModLister.GetActiveModWithIdentifier(LightweavePackageId, ignorePostfix: true) == null)
            {
                return;
            }

            string? dllPath = ResolveViewerDllPath();
            if (dllPath == null || !File.Exists(dllPath))
            {
                Log.Warn("Lightweave is active but the log viewer assembly was not found at " + (dllPath ?? "<unknown>"));
                return;
            }

            // Load from bytes (not LoadFrom) so the rule against path-based loading is satisfied;
            // every dependency (Lightweave, this core assembly, Verse/Unity) is already resident in
            // the AppDomain by the time this static ctor runs, so byte-loading resolves them by identity.
            Assembly viewer = Assembly.Load(File.ReadAllBytes(dllPath));
            viewer.GetType(BootTypeName)
                ?.GetMethod("Init", BindingFlags.Public | BindingFlags.Static)
                ?.Invoke(null, null);
            Patcher.Apply(viewer);
        }
        catch (Exception ex)
        {
            Log.Warn("Failed to load the Lightweave log viewer: " + ex);
        }
    }

    private static string? ResolveViewerDllPath()
    {
        ModContentPack? self = LoadedModManager.RunningModsListForReading
            .FirstOrDefault(pack => pack.PackageId.Equals(SelfPackageId, StringComparison.OrdinalIgnoreCase));
        return self == null ? null : Path.Combine(self.RootDir, ViewerFolder, ViewerDll);
    }
}
