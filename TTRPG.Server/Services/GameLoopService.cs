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
            if (CurrentState != GameState.Exploration) return;

            // Check if entity has required components
            if (_world.Has<Position>(entity) && _world.Has<Zone>(entity))
            {
                ref var pos = ref _world.Get<Position>(entity);

                // 1. RESTORED: Normal Movement for all directions
                switch (direction)
                {
                    case MoveDirection.Up: pos.Y -= 1; break;
                    case MoveDirection.Down: pos.Y += 1; break;
                    case MoveDirection.Left: pos.X -= 1; break;
                    case MoveDirection.Right: pos.X += 1; break;
                }

                // 2. NEW: Spatial Zoning Logic
                // If X is negative, we are in Zone_A. If X is positive, Zone_B.
                string newZoneId = (pos.X >= 0) ? "Zone_B" : "Zone_A";

                // Update the Zone Component
                _world.Set(entity, new Zone { Id = newZoneId });

                Console.WriteLine($"[GameLoop] Entity {entity.Id} moved to {pos.X},{pos.Y} (Zone: {newZoneId})");

                // 3. Broadcast to the Specific Zone
                BroadcastPositionToZone(entity.Id, pos, newZoneId);

                // 4. Encounter Logic
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