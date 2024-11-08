using System.Collections;

namespace CraftSharp.Control
{
    public abstract class InteractionInfo
    {
        public int Id { get; set; }

        public abstract string GetHintKey();

        public abstract string[] GetParamTexts();

        public abstract IEnumerator RunInteraction(BaseCornClient client);
    }
}