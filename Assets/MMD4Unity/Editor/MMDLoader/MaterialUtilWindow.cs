using UnityEngine;
using UnityEditor;

namespace MMD
{
    public class MaterialUtilWindow : EditorWindow
    {
        private Color HairHigh = Color.grey, HairDark = Color.black;
        private Color ClthHigh = Color.grey, ClthDark = Color.black;
        private Color BodyHigh = Color.grey, BodyDark = Color.black;
        private Color FaceHigh = Color.grey, FaceDark = Color.black;

        private GameObject target = null;

        [MenuItem("MMD for Unity/MFU Material Util")]
        static void Init()
        {
            var window = GetWindow<MaterialUtilWindow>(false, "MFU Material Util");
            window.Show();
        }

        void HandleTargetChange(GameObject newTarget)
        {
            target = newTarget;
        }

        void DetectTargetChange()
        {
            var newTarget = Selection.activeObject as GameObject;

            if (newTarget != target)
            {
                HandleTargetChange(newTarget);

                Repaint();
            }
        }

        void OnFocus()
        {
            DetectTargetChange();
        }

        void OnSelectionChange()
        {
            DetectTargetChange();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Target Selection", EditorStyles.boldLabel);

            GUI.enabled = false; // Draw the object to make it clear who's the current target

            EditorGUILayout.ObjectField("Target", target, typeof (GameObject), true);

            GUI.enabled = true;

            GUILayout.Space(10);

            if (target == null)
            {
                GUILayout.Label("Please select a valid target!");
            }
            else
            {
                EditorGUILayout.LabelField("Cell Lighting", EditorStyles.boldLabel);

                HairHigh = EditorGUILayout.ColorField("Hair High", HairHigh);
                HairDark = EditorGUILayout.ColorField("Hair Dark", HairDark);

                GUILayout.Button("Apply to Hair");

                ClthHigh = EditorGUILayout.ColorField("Clothes High", ClthHigh);
                ClthDark = EditorGUILayout.ColorField("Clothes Dark", ClthDark);

                GUILayout.BeginHorizontal();
                GUILayout.Button("Apply to [Metal Parts]");
                GUILayout.Button("Apply to [Non-Metal Parts]");
                GUILayout.Button("Apply to Clothes");
                GUILayout.EndHorizontal();

                FaceHigh = EditorGUILayout.ColorField("Face High", FaceHigh);
                FaceDark = EditorGUILayout.ColorField("Face Dark", FaceDark);

                GUILayout.Button("Apply to Face");

                BodyHigh = EditorGUILayout.ColorField("Body High", BodyHigh);
                BodyDark = EditorGUILayout.ColorField("Body Dark", BodyDark);

                GUILayout.BeginHorizontal();
                GUILayout.Button("Apply to Body");
                GUILayout.Button("Apply to Body & Face");
                GUILayout.EndHorizontal();

                GUILayout.Button("Apply to All Cell Shader Materials");
            }
        }
    }
}
