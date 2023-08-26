#nullable enable
using System.Collections.Generic;

namespace CraftSharp.Control
{
    public interface PlayerStates
    {
        // Grounded states
        public static readonly IPlayerState IDLE = new IdleState();
        public static readonly IPlayerState MOVE = new MoveState();

        // Attack states
        public static readonly IPlayerState MELEE = new MeleeState();

        // On wall states
        public static readonly IPlayerState CLIMB = new ClimbState();

        // Airborne states
        public static readonly IPlayerState FALL  = new FallState();
        public static readonly IPlayerState GLIDE = new GlideState();
        // In water states
        public static readonly IPlayerState SWIM  = new SwimState();
        public static readonly IPlayerState TREAD = new TreadState();

        // Special states
        public static readonly IPlayerState SPECTATE = new SpectateState();

        public static readonly List<IPlayerState> STATES = GetPlayerStates();
        
        private static List<IPlayerState> GetPlayerStates()
        {
            List<IPlayerState> list = new()
            {
                // Grounded
                IDLE,
                MOVE,
                // Attack
                MELEE,
                // On wall
                CLIMB,
                // Airborne
                FALL,
                GLIDE,
                // In water
                SWIM,
                TREAD,
                // Special
                SPECTATE
            };

            return list;
        }
    }
}