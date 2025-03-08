#nullable enable
using System;
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class BannerPatternsComponent : StructuredComponent
    {
        public int NumberOfLayers { get; set; }
        public List<BannerLayer> Layers { get; set; } = new();

        public BannerPatternsComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {

        }
        
        public override void Parse(Queue<byte> data)
        {
            NumberOfLayers = DataTypes.ReadNextVarInt(data);

            for (var i = 0; i < NumberOfLayers; i++)
            {
                var patternType = DataTypes.ReadNextVarInt(data);
                Layers.Add(new BannerLayer
                {
                    PatternType = patternType,
                    AssetId = patternType == 0 ? DataTypes.ReadNextString(data) : null,
                    TranslationKey = patternType == 0 ? DataTypes.ReadNextString(data) : null,
                    DyeColor = DataTypes.ReadNextVarInt(data)
                });
            }
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            data.AddRange(DataTypes.GetVarInt(NumberOfLayers));

            if (NumberOfLayers > 0)
            {
                if (NumberOfLayers != Layers.Count)
                    throw new Exception("Can't serialize BannerPatternsComponent because NumberOfLayers and Layers.Count differ!");

                foreach (var bannerLayer in Layers)
                {
                    data.AddRange(DataTypes.GetVarInt(bannerLayer.PatternType));

                    if (bannerLayer.PatternType == 0)
                    {
                        if(string.IsNullOrEmpty(bannerLayer.AssetId) || string.IsNullOrEmpty(bannerLayer.TranslationKey))
                            throw new Exception("Can't serialize BannerPatternsComponent because AssetId or TranslationKey is null/empty!");
                        
                        data.AddRange(DataTypes.GetString(bannerLayer.AssetId));
                        data.AddRange(DataTypes.GetString(bannerLayer.TranslationKey));
                    }
                    
                    data.AddRange(DataTypes.GetVarInt(bannerLayer.DyeColor));
                }
            }
            
            return new Queue<byte>(data);
        }
    }

    public class BannerLayer
    {
        public int PatternType { get; set; }
        public string? AssetId { get; set; } = null!;
        public string? TranslationKey { get; set; } = null!;
        public int DyeColor { get; set; }
    }
}