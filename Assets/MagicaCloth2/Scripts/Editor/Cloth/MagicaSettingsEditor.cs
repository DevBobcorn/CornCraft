// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;

namespace MagicaCloth2
{
    [CustomEditor(typeof(MagicaSettings))]
    [CanEditMultipleObjects]
    public class MagicaSettingsEditor : Editor
    {
        //=========================================================================================
        /// <summary>
        /// インスペクターGUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            var comp = target as MagicaSettings;

            serializedObject.Update();
            Undo.RecordObject(comp, "MagicaSettings");
            SettingsInspector();
            serializedObject.ApplyModifiedProperties();
        }

        void SettingsInspector()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("refreshMode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("simulationFrequency"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSimulationCountPerFrame"));
        }
    }
}
