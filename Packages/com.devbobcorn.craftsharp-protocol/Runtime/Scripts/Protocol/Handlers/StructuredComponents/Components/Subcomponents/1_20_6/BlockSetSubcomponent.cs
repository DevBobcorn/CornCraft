#nullable enable
using System;
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6
{
    public class BlockSetSubcomponent : SubComponent
    {
        public int Type { get; set; }
        public string? TagName { get; set; }
        public List<int>? BlockIds { get; set; }

        public BlockSetSubcomponent(DataTypes dataTypes, SubComponentRegistry subComponentRegistry)
            : base(dataTypes, subComponentRegistry)
        {
            
        }
        
        protected override void Parse(Queue<byte> data)
        {
            Type = DataTypes.ReadNextVarInt(data);

            if (Type == 0)
                TagName = DataTypes.ReadNextString(data);

            if (Type == 0) return;
            
            BlockIds = new();
                
            for (var i = 0; i < Type - 1; i++)
                BlockIds.Add(DataTypes.ReadNextVarInt(data));
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(DataTypes.GetVarInt(Type));
            if (Type == 0)
            {
                if (string.IsNullOrEmpty(TagName?.Trim()))
                    throw new ArgumentNullException($"Can not serialize an empty tag name when the Block Set type is 0!");
                
                data.AddRange(DataTypes.GetString(TagName));
            }

            if (Type == 0) return new Queue<byte>(data);
            
            if(BlockIds == null || BlockIds.Count == 0)
                throw new ArgumentNullException($"Can not serialize an empty list of Block IDs in a Block Set when the type is not 0!");
                
            for(var i = 0; i < Type - 1; i++)
                data.AddRange(DataTypes.GetVarInt(BlockIds[i]));

            return new Queue<byte>(data);
        }
    }
}