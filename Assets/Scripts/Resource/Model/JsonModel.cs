using System.Collections.Generic;
using UnityEngine;

namespace MinecraftClient.Resource
{
    public class JsonModel
    {
        private const int MAXDEPTH = 50;
        
        // texName -> texture resource location
        public readonly Dictionary<string, TextureReference> Textures = new Dictionary<string, TextureReference>();
        public readonly List<JsonModelElement> Elements = new List<JsonModelElement>();

        public ResourceLocation resolveTextureName(string texName)
        {
            if (Textures.ContainsKey(texName))
            {
                return resolveTextureRef(Textures[texName]);
            }

            // Might be templates who have place holder textures...
            // Debug.LogWarning("Texture " + texName + " not found in model");
            return ResourceLocation.INVALID;
        }

        public ResourceLocation resolveTextureRef(TextureReference texRef)
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
                    Debug.LogWarning("Failed to get texture " + texRef.name + " There might be a reference loop");
                    return ResourceLocation.INVALID;
                }

                depth++;
            }

            // Reach our destination, a tex ref whose name is a resource location
            return ResourceLocation.fromString(texRef.name);
        }
    }
}