using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    public abstract class ParticleRender<T> : MonoBehaviour where T : ParticleExtraData
    {
        protected Mesh finalMesh;
        protected Material material;

        protected ParticleTransform[] particleTransforms;
        protected Vector4[] particleTransformPos;
        protected ParticleStateData<T>[] particleStates;
        
        protected readonly ParticleRenderOptions options;
        
        protected static readonly int PosArrayProp = Shader.PropertyToID("_PosArray");

        protected MeshRenderer meshRenderer;
        protected MeshFilter meshFilter;

        public int ActiveParticles => activeParticles;

        private int activeParticles = 0;
        private bool simulating;

        protected ParticleRender(ParticleRenderOptions options)
        {
            this.options = options;
            
            particleTransformPos = new Vector4[options.MaxParticles];
            particleTransforms = new ParticleTransform[options.MaxParticles];
            particleStates = new ParticleStateData<T>[options.MaxParticles];

            for (int i = 0; i < options.MaxParticles; ++i)
            {
                particleTransforms[i] = new ParticleTransform();
                particleStates[i] = new ParticleStateData<T>();
            }
        }

        private void Start()
        {
            InitializeMeshAndMaterial();
            
            StartSimulate();
        }

        protected abstract void InitializeMeshAndMaterial();

        private void StartSimulate()
        {
            simulating = true;

            if (material != null)
            {
                var len = particleTransforms.Length;
                for (int i = 0; i < len; i++)
                {
                    var particleTransform = particleTransforms[i];
                    //particleTransform.Position = Vector3.zero;
                    ParticleStart(i, particleTransform, particleStates[i]);

                    particleTransformPos[i] = particleTransform.Position;

                    var bounds = meshRenderer.bounds;
                    bounds.Encapsulate(particleTransform.Position * 1.5F);
                    meshRenderer.bounds = bounds;
                }
                material.SetVectorArray(PosArrayProp, particleTransformPos);
            }

            meshRenderer.enabled = true;
        }

        private void StopSimulate()
        {
            simulating = false;
            meshRenderer.enabled = false;

            gameObject.name = $"[No active particle]";
        }

        private void Update()
        {
            if (!simulating) return;

            var len = particleTransforms.Length;

            for (int i = 0; i < len; i++)
            {
                var particleState = particleStates[i];
                var particleTransform = particleTransforms[i];

                if (particleState.LifeTime > 0)
                {
                    ParticleUpdate(i, particleTransform, particleStates[i]);
                    ParticlePhysicsUpdate(i, particleTransform, particleState);

                    particleTransformPos[i] = particleTransform.Position;

                    var bounds = meshRenderer.bounds;
                    bounds.Encapsulate(particleTransform.Position);
                    meshRenderer.bounds = bounds;

                    if (particleState.LifeTime <= 0) {
                        activeParticles--;
                    }
                }
                else
                {
                    //particleTransformMats[i] = Matrix4x4.zero;
                }
            }

            if (material != null)
            {
                material.SetVectorArray(PosArrayProp, particleTransformPos);

                Debug.Log(string.Join(',', particleTransformPos));
            }

            if (activeParticles <= 0)
            {
                StopSimulate();
            }

            gameObject.name = $"[Simulating {activeParticles} particles]";
        }

        public virtual void AddParticle(Vector3 initPos, T extraData)
        {
            for (int i = 0; i < particleStates.Length; i++)
            {
                if (particleStates[i].LifeTime <= 0F) // This particle is dead, use this slot
                {
                    var particleTransform = particleTransforms[i];

                    particleTransform.Position = initPos;
                    particleTransformPos[i] = particleTransform.Position;

                    
                    particleTransform.Scale = Vector3.one;

                    particleStates[i].ExtraData = extraData;
                    particleStates[i].LifeTime = options.Duration;

                    activeParticles++;

                    if (!simulating)
                    {
                        StartSimulate();
                    }

                    break;
                }
            }
            
        }

        protected virtual void ParticleStart(int idx, ParticleTransform particleTransform, ParticleStateData<T> particleState)
        {
            
        }

        protected virtual void ParticleUpdate(int idx, ParticleTransform particleTransform, ParticleStateData<T> particleState)
        {
            
        }

        protected virtual void ParticlePhysicsUpdate(int idx, ParticleTransform particleTransform, ParticleStateData<T> particleState)
        {
            particleTransform.Position += particleState.Velocity * Time.deltaTime;
        }

        public Mesh BuildMesh()
        {
            int particleCount = options.MaxParticles;

            int vertexCount = 4 * particleCount;
            int triIdxCount = vertexCount / 2 * 3;

            var visualBuffer = new VertexBuffer(vertexCount);
            int vertOffset;

            for (int i = 0; i < particleCount; ++i)
            {
                vertOffset = i * 4;

                visualBuffer.vert[vertOffset + 0] = new float3(0.1F, -.1F, 0F);
                visualBuffer.vert[vertOffset + 1] = new float3(0.1F, 0.1F, 0F);
                visualBuffer.vert[vertOffset + 2] = new float3(-.1F, -.1F, 0F);
                visualBuffer.vert[vertOffset + 3] = new float3(-.1F, 0.1F, 0F);

                // This channel is used in a different way than it is for block shaders
                visualBuffer.uvan[vertOffset + 0] = new(i, 0F/*particleOffset.x*/, 0F/*particleOffset.y*/, 0F);
                visualBuffer.uvan[vertOffset + 1] = new(i, 0F/*particleOffset.x*/, 0F/*particleOffset.y*/, 0F);
                visualBuffer.uvan[vertOffset + 2] = new(i, 0F/*particleOffset.x*/, 0F/*particleOffset.y*/, 0F);
                visualBuffer.uvan[vertOffset + 3] = new(i, 0F/*particleOffset.x*/, 0F/*particleOffset.y*/, 0F);
                
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

            //var bounds = new Bounds(new Vector3(0.5F, 0.5F, 0.5F), new Vector3(1F, 1F, 1F));

            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, triIdxCount)
            {
                //bounds = bounds,
                vertexCount = vertexCount
            }, MeshUpdateFlags.DontRecalculateBounds);

            // Create and assign mesh
            var mesh = new Mesh { /* bounds = bounds*/ };
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArr, mesh);

            return mesh;
        }
        
        private void OnDestroy()
        {
            DestroyImmediate(finalMesh, true);
            DestroyImmediate(material, true);
        }
    }
}