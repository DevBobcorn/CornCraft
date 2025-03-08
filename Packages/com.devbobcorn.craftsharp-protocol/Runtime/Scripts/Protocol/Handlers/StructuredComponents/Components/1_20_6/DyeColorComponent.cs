using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class DyeColorComponent : StructuredComponent
    {
        public int Color { get; set; }
        public bool ShowInTooltip { get; set; }

        public DyeColorComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }
        
        public override void Parse(Queue<byte> data)
        {
            Color = DataTypes.ReadNextInt(data);
            ShowInTooltip = DataTypes.ReadNextBool(data);
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(DataTypes.GetInt(Color));
            data.AddRange(DataTypes.GetBool(ShowInTooltip));
            return new Queue<byte>(data);
        }
    }
}