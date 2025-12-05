using System;
using System.Net;
using Arch.Core;
using LiteNetLib;
using LiteNetLib.Utils;
using TTRPG.Shared; // Required for JoinRequestPacket
using TTRPG.Shared.Enums;
using TTRPG.Shared.Components;
using Arch.Core.Extensions;

namespace TTRPG.Server.Services
{
    // We keep INetEventListener because that is what you successfully implemented
    public class ServerNetworkService : INetEventListener
    {
        private readonly Dictionary<NetPeer, Entity> _playerSessions = new Dictionary<NetPeer, Entity>();
        private readonly NetManager _netManager;
        private readonly NetPacketProcessor _packetProcessor; // <--- NEW: Handles data logic

        public int ConnectedPeersCount => _netManager.ConnectedPeersCount;
        public bool IsRunning => _netManager.IsRunning;

        public Action<NetPeer>? OnPlayerConnected;
        public Action<NetPeer, DisconnectInfo>? OnPlayerDisconnected;
        public Action<Entity, MoveDirection>? OnPlayerInput;

        public Action<NetPeer, ChatMessagePacket>? OnChatMessage;

        public ServerNetworkService()
        {
            _packetProcessor = new NetPacketProcessor();

            _packetProcessor.RegisterNestedType<TTRPG.Shared.Components.Position>(
                (writer, pos) => // Writer
                {
                    writer.Put(pos.X);
                    writer.Put(pos.Y);
                },
                (reader) => // Reader
                {
                    return new TTRPG.Shared.Components.Position
                    {
                        X = reader.GetInt(),
                        Y = reader.GetInt()
                    };
                }

        );

            // 2. Subscribe to the JoinRequestPacket
            // When this packet arrives, run the 'OnJoinReceived' method
            _packetProcessor.SubscribeReusable<JoinRequestPacket, NetPeer>(OnJoinReceived);
            _packetProcessor.SubscribeReusable<InspectEntityPacket, NetPeer>(OnInspectReceived);
            _packetProcessor.SubscribeReusable<PlayerMovePacket, NetPeer>(OnMoveReceived);

            _packetProcessor.SubscribeReusable<ChatMessagePacket, NetPeer>((packet, peer) =>
            {
                OnChatMessage?.Invoke(peer, packet);
            });

            // 3. Initialize Manager listening to 'this' class
            _netManager = new NetManager(this);
        }

        public void Start(int port)
        {
            _netManager.Start(port);
            Console.WriteLine($"[Network] Server listening on port {port}...");
        }

        public void Stop()
        {
            _netManager.Stop();
        }

        public void Poll()
        {
            _netManager.PollEvents();
        }

        // --- NEW: The Logic that handles the specific packet ---
        private void OnJoinReceived(JoinRequestPacket packet, NetPeer peer)
        {
            Console.WriteLine($"[Server] Join Request from {packet.Username} (v{packet.Version})");

            var response = new JoinResponsePacket
            {
                Success = true,
                Message = $"Welcome {packet.Username}!"
            };

            // Create a writer, write the packet, and send it back
            NetDataWriter writer = new NetDataWriter();
            _packetProcessor.Write(writer, response);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        // --- LiteNetLib Interface Implementations ---

        public void OnConnectionRequest(ConnectionRequest request)
        {
            // Must match the Client's key exactly
            request.AcceptIfKey("TTRPG_KEY");
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine($"[Network] Player connected: {peer}");
            OnPlayerConnected?.Invoke(peer);
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine($"[Network] Player disconnected: {disconnectInfo.Reason}");
            // Cleanup Session
            if (_playerSessions.ContainsKey(peer))
            {
                _playerSessions.Remove(peer);
            }
            OnPlayerDisconnected?.Invoke(peer, disconnectInfo);
        }

        public void BroadcastPacket<T>(T packet, DeliveryMethod method = DeliveryMethod.ReliableOrdered) where T : class, new()
        {
            NetDataWriter writer = new NetDataWriter();
            // This uses the processor that knows about 'Position'
            _packetProcessor.Write(writer, packet);
            _netManager.SendToAll(writer, method);
        }

        // CRITICAL FIX: This was empty before, which is why the Server ignored the data!
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            // Pass the raw data to the processor to convert it into a Class
            _packetProcessor.ReadAllPackets(reader, peer);
        }
        public void SendToAll(NetDataWriter writer, DeliveryMethod method)
        {
            _netManager.SendToAll(writer, method);
        }

