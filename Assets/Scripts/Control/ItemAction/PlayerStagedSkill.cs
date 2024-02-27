#nullable enable
using System;
using UnityEngine;

namespace CraftSharp.Control
{
    [Serializable]
    public class PlayerAttackStage
    {
        [SerializeField] public AnimationClip? AnimationClip;
        [SerializeField] public float Duration;
        [SerializeField] public float DamageStart;
        [SerializeField] public float DamageEnd;
    }

    [CreateAssetMenu(menuName = "Gameplay/Staged Skill")]
    public class PlayerStagedSkill : PlayerBaseSkill
    {
        [SerializeField] public PlayerAttackStage[] Stages = { };

        [SerializeField] public AnimationClip? DummyAnimationClip;

        public int StageCount => Stages.Length;
    }
}