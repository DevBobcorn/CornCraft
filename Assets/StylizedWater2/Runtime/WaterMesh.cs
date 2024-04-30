//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Random = System.Random;

namespace StylizedWater2
{
    [Serializable]
    public class WaterMesh
    {
        public enum Shape
        {
            Rectangle,
            Disk
        }
        public Shape shape;

        [FormerlySerializedAs("size")]
        [Range(10, 1000)]
        public float scale = 100f;
        [Tooltip("Distance between vertices")]
        [Range(0.15f, 10f)]
        public float vertexDistance = 1f;
        
        public float UVTiling = 1f;
        [Tooltip("Shifts the vertices in a random direction. Definitely use this when using flat shading")]
        [Range(0f, 1f)]
        public float noise;
        [Min(0)]
        [Tooltip("The surface is normally flat, yet vertex displacement on the GPU such as waves can give the surface artificial height." +
                 "\n\nThis can cause a Mesh Renderer to be prematurely culled, despite still actually being visible." +
                 "\n\nThis value adds an artificial amount of height to the generate mesh's bounds, to avoid this from happening.")]
        public float boundsPadding = 4f;
        
        /// <summary>
        /// Generated output mesh. Empty by default, use the Rebuild() function to generate one from the current settings.
        /// </summary>
        public Mesh mesh;
        
        private static Vector4 defaultTangent = new Vector4(-1f, 0f, 0f, -1f);
        
        public Mesh Rebuild()
        {
            switch (shape)
            {
                case Shape.Rectangle: mesh = CreatePlane();
                    break;
                case Shape.Disk: mesh = CreateCircle();
                    break;
            }

            return mesh;
        }

        public static Mesh Create(Shape shape, float size, float vertexDistance, float uvTiling = 1f, float noise = 0f)
        {
            WaterMesh waterMesh = new WaterMesh();
            waterMesh.shape = shape;
            waterMesh.scale = size;
            waterMesh.vertexDistance = vertexDistance;
            waterMesh.UVTiling = uvTiling;
            waterMesh.noise = noise;
			
            return waterMesh.Rebuild();
        }

        // Get the index of point number 'x' in circle number 'c'
        private int GetPointIndex(int c, int x)
        {
            if (c < 0) return 0;

            x = x % ((c + 1) * 6); 

            return (3 * c * (c + 1) + x + 1);
        }

