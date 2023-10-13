using System.Collections.Generic;
namespace CraftSharp.Event
{
    public record BlocksUpdateEvent : BaseEvent
    {
        public List<BlockLoc> Locations { get; }

        public BlocksUpdateEvent(List<BlockLoc> locations)
        {
            this.Locations = locations;
        }
    }
}
