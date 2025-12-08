using Xunit;
using Arch.Core;
using Arch.Core.Extensions;
using TTRPG.Rules.Pathfinder;
using TTRPG.Shared.Components;
using TTRPG.Core.DTOs;
using System.Collections.Generic;

namespace TTRPG.Tests
{
    public class CharacterSheetTests
    {
        [Fact]
        public void GetCharacterSheet_ShouldPopulateAttributes()
        {
            // Arrange
            var world = World.Create();
            var ui = new PathfinderUI();

            // Create a "Hero"
            var entity = world.Create(
                new Attributes
                {
                    Strength = 18,
                    Dexterity = 14,
                    Constitution = 10,
                    Intelligence = 8,
                    Wisdom = 10,
                    Charisma = 10
                },
                new DerivedStats { ArmorClass = 15, Speed = 6 }
            );

            // Act
            CharacterSheetData sheet = ui.GetCharacterSheet(world, entity);

            // Assert
            Assert.NotNull(sheet);

            // 1. Verify Attributes Category exists
            Assert.True(sheet.Categories.ContainsKey("Attributes"));
            var attrs = sheet.Categories["Attributes"];

            // 2. Verify Strength Calculation (18 should be "+4")
            var strEntry = attrs.Find(x => x.Label == "STR");
            Assert.NotNull(strEntry);
            Assert.Contains("18", strEntry.Value);
            Assert.Contains("+4", strEntry.Value);

            // 3. Verify Combat Stats
            Assert.True(sheet.Categories.ContainsKey("Combat"));
            var combat = sheet.Categories["Combat"];
            var acEntry = combat.Find(x => x.Label == "AC");
            Assert.Equal("15", acEntry.Value);

            // Cleanup
            World.Destroy(world);
        }
    }
}