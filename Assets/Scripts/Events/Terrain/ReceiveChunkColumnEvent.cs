namespace CraftSharp.Event
{
    public record ReceiveChunkColumnEvent : BaseEvent
    {
        public int ChunkX { get; }
        public int ChunkZ { get; }

        public ReceiveChunkColumnEvent(int x, int z)
        {
            this.ChunkX = x;
            this.ChunkZ = z;
        }
    }
}
