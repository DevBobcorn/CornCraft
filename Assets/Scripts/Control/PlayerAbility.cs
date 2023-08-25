#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    [CreateAssetMenu(fileName = "PlayerAbility", menuName = "CornCraft/PlayerAbility")]
    public class PlayerAbility : ScriptableObject
    {
        public enum PlayerColliderType
        {
            Box, Capsule
        }

        [SerializeField] public PlayerColliderType ColliderType;
        [SerializeField] [Range(0.1F, 2F)] public float ColliderRadius = 0.35F;
        [SerializeField] [Range(0.1F, 5F)] public float ColliderHeight =  1.8F;

        // Force move animation states
        public static readonly string CLIMB_1M = "Climb1m";
        public static readonly string CLIMB_2M = "Climb2m";

        [SerializeField] public AnimationCurve Climb1mX = new(), Climb1mY = new(), Climb1mZ = new();
        public AnimationCurve[] Climb1mCurves => new[] { Climb1mX, Climb1mY, Climb1mZ };
        [SerializeField] public AnimationCurve Climb2mX = new(), Climb2mY = new(), Climb2mZ = new();
        public AnimationCurve[] Climb2mCurves => new[] { Climb2mX, Climb2mY, Climb2mZ };

        [SerializeField] [Range(0.1F,  4F)] public float WalkSpeed   =   1F;
        [SerializeField] [Range(0.1F, 10F)] public float RunSpeed    =   2F;
        [SerializeField] [Range(0.1F, 10F)] public float SprintSpeed = 3.5F;
        [SerializeField] public AnimationCurve JumpSpeedCurve = new();
        [SerializeField] [Range( 10F, 30F)] public float SteerSpeed  =  20F;

        [SerializeField] [Range(1F,  100F)] public float MaxStamina        =  20F;
        [SerializeField] [Range(1F,  100F)] public float SprintStaminaCost =   3F;
        [SerializeField] [Range(1F,  100F)] public float SwimStaminaCost   =   1F;
        [SerializeField] [Range(1F,  100F)] public float StaminaRestore    =   1F;

        [SerializeField] [Range(-100F, -0.1F)] public float MaxFallSpeed         =  -20F;
        [SerializeField] [Range(-100F, -0.1F)] public float MaxInLiquidFallSpeed = -1.1F;
        [SerializeField] [Range( 0.1F,   10F)] public float WaterMoveMultiplier  =  0.5F;
    }
}