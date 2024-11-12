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

    public abstract class ToolInteractionInfo : InteractionInfo
    {
        protected readonly BlockLoc location;
        protected readonly Direction direction;
        protected readonly ToolInteraction? definition;

        public float Progress { get; set; } = 0.0f;

        public override string HintKey => definition?.HintKey ?? string.Empty;

        public BlockLoc Location => location;

        public ToolInteractionState State { get; protected set; } = ToolInteractionState.InProgress;

        protected ToolInteractionInfo(int id, BlockLoc loc, Direction dir, ToolInteraction? def)
        {
            Id = id;
            location = loc;
            direction = dir;
            definition = def;
        }
    }

    public sealed class LocalToolInteractionInfo : ToolInteractionInfo
    {
        private readonly float duration;

        public LocalToolInteractionInfo(int id, BlockLoc loc, Direction dir, Item? tool, float hardness,
            bool floating, bool grounded, ToolInteraction? def)
            : base(id, loc, dir, def)
        {
            Id = id;
            duration = CalculateDiggingTime(tool, hardness, floating, grounded);
        }

        private float CalculateDiggingTime(Item? item, float hardness, bool underwater, bool onGround)
        {
            bool isBestTool = item?.ActionType is not null && definition?.ActionType == item.ActionType;

            ItemTier? tier = null;

            bool canHarvest = definition?.ActionType == ItemActionType.None && item?.TierType is null || // Use hand case
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

            double damage = multiplier / hardness;

            damage /= canHarvest ? 30.0 : 100.0;

            // Instant breaking
            if (damage > 1.0) return 0.0f;

            float ticks = (float) Math.Ceiling(1.0f / damage);
            float seconds = ticks / 20.0f;

            return seconds;
        }

        protected override IEnumerator RunInteraction(BaseCornClient client)
        {
            client.DigBlock(location, direction);
            float elapsedTime = 0.0f;

            while (elapsedTime < duration)
            {
                if (State == ToolInteractionState.Cancelled)
                {
                    client.DigBlock(location, direction, DiggingStatus.Cancelled);
                    yield break;
                }

                if (State == ToolInteractionState.Paused)
                {
                    yield return null;
                    continue;
                }

                elapsedTime += Time.deltaTime;
                Progress = elapsedTime / duration;
                yield return null;
            }

            client.DigBlock(location, direction, DiggingStatus.Finished);
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

    public sealed class GhostToolInteractionInfo : ToolInteractionInfo
    {
        public GhostToolInteractionInfo(int id, BlockLoc loc, Direction dir, ToolInteraction? def)
            : base(id, loc, dir, def) { }

        protected override IEnumerator RunInteraction(BaseCornClient client)
        {
            while (Progress < 1.0f)
            {
                // Wait for server updates on Progress
                yield return null;
            }
        }
    }
}