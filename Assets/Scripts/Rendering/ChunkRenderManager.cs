#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using Unity.Mathematics;

using CraftSharp.Event;
using CraftSharp.Resource;
using UnityEngine.InputSystem;

namespace CraftSharp.Rendering
{
    public class ChunkRenderManager : MonoBehaviour
    {
        public const string MOVEMENT_LAYER_NAME = "Movement";
        public const string SOLID_LAYER_NAME = "Solid";
        public const string LIQUID_SURFACE_LAYER_NAME = "LiquidSurface";

        [SerializeField] private Transform? blockEntityParent;

        #region GameObject Prefabs for each block entity type
        [SerializeField] private GameObject? defaultPrefab;
        #endregion

        private readonly Dictionary<ResourceLocation, GameObject?> blockEntityPrefabs = new();
        private GameObject? GetPrefabForType(ResourceLocation type) => blockEntityPrefabs.GetValueOrDefault(type, defaultPrefab);

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
        public readonly ConcurrentDictionary<int2, Queue<byte>> LightDataCache = new();

        /// <summary>
        /// Chunk data parsing progress
        /// </summary>
        public int chunkCnt = 0;
        public int chunkLoadNotCompleted = 0;

        /// <summary>
        /// Unity thread access only
        /// </summary>
        private readonly Dictionary<int2, ChunkRenderColumn> renderColumns = new();

        /// <summary>
        /// Unity thread access only
        /// </summary>
        private PriorityQueue<ChunkRender> chunkRendersToBeBuilt = new();
        private List<ChunkRender> chunkRendersBeingBuilt = new();

        private BaseCornClient? client;
        private ChunkRenderBuilder? builder;

        // Terrain collider for movement
        private MeshCollider? movementCollider, liquidCollider;

        public void SetClient(BaseCornClient client) => this.client = client;

        public string GetDebugInfo()
        {
            return $"Queued Chunks: {chunkRendersToBeBuilt.Count}\nBuilding Chunks: {chunkRendersBeingBuilt.Count}\nBlock Entity Count: {blockEntityRenders.Count}";
        }

        #region Chunk render access
        private ChunkRenderColumn CreateChunkRenderColumn(int chunkX, int chunkZ)
        {
            // Create this chunk column...
            GameObject columnObj = new GameObject($"Column [{chunkX}, {chunkZ}]");
            ChunkRenderColumn newColumn = columnObj.AddComponent<ChunkRenderColumn>();
            newColumn.ChunkX = chunkX;
            newColumn.ChunkZ = chunkZ;
            // Set its parent to this world...
            columnObj.transform.parent = this.transform;
            columnObj.transform.localPosition = Vector3.zero;

            return newColumn;
        }

        private ChunkRenderColumn? GetChunkRenderColumn(int chunkX, int chunkZ, bool createIfEmpty)
        {
            int2 chunkCoord = new(chunkX, chunkZ);
            if (renderColumns.ContainsKey(chunkCoord))
                return renderColumns[chunkCoord];

            if (createIfEmpty)
            {
                ChunkRenderColumn newColumn = CreateChunkRenderColumn(chunkX, chunkZ);
                renderColumns.Add(chunkCoord, newColumn);
                return newColumn;
            }

            return null;
        }

        public ChunkRender? GetChunkRender(int chunkX, int chunkY, int chunkZ)
        {
            return GetChunkRenderColumn(chunkX, chunkZ, false)?.GetChunkRender(chunkY, false);
        }

        public bool IsChunkRenderReady(int chunkX, int chunkY, int chunkZ)
        {
            var column = GetChunkRenderColumn(chunkX, chunkZ, false);

            if (column is null)
                return false;
            
            var chunk = column.GetChunkRender(chunkY, false);
            
            // Empty chunks (air) are null, those chunks are always ready
            return chunk is null || chunk.State == ChunkBuildState.Ready;
        }
        #endregion

        #region Chunk data access

