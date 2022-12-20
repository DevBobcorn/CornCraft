using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy
{
    [System.Serializable]
    public class CozyPostFXManager : CozyFXModule
    {
        public override void OnFXEnable()
        {

        }

        public override void OnFXUpdate()
        {

            if (!isEnabled)
                return;

            if (vfx == null)
                vfx = CozyWeather.instance.GetModule<VFXModule>();

            if (parent == null)
                SetupFXParent();
            else if (parent.parent == null)
                parent.parent = vfx.parent;
                
            parent.transform.localPosition = Vector3.zero;

        }

        public override void OnFXDisable()
        {

            if (parent)
                MonoBehaviour.DestroyImmediate(parent.gameObject);

        }

        public override void SetupFXParent()
        {

            if (vfx.parent == null)
                return;

            parent = new GameObject().transform;
            parent.parent = vfx.parent;
            parent.localPosition = Vector3.zero;
            parent.localScale = Vector3.one;
            parent.name = "Post FX";
            parent.gameObject.AddComponent<FXParent>();
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(CozyPostFXManager))]
    public class CozyPostFXManagerDrawer : PropertyDrawer
    {


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            EditorGUI.BeginProperty(position, label, property);

            Rect pos = position;

            Rect tabPos = new Rect(pos.x + 35, pos.y, pos.width - 41, pos.height);
            Rect togglePos = new Rect(5, pos.y, 30, pos.height);

            property.FindPropertyRelative("_OpenTab").boolValue = EditorGUI.BeginFoldoutHeaderGroup(tabPos, property.FindPropertyRelative("_OpenTab").boolValue, new GUIContent("    Post Processing FX", "Post Processing FX overrides the current post processing settings based on a custom volume. Requires the Post Processing package in BIRP."), EditorUtilities.FoldoutStyle());

            bool toggle = EditorGUI.Toggle(togglePos, GUIContent.none, property.FindPropertyRelative("_IsEnabled").boolValue);

            if (property.FindPropertyRelative("_IsEnabled").boolValue != toggle)
            {
                property.FindPropertyRelative("_IsEnabled").boolValue = toggle;

                if (toggle == true)
                    (property.serializedObject.targetObject as VFXModule).postFXManager.OnFXEnable();
                else
                    (property.serializedObject.targetObject as VFXModule).postFXManager.OnFXDisable();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();


            if (property.FindPropertyRelative("_OpenTab").boolValue)
            {
                using (new EditorGUI.DisabledScope(!property.FindPropertyRelative("_IsEnabled").boolValue))
                {

                    EditorGUILayout.Space();
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