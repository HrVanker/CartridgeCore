using System;
using System.Collections.Generic;
using System.Net;
using Arch.Core;
using LiteNetLib;
using LiteNetLib.Utils;
using TTRPG.Shared;
using TTRPG.Shared.Enums;
using TTRPG.Shared.Components;
using Arch.Core.Extensions;
using TTRPG.Core;

namespace TTRPG.Server.Services
{
    public class ServerNetworkService : INetEventListener
    {
        private readonly Dictionary<int, (NetPeer Peer, Entity Entity)> _playerSessions = new Dictionary<int, (NetPeer, Entity)>();
        private readonly NetManager _netManager;
        private readonly NetPacketProcessor _packetProcessor;

        public int ConnectedPeersCount => _netManager.ConnectedPeersCount;
        public bool IsRunning => _netManager.IsRunning;

        public Action<NetPeer>? OnPlayerConnected;
        public Action<NetPeer, DisconnectInfo>? OnPlayerDisconnected;
        public Action<Entity, MoveDirection>? OnPlayerInput;
        public Action<NetPeer, ChatMessagePacket>? OnChatMessage;
        public Action<Entity, ActionType>? OnPlayerAction; // Required for Pickup

        private Arch.Core.World? _world;
        private IRuleset? _ruleset;
        private EntityFactory? _factory;

        public ServerNetworkService()
        {
            // --- FIX: THIS LINE WAS MISSING ---
            _packetProcessor = new NetPacketProcessor();
            // ----------------------------------

            // 1. Register Nested Types
            _packetProcessor.RegisterNestedType<TTRPG.Shared.Components.Position>(
                (writer, pos) => { writer.Put(pos.X); writer.Put(pos.Y); },
                (reader) => new TTRPG.Shared.Components.Position { X = reader.GetInt(), Y = reader.GetInt() }
            );

            // Register Dictionary (For Inspector)
            _packetProcessor.RegisterNestedType<Dictionary<string, string>>(
                (writer, dict) =>
                {
                    writer.Put(dict.Count);
                    foreach (var kvp in dict) { writer.Put(kvp.Key); writer.Put(kvp.Value); }
                },
                (reader) =>
                {
                    int count = reader.GetInt();
                    var dict = new Dictionary<string, string>();
                    for (int i = 0; i < count; i++) dict[reader.GetString()] = reader.GetString();
                    return dict;
                }
            );

            // Register InventoryData (For Phase 4.4) using JSON
            _packetProcessor.RegisterNestedType<TTRPG.Shared.DTOs.InventoryData>(
                (writer, data) => { writer.Put(Newtonsoft.Json.JsonConvert.SerializeObject(data)); },
                (reader) => { return Newtonsoft.Json.JsonConvert.DeserializeObject<TTRPG.Shared.DTOs.InventoryData>(reader.GetString()); }
            );

            // Register CharacterSheetData (For Phase 3.4) using JSON
            _packetProcessor.RegisterNestedType<TTRPG.Core.DTOs.CharacterSheetData>(
                (writer, sheet) => { writer.Put(Newtonsoft.Json.JsonConvert.SerializeObject(sheet)); },
                (reader) => { return Newtonsoft.Json.JsonConvert.DeserializeObject<TTRPG.Core.DTOs.CharacterSheetData>(reader.GetString()); }
            );

            // 2. Subscribe to Packets
            _packetProcessor.SubscribeReusable<JoinRequestPacket, NetPeer>(OnJoinReceived);
            _packetProcessor.SubscribeReusable<InspectEntityPacket, NetPeer>(OnInspectReceived);
            _packetProcessor.SubscribeReusable<PlayerMovePacket, NetPeer>(OnMoveReceived);
            _packetProcessor.SubscribeReusable<PlayerActionPacket, NetPeer>(OnActionReceived); // Required for Pickup
            _packetProcessor.SubscribeReusable<RequestInventoryPacket, NetPeer>(OnRequestInventory); // Required for Inventory Window
            _packetProcessor.SubscribeReusable<RequestSheetPacket, NetPeer>(OnSheetRequested);
            _packetProcessor.SubscribeReusable<ChatMessagePacket, NetPeer>((packet, peer) => OnChatMessage?.Invoke(peer, packet));

            _netManager = new NetManager(this);
        }

        public void Start(int port) { _netManager.Start(port); Console.WriteLine($"[Network] Server listening on port {port}..."); }
        public void Stop() => _netManager.Stop();
        public void Poll() => _netManager.PollEvents();

        public void SetWorld(Arch.Core.World world) => _world = world;
        public void SetRuleset(IRuleset ruleset) => _ruleset = ruleset;
        public void SetFactory(EntityFactory factory) => _factory = factory;

        // --- HANDLERS ---

        private void OnJoinReceived(JoinRequestPacket packet, NetPeer peer)
        {
            Console.WriteLine($"[Server] Join Request from {packet.Username}");
            var response = new JoinResponsePacket { Success = true, Message = $"Welcome {packet.Username}!" };
            BroadcastPacket(response, peer);
        }

        private void OnActionReceived(PlayerActionPacket packet, NetPeer peer)
        {
            if (_playerSessions.TryGetValue(peer.Id, out var session))
            {
                OnPlayerAction?.Invoke(session.Entity, packet.Action);
            }
        }

