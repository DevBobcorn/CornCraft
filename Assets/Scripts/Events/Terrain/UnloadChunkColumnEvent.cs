namespace MinecraftClient.Event
{
    public class UnloadChunkColumnEvent : BaseEvent
    {
        public readonly int chunkX, chunkZ;

        public UnloadChunkColumnEvent(int x, int z)
        {
            this.chunkX = x;
            this.chunkZ = z;
        }
    }
}
