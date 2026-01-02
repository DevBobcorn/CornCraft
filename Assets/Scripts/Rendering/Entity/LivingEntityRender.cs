using CraftSharp.Event;
using UnityEngine;
using UnityEngine.Rendering;

namespace CraftSharp.Rendering
{
    public class LivingEntityRender : EntityRender
    {
        public Transform head;

        /// <summary>
        /// Living Entity head yaw
        /// </summary>
        public readonly TrackedValue<float> HeadYaw = new(0F);
        protected float lastHeadYaw = 0F;
        
        protected double currentElapsedPitchUpdateMilSec = 0;
        protected double currentElapsedHeadYawUpdateMilSec = 0;
        
        public override Transform GetAimingRef()
        {
            return head;
        }

        public override void HandleAimingModeChange(CameraAimingEvent e)
        {
            if (!_visualTransform) return;
            
            MeshRenderer[] renderers = _visualTransform.GetComponentsInChildren<MeshRenderer>(true);
    
            foreach (var r in renderers)
            {
                r.shadowCastingMode = e.Aiming ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            }
        }

        public override void Initialize(EntitySpawnData source, Vector3Int originOffset)
        {
            base.Initialize(source, originOffset);
            
            // Update elapsed time in current tick
            currentElapsedPitchUpdateMilSec += Time.unscaledDeltaTime * 1000;
            currentElapsedHeadYawUpdateMilSec += Time.unscaledDeltaTime * 1000;
            
            lastHeadYaw = HeadYaw.Value = source.HeadYaw;
            
            Pitch.OnValueUpdate += (_, _) =>
            {
                // Update old head pitch and reset update timer
                lastPitch = head!.eulerAngles.x;
                currentElapsedPitchUpdateMilSec = 0.0;
            };
            
            HeadYaw.OnValueUpdate += (_, _) =>
            {
                // Update old head yaw and reset update timer
                lastHeadYaw = head!.eulerAngles.y;
                currentElapsedHeadYawUpdateMilSec = 0.0;
            };
        }

        protected override void UpdateTransform(float tickMilSec, Transform cameraTransform)
        {
            base.UpdateTransform(tickMilSec, cameraTransform);
            
            // Update elapsed time in current tick
            currentElapsedPitchUpdateMilSec += Time.unscaledDeltaTime * 1000;
            currentElapsedHeadYawUpdateMilSec += Time.unscaledDeltaTime * 1000;

            var _visualPitch = Mathf.LerpAngle(lastPitch, Pitch.Value, (float)(currentElapsedPitchUpdateMilSec / movementUpdateInterval));
            var _visualHeadYaw = Mathf.LerpAngle(lastHeadYaw, HeadYaw.Value, (float)(currentElapsedHeadYawUpdateMilSec / movementUpdateInterval));
            
            head!.eulerAngles = new Vector3(_visualPitch, _visualHeadYaw);
            
            // If yaw is too far from head yaw, align the body with head
            if (Mathf.Abs(Mathf.DeltaAngle(Yaw.Value, HeadYaw.Value)) > 75F)
            {
                Yaw.Value = HeadYaw.Value;
            }
        }
    }
}