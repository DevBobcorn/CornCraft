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

        public static void Build(ref VertexBuffer buffer, ResourceLocation liquid, int x, int y, int z, int cullFlags)
        {
            float h = (cullFlags & (1 << 0)) != 0 ? 0.875F : I;

            int vertOffset = buffer.vert.Length;
            int newLength = vertOffset + arraySizeMap[cullFlags];

            var verts = new float3[newLength];
            var txuvs = new float2[newLength];
            var tints = new float3[newLength];

            buffer.vert.CopyTo(verts, 0);
            buffer.txuv.CopyTo(txuvs, 0);
            buffer.tint.CopyTo(tints, 0);

            float2[] topUVs  = AtlasManager.GetUVs(liquid, FULL, 0);
            float2[] sideUVs = AtlasManager.GetUVs(liquid, new Vector4(0, 1 - h, 1, 1), 0);

            if ((cullFlags & (1 << 0)) != 0) // Up
            {
                verts[vertOffset]     = new(0 + z, h + y, 1 + x); // 4 => 2
                verts[vertOffset + 1] = new(1 + z, h + y, 1 + x); // 5 => 3
                verts[vertOffset + 2] = new(0 + z, h + y, 0 + x); // 3 => 1
                verts[vertOffset + 3] = new(1 + z, h + y, 0 + x); // 2 => 0
                topUVs.CopyTo(txuvs, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 1)) != 0) // Down
            {
                verts[vertOffset]     = new(0 + z, O + y, 0 + x); // 0 => 0
                verts[vertOffset + 1] = new(1 + z, O + y, 0 + x); // 1 => 1
                verts[vertOffset + 2] = new(0 + z, O + y, 1 + x); // 7 => 3
                verts[vertOffset + 3] = new(1 + z, O + y, 1 + x); // 6 => 2
                topUVs.CopyTo(txuvs, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 2)) != 0) // South
            {
                verts[vertOffset]     = new(I + z, h + y, O + x); // 2 => 1
                verts[vertOffset + 1] = new(I + z, h + y, I + x); // 5 => 2
                verts[vertOffset + 2] = new(I + z, 0 + y, O + x); // 1 => 0
                verts[vertOffset + 3] = new(I + z, 0 + y, I + x); // 6 => 3
                sideUVs.CopyTo(txuvs, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 3)) != 0) // North
            {
                verts[vertOffset]     = new(O + z, h + y, I + x); // 4 => 2
                verts[vertOffset + 1] = new(O + z, h + y, O + x); // 3 => 1
                verts[vertOffset + 2] = new(O + z, 0 + y, I + x); // 7 => 3
                verts[vertOffset + 3] = new(O + z, 0 + y, O + x); // 0 => 0
                sideUVs.CopyTo(txuvs, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 4)) != 0) // East
            {
                verts[vertOffset]     = new(I + z, h + y, I + x); // 5 => 1
                verts[vertOffset + 1] = new(O + z, h + y, I + x); // 4 => 0
                verts[vertOffset + 2] = new(I + z, 0 + y, I + x); // 6 => 2
                verts[vertOffset + 3] = new(O + z, 0 + y, I + x); // 7 => 3
                sideUVs.CopyTo(txuvs, vertOffset);
                vertOffset += 4;
            }

            if ((cullFlags & (1 << 5)) != 0) // West
            {
                verts[vertOffset]     = new(O + z, h + y, O + x); // 3 => 3
                verts[vertOffset + 1] = new(I + z, h + y, O + x); // 2 => 2
                verts[vertOffset + 2] = new(O + z, 0 + y, O + x); // 0 => 0
                verts[vertOffset + 3] = new(I + z, 0 + y, O + x); // 1 => 1
                sideUVs.CopyTo(txuvs, vertOffset);
                // Not necessary vertOffset += 4;
            }

            buffer.vert = verts;
            buffer.txuv = txuvs;
            buffer.tint = tints;

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