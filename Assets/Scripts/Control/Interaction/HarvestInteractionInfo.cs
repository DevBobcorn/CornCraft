#nullable enable
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

using CraftSharp.Event;

namespace CraftSharp.Control
{
    public enum HarvestInteractionState
    {
        InProgress,
        Cancelled,
        Completed,
        Paused
    }

    public abstract class HarvestInteractionInfo : BlockInteractionInfo
    {
        protected readonly Direction direction;
        protected readonly HarvestInteraction? definition;

        private float progress = 0F;

        public float Progress
        {
            get => progress;
            set
            {
                progress = Mathf.Clamp01(value);
                ParamTexts = new string[] { $"{progress * 100:0}%" };
            }
        }

        public override string HintKey => definition?.HintKey ?? string.Empty;

        public BlockLoc Location => blockLoc;

        public HarvestInteractionState State { get; protected set; } = HarvestInteractionState.InProgress;

        protected HarvestInteractionInfo(int id, Block block, BlockLoc loc, Direction dir, HarvestInteraction? def) : base(id, block, loc)
        {
            direction = dir;
            definition = def;

            ParamTexts = new string[] { string.Empty };
        }
    }

    public sealed class LocalHarvestInteractionInfo : HarvestInteractionInfo
    {
        private readonly float duration;

        public LocalHarvestInteractionInfo(int id, Block block, BlockLoc loc, Direction dir, Item? tool, float hardness,
            bool floating, bool grounded, HarvestInteraction? def) : base(id, block, loc, dir, def)
        {
            duration = CalculateDiggingTime(tool, hardness, floating, grounded);
        }

        private float CalculateDiggingTime(Item? item, float hardness, bool underwater, bool onGround)
        {
            if (hardness <= 0F) // Bedrock or something, takes forever to break
            {
                return float.PositiveInfinity;
            }

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
            // Takes 30 to 40 milsecs to send, don't wait for it
            Task.Run(() => client.DigBlock(blockLoc, direction, DiggingStatus.Started));

            float elapsedTime = 0F;

            while (elapsedTime < duration)
            {
                if (State == HarvestInteractionState.Cancelled)
                {
                    EventManager.Instance.Broadcast(new HarvestInteractionUpdateEvent(Id, block, blockLoc, DiggingStatus.Cancelled, 0F));

                    // Takes 30 to 40 milsecs to send, don't wait for it
                    Task.Run(() => client.DigBlock(blockLoc, direction, DiggingStatus.Cancelled));

                    yield break;
                }

                if (State == HarvestInteractionState.Paused)
                {
                    yield return null;
                    continue;
                }

                elapsedTime += Time.deltaTime;
                Progress = elapsedTime / duration;

                EventManager.Instance.Broadcast(new HarvestInteractionUpdateEvent(Id, block, blockLoc, DiggingStatus.Started, Progress));

                //Debug.Log($"{GetHashCode()} Destroy progress: {elapsedTime:0.0} / {duration:0.0}");

                yield return null;
            }

            if (State == HarvestInteractionState.Cancelled)
            {
                EventManager.Instance.Broadcast(new HarvestInteractionUpdateEvent(Id, block, blockLoc, DiggingStatus.Cancelled, 0F));

                // Takes 30 to 40 milsecs to send, don't wait for it
                Task.Run(() => client.DigBlock(blockLoc, direction, DiggingStatus.Cancelled));

                yield break;
            }

            //Debug.Log($"{GetHashCode()} Complete at {location}");

            EventManager.Instance.Broadcast(new HarvestInteractionUpdateEvent(Id, block, blockLoc, DiggingStatus.Finished, 1F));

            EventManager.Instance.Broadcast(new ParticlesEvent(CoordConvert.MC2Unity(client.WorldOriginOffset, blockLoc.ToCenterLocation()),
                    ParticleTypePalette.INSTANCE.GetNumIdById(InteractionUpdater.BLOCK_PARTICLE_ID), new BlockParticleExtraData(block.StateId), 16));

            // Takes 30 to 40 milsecs to send, don't wait for it
            Task.Run(() => client.DigBlock(blockLoc, direction, DiggingStatus.Finished));
            State = HarvestInteractionState.Completed;
        }

        public void CancelInteraction()
        {
            State = HarvestInteractionState.Cancelled;
        }

        public void PauseInteraction()
        {
            if (State == HarvestInteractionState.InProgress)
                State = HarvestInteractionState.Paused;
        }

        public void ResumeInteraction()
        {
            if (State == HarvestInteractionState.Paused)
                State = HarvestInteractionState.InProgress;
        }
    }
}