#nullable enable
using UnityEngine;
using MinecraftClient.Mapping;

namespace MinecraftClient.Control
{
    public class PlayerStatus
    {
        // Player data
        public float    StaminaLeft = 0F;

        // Player status
        public bool InLiquid  = false;
        public bool Grounded  = false;
        public bool OnWall    = false;
        public bool Moving    = false;
        public bool Sprinting = false;

        public float BrakeTime       = 0F;

        public bool Attacking  = false;
        public bool Spectating = false;
        public bool WalkMode   = false;

        public bool GravityDisabled = false;

        public bool PlayingRootMotion = false;
        public float PositionValidateCooldown = 0F;

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

        // Block selection data
        public Location? TargetBlockPos = null;

        public override string ToString() => 
            $"InLiquid:\t{InLiquid}\nGrounded:\t{Grounded}\nOn Wall:\t{OnWall}\nMoving:\t{Moving}\t{CenterDownDist:0.00}\t{FrontDownDist:0.00}\t{LiquidDist:0.00}\nGrounded:\t{Sprinting}\nGround Slope:\t{GroundSlope:0.00}\nBarrier Angle:\t{BarrierAngle:0.00}\tDist:\t{BarrierDist:0.00}\n\nBrake Time:\t{BrakeTime:0.00}";

    }
}