using Unity.Mathematics;

namespace CraftSharp.Rendering
{
    public record BlockParticleExtraDataWithColor : ParticleExtraData
    {
        public int BlockStateId;
        public int BlockColor;

        public BlockParticleExtraDataWithColor(int blockStateId, int blockColor)
        {
            BlockStateId = blockStateId;
            BlockColor = blockColor;
        }
    }
}