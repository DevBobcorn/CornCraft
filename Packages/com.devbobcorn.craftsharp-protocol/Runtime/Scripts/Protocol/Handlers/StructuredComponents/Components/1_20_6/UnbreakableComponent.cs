using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class UnbrekableComponent1206 : StructuredComponent
    {
        public bool Unbrekable { get; set; }

        public UnbrekableComponent1206(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {
            
        }
        
        public override void Parse(Queue<byte> data)
        {
            Unbrekable = DataTypes.ReadNextBool(data);
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(DataTypes.GetBool(Unbrekable));
            return new Queue<byte>(data);
        }
    }
}