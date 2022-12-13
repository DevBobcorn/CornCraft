#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class ForceMoveState : IPlayerState
    {
        public readonly string Name;
        public readonly ForceMoveOperation[] Operations;
        public Vector3 Origin => Operations[0].Origin;

        private int currentOperationIndex = 0;

        private ForceMoveOperation? currentOperation;

        private float currentTime = 0F;

        public ForceMoveState(string name, ForceMoveOperation[] op)
        {
            Name = name;
            Operations = op;
        }

        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody, PlayerController player)
        {
            if (currentOperation is null)
                return;

            currentTime = Mathf.Max(currentTime - interval, 0F);

            if (currentTime <= 0F)
            {
                // Finish current operation
                FinishOperation(info, ability, rigidbody, player);

                currentOperationIndex++;

                if (currentOperationIndex < Operations.Length)
                {
                    // Start next operation in sequence
                    StartOperation(info, ability, rigidbody, player);
                }
            }
            else
            {
                if (!currentOperation.UseRootMotionClipAsDisplacement)
                {
                    var moveProgress = currentTime / currentOperation.TimeTotal;
                    var curPosition = Vector3.Lerp(currentOperation.Destination!.Value, currentOperation.Origin, moveProgress);

                    rigidbody.transform.position = curPosition;
                }
                else
                {
                    // Sample animation 
                    var curPosition = currentOperation.SampleTargetAt(currentOperation.TimeTotal - currentTime);
                    
                    rigidbody.transform.position = curPosition;
                }

                // Call operation update
                currentOperation.OperationUpdate?.Invoke(interval, inputData, info, ability, rigidbody, player);
            }
        }

        private void StartOperation(PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody, PlayerController player)
        {
            // Update current operation
            currentOperation = Operations[currentOperationIndex];

            if (currentOperation is not null)
            {
                // Invoke operation init if present
                currentOperation.OperationInit?.Invoke(info, ability, rigidbody, player);
                
                currentTime = currentOperation.TimeTotal;

                // TODO Check validity
                info.PlayingForcedAnimation = currentOperation.UseRootMotionClipAsDisplacement;
            }
        }

        private void FinishOperation(PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody, PlayerController player)
        {
            if (currentOperation is not null)
            {
                currentOperation.OperationExit?.Invoke(info, ability, rigidbody, player);

                if (!currentOperation.UseRootMotionClipAsDisplacement)
                {
                    // Perform last move with rigidbody.MovePosition()
                    rigidbody!.MovePosition(currentOperation.Destination!.Value);
                }

                info.PlayingForcedAnimation = false;
            }
        }

        // This is not used, use PlayerController.StartForceMoveOperation() to enter this state
        public bool ShouldEnter(PlayerStatus info) => false;

        public bool ShouldExit(PlayerStatus info) => currentOperationIndex >= Operations.Length && currentTime <= 0F;

        public void OnEnter(PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody, PlayerController player)
        {
            if (Operations.Length > currentOperationIndex)
                StartOperation(info, ability, rigidbody, player);
            
            //info.PlayingForcedAnimation = true;
        }

        public void OnExit(PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody, PlayerController player)
        {
            //info.PlayingForcedAnimation = false;

        }

        public override string ToString() => $"ForceMove [{Name}] {currentOperationIndex + 1}/{Operations.Length} ({currentTime:0.00}/{currentOperation?.TimeTotal:0.00})";
    }
}