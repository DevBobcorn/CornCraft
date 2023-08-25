#nullable enable
using CraftSharp.Interaction;

namespace CraftSharp.Event
{
    public record PlayerLiquidEvent : BaseEvent
    {
        public bool Enter { get; }

        public PlayerLiquidEvent(bool enter)
        {
            Enter = enter;
        }
    }
}