using System.Collections;

namespace CraftSharp.Control
{
    public interface InteractionInfo
    {
        public int Id { get; set; }

        public string HintKey { get; }

        public string[] ParamTexts { get; }

        public IEnumerator RunInteraction(BaseCornClient client);
    }
}