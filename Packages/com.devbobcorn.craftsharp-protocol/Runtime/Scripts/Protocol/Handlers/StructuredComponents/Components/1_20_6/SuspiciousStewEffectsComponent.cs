using System;
using System.Collections.Generic;
using System.Linq;
using CraftSharp.Inventory;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class SuspiciousStewEffectsComponent : StructuredComponent
    {
        public int NumberOfEffects { get; set; }
        public List<SuspiciousStewEffect> Effects { get; set; } = new();

        public SuspiciousStewEffectsComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }

        public override void Parse(Queue<byte> data)
        {
            NumberOfEffects = DataTypes.ReadNextVarInt(data);

            for (var i = 0; i < NumberOfEffects; i++)
                Effects.Add(new SuspiciousStewEffect(DataTypes.ReadNextVarInt(data), DataTypes.ReadNextVarInt(data)));
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(DataTypes.GetVarInt(NumberOfEffects));

            if (NumberOfEffects != Effects.Count)
                throw new InvalidOperationException("Can not serialize SuspiciousStewEffectsComponent1206 because umberOfEffects != Effects.Count!");
            
            foreach (var effect in Effects)
            {
                data.AddRange(DataTypes.GetVarInt(effect.TypeId));
                data.AddRange(DataTypes.GetVarInt(effect.Duration));
            }
            return new Queue<byte>(data);
        }
    }
}