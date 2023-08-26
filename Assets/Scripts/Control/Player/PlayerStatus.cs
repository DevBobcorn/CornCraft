#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public class PlayerStatus
    {
        // Player data
        public float StaminaLeft = 0F;

        // Player status
        public bool InLiquid  = false;
        public bool Grounded  = false;
        public bool OnWall    = false;
        public bool Gliding   = false;
        public bool Moving    = false;
        public bool Sprinting = false;

        public Vector3 MoveVelocity = Vector3.zero;

        public bool Attacking = false;
        public AttackStatus AttackStatus = new();

        public bool Spectating = false;
        public bool WalkMode   = false;

        public bool EntityDisabled = false;

        public bool PlayingRootMotion = false;

        // Reference values
        public float CenterDownDist = 0F;
        public float FrontDownDist  = 0F;
        public float LiquidDist     = 0F;
        public float GroundSlope    = 0F;
        public float BarrierAngle   = 0F;
        public float BarrierDist    = 0F;

        public float UserInputYaw     = 0F;
        public float TargetVisualYaw  = 0F;
        public float CurrentVisualYaw = 0F;

        public float YawOffset => Mathf.Abs(Mathf.DeltaAngle(TargetVisualYaw, CurrentVisualYaw));

        public override string ToString()
        {
            var moveInfo = $"Moving:\t{Moving}\t{CenterDownDist:0.00}\t{FrontDownDist:0.00}\t{LiquidDist:0.00}\nSprinting:\t{Sprinting}";
            var envInfo = $"Ground Slope:\t{GroundSlope:0.00}\nBarrier Angle:\t{BarrierAngle:0.00}\tDist:\t{BarrierDist:0.00}";
            var atkInfo = Attacking ? AttackStatus.ToString() : string.Empty;
            
            return $"InLiquid:\t{InLiquid}\nGrounded:\t{Grounded}\nOn Wall:\t{OnWall}\n{moveInfo}\n{envInfo}\n{atkInfo}";
        }
    }
}