using Unity.Mathematics;

namespace MinecraftClient.Resource
{
    public class ItemGeometry
    {
        public readonly bool isGenerated = false;
        private readonly float3[] vertexArr;
        private readonly float3[] uvArr;
        private readonly float4[] uvAnimArr;
        private readonly int[] tintIndexArr;

        public ItemGeometry(float3[] vArr, float3[] uvArr, float4[] aArr, int[] tArr, bool isGenerated)
        {
            this.vertexArr = vArr;
            this.uvArr = uvArr;
            this.uvAnimArr = aArr;
            this.tintIndexArr = tArr;
            this.isGenerated = isGenerated;
        }

        public void Build(ref VertexBuffer buffer, float3 posOffset, float3[] itemTints)
        {
            int vertexCount = buffer.vert.Length + vertexArr.Length;

            var verts = new float3[vertexCount];
            var txuvs = new float3[vertexCount];
            var uvans = new float4[vertexCount];
            var tints = new float3[vertexCount];

            buffer.vert.CopyTo(verts, 0);
            buffer.txuv.CopyTo(txuvs, 0);
            buffer.uvan.CopyTo(uvans, 0);
            buffer.tint.CopyTo(tints, 0);

            uint i, vertOffset = (uint)buffer.vert.Length;

            if (vertexArr.Length > 0)
            {
                for (i = 0U;i < vertexArr.Length;i++)
                {
                    verts[i + vertOffset] = vertexArr[i] + posOffset;
                    tints[i + vertOffset] = tintIndexArr[i] >= 0 && tintIndexArr[i] < itemTints.Length ? itemTints[tintIndexArr[i]] : BlockGeometry.DEFAULT_COLOR;
                }
                uvArr.CopyTo(txuvs, vertOffset);
                uvAnimArr.CopyTo(uvans, vertOffset);
            }

            buffer.vert = verts;
            buffer.txuv = txuvs;
            buffer.uvan = uvans;
            buffer.tint = tints;

        }
    }
}