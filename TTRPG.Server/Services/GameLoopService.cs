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
        private readonly NotificationService _notifications;
        private readonly MapService _mapService;
        private World _world;
        private Random _random = new Random();

        private float _heartbeatTimer = 0f;
        private const float HEARTBEAT_RATE = 0.2f; // Faster updates for smoother movement
        private Arch.Core.QueryDescription _entitiesQuery = new Arch.Core.QueryDescription().WithAll<Position, Zone>();

        private Dictionary<string, GameState> _zoneStates = new Dictionary<string, GameState>();
        private Dictionary<string, int> _zoneSteps = new Dictionary<string, int>();
        private Dictionary<string, int> _zoneThresholds = new Dictionary<string, int>();

        public GameLoopService(ServerNetworkService network, World world, NotificationService notifications, MapService mapService)
        {
            _network = network;
            _world = world;
            _notifications = notifications;
            _mapService = mapService;

            _network.OnPlayerInput += HandlePlayerMove;

            // --- CRITICAL: RESTORE THIS SUBSCRIPTION ---
            _network.OnPlayerAction += HandlePlayerAction;
        }

        // STATIC HELPER for Zone Logic
        public static string GetZoneIdForPosition(int x, int y)
        {
            if (x >= 25) return "Zone_B";
            return "Zone_A";
        }

        private GameState GetZoneState(string zoneId)
        {
            return _zoneStates.ContainsKey(zoneId) ? _zoneStates[zoneId] : GameState.Exploration;
        }

        public void Update(float deltaTime)
        {
            _heartbeatTimer += deltaTime;
            if (_heartbeatTimer >= HEARTBEAT_RATE)
            {
                _heartbeatTimer = 0f;
                SendHeartbeat();
            }
        }

        private void SendHeartbeat()
        {
            _world.Query(in _entitiesQuery, (Entity entity, ref Position pos, ref Zone zone) =>
            {
                // FIX: Pass the whole Entity, not just ID
                BroadcastPositionToZone(entity, pos, zone.Id);
            });
        }

        private void HandlePlayerMove(Entity entity, MoveDirection direction)
        {
            if (_world.Has<Position>(entity) && _world.Has<Zone>(entity))
            {
                var currentZone = _world.Get<Zone>(entity);
                if (GetZoneState(currentZone.Id) == GameState.Combat) return;

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

                if (!_mapService.IsWalkable(targetX, targetY))
                {
                    // Console.WriteLine($"[Physics] Blocked move to {targetX},{targetY} (Wall)");
                    return;
                }

                ref var pos = ref _world.Get<Position>(entity);
                pos.X = targetX;
                pos.Y = targetY;

                string newZoneId = GetZoneIdForPosition(pos.X, pos.Y);

                if (currentZone.Id != newZoneId)
                {
                    Console.WriteLine($"[GameLoop] Entity {entity.Id} crossed from {currentZone.Id} to {newZoneId}");
                    _world.Set(entity, new Zone { Id = newZoneId });
                    BroadcastStateToZone(newZoneId);
                }

                BroadcastPositionToZone(entity, pos, newZoneId);
                IncrementSteps(newZoneId);
            }
        }

        // --- NEW: PICKUP LOGIC ---
        private void HandlePlayerAction(Entity player, ActionType action)
        {
            Console.WriteLine($"[GameLoop] Action Received: {action} from Entity {player.Id}");
            if (action == ActionType.Pickup)
            {
                HandlePickup(player);
            }
        }

        private void HandlePickup(Entity player)
        {
            bool hasPos = _world.Has<Position>(player);
            bool hasInv = _world.Has<Inventory>(player);

            if (!hasPos || !hasInv)
            {
                Console.WriteLine($"[GameLoop] Pickup Failed: Position={hasPos}, Inventory={hasInv}");
                return;
            }
            if (!_world.Has<Position>(player) || !_world.Has<Inventory>(player))
            {
                Console.WriteLine("[GameLoop] Pickup Failed: Player missing Position or Inventory.");
                return;
            }

            var playerPos = _world.Get<Position>(player);
            var inventory = _world.Get<Inventory>(player);

            Entity itemEntity = Entity.Null;
            string itemName = "";
            string itemId = "";

            // Query for Items at the exact same location
            // Use WithAll to ensure we only get valid items
            var query = new QueryDescription().WithAll<Position, Item>();

            _world.Query(in query, (Entity e, ref Position pos, ref Item item) =>
            {
                // Logic: Must be at same tile, and NOT be the player
                if (pos.X == playerPos.X && pos.Y == playerPos.Y && e.Id != player.Id)
                {
                    itemEntity = e;
                    itemName = item.Name;
                    itemId = item.Id;
                }
            });

            if (itemEntity != Entity.Null)
            {
                Console.WriteLine($"[GameLoop] Success! Picking up {itemName}");

                if (inventory.Items == null) inventory.Items = new System.Collections.Generic.List<string>();

                if (inventory.Items.Count < inventory.Capacity)
                {
                    inventory.Items.Add(itemId);
                    _world.Destroy(itemEntity); // Remove from world

                    var chat = new ChatMessagePacket { Sender = "System", Message = $"You picked up {itemName}." };
                    _network.BroadcastPacket(chat);
                }
                else
                {
                    var chat = new ChatMessagePacket { Sender = "System", Message = "Inventory full!" };
                    _network.BroadcastPacket(chat);
                }
            }
            else
            {
                Console.WriteLine($"[GameLoop] No item found at {playerPos.X},{playerPos.Y}");
            }
        }

        // ... (Keep existing IncrementSteps, StartCombat, EndCombat, Broadcast Helpers) ...
        private void IncrementSteps(string zoneId)
        {
            if (!_zoneSteps.ContainsKey(zoneId)) { _zoneSteps[zoneId] = 0; _zoneThresholds[zoneId] = 15; }
            _zoneSteps[zoneId]++;
            if (_zoneSteps[zoneId] >= _zoneThresholds[zoneId]) StartCombat(zoneId);
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
            BroadcastStateToZone(zoneId);
            _notifications.BroadcastZoneEvent(zoneId, "Combat Ended.");
        }
        private void BroadcastStateToZone(string zoneId)
        {
            var packet = new GameStatePacket { NewState = GetZoneState(zoneId) };
            _network.BroadcastToZone(zoneId, packet);
        }
        private void BroadcastPositionToZone(Entity entity, Position pos, string zoneId)
        {
            var packet = new EntityPositionPacket
            {
                EntityId = entity.Id,
                Position = pos,
                SpriteId = "goblin" // Default
            };

            // 1. Check Sprite Component
            if (_world.Has<Sprite>(entity))
            {
                packet.SpriteId = _world.Get<Sprite>(entity).Texture;
            }
            // 2. Check Item Component
            else if (_world.Has<Item>(entity))
            {
                packet.SpriteId = _world.Get<Item>(entity).Icon;
            }

            // Debug log if empty (Safety check)
            if (string.IsNullOrEmpty(packet.SpriteId))
            {
                Console.WriteLine($"[GameLoop] Warning: Entity {entity.Id} has empty SpriteId!");
                packet.SpriteId = "goblin";
            }

            _network.BroadcastToZone(zoneId, packet);
        }
    }
}