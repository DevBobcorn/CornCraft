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

            particleTransform.Scale = Vector3.one * (random.Next(50, 150) / 100F);
            
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
            particleState.Velocity.y -= 14f * Time.deltaTime;

            particleState.LifeTime -= 1 * Time.deltaTime;
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

            meshRenderer.sharedMaterial = material;
            meshRenderer.allowOcclusionWhenDynamic = false;

            var finalMesh = BuildMesh();

            meshFilter.sharedMesh = finalMesh;
        }
    }
}