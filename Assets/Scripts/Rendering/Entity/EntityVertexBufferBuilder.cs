#nullable enable
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    public class EntityVertexBufferBuilder
    {
        public static Mesh BuildMesh(EntityVertexBuffer visualBuffer)
        {
            int vertexCount = visualBuffer.vert.Length;
            int triIdxCount = vertexCount / 2 * 3;

            var meshDataArr = Mesh.AllocateWritableMeshData(1);
            var meshData = meshDataArr[0];

            var vertAttrs = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            vertAttrs[0] = new(VertexAttribute.Position,  dimension: 3, stream: 0);
            vertAttrs[1] = new(VertexAttribute.TexCoord0, dimension: 2, stream: 1);

            // Set mesh params
            meshData.SetVertexBufferParams(vertexCount, vertAttrs);
            vertAttrs.Dispose();

            meshData.SetIndexBufferParams(triIdxCount, IndexFormat.UInt32);

            // Set vertex data
            // Positions
            var positions = meshData.GetVertexData<float3>(0);
            positions.CopyFrom(visualBuffer.vert);
            // Tex Coordinates
            var texCoords = meshData.GetVertexData<float2>(1);
            texCoords.CopyFrom(visualBuffer.txuv);

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

            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, triIdxCount)
            {
                vertexCount = vertexCount
            }, MeshUpdateFlags.DontRecalculateBounds);

            // Create and assign mesh
            var mesh = new Mesh();
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArr, mesh);
            // Recalculate mesh normals ande bounds
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}