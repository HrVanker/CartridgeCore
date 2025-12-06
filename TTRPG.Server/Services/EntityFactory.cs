using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using TTRPG.Core;
using TTRPG.Shared.DTOs;

namespace TTRPG.Server.Services
{
    public class EntityFactory : IEntityFactory
    {
        private readonly Dictionary<string, EntityBlueprint> _blueprints;
        private readonly Dictionary<string, Type> _componentRegistry;

        public EntityFactory(List<EntityBlueprint> blueprints)
        {
            _blueprints = blueprints.ToDictionary(b => b.Id);
            _componentRegistry = new Dictionary<string, Type>();

            // AUTO-DISCOVERY: Find all structs in TTRPG.Shared.Components
            // We find the assembly using a known component type (e.g., Position)
            // Note: Since we haven't added specific using TTRPG.Shared.Components here, 
            // ensure at least one component is referenced or known, 
            // OR we iterate the assembly more dynamically. 
            // For safety, let's assume TTRPG.Shared is loaded.
            var assembly = typeof(TTRPG.Shared.Components.Position).Assembly;

            foreach (var type in assembly.GetTypes())
            {
                if (type.IsValueType && !type.IsEnum)
                {
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

            var entity = world.Create();
            Console.WriteLine($"[Factory] Spawning Base: {blueprintId}...");

            // Pass the 'world' to the template applicator
            ApplyTemplate(entity, blueprintId, world);

            return entity;
        }

        // UPDATED SIGNATURE: Accepts 'World'
        public void ApplyTemplate(Entity entity, string templateId, World world)
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
                    object newComponentData = CreateComponentFromData(compType, compData);

                    // DEBUG: Verify parsing
                    if (compName == "Stats")
                    {
                        var stats = (TTRPG.Shared.Components.Stats)newComponentData;
                        Console.WriteLine($"  -> Merging Stats: Str {stats.Strength}, Agi {stats.Agility}");
                    }

                    // --- FIX IS HERE ---
                    // Use 'world' to check/remove/add components
                    if (world.Has(entity, compType))
                    {
                        world.Set(entity, newComponentData);
                    }
                    else
                    {
                        world.Add(entity, newComponentData);
                    }

                    // Add the new component
                    world.Add(entity, newComponentData);
                }
            }
        }

        private object CreateComponentFromData(Type type, Dictionary<string, object> data)
        {
            object instance = Activator.CreateInstance(type)!;

            foreach (var field in type.GetFields())
            {
                var key = data.Keys.FirstOrDefault(k => k.Equals(field.Name, StringComparison.OrdinalIgnoreCase));

                if (key != null)
                {
                    object val = data[key];
                    try
                    {
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