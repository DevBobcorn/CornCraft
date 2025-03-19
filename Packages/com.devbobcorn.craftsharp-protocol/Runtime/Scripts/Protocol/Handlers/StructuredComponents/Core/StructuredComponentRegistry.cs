using System;
using System.Collections.Generic;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Core
{
    public abstract class StructuredComponentRegistry : IdentifierPalette<Type>
    {
        private readonly DataTypes dataTypes;
        private readonly ItemPalette itemPalette;
        private readonly SubComponentRegistry subComponentRegistry;

        protected override string Name => "StructuredComponent Palette";
        protected override Type UnknownObject => typeof (StructuredComponent);

        public StructuredComponentRegistry(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
        {
            this.dataTypes = dataTypes;
            this.itemPalette = itemPalette;
            this.subComponentRegistry = subComponentRegistry;
        }
        
        protected void RegisterComponent<T>(int numId, string name)
        {
            AddEntry(ResourceLocation.FromString(name), numId, typeof (T));
        }

        public StructuredComponent ParseComponent(int numId, Queue<byte> data)
        {
            if (TryGetByNumId(numId, out var type))
            {
                var component =
                    Activator.CreateInstance(type, dataTypes, itemPalette, subComponentRegistry) as StructuredComponent 
                    ?? throw new InvalidOperationException($"Could not instantiate a parser for a structured component type {numId}");
                
                component.Parse(data);
                return component;
            }

            throw new Exception($"No parser found for component with num Id {numId}");
        }
    }
}