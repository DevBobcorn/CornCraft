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
    [CreateAssetMenu(menuName = "Distant Lands/Cozy/FX/Precipitation FX", order = 361)]
    public class PrecipitationFX : FXProfile
    {



        [Range(0, 0.05f)]
        public float rainAmount;
        [Range(0, 0.05f)]
        public float snowAmount;
        public float weight;
        CozyWeather weather;


        public override void PlayEffect()
        {
            if (!VFXMod)
                if (InitializeEffect(null) == false)
                    return;
                    
            if (VFXMod.precipitationManager.isEnabled)
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

            if (VFXMod.precipitationManager.isEnabled)
                weight = 1 * Mathf.Clamp01(transitionTimeModifier.Evaluate(i));
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

            if (!VFX.precipitationManager.isEnabled)
            {

                return false;

            }

            VFX.precipitationManager.precipitationFXes.Add(this);
            weather = VFX.weatherSphere;

            return true;

        }

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PrecipitationFX))]
    [CanEditMultipleObjects]
    public class E_PrecipitationFX : E_FXProfile
    {


        void OnEnable()
        {

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("rainAmount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("snowAmount"));
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

            serializedObject.Update();

            EditorGUI.PropertyField(propPosA, serializedObject.FindProperty("rainAmount"));
            EditorGUI.PropertyField(propPosB, serializedObject.FindProperty("snowAmount"));
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