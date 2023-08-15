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

        // In water states
        public static readonly IPlayerState SWIM  = new SwimState();
        public static readonly IPlayerState TREAD = new TreadState();

        // Special states
        public static readonly IPlayerState SPECTATE = new SpectateState();

        public static readonly List<IPlayerState> STATES = GetPlayerStates();
        
        private static List<IPlayerState> GetPlayerStates()
        {
            List<IPlayerState> list = new();

            list.Add(IDLE);
            list.Add(MOVE);

            list.Add(MELEE);

            list.Add(CLIMB);

            list.Add(FALL);

            list.Add(SWIM);
            list.Add(TREAD);

            list.Add(SPECTATE);

            return list;
        }

    }
}