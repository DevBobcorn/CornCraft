using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class LodestoneTrackerComponent : StructuredComponent
    {
        public bool HasGlobalPosition { get; set; }
        public string Dimension { get; set; } = null!;
        public Location Position { get; set; }
        public bool Tracked { get; set; }

        public LodestoneTrackerComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }
        
        public override void Parse(Queue<byte> data)
        {
            HasGlobalPosition = DataTypes.ReadNextBool(data);

            if (HasGlobalPosition)
            {
                Dimension = DataTypes.ReadNextString(data);
                Position = DataTypes.ReadNextLocation(data);
            }
            
            Tracked = DataTypes.ReadNextBool(data);
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(DataTypes.GetBool(HasGlobalPosition));

            if (HasGlobalPosition)
            {
                data.AddRange(DataTypes.GetString(Dimension));
                data.AddRange(DataTypes.GetLocation(Position));
            }
            
            data.AddRange(DataTypes.GetBool(Tracked));
            return new Queue<byte>(data);
        }
    }
}