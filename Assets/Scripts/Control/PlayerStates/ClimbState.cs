#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public class ClimbState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            
        }

        public bool ShouldEnter(PlayerUserInputData inputData, PlayerStatus info)
        {
            // State only available via direct transition
            return false;
        }

        public bool ShouldExit(PlayerUserInputData inputData, PlayerStatus info)
        {
            if (info.Spectating || info.Grounded || info.InLiquid) // Exit when player is grounded
            {
                info.OnWall = false;
                return true;
            }

            return false;
        }

        public override string ToString() => "Climb";
    }
}