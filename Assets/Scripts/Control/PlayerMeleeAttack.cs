#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    [CreateAssetMenu(fileName = "PlayerMeleeAttack", menuName = "CornCraft/PlayerMeleeAttack")]
    public class PlayerMeleeAttack : ScriptableObject
    {
        [SerializeField] public float[] StageDurations = { 1.0F, 1.1F, 1.2F, 1.0F };
        [SerializeField] [Range(-5F, -0.1F)] public float IdleTimeout = -0.9F;

        public int StageCount => StageDurations.Length;

    }
}