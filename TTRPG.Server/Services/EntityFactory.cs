using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Arch.Core;
using Arch.Core.Extensions;
using TTRPG.Shared.DTOs;
// IMPORTANT: Ensure we reference the namespace where Inventory lives to force assembly load
using TTRPG.Shared.Components;

namespace TTRPG.Server.Services
{
    public class EntityFactory
    {
        private readonly Dictionary<string, EntityBlueprint> _blueprints;

        // RESTORED: The Type Cache
        private readonly Dictionary<string, Type> _componentTypes = new Dictionary<string, Type>();

        public EntityFactory(IEnumerable<EntityBlueprint> blueprints)
        {
            _blueprints = blueprints.ToDictionary(b => b.Id);
            LoadTypes(); // RESTORED: Call the loader
        }

        public EntityFactory(Dictionary<string, EntityBlueprint> blueprints)
        {
            _blueprints = blueprints;
            LoadTypes();
        }

        // RESTORED: The Assembly Scanner
        private void LoadTypes()
        {
            _componentTypes.Clear();

            // 1. Force load the assembly containing standard components
            // We use typeof(Inventory) to guarantee we get TTRPG.Shared
            var sharedAssembly = typeof(Inventory).Assembly;

            foreach (var type in sharedAssembly.GetTypes())
            {
                if (type.IsValueType || type.IsClass)
                {
                    _componentTypes[type.Name] = type;
                }
            }

            Console.WriteLine($"[Factory] Loaded {_componentTypes.Count} component types from Shared.");
        }

        public Entity Create(string blueprintId, World world)
        {
            if (!_blueprints.ContainsKey(blueprintId))
            {
                throw new ArgumentException($"Blueprint '{blueprintId}' not found.");
            }

            var blueprint = _blueprints[blueprintId];
            var entity = world.Create();

            // Console.WriteLine($"[Factory] Building entity '{blueprintId}'...");

            foreach (var componentName in blueprint.Components.Keys)
            {
                if (!_componentTypes.ContainsKey(componentName))
                {
                    Console.WriteLine($"[Factory] ERROR: Component type '{componentName}' not found!");
                    continue;
                }

                var type = _componentTypes[componentName];
                var componentData = blueprint.Components[componentName];

                try
                {
                    var componentInstance = CreateComponentFromData(type, componentData);
                    SetOrAddComponent(world, entity, componentInstance, type);

                    // DEBUG: Confirm Inventory is added
                    if (componentName == "Inventory")
                    {
                        // Console.WriteLine($"[Factory] SUCCESS: Added Inventory to {blueprintId}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Factory] CRITICAL: Failed to add {componentName}: {ex.Message}");
                }
            }

            return entity;
        }

        public void ApplyTemplate(Entity entity, string templateId, World world)
        {
            if (!_blueprints.ContainsKey(templateId)) return;

            var template = _blueprints[templateId];
            foreach (var componentName in template.Components.Keys)
            {
                if (_componentTypes.TryGetValue(componentName, out var type))
                {
                    var data = template.Components[componentName];
                    var component = CreateComponentFromData(type, data);
                    SetOrAddComponent(world, entity, component, type);
                }
            }
        }

        private object CreateComponentFromData(Type type, Dictionary<string, object> data)
        {
            object instance = Activator.CreateInstance(type)!;

            foreach (var field in type.GetFields())
            {
                // Case-insensitive match for YAML keys (e.g. "strength" vs "Strength")
                var key = data.Keys.FirstOrDefault(k => k.Equals(field.Name, StringComparison.OrdinalIgnoreCase));

                if (key != null)
                {
                    object val = data[key];
                    try
                    {
                        // FIX: List<string> handling for Inventory.Items
                        if (field.FieldType == typeof(List<string>) && val is List<object> listObj)
                        {
                            var listStr = listObj.Select(x => x.ToString()).ToList();
                            field.SetValue(instance, listStr);
                        }
                        else
                        {
                            // Standard Value Types (int, float, string)
                            // Convert.ChangeType handles mostly everything (long -> int, double -> float)
                            object convertedVal = Convert.ChangeType(val, field.FieldType);
                            field.SetValue(instance, convertedVal);
                        }
                    }
                    catch
                    {
                        Console.WriteLine($"    - Failed to set field {field.Name} on {type.Name}");
                    }
                }
            }
            return instance;
        }

        private void SetOrAddComponent(World world, Entity entity, object component, Type componentType)
        {
            // Dynamic Arch Reflection
            // 1. Check if entity has it
            var hasMethod = typeof(World).GetMethods()
                .FirstOrDefault(m => m.Name == "Has" && m.IsGenericMethod && m.GetParameters().Length == 1);

            var hasGeneric = hasMethod!.MakeGenericMethod(componentType);
            bool alreadyHas = (bool)hasGeneric.Invoke(world, new object[] { entity })!;

            // 2. Select Add or Set
            string methodName = alreadyHas ? "Set" : "Add";

            var actionMethod = typeof(World).GetMethods()
                .FirstOrDefault(m => m.Name == methodName &&
                                     m.IsGenericMethod &&
                                     m.GetParameters().Length == 2 &&
                                     m.GetParameters()[0].ParameterType == typeof(Entity)); // Ensure we get the right overload!

            if (actionMethod != null)
            {
                var generic = actionMethod.MakeGenericMethod(componentType);
                generic.Invoke(world, new object[] { entity, component });
            }
        }
        public EntityBlueprint? GetBlueprint(string id)
        {
            if (_blueprints.ContainsKey(id))
            {
                return _blueprints[id];
            }
            return null;
        }
    }
}