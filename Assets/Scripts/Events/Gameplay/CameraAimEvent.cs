#nullable enable
using UnityEngine;

namespace CraftSharp.Event
{
    public record CameraAimEvent : BaseEvent
    {
        public bool Aim { get; }
        public Transform? AimRef { get; }

        public CameraAimEvent(bool aim, Transform? aimRef)
        {
            Aim = aim;
            AimRef = aimRef;
        }
    }
}