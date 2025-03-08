using System.Collections.Generic;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Core
{
    public abstract class StructuredComponent
    {
        protected DataTypes DataTypes { get; private set; }
        protected SubComponentRegistry SubComponentRegistry { get; private set; }
        protected ItemPalette ItemPalette { get; private set; }

        public StructuredComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
        {
            DataTypes = dataTypes;
            ItemPalette = itemPalette;
            SubComponentRegistry = subComponentRegistry;
        }
        
        public abstract void Parse(Queue<byte> data);
        public abstract Queue<byte> Serialize();
    }
}