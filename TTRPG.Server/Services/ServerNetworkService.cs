using System;
using System.Net;
using Arch.Core;
using LiteNetLib;
using LiteNetLib.Utils;
using TTRPG.Shared; // Required for JoinRequestPacket
using TTRPG.Shared.Enums;

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

        public ServerNetworkService()
        {
            // 1. Initialize the Packet Processor
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

            _packetProcessor.SubscribeReusable<PlayerMovePacket, NetPeer>(OnMoveReceived);

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

        // Unused but required interfaces
        public void OnNetworkError(IPEndPoint endPoint, System.Net.Sockets.SocketError socketError) { }
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    }
}