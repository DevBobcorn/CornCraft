#nullable enable
using System;
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class InstrumentComponent : StructuredComponent
    {
        public int InstrumentType { get; set; }
        public int SoundEventType { get; set; }
        public string? SoundName { get; set; } = null!;
        public bool HasFixedRange { get; set; }
        public float FixedRange { get; set; }
        public float UseDuration { get; set; }
        public float Range { get; set; }

        public InstrumentComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }
        
        public override void Parse(Queue<byte> data)
        {
            InstrumentType = DataTypes.ReadNextVarInt(data);

            if (InstrumentType == 0)
            {
                SoundEventType = DataTypes.ReadNextVarInt(data);
                SoundName = DataTypes.ReadNextString(data);

                if (SoundEventType == 0)
                {
                    HasFixedRange = DataTypes.ReadNextBool(data);
                    FixedRange = DataTypes.ReadNextFloat(data);
                }

                UseDuration = DataTypes.ReadNextFloat(data);
                Range = DataTypes.ReadNextFloat(data);
            }
            
            // TODO: Check, if we need to load in defaults from a registry
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(DataTypes.GetVarInt(InstrumentType));

            if (InstrumentType == 0)
            {
                data.AddRange(DataTypes.GetVarInt(SoundEventType));

                if (string.IsNullOrEmpty(SoundName))
                    throw new NullReferenceException("Can't serialize InstrumentComponent because SoundName is empty!");
                
                data.AddRange(DataTypes.GetString(SoundName));
                if (SoundEventType == 0)
                {
                    data.AddRange(DataTypes.GetBool(HasFixedRange));
                    data.AddRange(DataTypes.GetFloat(FixedRange));
                }
                
                data.AddRange(DataTypes.GetFloat(UseDuration));
                data.AddRange(DataTypes.GetFloat(Range));
            }
            
            // TODO: Check, if we need to load in defaults from a registry if InstrumentType != 0 and send them
            return new Queue<byte>(data);
        }
    }
}