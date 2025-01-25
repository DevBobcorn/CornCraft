namespace CraftSharp.Control
{
    public abstract class BlockInteractionInfo : InteractionInfo
    {
        protected readonly Block block;
        public Block Block => block;
        
        protected readonly BlockLoc blockLoc; // Location for calculating distance
        public BlockLoc BlockLoc => blockLoc;

        protected BlockInteractionInfo(int id, Block block, BlockLoc loc) : base(id)
        {
            this.block = block;
            blockLoc = loc;
        }
    }
}