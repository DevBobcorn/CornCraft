using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#if URP
using UnityEngine.Rendering.Universal;
#endif

namespace StylizedWater2
{
    [CustomEditor(typeof(WaterObject))]
    [CanEditMultipleObjects]
    public class WaterObjectInspector : Editor
    {
        private WaterObject component;

        private SerializedProperty material;
        private SerializedProperty meshFilter;
        private SerializedProperty meshRenderer;
        
        private bool depthTextureRequired;
        private bool opaqueTextureRequired;

        private bool showInstances
        {
            get => SessionState.GetBool("WATEROBJECT_SHOW_INSTANCES", false);
            set => SessionState.SetBool("WATEROBJECT_SHOW_INSTANCES", value);
        }
        
        private Texture icon;
        
        private void OnEnable()
        {
            component = (WaterObject)target;

            icon = Resources.Load<Texture>("water-object-icon");
            
            material = serializedObject.FindProperty("material");
            meshFilter = serializedObject.FindProperty("meshFilter");
            meshRenderer = serializedObject.FindProperty("meshRenderer");

            CheckMaterial();
        }

        private void CheckMaterial()
        {
            #if URP
            if (UniversalRenderPipeline.asset == null || component.material == null) return;

            depthTextureRequired = UniversalRenderPipeline.asset.supportsCameraDepthTexture == false && component.material.GetFloat("_DisableDepthTexture") == 0f;
            opaqueTextureRequired = UniversalRenderPipeline.asset.supportsCameraOpaqueTexture == false && component.material.GetFloat("_RefractionOn") == 1f;
            #endif
        }
        
        public override void OnInspectorGUI()
        {
            #if URP
            if (UniversalRenderPipeline.asset)
            {
                UI.DrawNotification(
                    depthTextureRequired,
                    "Depth texture is disabled, but is required for the water material",
                    "Enable",
                    () =>
                    {
                        StylizedWaterEditor.EnableDepthTexture();
                        CheckMaterial();
                    },
                    MessageType.Error);
                
                UI.DrawNotification(
                    opaqueTextureRequired,
                    "Opaque texture is disabled, but is required for the water material",
                    "Enable",
                    () =>
                    {
                        StylizedWaterEditor.EnableOpaqueTexture();
                        CheckMaterial();
                    },
                    MessageType.Error);
            }
            #endif
            
            EditorGUILayout.HelpBox("This component provides a means for other scripts to identify and find water bodies", MessageType.None);
            
            EditorGUILayout.LabelField("References (Read only)", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(true);
            {
                EditorGUILayout.PropertyField(material);
                EditorGUILayout.PropertyField(meshFilter);
                EditorGUILayout.PropertyField(meshRenderer);
            }
            EditorGUI.EndDisabledGroup();

            //In case the material was changed on the attached Mesh Renderer, reflect the change
            foreach (Object currentTarget in targets)
            {
                WaterObject water = (WaterObject)currentTarget;
                water.FetchWaterMaterial();
            }

            if (WaterObject.Instances.Count > 1)
            {
                EditorGUILayout.Space();
                
                showInstances = EditorGUILayout.BeginFoldoutHeaderGroup(showInstances, $"Instances ({WaterObject.Instances.Count})");

                if (showInstances)
                {
                    this.Repaint();

                    using (new EditorGUILayout.VerticalScope(EditorStyles.textArea))
                    {
                        foreach (WaterObject obj in WaterObject.Instances)
                        {
                            var rect = EditorGUILayout.BeginHorizontal(EditorStyles.miniLabel);

                            if (rect.Contains(Event.current.mousePosition))
                            {
                                EditorGUIUtility.AddCursorRect(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 27, 27), MouseCursor.Link);
                                EditorGUI.DrawRect(rect, Color.gray * (EditorGUIUtility.isProSkin ? 0.66f : 0.20f));
                            }

                            if (GUILayout.Button(new GUIContent(" " + obj.name, icon), EditorStyles.miniLabel, GUILayout.Height(20f)))
                            {
                                EditorGUIUtility.PingObject(obj);
                                Selection.activeGameObject = obj.gameObject;
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }
    }
}