        private Mesh CreateCircle()
        {
            Mesh m = new Mesh();
            m.name = "WaterDisk";

            int subdivisions = Mathf.FloorToInt(scale / vertexDistance);
            
            float distance = 1f / subdivisions;

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var uvs2 = new List<Vector2>();
            vertices.Add(Vector3.zero); //Center
            var tris = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector4> tangents = new List<Vector4>();
            
            // First pass => build vertices
            for (int loop = 0; loop < subdivisions; loop++)
            {
                float angleStep = (Mathf.PI * 2f) / ((loop + 1) * 6);
                for (int point = 0; point < (loop + 1) * 6; ++point)
                {
                    Vector3 vPos = new Vector3(
                    Mathf.Sin(angleStep * point) ,
                    0f,
                    Mathf.Cos(angleStep * point));
                    
                    UnityEngine.Random.InitState(loop + point);
                    vPos.x += UnityEngine.Random.Range(-noise * 0.01f, noise * 0.01f);
                    vPos.z -= UnityEngine.Random.Range(noise * 0.01f, -noise * 0.01f);

                    vertices.Add(vPos * (scale * 0.5f) * distance * (loop + 1));
                }
            }

            //Planar mapping
            for (int i = 0; i < vertices.Count; i++)
            {
                uvs.Add(new Vector2(0.5f + (vertices[i].x) * UVTiling,0.5f + (vertices[i].z) * UVTiling));
                //Lightmap UV's
                uvs2.Add(new Vector2(0.5f + (vertices[i].x / scale),0.5f + (vertices[i].z / scale)));
                
                normals.Add(Vector3.up);
                tangents.Add(defaultTangent);
            }

            // Second pass => connect vertices into triangles
            for (int circ = 0; circ < subdivisions; ++circ)
            {
                for (int point = 0, other = 0; point < (circ + 1) * 6; ++point)
                {
                    if (point % (circ + 1) != 0)
                    {
                        // Create 2 triangles
                        tris.Add(GetPointIndex(circ - 1, other + 1));
                        tris.Add(GetPointIndex(circ - 1, other));
                        tris.Add(GetPointIndex(circ, point));

                        tris.Add(GetPointIndex(circ, point));
                        tris.Add(GetPointIndex(circ, point + 1));
                        tris.Add(GetPointIndex(circ - 1, other + 1));
                        ++other;
                    }
                    else
                    {
                        // Create 1 inverse triangle
                        tris.Add(GetPointIndex(circ, point));
                        tris.Add(GetPointIndex(circ, point + 1));
                        tris.Add(GetPointIndex(circ - 1, other));
                        // Do not move to the next point in the smaller circle
                    }
                }
            }

            // Create the mesh

            int vertexCount = vertices.Count;
            if (vertexCount >= 65536) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(vertices, 0, vertexCount, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
            m.SetTriangles(tris, 0, false);

            m.SetUVs(0, uvs);
            m.SetUVs(1, uvs2);
            m.colors = new Color[vertexCount];
            
            m.SetNormals(normals, 0, vertexCount, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
            m.SetTangents(tangents, 0, vertexCount, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);

            m.bounds = new Bounds(Vector3.zero, new Vector3(scale, boundsPadding, scale));
            
            return m;
        }

        private Mesh CreatePlane()
        {
            Mesh m = new Mesh();
            m.name = "WaterPlane";

            scale = Mathf.Max(1f, scale);
            int subdivisions = Mathf.FloorToInt(scale / vertexDistance);
            
            int xCount = subdivisions + 1;
            int zCount = subdivisions + 1;
            int numTriangles = subdivisions * subdivisions * 6;
            int vertexCount = xCount * zCount;

            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            Vector2[] uvs2 = new Vector2[vertexCount];
            int[] triangles = new int[numTriangles];
            Vector4[] tangents = new Vector4[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];

            int index = 0;
            float scaleX = scale / subdivisions;
            float scaleY = scale / subdivisions;

            float noiseScale = vertexDistance * 0.5f;
            
            for (int z = 0; z < zCount; z++)
            {
                for (int x = 0; x < xCount; x++)
                {
                    vertices[index] = new Vector3(x * scaleX - (scale * 0.5f), 0f, z * scaleY - (scale * 0.5f));
                    
                    UnityEngine.Random.InitState(index);
                    vertices[index].x += UnityEngine.Random.Range(-noise * noiseScale, noise * noiseScale);
                    vertices[index].z -= UnityEngine.Random.Range(noise * noiseScale, -noise * noiseScale);
                    
                    uvs[index] = new Vector2(0.5f + (vertices[index].x) * UVTiling, 0.5f + (vertices[index].z) * UVTiling);
                    //Lightmap UV's
                    uvs2[index] = new Vector2(0.5f + vertices[index].x / scale, 0.5f + vertices[index].z / scale);
                    
                    tangents[index] = defaultTangent;
                    normals[index] = Vector3.up;
                    
                    index++;
                }
            }

            index = 0;
            for (int z = 0; z < subdivisions; z++)
            {
                for (int x = 0; x < subdivisions; x++)
                {
                    triangles[index] = (z * xCount) + x;
                    triangles[index + 1] = ((z + 1) * xCount) + x;
                    triangles[index + 2] = (z * xCount) + x + 1;

                    triangles[index + 3] = ((z + 1) * xCount) + x;
                    triangles[index + 4] = ((z + 1) * xCount) + x + 1;
                    triangles[index + 5] = (z * xCount) + x + 1;
                    index += 6;
                }
            }

            if (vertexCount >= 65536) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(vertices, 0, vertexCount, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);;
            m.SetTriangles(triangles, 0, false);
            
            m.SetUVs(0, uvs);
            m.SetUVs(1, uvs2);
            
            m.SetNormals(normals, 0, vertexCount);
            m.SetTangents(tangents, 0, vertexCount);
            
            m.colors = new Color[vertexCount];
            m.bounds = new Bounds(Vector3.zero, new Vector3(scale, boundsPadding, scale));

            return m;
        }
    }
}