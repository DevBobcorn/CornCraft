#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    [RequireComponent(typeof (Rigidbody), typeof (PlayerStatusUpdater))]
    public class SpritePlayerController : PlayerController
    {
        [SerializeField] Transform? spriteRootTransform;

        void Update() => LogicalUpdate(Time.deltaTime);

        void FixedUpdate() => PhysicalUpdate(Time.fixedDeltaTime);

        protected override void LogicalUpdate(float interval)
        {
            PreLogicalUpdate(interval);
            
            // Update sprite facing
            spriteRootTransform!.localEulerAngles = new Vector3(0F,
                    cameraController!.GetViewEularAngles()?.y ?? spriteRootTransform.localEulerAngles.y, 0F);

            // Disable attack
            if (Status!.AttackStatus.AttackCooldown > 1F)
            {
                Status!.AttackStatus.AttackCooldown = 1F;
            }

            PostLogicalUpdate();
        }
    }
}
