using System.Collections.Generic;
using UnityEngine;

public class CelestiaBridgeFragment : MonoBehaviour
{
    [SerializeField] private int bridgeWidth = 6;
    [SerializeField] private int bridgeConnectionLength = 10;
    [SerializeField] private int bridgeBodyLength = 5;

    [SerializeField] private float perlinInitOffset = 0.1F;
    [SerializeField] private float perlinSampleInterval = 0.7F;
    [SerializeField] private float perlinSampleUnit = 0.1F;

    [SerializeField] private float defaultRiseSpeed = 1F;
    [SerializeField] private float targetHeight = 0F;
    [SerializeField] private float edgeOffset = 0.25F;
    [SerializeField] private float portalOffset = 0.5F;

    private readonly List<(GameObject, float, float)> bridgeBlocks = new();
    private readonly List<(GameObject, float, Material)> railBlocks = new();

    private float SamplePerlinNoise(int fragIndex, int horizontalPos)
    {
        return Mathf.PerlinNoise1D(fragIndex * perlinSampleInterval + horizontalPos * perlinSampleUnit + perlinInitOffset);
    }

    private float GetBlockOffset(int posInFragment)
    {
        return Random.Range(0F, posInFragment / (bridgeConnectionLength + bridgeBodyLength)) * 0.2F + posInFragment * 0.2F;
    }

    private float GetRailBlockOffset(int posInFragment)
    {
        return posInFragment * 0.35F;
    }

    public void BuildFragment(int fragIndex, GameObject surfaceBlock,
            GameObject edgeBlock, GameObject regularRail, GameObject poweredRail)
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
                var newBlock = CreateBridgeBlock(isEdge ? edgeBlock : surfaceBlock,
                        new Vector3(i, isEdge ? verticalPos + edgeOffset : verticalPos, prevConPos + j));
                var riseSpeed = defaultRiseSpeed + Random.Range(-0.1F, 0.1F);
                bridgeBlocks.Add((newBlock, riseSpeed, isEdge ? targetHeight + edgeOffset : targetHeight));

