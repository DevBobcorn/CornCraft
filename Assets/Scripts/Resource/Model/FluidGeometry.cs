using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

using MinecraftClient.Rendering;

namespace MinecraftClient.Resource
{
    public static class FluidGeometry
    {
        public static readonly ResourceLocation[] LiquidTextures = new ResourceLocation[]
        {
            new("block/water_still"),
            new("block/lava_still"),
            new("block/water_flow"),
            new("block/lava_flow")
        };

        private static readonly Vector4 FULL = new(0, 0, 1, 1);

        // Add a subtle offset to sides of water to avoid z-fighting
        private const float O = 0.001F;
        private const float I = 0.999F;

        private static float getAverageHeight(byte h1, byte h2, byte h3, byte h4)
        {
            int cnt = 0;
            if (h1 > 0) cnt++;
            if (h2 > 0) cnt++;
            if (h3 > 0) cnt++;
            if (h4 > 0) cnt++;

            return (h1 + h2 + h3 + h4) / 16F / cnt;
        }

        public static void Build(ref VertexBuffer buffer, ResourceLocation liquid, int x, int y, int z, byte[] heights, int cullFlags, float3 fluidColor)
        {
            // Unity                   Minecraft            Top Quad Vertices     Height References
            //  A +Z (East)             A +X (East)          v0---v1               NE---SE
            //  |                       |                    |     |               |     |
            //  *---> +X (South)        *---> +Z (South)     v2---v3               NW---SW
            
            var full = (cullFlags & (1 << 0)) == 0;

            var hne = full ? 1F : getAverageHeight(heights[0], heights[1], heights[3], heights[4]);
            var hse = full ? 1F : getAverageHeight(heights[1], heights[2], heights[4], heights[5]);
            var hnw = full ? 1F : getAverageHeight(heights[3], heights[4], heights[6], heights[7]);
            var hsw = full ? 1F : getAverageHeight(heights[4], heights[5], heights[7], heights[8]);

            int vertOffset = buffer.vert.Length;
            int newLength = vertOffset + arraySizeMap[cullFlags];

            var verts = new float3[newLength];
            var txuvs = new float3[newLength];
            var tints = new float3[newLength];

            buffer.vert.CopyTo(verts, 0);
            buffer.txuv.CopyTo(txuvs, 0);
            buffer.tint.CopyTo(tints, 0);

            for (int fti = vertOffset;fti < newLength;fti++)
                tints[fti] = fluidColor;

            float3[] fullUVs = CornApp.ActivePackManager.GetUVs(liquid, FULL, 0);
            //float3[] sideUVs = CornApp.ActivePackManager.GetUVs(liquid, new(0, 1 - h, 1, 1), 0);
            float3[] sideUVs = fullUVs;

            if ((cullFlags & (1 << 0)) != 0) // Up
            {
                verts[vertOffset]     = new(0 + z, hne + y, 1 + x); // 4 => 2
                verts[vertOffset + 1] = new(1 + z, hse + y, 1 + x); // 5 => 3
                verts[vertOffset + 2] = new(0 + z, hnw + y, 0 + x); // 3 => 1
                verts[vertOffset + 3] = new(1 + z, hsw + y, 0 + x); // 2 => 0
                fullUVs.CopyTo(txuvs, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 1)) != 0) // Down
            {
                verts[vertOffset]     = new(0 + z, O + y, 0 + x); // 0 => 0
                verts[vertOffset + 1] = new(1 + z, O + y, 0 + x); // 1 => 1
                verts[vertOffset + 2] = new(0 + z, O + y, 1 + x); // 7 => 3
                verts[vertOffset + 3] = new(1 + z, O + y, 1 + x); // 6 => 2
                fullUVs.CopyTo(txuvs, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 2)) != 0) // South
            {
                verts[vertOffset]     = new(I + z, hsw + y, O + x); // 2 => 1
                verts[vertOffset + 1] = new(I + z, hse + y, I + x); // 5 => 2
                verts[vertOffset + 2] = new(I + z,   0 + y, O + x); // 1 => 0
                verts[vertOffset + 3] = new(I + z,   0 + y, I + x); // 6 => 3
                sideUVs.CopyTo(txuvs, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 3)) != 0) // North
            {
                verts[vertOffset]     = new(O + z, hne + y, I + x); // 4 => 2
                verts[vertOffset + 1] = new(O + z, hnw + y, O + x); // 3 => 1
                verts[vertOffset + 2] = new(O + z,   0 + y, I + x); // 7 => 3
                verts[vertOffset + 3] = new(O + z,   0 + y, O + x); // 0 => 0
                sideUVs.CopyTo(txuvs, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 4)) != 0) // East
            {
                verts[vertOffset]     = new(I + z, hse + y, I + x); // 5 => 1
                verts[vertOffset + 1] = new(O + z, hne + y, I + x); // 4 => 0
                verts[vertOffset + 2] = new(I + z,   0 + y, I + x); // 6 => 2
                verts[vertOffset + 3] = new(O + z,   0 + y, I + x); // 7 => 3
                sideUVs.CopyTo(txuvs, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 5)) != 0) // West
            {
                verts[vertOffset]     = new(O + z, hnw + y, O + x); // 3 => 3
                verts[vertOffset + 1] = new(I + z, hsw + y, O + x); // 2 => 2
                verts[vertOffset + 2] = new(O + z,   0 + y, O + x); // 0 => 0
                verts[vertOffset + 3] = new(I + z,   0 + y, O + x); // 1 => 1
                sideUVs.CopyTo(txuvs, vertOffset);
                // Not necessary vertOffset += 4;
            }

            buffer.vert = verts;
            buffer.txuv = txuvs;
            buffer.tint = tints;

        }

        public static void BuildCollider(ref float3[] colliderVerts, int x, int y, int z, int cullFlags)
        {
            float h = (cullFlags & (1 << 0)) != 0 ? 0.875F : I;

            int vertOffset = colliderVerts.Length;
            int newLength = vertOffset + arraySizeMap[cullFlags];

            var verts = new float3[newLength];

            colliderVerts.CopyTo(verts, 0);

            if ((cullFlags & (1 << 0)) != 0) // Up
            {
                verts[vertOffset]     = new(0 + z, h + y, 1 + x); // 4 => 2
                verts[vertOffset + 1] = new(1 + z, h + y, 1 + x); // 5 => 3
                verts[vertOffset + 2] = new(0 + z, h + y, 0 + x); // 3 => 1
                verts[vertOffset + 3] = new(1 + z, h + y, 0 + x); // 2 => 0
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 1)) != 0) // Down
            {
                verts[vertOffset]     = new(0 + z, O + y, 0 + x); // 0 => 0
                verts[vertOffset + 1] = new(1 + z, O + y, 0 + x); // 1 => 1
                verts[vertOffset + 2] = new(0 + z, O + y, 1 + x); // 7 => 3
                verts[vertOffset + 3] = new(1 + z, O + y, 1 + x); // 6 => 2
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 2)) != 0) // South
            {
                verts[vertOffset]     = new(I + z, h + y, O + x); // 2 => 1
                verts[vertOffset + 1] = new(I + z, h + y, I + x); // 5 => 2
                verts[vertOffset + 2] = new(I + z, 0 + y, O + x); // 1 => 0
                verts[vertOffset + 3] = new(I + z, 0 + y, I + x); // 6 => 3
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 3)) != 0) // North
            {
                verts[vertOffset]     = new(O + z, h + y, I + x); // 4 => 2
                verts[vertOffset + 1] = new(O + z, h + y, O + x); // 3 => 1
                verts[vertOffset + 2] = new(O + z, 0 + y, I + x); // 7 => 3
                verts[vertOffset + 3] = new(O + z, 0 + y, O + x); // 0 => 0
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 4)) != 0) // East
            {
                verts[vertOffset]     = new(I + z, h + y, I + x); // 5 => 1
                verts[vertOffset + 1] = new(O + z, h + y, I + x); // 4 => 0
                verts[vertOffset + 2] = new(I + z, 0 + y, I + x); // 6 => 2
                verts[vertOffset + 3] = new(O + z, 0 + y, I + x); // 7 => 3
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 5)) != 0) // West
            {
                verts[vertOffset]     = new(O + z, h + y, O + x); // 3 => 3
                verts[vertOffset + 1] = new(I + z, h + y, O + x); // 2 => 2
                verts[vertOffset + 2] = new(O + z, 0 + y, O + x); // 0 => 0
                verts[vertOffset + 3] = new(I + z, 0 + y, O + x); // 1 => 1
                // Not necessary vertOffset += 4;
            }

            colliderVerts = verts;

        }

        private static readonly Dictionary<int, int> arraySizeMap = CreateArraySizeMap();

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