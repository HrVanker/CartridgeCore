using Xunit;
using Arch.Core;
using Arch.Core.Extensions;
using TTRPG.Rules.Pathfinder;
using TTRPG.Shared.Components;
using System.Collections.Generic;

namespace TTRPG.Tests
{
    public class RuleUITests
    {
        [Fact]
        public void GetInspectionDetails_ShouldShowStats_OnlyIfViewerIsSmart()
        {
            // Arrange
            var world = World.Create();
            var ui = new PathfinderUI();

            // FIX: Use Attributes
            var target = world.Create(new Attributes { Strength = 18, Intelligence = 5 });
            var dumbViewer = world.Create(new Attributes { Intelligence = 8 });
            var smartViewer = world.Create(new Attributes { Intelligence = 18 });

            // Act
            var dumbView = ui.GetInspectionDetails(world, dumbViewer, target);
            var smartView = ui.GetInspectionDetails(world, smartViewer, target);

            // Assert

            // FIX: Use 'Contains' instead of 'ContainsKey'
            Assert.Contains("Entity ID", dumbView);

            // FIX: Use 'DoesNotContain' instead of 'DoesNotContainKey'
            Assert.DoesNotContain("Analysis", dumbView);

            // Smart viewer checks
            Assert.Contains("Analysis", smartView);
            // This checks the string value attached to the key "Analysis"
            Assert.Contains("18 STR", smartView["Analysis"]);

            // Cleanup
            World.Destroy(world);
        }
    }
}