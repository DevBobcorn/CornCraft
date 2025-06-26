#nullable enable
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    public static class BlockMeshBuilder
    {
        private static readonly Mesh EMPTY_BLOCK_MESH = new();
        
        private static readonly float[] FULL_CORNER_LIGHTS = { 1F, 1F, 1F, 1F, 1F, 1F, 1F, 1F };
        private static readonly float[] DUMMY_BLOCK_VERT_LIGHT = Enumerable.Repeat(0F, 8).ToArray();
        private static readonly byte[] FLUID_HEIGHTS = { 15, 15, 15, 15, 15, 15, 15, 15, 15 };
        
        private static void ClearBlockVisual(GameObject modelObject)
        {
            // Clear mesh if present
            if (modelObject.TryGetComponent<MeshFilter>(out var meshFilter))
            {
                meshFilter.sharedMesh = null;
            }
            
            // Clear children if present
            foreach (Transform t in modelObject.transform)
            {
                Object.Destroy(t.gameObject);
            }
        }

        /// <summary>
        /// Build item object from item stack. Returns true if item is not empty and successfully built
        /// </summary>
        public static void BuildBlockGameObject(GameObject modelObject, BlockState blockState, World world)
        {
            ClearBlockVisual(modelObject);
            
            if (!modelObject.TryGetComponent<MeshFilter>(out var meshFilter))
            {
                meshFilter = modelObject.AddComponent<MeshFilter>();
            }

            if (!modelObject.TryGetComponent<MeshRenderer>(out var meshRenderer))
            {
                meshRenderer = modelObject.AddComponent<MeshRenderer>();
            }

            var blockId = blockState.BlockId;
            var stateId = BlockStatePalette.INSTANCE.GetNumIdByObject(blockState);

            var client = CornApp.CurrentClient;
            if (!client) return;
            
            var packManager = ResourcePackManager.Instance;
            packManager.StateModelTable.TryGetValue(stateId, out BlockStateModel? stateModel);
            if (stateModel is null) return;
            
            var blockGeometry = stateModel.Geometries[0];

            if (packManager.BuiltinEntityModels.Intersect(stateModel.ModelIds).Any()) // Use embedded entity render
            {
                if (BlockEntityTypePalette.INSTANCE.GetBlockEntityForBlock(blockId, out BlockEntityType blockEntityType))
                {
                    var blockEntityRender = client.ChunkRenderManager.CreateBlockEntityRenderForItemModel(modelObject.transform, blockEntityType);

                    blockEntityRender.UpdateBlockState(blockState);
                }
                else
                {
                    Debug.LogWarning($"One of {blockState}'s block models is specified to use entity render model, but no suitable entity render model is found!");
                }
            }
            
            var color = BlockStatePalette.INSTANCE.GetBlockColor(stateId, world, BlockLoc.Zero);
            var waterColor = world.GetWaterColor(BlockLoc.Zero);
            
            // Use regular mesh
            var mesh = BuildBlockMesh(blockState, blockGeometry, 0b111111, color, waterColor);
            
            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = client.ChunkMaterialManager.GetAtlasMaterial(stateModel.RenderType);

            if (blockState.InWater)
            {
                meshRenderer.sharedMaterials = new [] {
                        client.ChunkMaterialManager.GetAtlasMaterial(RenderType.WATER),
                        client.ChunkMaterialManager.GetAtlasMaterial(stateModel.RenderType) };
            }
            else
            {
                meshRenderer.sharedMaterial = client.ChunkMaterialManager.GetAtlasMaterial(stateModel.RenderType);
            }
        }

        private static Mesh BuildBlockMesh(BlockState state, BlockGeometry geometry, int cullFlags, float3 color, float3 waterColor)
        {
            int vertexCount = geometry.GetVertexCount(cullFlags);
            int fluidVertexCount = 0;

            if (state.InWater || state.InLava)
            {
                fluidVertexCount = FluidGeometry.GetVertexCount(cullFlags);
                vertexCount += fluidVertexCount;
            }
            
            // Make and set mesh...
            var visualBuffer = new VertexBuffer(vertexCount);

            uint vertexOffset = 0;

            if (state.InWater)
                FluidGeometry.Build(visualBuffer, ref vertexOffset, float3.zero, FluidGeometry.LiquidTextures[0], FLUID_HEIGHTS,
                        cullFlags, DUMMY_BLOCK_VERT_LIGHT, waterColor);
            else if (state.InLava)
                FluidGeometry.Build(visualBuffer, ref vertexOffset, float3.zero, FluidGeometry.LiquidTextures[1], FLUID_HEIGHTS,
                        cullFlags, DUMMY_BLOCK_VERT_LIGHT, BlockGeometry.DEFAULT_COLOR);

            geometry.Build(visualBuffer, ref vertexOffset, float3.zero, cullFlags, 0, 0F, DUMMY_BLOCK_VERT_LIGHT, color);

            int triIdxCount = vertexCount / 2 * 3;

            var meshDataArr = Mesh.AllocateWritableMeshData(1);
            var meshData = meshDataArr[0];

            var vertAttrs = new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            vertAttrs[0] = new(VertexAttribute.Position,  dimension: 3, stream: 0);
            vertAttrs[1] = new(VertexAttribute.TexCoord0, dimension: 3, stream: 1);
            vertAttrs[2] = new(VertexAttribute.TexCoord3, dimension: 4, stream: 2);
            vertAttrs[3] = new(VertexAttribute.Color,     dimension: 4, stream: 3);

            // Set mesh params
            meshData.SetVertexBufferParams(vertexCount, vertAttrs);
            vertAttrs.Dispose();

            meshData.SetIndexBufferParams(triIdxCount, IndexFormat.UInt32);

            // Set vertex data
            // Positions
            var positions = meshData.GetVertexData<float3>(0);
            positions.CopyFrom(visualBuffer.vert);
            // Tex Coordinates
            var texCoords = meshData.GetVertexData<float3>(1);
            texCoords.CopyFrom(visualBuffer.txuv);
            // Animation Info
            var animInfos = meshData.GetVertexData<float4>(2);
            animInfos.CopyFrom(visualBuffer.uvan);
            // Vertex colors
            var vertColors = meshData.GetVertexData<float4>(3);
            vertColors.CopyFrom(visualBuffer.tint);

            // Set face data
            var triIndices = meshData.GetIndexData<uint>();
            uint vi = 0; int ti = 0;
            for (; vi < vertexCount; vi += 4U, ti += 6)
            {
                triIndices[ti]     = vi;
                triIndices[ti + 1] = vi + 3U;
                triIndices[ti + 2] = vi + 2U;
                triIndices[ti + 3] = vi;
                triIndices[ti + 4] = vi + 1U;
                triIndices[ti + 5] = vi + 3U;
            }

            var bounds = new Bounds(new Vector3(0.5F, 0.5F, 0.5F), new Vector3(1F, 1F, 1F));

            if (state.InWater || state.InLava)
            {
                int fluidTriIdxCount = fluidVertexCount / 2 * 3;

                meshData.subMeshCount = 2;
                meshData.SetSubMesh(0, new SubMeshDescriptor(0, fluidTriIdxCount)
                {
                    bounds = bounds,
                    vertexCount = vertexCount
                }, MeshUpdateFlags.DontRecalculateBounds);
                meshData.SetSubMesh(1, new SubMeshDescriptor(fluidTriIdxCount, triIdxCount - fluidTriIdxCount)
                {
                    bounds = bounds,
                    vertexCount = vertexCount
                }, MeshUpdateFlags.DontRecalculateBounds);
            }
            else
            {
                meshData.subMeshCount = 1;
                meshData.SetSubMesh(0, new SubMeshDescriptor(0, triIdxCount)
                {
                    bounds = bounds,
                    vertexCount = vertexCount
                }, MeshUpdateFlags.DontRecalculateBounds);
            }

            // Create and assign mesh
            var mesh = new Mesh { bounds = bounds };
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArr, mesh);
            // Recalculate mesh normals
            mesh.RecalculateNormals();

            return mesh;
        }

        /// <summary>
        /// Build break block mesh.
        /// </summary>
        public static Mesh BuildBlockBreakMesh(BlockState blockState, float3 posOffset, int cullFlags)
        {
            var packManager = ResourcePackManager.Instance;
            var stateId = BlockStatePalette.INSTANCE.GetNumIdByObject(blockState);

            packManager.StateModelTable.TryGetValue(stateId, out BlockStateModel? stateModel);

            if (stateModel is null) return EMPTY_BLOCK_MESH;

            // Get and build the first geometry
            var blockGeometry = stateModel.Geometries[0];

            var mesh = BuildBlockBreakMesh_Internal(blockGeometry, posOffset, cullFlags);

            return mesh;
        }

        /// <summary>
        /// Build block break mesh directly from block geometry.
        /// </summary>
        private static Mesh BuildBlockBreakMesh_Internal(BlockGeometry blockGeometry, float3 posOffset, int cullFlags)
        {
            int vertexCount = blockGeometry.GetVertexCount(cullFlags);
            var visualBuffer = new VertexBuffer(vertexCount);
            uint vertexOffset = 0;
            blockGeometry.Build(visualBuffer, ref vertexOffset, posOffset, cullFlags, 0, 0F, FULL_CORNER_LIGHTS,
                float3.zero, BlockGeometry.VertexDataFormat.ExtraUV_Light);

            int triIdxCount = vertexCount / 2 * 3;

            var meshDataArr = Mesh.AllocateWritableMeshData(1);
            var meshData = meshDataArr[0];

            var vertAttrs = new NativeArray<VertexAttributeDescriptor>(3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            vertAttrs[0] = new(VertexAttribute.Position,  dimension: 3, stream: 0);
            vertAttrs[1] = new(VertexAttribute.TexCoord0, dimension: 3, stream: 1);
            vertAttrs[2] = new(VertexAttribute.Color,     dimension: 4, stream: 2);

            // Set mesh params
            meshData.SetVertexBufferParams(vertexCount, vertAttrs);
            vertAttrs.Dispose();

            meshData.SetIndexBufferParams(triIdxCount, IndexFormat.UInt32);

            // Set vertex data
            // Positions
            var positions = meshData.GetVertexData<float3>(0);
            positions.CopyFrom(visualBuffer.vert);
            // Tex Coordinates
            var texCoords = meshData.GetVertexData<float3>(1);
            texCoords.CopyFrom(visualBuffer.txuv);
            // Vertex colors
            var vertColors = meshData.GetVertexData<float4>(2);
            vertColors.CopyFrom(visualBuffer.tint);

            // Set face data
            var triIndices = meshData.GetIndexData<uint>();
            uint vi = 0; int ti = 0;
            for (;vi < vertexCount;vi += 4U, ti += 6)
            {
                triIndices[ti]     = vi;
                triIndices[ti + 1] = vi + 3U;
                triIndices[ti + 2] = vi + 2U;
                triIndices[ti + 3] = vi;
                triIndices[ti + 4] = vi + 1U;
                triIndices[ti + 5] = vi + 3U;
            }

            var bounds = new Bounds(new Vector3(0.5F, 0.5F, 0.5F), new Vector3(1F, 1F, 1F));

            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, triIdxCount)
            {
                bounds = bounds,
                vertexCount = vertexCount
            }, MeshUpdateFlags.DontRecalculateBounds);

            var mesh = new Mesh
            {
                bounds = bounds,
                name = "Proc Mesh"
            };

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArr, mesh);

            // Recalculate mesh bounds and normals
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}