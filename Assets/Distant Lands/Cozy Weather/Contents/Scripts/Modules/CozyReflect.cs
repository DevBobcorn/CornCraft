using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy
{
    [ExecuteAlways]
    public class CozyReflect : CozyModule
    {

        public Cubemap reflectionCubemap;
        public Camera reflectionCamera;
        [Tooltip("How many frames should pass before the cubemap renders again? A value of 0 renders every frame and a value of 30 renders once every 30 frames.")]
        [Range(0, 30)]
        public int framesBetweenRenders = 10;
        [Tooltip("What layers should be rendered into the skybox reflections?.")]
        public LayerMask layerMask = 2048;
        int framesLeft;

        void OnEnable()
        {

            base.SetupModule();
            reflectionCubemap = Resources.Load("Materials/Reflection Cubemap") as Cubemap;
            RenderSettings.customReflection = reflectionCubemap;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            weatherSphere.fogMesh.gameObject.layer = ToLayer(layerMask);
            weatherSphere.skyMesh.gameObject.layer = ToLayer(layerMask);
            weatherSphere.cloudMesh.gameObject.layer = ToLayer(layerMask);

        }
        
        void Update()
        {
            if (weatherSphere == null)
            {
                base.SetupModule();
            }

            if (framesLeft < 0)
            {

                RenderReflections();
                framesLeft = framesBetweenRenders;

            }

            framesLeft--;
        }

        public int ToLayer(LayerMask mask)
        {
            int value = mask.value;
            if (value == 0)
            {
                return 0;
            }
            for (int l = 1; l < 32; l++)
            {
                if ((value & (1 << l)) != 0)
                {
                    return l;
                }
            }
            return -1;
        }

        void OnDisable()
        {

            if (reflectionCamera)
                DestroyImmediate(reflectionCamera.gameObject);

            RenderSettings.customReflection = null;

        }

        public void RenderReflections()
        {

            if (!weatherSphere.cozyCamera)
            {
                Debug.LogError("COZY Reflections requires the cozy camera to be set in the settings tab!");
                return;
            }

            if (reflectionCamera == null)
                SetupCamera();

            reflectionCamera.enabled = true;
            reflectionCamera.transform.position = transform.position;
            reflectionCamera.nearClipPlane = weatherSphere.cozyCamera.nearClipPlane;
            reflectionCamera.farClipPlane = weatherSphere.cozyCamera.farClipPlane;
            reflectionCamera.cullingMask = layerMask;


            reflectionCamera.RenderToCubemap(reflectionCubemap);
            reflectionCamera.enabled = false;

        }

        public void SetupCamera()
        {


            GameObject i = new GameObject();
            i.name = "COZY Reflection Camera";
            i.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.HideInHierarchy;

            reflectionCamera = i.AddComponent<Camera>();
            reflectionCamera.depth = -50;
            reflectionCamera.enabled = false;

        }

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CozyReflect))]
    [CanEditMultipleObjects]
    public class E_CozyReflect : E_CozyModule
    {

        CozyReflect reflect;

        public override GUIContent GetGUIContent()
        {

            //Place your module's GUI content here.
            return new GUIContent("    Reflections", (Texture)Resources.Load("Reflections"), "Sets up a cubemap for reflections with COZY.");

        }

        void OnEnable()
        {

        }

        public override void DisplayInCozyWindow()
        {

            if (reflect == null)
                reflect = (CozyReflect)target;

            serializedObject.Update();


            EditorGUILayout.PropertyField(serializedObject.FindProperty("framesBetweenRenders"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("reflectionCubemap"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("layerMask"));


            serializedObject.ApplyModifiedProperties();

        }

    }
#endif
}