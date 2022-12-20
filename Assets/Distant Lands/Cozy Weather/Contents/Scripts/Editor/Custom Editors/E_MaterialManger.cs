using UnityEditor;
using UnityEngine;
using DistantLands.Cozy.Data;

namespace DistantLands.Cozy.EditorScripts
{

    [CustomEditor(typeof(CozyMaterialManager))]
    [CanEditMultipleObjects]
    public class E_MaterialManger : E_CozyModule
    {

        CozyMaterialManager materialManager;
        protected static bool profileSettings;
        protected static bool settings;
        SerializedObject so;


        void OnEnable()
        {


        }

        public override void OnInspectorGUI()
        {

        }

        public override GUIContent GetGUIContent()
        {

            return new GUIContent("    Materials", (Texture)Resources.Load("MaterialManager"), "Manages the materials that are affected by the COZY system.");

        }

        public override void DisplayInCozyWindow()
        {
            serializedObject.Update();

            if (materialManager == null)
                if (target)
                {
                    materialManager = (CozyMaterialManager)target;
                    so = new SerializedObject(materialManager.profile);

                }
                else
                    return;

            materialManager = (CozyMaterialManager)target;


            if (serializedObject.FindProperty("profile").objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Make sure that you have all of the necessary profile references!", MessageType.Error);
            }

            profileSettings = EditorGUILayout.BeginFoldoutHeaderGroup(profileSettings, "    Profile Settings", EditorUtilities.FoldoutStyle());
            EditorGUI.EndFoldoutHeaderGroup();
            if (profileSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("profile"));
                EditorGUILayout.Space();
                so.ApplyModifiedProperties();
                EditorGUI.indentLevel--;

            }

            if (materialManager.profile)
                (CreateEditor(materialManager.profile) as E_MaterialProfile).DisplayInCozyWindow();

            settings = EditorGUILayout.BeginFoldoutHeaderGroup(settings, "    Options", EditorUtilities.FoldoutStyle());
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (settings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SnowAmount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SnowMeltSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Wetness"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DryingSpeed"));
                EditorGUI.indentLevel--;
            }



            serializedObject.ApplyModifiedProperties();


        }
    }
}