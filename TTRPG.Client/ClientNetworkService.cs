using System;
using LiteNetLib;
using LiteNetLib.Utils;
using TTRPG.Shared;

namespace TTRPG.Client.Services
{
    public class ClientNetworkService
    {
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _client;
        private readonly NetPacketProcessor _packetProcessor;

        public ClientNetworkService()
        {
            _listener = new EventBasedNetListener();
            _client = new NetManager(_listener);
            _packetProcessor = new NetPacketProcessor();

            // 1. REGISTER RESPONSE HANDLER
            // When the Server replies, this method runs
            _packetProcessor.SubscribeReusable<JoinResponsePacket>(OnJoinResponse);

            // 2. HANDLE CONNECTION SUCCESS
            _listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("[Client] We are connected! Sending Join Request...");
                SendJoinRequest(peer);
            };

            // 3. HANDLE INCOMING DATA
            _listener.NetworkReceiveEvent += (peer, reader, deliveryMethod, channel) =>
            {
                _packetProcessor.ReadAllPackets(reader);
            };

            // 4. HANDLE DISCONNECTION
            _listener.PeerDisconnectedEvent += (peer, info) =>
            {
                Console.WriteLine($"[Client] Disconnected: {info.Reason}");
            };
        }

        public void Connect(string ip, int port)
        {
            _client.Start();
            _client.Connect(ip, port, "TTRPG_KEY");
        }

        public void Poll()
        {
            _client.PollEvents();
        }

        public void Stop()
        {
            _client.Stop();
        }

        // --- INTERNAL HELPERS ---

        private void SendJoinRequest(NetPeer peer)
        {
            // Create the packet
            var packet = new JoinRequestPacket
            {
                Username = "Traveler", // We can make this dynamic later
                Version = "1.0.0"
            };

            // Serialize and Send
            NetDataWriter writer = new NetDataWriter();
            _packetProcessor.Write(writer, packet);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        private void OnJoinResponse(JoinResponsePacket packet)
        {
            if (packet.Success)
            {
                Console.WriteLine($"[Client] Server Accepted Us: {packet.Message}");
            }
            else
            {
                Console.WriteLine($"[Client] Server Rejected Us: {packet.Message}");
            }
        }
    }
}