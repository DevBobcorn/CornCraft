#nullable enable
using System;
using System.Collections;
using UnityEngine;

namespace CraftSharp.Control
{
    public enum ToolInteractionState
    {
        InProgress,
        Cancelled,
        Completed,
        Paused
    }

    public class ToolInteractionInfo : InteractionInfo
    {
        private readonly Block block;
        private readonly BlockLoc location;
        private readonly Direction direction;
        private readonly ToolInteractionDefinition? definition;

        private float duration;

        public ToolInteractionState State { get; private set; } = ToolInteractionState.InProgress;

        public ToolInteractionInfo(int id, Item? tool, Block target, BlockLoc loc, Direction dir, bool floating, bool grounded, ToolInteractionDefinition? def)
        {
            Id = id;
            block = target; 
            location = loc;
            direction = dir;
            definition = def;
            duration = CalculateDiggingTime(tool, block, floating, grounded);
        }

        public override string GetHintKey()
        {
            return string.Empty;
        }

        public override string[] GetParamTexts()
        {
            return Array.Empty<string>();
        }

        private float CalculateDiggingTime(Item? item, Block block, bool underwater, bool onGround)
        {
            var isBestTool =
                item != null &&
                InteractionManager.INSTANCE.ToolInteractionTable.TryGetValue(block.StateId, out var tool) &&
                tool.Type == item.ActionType;

            ItemTier? tier = null;

            // TODO: I don't known is hand break belongs to harvest?
            var canHarvest =
                item is { TierType: not null } &&
                ItemTier.Tiers.TryGetValue(item.TierType.Value, out tier);

            float mult = 1.0f;

            if (isBestTool && tier != null)
            {
                mult = tier.Speed;

                if (!canHarvest) mult = 1.0f;
            }
            else mult = 1.0f;

            if (underwater) mult /= 5.0f;
            if (!onGround) mult /= 5.0f;

            double damage = mult / block.State.Hardness;

            damage /= canHarvest ? 30.0 : 100.0;

            // Instant breaking
            if (damage > 1.0) return 0.0f;

            float ticks = (float) Math.Ceiling(1.0f / damage);
            float seconds = ticks / 20.0f;

            return seconds;
        }

        public override IEnumerator RunInteraction(BaseCornClient client)
        {
            switch (definition?.Type)
            {
                case ItemActionType.Axe:
                case ItemActionType.Hoe:
                case ItemActionType.Pickaxe:
                case ItemActionType.Shovel:
                    client.DigBlock(location, direction);

                    float elapsedTime = 0.0f;
                    while (elapsedTime < duration)
                    {
                        if (State == ToolInteractionState.Cancelled)
                        {
                            client.DigBlock(location, direction, BaseCornClient.DiggingStatus.Cancelled);
                            yield break;
                        }

                        if (State == ToolInteractionState.Paused)
                        {
                            yield return null;
                            continue;
                        }

                        elapsedTime += Time.deltaTime;
                        yield return null;
                    }

                    client.DigBlock(location, direction, BaseCornClient.DiggingStatus.Finished);
                    State = ToolInteractionState.Completed;
                    break;
                default:
                    yield break;
            }
        }

        public void CancelInteraction()
        {
            if (State == ToolInteractionState.InProgress)
                State = ToolInteractionState.Cancelled;
        }

        public void PauseInteraction()
        {
            if (State == ToolInteractionState.InProgress)
                State = ToolInteractionState.Paused;
        }

        public void ResumeInteraction()
        {
            if (State == ToolInteractionState.Paused)
                State = ToolInteractionState.InProgress;
        }
    }
}