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
        

        [SerializeField] private ChunkMaterialManager chunkMaterialManager;
        [SerializeField] private EntityMaterialManager entityMaterialManager;
        [SerializeField] private Material particleMaterial;

        private readonly List<Transform> billboardTransforms = new();

        // Runs before a scene gets loaded
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeApp() => Loom.Initialize();
        
        private static readonly int BASE_MAP_NAME = Shader.PropertyToID("_BaseMap");

        private void TestBuildState(string stateName, BlockState state, World world, float3 pos)
        {
            var modelObject = new GameObject(stateName)
            {
                transform =
                {
                    parent = transform,
                    localPosition = pos
                }
            };

            BlockMeshBuilder.BuildBlockGameObject(modelObject, state, world);
        }

        private void TestBuildItem(string itemName, ItemStack itemStack, float3 pos)
        {
            var modelObject = new GameObject(itemName)
            {
                transform =
                {
                    parent = transform,
                    localPosition = pos + new float3(0.5F, 0.5F, 0.5F)
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
            foreach (var (stateId, _) in packManager.StateModelTable)
            {
                int index = count - start;
                if (index >= 0)
                {
                    var state = BlockStatePalette.INSTANCE.GetByNumId(stateId);
                    var pos = new float3(index % width * 1.5F, 0, index / width * 1.5F);

                    TestBuildState($"Block [{stateId}] {state}", state, world, pos);
                }

                count++;

                if (count >= start + limit)
                    break;
            }
            
            yield return null;

            count = 0; width = 32;
            foreach (var (itemNumId, _) in packManager.ItemModelTable)
            {
                int index = count - start;
                if (index >= 0)
                {
                    var item = ItemPalette.INSTANCE.GetByNumId(itemNumId);
                    var itemStack = new ItemStack(item, 1);
                    var pos = new float3(-(index % width) * 1.5F - 1.5F, 0F, index / width * 1.5F);

                    TestBuildItem($"Item [{itemNumId}] {item}", itemStack, pos);
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