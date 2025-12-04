using Arch.Core;
using System.Diagnostics;

namespace TTRPG.Server;

internal class Program
{
    // The Target Frame Rate (60 updates per second)
    private const int TargetFps = 60;
    // How many milliseconds each frame should take (approx 16.6ms)
    private const long TimePerFrame = 1000 / TargetFps;

    static void Main(string[] args)
    {
        Console.WriteLine($"[Server] Initializing Cartridge Core Server...");

        // 1. Initialize the Arch ECS World
        // This 'world' container will hold all our entities (Goblins, Players)
        // and their data (Health, Position).
        var world = World.Create();
        Console.WriteLine($"[Server] Arch World created. ID: {world.Id}");

        Console.WriteLine("[Server] Server loop starting... (Press Ctrl+C to stop)");

        // 2. The Game Loop
        // We use a Stopwatch to ensure our game runs at a consistent speed,
        // regardless of how fast the computer processor is.
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        long lastTime = stopwatch.ElapsedMilliseconds;

        while (true)
        {
            long currentTime = stopwatch.ElapsedMilliseconds;
            long deltaTime = currentTime - lastTime;

            // Only update if enough time has passed (Fixed Time Step)
            if (deltaTime >= TimePerFrame)
            {
                lastTime = currentTime;

                // --- LOGIC FRAME START ---

                // TODO: In the future, we will tell our Systems to run here.
                // Example: movementSystem.Update(deltaTime);

                // For now, let's just prove it's alive every 600 frames (approx 10 seconds)
                if (DateTime.Now.Second % 10 == 0 && DateTime.Now.Millisecond < 20)
                {
                    Console.WriteLine($"[Heartbeat] Server is running... Active Entities: {world.CountEntities(new QueryDescription())}");
                }

                // --- LOGIC FRAME END ---
            }
            else
            {
                // If we finished our work early, sleep to save CPU power.
                Thread.Sleep(1);
            }
        }
    }
}