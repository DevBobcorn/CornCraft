using UnityEngine;

namespace CraftSharp.Control
{
    [CreateAssetMenu(fileName = "Player Ability Config", menuName = "Config/Player Ability Config")]
    public class PlayerAbilityConfig : ScriptableObject
    {
        [Range(0.1F, 3F)] public float ClimbOverMaxDist    = 0.5F;
        public Vector3 ClimbOverExtraOffset = Vector2.zero;

        [Range(0.1F,  10F)] public float SneakSpeed  =   2F;
        [Range(0.1F,  10F)] public float WalkSpeed   =   6F;
        [Range(0.1F,  10F)] public float SprintSpeed = 3.5F;
        [Range(0.1F,  10F)] public float SwimSpeed   =   4F;
        [Range(0.1F,  10F)] public float GlideSpeed  = 3.6F;
        
        public AnimationCurve JumpSpeedCurve = new();
        [Range(10F, 1000F)] public float TurnSpeed = 600F;

        [Range(  1F, 100F)] public float MaxStamina        =  20F;
        [Range(0.1F, 100F)] public float SprintStaminaCost =   3F;
        [Range(0.1F, 100F)] public float GlideStaminaCost  = 0.1F;
        [Range(0.1F, 100F)] public float StaminaRestore    =   1F;
        [Range(  1F, 100F)] public float MaxFallSpeed      =   20F;
    }
}