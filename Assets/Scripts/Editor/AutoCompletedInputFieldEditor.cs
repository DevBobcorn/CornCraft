using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using TMPro.EditorUtilities;

namespace CraftSharp.UI.Editor
{
    [CustomEditor(typeof (AutoCompletedInputField), true), CanEditMultipleObjects]
    public class AutoCompletedInputFieldEditor : TMP_InputFieldEditor
    {
        SerializedProperty autoCompleteCanvasGroup;
        SerializedProperty autoCompleteOptionsText;
        SerializedProperty inputFieldTextGhost;

        protected override void OnEnable()
        {
            base.OnEnable();

            autoCompleteCanvasGroup = serializedObject.FindProperty("autoCompleteCanvasGroup");
            autoCompleteOptionsText = serializedObject.FindProperty("autoCompleteOptionsText");
            inputFieldTextGhost = serializedObject.FindProperty("inputFieldTextGhost");
        }

        /// <summary>
        /// Draw the standard custom inspector
        /// </summary>
        override public void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            // AUTO COMPLETE CANVAS GROUP
            EditorGUILayout.PropertyField(autoCompleteCanvasGroup);

            // AUTO COMPLETE OPTIONS TEXT
            EditorGUILayout.PropertyField(autoCompleteOptionsText);

            // INPUT FIELD TEXT GHOST
            EditorGUILayout.PropertyField(inputFieldTextGhost);

            serializedObject.ApplyModifiedProperties();
        }
    }
}