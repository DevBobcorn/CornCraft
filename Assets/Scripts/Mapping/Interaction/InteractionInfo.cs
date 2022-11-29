#nullable enable

namespace MinecraftClient.Mapping
{
    public abstract class InteractionInfo
    {
        public int Id { get; set; }

        public abstract string GetHint();

        public abstract void RunInteraction(CornClient game);
    }
}