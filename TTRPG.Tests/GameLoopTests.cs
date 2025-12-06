using Xunit;
using Arch.Core;
using Arch.Core.Extensions;
using TTRPG.Server.Services;
using TTRPG.Shared.Components;
using TTRPG.Shared.Enums;
using TTRPG.Shared.DTOs;

namespace TTRPG.Tests
{
    public class GameLoopTests
    {
        [Fact]
        public void Move_ShouldUpdatePosition()
        {
            // Arrange
            var world = World.Create();
            var network = new ServerNetworkService(); // We use the real service, just don't .Start() it
            var notifications = new NotificationService(network, world);
            var gameLoop = new GameLoopService(network, world, notifications);

            // Create a player at (0,0) in Zone_A
            var entity = world.Create(
                new Position { X = 0, Y = 0 },
                new Zone { Id = "Zone_A" }
            );

            // Act: Simulate a network packet arriving
            // We manually invoke the event that ServerNetworkService fires
            network.OnPlayerInput?.Invoke(entity, MoveDirection.Right);

            // Assert
            var newPos = world.Get<Position>(entity);
            Assert.Equal(1, newPos.X); // 0 + 1 = 1
            Assert.Equal(0, newPos.Y);

            // Cleanup
            World.Destroy(world);
        }

        [Fact]
        public void Move_ShouldChangeZone_WhenCrossingThreshold()
        {
            // Arrange
            var world = World.Create();
            var network = new ServerNetworkService();
            var notifications = new NotificationService(network, world);
            var gameLoop = new GameLoopService(network, world, notifications);

            // Start player at the edge (-1, 0) in Zone_A
            var entity = world.Create(
                new Position { X = -1, Y = 0 },
                new Zone { Id = "Zone_A" }
            );

            // Act: Move Right (X becomes 0) -> Should trigger Zone_B
            network.OnPlayerInput?.Invoke(entity, MoveDirection.Right);

            // Assert
            var zone = world.Get<Zone>(entity);
            var pos = world.Get<Position>(entity);

            Assert.Equal(0, pos.X);
            Assert.Equal("Zone_B", zone.Id); // Logic: X >= 0 is Zone_B

            // Cleanup
            World.Destroy(world);
        }
    }
}