using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Unity.Mathematics;

namespace MinecraftClient.Resource
{
    public class BlockGeometry
    {
        public static readonly float3 DEFAULT_COLOR = new(1F, 1F, 1F);

        private readonly Dictionary<CullDir, float3[]> vertexArrs;
        private readonly Dictionary<CullDir, float3[]> uvArrs;
        private readonly Dictionary<CullDir, float4[]> uvAnimArrs;
        private readonly Dictionary<CullDir, int[]> tintIndexArrs;

        public BlockGeometry(Dictionary<CullDir, float3[]> vArrs, Dictionary<CullDir, float3[]> uvArrs,
                Dictionary<CullDir, float4[]> aArrs,Dictionary<CullDir, int[]> tArrs)
        {
            this.vertexArrs = vArrs;
            this.uvArrs = uvArrs;
            this.uvAnimArrs = aArrs;
            this.tintIndexArrs = tArrs;
        }

        // Cache for array sizes, mapping cull flags to corresponding vertex array sizes
        private readonly ConcurrentDictionary<int, int> sizeCache = new();

        private int CalculateArraySize(int cullFlags)
        {
            int vertexCount = vertexArrs[CullDir.NONE].Length;

            for (int dirIdx = 0;dirIdx < 6;dirIdx++)
            {
                if ((cullFlags & (1 << dirIdx)) != 0)
                    vertexCount += vertexArrs[(CullDir) (dirIdx + 1)].Length;
            }

            return vertexCount;
        }

        public void Build(ref VertexBuffer buffer, float3 posOffset, int cullFlags, float3 blockTint)
        {
            // Compute value if absent
            int vertexCount = buffer.vert.Length + (sizeCache.ContainsKey(cullFlags) ? sizeCache[cullFlags] :
                    (sizeCache[cullFlags] = CalculateArraySize(cullFlags)));

            var verts = new float3[vertexCount];
            var txuvs = new float3[vertexCount];
            var uvans = new float4[vertexCount];
            var tints = new float3[vertexCount];

            buffer.vert.CopyTo(verts, 0);
            buffer.txuv.CopyTo(txuvs, 0);
            buffer.uvan.CopyTo(uvans, 0);
            buffer.tint.CopyTo(tints, 0);

            uint i, vertOffset = (uint)buffer.vert.Length;

            if (vertexArrs[CullDir.NONE].Length > 0)
            {
                for (i = 0U;i < vertexArrs[CullDir.NONE].Length;i++)
                {
                    verts[i + vertOffset] = vertexArrs[CullDir.NONE][i] + posOffset;
                    tints[i + vertOffset] = tintIndexArrs[CullDir.NONE][i] >= 0 ? blockTint : DEFAULT_COLOR;
                }
                uvArrs[CullDir.NONE].CopyTo(txuvs, vertOffset);
                uvAnimArrs[CullDir.NONE].CopyTo(uvans, vertOffset);
                vertOffset += (uint)vertexArrs[CullDir.NONE].Length;
            }

            for (int dirIdx = 0;dirIdx < 6;dirIdx++)
            {
                var dir = (CullDir) (dirIdx + 1);

                if ((cullFlags & (1 << dirIdx)) != 0 && vertexArrs[dir].Length > 0)
                {
                    for (i = 0U;i < vertexArrs[dir].Length;i++)
                    {
                        verts[i + vertOffset] = vertexArrs[dir][i] + posOffset;
                        tints[i + vertOffset] = tintIndexArrs[dir][i] >= 0 ? blockTint : DEFAULT_COLOR;
                    }
                    uvArrs[dir].CopyTo(txuvs, vertOffset);
                    uvAnimArrs[dir].CopyTo(uvans, vertOffset);
                    vertOffset += (uint)vertexArrs[dir].Length;
                }
            }

            buffer.vert = verts;
            buffer.txuv = txuvs;
            buffer.uvan = uvans;
            buffer.tint = tints;
        }

