using System;
using System.Collections.Generic;
using System.Linq;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class FireworksComponent : StructuredComponent
    {
        public int FlightDuration { get; set; }
        public int NumberOfExplosions { get; set; }

        public List<FireworkExplosionSubComponent> Explosions { get; set; } = new();

        public FireworksComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }
        
        public override void Parse(Queue<byte> data)
        {
            FlightDuration = DataTypes.ReadNextVarInt(data);
            NumberOfExplosions = DataTypes.ReadNextVarInt(data);

            if (NumberOfExplosions > 0)
            {
                for(var i = 0; i < NumberOfExplosions; i++)
                    Explosions.Add(
                        (FireworkExplosionSubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.FireworkExplosion,
                            data));
            }
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(DataTypes.GetVarInt(FlightDuration));
            data.AddRange(DataTypes.GetVarInt(NumberOfExplosions));
            if (NumberOfExplosions > 0)
            {
                if (NumberOfExplosions != Explosions.Count)
                    throw new Exception("Can't serialize FireworksComponent because NumberOfExplosions and the lenght of Explosions differ!");
                
                foreach(var explosion in Explosions)
                    data.AddRange(explosion.Serialize().ToList());
            }
            return new Queue<byte>(data);
        }
    }
}