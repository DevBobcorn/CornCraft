namespace CraftSharp.Event
{
    public class HungerUpdateEvent
    {
        public int Hunger { get; }
        public float Saturation { get; }

        public HungerUpdateEvent(int hunger, float saturation)
        {
            Hunger = hunger;
            Saturation = saturation;
        }
    }
}