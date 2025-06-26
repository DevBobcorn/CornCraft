#nullable enable
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    public static class ItemMeshBuilder
    {
        private static readonly float3 ITEM_CENTER = new(-0.5F, -0.5F, -0.5F);

        private static readonly Dictionary<ResourceLocation, Mesh> DEFAULT_MESH_CACHE = new();
        
        private static readonly List<Mesh> UNCACHED_ITEM_MESHES = new();

        public static void ClearMeshCache()
        {
            foreach (var mesh in DEFAULT_MESH_CACHE.Values)
            {
                Object.Destroy(mesh);
            }
            DEFAULT_MESH_CACHE.Clear();

            foreach (var mesh in UNCACHED_ITEM_MESHES)
            {
                Object.Destroy(mesh);
            }
            UNCACHED_ITEM_MESHES.Clear();
        }

        private static bool NBTDoesntAffectMesh(Dictionary<string, object> nbt)
        {
            return nbt.Count == 0;
        }

        private static void ClearItemVisual(GameObject modelObject)
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
        public static bool BuildItemGameObject(GameObject modelObject, ItemStack? itemStack,
            DisplayPosition displayPosition, bool useInventoryMaterial,
            Mesh? defaultMesh = null, Material? defaultMaterial = null)
        {
            ClearItemVisual(modelObject);
            
            if (itemStack is null || itemStack.Count <= 0) return false;
            
            var itemId = itemStack.ItemType.ItemId;
            var itemNumId = ItemPalette.INSTANCE.GetNumIdById(itemId);
            var itemModelId = new ResourceLocation(itemId.Namespace, $"item/{itemId.Path}");

            var client = CornApp.CurrentClient;
            if (!client) return false;
            
            // Use mesh cache if possible
            var shouldUseCache = itemStack.NBT is null || NBTDoesntAffectMesh(itemStack.NBT);
            
            var packManager = ResourcePackManager.Instance;
            packManager.ItemModelTable.TryGetValue(itemNumId, out ItemModel? itemModel);
            if (itemModel is null) return false;
            
            // TODO: Get and build the right geometry (base or override)
            var itemGeometry = itemModel.Geometry;

            if (packManager.BuiltinEntityModels.Contains(itemModelId)) // Use embedded entity render
            {
                if (BlockEntityTypePalette.INSTANCE.GetBlockEntityForBlock(itemId, out BlockEntityType blockEntityType))
                {
                    var blockState = BlockStatePalette.INSTANCE.GetDefault(itemId);
                    client.ChunkRenderManager.CreateBlockEntityRenderForItemModel(modelObject.transform, blockState, blockEntityType);
                }
                else
                {
                    Debug.LogWarning($"Item model {itemModelId} is specified to use entity render model, but no suitable entity render model is found!");
                }
            }
            else // Use regular mesh
            {
                if (!modelObject.TryGetComponent<MeshFilter>(out var meshFilter))
                {
                    meshFilter = modelObject.AddComponent<MeshFilter>();
                }

                if (!modelObject.TryGetComponent<MeshRenderer>(out var meshRenderer))
                {
                    meshRenderer = modelObject.AddComponent<MeshRenderer>();
                }
            
                var tintFunc = ItemPalette.INSTANCE.GetTintRule(itemId);
                var colors = tintFunc is null ? new float3[] { new(1F, 0F, 0F), new(0F, 0F, 1F), new(0F, 1F, 0F) } : tintFunc.Invoke(itemStack);
            
                var mesh = BuildItemMeshAndCache(itemGeometry, itemId, shouldUseCache, colors);
                if (mesh is not null)
                {
                    meshFilter.sharedMesh = mesh;
                    meshRenderer.sharedMaterial = client.ChunkMaterialManager
                        .GetAtlasMaterial(itemModel.RenderType, useInventoryMaterial);
                }
                else if (defaultMesh is not null && defaultMaterial is not null)
                {
                    meshFilter.sharedMesh = defaultMesh;
                    meshRenderer.sharedMaterial = defaultMaterial;
                }
                else
                {
                    return false;
                }
            }

            switch (displayPosition)
            {
                case DisplayPosition.GUI:
                    // Handle GUI display transform
                    bool hasGUITransform = itemGeometry.DisplayTransforms.TryGetValue(DisplayPosition.GUI, out float3x3 t);

                    if (hasGUITransform) // Apply specified local transform
                    {
                        // Apply local translation, '1' in translation field means 0.1 unit in local space, so multiply with 0.1
                        modelObject.transform.localPosition = t.c0 * 0.1F;
                        // Apply local rotation
                        modelObject.transform.localEulerAngles = Vector3.zero;
                        // - MC ROT X
                        modelObject.transform.Rotate(Vector3.back, t.c1.x, Space.Self);
                        // - MC ROT Y
                        modelObject.transform.Rotate(Vector3.down, t.c1.y, Space.Self);
                        // - MC ROT Z
                        modelObject.transform.Rotate(Vector3.left, t.c1.z, Space.Self);
                        // Apply local scale
                        modelObject.transform.localScale = t.c2;
                    }
                    else // Apply uniform local transform
                    {
                        // Apply local translation, set to zero
                        modelObject.transform.localPosition = Vector3.zero;
                        // Apply local rotation
                        modelObject.transform.localEulerAngles = Vector3.zero;
                        // Apply local scale
                        modelObject.transform.localScale = Vector3.one;
                    }
                    break;
            }
            
            return true;
        }

        /// <summary>
        /// Build item mesh and cache it(if applicable)
        /// </summary>
        public static Mesh? BuildItemMeshAndCache(ItemGeometry itemGeometry, ResourceLocation itemId, bool shouldUseCache, float3[] colors)
        {
            if (shouldUseCache && DEFAULT_MESH_CACHE.TryGetValue(itemId, out var defaultMesh))
            {
                return defaultMesh;
            }
            var mesh = BuildItemMesh(itemGeometry, colors);

            // Store in cache if applicable
            if (shouldUseCache)
            {
                DEFAULT_MESH_CACHE[itemId] = mesh;
            }
            else
            {
                UNCACHED_ITEM_MESHES.Add(mesh);
            }

            return mesh;
        }

        /// <summary>
        /// Build item mesh directly from item geometry. Not recommended because
        /// it doesn't utilize the model cache table.
        /// </summary>
        public static Mesh BuildItemMesh(ItemGeometry itemGeometry, float3[] colors)
        {
            
            int vertexCount = itemGeometry.GetVertexCount();
            var visualBuffer = new VertexBuffer(vertexCount);
            uint vertexOffset = 0;
            itemGeometry.Build(visualBuffer, ref vertexOffset, ITEM_CENTER, colors);

            int triIdxCount = vertexCount / 2 * 3;

            var meshDataArr = Mesh.AllocateWritableMeshData(1);
            var meshData = meshDataArr[0];

            var vertAttrs = new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            vertAttrs[0] = new(VertexAttribute.Position,  dimension: 3, stream: 0);
            vertAttrs[1] = new(VertexAttribute.TexCoord0, dimension: 3, stream: 1);
            vertAttrs[2] = new(VertexAttribute.TexCoord1, dimension: 4, stream: 2);
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
            // Animations
            var vertAnims = meshData.GetVertexData<float4>(2);
            vertAnims.CopyFrom(visualBuffer.uvan);
            // Vertex colors
            var vertColors = meshData.GetVertexData<float4>(3);
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