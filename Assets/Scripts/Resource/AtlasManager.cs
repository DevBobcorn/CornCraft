using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Unity.Mathematics;

using MinecraftClient.Rendering;

namespace MinecraftClient.Resource
{
    public static class AtlasManager
    {
        public struct TextureInfo
        {
            public Rect bounds;
            public int index;
            public bool animatable;

            public TextureInfo(Rect bounds, int index, bool animatable)
            {
                this.bounds = bounds;
                this.index = index;
                this.animatable = animatable;
            }
        }

        public static readonly TextureInfo DEFAULT_TEXTURE_INFO = new(new(), 0, false);

        private static Dictionary<ResourceLocation, TextureInfo> texAtlasTable = new();

        public static float3[] GetUVs(ResourceLocation identifier, Vector4 part, int areaRot)
        {
            var info = GetTextureInfo(identifier);
            return GetUVsAt(info.bounds, info.index, info.animatable, part, areaRot);
        }

        private static float3[] GetUVsAt(Rect bounds, int index, bool animatable, Vector4 part, int areaRot)
        {
            float oneU = bounds.width;
            float oneV;

            if (animatable)
            {
                // Use width here because a texture can contain multiple frames
                // TODO Real support for animatable texture

                oneV = bounds.width; 
            }
            else
                oneV = bounds.height;

            // Get texture offset in atlas
            float3 o = new(bounds.xMin, bounds.yMin, index + 0.1F);

            // vect:  x,  y,  z,  w
            // vect: x1, y1, x2, y2
            float u1 = part.x * oneU, v1 = part.y * oneV;
            float u2 = part.z * oneU, v2 = part.w * oneV;

            return areaRot switch
            {
                0 => new float3[]{ new float3(       u1, oneV - v1, 0F) + o, new float3(       u2, oneV - v1, 0F) + o, new float3(       u1, oneV - v2, 0F) + o, new float3(       u2, oneV - v2, 0F) + o }, //   0 Deg
                1 => new float3[]{ new float3(       v1,        u1, 0F) + o, new float3(       v1,        u2, 0F) + o, new float3(       v2,        u1, 0F) + o, new float3(       v2,        u2, 0F) + o }, //  90 Deg
                2 => new float3[]{ new float3(oneU - u1,        v1, 0F) + o, new float3(oneU - u2,        v1, 0F) + o, new float3(oneU - u1,        v2, 0F) + o, new float3(oneU - u2,        v2, 0F) + o }, // 180 Deg
                3 => new float3[]{ new float3(oneV - v1, oneV - u1, 0F) + o, new float3(oneV - v1, oneU - u2, 0F) + o, new float3(oneV - v2, oneU - u1, 0F) + o, new float3(oneV - v2, oneU - u2, 0F) + o }, // 270 Deg

                _ => new float3[]{ new float3(       u1, oneV - v1, 0F) + o, new float3(       u2, oneV - v1, 0F) + o, new float3(       u1, oneV - v2, 0F) + o, new float3(       u2, oneV - v2, 0F) + o }  // Default
            };
        }

        private static TextureInfo GetTextureInfo(ResourceLocation identifier)
        {
            if (texAtlasTable.ContainsKey(identifier))
                return texAtlasTable[identifier];
            
            return DEFAULT_TEXTURE_INFO;
        }

        private static readonly Texture2DArray[] atlasArrays = new Texture2DArray[]
        { 
            new Texture2DArray(2, 2, 1, DefaultFormat.HDR, TextureCreationFlags.None),
            new Texture2DArray(2, 2, 1, DefaultFormat.HDR, TextureCreationFlags.None)
        };

        public static Texture2DArray GetAtlasArray(RenderType type)
        {
            return type switch
            {
                RenderType.CUTOUT        => atlasArrays[0],
                RenderType.CUTOUT_MIPPED => atlasArrays[1],
                RenderType.SOLID         => atlasArrays[0],
                RenderType.TRANSLUCENT   => atlasArrays[0],

                _                        => atlasArrays[0]
            };
        }

        public const int ATLAS_SIZE = 512;
        
