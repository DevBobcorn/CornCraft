#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    [CreateAssetMenu(fileName = "PlayerMeleeAttack", menuName = "CornCraft/PlayerMeleeAttack")]
    public class PlayerMeleeAttack : ScriptableObject
    {
        [SerializeField] [Range(0.1F, 10F)] public float MaxStageDuration = 10F;
        [SerializeField] [Range(-5F, -0.1F)] public float IdleTimeout = -0.9F;
        [SerializeField] [Range(1, 10)] public int StageCount = 4;

    }
}