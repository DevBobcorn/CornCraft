using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Unity.Mathematics;

namespace CraftSharp.Resource
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

        public static float GetVertexLight(float3 vertPosInBlock, float[] lights)
        {
            /*
            // Original sampling: Accepts light values of neighbors and self
            float neighbor =
                    math.lerp(lights[2], lights[3], vertPosInBlock.x) + // North / South
                    math.lerp(lights[1], lights[0], vertPosInBlock.y) + // Down / Up
                    math.lerp(lights[4], lights[5], vertPosInBlock.z)   // East / West
                    / 3F;
            
            return Mathf.Max(neighbor, lights[6]);
            */

            // Enhanced sampling: Accepts light values of 8 corners
            float x0z0 = math.lerp(lights[0], lights[4], vertPosInBlock.y);
            float x1z0 = math.lerp(lights[1], lights[5], vertPosInBlock.y);
            float x0z1 = math.lerp(lights[2], lights[6], vertPosInBlock.y);
            float x1z1 = math.lerp(lights[3], lights[7], vertPosInBlock.y);

            var interpolated = math.lerp(
                    math.lerp(x0z0, x0z1, vertPosInBlock.x),
                    math.lerp(x1z0, x1z1, vertPosInBlock.x),
                    vertPosInBlock.z
            );

            return interpolated * interpolated;
        }

        private float GetCornerAO(bool side1, bool corner, bool side2)
        {
            //return 1F - (side1 ? 0.33F : 0F) - (corner ? 0.33F : 0F) - (side2 ? 0.33F : 0F);
            return 1F - (side1 ? 0.25F : 0F) - (corner ? 0.25F : 0F) - (side2 ? 0.25F : 0F);
        }

        // tl tm tr
        // ml mm mr
        // bl bm br
        private float[] GetCornersAO(bool tl, bool tm, bool tr, bool ml, bool mr, bool bl, bool bm, bool br)
        {
            return new float[]
            {
                GetCornerAO(ml, tl, tm), // tl
                GetCornerAO(tm, tr, mr), // tr
                GetCornerAO(bm, bl, ml), // bl
                GetCornerAO(mr, br, bm), // br
            };
        }

        private static readonly float[] NO_AO = new float[] { 1F, 1F, 1F, 1F };

        public float[] GetDirCornersAO(CullDir dir, bool[] isOpaque)
        {
            switch (dir)
            {
                case CullDir.DOWN:
                    //  6  7  8    A unity x+ (South)
                    //  3  4  5    |
                    //  0  1  2    o--> unity z+ (East)
                    return GetCornersAO(isOpaque[ 6], isOpaque[ 7], isOpaque[ 8], isOpaque[ 3], isOpaque[ 5], isOpaque[ 0], isOpaque[ 1], isOpaque[ 2]);
                case CullDir.UP:
                    // 20 23 26    A unity z+ (East)
                    // 19 22 25    |
                    // 18 21 24    o--> unity x+ (South)
                    return GetCornersAO(isOpaque[20], isOpaque[23], isOpaque[26], isOpaque[19], isOpaque[25], isOpaque[18], isOpaque[21], isOpaque[24]);
                case CullDir.SOUTH:
                    // 24 25 26    A unity y+ (Up)
                    // 15 16 17    |
                    //  6  7  8    o--> unity z+ (East)
                    return GetCornersAO(isOpaque[24], isOpaque[25], isOpaque[26], isOpaque[15], isOpaque[17], isOpaque[ 6], isOpaque[ 7], isOpaque[ 8]);
                case CullDir.NORTH:
                    //  2 11 20    A unity y+ (Up)
                    //  1 10 19    |
                    //  0  9 18    o--> unity z+ (East)
                    return GetCornersAO(isOpaque[ 2], isOpaque[11], isOpaque[20], isOpaque[ 1], isOpaque[19], isOpaque[ 0], isOpaque[ 9], isOpaque[18]);
                case CullDir.EAST:
                    //  8 17 26    A unity x+ (South)
                    //  5 14 23    |
                    //  2 11 20    o--> unity y+ (Up)
                    return GetCornersAO(isOpaque[ 8], isOpaque[17], isOpaque[26], isOpaque[ 5], isOpaque[23], isOpaque[ 2], isOpaque[11], isOpaque[20]);
                case CullDir.WEST:
                    // 18 21 24    A unity y+ (Up)
                    //  9 12 15    |
                    //  0  3  6    o--> unity x+ (South)
                    return GetCornersAO(isOpaque[18], isOpaque[21], isOpaque[24], isOpaque[ 9], isOpaque[15], isOpaque[ 0], isOpaque[ 3], isOpaque[ 6]);

                default:
                    return NO_AO;
            }
        }

        public float SampleVertexAO(CullDir dir, float[] cornersAO, float3 vertPosInBlock)
        {
            // AO Coord: 0 1
            //           2 3
            float2 AOCoord = dir switch
            {
                CullDir.DOWN   => vertPosInBlock.zx,
                CullDir.UP     => vertPosInBlock.xz,
                CullDir.SOUTH  => vertPosInBlock.zy,
                CullDir.NORTH  => vertPosInBlock.yz,
                CullDir.EAST   => vertPosInBlock.yx,
                CullDir.WEST   => vertPosInBlock.xy,

                _              => float2.zero
            };

            return math.lerp(math.lerp(cornersAO[2], cornersAO[0], AOCoord.y), math.lerp(cornersAO[3], cornersAO[1], AOCoord.y), AOCoord.x);
        }

        public void Build(ref VertexBuffer buffer, float3 posOffset, int cullFlags,
                bool[] ao, float[] blockLights, float3 blockColor)
        {
            // Compute value if absent
            int vertexCount = buffer.vert.Length + (sizeCache.ContainsKey(cullFlags) ? sizeCache[cullFlags] :
                    (sizeCache[cullFlags] = CalculateArraySize(cullFlags)));

            var verts = new float3[vertexCount];
            var txuvs = new float3[vertexCount];
            var uvans = new float4[vertexCount];
            var tints = new float4[vertexCount];

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
                    float vertLight = GetVertexLight(vertexArrs[CullDir.NONE][i], blockLights);
                    float3 vertColor = (tintIndexArrs[CullDir.NONE][i] >= 0 ? blockColor : DEFAULT_COLOR);
                    tints[i + vertOffset] = new float4(vertColor, vertLight);
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
                    var cornersAO = GetDirCornersAO(dir, ao);

                    for (i = 0U;i < vertexArrs[dir].Length;i++)
                    {
                        verts[i + vertOffset] = vertexArrs[dir][i] + posOffset;
                        float vertLight = GetVertexLight(vertexArrs[dir][i], blockLights);
                        float3 vertColor = (tintIndexArrs[dir][i] >= 0 ? blockColor : DEFAULT_COLOR)
                                * SampleVertexAO(dir, cornersAO, vertexArrs[dir][i]);
                        tints[i + vertOffset] = new float4(vertColor, vertLight);
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

        public void BuildWithCollider(ref VertexBuffer buffer, ref float3[] colliderVerts, float3 posOffset,
                int cullFlags, bool[] ao, float[] blockLights, float3 blockColor)
        {
            // Compute value if absent
            int extraVertCount  = sizeCache.ContainsKey(cullFlags) ? sizeCache[cullFlags] :
                    (sizeCache[cullFlags] = CalculateArraySize(cullFlags));
            int vVertexCount = buffer.vert.Length + extraVertCount;

            var verts = new float3[vVertexCount];
            var txuvs = new float3[vVertexCount];
            var uvans = new float4[vVertexCount];
            var tints = new float4[vVertexCount];

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
                    float vertLight = GetVertexLight(vertexArrs[CullDir.NONE][i], blockLights);
                    float3 vertColor = (tintIndexArrs[CullDir.NONE][i] >= 0 ? blockColor : DEFAULT_COLOR);
                    tints[i + vertOffset] = new float4(vertColor, vertLight);
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
                    var cornersAO = GetDirCornersAO(dir, ao);

                    for (i = 0U;i < vertexArrs[dir].Length;i++)
                    {
                        verts[i + vertOffset] = vertexArrs[dir][i] + posOffset;
                        float vertLight = GetVertexLight(vertexArrs[dir][i], blockLights);
                        float3 vertColor = (tintIndexArrs[dir][i] >= 0 ? blockColor : DEFAULT_COLOR)
                                * SampleVertexAO(dir, cornersAO, vertexArrs[dir][i]);
                        tints[i + vertOffset] = new float4(vertColor, vertLight);
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
