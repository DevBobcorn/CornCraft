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

        /// <summary>
        /// Build item mesh, material and transforms from given item stack.
        /// </summary>
        public static (Mesh mesh, Material material, Dictionary<DisplayPosition, float3x3> transforms)?
                BuildItem(ItemStack? itemStack, bool useInventoryMaterial)
        {
            if (itemStack is null) return null;

            var packManager = ResourcePackManager.Instance;
            var itemId = itemStack.ItemType.ItemId;

            // Use mesh cache if possible
            var shouldUseCache = itemStack.NBT is null || NBTDoesntAffectMesh(itemStack.NBT);

            var itemNumId = ItemPalette.INSTANCE.GetNumIdById(itemId);
            packManager.ItemModelTable.TryGetValue(itemNumId, out ItemModel? itemModel);

            if (itemModel is null) return null;

            var material = CornApp.CurrentClient!.ChunkMaterialManager
                .GetAtlasMaterial(itemModel.RenderType, useInventoryMaterial);

            if (shouldUseCache && DEFAULT_MESH_CACHE.TryGetValue(itemId, out var defaultMesh))
            {
                return (defaultMesh, material, itemModel.Geometry.DisplayTransforms);
            }

            // Make and set mesh...

            var tintFunc = ItemPalette.INSTANCE.GetTintRule(itemId);
            var colors = tintFunc is null ? new float3[] { new(1F, 0F, 0F), new(0F, 0F, 1F), new(0F, 1F, 0F) } : tintFunc.Invoke(itemStack);

            // TODO Get and build the right geometry (base or override)
            var itemGeometry = itemModel.Geometry;

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

            return (mesh, material, itemGeometry.DisplayTransforms);
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