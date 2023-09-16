#nullable enable
using System;
using UnityEngine;

namespace CraftSharp.Control
{
    [Serializable]
    public class PlayerMeleeAttackStage
    {
        [SerializeField] public AnimationClip? AnimationClip;
        [SerializeField] public float Duration;
        [SerializeField] public float DamageStart;
        [SerializeField] public float DamageEnd;
    }

    [CreateAssetMenu(menuName = "CornCraft/PlayerMeleeAttack")]
    public class PlayerMeleeAttack : ScriptableObject
    {
        [SerializeField] public PlayerMeleeAttackStage[] Stages = { };

        [SerializeField] [Range(-5F, -0.1F)] public float IdleTimeout = -0.9F;
        [SerializeField] public AnimationClip? DummyAnimationClip;

        public int StageCount => Stages.Length;
    }
}