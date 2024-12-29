#nullable enable
using System;
using System.Collections;
using UnityEngine;
using CraftSharp.Event;

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

        public float Progress { get; set; } = 0F;

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

            float multiplier = 1F;

            if (isBestTool && tier != null)
            {
                multiplier = tier.Speed;

                if (!canHarvest) multiplier = 1F;
                // TODO: Tool toolEfficiency level: mult += efficiencyLevel ^ 2 + 1
            }
            else multiplier = 1F;

            // TODO: if haste or conduitPower: speedMultiplier *= 0.2 * max(hasteLevel, conduitPowerLevel) + 1
            // TODO: if miningFatigue: speedMultiplier *= 0.3 ^ min(miningFatigueLevel, 4)

            if (underwater) multiplier /= 5F; // TODO: If not hasAquaAffinity
            if (!onGround) multiplier /= 5F;

            double damage = multiplier / hardness;

            damage /= canHarvest ? 30D : 100D;

            // Instant breaking
            if (damage > 1D) return 0F;

            float ticks = (float) Math.Ceiling(1F / damage);
            float seconds = ticks / 20F;

            return seconds;
        }

        protected override IEnumerator RunInteraction(BaseCornClient client)
        {
            var clientEntityId = client.GetClientEntityId();
            var targetBlock = client.GetChunkRenderManager().GetBlock(location);

            client.DigBlock(location, direction);
            float elapsedTime = 0F;

            while (elapsedTime < duration)
            {
                if (State == ToolInteractionState.Cancelled)
                {
                    EventManager.Instance.Broadcast(new ToolInteractionEvent(clientEntityId, targetBlock, location, DiggingStatus.Cancelled, 0F));

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

                EventManager.Instance.Broadcast(new ToolInteractionEvent(clientEntityId, targetBlock, location, DiggingStatus.Started, Progress));

                Debug.Log($"{GetHashCode()} Destroy progress: {elapsedTime:0.0} / {duration:0.0}");

                yield return null;
            }

            if (State == ToolInteractionState.Cancelled)
            {
                Debug.Log($"{GetHashCode()} at {location} Digging cancelled");

                EventManager.Instance.Broadcast(new ToolInteractionEvent(clientEntityId, targetBlock, location, DiggingStatus.Cancelled, 0F));

                client.DigBlock(location, direction, DiggingStatus.Cancelled);
                yield break;
            }

            //Debug.Log($"{GetHashCode()} at {location} Digging completed");

            EventManager.Instance.Broadcast(new ToolInteractionEvent(clientEntityId, targetBlock, location, DiggingStatus.Finished, 1F));

            client.DigBlock(location, direction, DiggingStatus.Finished);
            State = ToolInteractionState.Completed;
        }

        public void CancelInteraction()
        {
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