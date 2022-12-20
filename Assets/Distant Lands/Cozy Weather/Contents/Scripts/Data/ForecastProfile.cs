using System;
// Distant Lands 2022.



using DistantLands.Cozy.Data;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;



namespace DistantLands.Cozy.Data
{

    [System.Serializable]
    [CreateAssetMenu(menuName = "Distant Lands/Cozy/Forecast Profile", order = 361)]
    public class ForecastProfile : ScriptableObject
    {


        [Tooltip("The weather profiles that this profile will forecast.")]
        public List<WeatherProfile> profilesToForecast;

        [Tooltip("The weather profile that this profile will forecast initially.")]
        public WeatherProfile initialProfile;
        [Tooltip("The weather profiles that this profile will forecast initially.")]
        public List<CozyWeather.WeatherPattern> initialForecast;

        public enum StartWeatherWith { random, initialProfile, initialForecast }
        public StartWeatherWith startWeatherWith;

        [Tooltip("The amount of weather profiles to forecast ahead.")]
        public int forecastLength;

    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ForecastProfile))]
[CanEditMultipleObjects]
public class E_ForecastProfile : Editor
{

    SerializedProperty profilesToForecast;
    SerializedProperty forecastLength;
    SerializedProperty startWeatherWith;
    SerializedProperty startWithRandomWeather;
    ForecastProfile prof;
    Vector2 scrollPos;

    void OnEnable()
    {
        profilesToForecast = serializedObject.FindProperty("profilesToForecast");
        forecastLength = serializedObject.FindProperty("forecastLength");
        startWithRandomWeather = serializedObject.FindProperty("startWeatherWith");
        prof = (ForecastProfile)target;

    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();


        EditorGUILayout.PropertyField(profilesToForecast);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(startWithRandomWeather);
        if (startWithRandomWeather.enumValueIndex == (int)ForecastProfile.StartWeatherWith.initialProfile)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("initialProfile"));
        if (startWithRandomWeather.enumValueIndex == (int)ForecastProfile.StartWeatherWith.initialForecast)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("initialForecast"), true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(forecastLength, new GUIContent("Profiles to Forecast Ahead"));
        serializedObject.ApplyModifiedProperties();


    }

}
#endif