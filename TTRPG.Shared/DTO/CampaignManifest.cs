using System.Collections.Generic;

namespace TTRPG.Shared.DTOs
{
    public class CampaignManifest
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string Author { get; set; } = string.Empty;

        // Dependencies: "SystemName" -> "MinVersion"
        // Example: { "D20_Rules": "1.0.0", "Fantasy_Assets": "2.1.0" }
        public Dictionary<string, string> Dependencies { get; set; } = new Dictionary<string, string>();
    }
}