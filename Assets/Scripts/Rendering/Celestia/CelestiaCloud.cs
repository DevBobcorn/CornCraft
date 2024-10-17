using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using AnimeSkybox;

namespace CraftSharp
{
    public class CelestialCloudMesh : MonoBehaviour
    {
        [SerializeField] private Sprite[] cloudSprites;
        [SerializeField] private int cloudQuadCountPerFragment = 12;
        [SerializeField] private GameObject cloudFragmentPrefab;

        [SerializeField] private Transform viewerTransform;
        [SerializeField] private int viewFragmentCount = 18;
        [SerializeField] private int maxFragmentCount  = 20;
        [SerializeField] private int fragmentLength = 5;

        [SerializeField] private float cloudHeight =    0F;
        [SerializeField] private float maxFadeDelay = 100F;
        [SerializeField] private float minSize = 4F;
        [SerializeField] private float maxSize = 5F;
        [SerializeField] private float spreadWidth = 50F;

        private readonly Queue<MeshFilter> existingFragments = new();
        private int nextFragmentIndex = 0;

        void Update()
        {
            int targetFragmentIndex = Mathf.RoundToInt(viewerTransform.position.z / fragmentLength) + viewFragmentCount;

            while (nextFragmentIndex < targetFragmentIndex)
            {
                CreateFragment(nextFragmentIndex);
                nextFragmentIndex++;
            }
        }

        private void CreateFragment(int fragIndex)
        {
            var fragmentObj = GameObject.Instantiate(cloudFragmentPrefab);
            fragmentObj.transform.SetParent(transform, false);
            fragmentObj.transform.localPosition = new Vector3(0F, 0F, fragIndex * fragmentLength);
            fragmentObj.name = $"Fragment #{fragIndex}";
            fragmentObj.layer = gameObject.layer;

            var fragment = fragmentObj.GetComponent<MeshFilter>();

            // Build fragment
            fragment.sharedMesh = CloudMeshBuilder.BuildCloudMesh(cloudSprites, cloudQuadCountPerFragment, maxFadeDelay,
                    (i, cloudSprite, posList) =>
                    {
                        //Quaternion cloudRotation = Quaternion.Euler(0F, 0F, 0F);
                        float cloudSize = Random.Range(minSize, maxSize);
                        float cloudDist = Random.Range(0F, fragmentLength);
                        float cloudXPos = Random.Range(-spreadWidth, spreadWidth);
                        float cloudElev = Mathf.Abs(cloudXPos) / spreadWidth * 25F;

                        var minY = cloudSprite.vertices.Min(x => x.y);
                        
                        return posList.Select(pos => new Vector3(cloudXPos + pos.x * cloudSize,
                                (pos.y - minY) * cloudSize + cloudElev + cloudHeight, cloudDist));
                    });

            existingFragments.Enqueue(fragment);

            while (existingFragments.Count > maxFragmentCount)
            {
                var target = existingFragments.Dequeue();

                Destroy(target.sharedMesh);
                
                //Debug.Log($"Destroying {target.gameObject.name}");
                Destroy(target.gameObject);
            }
        }
    }
}