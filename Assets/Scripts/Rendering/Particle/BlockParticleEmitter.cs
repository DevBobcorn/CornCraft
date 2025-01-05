using CraftSharp.Resource;
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class BlockParticleRender : ParticleRender
    {
        private static readonly float _padding = 0.3F;
        private static readonly float _step = 0.2F;
        
        private static readonly System.Random _random = new();
        private ChunkRenderManager _chunkRenderManager;
        private ChunkMaterialManager _chunkMaterialManager;

        public BlockParticleRender() : base(new ParticleRenderOptions { MaxParticles = 64 })
        {

        }

        protected override int GetSingleParticleVertexCount()
        {
            return 4;
        }

        protected override VertexBuffer GetSingleParticleBuffer()
        {
            /*
            var shapeMesh = BlockMetaDatabase.GetBlockMetaByCode(blockCode).shape.GetShapeMesh(1, 1, 0);
            var uv = shapeMesh.texcoords[0];
            var meshBuilder = new MeshBuilder();
            colorize = shapeMesh.colors[0].b;
            
            meshBuilder.SetQuadUV(uv, 0.015625f, 0.015625f);
            meshBuilder.AddQuad(Vector3.zero, Vector3.right, Vector3.up, 0.12f, 0.12f);
            
            return meshBuilder.GetBlockMesh();
            */

            return new VertexBuffer(0);
        }

        protected override Vector2 GetUvOffset(int idx)
        {
            //if (_chunkMaterialManager == null) return Vector2.zero;

            //return new Vector2(idx / 4 % 4 * 0.015625f, idx % 4 * 0.015625f);

            return Vector2.zero;
        }

        protected override float GetColorMultiplier(int idx)
        {
            return 1F;
        }

        protected override void ParticleStart(int idx, ParticleTransform particleTransform, ParticleStateData particleState)
        {
            /*
            var isLeaves = blockCode == BlockMetaDatabase.BuiltinBlockCode.Leaves;
            particleState.lifeTime = isLeaves ? _random.Next(30, 100) / 100f : _random.Next(15, 70) / 100f;

            particleTransform.scale = Vector3.one * (_random.Next(50, 150) / 100f);
            
            var ofs = new Vector3(_random.Next(-100, 100), _random.Next(-100, 100), _random.Next(-100, 100)).normalized;
            particleTransform.position += ofs * 0.3f;

            var dot = Vector3.Dot(ofs, Vector3.up);
            
            var acc = dot * 0.4f + 0.4f;
            var acc2 = dot > 0.8f ? dot * 0.8f : 0;
            
            particleState.velocity = ofs * ((Mathf.Clamp(Mathf.Abs(ofs.y), 0f, 0.5f) + 
                                             (isLeaves ? 0.5f : 1 )) * 
                                            (0.5f + _random.Next(1, 150) / 100f) + (isLeaves ? 0 : acc + acc2 ));
            */
        }

        protected override void ParticleUpdate(int idx, ParticleTransform particleTransform, ParticleStateData particleState)
        {
            particleState.Velocity.y -= 14f * Time.deltaTime;

            particleState.LifeTime -= 1 * Time.deltaTime;
        }

        protected override void ParticlePhysicsUpdate(int idx, ParticleTransform particleTransform, ParticleStateData particleState)
        {
            if (_chunkRenderManager == null)
            {
                _chunkMaterialManager = CornApp.CurrentClient.ChunkMaterialManager;

                return;
            }
            
            var pPos = particleTransform.position;
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
            
            particleTransform.position += posDelta;
        }

        protected override void StartUp()
        {
            var client = CornApp.CurrentClient;

            _chunkRenderManager = client.ChunkRenderManager;
            _chunkMaterialManager = client.ChunkMaterialManager;
        }
    }
}