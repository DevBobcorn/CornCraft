using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif



namespace DistantLands.Cozy.Data
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Distant Lands/Cozy/FX/Multi FX", order = 361)]
    public class MultiFXProfile : FXProfile
    {

        public CozyWeather weather;

        [System.Serializable]
        public class MultiFXType
        {
            public FXProfile FX;
            public AnimationCurve intensityCurve;
        }

        [MultiAudio]
        public List<MultiFXType> multiFX;

        public override void PlayEffect()
        {
            if (weather == null)
                weather = CozyWeather.instance;

            foreach (MultiFXType i in multiFX)
            {
                i.FX.PlayEffect();
            }

        }
        public override void PlayEffect(float weight)
        {

            if (weather == null)
                weather = CozyWeather.instance;


            if (weight <= 0.03f)
            {
                StopEffect();
                return;
            }

            foreach (MultiFXType i in multiFX)
            {
                i.FX.PlayEffect(i.intensityCurve.Evaluate(weather.GetCurrentDayPercentage()) * weight);
            }

        }

        public override void StopEffect()
        {
            if (weather == null)
                weather = CozyWeather.instance;

            foreach (MultiFXType i in multiFX)
            {
                i.FX.StopEffect();
            }


        }

        public override bool InitializeEffect(VFXModule VFX)
        {

            if (VFX == null)
                VFX = CozyWeather.instance.VFX;

            VFXMod = VFX;

            weather = VFX.weatherSphere;

            foreach (MultiFXType i in multiFX)
            {
                i.FX.InitializeEffect(VFX);
            }

            return true;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MultiFXProfile))]
    [CanEditMultipleObjects]
    public class E_MultiFXProfile : E_FXProfile
    {


        void OnEnable()
        {

        }

        public override void RenderInWindow(Rect pos)
        {

            float space = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var propPosA = new Rect(pos.x, pos.y + space, pos.width, EditorGUIUtility.singleLineHeight);

            serializedObject.Update();

            EditorGUI.PropertyField(propPosA, serializedObject.FindProperty("multiFX"));

            serializedObject.ApplyModifiedProperties();
        }

        public override float GetLineHeight()
        {

            return 1 + (serializedObject.FindProperty("multiFX").isExpanded ? serializedObject.FindProperty("multiFX").arraySize : 0);

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("multiFX"));

            serializedObject.ApplyModifiedProperties();

        }

    }
#endif
}