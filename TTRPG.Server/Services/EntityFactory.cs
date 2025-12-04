using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Arch.Core;
using Arch.Core.Extensions; // Required for .Add()
using TTRPG.Core;
using TTRPG.Shared.Components; // Reference our new structs
using TTRPG.Shared.DTOs;

namespace TTRPG.Server.Services
{
    public class EntityFactory : IEntityFactory
    {
        private readonly Dictionary<string, EntityBlueprint> _blueprints;

        // This acts as our "Rules Cartridge" registry - mapping String -> Type
        private readonly Dictionary<string, Type> _componentRegistry;

        public EntityFactory(List<EntityBlueprint> blueprints)
        {
            // Index blueprints by ID for fast lookup
            _blueprints = blueprints.ToDictionary(b => b.Id);

            // AUTO-DISCOVERY: Find all structs in TTRPG.Shared.Components
            _componentRegistry = new Dictionary<string, Type>();

            // We look for types in the assembly where 'Position' is defined
            var assembly = typeof(Position).Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsValueType && !type.IsEnum) // Arch components are structs (ValueTypes)
                {
                    // Map "Position" -> typeof(TTRPG.Shared.Components.Position)
                    _componentRegistry[type.Name] = type;
                }
            }
        }

        public Entity Create(string blueprintId, World world)
        {
            if (!_blueprints.ContainsKey(blueprintId))
            {
                Console.WriteLine($"[Factory] Error: Blueprint '{blueprintId}' not found.");
                return Entity.Null;
            }

            // 1. Create Empty
            var entity = world.Create();
            Console.WriteLine($"[Factory] Spawning Base: {blueprintId}...");

            // 2. Apply the Base Blueprint as if it were a template
            ApplyTemplate(entity, blueprintId);

            return entity;
        }

        public void ApplyTemplate(Entity entity, string templateId)
        {
            if (!_blueprints.TryGetValue(templateId, out var blueprint))
            {
                Console.WriteLine($"[Factory] Warning: Template '{templateId}' not found.");
                return;
            }

            Console.WriteLine($"[Factory] Applying Template: {blueprint.Name}");

            foreach (var compEntry in blueprint.Components)
            {
                string compName = compEntry.Key;
                var compData = compEntry.Value;

                if (_componentRegistry.TryGetValue(compName, out var compType))
                {
                    // 1. Create the new component data (e.g., Strength 18)
                    object newComponentData = CreateComponentFromData(compType, compData);

                    // 2. DEBUG: Verify the parser actually got the numbers
                    // (This helps us see if it's a parsing error or an ECS error)
                    if (compName == "Stats")
                    {
                        var stats = (TTRPG.Shared.Components.Stats)newComponentData;
                        Console.WriteLine($"  -> Merging Stats: Str {stats.Strength}, Agi {stats.Agility}");
                    }

                    // 3. FORCE UPDATE LOGIC
                    // We explicitly remove the old struct to ensure the new one takes its place.
                    if (entity.Has(compType))
                    {
                        entity.Remove(compType);
                    }

                    // Add the new component (Arch handles the boxing/unboxing)
                    entity.Add(newComponentData);
                }
            }
        }

        // Helper: Uses basic property matching to fill the struct
        private object CreateComponentFromData(Type type, Dictionary<string, object> data)
        {
            // Create a default instance of the struct (e.g., new Position())
            object instance = Activator.CreateInstance(type)!;

            foreach (var field in type.GetFields())
            {
                // YAML implies case-insensitivity often, but we check exact or lowercase
                // Our loader uses CamelCase, so "x" matches "X" via manual check
                var key = data.Keys.FirstOrDefault(k => k.Equals(field.Name, StringComparison.OrdinalIgnoreCase));

                if (key != null)
                {
                    // Convert the value to the correct type (YAML reads numbers as strings/ints mixed)
                    object val = data[key];
                    try
                    {
                        // Safely convert (e.g. string "10" to int 10)
                        object convertedVal = Convert.ChangeType(val, field.FieldType);
                        field.SetValue(instance, convertedVal);
                    }
                    catch
                    {
                        Console.WriteLine($"    - Failed to set field {field.Name}");
                    }
                }
            }
            return instance;
        }
    }
}