namespace CraftSharp.Control
{
    public abstract class InteractionInfo
    {
        public int Id { get; set; }

        public abstract string GetHintKey();

        public abstract string[] GetParamTexts();

        public abstract void RunInteraction(BaseCornClient client);
    }
}