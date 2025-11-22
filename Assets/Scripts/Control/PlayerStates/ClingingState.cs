#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public class ClingingState : IPlayerState
    {
        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, PlayerController player)
        {
            
        }

        public bool ShouldEnter(PlayerActions inputData, PlayerStatus info)
        {
            // State only available via direct transition
            return false;
        }

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info)
        {
            if (info.Spectating || info.Grounded || info.Floating) // Exit when player is grounded
            {
                info.Clinging = false;
                return true;
            }

            return false;
        }

        public override string ToString() => "Clinging";
    }
}