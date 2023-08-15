#nullable enable
using UnityEngine;

using CraftSharp.Rendering;

namespace CraftSharp.Control
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