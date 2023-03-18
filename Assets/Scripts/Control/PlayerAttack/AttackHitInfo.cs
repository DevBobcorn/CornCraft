#nullable enable
using UnityEngine;

using MinecraftClient.Rendering;

namespace MinecraftClient.Control
{
    public record AttackHitInfo
    {
        public EntityRender EntityRender;
        public Collider HitCollider;

        public AttackHitInfo(EntityRender entityRender, Collider hitCollider)
        {
            EntityRender = entityRender;
            HitCollider = hitCollider;
        }

    }
}