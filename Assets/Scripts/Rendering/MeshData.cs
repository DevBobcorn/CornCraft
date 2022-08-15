using UnityEngine;

namespace MinecraftClient.Rendering
{
    public class MeshBuffer
    {
        public Vector3[] vert = { };
        public int[]     face = { };
        public Vector2[] uv   = { };

        public int offset = 0;

    }
}