using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;

using CraftSharp.Molang.Runtime;
using CraftSharp.Resource;
using CraftSharp.Rendering;
using CraftSharp.Resource.BedrockEntity;

namespace CraftSharp
{
    public class TestResource : MonoBehaviour
    {
        private static readonly byte[] FLUID_HEIGHTS = { 15, 15, 15, 15, 15, 15, 15, 15, 15 };

        [SerializeField] private ChunkMaterialManager chunkMaterialManager;
        [SerializeField] private EntityMaterialManager entityMaterialManager;
        [SerializeField] private Material particleMaterial;

        private readonly List<Transform> billboardTransforms = new();

        // Runs before a scene gets loaded
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeApp() => Loom.Initialize();

        private static readonly float[] DUMMY_BLOCK_VERT_LIGHT = Enumerable.Repeat(0F, 8).ToArray();

        private static readonly int BASE_MAP_NAME = Shader.PropertyToID("_BaseMap");

        private void TestBuildState(string stateName, int stateId, BlockState state, BlockStateModel stateModel, int cullFlags, World world, float3 pos)
        {
            int altitude = 0;
            foreach (var geometry in stateModel.Geometries)
            {
                var coord = pos + new float3(0F, -altitude * 1.25F, 0F);

                var modelObject = new GameObject(stateName)
                {
                    transform =
                    {
                        parent = transform,
                        localPosition = coord
                    }
                };

                var filter = modelObject.AddComponent<MeshFilter>();
                var render = modelObject.AddComponent<MeshRenderer>();

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
                            cullFlags, DUMMY_BLOCK_VERT_LIGHT, world.GetWaterColor(BlockLoc.Zero));
                else if (state.InLava)
                    FluidGeometry.Build(visualBuffer, ref vertexOffset, float3.zero, FluidGeometry.LiquidTextures[1], FLUID_HEIGHTS,
                            cullFlags, DUMMY_BLOCK_VERT_LIGHT, BlockGeometry.DEFAULT_COLOR);

                var color = BlockStatePalette.INSTANCE.GetBlockColor(stateId, world, BlockLoc.Zero);
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
                filter.sharedMesh = mesh;

                if (state.InWater)
                {
                    render.sharedMaterials =
                        new []{
                            chunkMaterialManager.GetAtlasMaterial(RenderType.WATER),
                            chunkMaterialManager.GetAtlasMaterial(stateModel.RenderType)
                        };
                }
                else
                    render.sharedMaterial = chunkMaterialManager.GetAtlasMaterial(stateModel.RenderType);
                
                // Add shape colliders
                foreach (var aabb in state.Shape.AABBs)
                {
                    var col = modelObject.AddComponent<BoxCollider>();

                    // Don't forget to swap x and z
                    col.size = new(aabb.SizeZ, aabb.SizeY, aabb.SizeX);
                    col.center = new(aabb.CenterZ, aabb.CenterY, aabb.CenterX);
                }

                // Add shape holder
                var shapeHolder = modelObject.AddComponent<BlockShapeHolder>();
                shapeHolder.Shape = state.Shape;

                altitude += 1;
            }
        }

        private void TestBuildItem(string itemName, ItemStack itemStack, float3 pos)
        {
            var coord = pos + new float3(0.5F, 0.5F, 0.5F);
            var modelObject = new GameObject(itemName)
            {
                transform =
                {
                    parent = transform,
                    localPosition = coord
                }
            };

            ItemMeshBuilder.BuildItemGameObject(modelObject, itemStack, DisplayPosition.Ground, false);
        }

        private void TestBuildParticle(string particleName, Mesh[] meshes, Material material, float3 pos)
        {
            for (int i = 0; i < meshes.Length; i++)
            {
                var coord = pos + new float3(0.5F, 0.5F + i, 0.5F);

                var particleObject = new GameObject($"{particleName} Frame {i}")
                {
                    transform =
                    {
                        parent = transform,
                        localPosition = coord
                    }
                };

                var filter = particleObject.AddComponent<MeshFilter>();
                filter.sharedMesh = meshes[i];

                var render = particleObject.AddComponent<MeshRenderer>();
                render.sharedMaterial = material;

                var meshCollider = particleObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshes[i];

                billboardTransforms.Add(particleObject.transform);
            }
        }

