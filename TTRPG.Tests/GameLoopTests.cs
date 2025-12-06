using Xunit;
using Arch.Core;
using Arch.Core.Extensions;
using TTRPG.Server.Services;
using TTRPG.Shared.Components;
using TTRPG.Shared.Enums;
using System;

namespace TTRPG.Tests
{
    public class GameLoopTests
    {
        [Fact]
        public void Move_ShouldUpdatePosition()
        {
            // Arrange
            var world = World.Create();
            var network = new ServerNetworkService();
            network.Start(0); // FIX: Start network to initialize internals
            network.SetWorld(world);

            var notifications = new NotificationService(network, world);
            var gameLoop = new GameLoopService(network, world, notifications, mapService);

            // Create a player at (0,0) in Zone_A
            var entity = world.Create(
                new Position { X = 0, Y = 0 },
                new Zone { Id = "Zone_A" }
            );

            // Act
            network.OnPlayerInput?.Invoke(entity, MoveDirection.Right);

            // Assert
            var newPos = world.Get<Position>(entity);
            Console.WriteLine($"[Test] Pos X: {newPos.X}"); // Debug Print

            Assert.Equal(1, newPos.X);
            Assert.Equal(0, newPos.Y);

            // Cleanup
            network.Stop();
            World.Destroy(world);
        }

        [Fact]
        public void Move_ShouldChangeZone_WhenCrossingThreshold()
        {
            // Arrange
            var world = World.Create();
            var network = new ServerNetworkService();
            network.Start(0); // FIX: Start network
            network.SetWorld(world);

            var notifications = new NotificationService(network, world);
            var gameLoop = new GameLoopService(network, world, notifications, mapService);

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

            Console.WriteLine($"[Test] Zone: {zone.Id}, Pos X: {pos.X}");

            Assert.Equal(0, pos.X);
            Assert.Equal("Zone_B", zone.Id);

            // Cleanup
            network.Stop();
            World.Destroy(world);
        }
        [Fact]
        public void Move_ShouldBeBlocked_WhenWallExists()
        {
            // Arrange
            var world = World.Create();
            var network = new ServerNetworkService();
            network.Start(0);
            network.SetWorld(world);

            // Setup Map with a wall at (1,0)
            var mapService = new MapService();
            // We can write a temp file or just subclass/mock MapService if we made it virtual.
            // For simplicity, we'll write a temp TMX again.
            string mapXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <map version=""1.10"" width=""5"" height=""5"" tilewidth=""16"" tileheight=""16"">
            <layer id=""1"" name=""Collisions"" width=""5"" height=""5"">
            <data encoding=""csv"">
            0,1,0,0,0,
            0,0,0,0,0,
            0,0,0,0,0,
            0,0,0,0,0,
            0,0,0,0,0
            </data>
            </layer>
            </map>";
            string tempPath = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(tempPath, mapXml);
            mapService.LoadMap(tempPath);

            var notifications = new NotificationService(network, world);
            var gameLoop = new GameLoopService(network, world, notifications, mapService);

            // Player at (0,0)
            var entity = world.Create(new Position { X = 0, Y = 0 }, new Zone { Id = "Zone_A" });

            // Act: Try to move Right to (1,0) -> Should hit Wall
            network.OnPlayerInput?.Invoke(entity, MoveDirection.Right);

            // Assert
            var pos = world.Get<Position>(entity);
            Assert.Equal(0, pos.X); // Should NOT have changed to 1

            // Cleanup
            network.Stop();
            World.Destroy(world);
            System.IO.File.Delete(tempPath);
        }
    }
}