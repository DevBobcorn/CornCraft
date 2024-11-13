using UnityEngine;

namespace CraftSharp.Control
{
    public enum LiquidStatus
    {
        None = 0,
        Treading,
        Submerged
    }

    public class PlayerStatus
    {
        // Player data
        public float StaminaLeft = 0F;

        // Player status

        /// <summary>
        /// Whether player currently have contact with liquid
        /// </summary>
        public bool InLiquid  = false;

        /// <summary>
        /// Whether player is floating in liquid
        /// </summary>
        public bool Floating  = false;

        /// <summary>
        /// Whether player is grounded
        /// </summary>
        public bool Grounded  = false;

        /// <summary>
        /// Whether player is grounded (interal use)
        /// </summary>
        //public bool GroundedCheck  = false;

        /// <summary>
        /// Unused for now
        /// </summary>
        public bool Clinging  = false;

        /// <summary>
        /// Whether player is gliding
        /// </summary>
        public bool Gliding   = false;

        /// <summary>
        /// Whether player have active horizontal movement input
        /// </summary>
        public bool Moving    = false;

        /// <summary>
        /// Whether player is sprinting
        /// </summary>
        public bool Sprinting = false;

        /// <summary>
        /// Gravity scale. Used only for controlling player entity enablement / disablement
        /// </summary>
        public float GravityScale = 0F;

        /// <summary>
        /// Attacking or digging (which is "attacking" a block)
        /// </summary>
        public bool Attacking = false;
        public AttackStatus AttackStatus = new();

        public bool Spectating = false;
        public bool WalkMode   = false;

        public bool EntityDisabled = false;
        public bool PhysicsDisabled = true;

        public bool PlayingRootMotion = false;

        // Reference values
        public float CenterDownDist = 0F;
        public float BarrierHeight  = 0F;
        public float LiquidDist     = 0F;
        public float BarrierYawAngle   = 0F;
        public float BarrierDistance   = 0F;
        public float WallDistance      = 0F;

        public float TargetVisualYaw  = 0F;
        public float CurrentVisualYaw = 0F;

        public float YawDeltaAbs => Mathf.Abs(Mathf.DeltaAngle(TargetVisualYaw, CurrentVisualYaw));

        public override string ToString()
        {
            var moveInfo = $"Moving: {Moving} {CenterDownDist:0.000} {LiquidDist:0.000}\nSprinting: {Sprinting}\nYaw: {CurrentVisualYaw:0.00} -> {TargetVisualYaw:0.00} ({YawDeltaAbs:0.00})";
            var envInfo = $"\nBarrier Yaw Angle: {BarrierYawAngle:0.00} Dist:\t{BarrierDistance:0.00} ({WallDistance:0.00}) Hgt:{BarrierHeight:0.00}\nGravity Scale: {GravityScale:0.00}";
            var atkInfo = Attacking ? AttackStatus.ToString() : string.Empty;
            
            return $"InLiquid: {InLiquid}\nFloating: {Floating}\nGrounded: {Grounded}\nClinging: {Clinging}\n{envInfo}\n{moveInfo}\n{atkInfo}";
        }
    }
}