        private IEnumerator DoBuild()
        {
            var packManager = ResourcePackManager.Instance;
            
            // Create a dummy world as provider of block colors
            var world = new World();

            const int start = 0;
            const int limit = 4096;
            int count = 0, width = 64;
            foreach (var pair in packManager.StateModelTable)
            {
                int index = count - start;
                if (index >= 0)
                {
                    var state = BlockStatePalette.INSTANCE.GetByNumId(pair.Key);

                    TestBuildState($"Block [{pair.Key}] {state}", pair.Key, state, pair.Value, 0b111111, world,
                        new(index % width * 1.5F, 0, index / width * 1.5F));
                }

                count++;

                if (count >= start + limit)
                    break;
            }
            
            yield return null;

            count = 0; width = 32;
            foreach (var pair in packManager.ItemModelTable)
            {
                int index = count - start;
                if (index >= 0)
                {
                    var item = ItemPalette.INSTANCE.GetByNumId(pair.Key);
                    var itemStack = new ItemStack(item, 1, null);

                    TestBuildItem($"Item [{pair.Key}] {item}", itemStack, new(-(index % width) * 1.5F - 1.5F, 0F, index / width * 1.5F));
                }

                count++;

                if (count >= start + limit)
                    break;
            }
            
            yield return null;

            count = 0; width = 32;
            var particleMaterialInstance = new Material(particleMaterial);
            particleMaterialInstance.SetTexture(BASE_MAP_NAME, packManager.GetParticleAtlas());

            foreach (var pair in packManager.ParticleMeshesTable)
            {
                int index = count;

                var particle = ParticleTypePalette.INSTANCE.GetById(pair.Key);
                var meshes = packManager.ParticleMeshesTable[pair.Key];

                TestBuildParticle($"Particle [{pair.Key}] {particle}", meshes, particleMaterialInstance,
                        new((index % width) * 3F, 0F, -(index / width) * 5F - 5F));

                count++;
            }
            
            yield return null;
        }

        private void TestAnimation(string jsonStr)
        {
            var data = Json.ParseJson(jsonStr);
            var anim = EntityBoneAnimation.FromJson(data);

            foreach (var (time, pre, post) in anim.scaleKeyframes)
            {
                Debug.Log($"SCALE [{time}] {pre} {post}");
            }

            var scope = new MoScope(new());
            var env = new MoLangEnvironment();

            for (float a = -1F; a < 3F; a += 0.05F)
            {
                var (trans, scale, rot) = anim.Evaluate(a, scope, env);
                Debug.Log($"[{a}] => [{trans} | {scale} {rot}]");
            }
        }

        private IEnumerator DoEntityBuild()
        {
            var entityResManager = BedrockEntityResourceManager.Instance;
            
            var testmentObj = new GameObject("[Entity Testment]");
            const int entityPerRow = 10;
            int index = 0;
            foreach (var (entityType, entityDef) in entityResManager.EntityRenderDefinitions)
            {
                int i = index / entityPerRow;
                int j = index % entityPerRow;

                var entityRenderObj = new GameObject($"{index} {entityType}");
                entityRenderObj.transform.SetParent(testmentObj.transform);
                entityRenderObj.transform.localPosition= new(i * 2, 0, - (entityPerRow - j) * 2);

                var entityRender = entityRenderObj.AddComponent<BedrockModelEntityRender>();
                try
                {
                    entityRender.SetDefinitionData(entityDef);
                    entityRender.BuildEntityModel(entityResManager, entityMaterialManager);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"An exception occurred building model for entity {entityType}: {e}");
                }
                
                index++;
            }
            
            yield return null;
        }

        private void Start()
        {
            mainCamera = Camera.main;

            StartCoroutine(DoBuild());
            StartCoroutine(DoEntityBuild());
        }

        private Camera mainCamera;

        private void Update()
        {
            if (billboardTransforms != null)
            {
                foreach (var t in billboardTransforms)
                {
                    t.localEulerAngles = mainCamera.transform.localEulerAngles;
                }
            }
        }
    }
}