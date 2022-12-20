// Distant Lands 2022.



using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy.Data
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Distant Lands/Cozy/FX/Thunder FX", order = 361)]
    public class ThunderFX : FXProfile
    {

        public Vector2 timeBetweenStrikes;
        public float weight;
        CozyThunderManager thunderManager;

        public override void PlayEffect()
        {

            if (!VFXMod)
                if (InitializeEffect(null) == false)
                    return;

            if (VFXMod.thunderManager.isEnabled)
                weight = 1;
            else
                weight = 0;


        }
        public override void PlayEffect(float i)
        {

            if (!VFXMod)
                if (InitializeEffect(null) == false)
                    return;
                
                
            if (i <= 0.03f)
            {
                StopEffect();
                return;
            }
                    
            if (VFXMod.thunderManager.isEnabled)
                weight = i;
            else
                weight = 0;

        }

        public override void StopEffect()
        {

            weight = 0;

        }


        public override bool InitializeEffect(VFXModule VFX)
        {
            if (VFX == null)
                VFX = CozyWeather.instance.VFX;
                
            VFXMod = VFX;

            if (!VFX.thunderManager.isEnabled)
            {

                return false;

            }

            thunderManager = VFX.thunderManager;
            thunderManager.thunderFX.Add(this);

            return true;

        }

    }


#if UNITY_EDITOR
    [CustomEditor(typeof(ThunderFX))]
    [CanEditMultipleObjects]
    public class E_ThunderFX : E_FXProfile
    {


        void OnEnable()
        {

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("timeBetweenStrikes"), new GUIContent("Time Between Strikes"));

            serializedObject.ApplyModifiedProperties();

        }

        public override void RenderInWindow(Rect pos)
        {

            float space = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var propPosA = new Rect(pos.x, pos.y + space, pos.width, EditorGUIUtility.singleLineHeight);

            serializedObject.Update();

            EditorGUI.PropertyField(propPosA, serializedObject.FindProperty("timeBetweenStrikes"));
            
            serializedObject.ApplyModifiedProperties();
        }

        public override float GetLineHeight()
        {

            return 1;
            
        }

    }
#endif
}