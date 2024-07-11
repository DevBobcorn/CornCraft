using UnityEngine;

namespace CraftSharp.Rendering
{
    public static class CameraExtensionsForChunkRender
    {
        private const int CHUNK_CENTER = Chunk.SIZE >> 1;

        public static bool ChunkInViewport(this Camera camera, int chunkX, int chunkY, int chunkZ, int offsetY)
        {
            var chunkPos = new Vector3(chunkZ * Chunk.SIZE + CHUNK_CENTER, chunkY *
                    Chunk.SIZE + CHUNK_CENTER + offsetY, chunkX * Chunk.SIZE + CHUNK_CENTER);
            
            return camera.PointInViewport(chunkPos);
        }

        public static bool PointInViewport(this Camera camera, Vector3 pos)
        {
            var vp = camera.WorldToViewportPoint(pos);

            if (vp.z < 0 || vp.x < 0 || vp.x > 1 || vp.y < 0 || vp.y > 1)
            {
                return false;
            }

            return true;
        }
    }
}