        /// <summary>
        /// Store a chunk, invoked from network thread
        /// </summary>
        public void StoreChunk(int chunkX, int chunkY, int chunkZ, int chunkColumnSize, Chunk? chunk)
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
            if (column is not null)
            {
                // Update chunk data
                var chunk = column.GetChunk(blockLoc);
                if (chunk is null)
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
        public ChunkColumn? GetChunkColumn(BlockLoc blockLoc)
        {
            return world.GetChunkColumn(blockLoc);
        }

        /// <summary>
        /// Get a chunk column from the world
        /// </summary>
        public ChunkColumn? GetChunkColumn(int chunkX, int chunkZ)
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
        public BlockEntityRender? GetBlockEntityRender(BlockLoc blockLoc)
        {
            if (blockEntityRenders.ContainsKey(blockLoc))
                return blockEntityRenders[blockLoc];
            
            return null;
        }

        /// <summary>
        /// Add a new block entity render to the world
        /// </summary>
        /// <param name="tags">Pass in null if auto-creating this block entity</param>
        public void AddBlockEntityRender(BlockLoc blockLoc, BlockEntityType blockEntityType, Dictionary<string, object>? tags = null)
        {
            // If the location is occupied by a block entity already
            if (blockEntityRenders.ContainsKey(blockLoc))
            {
                var prevBlockEntity = blockEntityRenders[blockLoc];
                if (prevBlockEntity.Type == blockEntityType) // Auto-created, keep it but replace data tags
                {
                    if (tag is null) // Auto-creating a block entity while it is already created
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

            GameObject? blockEntityPrefab = GetPrefabForType(blockEntityType.BlockEntityId);

            if (blockEntityPrefab is not null)
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
        private const int CHUNK_CENTER = Chunk.SIZE / 2 + 1;
        private const int OPERATION_CYCLE_LENGTH = 64;

        private void UpdateBuildPriority(Location currentBlockLoc, ChunkRender chunk, int offsetY)
        {   // Get this chunk's build priority based on its current distance to the player,
            // a smaller value means a higher priority...
            chunk.Priority = (int)(
                    new Location(chunk.ChunkX * Chunk.SIZE + CHUNK_CENTER, chunk.ChunkY * Chunk.SIZE + CHUNK_CENTER + offsetY,
                            chunk.ChunkZ * Chunk.SIZE + CHUNK_CENTER).DistanceTo(currentBlockLoc) / 16);
        }

        private void UpdateChunkRendersListAdd()
        {
            var playerLoc = client!.GetLocation();
            var blockLoc = playerLoc.GetBlockLoc();
            ChunkRenderColumn columnRender;

            int viewDist = CornGlobal.MCSettings.RenderDistance;
            int viewDistSqr = viewDist * viewDist;

            int chunkColumnSize = (World.GetDimension().height + Chunk.SIZE - 1) / Chunk.SIZE; // Round up
            int offsetY = World.GetDimension().minY;

            // Add nearby chunks
            for (int cx = -viewDist;cx <= viewDist;cx++)
                for (int cz = -viewDist;cz <= viewDist;cz++)
                {
                    if (cx * cx + cz * cz >= viewDistSqr) continue;

                    int chunkX = blockLoc.GetChunkX() + cx, chunkZ = blockLoc.GetChunkZ() + cz;
                    
                    if (world.IsChunkColumnReady(chunkX, chunkZ))
                    {
                        var column = GetChunkRenderColumn(chunkX, chunkZ, false);
                        if (column is null)
                        {   // Chunks data is ready, but chunk render column is not
                            //int chunkMask = world[chunkX, chunkZ]!.ChunkMask;
                            // Create it and add the whole column to render list...
                            columnRender = GetChunkRenderColumn(chunkX, chunkZ, true)!;
                            for (int chunkY = 0;chunkY < chunkColumnSize;chunkY++)
                            {   // Create chunk renders and queue them...
                                if (!world[chunkX, chunkZ]!.ChunkIsEmpty(chunkY))
                                {   // This chunk is not empty and needs to be added and queued
                                    var chunk = columnRender.GetChunkRender(chunkY, true);
                                    UpdateBuildPriority(playerLoc, chunk, offsetY);
                                    QueueChunkRenderBuild(chunk);
                                }
                            }
                        }
                        else
                        {
                            foreach (var chunk in column.GetChunkRenders().Values)
                            {
                                //if (chunk.State == ChunkBuildState.Delayed || chunk.State == ChunkBuildState.Cancelled)
                                if (chunk.State == ChunkBuildState.Delayed)
                                {   // Queue delayed or cancelled chunk builds...
                                    UpdateBuildPriority(playerLoc, chunk, offsetY);
                                    QueueChunkRenderBuild(chunk);
                                }
                            }
                        }
                    }
                }

        }

        private void UpdateChunkRendersListRemove()
        {
            // Add nearby chunks
            var blockLoc   = client!.GetLocation().GetBlockLoc();
            int unloadDist = Mathf.RoundToInt(CornGlobal.MCSettings.RenderDistance * 2F);

            var chunkCoords = renderColumns.Keys.ToArray();

            foreach (var chunkCoord in chunkCoords)
            {
                if (Mathf.Abs(blockLoc.GetChunkX() - chunkCoord.x) > unloadDist || Mathf.Abs(blockLoc.GetChunkZ() - chunkCoord.y) > unloadDist)
                {
                    renderColumns[chunkCoord].Unload(ref chunkRendersBeingBuilt, ref chunkRendersToBeBuilt);
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

        private void QueueChunkRenderBuildIfNotEmpty(ChunkRender? chunkRender)
        {
            if (chunkRender is not null) // Not empty(air) chunk
                QueueChunkRenderBuild(chunkRender);
        }

        public void BuildChunkRender(ChunkRender chunkRender)
        {
            int chunkX = chunkRender.ChunkX, chunkZ = chunkRender.ChunkZ;
            var chunkColumnData = GetChunkColumn(chunkX, chunkZ);

            if (chunkColumnData is null) // Chunk column data unloaded, cancel
            {
                int2 chunkCoord = new(chunkRender.ChunkX, chunkRender.ChunkZ);
                if (renderColumns.ContainsKey(chunkCoord))
                {
                    renderColumns[chunkCoord].Unload(ref chunkRendersBeingBuilt, ref chunkRendersToBeBuilt);
                    renderColumns.Remove(chunkCoord, out _);
                }
                chunkRender.State = ChunkBuildState.Cancelled;
                return;
            }

            var chunkData = chunkColumnData[chunkRender.ChunkY];

            if (chunkData is null) // Chunk not available or is empty(air chunk), cancel
            {
                chunkRender.State = ChunkBuildState.Cancelled;
                return;
            }

            if (!(  world.IsChunkColumnReady(chunkX, chunkZ - 1) && // ZNeg neighbor present
                    world.IsChunkColumnReady(chunkX, chunkZ + 1) && // ZPos neighbor present
                    world.IsChunkColumnReady(chunkX - 1, chunkZ) && // XNeg neighbor present
                    world.IsChunkColumnReady(chunkX + 1, chunkZ) )) // XPos neighbor present
            {
                chunkRender.State = ChunkBuildState.Delayed;
                return; // Not all neighbor data ready, delay it
            }

            chunkRendersBeingBuilt.Add(chunkRender);
            chunkRender.State = ChunkBuildState.Building;

            chunkRender.TokenSource = new();
            
            Task.Factory.StartNew(() => {
                var buildResult = builder!.Build(world, chunkData, chunkRender);

                Loom.QueueOnMainThread(() => {
                    if (chunkRender is not null)
                    {
                        if (buildResult == ChunkBuildResult.Cancelled)
                            chunkRender.State = ChunkBuildState.Cancelled;
                        
                        chunkRendersBeingBuilt.Remove(chunkRender);
                    }
                });
            }, chunkRender.TokenSource.Token);
        }

        public const int BUILD_COUNT_LIMIT = 4;
        private int operationCode = 0;
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
                    renderColumns[chunkCoord].Unload(ref chunkRendersBeingBuilt, ref chunkRendersToBeBuilt);
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
            chunkCnt = chunkLoadNotCompleted = 0;
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
                renderColumns[chunkCoord].Unload(ref chunkRendersBeingBuilt, ref chunkRendersToBeBuilt);
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

            if (column is not null) // Queue this chunk to rebuild list...
            {
                // Create the chunk render object if not present (previously empty)
                var chunk = column.GetChunkRender(chunkY, true);

                // Queue the chunk. Priority is left as 0(highest), so that changes can be seen instantly
                QueueChunkRenderBuild(chunk);

                if (blockLoc.GetChunkBlockY() == 0 && (chunkY - 1) >= 0) // In the bottom layer of this chunk
                {   // Queue the chunk below, if it isn't empty
                    QueueChunkRenderBuildIfNotEmpty(column.GetChunkRender(chunkY - 1, false));
                }
                else if (blockLoc.GetChunkBlockY() == Chunk.SIZE - 1 && ((chunkY + 1) * Chunk.SIZE) < World.GetDimension().height) // In the top layer of this chunk
                {   // Queue the chunk above, if it isn't empty
                    QueueChunkRenderBuildIfNotEmpty(column.GetChunkRender(chunkY + 1, false));
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
            if (column is not null) // Queue this chunk to rebuild list...
            {   // Create the chunk render object if not present (previously empty)
                var chunk = column.GetChunkRender(chunkY, true);

                // Queue the chunk. Priority is left as 0(highest), so that changes can be seen instantly
                QueueChunkRenderBuild(chunk);
            }
        }

        public void InitializeTerrainCollider(BlockLoc playerBlockLoc, Action? callback = null)
        {
            terrainColliderDirty = false;

            Task.Factory.StartNew(async () =>
            {
                // Wait for old data to be cleared up
                await Task.Delay(100);

                int chunkX = playerBlockLoc.GetChunkX();
                int chunkZ = playerBlockLoc.GetChunkZ();

                int delayCount = 50; // Max delay time to stop waiting forever
                
                while (!world.IsChunkColumnReady(chunkX, chunkZ) && delayCount > 0)
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

            Task.Factory.StartNew(() =>
            {
                builder!.BuildTerrainCollider(world, playerBlockLoc, movementCollider!, liquidCollider!, null);
            });
        }

        void Start()
        {
            client = CornApp.CurrentClient;

            var modelTable = ResourcePackManager.Instance.StateModelTable;
            builder = new(modelTable);

            var movementColliderObj = new GameObject("Movement Collider");
            movementColliderObj.layer = LayerMask.NameToLayer(MOVEMENT_LAYER_NAME);
            movementCollider = movementColliderObj.AddComponent<MeshCollider>();

            var liquidColliderObj = new GameObject("Liquid Collider");
            liquidColliderObj.layer = LayerMask.NameToLayer(LIQUID_SURFACE_LAYER_NAME);
            liquidCollider = liquidColliderObj.AddComponent<MeshCollider>();
        }

        void FixedUpdate()
        {
            // Don't build world until biomes are received and registered
            if (!World.BiomesInitialized) return;

            int newCount = BUILD_COUNT_LIMIT - chunkRendersBeingBuilt.Count;

            // Build chunks in queue...
            if (newCount > 0)
            {
                // Start chunk building tasks...
                while (chunkRendersToBeBuilt.Count > 0 && newCount > 0)
                {
                    var nextChunk = chunkRendersToBeBuilt.Dequeue();

                    if (nextChunk is null || GetChunkRenderColumn(nextChunk.ChunkX, nextChunk.ChunkZ, false) is null)
                    {   // Chunk is unloaded while waiting in the queue, ignore it...
                        continue;
                    }
                    else
                    {
                        BuildChunkRender(nextChunk);
                        newCount--;
                    }
                }
            }

            if (operationCode == 8)
                UpdateChunkRendersListAdd();
            else if (operationCode == 16)
                UpdateChunkRendersListRemove();
            
            operationCode = (operationCode + 1) % OPERATION_CYCLE_LENGTH;
        }

        void Update()
        {
            var client = CornApp.CurrentClient;

            if (client is null) // Game is not ready, cancel update
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

            if (lastPlayerBlockLoc is not null) // Updating location, update terrain collider if necessary
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

            if (Keyboard.current.qKey.wasPressedThisFrame) // Debug function, reload chunk renders
            {
                CornApp.Notify(Translations.Get("rendering.debug.reload_chunk_render"));
                // Don't destroy block entity renders
                ReloadChunksRender(false);
            }
        }
        #endregion
    }
}