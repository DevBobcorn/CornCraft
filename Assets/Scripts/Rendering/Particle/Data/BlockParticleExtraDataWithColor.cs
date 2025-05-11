using Unity.Mathematics;

namespace CraftSharp.Rendering
{
    public record BlockParticleExtraDataWithColor : ParticleExtraData
    {
        public int BlockStateId;
        public float3 BlockColor;

        public BlockParticleExtraDataWithColor(int blockStateId, float3 blockColor)
        {
            BlockStateId = blockStateId;
            BlockColor = blockColor;
        }
    }
}