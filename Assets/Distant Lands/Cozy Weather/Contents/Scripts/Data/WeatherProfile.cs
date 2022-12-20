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
    [CreateAssetMenu(menuName = "Distant Lands/Cozy/Weather Profile", order = 361)]
    public class WeatherProfile : ScriptableObject
    {

        [Tooltip("Specifies the minimum (x) and maximum (y) length for this weather profile.")]
        public Vector2 weatherTime = new Vector2(120, 480);
        [Tooltip("Multiplier for the computational chance that this weather profile will play; 0 being never, and 2 being twice as likely as the average.")]
        [Range(0, 2)]
        public float likelihood = 1;
        [HideTitle]
        [Tooltip("Allow only these weather profiles to immediately follow this weather profile in a forecast.")]
        public WeatherProfile[] forecastNext;
        public enum ForecastModifierMethod { forecastNext, DontForecastNext, forecastAnyProfileNext }
        public ForecastModifierMethod forecastModifierMethod = ForecastModifierMethod.forecastAnyProfileNext;
        [Tooltip("Animation curves that increase or decrease weather chance based on time, temprature, etc.")]
        [ChanceEffector]
        public List<ChanceEffector> chances;


        public CloudSettings cloudSettings;


        [Tooltip("The density of fog for this weather profile.")]
        [Range(0.1f, 5)]
        public float fogDensity = 1;



        [FX]
        public FXProfile[] FX;

        [System.Serializable]
        public class CloudSettings
        {
            [Tooltip("Multiplier for cumulus clouds.")]
            [Range(0, 2)]
            public float cumulusCoverage = 1;
            [Space(5)]
            [Tooltip("Multiplier for altocumulus clouds.")]
            [Range(0, 2)]
            public float altocumulusCoverage = 0;
            [Tooltip("Multiplier for chemtrails.")]
            [Range(0, 2)]
            public float chemtrailCoverage = 0;
            [Tooltip("Multiplier for cirrostratus clouds.")]
            [Range(0, 2)]
            public float cirrostratusCoverage = 0;
            [Tooltip("Multiplier for cirrus clouds.")]
            [Range(0, 2)]
            public float cirrusCoverage = 0;
            [Tooltip("Multiplier for nimbus clouds.")]
            [Space(5)]
            [Range(0, 2)]
            public float nimbusCoverage = 0;
            [Tooltip("Variation for nimbus clouds.")]
            [Range(0, 1)]
            public float nimbusVariation = 0.9f;
            [Tooltip("Height mask effect for nimbus clouds.")]
            [Range(0, 1)]
            public float nimbusHeightEffect = 1;

            [Space(5)]
            [Tooltip("Starting height for cloud border.")]
            [Range(0, 1)]
            public float borderHeight = 0.5f;
            [Tooltip("Variation for cloud border.")]
            [Range(0, 1)]
            public float borderVariation = 0.9f;
            [Tooltip("Multiplier for the border. Values below zero clip the clouds whereas values above zero add clouds.")]
            [Range(-1, 1)]
            public float borderEffect = 1;

        }

        public float GetChance(float temp, float precip, float yearPercent, float time, float snow, float rain)
        {

            float i = likelihood;

            foreach (ChanceEffector j in chances)
            {
                i *= j.GetChance(temp, precip, yearPercent, time, snow, rain);
            }

            return i > 0 ? i : 0;

        }
        public float GetChance(CozyWeather weather)
        {

            float i = likelihood;

            foreach (ChanceEffector j in chances)
            {
                i *= j.GetChance(weather);
            }

            return i > 0 ? i : 0;

        }


        public void SetWeatherWeight(float weightVal)
        {

            foreach (FXProfile fx in FX)
                if (fx != null)
                    fx.PlayEffect(weightVal);

        }

        public void StopWeather()
        {

            foreach (FXProfile fx in FX)
                fx.StopEffect();

        }



    }


