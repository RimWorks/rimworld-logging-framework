using System.IO;
using UnityEngine;

namespace RimWorks.RimLogging.Settings;

/// <summary>Provides the default log output directory location.</summary>
public static class LogDirectory
{
    /// <summary>The default log directory (a "RimLogging" folder under Unity's persistent data path), created if missing.</summary>
    public static string Default => LogDirectoryResolver.EnsureUnderBase(Application.persistentDataPath);
}
