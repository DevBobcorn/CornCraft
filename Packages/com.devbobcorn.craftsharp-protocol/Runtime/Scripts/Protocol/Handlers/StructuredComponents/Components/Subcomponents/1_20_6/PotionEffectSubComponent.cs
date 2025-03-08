using System;
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6
{
    public class PotionEffectSubComponent : SubComponent
    {
        public int TypeId { get; set; }
        public DetailsSubComponent Details { get; set; }

        public PotionEffectSubComponent(DataTypes dataTypes, SubComponentRegistry subComponentRegistry)
            : base(dataTypes, subComponentRegistry)
        {
            
        }
        
        protected override void Parse(Queue<byte> data)
        {
            TypeId = DataTypes.ReadNextVarInt(data);
            Details = (DetailsSubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.Details, data);
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(DataTypes.GetVarInt(TypeId));
            data.AddRange(Details.Serialize());
            return new Queue<byte>(data);
        }
    }
}