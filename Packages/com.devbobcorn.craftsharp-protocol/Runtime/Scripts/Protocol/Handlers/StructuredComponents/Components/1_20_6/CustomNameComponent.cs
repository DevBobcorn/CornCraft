using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;
using CraftSharp.Protocol.Message;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class CustomNameComponent : StructuredComponent
    {
        public string CustomName { get; set; } = string.Empty;

        public CustomNameComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }
        
        public override void Parse(Queue<byte> data)
        {
            CustomName = ChatParser.ParseText(DataTypes.ReadNextString(data));
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(DataTypes.GetString(CustomName));
            return new Queue<byte>(data);
        }
    }
}