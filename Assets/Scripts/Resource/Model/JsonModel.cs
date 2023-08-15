using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace CraftSharp.Resource
{
    public class JsonModel
    {
        private const int MAXDEPTH = 50;
        
        // texName -> texture resource location
        public readonly Dictionary<string, TextureReference> Textures = new();
        public readonly List<JsonModelElement> Elements = new();
        public readonly Dictionary<DisplayPosition, float3x3> DisplayTransforms = new();

        public ResourceLocation ResolveTextureName(string texName)
        {
            if (Textures.ContainsKey(texName))
            {
                return ResolveTextureRef(Textures[texName]);
            }

            // Might be templates who have place holder textures...
            // Debug.LogWarning("Texture " + texName + " not found in model");
            return ResourceLocation.INVALID;
        }

        public ResourceLocation ResolveTextureRef(TextureReference texRef)
        {
            int depth = 0;
            while (texRef.isPointer)
            {
                if (Textures.ContainsKey(texRef.name)) // Pointer valid, go to that tex ref
                {
                    texRef = Textures[texRef.name];
                }
                else
                {
                    // Might be templates who have place holder textures...
                    // Debug.LogWarning("Texture cannot be found: " + texRef.name);
                    return ResourceLocation.INVALID;
                }

                if (depth > MAXDEPTH)
                {
                    Debug.LogWarning($"Failed to get texture {texRef.name}, there might be a reference loop");
                    return ResourceLocation.INVALID;
                }

                depth++;
            }

            // Reach our destination, a tex ref whose name is a resource location
            return ResourceLocation.fromString(texRef.name);
        }
    }
}