#nullable enable
using System;
using System.Collections;

namespace CraftSharp.Control
{
    public abstract class InteractionInfo
    {
        public int Id { get; protected set; }

        public virtual string HintKey { get; protected set; } = string.Empty;

        public virtual string[] ParamTexts { get; protected set; } = Array.Empty<string>();

        private IEnumerator? interactionEnumerator;

        public bool UpdateInteraction(BaseCornClient client)
        {
            interactionEnumerator ??= RunInteraction(client);
            return interactionEnumerator.MoveNext();
        }

        protected abstract IEnumerator RunInteraction(BaseCornClient client);
    }
}