#nullable enable
using System.Collections.Generic;
using UnityEngine;

using MinecraftClient.Rendering;

namespace MinecraftClient.Control
{
    public class MeleeWeapon : MonoBehaviour
    {
        [SerializeField] public Vector3 slotPosition;
        [SerializeField] public Vector3 slotEularAngles;
        [SerializeField] private TrailRenderer? slashTrail;

        private readonly List<Collider> slashHits = new();
        private bool slashActive = false;

        public void StartSlash()
        {
            slashHits.Clear();
            slashActive = true;

            if (slashTrail is not null)
                slashTrail.emitting = true;
        }

        public List<AttackHitInfo> EndSlash()
        {
            slashActive = false;

            if (slashTrail is not null)
                slashTrail.emitting = false;
            
            List<AttackHitInfo> infos = new();

            foreach (var hit in slashHits)
            {
                EntityRender? entityRender;

                if (entityRender = hit.GetComponentInParent<EntityRender>())
                    infos.Add(new(entityRender, hit));
            }

            return infos;
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