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

namespace MinecraftClient.Rendering
{
    public class WorldRender : MonoBehaviour
    {
        private static ResourceLocation WATER_STILL = new ResourceLocation("block/water_still");

        private static WorldRender instance;
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

        private Dictionary<int, Dictionary<int, ChunkRenderColumn>> columns = new();
        // Operated on Unity main thread only
        private PriorityQueue<ChunkRender> chunksToBeBuild = new();
        private List<ChunkRender>         chunksBeingBuilt = new();

        private CornClient game;

        public string GetDebugInfo()
        {
            lock (chunksBeingBuilt)
            {
                return $"QueC: {chunksToBeBuild.Count}\t BldC: {chunksBeingBuilt.Count}";
            }
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

        private ChunkRenderColumn GetChunkColumn(int chunkX, int chunkZ, bool createIfEmpty)
        {
            if (columns.ContainsKey(chunkX))
            {
                if (columns[chunkX].ContainsKey(chunkZ))
                {
                    return columns[chunkX][chunkZ];
                }
                else
                {
                    if (createIfEmpty)
                    {
                        ChunkRenderColumn newColumn = CreateColumn(chunkX, chunkZ);
                        columns[chunkX].Add(chunkZ, newColumn);
                        return newColumn;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                if (createIfEmpty)
                {
                    columns.Add(chunkX, new Dictionary<int, ChunkRenderColumn>());
                    ChunkRenderColumn newColumn = CreateColumn(chunkX, chunkZ);
                    columns[chunkX].Add(chunkZ, newColumn);
                    return newColumn;
                }
                else
                {
                    return null;
                }
            }
        }

        public ChunkRender GetChunk(int chunkX, int chunkY, int chunkZ)
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
                ChunkColumn column = game.GetWorld().GetChunkColumn(chunkX, chunkZ);
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

        public void UnloadChunkColumns(int chunkX)
        {
            // Short-circuit here
            if (columns.ContainsKey(chunkX))
            {
                foreach (var chunkZ in columns[chunkX].Keys)
                {
                    // Unload this chunk column (and it'll cancel building by itself)
                    columns[chunkX][chunkZ].Unload(ref chunksBeingBuilt, ref chunksToBeBuild);
                }

                // Clear section (and column indices)
                columns.Remove(chunkX);
            }

        }

        public void UnloadChunkColumn(int chunkX, int chunkZ)
        {
            // Short-circuit here
            if (columns.ContainsKey(chunkX) && columns[chunkX].ContainsKey(chunkZ))
            {
                // Unload this chunk column
                columns[chunkX][chunkZ].Unload(ref chunksBeingBuilt, ref chunksToBeBuild);

                // Remove its index
                columns[chunkX].Remove(chunkZ);

                // And clean up if section is now empty
                if (columns[chunkX].Count == 0)
                    columns.Remove(chunkX);
            }

        }
        #endregion

        #region Chunk building
        private static Chunk.BlockCheck notFullSolid = new Chunk.BlockCheck((bloc) => { return !bloc.State.FullSolid; });
        private static Chunk.BlockCheck waterSurface = new Chunk.BlockCheck((bloc) => { return !(bloc.State.InWater || bloc.State.FullSolid); });

        public void BuildChunk(ChunkRender chunkRender)
        {
            var chunkColumnData = game.GetWorld()[chunkRender.ChunkX, chunkRender.ChunkZ];
            if (chunkColumnData is null) // Chunk column data unloaded, cancel
            {
                UnloadChunkColumn(chunkRender.ChunkX, chunkRender.ChunkZ);
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

            var ts    = chunkRender.TokenSource = new CancellationTokenSource();

            lock (chunksBeingBuilt)
            {
                chunksBeingBuilt.Add(chunkRender);
            }

            chunkRender.State = BuildState.Building;
            
            Task.Factory.StartNew(() => {
                try
                {
                    bool isAllEmpty = true;
                    int count = ChunkRender.TYPES.Length, layerMask = 0;
                    int offsetY = World.GetDimension().minY;

                    var visualBuffer = new MeshBuffer[count];
                    for (int i = 0;i < count;i++)
                        visualBuffer[i] = new MeshBuffer();

                    int waterLayerIndex = ChunkRender.TypeIndex(RenderType.TRANSLUCENT);

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
                                    lock (chunksBeingBuilt)
                                    {
                                        chunksBeingBuilt.Remove(chunkRender);
                                    }
                                    
                                    chunkRender.State = BuildState.Cancelled;
                                    return;
                                }

                                var loc = GetLocationInChunkRender(chunkRender, x, y, z, offsetY);

                                var bloc = chunkData[x, y, z];
                                var state = bloc.State;
                                var stateId = bloc.StateId;

                                if (state.InWater) // Build water here
                                {
                                    int waterCullFlags = chunkData.GetCullFlags(loc, waterSurface);

                                    // TODO ChunkFluidGeometry.Build(ref visualBuffer[waterLayerIndex], WATER_STILL, true, x, y, z, waterCullFlags);
                                    layerMask |= (1 << waterLayerIndex);
                                    isAllEmpty = false;
                                }

                                // If air-like (air, water block, etc), ignore it...
                                if (state.LikeAir) continue;

                                var layer = Block.Palette.GetRenderType(stateId);
                                int layerIndex = ChunkRender.TypeIndex(layer);
                                
                                int cullFlags = chunkData.GetCullFlags(loc, notFullSolid); // TODO Correct
                                
                                if (cullFlags != 0) // This chunk has at least one visible block of this render type
                                {
                                    ChunkStateGeometry.Build(ref visualBuffer[layerIndex], stateId, x, y, z, cullFlags);
                                    
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
                                    lock (chunksBeingBuilt)
                                    {
                                        chunksBeingBuilt.Remove(chunkRender);
                                    }
                                    chunkRender.State = BuildState.Cancelled;
                                }
                                return;
                            }

                            // TODO Improve below cleaning
                            chunkRender.GetComponent<MeshFilter>().sharedMesh?.Clear(false);

                            chunkRender.ClearCollider();

                            lock (chunksBeingBuilt)
                            {
                                chunksBeingBuilt.Remove(chunkRender);
                            }
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
                                    lock (chunksBeingBuilt)
                                    {
                                        chunksBeingBuilt.Remove(chunkRender);
                                    }
                                    chunkRender.State = BuildState.Cancelled;
                                }
                                return;
                            }

                            // Count layers, vertices and face indices
                            int layerCount = 0, totalVertCount = 0, totalFaceIdxCount = 0;
                            for (int layer = 0;layer < count;layer++)
                            {
                                if ((layerMask & (1 << layer)) != 0)
                                {
                                    layerCount++;
                                    totalVertCount    += visualBuffer[layer].vert.Length;
                                    totalFaceIdxCount += visualBuffer[layer].face.Length;
                                }
                            }
                            
                            var meshDataArr  = Mesh.AllocateWritableMeshData(1);
                            var materialArr  = new UnityEngine.Material[layerCount];
                            int vertOffset = 0, faceIdxOffset = 0;

                            var meshData = meshDataArr[0];
                            meshData.subMeshCount = layerCount;

                            var visVertAttrs = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                            visVertAttrs[0]  = new(VertexAttribute.Position,  dimension: 3, stream: 0);
                            visVertAttrs[1]  = new(VertexAttribute.TexCoord0, dimension: 2, stream: 1);

                            meshData.SetVertexBufferParams(totalVertCount,   visVertAttrs);
                            meshData.SetIndexBufferParams(totalFaceIdxCount, IndexFormat.UInt32);

                            visVertAttrs.Dispose();

                            // Prepare source data arrays
                            var allVerts = new float3[totalVertCount];
                            var allUVs   = new float2[totalVertCount];
                            var allFaceIndices = new uint[totalFaceIdxCount];
                            for (int layer = 0;layer < count;layer++)
                            {
                                if ((layerMask & (1 << layer)) != 0)
                                {
                                    int vertCount    = visualBuffer[layer].vert.Length;
                                    int faceIdxCount = visualBuffer[layer].face.Length;

                                    visualBuffer[layer].vert.CopyTo(allVerts, vertOffset);
                                    visualBuffer[layer].uv.CopyTo(allUVs, vertOffset);

                                    //visualBuffer[layer].face.CopyTo(allFaceIndices, faceIdxOffset);
                                    ArrayUtil.GetWithOffset(visualBuffer[layer].face, (uint)vertOffset).CopyTo(allFaceIndices, faceIdxOffset);

                                    vertOffset    += vertCount;
                                    faceIdxOffset += faceIdxCount;
                                }
                            }

                            // Copy the source arrays to mesh data
                            var positions  = meshData.GetVertexData<float3>(0);
                            positions.CopyFrom(allVerts);
                            var texCoords  = meshData.GetVertexData<float2>(1);
                            texCoords.CopyFrom(allUVs);
                            var triIndices = meshData.GetIndexData<uint>();
                            triIndices.CopyFrom(allFaceIndices);

                            int subMeshIndex = 0;
                            vertOffset = 0;
                            faceIdxOffset = 0;

                            for (int layer = 0;layer < count;layer++)
                            {
                                if ((layerMask & (1 << layer)) != 0)
                                {
                                    materialArr[subMeshIndex] = MaterialManager.GetBlockMaterial(ChunkRender.TYPES[layer]);

                                    int vertCount    = visualBuffer[layer].vert.Length;
                                    int faceIdxCount = visualBuffer[layer].face.Length;

                                    meshData.SetSubMesh(subMeshIndex, new(faceIdxOffset, faceIdxCount){ vertexCount = vertCount });

                                    vertOffset    += vertCount;
                                    faceIdxOffset += faceIdxCount;

                                    subMeshIndex++;
                                }
                            }

                            var visualMesh = new Mesh { subMeshCount = layerCount };

                            Mesh.ApplyAndDisposeWritableMeshData(meshDataArr, visualMesh);

                            visualMesh.RecalculateNormals();
                            visualMesh.RecalculateBounds();

                            chunkRender.GetComponent<MeshFilter>().sharedMesh = visualMesh;
                            chunkRender.GetComponent<MeshRenderer>().sharedMaterials = materialArr;

                            // TODO Collider Mesh

                            lock (chunksBeingBuilt)
                            {
                                chunksBeingBuilt.Remove(chunkRender);
                            }
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
                        lock (chunksBeingBuilt)
                        {
                            chunksBeingBuilt.Remove(chunkRender);
                        }
                        chunkRender.State = BuildState.Cancelled;
                    }
                }

            }, ts.Token);
        }
        #endregion