        public void RegisterPlayerEntity(NetPeer peer, Entity entity)
        {
            _playerSessions[peer] = entity;
        }

        public Entity GetEntityForPeer(NetPeer peer)
        {
            return _playerSessions.TryGetValue(peer, out var entity) ? entity : Entity.Null;
        }
        private void OnMoveReceived(PlayerMovePacket packet, NetPeer peer)
        {
            //1. Validate Session
            if (_playerSessions.TryGetValue(peer, out var entity))
            {
                // 2. Forward to Game Logic
                OnPlayerInput?.Invoke(entity, packet.Direction);
            }
        }
        private Arch.Core.World _world;

        // Add Method to inject it (Call this from Program.cs)
        public void SetWorld(Arch.Core.World world)
        {
            _world = world;
        }
        public void BroadcastToZone<T>(string targetZoneId, T packet, DeliveryMethod method = DeliveryMethod.ReliableOrdered) where T : class, new()
        {
            if (_world == null) return;

            NetDataWriter writer = new NetDataWriter();
            _packetProcessor.Write(writer, packet);

            // Iterate over all connected players
            foreach (var session in _playerSessions)
            {
                NetPeer peer = session.Key;
                Arch.Core.Entity entity = session.Value;

                // CHECK: Does this player have a Zone component?
                if (_world.Has<Zone>(entity))
                {
                    var playerZone = _world.Get<Zone>(entity);

                    // FILTER: Only send if they are in the target zone
                    if (playerZone.Id == targetZoneId)
                    {
                        peer.Send(writer, method);
                    }
                }
            }
        }

        // Unused but required interfaces
        public void OnNetworkError(IPEndPoint endPoint, System.Net.Sockets.SocketError socketError) { }
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

        private void OnInspectReceived(InspectEntityPacket packet, NetPeer peer)
        {
            if (_world == null) return;

            // Arch Entity IDs are just integers, but usually need a "Generation" for safety.
            // For this phase, we assume the ID provided is valid and alive.
            // We construct a "Unsafe" entity reference using just the ID.
            // In a full production Arch app, we'd track Generation too.
            var targetEntity = _world.Reference(packet.EntityId);

            if (targetEntity.IsAlive())
            {
                var entity = targetEntity.Entity;
                string info = $"ID: {entity.Id}";

                // 1. Get Health
                if (_world.Has<TTRPG.Shared.Components.Health>(entity))
                {
                    var hp = _world.Get<TTRPG.Shared.Components.Health>(entity);
                    info += $"\nHP: {hp.Current}/{hp.Max}";
                }

                // 2. Get Stats
                if (_world.Has<TTRPG.Shared.Components.Stats>(entity))
                {
                    var stats = _world.Get<TTRPG.Shared.Components.Stats>(entity);
                    info += $"\nSTR: {stats.Strength} AGI: {stats.Agility}";
                }

                // 3. Get Zone
                if (_world.Has<TTRPG.Shared.Components.Zone>(entity))
                {
                    var zone = _world.Get<TTRPG.Shared.Components.Zone>(entity);
                    info += $"\nZone: {zone.Id}";
                }

                // 4. Send Reply (Private message to the requester)
                var response = new EntityDetailsPacket
                {
                    EntityId = packet.EntityId,
                    Details = info
                };

                NetDataWriter writer = new NetDataWriter();
                _packetProcessor.Write(writer, response);
                peer.Send(writer, DeliveryMethod.ReliableOrdered);

                Console.WriteLine($"[Inspector] Sent details for Entity {entity.Id} to Peer {peer.Id}");
            }
        }
    }
}