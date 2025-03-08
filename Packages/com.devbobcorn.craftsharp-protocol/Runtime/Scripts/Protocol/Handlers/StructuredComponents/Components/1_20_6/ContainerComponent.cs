using System.Collections.Generic;
using CraftSharp.Inventory;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class ContainerComponent : StructuredComponent
    {
        public int NumberOfItems { get; set; }
        public List<ItemStack> Items { get; set; } = new();

        public ContainerComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }
        
        public override void Parse(Queue<byte> data)
        {
            NumberOfItems = DataTypes.ReadNextVarInt(data);
            for (var i = 0; i < NumberOfItems; i++)
            {
                var item = DataTypes.ReadNextItemSlot(data, ItemPalette);

                if (item is null)
                    continue;
                
                Items.Add(item);
            }
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(DataTypes.GetVarInt(NumberOfItems));
            for (var i = 0; i < NumberOfItems; i++)
                data.AddRange(DataTypes.GetItemSlot(Items[i], ItemPalette));
                
            return new Queue<byte>(data);
        }
    }
}