using System;
using System.IO;
using System.Threading;
using Arch.Core; // <--- The ECS Core
using TTRPG.Server.Services;
using TTRPG.Shared.Components;

namespace TTRPG.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== SERVER STARTED ===");

            // 1. Initialize the ECS World (The "Game State")
            // This holds all entities (players, monsters, items).
            var world = World.Create();

            // 2. Load Blueprints (The "Campaign Cartridge")
            var loader = new BlueprintLoader();
            string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "monsters.yaml");
            var blueprints = loader.LoadBlueprints(dataPath);

            // 3. Initialize the Factory (The "Builder")
            // We pass the blueprints so the factory knows how to build a "goblin_grunt"
            var factory = new EntityFactory(blueprints);

            // 4. Spawn Entities (Simulating Level Load)
            if (blueprints.Count > 0)
            {
                Console.WriteLine("[Server] Spawning entities...");

                // Spawn a standard Goblin
                var goblin = factory.Create("goblin_grunt", world);

                // Apply the "Elite" Template (Decorator Pattern)
                factory.ApplyTemplate(goblin, "template_elite");
                
                // --- VERIFICATION START ---
                // We ask the World: "Give me the Stats and Health for this specific goblin entity"
                // Note: Ensure TTRPG.Shared.Components is using'd at the top
                if (world.Has<Stats>(goblin))
                {
                    var stats = world.Get<Stats>(goblin);
                    Console.WriteLine($"[Verify] Goblin Strength: {stats.Strength} (Expected: 18)");
                }

                if (world.Has<Health>(goblin))
                {
                    var health = world.Get<Health>(goblin);
                    Console.WriteLine($"[Verify] Goblin Health: {health.Current}/{health.Max} (Expected: 50/50)");
                }
            }

            // 5. Verify Spawns
            // We ask Arch: "How many entities exist right now?"
            var count = world.CountEntities(new QueryDescription());
            Console.WriteLine($"[Server] Total Entities Active in World: {count}");

            // 6. Start Networking
            var serverService = new ServerNetworkService();
            serverService.Start(9050);

            Console.WriteLine("Press ESC to stop...");

            // 7. Main Loop
            bool isRunning = true;
            while (isRunning)
            {
                // Network Tick
                serverService.Poll();

                // (Future: World.Update() would go here for game logic)

                Thread.Sleep(15);

                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    isRunning = false;
                }
            }

            // Cleanup
            serverService.Stop();
            World.Destroy(world);
            Console.WriteLine("Server stopped.");
        }
    }
}