using System;
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6
{
    public class EffectSubComponent : SubComponent
    {
        public PotionEffectSubComponent TypeId { get; set; }
        public float Probability { get; set; }

        public EffectSubComponent(DataTypes dataTypes, SubComponentRegistry subComponentRegistry)
            : base(dataTypes, subComponentRegistry)
        {
            
        }
        
        protected override void Parse(Queue<byte> data)
        {
            TypeId = (PotionEffectSubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.PotionEffect, data);
            Probability = DataTypes.ReadNextFloat(data);
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(TypeId.Serialize());
            data.AddRange(DataTypes.GetFloat(Probability));
            return new Queue<byte>(data);
        }
    }
}