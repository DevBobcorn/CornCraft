using Unity.Mathematics;

namespace CraftSharp.Resource
{
    public class VertexBuffer
    {
        /// <summary>
        /// Vertex position array
        /// </summary>
        public float3[] vert = { };

        /// <summary>
        /// Texture uv array, 3d because we're using a texture array
        /// </summary>
        public float3[] txuv = { };

        /// <summary>
        /// Texture uv animation array (frame count, frame interval, frame offset)
        /// </summary>
        public float4[] uvan = { };

        /// <summary>
        /// Tint color / block light array
        /// </summary>
        public float4[] tint = { };
    }
}