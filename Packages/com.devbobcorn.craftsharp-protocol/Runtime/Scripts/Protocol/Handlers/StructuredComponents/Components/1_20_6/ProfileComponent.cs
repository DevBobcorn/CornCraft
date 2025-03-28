#nullable enable
using System;
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class ProfileComponent : StructuredComponent
    {
        public bool HasName { get; set; }
        public string? Name { get; set; } = null!;
        public bool HasUniqueId { get; set; }
        public Guid UUID { get; set; }
        public int NumberOfProperties { get; set; }
        public List<ProfileProperty> ProfileProperties { get; set; } = new();

        public ProfileComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }
        
        public override void Parse(Queue<byte> data)
        {
            HasName = DataTypes.ReadNextBool(data);
            
            if(HasName)
                Name = DataTypes.ReadNextString(data);

            HasUniqueId = DataTypes.ReadNextBool(data);

            if (HasUniqueId)
                UUID = DataTypes.ReadNextUUID(data);

            NumberOfProperties = DataTypes.ReadNextVarInt(data);
            for (var i = 0; i < NumberOfProperties; i++)
            {
                var propertyName = DataTypes.ReadNextString(data);
                var propertyValue = DataTypes.ReadNextString(data);
                var hasSignature = DataTypes.ReadNextBool(data);
                var signature = hasSignature ? DataTypes.ReadNextString(data) : null;
                
                ProfileProperties.Add(new ProfileProperty(propertyName, propertyValue, hasSignature, signature));
            }
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            
            data.AddRange(DataTypes.GetBool(HasName));
            if (HasName)
            {
                if (string.IsNullOrEmpty(Name))
                    throw new NullReferenceException("Can't serialize the ProfileComponent because the Name is null/empty!");
                    
                data.AddRange(DataTypes.GetString(Name));
            }
            
            if (HasUniqueId)
                data.AddRange(DataTypes.GetUUID(UUID));

            if (NumberOfProperties > 0)
            {
                if(NumberOfProperties != ProfileProperties.Count)
                    throw new Exception("Can't serialize the ProfileComponent because the NumberOfProperties and ProfileProperties.Count differ!");

                foreach (var profileProperty in ProfileProperties)
                {
                    data.AddRange(DataTypes.GetString(profileProperty.Name));
                    data.AddRange(DataTypes.GetString(profileProperty.Value));
                    data.AddRange(DataTypes.GetBool(profileProperty.HasSignature));
                    if (profileProperty.HasSignature)
                    {
                        if(string.IsNullOrEmpty(profileProperty.Signature))
                            throw new NullReferenceException("Can't serialize the ProfileComponent because HasSignature is true, but the Signature is null/empty!");
                        
                        data.AddRange(DataTypes.GetString(profileProperty.Signature));
                    }
                }
            }
            
            return new Queue<byte>(data);
        }
    }

    public record ProfileProperty(string Name, string Value, bool HasSignature, string? Signature)
    {
        public string Name { get; } = Name;
        public string Value { get; } = Value;
        public bool HasSignature { get; } = HasSignature;
        public string? Signature { get; } = Signature;
    }
}