        public static IEnumerator Generate(ResourcePackManager packManager, LoadStateInfo loadStateInfo)
        {
            texAtlasTable.Clear(); // Clear previously loaded table...

            var texDict = packManager.TextureFileTable;

            int count = 0;

            var textures = new Texture2D[texDict.Count];
            var ids = new ResourceLocation[texDict.Count];

            foreach (var pair in texDict) // Load texture files...
            {
                var texFilePath = pair.Value;
                ids[count] = pair.Key;
                //Debug.Log($"Loading {texId} from {pair.Key}");

                Texture2D tex = new(2, 2);
                tex.name = pair.Key.ToString();
                tex.LoadImage(File.ReadAllBytes(texFilePath));

                textures[count] = tex;

                count++;
                if (count % 20 == 0)
                {
                    loadStateInfo.infoText = $"Loading texture file {pair.Key}";
                    yield return null;
                }
            }
            
            int curTexIndex = 0, curAtlasIndex = 0;
            List<Texture2D> atlases = new();

            int totalVolume = ATLAS_SIZE * ATLAS_SIZE;
            int maxContentVolume = (int)(totalVolume * 0.95F);

            do
            {
                loadStateInfo.infoText = $"Stitching texture atlas #{curAtlasIndex}";
                
                // First count all the textures to be stitched onto this atlas
                int lastTexIndex = curTexIndex - 1, curVolume = 0; // lastTexIndex is inclusive

                while (true)
                {
                    if (lastTexIndex >= texDict.Count - 1)
                        break;

                    var nextTex = textures[lastTexIndex + 1];
                    curVolume += nextTex.width * nextTex.height;

                    if (curVolume < maxContentVolume)
                        lastTexIndex++;
                    else
                        break;
                }

                int consumedTexCount = lastTexIndex + 1 - curTexIndex;

                if (consumedTexCount == 0)
                {
                    // In this occasion the texture is too large and can only be scaled a bit and placed on a separate atlas
                    lastTexIndex = curTexIndex;
                    consumedTexCount = 1;
                }

                // Then we go stitch 'em        (inclusive)..(exclusive)
                var texturesConsumed = textures[curTexIndex..(lastTexIndex + 1)];

                var atlas = new Texture2D(ATLAS_SIZE, ATLAS_SIZE); // First assign a placeholder
                atlas.filterMode = FilterMode.Point;

                var rects = atlas.PackTextures(texturesConsumed, 0, ATLAS_SIZE, false);

                if (atlas.width != ATLAS_SIZE || atlas.height != ATLAS_SIZE)
                {
                    // Size not right, replace it (usually the last atlas in array which doesn't
                    // have enough textures to take up all the place and thus is smaller in size)
                    var newAtlas = new Texture2D(ATLAS_SIZE, ATLAS_SIZE);

                    Graphics.CopyTexture(atlas, 0, 0, 0, 0, atlas.width, atlas.height, newAtlas, 0, 0, 0, 0);

                    float scaleX = (float) atlas.width  / (float) ATLAS_SIZE;
                    float scaleY = (float) atlas.height / (float) ATLAS_SIZE;

                    // Rescale the texture boundaries
                    for (int i = 0;i < rects.Length;i++)
                    {
                        rects[i] = new Rect(
                            rects[i].x     * scaleX,    rects[i].y      * scaleY,
                            rects[i].width * scaleX,    rects[i].height * scaleY
                        );
                    }

                    atlas = newAtlas;
                }

                atlases.Add(atlas);

                for (int i = 0;i < consumedTexCount;i++)
                {
                    //Debug.Log($"{ids[curTexIndex + i]} => ({curAtlasIndex}) {rects[i].xMin} {rects[i].xMax} {rects[i].yMin} {rects[i].yMax}");
                    
                    // TODO Read texture meta file and use that information
                    bool animatable = ids[curTexIndex + i].Path.StartsWith("item") || ids[curTexIndex + i].Path.StartsWith("block");
                    
                    texAtlasTable.Add(ids[curTexIndex + i], new(rects[i], curAtlasIndex, animatable));
                }

                curTexIndex += consumedTexCount;

                curAtlasIndex++;

                yield return null;

            }
            while (curTexIndex < texDict.Count);

            var atlasArray = new Texture2DArray(ATLAS_SIZE, ATLAS_SIZE, curAtlasIndex, DefaultFormat.HDR, TextureCreationFlags.None);
            atlasArray.filterMode = FilterMode.Point;

            for (int index = 0;index < atlases.Count;index++)
            {
                atlasArray.SetPixels(atlases[index].GetPixels(), index, 0);
            }

            atlasArray.Apply(true, false);

            atlasArrays[0] = atlasArray;
            atlasArrays[1] = atlasArray;

        }

    }

}