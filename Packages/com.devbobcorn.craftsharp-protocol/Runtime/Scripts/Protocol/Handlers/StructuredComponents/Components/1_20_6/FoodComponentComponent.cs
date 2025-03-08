using System;
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class FoodComponentComponent : StructuredComponent
    {
        public int Nutrition { get; set; }
        public bool Saturation { get; set; }
        public bool CanAlwaysEat { get; set; }
        public float SecondsToEat { get; set; }
        public int NumberOfEffects { get; set; }
        public List<EffectSubComponent> Effects { get; set; } = new();

        public FoodComponentComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }
        
        public override void Parse(Queue<byte> data)
        {
            Nutrition = DataTypes.ReadNextVarInt(data);
            Saturation = DataTypes.ReadNextBool(data);
            CanAlwaysEat = DataTypes.ReadNextBool(data);
            SecondsToEat = DataTypes.ReadNextFloat(data);
            NumberOfEffects = DataTypes.ReadNextVarInt(data);
            
            for(var i = 0; i < NumberOfEffects; i++)
                Effects.Add((EffectSubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.Effect, data));
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(DataTypes.GetVarInt(Nutrition));
            data.AddRange(DataTypes.GetBool(Saturation));
            data.AddRange(DataTypes.GetBool(CanAlwaysEat));
            data.AddRange(DataTypes.GetFloat(SecondsToEat));
            data.AddRange(DataTypes.GetFloat(NumberOfEffects));

            if (NumberOfEffects > 0)
            {
                if(Effects.Count != NumberOfEffects)
                    throw new ArgumentNullException($"Can not serialize FoodComponent1206 due to NumberOfEffcets being different from the count of elements in the Effects list!");
                
                foreach(var effect in Effects)
                    data.AddRange(effect.Serialize());
            }
            
            return new Queue<byte>(data);
        }
    }
}