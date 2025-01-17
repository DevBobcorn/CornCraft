using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    public interface IParticleRender
    {
        public void Initialize();

        public void ManagedUpdate();

        public void AddParticles(Vector3 initPos, int typeNumId, ParticleExtraData extraData, int count);
    }

    public abstract class ParticleRender<T> : MonoBehaviour, IParticleRender where T : ParticleExtraData
    {
        protected Mesh finalMesh;
        protected Material material;

        protected readonly ParticleTransform[] particleTransforms;
        protected readonly Vector4[] particleTransformPos; // X, Y, Z, Scale
        protected readonly Vector4[] particleTransformCol; // R, G, B, Light
        protected readonly Vector4[] particleTransformTex; // (U, V, Z, Size) * 2
        protected readonly ParticleStateData<T>[] particleStates;
        
        protected readonly ParticleRenderOptions options;
        
        protected static readonly int PosArrayProp = Shader.PropertyToID("_PosArray");
        protected static readonly int ColArrayProp = Shader.PropertyToID("_ColArray");
        protected static readonly int TexArrayProp = Shader.PropertyToID("_TexArray");

        protected MeshRenderer meshRenderer;
        protected MeshFilter meshFilter;

        public int ActiveParticles => activeParticles;

        private int activeParticles = 0;
        private bool simulating;

        protected ParticleRender(ParticleRenderOptions options)
        {
            this.options = options;
            
            particleTransformPos = new Vector4[options.MaxParticles];
            particleTransformCol = new Vector4[options.MaxParticles];
            particleTransformTex = new Vector4[options.MaxParticles * 4];

            particleTransforms = new ParticleTransform[options.MaxParticles];
            particleStates = new ParticleStateData<T>[options.MaxParticles];

            for (int i = 0; i < options.MaxParticles; ++i)
            {
                particleTransforms[i] = new ParticleTransform();
                particleStates[i] = new ParticleStateData<T>();
            }
        }

        public virtual void Initialize()
        {
            InitializeMeshAndMaterial();
        }

        public virtual void HandleResourceLoad()
        {
            InitializeMeshAndMaterial();
        }

        protected abstract void InitializeMeshAndMaterial();

        private void StartSimulate()
        {
            simulating = true;

            if (material != null)
            {
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

        public virtual void ManagedUpdate()
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

                    particleTransformPos[i] = particleTransform.GetAsVector4();

                    var bounds = meshRenderer.bounds;
                    bounds.Encapsulate(particleTransform.Position);
                    meshRenderer.bounds = bounds;

                    if (particleState.LifeTime <= 0) {
                        activeParticles--;

                        particleTransform.Scale = 0F;
                        particleTransformPos[i].w = 0F;
                    }
                }
            }

            if (material != null)
            {
                material.SetVectorArray(PosArrayProp, particleTransformPos);
                material.SetVectorArray(ColArrayProp, particleTransformCol);
                material.SetVectorArray(TexArrayProp, particleTransformTex);

                //Debug.Log(string.Join(',', particleTransformData));
            }

            if (activeParticles <= 0)
            {
                StopSimulate();
            }

            gameObject.name = $"[Simulating {activeParticles} particles]";
        }

        public virtual void AddParticles(Vector3 initPos, int typeNumId, ParticleExtraData extraData, int count)
        {
            if (extraData is T specificExtraData)
            {
                for (int i = 0; i < count; i++)
                {
                    AddParticle(initPos, typeNumId, specificExtraData);
                }
            }
            else
            {
                Debug.LogError($"Particle extra data type doesn't match: Expected {typeof (T)}, got {extraData.GetType()}");
            }
        }

        public virtual void AddParticle(Vector3 initPos, int typeNumId, T extraData)
        {
            for (int i = 0; i < particleStates.Length; i++)
            {
                if (particleStates[i].LifeTime <= 0F) // This particle is dead, use this slot
                {
                    var particleTransform = particleTransforms[i];

                    particleStates[i].TypeNumId = typeNumId;

                    particleTransform.Position = initPos;
                    particleStates[i].ExtraData = extraData;
                    particleStates[i].LifeTime = options.Duration;

                    ParticleStart(i, particleTransform, particleStates[i]);

                    particleTransformPos[i] = particleTransform.GetAsVector4();

                    activeParticles++;

                    if (!simulating)
                    {
                        StartSimulate();
                    }

                    //Debug.Log($"Add particle #{i} at {initPos}");
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

        protected Mesh BuildMesh()
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
                visualBuffer.uvan[vertOffset + 0] = new(i,  i << 2,      0F, 0F);
                visualBuffer.uvan[vertOffset + 1] = new(i, (i << 2) | 1, 0F, 0F);
                visualBuffer.uvan[vertOffset + 2] = new(i, (i << 2) | 2, 0F, 0F);
                visualBuffer.uvan[vertOffset + 3] = new(i, (i << 2) | 3, 0F, 0F);
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

            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, triIdxCount)
            {
                //bounds = bounds,
                vertexCount = vertexCount
            }, MeshUpdateFlags.DontRecalculateBounds);

            // Create and assign mesh
            var mesh = new Mesh();
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