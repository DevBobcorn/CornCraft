namespace CraftSharp.Event
{
    public class HungerUpdateEvent
    {
        public int Hunger { get; }

        public HungerUpdateEvent(int hunger)
        {
            Hunger = hunger;
        }
    }
}