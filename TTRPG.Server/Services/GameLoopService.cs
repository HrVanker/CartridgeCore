using System;
using System.Collections.Generic;
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
        private readonly NotificationService _notifications;
        private readonly MapService _mapService;

        // NEW: Timer for Heartbeat
        private float _heartbeatTimer = 0f;
        private const float HEARTBEAT_RATE = 0.5f; // Send updates every 500ms

        // NEW: Arch Query to find everyone
        private Arch.Core.QueryDescription _entitiesQuery = new Arch.Core.QueryDescription().WithAll<Position, Zone>();

        // 1. STATE PER ZONE: We track the status of each room independently
        private Dictionary<string, GameState> _zoneStates = new Dictionary<string, GameState>();
        private Dictionary<string, int> _zoneSteps = new Dictionary<string, int>();
        private Dictionary<string, int> _zoneThresholds = new Dictionary<string, int>();

        public GameLoopService(ServerNetworkService network, World world, NotificationService notifications, MapService mapService)
        {
            _network = network;
            _world = world;
            _network.OnPlayerInput += HandlePlayerMove;
            _notifications = notifications;
            _mapService = mapService;
        }

        // Helper to safely get state (Default to Exploration)
        private GameState GetZoneState(string zoneId)
        {
            return _zoneStates.ContainsKey(zoneId) ? _zoneStates[zoneId] : GameState.Exploration;
        }

        public void Update(float deltaTime)
        {
            // 1. Run Heartbeat Logic
            _heartbeatTimer += deltaTime;
            if (_heartbeatTimer >= HEARTBEAT_RATE)
            {
                _heartbeatTimer = 0f;
                SendHeartbeat();
            }
        }
        private void SendHeartbeat()
        {
            // Iterate over ALL entities with Position + Zone
            _world.Query(in _entitiesQuery, (Entity entity, ref Position pos, ref Zone zone) =>
            {
                // Re-broadcast their location to their specific zone
                // This keeps the Client's "TTL" timer fresh so they don't disappear.
                BroadcastPositionToZone(entity.Id, pos, zone.Id);
            });
        }

        private void HandlePlayerMove(Entity entity, MoveDirection direction)
        {
            if (_world.Has<Position>(entity) && _world.Has<Zone>(entity))
            {
                var currentZone = _world.Get<Zone>(entity);

                // 1. Combat Lock Check
                if (GetZoneState(currentZone.Id) == GameState.Combat)
                {
                    return;
                }

                // 2. Calculate Proposed Position
                var currentPos = _world.Get<Position>(entity);
                int targetX = currentPos.X;
                int targetY = currentPos.Y;

                switch (direction)
                {
                    case MoveDirection.Up: targetY -= 1; break;
                    case MoveDirection.Down: targetY += 1; break;
                    case MoveDirection.Left: targetX -= 1; break;
                    case MoveDirection.Right: targetX += 1; break;
                }

                // 3. COLLISION CHECK (New Logic)
                // If the target tile is NOT walkable, we simply return (ignore the input).
                // Note: We only check collision if we are inside the map bounds.
                // For this prototype, if the map isn't loaded or coordinates are huge, we default to "True" in MapService.
                if (!_mapService.IsWalkable(targetX, targetY))
                {
                    Console.WriteLine($"[Physics] Blocked move to {targetX},{targetY} (Wall)");
                    return;
                }

                // 4. Apply Move
                ref var pos = ref _world.Get<Position>(entity);
                pos.X = targetX;
                pos.Y = targetY;

                // 5. Zone Change Logic
                string newZoneId = (pos.X >= 0) ? "Zone_B" : "Zone_A";

                if (currentZone.Id != newZoneId)
                {
                    Console.WriteLine($"[GameLoop] Entity {entity.Id} crossed from {currentZone.Id} to {newZoneId}");
                    _world.Set(entity, new Zone { Id = newZoneId });
                    BroadcastStateToZone(newZoneId);
                }

                BroadcastPositionToZone(entity.Id, pos, newZoneId);
                IncrementSteps(newZoneId);
            }
        }

        private void IncrementSteps(string zoneId)
        {
            if (!_zoneSteps.ContainsKey(zoneId))
            {
                _zoneSteps[zoneId] = 0;
                _zoneThresholds[zoneId] = _random.Next(5, 15);
            }
            _zoneSteps[zoneId]++;
            if (_zoneSteps[zoneId] >= _zoneThresholds[zoneId])
            {
                Console.WriteLine($"[{zoneId}] ENCOUNTER STARTED!");
                StartCombat(zoneId);
            }
        }
        private void StartCombat(string zoneId)
        {
            _zoneStates[zoneId] = GameState.Combat;
            BroadcastStateToZone(zoneId);
            _notifications.BroadcastZoneEvent(zoneId, "ENCOUNTER STARTED!");
            System.Threading.Tasks.Task.Delay(5000).ContinueWith(_ => EndCombat(zoneId));
        }
        private void EndCombat(string zoneId)
        {
            _zoneStates[zoneId] = GameState.Exploration;
            _zoneSteps[zoneId] = 0;
            _zoneThresholds[zoneId] = _random.Next(8, 20);
            Console.WriteLine($"[{zoneId}] Combat Ended.");
            BroadcastStateToZone(zoneId);
            _notifications.BroadcastZoneEvent(zoneId, "Combat Ended.");
        }
        private void BroadcastStateToZone(string zoneId)
        {
            var packet = new GameStatePacket { NewState = GetZoneState(zoneId) };
            _network.BroadcastToZone(zoneId, packet);
        }
        private void BroadcastPositionToZone(int entityId, Position pos, string zoneId)
        {
            var packet = new EntityPositionPacket { EntityId = entityId, Position = pos };
            _network.BroadcastToZone(zoneId, packet);
        }
    }
}