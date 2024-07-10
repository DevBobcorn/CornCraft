#nullable enable
using System.Collections.Generic;

namespace CraftSharp.Control
{
    public interface PlayerStates
    {
        // Grounded states
        public static readonly IPlayerState GROUNDED   = new GroundedState();

        // Attack states
        public static readonly IPlayerState MELEE      = new MeleeState();
        public static readonly IPlayerState RANGED_AIM = new RangedAimState();

        // On wall states
        public static readonly IPlayerState CLIMB      = new ClingingState();

        // Airborne states
        public static readonly IPlayerState AIRBORNE   = new AirborneState();
        // Floating states
        public static readonly IPlayerState FLOATING  = new FloatingState();

        // Special states
        public static readonly IPlayerState SPECTATE = new SpectateState();

        public static readonly List<IPlayerState> STATES = GetPlayerStates();
        
        private static List<IPlayerState> GetPlayerStates()
        {
            List<IPlayerState> list = new()
            {
                // Grounded State
                GROUNDED,

                // Attack States (no state machine entry)
                // MELEE,
                // RANGED_AIM

                // On wall States (no state machine entry)
                // CLIMB,

                // Airborne State
                AIRBORNE,

                // Floating States
                FLOATING,

                // Special States
                SPECTATE
            };

            return list;
        }
    }
}