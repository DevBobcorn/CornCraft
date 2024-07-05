#nullable enable
using UnityEngine;
using UnityEditor;

namespace CraftSharp.Control
{
    public class AnimationCurveExport : MonoBehaviour
    {
        public AnimationClip? clip;
        public PlayerAbility? ability;

        void Start()
        {
            if (ability != null && clip != null)
            {
#if UNITY_EDITOR
                var curves = AnimationUtility.GetCurveBindings(clip);

                foreach (var binding in curves)
                {
                    //Debug.Log(binding.propertyName);

                    if (binding.propertyName == "m_LocalPosition.x")
                    {
                        ability.Climb2mX = AnimationUtility.GetEditorCurve(clip, binding);
                        Debug.Log("Animation x curve exported");
                    }
                    else if (binding.propertyName == "m_LocalPosition.y")
                    {
                        ability.Climb2mY = AnimationUtility.GetEditorCurve(clip, binding);
                        Debug.Log("Animation y curve exported");
                    }
                    else if (binding.propertyName == "m_LocalPosition.z")
                    {
                        ability.Climb2mZ = AnimationUtility.GetEditorCurve(clip, binding);
                        Debug.Log("Animation z curve exported");
                    }
                    else
                        Debug.LogWarning($"Unused property: {binding.propertyName}");
                }
#else
                Debug.LogError("Trying to export outside the editor!");
#endif
            }
            else
                Debug.LogWarning("Exporting operation not ready!");
        }
    }
}