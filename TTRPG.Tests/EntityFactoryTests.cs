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
            var stats = world.Get<Attributes>(entity);

            // Verify the values matched the TEMPLATE, not the BASE
            Assert.Equal(18, stats.Strength);
            Assert.Equal(15, stats.Dexterity);

            // Cleanup
            World.Destroy(world);
        }
    
    [Fact]
        public void Create_ShouldInitializeInventoryComponent_Correctly()
        {
            // Arrange
            var world = World.Create();

            // Define a blueprint with an Inventory
            var blueprint = new TTRPG.Shared.DTOs.EntityBlueprint
            {
                Id = "hero",
                Components = new Dictionary<string, Dictionary<string, object>>
                {
                    {
                        "Inventory", new Dictionary<string, object>
                        {
                            { "Capacity", 20 },
                            // Note: YAML deserializer usually passes List<object> here
                            { "Items", new List<object> { "starter_sword" } }
                        }
                    }
                }
            };

            var factory = new EntityFactory(new List<TTRPG.Shared.DTOs.EntityBlueprint> { blueprint });

            // Act
            var entity = factory.Create("hero", world);

            // Assert
            Assert.True(world.Has<Inventory>(entity));
            var inv = world.Get<Inventory>(entity);

            Assert.Equal(20, inv.Capacity);
            Assert.NotNull(inv.Items);
            Assert.Single(inv.Items);
            Assert.Equal("starter_sword", inv.Items[0]);

            World.Destroy(world);
        }
    } }