#nullable enable
using System;
using System.Collections;

namespace CraftSharp.Control
{
    public abstract class InteractionInfo
    {
        public int Id { get; }

        public virtual string HintKey { get; protected set; } = string.Empty;

        public virtual string[] ParamTexts { get; protected set; } = Array.Empty<string>();

        private IEnumerator? interactionEnumerator;

        protected InteractionInfo(int id)
        {
            Id = id;
        }

        public bool UpdateInteraction(BaseCornClient client)
        {
            interactionEnumerator ??= RunInteraction(client);
            return interactionEnumerator.MoveNext();
        }

        protected abstract IEnumerator RunInteraction(BaseCornClient client);
    }
}