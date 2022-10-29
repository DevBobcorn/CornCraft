#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    [CreateAssetMenu(fileName = "PlayerAbility", menuName = "CornCraft/PlayerAbility")]
    public class PlayerAbility : ScriptableObject
    {
        [SerializeField] [Range(0.1F,  4F)] public float WalkSpeed  =  1F;
        [SerializeField] [Range(0.1F, 10F)] public float RunSpeed   =  2F;
        [SerializeField] [Range(0.1F, 10F)] public float JumpSpeed  =  4F;
        [SerializeField] [Range(10F,  30F)] public float SteerSpeed = 20F;
        
        [SerializeField] public AnimationCurve AidSpeedCurve      = new();
        [SerializeField] public AnimationCurve WaterAidSpeedCurve = new();

        [SerializeField] [Range(-50F,  -1F)] public float MaxFallSpeed        =  -20F;
        [SerializeField] [Range(0.01F,  8F)] public float MaxWaterMoveSpeed   =  1.8F;
        [SerializeField] [Range(-5F, -0.1F)] public float MaxWaterFallSpeed   = -1.1F;
        [SerializeField] [Range(0.1F,  10F)] public float WaterMoveMultiplier =  0.5F;
    }
}