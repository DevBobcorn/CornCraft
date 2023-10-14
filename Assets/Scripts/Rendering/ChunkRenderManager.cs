#nullable enable
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using Unity.Mathematics;

using CraftSharp.Event;
using CraftSharp.Resource;

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

        /// <summary>
        /// All block entity renders in the current world
        /// </summary>
        private readonly Dictionary<BlockLoc, BlockEntityRender> blockEntityRenders = new();

        // Squared distance range of a entity to be considered as "near" the player
        private const float NEARBY_THERESHOLD_INNER =  81F; //  9 *  9
        private const float NEARBY_THERESHOLD_OUTER = 100F; // 10 * 10

        /// <summary>
        /// A dictionary storing entities near the player
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

        #region Chunk management
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
                var chunk = column.GetChunk(blockLoc);
                if (chunk is null)
                    column[blockLoc.GetChunkY()] = chunk = new Chunk();
                chunk[blockLoc.GetChunkBlockX(), blockLoc.GetChunkBlockY(), blockLoc.GetChunkBlockZ()] = block;
            }
            MarkDirtyAt(blockLoc);
        }

        public ChunkColumn? GetChunkColumn(BlockLoc location)
        {
            return world.GetChunkColumn(location);
        }

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

        public Biome GetBiome(BlockLoc blockLoc)
        {
            return world.GetBiome(blockLoc);
        }

        public byte GetBlockLight(BlockLoc blockLoc)
        {
            var column = GetChunkColumn(blockLoc);
            if (column != null)
                return column.GetBlockLight(blockLoc);
            
            return (byte) 0; // Not available
        }

        public bool IsLiquidAt(BlockLoc blockLoc)
        {
            var column = GetChunkColumn(blockLoc);
            if (column != null)
            {
                var chunk = column.GetChunk(blockLoc);
                if (chunk != null)
                    return chunk.GetBlock(blockLoc).State.InWater || chunk.GetBlock(blockLoc).State.InLava;
            }
            return false;
        }

        public bool IsOpaqueAt(BlockLoc blockLoc)
        {
            var column = GetChunkColumn(blockLoc);
            if (column != null)
                return column.GetIsOpaque(blockLoc);
            
            return false; // Not available
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

        public void AddBlockEntityRender(BlockLoc blockLoc, BlockEntityType blockEntityType, Dictionary<string, object> tags)
        {
            // If the location is occupied by a block entity already,
            // destroy this block entity first
            if (blockEntityRenders.ContainsKey(blockLoc))
            {
                blockEntityRenders[blockLoc]?.Unload();
                blockEntityRenders.Remove(blockLoc);
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
                blockEntityRender.Initialize(blockLoc, blockEntityType, tags);
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

        private void QueueChunkBuildIfNotEmpty(ChunkRender? chunkRender)
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
                var buildResult = builder!.Build(this, world, chunkData, chunkRender);

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
        /// Unload a chunk, invoked from network thread
        /// </summary>
        /// <param name="chunkX"></param>
        /// <param name="chunkZ"></param>
        public void UnloadChunk(int chunkX, int chunkZ)
        {
            world[chunkX, chunkZ] = null;

            int2 chunkCoord = new(chunkX, chunkZ);
            Loom.QueueOnMainThread(() => {
                if (renderColumns.ContainsKey(chunkCoord))
                {
                    renderColumns[chunkCoord].Unload(ref chunkRendersBeingBuilt, ref chunkRendersToBeBuilt);
                    renderColumns.Remove(chunkCoord);
                }
            });
        }

        public void ClearWorld()
        {
            world.Clear();
            chunkCnt = chunkLoadNotCompleted = 0;
        }

        public void ReloadWorldRender()
        {
            // Clear the queue first...
            chunkRendersToBeBuilt.Clear();

            // And cancel current chunk builds
            foreach (var chunkRender in chunkRendersBeingBuilt)
                chunkRender.TokenSource?.Cancel();
            
            chunkRendersBeingBuilt.Clear();

            // Clear all chunk columns in world
            var chunkCoords = renderColumns.Keys.ToArray();

            foreach (var chunkCoord in chunkCoords)
            {
                renderColumns[chunkCoord].Unload(ref chunkRendersBeingBuilt, ref chunkRendersToBeBuilt);
                renderColumns.Remove(chunkCoord);
            }

            // Unload all block renders in world
            foreach (var pair in blockEntityRenders)
            {
                pair.Value.Unload();
            }
            blockEntityRenders.Clear();

            renderColumns.Clear();
        }

        public void MarkDirtyAt(BlockLoc blockLoc)
        {
            int chunkX = blockLoc.GetChunkX(), chunkY = blockLoc.GetChunkY(), chunkZ = blockLoc.GetChunkZ();
            var column = GetChunkRenderColumn(chunkX, chunkZ, false);
            if (column is not null) // Queue this chunk to rebuild list...
            {   // Create the chunk render object if not present (previously empty)
                var chunk = column.GetChunkRender(chunkY, true);

                // Queue the chunk. Priority is left as 0(highest), so that changes can be seen instantly
                QueueChunkRenderBuild(chunk);

                if (blockLoc.GetChunkBlockY() == 0 && (chunkY - 1) >= 0) // In the bottom layer of this chunk
                {   // Queue the chunk below, if it isn't empty
                    QueueChunkBuildIfNotEmpty(column.GetChunkRender(chunkY - 1, false));
                }
                else if (blockLoc.GetChunkBlockY() == Chunk.SIZE - 1 && ((chunkY + 1) * Chunk.SIZE) < World.GetDimension().height) // In the top layer of this chunk
                {   // Queue the chunk above, if it isn't empty
                    QueueChunkBuildIfNotEmpty(column.GetChunkRender(chunkY + 1, false));
                }
            }

            if (blockLoc.GetChunkBlockX() == 0) // Check MC X direction neighbors
                QueueChunkBuildIfNotEmpty(GetChunkRender(chunkX - 1, chunkY, chunkZ));
            else if (blockLoc.GetChunkBlockX() == Chunk.SIZE - 1)
                QueueChunkBuildIfNotEmpty(GetChunkRender(chunkX + 1, chunkY, chunkZ));

            if (blockLoc.GetChunkBlockZ() == 0) // Check MC Z direction neighbors
                QueueChunkBuildIfNotEmpty(GetChunkRender(chunkX, chunkY, chunkZ - 1));
            else if (blockLoc.GetChunkBlockZ() == Chunk.SIZE - 1)
                QueueChunkBuildIfNotEmpty(GetChunkRender(chunkX, chunkY, chunkZ + 1));
            
            if (blockLoc.DistanceSquared(client!.GetLocation().GetBlockLoc()) <= ChunkRenderBuilder.MOVEMENT_RADIUS_SQR)
                terrainColliderDirty = true; // Terrain collider needs to be updated
        }

        public void RebuildTerrainCollider(BlockLoc playerBlockLoc)
        {
            terrainColliderDirty = false;
            Task.Factory.StartNew(() => builder!.BuildTerrainCollider(world, playerBlockLoc, movementCollider!, liquidCollider!));
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

            // Update terrain collider if necessary
            var playerBlockLoc = client?.GetLocation().GetBlockLoc();
            if (playerBlockLoc is not null)
            {
                if (lastPlayerBlockLoc is not null)
                {
                    if (terrainColliderDirty || lastPlayerBlockLoc.Value != playerBlockLoc.Value)
                    {
                        RebuildTerrainCollider(playerBlockLoc.Value);
                        // Update player liquid state
                        var inLiquid = GetBlock(playerBlockLoc.Value).State.InLiquid;
                        var prevInLiquid = GetBlock(lastPlayerBlockLoc.Value).State.InLiquid;
                        if (prevInLiquid != inLiquid) // Player liquid state changed, broadcast this change
                        {
                            EventManager.Instance.Broadcast(new PlayerLiquidEvent(inLiquid));
                        }
                        // Update last location only if it is used
                        lastPlayerBlockLoc = playerBlockLoc;
                    }
                }
                else
                {
                    RebuildTerrainCollider(playerBlockLoc.Value);
                    // Update player liquid state
                    var inLiquid = GetBlock(playerBlockLoc.Value).State.InLiquid;
                    if (inLiquid) // Player liquid state changed, broadcast this change
                    {
                        EventManager.Instance.Broadcast(new PlayerLiquidEvent(true));
                        Debug.Log($"Enter water at {playerBlockLoc}");
                    }
                    // Update last location
                    lastPlayerBlockLoc = playerBlockLoc;
                }
            }
        }

        void Update()
        {
            var client = CornApp.CurrentClient;

            if (client is null) // Game is not ready, cancel update
                return;

            foreach (var pair in blockEntityRenders)
            {
                var blockLoc = pair.Key;
                var render = pair.Value;

                // Call managed update
                render.ManagedUpdate(client!.GetTickMilSec());

                // Update entities around the player
                float dist = (float) blockLoc.ToCenterLocation().DistanceSquared(client.GetLocation());
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
        }
        #endregion
    }
}