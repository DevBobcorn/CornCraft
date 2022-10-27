#nullable enable
using UnityEngine;
using MinecraftClient.Mapping;

namespace MinecraftClient.Control
{
    public class PlayerStatus
    {
        // Player state
        public bool InWater  = false;
        public bool Grounded = false;
        public bool Climbing = false;
        public bool Moving   = false;

        public bool Attacking  = false;
        public bool Spectating = false;
        public bool WalkMode   = false;

        // Reference values
        public float CenterDownDist = 0F;
        public float FrontDownDist  = 0F;

        public float UserInputYaw     = 0F;
        public float TargetVisualYaw  = 0F;
        public float CurrentVisualYaw = 0F;

        // Block selection data
        public Location? TargetBlockPos = null;

        public override string ToString() => 
            $"InWater:\t{InWater}\nGrounded:\t{Grounded}\nClimbing:\t{Climbing}\nMoving:\t{Moving} {CenterDownDist} {FrontDownDist}";

    }
}