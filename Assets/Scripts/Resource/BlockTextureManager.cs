using System.Collections.Generic;
using System.IO;
using UnityEngine;

using MinecraftClient.Rendering;

namespace MinecraftClient.Resource
{
    public class BlockTextureManager
    {
        private static Dictionary<ResourceLocation, int> blockAtlasTable = new Dictionary<ResourceLocation, int>();
        private static Dictionary<RenderType, int> plcboAtlasTable       = new Dictionary<RenderType, int>();
        private static bool initialized = false;

        public static Vector2[] GetUVs(ResourceLocation identifier, Vector4 part, int areaRot)
        {
            return GetUVsAtOffset(GetAtlasOffset(identifier), part, areaRot);
        }

        private const int TexturesInALine = 32;
        private const float One = 1.0F / TexturesInALine; // Size of a single block texture

        private static Vector2[] GetUVsAtOffset(int offset, Vector4 part, int areaRot)
        {
            // vect: x,  y,  z,  w
            // vect: x1, y1, x2, y2

            part *= One;

            float blockU = (offset % TexturesInALine) / (float)TexturesInALine;
            float blockV = (offset / TexturesInALine) / (float)TexturesInALine;
            Vector2 o = new Vector2(blockU, blockV);

            float u1 = part.x, v1 = part.y;
            float u2 = part.z, v2 = part.w;

            return areaRot switch
            {
                0 => new Vector2[]{ new Vector2(      u1, One - v1) + o, new Vector2(      u2, One - v1) + o, new Vector2(      u1, One - v2) + o, new Vector2(      u2, One - v2) + o }, //   0 Deg
                1 => new Vector2[]{ new Vector2(      v1,       u1) + o, new Vector2(      v1,       u2) + o, new Vector2(      v2,       u1) + o, new Vector2(      v2,       u2) + o }, //  90 Deg
                2 => new Vector2[]{ new Vector2(One - u1,       v1) + o, new Vector2(One - u2,       v1) + o, new Vector2(One - u1,       v2) + o, new Vector2(One - u2,       v2) + o }, // 180 Deg
                3 => new Vector2[]{ new Vector2(One - v1, One - u1) + o, new Vector2(One - v1, One - u2) + o, new Vector2(One - v2, One - u1) + o, new Vector2(One - v2, One - u2) + o }, // 270 Deg

                _ => new Vector2[]{ new Vector2(      u1, One - v1) + o, new Vector2(      u2, One - v1) + o, new Vector2(      u1, One - v2) + o, new Vector2(      u2, One - v2) + o }  // Default
            };
        }        

        private static int GetAtlasOffset(ResourceLocation identifier)
        {
            EnsureInitialized();

            if (blockAtlasTable.ContainsKey(identifier))
                return blockAtlasTable[identifier];
            
            return 0;
        }

        private static Texture2D plcboTexture = new Texture2D(2, 2); // First assign a place holder...
        public static Texture2D PlcboTexture
        {
            get {
                EnsureInitialized();
                return plcboTexture;
            }
        }

        private static Texture2D[] atlasTexture = new Texture2D[]
        { 
            new Texture2D(2, 2),
            new Texture2D(2, 2)
        };
        public static Texture2D GetAtlasTexture(RenderType type)
        {
            EnsureInitialized();
            return type switch
            {
                RenderType.CUTOUT        => atlasTexture[0],
                RenderType.CUTOUT_MIPPED => atlasTexture[1],
                RenderType.SOLID         => atlasTexture[0],
                RenderType.TRANSLUCENT   => atlasTexture[0],

                _                        => atlasTexture[0]
            };
        }

        public static void EnsureInitialized()
        {
            if (!initialized) Initialze();
        }

        private static void Initialze()
        {
            Debug.Log("Read and initialize Block Atlas table...");
            string atlasFilePath = PathHelper.GetPacksDirectory() + "/block_atlas.png";
            string atlasJsonPath = PathHelper.GetPacksDirectory() + "/block_atlas_dict.json";

            string plcboFilePath = PathHelper.GetPacksDirectory() + "/block_atlas_placebo.png";

            if (File.Exists(atlasJsonPath) && File.Exists(atlasFilePath) && File.Exists(plcboFilePath))
            {
                // Set up atlas textures...
                for (int i = 0;i < atlasTexture.Length;i++)
                {
                    atlasTexture[i].LoadImage(File.ReadAllBytes(atlasFilePath));
                    atlasTexture[i].filterMode = FilterMode.Point;
                }
                
                atlasTexture[1].mipMapBias = -1F;

                string jsonText = File.ReadAllText(atlasJsonPath);
                Json.JSONData atlasJson = Json.ParseJson(jsonText);

                foreach (KeyValuePair<string, Json.JSONData> item in atlasJson.Properties)
                {
                    if (blockAtlasTable.ContainsKey(ResourceLocation.fromString(item.Key)))
                    {
                        throw new InvalidDataException("Duplicate block atlas with one name " + item.Key + "!?");
                    }
                    else
                    {
                        blockAtlasTable[ResourceLocation.fromString(item.Key)] = int.Parse(item.Value.StringValue);
                    }
                }

                plcboTexture.LoadImage(File.ReadAllBytes(plcboFilePath));
                plcboTexture.filterMode = FilterMode.Point;

                plcboAtlasTable.Add(RenderType.SOLID,         0);
                plcboAtlasTable.Add(RenderType.CUTOUT,        1);
                plcboAtlasTable.Add(RenderType.CUTOUT_MIPPED, 2);
                plcboAtlasTable.Add(RenderType.TRANSLUCENT,   3);

            }
            else
            {
                Debug.LogWarning("Texture files not all available!");
            }

            initialized = true;

        }
        
    }

}