using System.Collections.Generic;
using MinecraftClient.Mapping;

namespace MinecraftClient.Event
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
