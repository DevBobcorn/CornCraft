#nullable enable
using System;
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6
{
    public class BlockPredicateSubcomponent : SubComponent
    {
        public bool HasBlocks { get; set; }
        public BlockSetSubcomponent? BlockSet { get; set; }
        public bool HasProperities { get; set; }
        public List<PropertySubComponent>? Properties { get; set; }
        public bool HasNbt { get; set; }
        public Dictionary<string, object>? Nbt { get; set; }

        public BlockPredicateSubcomponent(DataTypes dataTypes, SubComponentRegistry subComponentRegistry)
            : base(dataTypes, subComponentRegistry)
        {
            
        }
            
        protected override void Parse(Queue<byte> data)
        {
            HasBlocks = DataTypes.ReadNextBool(data);

            if (HasBlocks)
                BlockSet = (BlockSetSubcomponent)SubComponentRegistry.ParseSubComponent(SubComponents.BlockSet, data);

            HasProperities = DataTypes.ReadNextBool(data);

            if (HasProperities)
            {
                Properties = new();
                var numberOfProperties = DataTypes.ReadNextVarInt(data);
                for (var i = 0; i < numberOfProperties; i++)
                    Properties.Add((PropertySubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.Property, data));
            }

            HasNbt = DataTypes.ReadNextBool(data);
            
            if (HasNbt)
                Nbt = DataTypes.ReadNextNbt(data, DataTypes.UseAnonymousNBT);
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            
            // Block Sets
            data.AddRange(DataTypes.GetBool(HasBlocks));
            if (HasBlocks)
            {
                if(BlockSet == null)
                    throw new ArgumentNullException($"Can not serialize a BlockPredicate when the BlockSet is empty but HasBlocks is true!");
                
                data.AddRange(BlockSet.Serialize());
            }
            
            // Properites
            data.AddRange(DataTypes.GetBool(HasProperities));
            if (HasProperities)
            {
                if(Properties == null || Properties.Count == 0)
                    throw new ArgumentNullException($"Can not serialize a BlockPredicate when the Properties is empty but HasProperties is true!");

                foreach (var property in Properties)
                    data.AddRange(property.Serialize());
            }

            // NBT
            if (HasNbt)
            {
                if(Nbt == null)
                    throw new ArgumentNullException($"Can not serialize a BlockPredicate when the Nbt is empty but HasNbt is true!");
                
                data.AddRange(DataTypes.GetNbt(Nbt));
            }
            
            return new Queue<byte>(data);
        }
    }
}