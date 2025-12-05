using System;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib;
using TTRPG.Shared;
using TTRPG.Shared.Components;
using TTRPG.Shared.Enums;

namespace TTRPG.Server.Services
{
    public class GameLoopService
    {
        private readonly ServerNetworkService _network;
        private World _world;
        private Random _random = new Random();

        public GameState CurrentState { get; private set; } = GameState.Exploration;

        // ENCOUNTER LOGIC
        private int _stepsTaken = 0;
        private int _stepsUntilEncounter = 10; // First battle happens after 10 steps

        public GameLoopService(ServerNetworkService network, World world)
        {
            _network = network;
            _world = world;
            _network.OnPlayerInput += HandlePlayerMove;

            // Randomize first encounter slightly
            _stepsUntilEncounter = _random.Next(5, 15);
        }

        public void Update(float deltaTime)
        {
            // REMOVED: The Timer Logic. 
            // We no longer switch states automatically.
        }

        private void HandlePlayerMove(Entity entity, MoveDirection direction)
        {
            if (CurrentState != GameState.Exploration)
            {
                // In Combat, you cannot move!
                return;
            }

            if (_world.Has<Position>(entity))
            {
                ref var pos = ref _world.Get<Position>(entity);
                var zone = _world.Get<Zone>(entity);

                // 1. Move the Player
                switch (direction)
                {
                    // TEST ZONES
                    case MoveDirection.Up:
                        // Set Zone A
                        _world.Set(entity, new Zone { Id = "Zone_A" });
                        Console.WriteLine($"[Debug] Entity {entity.Id} entered Zone_A");
                        break;
                    case MoveDirection.Down:
                        // Set Zone B
                        _world.Set(entity, new Zone { Id = "Zone_B" });
                        Console.WriteLine($"[Debug] Entity {entity.Id} entered Zone_B");
                        break;

                    // Regular Movement
                    case MoveDirection.Left: pos.X -= 1; break;
                    case MoveDirection.Right: pos.X += 1; break;
                }

                Console.WriteLine($"[GameLoop] Entity {entity.Id} moved to {pos.X}, {pos.Y}");
                // RELEVANCY: Only broadcast to people in THIS zone
                BroadcastPositionToZone(entity.Id, pos, zone.Id);

                // 2. CHECK FOR ENCOUNTER
                _stepsTaken++;
                CheckForEncounter();
            }

        }

        private void BroadcastPositionToZone(int entityId, Position pos, string zoneId)
        {
            var packet = new EntityPositionPacket { EntityId = entityId, Position = pos };
            // Use the new filtering method
            _network.BroadcastToZone(zoneId, packet);
        }

        private void CheckForEncounter()
        {
            Console.WriteLine($"[GameLoop] Steps: {_stepsTaken}/{_stepsUntilEncounter}");

            if (_stepsTaken >= _stepsUntilEncounter)
            {
                Console.WriteLine("[GameLoop] ENCOUNTER TRIGGERED!");
                StartCombat();
            }
        }

        private void StartCombat()
        {
            CurrentState = GameState.Combat;
            BroadcastState();

            // In a real game, we would spawn enemies here.
            // For now, we just stay in combat for 5 seconds then go back, 
            // simulating a quick "Auto-Win" battle.

            System.Threading.Tasks.Task.Delay(5000).ContinueWith(_ => EndCombat());
        }

        private void EndCombat()
        {
            CurrentState = GameState.Exploration;
            _stepsTaken = 0;
            _stepsUntilEncounter = _random.Next(8, 20); // Reset threshold

            Console.WriteLine($"[GameLoop] Combat Ended. Next battle in {_stepsUntilEncounter} steps.");
            BroadcastState();
        }

        private void BroadcastState()
        {
            var packet = new GameStatePacket { NewState = CurrentState };
            _network.BroadcastPacket(packet);
        }

        private void BroadcastPosition(int entityId, Position pos)
        {
            var packet = new EntityPositionPacket { EntityId = entityId, Position = pos };
            _network.BroadcastPacket(packet);
        }
    }
}