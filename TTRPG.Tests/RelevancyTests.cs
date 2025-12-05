using Xunit;
using Arch.Core;
using Arch.Core.Extensions;
using TTRPG.Shared.Components;
using System.Collections.Generic;

namespace TTRPG.Tests
{
    public class RelevancyTests
    {
        [Fact]
        public void ShouldFilterEntitiesByZone()
        {
            // Arrange
            var world = World.Create();

            // Create Entity A in "Zone_A"
            var entityA = world.Create(new Zone { Id = "Zone_A" });

            // Create Entity B in "Zone_B"
            var entityB = world.Create(new Zone { Id = "Zone_B" });

            // Act: Simulate the Server finding all players in Zone_A
            var entitiesInZoneA = new List<Entity>();
            var query = new QueryDescription().WithAll<Zone>();

            world.Query(in query, (Entity e, ref Zone zone) =>
            {
                if (zone.Id == "Zone_A")
                {
                    entitiesInZoneA.Add(e);
                }
            });

            // Assert
            Assert.Contains(entityA, entitiesInZoneA);
            Assert.DoesNotContain(entityB, entitiesInZoneA); // Crucial!
            Assert.Single(entitiesInZoneA);

            // Cleanup
            World.Destroy(world);
        }
    }
}