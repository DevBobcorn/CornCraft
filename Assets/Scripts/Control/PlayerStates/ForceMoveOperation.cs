#nullable enable
using UnityEngine;
using UnityEditor;

namespace MinecraftClient.Control
{
    public class ForceMoveOperation
    {
        public readonly bool UseRootMotionClipAsDisplacement;
        public readonly Vector3 Origin;

        // Offset using curves
        public readonly AnimationClip? RootMotionClip;
        public readonly Quaternion? RootMotionRotation;
        public readonly float RootMotionPlaybackSpeed = 1F;
        public readonly float RootMotionTimeOffset = 0F;

        private readonly AnimationCurve? XCurve, YCurve, ZCurve;
        private readonly Vector3 OriginOffset = Vector3.zero;

        private Vector3 SampleVector3At(float time)
        {
            var t = time * RootMotionPlaybackSpeed + RootMotionTimeOffset;
            return new(XCurve!.Evaluate(t), YCurve!.Evaluate(t), ZCurve!.Evaluate(t));
        }

        public Vector3 SampleTargetAt(float time)
        {
            return Origin + RootMotionRotation!.Value * (SampleVector3At(time) - OriginOffset);
        }

        // Offset using linear lerp
        public readonly Vector3? Destination;
        public readonly float TimeTotal;

        // Operation actions
        public readonly OperationInitAction?   OperationInit;
        public readonly OperationExitAction?   OperationExit;
        public readonly OperationUpdateAction? OperationUpdate;

        public ForceMoveOperation(Vector3 origin, AnimationClip clip, Quaternion rotation, float timeOffset, float time, float playbackSpeed = 1F, OperationInitAction? init = null, OperationExitAction? exit = null, OperationUpdateAction? update = null)
        {
            Origin = origin;
            RootMotionClip = clip;
            RootMotionPlaybackSpeed = playbackSpeed;

            RootMotionRotation = rotation;
            RootMotionTimeOffset = timeOffset;

            var curves = AnimationUtility.GetCurveBindings(clip);

            foreach (var binding in curves)
            {
                //Debug.Log(binding.propertyName);

                if (binding.propertyName == "m_LocalPosition.x")
                    XCurve = AnimationUtility.GetEditorCurve(clip, binding);
                else if (binding.propertyName == "m_LocalPosition.y")
                    YCurve = AnimationUtility.GetEditorCurve(clip, binding);
                else if (binding.propertyName == "m_LocalPosition.z")
                    ZCurve = AnimationUtility.GetEditorCurve(clip, binding);
            }

            OriginOffset = SampleVector3At(0F);

            TimeTotal = time;

            OperationInit = init;
            OperationExit = exit;
            OperationUpdate = update;

            UseRootMotionClipAsDisplacement = true;
        }

        public ForceMoveOperation(Vector3 origin, Vector3 dest, float time, OperationInitAction? init = null, OperationExitAction? exit = null, OperationUpdateAction? update = null)
        {
            Origin = origin;
            Destination = dest;
            TimeTotal = time;

            OperationInit = init;
            OperationExit = exit;
            OperationUpdate = update;

            UseRootMotionClipAsDisplacement = false;
        }

        public delegate void OperationInitAction(PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody, PlayerController player);

        public delegate void OperationExitAction(PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody, PlayerController player);

        public delegate void OperationUpdateAction(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody, PlayerController player);
    }
}