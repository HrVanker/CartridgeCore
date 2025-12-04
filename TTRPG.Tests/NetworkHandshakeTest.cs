using System;
using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using TTRPG.Shared;

namespace TTRPG.Server.Services
{
    public class ServerNetworkService
    {
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _server;
        private readonly NetPacketProcessor _packetProcessor;

        // Events exposed to the rest of the server
        public Action<NetPeer>? OnPlayerConnected;
        public Action<NetPeer, DisconnectInfo>? OnPlayerDisconnected; // Fixed: Added this back

        public ServerNetworkService()
        {
            _listener = new EventBasedNetListener();
            _server = new NetManager(_listener);
            _packetProcessor = new NetPacketProcessor();

            // 1. REGISTER PACKETS
            // We tell the processor: "When you see JoinRequestPacket, call OnJoinReceived"
            _packetProcessor.SubscribeReusable<JoinRequestPacket, NetPeer>(OnJoinReceived);

            // 2. CONNECTION LOGIC
            _listener.ConnectionRequestEvent += request =>
            {
                // Simple password check matching your previous logic
                if (_server.ConnectedPeersCount < 10)
                    request.AcceptIfKey("TTRPG_KEY");
                else
                    request.Reject();
            };

            _listener.PeerConnectedEvent += OnPeerConnected;

            // Fixed: Re-implemented the Disconnected logic
            _listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                Console.WriteLine($"[Network] Player disconnected: {disconnectInfo.Reason}");
                OnPlayerDisconnected?.Invoke(peer, disconnectInfo);
            };

            // 3. NETWORK RECEIVE LOGIC
            _listener.NetworkReceiveEvent += (peer, reader, deliveryMethod, channel) =>
            {
                // This reads the raw bytes and hands them to the PacketProcessor
                _packetProcessor.ReadAllPackets(reader, peer);
            };
        }

        public void Start(int port)
        {
            _server.Start(port);
            Console.WriteLine($"[Network] Server started on port {port}");
        }

        public void Poll()
        {
            _server.PollEvents();
        }

        public void Stop()
        {
            _server.Stop();
        }

        // --- INTERNAL HANDLERS ---

        private void OnPeerConnected(NetPeer peer)
        {
            // Using ToString() avoids the CS1061 EndPoint error
            Console.WriteLine($"[Network] Player connected: {peer}");
            OnPlayerConnected?.Invoke(peer);
        }

        private void OnJoinReceived(JoinRequestPacket packet, NetPeer peer)
        {
            Console.WriteLine($"[Server] Join Request from {packet.Username} (v{packet.Version})");

            var response = new JoinResponsePacket
            {
                Success = true,
                Message = $"Welcome {packet.Username}!"
            };

            // FIX for CS7036: LiteNetLib requires a NetDataWriter to write the packet into
            NetDataWriter writer = new NetDataWriter();
            _packetProcessor.Write(writer, response);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }
    }
}