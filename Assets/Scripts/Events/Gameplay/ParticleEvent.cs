using UnityEngine;

namespace CraftSharp.Event
{
    public record ParticleEvent : BaseEvent
    {
        public Vector3 Position { get; }
        public int TypeNumId { get; }
        public ParticleExtraData ExtraData { get; }

        public ParticleEvent(Vector3 position, int typeNumId, ParticleExtraData extraData)
        {
            Position = position;
            TypeNumId = typeNumId;
            ExtraData = extraData;
        }
    }
}