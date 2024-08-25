using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;

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

        void Start()
        {
            GenerateClouds();
        }

        private void GenerateClouds()
        {
            Mesh combinedMesh = new();

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var delays = new List<Vector2>();
            var triangles = new List<int>();

            var cloudAngles = new List<float>();

            var angleForEach = 360F / cloudQuadCount;

            for (int i = 0; i < cloudQuadCount; i++)
            {
                var vertexIndexOffset = vertices.Count;
                var cloudSpriteIndex = i % cloudSprites.Length;
                var cloudSprite = cloudSprites[cloudSpriteIndex];

                var minV = float.MaxValue;
                var minU = float.MaxValue;
                var maxV = float.MinValue;
                var maxU = float.MinValue;

                var minY = float.MaxValue;

                for (int s = 0; s < cloudSprite.uv.Length; s++)
                {
                    var uv = cloudSprite.uv[s];

                    if (uv.y < minV) minV = uv.y;
                    if (uv.y > maxV) maxV = uv.y;
                    if (uv.x < minU) minU = uv.x;
                    if (uv.x > maxU) maxU = uv.x;

                    if (cloudSprite.vertices[s].y < minY)
                    {
                        minY = cloudSprite.vertices[s].y;
                    }
                }

                float cloudAngle = angleForEach * i + UnityEngine.Random.Range(-angleForEach, angleForEach);
                float cloudSize = UnityEngine.Random.Range(minSize, maxSize);
                float cloudElev = UnityEngine.Random.Range(0F, maxElevation);
                float fadeDelay = UnityEngine.Random.Range(0F, maxFadeDelay);
                
                Quaternion cloudRotation = Quaternion.Euler(0f, cloudAngle, 0f);

                cloudAngles.Add(cloudAngle);

                var spriteVertexCount = cloudSprite.GetVertexCount();
                var spriteVertices = cloudSprite.vertices;
                // Flip UVs vertically
                var spriteUVs = cloudSprite.uv.Select(uv => new Vector2(
                        Mathf.Lerp(maxU, minU, (uv.x - minU) / (maxU - minU)),
                        Mathf.Lerp(maxV, minV, (uv.y - minV) / (maxV - minV))
                ));
                
                var spriteIndices = cloudSprite.triangles;

                vertices.AddRange(spriteVertices.Select(pos => cloudRotation * new Vector3(pos.x * cloudSize, (pos.y - minY) * cloudSize + cloudElev + cloudHeight, cloudRadius)));
                uvs.AddRange(spriteUVs);
                delays.AddRange(Enumerable.Repeat(new Vector2(fadeDelay, 0F), spriteVertexCount));
                triangles.AddRange(spriteIndices.Select(x => vertexIndexOffset + x));

                //Debug.Log($"[{i}] Idx: {string.Join(", ", spriteIndices)}");
            }

            combinedMesh.vertices = vertices.ToArray();
            combinedMesh.triangles = triangles.ToArray();
            combinedMesh.uv2 = uvs.ToArray();
            combinedMesh.uv3 = delays.ToArray();

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = combinedMesh;
        }
    }
}