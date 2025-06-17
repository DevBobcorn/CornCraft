using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Profiling;
using Unity.Mathematics;

using CraftSharp.Control;
using CraftSharp.Event;
using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    public class ChunkRenderManager : MonoBehaviour, IChunkRenderManager
    {
        private const string TERRAIN_BOX_COLLIDER_LAYER_NAME = "TerrainBoxCollider";
        private const string TERRAIN_MESH_COLLIDER_LAYER_NAME = "TerrainMeshCollider";
        private const string LIQUID_BOX_COLLIDER_LAYER_NAME = "LiquidBoxCollider";

        [SerializeField] private Transform blockEntityParent;

        #region GameObject Prefabs for each block entity type
        [SerializeField] private GameObject defaultPrefab;
        #endregion

        private readonly Dictionary<ResourceLocation, GameObject> blockEntityPrefabs = new();

        private GameObject GetPrefabForType(ResourceLocation type)
        {
            return blockEntityPrefabs.GetValueOrDefault(type, defaultPrefab);
        }

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
        private const float NEARBY_THRESHOLD_INNER =  81F; //  9 *  9
        private const float NEARBY_THRESHOLD_OUTER = 100F; // 10 * 10

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
        private readonly HashSet<ChunkRender> chunkRendersToBeBuiltAsSet = new();
        private readonly HashSet<ChunkRender> chunkRendersBeingBuilt = new(BUILD_COUNT_LIMIT);

        // Stores whether an active chunk(which is visible to the player) has been scheduled for rebuild
        private readonly HashSet<int2> nearbyChunkCoords = new();
        private readonly HashSet<int2> nearbyChunkCoordsToBeChecked = new();

        private readonly HashSet<int3> lightUpdateRequests = new();
        private readonly HashSet<int3> lightUpdateUnfinished = new();

        private BaseCornClient client;
        private ChunkRenderBuilder builder;

        // Terrain collider for movement
        private GameObject terrainBoxColliderGameObject, liquidBoxColliderGameObject;
        private readonly Dictionary<BlockLoc, BoxCollider[]> colliderList = new();

        public void SetClient(BaseCornClient curClient) => client = curClient;

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
            foreach (var boxCollider in terrainBoxColliderGameObject.GetComponents<BoxCollider>())
                boxCollider.center += posDelta;
            foreach (var boxCollider in liquidBoxColliderGameObject.GetComponents<BoxCollider>())
                boxCollider.center += posDelta;

            Physics.SyncTransforms();
        }

        private static int dataBuildTimeSum, vertexBuildTimeSum;
        private static readonly Queue<int> dataBuildTimeRecord = new(Enumerable.Repeat(0, 200));
        private static readonly Queue<int> vertexBuildTimeRecord = new(Enumerable.Repeat(0, 200));

        public string GetDebugInfo()
        {
            return $"Nearby Chunks To Check: {nearbyChunkCoordsToBeChecked.Count}/{nearbyChunkCoords.Count}\nUnfinished Light Updates: {lightUpdateUnfinished.Count}\nQueued Chunks: {chunkRendersToBeBuiltAsSet.Count}\nBuilding Chunks: {chunkRendersBeingBuilt.Count}\n- Data Build Time Avg: {dataBuildTimeSum / 200F:0.00} ms\n- Vert Build Time Avg: {vertexBuildTimeSum / 200F:0.00} ms\nBlock Entity Count: {blockEntityRenders.Count}";
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
            var chunkObj = new GameObject("Chunk [Pooled]")
            {
                layer = LayerMask.NameToLayer(TERRAIN_MESH_COLLIDER_LAYER_NAME)
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
            chunkObj.name = "Chunk [Pooled]";
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
            var columnObj = new GameObject("Column [Pooled]");
            ChunkRenderColumn newColumn = columnObj.AddComponent<ChunkRenderColumn>();

            return newColumn;
        }

        /// <summary>
        /// Release method used by object pool
        /// </summary>
        private static void OnReleaseChunkRenderColumn(ChunkRenderColumn column)
        {
            var columnObj = column.gameObject;
            columnObj.name = "Column [Pooled]";
        }

        /// <summary>
        /// Get existing ChunkRenderColumn, returns null if not present
        /// </summary>
        private ChunkRenderColumn GetChunkRenderColumn(int chunkX, int chunkZ)
        {
            int2 chunkCoord = new(chunkX, chunkZ);

            return renderColumns.GetValueOrDefault(chunkCoord);
        }

        /// <summary>
        /// Get existing ChunkRenderColumn or create one from object pool if not present
        /// </summary>
        private ChunkRenderColumn GetOrCreateChunkRenderColumn(int chunkX, int chunkZ)
        {
            int2 chunkCoord = new(chunkX, chunkZ);

            if (renderColumns.TryGetValue(chunkCoord, out var renderColumn))
            {
                return renderColumn;
            }

            // This ChunkRenderColumn doesn't currently exist...
            Profiler.BeginSample("Create chunk render object");

            // Get one from pool
            var column = chunkRenderColumnPool.Get();
            column.ColumnPos = new(chunkX, chunkZ);

            var columnObj = column.gameObject;
            columnObj.name = $"Column [{chunkX}, {chunkZ}]";

            // Set its parent to world transform...
            columnObj.transform.parent = transform;
            columnObj.transform.localPosition = CoordConvert.MC2Unity(_worldOriginOffset, chunkX * Chunk.SIZE, 0F, chunkZ * Chunk.SIZE);

            renderColumns.Add(chunkCoord, column);

            Profiler.EndSample();

            return column;
        }

        /// <summary>
        /// Get existing ChunkRender, returns null if not present
        /// </summary>
        private ChunkRender GetChunkRender(int chunkX, int chunkYIndex, int chunkZ)
        {
            var column = GetChunkRenderColumn(chunkX, chunkZ);

            return column ? column.GetChunkRender(chunkYIndex) : null;
        }
        #endregion

        #region Chunk data access

        /// <summary>
        /// Store a chunk, invoked from network thread
        /// </summary>
        public void StoreChunk(int chunkX, int chunkYIndex, int chunkZ, int chunkColumnSize, Chunk chunk)
        {
            world.StoreChunk(chunkX, chunkYIndex, chunkZ, chunkColumnSize, chunk);
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
                lightUpdateUnfinished.Remove(chunkPos);

                QueueChunkBuildAfterLightUpdate(chunkPos.x, chunkPos.y, chunkPos.z);
            });
        }

        /// <summary>
        /// Set block at the specified location, invoked from network thread
        /// </summary>
        /// <param name="blockLoc">Location to set block to</param>
        /// <param name="block">Block to set</param>
        /// <param name="doImmediateBuild">Whether to immediately rebuild chunk mesh</param>
        public void SetBlock(BlockLoc blockLoc, Block block, bool doImmediateBuild = false)
        {
            var chunkX = blockLoc.GetChunkX();
            var chunkZ = blockLoc.GetChunkZ();
            var column = GetChunkColumn(chunkX, chunkZ);
            bool shouldRecalculateLight = false;

            if (column != null)
            {
                // Update chunk data
                var chunk = column.GetChunk(blockLoc);
                if (chunk == null)
                    column[blockLoc.GetChunkYIndex(column.MinimumY)] = chunk = new Chunk();
                chunk[blockLoc.GetChunkBlockX(), blockLoc.GetChunkBlockY(), blockLoc.GetChunkBlockZ()] = block;
                // Update ambient occlusion and light data cache
                shouldRecalculateLight = column.UpdateCachedBlockData(blockLoc, block.State);
            }

            // Block render is considered separately even if chunk data is not present
            Loom.QueueOnMainThread(() =>
            {
                // Check if the location has a block entity and remove it
                RemoveBlockEntityRender(blockLoc);
                // Auto-create block entity if present
                if (BlockEntityTypePalette.INSTANCE.GetBlockEntityForBlock(block.BlockId, out BlockEntityType blockEntityType))
                {
                    AddBlockEntityRender(blockLoc, blockEntityType);
                }

                var chunkYIndex = blockLoc.GetChunkYIndex(column.MinimumY);
                if (doImmediateBuild) // Light is not updated yet, but build the mesh anyway
                {
                    var chunkRenderColumn = GetChunkRenderColumn(chunkX, chunkZ);
                    if (chunkRenderColumn)
                    {
                        // Just rebuild the mesh, don't recalculate lighting data yet (do that later)
                        BuildChunkRenderIfNotEmpty(chunkRenderColumn.GetChunkRender(chunkYIndex));

                        if (blockLoc.GetChunkBlockY() == 0 && chunkYIndex - 1 >= 0) // In the bottom layer of this chunk
                        {
                            // Rebuild below, if it isn't empty
                            BuildChunkRenderIfNotEmpty(chunkRenderColumn.GetChunkRender(chunkYIndex - 1));
                        }
                        else if (blockLoc.GetChunkBlockY() == Chunk.SIZE - 1 && (chunkYIndex + 1) * Chunk.SIZE < World.GetDimensionType().height) // In the top layer of this chunk
                        {
                            // Rebuild the chunk above, if it isn't empty
                            BuildChunkRenderIfNotEmpty(chunkRenderColumn.GetChunkRender(chunkYIndex + 1));
                        }
                    }

                    if (blockLoc.GetChunkBlockX() == 0) // Check MC X direction neighbors
                        BuildChunkRenderIfNotEmpty(GetChunkRender(chunkX - 1, chunkYIndex, chunkZ));
                    else if (blockLoc.GetChunkBlockX() == Chunk.SIZE - 1)
                        BuildChunkRenderIfNotEmpty(GetChunkRender(chunkX + 1, chunkYIndex, chunkZ));

                    if (blockLoc.GetChunkBlockZ() == 0) // Check MC Z direction neighbors
                        BuildChunkRenderIfNotEmpty(GetChunkRender(chunkX, chunkYIndex, chunkZ - 1));
                    else if (blockLoc.GetChunkBlockZ() == Chunk.SIZE - 1)
                        BuildChunkRenderIfNotEmpty(GetChunkRender(chunkX, chunkYIndex, chunkZ + 1));

                    // Update terrain collider
                    RebuildTerrainBoxColliderAt(blockLoc);
                }

                if (shouldRecalculateLight)
                {
                    var cp = new int3(blockLoc.GetChunkX(), blockLoc.GetChunkYIndex(column!.MinimumY), blockLoc.GetChunkZ());

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
            lightUpdateUnfinished.Add(chunkPos);
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
        /// Get block color at the specified location
        /// </summary>
        /// <param name="stateId">Target block state id</param>
        /// <param name="blockLoc">Location to retrieve block color from</param>
        /// <returns>Block at specified location or Air if the location is not loaded</returns>
        public float3 GetBlockColor(int stateId, BlockLoc blockLoc)
        {
            return BlockStatePalette.INSTANCE.GetBlockColor(stateId, world, blockLoc);
        }

        /// <summary>
        /// Get biome at the specified location
        /// </summary>
        public Biome GetBiome(BlockLoc blockLoc)
        {
            return world.GetBiome(blockLoc);
        }

        public static ResourceLocation GetDimensionId()
        {
            return World.GetDimensionId();
        }

        public DimensionType GetDimensionType()
        {
            return World.GetDimensionType();
        }

        /// <summary>
        /// Get block light at the specified location
        /// </summary>
        public byte GetBlockLight(BlockLoc blockLoc)
        {
            return world.GetBlockLight(blockLoc);
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
            return blockEntityRenders.GetValueOrDefault(blockLoc);
        }

        /// <summary>
        /// Add a new block entity render to the world
        /// </summary>
        /// <param name="blockLoc">Location of the block entity</param>
        /// <param name="blockEntityType">Type of the block entity</param>
        /// <param name="tags">Pass in null if auto-creating this block entity</param>
        public void AddBlockEntityRender(BlockLoc blockLoc, BlockEntityType blockEntityType, Dictionary<string, object> tags = null)
        {
            // If the location is occupied by a block entity already
            if (blockEntityRenders.TryGetValue(blockLoc, out var prevBlockEntity))
            {
                if (prevBlockEntity.Type == blockEntityType) // Auto-created, keep it but replace data tags
                {
                    // Update block entity data tags
                    // Auto-creating a block entity while it is already created
                    prevBlockEntity.BlockEntityTags = tags ?? new();
                    // TODO: Update render with updated data tags
                    return;
                }
                // Remove it otherwise
                RemoveBlockEntityRender(blockLoc);
            }

            GameObject blockEntityPrefab = GetPrefabForType(blockEntityType.TypeId);

            if (blockEntityPrefab)
            {
                var blockEntityObj = Instantiate(blockEntityPrefab, blockEntityParent, true);
                var blockEntityRender = blockEntityObj!.GetComponent<BlockEntityRender>();

                blockEntityRenders.Add(blockLoc, blockEntityRender);

                blockEntityObj.name = $"[{blockLoc}] {blockEntityType}";
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

        private static void UpdateBuildPriority(Location currentBlockLoc, ChunkRender chunk, int offsetY)
        {
            // Get this chunk's build priority based on its current distance to the player,
            // a smaller value means a higher priority...
            chunk.Priority = (int)(
                    new Location(chunk.ChunkX * Chunk.SIZE + CHUNK_CENTER, chunk.ChunkYIndex * Chunk.SIZE + CHUNK_CENTER + offsetY,
                            chunk.ChunkZ * Chunk.SIZE + CHUNK_CENTER).DistanceTo(currentBlockLoc) / 16);
        }

        private void UpdateChunkRendersListAdd()
        {
            var playerLoc = client!.GetCurrentLocation();

            int chunkColumnSize = (World.GetDimensionType().height + Chunk.SIZE - 1) / Chunk.SIZE; // Round up
            int offsetY = World.GetDimensionType().minY;

            //var renderCamera = client.CameraController.RenderCamera;

            // Add nearby chunks
            foreach (var coord in nearbyChunkCoordsToBeChecked.ToArray())
            {
                int chunkX = coord.x, chunkZ = coord.y;
                
                if (world.IsChunkColumnLoaded(chunkX, chunkZ))
                {
                    if (!(  world.IsChunkColumnLoaded(chunkX,     chunkZ - 1) && // ZNeg neighbor present
                            world.IsChunkColumnLoaded(chunkX,     chunkZ + 1) && // ZPos neighbor present
                            world.IsChunkColumnLoaded(chunkX - 1, chunkZ    ) && // XNeg neighbor present
                            world.IsChunkColumnLoaded(chunkX + 1, chunkZ    ) && // XPos neighbor present
                            world.IsChunkColumnLoaded(chunkX - 1, chunkZ - 1) &&
                            world.IsChunkColumnLoaded(chunkX - 1, chunkZ + 1) &&
                            world.IsChunkColumnLoaded(chunkX + 1, chunkZ - 1) &&
                            world.IsChunkColumnLoaded(chunkX + 1, chunkZ + 1)
                    ))
                    {
                        continue;
                    }

                    var column = GetChunkRenderColumn(chunkX, chunkZ);
                    if (!column)
                    {
                        // Chunks data is ready, but chunk render column is not
                        //int chunkMask = world[chunkX, chunkZ]!.ChunkMask;
                        // Create it and add the whole column to render list...
                        var columnRender = GetOrCreateChunkRenderColumn(chunkX, chunkZ);
                        for (int chunkY = 0; chunkY < chunkColumnSize; chunkY++)
                        {
                            // Create chunk renders and queue them...
                            if (!(world[chunkX, chunkZ]?.ChunkIsEmpty(chunkY) ?? true))
                            {
                                // This chunk is not empty and needs to be added and queued
                                var chunk = columnRender.GetOrCreateChunkRender(chunkY, chunkRenderPool);
                                //var inView = renderCamera.ChunkInViewport(chunkX, chunk.ChunkYIndex, chunkZ, offsetY);

                                UpdateBuildPriority(playerLoc, chunk, offsetY);
                                QueueChunkRenderBuild(chunk);

                                nearbyChunkCoordsToBeChecked.Remove(coord); // Remove from checklist
                            }
                        }
                    }
                }
            }

        }

        private void UpdateChunkRendersListRemove()
        {
            // Add nearby chunks
            var blockLoc   = client!.GetCurrentLocation().GetBlockLoc();
            int unloadDist = Mathf.RoundToInt(client.RenderDistance * 1.25F);

            foreach (var chunkCoord in renderColumns.Keys.ToArray())
            {
                if (Mathf.Abs(blockLoc.GetChunkX() - chunkCoord.x) > unloadDist || Mathf.Abs(blockLoc.GetChunkZ() - chunkCoord.y) > unloadDist)
                {
                    var column = renderColumns[chunkCoord];
                    column.Unload(chunkRendersBeingBuilt, chunkRendersToBeBuiltAsSet, chunkRendersToBeBuilt, chunkRenderPool);

                    // Return this ChunkRenderColumn to pool
                    chunkRenderColumnPool.Release(column);

                    renderColumns.Remove(chunkCoord);
                }
            }
        }

        private void QueueChunkRenderBuild(ChunkRender chunkRender)
        {
            if (chunkRendersBeingBuilt.Contains(chunkRender))
            {
                chunkRender.TokenSource?.Cancel();
            }

            if (!chunkRendersToBeBuiltAsSet.Contains(chunkRender))
            {
                chunkRendersToBeBuilt.Enqueue(chunkRender);
                chunkRendersToBeBuiltAsSet.Add(chunkRender);
            }
        }

        private void QueueChunkRenderBuildIfNotEmpty(ChunkRender chunkRender)
        {
            if (chunkRender) // Not empty(air) chunk
            {
                QueueChunkRenderBuild(chunkRender);
            }
        }

        /// <summary>
        /// Queue a chunk mesh rebuild after light update
        /// </summary>
        public void QueueChunkBuildAfterLightUpdate(int chunkX, int chunkYIndex, int chunkZ)
        {
            var column = GetChunkRenderColumn(chunkX, chunkZ);
            if (column) // Queue this chunk to rebuild list...
            {   // Create the chunk render object if not present (previously empty)
                var chunk = column.GetOrCreateChunkRender(chunkYIndex, chunkRenderPool);

                // Queue the chunk. Priority is left as 0(highest), so that changes can be seen instantly
                QueueChunkRenderBuildIfNotEmpty(chunk);

                //Debug.Log($"Queued chunk [{chunkX}, {chunkYIndex}, {chunkZ}] after light update");
            }
        }

        private void BuildChunkRender(ChunkRender chunkRender)
        {
            int chunkX = chunkRender.ChunkX, chunkZ = chunkRender.ChunkZ, chunkYIndex = chunkRender.ChunkYIndex;
            var chunkColumnData = GetChunkColumn(chunkX, chunkZ);

            if (chunkColumnData == null) // Chunk column data unloaded, cancel
            {
                int2 chunkCoord = new(chunkRender.ChunkX, chunkRender.ChunkZ);
                if (renderColumns.ContainsKey(chunkCoord))
                {
                    var column = renderColumns[chunkCoord];
                    column.Unload(chunkRendersBeingBuilt, chunkRendersToBeBuiltAsSet, chunkRendersToBeBuilt, chunkRenderPool);

                    // Return this ChunkRenderColumn to pool
                    chunkRenderColumnPool.Release(column);

                    renderColumns.Remove(chunkCoord);
                }
                return;
            }

            var chunkData = chunkColumnData[chunkYIndex];
            if (chunkData == null) // Chunk not available or is empty(air chunk), cancel
            {
                return;
            }

            chunkRendersBeingBuilt.Add(chunkRender);

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
                builder.Build(buildData, chunkRender);

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
                    if (chunkRender)
                    {
                        chunkRendersBeingBuilt.Remove(chunkRender);
                    }
                });

            }, chunkRender.TokenSource.Token);
        }

        private void BuildChunkRenderIfNotEmpty(ChunkRender chunkRender)
        {
            if (chunkRender) // Not empty(air) chunk
            {
                BuildChunkRender(chunkRender);
            }
        }

        private const int BUILD_COUNT_LIMIT = 6;
        private BlockLoc? lastPlayerBlockLoc;
        private int2? lastPlayerChunkLoc;

        /// <summary>
        /// Unload a chunk column, invoked from network thread
        /// </summary>
        /// <param name="chunkX"></param>
        /// <param name="chunkZ"></param>
        public void UnloadChunkColumn(int chunkX, int chunkZ)
        {
            world[chunkX, chunkZ] = null;

            int2 chunkCoord = new(chunkX, chunkZ);

            Loom.QueueOnMainThread(() =>
            {
                if (renderColumns.ContainsKey(chunkCoord))
                {
                    var column = renderColumns[chunkCoord];
                    column.Unload(chunkRendersBeingBuilt, chunkRendersToBeBuiltAsSet, chunkRendersToBeBuilt, chunkRenderPool);

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
            
            if (nearbyChunkCoords.Contains(chunkCoord))
            {
                nearbyChunkCoordsToBeChecked.Add(chunkCoord);
            }
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
            chunkRendersToBeBuiltAsSet.Clear();
            lightUpdateRequests.Clear();

            // And cancel current chunk builds
            foreach (var chunkRender in chunkRendersBeingBuilt)
            {
                chunkRender.TokenSource?.Cancel();
            }
            chunkRendersBeingBuilt.Clear();
            lightUpdateUnfinished.Clear();

            // Clear all chunk columns in world
            var chunkCoords = renderColumns.Keys.ToArray();

            foreach (var chunkCoord in chunkCoords)
            {
                var column = renderColumns[chunkCoord];
                column.Unload(chunkRendersBeingBuilt, chunkRendersToBeBuiltAsSet, chunkRendersToBeBuilt, chunkRenderPool);

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
            int chunkYIndex = blockLoc.GetChunkYIndex(World.GetDimensionType().minY);

            if (column) // Queue this chunk to rebuild list...
            {
                // Create the chunk render object if not present (previously empty)
                var chunk = column.GetOrCreateChunkRender(chunkYIndex, chunkRenderPool);

                // Queue the chunk. Priority is left as 0(highest), so that changes can be seen instantly
                QueueChunkRenderBuild(chunk);

                if (blockLoc.GetChunkBlockY() == 0 && chunkYIndex - 1 >= 0) // In the bottom layer of this chunk
                {   // Queue the chunk below, if it isn't empty
                    QueueChunkRenderBuildIfNotEmpty(column.GetChunkRender(chunkYIndex - 1));
                }
                else if (blockLoc.GetChunkBlockY() == Chunk.SIZE - 1 && (chunkYIndex + 1) * Chunk.SIZE < World.GetDimensionType().height) // In the top layer of this chunk
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

            if (blockLoc.DistanceSquared(client!.GetCurrentLocation().GetBlockLoc()) <=
                ChunkRenderBuilder.MOVEMENT_RADIUS_SQR_PLUS)
            {
                RebuildTerrainBoxColliderAt(blockLoc);
            }
        }

        public void InitializeBoxTerrainCollider(BlockLoc playerBlockLoc, Action callback = null)
        {
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

                Loom.QueueOnMainThread(() =>
                {
                    foreach (var boxCollider in terrainBoxColliderGameObject.GetComponents<BoxCollider>())
                    {
                        Destroy(boxCollider);
                    }
                    foreach (var boxCollider in liquidBoxColliderGameObject.GetComponents<BoxCollider>())
                    {
                        Destroy(boxCollider);
                    }
                    colliderList.Clear();

                    ChunkRenderBuilder.BuildTerrainColliderBoxes(world, playerBlockLoc, _worldOriginOffset, terrainBoxColliderGameObject!, liquidBoxColliderGameObject!, colliderList);

                    // Set last player location
                    lastPlayerBlockLoc = playerBlockLoc;
                    lastPlayerChunkLoc = new(playerBlockLoc.GetChunkX(), playerBlockLoc.GetChunkZ());

                    callback?.Invoke();
                });
            });
        }

        public void RebuildTerrainBoxCollider(BlockLoc playerBlockLoc)
        {
            ChunkRenderBuilder.BuildTerrainColliderBoxes(world, playerBlockLoc, _worldOriginOffset, terrainBoxColliderGameObject, liquidBoxColliderGameObject, colliderList);
        }

        private void RebuildTerrainBoxColliderAt(BlockLoc blockLoc)
        {
            ChunkRenderBuilder.BuildTerrainColliderBoxesAt(world, blockLoc, _worldOriginOffset, terrainBoxColliderGameObject, liquidBoxColliderGameObject, colliderList);
        }

        public void UpdateNearbyChunkCoordList(int playerChunkX, int playerChunkZ)
        {
            int cx, cz;
            int viewDist = client.RenderDistance;
            int viewDistSqr = viewDist * viewDist;

            foreach (var coord in nearbyChunkCoords.ToArray())
            {
                cx = coord.x - playerChunkX;
                cz = coord.y - playerChunkZ;

                if (cx * cx + cz * cz >= viewDistSqr)
                {
                    nearbyChunkCoords.Remove(coord);
                    nearbyChunkCoordsToBeChecked.Remove(coord);
                }
            }

            for (cx = -viewDist; cx <= viewDist; cx++)
                for (cz = -viewDist; cz <= viewDist; cz++)
                {
                    if (cx * cx + cz * cz >= viewDistSqr) continue;

                    var coord = new int2(cx + playerChunkX, cz + playerChunkZ);

                    if (nearbyChunkCoords.Add(coord))
                    {
                        nearbyChunkCoordsToBeChecked.Add(coord);
                    }
                }
        }

        public record BlockRaycastInfo
        {
            public Vector3Int CellPos;
            public BlockLoc BlockLoc;
            public Location ExactLoc;
            public readonly BlockState BlockState;
            public readonly int StateId;

            public BlockRaycastInfo(Vector3Int cellPos, BlockLoc blockLoc, Location exactLoc, BlockState blockState, int stateId)
            {
                CellPos = cellPos;
                BlockLoc = blockLoc;
                ExactLoc = exactLoc;
                BlockState = blockState;
                StateId = stateId;
            }
        }

        public bool RaycastBlocks(List<Vector3Int> cellPosList, Ray ray,
            out Raycaster.AABBRaycastHit aabbInfo,
            out BlockRaycastInfo blockInfo)
        {
            var stateModelTable = ResourcePackManager.Instance.StateModelTable;

            foreach (var cellPos in cellPosList) // Go through the list
            {
                var cellSpaceRay = new Ray(ray.origin - cellPos, ray.direction);
                
                var blockLoc = CoordConvert.Unity2MC(_worldOriginOffset, cellPos);
                var block = GetBlock(blockLoc);
                if (block.StateId == 0) continue; // Extra optimization for air
                
                var blockState = block.State;
                if (blockState.Shape.AABBs.Count == 0) continue; // No AABB, skip

                var offsetType = stateModelTable[block.StateId].OffsetType;
                Vector3? offset = ChunkRenderBuilder.OffsetTypeAffectsAABB(offsetType) ? ChunkRenderBuilder.GetBlockOffsetInBlock(
                    offsetType, blockLoc.X >> 4, blockLoc.Z >> 4, blockLoc.X & 0xF, blockLoc.Z & 0xF) : null;

                aabbInfo = Raycaster.RaycastBlockShape(cellSpaceRay, blockState.Shape, offset);
                
                if (aabbInfo.hit)
                {
                    // Convert from cell space back to world space
                    aabbInfo.point += cellPos;
                    var exactLoc = CoordConvert.Unity2MC(_worldOriginOffset, aabbInfo.point);
                    
                    blockInfo = new(cellPos, blockLoc, exactLoc, blockState, block.StateId);
                    return true;
                }
            }
            
            aabbInfo = new Raycaster.AABBRaycastHit
            {
                hit = false,
                point = Vector3.zero,
                direction = Direction.Up
            };
            blockInfo = null;
            return false;
        }

        private Action<BlockPredictionEvent> blockPredictionCallback;

        private void Awake()
        {
            // Create Unity object pools on awake
            chunkRenderPool = new(CreateNewChunkRender, null, OnReleaseChunkRender, null, false, 5000);
            chunkRenderColumnPool = new(CreateNewChunkRenderColumn, null, OnReleaseChunkRenderColumn, null, false, 500);
        }

        private void Start()
        {
            client = CornApp.CurrentClient;

            var modelTable = ResourcePackManager.Instance.StateModelTable;
            builder = new(modelTable);

            terrainBoxColliderGameObject = new GameObject("Terrain Box Collider")
            {
                layer = LayerMask.NameToLayer(TERRAIN_BOX_COLLIDER_LAYER_NAME)
            };

            liquidBoxColliderGameObject = new GameObject("Liquid Box Collider")
            {
                layer = LayerMask.NameToLayer(LIQUID_BOX_COLLIDER_LAYER_NAME)
            };

            blockPredictionCallback = e =>
            {
                if (!client) return;
                
                // Make sure to set block from network thread
                client.InvokeOnNetMainThread(() =>
                {
                    var block = new Block((ushort) e.BlockStateId);
                    //Debug.Log($"Prediction: {e.BlockLoc} => {block}");

                    SetBlock(e.BlockLoc, block, true);
                });
            };

            EventManager.Instance.Register(blockPredictionCallback);
        }

        private void OnDestroy()
        {
            if (blockPredictionCallback is not null)
                EventManager.Instance.Unregister(blockPredictionCallback);
        }

        private void FixedUpdate()
        {
            // Don't build world until biomes are received and registered
            if (!World.BiomesInitialized || !client) return;

            // Build chunks in queue...
            Profiler.BeginSample("Start chunk render build(/light update) tasks");

            int newCount = 6; // Count quota for light updates

            // Start light update tasks
            while (newCount > 0 && lightUpdateRequests.Count > 0)
            {
                var chunkPos = lightUpdateRequests.First();
                lightUpdateRequests.Remove(chunkPos);

                //Debug.Log($"Queued light update for chunk [{chunkPos.x}, {chunkPos.y}, {chunkPos.z}]");

                Task.Run(() =>
                {
                    try
                    {
                        RecalculateBlockLight(chunkPos);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                });
                newCount--;
            }

            newCount = BUILD_COUNT_LIMIT - chunkRendersBeingBuilt.Count; // Count quota for chunk builds

            // Make sure no null element in hashset
            chunkRendersToBeBuiltAsSet.Remove(null);

            // Start chunk building tasks...
            while (newCount > 0 && chunkRendersToBeBuiltAsSet.Count > 0)
            {
                var nextChunk = chunkRendersToBeBuilt.Peek();
                var chunkPos = nextChunk.ChunkPos;

                // If lighting data of any neighbor of this chunk is dirty, don't build it
                if (   lightUpdateUnfinished.Contains(chunkPos)
                    || lightUpdateUnfinished.Contains(new(chunkPos.x - 1, chunkPos.y,     chunkPos.z    ))
                    || lightUpdateUnfinished.Contains(new(chunkPos.x + 1, chunkPos.y,     chunkPos.z    ))
                    || lightUpdateUnfinished.Contains(new(chunkPos.x,     chunkPos.y,     chunkPos.z - 1))
                    || lightUpdateUnfinished.Contains(new(chunkPos.x,     chunkPos.y,     chunkPos.z + 1))
                    || lightUpdateUnfinished.Contains(new(chunkPos.x,     chunkPos.y - 1, chunkPos.z    ))
                    || lightUpdateUnfinished.Contains(new(chunkPos.x,     chunkPos.y + 1, chunkPos.z    ))
                    )
                {
                    break; // We can't easily skip this chunk, so just stop building chunks for a while
                }

                chunkRendersToBeBuilt.Dequeue(); // Remove the one peeked

                if (!nextChunk || !GetChunkRenderColumn(nextChunk.ChunkX, nextChunk.ChunkZ))
                {
                    // Chunk is unloaded while waiting in the queue, ignore it...
                }
                else
                {
                    chunkRendersToBeBuiltAsSet.Remove(nextChunk);

                    Profiler.BeginSample("Build tasks");
                    BuildChunkRender(nextChunk);
                    Profiler.EndSample();
                    newCount--;
                }
            }
            Profiler.EndSample();

            // Check and queue new chunk rebuilds
            Profiler.BeginSample("Update chunks (Add)");
            UpdateChunkRendersListAdd();
            Profiler.EndSample();

            if (chunkRendersToBeBuiltAsSet.Count < BUILD_COUNT_LIMIT) // If CPU is not so busy
            {
                Profiler.BeginSample("Update chunks (Remove)");
                UpdateChunkRendersListRemove();
                Profiler.EndSample();
            }
        }

        private void Update()
        {
            if (!client) // Game is not ready, cancel update
                return;

            var playerBlockLoc = client.GetCurrentLocation().GetBlockLoc();
            int pcx = playerBlockLoc.GetChunkX();
            int pcz = playerBlockLoc.GetChunkZ();

            foreach (var (blockLoc, render) in blockEntityRenders)
            {
                // Call managed update
                render.ManagedUpdate(client.GetTickMilSec());

                // Update entities around the player
                float dist = (float) blockLoc.DistanceSquared(playerBlockLoc);
                bool inNearbyDict = nearbyBlockEntities.ContainsKey(blockLoc);

                if (dist < NEARBY_THRESHOLD_INNER) // Add entity to dictionary
                {
                    if (inNearbyDict)
                        nearbyBlockEntities[blockLoc] = dist;
                    else
                        nearbyBlockEntities.Add(blockLoc, dist);
                }
                else if (dist > NEARBY_THRESHOLD_OUTER) // Remove entity from dictionary
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

            if (lastPlayerBlockLoc != null && lastPlayerBlockLoc.Value != playerBlockLoc) // Updating block location, update terrain collider if necessary
            {
                RebuildTerrainBoxCollider(playerBlockLoc);
                // Update last block location only if it is changed
                lastPlayerBlockLoc = playerBlockLoc;
            }

            if (lastPlayerChunkLoc != null && (lastPlayerChunkLoc.Value.x != pcx || lastPlayerChunkLoc.Value.y != pcz)) // Updating chunk coordinate, update nearby chunk list if necessary
            {
                UpdateNearbyChunkCoordList(pcx, pcz);
                // Update last chunk coordinate only if it is changed
                lastPlayerChunkLoc = new(pcx, pcz);
            }
        }
        #endregion
    }
}