        private void OnInspectReceived(InspectEntityPacket packet, NetPeer peer)
        {
            if (_world == null) return;
            Arch.Core.Entity foundEntity = Arch.Core.Entity.Null;
            var query = new Arch.Core.QueryDescription();
            _world.Query(in query, (Arch.Core.Entity e) => { if (e.Id == packet.EntityId) foundEntity = e; });

            if (foundEntity != Arch.Core.Entity.Null && _world.IsAlive(foundEntity))
            {
                Entity viewer = GetEntityForPeer(peer);
                Dictionary<string, string> data;
                if (_ruleset != null) data = _ruleset.GetUI().GetInspectionDetails(_world, viewer, foundEntity);
                else data = new Dictionary<string, string> { { "Error", "No Ruleset Loaded" } };

                var response = new EntityDetailsPacket { EntityId = packet.EntityId, Stats = data };
                BroadcastPacket(response, peer);
                Console.WriteLine($"[Inspector] Sent {data.Count} stats for Entity {foundEntity.Id}");
            }
        }

        private void OnRequestInventory(RequestInventoryPacket packet, NetPeer peer)
        {
            if (_world == null || _factory == null) return;
            var player = GetEntityForPeer(peer);
            if (player == Arch.Core.Entity.Null) return;

            if (_world.Has<Inventory>(player))
            {
                var invComponent = _world.Get<Inventory>(player);
                var data = new TTRPG.Shared.DTOs.InventoryData { Capacity = invComponent.Capacity };

                if (invComponent.Items != null)
                {
                    foreach (var itemId in invComponent.Items)
                    {
                        var bp = _factory.GetBlueprint(itemId);
                        if (bp != null)
                        {
                            string name = bp.Name;
                            string icon = "goblin";
                            string desc = "";
                            if (bp.Components.TryGetValue("Item", out var itemRaw) && itemRaw is Dictionary<string, object> itemData)
                            {
                                if (itemData.ContainsKey("name")) name = itemData["name"].ToString();
                                if (itemData.ContainsKey("icon")) icon = itemData["icon"].ToString();
                                if (itemData.ContainsKey("description")) desc = itemData["description"].ToString();
                            }
                            data.Items.Add(new TTRPG.Shared.DTOs.ItemDisplay { Id = itemId, Name = name, Icon = icon, Description = desc, Count = 1 });
                        }
                    }
                }
                var response = new InventoryPacket { Data = data };
                BroadcastPacket(response, peer);
                Console.WriteLine($"[Inventory] Sent {data.Items.Count} items to peer {peer.Id}");
            }
        }

        private void OnSheetRequested(RequestSheetPacket packet, NetPeer peer)
        {
            if (_world == null || _ruleset == null) return;
            var entity = GetEntityForPeer(peer);
            if (entity == Arch.Core.Entity.Null) return;

            Console.WriteLine($"[Server] Generating Sheet for Entity {entity.Id}...");
            var sheetData = _ruleset.GetUI().GetCharacterSheet(_world, entity);
            var response = new SheetDataPacket { Sheet = sheetData };
            BroadcastPacket(response, peer);
        }

        // --- NETWORK HELPERS ---
        public void OnConnectionRequest(ConnectionRequest request) => request.AcceptIfKey("TTRPG_KEY");
        public void OnPeerConnected(NetPeer peer) { Console.WriteLine($"[Network] Connected: {peer}"); OnPlayerConnected?.Invoke(peer); }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info) { if (_playerSessions.ContainsKey(peer.Id)) _playerSessions.Remove(peer.Id); OnPlayerDisconnected?.Invoke(peer, info); }
        private void BroadcastPacket<T>(T packet, NetPeer target) where T : class, new()
        {
            NetDataWriter writer = new NetDataWriter(); _packetProcessor.Write(writer, packet); target.Send(writer, DeliveryMethod.ReliableOrdered);
        }
        public void BroadcastPacket<T>(T packet, DeliveryMethod method = DeliveryMethod.ReliableOrdered) where T : class, new()
        {
            NetDataWriter writer = new NetDataWriter(); _packetProcessor.Write(writer, packet); _netManager.SendToAll(writer, method);
        }
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod method) => _packetProcessor.ReadAllPackets(reader, peer);
        public void RegisterPlayerEntity(NetPeer peer, Entity entity) => _playerSessions[peer.Id] = (peer, entity);
        public Entity GetEntityForPeer(NetPeer peer) => _playerSessions.TryGetValue(peer.Id, out var s) ? s.Entity : Entity.Null;
        private void OnMoveReceived(PlayerMovePacket packet, NetPeer peer) { if (_playerSessions.TryGetValue(peer.Id, out var s)) OnPlayerInput?.Invoke(s.Entity, packet.Direction); }
        public void BroadcastToZone<T>(string targetZoneId, T packet, DeliveryMethod method = DeliveryMethod.ReliableOrdered) where T : class, new()
        {
            if (_world == null) return;

            // REMOVED: The logic that searched for "if packet is EntityPositionPacket"
            // The GameLoopService now handles populating SpriteId.

            NetDataWriter writer = new NetDataWriter();
            _packetProcessor.Write(writer, packet);

            foreach (var session in _playerSessions.Values)
            {
                if (_world.Has<Zone>(session.Entity) && _world.Get<Zone>(session.Entity).Id == targetZoneId)
                {
                    session.Peer.Send(writer, method);
                }
            }
        }
        public void OnNetworkError(IPEndPoint e, System.Net.Sockets.SocketError s) { }
        public void OnNetworkReceiveUnconnected(IPEndPoint e, NetPacketReader r, UnconnectedMessageType m) { }
        public void OnNetworkLatencyUpdate(NetPeer p, int l) { }
    }
}