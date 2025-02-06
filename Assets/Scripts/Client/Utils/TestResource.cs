using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using TMPro;

using CraftSharp.Molang.Runtime;
using CraftSharp.Resource;
using CraftSharp.Rendering;
using CraftSharp.Resource.BedrockEntity;
using CraftSharp.Event;

namespace CraftSharp
{
    public class TestResource : MonoBehaviour
    {
        private static readonly byte[] FLUID_HEIGHTS = new byte[] { 15, 15, 15, 15, 15, 15, 15, 15, 15 };

        public TMP_Text InfoText;
        public Animator CrosshairAnimator;
        [SerializeField] private ChunkMaterialManager chunkMaterialManager;
        [SerializeField] private EntityMaterialManager entityMaterialManager;
        [SerializeField] private Material particleMaterial;

        [SerializeField] private RectTransform inventory;
        [SerializeField] private GameObject inventoryItemPrefab;
        public int[] InventoryBuildList = { };

        [SerializeField] private Viewer viewer;

        private bool loaded = false;
        private readonly List<Transform> billBoardTransforms = new();

        // Runs before a scene gets loaded
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeApp() => Loom.Initialize();

        private static readonly float[] DUMMY_BLOCK_VERT_LIGHT = Enumerable.Repeat(0F, 8).ToArray();

        private static readonly ResourceLocation BLOCK_PARTICLE_ID = new("block");

