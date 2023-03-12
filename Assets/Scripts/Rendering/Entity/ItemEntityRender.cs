#nullable enable
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

using MinecraftClient.Mapping;
using MinecraftClient.Resource;

namespace MinecraftClient.Rendering
{
    public class ItemEntityRender : EntityRender
    {
        public MeshFilter? itemMeshFilter;
        public MeshRenderer? itemMeshRenderer;

        public override void Initialize(EntityType entityType, Entity entity)
        {
            base.Initialize(entityType, entity);
            
            if (itemMeshFilter is not null && itemMeshRenderer is not null)
            {
                var itemStack = entity?.Item;

                if (itemStack is null)
                {
                    Debug.LogWarning("Item entity doesn't have an item!");
                    return;
                }

                var packManager = CornClient.Instance!.PackManager;

                var itemNumId = ItemPalette.INSTANCE.ToNumId(itemStack.Type.ItemId);
                ItemModel? itemModel = null;
                packManager!.ItemModelTable.TryGetValue(itemNumId, out itemModel);

                if (itemModel is null)
                {
                    Debug.LogWarning($"Item model for {itemStack.Type.ItemId} is not available!");
                    return;
                }

                // Make and set mesh...
                var visualBuffer = new VertexBuffer();

                int fluidVertexCount = visualBuffer.vert.Length;
                int fluidTriIdxCount = (fluidVertexCount / 2) * 3;

                float3[] colors;

                var tintFunc = ItemPalette.INSTANCE.GetTintRule(itemNumId);
                if (tintFunc is null)
                    colors = new float3[]{ new(1F, 0F, 0F), new(0F, 0F, 1F), new(0F, 1F, 0F) };
                else
                    colors = tintFunc.Invoke(itemStack);

                // TODO Get and build the right geometry (base or override)
                var itemGeometry = itemModel.Geometry;
                itemGeometry.Build(ref visualBuffer, float3.zero, colors);

                int vertexCount = visualBuffer.vert.Length;
                int triIdxCount = (vertexCount / 2) * 3;

                var meshDataArr = Mesh.AllocateWritableMeshData(1);
                var meshData = meshDataArr[0];

                var vertAttrs = new NativeArray<VertexAttributeDescriptor>(3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                vertAttrs[0] = new(VertexAttribute.Position,  dimension: 3, stream: 0);
                vertAttrs[1] = new(VertexAttribute.TexCoord0, dimension: 3, stream: 1);
                vertAttrs[2] = new(VertexAttribute.Color,     dimension: 3, stream: 2);

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
                var vertColors = meshData.GetVertexData<float3>(2);
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

                // Recalculate mesh normals
                mesh.RecalculateNormals();

                itemMeshFilter.sharedMesh = mesh;
                itemMeshRenderer.sharedMaterial = MaterialManager.GetAtlasMaterial(itemModel.RenderType);

                if (itemGeometry.isGenerated) // Put it onto the ground, instead of floating in the air
                {
                    var meshTransform = itemMeshRenderer.transform;
                    meshTransform.localEulerAngles = new(0F, 0F, 90F);

                    var offset = -0.5F + (packManager.GeneratedItemModelThickness / 32F);
                    meshTransform.localPosition = new(0.5F, offset, -0.5F);

                    visual!.localEulerAngles = new(0F, (entity!.ID * 71F) % 360F, 0F);
                }
                else // Just apply random rotation
                {
                    var meshTransform = itemMeshRenderer.transform;
                    meshTransform.localEulerAngles = new(0F, (entity!.ID * 350F) % 360F, 0F);

                    meshTransform.localPosition = new(0.5F, 0F, -0.5F);
                }

                
            }
            else
            {
                Debug.LogWarning("Item entity prefab components not assigned!");
            }
        }

    }
}