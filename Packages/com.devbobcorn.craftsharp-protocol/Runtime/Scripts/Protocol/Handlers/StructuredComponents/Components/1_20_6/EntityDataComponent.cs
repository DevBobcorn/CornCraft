#nullable enable
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class EntityDataComponent : StructuredComponent
    {
        public Dictionary<string, object>? Nbt { get; set; }

        public EntityDataComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
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

    public class BucketEntityDataComponent : EntityDataComponent
    {
        public BucketEntityDataComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }
    }

    public class BlockEntityDataComponent : EntityDataComponent
    {
        public BlockEntityDataComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }
    }
}