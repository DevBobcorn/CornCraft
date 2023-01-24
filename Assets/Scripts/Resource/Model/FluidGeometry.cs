using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

using MinecraftClient.Rendering;

namespace MinecraftClient.Resource
{
    public static class FluidGeometry
    {
        private static readonly Vector4 FULL = new(0, 0, 1, 1);

        // Add a subtle offset to sides of water to avoid z-fighting
        private const float O = 0.001F;
        private const float I = 0.999F;

        private const int VISUAL_PRECISION = 2;
        private const int VISUAL_PRECISION_SQR = VISUAL_PRECISION * VISUAL_PRECISION;

        public static void Build(ref VertexBuffer buffer, ResourceLocation liquid, int x, int y, int z, float2 chunkPos, int cullFlags, float3 fluidColor)
        {
            float h = (cullFlags & (1 << 0)) != 0 ? 0.875F : I;

            int vertOffset = buffer.vert.Length;
            int newLength = vertOffset + visualArraySizeMap[cullFlags];

            var verts = new float3[newLength];
            var txuvs = new float3[newLength];
            var tints = new float3[newLength];

            buffer.vert.CopyTo(verts, 0);
            buffer.txuv.CopyTo(txuvs, 0);
            buffer.tint.CopyTo(tints, 0);

            //for (int fti = vertOffset;fti < newLength;fti++)
            //    tints[fti] = fluidColor;

            for (int fti = vertOffset;fti < newLength;fti++)
                tints[fti] = new(1F, 0F, 0F);

            float3[] topUVs  = AtlasManager.GetUVs(liquid, FULL, 0);
            float3[] sideUVs = AtlasManager.GetUVs(liquid, new Vector4(0, 1 - h, 1, 1), 0);

            if ((cullFlags & (1 << 0)) != 0) // Up
            {
                var y1 = h + y;

                for (int p = 0;p < VISUAL_PRECISION;p++)
                {
                    var z0 = positionMap[p]     + z;
                    var z1 = positionMap[p + 1] + z;

                    for (int q = 0;q < VISUAL_PRECISION;q++)
                    {
                        var x0 = positionMap[q]     + x;
                        var x1 = positionMap[q + 1] + x;

                        verts[vertOffset]     = new(z0, y1, x1);
                        verts[vertOffset + 1] = new(z1, y1, x1);
                        verts[vertOffset + 2] = new(z0, y1, x0);
                        verts[vertOffset + 3] = new(z1, y1, x0);

                        txuvs[vertOffset]     = new(z0 + chunkPos.y, x1 + chunkPos.x, 0F);
                        txuvs[vertOffset + 1] = new(z1 + chunkPos.y, x1 + chunkPos.x, 0F);
                        txuvs[vertOffset + 2] = new(z0 + chunkPos.y, x0 + chunkPos.x, 0F);
                        txuvs[vertOffset + 3] = new(z1 + chunkPos.y, x0 + chunkPos.x, 0F);

                        vertOffset += 4;
                    }
                }
            }

            if ((cullFlags & (1 << 1)) != 0) // Down
            {
                var z0 = 0 + z;
                var z1 = 1 + z;
                var y0 = O + y;
                var x0 = 0 + x;
                var x1 = 1 + x;

                verts[vertOffset]     = new(z0, y0, x0);
                verts[vertOffset + 1] = new(z1, y0, x0);
                verts[vertOffset + 2] = new(z0, y0, x1);
                verts[vertOffset + 3] = new(z1, y0, x1);

                topUVs.CopyTo(txuvs, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 2)) != 0) // South
            {
                var z1 = I + z;

                var y0 = 0 + y;
                var y1 = h + y;

                for (int p = 0;p < VISUAL_PRECISION;p++)
                {
                    var x0 = positionMapAdjusted[p]     + x;
                    var x1 = positionMapAdjusted[p + 1] + x;
                    
                    verts[vertOffset]     = new(z1, y1, x0);
                    verts[vertOffset + 1] = new(z1, y1, x1);
                    verts[vertOffset + 2] = new(z1, y0, x0);
                    verts[vertOffset + 3] = new(z1, y0, x1);

                    sideUVs.CopyTo(txuvs, vertOffset);
                    vertOffset += 4;
                }
            }

