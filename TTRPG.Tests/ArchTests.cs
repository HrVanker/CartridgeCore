using Xunit;
using Arch.Core;
using Arch.Core.Extensions;

namespace TTRPG.Tests
{
    public class ArchTests
    {
        // Simple Component for testing
        public struct Position { public float X, Y; }
        public struct Velocity { public float Dx, Dy; }

        [Fact]
        public void ECS_ShouldCreateAndQueryEntities()
        {
            // Arrange
            var world = World.Create();

            // Act: Create an entity with Position and Velocity
            var entity = world.Create(new Position { X = 10, Y = 10 }, new Velocity { Dx = 1, Dy = 1 });

            // Query the world for entities with Position
            var query = new QueryDescription().WithAll<Position>();
            int count = 0;

            world.Query(in query, (Entity e, ref Position pos) =>
            {
                count++;
                // Verify data integrity
                Assert.Equal(10, pos.X);
            });

            // Assert
            Assert.Equal(1, count);

            // Cleanup
            world.Destroy(entity);
            World.Destroy(world);
        }
    }
}