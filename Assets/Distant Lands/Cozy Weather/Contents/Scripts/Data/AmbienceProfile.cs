// Distant Lands 2022.



using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace DistantLands.Cozy.Data
{

    [System.Serializable]
    [CreateAssetMenu(menuName = "Distant Lands/Cozy/Ambience Profile", order = 361)]
    public class AmbienceProfile : ScriptableObject
    {

        [Tooltip("Specifies the minimum (x) and maximum (y) length for this ambience profile.")]
        public Vector2 playTime = new Vector2(30, 60);
        [Tooltip("Multiplier for the computational chance that this ambience profile will play; 0 being never, and 2 being twice as likely as the average.")]
        [Range(0, 2)]
        public float likelihood = 1;
        [HideTitle(2)]
        public WeatherProfile[] dontPlayDuring;
        [ChanceEffector]
        public List<ChanceEffector> chances;


        [FX]
        public FXProfile[] FX;
        [Range(0, 1)]
        public float FXVolume = 1;
        public bool useVFX;

        public float GetChance (CozyWeather weather)
        {

            float i = likelihood;

            foreach (ChanceEffector j in chances)
            {
                i *= j.GetChance(weather);
            }

            return Mathf.Clamp(i, 0, 1000000);

        }
        public void SetWeight(float weightVal)
        {

            foreach (FXProfile fx in FX)
                fx.PlayEffect(weightVal);

        }

        public void Stop()
        {

            foreach (FXProfile fx in FX)
                fx.StopEffect();

        }

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(AmbienceProfile))]
    [CanEditMultipleObjects]
    public class E_AmbienceProfile : Editor
    {

        SerializedProperty dontPlayDuring;
        SerializedProperty chances;
        SerializedProperty particleFX;
        SerializedProperty soundFX;
        SerializedProperty likelihood;
        Vector2 scrollPos;
        AmbienceProfile prof;



        void OnEnable()
        {

            prof = (AmbienceProfile)target;

            dontPlayDuring = serializedObject.FindProperty("dontPlayDuring");
            chances = serializedObject.FindProperty("chances");
            particleFX = serializedObject.FindProperty("particleFX");
            soundFX = serializedObject.FindProperty("soundFX");
            likelihood = serializedObject.FindProperty("likelihood");

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Undo.RecordObject(prof, prof.name + " Profile Changes");

            EditorGUILayout.LabelField("Forecasting Behaviours", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            Rect position = EditorGUILayout.GetControlRect();
            float startPos = position.width / 2.75f;
            var titleRect = new Rect(position.x, position.y, 70, position.height);
            EditorGUI.PrefixLabel(titleRect, new GUIContent("Ambience Length"));
            float min = serializedObject.FindProperty("playTime").vector2Value.x;
            float max = serializedObject.FindProperty("playTime").vector2Value.y;
            var label1Rect = new Rect();
            var label2Rect = new Rect();
            var sliderRect = new Rect();

            if (position.width > 359)
            {
                label1Rect = new Rect(startPos, position.y, 64, position.height);
                label2Rect = new Rect(position.width - 47, position.y, 64, position.height);
                sliderRect = new Rect(startPos + 56, position.y, (position.width - startPos) - 95, position.height);
                EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, 0, 120);
            }
            else
            {

                label1Rect = new Rect(position.width - 110, position.y, 50, position.height);
                label2Rect = new Rect(position.width - 72, position.y, 50, position.height);

            }

            min = EditorGUI.FloatField(label1Rect, (Mathf.Round(min * 100) / 100));
            max = EditorGUI.FloatField(label2Rect, (Mathf.Round(max * 100) / 100));

            if (min > max)
                min = max;

            serializedObject.FindProperty("playTime").vector2Value = new Vector2(min, max);
            EditorGUILayout.PropertyField(likelihood);

            EditorGUILayout.Space(10);

            EditorGUILayout.PropertyField(dontPlayDuring, true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("chances"), new GUIContent("Chance Effectors"), true);
            EditorGUILayout.Space(10);
            EditorGUI.indentLevel--;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FX"));
            serializedObject.ApplyModifiedProperties();

        }
    }
#endif
}