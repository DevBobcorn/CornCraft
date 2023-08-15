using System.Collections.Generic;
namespace CraftSharp.Event
{
    public record BlocksUpdateEvent : BaseEvent
    {
        public List<Location> Locations { get; }

        public BlocksUpdateEvent(List<Location> locs)
        {
            this.Locations = locs;
        }
    }
}
