using System.Collections.Generic;
using CraftSharp.Inventory;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class EnchantmentsComponent : StructuredComponent
    {
        public int NumberOfEnchantments { get; set; }
        public List<Enchantment> Enchantments { get; set; } = new();
        public bool ShowTooltip { get; set; }

        public EnchantmentsComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }

        public override void Parse(Queue<byte> data)
        {
            NumberOfEnchantments = DataTypes.ReadNextVarInt(data);

            for (var i = 0; i < NumberOfEnchantments; i++)
                Enchantments.Add(new Enchantment((Enchantments)DataTypes.ReadNextVarInt(data), DataTypes.ReadNextVarInt(data)));

            ShowTooltip = DataTypes.ReadNextBool(data);
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(DataTypes.GetVarInt(Enchantments.Count));
            foreach (var enchantment in Enchantments)
            {
                data.AddRange(DataTypes.GetVarInt((int)enchantment.Type));
                data.AddRange(DataTypes.GetVarInt(enchantment.Level));
            }
            data.AddRange(DataTypes.GetBool(ShowTooltip));
            return new Queue<byte>(data);
        }
    }
}