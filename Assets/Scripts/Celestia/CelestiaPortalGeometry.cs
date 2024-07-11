using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace CraftSharp.Resource
{
    public static class CelestiaPortalGeometry
    {
        private static readonly Vector4 FULL = new(0, 0, 1, 1);

        public static void Build(ref VertexBuffer buffer, float3 posOffset, int cullFlags,
                float3 portalColor, float frameInterval, int frameCount, int framePerLine)
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

            //var (fullUVs, anim) = ResourcePackManager.Instance.GetUVs(tex, FULL, 0);
            float oneU = 1F / framePerLine;
            float oneV = 1F / framePerLine;
            float u1 = 0F, u2 = oneU;
            float v1 = 0F, v2 = oneV;

            var fullUVs = new float3[]
            {
                new float3( u1, 1F - (oneV - v2), 0F),
                new float3( u2, 1F - (oneV - v2), 0F),
                new float3( u1, 1F - (oneV - v1), 0F),
                new float3( u2, 1F - (oneV - v1), 0F)
            };

            // frame count, frame interval, frame UV size, frame per row
            float4 anim = new(frameCount, frameInterval, oneU, framePerLine);

            float4[] uvAnims = { anim, anim, anim, anim };

            if ((cullFlags & (1 << 0)) != 0) // Up
            {
                verts[vertOffset]     = new(0, 1, 1); // 4 => 2
                verts[vertOffset + 1] = new(1, 1, 1); // 5 => 3
                verts[vertOffset + 2] = new(0, 1, 0); // 3 => 1
                verts[vertOffset + 3] = new(1, 1, 0); // 2 => 0
                fullUVs.CopyTo(txuvs, vertOffset);
                uvAnims.CopyTo(uvans, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 1)) != 0) // Down
            {
                verts[vertOffset]     = new(0, 0, 0); // 0 => 0
                verts[vertOffset + 1] = new(1, 0, 0); // 1 => 1
                verts[vertOffset + 2] = new(0, 0, 1); // 7 => 3
                verts[vertOffset + 3] = new(1, 0, 1); // 6 => 2
                fullUVs.CopyTo(txuvs, vertOffset);
                uvAnims.CopyTo(uvans, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 2)) != 0) // South
            {
                verts[vertOffset]     = new(1, 1, 0); // 2 => 1
                verts[vertOffset + 1] = new(1, 1, 1); // 5 => 2
                verts[vertOffset + 2] = new(1, 0, 0); // 1 => 0
                verts[vertOffset + 3] = new(1, 0, 1); // 6 => 3
                fullUVs.CopyTo(txuvs, vertOffset);
                uvAnims.CopyTo(uvans, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 3)) != 0) // North
            {
                verts[vertOffset]     = new(0, 1, 1); // 4 => 2
                verts[vertOffset + 1] = new(0, 1, 0); // 3 => 1
                verts[vertOffset + 2] = new(0, 0, 1); // 7 => 3
                verts[vertOffset + 3] = new(0, 0, 0); // 0 => 0
                fullUVs.CopyTo(txuvs, vertOffset);
                uvAnims.CopyTo(uvans, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 4)) != 0) // East
            {
                verts[vertOffset]     = new(1, 1, 1); // 5 => 1
                verts[vertOffset + 1] = new(0, 1, 1); // 4 => 0
                verts[vertOffset + 2] = new(1, 0, 1); // 6 => 2
                verts[vertOffset + 3] = new(0, 0, 1); // 7 => 3
                fullUVs.CopyTo(txuvs, vertOffset);
                uvAnims.CopyTo(uvans, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 5)) != 0) // West
            {
                verts[vertOffset]     = new(0, 1, 0); // 3 => 3
                verts[vertOffset + 1] = new(1, 1, 0); // 2 => 2
                verts[vertOffset + 2] = new(0, 0, 0); // 0 => 0
                verts[vertOffset + 3] = new(1, 0, 0); // 1 => 1
                fullUVs.CopyTo(txuvs, vertOffset);
                uvAnims.CopyTo(uvans, vertOffset);
                // Not necessary vertOffset += 4;
            }

            for (int i = buffer.vert.Length; i < verts.Length; i++) // For each new vertex in the mesh
            {
                // Calculate vertex lighting
                tints[i] = new float4(portalColor, 0.5F);
                // Offset vertices
                verts[i] = verts[i] + posOffset;
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