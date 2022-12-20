using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy
{
    [System.Serializable]
    public class CozyFilterManager : CozyFXModule
    {

        public override void OnFXEnable()
        {

        }

        public override void OnFXUpdate()
        {

            if (!isEnabled)
                return;

        }

        public override void OnFXDisable()
        {



        }

        public override void SetupFXParent()
        {
            


        }
    }
    
#if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(CozyFilterManager))]
    public class FilterManagerDrawer : PropertyDrawer
    {


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            EditorGUI.BeginProperty(position, label, property);

            Rect pos = position;

            Rect tabPos = new Rect(pos.x + 35, pos.y, pos.width - 41, pos.height);
            Rect togglePos = new Rect(5, pos.y, 30, pos.height);

            property.FindPropertyRelative("_OpenTab").boolValue = EditorGUI.BeginFoldoutHeaderGroup(tabPos, property.FindPropertyRelative("_OpenTab").boolValue, new GUIContent("    Filter FX", "Filter FX recolor clouds, sunlight, and fog based on an HSV adjustment and several color filters."), EditorUtilities.FoldoutStyle());

            bool toggle = EditorGUI.Toggle(togglePos, GUIContent.none, property.FindPropertyRelative("_IsEnabled").boolValue);

            if (property.FindPropertyRelative("_IsEnabled").boolValue != toggle)
            {
                property.FindPropertyRelative("_IsEnabled").boolValue = toggle;

                if (toggle == true)
                    (property.serializedObject.targetObject as VFXModule).filterManager.OnFXEnable();
                else
                    (property.serializedObject.targetObject as VFXModule).filterManager.OnFXDisable();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();


            if (property.FindPropertyRelative("_OpenTab").boolValue)
            {
                using (new EditorGUI.DisabledScope(!property.FindPropertyRelative("_IsEnabled").boolValue))
                {

                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox("No additional values to tweak here!", MessageType.Info);
                    EditorGUI.indentLevel--;

                }

            }


            EditorGUI.EndProperty();
        }

    }
#endif
}