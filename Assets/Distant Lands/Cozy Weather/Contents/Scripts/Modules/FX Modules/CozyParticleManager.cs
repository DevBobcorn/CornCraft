using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy
{
    [System.Serializable]
    public class CozyParticleManager : CozyFXModule
    {
        [Tooltip("Multiply particle emission amounts by this number.")]
        [Range (0, 2)]
        public float multiplier = 1;

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

        public override void SetupFXParent()
        {
            
            if (vfx.parent == null)
                return;

            parent = new GameObject().transform;
            parent.parent = vfx.parent;
            parent.localPosition = Vector3.zero;
            parent.localScale = Vector3.one;
            parent.name = "Particle FX";
            parent.gameObject.AddComponent<FXParent>();
        }

        public override void OnFXDisable()
        {

            if (parent)
                MonoBehaviour.DestroyImmediate(parent.gameObject);

        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(CozyParticleManager))]
    public class CozyParticleManagerDrawer : PropertyDrawer
    {


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            EditorGUI.BeginProperty(position, label, property);

            Rect pos = position;

            Rect tabPos = new Rect(pos.x + 35, pos.y, pos.width - 41, pos.height);
            Rect togglePos = new Rect(5, pos.y, 30, pos.height);

            property.FindPropertyRelative("_OpenTab").boolValue = EditorGUI.BeginFoldoutHeaderGroup(tabPos, property.FindPropertyRelative("_OpenTab").boolValue, new GUIContent("    Particle FX", "Particle FX manage the particles in your scene. For example, rain, snow, and dust."), EditorUtilities.FoldoutStyle());

            bool toggle = EditorGUI.Toggle(togglePos, GUIContent.none, property.FindPropertyRelative("_IsEnabled").boolValue);

            if (property.FindPropertyRelative("_IsEnabled").boolValue != toggle)
            {
                property.FindPropertyRelative("_IsEnabled").boolValue = toggle;

                if (toggle == true)
                    (property.serializedObject.targetObject as VFXModule).particleManager.OnFXEnable();
                else
                    (property.serializedObject.targetObject as VFXModule).particleManager.OnFXDisable();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();


            if (property.FindPropertyRelative("_OpenTab").boolValue)
            {
                using (new EditorGUI.DisabledScope(!property.FindPropertyRelative("_IsEnabled").boolValue))
                {

                    EditorGUILayout.Space();
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("multiplier"));
                    EditorGUI.indentLevel--;

                }

            }


            EditorGUI.EndProperty();
        }

    }
#endif
}