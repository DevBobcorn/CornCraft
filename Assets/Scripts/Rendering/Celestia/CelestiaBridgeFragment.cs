using System.Collections.Generic;
using UnityEngine;

namespace CraftSharp
{
    public class CelestiaBridgeFragment : MonoBehaviour
    {
        [SerializeField] protected int bridgeWidth = 6;
        [SerializeField] protected int bridgeConnectionLength = 10;
        [SerializeField] protected int bridgeBodyLength = 5;

        [SerializeField] protected float perlinInitOffset = 0.1F;
        [SerializeField] protected float perlinSampleInterval = 0.7F;
        [SerializeField] protected float perlinSampleUnit = 0.1F;

        [SerializeField] protected float defaultRiseSpeed = 1F;
        [SerializeField] protected float targetHeight = 0F;
        [SerializeField] protected float edgeOffset = 0.25F;

        [SerializeField] protected GameObject bridgeBlock;
        [SerializeField] protected GameObject bridgeEdge;
        [SerializeField] protected GameObject regularRail;
        [SerializeField] protected GameObject poweredRail;

        protected readonly List<(GameObject, float, float)> bridgeBlocks = new();
        protected readonly List<(GameObject, float, Material)> railBlocks = new();

        protected float SamplePerlinNoise(int fragIndex, int horizontalPos)
        {
            return Mathf.PerlinNoise1D(fragIndex * perlinSampleInterval + horizontalPos * perlinSampleUnit + perlinInitOffset);
        }

        protected float GetBlockOffset(int posInFragment)
        {
            return Random.Range(0F, posInFragment / (bridgeConnectionLength + bridgeBodyLength)) * 0.2F + posInFragment * 0.2F;
        }

        private float GetRailBlockOffset(int posInFragment)
        {
            return posInFragment / 7.5F;
        }

        public virtual void BuildFragment(int fragIndex)
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
                    var riseSpeed = defaultRiseSpeed + Random.Range(-0.1F, 0.1F);
                    bridgeBlocks.Add((newBlock, riseSpeed, isEdge ? targetHeight + edgeOffset : targetHeight));

                    if (i == 0) // Block in the middle, place rail block
                    {
                        var railOffset = GetRailBlockOffset(j);
                        var (railBlock, railMat) = CreateFadingBlock(regularRail, new(i, targetHeight + 0.55F, prevConPos + j));
                        railBlocks.Add((railBlock, Time.timeSinceLevelLoad + railOffset, railMat));
                    }
                }
            }

            for (int i = -bridgeWidth + 1;i < bridgeWidth;i++)
            {
                bool isEdge = Mathf.Abs(i) == bridgeWidth - 1;

                for (int j = 0;j < bridgeBodyLength;j++)
                {
                    float randomOffset = GetBlockOffset(j);
                    var newBlock = CreateBlock(isEdge ? bridgeEdge : bridgeBlock,
                            new Vector3(i, (isEdge ? verticalPos + edgeOffset : verticalPos) - randomOffset, forwardPos + j));
                    var riseSpeed = defaultRiseSpeed + Random.Range(-0.1F, 0.1F);
                    bridgeBlocks.Add((newBlock, riseSpeed, isEdge ? targetHeight + edgeOffset : targetHeight));

                    if (i == 0) // Block in the middle, place rail block
                    {
                        var railOffset = GetRailBlockOffset(j + bridgeConnectionLength);
                        var (railBlock, railMat) = CreateFadingBlock(poweredRail, new(i, targetHeight + 0.55F, forwardPos + j));
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
                    var newBlock = CreateBlock(isEdge ? bridgeEdge : bridgeBlock,
                            new Vector3(i, (isEdge ? verticalPos + edgeOffset : verticalPos) - randomOffset, nextConPos + j));
                    var riseSpeed = defaultRiseSpeed + Random.Range(-0.1F, 0.1F);
                    bridgeBlocks.Add((newBlock, riseSpeed, isEdge ? targetHeight + edgeOffset : targetHeight));

                    if (i == 0) // Block in the middle, place rail block
                    {
                        var railOffset = GetRailBlockOffset(j + bridgeBodyLength + bridgeConnectionLength);
                        var (railBlock, railMat) = CreateFadingBlock(regularRail, new(i, targetHeight + 0.55F, nextConPos + j));
                        railBlocks.Add((railBlock, Time.timeSinceLevelLoad + railOffset, railMat));
                    }
                }
            }
        }

        protected GameObject CreateBlock(GameObject prefab, Vector3 position)
        {
            var newBlockObj = GameObject.Instantiate(prefab);
            newBlockObj.transform.SetParent(transform, false);
            newBlockObj.transform.position = position;

            return newBlockObj;
        }

        protected (GameObject, Material) CreateFadingBlock(GameObject prefab, Vector3 position)
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

        protected virtual void Update()
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
                var lifeTime = Mathf.Clamp01(curTime - creationTime - 2F);
                railMat.SetFloat("_FadeTime", lifeTime);
            }
        }
    }
}