namespace CraftSharp.Event
{
    public record UnloadChunkColumnEvent : BaseEvent
    {
        public int ChunkX { get; }
        public int ChunkZ { get; }

        public UnloadChunkColumnEvent(int x, int z)
        {
            this.ChunkX = x;
            this.ChunkZ = z;
        }
    }
}
