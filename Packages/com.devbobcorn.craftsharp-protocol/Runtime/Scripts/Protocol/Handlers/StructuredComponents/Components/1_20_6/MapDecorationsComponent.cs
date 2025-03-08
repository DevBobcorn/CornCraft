#nullable enable
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class MapDecorationsComponent : StructuredComponent
    {
        public Dictionary<string, object>? Nbt { get; set; } = new();

        public MapDecorationsComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }
        
        public override void Parse(Queue<byte> data)
        {
            Nbt = DataTypes.ReadNextNbt(data, DataTypes.UseAnonymousNBT);
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(DataTypes.GetNbt(Nbt));
            return new Queue<byte>(data);
        }
    }
}