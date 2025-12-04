using System;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;

namespace TTRPG.Server.Services;

public class ServerNetworkService : INetEventListener
{
    private readonly NetManager _netManager;
    public int ConnectedPeersCount => _netManager.ConnectedPeersCount;
    public bool IsRunning => _netManager.IsRunning;

    public Action<LiteNetLib.NetPeer>? OnPlayerConnected;
    public event Action<NetPeer>? OnPlayerDisconnected;

    public ServerNetworkService()
    {
        // The server listens for events on 'this' class
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

    // This must be called every frame (in the Game Loop) to process packets
    public void Poll()
    {
        _netManager.PollEvents();
    }

    // --- LiteNetLib Interface Implementations ---
    public void OnPeerConnected(LiteNetLib.NetPeer peer)
    {
        // EndPoint returns a System.Net.IPEndPoint. 
        // If System.Net is missing, this property often appears "missing" to the compiler.
        Console.WriteLine($"[Network] Player connected: {peer}");

        // Invoke the event
        OnPlayerConnected?.Invoke(peer);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Console.WriteLine($"[Network] Player disconnected: {disconnectInfo.Reason}");
        OnPlayerDisconnected?.Invoke(peer);
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        // For Phase 1, we accept everyone. Later, we can check passwords/versions.
        request.AcceptIfKey("CartridgeCore_Key");
    }

    // Unused for Handshake, but required by interface
    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError) { }
    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod) { }
    public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
}