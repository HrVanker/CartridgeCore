using System.IO;
using Xunit;
using TiledCS; // Uses your custom local class

namespace TTRPG.Tests
{
    public class TiledParserTests
    {
        [Fact]
        public void ShouldParse_EmbeddedTileset_FromXmlString()
        {
            // 1. Arrange: A simplified TMX string with the <tileset> block we expect
            string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<map version=""1.10"" width=""5"" height=""5"" tilewidth=""16"" tileheight=""16"">
 <tileset firstgid=""1"" name=""ground"" tilewidth=""16"" tileheight=""16"" tilecount=""1"" columns=""1"">
  <image source=""ground.png"" width=""16"" height=""16""/>
 </tileset>
 <layer id=""1"" name=""Ground"" width=""5"" height=""5"">
  <data encoding=""csv"">
1,1,1,1,1,
1,1,1,1,1,
1,1,1,1,1,
1,1,1,1,1,
1,1,1,1,1
</data>
 </layer>
</map>";
            // Save to temp file because TiledMap expects a path
            string tempPath = Path.GetTempFileName();
            File.WriteAllText(tempPath, xml);

            try
            {
                // 2. Act
                var map = new TiledMap(tempPath);

                // 3. Assert
                Assert.NotNull(map);
                Assert.Equal(5, map.Width);

                // CRITICAL: Did we find the tileset?
                Assert.Single(map.Tilesets);
                var tileset = map.Tilesets[0];

                Assert.Equal("ground", tileset.Name);
                Assert.Equal(1, tileset.FirstGid);

                // CRITICAL: Did we find the image source?
                Assert.NotNull(tileset.Image);
                Assert.Equal("ground.png", tileset.Image.Source);
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Fact]
        public void ShouldLoad_ActualProjectMap_AndFindTileset()
        {
            // 1. Arrange: Locate the actual file in your project
            // We look relative to the Test execution folder
            // Usually: TTRPG.Tests/bin/Debug/net8.0/
            // We need to go up to TTRPG.Server/Data/test_map.tmx

            // NOTE: This path might need adjustment depending on where you run tests from.
            // A safer bet is to copy test_map.tmx to the test output directory, similar to manifest.yaml
            string fileName = "test_map.tmx";

            // Check if file exists in output (we will add it to csproj in next step)
            if (!File.Exists(fileName))
            {
                // Fallback test to warn us if file is missing entirely
                Assert.Fail($"Could not find {fileName} in test output. Did you add it to TTRPG.Tests.csproj?");
            }

            // 2. Act
            var map = new TiledMap(fileName);

            // 3. Assert
            Assert.True(map.Tilesets.Count > 0, "The actual test_map.tmx has NO tilesets defined! TiledMapRenderer will draw nothing.");

            var tileset = map.Tilesets[0];
            Assert.NotNull(tileset.Image);
            Assert.Equal("ground.png", tileset.Image.Source);
        }
    }
}