                if (i == 0) // Block in the middle, place rail block
                {
                    var (railBlock, railMat) = CreateRailBlock(regularRail, new(i, targetHeight + 0.55F, prevConPos + j));
                    railBlocks.Add((railBlock, Time.timeSinceLevelLoad, railMat));
                }
            }
        }

        for (int i = -bridgeWidth + 1;i < bridgeWidth;i++)
        {
            bool isEdge = Mathf.Abs(i) == bridgeWidth - 1;

            for (int j = 0;j < bridgeBodyLength;j++)
            {
                float randomOffset = GetBlockOffset(j);
                var newBlock = CreateBridgeBlock(isEdge ? edgeBlock : surfaceBlock,
                        new Vector3(i, (isEdge ? verticalPos + edgeOffset : verticalPos) - randomOffset, forwardPos + j));
                var riseSpeed = defaultRiseSpeed + Random.Range(-0.1F, 0.1F);
                bridgeBlocks.Add((newBlock, riseSpeed, isEdge ? targetHeight + edgeOffset : targetHeight));

                if (i == 0) // Block in the middle, place rail block
                {
                    var railOffset = GetRailBlockOffset(j);
                    var (railBlock, railMat) = CreateRailBlock(poweredRail, new(i, targetHeight + 0.55F, forwardPos + j));
                    railBlocks.Add((railBlock, Time.timeSinceLevelLoad + railOffset, railMat));
                }
            }
        }

        // Connection with next fragment
        for (int i = -bridgeWidth + 1;i < bridgeWidth;i++)
        {
            bool isEdge = Mathf.Abs(i) == bridgeWidth - 1;

            int nextConPos = forwardPos + bridgeBodyLength;
            float sample = SamplePerlinNoise(fragIndex, i);

            for (int j = 0;j < Mathf.RoundToInt(sample * bridgeConnectionLength);j++)
            {
                float randomOffset = GetBlockOffset(j + bridgeBodyLength);
                var newBlock = CreateBridgeBlock(isEdge ? edgeBlock : surfaceBlock,
                        new Vector3(i, (isEdge ? verticalPos + edgeOffset : verticalPos) - randomOffset, nextConPos + j));
                var riseSpeed = defaultRiseSpeed + Random.Range(-0.1F, 0.1F);
                bridgeBlocks.Add((newBlock, riseSpeed, isEdge ? targetHeight + edgeOffset : targetHeight));

                if (i == 0) // Block in the middle, place rail block
                {
                    var railOffset = GetRailBlockOffset(j + bridgeBodyLength);
                    var (railBlock, railMat) = CreateRailBlock(regularRail, new(i, targetHeight + 0.55F, nextConPos + j));
                    railBlocks.Add((railBlock, Time.timeSinceLevelLoad + railOffset, railMat));
                }
            }
        }
    }

    public void BuildFinalFragment(int fragIndex, GameObject surfaceBlock,
            GameObject edgeBlock, GameObject portalFrameBlock, GameObject portalBlock)
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
                var newBlock = CreateBridgeBlock(isEdge ? edgeBlock : surfaceBlock,
                        new Vector3(i, isEdge ? verticalPos + edgeOffset : verticalPos, prevConPos + j));
                var riseSpeed = defaultRiseSpeed + Random.Range(-0.1F, 0.1F);
                bridgeBlocks.Add((newBlock, riseSpeed, isEdge ? targetHeight + edgeOffset : targetHeight));
            }
        }

        for (int i = -bridgeWidth + 1;i < bridgeWidth;i++)
        {
            for (int j = 0;j < bridgeBodyLength;j++)
            {
                float randomOffset = GetBlockOffset(j);
                var newBlock = CreateBridgeBlock(surfaceBlock,
                        new Vector3(i, verticalPos + edgeOffset - randomOffset, forwardPos + j));
                var riseSpeed = defaultRiseSpeed + Random.Range(-0.1F, 0.1F);
                bridgeBlocks.Add((newBlock, riseSpeed, targetHeight + edgeOffset));
            }
        }

        int portalHeight = 7;

        // Create portal
        for (int i = -bridgeWidth + 1;i < bridgeWidth;i++)
        {
            bool isEdge = Mathf.Abs(i) == bridgeWidth - 1;
            int j = bridgeBodyLength;

            for (int k = 0; k < portalHeight; k++)
            {
                float randomOffset = GetBlockOffset(j);

                bool isFrame = isEdge || k == 0 || k == portalHeight - 1;

                if (isFrame)
                {
                    var newBlock = CreateBridgeBlock(portalFrameBlock,
                            new Vector3(i, verticalPos - randomOffset, forwardPos + j + randomOffset));
                    var riseSpeed = Random.Range(4F, 12F);
                    bridgeBlocks.Add((newBlock, riseSpeed, targetHeight + portalOffset + k + 0.5F));
                }
            }
        }
    }

    private GameObject CreateBridgeBlock(GameObject prefab, Vector3 position)
    {
        var newBlockObj = GameObject.Instantiate(prefab);
        newBlockObj.transform.SetParent(transform, false);
        newBlockObj.transform.position = position;

        return newBlockObj;
    }

    private (GameObject, Material) CreateRailBlock(GameObject prefab, Vector3 position)
    {
        var newBlockObj = GameObject.Instantiate(prefab);
        newBlockObj.transform.SetParent(transform, false);
        newBlockObj.transform.position = position;

        var renderer = newBlockObj.GetComponent<MeshRenderer>();

        var selfMaterial = renderer.sharedMaterial;
        // Make its own instance material
        renderer.material = new Material(selfMaterial);
        renderer.material.SetFloat("_FadeTime", 0F);

        return (newBlockObj, renderer.material);
    }

    void Update()
    {
        foreach (var (block, riseSpeed, target) in bridgeBlocks)
        {
            if (block.transform.position.y < target)
            {
                var newPos = block.transform.position;
                newPos.y = Mathf.MoveTowards(newPos.y, target, riseSpeed * Time.deltaTime);

                block.transform.position = newPos;
            }
        }
        
        var curTime = Time.timeSinceLevelLoad;

        foreach (var (railBlock, creationTime, railMat) in railBlocks)
        {
            var lifeTime = Mathf.Clamp01(curTime - creationTime - 1F);
            railMat.SetFloat("_FadeTime", lifeTime);
        }
    }
}
