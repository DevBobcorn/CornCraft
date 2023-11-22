using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

using CraftSharp.Resource;
using CraftSharp.Demo;

namespace CraftSharp
{
    public class CelestiaPortalFragment : CelestiaBridgeFragment
    {
        [SerializeField] private float portalOffset = 0.5F;
        [SerializeField] private int portalWidth  = 3;
        [SerializeField] private int portalHeight = 5;
        [SerializeField] private GameObject portalFrame;
        [SerializeField] private Material portalBlockMaterial;
        
        [SerializeField] [ColorUsage(false, true)] private Color portalBlockEmissionColor;

        public bool FrameGenerationComplete = false;

        private GameObject portalBlockPrototype;
        private float portalPosZ = 0F;
        private bool enablePortalEmission = false;
        private float emissionTime = 0F;

        private readonly List<(GameObject, float, Vector3)> portalFrameBlocks = new();

        protected readonly List<(GameObject, float)> portalBlocks = new();

        public override void BuildFragment(int fragIndex)
        {
            int forwardPos = fragIndex * (bridgeBodyLength + bridgeConnectionLength);
            float verticalPos = -2F; // (fragIndex % 2) * 0.1F;

            // Connection with previous fragment
            for (int i = -bridgeWidth + 1;i < bridgeWidth;i++)
            {
                bool isEdge = Mathf.Abs(i) == bridgeWidth - 1;

                int prevConPos = forwardPos - bridgeConnectionLength;
                float sample = SamplePerlinNoise(fragIndex - 1, i);

                for (int j = Mathf.RoundToInt(sample * bridgeConnectionLength);j < bridgeConnectionLength;j++)
                {
                    var newBlock = CreateBlock(isEdge ? bridgeEdge : bridgeBlock,
                            new Vector3(i, isEdge ? verticalPos + edgeOffset : verticalPos, prevConPos + j));
                    var riseSpeed = defaultRiseSpeed + UnityEngine.Random.Range(-0.1F, 0.1F);
                    bridgeBlocks.Add((newBlock, riseSpeed, isEdge ? targetHeight + edgeOffset : targetHeight));
                }
            }

            for (int i = -bridgeWidth + 1;i < bridgeWidth;i++)
            {
                for (int j = 0;j < 12;j++)
                {
                    float randomOffset = GetBlockOffset(j);
                    var newBlock = CreateBlock(bridgeEdge,
                            new Vector3(i, verticalPos + edgeOffset - randomOffset, forwardPos + j));
                    var riseSpeed = defaultRiseSpeed + UnityEngine.Random.Range(-0.1F, 0.1F);
                    bridgeBlocks.Add((newBlock, riseSpeed, targetHeight + edgeOffset));
                }
            }

            portalPosZ = forwardPos + 12;

            // Create portal
            for (int i = -portalWidth + 1;i < portalWidth;i++)
            {
                bool isEdge = Mathf.Abs(i) == portalWidth - 1;

                for (int k = 0; k < portalHeight; k++)
                {
                    float randomOffset = GetBlockOffset(k);

                    bool isFrame = isEdge || k == 0 || k == portalHeight - 1;

                    if (isFrame)
                    {
                        var newBlock = CreateBlock(portalFrame,
                                new Vector3(i * 2 + randomOffset, verticalPos - randomOffset, portalPosZ));
                        var riseSpeed = UnityEngine.Random.Range(8F, 12F);
                        portalFrameBlocks.Add((newBlock, riseSpeed, 
                                new Vector3(i, targetHeight + portalOffset + k, portalPosZ)));
                    }
                }
            }
        }

        private IEnumerator FillPortal(float posZ)
        {
            var wait = new WaitForSecondsRealtime(0.1F);

            for (int dist = 0; dist < portalWidth - 1 + (portalHeight / 2); dist++)
            {
                // Fill portal
                for (int i = -portalWidth + 2;i < portalWidth - 1;i++)
                {
                    for (int k = 1; k < portalHeight - 1; k++)
                    {
                        if (Mathf.Abs(i) + Mathf.Abs(k - (portalHeight / 2)) != dist)
                        {
                            continue;
                        }

                        var newBlock = CreateBlock(portalBlockPrototype,
                                new Vector3(i, targetHeight + portalOffset + k, posZ));
                        
                        // Rescale portal blocks
                        newBlock.transform.localScale = new Vector3(0F, 0F, 0.125F);

                        portalBlocks.Add((newBlock, 0.2F));
                    }
                }
                
                yield return wait;
            }
        }

        public void EnablePortalEmission()
        {
            enablePortalEmission = true;
            emissionTime = 0F;
        }

        public void Start()
        {
            var visualBuffer = new VertexBuffer();
            CelestiaPortalGeometry.Build(ref visualBuffer, new float3(-0.5F),
                    0b110000, new float3(1F), 0.05F, 32, 8);
            
            var portalBlockMesh = VertexBufferBuilder.BuildMesh(visualBuffer);
            var portalBlockObject = new GameObject("Portal Block");
            
            // Add mesh filter to the block
            var portalMeshFilter = portalBlockObject.AddComponent<MeshFilter>();
            portalMeshFilter.sharedMesh = portalBlockMesh;

            // Add mesh renderer to the block
            var portalMeshRenderer = portalBlockObject.AddComponent<MeshRenderer>();
            portalMeshRenderer.material = new Material(portalBlockMaterial);

            portalBlockPrototype = portalBlockObject;
        }

        public override void Update()
        {
            base.Update();

            var generationComplete = true;

            foreach (var (block, riseSpeed, target) in portalFrameBlocks)
            {
                if (block.transform.position != target)
                {
                    var newPos = Vector3.MoveTowards(block.transform.position, target, riseSpeed * Time.deltaTime);

                    block.transform.position = newPos;

                    generationComplete = false;
                }
            }

            if (generationComplete && !FrameGenerationComplete)
            {
                FrameGenerationComplete = true;
                // Fill the portal
                StartCoroutine(FillPortal(portalPosZ));
            }

            if (enablePortalEmission)
            {
                emissionTime += Time.deltaTime * 10F;

                // Prototype and other portal blocks share the same
                // material instance
                portalBlockPrototype.GetComponent<MeshRenderer>().sharedMaterial
                        .SetColor("_EmissionColor", portalBlockEmissionColor * emissionTime);
            }

            foreach (var (portalBlock, speed) in portalBlocks)
            {
                var curSize = portalBlock.transform.localScale;
                var newSize = Mathf.MoveTowards(curSize.x, 1F, speed);

                portalBlock.transform.localScale = new(newSize, newSize, curSize.z);
            }
        }
    }
}