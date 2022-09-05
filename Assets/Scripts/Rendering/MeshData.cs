using Unity.Mathematics;

namespace MinecraftClient.Rendering
{
    public class MeshBuffer
    {
        public float3[] vert = { };
        public float2[] uv   = { };

        public uint[]    face = { };
        public uint offset = 0;

    }
}