            if ((cullFlags & (1 << 3)) != 0) // North
            {
                var z0 = O + z;

                var y0 = 0 + y;
                var y1 = h + y;

                for (int p = 0;p < VISUAL_PRECISION;p++)
                {
                    var x0 = positionMapAdjusted[p]     + x;
                    var x1 = positionMapAdjusted[p + 1] + x;

                    verts[vertOffset]     = new(z0, y1, x1);
                    verts[vertOffset + 1] = new(z0, y1, x0);
                    verts[vertOffset + 2] = new(z0, y0, x1);
                    verts[vertOffset + 3] = new(z0, y0, x0);

                    sideUVs.CopyTo(txuvs, vertOffset);
                    vertOffset += 4;
                }
            }

            if ((cullFlags & (1 << 4)) != 0) // East
            {
                var x1 = I + x;

                var y0 = 0 + y;
                var y1 = h + y;

                for (int p = 0;p < VISUAL_PRECISION;p++)
                {
                    var z0 = positionMapAdjusted[p]     + z;
                    var z1 = positionMapAdjusted[p + 1] + z;

                    verts[vertOffset]     = new(z1, y1, x1);
                    verts[vertOffset + 1] = new(z0, y1, x1);
                    verts[vertOffset + 2] = new(z1, y0, x1);
                    verts[vertOffset + 3] = new(z0, y0, x1);

                    sideUVs.CopyTo(txuvs, vertOffset);
                    vertOffset += 4;
                }
            }

            if ((cullFlags & (1 << 5)) != 0) // West
            {
                var x0 = O + x;

                var y0 = 0 + y;
                var y1 = h + y;

                for (int p = 0;p < VISUAL_PRECISION;p++)
                {
                    var z0 = positionMapAdjusted[p]     + z;
                    var z1 = positionMapAdjusted[p + 1] + z;

                    verts[vertOffset]     = new(z0, y1, x0);
                    verts[vertOffset + 1] = new(z1, y1, x0);
                    verts[vertOffset + 2] = new(z0, y0, x0);
                    verts[vertOffset + 3] = new(z1, y0, x0);

                    sideUVs.CopyTo(txuvs, vertOffset);
                    vertOffset += 4;
                }
            }

            buffer.vert = verts;
            buffer.txuv = txuvs;
            buffer.tint = tints;

        }

        public static void BuildCollider(ref float3[] colliderVerts, int x, int y, int z, int cullFlags)
        {
            float h = (cullFlags & (1 << 0)) != 0 ? 0.875F : I;

            int vertOffset = colliderVerts.Length;
            int newLength = vertOffset + colliderArraySizeMap[cullFlags];

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

        private static readonly Dictionary<int, int> visualArraySizeMap = CreateVisualArraySizeMap();
        private static readonly Dictionary<int, int> colliderArraySizeMap = CreateColliderArraySizeMap();

        private static Dictionary<int, int> CreateVisualArraySizeMap()
        {
            Dictionary<int, int> sizeMap = new();

            for (int cullFlags = 0b000000;cullFlags <= 0b111111;cullFlags++)
            {
                int vertexCount = 0;

                if ((cullFlags & (1 << 0)) != 0) // Top face presents
                    vertexCount += VISUAL_PRECISION_SQR * 4;
                
                if ((cullFlags & (1 << 1)) != 0) // Bottom face presents
                    vertexCount += 4; // Bottom face doesn't required sub-division

                for (int i = 2;i < 6;i++)
                {
                    if ((cullFlags & (1 << i)) != 0) // This face(side) presents
                        vertexCount += VISUAL_PRECISION * 4;
                }

                sizeMap.Add(cullFlags, vertexCount);

            }

            return sizeMap;
        }

        private static Dictionary<int, int> CreateColliderArraySizeMap()
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

        private static float[] positionMap = CreatePositionMap();
        private static float[] positionMapAdjusted = CreatePositionMapAdjusted();

        private static float[] CreatePositionMap()
        {
            var map = new float[VISUAL_PRECISION + 1];

            for (int p = 0;p < VISUAL_PRECISION;p++)
                map[p] = p / (float) VISUAL_PRECISION;
            
            map[VISUAL_PRECISION] = 1F;

            return map;
        }

        private static float[] CreatePositionMapAdjusted()
        {
            var map = new float[VISUAL_PRECISION + 1];

            map[0] = O;

            for (int p = 1;p < VISUAL_PRECISION;p++)
                map[p] = p / (float) VISUAL_PRECISION;

            map[VISUAL_PRECISION] = I;

            return map;
        }

    }

}