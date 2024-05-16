#if URP
using System;
using UnityEditor;
using UnityEngine;

namespace StylizedWater2
{
    [CustomEditor(typeof(StylizedWaterRenderFeature))]
    public class RenderFeatureEditor : Editor
    {
        private SerializedProperty screenSpaceReflectionSettings;
        
        private SerializedProperty directionalCaustics;
        
        private SerializedProperty displacementPrePassSettings;

        private void OnEnable()
        {
            screenSpaceReflectionSettings = serializedObject.FindProperty("screenSpaceReflectionSettings");
            
            directionalCaustics = serializedObject.FindProperty("directionalCaustics");
            
            displacementPrePassSettings = serializedObject.FindProperty("displacementPrePassSettings");
        }
        
        public override void OnInspectorGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Version {AssetInfo.INSTALLED_VERSION}", EditorStyles.miniLabel);

                if (GUILayout.Button(new GUIContent(" Documentation", EditorGUIUtility.FindTexture("_Help"))))
                {
                    Application.OpenURL(AssetInfo.DOC_URL);
                }
            }
            EditorGUILayout.Space();
            
            UI.DrawRenderGraphError();
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(directionalCaustics);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(screenSpaceReflectionSettings);
            if(screenSpaceReflectionSettings.isExpanded) EditorGUILayout.HelpBox("This feature is available for preview, no configurable settings are available yet", MessageType.Info);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(displacementPrePassSettings);
            if (displacementPrePassSettings.isExpanded)
            {
                EditorGUILayout.HelpBox("This will pre-render all the water geometry's height (including any displacement effects) into a buffer. Allowing other shaders to access this information." +
                                        "\n\nSee the Displacement.hlsl shader library for the API, or use the \"Sample Water Height\" Sub-graph in Shader Graph." +
                                        "\n\nThis is for advanced users, there is currently no functionality in Stylized Water 2 that makes use of this.", MessageType.Info);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
            
            UI.DrawFooter();
        }
    }
}
#endif