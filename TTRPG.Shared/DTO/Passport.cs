using System.Collections.Generic;

namespace TTRPG.Shared.DTOs
{
    // The "Universal Save File" for a character
    public class Passport
    {
        public string CharacterName { get; set; } = string.Empty;
        public string SourceCampaignId { get; set; } = string.Empty;

        // Flattens complex components into simple Key-Value pairs for easy migration
        // Example: "Stats_Strength" : 18
        // Example: "Inventory_Gold" : 100
        public Dictionary<string, int> RawIntValues { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, string> RawStringValues { get; set; } = new Dictionary<string, string>();
    }
}