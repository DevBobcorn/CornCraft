#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public class PreInitState : IPlayerState
    {

        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, PlayerController player)
        {
            // Do nothing
        }

        public bool ShouldEnter(PlayerActions inputData, PlayerStatus info)
        {
            return false;
        }

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info)
        {
            return false;
        }
        
        public override string ToString() => "PreInit";
    }
}