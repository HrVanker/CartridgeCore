using System.Collections.Generic;

namespace TTRPG.Core.DTOs // Namespace changed to Core
{
    public class CharacterSheetData
    {
        public string Name { get; set; } = "Unknown";
        public string Description { get; set; } = "";

        // Key: Category Name (e.g. "Attributes"), Value: List of Stats
        public Dictionary<string, List<StatEntry>> Categories { get; set; }
            = new Dictionary<string, List<StatEntry>>();
    }

    public class StatEntry
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
        public string Tooltip { get; set; } = "";
    }
}