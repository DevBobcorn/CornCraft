#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public interface IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerActions inputData, PlayerStatus info, Rigidbody rigidbody, PlayerController player);

        public abstract bool ShouldEnter(PlayerActions inputData, PlayerStatus info);

        public abstract bool ShouldExit(PlayerActions inputData, PlayerStatus info);

        public virtual void OnEnter(PlayerStatus info, Rigidbody rigidbody, PlayerController player) { }

        public virtual void OnExit(PlayerStatus info, Rigidbody rigidbody, PlayerController player) { }
    }
}