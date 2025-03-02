#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace CraftSharp.Control
{
    public enum InteractionType
    {
        Break = 0,
        Place = 1,
        Interact = 2
    }

    public enum InteractionIconType
    {
        Dialog,
        EnterLocation,
        Ride,
        ItemIcon
    }

    public abstract record Interaction(InteractionType Type, string HintKey, string Tag)
    {
        public InteractionType Type { get; } = Type;
        public string HintKey { get; } = HintKey;
        public string Tag { get; } = Tag;
    }

    public record TriggerInteraction(
        InteractionIconType IconType,
        ResourceLocation IconItemId,
        bool Reusable,
        InteractionType Type,
        string HintKey,
        string Tag)
        : Interaction(Type, HintKey, Tag)
    {
        public InteractionIconType IconType { get; } = IconType;
        public ResourceLocation IconItemId { get; } = IconItemId;
        public bool Reusable { get; } = Reusable;
    }

    public record HarvestInteraction(
        ItemActionType ActionType,
        InteractionType Type,
        string HintKey,
        string Tag)
        : Interaction(Type, HintKey, Tag)
    {
        public ItemActionType ActionType { get; } = ActionType;
    }

    public class InteractionDefinition
    {
        public List<Interaction> Interactions { get; } = new();
        public InteractionType[] Types => Interactions.Select(x => x.Type).Distinct().ToArray();
        public string[] Tags => Interactions.Select(interaction => interaction.Tag).ToArray();
    
        public InteractionDefinition(IEnumerable<Interaction> interactions) => Interactions.AddRange(interactions);

        public T? Get<T>() where T : Interaction => Interactions.OfType<T>().FirstOrDefault();

        public void Add(Interaction interaction) => Interactions.Add(interaction);

        public void AddRange(IEnumerable<Interaction> interactions) => Interactions.AddRange(interactions);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var tag in Tags)
                hash.Add(tag);
            return hash.ToHashCode();
        }
    }
}