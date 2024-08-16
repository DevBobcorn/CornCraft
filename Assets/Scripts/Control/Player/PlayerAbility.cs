#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    [CreateAssetMenu(fileName = "Player Ability", menuName = "CornCraft/Player Ability")]
    public class PlayerAbility : ScriptableObject
    {
        // Force move animation state names
        public static readonly string CLIMB_1M = "Climb1m";
        [Range(0.1F, 3F)] public float ClimbOverMaxDist    = 0.5F;
        [Range(0F, 1F)] public float ClimbOverExtraForward = 0.2F;
        [Range(0F, 1F)] public float ClimbOverExtraUpward  = 0.2F;
        [Range(0.1F, 5F)] public float ClimbOverTotalTime  = 1.1F;
        [Range(0.1F, 5F)] public float ClimbOverMoveTime   = 0.3F;
        [Range(0.1F, 5F)] public float ClimbOverCheckExit  = 0.2F;

        // Skill animation state name
        public static readonly string SKILL      = "Skill";

        [Range(0.1F,  4F)] public float WalkSpeed   =   2F;
        [Range(0.1F, 10F)] public float RunSpeed    =   6F;
        [Range(0.1F, 100F)] public float AccSpeed   =  10F;
        [Range(0.1F, 100F)] public float DecSpeed   =  10F;
        [Range(0.1F, 10F)] public float SwimSpeed   =   4F;
        [Range(0.1F, 10F)] public float SprintSpeed = 3.5F;
        [Range(0.1F, 10F)] public float GlideSpeed  = 3.6F;
        public AnimationCurve JumpSpeedCurve = new();
        [Range( 10F, 1000F)] public float TurnSpeed = 600F;

        [Range(  1F, 100F)] public float MaxStamina        =  20F;
        [Range(0.1F, 100F)] public float SprintStaminaCost =   3F;
        [Range(0.1F, 100F)] public float SwimStaminaCost   = 0.5F;
        [Range(0.1F, 100F)] public float GlideStaminaCost  = 0.1F;
        [Range(0.1F, 100F)] public float StaminaRestore    =   1F;

        [Range(1F, 100F)] public float MaxFallSpeed         =  20F;
        [Range(1F, 100F)] public float MaxGlideFallSpeed    =   1F;
        [Range( 0.1F,    1F)] public float LiquidMoveMultiplier = 0.85F;
    }
}