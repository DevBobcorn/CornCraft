#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    [CreateAssetMenu(menuName = "CornCraft/PlayerMeleeAttack")]
    public class PlayerMeleeAttack : ScriptableObject
    {
        [SerializeField] [Range(0.1F, 10F)] public float MaxStageDuration = 10F;
        [SerializeField] [Range(-5F, -0.1F)] public float IdleTimeout = -0.9F;
        [SerializeField] public AnimationClip? DummyAnimationClip;
        [SerializeField] public AnimationClip[] AnimationClips = { };
        [SerializeField] public float[] StageDurations = { };
        public int StageCount => AnimationClips.Length;
    }
}