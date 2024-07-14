using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Profiling;

namespace CraftSharp.Rendering
{
    public class ChunkRenderColumn : MonoBehaviour
    {
        public int ChunkX, ChunkZ;

        private readonly Dictionary<int, ChunkRender> chunks = new();

        private ChunkRender CreateChunkRender(int chunkYIndex, IObjectPool<ChunkRender> pool)
        {
            // Get one from pool
            var chunk = pool.Get();

            chunk.ChunkX = this.ChunkX;
            chunk.ChunkZ = this.ChunkZ;
            chunk.ChunkYIndex = chunkYIndex;

            var chunkObj = chunk.gameObject;
            chunkObj.name = $"Chunk [{chunkYIndex}]";

            // Set its parent to this chunk column...
            chunkObj.transform.parent = this.transform;
            chunkObj.transform.localPosition = CoordConvert.MC2Unity(this.ChunkX * Chunk.SIZE, chunkYIndex * Chunk.SIZE + World.GetDimension().minY, this.ChunkZ * Chunk.SIZE);

            chunkObj.hideFlags = HideFlags.HideAndDontSave;

            return chunk;
        }

        public bool HasChunkRender(int chunkYIndex) => chunks.ContainsKey(chunkYIndex);

        public Dictionary<int, ChunkRender> GetChunkRenders() => chunks;

        public ChunkRender GetChunkRender(int chunkYIndex)
        {
            if (chunks.ContainsKey(chunkYIndex))
            {
                return chunks[chunkYIndex];
            }
            else
            {
                // This chunk doesn't currently exist...
                if (chunkYIndex >= 0 && chunkYIndex * Chunk.SIZE < World.GetDimension().height)
                {
                    return null;
                }
                else
                {
                    //Debug.Log("Trying to get a chunk at invalid height: " + chunkY);
                    return null;
                }
            }
        }

        public ChunkRender GetOrCreateChunkRender(int chunkYIndex, IObjectPool<ChunkRender> pool)
        {
            if (chunks.ContainsKey(chunkYIndex))
            {
                return chunks[chunkYIndex];
            }
            else
            {
                // This chunk doesn't currently exist...
                if (chunkYIndex >= 0 && chunkYIndex * Chunk.SIZE < World.GetDimension().height)
                {
                    Profiler.BeginSample("Create chunk render object");
                    ChunkRender newChunk = CreateChunkRender(chunkYIndex, pool);
                    chunks.Add(chunkYIndex, newChunk);
                    Profiler.EndSample();
                    return newChunk;
                }
                else
                {
                    //Debug.Log("Trying to get a chunk at invalid height: " + chunkY);
                    return null;
                }
            }
        }

        /// <summary>
        /// Unload a chunk render, accessible on unity thread only
        /// </summary>
        /// <param name="chunksBeingBuilt"></param>
        /// <param name="chunks2Build"></param>
        public void Unload(ref List<ChunkRender> chunksBeingBuilt, ref PriorityQueue<ChunkRender> chunks2Build, IObjectPool<ChunkRender> pool)
        {
            // Unload this chunk column...
            foreach (int i in chunks.Keys)
            {
                var chunk = chunks[i];

                // Unload all chunks in this column, except empty chunks...
                if (chunk != null)
                {   // Before destroying the chunk object, do one last thing
                    

                    if (chunks2Build.Contains(chunk))
                        chunks2Build.Remove(chunk);
                    
                    chunksBeingBuilt.Remove(chunk);
                    chunk.Unload();
                    // Return this chunk render to pool
                    pool.Release(chunk);
                }
            }
            chunks.Clear();

            if (this != null)
            {
                Destroy(this.gameObject);
            }
        }

        public override string ToString() => $"[ChunkRenderColumn {ChunkX}, {ChunkZ}]";
    }
}
