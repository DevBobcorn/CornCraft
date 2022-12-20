using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy
{
    [ExecuteAlways]
    public class CozyReports : CozyModule
    {

        void OnEnable()
        {

            if (GetComponent<CozyWeather>())
            {

                GetComponent<CozyWeather>().IntitializeModule(typeof(CozyReports));
                DestroyImmediate(this);
                Debug.LogWarning("Add modules in the settings tab in COZY 2!");
                return;

            }
        }

        // Start is called before the first frame update
        void Awake()
        {

            if (!enabled)
                return;

            SetupModule();

        }

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CozyReports))]
    public class E_CozyReports : E_CozyModule
    {

        CozyReports t;

        void OnEnable()
        {

            t = (CozyReports)target;

        }

        public override GUIContent GetGUIContent()
        {

            return new GUIContent("    Reports", (Texture)Resources.Load("Reports"), "Passes information on the current weather system to the editor.");

        }

        public override void OnInspectorGUI()
        {


        }

        public override void DisplayInCozyWindow()
        {


            if (t.weatherSphere.perennialProfile.realisticYear)
                EditorGUILayout.HelpBox("Currently it is " + t.weatherSphere.perennialProfile.FormatTime(false, t.weatherSphere.currentTicks) + " on " + t.weatherSphere.MonthTitle(t.weatherSphere.GetCurrentYearPercentage()) + ".", MessageType.None, true);
            else
                EditorGUILayout.HelpBox("Currently it is " + t.weatherSphere.perennialProfile.FormatTime(false, t.weatherSphere.currentTicks) + " in " + t.weatherSphere.MonthTitle(t.weatherSphere.GetCurrentYearPercentage()) + ".", MessageType.None, true);

            EditorGUILayout.HelpBox("Currently the global ecosystem is running at " + Mathf.Round(t.weatherSphere.currentTemperature) + "°F or " + Mathf.Round(t.weatherSphere.currentTemperatureCelsius) + "°C with a precipitation chance of " + Mathf.Round(t.weatherSphere.currentPrecipitation) + "%.\n" +
                    "Temperatures will " + (t.weatherSphere.currentTemperature > t.weatherSphere.GetTemperature(false, t.weatherSphere.perennialProfile.ticksPerDay) ? "drop" : "rise") + " tomorrow, bringing the temperature to " + Mathf.Round(t.weatherSphere.GetTemperature(false, t.weatherSphere.perennialProfile.ticksPerDay)) + "°F", MessageType.None);

            if (t.weatherSphere.currentForecast.Count == 0)
            {
                EditorGUILayout.HelpBox("No forecast information yet!", MessageType.None);

            }
            else
            {
                EditorGUILayout.HelpBox("Currently it is " + t.weatherSphere.currentWeather.name, MessageType.None);


                for (int i = 0; i < t.weatherSphere.currentForecast.Count; i++)
                {

                    EditorGUILayout.HelpBox("Starting at " + t.weatherSphere.perennialProfile.FormatTime(false, t.weatherSphere.currentForecast[i].startTicks) + " the weather will change to " +
                        t.weatherSphere.currentForecast[i].profile.name + " for " + Mathf.Round(t.weatherSphere.currentForecast[i].weatherProfileDuration) +
                        " ticks or unitl " + t.weatherSphere.perennialProfile.FormatTime(false, t.weatherSphere.currentForecast[i].endTicks) + ".", MessageType.None, true);

                    EditorGUILayout.Space(2);

                }
            }


        }

    }
#endif
}
