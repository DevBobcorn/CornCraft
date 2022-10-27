#nullable enable
using System.Collections.Generic;

namespace MinecraftClient.Control
{
    public interface PlayerStates
    {
        // Grounded states
        public static readonly IPlayerState IDLE = new IdleState();
        public static readonly IPlayerState MOVE = new MoveState();

        // Climbing states
        // TODO

        // Airborne states
        public static readonly IPlayerState FALL = new FallState();

        // In water states
        // TODO

        // Special states
        public static readonly IPlayerState SPECTATE = new SpectateState();

        public static readonly List<IPlayerState> STATES = GetPlayerStates();
        
        private static List<IPlayerState> GetPlayerStates()
        {
            List<IPlayerState> list = new();

            list.Add(IDLE);
            list.Add(MOVE);

            list.Add(FALL);

            list.Add(SPECTATE);

            return list;
        }

    }
}