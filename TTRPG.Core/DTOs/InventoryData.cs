using System.Collections.Generic;

namespace TTRPG.Core.DTOs
{
    public class InventoryData
    {
        public int Capacity { get; set; }
        public List<ItemDisplay> Items { get; set; } = new List<ItemDisplay>();
    }

    public class ItemDisplay
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public int Count { get; set; } = 1;
        public string Description { get; set; } = "";
    }
}