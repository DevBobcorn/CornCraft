using System.Linq;
using UnityEngine;

using MinecraftClient.Resource;

namespace MinecraftClient.Rendering
{
    public class ChunkFluidGeometry
    {
        public static Vector3[] GetUpVertices(int blockX, int blockY, int blockZ)
        {
            Vector3[] arr = {
                new Vector3(0 + blockZ, 1 + blockY, 1 + blockX), // 4 => 2
                new Vector3(1 + blockZ, 1 + blockY, 1 + blockX), // 5 => 3
                new Vector3(0 + blockZ, 1 + blockY, 0 + blockX), // 3 => 1
                new Vector3(1 + blockZ, 1 + blockY, 0 + blockX), // 2 => 0
            };
            return arr;
        }

        public static Vector3[] GetDownVertices(int blockX, int blockY, int blockZ)
        {
            Vector3[] arr = {
                new Vector3(0 + blockZ, 0 + blockY, 0 + blockX), // 0 => 0
                new Vector3(1 + blockZ, 0 + blockY, 0 + blockX), // 1 => 1
                new Vector3(0 + blockZ, 0 + blockY, 1 + blockX), // 7 => 3
                new Vector3(1 + blockZ, 0 + blockY, 1 + blockX), // 6 => 2
            };
            return arr;
        }

        public static Vector3[] GetNorthVertices(int blockX, int blockY, int blockZ)
        {
            Vector3[] arr = {
                new Vector3(0 + blockZ, 1 + blockY, 1 + blockX), // 4 => 2
                new Vector3(0 + blockZ, 1 + blockY, 0 + blockX), // 3 => 1
                new Vector3(0 + blockZ, 0 + blockY, 1 + blockX), // 7 => 3
                new Vector3(0 + blockZ, 0 + blockY, 0 + blockX), // 0 => 0
            };
            return arr;
        }

        public static Vector3[] GetSouthVertices(int blockX, int blockY, int blockZ)
        {
            Vector3[] arr = {
                new Vector3(1 + blockZ, 1 + blockY, 0 + blockX), // 2 => 1
                new Vector3(1 + blockZ, 1 + blockY, 1 + blockX), // 5 => 2
                new Vector3(1 + blockZ, 0 + blockY, 0 + blockX), // 1 => 0
                new Vector3(1 + blockZ, 0 + blockY, 1 + blockX), // 6 => 3
            };
            return arr;
        }

        public static Vector3[] GetWestVertices(int blockX, int blockY, int blockZ)
        {
            Vector3[] arr = {
                new Vector3(0 + blockZ, 1 + blockY, 0 + blockX), // 3 => 3
                new Vector3(1 + blockZ, 1 + blockY, 0 + blockX), // 2 => 2
                new Vector3(0 + blockZ, 0 + blockY, 0 + blockX), // 0 => 0
                new Vector3(1 + blockZ, 0 + blockY, 0 + blockX), // 1 => 1
            };
            return arr;
        }

        public static Vector3[] GetEastVertices(int blockX, int blockY, int blockZ)
        {
            Vector3[] arr = {
                new Vector3(1 + blockZ, 1 + blockY, 1 + blockX), // 5 => 1
                new Vector3(0 + blockZ, 1 + blockY, 1 + blockX), // 4 => 0
                new Vector3(1 + blockZ, 0 + blockY, 1 + blockX), // 6 => 2
                new Vector3(0 + blockZ, 0 + blockY, 1 + blockX), // 7 => 3
            };
            return arr;
        }

        public static int[] GetQuad(int offset)
        {
            int[] arr = {
                0 + offset, 3 + offset, 2 + offset, // MC: +X <=> Unity: +Z
                0 + offset, 1 + offset, 3 + offset
            };
            return arr;
        }

        private static Vector4 FULL = new Vector4(0, 0, 1, 1);

        public static void Build(ref MeshBuffer buffer, ResourceLocation tex, bool uv, int x, int y, int z, int cullFlags)
        {
            if ((cullFlags & (1 << 0)) != 0) // Up
            {
                buffer.vert = buffer.vert.Concat(PlaceboGeometry.GetUpVertices(x, y, z)).ToArray();
                buffer.face = buffer.face.Concat(GetQuad(buffer.offset)).ToArray();
                if (uv) buffer.uv = buffer.uv.Concat(BlockTextureManager.GetUVs(tex, FULL, 0)).ToArray();
                buffer.offset += 4;
            }
            if ((cullFlags & (1 << 1)) != 0) // Down
            {
                buffer.vert = buffer.vert.Concat(PlaceboGeometry.GetDownVertices(x, y, z)).ToArray();
                buffer.face = buffer.face.Concat(GetQuad(buffer.offset)).ToArray();
                if (uv) buffer.uv = buffer.uv.Concat(BlockTextureManager.GetUVs(tex, FULL, 0)).ToArray();
                buffer.offset += 4;
            }
            if ((cullFlags & (1 << 2)) != 0) // South
            {
                buffer.vert = buffer.vert.Concat(PlaceboGeometry.GetSouthVertices(x, y, z)).ToArray();
                buffer.face = buffer.face.Concat(GetQuad(buffer.offset)).ToArray();
                if (uv) buffer.uv = buffer.uv.Concat(BlockTextureManager.GetUVs(tex, FULL, 0)).ToArray();
                buffer.offset += 4;
            }
            if ((cullFlags & (1 << 3)) != 0) // North
            {
                buffer.vert = buffer.vert.Concat(PlaceboGeometry.GetNorthVertices(x, y, z)).ToArray();
                buffer.face = buffer.face.Concat(GetQuad(buffer.offset)).ToArray();
                if (uv) buffer.uv = buffer.uv.Concat(BlockTextureManager.GetUVs(tex, FULL, 0)).ToArray();
                buffer.offset += 4;
            }
            if ((cullFlags & (1 << 4)) != 0) // East
            {
                buffer.vert = buffer.vert.Concat(PlaceboGeometry.GetEastVertices(x, y, z)).ToArray();
                buffer.face = buffer.face.Concat(GetQuad(buffer.offset)).ToArray();
                if (uv) buffer.uv = buffer.uv.Concat(BlockTextureManager.GetUVs(tex, FULL, 0)).ToArray();
                buffer.offset += 4;
            }
            if ((cullFlags & (1 << 5)) != 0) // West
            {
                buffer.vert = buffer.vert.Concat(PlaceboGeometry.GetWestVertices(x, y, z)).ToArray();
                buffer.face = buffer.face.Concat(GetQuad(buffer.offset)).ToArray();
                if (uv) buffer.uv = buffer.uv.Concat(BlockTextureManager.GetUVs(tex, FULL, 0)).ToArray();
                buffer.offset += 4;
            }
        }
    
    }

}