using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    [ExecuteAlways]
    public abstract class ParticleRender : MonoBehaviour
    {
        protected Mesh finalMesh;
        protected Material material;

        protected ParticleTransform[] particleTransforms;
        protected Matrix4x4[] particleTransformMats;
        protected ParticleStateData[] particleStates;
        
        protected ParticleRenderOptions options;
        
        private static readonly int MatArrayProp = Shader.PropertyToID("_M_MatArray");

        protected MeshRenderer meshRenderer;
        protected MeshFilter meshFilter;

        protected float lastingTime;
        protected int maxParticles;

        private bool hidden;
        private static readonly int MAIN_TEX = Shader.PropertyToID("_MainTex");

        private Transform cameraTransform;
        private static readonly int ENV_LIGHT = Shader.PropertyToID("_EnvLight");

        protected ParticleRender(ParticleRenderOptions options)
        {
            this.options = options;
            lastingTime = options.MaxParticles;
            maxParticles = options.MaxParticles;
            
            particleTransformMats = new Matrix4x4[maxParticles];
            particleTransforms = new ParticleTransform[maxParticles];
            particleStates = new ParticleStateData[maxParticles];

            for (int i = 0; i < maxParticles; ++i)
            {
                particleTransforms[i] = new ParticleTransform();
                particleStates[i] = new ParticleStateData();
            }
        }

        private void Awake()
        {
            var shader = Shader.Find("B2nd/Block2nd_Particle_New");
            if (shader == null)
            {
                Debug.LogWarning("Particle Shader not Found");
                shader = Shader.Find("Standard");
            }
            material = new Material(shader);
            material.SetMatrixArray(MatArrayProp, particleTransformMats);

            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();

            meshRenderer.sharedMaterial = material;
            meshRenderer.allowOcclusionWhenDynamic = false;

            cameraTransform = Camera.main.transform;
        }

        private void Start()
        {
            BuildMesh();
            meshFilter.sharedMesh = finalMesh;

            if (options.PlayOnAwake)
            {
                Play();
            }
            
            StartUp();
        }

        protected virtual void StartUp()
        {
            
        }
        
        public void SetEnvLight(float skyLight, float blockLight)
        {
            material.SetVector(ENV_LIGHT, new Vector4(skyLight, blockLight));
        }

        private void PlayStart()
        {
            if (cameraTransform == null)
            {
                cameraTransform = Camera.main.transform;
            }

            var cameraPos = cameraTransform.position == null ? Vector3.zero : cameraTransform.position;
            var cameraUp = cameraTransform.up == null ? Vector3.up : cameraTransform.up;
            
            var rootPos = transform.position;
            var len = particleTransforms.Length;
            for (int i = 0; i < len; ++i)
            {
                var particleTransform = particleTransforms[i];
                particleTransform.position = transform.position;
                ParticleStart(i, particleTransform, particleStates[i]);

                particleTransformMats[i] = particleTransform.GetTransformMatrix4x4(rootPos, cameraPos, cameraUp);

                var bounds = meshRenderer.bounds;
                bounds.Encapsulate((particleTransform.position - transform.position) * 1.5F);
                meshRenderer.bounds = bounds;
            }
            material.SetMatrixArray(MatArrayProp, particleTransformMats);
        }

        private void Update()
        {
            if (hidden) return;

            var cameraPos = cameraTransform.position == null ? Vector3.zero : cameraTransform.position;
            var cameraUp = cameraTransform.up == null ? Vector3.up : cameraTransform.up;
            
            var len = particleTransforms.Length;
            var deadParticlesCount = 0;
            var rootPos = transform.position;
            for (int i = 0; i < len; ++i)
            {
                var particleState = particleStates[i];
                var particleTransform = particleTransforms[i];
                if (particleState.LifeTime > 0)
                {
                    ParticleUpdate(i, particleTransform, particleStates[i]);
                    ParticlePhysicsUpdate(i, particleTransform, particleState);

                    particleTransformMats[i] = particleTransform.GetTransformMatrix4x4(rootPos, cameraPos, cameraUp);

                    var bounds = meshRenderer.bounds;
                    bounds.Encapsulate(particleTransform.position - transform.position);
                    meshRenderer.bounds = bounds;
                }
                else
                {
                    deadParticlesCount++;
                    particleTransformMats[i] = Matrix4x4.zero;
                }
            }
            
            material.SetMatrixArray(MatArrayProp, particleTransformMats);

            lastingTime -= Time.deltaTime;
            if (lastingTime <= 0 || deadParticlesCount >= maxParticles)
            {
                Hide();
            }
        }

        private void Hide()
        {
            hidden = true;
            meshRenderer.enabled = false;
        }

        protected virtual void ParticleStart(int idx, ParticleTransform particleTransform, ParticleStateData particleState)
        {
            
        }

        protected virtual void ParticleUpdate(int idx, ParticleTransform particleTransform, ParticleStateData particleState)
        {
            
        }

        protected virtual void ParticlePhysicsUpdate(int idx, ParticleTransform particleTransform, ParticleStateData particleState)
        {
            particleTransform.position += particleState.Velocity * Time.deltaTime;
        }

        public Mesh BuildMesh()
        {
            var singleParticleBuffer = GetSingleParticleBuffer();

            int singleParticleVertexCount = singleParticleBuffer.vert.Length;
            int particleCount = options.MaxParticles;

            int vertexCount = singleParticleVertexCount * particleCount;
            int triIdxCount = vertexCount / 2 * 3;

            var visualBuffer = new VertexBuffer(vertexCount);
            int vertOffset;

            for (int i = 0; i < particleCount; ++i)
            {
                vertOffset = i * singleParticleVertexCount;

                var colorMultiplier = GetColorMultiplier(i);
                var particleOffset = GetUvOffset(i);

                for (int j = 0; j < singleParticleVertexCount; j++)
                {
                    visualBuffer.vert[vertOffset + j] = singleParticleBuffer.vert[j];
                    visualBuffer.txuv[vertOffset + j] = singleParticleBuffer.txuv[j];
                    // Get original tint * color multiplier
                    visualBuffer.tint[vertOffset + j] = singleParticleBuffer.tint[j] * colorMultiplier;
                    // This channel is used in a different way than it is for block shaders
                    visualBuffer.uvan[vertOffset + j] = new(i, particleOffset.x, particleOffset.y, 0F);
                }
            }

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

            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, triIdxCount)
            {
                bounds = bounds,
                vertexCount = vertexCount
            }, MeshUpdateFlags.DontRecalculateBounds);

            // Create and assign mesh
            var mesh = new Mesh { bounds = bounds };
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArr, mesh);
            // Recalculate mesh normals
            mesh.RecalculateNormals();
            // Recalculate mesh bounds
            mesh.RecalculateBounds();

            return mesh;
        }

        protected virtual Vector2 GetUvOffset(int idx)
        {
            return Vector2.zero;
        }

        protected virtual float GetColorMultiplier(int idx)
        {
            return 0;
        }

        protected abstract int GetSingleParticleVertexCount();
        
        protected abstract VertexBuffer GetSingleParticleBuffer();
        
        private void OnDestroy()
        {
            DestroyImmediate(finalMesh, true);
            DestroyImmediate(material, true);
        }

        public void Play()
        {
            hidden = false;
            meshRenderer.enabled = true;
            
            PlayStart();
        }
    }
}