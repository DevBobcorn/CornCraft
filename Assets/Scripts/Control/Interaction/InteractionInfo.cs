namespace CraftSharp.Control
{
    public abstract class InteractionInfo
    {
        public int Id { get; set; }

        public abstract InteractionIconType GetIconType();

        public abstract ResourceLocation GetIconItemId();

        public abstract string GetHintKey();

        public abstract string[] GetParamTexts();

        public abstract void RunInteraction(BaseCornClient client);
    }
}