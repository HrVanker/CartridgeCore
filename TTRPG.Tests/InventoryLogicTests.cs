using Xunit;
using Arch.Core;
using Arch.Core.Extensions;
using TTRPG.Shared.Components;
using System.Collections.Generic;

namespace TTRPG.Tests
{
    public class InventoryLogicTests
    {
        [Fact]
        public void Pickup_ShouldTransferItem_FromWorld_ToInventory()
        {
            // Arrange
            var world = World.Create();

            // 1. Create Player at (10, 10)
            var player = world.Create(
                new Position { X = 10, Y = 10 },
                new Inventory { Capacity = 5, Items = new List<string>() }
            );

            // 2. Create Potion on the ground at (10, 10)
            var potion = world.Create(
                new Position { X = 10, Y = 10 },
                new Item { Id = "potion_healing", Name = "Health Potion" }
            );

            // 3. Create Sword far away at (20, 20) (Should NOT be picked up)
            var sword = world.Create(
                new Position { X = 20, Y = 20 },
                new Item { Id = "sword_iron", Name = "Iron Sword" }
            );

            // Act (Simulate GameLoopService.HandlePickup logic)
            var playerPos = world.Get<Position>(player);
            var inventory = world.Get<Inventory>(player);

            Entity itemToPickup = Entity.Null;

            var query = new QueryDescription().WithAll<Position, Item>();
            world.Query(in query, (Entity e, ref Position pos, ref Item item) =>
            {
                if (pos.X == playerPos.X && pos.Y == playerPos.Y && e.Id != player.Id)
                {
                    itemToPickup = e;
                }
            });

            if (itemToPickup != Entity.Null)
            {
                // Move ID to bag
                var itemData = world.Get<Item>(itemToPickup);
                inventory.Items.Add(itemData.Id);

                // Destroy world entity
                world.Destroy(itemToPickup);
            }

            // Assert

            // Player should have the potion
            Assert.Contains("potion_healing", inventory.Items);
            Assert.Single(inventory.Items);

            // Potion entity should be dead
            Assert.False(world.IsAlive(potion));

            // Sword entity should still be alive
            Assert.True(world.IsAlive(sword));

            // Cleanup
            World.Destroy(world);
        }

        [Fact]
        public void Inventory_ShouldRespectCapacity()
        {
            // Arrange
            var inv = new Inventory { Capacity = 2, Items = new List<string> { "a", "b" } };

            // Act
            bool canPickup = inv.Items.Count < inv.Capacity;

            // Assert
            Assert.False(canPickup);
        }
    }
}