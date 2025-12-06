using Xunit;
using Arch.Core;
using Arch.Core.Extensions;
using TTRPG.Server.Services;
using TTRPG.Shared.Components;
using LiteNetLib;
using System.Runtime.CompilerServices;

namespace TTRPG.Tests
{
    public class CommandTests
    {
        [Fact]
        public void TeleportCommand_ShouldMoveEntityToNewZone()
        {
            // Arrange
            var world = World.Create();
            var network = new ServerNetworkService();

            // FIX: We MUST Start the network so the internal Socket is created.
            // Port 0 tells the OS to assign any free port.
            network.Start(0);

            var service = new NotificationService(network, world);

            // Create a dummy peer key
            var peer = (NetPeer)RuntimeHelpers.GetUninitializedObject(typeof(NetPeer));

            var entity = world.Create(
                new Position { X = 10, Y = 10 },
                new Zone { Id = "Zone_A" }
            );

            // Register the session
            network.RegisterPlayerEntity(peer, entity);

            // Act
            // Send a chat command as if it came from this peer
            service.SendChatMessage(peer, "/tp Zone_Hidden");

            // Assert
            var newZone = world.Get<Zone>(entity);
            var newPos = world.Get<Position>(entity);

            Assert.Equal("Zone_Hidden", newZone.Id);
            Assert.Equal(0, newPos.X);
            Assert.Equal(0, newPos.Y);

            // Cleanup
            network.Stop();
            World.Destroy(world);
        }
    }
}