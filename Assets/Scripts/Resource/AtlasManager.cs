using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Mathematics;

using MinecraftClient.Rendering;

namespace MinecraftClient.Resource
{
    public static class AtlasManager
    {
        private static Dictionary<ResourceLocation, Rect> texAtlasTable = new();

        public static float2[] GetUVs(ResourceLocation identifier, Vector4 part, int areaRot)
        {
            return GetUVsAt(GetAtlasRect(identifier), part, areaRot);
        }

        private static float2[] GetUVsAt(Rect rect, Vector4 part, int areaRot)
        {
            var oneU = rect.width;
            var oneV = rect.width; // Use width here because a texture can contain multiple frames

            // Get texture offset in atlas
            float2 o = new(rect.xMin, rect.yMin);

            // vect:  x,  y,  z,  w
            // vect: x1, y1, x2, y2
            float u1 = part.x * oneU, v1 = part.y * oneV;
            float u2 = part.z * oneU, v2 = part.w * oneV;

            return areaRot switch
            {
                0 => new float2[]{ new float2(       u1, oneV - v1) + o, new float2(       u2, oneV - v1) + o, new float2(       u1, oneV - v2) + o, new float2(       u2, oneV - v2) + o }, //   0 Deg
                1 => new float2[]{ new float2(       v1,        u1) + o, new float2(       v1,        u2) + o, new float2(       v2,        u1) + o, new float2(       v2,        u2) + o }, //  90 Deg
                2 => new float2[]{ new float2(oneU - u1,        v1) + o, new float2(oneU - u2,        v1) + o, new float2(oneU - u1,        v2) + o, new float2(oneU - u2,        v2) + o }, // 180 Deg
                3 => new float2[]{ new float2(oneV - v1, oneV - u1) + o, new float2(oneV - v1, oneU - u2) + o, new float2(oneV - v2, oneU - u1) + o, new float2(oneV - v2, oneU - u2) + o }, // 270 Deg

                _ => new float2[]{ new float2(       u1, oneV - v1) + o, new float2(       u2, oneV - v1) + o, new float2(       u1, oneV - v2) + o, new float2(       u2, oneV - v2) + o }  // Default
            };
        }        

        private static Rect GetAtlasRect(ResourceLocation identifier)
        {
            if (texAtlasTable.ContainsKey(identifier))
                return texAtlasTable[identifier];
            
            return Rect.zero; // TODO Fix
        }

        private static readonly Texture2D[] atlasTexture = new Texture2D[]
        { 
            new Texture2D(2, 2),
            new Texture2D(2, 2)
        };

        public static Texture2D GetAtlasTexture(RenderType type)
        {
            return type switch
            {
                RenderType.CUTOUT        => atlasTexture[0],
                RenderType.CUTOUT_MIPPED => atlasTexture[1],
                RenderType.SOLID         => atlasTexture[0],
                RenderType.TRANSLUCENT   => atlasTexture[0],

                _                        => atlasTexture[0]
            };
        }
        
        public static IEnumerator Generate(ResourcePackManager packManager, LoadStateInfo loadStateInfo)
        {
            texAtlasTable.Clear(); // Clear previously loaded table...

            var texDict = packManager.TextureFileTable;

            int count = 0;

            var textures = new Texture2D[texDict.Count];
            var ids = new ResourceLocation[texDict.Count];

            foreach (var pair in texDict) // Stitch texture atlas...
            {
                var texFilePath = pair.Value;
                ids[count] = pair.Key;
                //Debug.Log($"Loading {texId} from {pair.Key}");

                Texture2D tex = new(2, 2);
                tex.LoadImage(File.ReadAllBytes(texFilePath));

                textures[count] = tex;

                count++;
                if (count % 20 == 0)
                {
                    loadStateInfo.infoText = $"Loading texture atlas {pair.Key}";
                    yield return null;
                }
            }
            
            var atlas = new Texture2D(2, 2); // First assign a placeholder
            atlas.filterMode = FilterMode.Point;
            var rects = atlas.PackTextures(textures, 0, 4096, false);

            atlasTexture[0] = atlas;
            atlasTexture[1] = atlas;

            for (int i = 0;i < textures.Length;i++)
            {
                texAtlasTable.Add(ids[i], rects[i]);
                //Debug.Log($"{ids[i]} => {rects[i].xMin} {rects[i].xMax} {rects[i].yMin} {rects[i].yMax}");
            }

            //File.WriteAllBytes(@"D:\Images\AtlasPrev.png", atlas.EncodeToPNG());

        }

    }

}