using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Arch.Core;
using Microsoft.Extensions.DependencyInjection; // <--- NEW
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

            // --- PHASE 1: LOAD DATA ---
            // We load data *before* the container because the Factory needs it immediately.
            var blueprintLoader = new BlueprintLoader();

            // Load Monsters
            string monsterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "monsters.yaml");
            var blueprints = blueprintLoader.LoadBlueprints(monsterPath);

            // Load Items
            string itemPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "items.yaml");
            if (File.Exists(itemPath))
            {
                var items = blueprintLoader.LoadBlueprints(itemPath);
                blueprints.AddRange(items);
                Console.WriteLine($"[Loader] Merged {items.Count} items into blueprint database.");
            }

            // Load Ruleset
            var pluginLoader = new PluginLoader();
            string rulesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TTRPG.Rules.Pathfinder.dll");
            IRuleset? activeRuleset = null;

            if (File.Exists(rulesPath))
            {
                activeRuleset = pluginLoader.LoadRuleset(rulesPath);
                Console.WriteLine($"[Program] Loaded Rules: {activeRuleset.Name}");
            }
            else
            {
                Console.WriteLine("[Program] WARNING: Ruleset DLL not found.");
            }

            // --- PHASE 2: CONFIGURE SERVICES (Dependency Injection) ---
            var serviceCollection = new ServiceCollection();

            // 1. Core State
            // Register World as a Singleton so everyone gets the SAME world
            var world = World.Create();
            serviceCollection.AddSingleton(world);

            if (activeRuleset != null)
            {
                activeRuleset.Register(world);
                serviceCollection.AddSingleton(activeRuleset);
            }

            // 2. Services
            serviceCollection.AddSingleton<EntityFactory>(new EntityFactory(blueprints)); // Inject pre-loaded blueprints
            serviceCollection.AddSingleton<MapService>();
            serviceCollection.AddSingleton<ServerNetworkService>();
            serviceCollection.AddSingleton<NotificationService>();
            serviceCollection.AddSingleton<GameLoopService>(); // Automatically gets Network, World, Notification, Map injected!

            // 3. Build Provider
            var provider = serviceCollection.BuildServiceProvider();

            // --- PHASE 3: INITIALIZE ---

            // Init Map
            var mapService = provider.GetRequiredService<MapService>();
            string mapPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "test_map.tmx");
            mapService.LoadMap(mapPath);

            // Init Network
            var network = provider.GetRequiredService<ServerNetworkService>();
            network.SetWorld(world);
            network.SetFactory(provider.GetRequiredService<EntityFactory>());
            if (activeRuleset != null) network.SetRuleset(activeRuleset);

            network.Start(9050);

            // Spawn Initial World State
            var factory = provider.GetRequiredService<EntityFactory>();
            if (blueprints.Count > 0)
            {
                // Potion Spawn
                var potion = factory.Create("potion_healing", world);
                var potionPos = new Position { X = 3, Y = 3 };
                var zId = GameLoopService.GetZoneIdForPosition(potionPos.X, potionPos.Y);
                world.Add(potion, potionPos, new Zone { Id = zId });
                Console.WriteLine($"[Server] Spawned Potion at {potionPos.X},{potionPos.Y}");
            }

            // --- PHASE 4: GAME LOOP ---
            var gameLoop = provider.GetRequiredService<GameLoopService>();
            var notificationService = provider.GetRequiredService<NotificationService>();

            // Hook Player Connection Logic
            network.OnPlayerConnected += (peer) =>
            {
                Console.WriteLine($"[Server] Spawning Player for Peer {peer.Id}...");
                var playerEntity = factory.Create("goblin_grunt", world);

                // Auto-Zone
                if (world.Has<Position>(playerEntity))
                {
                    var pos = world.Get<Position>(playerEntity);
                    var zoneId = GameLoopService.GetZoneIdForPosition(pos.X, pos.Y);
                    world.Add(playerEntity, new Zone { Id = zoneId });
                }

                network.RegisterPlayerEntity(peer, playerEntity);
            };

            Console.WriteLine("Server Running. Press ESC to stop.");

            var stopwatch = Stopwatch.StartNew();
            bool isRunning = true;

            while (isRunning)
            {
                float deltaTime = (float)stopwatch.Elapsed.TotalSeconds;
                stopwatch.Restart();

                network.Poll();
                gameLoop.Update(deltaTime);

                Thread.Sleep(15);

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Escape) isRunning = false;
                    if (key == ConsoleKey.C) notificationService.ToggleMode();
                }
            }

            network.Stop();
            World.Destroy(world);
            Console.WriteLine("Server stopped.");
        }
    }
}