using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CraftSharp.Resource
{
    public class EntityGeometry
    {
        private static readonly BedrockVersion UNSPECIFIED_VERSION = new(999, 0, 0);
        // File versions
        public BedrockVersion FormatVersion;
        public BedrockVersion MinEngineVersion;

        public Dictionary<string, EntityModelBone> Bones = new();
        public int TextureWidth = 0;
        public int TextureHeight = 0;

        private static readonly BedrockVersion V1_12_0 = new(1, 12, 0);

        private EntityGeometry(BedrockVersion formatVersion, BedrockVersion minEnVersion)
        {
            FormatVersion = formatVersion;
            MinEngineVersion = minEnVersion;
        }

        public static Dictionary<string, EntityGeometry> TableFromJson(Json.JSONData data)
        {
            var geoVersion = BedrockVersion.FromString(data.Properties["format_version"].StringValue);
            var result = new Dictionary<string, EntityGeometry>();

            if (geoVersion >= V1_12_0) // New format (not sure from what version did it start TODO: Figure out)
            {
                foreach (var item in data.Properties["minecraft:geometry"].DataArray) // For each geometry json
                {
                    var itemDesc = item.Properties["description"];
                    var itemName = itemDesc.Properties["identifier"].StringValue;

                    int texWidth = 0, texHeight = 0;
                    // Texture size can somehow be given as something like "64.0", which cannot be parsed directly
                    // into an integer, so we need to handle this here
                    if (itemDesc.Properties.ContainsKey("texture_width"))
                    {
                        if (!int.TryParse(itemDesc.Properties["texture_width"].StringValue, out texWidth))
                        {   // Try parsing as float point number, and then round to int
                            texWidth = Mathf.RoundToInt(float.Parse(itemDesc.Properties["texture_width"].StringValue));
                        }
                    }
                    if (itemDesc.Properties.ContainsKey("texture_height"))
                    {
                        if (!int.TryParse(itemDesc.Properties["texture_height"].StringValue, out texHeight))
                        {   // Try parsing as float point number, and then round to int
                            texHeight = Mathf.RoundToInt(float.Parse(itemDesc.Properties["texture_height"].StringValue));
                        }
                    }

                    Dictionary<string, EntityModelBone> bones;
                    if (item.Properties.ContainsKey("bones"))
                        bones = item.Properties["bones"].DataArray.Select(x => EntityModelBone.FromJson(x))
                                .ToDictionary(x => x.Name, x => x);
                    else
                        bones = new();
                    
                    var minEnVersion = UNSPECIFIED_VERSION;
                    if (itemDesc.Properties.ContainsKey("min_engine_version"))
                        minEnVersion = BedrockVersion.FromString(itemDesc.Properties["min_engine_version"].StringValue);

                    result.Add(itemName, new EntityGeometry(geoVersion, minEnVersion)
                    {
                        Bones = bones,
                        TextureWidth = texWidth,
                        TextureHeight = texHeight
                    });
                }
            }
            else // Old format, 1.8.0 or older
            {
                foreach (var pair in data.Properties.Where(x => x.Key.StartsWith("geometry."))) // For each geometry json
                {
                    var item = pair.Value;
                    var itemName = pair.Key;

                    int texWidth = 0, texHeight = 0;
                    // Texture size can somehow be given as something like "64.0", which cannot be parsed directly
                    // into an integer, so we need to handle this here
                    if (item.Properties.ContainsKey("texturewidth"))
                    {
                        if (!int.TryParse(item.Properties["texturewidth"].StringValue, out texWidth))
                        {   // Try parsing as float point number, and then round to int
                            texWidth = Mathf.RoundToInt(float.Parse(item.Properties["texturewidth"].StringValue));
                        }
                    }
                    if (item.Properties.ContainsKey("textureheight"))
                    {
                        if (!int.TryParse(item.Properties["textureheight"].StringValue, out texHeight))
                        {   // Try parsing as float point number, and then round to int
                            texHeight = Mathf.RoundToInt(float.Parse(item.Properties["textureheight"].StringValue));
                        }
                    }

                    Dictionary<string, EntityModelBone> bones;
                    if (item.Properties.ContainsKey("bones"))
                        bones = item.Properties["bones"].DataArray.Select(x => EntityModelBone.FromJson(x))
                                .ToDictionary(x => x.Name, x => x);
                    else
                        bones = new();
                    
                    var minEnVersion = UNSPECIFIED_VERSION;
                    if (item.Properties.ContainsKey("min_engine_version"))
                        minEnVersion = BedrockVersion.FromString(item.Properties["min_engine_version"].StringValue);

                    result.Add(itemName, new EntityGeometry(geoVersion, minEnVersion)
                    {
                        Bones = bones,
                        TextureWidth = texWidth,
                        TextureHeight = texHeight
                    });
                }
            }

            return result;
        }
    }
}