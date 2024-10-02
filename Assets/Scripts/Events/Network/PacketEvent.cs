namespace CraftSharp.Event
{
    public record PacketEvent : BaseEvent
    {
        public bool InBound { get; }
        public int PacketId { get; }
        public byte[] Bytes { get; }

        public PacketEvent(bool inBound, int packetId, byte[] bytes)
        {
            InBound = inBound;
            PacketId = packetId;
            Bytes = bytes;
        }
    }
}
