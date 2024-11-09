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
        private readonly ToolInteraction? defination;

        private readonly float duration;

        public ToolInteractionState State { get; private set; } = ToolInteractionState.InProgress;

        public ToolInteractionInfo(int id, Item? tool, Block target, BlockLoc loc, Direction dir,
            bool floating, bool grounded, ToolInteraction? def)
        {
            Id = id;
            block = target; 
            location = loc;
            direction = dir;
            defination = def;
            duration = CalculateDiggingTime(tool, floating, grounded);
        }

        public override string GetHintKey()
        {
            return defination?.HintKey ?? string.Empty;
        }

        public override string[] GetParamTexts()
        {
            return Array.Empty<string>();
        }

        private float CalculateDiggingTime(Item? item, bool underwater, bool onGround)
        {
            bool isBestTool = item?.ActionType is not null && defination?.ActionType == item.ActionType;

            ItemTier? tier = null;

            bool canHarvest = defination?.ActionType == ItemActionType.None && item?.TierType is null || // Use hand case
                              item is { TierType: not null } &&
                              ItemTier.Tiers.TryGetValue(item.TierType.Value, out tier); // Use tool case

            float multiplier = 1.0f;

            if (isBestTool && tier != null)
            {
                multiplier = tier.Speed;

                if (!canHarvest) multiplier = 1.0f;
                // TODO: Tool toolEfficiency level: mult += efficiencyLevel ^ 2 + 1
            }
            else multiplier = 1.0f;

            // TODO: if haste or conduitPower: speedMultiplier *= 0.2 * max(hasteLevel, conduitPowerLevel) + 1
            // TODO: if miningFatigue: speedMultiplier *= 0.3 ^ min(miningFatigueLevel, 4)

            if (underwater) multiplier /= 5.0f; // TODO: If not hasAquaAffinity
            if (!onGround) multiplier /= 5.0f;

            double damage = multiplier / block.State.Hardness;

            damage /= canHarvest ? 30.0 : 100.0;

            // Instant breaking
            if (damage > 1.0) return 0.0f;

            float ticks = (float) Math.Ceiling(1.0f / damage);
            float seconds = ticks / 20.0f;

            return seconds;
        }

        public override IEnumerator RunInteraction(BaseCornClient client)
        {
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