// Distant Lands 2022.



using System.Collections.Generic;
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif



namespace DistantLands.Cozy.Data
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Distant Lands/Cozy/FX/Wind FX", order = 361)]
    public class WindFX : FXProfile
    {



        [Range(0, 2)]
        public float windAmount;
        [Range(0, 2)]
        public float windSpeed;
        public float weight;
        CozyWeather weather;


        public override void PlayEffect()
        {
            weight = 1;
        }

        public override void PlayEffect(float i)
        {
            
            if (i <= 0.03f)
            {
                StopEffect();
                return;
            }
            weight = Mathf.Clamp01(transitionTimeModifier.Evaluate(i));
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

            if (!VFX.filterManager.isEnabled)
            {

                return false;

            }

            if (VFX) { 
                VFX.windManager.windFXes.Add(this);
                weather = VFX.weatherSphere;
            }

            return true;

        }


    }

#if UNITY_EDITOR
    [CustomEditor(typeof(WindFX))]
    [CanEditMultipleObjects]
    public class E_WindFX : E_FXProfile
    {


        void OnEnable()
        {

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("windAmount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("windSpeed"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("transitionTimeModifier"));

            serializedObject.ApplyModifiedProperties();

        }
        public override void RenderInWindow(Rect pos)
        {

            float space = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var propPosA = new Rect(pos.x, pos.y + space, pos.width, EditorGUIUtility.singleLineHeight);
            var propPosB = new Rect(pos.x, pos.y + space * 2, pos.width, EditorGUIUtility.singleLineHeight);
            var propPosC = new Rect(pos.x, pos.y + space * 3, pos.width, EditorGUIUtility.singleLineHeight);
            var propPosD = new Rect(pos.x, pos.y + space * 4, pos.width, EditorGUIUtility.singleLineHeight);
            var propPosE = new Rect(pos.x, pos.y + space * 5, pos.width, EditorGUIUtility.singleLineHeight);
            var propPosF = new Rect(pos.x, pos.y + space * 6, pos.width, EditorGUIUtility.singleLineHeight);

            serializedObject.Update();

            EditorGUI.PropertyField(propPosA, serializedObject.FindProperty("windAmount"));
            EditorGUI.PropertyField(propPosB, serializedObject.FindProperty("windSpeed"));

            EditorGUI.PropertyField(propPosC, serializedObject.FindProperty("transitionTimeModifier"));
            
            serializedObject.ApplyModifiedProperties();
        }

        public override float GetLineHeight()
        {

            return 3;
            
        }

    }
#endif
}