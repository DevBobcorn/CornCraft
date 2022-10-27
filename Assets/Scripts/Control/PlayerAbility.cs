#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    [CreateAssetMenu(fileName = "PlayerAbility", menuName = "CornCraft/PlayerAbility")]
    public class PlayerAbility : ScriptableObject
    {
        [SerializeField] [Range(0.1F,  4F)] public float WalkSpeed = 1F;
        [SerializeField] [Range(0.1F, 10F)] public float RunSpeed  = 2F;
        [SerializeField] [Range(0.1F, 10F)] public float JumpSpeed = 4F;
        [SerializeField] [Range(10F,  30F)] public float SteerSpeed = 20F;

        [SerializeField] [Range(-20F, -1F)] public float MaxFallSpeed = -10F;
    }
}