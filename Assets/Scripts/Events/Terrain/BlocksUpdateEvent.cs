using System.Collections.Generic;
using MinecraftClient.Mapping;

namespace MinecraftClient.Event
{
    public class BlocksUpdateEvent : BaseEvent
    {
        public readonly List<Location> locations;

        public BlocksUpdateEvent(List<Location> locs)
        {
            this.locations = locs;
        }
    }
}
