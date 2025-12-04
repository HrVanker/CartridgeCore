using Arch.Core;
using Xunit;
using TTRPG.Server.Services;
using TTRPG.Shared.Components;

namespace TTRPG.Tests
{
    public class PassportTests
    {
        [Fact]
        public void ShouldExportEntityToPassport()
        {
            // Arrange
            var world = World.Create();
            var service = new PassportService();

            // Create a "Player" entity manually
            var player = world.Create(
                new Stats { Strength = 18, Agility = 10 },
                new Health { Current = 50, Max = 100 }
            );

            // Act
            var passport = service.CreatePassport(player, "Test_Campaign_v1");

            // Assert
            Assert.Equal("Test_Campaign_v1", passport.SourceCampaignId);
            Assert.Equal(18, passport.RawIntValues["Stats_Strength"]);
            Assert.Equal(50, passport.RawIntValues["Health_Current"]);

            // Cleanup
            World.Destroy(world);
        }
    }
}