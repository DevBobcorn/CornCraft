#nullable enable
using KinematicCharacterController;
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

        public delegate void OperationInitAction(PlayerStatus info, KinematicCharacterMotor motor, PlayerController player);

        public delegate void OperationExitAction(PlayerStatus info, KinematicCharacterMotor motor, PlayerController player);

        public delegate void OperationUpdateAction(float interval, PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player);
    }
}