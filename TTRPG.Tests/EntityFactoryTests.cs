using Xunit;
using Arch.Core;
using Arch.Core.Extensions;
using System.Collections.Generic;
using TTRPG.Server.Services;
using TTRPG.Shared.DTOs;
using TTRPG.Shared.Components; // Ensure this is using Attributes

namespace TTRPG.Tests
{
    public class EntityFactoryTests
    {
        [Fact]
        public void ApplyTemplate_ShouldOverwriteExistingComponentValues()
        {
            // Arrange
            var world = World.Create();

            // 1. Define Base Blueprint (Weak Goblin)
            var baseBp = new EntityBlueprint
            {
                Id = "goblin",
                Components = new Dictionary<string, Dictionary<string, object>>
                {
                    {
                        "Attributes", new Dictionary<string, object>
                        {
                            { "Strength", 8 },
                            { "Dexterity", 10 }
                        }
                    }
                }
            };

            // 2. Define Template (Elite Modifier)
            var templateBp = new EntityBlueprint
            {
                Id = "elite",
                Components = new Dictionary<string, Dictionary<string, object>>
                {
                    {
                        "Attributes", new Dictionary<string, object>
                        {
                            { "Strength", 18 } // Should overwrite 8 -> 18
                        }
                    }
                }
            };

            var factory = new EntityFactory(new List<EntityBlueprint> { baseBp, templateBp });

            // Act
            var entity = factory.Create("goblin", world);
            factory.ApplyTemplate(entity, "elite", world);

            // Assert
            Assert.True(world.Has<Attributes>(entity));
            var attrs = world.Get<Attributes>(entity);

            Assert.Equal(18, attrs.Strength); // Modified by template
            Assert.Equal(10, attrs.Dexterity); // Kept from base

            // Cleanup
            World.Destroy(world);
        }

        // ... (Keep the Inventory test if you have it) ...
        [Fact]
        public void Create_ShouldInitializeInventoryComponent_Correctly()
        {
            var world = World.Create();
            var blueprint = new EntityBlueprint
            {
                Id = "hero",
                Components = new Dictionary<string, Dictionary<string, object>>
                {
                    { "Inventory", new Dictionary<string, object> { { "Capacity", 20 }, { "Items", new List<object> { "sword" } } } }
                }
            };
            var factory = new EntityFactory(new List<EntityBlueprint> { blueprint });
            var entity = factory.Create("hero", world);

            var inv = world.Get<Inventory>(entity);
            Assert.Equal(20, inv.Capacity);
            Assert.Equal("sword", inv.Items[0]);
            World.Destroy(world);
        }
    }
}