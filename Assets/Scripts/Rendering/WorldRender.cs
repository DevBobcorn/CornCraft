#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
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
    public class WorldRender : MonoBehaviour
    {
        private static WorldRender? instance;
        public static WorldRender Instance
        {
            get {
                if (instance is null)
                {
                    instance = Component.FindObjectOfType<WorldRender>();
                }
                
                return instance;
            }
        }

        private Dictionary<int2, ChunkRenderColumn> columns = new();

        // Both manipulated on Unity main thread only
        private PriorityQueue<ChunkRender> chunksToBeBuild = new();
        private List<ChunkRender>         chunksBeingBuilt = new();

        public static PhysicMaterial? CHUNK_MATERIAL;

        private CornClient? game;

        // Terrain collider for movement
        private MeshCollider? movementCollider;

        public string GetDebugInfo()
        {
            return $"QueC: {chunksToBeBuild.Count}\t BldC: {chunksBeingBuilt.Count}";
        }

        #region Chunk management
        private ChunkRenderColumn CreateColumn(int chunkX, int chunkZ)
        {
            // Create this chunk column...
            GameObject columnObj = new GameObject("Column [" + chunkX.ToString() + ", " + chunkZ.ToString() + "]");
            ChunkRenderColumn newColumn = columnObj.AddComponent<ChunkRenderColumn>();
            newColumn.ChunkX = chunkX;
            newColumn.ChunkZ = chunkZ;
            // Set its parent to this world...
            columnObj.transform.parent = this.transform;
            columnObj.transform.localPosition = Vector3.zero;

            return newColumn;
        }

        private ChunkRenderColumn? GetChunkColumn(int chunkX, int chunkZ, bool createIfEmpty)
        {
            int2 chunkCoord = new(chunkX, chunkZ);
            if (columns.ContainsKey(chunkCoord))
                return columns[chunkCoord];

            if (createIfEmpty)
            {
                ChunkRenderColumn newColumn = CreateColumn(chunkX, chunkZ);
                columns.Add(chunkCoord, newColumn);
                return newColumn;
            }

            return null;
        }

        public ChunkRender? GetChunk(int chunkX, int chunkY, int chunkZ)
        {
            return GetChunkColumn(chunkX, chunkZ, false)?.GetChunk(chunkY, false);
        }

        public bool IsChunkDataReady(int chunkX, int chunkY, int chunkZ)
        {
            if (chunkY < 0 || chunkY * Chunk.SizeY >= World.GetDimension().height)
            {
                // Above the sky or below the bedrock, treat it as ready empty chunks...
                return true;
            }
            else
            {
                var column = game!.GetWorld().GetChunkColumn(chunkX, chunkZ);
                if (column is not null)
                {
                    // Chunk position is valid. If this chunk we're getting
                    // is null, it means this chunks is filled with air,
                    // and in this case the chunk data is also ready.
                    return true;
                }
                else
                {
                    // Chunk position is valid, but its data is still not known (not loaded)
                    return false;
                }
            }
        }

        #endregion

        #region Chunk building
        //private static readonly Chunk.BlockCheck notFullSolid = new Chunk.BlockCheck((self, neighbor) => { return !neighbor.State.FullSolid; });
        private static readonly Chunk.BlockCheck waterSurface = new Chunk.BlockCheck((self, neighbor) => { return !(neighbor.State.InWater || neighbor.State.FullSolid); });
        private static readonly Chunk.BlockCheck lavaSurface  = new Chunk.BlockCheck((self, neighbor) => { return !(neighbor.State.InLava  || neighbor.State.FullSolid); });

        private static readonly Chunk.BlockCheck notFullSolid = new Chunk.BlockCheck((self, neighbor) => { return !neighbor.State.FullSolid; });
        
        private static readonly ResourceLocation WATER_STILL = new("block/water_still");
        private static readonly ResourceLocation LAVA_STILL  = new("block/lava_still");
        private static readonly int waterLayerIndex = ChunkRender.TypeIndex(RenderType.WATER);
        private static readonly int lavaLayerIndex  = ChunkRender.TypeIndex(RenderType.SOLID);

        public void BuildChunk(ChunkRender chunkRender)
        {
            var chunkColumnData = game!.GetWorld()[chunkRender.ChunkX, chunkRender.ChunkZ];
            if (chunkColumnData is null) // Chunk column data unloaded, cancel
            {
                int2 chunkCoord = new(chunkRender.ChunkX, chunkRender.ChunkZ);
                if (columns.ContainsKey(chunkCoord))
                {
                    columns[chunkCoord].Unload(ref chunksBeingBuilt, ref chunksToBeBuild);
                    columns.Remove(chunkCoord);
                }
                chunkRender.State = BuildState.Cancelled;
                return;
            }

            var chunkData = chunkColumnData[chunkRender.ChunkY];

            if (chunkData is null) // Chunk not available, delay
            {
                chunkRender.State = BuildState.Delayed;
                return;
            }

            // Save neighbors' status(present or not) right before mesh building
            chunkRender.UpdateNeighborStatus();

            if (!chunkRender.AllNeighborDataPresent) // TODO: Check if this is reasonable
            {
                chunkRender.State = BuildState.Cancelled;
                return; // Not all neighbor data ready, just cancel
            }

            var ts = chunkRender.TokenSource = new CancellationTokenSource();

            chunksBeingBuilt.Add(chunkRender);
            chunkRender.State = BuildState.Building;
            
            Task.Factory.StartNew(() => {
                try
                {
                    var table = CornClient.Instance?.PackManager?.finalTable;
                    if (table is null)
                    {
                        chunksBeingBuilt.Remove(chunkRender);
                        chunkRender.State = BuildState.Cancelled;
                        return;
                    }

                    bool isAllEmpty = true;
                    int count = ChunkRender.TYPES.Length, layerMask = 0;
                    int offsetY = World.GetDimension().minY;

                    var visualBuffer = new VertexBuffer[count];
                    for (int i = 0;i < count;i++)
                        visualBuffer[i] = new();
                    
                    float3[] colliderVerts = { };

                    // Build chunk mesh block by block
                    for (int x = 0;x < Chunk.SizeX;x++)
                    {
                        for (int y = 0;y < Chunk.SizeY;y++)
                        {
                            for (int z = 0;z < Chunk.SizeZ;z++)
                            {
                                if (ts.IsCancellationRequested)
                                {
                                    //Debug.Log(chunkRender.ToString() + " cancelled. (Building Mesh)");
                                    chunksBeingBuilt.Remove(chunkRender);
                                    chunkRender.State = BuildState.Cancelled;
                                    return;
                                }

                                var loc = GetLocationInChunkRender(chunkRender, x, y, z, offsetY);

                                var bloc = chunkData[x, y, z];
                                var state = bloc.State;
                                var stateId = bloc.StateId;

                                if (state.InWater) // Build water here
                                {
                                    int waterCullFlags = chunkData.GetCullFlags(loc, bloc, waterSurface);
                                    if (waterCullFlags != 0)
                                    {
                                        FluidGeometry.Build(ref visualBuffer[waterLayerIndex], WATER_STILL, x, y, z, waterCullFlags);
                                        layerMask |= (1 << waterLayerIndex);
                                        isAllEmpty = false;
                                    }
                                }
                                else if (state.InLava) // Build lava here
                                {
                                    int lavaCullFlags = chunkData.GetCullFlags(loc, bloc, lavaSurface);
                                    if (lavaCullFlags != 0)
                                    {
                                        FluidGeometry.Build(ref visualBuffer[lavaLayerIndex], LAVA_STILL, x, y, z, lavaCullFlags);
                                        layerMask |= (1 << lavaLayerIndex);
                                        isAllEmpty = false;
                                    }
                                }

                                // If air-like (air, water block, etc), ignore it...
                                if (state.LikeAir) continue;

                                var layer = BlockStatePalette.INSTANCE.GetRenderType(stateId);
                                int layerIndex = ChunkRender.TypeIndex(layer);
                                
                                int cullFlags = chunkData.GetCullFlags(loc, bloc, notFullSolid); // TODO Make it more accurate
                                
                                if (cullFlags != 0 && table is not null && table.ContainsKey(stateId)) // This chunk has at least one visible block of this render type
                                {
                                    var models = table[stateId].Geometries;
                                    var chosen = (x + y + z) % models.Length;

                                    if (state.NoCollision)
                                        models[chosen].Build(ref visualBuffer[layerIndex], new(z, y, x), cullFlags);
                                    else
                                        models[chosen].BuildWithCollider(ref visualBuffer[layerIndex], ref colliderVerts, new(z, y, x), cullFlags);
                                    
                                    layerMask |= (1 << layerIndex);
                                    isAllEmpty = false;
                                }
                                
                            }
                        }
                    }

                    if (isAllEmpty)
                    {
                        // Skip empty chunks...
                        Loom.QueueOnMainThread(() => {
                            // Mission cancelled...
                            if (ts.IsCancellationRequested || !chunkRender || !chunkRender.gameObject)
                            {
                                //Debug.Log(chunkRender?.ToString() + " cancelled. (Skipping Empty Mesh)");
                                if (chunkRender is not null)
                                {
                                    chunksBeingBuilt.Remove(chunkRender);
                                    chunkRender.State = BuildState.Cancelled;
                                }
                                return;
                            }

                            // TODO Improve below cleaning
                            chunkRender.GetComponent<MeshFilter>().sharedMesh?.Clear(false);

                            chunkRender.ClearCollider();

                            chunksBeingBuilt.Remove(chunkRender);
                            chunkRender.State = BuildState.Ready;

                        });
                    }
                    else
                    {
                        Loom.QueueOnMainThread(() => {
                            // Mission cancelled...
                            if (ts.IsCancellationRequested || !chunkRender || !chunkRender.gameObject)
                            {
                                //Debug.Log(chunkRender?.ToString() + " cancelled. (Applying Mesh)");
                                if (chunkRender is not null)
                                {
                                    chunksBeingBuilt.Remove(chunkRender);
                                    chunkRender.State = BuildState.Cancelled;
                                }
                                return;
                            }

                            // Visual Mesh
                            // Count layers, vertices and face indices
                            int layerCount = 0, totalVertCount = 0;
                            for (int layer = 0;layer < count;layer++)
                            {
                                if ((layerMask & (1 << layer)) != 0)
                                {
                                    layerCount++;
                                    totalVertCount += visualBuffer[layer].vert.Length;
                                }
                            }
                            
                            var meshDataArr  = Mesh.AllocateWritableMeshData(1);
                            var materialArr  = new UnityEngine.Material[layerCount];
                            int vertOffset = 0;

                            var meshData = meshDataArr[0];
                            meshData.subMeshCount = layerCount;

                            // Set mesh attributes
                            var visVertAttrs = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                            visVertAttrs[0]  = new(VertexAttribute.Position,  dimension: 3, stream: 0);
                            visVertAttrs[1]  = new(VertexAttribute.TexCoord0, dimension: 2, stream: 1);

                            meshData.SetVertexBufferParams(totalVertCount,          visVertAttrs);
                            meshData.SetIndexBufferParams((totalVertCount / 2) * 3, IndexFormat.UInt32);

                            visVertAttrs.Dispose();

                            // Prepare source data arrays
                            var allVerts = new float3[totalVertCount];
                            var allUVs   = new float2[totalVertCount];
                            for (int layer = 0;layer < count;layer++)
                            {
                                if ((layerMask & (1 << layer)) != 0)
                                {
                                    visualBuffer[layer].vert.CopyTo(allVerts, vertOffset);
                                    visualBuffer[layer].txuv.CopyTo(allUVs, vertOffset);

                                    vertOffset += visualBuffer[layer].vert.Length;
                                }
                            }

                            // Copy the source arrays to mesh data
                            var positions  = meshData.GetVertexData<float3>(0);
                            positions.CopyFrom(allVerts);
                            var texCoords  = meshData.GetVertexData<float2>(1);
                            texCoords.CopyFrom(allUVs);
                            // Generate triangle arrays
                            var triIndices = meshData.GetIndexData<uint>();
                            uint vi = 0; int ti = 0;
                            for (;vi < totalVertCount;vi += 4U, ti += 6)
                            {
                                triIndices[ti]     = vi;
                                triIndices[ti + 1] = vi + 3U;
                                triIndices[ti + 2] = vi + 2U;
                                triIndices[ti + 3] = vi;
                                triIndices[ti + 4] = vi + 1U;
                                triIndices[ti + 5] = vi + 3U;
                            }

                            int subMeshIndex = 0;
                            vertOffset = 0;

                            for (int layer = 0;layer < count;layer++)
                            {
                                if ((layerMask & (1 << layer)) != 0)
                                {
                                    materialArr[subMeshIndex] = MaterialManager.GetBlockMaterial(ChunkRender.TYPES[layer]);
                                    int vertCount = visualBuffer[layer].vert.Length;
                                    meshData.SetSubMesh(subMeshIndex, new((vertOffset / 2) * 3, (vertCount / 2) * 3){ vertexCount = vertCount });
                                    vertOffset += vertCount;
                                    subMeshIndex++;
                                }
                            }

                            var visualMesh = new Mesh { subMeshCount = layerCount };
                            Mesh.ApplyAndDisposeWritableMeshData(meshDataArr, visualMesh);

                            visualMesh.RecalculateNormals();
                            visualMesh.RecalculateBounds();

                            chunkRender.GetComponent<MeshFilter>().sharedMesh = visualMesh;
                            chunkRender.GetComponent<MeshRenderer>().sharedMaterials = materialArr;

                            // Collider Mesh
                            int colVertCount = colliderVerts.Length;

                            if (colVertCount > 0)
                            {
                                var colMeshDataArr  = Mesh.AllocateWritableMeshData(1);
                                var colMeshData = colMeshDataArr[0];
                                colMeshData.subMeshCount = 1;

                                // Set mesh attributes
                                var colVertAttrs = new NativeArray<VertexAttributeDescriptor>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                                colVertAttrs[0]  = new(VertexAttribute.Position,  dimension: 3);

                                colMeshData.SetVertexBufferParams(colVertCount,          colVertAttrs);
                                colMeshData.SetIndexBufferParams((colVertCount / 2) * 3, IndexFormat.UInt32);

                                colVertAttrs.Dispose();

                                // Copy the source arrays to mesh data
                                var colPositions  = colMeshData.GetVertexData<float3>(0);
                                colPositions.CopyFrom(colliderVerts);

                                // Generate triangle arrays
                                var colTriIndices = colMeshData.GetIndexData<uint>();
                                vi = 0; ti = 0;
                                for (;vi < colliderVerts.Length;vi += 4U, ti += 6)
                                {
                                    colTriIndices[ti]     = vi;
                                    colTriIndices[ti + 1] = vi + 3U;
                                    colTriIndices[ti + 2] = vi + 2U;
                                    colTriIndices[ti + 3] = vi;
                                    colTriIndices[ti + 4] = vi + 1U;
                                    colTriIndices[ti + 5] = vi + 3U;
                                }

                                colMeshData.SetSubMesh(0, new(0, (colVertCount / 2) * 3){ vertexCount = colVertCount });
                                var colliderMesh = new Mesh { subMeshCount = 1 };
                                Mesh.ApplyAndDisposeWritableMeshData(colMeshDataArr, colliderMesh);

                                colliderMesh.RecalculateNormals();
                                colliderMesh.RecalculateBounds();

                                chunkRender.UpdateCollider(colliderMesh);

                            }
                            else
                                chunkRender.ClearCollider();

                            chunksBeingBuilt.Remove(chunkRender);
                            chunkRender.State = BuildState.Ready;

                        });
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message + ":" + e.StackTrace);

                    // Remove chunk from list
                    if (chunkRender is not null)
                    {
                        Loom.QueueOnMainThread(() => {
                            chunksBeingBuilt.Remove(chunkRender);
                        });
                        chunkRender.State = BuildState.Cancelled;
                    }
                }

            }, ts.Token);
        }
        #endregion

        #region Chunk updates
        Action<ReceiveChunkColumnEvent>? columnCallBack1;
        Action<UnloadChunkColumnEvent>? columnCallBack2;
        Action<BlockUpdateEvent>? blockCallBack;
        Action<BlocksUpdateEvent>? blocksCallBack;

        private Location GetLocationInChunkRender(ChunkRender chunk, int x, int y, int z, int offsetY)
        {   // Offset y coordinate by the current dimension's minY...
            return new Location(chunk.ChunkX * Chunk.SizeX + x, chunk.ChunkY * Chunk.SizeY + y + offsetY, chunk.ChunkZ * Chunk.SizeZ + z);
        }

        private const int ChunkCenterX = Chunk.SizeX / 2 + 1;
        private const int ChunkCenterY = Chunk.SizeY / 2 + 1;
        private const int ChunkCenterZ = Chunk.SizeZ / 2 + 1;

        private void UpdateBuildPriority(Location currentLocation, ChunkRender chunk, int offsetY)
        {   // Get this chunk's build priority based on its current distance to the player,
            // a smaller value means a higher priority...
            chunk.Priority = (int)(
                new Location(chunk.ChunkX * Chunk.SizeX + ChunkCenterX, chunk.ChunkY * Chunk.SizeY + ChunkCenterY + offsetY, chunk.ChunkZ * Chunk.SizeZ + ChunkCenterZ)
                    .DistanceTo(currentLocation) / 16);
        }

        // Add new chunks into render list
        public void UpdateChunkRendersListAdd()
        {
            World world = game!.GetWorld();
            if (world is null) return;

            // Add nearby chunks
            var location = game!.GetCurrentLocation();
            ChunkRenderColumn columnRender;

            int viewDist = CornCraft.MCSettings_RenderDistance;

            int chunkColumnSize = (World.GetDimension().height + Chunk.SizeY - 1) / Chunk.SizeY; // Round up
            int offsetY = World.GetDimension().minY;

            for (int cx = -viewDist;cx <= viewDist;cx++)
                for (int cz = -viewDist;cz <= viewDist;cz++)
                {
                    int chunkX = location.ChunkX + cx, chunkZ = location.ChunkZ + cz;
                    
                    if (world.isChunkColumnReady(chunkX, chunkZ))
                    {
                        var column = GetChunkColumn(chunkX, chunkZ, false);
                        if (column is null)
                        {   // Chunks data is ready, but chunk render column is not
                            int chunkMask = world[chunkX, chunkZ]!.ChunkMask;
                            // Create it and add the whole column to render list...
                            columnRender = GetChunkColumn(chunkX, chunkZ, true)!;
                            for (int chunkY = 0;chunkY < chunkColumnSize;chunkY++)
                            {   // Create chunk renders and queue them...
                                if ((chunkMask & (1 << chunkY)) != 0)
                                {   // This chunk is not empty and needs to be added and queued
                                    var chunk = columnRender.GetChunk(chunkY, true);
                                    UpdateBuildPriority(location, chunk, offsetY);
                                    QueueChunkBuild(chunk);
                                }
                                
                            }
                        }
                        else
                        {
                            foreach (var chunk in column.GetChunks().Values)
                            {
                                if (chunk.State == BuildState.Delayed || chunk.State == BuildState.Cancelled)
                                {   // Queue delayed or cancelled chunk builds...
                                    UpdateBuildPriority(location, chunk, offsetY);
                                    QueueChunkBuild(chunk);
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
                    columns[chunkCoord].Unload(ref chunksBeingBuilt, ref chunksToBeBuild);
                    columns.Remove(chunkCoord);
                }

            }

        }

        public void QueueChunkBuild(ChunkRender chunkRender)
        {
            if (!chunksToBeBuild.Contains(chunkRender))
            {
                chunksToBeBuild.Enqueue(chunkRender);
                chunkRender.State = BuildState.Pending;
            }

        }

        public void QueueChunkBuildIfNotEmpty(ChunkRender? chunkRender)
        {
            if (chunkRender is not null) // Not empty(air) chunk
                QueueChunkBuild(chunkRender);
        }

        public void UnloadWorld()
        {
            // Clear the queue first...
            chunksToBeBuild.Clear();

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
            chunksToBeBuild.Clear();

            // And cancel current chunk builds
            foreach (var chunk in chunksBeingBuilt)
                chunk.TokenSource?.Cancel();
            
            chunksBeingBuilt.Clear();

            // Clear all chunk columns in world
            var chunkCoords = columns.Keys.ToArray();

            foreach (var chunkCoord in chunkCoords)
            {
                columns[chunkCoord].Unload(ref chunksBeingBuilt, ref chunksToBeBuild);
                columns.Remove(chunkCoord);
            }

            columns.Clear();
        }

        public const int BUILD_COUNT_LIMIT = 8;
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

                var table = CornClient.Instance?.PackManager?.finalTable;
                if (table is null)
                    return;

                int offsetY = World.GetDimension().minY;
                
                float3[] movementVerts = { };

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

                            if (state.LikeAir || state.NoCollision)
                                continue;
                            
                            // Build collider here
                            var stateId = bloc.StateId;

                            var layer = BlockStatePalette.INSTANCE.GetRenderType(stateId);
                            int layerIndex = ChunkRender.TypeIndex(layer);
                            
                            int cullFlags = world.GetCullFlags(loc, bloc, notFullSolid);
                            
                            if (cullFlags != 0 && table is not null && table.ContainsKey(stateId)) // This chunk has at least one visible block of this render type
                            {
                                // They all have the same collider so we just pick the 1st one
                                table[stateId].Geometries[0].BuildCollider(ref movementVerts, new((float)loc.Z, (float)loc.Y, (float)loc.X), cullFlags);

                            }

                        }
                
                Loom.QueueOnMainThread(() => {
                    int movVertCount = movementVerts.Length;

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
                });
                
            });
        }

        void Start()
        {
            game = CornClient.Instance;

            // Physic materials are not allowed to be created in a static method,
            // so we put the initialization here...
            CHUNK_MATERIAL = new()
            {
                frictionCombine = PhysicMaterialCombine.Minimum,
                staticFriction = 0F,
                dynamicFriction = 0F
            };

            var movementColliderObj = new GameObject("Terrain Movement Collider");
            movementColliderObj.layer = LayerMask.NameToLayer("Movement");
            movementCollider = movementColliderObj.AddComponent<MeshCollider>();

            // Register event callbacks
            EventManager.Instance.Register(columnCallBack1 = (e) => {
                // TODO Implement

            });

            EventManager.Instance.Register(columnCallBack2 = (e) => {
                int2 chunkCoord = new(e.chunkX, e.chunkZ);
                if (columns.ContainsKey(chunkCoord))
                {
                    columns[chunkCoord].Unload(ref chunksBeingBuilt, ref chunksToBeBuild);
                    columns.Remove(chunkCoord);
                }
            });

            EventManager.Instance.Register(blockCallBack = (e) => {
                var loc = e.location;
                int chunkX = loc.ChunkX, chunkY = loc.ChunkY, chunkZ = loc.ChunkZ;

                var chunkData = game?.GetWorld()?[chunkX, chunkZ];
                if (chunkData is null) return;
                
                var column = GetChunkColumn(loc.ChunkX, loc.ChunkZ, false);

                if (column is not null) // Queue this chunk to rebuild list...
                {   // Create the chunk render object if not present (previously empty)
                    var chunk = column.GetChunk(chunkY, true);
                    chunkData.ChunkMask |= 1 << chunkY;

                    // Queue the chunk. Priority is left as 0(highest), so that changes can be seen instantly
                    QueueChunkBuild(chunk);

                    if (loc.ChunkBlockY == 0 && (chunkY - 1) >= 0) // In the bottom layer of this chunk
                    {   // Queue the chunk below, if it isn't empty
                        QueueChunkBuildIfNotEmpty(column.GetChunk(chunkY - 1, false));
                    }
                    else if (loc.ChunkBlockY == Chunk.SizeY - 1 && ((chunkY + 1) * Chunk.SizeY) < World.GetDimension().height) // In the top layer of this chunk
                    {   // Queue the chunk above, if it isn't empty
                        QueueChunkBuildIfNotEmpty(column.GetChunk(chunkY + 1, false));
                    }
                }

                if (loc.ChunkBlockX == 0) // Check MC X direction neighbors
                {
                    QueueChunkBuildIfNotEmpty(GetChunk(chunkX - 1, chunkY, chunkZ));
                }
                else if (loc.ChunkBlockX == Chunk.SizeX - 1)
                {
                    QueueChunkBuildIfNotEmpty(GetChunk(chunkX + 1, chunkY, chunkZ));
                }

                if (loc.ChunkBlockZ == 0) // Check MC Z direction neighbors
                {
                    QueueChunkBuildIfNotEmpty(GetChunk(chunkX, chunkY, chunkZ - 1));
                }
                else if (loc.ChunkBlockZ == Chunk.SizeZ - 1)
                {
                    QueueChunkBuildIfNotEmpty(GetChunk(chunkX, chunkY, chunkZ + 1));
                }

                terrainColliderDirty = true;

            });

            EventManager.Instance.Register(blocksCallBack = (e) => {
                World world = game!.GetWorld();
                if (world is null) return;
                
                foreach (var loc in e.locations)
                {
                    int chunkX = loc.ChunkX, chunkY = loc.ChunkY, chunkZ = loc.ChunkZ;

                    var chunkData = world[chunkX, chunkZ];
                    if (chunkData is null) continue;
                    
                    var column = GetChunkColumn(loc.ChunkX, loc.ChunkZ, false);

                    if (column is not null) // Queue this chunk to rebuild list...
                    {   // Create the chunk render object if not present (previously empty)
                        var chunk = column.GetChunk(chunkY, true);
                        chunkData.ChunkMask |= 1 << chunkY;

                        // Queue the chunk. Priority is left as 0(highest), so that changes can be seen instantly
                        QueueChunkBuild(chunk);

                        if (loc.ChunkBlockY == 0 && (chunkY - 1) >= 0) // In the bottom layer of this chunk
                        {   // Queue the chunk below, if it isn't empty
                            QueueChunkBuildIfNotEmpty(column.GetChunk(chunkY - 1, false));
                        }
                        else if (loc.ChunkBlockY == Chunk.SizeY - 1 && ((chunkY + 1) * Chunk.SizeY) < World.GetDimension().height) // In the top layer of this chunk
                        {   // Queue the chunk above, if it isn't empty
                            QueueChunkBuildIfNotEmpty(column.GetChunk(chunkY + 1, false));
                        }
                    }

                    if (loc.ChunkBlockX == 0) // Check MC X direction neighbors
                    {
                        QueueChunkBuildIfNotEmpty(GetChunk(chunkX - 1, chunkY, chunkZ));
                    }
                    else if (loc.ChunkBlockX == Chunk.SizeX - 1)
                    {
                        QueueChunkBuildIfNotEmpty(GetChunk(chunkX + 1, chunkY, chunkZ));
                    }

                    if (loc.ChunkBlockZ == 0) // Check MC Z direction neighbors
                    {
                        QueueChunkBuildIfNotEmpty(GetChunk(chunkX, chunkY, chunkZ - 1));
                    }
                    else if (loc.ChunkBlockZ == Chunk.SizeZ - 1)
                    {
                        QueueChunkBuildIfNotEmpty(GetChunk(chunkX, chunkY, chunkZ + 1));
                    }
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
                while (chunksToBeBuild.Count > 0 && newCount > 0)
                {
                    var nextChunk = chunksToBeBuild.Dequeue();

                    if (nextChunk is null)
                    {   // Chunk is unloaded while waiting in the queue, ignore it...
                        continue;
                    }
                    else if (GetChunkColumn(nextChunk.ChunkX, nextChunk.ChunkZ, false) is null)
                    {   // Chunk column is unloaded while waiting in the queue, ignore it...
                        nextChunk.State = BuildState.Cancelled;
                        continue;
                    }
                    else
                    {
                        BuildChunk(nextChunk);
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
                var playerLoc = game!.GetPlayerController()?.GetLocation().ToFloor();
                
                if (playerLoc is not null)
                    if (terrainColliderDirty || lastPlayerLoc != playerLoc)
                    {
                        RefreshTerrainCollider(playerLoc.Value.Up());
                        lastPlayerLoc = playerLoc;
                    }
            }

            operationCooldown -= Time.fixedDeltaTime;

        }
        #endregion

    }

}