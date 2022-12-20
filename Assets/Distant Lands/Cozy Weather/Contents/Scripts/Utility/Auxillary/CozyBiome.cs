using DistantLands.Cozy.Data;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace DistantLands.Cozy
{
    public class CozyBiome : CozyEcosystem
    {


        public List<Vector3> bounds = new List<Vector3>() { new Vector3(-5, 0, 5), new Vector3(5, 0, 5), new Vector3(5, 0, -5), new Vector3(-5, 0, -5) };
        public float height = 10;
        public float transitionDistance = 5;
        public Color displayColor = new Color(1, 1, 1, 1);
        MeshCollider trigger;
        Transform cam;


        // Start is called before the first frame update
        void Start()
        {

            weatherSphere = CozyWeather.instance;
            cam = Camera.main.transform;

            if (Application.isPlaying)
            {
                trigger = gameObject.AddComponent<MeshCollider>();
                trigger.sharedMesh = BuildZoneCollider();
                trigger.convex = true;
                trigger.isTrigger = true;

                weatherSphere.ecosystems.Add(this);
            }


        }

        public Mesh BuildZoneCollider()
        {

            Mesh mesh = new Mesh();
            mesh.name = $"{name} Custom Trigger Mesh";

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            foreach (Vector3 i in bounds)
            {

                verts.Add(i);
                verts.Add(new Vector3(i.x, height, i.z));

            }

            for (int i = 0; i < bounds.Count; i++)
            {

                if (i == 0)
                {
                    tris.Add(0);
                    tris.Add(verts.Count - 1);
                    tris.Add(verts.Count - 2);

                    tris.Add(0);
                    tris.Add(1);
                    tris.Add(verts.Count - 1);
                }
                else
                {
                    int start = i * 2;

                    tris.Add(start);
                    tris.Add(start - 1);
                    tris.Add(start - 2);

                    tris.Add(start);
                    tris.Add(start + 1);
                    tris.Add(start - 1);

                }
            }

            for (int i = 0; i < verts.Count - 4; i += 2)
            {

                tris.Add(0);
                tris.Add(i + 2);
                tris.Add(i + 4);

                tris.Add(1);
                tris.Add(i + 3);
                tris.Add(i + 5);

            }


            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0, true);
            mesh.RecalculateNormals();

            return mesh;


        }

        private new void Update()
        {
            base.Update();

            if (Application.isPlaying)
                weight = GetWeight();

        }

        public float GetWeight()
        {

            Vector3 point = cam.position;
            Vector3 closestPoint = trigger.ClosestPoint(point);


            if (point == closestPoint)
            {
                return 1;
            }

            float distToClosestPoint = Vector3.Distance(point, closestPoint);
            return 1 - (Mathf.Clamp(distToClosestPoint, 0, transitionDistance) / transitionDistance);

        }

        private void OnEnable()
        {
            if (Application.isPlaying)
                AddBiome();
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
                RemoveBiome();
        }

        public void AddBiome()
        {

            weatherSphere.ecosystems.Add(this);


        }

        public void RemoveBiome()
        {

            weatherSphere.ecosystems.Remove(this);

        }

        private void OnDrawGizmos()
        {

            if (bounds.Count >= 3)
            {
                for (int i = 0; i < bounds.Count; i++)
                {

                    Gizmos.color = new Color(displayColor.r, displayColor.g, displayColor.b, 0.3f);
                    Gizmos.DrawSphere(TransformToLocalSpace(bounds[i]), 0.2f);

                    Vector3 point = Vector3.zero;
                    if (i == 0)
                        point = bounds.Last();
                    else
                        point = bounds[i - 1];

                    Gizmos.color = new Color(displayColor.r, displayColor.g, displayColor.b, 1);
                    Gizmos.DrawLine(TransformToLocalSpace(bounds[i]), TransformToLocalSpace(point));

                }

                for (int i = 0; i < bounds.Count; i++)
                {

                    Gizmos.color = new Color(displayColor.r, displayColor.g, displayColor.b, 0.5f);
                    Gizmos.DrawSphere(TransformToLocalSpace(bounds[i]) + Vector3.up * height, 0.2f);

                    Vector3 point = Vector3.zero;
                    if (i == 0)
                        point = bounds.Last();
                    else
                        point = bounds[i - 1];

                    Gizmos.color = new Color(displayColor.r, displayColor.g, displayColor.b, 1);
                    Gizmos.DrawLine(TransformToLocalSpace(bounds[i]) + Vector3.up * height, TransformToLocalSpace(point) + Vector3.up * height);
                    Gizmos.DrawLine(TransformToLocalSpace(bounds[i]), TransformToLocalSpace(bounds[i]) + Vector3.up * height);
                    Gizmos.color = new Color(displayColor.r, displayColor.g, displayColor.b, 0.3f);
                    Gizmos.DrawLine((TransformToLocalSpace(bounds[i]) + TransformToLocalSpace(point)) / 2,
                        (TransformToLocalSpace(bounds[i]) + TransformToLocalSpace(point)) / 2 + Vector3.up * height);


                }
            }

        }

        private Vector3 TransformToLocalSpace(Vector3 pos)
        {
            Vector3 i = pos.x * transform.right + pos.y * transform.up + pos.z * transform.forward;

            i += transform.position;



            return i;

        }

        void Reset()
        {

            forecastProfile = Resources.Load("Profiles/Forecast Profiles/Complex Forecast Profile") as ForecastProfile;
            currentWeather = Resources.Load("Profiles/Weather Profiles/Partly Cloudy") as WeatherProfile;
            climateProfile = Resources.Load("Profiles/Climate Profiles/Default Climate") as ClimateProfile;

        }

    }

