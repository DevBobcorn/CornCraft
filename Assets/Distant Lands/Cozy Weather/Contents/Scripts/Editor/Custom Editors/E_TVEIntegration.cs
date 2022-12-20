using UnityEditor;
using UnityEngine;

namespace DistantLands.Cozy.EditorScripts
{

    [CustomEditor(typeof(CozyTVEModule))]
    [CanEditMultipleObjects]
    public class E_TVEIntegration : E_CozyModule
    {
        SerializedProperty updateFrequency;
        CozyTVEModule module;


        void OnEnable()
        {

        }

        public override GUIContent GetGUIContent()
        {

            return new GUIContent("    TVE Control", (Texture)Resources.Load("Integration"), "Links the COZY system with the vegetation engine by BOXOPHOBIC.");

        }

        public override void DisplayInCozyWindow()
        {
            serializedObject.Update();

            if (module == null)
                module = (CozyTVEModule)target;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("updateFrequency"));
            serializedObject.ApplyModifiedProperties();

#if THE_VEGETATION_ENGINE
            if (!module.globalControl || !module.globalMotion)
            {
                EditorGUILayout.Space(20);
                EditorGUILayout.HelpBox("Make sure that you have active TVE Global Motion and TVE Global Control objects in your scene!", MessageType.Warning);

            }
#else
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox("The Vegetation Engine is not currently in this project! Please make sure that it has been properly downloaded before using this module.", MessageType.Warning);

#endif
        }
    }
}
