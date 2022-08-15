using System.Collections.Generic;
using UnityEngine;

namespace MinecraftClient.Resource
{
    public struct TextureReference
    {
        public bool isPointer;
        public string name;

        public TextureReference(bool pointer, string name)
        {
            isPointer = pointer;
            this.name = name;
        }
    }

    public class BlockModel
    {
        private const int MAXDEPTH = 50;
        // texName -> texture resource location
        public readonly Dictionary<string, TextureReference> textures = new Dictionary<string, TextureReference>();
        public readonly List<BlockModelElement> elements = new List<BlockModelElement>();

        public ResourceLocation resolveTextureName(string texName)
        {
            if (textures.ContainsKey(texName))
            {
                return resolveTextureRef(textures[texName]);
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
                if (textures.ContainsKey(texRef.name)) // Pointer valid, go to that tex ref
                {
                    texRef = textures[texRef.name];
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
