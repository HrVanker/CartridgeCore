using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection; // Required for the fix
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

        // Cache MethodInfos to improve performance (optional but good practice)
        private static readonly MethodInfo _setMethodTemplate = typeof(World).GetMethods()
            .First(m => m.Name == "Set" && m.IsGenericMethod && m.GetParameters().Length == 2);

        private static readonly MethodInfo _addMethodTemplate = typeof(World).GetMethods()
            .First(m => m.Name == "Add" && m.IsGenericMethod && m.GetParameters().Length == 2);

        public EntityFactory(List<EntityBlueprint> blueprints)
        {
            _blueprints = blueprints.ToDictionary(b => b.Id);
            _componentRegistry = new Dictionary<string, Type>();

            // AUTO-DISCOVERY
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

            ApplyTemplate(entity, blueprintId, world);

            return entity;
        }

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

                    // FIX: Use Reflection to invoke the Generic Set<T> / Add<T> methods.
                    // This forces the Runtime to unbox the 'object' into the actual Struct 
                    // before handing it to Arch. This prevents InvalidCastExceptions 
                    // caused by trying to shove a Boxed Object into a Struct Array.
                    SetOrAddComponent(world, entity, newComponentData, compType);
                }
            }
        }

        private void SetOrAddComponent(World world, Entity entity, object component, Type componentType)
        {
            // 1. Define match criteria for the Entity parameter (can be "Entity" or "ref Entity")
            bool IsEntityParam(ParameterInfo p) =>
                p.ParameterType == typeof(Entity) ||
                p.ParameterType == typeof(Entity).MakeByRefType();

            // 2. Find "Has<T>(Entity)"
            var hasMethod = typeof(World).GetMethods()
                .FirstOrDefault(m => m.Name == "Has" &&
                                     m.IsGenericMethod &&
                                     m.GetParameters().Length == 1 &&
                                     IsEntityParam(m.GetParameters()[0]));

            if (hasMethod == null) throw new MissingMethodException($"Could not find World.Has<{componentType.Name}>(Entity)");

            // 3. Check if the entity already has the component
            var hasGeneric = hasMethod.MakeGenericMethod(componentType);
            bool alreadyHasComponent = (bool)hasGeneric.Invoke(world, new object[] { entity })!;

            // 4. Select "Set<T>" or "Add<T>" based on existence
            string methodName = alreadyHasComponent ? "Set" : "Add";

            var actionMethod = typeof(World).GetMethods()
                .FirstOrDefault(m => m.Name == methodName &&
                                     m.IsGenericMethod &&
                                     m.GetParameters().Length == 2 &&
                                     IsEntityParam(m.GetParameters()[0]));

            if (actionMethod == null) throw new MissingMethodException($"Could not find World.{methodName}<{componentType.Name}>(Entity, Component)");

            // 5. Invoke the correct method
            var actionGeneric = actionMethod.MakeGenericMethod(componentType);
            actionGeneric.Invoke(world, new object[] { entity, component });
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