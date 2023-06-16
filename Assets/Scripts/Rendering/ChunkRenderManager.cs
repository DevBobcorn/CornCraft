#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;

using MinecraftClient.Event;
using MinecraftClient.Mapping;
using MinecraftClient.Resource;

namespace MinecraftClient.Rendering
{
    public class ChunkRenderManager : MonoBehaviour
    {
        public const string MOVEMENT_LAYER_NAME = "Movement";
        public const string OBSTACLE_LAYER_NAME = "Obstacle";
        public const string LIQUID_LAYER_NAME   = "Liquid";

        private Dictionary<int2, ChunkRenderColumn> columns = new();

        // Both manipulated on Unity main thread only
        private PriorityQueue<ChunkRender> chunkRendersToBeBuilt = new();
        private List<ChunkRender> chunksBeingBuilt = new();

        private CornClient? client;
        private World? world;
        private ChunkRenderBuilder? builder;

        // Terrain collider for movement
        private MeshCollider? movementCollider, liquidCollider;

        public void SetClient(CornClient client) => this.client = client;

        public string GetDebugInfo() => $"QueC: {chunkRendersToBeBuilt.Count}\t BldC: {chunksBeingBuilt.Count}";

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
            if (columns.ContainsKey(chunkCoord))
                return columns[chunkCoord];

            if (createIfEmpty)
            {
                ChunkRenderColumn newColumn = CreateChunkRenderColumn(chunkX, chunkZ);
                columns.Add(chunkCoord, newColumn);
                return newColumn;
            }

            return null;
        }

        public ChunkRender? GetChunkRender(int chunkX, int chunkY, int chunkZ)
        {
            return GetChunkRenderColumn(chunkX, chunkZ, false)?.GetChunkRender(chunkY, false);
        }

        public bool IsChunkRenderColumnReady(int chunkX, int chunkZ)
        {
            var column = GetChunkRenderColumn(chunkX, chunkZ, false);

            if (column is null)
                return false;
            
            return column.IsReady();
        }
        #endregion

