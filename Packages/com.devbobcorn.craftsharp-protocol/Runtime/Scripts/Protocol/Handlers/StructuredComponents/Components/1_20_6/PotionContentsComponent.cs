using System;
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class PotionContentsComponent : StructuredComponent
    {
        public bool HasPotionId { get; set; }
        public int PotiononId { get; set; }
        public bool HasCustomColor { get; set; }
        public int CustomColor { get; set; }
        public int NumberOfCustomEffects { get; set; }
        public List<PotionEffectSubComponent> Effects { get; set; } = new();

        public PotionContentsComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }
        
        public override void Parse(Queue<byte> data)
        {
            HasPotionId = DataTypes.ReadNextBool(data);
            PotiononId = HasPotionId ? DataTypes.ReadNextVarInt(data) : 0; // TODO: Find from the registry
            HasCustomColor = DataTypes.ReadNextBool(data);
            CustomColor = HasCustomColor ? DataTypes.ReadNextInt(data) : 0; // TODO: Find from the registry
            NumberOfCustomEffects = DataTypes.ReadNextVarInt(data);
            
            for(var i = 0; i < NumberOfCustomEffects; i++)
                Effects.Add((PotionEffectSubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.PotionEffect, data));
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(DataTypes.GetBool(HasPotionId));
            data.AddRange(DataTypes.GetVarInt(PotiononId));
            data.AddRange(DataTypes.GetBool(HasCustomColor));
            data.AddRange(DataTypes.GetInt(CustomColor));

            if (NumberOfCustomEffects > 0)
            {
                if(Effects.Count != NumberOfCustomEffects)
                    throw new ArgumentNullException($"Can not serialize PotionContentsComponentComponent1206 due to NumberOfCustomEffects being different from the count of elements in the Effects list!");
                
                foreach(var effect in Effects)
                    data.AddRange(effect.Serialize());
            }
            
            return new Queue<byte>(data);
        }
    }
}