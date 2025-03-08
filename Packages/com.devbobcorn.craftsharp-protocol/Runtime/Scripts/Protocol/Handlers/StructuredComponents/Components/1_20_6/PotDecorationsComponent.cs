using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class PotDecorationsComponent : StructuredComponent
    {
        public int NumberOfItems { get; set; }
        public List<int> Items { get; set; } = new();

        public PotDecorationsComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }
        
        public override void Parse(Queue<byte> data)
        {
            NumberOfItems = DataTypes.ReadNextVarInt(data);
            for(var i = 0; i < NumberOfItems; i++)
                Items.Add(DataTypes.ReadNextVarInt(data));
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(DataTypes.GetVarInt(NumberOfItems));
            for(var i = 0; i < NumberOfItems; i++)
                data.AddRange(DataTypes.GetVarInt(Items[i]));
            return new Queue<byte>(data);
        }
    }
}