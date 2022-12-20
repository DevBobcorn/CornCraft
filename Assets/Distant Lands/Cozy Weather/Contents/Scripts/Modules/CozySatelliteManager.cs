using System.Collections;
using System.Collections.Generic;
// Distant Lands 2022.



using UnityEngine;
using DistantLands.Cozy.Data;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy
{
    [ExecuteAlways]
    public class CozySatelliteManager : CozyModule
    {


        public List<SatelliteProfile> satellites = new List<SatelliteProfile>();
        [HideInInspector]
        public Transform satHolder = null;
        public bool hideInHierarchy = true;


        void OnEnable()
        {
            if (GetComponent<CozyWeather>())
            {

                GetComponent<CozyWeather>().IntitializeModule(typeof(CozySatelliteManager));
                DestroyImmediate(this);
                Debug.LogWarning("Add modules in the settings tab in COZY 2!");
                return;

            }
        }

        // Start is called before the first frame update
        void Awake()
        {
            weatherSphere = CozyWeather.instance;
            UpdateSatellites();

        }

        // Update is called once per frame
        void Update()
        {

            if (satHolder == null)
            {
                UpdateSatellites();
            }

            if (satHolder.hideFlags == (HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild) && hideInHierarchy)
                UpdateSatellites();
                
            if (satHolder.hideFlags != (HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild) && !hideInHierarchy)
                UpdateSatellites();

            satHolder.position = transform.position;

            if (satellites != null)
                foreach (SatelliteProfile sat in satellites)
                {
                    if (!sat)
                        break;

                    if (sat.orbitRef == null)
                        UpdateSatellites();

                    if (sat.changedLastFrame == true)
                        UpdateSatellites();

                    sat.satelliteRotation += Time.deltaTime * sat.satelliteRotateSpeed;

                    sat.orbitRef.localEulerAngles = new Vector3(0, sat.satelliteDirection, sat.satellitePitch);
                    sat.orbitRef.GetChild(0).localEulerAngles = Vector3.right * ((360 * weatherSphere.GetCurrentDayPercentage()) + sat.orbitOffset);
                    sat.moonRef.localEulerAngles = sat.initialRotation + sat.satelliteRotateAxis.normalized * sat.satelliteRotation;

                    if (sat.useLight)
                    {

                        sat.lightRef.color = weatherSphere.moonlightColor * sat.lightColorMultiplier;


                    }
                }
        }


        public void UpdateSatellites()
        {
            if (weatherSphere == null)
                weatherSphere = CozyWeather.instance;

            Transform oldHolder = null;


            if (satHolder)
            {
                oldHolder = satHolder;
            }

            satHolder = new GameObject("Cozy Satellites").transform;
            if (hideInHierarchy)
                satHolder.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.HideInHierarchy;
            else
                satHolder.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;


            bool j = true;

            if (satellites != null)
                foreach (SatelliteProfile i in satellites)
                {
                    InitializeSatellite(i, j);
                    j = false;
                }

            if (oldHolder)
                DestroyImmediate(oldHolder.gameObject);

        }

        public void DestroySatellites()
        {

            if (satHolder)
                DestroyImmediate(satHolder.gameObject);

        }

        public void DestroySatellite(SatelliteProfile sat)
        {

            if (sat.orbitRef)
                DestroyImmediate(sat.orbitRef.gameObject);

        }

        private void OnDisable()
        {
            DestroySatellites();
        }

        public void InitializeSatellite(SatelliteProfile sat, bool mainMoon)
        {


            float dist = 0;

            if (weatherSphere.lockToCamera != CozyWeather.LockToCameraStyle.DontLockToCamera && weatherSphere.cozyCamera)
                dist = .92f * weatherSphere.cozyCamera.farClipPlane * sat.distance;
            else
                dist = .92f * 1000 * sat.distance * weatherSphere.transform.localScale.x;

            sat.orbitRef = new GameObject(sat.name).transform;
            sat.orbitRef.parent = satHolder;
            sat.orbitRef.transform.localPosition = Vector3.zero;
            var orbitArm = new GameObject("Orbit Arm");
            orbitArm.transform.parent = sat.orbitRef;
            orbitArm.transform.localPosition = Vector3.zero;
            orbitArm.transform.localEulerAngles = Vector3.zero;
            sat.moonRef = Instantiate(sat.satelliteReference, Vector3.forward * dist, Quaternion.identity, sat.orbitRef.GetChild(0)).transform;
            sat.moonRef.transform.localPosition = -Vector3.forward * dist;
            sat.moonRef.transform.localEulerAngles = sat.initialRotation;
            sat.moonRef.transform.localScale = sat.satelliteReference.transform.localScale * sat.size * dist / 1000;
            sat.orbitRef.localEulerAngles = new Vector3(0, sat.satelliteDirection, sat.satellitePitch);
            sat.orbitRef.GetChild(0).localEulerAngles = Vector3.right * ((360 * weatherSphere.GetCurrentDayPercentage()) + sat.orbitOffset);

            if (sat.useLight)
            {
                var obj = new GameObject("Light");
                obj.transform.parent = sat.orbitRef.GetChild(0);
                sat.lightRef = obj.AddComponent<Light>();
                sat.lightRef.transform.localEulerAngles = new Vector3(0, 0, 0);
                sat.lightRef.transform.localPosition = new Vector3(0, 0, 0);
                sat.lightRef.type = LightType.Directional;
                sat.lightRef.shadows = sat.castShadows;
                if (sat.flare)
                    sat.lightRef.flare = sat.flare;

            }
            if (mainMoon)
            {
                sat.orbitRef.GetChild(0).gameObject.AddComponent<CozySetMoonDirection>();
            }

            sat.changedLastFrame = false;
        }

        void Reset()
        {
            satellites = new List<SatelliteProfile>();
            satellites.Add(Resources.Load("Profiles/Satellites/Moon") as SatelliteProfile);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CozySatelliteManager))]
    [CanEditMultipleObjects]
    public class E_SatelliteManager : E_CozyModule
    {

        CozySatelliteManager t;
        static bool manageSatellites;

        private void OnEnable()
        {
            t = (CozySatelliteManager)target;
        }

        public override GUIContent GetGUIContent()
        {

            return new GUIContent("    Satellites", (Texture)Resources.Load("CozyMoon"), "Manage satellites and moons within the COZY system.");

        }

        public override void OnInspectorGUI()
        {


        }

        public override void DisplayInCozyWindow()
        {

            serializedObject.Update();
            manageSatellites = EditorGUILayout.BeginFoldoutHeaderGroup(manageSatellites, new GUIContent("    Manage Satellites"), EditorUtilities.FoldoutStyle());
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (manageSatellites)
            {
                EditorGUILayout.Space();
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("satellites"));
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hideInHierarchy"));
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel++;
            if (t.satellites != null)
                foreach (SatelliteProfile i in t.satellites)
                {
                    if (i)
                        (CreateEditor(i) as E_SatelliteProfile).NestedGUI();
                }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            if (GUILayout.Button("Refresh Satellites"))
                ((CozySatelliteManager)target).UpdateSatellites();

            serializedObject.ApplyModifiedProperties();

        }

    }

#endif
}