#if UNITY_EDITOR
    [CustomEditor(typeof(WeatherProfile))]
    [CanEditMultipleObjects]
    public class E_WeatherProfile : Editor
    {

        WeatherProfile prof;



        void OnEnable()
        {

            prof = (WeatherProfile)target;

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Forecasting Behaviours", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            Rect position = EditorGUILayout.GetControlRect();
            float startPos = position.width / 2.75f;
            var titleRect = new Rect(position.x, position.y, 70, position.height);
            EditorGUI.PrefixLabel(titleRect, new GUIContent("Weather Length"));
            float min = serializedObject.FindProperty("weatherTime").vector2Value.x;
            float max = serializedObject.FindProperty("weatherTime").vector2Value.y;
            var label1Rect = new Rect();
            var label2Rect = new Rect();
            var sliderRect = new Rect();

            if (position.width > 359)
            {
                label1Rect = new Rect(startPos, position.y, 64, position.height);
                label2Rect = new Rect(position.width - 47, position.y, 64, position.height);
                sliderRect = new Rect(startPos + 56, position.y, (position.width - startPos) - 95, position.height);
                EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, 0, 180);
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

            serializedObject.FindProperty("weatherTime").vector2Value = new Vector2(min, max);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("likelihood"));

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("forecastModifierMethod"), true);
            switch ((WeatherProfile.ForecastModifierMethod)serializedObject.FindProperty("forecastModifierMethod").intValue)
            {

                default:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("forecastNext"), new GUIContent("Forecast Modifiers", "Modifies the weather profiles that follow this in the forecast. Use the dropdown to force the forecast to either choose only one of the included profiles to forecast next, or to avoid the selected profiles entirely."), true);
                    break;
                case (WeatherProfile.ForecastModifierMethod.DontForecastNext):
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("forecastNext"), new GUIContent("Don't Forecast Next", "The forecast module will not select any of these weather profiles to immediately follow this profile in the forecast."), true);
                    break;
                case (WeatherProfile.ForecastModifierMethod.forecastNext):
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("forecastNext"), new GUIContent("Forecast Next", "The forecast module will only select one of these weather profiles to immediately follow this profile in the forecast."), true);
                    break;
                case (WeatherProfile.ForecastModifierMethod.forecastAnyProfileNext):
                    break;

            }
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("chances"), new GUIContent("Chance Effectors"), true);
            EditorGUI.indentLevel--;


            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Cloud Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("cumulusCoverage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("altocumulusCoverage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("chemtrailCoverage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("cirrostratusCoverage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("nimbusCoverage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("nimbusVariation"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("nimbusHeightEffect"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("borderHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("borderVariation"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("borderEffect"));
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogDensity"));


            EditorGUILayout.Space(20);
            EditorGUI.indentLevel--;


            EditorGUILayout.PropertyField(serializedObject.FindProperty("FX"), new GUIContent("Weather Effects"));




            serializedObject.ApplyModifiedProperties();


        }


        public void DisplayInCozyWindow(CozyWeather t)
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Forecasting Behaviours", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            Rect position = EditorGUILayout.GetControlRect();
            float startPos = position.width / 2.75f;
            var titleRect = new Rect(position.x, position.y, 70, position.height);
            EditorGUI.PrefixLabel(titleRect, new GUIContent("Weather Length"));
            float min = serializedObject.FindProperty("weatherTime").vector2Value.x;
            float max = serializedObject.FindProperty("weatherTime").vector2Value.y;
            var label1Rect = new Rect();
            var label2Rect = new Rect();
            var sliderRect = new Rect();

            if (position.width > 359)
            {
                label1Rect = new Rect(startPos, position.y, 64, position.height);
                label2Rect = new Rect(position.width - 47, position.y, 64, position.height);
                sliderRect = new Rect(startPos + 56, position.y, (position.width - startPos) - 95, position.height);
                EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, 0, 180);
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

            serializedObject.FindProperty("weatherTime").vector2Value = new Vector2(min, max);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("likelihood"));

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("forecastModifierMethod"), true);
            switch ((WeatherProfile.ForecastModifierMethod)serializedObject.FindProperty("forecastModifierMethod").intValue)
            {

                default:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("forecastNext"), new GUIContent("Forecast Modifiers", "Modifies the weather profiles that follow this in the forecast. Use the dropdown to force the forecast to either choose only one of the included profiles to forecast next, or to avoid the selected profiles entirely."), true);
                    break;
                case (WeatherProfile.ForecastModifierMethod.DontForecastNext):
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("forecastNext"), new GUIContent("Don't Forecast Next", "The forecast module will not select any of these weather profiles to immediately follow this profile in the forecast."), true);
                    break;
                case (WeatherProfile.ForecastModifierMethod.forecastNext):
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("forecastNext"), new GUIContent("Forecast Next", "The forecast module will only select one of these weather profiles to immediately follow this profile in the forecast."), true);
                    break;
                case (WeatherProfile.ForecastModifierMethod.forecastAnyProfileNext):
                    break;

            }
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("chances"), new GUIContent("Chance Effectors"), true);
            EditorGUI.indentLevel--;


            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Cloud Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("cumulusCoverage"));

            if (t.cloudStyle == CozyWeather.CloudStyle.cozyDesktop || t.cloudStyle == CozyWeather.CloudStyle.paintedSkies || t.cloudStyle == CozyWeather.CloudStyle.soft)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("altocumulusCoverage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("chemtrailCoverage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("cirrostratusCoverage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("nimbusCoverage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("nimbusVariation"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("nimbusHeightEffect"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("borderHeight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("borderVariation"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSettings").FindPropertyRelative("borderEffect"));
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogDensity"));


            EditorGUILayout.Space(20);
            EditorGUI.indentLevel--;


            EditorGUILayout.PropertyField(serializedObject.FindProperty("FX"), new GUIContent("Weather Effects"));




            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

}