using Xunit;
using TTRPG.Server.Services;
using System.IO;

namespace TTRPG.Tests
{
    public class BlueprintLoaderTests
    {
        [Fact]
        public void LoadBlueprints_ShouldReturnEmptyList_WhenFileMissing()
        {
            // Arrange
            var loader = new BlueprintLoader();
            string fakePath = "NonExistentFile.yaml";

            // Act
            var result = loader.LoadBlueprints(fakePath);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            // Crucially: It did not throw an exception
        }

        [Fact]
        public void LoadManifest_ShouldReturnDefault_WhenFileMissing()
        {
            // Arrange
            var loader = new BlueprintLoader();
            string fakePath = "NoManifest.yaml";

            // Act
            var result = loader.LoadManifest(fakePath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Unknown Campaign", result.Name);
        }
    }
}