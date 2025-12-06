using Xunit;
using Arch.Core;
using Arch.Core.Extensions;
using System.Collections.Generic;
using TTRPG.Server.Services;
using TTRPG.Shared.Components;
using TTRPG.Shared.DTOs;

namespace TTRPG.Tests
{
    [Collection("Sequential")]
    public class ComponentSerializationTests
    {
        [Fact]
        public void EntityFactory_ShouldPopulateAttributesCorrectly()
        {
            // Arrange
            var world = World.Create();

            // simulate a loaded YAML blueprint
            var blueprint = new EntityBlueprint
            {
                Id = "hero_paladin",
                Components = new Dictionary<string, Dictionary<string, object>>
                {
                    {
                        "Attributes", new Dictionary<string, object>
                        {
                            { "Strength", 18 },
                            { "Dexterity", 12 },
                            { "Constitution", 14 },
                            { "Intelligence", 10 },
                            { "Wisdom", 10 },
                            { "Charisma", 16 }
                        }
                    },
                    {
                        "DerivedStats", new Dictionary<string, object>
                        {
                            { "ArmorClass", 18 }, // Plate mail
                            { "Speed", 6 }
                        }
                    }
                }
            };

            var factory = new EntityFactory(new List<EntityBlueprint> { blueprint });

            // Act
            var entity = factory.Create("hero_paladin", world);

            // Assert
            Assert.True(world.Has<Attributes>(entity), "Entity missing Attributes");
            Assert.True(world.Has<DerivedStats>(entity), "Entity missing DerivedStats");

            var attrs = world.Get<Attributes>(entity);
            Assert.Equal(18, attrs.Strength);
            Assert.Equal(16, attrs.Charisma);
            Assert.Equal(4, attrs.GetModifier(attrs.Strength)); // (18-10)/2 = 4

            var derived = world.Get<DerivedStats>(entity);
            Assert.Equal(18, derived.ArmorClass);
            Assert.Equal(6, derived.Speed);

            // Cleanup
            World.Destroy(world);
        }
    }
}