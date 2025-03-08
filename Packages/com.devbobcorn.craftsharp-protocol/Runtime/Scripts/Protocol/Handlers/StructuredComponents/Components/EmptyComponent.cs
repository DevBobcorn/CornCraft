using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components
{
    public class EmptyComponent : StructuredComponent
    {
        public EmptyComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {
            
        }

        public override void Parse(Queue<byte> data)
        {
        }

        public override Queue<byte> Serialize()
        {
            return new Queue<byte>();
        }
    }
}