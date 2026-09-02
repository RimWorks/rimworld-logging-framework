using LudeonTK;
using RimWorld;
using Verse;

namespace RimWorks.RimLogging.Dev;

/// <summary>Exposes the log seeder in RimWorld's dev menu.</summary>
internal static class LogSeederDebugAction
{
    // Invalid means zero, and every state check reads (allowedGameStates & X) == 0 || state == X,
    // so a set bit is a requirement rather than a permission. Entry | Playing would demand both at
    // once and show nowhere; zero is the only value that allows the main menu and a running game.
    [DebugAction("RimLogging", "Toggle log seeding", allowedGameStates = AllowedGameStates.Invalid)]
    private static void ToggleLogSeeding()
    {
        bool running = LogSeeder.Toggle();
        Messages.Message(
            (running ? "CRL_Dev_SeedingOn" : "CRL_Dev_SeedingOff").Translate(),
            MessageTypeDefOf.TaskCompletion,
            false);
    }
}
