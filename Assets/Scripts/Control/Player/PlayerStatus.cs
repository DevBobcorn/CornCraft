using System.IO;
using UnityEngine;

namespace CraftSharp.Control
{
    public class PlayerStatus
    {
        // Player data
        public GameMode GameMode;
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
        /// Time since last grounded, 0 when player is not in air
        /// </summary>
        public float AirTime  = 0F;

        /// <summary>
        /// Unused for now
        /// </summary>
        public bool Clinging  = false;

        /// <summary>
        /// Whether player is gliding
        /// </summary>
        public bool Gliding   = false;
        
        /// <summary>
        /// Whether player is flying
        /// </summary>
        public bool Flying    = false;

        /// <summary>
        /// Whether player have active horizontal movement input
        /// </summary>
        public bool Moving    = false;

        /// <summary>
        /// Whether player is sprinting
        /// </summary>
        public bool Sprinting = false;
        
        /// <summary>
        /// Whether player is sneaking
        /// </summary>
        public bool Sneaking   = false;

        /// <summary>
        /// Gravity scale. Used only for controlling player entity enablement / disablement
        /// </summary>
        public float GravityScale = 0F;

        /// <summary>
        /// Attacking or digging (which is "attacking" a block)
        /// </summary>
        public bool Attacking = false;
        public readonly AttackStatus AttackStatus = new();

        /// <summary>
        /// Whether player is playing as a spectator
        /// </summary>
        public bool Spectating = false;

        public bool EntityDisabled = false;
        public bool PhysicsDisabled = true;

        // Reference values
        public float GroundDist = 0F;
        public float LiquidDist = 0F;
        public float TargetVisualYaw  = 0F;
        public float MovementInputYaw = 0F;
        public float CurrentVisualYaw = 0F;

        public float YawDeltaAbs => Mathf.Abs(Mathf.DeltaAngle(TargetVisualYaw, CurrentVisualYaw));

        public static Direction GetYawDirection(float yawAngle)
        {
            while (yawAngle >= 360F) yawAngle -= 360F;
            while (yawAngle < 0F) yawAngle += 360F;
            
            return yawAngle switch
            {
                > 315 or <= 45 => Direction.East,
                > 45 and <= 135 => Direction.South,
                > 135 and <= 225 => Direction.West,
                > 225 and <= 315 => Direction.North,
                _ => throw new InvalidDataException($"Invalid yaw angle: {yawAngle}!")
            };
        }
        
        public override string ToString()
        {
            var moveInfo = $"Moving: {Moving} GroundDist: {GroundDist:0.000} LiquidDist: {LiquidDist:0.000}\nSprinting: {Sprinting}\nSneaking: {Sneaking}\nYaw: {TargetVisualYaw:0.00} ({GetYawDirection(TargetVisualYaw)})";
            var envInfo = $"\nGravity Scale: {GravityScale:0.00}\nAir Time: {AirTime:0.00}";
            var atkInfo = Attacking ? AttackStatus.ToString() : string.Empty;
            
            return $"Flying: {Flying}\nGliding: {Gliding}\nInLiquid: {InLiquid}\nFloating: {Floating}\nGrounded: {Grounded}\nClinging: {Clinging}\n{envInfo}\n{moveInfo}\n{atkInfo}";
        }
    }
}