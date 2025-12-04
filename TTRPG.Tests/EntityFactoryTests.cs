using Xunit;
using Arch.Core;
using System.Collections.Generic;
using TTRPG.Server.Services;
using TTRPG.Shared.DTOs;
using TTRPG.Shared.Components;

namespace TTRPG.Tests
{
    public class EntityFactoryTests
    {
        [Fact]
        public void ApplyTemplate_ShouldOverwriteExistingComponentValues()
        {
            // Arrange
            var world = World.Create();

            // 1. Define a "Base" Blueprint (Weak Goblin)
            var baseBlueprint = new EntityBlueprint
            {
                Id = "goblin_base",
                Name = "Goblin",
                Components = new Dictionary<string, Dictionary<string, object>>
                {
                    { "Stats", new Dictionary<string, object> { { "Strength", 5 }, { "Agility", 5 } } }
                }
            };

            // 2. Define a "Template" Blueprint (Elite Buff)
            var eliteTemplate = new EntityBlueprint
            {
                Id = "template_elite",
                Name = "Elite",
                Components = new Dictionary<string, Dictionary<string, object>>
                {
                    { "Stats", new Dictionary<string, object> { { "Strength", 18 }, { "Agility", 15 } } }
                }
            };

            var blueprints = new List<EntityBlueprint> { baseBlueprint, eliteTemplate };
            var factory = new EntityFactory(blueprints);

            // Act
            // Spawn base
            var entity = factory.Create("goblin_base", world);

            // Apply decorator
            factory.ApplyTemplate(entity, "template_elite", world);

            // Assert
            var stats = world.Get<Stats>(entity);

            // Verify the values matched the TEMPLATE, not the BASE
            Assert.Equal(18, stats.Strength);
            Assert.Equal(15, stats.Agility);

            // Cleanup
            World.Destroy(world);
        }
    }
}