        #region Chunk building
        public void BuildChunkRender(ChunkRender chunkRender)
        {
            if (world is null) return;

            int chunkX = chunkRender.ChunkX, chunkZ = chunkRender.ChunkZ;
            var chunkColumnData = world[chunkRender.ChunkX, chunkRender.ChunkZ];

            if (chunkColumnData is null) // Chunk column data unloaded, cancel
            {
                int2 chunkCoord = new(chunkRender.ChunkX, chunkRender.ChunkZ);
                if (columns.ContainsKey(chunkCoord))
                {
                    columns[chunkCoord].Unload(ref chunksBeingBuilt, ref chunkRendersToBeBuilt);
                    columns.Remove(chunkCoord);
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

            if (!(  world.isChunkColumnReady(chunkX, chunkZ - 1) && // ZNeg neighbor present
                    world.isChunkColumnReady(chunkX, chunkZ + 1) && // ZPos neighbor present
                    world.isChunkColumnReady(chunkX - 1, chunkZ) && // XNeg neighbor present
                    world.isChunkColumnReady(chunkX + 1, chunkZ) )) // XPos neighbor present
            {
                chunkRender.State = ChunkBuildState.Delayed;
                return; // Not all neighbor data ready, delay it
            }

            chunksBeingBuilt.Add(chunkRender);
            chunkRender.State = ChunkBuildState.Building;

            chunkRender.TokenSource = new();
            
            Task.Factory.StartNew(() => {
                var buildResult = builder!.Build(world, chunkData, chunkRender);

                Loom.QueueOnMainThread(() => {
                    if (chunkRender is not null)
                    {
                        if (buildResult == ChunkBuildResult.Cancelled)
                            chunkRender.State = ChunkBuildState.Cancelled;
                        
                        chunksBeingBuilt.Remove(chunkRender);
                    }
                });
            }, chunkRender.TokenSource.Token);
        }
        #endregion

        #region Chunk updates
        Action<ReceiveChunkColumnEvent>? columnCallBack1;
        Action<UnloadChunkColumnEvent>? columnCallBack2;
        Action<BlockUpdateEvent>? blockCallBack;
        Action<BlocksUpdateEvent>? blocksCallBack;

        private const int ChunkCenterX = Chunk.SizeX / 2 + 1;
        private const int ChunkCenterY = Chunk.SizeY / 2 + 1;
        private const int ChunkCenterZ = Chunk.SizeZ / 2 + 1;
        private const int OPERATION_CYCLE_LENGTH = 64;

        private void UpdateBuildPriority(Location currentLocation, ChunkRender chunk, int offsetY)
        {   // Get this chunk's build priority based on its current distance to the player,
            // a smaller value means a higher priority...
            chunk.Priority = (int)(
                    new Location(chunk.ChunkX * Chunk.SizeX + ChunkCenterX, chunk.ChunkY * Chunk.SizeY + ChunkCenterY + offsetY,
                            chunk.ChunkZ * Chunk.SizeZ + ChunkCenterZ).DistanceTo(currentLocation) / 16);
        }

        public void UpdateChunkRendersListAdd()
        {
            var world = client?.GetWorld();
            if (world is null) return;

            var location = client!.GetLocation();
            ChunkRenderColumn columnRender;

            int viewDist = CornGlobal.MCSettings.RenderDistance;
            int viewDistSqr = viewDist * viewDist;

            int chunkColumnSize = (World.GetDimension().height + Chunk.SizeY - 1) / Chunk.SizeY; // Round up
            int offsetY = World.GetDimension().minY;

            // Add nearby chunks
            for (int cx = -viewDist;cx <= viewDist;cx++)
                for (int cz = -viewDist;cz <= viewDist;cz++)
                {
                    if (cx * cx + cz * cz >= viewDistSqr) continue;

                    int chunkX = location.GetChunkX() + cx, chunkZ = location.GetChunkZ() + cz;
                    
                    if (world.isChunkColumnReady(chunkX, chunkZ))
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
                                    UpdateBuildPriority(location, chunk, offsetY);
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
                                    UpdateBuildPriority(location, chunk, offsetY);
                                    QueueChunkRenderBuild(chunk);
                                }
                            }
                        }
                    }
                }

        }

        public void UpdateChunkRendersListRemove()
        {
            var world = client?.GetWorld();
            if (world is null) return;

            // Add nearby chunks
            var location   = client!.GetLocation();
            int unloadDist = Mathf.RoundToInt(CornGlobal.MCSettings.RenderDistance * 2F);

            var chunkCoords = columns.Keys.ToArray();

            foreach (var chunkCoord in chunkCoords)
            {
                if (Mathf.Abs(location.GetChunkX() - chunkCoord.x) > unloadDist || Mathf.Abs(location.GetChunkZ() - chunkCoord.y) > unloadDist)
                {
                    columns[chunkCoord].Unload(ref chunksBeingBuilt, ref chunkRendersToBeBuilt);
                    columns.Remove(chunkCoord);
                }

            }

        }

        public void QueueChunkRenderBuild(ChunkRender chunkRender)
        {
            if (!chunkRendersToBeBuilt.Contains(chunkRender))
            {
                chunkRendersToBeBuilt.Enqueue(chunkRender);
                chunkRender.State = ChunkBuildState.Pending;
            }

        }

        public void QueueChunkBuildIfNotEmpty(ChunkRender? chunkRender)
        {
            if (chunkRender is not null) // Not empty(air) chunk
                QueueChunkRenderBuild(chunkRender);
        }

        public void ReloadWorld()
        {
            // Clear the queue first...
            chunkRendersToBeBuilt.Clear();

            // And cancel current chunk builds
            foreach (var chunk in chunksBeingBuilt)
                chunk.TokenSource?.Cancel();
            
            chunksBeingBuilt.Clear();

            // Clear all chunk columns in world
            var chunkCoords = columns.Keys.ToArray();

            foreach (var chunkCoord in chunkCoords)
            {
                columns[chunkCoord].Unload(ref chunksBeingBuilt, ref chunkRendersToBeBuilt);
                columns.Remove(chunkCoord);
            }

            columns.Clear();
        }

        public const int BUILD_COUNT_LIMIT = 4;
        private int operationCode    = 0;

        private static readonly Block AIR_INSTANCE = new Block(0);
        private Location? lastPlayerLoc = null;
        private bool terrainColliderDirty = true;

        void Start()
        {
            client = CornApp.CurrentClient;
            world = client?.GetWorld();

            var modelTable = ResourcePackManager.Instance.StateModelTable;
            builder = new(modelTable);

            var movementColliderObj = new GameObject("Movement Collider");
            movementColliderObj.layer = LayerMask.NameToLayer(MOVEMENT_LAYER_NAME);
            movementCollider = movementColliderObj.AddComponent<MeshCollider>();

            var liquidColliderObj = new GameObject("Liquid Collider");
            liquidColliderObj.layer = LayerMask.NameToLayer(LIQUID_LAYER_NAME);
            liquidCollider = liquidColliderObj.AddComponent<MeshCollider>();

            // Register event callbacks
            EventManager.Instance.Register(columnCallBack1 = (e) => { });

            EventManager.Instance.Register(columnCallBack2 = (e) => {
                int2 chunkCoord = new(e.ChunkX, e.ChunkZ);
                if (columns.ContainsKey(chunkCoord))
                {
                    columns[chunkCoord].Unload(ref chunksBeingBuilt, ref chunkRendersToBeBuilt);
                    columns.Remove(chunkCoord);
                }
            });

            EventManager.Instance.Register(blockCallBack = (e) => {
                UpdateBlockAt(e.Location);
            });

            EventManager.Instance.Register(blocksCallBack = (e) => {
                foreach (var loc in e.Locations)
                    UpdateBlockAt(loc);
            });
        }

        private void UpdateBlockAt(Location loc)
        {
            int chunkX = loc.GetChunkX(), chunkY = loc.GetChunkY(), chunkZ = loc.GetChunkZ();
            var column = GetChunkRenderColumn(loc.GetChunkX(), loc.GetChunkZ(), false);
            if (column is not null) // Queue this chunk to rebuild list...
            {   // Create the chunk render object if not present (previously empty)
                var chunk = column.GetChunkRender(chunkY, true);

                // Queue the chunk. Priority is left as 0(highest), so that changes can be seen instantly
                QueueChunkRenderBuild(chunk);

                if (loc.GetChunkBlockY() == 0 && (chunkY - 1) >= 0) // In the bottom layer of this chunk
                {   // Queue the chunk below, if it isn't empty
                    QueueChunkBuildIfNotEmpty(column.GetChunkRender(chunkY - 1, false));
                }
                else if (loc.GetChunkBlockY() == Chunk.SizeY - 1 && ((chunkY + 1) * Chunk.SizeY) < World.GetDimension().height) // In the top layer of this chunk
                {   // Queue the chunk above, if it isn't empty
                    QueueChunkBuildIfNotEmpty(column.GetChunkRender(chunkY + 1, false));
                }
            }

            if (loc.GetChunkBlockX() == 0) // Check MC X direction neighbors
                QueueChunkBuildIfNotEmpty(GetChunkRender(chunkX - 1, chunkY, chunkZ));
            else if (loc.GetChunkBlockX() == Chunk.SizeX - 1)
                QueueChunkBuildIfNotEmpty(GetChunkRender(chunkX + 1, chunkY, chunkZ));

            if (loc.GetChunkBlockZ() == 0) // Check MC Z direction neighbors
                QueueChunkBuildIfNotEmpty(GetChunkRender(chunkX, chunkY, chunkZ - 1));
            else if (loc.GetChunkBlockZ() == Chunk.SizeZ - 1)
                QueueChunkBuildIfNotEmpty(GetChunkRender(chunkX, chunkY, chunkZ + 1));
            
            if (loc.DistanceSquared(client!.GetLocation()) <= ChunkRenderBuilder.MOVEMENT_RADIUS_SQR)
                terrainColliderDirty = true; // Terrain collider needs to be updated
        }

        public void RebuildTerrainCollider(Location playerLoc)
        {
            terrainColliderDirty = false;
            Task.Factory.StartNew(() => builder!.BuildTerrainCollider(client!.GetWorld(), playerLoc, movementCollider!, liquidCollider!));
            lastPlayerLoc = playerLoc;
        }

        void FixedUpdate()
        {
            int newCount = BUILD_COUNT_LIMIT - chunksBeingBuilt.Count;

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
            var playerLoc = client?.GetLocation().ToFloor();
            if (playerLoc != null && (terrainColliderDirty || lastPlayerLoc != playerLoc))
                RebuildTerrainCollider(playerLoc.Value);
        }
        #endregion

    }

}