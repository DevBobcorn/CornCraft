using System.Collections.Generic;
using System.Linq;

namespace CraftSharp.Resource
{
    public class EntityGeometry
    {
        public readonly Dictionary<string, EntityModelBone> Bones;
        public int TextureWidth;
        public int TextureHeight;

        private EntityGeometry(Dictionary<string, EntityModelBone> bones, int texWidth, int texHeight)
        {
            Bones = bones;
            TextureWidth = texWidth;
            TextureHeight = texHeight;
        }

        public static Dictionary<string, EntityGeometry> TableFromJson(Json.JSONData data)
        {
            var geoVersion = data.Properties["format_version"].StringValue;
            var result = new Dictionary<string, EntityGeometry>();

            if (geoVersion == "1.12.0") // New format (not sure from what version did it start TODO: Figure out)
            {
                foreach (var item in data.Properties["minecraft:geometry"].DataArray) // For each geometry json
                {
                    var itemDesc = item.Properties["description"];
                    var itemName = itemDesc.Properties["identifier"].StringValue;

                    int texWidth = int.Parse(itemDesc.Properties["texture_width"].StringValue);
                    int texHeight = int.Parse(itemDesc.Properties["texture_height"].StringValue);

                    var bones = item.Properties["bones"].DataArray.Select(x => EntityModelBone.FromJson(x))
                            .ToDictionary(x => x.Name, x => x);

                    result.Add(itemName, new EntityGeometry(bones, texWidth, texHeight));
                }
            }
            else // Old format, 1.8.0 or older
            {
                foreach (var pair in data.Properties.Where(x => x.Key.StartsWith("geometry."))) // For each geometry json
                {
                    var item = pair.Value;
                    var itemName = pair.Key;

                    int texWidth = int.Parse(item.Properties["texturewidth"].StringValue);
                    int texHeight = int.Parse(item.Properties["textureheight"].StringValue);

                    var bones = item.Properties["bones"].DataArray.Select(x => EntityModelBone.FromJson(x))
                            .ToDictionary(x => x.Name, x => x);

                    result.Add(itemName, new EntityGeometry(bones, texWidth, texHeight));
                }
            }

            return result;
        }
    }
}