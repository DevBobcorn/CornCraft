using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Profiling;
using Unity.Mathematics;

using CraftSharp.Event;
using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    public class ChunkRenderManager : MonoBehaviour, IChunkRenderManager
    {
        public const string MOVEMENT_LAYER_NAME = "Movement";
        public const string SOLID_LAYER_NAME = "Solid";
        public const string LIQUID_SURFACE_LAYER_NAME = "LiquidSurface";

        [SerializeField] private Transform blockEntityParent;

        #region GameObject Prefabs for each block entity type
        [SerializeField] private GameObject defaultPrefab;
        #endregion

        private readonly Dictionary<ResourceLocation, GameObject> blockEntityPrefabs = new();
        private GameObject GetPrefabForType(ResourceLocation type) => blockEntityPrefabs.GetValueOrDefault(type, defaultPrefab);

        /// <summary>
        /// World data, accessible on both Unity thread and mesh builder threads
        /// </summary>
        private readonly World world = new();

        private readonly LightCalculator lightCalc = new();

        /// <summary>
        /// All block entity renders in the current world
        /// </summary>
        private readonly Dictionary<BlockLoc, BlockEntityRender> blockEntityRenders = new();

        // Squared distance range of a block entity to be considered as "near" the player
        private const float NEARBY_THERESHOLD_INNER =  81F; //  9 *  9
        private const float NEARBY_THERESHOLD_OUTER = 100F; // 10 * 10

        /// <summary>
        /// A dictionary storing block entities near the player
        /// Block entity location => Square distance to player
        /// </summary>
        private readonly Dictionary<BlockLoc, float> nearbyBlockEntities = new();

        /// <summary>
        /// Dictionary for temporarily storing received lighting data
        /// Accessible on network thread
        /// </summary>
        private readonly ConcurrentDictionary<int2, Queue<byte>> lightingCache = new();

        /// <summary>
        /// Get lighting cache for this chunk render manager
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<int2, Queue<byte>> GetLightingCache() => lightingCache;

        /// <summary>
        /// Unity thread access only
        /// </summary>
        private readonly Dictionary<int2, ChunkRenderColumn> renderColumns = new();

        /// <summary>
        /// Unity thread access only
        /// </summary>
        private PriorityQueue<ChunkRender> chunkRendersToBeBuilt = new();
        private List<ChunkRender> chunkRendersBeingBuilt = new();

        private BaseCornClient client;
        private ChunkRenderBuilder builder;

        // Terrain collider for movement
        private MeshCollider movementCollider, liquidCollider;

        public void SetClient(BaseCornClient client) => this.client = client;

        private static int dataBuildTimeSum = 0, vertexBuildTimeSum = 0;
        private static readonly Queue<int> dataBuildTimeRecord = new(Enumerable.Repeat(0, 200));
        private static readonly Queue<int> vertexBuildTimeRecord = new(Enumerable.Repeat(0, 200));

        public string GetDebugInfo()
        {
            return $"Queued Chunks: {chunkRendersToBeBuilt.Count}\nBuilding Chunks: {chunkRendersBeingBuilt.Count}\n- Data Build Time Avg: {dataBuildTimeSum / 200F:0.00} ms\n- Vert Build Time Avg: {vertexBuildTimeSum / 200F:0.00} ms\nBlock Entity Count: {blockEntityRenders.Count}";
        }

        #region Chunk render access
        private ObjectPool<ChunkRender> chunkRenderPool;

        private static ChunkRender CreateNewChunkRender()
        {
            // Create a new chunk render object...
            var chunkObj = new GameObject($"Chunk [Pooled]")
            {
                layer = LayerMask.NameToLayer(ChunkRenderManager.SOLID_LAYER_NAME)
            };
            ChunkRender newChunk = chunkObj.AddComponent<ChunkRender>();
            
            return newChunk;
        }

        private static void GetChunkRender(ChunkRender chunk)
        {
            //chunk.gameObject.hideFlags = HideFlags.None;
        }

        private static void ReleaseChunkRender(ChunkRender chunk)
        {
            // Unparent and hide this chunk render
            chunk.transform.parent = null;
            var chunkObj = chunk.gameObject;
            chunkObj.name = $"Chunk [Pooled]";
            chunkObj.SetActive(false);
        }

        private ChunkRenderColumn CreateChunkRenderColumn(int chunkX, int chunkZ)
        {
            // Create this chunk column...
            var columnObj = new GameObject($"Column [{chunkX}, {chunkZ}]");
            ChunkRenderColumn newColumn = columnObj.AddComponent<ChunkRenderColumn>();
            newColumn.ChunkX = chunkX;
            newColumn.ChunkZ = chunkZ;
            // Set its parent to this world...
            columnObj.transform.parent = this.transform;
            columnObj.transform.localPosition = Vector3.zero;

            return newColumn;
        }

        private ChunkRenderColumn GetChunkRenderColumn(int chunkX, int chunkZ, bool createIfEmpty)
        {
            int2 chunkCoord = new(chunkX, chunkZ);
            if (renderColumns.ContainsKey(chunkCoord))
                return renderColumns[chunkCoord];

            if (createIfEmpty)
            {
                Profiler.BeginSample("Create chunk render object");
                ChunkRenderColumn newColumn = CreateChunkRenderColumn(chunkX, chunkZ);
                Profiler.EndSample();
                renderColumns.Add(chunkCoord, newColumn);
                return newColumn;
            }

            return null;
        }

        public ChunkRender GetChunkRender(int chunkX, int chunkY, int chunkZ)
        {
            var column = GetChunkRenderColumn(chunkX, chunkZ, false);

            if (column == null)
                return null;

            return column.GetChunkRender(chunkY);
        }

        public bool IsChunkRenderReady(int chunkX, int chunkY, int chunkZ)
        {
            var column = GetChunkRenderColumn(chunkX, chunkZ, false);

            if (column == null)
                return false;
            
            var chunk = column.GetChunkRender(chunkY);
            
            // Empty chunks (air) are null, those chunks are always ready
            return chunk == null || chunk.State == ChunkBuildState.Ready;
        }
        #endregion

        #region Chunk data access

        /// <summary>
        /// Store a chunk, invoked from network thread
        /// </summary>
        public void StoreChunk(int chunkX, int chunkY, int chunkZ, int chunkColumnSize, Chunk chunk)
        {
            world.StoreChunk(chunkX, chunkY, chunkZ, chunkColumnSize, chunk);
        }

        /// <summary>
        /// Create an empty chunk column, invoked from network thread
        /// </summary>
        public void CreateEmptyChunkColumn(int chunkX, int chunkZ, int chunkColumnSize)
        {
            world.CreateEmptyChunkColumn(chunkX, chunkZ, chunkColumnSize);
        }

        private void RecalculateBlockLightAt(BlockLoc center)
        {
            // Set up light calculator
            lightCalc.SetUpRecalculateArea(world, center);
            // Recalculate block light
            var result = lightCalc.RecalculateLightValues();

            // Apply the result to chunk light data
            int boxMinX = lightCalc.BoxMinX;
            int boxMinY = lightCalc.BoxMinY;
            int boxMinZ = lightCalc.BoxMinZ;

            for (int x = LightCalculator.MAX_SPREAD_DIST; x < LightCalculator.LIGHT_CALC_BOX_SIZE - LightCalculator.MAX_SPREAD_DIST; x++)
                for (int y = LightCalculator.MAX_SPREAD_DIST; y < LightCalculator.LIGHT_CALC_BOX_SIZE - LightCalculator.MAX_SPREAD_DIST; y++)
                    for (int z = LightCalculator.MAX_SPREAD_DIST; z < LightCalculator.LIGHT_CALC_BOX_SIZE - LightCalculator.MAX_SPREAD_DIST; z++)
                    {
                        if (LightCalculator.ManhattanDistToCenter(x, y, z) >= LightCalculator.MAX_SPREAD_DIST)
                        {
                            // These light might be recalculated, but they are not
                            // within the affect range of this block change
                            continue;
                        }

                        world.SetBlockLight(new(boxMinX + x, boxMinY + y, boxMinZ + z), result[x, y, z]);
                    }
            
            // Mark chunk renders as dirty
            int worldMinY = World.GetDimension().minY;
            int worldMaxY = World.GetDimension().maxY;

            var minAffectedBlockLoc = new BlockLoc(
                lightCalc.BoxMinX + LightCalculator.MAX_SPREAD_DIST,
                math.max(worldMinY, lightCalc.BoxMinY + LightCalculator.MAX_SPREAD_DIST),
                lightCalc.BoxMinZ + LightCalculator.MAX_SPREAD_DIST
            );

            var maxAffectedBlockLoc = new BlockLoc(
                lightCalc.BoxMaxX - LightCalculator.MAX_SPREAD_DIST,
                math.min(worldMaxY, lightCalc.BoxMaxY - LightCalculator.MAX_SPREAD_DIST),
                lightCalc.BoxMaxZ - LightCalculator.MAX_SPREAD_DIST
            );

            Loom.QueueOnMainThread(() =>
            {
                for (int chunkX = minAffectedBlockLoc.GetChunkX(); chunkX <= maxAffectedBlockLoc.GetChunkX(); chunkX++)
                    for (int chunkZ = minAffectedBlockLoc.GetChunkZ(); chunkZ <= maxAffectedBlockLoc.GetChunkZ(); chunkZ++)
                        for (int chunkY = minAffectedBlockLoc.GetChunkY(worldMinY);
                                chunkY <= maxAffectedBlockLoc.GetChunkY(worldMinY); chunkY++)
                        {
                            MarkDirtyBecauseOfLightUpdate(chunkX, chunkY, chunkZ);
                        }
            });
        }

        /// <summary>
        /// Set block at the specified location, invoked from network thread
        /// </summary>
        /// <param name="blockLoc">Location to set block to</param>
        /// <param name="block">Block to set</param>
        public void SetBlock(BlockLoc blockLoc, Block block)
        {
            var column = GetChunkColumn(blockLoc);
            if (column != null)
            {
                // Update chunk data
                var chunk = column.GetChunk(blockLoc);
                if (chunk == null)
                    column[blockLoc.GetChunkY(column.MinimumY)] = chunk = new Chunk();
                chunk[blockLoc.GetChunkBlockX(), blockLoc.GetChunkBlockY(), blockLoc.GetChunkBlockZ()] = block;
                // Update ambient occulsion and light data cache
                bool shouldRecalcLight = column.UpdateCachedBlockData(blockLoc, block.State);
                if (shouldRecalcLight)
                {
                    RecalculateBlockLightAt(blockLoc);
                }
            }

            Loom.QueueOnMainThread(() => {
                // Check if the location has a block entity and remove it
                RemoveBlockEntityRender(blockLoc);
                // Auto-create block entity if present
                if (BlockEntityPalette.INSTANCE.GetBlockEntityForBlock(block.BlockId, out BlockEntityType blockEntityType))
                {
                    AddBlockEntityRender(blockLoc, blockEntityType, null);
                }
                // Mark the chunk dirty and queue for mesh rebuild
                MarkDirtyAt(blockLoc);
            });
        }

        /// <summary>
        /// Get a chunk column from the world by a block location
        /// </summary>
        public ChunkColumn GetChunkColumn(BlockLoc blockLoc)
        {
            return world.GetChunkColumn(blockLoc);
        }

        /// <summary>
        /// Get a chunk column from the world
        /// </summary>
        public ChunkColumn GetChunkColumn(int chunkX, int chunkZ)
        {
            return world[chunkX, chunkZ];
        }

        /// <summary>
        /// Get block at the specified location
        /// </summary>
        /// <param name="blockLoc">Location to retrieve block from</param>
        /// <returns>Block at specified location or Air if the location is not loaded</returns>
        public Block GetBlock(BlockLoc blockLoc)
        {
            return world.GetBlock(blockLoc);
        }

        /// <summary>
        /// Get biome at the specified location
        /// </summary>
        public Biome GetBiome(BlockLoc blockLoc)
        {
            return world.GetBiome(blockLoc);
        }

        /// <summary>
        /// Get block light at the specified location
        /// </summary>
        public byte GetBlockLight(BlockLoc blockLoc)
        {
            return world.GetBlockLight(blockLoc);
        }

        /// <summary>
        /// Check if the block at specified location causes ambient occlusion
        /// </summary>
        public bool GetAmbientOcclusion(BlockLoc blockLoc)
        {
            return world.GetAmbientOcclusion(blockLoc);
        }

        /// <summary>
        /// Check if the given location is occupied by a block entity
        /// </summary>
        /// <param name="blockLoc">Location of the block entity</param>
        /// <returns></returns>
        public bool HasBlockEntityRender(BlockLoc blockLoc)
        {
            return blockEntityRenders.ContainsKey(blockLoc);
        }

        /// <summary>
        /// Get block entity render at a given location
        /// </summary>
        /// <param name="blockLoc">Location of the block entity</param>
        /// <returns>Block entity render at the location. Null if not present</returns>
        public BlockEntityRender GetBlockEntityRender(BlockLoc blockLoc)
        {
            if (blockEntityRenders.ContainsKey(blockLoc))
                return blockEntityRenders[blockLoc];
            
            return null;
        }

        /// <summary>
        /// Add a new block entity render to the world
        /// </summary>
        /// <param name="tags">Pass in null if auto-creating this block entity</param>
        public void AddBlockEntityRender(BlockLoc blockLoc, BlockEntityType blockEntityType, Dictionary<string, object> tags = null)
        {
            // If the location is occupied by a block entity already
            if (blockEntityRenders.ContainsKey(blockLoc))
            {
                var prevBlockEntity = blockEntityRenders[blockLoc];
                if (prevBlockEntity.Type == blockEntityType) // Auto-created, keep it but replace data tags
                {
                    if (tags == null) // Auto-creating a block entity while it is already created
                    {
                        // Do nothing
                    }
                    else // Update block entity data tags
                    {
                        prevBlockEntity.BlockEntityTags = tags;
                        // TODO: Update render with updated data tags
                    }
                    return;
                }
                else // Previously another type of block entity, remove it
                {
                    RemoveBlockEntityRender(blockLoc);
                }
            }

            GameObject blockEntityPrefab = GetPrefabForType(blockEntityType.BlockEntityId);

            if (blockEntityPrefab != null)
            {
                var blockEntityObj = GameObject.Instantiate(blockEntityPrefab);
                var blockEntityRender = blockEntityObj!.GetComponent<BlockEntityRender>();

                blockEntityRenders.Add(blockLoc, blockEntityRender);

                blockEntityObj.name = $"[{blockLoc}] {blockEntityType}";
                blockEntityObj.transform.parent = blockEntityParent;
                blockEntityObj.transform.position = CoordConvert.MC2Unity(blockLoc.ToCenterLocation());

                // Initialize the entity
                blockEntityRender.Initialize(blockLoc, blockEntityType, tags ?? new());
            }
        }

        /// <summary>
        /// Remove a block entity render from the world
        /// </summary>
        public void RemoveBlockEntityRender(BlockLoc blockLoc)
        {
            if (blockEntityRenders.ContainsKey(blockLoc))
            {
                blockEntityRenders[blockLoc].Unload();
                blockEntityRenders.Remove(blockLoc);

                if (nearbyBlockEntities.ContainsKey(blockLoc))
                {
                    nearbyBlockEntities.Remove(blockLoc);
                }
            }
        }
        #endregion

        #region Chunk updating
        private const int CHUNK_CENTER = Chunk.SIZE >> 1;
        private const int MASK_CYCLE_LENGTH = 64; // Must be power of 2
        private int updateTargetMask = 0;

        private void UpdateBuildPriority(Location currentBlockLoc, ChunkRender chunk, int offsetY)
        {   // Get this chunk's build priority based on its current distance to the player,
            // a smaller value means a higher priority...
            chunk.Priority = (int)(
                    new Location(chunk.ChunkX * Chunk.SIZE + CHUNK_CENTER, chunk.ChunkYIndex * Chunk.SIZE + CHUNK_CENTER + offsetY,
                            chunk.ChunkZ * Chunk.SIZE + CHUNK_CENTER).DistanceTo(currentBlockLoc) / 16);
        }

        /// <summary>
        /// Use a mask to specify a subset of chunks to update
        /// </summary>
        /// <param name="mask">A value from 0x00 to 0x1F (0 to 31)</param>
        private void UpdateChunkRendersListAdd(int mask)
        {
            var playerLoc = client!.GetLocation();
            var blockLoc = playerLoc.GetBlockLoc();
            ChunkRenderColumn columnRender;

            int viewDist = CornGlobal.MCSettings.RenderDistance;
            int viewDistSqr = viewDist * viewDist;

            int chunkColumnSize = (World.GetDimension().height + Chunk.SIZE - 1) / Chunk.SIZE; // Round up
            int offsetY = World.GetDimension().minY;

            var renderCamera = client.CameraController.RenderCamera;

            // Add nearby chunks
            for (int cx = -viewDist;cx <= viewDist;cx++)
                for (int cz = -viewDist;cz <= viewDist;cz++)
                {
                    if (((cx + cz) & (MASK_CYCLE_LENGTH - 1)) != mask || cx * cx + cz * cz >= viewDistSqr) continue;
                    int chunkX = blockLoc.GetChunkX() + cx, chunkZ = blockLoc.GetChunkZ() + cz;
                    
                    if (world.IsChunkColumnLoaded(chunkX, chunkZ))
                    {
                        var column = GetChunkRenderColumn(chunkX, chunkZ, false);
                        if (column == null)
                        {   // Chunks data is ready, but chunk render column is not
                            //int chunkMask = world[chunkX, chunkZ]!.ChunkMask;
                            // Create it and add the whole column to render list...
                            columnRender = GetChunkRenderColumn(chunkX, chunkZ, true)!;
                            for (int chunkY = 0;chunkY < chunkColumnSize;chunkY++)
                            {
                                // Create chunk renders and queue them...
                                if (!world[chunkX, chunkZ]!.ChunkIsEmpty(chunkY))
                                {
                                    // This chunk is not empty and needs to be added and queued
                                    var chunk = columnRender.GetOrCreateChunkRender(chunkY, chunkRenderPool);
                                    /*
                                    if (renderCamera.ChunkInViewport(chunkX, chunk.ChunkY, chunkZ, offsetY))
                                    {
                                        
                                    }
                                    else
                                    {
                                        chunk.State = ChunkBuildState.Delayed;
                                    }
                                    */
                                    UpdateBuildPriority(playerLoc, chunk, offsetY);
                                    QueueChunkRenderBuild(chunk);
                                }
                            }
                        }
                        else
                        {
                            foreach (var chunk in column.GetChunkRenders().Values)
                            {
                                if (chunk.State == ChunkBuildState.Delayed && renderCamera.ChunkInViewport(chunkX, chunk.ChunkYIndex, chunkZ, offsetY))
                                {
                                    // Queue delayed or cancelled chunk builds...
                                    UpdateBuildPriority(playerLoc, chunk, offsetY);
                                    QueueChunkRenderBuild(chunk);
                                }
                            }
                        }
                    }
                }

        }

        private void UpdateChunkRendersListRemove(int mask)
        {
            // Add nearby chunks
            var blockLoc   = client!.GetLocation().GetBlockLoc();
            int unloadDist = Mathf.RoundToInt(CornGlobal.MCSettings.RenderDistance * 2F);

            var chunkCoords = renderColumns.Keys.ToArray();

            for (int i = mask; i < chunkCoords.Length; i += MASK_CYCLE_LENGTH)
            {
                var chunkCoord = chunkCoords[i];
                if (Mathf.Abs(blockLoc.GetChunkX() - chunkCoord.x) > unloadDist || Mathf.Abs(blockLoc.GetChunkZ() - chunkCoord.y) > unloadDist)
                {
                    renderColumns[chunkCoord].Unload(ref chunkRendersBeingBuilt, ref chunkRendersToBeBuilt, chunkRenderPool);
                    renderColumns.Remove(chunkCoord);
                }
            }
        }

        private void QueueChunkRenderBuild(ChunkRender chunkRender)
        {
            if (!chunkRendersToBeBuilt.Contains(chunkRender))
            {
                chunkRendersToBeBuilt.Enqueue(chunkRender);
                chunkRender.State = ChunkBuildState.Pending;
            }
        }

        private void QueueChunkRenderBuildIfNotEmpty(ChunkRender chunkRender)
        {
            if (chunkRender != null) // Not empty(air) chunk
                QueueChunkRenderBuild(chunkRender);
        }

        public void BuildChunkRender(ChunkRender chunkRender)
        {
            int chunkX = chunkRender.ChunkX, chunkZ = chunkRender.ChunkZ, chunkYIndex = chunkRender.ChunkYIndex;
            var chunkColumnData = GetChunkColumn(chunkX, chunkZ);

            if (chunkColumnData == null) // Chunk column data unloaded, cancel
            {
                int2 chunkCoord = new(chunkRender.ChunkX, chunkRender.ChunkZ);
                if (renderColumns.ContainsKey(chunkCoord))
                {
                    renderColumns[chunkCoord].Unload(ref chunkRendersBeingBuilt, ref chunkRendersToBeBuilt, chunkRenderPool);
                    renderColumns.Remove(chunkCoord, out _);
                }
                chunkRender.State = ChunkBuildState.Cancelled;
                return;
            }

            var chunkData = chunkColumnData[chunkYIndex];

            if (chunkData == null) // Chunk not available or is empty(air chunk), cancel
            {
                chunkRender.State = ChunkBuildState.Cancelled;
                return;
            }

            if (!(  world.IsChunkColumnLoaded(chunkX, chunkZ - 1) && // ZNeg neighbor present
                    world.IsChunkColumnLoaded(chunkX, chunkZ + 1) && // ZPos neighbor present
                    world.IsChunkColumnLoaded(chunkX - 1, chunkZ) && // XNeg neighbor present
                    world.IsChunkColumnLoaded(chunkX + 1, chunkZ) && // XPos neighbor present
                    world.IsChunkColumnLoaded(chunkX - 1, chunkZ - 1) &&
                    world.IsChunkColumnLoaded(chunkX - 1, chunkZ + 1) &&
                    world.IsChunkColumnLoaded(chunkX + 1, chunkZ - 1) &&
                    world.IsChunkColumnLoaded(chunkX + 1, chunkZ + 1) ))
            {
                chunkRender.State = ChunkBuildState.Delayed;
                return; // Not all neighbor data ready, delay it
            }

            chunkRendersBeingBuilt.Add(chunkRender);
            chunkRender.State = ChunkBuildState.Building;

            chunkRender.TokenSource = new();

            Task.Run(() =>
            {
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                // Build chunk data
                var buildData = world.GetChunkBuildData(chunkX, chunkZ, chunkYIndex);

                var time = (int) sw.ElapsedMilliseconds;

                lock (dataBuildTimeRecord)
                {
                    if (dataBuildTimeRecord.TryDequeue(out int prev))
                    {
                        dataBuildTimeSum -= prev;
                        dataBuildTimeRecord.Enqueue(time);
                        dataBuildTimeSum += time;
                    }
                }

                sw.Restart();

                // Build chunk vertex
                var buildResult = builder.Build(buildData, chunkRender);

                time = (int) sw.ElapsedMilliseconds;

                lock (vertexBuildTimeRecord)
                {
                    if (vertexBuildTimeRecord.TryDequeue(out int prev))
                    {
                        vertexBuildTimeSum -= prev;
                        vertexBuildTimeRecord.Enqueue(time);
                        vertexBuildTimeSum += time;
                    }
                }

                Loom.QueueOnMainThread(() =>
                {
                    if (chunkRender != null)
                    {
                        if (buildResult == ChunkBuildResult.Cancelled)
                            chunkRender.State = ChunkBuildState.Cancelled;

                        chunkRendersBeingBuilt.Remove(chunkRender);
                    }
                });

            }, chunkRender.TokenSource.Token);
        }

        public const int BUILD_COUNT_LIMIT = 6;
        private BlockLoc? lastPlayerBlockLoc = null;
        private bool terrainColliderDirty = true;

        /// <summary>
        /// Unload a chunk column, invoked from network thread
        /// </summary>
        /// <param name="chunkX"></param>
        /// <param name="chunkZ"></param>
        public void UnloadChunkColumn(int chunkX, int chunkZ)
        {
            world[chunkX, chunkZ] = null;

            int2 chunkCoord = new(chunkX, chunkZ);
            Loom.QueueOnMainThread(() => {
                if (renderColumns.ContainsKey(chunkCoord))
                {
                    renderColumns[chunkCoord].Unload(ref chunkRendersBeingBuilt, ref chunkRendersToBeBuilt, chunkRenderPool);
                    renderColumns.Remove(chunkCoord);
                }

                var blockEntityLocs = blockEntityRenders.Keys.Where(l =>
                        l.GetChunkX() == chunkX && l.GetChunkZ() == chunkZ).ToArray();
                
                foreach (var blockLoc in blockEntityLocs)
                {
                    // Block entity is within the chunk column to be unloaded
                    blockEntityRenders[blockLoc].Unload();
                    blockEntityRenders.Remove(blockLoc);
                }
            });
        }

        /// <summary>
        /// Clear all chunks data
        /// </summary>
        public void ClearChunksData()
        {
            world.Clear();
        }

        /// <summary>
        /// Unload all chunks and block entity renders in the world
        /// </summary>
        public void ReloadChunksRender(bool clearBlockEntities = true)
        {
            // Clear the queue first...
            chunkRendersToBeBuilt.Clear();

            // And cancel current chunk builds
            foreach (var chunkRender in chunkRendersBeingBuilt)
            {
                chunkRender.TokenSource?.Cancel();
            }
            chunkRendersBeingBuilt.Clear();

            // Clear all chunk columns in world
            var chunkCoords = renderColumns.Keys.ToArray();

            foreach (var chunkCoord in chunkCoords)
            {
                renderColumns[chunkCoord].Unload(ref chunkRendersBeingBuilt, ref chunkRendersToBeBuilt, chunkRenderPool);
                renderColumns.Remove(chunkCoord);
            }
            renderColumns.Clear();

            // Clear all block entity renders
            if (clearBlockEntities)
            {
                var locs = blockEntityRenders.Keys.ToArray();
                foreach (var loc in locs)
                {
                    blockEntityRenders[loc].Unload();
                }
                blockEntityRenders.Clear();
                nearbyBlockEntities.Clear();
            }
        }

        public void MarkDirtyAt(BlockLoc blockLoc)
        {
            int chunkX = blockLoc.GetChunkX(), chunkZ = blockLoc.GetChunkZ();
            var column = GetChunkRenderColumn(chunkX, chunkZ, false);
            int chunkY = blockLoc.GetChunkY(World.GetDimension().minY);

            if (column != null) // Queue this chunk to rebuild list...
            {
                // Create the chunk render object if not present (previously empty)
                var chunk = column.GetOrCreateChunkRender(chunkY, chunkRenderPool);

                // Queue the chunk. Priority is left as 0(highest), so that changes can be seen instantly
                QueueChunkRenderBuild(chunk);

                if (blockLoc.GetChunkBlockY() == 0 && (chunkY - 1) >= 0) // In the bottom layer of this chunk
                {   // Queue the chunk below, if it isn't empty
                    QueueChunkRenderBuildIfNotEmpty(column.GetChunkRender(chunkY - 1));
                }
                else if (blockLoc.GetChunkBlockY() == Chunk.SIZE - 1 && ((chunkY + 1) * Chunk.SIZE) < World.GetDimension().height) // In the top layer of this chunk
                {   // Queue the chunk above, if it isn't empty
                    QueueChunkRenderBuildIfNotEmpty(column.GetChunkRender(chunkY + 1));
                }
            }

            if (blockLoc.GetChunkBlockX() == 0) // Check MC X direction neighbors
                QueueChunkRenderBuildIfNotEmpty(GetChunkRender(chunkX - 1, chunkY, chunkZ));
            else if (blockLoc.GetChunkBlockX() == Chunk.SIZE - 1)
                QueueChunkRenderBuildIfNotEmpty(GetChunkRender(chunkX + 1, chunkY, chunkZ));

            if (blockLoc.GetChunkBlockZ() == 0) // Check MC Z direction neighbors
                QueueChunkRenderBuildIfNotEmpty(GetChunkRender(chunkX, chunkY, chunkZ - 1));
            else if (blockLoc.GetChunkBlockZ() == Chunk.SIZE - 1)
                QueueChunkRenderBuildIfNotEmpty(GetChunkRender(chunkX, chunkY, chunkZ + 1));
            
            if (blockLoc.DistanceSquared(client!.GetLocation().GetBlockLoc()) <= ChunkRenderBuilder.MOVEMENT_RADIUS_SQR)
                terrainColliderDirty = true; // Terrain collider needs to be updated
        }

        public void MarkDirtyBecauseOfLightUpdate(int chunkX, int chunkY, int chunkZ)
        {
            var column = GetChunkRenderColumn(chunkX, chunkZ, false);
            if (column != null) // Queue this chunk to rebuild list...
            {   // Create the chunk render object if not present (previously empty)
                var chunk = column.GetOrCreateChunkRender(chunkY, chunkRenderPool);

                // Queue the chunk. Priority is left as 0(highest), so that changes can be seen instantly
                QueueChunkRenderBuild(chunk);
            }
        }

        public void InitializeTerrainCollider(BlockLoc playerBlockLoc, Action callback = null)
        {
            terrainColliderDirty = false;

            Task.Run(async () =>
            {
                // Wait for old data to be cleared up
                await Task.Delay(100);

                int chunkX = playerBlockLoc.GetChunkX();
                int chunkZ = playerBlockLoc.GetChunkZ();

                int delayCount = 50; // Max delay time to stop waiting forever
                
                while (!world.IsChunkColumnLoaded(chunkX, chunkZ) && delayCount > 0)
                {
                    // Wait until the chunk column data is ready
                    await Task.Delay(100);
                    delayCount--;
                }

                // Update player liquid state
                var inLiquid = GetBlock(playerBlockLoc).State.InLiquid;
                EventManager.Instance.Broadcast(new PlayerLiquidEvent(inLiquid));

                builder!.BuildTerrainCollider(world, playerBlockLoc, movementCollider!, liquidCollider!, callback);

                // Set last player location
                lastPlayerBlockLoc = playerBlockLoc;
            });
        }

        public void RebuildTerrainCollider(BlockLoc playerBlockLoc)
        {
            terrainColliderDirty = false;

            Task.Run(() =>
            {
                builder!.BuildTerrainCollider(world, playerBlockLoc, movementCollider!, liquidCollider!, null);
            });
        }

        void Awake()
        {
            // Create Unity object pools on awake
            chunkRenderPool = new(CreateNewChunkRender, GetChunkRender, ReleaseChunkRender, null, false, 500);
        }

        void Start()
        {
            client = CornApp.CurrentClient;

            var modelTable = ResourcePackManager.Instance.StateModelTable;
            builder = new(modelTable);

            var movementColliderObj = new GameObject("Movement Collider")
            {
                layer = LayerMask.NameToLayer(MOVEMENT_LAYER_NAME)
            };
            movementCollider = movementColliderObj.AddComponent<MeshCollider>();

            var liquidColliderObj = new GameObject("Liquid Collider")
            {
                layer = LayerMask.NameToLayer(LIQUID_SURFACE_LAYER_NAME)
            };
            liquidCollider = liquidColliderObj.AddComponent<MeshCollider>();
        }

        void FixedUpdate()
        {
            // Don't build world until biomes are received and registered
            if (!World.BiomesInitialized) return;

            int newCount = BUILD_COUNT_LIMIT - chunkRendersBeingBuilt.Count;

            // Build chunks in queue...
            Profiler.BeginSample("Start chunk render build tasks");
            // Start chunk building tasks...
            while (newCount > 0 && chunkRendersToBeBuilt.Count > 0)
            {
                var nextChunk = chunkRendersToBeBuilt.Dequeue();

                if (nextChunk == null || GetChunkRenderColumn(nextChunk.ChunkX, nextChunk.ChunkZ, false) == null)
                {   // Chunk is unloaded while waiting in the queue, ignore it...
                    continue;
                }
                else
                {
                    Profiler.BeginSample("Build tasks");
                    BuildChunkRender(nextChunk);
                    Profiler.EndSample();
                    newCount--;
                }
            }
            Profiler.EndSample();

            // Update only a small subset of chunks to reduce lag spikes
            Profiler.BeginSample("Update chunks (Add)");
            UpdateChunkRendersListAdd(updateTargetMask);
            Profiler.EndSample();

            if (chunkRendersToBeBuilt.Count < BUILD_COUNT_LIMIT) // If CPU is not so busy
            {
                Profiler.BeginSample("Update chunks (Remove)");
                UpdateChunkRendersListRemove(updateTargetMask);
                Profiler.EndSample();
            }
            
            updateTargetMask = (updateTargetMask + 1) % MASK_CYCLE_LENGTH;
        }

        void Update()
        {
            var client = CornApp.CurrentClient;

            if (client == null) // Game is not ready, cancel update
                return;
            
            var playerBlockLoc = client.GetLocation().GetBlockLoc();

            foreach (var pair in blockEntityRenders)
            {
                var blockLoc = pair.Key;
                var render = pair.Value;

                // Call managed update
                render.ManagedUpdate(client.GetTickMilSec());

                // Update entities around the player
                float dist = (float) blockLoc.DistanceSquared(playerBlockLoc);
                bool inNearbyDict = nearbyBlockEntities.ContainsKey(blockLoc);

                if (dist < NEARBY_THERESHOLD_INNER) // Add entity to dictionary
                {
                    if (inNearbyDict)
                        nearbyBlockEntities[blockLoc] = dist;
                    else
                        nearbyBlockEntities.Add(blockLoc, dist);
                }
                else if (dist > NEARBY_THERESHOLD_OUTER) // Remove entity from dictionary
                {
                    if (inNearbyDict)
                        nearbyBlockEntities.Remove(blockLoc);
                }
                else // Update entity's distance to the player if it is in the dictionary, otherwise do nothing
                {
                    if (inNearbyDict)
                        nearbyBlockEntities[blockLoc] = dist;
                }
            }

            if (lastPlayerBlockLoc != null) // Updating location, update terrain collider if necessary
            {
                if (terrainColliderDirty || lastPlayerBlockLoc.Value != playerBlockLoc)
                {
                    RebuildTerrainCollider(playerBlockLoc);
                    // Update player liquid state
                    var inLiquid = GetBlock(playerBlockLoc).State.InLiquid;
                    var prevInLiquid = GetBlock(lastPlayerBlockLoc.Value).State.InLiquid;
                    if (prevInLiquid != inLiquid) // Player liquid state changed, broadcast this change
                    {
                        EventManager.Instance.Broadcast(new PlayerLiquidEvent(inLiquid));
                    }
                    // Update last location only if it is used
                    lastPlayerBlockLoc = playerBlockLoc; 
                }
            }
        }
        #endregion
    }
}