using System;
using System.Collections.Generic;
using System.Net;
using Arch.Core;
using LiteNetLib;
using LiteNetLib.Utils;
using TTRPG.Shared; // Required for JoinRequestPacket
using TTRPG.Shared.Enums;
using TTRPG.Shared.Components;
using Arch.Core.Extensions;
using TTRPG.Core;

namespace TTRPG.Server.Services
{
    // We keep INetEventListener because that is what you successfully implemented
    public class ServerNetworkService : INetEventListener
    {
        private readonly Dictionary<int, (NetPeer Peer, Entity Entity)> _playerSessions = new Dictionary<int, (NetPeer, Entity)>();
        private readonly NetManager _netManager;
        private readonly NetPacketProcessor _packetProcessor; // <--- NEW: Handles data logic

        public int ConnectedPeersCount => _netManager.ConnectedPeersCount;
        public bool IsRunning => _netManager.IsRunning;

        public Action<NetPeer>? OnPlayerConnected;
        public Action<NetPeer, DisconnectInfo>? OnPlayerDisconnected;
        public Action<Entity, MoveDirection>? OnPlayerInput;

        public Action<NetPeer, ChatMessagePacket>? OnChatMessage;
        private IRuleset? _ruleset;

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
            if (_playerSessions.ContainsKey(peer.Id))
            {
                _playerSessions.Remove(peer.Id);
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
            _playerSessions[peer.Id] = (peer, entity);
        }

        public Entity GetEntityForPeer(NetPeer peer)
        {
            return _playerSessions.TryGetValue(peer.Id, out var session) ? session.Entity : Entity.Null;
        }
        private void OnMoveReceived(PlayerMovePacket packet, NetPeer peer)
        {
            //1. Validate Session
            if (_playerSessions.TryGetValue(peer.Id, out var session))
            {
                OnPlayerInput?.Invoke(session.Entity, packet.Direction);
            }
        }
        private Arch.Core.World? _world;

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

            // FIX: Iterate values of the dictionary
            foreach (var session in _playerSessions.Values)
            {
                NetPeer peer = session.Peer;
                Arch.Core.Entity entity = session.Entity;

                if (_world.Has<Zone>(entity))
                {
                    var playerZone = _world.Get<Zone>(entity);

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

            // 1. Find the Entity
            Arch.Core.Entity foundEntity = Arch.Core.Entity.Null;
            var query = new Arch.Core.QueryDescription();
            _world.Query(in query, (Arch.Core.Entity e) =>
            {
                if (e.Id == packet.EntityId) foundEntity = e;
            });

            if (foundEntity != Arch.Core.Entity.Null && _world.IsAlive(foundEntity))
            {
                // 2. Identify the Viewer (The Player asking)
                Entity viewer = GetEntityForPeer(peer);

                // 3. Ask the Ruleset for Data (The Logic Phase)
                Dictionary<string, string> data;

                if (_ruleset != null)
                {
                    // Use the Interface logic we just wrote!
                    data = _ruleset.GetUI().GetInspectionDetails(_world, viewer, foundEntity);
                }
                else
                {
                    // Fallback if no rules loaded
                    data = new Dictionary<string, string> { { "Error", "No Ruleset Loaded" } };
                }

                // 4. Send Response
                var response = new EntityDetailsPacket
                {
                    EntityId = packet.EntityId,
                    Stats = data
                };

                BroadcastPacket(response, peer); // Helper to write/send
                Console.WriteLine($"[Inspector] Sent {data.Count} stats for Entity {foundEntity.Id}");
            }
        }

        // Helper to handle the write/send boilerplate
        private void BroadcastPacket<T>(T packet, NetPeer target) where T : class, new()
        {
            NetDataWriter writer = new NetDataWriter();
            _packetProcessor.Write(writer, packet);
            target.Send(writer, DeliveryMethod.ReliableOrdered);
        }
    }
}