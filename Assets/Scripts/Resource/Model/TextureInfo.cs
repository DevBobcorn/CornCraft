using UnityEngine;

namespace MinecraftClient.Resource
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
}