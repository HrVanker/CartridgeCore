using Xunit;
using TTRPG.Shared.DTOs;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace TTRPG.Tests
{
    public class InventoryPacketTests
    {
        [Fact]
        public void InventoryData_ShouldSerializeAndDeserialize_ViaJson()
        {
            // Arrange
            var original = new InventoryData
            {
                Capacity = 10,
                Items = new List<ItemDisplay>
                {
                    new ItemDisplay { Id = "p1", Name = "Potion", Count = 5 },
                    new ItemDisplay { Id = "s1", Name = "Sword", Count = 1 }
                }
            };

            // Act (Simulate Network Processor)
            string json = JsonConvert.SerializeObject(original);
            var result = JsonConvert.DeserializeObject<InventoryData>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Capacity);
            Assert.Equal(2, result.Items.Count);

            Assert.Equal("Potion", result.Items[0].Name);
            Assert.Equal(5, result.Items[0].Count);
        }
    }
}