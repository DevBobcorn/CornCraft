using System.Collections.Generic;
using System.Linq;

namespace CraftSharp.Resource
{
    public class EntityDefinition
    {
        private static readonly BedrockVersion UNSPECIFIED_VERSION = new(999, 0, 0);
        // File versions
        public BedrockVersion FormatVersion;
        public BedrockVersion MinEngineVersion;
        // Identifier of this entity type
        public ResourceLocation EntityType;
        // Texture name => texture path in pack
        public Dictionary<string, string> TexturePaths = new();
        // Variant name => geometry name
        public Dictionary<string, string> GeometryNames = new();

        private EntityDefinition(BedrockVersion formatVersion, BedrockVersion minEnVersion, ResourceLocation entityType)
        {
            FormatVersion = formatVersion;
            MinEngineVersion = minEnVersion;

            EntityType = entityType;
        }

        public static EntityDefinition FromJson(Json.JSONData data)
        {
            var defVersion = BedrockVersion.FromString(data.Properties["format_version"].StringValue);

            var desc = data.Properties["minecraft:client_entity"].Properties["description"];
            var entityType = ResourceLocation.FromString(desc.Properties["identifier"].StringValue);

            Dictionary<string, string> texturePaths;
            if (desc.Properties.ContainsKey("textures"))
            {
                texturePaths = desc.Properties["textures"].Properties.ToDictionary(x => x.Key, x => x.Value.StringValue);
            }
            else
            {
                texturePaths = new();
            }

            Dictionary<string, string> geometryNames;
            if (desc.Properties.ContainsKey("geometry"))
            {
                geometryNames = desc.Properties["geometry"].Properties.ToDictionary(x => x.Key, x => x.Value.StringValue);
            }
            else
            {
                geometryNames = new();
            }

            var minEnVersion = UNSPECIFIED_VERSION;
            if (desc.Properties.ContainsKey("min_engine_version"))
            {
                minEnVersion = BedrockVersion.FromString(desc.Properties["min_engine_version"].StringValue);
            }

            return new(defVersion, minEnVersion, entityType)
            {
                TexturePaths = texturePaths,
                GeometryNames = geometryNames
            };
        }
    }
}