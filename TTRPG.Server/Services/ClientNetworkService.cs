using LiteNetLib;
using LiteNetLib.Utils;

namespace TTRPG.Client.Services;

public class ClientNetworkService : INetEventListener
{
    private readonly NetManager _netManager;
    public bool IsConnected => _netManager.FirstPeer?.ConnectionState == ConnectionState.Connected;

    public ClientNetworkService()
    {
        _netManager = new NetManager(this);
    }

    public void Connect(string ip, int port)
    {
        _netManager.Start();
        // The "Key" must match what the server expects
        _netManager.Connect(ip, port, "CartridgeCore_Key");
    }

    public void Poll()
    {
        _netManager.PollEvents();
    }

    public void Stop()
    {
        _netManager.Stop();
    }

    // --- LiteNetLib Interface Implementations ---
    public void OnPeerConnected(NetPeer peer)
    {
        Console.WriteLine("[Client] Successfully connected to Server!");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Console.WriteLine($"[Client] Disconnected: {disconnectInfo.Reason}");
    }

    // Unused for Handshake
    public void OnConnectionRequest(ConnectionRequest request) { }
    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError) { }
    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod) { }
    public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
}