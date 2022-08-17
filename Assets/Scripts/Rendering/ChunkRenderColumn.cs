using System.Collections.Generic;
using UnityEngine;

using MinecraftClient.Mapping;

namespace MinecraftClient.Rendering
{
    public class ChunkRenderColumn : MonoBehaviour
    {
        public int ChunkX, ChunkZ;

        public static bool IsValidChunkY(int chunkY)
        {
            // This can be changed in the future...
            return chunkY >= minChunkY && chunkY < minChunkY + ChunkColumn.ColumnSize;
        }

        public static int minChunkY = 0;

        private readonly Dictionary<int, ChunkRender> chunks = new Dictionary<int, ChunkRender>();

        private ChunkRender CreateChunk(int chunkY)
        {
            // Create this chunk...
            GameObject chunkObj = new GameObject("Chunk " + chunkY.ToString());
            ChunkRender newChunk = chunkObj.AddComponent<ChunkRender>();
            newChunk.ChunkX = this.ChunkX;
            newChunk.ChunkY = chunkY;
            newChunk.ChunkZ = this.ChunkZ;
            // Set its parent to this chunk column...
            chunkObj.transform.parent = this.transform;
            chunkObj.transform.localPosition = CoordConvert.MC2Unity(this.ChunkX * Chunk.SizeX, chunkY * Chunk.SizeY, this.ChunkZ * Chunk.SizeZ);
            newChunk.UpdateLayers(0);
            
            return newChunk;
        }

        public bool HasChunk(int chunkY)
        {
            return chunks.ContainsKey(chunkY);
        }

        public Dictionary<int, ChunkRender> GetChunks()
        {
            return chunks;
        }

        private ChunkRender GetChunkAt(int chunkY)
        {
            return chunks[chunkY];
        }

        public ChunkRender GetChunk(int chunkY, bool createIfEmpty)
        {
            if (chunks.ContainsKey(chunkY))
            {
                return chunks[chunkY];
            }
            else
            {
                // This chunk doesn't currently exist...
                if (IsValidChunkY(chunkY))
                {
                    if (createIfEmpty)
                    {
                        ChunkRender newChunk = CreateChunk(chunkY);
                        chunks.Add(chunkY, newChunk);
                        return newChunk;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    Debug.Log("Trying to get a chunk at invalid height: " + chunkY);
                    return null;
                }
            }
        }

        public void Unload(ref List<ChunkRender> chunksBeingBuilt, ref PriorityQueue<ChunkRender> chunks2Build)
        {
            // Unload this chunk column...
            foreach (var chunk in chunks.Values)
            {
                // Unload all chunks in this column, except empty chunks...
                if (chunk is not null)
                {   // Before destroying the chunk object, do one last thing
                    if (chunksBeingBuilt.Contains(chunk))
                    {
                        chunksBeingBuilt.Remove(chunk);
                        //Debug.Log("Removed " + chunk.ToString() + " from build list");
                    }

                    chunk.Unload();
                }

            }
            chunks.Clear();
            Destroy(this.gameObject);
        }

        public override string ToString()
        {
            return "[ChunkRenderColumn " + ChunkX + ", " + ChunkZ + "]";
        }

    }
}
