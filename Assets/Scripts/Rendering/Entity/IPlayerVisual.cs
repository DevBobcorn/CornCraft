#nullable enable
using UnityEngine;
using MinecraftClient.Control;
using MinecraftClient.Mapping;

namespace MinecraftClient.Rendering
{
    public interface IPlayerVisual
    {
        public abstract void UpdateEntity(Entity entity);

        public abstract void UpdateVelocity(Vector3 velocity);

        public virtual void UpdateVisual(float tickMilSec) { }

        public virtual void UpdateStateMachine(PlayerStatus info) { }

        public virtual void CrossFadeState(string stateName, float time, int layer, float timeOffset) { }
    }
}