#nullable enable
using System.Collections.Generic;

namespace CraftSharp.Control
{
    public static class PlayerStates
    {
        // Used before player init
        public static readonly IPlayerState PRE_INIT    = new GroundedState();

        // Grounded states
        public static readonly IPlayerState GROUNDED    = new GroundedState();

        // Attack states
        public static readonly IPlayerState MELEE       = new MeleeState();
        public static readonly IPlayerState RANGED_AIM  = new RangedAimState();

        // Block interaction states
        public static readonly IPlayerState DIGGING_AIM = new DiggingAimState();

        // Clinging states
        public static readonly IPlayerState CLINGING    = new ClingingState();

        // Airborne states
        public static readonly IPlayerState AIRBORNE    = new AirborneState();
        // Floating states
        public static readonly IPlayerState FLOATING    = new FloatingState();

        // Special states
        public static readonly IPlayerState SPECTATE    = new SpectateState();

        public static readonly List<IPlayerState> STATES = GetPlayerStates();
        
        private static List<IPlayerState> GetPlayerStates()
        {
            List<IPlayerState> list = new()
            {
                // Pre-init State
                PRE_INIT,

                // Grounded State
                GROUNDED,

                // Attack States (no state machine entry)
                // MELEE,
                // RANGED_AIM,

                // Block interaction states (no state machine entry)
                // DIGGING_AIM,

                // Clinging States (no state machine entry)
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