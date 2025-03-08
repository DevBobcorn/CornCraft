#nullable enable
using System;
using System.Collections.Generic;
using CraftSharp.Inventory;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;
using CraftSharp.Protocol.Message;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class TrimComponent : StructuredComponent
    {
        public int TrimMaterialType { get; set; }
        public string AssetName { get; set; } = null!;
        public int Ingredient { get; set; }
        public float ItemModelIndex { get; set; }
        public int NumberOfOverrides { get; set; }
        public List<TrimAssetOverride>? Overrides { get; set; }
        public string Description { get; set; } = null!;
        public int TrimPatternType { get; set; }
        public string TrimPatternTypeAssetName { get; set; } = null!;
        public int TemplateItem { get; set; }
        public string TrimPatternTypeDescription { get; set; } = null!;
        public bool Decal { get; set; }
        public bool ShowInTooltip { get; set; }

        public TrimComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }
        
        public override void Parse(Queue<byte> data)
        {
            TrimMaterialType = DataTypes.ReadNextVarInt(data);

            if (TrimMaterialType == 0)
            {
                AssetName = DataTypes.ReadNextString(data);
                Ingredient = DataTypes.ReadNextVarInt(data);
                ItemModelIndex = DataTypes.ReadNextFloat(data);
                NumberOfOverrides = DataTypes.ReadNextVarInt(data);

                if (NumberOfOverrides > 0)
                {
                    Overrides = new();

                    for (var i = 0; i < NumberOfOverrides; i++)
                        Overrides.Add(new TrimAssetOverride(DataTypes.ReadNextVarInt(data),
                            DataTypes.ReadNextString(data)));
                }

                Description = ChatParser.ParseText(DataTypes.ReadNextString(data));
            }

            TrimPatternType = DataTypes.ReadNextVarInt(data);

            if (TrimPatternType == 0)
            {
                TrimPatternTypeAssetName = DataTypes.ReadNextString(data);
                TemplateItem = DataTypes.ReadNextVarInt(data);
                TrimPatternTypeDescription = DataTypes.ReadNextString(data);
                Decal = DataTypes.ReadNextBool(data);
            }

            ShowInTooltip = DataTypes.ReadNextBool(data);
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();

            data.AddRange(DataTypes.GetVarInt(TrimMaterialType));

            if (TrimMaterialType == 0)
            {
                if (string.IsNullOrEmpty(AssetName) || string.IsNullOrEmpty(Description))
                    throw new NullReferenceException("Can't serialize the TrimComponent because the Asset Name or Description are null!");
                
                data.AddRange(DataTypes.GetString(AssetName));
                data.AddRange(DataTypes.GetVarInt(Ingredient));
                data.AddRange(DataTypes.GetFloat(ItemModelIndex));
                data.AddRange(DataTypes.GetVarInt(NumberOfOverrides));
                if (NumberOfOverrides > 0)
                {
                    if(NumberOfOverrides != Overrides?.Count)
                        throw new NullReferenceException("Can't serialize the TrimComponent because value of NumberOfOverrides and the size of Overrides don't match!");
                    
                    foreach (var (armorMaterialType, assetName) in Overrides)
                    {
                        data.AddRange(DataTypes.GetVarInt(armorMaterialType));
                        data.AddRange(DataTypes.GetString(assetName));
                    }
                }
                data.AddRange(DataTypes.GetString(Description));

                data.AddRange(DataTypes.GetVarInt(TrimPatternType));
                if (TrimPatternType == 0)
                {
                    if (string.IsNullOrEmpty(TrimPatternTypeAssetName) || string.IsNullOrEmpty(TrimPatternTypeDescription))
                        throw new NullReferenceException("Can't serialize the TrimComponent because the TrimPatternTypeAssetName or TrimPatternTypeDescription are null!");
                    
                    data.AddRange(DataTypes.GetString(TrimPatternTypeAssetName));
                    data.AddRange(DataTypes.GetVarInt(TemplateItem));
                    data.AddRange(DataTypes.GetString(TrimPatternTypeDescription));
                    data.AddRange(DataTypes.GetBool(Decal));
                }
                
                data.AddRange(DataTypes.GetBool(ShowInTooltip));
            }
            
            return new Queue<byte>(data);
        }
    }
}