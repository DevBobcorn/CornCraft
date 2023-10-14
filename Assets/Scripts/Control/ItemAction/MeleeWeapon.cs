#nullable enable
using System.Collections.Generic;
using UnityEngine;

using CraftSharp.Rendering;

namespace CraftSharp.Control
{
    public class MeleeWeapon : PlayerActionItem
    {
        [SerializeField] public TrailRenderer? SlashTrail;
        private readonly List<Collider> slashHits = new();
        private bool slashActive = false;

        public override void StartAction()
        {
            slashHits.Clear();
            slashActive = true;

            if (SlashTrail != null)
            {
                SlashTrail.emitting = true;
            }
        }

        public override void EndAction()
        {
            slashActive = false;

            if (SlashTrail != null)
            {
                SlashTrail.emitting = false;
            }
            
            List<AttackHitInfo> infos = new();

            foreach (var hit in slashHits)
            {
                EntityRender? entityRender;

                if (entityRender = hit.GetComponentInParent<EntityRender>())
                    infos.Add(new(entityRender, hit));
            }

            //return infos;
        }

        void OnTriggerEnter(Collider hit)
        {
            if (!slashActive)
                return;
            
            if (!slashHits.Contains(hit))
                slashHits.Add(hit);
        }
    }
}