using System;
using System.Collections.Generic;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Core
{
    public abstract class StructuredComponentRegistry
    {
        private Dictionary<string, Type> ComponentParsers { get; } = new();
        private Dictionary<int, string> IdToComponent { get; } = new();
        private Dictionary<string, int> ComponentToId { get; } = new();

        private readonly DataTypes dataTypes;
        private readonly ItemPalette itemPalette;
        private readonly SubComponentRegistry subComponentRegistry;

        public StructuredComponentRegistry(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
        {
            this.dataTypes = dataTypes;
            this.itemPalette = itemPalette;
            this.subComponentRegistry = subComponentRegistry;
        }

        protected void RegisterComponent<T>(int id, string name) where T : StructuredComponent
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            name = name.ToLower();

            if (ComponentParsers.ContainsKey(name) || IdToComponent.ContainsValue(name)
                                                || ComponentToId.ContainsKey(name) || IdToComponent.ContainsKey(id))
                throw new InvalidOperationException($"A component with name '{name}' or id '{id}' is already registered.");

            ComponentParsers[name] = typeof(T);
            IdToComponent[id] = name;
            ComponentToId[name] = id;
        }

        public StructuredComponent ParseComponent(int id, Queue<byte> data)
        {
            if (IdToComponent.TryGetValue(id, out var name))
            {
                if (ComponentParsers.TryGetValue(name, out var type))
                {
                    var component =
                        Activator.CreateInstance(type, dataTypes, itemPalette, subComponentRegistry) as StructuredComponent 
                        ?? throw new InvalidOperationException($"Could not instantiate a parser for a structured component type {name}");
                    
                    component.Parse(data);
                    return component;
                }
            }

            throw new Exception($"No parser found for component with ID {id}");
        }

        public string GetComponentNameById(int id)
        {
            if (IdToComponent.TryGetValue(id, out var value))
                return value;

            throw new Exception($"No component found for ID {id}");
        }

        public int GetComponentIdByName(string name)
        {
            name = name.ToLower();

            if (ComponentToId.TryGetValue(name, out var value))
                return value;

            throw new Exception($"No ID found for component {name}");
        }
    }
}