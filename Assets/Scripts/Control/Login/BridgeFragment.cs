using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BridgeFragment : MonoBehaviour
{
    [SerializeField] private int bridgeWidth = 6;
    [SerializeField] private int bridgeConnectionLength = 10;
    [SerializeField] private int bridgeBodyLength = 5;

    [SerializeField] private float perlinInitOffset = 0.1F;
    [SerializeField] private float perlinSampleInterval = 0.7F;
    [SerializeField] private float perlinSampleUnit = 0.1F;

    private readonly List<GameObject> surfaceBlocks = new();
    private readonly List<GameObject> edgeBlocks = new();

    private float SamplePerlinNoise(int index, int horizontalPos)
    {
        return Mathf.PerlinNoise1D(index * perlinSampleInterval + horizontalPos * perlinSampleUnit + perlinInitOffset);
    }

    public void BuildFragment(int fragIndex, GameObject surfaceBlock, GameObject edgeBlock)
    {
        int forwardPos = fragIndex * (bridgeBodyLength + bridgeConnectionLength);
        float verticalPos = (fragIndex % 2) * 0.1F;

        // Connection with previous fragment
        for (int i = -bridgeWidth + 1;i < bridgeWidth;i++)
        {
            int prevConPos = forwardPos - bridgeConnectionLength;
            float sample = SamplePerlinNoise(fragIndex - 1, i);

            for (float j = Mathf.RoundToInt(sample * bridgeConnectionLength);j < bridgeConnectionLength;j++)
            {
                CreateBlock(surfaceBlock, new Vector3(i, verticalPos, prevConPos + j));
            }
        }

        for (int i = -bridgeWidth + 1;i < bridgeWidth;i++)
        {
            for (int j = 0;j < bridgeBodyLength;j++)
            {
                CreateBlock(surfaceBlock, new Vector3(i, verticalPos, forwardPos + j));
            }
        }

        // Connection with next fragment
        for (int i = -bridgeWidth + 1;i < bridgeWidth;i++)
        {
            int nextConPos = forwardPos + bridgeBodyLength;
            float sample = SamplePerlinNoise(fragIndex, i);

            for (float j = 0;j < Mathf.RoundToInt(sample * bridgeConnectionLength);j++)
            {
                CreateBlock(surfaceBlock, new Vector3(i, verticalPos, nextConPos + j));
            }
        }
    }

    private void CreateBlock(GameObject prefab, Vector3 position)
    {
        var newBlockObj = GameObject.Instantiate(prefab);
        newBlockObj.transform.SetParent(transform, false);
        newBlockObj.transform.position = position;
    }

    void Update()
    {
        
    }
}
