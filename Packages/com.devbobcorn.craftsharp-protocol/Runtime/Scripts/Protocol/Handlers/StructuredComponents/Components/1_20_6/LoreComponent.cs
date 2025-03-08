using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;
using CraftSharp.Protocol.Message;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class LoreNameComponent1206 : StructuredComponent
    {
        public int NumberOfLines { get; set; }
        public List<string> Lines { get; set; } = new();

        public LoreNameComponent1206(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }
        
        public override void Parse(Queue<byte> data)
        {
            NumberOfLines = DataTypes.ReadNextVarInt(data);
            
            if (NumberOfLines <= 0) return;
            
            for (var i = 0; i < NumberOfLines; i++)
                Lines.Add(ChatParser.ParseText(DataTypes.ReadNextString(data)));
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(DataTypes.GetVarInt(Lines.Count));

            if (Lines.Count <= 0) return new Queue<byte>(data);
            
            foreach (var line in Lines)
                data.AddRange(DataTypes.GetString(line));

            return new Queue<byte>(data);
        }
    }
}