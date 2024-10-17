using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;

namespace AnimeSkybox
{
    public class CloudMeshBuilder
    {
        public static Mesh BuildCloudMesh(Sprite[] cloudSprites, int cloudQuadCount, float maxFadeDelay, Func<int, Sprite, IEnumerable<Vector2>, IEnumerable<Vector3>> getMeshVertices)
        {
            Mesh combinedMesh = new();

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var delays = new List<Vector2>();
            var triangles = new List<int>();

            for (int i = 0; i < cloudQuadCount; i++)
            {
                var vertexIndexOffset = vertices.Count;
                var cloudSpriteIndex = i % cloudSprites.Length;
                var cloudSprite = cloudSprites[cloudSpriteIndex];

                var minV = float.MaxValue;
                var minU = float.MaxValue;
                var maxV = float.MinValue;
                var maxU = float.MinValue;

                for (int s = 0; s < cloudSprite.uv.Length; s++)
                {
                    var uv = cloudSprite.uv[s];

                    if (uv.y < minV) minV = uv.y;
                    if (uv.y > maxV) maxV = uv.y;
                    if (uv.x < minU) minU = uv.x;
                    if (uv.x > maxU) maxU = uv.x;
                }

                //float cloudAngle = angleForEach * i + UnityEngine.Random.Range(-angleForEach, angleForEach);
                //float cloudSize = UnityEngine.Random.Range(minSize, maxSize);
                //float cloudElev = UnityEngine.Random.Range(0F, maxElevation);
                //Quaternion cloudRotation = Quaternion.Euler(0f, cloudAngle, 0f);
                float fadeDelay = UnityEngine.Random.Range(0F, maxFadeDelay);

                var spriteVertexCount = cloudSprite.GetVertexCount();
                var spriteVertices = cloudSprite.vertices;
                // Flip UVs vertically
                var spriteUVs = cloudSprite.uv.Select(uv => new Vector2(
                        Mathf.Lerp(maxU, minU, (uv.x - minU) / (maxU - minU)),
                        Mathf.Lerp(maxV, minV, (uv.y - minV) / (maxV - minV))
                ));
                
                var spriteIndices = cloudSprite.triangles;

                //vertices.AddRange(spriteVertices.Select(pos => cloudRotation * new Vector3(pos.x * cloudSize, (pos.y - minY) * cloudSize + cloudElev + cloudHeight, cloudRadius)));
                vertices.AddRange(getMeshVertices(i, cloudSprite, spriteVertices));
                uvs.AddRange(spriteUVs);
                delays.AddRange(Enumerable.Repeat(new Vector2(fadeDelay, 0F), spriteVertexCount));
                triangles.AddRange(spriteIndices.Select(x => vertexIndexOffset + x));
            }

            combinedMesh.vertices = vertices.ToArray();
            combinedMesh.triangles = triangles.ToArray();
            combinedMesh.uv2 = uvs.ToArray();
            combinedMesh.uv3 = delays.ToArray();

            return combinedMesh;
        }
    }
}