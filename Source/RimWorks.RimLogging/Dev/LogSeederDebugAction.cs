using LudeonTK;
using RimWorld;
using Verse;

namespace RimWorks.RimLogging.Dev;

/// <summary>Exposes the log seeder in RimWorld's dev menu.</summary>
internal static class LogSeederDebugAction
{
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
