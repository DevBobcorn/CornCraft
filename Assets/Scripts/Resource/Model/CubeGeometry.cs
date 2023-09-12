using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace CraftSharp.Resource
{
    public static class CubeGeometry
    {
        private static readonly Vector4 FULL = new(0, 0, 1, 1);

        public static void Build(ref VertexBuffer buffer, ResourceLocation tex,
                int x, int y, int z, int cullFlags, float4 vertColor)
        {
            // Unity                   Minecraft            Top Quad Vertices
            //  A +Z (East)             A +X (East)          v0---v1
            //  |                       |                    |     |
            //  *---> +X (South)        *---> +Z (South)     v2---v3

            int vertOffset = buffer.vert.Length;
            int newLength = vertOffset + ArraySizeMap[cullFlags];

            var verts = new float3[newLength];
            var txuvs = new float3[newLength];
            var uvans = new float4[newLength];
            var tints = new float4[newLength];

            buffer.vert.CopyTo(verts, 0);
            buffer.txuv.CopyTo(txuvs, 0);
            buffer.uvan.CopyTo(uvans, 0);
            buffer.tint.CopyTo(tints, 0);

            for (int fti = vertOffset;fti < newLength;fti++)
                tints[fti] = vertColor;

            var (fullUVs, anim) = ResourcePackManager.Instance.GetUVs(tex, FULL, 0);

            float4[] uvAnims = { anim, anim, anim, anim };

            if ((cullFlags & (1 << 0)) != 0) // Up
            {
                verts[vertOffset]     = new(0 + x, 1 + y, 1 + z); // 4 => 2
                verts[vertOffset + 1] = new(1 + x, 1 + y, 1 + z); // 5 => 3
                verts[vertOffset + 2] = new(0 + x, 1 + y, 0 + z); // 3 => 1
                verts[vertOffset + 3] = new(1 + x, 1 + y, 0 + z); // 2 => 0
                fullUVs.CopyTo(txuvs, vertOffset);
                uvAnims.CopyTo(uvans, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 1)) != 0) // Down
            {
                verts[vertOffset]     = new(0 + x, 0 + y, 0 + z); // 0 => 0
                verts[vertOffset + 1] = new(1 + x, 0 + y, 0 + z); // 1 => 1
                verts[vertOffset + 2] = new(0 + x, 0 + y, 1 + z); // 7 => 3
                verts[vertOffset + 3] = new(1 + x, 0 + y, 1 + z); // 6 => 2
                fullUVs.CopyTo(txuvs, vertOffset);
                uvAnims.CopyTo(uvans, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 2)) != 0) // South
            {
                verts[vertOffset]     = new(1 + x, 1 + y, 0 + z); // 2 => 1
                verts[vertOffset + 1] = new(1 + x, 1 + y, 1 + z); // 5 => 2
                verts[vertOffset + 2] = new(1 + x, 0 + y, 0 + z); // 1 => 0
                verts[vertOffset + 3] = new(1 + x, 0 + y, 1 + z); // 6 => 3
                fullUVs.CopyTo(txuvs, vertOffset);
                uvAnims.CopyTo(uvans, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 3)) != 0) // North
            {
                verts[vertOffset]     = new(0 + x, 1 + y, 1 + z); // 4 => 2
                verts[vertOffset + 1] = new(0 + x, 1 + y, 0 + z); // 3 => 1
                verts[vertOffset + 2] = new(0 + x, 0 + y, 1 + z); // 7 => 3
                verts[vertOffset + 3] = new(0 + x, 0 + y, 0 + z); // 0 => 0
                fullUVs.CopyTo(txuvs, vertOffset);
                uvAnims.CopyTo(uvans, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 4)) != 0) // East
            {
                verts[vertOffset]     = new(1 + x, 1 + y, 1 + z); // 5 => 1
                verts[vertOffset + 1] = new(0 + x, 1 + y, 1 + z); // 4 => 0
                verts[vertOffset + 2] = new(1 + x, 0 + y, 1 + z); // 6 => 2
                verts[vertOffset + 3] = new(0 + x, 0 + y, 1 + z); // 7 => 3
                fullUVs.CopyTo(txuvs, vertOffset);
                uvAnims.CopyTo(uvans, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 5)) != 0) // West
            {
                verts[vertOffset]     = new(0 + x, 1 + y, 0 + z); // 3 => 3
                verts[vertOffset + 1] = new(1 + x, 1 + y, 0 + z); // 2 => 2
                verts[vertOffset + 2] = new(0 + x, 0 + y, 0 + z); // 0 => 0
                verts[vertOffset + 3] = new(1 + x, 0 + y, 0 + z); // 1 => 1
                fullUVs.CopyTo(txuvs, vertOffset);
                uvAnims.CopyTo(uvans, vertOffset);
                // Not necessary vertOffset += 4;
            }

            buffer.vert = verts;
            buffer.txuv = txuvs;
            buffer.uvan = uvans;
            buffer.tint = tints;
        }

        public static readonly Dictionary<int, int> ArraySizeMap = CreateArraySizeMap();

        private static Dictionary<int, int> CreateArraySizeMap()
        {
            Dictionary<int, int> sizeMap = new();

            for (int cullFlags = 0b000000;cullFlags <= 0b111111;cullFlags++)
            {
                int vertexCount = 0;

                for (int i = 0;i < 6;i++)
                {
                    if ((cullFlags & (1 << i)) != 0) // This face(side) presents
                        vertexCount += 4;
                }

                sizeMap.Add(cullFlags, vertexCount);

            }

            return sizeMap;
        }
    }
}