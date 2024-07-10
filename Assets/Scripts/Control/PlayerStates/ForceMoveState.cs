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
                switch (currentOperation.DisplacementType)
                {
                    case ForceMoveDisplacementType.FixedDisplacement:
                        var moveProgress = currentTime / currentOperation.TimeTotal;
                        var curPosition1 = Vector3.Lerp(currentOperation.Destination!.Value, currentOperation.Origin, moveProgress);
                        //motor.SetPosition(curPosition1);
                        motor.MoveCharacter(curPosition1);
                        break;
                    case ForceMoveDisplacementType.RootMotionDisplacement:
                        // Do nothing
                        
                        break;
                }

                // Call operation update
                currentOperation.OperationUpdate?.Invoke(interval, inputData, info, motor, player);
            }
        }

        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            if (currentOperation is not null)
            {
                switch (currentOperation.DisplacementType)
                {
                    case ForceMoveDisplacementType.RootMotionDisplacement:
                        currentVelocity = Vector3.zero;
                        break;
                }
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
                
                currentTime = currentOperation.TimeTotal;

                switch (currentOperation.DisplacementType)
                {
                    case ForceMoveDisplacementType.FixedDisplacement:
                        //rigidbody.isKinematic = true;
                        break;
                    case ForceMoveDisplacementType.RootMotionDisplacement:
                        info.PlayingRootMotion = true;
                        player.UseRootMotion = true;
                        break;
                }
            }
        }

        private void FinishOperation(PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            if (currentOperation is not null)
            {
                currentOperation.OperationExit?.Invoke(info, motor, player);

                switch (currentOperation.DisplacementType)
                {
                    case ForceMoveDisplacementType.FixedDisplacement:
                        // Perform last move with rigidbody.MovePosition()
                        motor.MoveCharacter(currentOperation.Destination!.Value);
                        break;
                    case ForceMoveDisplacementType.RootMotionDisplacement:
                        player.UseRootMotion = false;
                        
                        motor.MoveCharacter(motor.transform.position + motor.CharacterUp * (-info.CenterDownDist + 0.01F));
                        if (!info.Moving)
                        {
                            motor.BaseVelocity = Vector3.zero; // Reset velocity
                        }
                        info.PlayingRootMotion = false;
                        
                        break;
                }
            }
        }

        public Vector3 GetFakePlayerOffset()
        {
            if (currentOperation is not null)
            {
                return currentOperation.Origin;
            }

            return Vector3.zero;
        }

        // This is not used, use PlayerController.StartForceMoveOperation() to enter this state
        public bool ShouldEnter(PlayerActions inputData, PlayerStatus info) => false;

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info) =>
                currentOperationIndex >= Operations.Length && currentTime <= 0F;

        public void OnEnter(PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            if (Operations.Length > currentOperationIndex)
                StartOperation(info, motor, player);
            
        }

        public void OnExit(PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            
        }

        public override string ToString() => $"ForceMove [{Name}] {currentOperationIndex + 1}/{Operations.Length} ({currentTime:0.00}/{currentOperation?.TimeTotal:0.00})";
    }
}