using System.Collections.Generic;
using UnityEngine;

using MinecraftClient.Mapping;

namespace MinecraftClient.Rendering
{
    public class ChunkRenderColumn : MonoBehaviour
    {
        public int ChunkX, ChunkZ;

        private readonly Dictionary<int, ChunkRender> chunks = new Dictionary<int, ChunkRender>();

        private ChunkRender CreateChunkRender(int chunkY)
        {
            // Create this chunk...
            GameObject chunkObj = new GameObject($"Chunk [{chunkY}]");
            chunkObj.layer = UnityEngine.LayerMask.NameToLayer(ChunkRenderManager.OBSTACLE_LAYER_NAME);
            ChunkRender newChunk = chunkObj.AddComponent<ChunkRender>();
            newChunk.ChunkX = this.ChunkX;
            newChunk.ChunkY = chunkY;
            newChunk.ChunkZ = this.ChunkZ;
            // Set its parent to this chunk column...
            chunkObj.transform.parent = this.transform;
            chunkObj.transform.localPosition = CoordConvert.MC2Unity(this.ChunkX * Chunk.SizeX, chunkY * Chunk.SizeY + World.GetDimension().minY, this.ChunkZ * Chunk.SizeZ);
            
            return newChunk;
        }

        public bool HasChunkRender(int chunkY) => chunks.ContainsKey(chunkY);

        public Dictionary<int, ChunkRender> GetChunkRenders() => chunks;

        public bool IsReady()
        {
            foreach (var chunk in chunks.Values)
            {
                if (chunk.State != ChunkBuildState.Ready)
                    return false;
            }

            return true;
        }

        public ChunkRender GetChunkRender(int chunkY, bool createIfEmpty)
        {
            if (chunks.ContainsKey(chunkY))
            {
                return chunks[chunkY];
            }
            else
            {
                // This chunk doesn't currently exist...
                if (chunkY >= 0 && chunkY * Chunk.SizeY < World.GetDimension().height)
                {
                    if (createIfEmpty)
                    {
                        ChunkRender newChunk = CreateChunkRender(chunkY);
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
            foreach (int i in chunks.Keys)
            {
                // Unload all chunks in this column, except empty chunks...
                if (chunks[i] is not null)
                {   // Before destroying the chunk object, do one last thing
                    var chunk = chunks[i];

                    if (chunks2Build.Contains(chunk))
                        chunks2Build.Remove(chunk);
                    
                    chunksBeingBuilt.Remove(chunk);
                    chunk.Unload();
                }

            }
            chunks.Clear();
            Destroy(this.gameObject);
        }

        public override string ToString() => $"[ChunkRenderColumn {ChunkX}, {ChunkZ}]";

    }
}
