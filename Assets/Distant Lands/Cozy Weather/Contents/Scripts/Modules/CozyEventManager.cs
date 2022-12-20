using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DistantLands.Cozy.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy
{
    public class CozyEventManager : CozyModule
    {


        public UnityEvent onMorning;
        public UnityEvent onNoon;
        public UnityEvent onEvening;
        public UnityEvent onMidnight;
        public UnityEvent onNewTick;
        public UnityEvent onNewHour;
        public UnityEvent onNewDay;
        public UnityEvent onNewYear;
        public UnityEvent onWeatherProfileChange;

        [System.Serializable]
        public class CozyEvent
        {

            public EventFX fxReference;
            public UnityEvent onPlay;
            public UnityEvent onStop;

        }

        public List<CozyEvent> cozyEvents;
        public static List<EventFX> allEventTypes;
        public static List<EventFX> allEventTypesRef;

        void Awake()
        {

            if (Application.isPlaying)
                foreach (CozyEvent i in cozyEvents)
                {

                    i.fxReference.playing = false;

                    if (i.fxReference)
                    {

                        i.fxReference.onCall += i.onPlay.Invoke;
                        i.fxReference.onEnd += i.onStop.Invoke;

                    }
                }
        }


        private void OnEnable()
        {

            if (GetComponent<CozyWeather>())
            {

                GetComponent<CozyWeather>().IntitializeModule(typeof(CozyEventManager));
                DestroyImmediate(this);
                Debug.LogWarning("Add modules in the settings tab in COZY 2!");
                return;

            }

            StartCoroutine(Refresh());

        }

        public IEnumerator Refresh()
        {

            yield return new WaitForEndOfFrame();

            CozyWeather.Events.onMorning += onMorning.Invoke;
            CozyWeather.Events.onNoon += onNoon.Invoke;
            CozyWeather.Events.onEvening += onEvening.Invoke;
            CozyWeather.Events.onMidnight += onMidnight.Invoke;
            CozyWeather.Events.onNewTick += onNewTick.Invoke;
            CozyWeather.Events.onNewHour += onNewHour.Invoke;
            CozyWeather.Events.onNewDay += onNewDay.Invoke;
            CozyWeather.Events.onNewYear += onNewYear.Invoke;
            CozyWeather.Events.onWeatherChange += onWeatherProfileChange.Invoke;

        }

        private void OnDisable()
        {

            CozyWeather.Events.onMorning -= onMorning.Invoke;
            CozyWeather.Events.onNoon -= onNoon.Invoke;
            CozyWeather.Events.onEvening -= onEvening.Invoke;
            CozyWeather.Events.onMidnight -= onMidnight.Invoke;
            CozyWeather.Events.onNewTick -= onNewTick.Invoke;
            CozyWeather.Events.onNewHour -= onNewHour.Invoke;
            CozyWeather.Events.onNewDay -= onNewDay.Invoke;
            CozyWeather.Events.onNewYear -= onNewYear.Invoke;
            CozyWeather.Events.onWeatherChange -= onWeatherProfileChange.Invoke;

        }

        public void LogConsoleEvent()
        {

            Debug.Log("Test Event Passed.");

        }
        public void LogConsoleEvent(string log)
        {

            Debug.Log($"Test Event Passed. Log: {log}");

        }
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(CozyEventManager))]
    [CanEditMultipleObjects]
    public class E_EventManager : E_CozyModule
    {

        protected static bool todEvents;
        protected static bool teEvents;
        protected static bool weatherEvents;
        protected static bool eventSettings;

        public override GUIContent GetGUIContent()
        {

            return new GUIContent("    Events", (Texture)Resources.Load("Events"), "Setup Unity events that directly integrate into the COZY system.");

        }

        void OnEnable()
        {

        }

        public override void DisplayInCozyWindow()
        {
            serializedObject.Update();

            todEvents = EditorGUILayout.BeginFoldoutHeaderGroup(todEvents,
                    new GUIContent("    Time of Day Events"), EditorUtilities.FoldoutStyle());

            EditorGUILayout.EndFoldoutHeaderGroup();
            if (todEvents)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onMorning"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onNoon"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onEvening"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onMidnight"));
                EditorGUI.indentLevel--;
            }

            teEvents = EditorGUILayout.BeginFoldoutHeaderGroup(teEvents,
                new GUIContent("    Time Elapsed Events"), EditorUtilities.FoldoutStyle());

            EditorGUILayout.EndFoldoutHeaderGroup();
            if (teEvents)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onNewTick"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onNewHour"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onNewDay"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onNewYear"));
                EditorGUI.indentLevel--;
            }

            weatherEvents = EditorGUILayout.BeginFoldoutHeaderGroup(weatherEvents,
                new GUIContent("    Weather Events"), EditorUtilities.FoldoutStyle());

            EditorGUILayout.EndFoldoutHeaderGroup();
            if (weatherEvents)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onWeatherProfileChange"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cozyEvents"), new GUIContent("Event FX"));
                
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();

        }

    }
#endif
}