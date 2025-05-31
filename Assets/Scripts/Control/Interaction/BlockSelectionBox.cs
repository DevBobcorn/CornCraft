using CraftSharp.Rendering;
using CraftSharp.Resource;
using Unity.Mathematics;
using UnityEngine;

namespace CraftSharp.Control
{
    public class BlockSelectionBox : MonoBehaviour
    {
        private static readonly int DESTROY_STAGE = Shader.PropertyToID("_DestroyStage");
        private static readonly int DESTROY_TEXTURE_ARRAY = Shader.PropertyToID("_DestroyTextureArray");
        private static readonly int LINE_LENGTH = Shader.PropertyToID("_Line_Length");
        private static readonly int BILLBOARD_AXIS = Shader.PropertyToID("_BillboardAxis");
        private const int MAX_AABB_COUNT = 64;

        [SerializeField] private Material lineMaterial;
        [SerializeField] private Mesh lineMesh;
        [SerializeField] private MeshFilter breakMeshFilter;
        [SerializeField] private MeshRenderer breakMeshRenderer;

        private readonly MeshRenderer[] lineMeshRenderers = new MeshRenderer[MAX_AABB_COUNT * 12];
        private static Texture2DArray destroyTextureArray;

#nullable enable
        private BlockShape? currentBlockShape;
        private BlockState? currentBlockState;
        private Mesh? currentBreakMesh;
#nullable disable
        private int currentBoxCount = 0;
        private int createdBoxCount = 0;
        private int currentDestroyStage = -1;

        private static MaterialPropertyBlock GetLengthPropertyBlock(int axis, float length)
        {
            var propBlock = new MaterialPropertyBlock();
            propBlock.SetFloat(LINE_LENGTH, length);
            propBlock.SetInteger(BILLBOARD_AXIS, axis);

            return propBlock;
        }

        private static MaterialPropertyBlock GetBlockBreakPropertyBlock(int destroyStage)
        {
            var propBlock = new MaterialPropertyBlock();
            
            if (!destroyTextureArray)
            {
                destroyTextureArray = ResourcePackManager.Instance.GetDestroyTextureArray();
            }

            if (destroyTextureArray) // If destroy texture array is present
            {
                propBlock.SetTexture(DESTROY_TEXTURE_ARRAY, destroyTextureArray);
            }
            // Add an extra offset to ensure reading at the right depth
            propBlock.SetFloat(DESTROY_STAGE, destroyStage + 0.1F);

            return propBlock;
        }

