using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Profiling;
using Unity.Mathematics;

namespace CraftSharp.Rendering
{
    public class ChunkRenderColumn : MonoBehaviour
    {
        /// <summary>
        /// Chunk x coordinate in Minecraft space
        /// </summary>
        public int ChunkX { get; private set; }

        /// <summary>
        /// Chunk z coordinate in Minecraft space
        /// </summary>
        public int ChunkZ { get; private set; }

        private int2 columnPos;
        public int2 ColumnPos
        {
            get => columnPos;
            set
            {
                columnPos = value;
                ChunkX = columnPos.x;
                ChunkZ = columnPos.y;
            }
        }

        private readonly Dictionary<int, ChunkRender> chunks = new();

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
                if (chunkYIndex >= 0 && chunkYIndex * Chunk.SIZE < World.GetDimensionType().height)
                {
                    return null;
                }
                else
                {
                    //Debug.Log("Trying to get a ChunkRender at invalid height: " + chunkY);
                    return null;
                }
            }
        }

        /// <summary>
        /// Get existing ChunkRender or create one from object pool
        /// </summary>
        public ChunkRender GetOrCreateChunkRender(int chunkYIndex, IObjectPool<ChunkRender> pool)
        {
            if (chunks.ContainsKey(chunkYIndex))
            {
                return chunks[chunkYIndex];
            }

            // This ChunkRender doesn't currently exist...
            if (chunkYIndex >= 0 && chunkYIndex * Chunk.SIZE < World.GetDimensionType().height)
            {
                Profiler.BeginSample("Create chunk render object");

                // Get one from pool
                var chunk = pool.Get();
                chunk.ChunkPos = new int3(this.ChunkX, chunkYIndex, this.ChunkZ);

                var chunkObj = chunk.gameObject;
                chunkObj.name = $"Chunk [{chunkYIndex}]";

                // Set its parent to this ChunkRenderColumn...
                chunkObj.transform.parent = this.transform;
                chunkObj.transform.localPosition = new(0F, chunkYIndex * Chunk.SIZE + World.GetDimensionType().minY, 0F);

                //chunkObj.hideFlags = HideFlags.HideAndDontSave;
                
                chunks.Add(chunkYIndex, chunk);

                Profiler.EndSample();

                return chunk;
            }
            else
            {
                //Debug.Log("Trying to get a ChunkRender at invalid height: " + chunkY);
                return null;
            }
        }

        /// <summary>
        /// Unload a chunk render, accessible on unity thread only
        /// </summary>
        /// <param name="chunksBeingBuilt"></param>
        /// <param name="chunks2Build"></param>
        public void Unload(HashSet<ChunkRender> chunksBeingBuilt, HashSet<ChunkRender> chunks2BuildAsSet, PriorityQueue<ChunkRender> chunks2Build, IObjectPool<ChunkRender> pool)
        {
            // Unload this chunk column...
            foreach (int i in chunks.Keys)
            {
                var chunk = chunks[i];

                // Unload all chunks in this column, except empty chunks...
                if (chunk != null)
                {
                    // Before releasing the chunk object, do one last thing
                    if (chunks2BuildAsSet.Contains(chunk))
                    {
                        chunks2Build.Remove(chunk);
                        chunks2BuildAsSet.Remove(chunk);
                    }
                    
                    chunksBeingBuilt.Remove(chunk);
                    chunk.Unload();

                    // Return this ChunkRender to pool
                    pool.Release(chunk);
                }
            }
            chunks.Clear();
        }

        public override string ToString() => $"[ChunkRenderColumn {ChunkX}, {ChunkZ}]";
    }
}
