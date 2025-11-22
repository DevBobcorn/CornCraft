#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public class ForceMoveOperation
    {
        /// <summary>
        /// An offset to be applied. This can be applied alongside root motion displacement.
        /// </summary>
        public readonly Vector3 Offset;

        /// <summary>
        /// Time of this operation, in seconds.
        /// </summary>
        public readonly float Time;

        // Operation actions
        public readonly OperationInitAction?   OperationInit;
        public readonly OperationExitAction?   OperationExit;
        public readonly OperationUpdateAction? OperationUpdate;

        public ForceMoveOperation(Vector3 offset, float time, OperationInitAction? init = null, OperationExitAction? exit = null, OperationUpdateAction? update = null)
        {
            Offset = offset;
            Time = time;

            OperationInit = init;
            OperationExit = exit;
            OperationUpdate = update;
        }

        /// <summary>
        /// Callback invoked upon operation begins
        /// </summary>
        public delegate void OperationInitAction(PlayerStatus info, PlayerController player);

        /// <summary>
        /// Callback invoked upon operation ends
        /// </summary>
        public delegate void OperationExitAction(PlayerStatus info, PlayerController player);

        /// <summary>
        /// Callback invoked upon operation updates
        /// </summary>
        /// <returns>Whether to terminate the operation</returns>
        public delegate bool OperationUpdateAction(float interval, float curTime, PlayerActions inputData, PlayerStatus info, PlayerController player);
    }
}