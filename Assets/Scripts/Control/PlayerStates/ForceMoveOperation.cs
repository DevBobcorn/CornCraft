#nullable enable
using KinematicCharacterController;
using UnityEngine;

namespace CraftSharp.Control
{
    public enum ForceMoveDisplacementType
    {
        FixedDisplacement,
        RootMotionDisplacement
    }

    public class ForceMoveOperation
    {
        public readonly ForceMoveDisplacementType DisplacementType;
        public readonly Vector3 Origin;

        // Offset using curves
        public readonly Quaternion? RootMotionRotation;
        public readonly float RootMotionPlaybackSpeed = 1F;
        public readonly float RootMotionTimeOffset = 0F;

        // Offset using linear lerp
        public readonly Vector3? Destination;
        public readonly float TimeTotal;

        // Operation actions
        public readonly OperationInitAction?   OperationInit;
        public readonly OperationExitAction?   OperationExit;
        public readonly OperationUpdateAction? OperationUpdate;

        public ForceMoveOperation(Vector3 origin, Quaternion rotation, float timeOffset, float time, OperationInitAction? init = null, OperationExitAction? exit = null, OperationUpdateAction? update = null)
        {
            DisplacementType = ForceMoveDisplacementType.RootMotionDisplacement;

            Origin = origin;

            RootMotionRotation = rotation;
            RootMotionTimeOffset = timeOffset;

            TimeTotal = time;

            OperationInit = init;
            OperationExit = exit;
            OperationUpdate = update;
        }

        public ForceMoveOperation(Vector3 origin, Vector3 dest, float time, OperationInitAction? init = null, OperationExitAction? exit = null, OperationUpdateAction? update = null)
        {
            DisplacementType = ForceMoveDisplacementType.FixedDisplacement;

            Origin = origin;
            Destination = dest;
            TimeTotal = time;

            OperationInit = init;
            OperationExit = exit;
            OperationUpdate = update;
        }

        public delegate void OperationInitAction(PlayerStatus info, KinematicCharacterMotor motor, PlayerController player);

        public delegate void OperationExitAction(PlayerStatus info, KinematicCharacterMotor motor, PlayerController player);

        public delegate void OperationUpdateAction(float interval, PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player);
    }
}