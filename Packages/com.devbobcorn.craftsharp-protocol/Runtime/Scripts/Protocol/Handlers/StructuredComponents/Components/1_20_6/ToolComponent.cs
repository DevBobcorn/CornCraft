using System;
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class ToolComponent : StructuredComponent
    {
        public int NumberOfRules { get; set; }
        public List<RuleSubComponent> Rules { get; set; } = new();
        public float DefaultMiningSpeed { get; set; }
        public int DamagePerBlock { get; set; }

        public ToolComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }
        
        public override void Parse(Queue<byte> data)
        {
            NumberOfRules = DataTypes.ReadNextVarInt(data);

            for (var i = 0; i < NumberOfRules; i++)
                Rules.Add((RuleSubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.Rule, data));

            DefaultMiningSpeed = DataTypes.ReadNextFloat(data);
            DamagePerBlock = DataTypes.ReadNextVarInt(data);
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(DataTypes.GetVarInt(NumberOfRules));
            
            if(Rules.Count != NumberOfRules)
                throw new ArgumentNullException($"Can not serialize a ToolComponent1206 when the Rules count != NumberOfRules!");
            
            foreach (var rule in Rules)
                data.AddRange(rule.Serialize());
            
            data.AddRange(DataTypes.GetFloat(DefaultMiningSpeed));
            data.AddRange(DataTypes.GetVarInt(DamagePerBlock));
            return new Queue<byte>(data);
        }
    }
}