        public void BuildWithCollider(ref VertexBuffer buffer, ref float3[] colliderVerts,
                float3 posOffset, int cullFlags, float3 blockTint)
        {
            // Compute value if absent
            int extraVertCount  = sizeCache.ContainsKey(cullFlags) ? sizeCache[cullFlags] :
                    (sizeCache[cullFlags] = CalculateArraySize(cullFlags));
            int vVertexCount = buffer.vert.Length + extraVertCount;

            var verts = new float3[vVertexCount];
            var txuvs = new float3[vVertexCount];
            var uvans = new float4[vVertexCount];
            var tints = new float3[vVertexCount];

            buffer.vert.CopyTo(verts, 0);
            buffer.txuv.CopyTo(txuvs, 0);
            buffer.uvan.CopyTo(uvans, 0);
            buffer.tint.CopyTo(tints, 0);

            var cVerts = new float3[colliderVerts.Length + extraVertCount];
            colliderVerts.CopyTo(cVerts, 0);

            uint i, vertOffset = (uint)buffer.vert.Length;
            uint offsetAtStart = vertOffset;

            if (vertexArrs[CullDir.NONE].Length > 0)
            {
                for (i = 0U;i < vertexArrs[CullDir.NONE].Length;i++)
                {
                    verts[i + vertOffset] = vertexArrs[CullDir.NONE][i] + posOffset;
                    tints[i + vertOffset] = tintIndexArrs[CullDir.NONE][i] >= 0 ? blockTint : DEFAULT_COLOR;
                }
                uvArrs[CullDir.NONE].CopyTo(txuvs, vertOffset);
                uvAnimArrs[CullDir.NONE].CopyTo(uvans, vertOffset);
                vertOffset += (uint)vertexArrs[CullDir.NONE].Length;
            }

            for (int dirIdx = 0;dirIdx < 6;dirIdx++)
            {
                var dir = (CullDir) (dirIdx + 1);

                if ((cullFlags & (1 << dirIdx)) != 0 && vertexArrs[dir].Length > 0)
                {
                    for (i = 0U;i < vertexArrs[dir].Length;i++)
                    {
                        verts[i + vertOffset] = vertexArrs[dir][i] + posOffset;
                        tints[i + vertOffset] = tintIndexArrs[dir][i] >= 0 ? blockTint : DEFAULT_COLOR;
                    }
                    uvArrs[dir].CopyTo(txuvs, vertOffset);
                    uvAnimArrs[dir].CopyTo(uvans, vertOffset);
                    vertOffset += (uint)vertexArrs[dir].Length;
                }
            }

            // Copy from visual buffer to collider
            Array.Copy(verts, offsetAtStart, cVerts, colliderVerts.Length, extraVertCount);

            buffer.vert = verts;
            buffer.txuv = txuvs;
            buffer.uvan = uvans;
            buffer.tint = tints;

            colliderVerts = cVerts;
        }

        public void BuildCollider(ref float3[] colliderVerts, float3 posOffset, int cullFlags)
        {
            // Compute value if absent
            int vertexCount = colliderVerts.Length + ((sizeCache.ContainsKey(cullFlags)) ? sizeCache[cullFlags] :
                    (sizeCache[cullFlags] = CalculateArraySize(cullFlags)));

            var verts = new float3[vertexCount];

            colliderVerts.CopyTo(verts, 0);

            uint i, vertOffset = (uint)colliderVerts.Length;

            if (vertexArrs[CullDir.NONE].Length > 0)
            {
                for (i = 0U;i < vertexArrs[CullDir.NONE].Length;i++)
                    verts[i + vertOffset] = vertexArrs[CullDir.NONE][i] + posOffset;
                vertOffset += (uint)vertexArrs[CullDir.NONE].Length;
            }

            for (int dirIdx = 0;dirIdx < 6;dirIdx++)
            {
                var dir = (CullDir) (dirIdx + 1);

                if ((cullFlags & (1 << dirIdx)) != 0 && vertexArrs[dir].Length > 0)
                {
                    for (i = 0U;i < vertexArrs[dir].Length;i++)
                        verts[i + vertOffset] = vertexArrs[dir][i] + posOffset;
                    vertOffset += (uint)vertexArrs[dir].Length;
                }
            }

            colliderVerts = verts;
        }

    }
}
