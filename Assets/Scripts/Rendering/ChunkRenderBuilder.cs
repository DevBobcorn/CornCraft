#nullable enable
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;

using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    public class ChunkRenderBuilder
    {
        private static readonly int waterLayerIndex = ChunkRender.TypeIndex(RenderType.WATER);
        private static readonly int lavaLayerIndex  = ChunkRender.TypeIndex(RenderType.SOLID);
        public const int MOVEMENT_RADIUS = 3;
        public const int MOVEMENT_RADIUS_SQR = MOVEMENT_RADIUS * MOVEMENT_RADIUS;

        private static Location GetLocationInChunkRender(ChunkRender chunk, int x, int y, int z, int offsetY)
        {   // Offset y coordinate by the current dimension's minY...
            return new(chunk.ChunkX * Chunk.SizeX + x, chunk.ChunkY * Chunk.SizeY + y + offsetY, chunk.ChunkZ * Chunk.SizeZ + z);
        }

        private Dictionary<int, BlockStateModel> modelTable;

        public ChunkRenderBuilder(Dictionary<int, BlockStateModel> modelTable)
        {
            this.modelTable = modelTable;
        }

        private float GetLight(World world, Location loc)
        {
            //return Mathf.Max(world.GetBlockLight(loc), world.GetSkyLight(loc)) / 15F;
            return Mathf.Max(world.GetBlockLight(loc), 4) / 15F;
        }

        public ChunkBuildResult Build(World world, Chunk chunkData, ChunkRender chunkRender)
        {
            try
            {
                var ts = chunkRender.TokenSource;

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
                                return ChunkBuildResult.Cancelled;

                            var loc = GetLocationInChunkRender(chunkRender, x, y, z, offsetY);

                            var bloc = chunkData[x, y, z];
                            var state = bloc.State;
                            var stateId = bloc.StateId;

                            if (state.InLiquid) // Build liquid here
                            {
                                var neighborCheck = state.InWater ? BlockNeighborChecks.WATER_SURFACE : BlockNeighborChecks.LAVA_SURFACE;
                                int liquidCullFlags = chunkData.GetCullFlags(loc, bloc, neighborCheck);
                                var liquidHeights = chunkData.GetLiquidHeights(loc);

                                var liquidLayerIndex = state.InWater ? waterLayerIndex : lavaLayerIndex;
                                var liquidTexture = FluidGeometry.LiquidTextures[state.InWater ? 0 : 1];

                                if (liquidCullFlags != 0)
                                {
                                    FluidGeometry.Build(ref visualBuffer[liquidLayerIndex], liquidTexture, x, y, z, liquidHeights, liquidCullFlags, world.GetWaterColor(loc));
                                    
                                    layerMask |= (1 << liquidLayerIndex);
                                }
                            }

                            // If air-like (air, water block, etc), ignore it...
                            if (state.LikeAir) continue;
                            
                            int cullFlags = chunkData.GetCullFlags(loc, bloc, BlockNeighborChecks.NON_FULL_SOLID); // TODO Make it more accurate
                            
                            if (cullFlags != 0 && modelTable.ContainsKey(stateId)) // This chunk has at least one visible block of this render type
                            {
                                int layerIndex = ChunkRender.TypeIndex(modelTable[stateId].RenderType);

                                var models = modelTable[stateId].Geometries;
                                var chosen = (x + y + z) % models.Length;
                                var color  = BlockStatePalette.INSTANCE.GetBlockColor(stateId, world, loc, state);

                                if (state.NoCollision)
                                    models[chosen].Build(ref visualBuffer[layerIndex], new(z, y, x), cullFlags, color);
                                else
                                    models[chosen].BuildWithCollider(ref visualBuffer[layerIndex], ref colliderVerts, new(z, y, x), cullFlags, color);
                                
                                layerMask |= (1 << layerIndex);
                            }
                        }
                    }
                }

                if (layerMask == 0) // Skip empty chunks...
                {
                    Loom.QueueOnMainThread(() => {
                        if (chunkRender == null || chunkRender.gameObject == null)
                            return;

                        // TODO Improve below cleaning
                        chunkRender.GetComponent<MeshFilter>().sharedMesh?.Clear(false);
                        chunkRender.ClearCollider();

                        chunkRender.State = ChunkBuildState.Ready;
                    });
                
                    return ChunkBuildResult.Succeeded;
                }
                else
                {
                    Loom.QueueOnMainThread(() => {
                        if (chunkRender == null || chunkRender.gameObject == null)
                            return;

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
                        var meshData = meshDataArr[0];
                        meshData.subMeshCount = layerCount;

                        // Set mesh attributes
                        var visVertAttrs = new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                        visVertAttrs[0]  = new(VertexAttribute.Position,  dimension: 3, stream: 0);
                        visVertAttrs[1]  = new(VertexAttribute.TexCoord0, dimension: 3, stream: 1);
                        visVertAttrs[2]  = new(VertexAttribute.TexCoord1, dimension: 4, stream: 2);
                        visVertAttrs[3]  = new(VertexAttribute.Color,     dimension: 3, stream: 3);

                        meshData.SetVertexBufferParams(totalVertCount,          visVertAttrs);
                        meshData.SetIndexBufferParams((totalVertCount / 2) * 3, IndexFormat.UInt32);

                        visVertAttrs.Dispose();

                        // Prepare source data arrays
                        var allVerts = new float3[totalVertCount];
                        var allUVs   = new float3[totalVertCount];
                        var allAnims = new float4[totalVertCount];
                        var allTints = new float3[totalVertCount];

                        int vertOffset = 0;
                        for (int layer = 0;layer < count;layer++)
                        {
                            if ((layerMask & (1 << layer)) != 0)
                            {
                                visualBuffer[layer].vert.CopyTo(allVerts, vertOffset);
                                visualBuffer[layer].txuv.CopyTo(allUVs,   vertOffset);
                                visualBuffer[layer].uvan.CopyTo(allAnims, vertOffset);
                                visualBuffer[layer].tint.CopyTo(allTints, vertOffset);

                                vertOffset += visualBuffer[layer].vert.Length;
                            }
                        }

                        // Copy the source arrays to mesh data
                        var positions  = meshData.GetVertexData<float3>(0);
                        positions.CopyFrom(allVerts);
                        var texCoords  = meshData.GetVertexData<float3>(1);
                        texCoords.CopyFrom(allUVs);
                        var texAnims   = meshData.GetVertexData<float4>(2);
                        texAnims.CopyFrom(allAnims);
                        var vertColors = meshData.GetVertexData<float3>(3);
                        vertColors.CopyFrom(allTints);

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
                                materialArr[subMeshIndex] = CornApp.CurrentClient!.MaterialManager!.GetAtlasMaterial(ChunkRender.TYPES[layer]);
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
                        //visualMesh.RecalculateTangents();

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
                        
                        chunkRender.State = ChunkBuildState.Ready;
                    });

                    return ChunkBuildResult.Succeeded;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.Message}: {e.StackTrace}");

                return ChunkBuildResult.Cancelled;
            }
        }

        // For player movement, it is not favorable to use per-chunk mesh colliders
        // because player can get stuck on the edge of chunks due to a bug of Unity
        // (or say PhysX) bug, so we dynamically build the mesh collider around the
        // player as a solution to this. See the problem discussion at
        // https://forum.unity.com/threads/ball-rolling-on-mesh-hits-edges.772760/
        public void BuildTerrainCollider(World world, Location playerLoc, MeshCollider movementCollider, MeshCollider liquidCollider)
        {
            int offsetY = World.GetDimension().minY;
            
            float3[] movementVerts = { }, fluidVerts = { };

            for (int x = -MOVEMENT_RADIUS;x <= MOVEMENT_RADIUS;x++)
                for (int y = -MOVEMENT_RADIUS;y <= MOVEMENT_RADIUS;y++)
                    for (int z = -MOVEMENT_RADIUS;z <= MOVEMENT_RADIUS;z++)
                    {
                        if (x * x + y * y + z * z > MOVEMENT_RADIUS_SQR)
                            continue;

                        var loc  = playerLoc + new Location(x, y, z);
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
                        
                        if (cullFlags != 0 && modelTable.ContainsKey(stateId)) // This chunk has at least one visible block of this render type
                        {
                            // They all have the same collider so we just pick the 1st one
                            modelTable[stateId].Geometries[0].BuildCollider(ref movementVerts, new((float)loc.Z, (float)loc.Y, (float)loc.X), cullFlags);
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
        }

    }
}