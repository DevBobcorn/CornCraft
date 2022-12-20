// Distant Lands 2022.



using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DistantLands.Cozy.EditorScripts
{
    public class E_CozyMenuItems : MonoBehaviour
    {

        [MenuItem("Distant Lands/Cozy/Create Cozy Volume")]
        static void CozyVolumeCreation()
        {


            Camera view = SceneView.lastActiveSceneView.camera;


            GameObject i = new GameObject();
            i.name = "Cozy Volume";
            i.AddComponent<BoxCollider>().isTrigger = true;
            i.AddComponent<CozyVolume>();
            i.transform.position = (view.transform.forward * 5) + view.transform.position;

            Undo.RegisterCreatedObjectUndo(i, "Create Cozy Volume");
            Selection.activeGameObject = i;


        }


        [MenuItem("Distant Lands/Cozy/Create Cozy FX Block Zone")]
        static void CozyBlockZoneCreation()
        {


            Camera view = SceneView.lastActiveSceneView.camera;


            GameObject i = new GameObject();
            i.name = "Cozy FX Block Zone";
            i.AddComponent<BoxCollider>().isTrigger = true;
            i.tag = "FX Block Zone";
            i.transform.position = (view.transform.forward * 5) + view.transform.position;

            Undo.RegisterCreatedObjectUndo(i, "Create Cozy FX Block Zone");
            Selection.activeGameObject = i;


        }

        [MenuItem("Distant Lands/Cozy/Create Cozy Biome")]
        static void CozyBiomeCreation()
        {


            Camera view = SceneView.lastActiveSceneView.camera;


            GameObject i = new GameObject();
            i.name = "Cozy Biome";
            i.AddComponent<CozyBiome>();
            i.transform.position = (view.transform.forward * 5) + view.transform.position;

            Undo.RegisterCreatedObjectUndo(i, "Create Cozy Biome");
            Selection.activeGameObject = i;


        }


        [MenuItem("Distant Lands/Cozy/Toggle Tooltips")]
        static void CozyToggleTooltips()
        {
            
            EditorPrefs.SetBool("CZY_Tooltips", !EditorPrefs.GetBool("CZY_Tooltips"));


        }

        [MenuItem("Distant Lands/Cozy/Setup Scene (No Modules)", false, 1)]
        public static void CozySetupSceneSimple()
        {
            
            if (FindObjectOfType<CozyWeather>())
            {
                EditorUtility.DisplayDialog("Cozy:Weather", "You already have a Cozy:Weather system in your scene!", "Ok");
                return;
            }

            if (!Camera.main)
            {
                EditorUtility.DisplayDialog("Cozy:Weather", "You need a main camera in your scene to setup for Cozy:Weather!", "Ok");
                return;
            }

            if (FindObjectsOfType<Light>().Length != 0)
                foreach (Light i in FindObjectsOfType<Light>())
                {

                    if (i.type == LightType.Directional)
                        if (EditorUtility.DisplayDialog("You already have a directional light!", "Do you want to delete " + i.gameObject.name + "? Cozy:Weather will properly light your scene", "Delete", "Keep this light"))
                            DestroyImmediate(i.gameObject);

                }
            if (!Camera.main.GetComponent<FlareLayer>())
                Camera.main.gameObject.AddComponent<FlareLayer>();



#if UNITY_POST_PROCESSING_STACK_V2

            if (!FindObjectOfType<UnityEngine.Rendering.PostProcessing.PostProcessVolume>())
            {
                List<string> path = new List<string>();
                path.Add("Assets/Distant Lands/Cozy Weather/Post FX/");


                GameObject i = new GameObject();

                i.name = "Post FX Volume";
                i.AddComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>().profile = GetAssets<UnityEngine.Rendering.PostProcessing.PostProcessProfile>(path.ToArray(), "Post FX")[0];
                i.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>().isGlobal = true;
                i.layer = 1;




                if (!Camera.main.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>())
                    Camera.main.gameObject.AddComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>().volumeLayer = 2;
            }
#endif


            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;

            GameObject weatherSphere = Instantiate(Resources.Load("Cozy Prefabs/Empty Weather Sphere Reference") as GameObject);

            weatherSphere.name = "Cozy Weather Sphere";


        }
        
        [MenuItem("Distant Lands/Cozy/Setup Scene", false, 1)]
        public static void CozySetupScene()
        {
            
            if (FindObjectOfType<CozyWeather>())
            {
                EditorUtility.DisplayDialog("Cozy:Weather", "You already have a Cozy:Weather system in your scene!", "Ok");
                return;
            }

            if (!Camera.main)
            {
                EditorUtility.DisplayDialog("Cozy:Weather", "You need a main camera in your scene to setup for Cozy:Weather!", "Ok");
                return;
            }

            if (FindObjectsOfType<Light>().Length != 0)
                foreach (Light i in FindObjectsOfType<Light>())
                {

                    if (i.type == LightType.Directional)
                        if (EditorUtility.DisplayDialog("You already have a directional light!", "Do you want to delete " + i.gameObject.name + "? Cozy:Weather will properly light your scene", "Delete", "Keep this light"))
                            DestroyImmediate(i.gameObject);

                }
            if (!Camera.main.GetComponent<FlareLayer>())
                Camera.main.gameObject.AddComponent<FlareLayer>();



#if UNITY_POST_PROCESSING_STACK_V2

            if (!FindObjectOfType<UnityEngine.Rendering.PostProcessing.PostProcessVolume>())
            {
                List<string> path = new List<string>();
                path.Add("Assets/Distant Lands/Cozy Weather/Post FX/");


                GameObject i = new GameObject();

                i.name = "Post FX Volume";
                i.AddComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>().profile = GetAssets<UnityEngine.Rendering.PostProcessing.PostProcessProfile>(path.ToArray(), "Post FX")[0];
                i.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>().isGlobal = true;
                i.layer = 1;




                if (!Camera.main.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>())
                    Camera.main.gameObject.AddComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>().volumeLayer = 2;
            }
#endif


            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;

            GameObject weatherSphere = Instantiate(Resources.Load("Cozy Prefabs/Cozy Weather Sphere") as GameObject);

            weatherSphere.name = "Cozy Weather Sphere";


        }

        

        public static List<T> GetAssets<T>(string[] _foldersToSearch, string _filter) where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets(_filter, _foldersToSearch);
            List<T> a = new List<T>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                a.Add(AssetDatabase.LoadAssetAtPath<T>(path));
            }
            return a;
        }

    }
}