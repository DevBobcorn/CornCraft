using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.FernRenderPipeline;

namespace UnityEditor.Rendering.FernRenderPipeline
{
    [CustomEditor(typeof(FernRP))]
    public class FernRPEditor : Editor
    {
        FernRP script;
        private SerializedProperty renderPipelineAsset;

        private void OnEnable()
        {
            script = (FernRP)target;

            renderPipelineAsset = serializedObject.FindProperty("renderPipelineAsset");
            OnEnableGrass();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            
            EditorGUI.BeginChangeCheck();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(renderPipelineAsset);

                if (GraphicsSettings.renderPipelineAsset != script.renderPipelineAsset)
                {
                    if (GUILayout.Button("Apply", GUILayout.MaxWidth(70f)))
                    {
                        script.ApplyRenderPipeline();
                    }
                }
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            OnInspectorGUIGrass();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
            
#if FERN_ENV_GRASS
    
        private SerializedProperty fernGrassProperty;

        private void OnEnableGrass()
        {
            // Search GrassRender
            script.fernGrassRender = FindAnyObjectByType<FernGrassRender>();
            fernGrassProperty = serializedObject.FindProperty("fernGrassRender");
        }

        private void OnInspectorGUIGrass()
        {
            if (script.fernGrassRender == null)
            {
                FernRenderGUIUtility.DrawActionBox("Fern Grass Render is Missing, Create One, If you want to use Grass Render, you can create one","Create", MessageType.Warning, CreateFernGrassRender);
            }
            
            else
            {
                EditorGUILayout.PropertyField(fernGrassProperty);
            }
        }

        private void CreateFernGrassRender()
        {
            script.fernGrassRender = FindAnyObjectByType<FernGrassRender>();
            if (script.fernGrassRender != null) return;
            var fernGrassRenderGameObject = new GameObject("Fern Grass Render");
            fernGrassRenderGameObject.transform.SetParent(script.transform);
            script.fernGrassRender = fernGrassRenderGameObject.AddComponent<FernGrassRender>();
            fernGrassProperty = serializedObject.FindProperty("fernGrassRender");
        }
#else
        private void OnEnableGrass()
        {
        }
        
        private void OnInspectorGUIGrass()
        {
        }
#endif
    }
}