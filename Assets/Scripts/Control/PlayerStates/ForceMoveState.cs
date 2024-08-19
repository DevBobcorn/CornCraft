#nullable enable
using KinematicCharacterController;
using UnityEngine;

namespace CraftSharp.Control
{
    public class ForceMoveState : IPlayerState
    {
        public readonly string Name;
        public readonly ForceMoveOperation[] Operations;

        private int currentOperationIndex = 0;

        private ForceMoveOperation? currentOperation;

        private float currentTime = 0F;

        public ForceMoveState(string name, ForceMoveOperation[] op)
        {
            Name = name;
            Operations = op;
        }

        public bool IgnoreCollision()
        {
            return true;
        }

        public void UpdateBeforeMotor(float interval, PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            if (currentOperation is null)
                return;

            currentTime = Mathf.Max(currentTime - interval, 0F);

            if (currentTime <= 0F)
            {
                // Finish current operation
                FinishOperation(info, motor, player);

                currentOperationIndex++;

                if (currentOperationIndex < Operations.Length)
                {
                    // Start next operation in sequence
                    StartOperation(info, motor, player);
                }
            }
            else
            {
                // Call operation update
                var terminate = currentOperation.OperationUpdate?.Invoke(interval, currentTime, inputData, info, motor, player);

                if (terminate ?? false)
                {
                    // Finish current operation
                    FinishOperation(info, motor, player);

                    currentOperationIndex++;
                    currentTime = 0F;

                    if (currentOperationIndex < Operations.Length)
                    {
                        // Start next operation in sequence
                        StartOperation(info, motor, player);
                    }
                }
            }
        }

        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            if (currentOperation is not null)
            {
                // Distribute offset evenly into the movement
                currentVelocity = currentOperation.Offset / currentOperation.Time;
            }
            else
            {
                currentVelocity = Vector3.zero;
            }
        }

        private void StartOperation(PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            // Update current operation
            currentOperation = Operations[currentOperationIndex];

            if (currentOperation is not null)
            {
                // Invoke operation init if present
                currentOperation.OperationInit?.Invoke(info, motor, player);
                currentTime = currentOperation.Time;
            }
            else
            {
                currentTime = 0F;
            }
        }

        private void FinishOperation(PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            currentOperation?.OperationExit?.Invoke(info, motor, player);
        }

        // This is not used, use PlayerController.StartForceMoveOperation() to enter this state
        public bool ShouldEnter(PlayerActions inputData, PlayerStatus info) => false;

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info) =>
                currentOperationIndex >= Operations.Length && currentTime <= 0F;

        public void OnEnter(IPlayerState prevState, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            if (Operations.Length > currentOperationIndex)
                StartOperation(info, motor, player);
            
        }

        public void OnExit(IPlayerState nextState, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            
        }

        public override string ToString() => $"ForceMove [{Name}] {currentOperationIndex + 1}/{Operations.Length} ({currentTime:0.00}/{currentOperation?.Time:0.00})";
    }
}