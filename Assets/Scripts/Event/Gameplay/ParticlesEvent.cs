using UnityEngine;

namespace CraftSharp.Event
{
    public record ParticlesEvent : BaseEvent
    {
        public Vector3 Position { get; }
        public int TypeNumId { get; }
        public ParticleExtraData ExtraData { get; }
        public int Count { get; }

        public ParticlesEvent(Vector3 position, int typeNumId, ParticleExtraData extraData, int count)
        {
            Position = position;
            TypeNumId = typeNumId;
            ExtraData = extraData;
            Count = count;
        }
    }
}