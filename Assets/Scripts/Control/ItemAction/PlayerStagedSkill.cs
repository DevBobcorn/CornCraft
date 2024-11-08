#nullable enable
using System;
using CraftSharp.Rendering;
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

        [SerializeField] public GameObject? VisualFXPrefab;
    }

    [CreateAssetMenu(menuName = "Gameplay/Staged Skill")]
    public class PlayerStagedSkill : PlayerBaseSkill
    {
        [SerializeField] public PlayerAttackStage[] Stages = { };

        [SerializeField] public AnimationClip? DummyAnimationClip;
        [SerializeField] public ItemActionType SkillItemActionType = ItemActionType.Sword;
        [SerializeField] public PlayerSkillItemConfig? SkillItemConf;

        public int StageCount => Stages.Length;
    }
}