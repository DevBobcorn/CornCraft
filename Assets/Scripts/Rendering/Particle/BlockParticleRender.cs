using CraftSharp.Resource;
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class BlockParticleRender : ParticleRender<BlockParticleExtraData>
    {
        private static readonly System.Random random = new();

        public BlockParticleRender() : base(new ParticleRenderOptions { Duration = 5F, MaxParticles = 64 })
        {
            
        }

        protected override void ParticleStart(int idx, ParticleTransform particleTransform, ParticleStateData<BlockParticleExtraData> particleState)
        {
            var isLeaves = false; // TODO: Check with block tag

            particleState.LifeTime = isLeaves ? random.Next(30, 100) / 100F : random.Next(15, 70) / 100F;
            particleTransform.Scale = random.Next(80, 120) / 100F;

            // Assign particle color
            particleTransformCol[idx] = new Vector4((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble(), 1F);

            float xOfs = random.Next(0, 14) / 16F;
            float yOfs = random.Next(0, 14) / 16F;
            var part = new Vector4(xOfs, yOfs, 0.125F + xOfs, 0.125F + yOfs);
            //var part = new Vector4(0, 0, 1, 1);

            var uvs = ResourcePackManager.Instance.GetParticleUVs(particleState.ExtraData.BlockStateId, part);

            // Assign particle texture uvs (U, V, Z) * 4
            particleTransformTex[ idx << 2]      = new Vector4(uvs[3].x, uvs[3].y, uvs[3].z, 0F);
            particleTransformTex[(idx << 2) | 1] = new Vector4(uvs[1].x, uvs[1].y, uvs[1].z, 0F);
            particleTransformTex[(idx << 2) | 2] = new Vector4(uvs[2].x, uvs[2].y, uvs[2].z, 0F);
            particleTransformTex[(idx << 2) | 3] = new Vector4(uvs[0].x, uvs[0].y, uvs[0].z, 0F);

            Debug.Log($"Tex: {particleTransformTex[ idx << 2]} {particleTransformTex[(idx << 2) | 1]} {particleTransformTex[(idx << 2) | 2]} {particleTransformTex[(idx << 2) | 3]}");
            
            var ofs = new Vector3(random.Next(-100, 100), random.Next(-100, 100), random.Next(-100, 100)).normalized;
            particleTransform.Position += ofs * 3F; // * 0.3F;

            var dot = Vector3.Dot(ofs, Vector3.up);
            
            var acc = dot * 0.4F + 0.4F;
            var acc2 = dot > 0.8F ? dot * 0.8F : 0;
            
            particleState.Velocity = ofs * ((Mathf.Clamp(Mathf.Abs(ofs.y), 0F, 0.5F) + (isLeaves ? 0.5F : 1F)) * 
                                            (0.5F + random.Next(1, 150) / 100F) + (isLeaves ? 0F : acc + acc2));
        }

        protected override void ParticleUpdate(int idx, ParticleTransform particleTransform, ParticleStateData<BlockParticleExtraData> particleState)
        {
            particleState.Velocity.y -= 14F * Time.unscaledDeltaTime;

            particleState.LifeTime -= 1F * Time.unscaledDeltaTime;

            //Debug.Log($"Life {idx}: {particleState.LifeTime}");
        }

        protected override void ParticlePhysicsUpdate(int idx, ParticleTransform particleTransform, ParticleStateData<BlockParticleExtraData> particleState)
        {
            var velocity = particleState.Velocity;

            var posDelta = velocity * Time.deltaTime;
            var oldPosDelta = posDelta;

            if (posDelta.x == 0)
            {
                velocity.x = 0;
            }

            if (posDelta.z == 0)
            {
                velocity.z = 0;
            }

            if (oldPosDelta.y != posDelta.y)
            {
                var sign = Mathf.Sign(velocity.x);
                velocity.x -= sign * 6 * Time.deltaTime;
                if (velocity.x * sign <= 0)
                    velocity.x = 0;
                
                sign = Mathf.Sign(velocity.z);
                velocity.z -= sign * 6 * Time.deltaTime;
                if (velocity.z * sign <= 0)
                    velocity.z = 0;

                particleState.Velocity = velocity;
            }
            
            particleTransform.Position += posDelta;
        }

        protected override void InitializeMeshAndMaterial()
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshFilter = gameObject.AddComponent<MeshFilter>();

            material = new Material(Shader.Find("CornShader/Unlit/BlockParticle"))
            {
                name = "Block Particle"
            };
            material.SetTexture("_BaseMap", ResourcePackManager.Instance.GetAtlasArray(false));

            meshRenderer.sharedMaterial = material;
            meshRenderer.allowOcclusionWhenDynamic = false;

            var finalMesh = BuildMesh();

            meshFilter.sharedMesh = finalMesh;
        }
    }
}