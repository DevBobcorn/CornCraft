#nullable enable
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
        private static readonly float[] FULL_CORNER_LIGHTS =
        {
            1F, 1F, 1F, 1F, 1F, 1F, 1F, 1F
        };


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
        /// Build block mesh directly from block geometry.
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