#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CozyBiome))]
    public class CozyBiomeEditor : Editor
    {

        protected static bool weatherFoldout;
        protected static bool biomeFoldout;
        protected static bool boundsFoldout;
        protected static bool infoFoldout;
        CozyBiome biome;
        CozyWeather weatherSphere;


        private void OnEnable()
        {

            biome = (CozyBiome)target;
            weatherSphere = CozyWeather.instance;

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.indentLevel = 0;

            GUIStyle foldoutStyle = new GUIStyle(GUI.skin.GetStyle("toolbarPopup"));
            foldoutStyle.fontStyle = FontStyle.Bold;
            foldoutStyle.margin = new RectOffset(30, 10, 5, 5);


            weatherFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(weatherFoldout, "   Weather and Forecast", foldoutStyle);
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (weatherFoldout)
            {

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("weatherSelectionMode"));

                EditorGUILayout.Space();

                if ((CozyEcosystem.EcosystemStyle)serializedObject.FindProperty("weatherSelectionMode").intValue == CozyEcosystem.EcosystemStyle.forecast)
                {

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("currentWeather"));
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("forecastProfile"));

                    EditorGUILayout.Space();

                    EditorGUI.indentLevel++;
                    if (serializedObject.FindProperty("forecastProfile").objectReferenceValue)
                        CreateEditor(serializedObject.FindProperty("forecastProfile").objectReferenceValue).OnInspectorGUI();
                    else
                    {
                        EditorGUILayout.HelpBox("Assign a forecast profile!", MessageType.Error);

                    }
                    EditorGUI.indentLevel--;

                }
                else
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("currentWeather"));


                EditorGUILayout.Space();


                EditorGUI.indentLevel--;

            }

            biomeFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(biomeFoldout, "   Climate", foldoutStyle);
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (biomeFoldout)
            {

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("climateProfile"));
                EditorGUILayout.Space();

                EditorGUI.indentLevel++;
                if (serializedObject.FindProperty("climateProfile").objectReferenceValue)
                    CreateEditor(serializedObject.FindProperty("climateProfile").objectReferenceValue).OnInspectorGUI();
                else
                {
                    EditorGUILayout.HelpBox("Assign a climate profile!", MessageType.Error);

                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("localTemperatureFilter"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("localPrecipitationFilter"));
                EditorGUI.indentLevel--;


            }

            boundsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(boundsFoldout, "   Bounds and Trigger", foldoutStyle);
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (boundsFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bounds"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("height"));
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("displayColor"));
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("transitionDistance"));
                EditorGUI.indentLevel--;

            }


            infoFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(infoFoldout, "   Current Information", foldoutStyle);
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (infoFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.HelpBox("Currently it is " + Mathf.Round(biome.currentTemperature) + "F or " + Mathf.Round(biome.currentTemperatureCelsius) + "C with a precipitation chance of " + Mathf.Round(biome.currentPrecipitation) + "%.\n" +
"Temperatures will " + (biome.currentTemperature > biome.GetTemperature(false, weatherSphere.perennialProfile.ticksPerDay) ? "drop" : "rise") + " tomorrow, bringing the temprature to " + Mathf.Round(biome.GetTemperature(false, weatherSphere.perennialProfile.ticksPerDay)) + "F", MessageType.None);
                EditorGUILayout.Space();


                if (biome.currentForecast.Count == 0)
                {
                    EditorGUILayout.HelpBox("No forecast information yet!", MessageType.None);

                }
                else
                {
                    EditorGUILayout.HelpBox("Currently it is " + biome.weatherSphere.currentWeather.name, MessageType.None);


                    for (int i = 0; i < biome.currentForecast.Count; i++)
                    {

                        EditorGUILayout.HelpBox("Starting at " + biome.weatherSphere.perennialProfile.FormatTime(false, biome.currentForecast[i].startTicks) + " the weather will change to " +
                            biome.currentForecast[i].profile.name + " for " + Mathf.Round(biome.currentForecast[i].weatherProfileDuration) +
                            " ticks or unitl " + biome.weatherSphere.perennialProfile.FormatTime(false, biome.currentForecast[i].endTicks) + ".", MessageType.None, true);

                        EditorGUILayout.Space(2);

                    }
                }
                EditorGUI.indentLevel--;

            }

            serializedObject.ApplyModifiedProperties();


        }
    }
#endif
}