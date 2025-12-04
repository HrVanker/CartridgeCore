using System;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using TTRPG.Shared; // Required for JoinRequestPacket

namespace TTRPG.Server.Services
{
    // We keep INetEventListener because that is what you successfully implemented
    public class ServerNetworkService : INetEventListener
    {
        private readonly NetManager _netManager;
        private readonly NetPacketProcessor _packetProcessor; // <--- NEW: Handles data logic

        public int ConnectedPeersCount => _netManager.ConnectedPeersCount;
        public bool IsRunning => _netManager.IsRunning;

        public Action<NetPeer>? OnPlayerConnected;
        public event Action<NetPeer>? OnPlayerDisconnected;

        public ServerNetworkService()
        {
            // 1. Initialize the Packet Processor
            _packetProcessor = new NetPacketProcessor();

            // 2. Subscribe to the JoinRequestPacket
            // When this packet arrives, run the 'OnJoinReceived' method
            _packetProcessor.SubscribeReusable<JoinRequestPacket, NetPeer>(OnJoinReceived);

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
            OnPlayerDisconnected?.Invoke(peer);
        }

        // CRITICAL FIX: This was empty before, which is why the Server ignored the data!
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            // Pass the raw data to the processor to convert it into a Class
            _packetProcessor.ReadAllPackets(reader, peer);
        }

        // Unused but required interfaces
        public void OnNetworkError(IPEndPoint endPoint, System.Net.Sockets.SocketError socketError) { }
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    }
}