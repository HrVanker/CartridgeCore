using Xunit;
using TTRPG.Server.Services;
using TTRPG.Client.Services;
using System.Threading;

namespace TTRPG.Tests;

public class NetworkHandshakeTests
{
    [Fact]
    public void Client_ShouldConnect_WhenServerIsRunning()
    {
        // Arrange
        int testPort = 9055; // Use a different port just for testing
        var server = new ServerNetworkService();
        var client = new ClientNetworkService();

        // Act
        server.Start(testPort);
        client.Connect("localhost", testPort);

        // We need to "tick" the network engines for a moment to let the handshake happen.
        // Network isn't instant!
        for (int i = 0; i < 20; i++)
        {
            server.Poll();
            client.Poll();
            Thread.Sleep(15); // Wait a tiny bit (simulating ~60fps)
        }

        // Assert
        Assert.True(server.IsRunning, "Server should be running");
        Assert.Equal(1, server.ConnectedPeersCount); // Server sees 1 client
        Assert.True(client.IsConnected, "Client should report connected");

        // Cleanup
        client.Stop();
        server.Stop();
    }
}