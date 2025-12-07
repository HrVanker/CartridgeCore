using System;
using System.Diagnostics; // Required for Stopwatch
using System.IO;
using System.Threading;
using Arch.Core; // <--- The ECS Core
using TTRPG.Server.Services;
using TTRPG.Shared.Components;
using TTRPG.Core;

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
            var loader = new BlueprintLoader();
            string manifestPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "manifest.yaml");
            var manifest = loader.LoadManifest(manifestPath);
            var mapService = new MapService();
            // Load the test map for now (In Phase 3, this will be dynamic based on the Zone)
            string mapPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "test_map.tmx");
            mapService.LoadMap(mapPath);
            var pluginLoader = new PluginLoader();
            string rulesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TTRPG.Rules.Pathfinder.dll");

            IRuleset activeRuleset = null;
            if (File.Exists(rulesPath))
            {
                activeRuleset = pluginLoader.LoadRuleset(rulesPath);
                activeRuleset.Register(world); // Initialize it
                Console.WriteLine($"[Program] Active Rules: {activeRuleset.Name}");
            }
            else
            {
                Console.WriteLine("[Program] WARNING: Pathfinder DLL not found.");
            }

            Console.WriteLine($"[Cartridge] Loading '{manifest.Name}' (v{manifest.Version}) by {manifest.Author}");

            // 2. Load Blueprints (The "Campaign Cartridge")
            string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "monsters.yaml");
            var blueprints = loader.LoadBlueprints(dataPath);
            foreach (var dep in manifest.Dependencies)
            {
                Console.WriteLine($"  - Requires: {dep.Key} (v{dep.Value}+)");
                // Simulation: We assume "Core_Rules" is always present.
                if (dep.Key == "Core_Rules")
                {
                    Console.WriteLine("    [OK] Dependency Satisfied.");
                }
                else
                {
                    Console.WriteLine("    [WARNING] Missing Dependency!");
                }
            }

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
                factory.ApplyTemplate(goblin, "template_elite", world);
                
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
            serverService.SetWorld(world);
            if (activeRuleset != null) serverService.SetRuleset(activeRuleset);
            serverService.Start(9050);

            Console.WriteLine("Press ESC to stop...");

            // 7. Main Loop
            var stopwatch = Stopwatch.StartNew();
            var notificationService = new NotificationService(serverService, world);

            // 8. HOOK UP SPAWNING LOGIC
            // When a peer connects, create a Goblin for them!
            var gameLoop = new GameLoopService(serverService, world, notificationService, mapService);
            serverService.OnPlayerConnected += (peer) =>
            {
                Console.WriteLine($"[Server] Spawning Player for Peer {peer.Id}...");

                var playerEntity = factory.Create("goblin_grunt", world);

                // ADD ZONE COMPONENT
                world.Add(playerEntity, new Zone { Id = "Zone_A" }); // Start in Zone A

                serverService.RegisterPlayerEntity(peer, playerEntity);
            };

            bool isRunning = true;
            while (isRunning)
            {
                // Calculate Delta Time
                float deltaTime = (float)stopwatch.Elapsed.TotalSeconds;
                stopwatch.Restart();

                serverService.Poll();
                gameLoop.Update(deltaTime);

                Thread.Sleep(15);

                // FIX: READ KEY ONCE
                if (Console.KeyAvailable)
                {
                    // Consumes the key from the buffer immediately
                    var key = Console.ReadKey(true).Key;

                    if (key == ConsoleKey.Escape)
                    {
                        isRunning = false;
                    }
                    else if (key == ConsoleKey.C)
                    {
                        // Now 'C' will actually trigger!
                        notificationService.ToggleMode();
                    }
                }
            }

            // Cleanup
            serverService.Stop();
            World.Destroy(world);
            Console.WriteLine("Server stopped.");
        }
    }
}