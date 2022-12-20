using UnityEditor;
using UnityEngine;

namespace DistantLands.Cozy.EditorScripts
{

    [CustomEditor(typeof(CozyAmbienceManager))]
    [CanEditMultipleObjects]
    public class E_AmbienceManager : E_CozyModule
    {


        protected static bool profileSettings;
        protected static bool currentInfo;
        CozyAmbienceManager ambienceManager;


        public override GUIContent GetGUIContent()
        {

            return new GUIContent("    Ambience", (Texture)Resources.Load("Ambience Profile"), "Controls a secondary weather system that runs parallel to the main system allowing for ambient noises and FX.");

        }

        void OnEnable()
        {

            if (target)
                ambienceManager = (CozyAmbienceManager)target;



        }

        public override void OnInspectorGUI()
        {

        }
        public override void DisplayInCozyWindow()
        {
            serializedObject.Update();

            if (ambienceManager == null)
                if (target)
                    ambienceManager = (CozyAmbienceManager)target;
                else
                    return;

            profileSettings = EditorGUILayout.BeginFoldoutHeaderGroup(profileSettings, "    Forecast Settings", EditorUtilities.FoldoutStyle());
            EditorGUI.EndFoldoutHeaderGroup();
            if (profileSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ambienceProfiles"));
                EditorGUILayout.Space();
                if (GUILayout.Button("Add all ambience profiles"))
                    ambienceManager.FindAllAmbiences();
                EditorGUI.indentLevel--;

            }


            currentInfo = EditorGUILayout.BeginFoldoutHeaderGroup(currentInfo, "    Current Information", EditorUtilities.FoldoutStyle());
            EditorGUI.EndFoldoutHeaderGroup();
            if (currentInfo)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("currentAmbienceProfile"));
                if (Application.isPlaying)
                    EditorGUILayout.HelpBox(ambienceManager.currentAmbienceProfile.name + " will be playing for the next " + Mathf.Round(ambienceManager.GetTimeTillNextAmbience()) + " ticks.", MessageType.None, true);

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}