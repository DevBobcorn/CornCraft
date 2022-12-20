using DistantLands.Cozy.Data;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy
{
    public class CozySaveLoad : CozyModule
    {


        void OnEnable()
        {
            if (GetComponent<CozyWeather>())
            {

                GetComponent<CozyWeather>().IntitializeModule(typeof(CozySaveLoad));
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


        public void Save()
        {

            if (weatherSphere == null)
                SetupModule();


            string weatherJSON = JsonUtility.ToJson(weatherSphere);
            PlayerPrefs.SetString("CZY_Properties", weatherJSON);
            PlayerPrefs.SetString("CZY_Perennial", JsonUtility.ToJson(weatherSphere.perennialProfile));

        }

        public void Load()
        {


            if (weatherSphere == null)
                SetupModule();

            JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString("CZY_Properties"), weatherSphere);
            JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString("CZY_Perennial"), weatherSphere.perennialProfile);

            weatherSphere.Update();

        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CozySaveLoad))]
    public class E_CozySaveLoad : E_CozyModule
    {

        CozySaveLoad saveLoad;

        void OnEnable()
        {

            saveLoad = (CozySaveLoad)target;

        }

        public override GUIContent GetGUIContent()
        {

            return new GUIContent("    Save & Load", (Texture)Resources.Load("Save"), "Manage save and load commands within the COZY system.");

        }

        public override void OnInspectorGUI()
        {


        }

        public override void DisplayInCozyWindow()
        {

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Save"))
                saveLoad.Save();
            if (GUILayout.Button("Load"))
                saveLoad.Load();

            EditorGUILayout.EndHorizontal();

        }

    }
#endif
}