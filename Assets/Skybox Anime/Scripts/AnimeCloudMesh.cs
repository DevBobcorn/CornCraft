using System.Linq;
using UnityEngine;

namespace AnimeSkybox
{
    public class AnimeCloudMesh : MonoBehaviour
    {
        [SerializeField] private Sprite[] cloudSprites;
        [SerializeField] private int cloudQuadCount = 12;

        [SerializeField] private float cloudHeight =   0F;
        [SerializeField] private float cloudRadius = 400F;
        [SerializeField] private float maxElevation = 100F;
        [SerializeField] private float maxFadeDelay = 100F;
        [SerializeField] private float minSize = 80F;
        [SerializeField] private float maxSize = 120F;

        private void Start()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.sharedMesh = CloudMeshBuilder.BuildCloudMesh(cloudSprites, cloudQuadCount, maxFadeDelay,
                    (i, cloudSprite, posList) =>
                    {
                        var angleForEach = 360F / cloudQuadCount;
                        float cloudAngle = angleForEach * i + Random.Range(-angleForEach, angleForEach);
                        Quaternion cloudRotation = Quaternion.Euler(0F, cloudAngle, 0F);
                        float cloudSize = Random.Range(minSize, maxSize);
                        float cloudElev = Random.Range(0F, maxElevation);

                        var minY = cloudSprite.vertices.Min(x => x.y);
                        
                        return posList.Select(pos => cloudRotation * new Vector3(pos.x * cloudSize,
                                (pos.y - minY) * cloudSize + cloudElev + cloudHeight, cloudRadius));
                    });
        }
    }
}