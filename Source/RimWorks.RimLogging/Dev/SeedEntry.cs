namespace RimWorks.RimLogging.Dev;

/// <summary>One synthetic entry the seeder emits. Pure so the shape of a seed run is testable.</summary>
internal readonly struct SeedEntry
{
    private static readonly (string Channel, string? Mod)[] Sources =
    {
        ("mod.harmony", "Harmony"),
        ("mod.cosmere", "Cosmere"),
        ("default", null),
        ("Vanilla", null),
    };

    private static readonly string[] Chatter =
    {
        "pawn roster rebuilt",
        "pathfinding grid recalculated",
        "texture atlas repacked",
        "def cross-references resolved",
        "weather transitioned to rain",
    };

    private SeedEntry(LogLevel level, string channel, string? mod, string message, string? stack)
    {
        Level = level;
        Channel = channel;
        Mod = mod;
        Message = message;
        Stack = stack;
    }

    internal LogLevel Level { get; }

    internal string Channel { get; }

    internal string? Mod { get; }

    internal string Message { get; }

    internal string? Stack { get; }

    /// <summary>Whether this entry carries ambient scope, so scoped context is exercised too.</summary>
    internal bool Scoped => Stack != null;

    /// <summary>
    /// Builds the entry for a position in the run. Errors land every 25 and a fatal every 100, so
    /// a seeded log has something for error navigation to walk without drowning the list.
    /// </summary>
    internal static SeedEntry At(int index)
    {
        (string channel, string? mod) = Sources[index % Sources.Length];

        if (index % 25 == 24)
        {
            return new SeedEntry(
                index % 100 == 99 ? LogLevel.Fatal : LogLevel.Error,
                channel,
                mod,
                $"[{index}] Unhandled exception ticking pawn",
                "at Verse.Pawn.Tick ()\n  at Verse.TickList.Tick ()");
        }

        LogLevel level = (index % 7) switch
        {
            0 => LogLevel.Trace,
            1 => LogLevel.Debug,
            2 or 3 or 4 => LogLevel.Info,
            _ => LogLevel.Warn,
        };
        return new SeedEntry(level, channel, mod, $"[{index}] {Chatter[index % Chatter.Length]}", null);
    }
}
