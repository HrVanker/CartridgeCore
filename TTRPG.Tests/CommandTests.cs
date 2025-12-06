using Xunit;
using Arch.Core;
using Arch.Core.Extensions;
using TTRPG.Server.Services;
using TTRPG.Shared.Components;
using LiteNetLib;
using System.Runtime.CompilerServices;
using System;

namespace TTRPG.Tests
{
    public class CommandTests
    {
        [Fact]
        public void TeleportCommand_ShouldMoveEntityToNewZone()
        {
            Console.WriteLine("[Test] Starting Teleport Test...");

            // 1. Setup
            var world = World.Create();
            var network = new ServerNetworkService();
            // Start network to prevent NullRef on socket
            network.Start(0);
            // Important: Link the world to the network so BroadcastToZone doesn't crash
            network.SetWorld(world);

            var service = new NotificationService(network, world);

            // Mock Peer
            var peer = (NetPeer)RuntimeHelpers.GetUninitializedObject(typeof(NetPeer));

            // Create Entity
            var entity = world.Create(
                new Position { X = 10, Y = 10 },
                new Zone { Id = "Zone_A" }
            );

            // Verify Initial State
            var initialZone = world.Get<Zone>(entity);
            Console.WriteLine($"[Test] Initial Zone: '{initialZone.Id}'");
            Assert.Equal("Zone_A", initialZone.Id);

            // Register
            network.RegisterPlayerEntity(peer, entity);

            // 2. Act
            Console.WriteLine("[Test] Sending Command: /tp Zone_Hidden");
            service.SendChatMessage(peer, "/tp Zone_Hidden");

            // 3. Verify
            var newZone = world.Get<Zone>(entity);
            var newPos = world.Get<Position>(entity);

            Console.WriteLine($"[Test] Post-Command Zone ID: '{newZone.Id}'");
            Console.WriteLine($"[Test] Post-Command Pos X: {newPos.X}");

            // Assertions
            Assert.NotNull(newZone.Id);
            Assert.Equal("Zone_Hidden", newZone.Id);
            Assert.Equal(0, newPos.X);
            Assert.Equal(0, newPos.Y);

            // Cleanup
            network.Stop();
            World.Destroy(world);
        }
    }
}