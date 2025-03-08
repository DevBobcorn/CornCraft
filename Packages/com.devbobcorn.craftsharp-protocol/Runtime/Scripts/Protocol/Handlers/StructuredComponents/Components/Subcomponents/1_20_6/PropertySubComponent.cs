#nullable enable
using System;
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6
{
    public class PropertySubComponent : SubComponent
    {
        public string? Name { get; set; }
        public bool IsExactMatch { get; set; }
        public string? ExactValue { get; set; }
        public string? MinValue { get; set; }
        public string? MaxValue { get; set; }

        public PropertySubComponent(DataTypes dataTypes, SubComponentRegistry subComponentRegistry)
            : base(dataTypes, subComponentRegistry)
        {
            
        }
        
        protected override void Parse(Queue<byte> data)
        {
            Name = DataTypes.ReadNextString(data);
            IsExactMatch = DataTypes.ReadNextBool(data);

            if (IsExactMatch)
                ExactValue = DataTypes.ReadNextString(data);
            else // Ranged Match
            {
                MinValue = DataTypes.ReadNextString(data);
                MaxValue = DataTypes.ReadNextString(data);
            }
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();

            if (string.IsNullOrEmpty(Name?.Trim()))
                throw new ArgumentNullException($"Can not serialize a Property sub-component if the Name is null or empty!");
            
            data.AddRange(DataTypes.GetString(Name));
            data.AddRange(DataTypes.GetBool(IsExactMatch));

            if (IsExactMatch)
            {
                if (string.IsNullOrEmpty(ExactValue?.Trim()))
                    throw new ArgumentNullException($"Can not serialize a Property sub-component if the ExactValue is null or empty when the type is Exact Match!");
                
                data.AddRange(DataTypes.GetString(ExactValue));
            }
            else
            {
                if (string.IsNullOrEmpty(MinValue?.Trim()) || string.IsNullOrEmpty(MaxValue?.Trim()))
                    throw new ArgumentNullException($"Can not serialize a Property sub-component if the MinValue or MaxValue is null or empty when the type is not Exact Match!");
                
                data.AddRange(DataTypes.GetString(MinValue));
                data.AddRange(DataTypes.GetString(MaxValue));
            }
            
            return new Queue<byte>(data);
        }
    }
}