        public void TestBuildState(string name, int stateId, BlockState state, BlockStateModel stateModel, int cullFlags, World world, float3 pos)
        {
            int altitude = 0;
            foreach (var geometry in stateModel.Geometries)
            {
                var coord = pos + new float3(0F, -altitude * 1.25F, 0F);

                var modelObject = new GameObject(name);
                modelObject.transform.parent = transform;
                modelObject.transform.localPosition = coord;

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

                var color = BlockStatePalette.INSTANCE.GetBlockColor(stateId, world, BlockLoc.Zero, state);
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

                if (state.InWater || state.InLava)
                {
                    int fluidTriIdxCount = (fluidVertexCount / 2) * 3;

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

        public void TestBuildItem(string name, ItemStack itemStack, ItemModel itemModel, float3 pos)
        {
            // Gather all geometries of this model
            Dictionary<ItemModelPredicate, ItemGeometry> buildDict = new()
            {
                { ItemModelPredicate.EMPTY, itemModel.Geometry }
            };
            foreach (var pair in itemModel.Overrides)
                buildDict.TryAdd(pair.Key, pair.Value);

            int altitude = 0;
            foreach (var pair in buildDict)
            {
                var coord = pos + new float3(0.5F, altitude * 1.25F + 0.5F, 0.5F);

                var modelObject = new GameObject(pair.Key == ItemModelPredicate.EMPTY ? name : $"{name}{pair.Key}");
                modelObject.transform.parent = transform;
                modelObject.transform.localPosition = coord;

                var filter = modelObject.AddComponent<MeshFilter>();
                var render = modelObject.AddComponent<MeshRenderer>();
                var collider = modelObject.AddComponent<MeshCollider>();

                float3[] colors;

                var tintFunc = ItemPalette.INSTANCE.GetTintRule(itemStack.ItemType.ItemId);
                if (tintFunc is null)
                {
                    // Use high-constrast colors for troubleshooting and debugging
                    //colors = new float3[]{ new(1F, 0F, 0F), new(0F, 1F, 0F), new(0F, 0F, 1F) };
                    // Or use white colors to make sure models with mis-tagged tinted faces still look right
                    colors = new float3[] { new(1F, 1F, 1F), new(1F, 1F, 1F), new(1F, 1F, 1F) };
                }
                else
                {
                    colors = tintFunc.Invoke(itemStack);
                }

                var itemMesh = ItemMeshBuilder.BuildItemMesh(pair.Value, colors);

                filter.sharedMesh = itemMesh;
                collider.sharedMesh = itemMesh;
                render.sharedMaterial = chunkMaterialManager.GetAtlasMaterial(itemModel.RenderType);

                altitude += 1;
            }
        }

        public void TestBuildInventoryItem(string name, ItemStack itemStack, ItemModel itemModel)
        {
            var invItemObj = GameObject.Instantiate(inventoryItemPrefab);
            invItemObj.name = name;
            invItemObj.GetComponent<RectTransform>().SetParent(inventory, false);

            var filter = invItemObj.GetComponentInChildren<MeshFilter>();
            var modelObject = filter.gameObject;

            var render = modelObject.GetComponent<MeshRenderer>();
            var itemGeometry = itemModel.Geometry;

            float3[] colors;

            var tintFunc = ItemPalette.INSTANCE.GetTintRule(itemStack.ItemType.ItemId);
            if (tintFunc is null)
            {
                // Use high-constrast colors for troubleshooting and debugging
                //colors = new float3[]{ new(1F, 0F, 0F), new(0F, 1F, 0F), new(0F, 0F, 1F) };
                // Or use white colors to make sure models with mis-tagged tinted faces still look right
                colors = new float3[] { new(1F, 1F, 1F), new(1F, 1F, 1F), new(1F, 1F, 1F) };
            }
            else
            {
                colors = tintFunc.Invoke(itemStack);
            }

            var itemMesh = ItemMeshBuilder.BuildItemMesh(itemGeometry, colors);

            // Handle GUI display transform
            bool hasGUITransform = itemGeometry.DisplayTransforms.TryGetValue(DisplayPosition.GUI, out float3x3 t);

            // Make use of the debug text
            invItemObj.GetComponentInChildren<TMP_Text>().text = hasGUITransform ? $"{t.c1.x} {t.c1.y} {t.c1.z}" : string.Empty;

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

            filter.sharedMesh = itemMesh;
            render.sharedMaterial = chunkMaterialManager.GetAtlasMaterial(itemModel.RenderType, true);
        }

        public void TestBuildParticle(string name, Mesh[] meshes, Material material, float3 pos)
        {
            var particleTransforms = new List<Transform>();

            for (int i = 0; i < meshes.Length; i++)
            {
                var coord = pos + new float3(0.5F, 0.5F + i, 0.5F);

                var particleObject = new GameObject($"{name} Frame {i}");
                particleObject.transform.parent = transform;
                particleObject.transform.localPosition = coord;

                var filter = particleObject.AddComponent<MeshFilter>();
                filter.sharedMesh = meshes[i];

                var render = particleObject.AddComponent<MeshRenderer>();
                render.sharedMaterial = material;

                var collider = particleObject.AddComponent<MeshCollider>();
                collider.sharedMesh = meshes[i];

                billBoardTransforms.Add(particleObject.transform);
            }
        }

        private IEnumerator DoBuild(string dataVersion, string resourceVersion, string[] resourceOverrides)
        {
            // Load block/blockstate definitions
            var loadFlag = new DataLoadFlag();
            Task.Run(() => BlockStatePalette.INSTANCE.PrepareData(dataVersion, loadFlag));
            while (!loadFlag.Finished) yield return null;

            // Load item definitions
            loadFlag.Finished = false;
            Task.Run(() => ItemPalette.INSTANCE.PrepareData(dataVersion, loadFlag));
            while (!loadFlag.Finished) yield return null;

            // Load particle definitions
            loadFlag.Finished = false;
            Task.Run(() => ParticleTypePalette.INSTANCE.PrepareData(dataVersion, loadFlag));
            while (!loadFlag.Finished) yield return null;

            // Load resource packs
            var packManager = ResourcePackManager.Instance;
            packManager.ClearPacks();

            // First add base resources
            ResourcePack basePack = new($"vanilla-{resourceVersion}");
            packManager.AddPack(basePack);

            // Then append overrides
            foreach (var packName in resourceOverrides)
                packManager.AddPack(new(packName));

            // Load valid packs...
            loadFlag.Finished = false;
            Task.Run(() => packManager.LoadPacks(loadFlag,
                    (status) => Loom.QueueOnMainThread(() =>
                        InfoText.text = Translations.Get(status)), loadParticles: true));
            while (!loadFlag.Finished) yield return null;

            // Loading complete!
            loaded = true;
            
            // Create a dummy world as provider of block colors
            var world = new World();

            float startTime = Time.realtimeSinceStartup;

            int start = 0, limit = 0;
            int count = 0, width = 64;
            foreach (var pair in packManager.StateModelTable)
            {
                int index = count - start;
                if (index >= 0)
                {
                    var state = BlockStatePalette.INSTANCE.GetByNumId(pair.Key);

                    TestBuildState($"Block [{pair.Key}] {state}", pair.Key, state, pair.Value, 0b111111, world, new((index % width) * 1.5F, 0, (index / width) * 1.5F));
                }

                count++;

                if (count >= start + limit)
                    break;
            }

            count = 0; width = 32;
            foreach (var pair in packManager.ItemModelTable)
            {
                int index = count - start;
                if (index >= 0)
                {
                    var item = ItemPalette.INSTANCE.GetByNumId(pair.Key);
                    var itemStack = new ItemStack(item, 1, null);

                    TestBuildItem($"Item [{pair.Key}] {item}", itemStack, pair.Value, new(-(index % width) * 1.5F - 1.5F, 0F, (index / width) * 1.5F));
                }

                count++;

                if (count >= start + limit)
                    break;

            }

            count = 0; width = 32;
            var particleMaterialInstance = new Material(particleMaterial);
            particleMaterialInstance.SetTexture("_BaseMap", packManager.GetParticleAtlas());

            foreach (var pair in packManager.ParticleMeshesTable)
            {
                int index = count;

                var particle = ParticleTypePalette.INSTANCE.GetById(pair.Key);
                var meshes = packManager.ParticleMeshesTable[pair.Key];

                TestBuildParticle($"Particle [{pair.Key}] {particle}", meshes, particleMaterialInstance,
                        new((index % width) * 3F, 0F, -(index / width) * 5F - 5F));

                count++;
            }

            InfoText.text = $"Meshes built in {Time.realtimeSinceStartup - startTime} second(s).";
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
            var entityResPath = PathHelper.GetPackDirectoryNamed("bedrock_res");
            var playerModelsPath = PathHelper.GetPackDirectoryNamed("player_models");

            var entityResManager = new BedrockEntityResourceManager(entityResPath, playerModelsPath);

            yield return StartCoroutine(entityResManager.LoadEntityResources(new(),
                    (status) => Loom.QueueOnMainThread(() => InfoText.text = Translations.Get(status))));
            
            var testmentObj = new GameObject("[Entity Testment]");
            int entityPerRow = 10;
            int index = 0;
            foreach (var pair in entityResManager.EntityRenderDefinitions)
            {
                var entityType = pair.Key;
                var entityDef = pair.Value;

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
        }

        void Start()
        {
            var overrides = new string[] { "vanilla_fix"/*, "3D Default 1.16.2+ v1.6.0"*/ };
            string resVersion = "1.16.5", dataVersion = "1.16.5";

            if (!Directory.Exists(PathHelper.GetPackDirectoryNamed($"vanilla-{resVersion}"))) // Prepare resources first
            {
                Debug.Log($"Resources for {resVersion} not present. Downloading...");

                StartCoroutine(ResourceDownloader.DownloadResource(resVersion,
                        (status) => Loom.QueueOnMainThread(() => InfoText.text = Translations.Get(status)), () => { },
                        (succeeded) => {
                            if (succeeded) // Resources ready, do build
                                StartCoroutine(DoBuild(dataVersion, resVersion, overrides));
                            else // Failed to download resources
                                InfoText.text = $"Failed to download resources for {resVersion}.";
                        }));
            }
            else // Resources ready, do build
            {
                StartCoroutine(DoBuild(dataVersion, resVersion, overrides));
            }

            StartCoroutine(DoEntityBuild());

            IsPaused = false;
        }

        private bool isPaused = false;
        public bool IsPaused
        {
            get => isPaused;
            set {
                isPaused = value;
                // Update cursor lock
                Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
                // Update crosshair visibility
                CrosshairAnimator.SetBool("Show", !value);
                // Update viewer
                if (viewer != null)
                {
                    viewer.enabled = !IsPaused;
                }
            }
        }

        void Update()
        {
            if (ResourcePackManager.Instance.Loaded && UnityEngine.Random.Range(0, 4) == 0)
            {
                var stateId = UnityEngine.Random.Range(0, 20);
                var typeId = ParticleTypePalette.INSTANCE.GetNumIdById(BLOCK_PARTICLE_ID);

                EventManager.Instance.Broadcast(new ParticlesEvent(Vector3.up, typeId, new BlockParticleExtraData(stateId), 1));
            }

            if (billBoardTransforms != null)
            {
                foreach (var t in billBoardTransforms)
                {
                    t.localEulerAngles = Camera.main.transform.localEulerAngles;
                }
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                IsPaused = !IsPaused;
            }

            if (Keyboard.current.qKey.wasPressedThisFrame) // Rebuild inventory items
            {
                if (!loaded)
                {
                    Debug.LogWarning($"Resource loading in progress, please wait...");
                    return;
                }

                var items = new List<Transform>();
                foreach (Transform item in inventory.transform)
                {
                    items.Add(item);
                }
                
                foreach (var item in items)
                {
                    Destroy(item.gameObject);
                }

                var packManager = ResourcePackManager.Instance;

                foreach (var itemNumId in InventoryBuildList)
                {
                    var item = ItemPalette.INSTANCE.GetByNumId(itemNumId);
                    var itemStack = new ItemStack(item, 1, null);

                    TestBuildInventoryItem($"Item [{itemNumId}] {item}", itemStack, packManager.ItemModelTable[itemNumId]);
                }
            }
        }
    }
}