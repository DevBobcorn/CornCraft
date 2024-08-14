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
        public const string INTERACTION_LAYER_NAME = "Interaction";
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

        /// <summary>
        /// Light calculator used for updating block lights
        /// </summary>
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
        private readonly PriorityQueue<ChunkRender> chunkRendersToBeBuilt = new();
        private readonly List<ChunkRender> chunkRendersBeingBuilt = new();

        private readonly HashSet<int3> lightUpdateRequests = new();
        private readonly HashSet<int3> lightUpdateChecklist = new();

        private BaseCornClient client;
        private ChunkRenderBuilder builder;

        // Terrain collider for movement
        private MeshCollider movementCollider, liquidCollider;

        public void SetClient(BaseCornClient client) => this.client = client;

        private Vector3Int _worldOriginOffset = Vector3Int.zero;

        public void SetWorldOriginOffset(Vector3 posDelta, Vector3Int offset)
        {
            _worldOriginOffset = offset;

            // Move all registered ChunkColumns
            foreach (var pair in renderColumns)
            {
                pair.Value.transform.localPosition = CoordConvert.MC2Unity(_worldOriginOffset, pair.Key.x * Chunk.SIZE, 0F, pair.Key.y * Chunk.SIZE);
            }

            // Move all registered BlockEntityRenders
            foreach (var pair in blockEntityRenders)
            {
                pair.Value.transform.localPosition = CoordConvert.MC2Unity(_worldOriginOffset, pair.Key.ToCenterLocation());
            }

            // Update collider positions and force flush collider transform changes
            movementCollider.transform.position += posDelta;
            liquidCollider.transform.position += posDelta;

            Physics.SyncTransforms();
        }

        private static int dataBuildTimeSum = 0, vertexBuildTimeSum = 0;
        private static readonly Queue<int> dataBuildTimeRecord = new(Enumerable.Repeat(0, 200));
        private static readonly Queue<int> vertexBuildTimeRecord = new(Enumerable.Repeat(0, 200));

        public string GetDebugInfo()
        {
            return $"Unfinished Light Updates: {lightUpdateChecklist.Count}\nQueued Chunks: {chunkRendersToBeBuilt.Count}\nBuilding Chunks: {chunkRendersBeingBuilt.Count}\n- Data Build Time Avg: {dataBuildTimeSum / 200F:0.00} ms\n- Vert Build Time Avg: {vertexBuildTimeSum / 200F:0.00} ms\nBlock Entity Count: {blockEntityRenders.Count}";
        }

        #region Chunk render access
        private ObjectPool<ChunkRenderColumn> chunkRenderColumnPool;
        private ObjectPool<ChunkRender> chunkRenderPool;

        /// <summary>
        /// Creation method used by object pool
        /// </summary>
        private static ChunkRender CreateNewChunkRender()
        {
            // Create a new chunk render object...
            var chunkObj = new GameObject($"Chunk [Pooled]")
            {
                layer = LayerMask.NameToLayer(INTERACTION_LAYER_NAME)
            };
            ChunkRender newChunk = chunkObj.AddComponent<ChunkRender>();
            
            return newChunk;
        }

        /// <summary>
        /// Release method used by object pool
        /// </summary>
        private static void OnReleaseChunkRender(ChunkRender chunk)
        {
            // Unparent and hide this ChunkRender
            chunk.transform.parent = null;
            var chunkObj = chunk.gameObject;
            chunkObj.name = $"Chunk [Pooled]";
            chunkObj.SetActive(false);

            var meshFilter = chunkObj.GetComponent<MeshFilter>();
            Destroy(meshFilter.sharedMesh);
            meshFilter.sharedMesh = null;
        }

        /// <summary>
        /// Creation method used by object pool
        /// </summary>
        private static ChunkRenderColumn CreateNewChunkRenderColumn()
        {
            // Create this chunk column...
            var columnObj = new GameObject($"Column [Pooled]");
            ChunkRenderColumn newColumn = columnObj.AddComponent<ChunkRenderColumn>();

            return newColumn;
        }

        /// <summary>
        /// Release method used by object pool
        /// </summary>
        private static void OnReleaseChunkRenderColumn(ChunkRenderColumn column)
        {
            var columnObj = column.gameObject;
            columnObj.name = $"Column [Pooled]";
        }

        /// <summary>
        /// Get existing ChunkRenderColumn, returns null if not present
        /// </summary>
        private ChunkRenderColumn GetChunkRenderColumn(int chunkX, int chunkZ)
        {
            int2 chunkCoord = new(chunkX, chunkZ);

            if (renderColumns.ContainsKey(chunkCoord))
            {
                return renderColumns[chunkCoord];
            }

            return null;
        }

        /// <summary>
        /// Get existing ChunkRenderColumn or create one from object pool if not present
        /// </summary>
        private ChunkRenderColumn GetOrCreateChunkRenderColumn(int chunkX, int chunkZ)
        {
            int2 chunkCoord = new(chunkX, chunkZ);

            if (renderColumns.ContainsKey(chunkCoord))
            {
                return renderColumns[chunkCoord];
            }
                
            // This ChunkRenderColumn doesn't currently exist...
            Profiler.BeginSample("Create chunk render object");

            // Get one from pool
            var column = chunkRenderColumnPool.Get();

            column.ChunkX = chunkX;
            column.ChunkZ = chunkZ;

            var columnObj = column.gameObject;
            columnObj.name = $"Column [{chunkX}, {chunkZ}]";

            // Set its parent to world transform...
            columnObj.transform.parent = this.transform;
            columnObj.transform.localPosition = CoordConvert.MC2Unity(_worldOriginOffset, chunkX * Chunk.SIZE, 0F, chunkZ * Chunk.SIZE);

            renderColumns.Add(chunkCoord, column);

            Profiler.EndSample();

            return column;
        }

        /// <summary>
        /// Get existing ChunkRender, returns null if not present
        /// </summary>
        public ChunkRender GetChunkRender(int chunkX, int chunkY, int chunkZ)
        {
            var column = GetChunkRenderColumn(chunkX, chunkZ);

            if (column == null)
                return null;

            return column.GetChunkRender(chunkY);
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

        /// <summary>
        /// Should be invoked using a Task or worker thread
        /// </summary>
        private void RecalculateBlockLight(int3 chunkPos)
        {
            // Recalculate block light
            var result = lightCalc.RecalculateLightValues(world, chunkPos);

            // Write back updated light values
            world.SetBlockLightForChunk(chunkPos.x, chunkPos.y, chunkPos.z, result);

            Loom.QueueOnMainThread(() =>
            {
                QueueChunkBuildAfterLightUpdate(chunkPos.x, chunkPos.y, chunkPos.z);

                lightUpdateChecklist.Remove(chunkPos);
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
            bool shouldRecalcLight = false;

            if (column != null)
            {
                // Update chunk data
                var chunk = column.GetChunk(blockLoc);
                if (chunk == null)
                    column[blockLoc.GetChunkYIndex(column.MinimumY)] = chunk = new Chunk();
                chunk[blockLoc.GetChunkBlockX(), blockLoc.GetChunkBlockY(), blockLoc.GetChunkBlockZ()] = block;
                // Update ambient occulsion and light data cache
                shouldRecalcLight = column.UpdateCachedBlockData(blockLoc, block.State);
            }

            // Block render is considered separately even if chunk data is not present
            Loom.QueueOnMainThread(() => {
                // Check if the location has a block entity and remove it
                RemoveBlockEntityRender(blockLoc);
                // Auto-create block entity if present
                if (BlockEntityTypePalette.INSTANCE.GetBlockEntityForBlock(block.BlockId, out BlockEntityType blockEntityType))
                {
                    AddBlockEntityRender(blockLoc, blockEntityType, null);
                }

                if (shouldRecalcLight)
                {
                    var cp = new int3(blockLoc.GetChunkX(), blockLoc.GetChunkYIndex(column.MinimumY), blockLoc.GetChunkZ());

                    // Calculate neighbor chunks which might be affected by this light update
                    int px = blockLoc.GetChunkBlockX();
                    int py = blockLoc.GetChunkBlockY();
                    int pz = blockLoc.GetChunkBlockZ();

                    // 6 face neighbors
                    if (px <= 13) AddLightUpdateRequest(new(cp.x - 1, cp.y,     cp.z    ));
                    if (py <= 13) AddLightUpdateRequest(new(cp.x,     cp.y - 1, cp.z    ));
                    if (pz <= 13) AddLightUpdateRequest(new(cp.x,     cp.y,     cp.z - 1));
                    if (px >=  2) AddLightUpdateRequest(new(cp.x + 1, cp.y,     cp.z    ));
                    if (py >=  2) AddLightUpdateRequest(new(cp.x,     cp.y + 1, cp.z    ));
                    if (pz >=  2) AddLightUpdateRequest(new(cp.x,     cp.y,     cp.z + 1));

                    // 12 edge neighbors
                    if (px + py <= 12) AddLightUpdateRequest(new(cp.x - 1, cp.y - 1, cp.z    ));
                    if (px - py >=  3) AddLightUpdateRequest(new(cp.x + 1, cp.y - 1, cp.z    ));
                    if (py - px >=  3) AddLightUpdateRequest(new(cp.x - 1, cp.y + 1, cp.z    ));
                    if (px + py >= 18) AddLightUpdateRequest(new(cp.x + 1, cp.y + 1, cp.z    ));
                    if (pz + py <= 12) AddLightUpdateRequest(new(cp.x,     cp.y - 1, cp.z - 1));
                    if (pz - py >=  3) AddLightUpdateRequest(new(cp.x,     cp.y - 1, cp.z + 1));
                    if (py - pz >=  3) AddLightUpdateRequest(new(cp.x,     cp.y + 1, cp.z - 1));
                    if (pz + py >= 18) AddLightUpdateRequest(new(cp.x,     cp.y + 1, cp.z + 1));
                    if (px + pz <= 12) AddLightUpdateRequest(new(cp.x - 1, cp.y,     cp.z - 1));
                    if (px - pz >=  3) AddLightUpdateRequest(new(cp.x + 1, cp.y,     cp.z - 1));
                    if (pz - px >=  3) AddLightUpdateRequest(new(cp.x - 1, cp.y,     cp.z + 1));
                    if (px + pz >= 18) AddLightUpdateRequest(new(cp.x + 1, cp.y,     cp.z + 1));

                    // 8 corner neighbors
                    if (px + py + pz <= 11) AddLightUpdateRequest(new(cp.x - 1, cp.y - 1, cp.z - 1));
                    if (px - py - pz >=  4) AddLightUpdateRequest(new(cp.x + 1, cp.y - 1, cp.z - 1));
                    if (py - px - pz >=  4) AddLightUpdateRequest(new(cp.x - 1, cp.y + 1, cp.z - 1));
                    if (pz - px - py >=  4) AddLightUpdateRequest(new(cp.x - 1, cp.y - 1, cp.z + 1));
                    if (py + pz - px >= 19) AddLightUpdateRequest(new(cp.x - 1, cp.y + 1, cp.z + 1));
                    if (px + pz - py >= 19) AddLightUpdateRequest(new(cp.x + 1, cp.y - 1, cp.z + 1));
                    if (px + py - pz >= 19) AddLightUpdateRequest(new(cp.x + 1, cp.y + 1, cp.z - 1));
                    if (py + pz + px >= 34) AddLightUpdateRequest(new(cp.x + 1, cp.y + 1, cp.z + 1));

                    // Self
                    AddLightUpdateRequest(cp);
                }
                else
                {
                    // Mark the chunk dirty and queue for mesh rebuild
                    MarkDirtyAt(blockLoc);
                }
            });
        }

        private void AddLightUpdateRequest(int3 chunkPos)
        {
            lightUpdateRequests.Add(chunkPos);
            lightUpdateChecklist.Add(chunkPos);
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
                        prevBlockEntity.BlockEntityTags = new();
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
                blockEntityObj.transform.localPosition = CoordConvert.MC2Unity(_worldOriginOffset, blockLoc.ToCenterLocation());

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
        /// Use a mask to specify an evenly distributed subset of ChunkRenders to update
        /// </summary>
        /// <param name="mask">A value from 0 to MASK_CYCLE_LENGTH - 1</param>
        private void UpdateChunkRendersListAdd(int mask)
        {
            var playerLoc = client!.GetLocation();
            var blockLoc = playerLoc.GetBlockLoc();
            ChunkRenderColumn columnRender;

            int viewDist = CornGlobal.MCSettings.RenderDistance;
            int viewDistSqr = viewDist * viewDist;

            int chunkColumnSize = (World.GetDimension().height + Chunk.SIZE - 1) / Chunk.SIZE; // Round up
            int offsetY = World.GetDimension().minY;

            //var renderCamera = client.CameraController.RenderCamera;

            // Add nearby chunks
            for (int cx = -viewDist;cx <= viewDist;cx++)
                for (int cz = -viewDist;cz <= viewDist;cz++)
                {
                    if (((cx + cz) & (MASK_CYCLE_LENGTH - 1)) != mask || cx * cx + cz * cz >= viewDistSqr) continue;
                    int chunkX = blockLoc.GetChunkX() + cx, chunkZ = blockLoc.GetChunkZ() + cz;
                    
                    if (world.IsChunkColumnLoaded(chunkX, chunkZ))
                    {
                        var column = GetChunkRenderColumn(chunkX, chunkZ);
                        if (column == null)
                        {   // Chunks data is ready, but chunk render column is not
                            //int chunkMask = world[chunkX, chunkZ]!.ChunkMask;
                            // Create it and add the whole column to render list...
                            columnRender = GetOrCreateChunkRenderColumn(chunkX, chunkZ);
                            for (int chunkY = 0;chunkY < chunkColumnSize;chunkY++)
                            {
                                // Create chunk renders and queue them...
                                if (!(world[chunkX, chunkZ]?.ChunkIsEmpty(chunkY) ?? true))
                                {
                                    // This chunk is not empty and needs to be added and queued
                                    var chunk = columnRender.GetOrCreateChunkRender(chunkY, chunkRenderPool);
                                    //var inView = renderCamera.ChunkInViewport(chunkX, chunk.ChunkYIndex, chunkZ, offsetY);

                                    UpdateBuildPriority(playerLoc, chunk, offsetY);
                                    QueueChunkRenderBuild(chunk);
                                }
                            }
                        }
                        else
                        {
                            foreach (var chunk in column.GetChunkRenders().Values)
                            {
                                if (chunk.State == ChunkBuildState.Delayed)
                                {
                                    //var inView = renderCamera.ChunkInViewport(chunkX, chunk.ChunkYIndex, chunkZ, offsetY);

                                    // Queue delayed or cancelled chunk builds...
                                    UpdateBuildPriority(playerLoc, chunk, offsetY);
                                    QueueChunkRenderBuild(chunk);
                                }
                            }
                        }
                    }
                }

        }

        /// <summary>
        /// Use a mask to specify an evenly distributed subset of ChunkRenders to update
        /// </summary>
        /// <param name="mask">A value from 0 to MASK_CYCLE_LENGTH - 1</param>
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
                    var column = renderColumns[chunkCoord];
                    column.Unload(chunkRendersBeingBuilt, chunkRendersToBeBuilt, chunkRenderPool);

                    // Return this ChunkRenderColumn to pool
                    chunkRenderColumnPool.Release(column);

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
                    var column = renderColumns[chunkCoord];
                    column.Unload(chunkRendersBeingBuilt, chunkRendersToBeBuilt, chunkRenderPool);

                    // Return this ChunkRenderColumn to pool
                    chunkRenderColumnPool.Release(column);

                    renderColumns.Remove(chunkCoord);
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
                    world.IsChunkColumnLoaded(chunkX + 1, chunkZ + 1) 
            ))
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
                    var column = renderColumns[chunkCoord];
                    column.Unload(chunkRendersBeingBuilt, chunkRendersToBeBuilt, chunkRenderPool);

                    // Return this ChunkRenderColumn to pool
                    chunkRenderColumnPool.Release(column);

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
            lightUpdateRequests.Clear();

            // And cancel current chunk builds
            foreach (var chunkRender in chunkRendersBeingBuilt)
            {
                chunkRender.TokenSource?.Cancel();
            }
            chunkRendersBeingBuilt.Clear();
            lightUpdateChecklist.Clear();

            // Clear all chunk columns in world
            var chunkCoords = renderColumns.Keys.ToArray();

            foreach (var chunkCoord in chunkCoords)
            {
                var column = renderColumns[chunkCoord];
                column.Unload(chunkRendersBeingBuilt, chunkRendersToBeBuilt, chunkRenderPool);

                // Return this ChunkRenderColumn to pool
                chunkRenderColumnPool.Release(column);

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

        private void MarkDirtyAt(BlockLoc blockLoc)
        {
            int chunkX = blockLoc.GetChunkX(), chunkZ = blockLoc.GetChunkZ();
            var column = GetChunkRenderColumn(chunkX, chunkZ);
            int chunkYIndex = blockLoc.GetChunkYIndex(World.GetDimension().minY);

            if (column != null) // Queue this chunk to rebuild list...
            {
                // Create the chunk render object if not present (previously empty)
                var chunk = column.GetOrCreateChunkRender(chunkYIndex, chunkRenderPool);

                // Queue the chunk. Priority is left as 0(highest), so that changes can be seen instantly
                QueueChunkRenderBuild(chunk);

                if (blockLoc.GetChunkBlockY() == 0 && (chunkYIndex - 1) >= 0) // In the bottom layer of this chunk
                {   // Queue the chunk below, if it isn't empty
                    QueueChunkRenderBuildIfNotEmpty(column.GetChunkRender(chunkYIndex - 1));
                }
                else if (blockLoc.GetChunkBlockY() == Chunk.SIZE - 1 && ((chunkYIndex + 1) * Chunk.SIZE) < World.GetDimension().height) // In the top layer of this chunk
                {   // Queue the chunk above, if it isn't empty
                    QueueChunkRenderBuildIfNotEmpty(column.GetChunkRender(chunkYIndex + 1));
                }
            }

            if (blockLoc.GetChunkBlockX() == 0) // Check MC X direction neighbors
                QueueChunkRenderBuildIfNotEmpty(GetChunkRender(chunkX - 1, chunkYIndex, chunkZ));
            else if (blockLoc.GetChunkBlockX() == Chunk.SIZE - 1)
                QueueChunkRenderBuildIfNotEmpty(GetChunkRender(chunkX + 1, chunkYIndex, chunkZ));

            if (blockLoc.GetChunkBlockZ() == 0) // Check MC Z direction neighbors
                QueueChunkRenderBuildIfNotEmpty(GetChunkRender(chunkX, chunkYIndex, chunkZ - 1));
            else if (blockLoc.GetChunkBlockZ() == Chunk.SIZE - 1)
                QueueChunkRenderBuildIfNotEmpty(GetChunkRender(chunkX, chunkYIndex, chunkZ + 1));
            
            if (blockLoc.DistanceSquared(client!.GetLocation().GetBlockLoc()) <= ChunkRenderBuilder.MOVEMENT_RADIUS_SQR)
                terrainColliderDirty = true; // Terrain collider needs to be updated
        }

        /// <summary>
        /// Queue a chunk mesh rebuild after light update
        /// </summary>
        public void QueueChunkBuildAfterLightUpdate(int chunkX, int chunkYIndex, int chunkZ)
        {
            var column = GetChunkRenderColumn(chunkX, chunkZ);
            if (column != null) // Queue this chunk to rebuild list...
            {   // Create the chunk render object if not present (previously empty)
                var chunk = column.GetOrCreateChunkRender(chunkYIndex, chunkRenderPool);

                // Queue the chunk. Priority is left as 0(highest), so that changes can be seen instantly
                QueueChunkRenderBuildIfNotEmpty(chunk);
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

                builder!.BuildTerrainCollider(world, playerBlockLoc, _worldOriginOffset, movementCollider!, liquidCollider!, callback);

                // Set last player location
                lastPlayerBlockLoc = playerBlockLoc;
            });
        }

        public void RebuildTerrainCollider(BlockLoc playerBlockLoc)
        {
            terrainColliderDirty = false;

            Task.Run(() =>
            {
                builder!.BuildTerrainCollider(world, playerBlockLoc, _worldOriginOffset, movementCollider!, liquidCollider!, null);
            });
        }

        void Awake()
        {
            // Create Unity object pools on awake
            chunkRenderPool = new(CreateNewChunkRender, null, OnReleaseChunkRender, null, false, 5000);
            chunkRenderColumnPool = new(CreateNewChunkRenderColumn, null, OnReleaseChunkRenderColumn, null, false, 500);
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
            Profiler.BeginSample("Start chunk render build(/light update) tasks");
            // Start light update tasks
            while (newCount > 0 && lightUpdateRequests.Count > 0)
            {
                var cp = lightUpdateRequests.First();
                lightUpdateRequests.Remove(cp);

                Task.Run(() =>
                {
                    try
                    {
                        RecalculateBlockLight(cp);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                });
                newCount--;
            }

            // Start chunk building tasks...
            while (newCount > 0 && chunkRendersToBeBuilt.Count > 0)
            {
                var nextChunk = chunkRendersToBeBuilt.Dequeue();

                if (nextChunk == null || GetChunkRenderColumn(nextChunk.ChunkX, nextChunk.ChunkZ) == null)
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