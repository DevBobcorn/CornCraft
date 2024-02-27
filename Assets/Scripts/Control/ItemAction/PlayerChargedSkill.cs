#nullable enable
using System;
using UnityEngine;

namespace CraftSharp.Control
{
    [CreateAssetMenu(menuName = "Gameplay/Charged Skill")]
    public class PlayerChargedSkill : PlayerBaseSkill
    {
        [SerializeField] [Range(0.1F, 2F)] public float SetupTime = 0.5F;

        [SerializeField] public AnimationClip? DrawWeapon;
        [SerializeField] public AnimationClip? DummyAnimationClip;
    }
}