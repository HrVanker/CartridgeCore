using System.Collections.Generic;

namespace TTRPG.Shared.DTOs
{
    public class EntityBlueprint
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        // A generic dictionary to hold component data
        // Example YAML: 
        // components:
        //   Position: { x: 10, y: 10 }
        //   Health: { current: 100, max: 100 }
        public Dictionary<string, Dictionary<string, object>> Components { get; set; }
            = new Dictionary<string, Dictionary<string, object>>();
    }
}