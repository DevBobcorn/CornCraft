using Unity.Mathematics;

namespace CraftSharp.Resource
{
    public static class CelestiaPortalGeometry
    {
        public static void Build(VertexBuffer buffer, ref uint vertOffset, float3 posOffset, int cullFlags,
                float3 portalColor, float frameInterval, int frameCount, int framePerLine)
        {
            uint startOffset = vertOffset;

            // Unity                   Minecraft            Top Quad Vertices
            //  A +Z (East)             A +X (East)          v0---v1
            //  |                       |                    |     |
            //  *---> +X (South)        *---> +Z (South)     v2---v3

            var verts = buffer.vert;
            var txuvs = buffer.txuv;
            var uvans = buffer.uvan;
            var tints = buffer.tint;

            //var (fullUVs, anim) = ResourcePackManager.Instance.GetUVs(tex, FULL, 0);
            float oneU = 1F / framePerLine;
            float oneV = 1F / framePerLine;
            float u1 = 0F, u2 = oneU;
            float v1 = 0F, v2 = oneV;

            var fullUVs = new float3[]
            {
                new( u1, 1F - (oneV - v2), 0F),
                new( u2, 1F - (oneV - v2), 0F),
                new( u1, 1F - (oneV - v1), 0F),
                new( u2, 1F - (oneV - v1), 0F)
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
                vertOffset += 4;
            }

            for (uint i = startOffset; i < vertOffset; i++) // For each new vertex in the mesh
            {
                // Calculate vertex lighting
                tints[i] = new float4(portalColor, 0.5F);
                // Offset vertices
                verts[i] = verts[i] + posOffset;
            }
        }
    }
}