#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
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

        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            if (currentOperation is null)
                return;

            currentTime = Mathf.Max(currentTime - interval, 0F);

            if (currentTime <= 0F)
            {
                // Finish current operation
                FinishOperation(info, rigidbody, player);

                currentOperationIndex++;

                if (currentOperationIndex < Operations.Length)
                {
                    // Start next operation in sequence
                    StartOperation(info, rigidbody, player);
                }
            }
            else
            {
                switch (currentOperation.DisplacementType)
                {
                    case ForceMoveDisplacementType.FixedDisplacement:
                        var moveProgress = currentTime / currentOperation.TimeTotal;
                        var curPosition1 = Vector3.Lerp(currentOperation.Destination!.Value, currentOperation.Origin, moveProgress);

                        rigidbody.transform.position = curPosition1;
                        break;
                    case ForceMoveDisplacementType.CurvesDisplacement:
                        // Sample animation 
                        var curPosition2 = currentOperation.SampleTargetAt(currentOperation.TimeTotal - currentTime);
                        
                        rigidbody.transform.position = curPosition2;
                        break;
                    case ForceMoveDisplacementType.RootMotionDisplacement:
                        // Do nothing
                        
                        break;
                }

                // Call operation update
                currentOperation.OperationUpdate?.Invoke(interval, inputData, info, rigidbody, player);
            }
        }

        private void StartOperation(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            // Update current operation
            currentOperation = Operations[currentOperationIndex];

            if (currentOperation is not null)
            {
                // Invoke operation init if present
                currentOperation.OperationInit?.Invoke(info, rigidbody, player);
                
                currentTime = currentOperation.TimeTotal;

                // TODO Check validity
                info.PlayingRootMotion = currentOperation.DisplacementType != ForceMoveDisplacementType.FixedDisplacement;
            }
        }

        private void FinishOperation(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            if (currentOperation is not null)
            {
                currentOperation.OperationExit?.Invoke(info, rigidbody, player);

                if (currentOperation.DisplacementType == ForceMoveDisplacementType.FixedDisplacement)
                {
                    // Perform last move with rigidbody.MovePosition()
                    rigidbody!.MovePosition(currentOperation.Destination!.Value);
                }

                info.PlayingRootMotion = false;
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
        public bool ShouldEnter(PlayerUserInputData inputData, PlayerStatus info) => false;

        public bool ShouldExit(PlayerUserInputData inputData, PlayerStatus info) =>
                currentOperationIndex >= Operations.Length && currentTime <= 0F;

        public void OnEnter(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            if (Operations.Length > currentOperationIndex)
                StartOperation(info, rigidbody, player);
            
            //info.PlayingForcedAnimation = true;
        }

        public void OnExit(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            //info.PlayingForcedAnimation = false;

        }

        public override string ToString() => $"ForceMove [{Name}] {currentOperationIndex + 1}/{Operations.Length} ({currentTime:0.00}/{currentOperation?.TimeTotal:0.00})";
    }
}