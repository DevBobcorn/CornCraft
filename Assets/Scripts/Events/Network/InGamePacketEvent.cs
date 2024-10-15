namespace CraftSharp.Event
{
    public record InGamePacketEvent : BaseEvent
    {
        public bool InBound { get; }
        public int PacketId { get; }
        public byte[] Bytes { get; }

        public InGamePacketEvent(bool inBound, int packetId, byte[] bytes)
        {
            InBound = inBound;
            PacketId = packetId;
            Bytes = bytes;
        }
    }
}
