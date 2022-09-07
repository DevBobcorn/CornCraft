using System.Collections.Generic;
using Unity.Mathematics;

namespace MinecraftClient.Rendering
{
    public static class PlaceboGeometry
    {
        public static float3[] GetUpVertices(int blockX, int blockY, int blockZ)
        {
            return new float3[]
            {
                new float3(0 + blockZ, 1 + blockY, 1 + blockX), // 4 => 2
                new float3(1 + blockZ, 1 + blockY, 1 + blockX), // 5 => 3
                new float3(0 + blockZ, 1 + blockY, 0 + blockX), // 3 => 1
                new float3(1 + blockZ, 1 + blockY, 0 + blockX), // 2 => 0
            };
        }

        public static float3[] GetDownVertices(int blockX, int blockY, int blockZ)
        {
            return new float3[]
            {
                new float3(0 + blockZ, 0 + blockY, 0 + blockX), // 0 => 0
                new float3(1 + blockZ, 0 + blockY, 0 + blockX), // 1 => 1
                new float3(0 + blockZ, 0 + blockY, 1 + blockX), // 7 => 3
                new float3(1 + blockZ, 0 + blockY, 1 + blockX), // 6 => 2
            };
        }

        public static float3[] GetNorthVertices(int blockX, int blockY, int blockZ)
        {
            return new float3[]
            {
                new float3(0 + blockZ, 1 + blockY, 1 + blockX), // 4 => 2
                new float3(0 + blockZ, 1 + blockY, 0 + blockX), // 3 => 1
                new float3(0 + blockZ, 0 + blockY, 1 + blockX), // 7 => 3
                new float3(0 + blockZ, 0 + blockY, 0 + blockX), // 0 => 0
            };
        }

        public static float3[] GetSouthVertices(int blockX, int blockY, int blockZ)
        {
            return new float3[]
            {
                new float3(1 + blockZ, 1 + blockY, 0 + blockX), // 2 => 1
                new float3(1 + blockZ, 1 + blockY, 1 + blockX), // 5 => 2
                new float3(1 + blockZ, 0 + blockY, 0 + blockX), // 1 => 0
                new float3(1 + blockZ, 0 + blockY, 1 + blockX), // 6 => 3
            };
        }

        public static float3[] GetWestVertices(int blockX, int blockY, int blockZ)
        {
            return new float3[]
            {
                new float3(0 + blockZ, 1 + blockY, 0 + blockX), // 3 => 3
                new float3(1 + blockZ, 1 + blockY, 0 + blockX), // 2 => 2
                new float3(0 + blockZ, 0 + blockY, 0 + blockX), // 0 => 0
                new float3(1 + blockZ, 0 + blockY, 0 + blockX), // 1 => 1
            };
        }

        public static float3[] GetEastVertices(int blockX, int blockY, int blockZ)
        {
            return new float3[]
            {
                new float3(1 + blockZ, 1 + blockY, 1 + blockX), // 5 => 1
                new float3(0 + blockZ, 1 + blockY, 1 + blockX), // 4 => 0
                new float3(1 + blockZ, 0 + blockY, 1 + blockX), // 6 => 2
                new float3(0 + blockZ, 0 + blockY, 1 + blockX), // 7 => 3
            };
        }

        private const int TexturesInALine = 32;
        private const float One = 1.0F / TexturesInALine; // Size of a single block texture

        public static float2[] GetUVs(RenderType type)
        {
            int offset = type switch
            {
                RenderType.SOLID         => 0,
                RenderType.CUTOUT        => 1,
                RenderType.CUTOUT_MIPPED => 2,
                RenderType.TRANSLUCENT   => 3,

                _                        => 0
            };

            float blockU = (offset % TexturesInALine) / (float)TexturesInALine;
            float blockV = (offset / TexturesInALine) / (float)TexturesInALine;
            float2 o = new float2(blockU, blockV);

            return new float2[]{ new float2(0F, 0F) + o, new float2(One, 0F) + o, new float2(0F, One) + o, new float2(One, One) + o };

        }

        public static void Build(ref VertexBuffer buffer, RenderType type, bool uv, int x, int y, int z, int cullFlags)
        {
            if ((cullFlags & (1 << 0)) != 0) // Up
            {
                buffer.vert = ArrayUtil.GetConcated(buffer.vert, PlaceboGeometry.GetUpVertices(x, y, z));
                //buffer.face = ArrayUtil.GetConcated(buffer.face, GetQuad(buffer.offset));
                if (uv) buffer.uv = ArrayUtil.GetConcated(buffer.uv, GetUVs(type));
                buffer.offset += 4;
            }
            if ((cullFlags & (1 << 1)) != 0) // Down
            {
                buffer.vert = ArrayUtil.GetConcated(buffer.vert, PlaceboGeometry.GetDownVertices(x, y, z));
                //buffer.face = ArrayUtil.GetConcated(buffer.face, GetQuad(buffer.offset));
                if (uv) buffer.uv = ArrayUtil.GetConcated(buffer.uv, GetUVs(type));
                buffer.offset += 4;
            }
            if ((cullFlags & (1 << 2)) != 0) // South
            {
                buffer.vert = ArrayUtil.GetConcated(buffer.vert, PlaceboGeometry.GetSouthVertices(x, y, z));
                //buffer.face = ArrayUtil.GetConcated(buffer.face, GetQuad(buffer.offset));
                if (uv) buffer.uv = ArrayUtil.GetConcated(buffer.uv, GetUVs(type));
                buffer.offset += 4;
            }
            if ((cullFlags & (1 << 3)) != 0) // North
            {
                buffer.vert = ArrayUtil.GetConcated(buffer.vert, PlaceboGeometry.GetNorthVertices(x, y, z));
                //buffer.face = ArrayUtil.GetConcated(buffer.face, GetQuad(buffer.offset));
                if (uv) buffer.uv = ArrayUtil.GetConcated(buffer.uv, GetUVs(type));
                buffer.offset += 4;
            }
            if ((cullFlags & (1 << 4)) != 0) // East
            {
                buffer.vert = ArrayUtil.GetConcated(buffer.vert, PlaceboGeometry.GetEastVertices(x, y, z));
                //buffer.face = ArrayUtil.GetConcated(buffer.face, GetQuad(buffer.offset));
                if (uv) buffer.uv = ArrayUtil.GetConcated(buffer.uv, GetUVs(type));
                buffer.offset += 4;
            }
            if ((cullFlags & (1 << 5)) != 0) // West
            {
                buffer.vert = ArrayUtil.GetConcated(buffer.vert, PlaceboGeometry.GetWestVertices(x, y, z));
                //buffer.face = ArrayUtil.GetConcated(buffer.face, GetQuad(buffer.offset));
                if (uv) buffer.uv = ArrayUtil.GetConcated(buffer.uv, GetUVs(type));
                buffer.offset += 4;
            }
        }

        private static bool prepareFlag = PrepareGeometryData();

        private static Dictionary<int, float3[]> vertexArrayTable = new();

        private static bool PrepareGeometryData()
        {
            for (int cullFlags = 0b000000;cullFlags <= 0b111111;cullFlags++)
            {
                int vertexCount = 0;
                int triIdxCount = 0;

                for (int i = 0;i < 6;i++)
                {
                    if ((cullFlags & (1 << i)) != 0) // This face(side) presents
                    {
                        vertexCount += 4;
                        triIdxCount += 6;
                    }
                }

                // Prepare vertex & triIndex arrays
                var verts  = new float3[vertexCount];
                var tris   = new uint[triIdxCount];

            }

            return true;

        }

    }

}