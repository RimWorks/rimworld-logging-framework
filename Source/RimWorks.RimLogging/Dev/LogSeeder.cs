using System.Threading;

namespace RimWorks.RimLogging.Dev;

/// <summary>
/// Emits synthetic entries so the viewer has something to filter, scroll and navigate. Off unless
/// switched on from the dev menu, and never started on its own.
/// </summary>
internal static class LogSeeder
{
    private const int BurstCount = 400;
    private const int TrickleDelayMs = 700;

    private static readonly object Gate = new object();
    private static volatile bool running;
    private static Thread? worker;

    /// <summary>Whether the seeder is currently emitting.</summary>
    internal static bool IsRunning => running;

    /// <summary>Starts the seeder if it is stopped, stops it if it is running.</summary>
    /// <returns><c>true</c> when the seeder is running after the call.</returns>
    internal static bool Toggle()
    {
        lock (Gate)
        {
            if (running)
            {
                running = false;
                worker = null;
                return false;
            }

            running = true;
            worker = new Thread(Run) { IsBackground = true, Name = "RimLogging seeder" };
            worker.Start();
            return true;
        }
    }

    private static void Run()
    {
        // a burst first, so the list is long enough to scroll and to hold several errors
        for (int i = 0; i < BurstCount && running; i++)
        {
            Emit(i);
        }

        for (int i = BurstCount; running; i++)
        {
            Emit(i);
            Thread.Sleep(TrickleDelayMs);
        }
    }

    private static void Emit(int index)
    {
        SeedEntry seed = SeedEntry.At(index);
        if (!seed.Scoped)
        {
            Log.EmitCaptured(seed.Level, seed.Channel, seed.Message, seed.Stack, seed.Mod);
            return;
        }

        using (Log.PushContext("pawn", "Randy"))
        using (Log.PushContext("map", index % 3))
        {
            Log.EmitCaptured(seed.Level, seed.Channel, seed.Message, seed.Stack, seed.Mod);
        }
    }
}
