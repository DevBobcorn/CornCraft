#nullable enable
using System;
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class FireworkExplosionComponent : StructuredComponent
    {
        public FireworkExplosionSubComponent? FireworkExplosionSubComponent { get; set; }

        public FireworkExplosionComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }
        
        public override void Parse(Queue<byte> data)
        {
            FireworkExplosionSubComponent = (FireworkExplosionSubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.FireworkExplosion, data);
        }

        public override Queue<byte> Serialize()
        {
            return FireworkExplosionSubComponent!.Serialize();
        }
    }
}