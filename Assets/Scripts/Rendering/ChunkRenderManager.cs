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

        private static ChunkRenderManager? instance;
        public static ChunkRenderManager Instance
        {
            get {
                if (instance is null)
                {
                    instance = Component.FindObjectOfType<ChunkRenderManager>();
                }
                
                return instance;
            }
        }

        private Dictionary<int2, ChunkRenderColumn> columns = new();

        // Both manipulated on Unity main thread only
        private PriorityQueue<ChunkRender> chunkRendersToBeBuilt = new();
        private List<ChunkRender> chunksBeingBuilt = new();

        private CornClient? game;
        private World? world;
        private ChunkRenderBuilder? builder;

        // Terrain collider for movement
        private MeshCollider? movementCollider, liquidCollider;

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
            if (world is null)
                return;

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

            if (chunkData is null) // Chunk not available, delay
            {
                chunkRender.State = ChunkBuildState.Delayed;
                return;
            }

            // Save neighbors' status(present or not) right before mesh building
            chunkRender.UpdateNeighborStatus();

            if (!chunkRender.AllNeighborDataPresent)
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

        private void UpdateBuildPriority(Location currentLocation, ChunkRender chunk, int offsetY)
        {   // Get this chunk's build priority based on its current distance to the player,
            // a smaller value means a higher priority...
            chunk.Priority = (int)(
                    new Location(chunk.ChunkX * Chunk.SizeX + ChunkCenterX, chunk.ChunkY * Chunk.SizeY + ChunkCenterY + offsetY,
                            chunk.ChunkZ * Chunk.SizeZ + ChunkCenterZ).DistanceTo(currentLocation) / 16);
        }

        // Add new chunks into render list
        public void UpdateChunkRendersListAdd()
        {
            World world = game!.GetWorld();
            if (world is null) return;
            var location = game!.GetCurrentLocation();
            ChunkRenderColumn columnRender;

            int viewDist = CornCraft.MCSettings_RenderDistance;
            int viewDistSqr = viewDist * viewDist;

            int chunkColumnSize = (World.GetDimension().height + Chunk.SizeY - 1) / Chunk.SizeY; // Round up
            int offsetY = World.GetDimension().minY;

            // Add nearby chunks
            for (int cx = -viewDist;cx <= viewDist;cx++)
                for (int cz = -viewDist;cz <= viewDist;cz++)
                {
                    if (cx * cx + cz * cz >= viewDistSqr)
                        continue;

                    int chunkX = location.ChunkX + cx, chunkZ = location.ChunkZ + cz;
                    
                    if (world.isChunkColumnReady(chunkX, chunkZ))
                    {
                        var column = GetChunkRenderColumn(chunkX, chunkZ, false);
                        if (column is null)
                        {   // Chunks data is ready, but chunk render column is not
                            int chunkMask = world[chunkX, chunkZ]!.ChunkMask;
                            // Create it and add the whole column to render list...
                            columnRender = GetChunkRenderColumn(chunkX, chunkZ, true)!;
                            for (int chunkY = 0;chunkY < chunkColumnSize;chunkY++)
                            {   // Create chunk renders and queue them...
                                if ((chunkMask & (1 << chunkY)) != 0)
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
                                if (chunk.State == ChunkBuildState.Delayed || chunk.State == ChunkBuildState.Cancelled)
                                {   // Queue delayed or cancelled chunk builds...
                                    UpdateBuildPriority(location, chunk, offsetY);
                                    QueueChunkRenderBuild(chunk);
                                }
                            }
                        }
                    }
                }

        }

        // Remove far chunks from render list
        public void UpdateChunkRendersListRemove()
        {
            World world = game!.GetWorld();
            if (world is null) return;

            // Add nearby chunks
            var location   = game.GetCurrentLocation();
            int unloadDist = Mathf.RoundToInt(CornCraft.MCSettings_RenderDistance * 2F);

            var chunkCoords = columns.Keys.ToArray();

            foreach (var chunkCoord in chunkCoords)
            {
                if (Mathf.Abs(location.ChunkX - chunkCoord.x) > unloadDist || Mathf.Abs(location.ChunkZ - chunkCoord.y) > unloadDist)
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

        public void UnloadWorld()
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
                columns.Remove(chunkCoord);

            columns.Clear();

            // Unregister callbacks
            if (columnCallBack1 is not null)
            {
                EventManager.Instance.Unregister(columnCallBack1);
                columnCallBack1 = null;
            }
            
            if (columnCallBack2 is not null)
            {
                EventManager.Instance.Unregister(columnCallBack2);
                columnCallBack2 = null;
            }

            if (blockCallBack is not null)
            {
                EventManager.Instance.Unregister(blockCallBack);
                blockCallBack = null;
            }
            
            if (blocksCallBack is not null)
            {
                EventManager.Instance.Unregister(blocksCallBack);
                blocksCallBack = null;
            }

            // Reset instance
            instance = null;

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
        private float operationCooldown  = 0;
        private int   operationAction    = 0;

        private static readonly Block AIR_INSTANCE = new Block(0);
        public const int MOVEMENT_RADIUS = 3;
        public const int MOVEMENT_RADIUS_SQR = MOVEMENT_RADIUS * MOVEMENT_RADIUS;

        private Location? lastPlayerLoc = null;
        private bool terrainColliderDirty = true;

        // For player movement, it is not favorable to use per-chunk mesh colliders
        // because player can get stuck on the edge of chunks due to a bug of Unity
        // (or say PhysX) bug, so we dynamically build the mesh collider around the
        // player as a solution to this. See the problem discussion at
        // https://forum.unity.com/threads/ball-rolling-on-mesh-hits-edges.772760/
        public void RefreshTerrainCollider(Location playerLoc)
        {
            // Build nearby collider
            Task.Factory.StartNew(() => {
                terrainColliderDirty = false;
                var world = game!.GetWorld();

                var table = CornClient.Instance?.PackManager?.StateModelTable;
                if (table is null)
                    return;

                int offsetY = World.GetDimension().minY;
                
                float3[] movementVerts = { }, fluidVerts = { };

                for (int x = -MOVEMENT_RADIUS;x <= MOVEMENT_RADIUS;x++)
                    for (int y = -MOVEMENT_RADIUS;y <= MOVEMENT_RADIUS;y++)
                        for (int z = -MOVEMENT_RADIUS;z <= MOVEMENT_RADIUS;z++)
                        {
                            if (x * x + y * y + z * z > MOVEMENT_RADIUS_SQR)
                                continue;

                            var loc  = playerLoc + new Location(x, y, z);
                            var column = world.GetChunkColumn(loc);
                            if (column is null || !column.FullyLoaded)
                            {
                                terrainColliderDirty = true;
                                return;
                            }

                            var bloc = world.GetBlock(loc);
                            var state = bloc.State;

                            if (state.InWater || state.InLava) // Build liquid collider
                            {
                                var neighborCheck = state.InWater ? BlockNeighborChecks.WATER_SURFACE : BlockNeighborChecks.LAVA_SURFACE;
                                int liquidCullFlags = world.GetCullFlags(loc, bloc, neighborCheck);

                                if (liquidCullFlags != 0)
                                    FluidGeometry.BuildCollider(ref fluidVerts, (int)loc.X, (int)loc.Y, (int)loc.Z, liquidCullFlags);
                            }

                            if (state.LikeAir || state.NoCollision)
                                continue;
                            
                            // Build collider here
                            var stateId = bloc.StateId;
                            int cullFlags = world.GetCullFlags(loc, bloc, BlockNeighborChecks.NON_FULL_SOLID);
                            
                            if (cullFlags != 0 && table is not null && table.ContainsKey(stateId)) // This chunk has at least one visible block of this render type
                            {
                                // They all have the same collider so we just pick the 1st one
                                table[stateId].Geometries[0].BuildCollider(ref movementVerts, new((float)loc.Z, (float)loc.Y, (float)loc.X), cullFlags);
                            }

                        }
                
                Loom.QueueOnMainThread(() => {
                    int movVertCount = movementVerts.Length;
                    int fldVertCount = fluidVerts.Length;

                    // Make vertex attributes
                    var colVertAttrs = new NativeArray<VertexAttributeDescriptor>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                    colVertAttrs[0]  = new(VertexAttribute.Position,  dimension: 3);

                    if (movVertCount > 0)
                    {
                        var colMeshDataArr  = Mesh.AllocateWritableMeshData(1);
                        var colMeshData = colMeshDataArr[0];
                        colMeshData.subMeshCount = 1;

                        colMeshData.SetVertexBufferParams(movVertCount,          colVertAttrs);
                        colMeshData.SetIndexBufferParams((movVertCount / 2) * 3, IndexFormat.UInt32);

                        // Copy the source arrays to mesh data
                        var colPositions = colMeshData.GetVertexData<float3>(0);
                        colPositions.CopyFrom(movementVerts);

                        // Generate triangle arrays
                        var colTriIndices = colMeshData.GetIndexData<uint>();
                        uint vi = 0; int ti = 0;
                        for (;vi < movVertCount;vi += 4U, ti += 6)
                        {
                            colTriIndices[ti]     = vi;
                            colTriIndices[ti + 1] = vi + 3U;
                            colTriIndices[ti + 2] = vi + 2U;
                            colTriIndices[ti + 3] = vi;
                            colTriIndices[ti + 4] = vi + 1U;
                            colTriIndices[ti + 5] = vi + 3U;
                        }

                        colMeshData.SetSubMesh(0, new(0, (movVertCount / 2) * 3){ vertexCount = movVertCount });
                        var colliderMesh = new Mesh { subMeshCount = 1 };
                        Mesh.ApplyAndDisposeWritableMeshData(colMeshDataArr, colliderMesh);

                        colliderMesh.RecalculateNormals();
                        colliderMesh.RecalculateBounds();

                        movementCollider!.sharedMesh = colliderMesh;

                    }
                    else
                        movementCollider!.sharedMesh?.Clear();
                    
                    if (fldVertCount > 0)
                    {
                        var colMeshDataArr  = Mesh.AllocateWritableMeshData(1);
                        var colMeshData = colMeshDataArr[0];
                        colMeshData.subMeshCount = 1;

                        colMeshData.SetVertexBufferParams(fldVertCount,          colVertAttrs);
                        colMeshData.SetIndexBufferParams((fldVertCount / 2) * 3, IndexFormat.UInt32);

                        // Copy the source arrays to mesh data
                        var colPositions = colMeshData.GetVertexData<float3>(0);
                        colPositions.CopyFrom(fluidVerts);

                        // Generate triangle arrays
                        var colTriIndices = colMeshData.GetIndexData<uint>();
                        uint vi = 0; int ti = 0;
                        for (;vi < fldVertCount;vi += 4U, ti += 6)
                        {
                            colTriIndices[ti]     = vi;
                            colTriIndices[ti + 1] = vi + 3U;
                            colTriIndices[ti + 2] = vi + 2U;
                            colTriIndices[ti + 3] = vi;
                            colTriIndices[ti + 4] = vi + 1U;
                            colTriIndices[ti + 5] = vi + 3U;
                        }

                        colMeshData.SetSubMesh(0, new(0, (fldVertCount / 2) * 3){ vertexCount = fldVertCount });
                        var colliderMesh = new Mesh { subMeshCount = 1 };
                        Mesh.ApplyAndDisposeWritableMeshData(colMeshDataArr, colliderMesh);

                        colliderMesh.RecalculateNormals();
                        colliderMesh.RecalculateBounds();

                        liquidCollider!.sharedMesh = colliderMesh;

                    }
                    else
                        liquidCollider!.sharedMesh?.Clear();
                    
                    colVertAttrs.Dispose();

                });
                
            });
        }

        void Start()
        {
            game = CornClient.Instance;
            world = game!.GetWorld();

            var modelTable = game!.PackManager.StateModelTable;
            builder = new(modelTable);

            var movementColliderObj = new GameObject("Movement Collider");
            movementColliderObj.layer = LayerMask.NameToLayer(MOVEMENT_LAYER_NAME);
            movementCollider = movementColliderObj.AddComponent<MeshCollider>();

            var liquidColliderObj = new GameObject("Liquid Collider");
            liquidColliderObj.layer = LayerMask.NameToLayer(LIQUID_LAYER_NAME);
            liquidCollider = liquidColliderObj.AddComponent<MeshCollider>();

            // Register event callbacks
            EventManager.Instance.Register(columnCallBack1 = (e) => {
                // TODO Implement

            });

            EventManager.Instance.Register(columnCallBack2 = (e) => {
                int2 chunkCoord = new(e.ChunkX, e.ChunkZ);
                if (columns.ContainsKey(chunkCoord))
                {
                    columns[chunkCoord].Unload(ref chunksBeingBuilt, ref chunkRendersToBeBuilt);
                    columns.Remove(chunkCoord);
                }
            });

            EventManager.Instance.Register(blockCallBack = (e) => {
                var loc = e.Location;
                int chunkX = loc.ChunkX, chunkY = loc.ChunkY, chunkZ = loc.ChunkZ;

                var chunkData = game?.GetWorld()?[chunkX, chunkZ];
                if (chunkData is null) return;
                
                var column = GetChunkRenderColumn(loc.ChunkX, loc.ChunkZ, false);

                if (column is not null) // Queue this chunk to rebuild list...
                {   // Create the chunk render object if not present (previously empty)
                    var chunk = column.GetChunkRender(chunkY, true);
                    chunkData.ChunkMask |= 1 << chunkY;

                    // Queue the chunk. Priority is left as 0(highest), so that changes can be seen instantly
                    QueueChunkRenderBuild(chunk);

                    if (loc.ChunkBlockY == 0 && (chunkY - 1) >= 0) // In the bottom layer of this chunk
                    {   // Queue the chunk below, if it isn't empty
                        QueueChunkBuildIfNotEmpty(column.GetChunkRender(chunkY - 1, false));
                    }
                    else if (loc.ChunkBlockY == Chunk.SizeY - 1 && ((chunkY + 1) * Chunk.SizeY) < World.GetDimension().height) // In the top layer of this chunk
                    {   // Queue the chunk above, if it isn't empty
                        QueueChunkBuildIfNotEmpty(column.GetChunkRender(chunkY + 1, false));
                    }
                }

                if (loc.ChunkBlockX == 0) // Check MC X direction neighbors
                {
                    QueueChunkBuildIfNotEmpty(GetChunkRender(chunkX - 1, chunkY, chunkZ));
                }
                else if (loc.ChunkBlockX == Chunk.SizeX - 1)
                {
                    QueueChunkBuildIfNotEmpty(GetChunkRender(chunkX + 1, chunkY, chunkZ));
                }

                if (loc.ChunkBlockZ == 0) // Check MC Z direction neighbors
                {
                    QueueChunkBuildIfNotEmpty(GetChunkRender(chunkX, chunkY, chunkZ - 1));
                }
                else if (loc.ChunkBlockZ == Chunk.SizeZ - 1)
                {
                    QueueChunkBuildIfNotEmpty(GetChunkRender(chunkX, chunkY, chunkZ + 1));
                }

                terrainColliderDirty = true;

            });

            EventManager.Instance.Register(blocksCallBack = (e) => {
                World world = game!.GetWorld();
                if (world is null) return;
                
                foreach (var loc in e.Locations)
                {
                    int chunkX = loc.ChunkX, chunkY = loc.ChunkY, chunkZ = loc.ChunkZ;

                    var chunkData = world[chunkX, chunkZ];
                    if (chunkData is null) continue;
                    
                    var column = GetChunkRenderColumn(loc.ChunkX, loc.ChunkZ, false);

                    if (column is not null) // Queue this chunk to rebuild list...
                    {   // Create the chunk render object if not present (previously empty)
                        var chunk = column.GetChunkRender(chunkY, true);
                        chunkData.ChunkMask |= 1 << chunkY;

                        // Queue the chunk. Priority is left as 0(highest), so that changes can be seen instantly
                        QueueChunkRenderBuild(chunk);

                        if (loc.ChunkBlockY == 0 && (chunkY - 1) >= 0) // In the bottom layer of this chunk
                        {   // Queue the chunk below, if it isn't empty
                            QueueChunkBuildIfNotEmpty(column.GetChunkRender(chunkY - 1, false));
                        }
                        else if (loc.ChunkBlockY == Chunk.SizeY - 1 && ((chunkY + 1) * Chunk.SizeY) < World.GetDimension().height) // In the top layer of this chunk
                        {   // Queue the chunk above, if it isn't empty
                            QueueChunkBuildIfNotEmpty(column.GetChunkRender(chunkY + 1, false));
                        }
                    }

                    if (loc.ChunkBlockX == 0) // Check MC X direction neighbors
                        QueueChunkBuildIfNotEmpty(GetChunkRender(chunkX - 1, chunkY, chunkZ));
                    else if (loc.ChunkBlockX == Chunk.SizeX - 1)
                        QueueChunkBuildIfNotEmpty(GetChunkRender(chunkX + 1, chunkY, chunkZ));

                    if (loc.ChunkBlockZ == 0) // Check MC Z direction neighbors
                        QueueChunkBuildIfNotEmpty(GetChunkRender(chunkX, chunkY, chunkZ - 1));
                    else if (loc.ChunkBlockZ == Chunk.SizeZ - 1)
                        QueueChunkBuildIfNotEmpty(GetChunkRender(chunkX, chunkY, chunkZ + 1));
                }

                terrainColliderDirty = true;

            });

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

                    if (nextChunk is null)
                    {   // Chunk is unloaded while waiting in the queue, ignore it...
                        continue;
                    }
                    else if (GetChunkRenderColumn(nextChunk.ChunkX, nextChunk.ChunkZ, false) is null)
                    {   // Chunk column is unloaded while waiting in the queue, ignore it...
                        nextChunk.State = ChunkBuildState.Cancelled;
                        continue;
                    }
                    else
                    {
                        BuildChunkRender(nextChunk);
                        newCount--;
                    }

                }
            }

            if (operationCooldown <= 0F)
            {   // TODO Make this better
                switch (operationAction)
                {
                    case 0:
                        UpdateChunkRendersListAdd();
                        break;
                    case 1:
                        UpdateChunkRendersListRemove();
                        break;
                }
                
                operationAction = (operationAction + 1) % 2;

                operationCooldown = 0.5F;
            }
            else
            {
                var playerLoc = game!.PlayerData.location.ToFloor();
                
                if (terrainColliderDirty || lastPlayerLoc != playerLoc)
                {
                    RefreshTerrainCollider(playerLoc.Up());
                    lastPlayerLoc = playerLoc;
                }
            }

            operationCooldown -= Time.fixedDeltaTime;

        }
        #endregion

    }

}