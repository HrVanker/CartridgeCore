using Xunit;
using TTRPG.Server.Services;
using System.IO;

namespace TTRPG.Tests
{
    public class MapServiceTests
    {
        [Fact]
        public void LoadMap_ShouldParseCollisionsCorrectly()
        {
            // Arrange: Create a temporary CSV map file
            string mapXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<map version=""1.10"" width=""3"" height=""3"" tilewidth=""16"" tileheight=""16"">
 <layer id=""1"" name=""Collisions"" width=""3"" height=""3"">
  <data encoding=""csv"">
1,1,1,
1,0,1,
1,1,1
</data>
 </layer>
</map>";
            string tempPath = Path.GetTempFileName();
            File.WriteAllText(tempPath, mapXml);

            var service = new MapService();

            // Act
            service.LoadMap(tempPath);

            // Assert
            // (0,0) is Wall (1) -> IsWalkable = False
            Assert.False(service.IsWalkable(0, 0), "0,0 should be a wall");

            // (1,1) is Empty (0) -> IsWalkable = True
            Assert.True(service.IsWalkable(1, 1), "1,1 should be walkable");

            // Cleanup
            File.Delete(tempPath);
        }
    }
}