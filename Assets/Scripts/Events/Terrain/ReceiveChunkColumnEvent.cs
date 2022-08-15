namespace MinecraftClient.Event
{
    public class ReceiveChunkColumnEvent : BaseEvent
    {
        public readonly int chunkX, chunkZ;

        public ReceiveChunkColumnEvent(int x, int z)
        {
            this.chunkX = x;
            this.chunkZ = z;
        }
    }
}
