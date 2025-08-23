namespace CraftSharp.Event
{
    public record TickSyncEvent
    {
        public int PassedTicks { get; }
        
        public TickSyncEvent(int passedTicks)
        {
            PassedTicks = passedTicks;
        }
    }
}