using UnityEngine;
using Unity.Mathematics;

namespace CraftSharp.Resource
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

        private static float GetAverageHeight(byte h1, byte h2, byte h3, byte h4)
        {
            int cnt = 0;
            if (h1 > 0) cnt++;
            if (h2 > 0) cnt++;
            if (h3 > 0) cnt++;
            if (h4 > 0) cnt++;

            return (h1 + h2 + h3 + h4) / 16F / cnt;
        }

        public static void Build(ref VertexBuffer buffer, float3 posOffset, ResourceLocation liquid,
                byte[] heights, int cullFlags, float[] blockLights, float3 fluidColor)
        {
            // Unity                   Minecraft            Top Quad Vertices     Height References
            //  A +Z (East)             A +X (East)          v0---v1               NE---SE
            //  |                       |                    |     |               |     |
            //  *---> +X (South)        *---> +Z (South)     v2---v3               NW---SW
            
            var full = (cullFlags & (1 << 0)) == 0;

            var hne = full ? 1F : GetAverageHeight(heights[0], heights[1], heights[3], heights[4]);
            var hse = full ? 1F : GetAverageHeight(heights[1], heights[2], heights[4], heights[5]);
            var hnw = full ? 1F : GetAverageHeight(heights[3], heights[4], heights[6], heights[7]);
            var hsw = full ? 1F : GetAverageHeight(heights[4], heights[5], heights[7], heights[8]);

            int vertOffset = buffer.vert.Length;
            int newLength = vertOffset + CubeGeometry.ArraySizeMap[cullFlags];

            var verts = new float3[newLength];
            var txuvs = new float3[newLength];
            var uvans = new float4[newLength];
            var tints = new float4[newLength];

            buffer.vert.CopyTo(verts, 0);
            buffer.txuv.CopyTo(txuvs, 0);
            buffer.uvan.CopyTo(uvans, 0);
            buffer.tint.CopyTo(tints, 0);

            var (fullUVs, anim) = ResourcePackManager.Instance.GetUVs(liquid, FULL, 0);
            float3[] sideUVs = fullUVs;

            float4[] uvAnims = { anim, anim, anim, anim };

            if ((cullFlags & (1 << 0)) != 0) // Up
            {
                verts[vertOffset]     = new(0, hne, 1); // 4 => 2
                verts[vertOffset + 1] = new(1, hse, 1); // 5 => 3
                verts[vertOffset + 2] = new(0, hnw, 0); // 3 => 1
                verts[vertOffset + 3] = new(1, hsw, 0); // 2 => 0
                fullUVs.CopyTo(txuvs, vertOffset);
                uvAnims.CopyTo(uvans, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 1)) != 0) // Down
            {
                verts[vertOffset]     = new(0, O, 0); // 0 => 0
                verts[vertOffset + 1] = new(1, O, 0); // 1 => 1
                verts[vertOffset + 2] = new(0, O, 1); // 7 => 3
                verts[vertOffset + 3] = new(1, O, 1); // 6 => 2
                fullUVs.CopyTo(txuvs, vertOffset);
                uvAnims.CopyTo(uvans, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 2)) != 0) // South
            {
                verts[vertOffset]     = new(I, hsw, O); // 2 => 1
                verts[vertOffset + 1] = new(I, hse, I); // 5 => 2
                verts[vertOffset + 2] = new(I,   0, O); // 1 => 0
                verts[vertOffset + 3] = new(I,   0, I); // 6 => 3
                sideUVs.CopyTo(txuvs, vertOffset);
                uvAnims.CopyTo(uvans, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 3)) != 0) // North
            {
                verts[vertOffset]     = new(O, hne, I); // 4 => 2
                verts[vertOffset + 1] = new(O, hnw, O); // 3 => 1
                verts[vertOffset + 2] = new(O,   0, I); // 7 => 3
                verts[vertOffset + 3] = new(O,   0, O); // 0 => 0
                sideUVs.CopyTo(txuvs, vertOffset);
                uvAnims.CopyTo(uvans, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 4)) != 0) // East
            {
                verts[vertOffset]     = new(I, hse, I); // 5 => 1
                verts[vertOffset + 1] = new(O, hne, I); // 4 => 0
                verts[vertOffset + 2] = new(I,   0, I); // 6 => 2
                verts[vertOffset + 3] = new(O,   0, I); // 7 => 3
                sideUVs.CopyTo(txuvs, vertOffset);
                uvAnims.CopyTo(uvans, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 5)) != 0) // West
            {
                verts[vertOffset]     = new(O, hnw, O); // 3 => 3
                verts[vertOffset + 1] = new(I, hsw, O); // 2 => 2
                verts[vertOffset + 2] = new(O,   0, O); // 0 => 0
                verts[vertOffset + 3] = new(I,   0, O); // 1 => 1
                sideUVs.CopyTo(txuvs, vertOffset);
                uvAnims.CopyTo(uvans, vertOffset);
                // Not necessary vertOffset += 4;
            }

            for (int i = buffer.vert.Length; i < verts.Length; i++) // For each new vertex in the mesh
            {
                // Calculate vertex lighting
                tints[i] = new float4(fluidColor, BlockGeometry.GetVertexLight(verts[i], blockLights));
                // Offset vertices
                verts[i] = verts[i] + posOffset;
            }

            buffer.vert = verts;
            buffer.txuv = txuvs;
            buffer.uvan = uvans;
            buffer.tint = tints;
        }

        public static void BuildCollider(ref float3[] colliderVerts, float3 posOffset, int cullFlags)
        {
            float h = (cullFlags & (1 << 0)) != 0 ? 0.875F : I;

            int vertOffset = colliderVerts.Length;
            int newLength = vertOffset + CubeGeometry.ArraySizeMap[cullFlags];

            var verts = new float3[newLength];

            colliderVerts.CopyTo(verts, 0);

            if ((cullFlags & (1 << 0)) != 0) // Up
            {
                verts[vertOffset]     = new(0, h, 1); // 4 => 2
                verts[vertOffset + 1] = new(1, h, 1); // 5 => 3
                verts[vertOffset + 2] = new(0, h, 0); // 3 => 1
                verts[vertOffset + 3] = new(1, h, 0); // 2 => 0
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 1)) != 0) // Down
            {
                verts[vertOffset]     = new(0, O, 0); // 0 => 0
                verts[vertOffset + 1] = new(1, O, 0); // 1 => 1
                verts[vertOffset + 2] = new(0, O, 1); // 7 => 3
                verts[vertOffset + 3] = new(1, O, 1); // 6 => 2
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 2)) != 0) // South
            {
                verts[vertOffset]     = new(I, h, O); // 2 => 1
                verts[vertOffset + 1] = new(I, h, I); // 5 => 2
                verts[vertOffset + 2] = new(I, 0, O); // 1 => 0
                verts[vertOffset + 3] = new(I, 0, I); // 6 => 3
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 3)) != 0) // North
            {
                verts[vertOffset]     = new(O, h, I); // 4 => 2
                verts[vertOffset + 1] = new(O, h, O); // 3 => 1
                verts[vertOffset + 2] = new(O, 0, I); // 7 => 3
                verts[vertOffset + 3] = new(O, 0, O); // 0 => 0
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 4)) != 0) // East
            {
                verts[vertOffset]     = new(I, h, I); // 5 => 1
                verts[vertOffset + 1] = new(O, h, I); // 4 => 0
                verts[vertOffset + 2] = new(I, 0, I); // 6 => 2
                verts[vertOffset + 3] = new(O, 0, I); // 7 => 3
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 5)) != 0) // West
            {
                verts[vertOffset]     = new(O, h, O); // 3 => 3
                verts[vertOffset + 1] = new(I, h, O); // 2 => 2
                verts[vertOffset + 2] = new(O, 0, O); // 0 => 0
                verts[vertOffset + 3] = new(I, 0, O); // 1 => 1
                // Not necessary vertOffset += 4;
            }

            for (int i = colliderVerts.Length; i < verts.Length; i++) // For each new vertex in the mesh
            {
                // Offset vertices
                verts[i] = verts[i] + posOffset;
            }

            colliderVerts = verts;
        }
    }
}