        #region Chunk updates
        Action<ReceiveChunkColumnEvent> columnCallBack1;
        Action<UnloadChunkColumnEvent> columnCallBack2;
        Action<BlockUpdateEvent> blockCallBack;
        Action<BlocksUpdateEvent> blocksCallBack;

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
            World world = game?.GetWorld();
            if (world is null) return;

            // Add nearby chunks
            var location = game.GetCurrentLocation();
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
                            int chunkMask = world[chunkX, chunkZ].ChunkMask;
                            // Create it and add the whole column to render list...
                            columnRender = GetChunkColumn(chunkX, chunkZ, true);
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
            World world = game?.GetWorld();
            if (world is null) return;

            // Add nearby chunks
            var location   = game.GetCurrentLocation();
            int unloadDist = Mathf.RoundToInt(CornCraft.MCSettings_RenderDistance * 2F);

            var xs = columns.Keys.ToArray();

            foreach (var chunkX in xs)
            {
                if (Mathf.Abs(location.ChunkX - chunkX) > unloadDist)
                {
                    UnloadChunkColumns(chunkX);
                }
                else
                {
                    var zs = columns[chunkX].Keys.ToArray();

                    foreach (var chunkZ in zs)
                    {
                        if (Mathf.Abs(location.ChunkX - chunkX) > unloadDist)
                        {
                            UnloadChunkColumn(chunkX, chunkZ);
                        }
                    }
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

        public void QueueChunkBuildIfNotEmpty(ChunkRender chunkRender)
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
            
            lock (chunksBeingBuilt)
            {
                chunksBeingBuilt.Clear();
            }

            // Clear all chunk columns in world
            var xs = columns.Keys.ToArray();

            foreach (var chunkX in xs)
                UnloadChunkColumns(chunkX);

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

        void Start()
        {
            game = CornClient.Instance;

            // Register event callbacks
            EventManager.Instance.Register<ReceiveChunkColumnEvent>(columnCallBack1 = (e) => {
                // TODO Implement

            });

            EventManager.Instance.Register<UnloadChunkColumnEvent>(columnCallBack2 = (e) => {
                UnloadChunkColumn(e.chunkX, e.chunkZ);
            });

            EventManager.Instance.Register<BlockUpdateEvent>(blockCallBack = (e) => {
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

            });

            EventManager.Instance.Register<BlocksUpdateEvent>(blocksCallBack = (e) => {
                World world = game?.GetWorld();
                if (world is null) return;
                
                foreach (var loc in e.locations)
                {
                    int chunkX = loc.ChunkX, chunkY = loc.ChunkY, chunkZ = loc.ChunkZ;
                    var column = GetChunkColumn(loc.ChunkX, loc.ChunkZ, false);

                    if (column is not null) // Queue this chunk to rebuild list...
                    {   // Create the chunk render object if not present (previously empty)
                        var chunk = column.GetChunk(chunkY, true);
                        world[chunkX, chunkZ].ChunkMask |= 1 << chunkY;

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

            });

        }

        public const int BuildCountLimit = 8;
        private float operationCooldown  = 0;
        private int   operationAction    = 0;

        void FixedUpdate()
        {
            int newCount;

            lock (chunksBeingBuilt)
            {
                newCount = BuildCountLimit - chunksBeingBuilt.Count;
            }

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
                    case 2:
                        // TODO RemoveDeadBuilds();
                        break;
                }
                
                operationAction = (operationAction + 1) % 3;

                operationCooldown = 0.5F;
            }

            operationCooldown -= Time.fixedDeltaTime;

        }
        #endregion

    }

}