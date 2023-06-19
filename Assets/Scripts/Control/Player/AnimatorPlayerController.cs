#nullable enable
using UnityEngine;

using MinecraftClient.Mapping;
using MinecraftClient.Rendering;

namespace MinecraftClient.Control
{
    [RequireComponent(typeof (Rigidbody), typeof (EntityRender), typeof (PlayerStatusUpdater))]
    public class AnimatorPlayerController : PlayerController
    {
        private PlayerEntityRiggedRender? playerRender;

        public override void InitializePlayer(Entity clientEntity, GameMode initGameMode)
        {
            base.InitializePlayer(clientEntity, initGameMode);

            // Initialize player render
            playerRender = GetComponent<PlayerEntityRiggedRender>();
            playerRender.Initialize(clientEntity.Type, clientEntity);
        }

        public override void CrossFadeState(string stateName, float time = 0.2F, int layer = 0, float timeOffset = 0F)
        {
            playerRender!.CrossFadeState(stateName, time, layer, timeOffset);
        }

        public override void RandomizeMirroredFlag()
        {
            var mirrored = Time.frameCount % 2 == 0;
            playerRender!.SetMirroredFlag(mirrored);
            //Debug.Log($"Animation mirrored: {mirrored}");
        }

        void Update() => LogicalUpdate(Time.deltaTime);

        void FixedUpdate() => PhysicalUpdate(Time.fixedDeltaTime);

        protected override void LogicalUpdate(float interval)
        {
            PreLogicalUpdate(interval);
            
            if (playerRender != null)
            {
                // Update player render state machine
                playerRender.UpdateStateMachine(Status!);
                // Update player render velocity
                playerRender.SetVisualMovementVelocity(playerRigidbody!.velocity);
                // Update render
                playerRender.UpdateAnimation(client!.GetTickMilSec());
            }

            PostLogicalUpdate();
        }
    }
}
