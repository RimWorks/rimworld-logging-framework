using RimWorks.RimLogging;
using UnityEngine;
using Verse;

namespace RimWorks.RimLogging.Settings;

/// <summary>RimWorld <see cref="Mod"/> entry point that loads the logging settings and applies the global minimum level.</summary>
public sealed class LoggingMod : Mod
{
    /// <summary>The loaded logging settings instance for this mod.</summary>
    public static LoggingSettings Settings { get; private set; } = null!;

    /// <summary>
    /// Loads settings, normalizes the log directory, and performs early bootstrap. Runs during
    /// mod loading (in mod-list order), well before any <see cref="Verse.StaticConstructorOnStartup"/>,
    /// so the Verse.Log hijack is live before other mods emit their load-time logs.
    /// </summary>
    /// <param name="content">The mod content pack provided by RimWorld.</param>
    public LoggingMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<LoggingSettings>();
        Settings.logDirectory = LogDirectoryResolver.Normalize(Settings.logDirectory, UnityEngine.Application.persistentDataPath);
        Bootstrap.EarlyInit.Run(Settings);
    }

    /// <summary>Renders the settings window contents and re-applies the global minimum level from the edited settings.</summary>
    /// <param name="inRect">The rect to draw the settings UI within.</param>
    public override void DoSettingsWindowContents(Rect inRect)
    {
        LoggingSettingsWindow.Render(Settings, inRect);
        Logging.GlobalMinLevel = Settings.globalMinLevel;
        Logging.CaptureStackTraces = Settings.captureStackTraces;
    }

    /// <summary>The category label shown for this mod in RimWorld's mod settings list.</summary>
    /// <returns>The settings category name.</returns>
    public override string SettingsCategory() => "RimLogging";
}
