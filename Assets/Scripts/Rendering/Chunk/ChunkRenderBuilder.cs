using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;

using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    public class ChunkRenderBuilder
    {
        private static readonly int WATER_LAYER_INDEX = ChunkRender.TypeIndex(RenderType.WATER);
        private static readonly int LAVA_LAYER_INDEX  = ChunkRender.TypeIndex(RenderType.SOLID);

        private const int MOVEMENT_RADIUS = 3;
        public const float MOVEMENT_RADIUS_SQR_MINI = (MOVEMENT_RADIUS - 0.5F) * (MOVEMENT_RADIUS - 0.5F);
        public const float MOVEMENT_RADIUS_SQR_PLUS = (MOVEMENT_RADIUS + 0.5F) * (MOVEMENT_RADIUS + 0.5F);
        
        private static readonly List<BlockLoc> validOffsets = ComputeOffsets();

        private static List<BlockLoc> ComputeOffsets()
        {
            var offsets = new List<BlockLoc>();
            for (int x = -MOVEMENT_RADIUS; x <= MOVEMENT_RADIUS; x++)
                for (int y = -MOVEMENT_RADIUS; y <= MOVEMENT_RADIUS; y++)
                    for (int z = -MOVEMENT_RADIUS; z <= MOVEMENT_RADIUS; z++)
                        if (x * x + y * y + z * z <= MOVEMENT_RADIUS_SQR_MINI)
                            offsets.Add(new BlockLoc(x, y, z));
            return offsets;
        }

        private readonly Dictionary<int, BlockStateModel> modelTable;
        private readonly Dictionary<int, BlockNeighborCheck> cullingRules;
        
        private static readonly HashSet<ResourceLocation> GLASS_BLOCK_IDS = new()
        {
            new("glass"), new("white_stained_glass"), new("orange_stained_glass"),
            new("magenta_stained_glass"), new("light_blue_stained_glass"), new("yellow_stained_glass"),
            new("lime_stained_glass"), new("pink_stained_glass"), new("gray_stained_glass"),
            new("light_gray_stained_glass"), new("cyan_stained_glass"), new("purple_stained_glass"),
            new("blue_stained_glass"), new("brown_stained_glass"), new("green_stained_glass"),
            new("red_stained_glass"), new("black_stained_glass")
        };
        
        private static readonly ResourceLocation ICE_BLOCK_ID = new("ice");

        public ChunkRenderBuilder(Dictionary<int, BlockStateModel> modelTable)
        {
            this.modelTable = modelTable;
            var palette = BlockStatePalette.INSTANCE;
            
            cullingRules = new();

            // Glass blocks
            BlockNeighborCheck glassNeighborCheck = (_, neighbor) =>
                !GLASS_BLOCK_IDS.Contains(neighbor.BlockId) && !neighbor.MeshFaceOcclusionSolid;

            foreach (var stateId in GLASS_BLOCK_IDS
                         .SelectMany(blockId => palette.GetAllNumIds(blockId)))
                cullingRules[stateId] = glassNeighborCheck;
            
            // Ice block
            BlockNeighborCheck iceNeighborCheck = (_, neighbor) =>
                ICE_BLOCK_ID != neighbor.BlockId && !neighbor.MeshFaceOcclusionSolid;

            foreach (var stateId in palette.GetAllNumIds(ICE_BLOCK_ID))
                cullingRules[stateId] = iceNeighborCheck;
        }

        private static long GetSeedForCoords(int i, int j, int k)
        {
            long l = (i * 3129871) ^ k * 116129781L ^ j;
            l = l * l * 42317861L + l * 11L;
            return l >> 16;
        }

        public static float3 GetBlockOffsetInChunk(OffsetType offsetType, int chunkX, int chunkZ, int blocX, int blocY, int blocZ)
        {
            if (offsetType is OffsetType.XZ or OffsetType.XZ_BoundingBox) // Apply random offset on horizontal directions
            {
                var oSeed = GetSeedForCoords((chunkX << 4) + blocX, 0, (chunkZ << 4) + blocZ);
                var ox = ((oSeed & 15L)      / 15.0F - 0.5F) * 0.5F; // -0.25F to 0.25F
                var oz = ((oSeed >> 8 & 15L) / 15.0F - 0.5F) * 0.5F; // -0.25F to 0.25F
                
                return new float3(blocZ + oz, blocY, blocX + ox); // Swap x and z
            }
            if (offsetType == OffsetType.XYZ) // Apply random offset on all directions
            {
                var oSeed = GetSeedForCoords((chunkX << 4) + blocX, 0, (chunkZ << 4) + blocZ);
                var ox = ((oSeed & 15L)      / 15.0F - 0.5F) * 0.5F; // -0.25F to 0.25F
                var oz = ((oSeed >> 8 & 15L) / 15.0F - 0.5F) * 0.5F; // -0.25F to 0.25F
                var oy = ((oSeed >> 4 & 15L) / 15.0F - 1.0F) * 0.2F; //  -0.2F to    0F

                return new float3(blocZ + oz, blocY + oy, blocX + ox); // Swap x and z
            }
            
            return new float3(blocZ, blocY, blocX); // Swap x and z
        }

        public static float3 GetBlockOffsetInBlock(OffsetType offsetType, int chunkX, int chunkZ, int blocX, int blocZ)
        {
            if (offsetType is OffsetType.XZ or OffsetType.XZ_BoundingBox) // Apply random offset on horizontal directions
            {
                var oSeed = GetSeedForCoords((chunkX << 4) + blocX, 0, (chunkZ << 4) + blocZ);
                var ox = ((oSeed & 15L)      / 15.0F - 0.5F) * 0.5F; // -0.25F to 0.25F
                var oz = ((oSeed >> 8 & 15L) / 15.0F - 0.5F) * 0.5F; // -0.25F to 0.25F
                
                return new float3(oz, 0F, ox); // Swap x and z
            }
            if (offsetType == OffsetType.XYZ) // Apply random offset on all directions
            {
                var oSeed = GetSeedForCoords((chunkX << 4) + blocX, 0, (chunkZ << 4) + blocZ);
                var ox = ((oSeed & 15L)      / 15.0F - 0.5F) * 0.5F; // -0.25F to 0.25F
                var oz = ((oSeed >> 8 & 15L) / 15.0F - 0.5F) * 0.5F; // -0.25F to 0.25F
                var oy = ((oSeed >> 4 & 15L) / 15.0F - 1.0F) * 0.2F; //  -0.2F to    0F

                return new float3(oz, oy, ox); // Swap x and z
            }
            
            return float3.zero; // Swap x and z
        }

        public ChunkBuildResult Build(ChunkBuildData data, ChunkRender chunkRender)
        {
            try
            {
                var ts = chunkRender.TokenSource;

                int count = ChunkRender.TYPES.Length, layerMask = 0;
                
                // Set up vertex counter
                var vertexCount = new int[count];
                for (int i = 0; i < count; i++)
                {
                    vertexCount[i] = 0;
                }
                int colliderVertexCount = 0;

                var blocs = data.BlockStates;
                var stids = data.BlockStateIds;
                var light = data.Light;
                var allColors = data.Color;

                var stateTable = BlockStatePalette.INSTANCE;

                // Collect vertex count and layer mask before collecting actual vertex data
                for (int x = 1; x <= Chunk.SIZE; x++) // From 1 to 16, because we have a padding for blocks in adjacent chunks
                {
                    for (int y = 1; y <= Chunk.SIZE; y++)
                    {
                        for (int z = 1; z <= Chunk.SIZE; z++)
                        {
                            var state = data.BlockStates[x, y, z];
                            var stateId = data.BlockStateIds[x, y, z];

                            if (state.InLiquid) // Build liquid here
                            {
                                var liquidNeighborCheck = state.InWater ? BlockNeighborChecks.WATER_SURFACE : BlockNeighborChecks.LAVA_SURFACE;
                                int liquidCullFlags = getCullFlags(x, y, z, state, liquidNeighborCheck);

                                if (liquidCullFlags != 0)
                                {
                                    var liquidLayerIndex = state.InWater ? WATER_LAYER_INDEX : LAVA_LAYER_INDEX;
                                    vertexCount[liquidLayerIndex] += FluidGeometry.GetVertexCount(liquidCullFlags);
                                    
                                    layerMask |= 1 << liquidLayerIndex;
                                }
                            }

                            // If air-like (air, water block, etc), ignore it...
                            if (state.NoSolidMesh) continue;
                            
                            var neighborCheck = cullingRules.GetValueOrDefault(stateId, BlockNeighborChecks.NON_FULL_SOLID);
                            int cullFlags = getCullFlags(x, y, z, state, neighborCheck);
                            
                            if (cullFlags != 0 && modelTable.ContainsKey(stateId)) // This chunk has at least one visible block of this render type
                            {
                                int layerIndex = ChunkRender.TypeIndex(modelTable[stateId].RenderType);

                                var models = modelTable[stateId].Geometries;
                                var chosen = (x + y + z) % models.Length;

                                if (state.NoCollision)
                                {
                                    vertexCount[layerIndex] += models[chosen].GetVertexCount(cullFlags);
                                }
                                else
                                {
                                    int vertCount = models[chosen].GetVertexCount(cullFlags);
                                    vertexCount[layerIndex] += vertCount;
                                    colliderVertexCount += vertCount;
                                }
                                
                                layerMask |= 1 << layerIndex;
                            }
                        }
                    }
                }

                // Create vertex buffers for containing vertex data
                var visualBuffer = new VertexBuffer[count];
                for (int layer = 0; layer < count; layer++)
                {
                    if ((layerMask & (1 << layer)) != 0)
                    {
                        visualBuffer[layer] = new(vertexCount[layer]);
                    }
                }
                float3[] colliderVerts = new float3[colliderVertexCount];

                // Setup vertex offset
                var vertOffset = new uint[count];
                for (int layer = 0; layer < count; layer++)
                {
                    vertOffset[layer] = 0;
                }
                uint colliderVertOffset = 0;

                // Build mesh vertices block by block
                for (int x = 1; x <= Chunk.SIZE; x++) // From 1 to 16, because we have a padding for blocks in adjacent chunks
                {
                    int blocX = x - 1;
                    for (int y = 1; y <= Chunk.SIZE; y++)
                    {
                        int blocY = y - 1;
                        for (int z = 1; z <= Chunk.SIZE; z++)
                        {
                            int blocZ = z - 1;
                            var state = data.BlockStates[x, y, z];
                            var stateId = data.BlockStateIds[x, y, z];

                            if (state.InLiquid) // Build liquid here
                            {
                                var liquidNeighborCheck = state.InWater ? BlockNeighborChecks.WATER_SURFACE : BlockNeighborChecks.LAVA_SURFACE;
                                int liquidCullFlags = getCullFlags(x, y, z, state, liquidNeighborCheck);

                                if (liquidCullFlags != 0)
                                {
                                    var liquidHeights = new byte[] { 15, 15, 15, 15, 15, 15, 15, 15, 15 }; // TODO: Implement
                                    var liquidLayerIndex = state.InWater ? WATER_LAYER_INDEX : LAVA_LAYER_INDEX;
                                    var liquidTexture = FluidGeometry.LiquidTextures[state.InWater ? 0 : 1];
                                    var lights = getCornerLights(x, y, z);

                                    FluidGeometry.Build(visualBuffer[liquidLayerIndex], ref vertOffset[liquidLayerIndex], new float3(blocZ, blocY, blocX),
                                            liquidTexture, liquidHeights, liquidCullFlags, lights, new float3(1F));
                                }
                            }

                            // If air-like (air, water block, etc), ignore it...
                            if (state.NoSolidMesh) continue;

                            var neighborCheck = cullingRules.GetValueOrDefault(stateId, BlockNeighborChecks.NON_FULL_SOLID);
                            int cullFlags = getCullFlags(x, y, z, state, neighborCheck);
                            
                            if (cullFlags != 0 && modelTable.ContainsKey(stateId)) // This chunk has at least one visible block of this render type
                            {
                                var renderType = modelTable[stateId].RenderType;
                                int layerIndex = ChunkRender.TypeIndex(renderType);
                                var aoIntensity = 0.2F;

                                var datFormat = BlockGeometry.VertexDataFormat.Color_Light;
                                if (renderType == RenderType.FOLIAGE)
                                {
                                    datFormat = BlockGeometry.VertexDataFormat.Color_Light_BlockNormal;
                                }
                                else if (renderType is RenderType.PLANTS or RenderType.TALL_PLANTS)
                                {
                                    datFormat = BlockGeometry.VertexDataFormat.Color_Light_CrossNormal;

                                    aoIntensity = 0.15F;
                                }

                                var models = modelTable[stateId].Geometries;
                                var chosen = (x + y + z) % models.Length;
                                var color  = allColors[blocX, blocY, blocZ];
                                var lights = getCornerLights(x, y, z);
                                var aoMask = getNeighborCastAOMask(x, y, z);

                                float3 posOffset = GetBlockOffsetInChunk(modelTable[stateId].OffsetType, chunkRender.ChunkX, chunkRender.ChunkZ, blocX, blocY, blocZ);

                                if (state.NoCollision)
                                {
                                    models[chosen].Build(visualBuffer[layerIndex], ref vertOffset[layerIndex],
                                            posOffset , cullFlags, aoMask, aoIntensity, lights, color, datFormat);
                                }
                                else
                                {
                                    models[chosen].BuildWithCollider(visualBuffer[layerIndex], ref vertOffset[layerIndex], colliderVerts,
                                            ref colliderVertOffset, posOffset, cullFlags, aoMask, aoIntensity, lights, color, datFormat);
                                }
                            }
                        }
                    }
                }

                if (ts.IsCancellationRequested)
                    return ChunkBuildResult.Cancelled;

                if (layerMask == 0) // Skip empty chunks...
                {
                    Loom.QueueOnMainThread(() => 
                    {
                        if (!chunkRender || !chunkRender.gameObject)
                            return;

                        // TODO Improve below cleaning
                        Profiler.BeginSample("Clear chunk render mesh");

                        var mesh = chunkRender.GetComponent<MeshFilter>().sharedMesh;
                        if (mesh)
                        {
                            mesh.Clear(false);
                        }
                        chunkRender.ClearCollider();
                    
                        Profiler.EndSample();

                        chunkRender.State = ChunkBuildState.Ready;
                    });
                }
                else
                {
                    Loom.QueueOnMainThreadMinor(() => {
                        if (!chunkRender || !chunkRender.gameObject)
                            return;
                        
                        Profiler.BeginSample("Update chunk render mesh");

                        Profiler.BeginSample("Build and apply mesh data");

                        Profiler.BeginSample("Build mesh data");

                        // Visual Mesh
                        // Count layers, vertices and face indices
                        int layerCount = 0, totalVertCount = 0;
                        for (int layer = 0; layer < count; layer++)
                        {
                            if ((layerMask & (1 << layer)) != 0)
                            {
                                layerCount++;
                                totalVertCount += visualBuffer[layer].vert.Length;
                            }
                        }
                        
                        var meshDataArr  = Mesh.AllocateWritableMeshData(1);
                        var materialArr  = new Material[layerCount];
                        var meshData = meshDataArr[0];
                        meshData.subMeshCount = layerCount;

                        // Set mesh attributes
                        var visVertAttrs = new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                        visVertAttrs[0]  = new(VertexAttribute.Position,  dimension: 3, stream: 0);
                        visVertAttrs[1]  = new(VertexAttribute.TexCoord0, dimension: 3, stream: 1);
                        visVertAttrs[2]  = new(VertexAttribute.TexCoord1, dimension: 4, stream: 2);
                        visVertAttrs[3]  = new(VertexAttribute.Color,     dimension: 4, stream: 3);

                        meshData.SetVertexBufferParams(totalVertCount, visVertAttrs);
                        meshData.SetIndexBufferParams(totalVertCount / 2 * 3, IndexFormat.UInt32);

                        visVertAttrs.Dispose();

                        // Prepare source data arrays
                        var allVerts = new float3[totalVertCount];
                        var allUVs   = new float3[totalVertCount];
                        var allAnims = new float4[totalVertCount];
                        var allTints = new float4[totalVertCount];

                        int curVertOffset = 0;
                        for (int layer = 0; layer < count; layer++)
                        {
                            if ((layerMask & (1 << layer)) != 0)
                            {
                                visualBuffer[layer].vert.CopyTo(allVerts, curVertOffset);
                                visualBuffer[layer].txuv.CopyTo(allUVs,   curVertOffset);
                                visualBuffer[layer].uvan.CopyTo(allAnims, curVertOffset);
                                visualBuffer[layer].tint.CopyTo(allTints, curVertOffset);

                                curVertOffset += visualBuffer[layer].vert.Length;
                            }
                        }

                        // Copy the source arrays to mesh data
                        var positions  = meshData.GetVertexData<float3>(0);
                        positions.CopyFrom(allVerts);
                        var texCoords  = meshData.GetVertexData<float3>(1);
                        texCoords.CopyFrom(allUVs);
                        var texAnims   = meshData.GetVertexData<float4>(2);
                        texAnims.CopyFrom(allAnims);
                        var vertColors = meshData.GetVertexData<float4>(3);
                        vertColors.CopyFrom(allTints);

                        // Generate triangle arrays
                        var triIndices = meshData.GetIndexData<uint>();
                        uint vi = 0; int ti = 0;
                        for (; vi < totalVertCount; vi += 4U, ti += 6)
                        {
                            triIndices[ti]     = vi;
                            triIndices[ti + 1] = vi + 3U;
                            triIndices[ti + 2] = vi + 2U;
                            triIndices[ti + 3] = vi;
                            triIndices[ti + 4] = vi + 1U;
                            triIndices[ti + 5] = vi + 3U;
                        }

                        int subMeshIndex = 0;
                        curVertOffset = 0;
                        for (int layer = 0; layer < count; layer++)
                        {
                            if ((layerMask & (1 << layer)) != 0)
                            {
                                materialArr[subMeshIndex] = CornApp.CurrentClient!.ChunkMaterialManager.GetAtlasMaterial(ChunkRender.TYPES[layer]);
                                int vertCount = visualBuffer[layer].vert.Length;
                                meshData.SetSubMesh(subMeshIndex, new(curVertOffset / 2 * 3, vertCount / 2 * 3){ vertexCount = vertCount });
                                curVertOffset += vertCount;
                                subMeshIndex++;
                            }
                        }

                        Profiler.EndSample(); // "Build mesh data"
                        Profiler.BeginSample("Create mesh object");

                        var visualMesh = new Mesh { subMeshCount = layerCount };
                        Mesh.ApplyAndDisposeWritableMeshData(meshDataArr, visualMesh);

                        Profiler.EndSample(); // "Create mesh object"

                        Profiler.EndSample(); // "Build and apply mesh data"

                        visualMesh.RecalculateNormals();
                        visualMesh.RecalculateBounds();
                        //visualMesh.RecalculateTangents();

                        chunkRender.GetComponent<MeshFilter>().sharedMesh = visualMesh;
                        chunkRender.GetComponent<MeshRenderer>().sharedMaterials = materialArr;

                        // Collider Mesh
                        int colVertCount = colliderVerts.Length;

                        if (colVertCount > 0)
                        {
                            Profiler.BeginSample("Build and apply mesh data");

                            Profiler.BeginSample("Build mesh data");

                            var colMeshDataArr  = Mesh.AllocateWritableMeshData(1);
                            var colMeshData = colMeshDataArr[0];
                            colMeshData.subMeshCount = 1;

                            // Set mesh attributes
                            var colVertAttrs = new NativeArray<VertexAttributeDescriptor>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                            colVertAttrs[0]  = new(VertexAttribute.Position, dimension: 3);

                            colMeshData.SetVertexBufferParams(colVertCount, colVertAttrs);
                            colMeshData.SetIndexBufferParams(colVertCount / 2 * 3, IndexFormat.UInt32);

                            colVertAttrs.Dispose();

                            // Copy the source arrays to mesh data
                            var colPositions  = colMeshData.GetVertexData<float3>(0);
                            colPositions.CopyFrom(colliderVerts);

                            // Generate triangle arrays
                            var colTriIndices = colMeshData.GetIndexData<uint>();
                            vi = 0; ti = 0;
                            for (; vi < colliderVerts.Length; vi += 4U, ti += 6)
                            {
                                colTriIndices[ti]     = vi;
                                colTriIndices[ti + 1] = vi + 3U;
                                colTriIndices[ti + 2] = vi + 2U;
                                colTriIndices[ti + 3] = vi;
                                colTriIndices[ti + 4] = vi + 1U;
                                colTriIndices[ti + 5] = vi + 3U;
                            }

                            colMeshData.SetSubMesh(0, new(0, (colVertCount / 2) * 3){ vertexCount = colVertCount });

                            Profiler.EndSample(); // "Build mesh data"
                            Profiler.BeginSample("Create mesh object");

                            var colliderMesh = new Mesh { subMeshCount = 1 };
                            Mesh.ApplyAndDisposeWritableMeshData(colMeshDataArr, colliderMesh);

                            Profiler.EndSample(); // "Create mesh object"

                            Profiler.EndSample(); // "Build and apply mesh data"

                            colliderMesh.RecalculateNormals();
                            colliderMesh.RecalculateBounds();

                            chunkRender.UpdateCollider(colliderMesh);
                        }
                        else
                        {
                            chunkRender.ClearCollider();
                        }
                        
                        Profiler.EndSample(); // "Update chunk render mesh"
                        
                        chunkRender.State = ChunkBuildState.Ready;

                        chunkRender.gameObject.SetActive(true);
                    });
                }

                return ChunkBuildResult.Succeeded;

                // Value range of each corner light: [0, 15]
                float[] getCornerLights(int x, int y, int z)
                {
                    var result = new float[8];

                    for (int y_ = 0; y_ < 3; y_++) for (int z_ = 0; z_ < 3; z_++) for (int x_ = 0; x_ < 3; x_++)
                    {
                        byte sample = light[x + x_ - 1, y + y_ - 1, z + z_ - 1];

                        if (y_ != 2 && z_ != 2 && x_ != 2) // [0] x0z0 y0
                        {
                            result[0] += sample;
                        }
                        if (y_ != 2 && z_ != 2 && x_ != 0) // [1] x1z0 y0
                        {
                            result[1] += sample;
                        }
                        if (y_ != 2 && z_ != 0 && x_ != 2) // [2] x0z1 y0
                        {
                            result[2] += sample;
                        }
                        if (y_ != 2 && z_ != 0 && x_ != 0) // [3] x1z1 y0
                        {
                            result[3] += sample;
                        }
                        if (y_ != 0 && z_ != 2 && x_ != 2) // [4] x0z0 y1
                        {
                            result[4] += sample;
                        }
                        if (y_ != 0 && z_ != 2 && x_ != 0) // [5] x1z0 y1
                        {
                            result[5] += sample;
                        }
                        if (y_ != 0 && z_ != 0 && x_ != 2) // [6] x0z1 y1
                        {
                            result[6] += sample;
                        }
                        if (y_ != 0 && z_ != 0 && x_ != 0) // [7] x1z1 y1
                        {
                            result[7] += sample;
                        }
                    }

                    return result.Select(f => f / 8F).ToArray();
                }

                int getCullFlags(int x, int y, int z, BlockState self, BlockNeighborCheck check)
                {
                    int cullFlags = 0;

                    if (check(self, blocs[x, y + 1, z])) // Up
                        cullFlags |= 1 << 0;

                    if (check(self, blocs[x, y - 1, z])) // Down
                        cullFlags |= 1 << 1;
                    
                    if (check(self, blocs[x, y, z + 1])) // South
                        cullFlags |= 1 << 2;

                    if (check(self, blocs[x, y, z - 1])) // North
                        cullFlags |= 1 << 3;
                    
                    if (check(self, blocs[x + 1, y, z])) // East
                        cullFlags |= 1 << 4;

                    if (check(self, blocs[x - 1, y, z])) // West
                        cullFlags |= 1 << 5;
                    
                    return cullFlags;
                }

                int getNeighborCastAOMask(int x, int y, int z)
                {
                    int result = 0;

                    for (int y_ = 0; y_ < 3; y_++) for (int z_ = 0; z_ < 3; z_++) for (int x_ = 0; x_ < 3; x_++)
                    {
                        if (stateTable.GetByNumId(stids[x + x_ - 1, y + y_ - 1, z + z_ - 1]).AmbientOcclusionSolid)
                        {
                            result |= 1 << (y_ * 9 + z_ * 3 + x_);
                        }
                    }

                    return result;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.Message}: {e.StackTrace}");

                return ChunkBuildResult.Cancelled;
            }
        }

        private static BoxCollider[] GetBoxCollidersAt(BlockState state, BlockLoc blockLoc,
            Vector3Int originOffset, Vector3? blockOffset, GameObject solidGameObject, GameObject fluidGameObject)
        {
            var aabbs = state.Shape.ColliderAABBs ?? state.Shape.AABBs;
            var colliderCount = (state.NoSolidMesh ? 0 : aabbs.Count) + (state.InLiquid ? 1 : 0);
            
            if (colliderCount == 0)
                return Array.Empty<BoxCollider>();
            
            var boxColliders = new BoxCollider[colliderCount];
            int i = 0;

            var noCollision = state.NoCollision;
            var position = CoordConvert.MC2Unity(originOffset, blockLoc.ToLocation());
            
            foreach (var aabb in aabbs) // Add block shape colliders
            {
                var collider = solidGameObject.AddComponent<BoxCollider>();
                
                collider.size = new(aabb.SizeZ, aabb.SizeY, aabb.SizeX);
                collider.center = position + new Vector3(aabb.CenterZ, aabb.CenterY, aabb.CenterX);
                if (blockOffset.HasValue)
                    collider.center += blockOffset.Value;
                
                boxColliders[i++] = collider;
                collider.isTrigger = noCollision;
            }
            if (state.InLiquid) // Add liquid collider
            {
                var collider = fluidGameObject.AddComponent<BoxCollider>();
                
                collider.size = new(1F, 1F, 1F);
                collider.center = new Vector3(0.5F, 0.5F, 0.5F) + position;
                boxColliders[i] = collider; // Don't increase since i is not used afterwards
                collider.isTrigger = true;
            }
                
            return boxColliders;
        }

        public static bool OffsetTypeAffectsAABB(OffsetType offsetType)
        {
            return offsetType == OffsetType.XZ_BoundingBox;
        }

        public static void BuildTerrainColliderBoxes(World world, BlockLoc playerBlockLoc, Vector3Int originOffset,
            GameObject solidGameObject, GameObject fluidGameObject, Dictionary<BlockLoc, BoxCollider[]> colliderList)
        {
            foreach (var blockLoc in colliderList.Keys.ToList()
                         .Where(blockLoc => playerBlockLoc.SqrDistanceTo(blockLoc) > MOVEMENT_RADIUS_SQR_PLUS))
            {
                // Remove colliders at this location
                foreach (var collider in colliderList[blockLoc])
                {
                    UnityEngine.Object.Destroy(collider);
                }
                colliderList.Remove(blockLoc);
            }

            var availableBlockLocs = validOffsets.Select(offset => offset + playerBlockLoc);
            var stateModelTable = ResourcePackManager.Instance.StateModelTable;

            foreach (var blockLoc in availableBlockLocs)
            {
                if (colliderList.ContainsKey(blockLoc))
                    continue;

                var block = world.GetBlock(blockLoc);
                Vector3? blockOffset = stateModelTable.TryGetValue(block.StateId, out var stateModel)
                    && OffsetTypeAffectsAABB(stateModel.OffsetType)
                    ? (Vector3) GetBlockOffsetInBlock(stateModel.OffsetType, blockLoc.X >> 4,
                        blockLoc.Z >> 4, blockLoc.X & 0xF, blockLoc.Z & 0xF) : null;

                colliderList.Add(blockLoc, GetBoxCollidersAt(block.State, blockLoc,
                    originOffset, blockOffset, solidGameObject, fluidGameObject));
            }
        }
        
        public static void BuildTerrainColliderBoxesAt(World world, BlockLoc blockLoc, Vector3Int originOffset,
            GameObject solidGameObject, GameObject fluidGameObject, Dictionary<BlockLoc, BoxCollider[]> colliderList)
        {
            if (colliderList.ContainsKey(blockLoc))
            {
                // Remove colliders at this location
                foreach (var collider in colliderList[blockLoc])
                {
                    UnityEngine.Object.Destroy(collider); 
                }
                colliderList.Remove(blockLoc);
            }

            var stateModelTable = ResourcePackManager.Instance.StateModelTable;
            var block = world.GetBlock(blockLoc);
            Vector3? blockOffset = stateModelTable.TryGetValue(block.StateId, out var stateModel)
                && OffsetTypeAffectsAABB(stateModel.OffsetType)
                ? (Vector3) GetBlockOffsetInBlock(stateModel.OffsetType, blockLoc.X >> 4,
                    blockLoc.Z >> 4, blockLoc.X & 0xF, blockLoc.Z & 0xF) : null;

            colliderList.Add(blockLoc, GetBoxCollidersAt(block.State, blockLoc,
                originOffset, blockOffset, solidGameObject, fluidGameObject));
        }
    }
}