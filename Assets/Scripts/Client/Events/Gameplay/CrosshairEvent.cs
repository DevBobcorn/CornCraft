#nullable enable
using UnityEngine;

namespace CraftSharp.Event
{
    public record CrosshairEvent : BaseEvent
    {
        public bool Show { get; }

        public CrosshairEvent(bool show)
        {
            Show = show;
        }
    }
}