        public void UpdateShape(BlockShape blockShape)
        {
            if (currentBlockShape == blockShape) return;
            currentBlockShape = blockShape;

            var aabbs = blockShape.AABBs;
            var displayedBoxCount = Mathf.Min(aabbs.Length, MAX_AABB_COUNT);

            int i = 0;

            for (; i < displayedBoxCount; i++)
            {
                if (i == createdBoxCount) // Create new boxes if not created yet
                {
                    for (int j = 0; j < 12; j++)
                    {
                        var lineObj = new GameObject($"Box #{i} - {j}");
                        lineObj.transform.SetParent(transform, false);

                        lineMeshRenderers[i * 12 + j] = lineObj.AddComponent<MeshRenderer>();
                        var lineMeshFilter = lineObj.AddComponent<MeshFilter>();
                        lineMeshFilter.sharedMesh = lineMesh;
                        lineMeshRenderers[i * 12 + j].sharedMaterial = lineMaterial;

                        lineMeshRenderers[i * 12 + j].gameObject.SetActive(false);
                    }

                    createdBoxCount = i + 1;
                }

                var aabb = aabbs[i];

                // Swap X and Z
                float minX = aabb.MinZ, minY = aabb.MinY, minZ = aabb.MinX;
                float maxX = aabb.MaxZ, maxY = aabb.MaxY, maxZ = aabb.MaxX;
                var halfX = (minX + maxX) / 2F;
                var halfY = (minY + maxY) / 2F;
                var halfZ = (minZ + maxZ) / 2F;
                var propX = GetLengthPropertyBlock(0, maxX - minX);
                var propY = GetLengthPropertyBlock(1, maxY - minY);
                var propZ = GetLengthPropertyBlock(2, maxZ - minZ);

                lineMeshRenderers[i * 12     ].SetPropertyBlock(propX);
                lineMeshRenderers[i * 12 + 1 ].SetPropertyBlock(propX);
                lineMeshRenderers[i * 12 + 2 ].SetPropertyBlock(propX);
                lineMeshRenderers[i * 12 + 3 ].SetPropertyBlock(propX);

                lineMeshRenderers[i * 12 + 4 ].SetPropertyBlock(propY);
                lineMeshRenderers[i * 12 + 5 ].SetPropertyBlock(propY);
                lineMeshRenderers[i * 12 + 6 ].SetPropertyBlock(propY);
                lineMeshRenderers[i * 12 + 7 ].SetPropertyBlock(propY);

                lineMeshRenderers[i * 12 + 8 ].SetPropertyBlock(propZ);
                lineMeshRenderers[i * 12 + 9 ].SetPropertyBlock(propZ);
                lineMeshRenderers[i * 12 + 10].SetPropertyBlock(propZ);
                lineMeshRenderers[i * 12 + 11].SetPropertyBlock(propZ);

                lineMeshRenderers[i * 12     ].transform.localPosition = new(halfX, minY, minZ);
                lineMeshRenderers[i * 12 + 1 ].transform.localPosition = new(halfX, minY, maxZ);
                lineMeshRenderers[i * 12 + 2 ].transform.localPosition = new(halfX, maxY, minZ);
                lineMeshRenderers[i * 12 + 3 ].transform.localPosition = new(halfX, maxY, maxZ);

                lineMeshRenderers[i * 12 + 4 ].transform.localPosition = new(minX, halfY, minZ);
                lineMeshRenderers[i * 12 + 5 ].transform.localPosition = new(minX, halfY, maxZ);
                lineMeshRenderers[i * 12 + 6 ].transform.localPosition = new(maxX, halfY, minZ);
                lineMeshRenderers[i * 12 + 7 ].transform.localPosition = new(maxX, halfY, maxZ);

                lineMeshRenderers[i * 12 + 8 ].transform.localPosition = new(minX, minY, halfZ);
                lineMeshRenderers[i * 12 + 9 ].transform.localPosition = new(minX, maxY, halfZ);
                lineMeshRenderers[i * 12 + 10].transform.localPosition = new(maxX, minY, halfZ);
                lineMeshRenderers[i * 12 + 11].transform.localPosition = new(maxX, maxY, halfZ);

                for (int j = 0; j < 12; j++)
                {
                    lineMeshRenderers[i * 12 + j].gameObject.SetActive(true);
                }
            }

            for (; i < currentBoxCount; i++) // Hide unused boxes, but don't destroy them
            {
                for (int j = 0; j < 12; j++)
                {
                    lineMeshRenderers[i * 12 + j].gameObject.SetActive(false);
                }
            }

            currentBoxCount = aabbs.Length;
        }

        public void UpdateBreakMesh(BlockState blockState, float3 posOffset, int cullFlags, int stage)
        {
            if (currentBlockState == blockState) return;
            currentBlockState = blockState;

            if (currentBreakMesh) // Take care of previous break mesh
            {
                breakMeshFilter.sharedMesh = null;
                Destroy(currentBreakMesh);
            }

            if (blockState is not null)
            {
                currentBreakMesh = BlockMeshBuilder.BuildBlockBreakMesh(currentBlockState, posOffset, cullFlags);
                breakMeshFilter.sharedMesh = currentBreakMesh;
                breakMeshRenderer.SetPropertyBlock(GetBlockBreakPropertyBlock(stage));
            }
        }

        public void UpdateBreakStage(int stage)
        {
            if (stage == currentDestroyStage || stage < 0 || stage >= ResourcePackManager.DESTROY_TEXTURES.Length)
            {
                return;
            }

            breakMeshRenderer.SetPropertyBlock(GetBlockBreakPropertyBlock(stage));
        }

        public void ClearShape()
        {
            if (currentBlockShape is null) return;
            currentBlockShape = null;

            for (int i = 0; i < currentBoxCount * 12; i++)
            {
                lineMeshRenderers[i].gameObject.SetActive(false);
            }

            currentBoxCount = 0;
        }
    
        public void ClearBreakMesh()
        {
            currentBlockState = null;

            if (currentBreakMesh) // Take care of previous break mesh
            {
                breakMeshFilter.sharedMesh = null;
                Destroy(currentBreakMesh);
            }

            currentDestroyStage = -1;
        }
    }
}