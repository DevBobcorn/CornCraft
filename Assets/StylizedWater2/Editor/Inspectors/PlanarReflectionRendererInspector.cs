using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace StylizedWater2
{
    [CustomEditor(typeof(PlanarReflectionRenderer))]
    public class PlanarReflectionRendererInspector : Editor
    {
        private PlanarReflectionRenderer renderer;
        
        //Rendering
        private SerializedProperty rotatable;
        private SerializedProperty cullingMask;
        private SerializedProperty rendererIndex;
        private SerializedProperty offset;
        private SerializedProperty includeSkybox;
        private SerializedProperty enableFog;
        
        //Quality
        private SerializedProperty renderShadows;
        private SerializedProperty renderRange;
        private SerializedProperty renderScale;
        private SerializedProperty maximumLODLevel;
        
        private SerializedProperty waterObjects;
        private SerializedProperty moveWithTransform;

        private Bounds curBounds;
        private bool waterLayerError;

        private bool previewReflection
        {
            get => EditorPrefs.GetBool("SWS2_PREVIEW_REFLECTION_ENABLED", true);
            set => EditorPrefs.SetBool("SWS2_PREVIEW_REFLECTION_ENABLED", value);
        }
        private RenderTexture previewTexture;

#if URP
        private void OnEnable()
        {
            PipelineUtilities.RefreshRendererList();
            
            renderer = (PlanarReflectionRenderer)target;

            rotatable = serializedObject.FindProperty("rotatable");
            cullingMask = serializedObject.FindProperty("cullingMask");
            rendererIndex = serializedObject.FindProperty("rendererIndex");
            offset = serializedObject.FindProperty("offset");
            includeSkybox = serializedObject.FindProperty("includeSkybox");
            enableFog = serializedObject.FindProperty("enableFog");
            renderShadows = serializedObject.FindProperty("renderShadows");
            renderRange = serializedObject.FindProperty("renderRange");
            renderScale = serializedObject.FindProperty("renderScale");
            maximumLODLevel = serializedObject.FindProperty("maximumLODLevel");
            waterObjects = serializedObject.FindProperty("waterObjects");
            moveWithTransform = serializedObject.FindProperty("moveWithTransform");
            
            if (renderer.waterObjects.Count == 0 && WaterObject.Instances.Count == 1)
            {
                renderer.waterObjects.Add(WaterObject.Instances[0]);
                renderer.RecalculateBounds();
                renderer.EnableMaterialReflectionSampling();
                
                EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            ValidateWaterObjectLayer();

            curBounds = renderer.CalculateBounds();

            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private Camera currentCamera;
        private string currentCameraName;
        private bool waterObjectsVisible;
        
        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!previewReflection) return;

            if (PlanarReflectionRenderer.InvalidContext(camera)) return;

            currentCamera = camera;
            
            waterObjectsVisible = renderer.WaterObjectsVisible(currentCamera);
            
            previewTexture = renderer.TryGetReflectionTexture(currentCamera);
            currentCameraName = currentCamera.name;
        }
        
        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }
#endif

        public override void OnInspectorGUI()
        {
#if !URP
            UI.DrawNotification("The Universal Render Pipeline package v" + AssetInfo.MIN_URP_VERSION + " or newer is not installed", MessageType.Error);
#else
            UI.DrawHeader();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.labelWidth);
                previewReflection =
                    GUILayout.Toggle(previewReflection, new GUIContent("  Preview reflection", EditorGUIUtility.IconContent(
                        (previewReflection ? "animationvisibilitytoggleon" : "animationvisibilitytoggleoff")).image), "Button");
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.labelWidth);
                EditorGUILayout.LabelField("Status: " + (waterObjectsVisible && currentCamera ? $"Rendering (camera: {currentCamera.name})" : "Not rendering (water not in view for any camera)"), EditorStyles.miniLabel);
            }
            
            UI.DrawNotification(PipelineUtilities.VREnabled(), "Not supported with VR rendering", MessageType.Error);
            
            UI.DrawNotification(PlanarReflectionRenderer.AllowReflections == false, "Reflections have been globally disabled by an external script", MessageType.Warning);
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            UI.DrawRendererProperty(rendererIndex);
            if (EditorGUI.EndChangeCheck())
            {
                renderer.SetRendererIndex(rendererIndex.intValue);
            }
            
            //Default renderer
            if (rendererIndex.intValue == 0)
            {
                UI.DrawNotification("\n" +
                                        "Using the default renderer for reflections is strongly discouraged." +
                                        "\n\nMost (if not all) render features, such as third-party post processing effects, will also render for the reflection." +
                                        "\n\nThis can lead to rendering artefacts and negatively impacts overall performance." +
                                        "\n", MessageType.Warning);
                
                //If there are no other renderers to assign, suggest to auto-create one
                UI.DrawNotification(PipelineUtilities.rendererIndexList.Length <= 2, "It is highly recommend to create a separate empty renderer", "Create and assign", CreateRenderer, MessageType.None);
                
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.PropertyField(cullingMask);
            
            EditorGUILayout.PropertyField(includeSkybox);
            EditorGUILayout.PropertyField(enableFog);
            
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(rotatable);
            EditorGUILayout.PropertyField(offset);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Quality", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(renderShadows);
            if (EditorGUI.EndChangeCheck())
            {
                renderer.ToggleShadows(renderShadows.boolValue);
            }
            EditorGUILayout.PropertyField(renderRange);
            EditorGUILayout.PropertyField(renderScale);
            EditorGUILayout.PropertyField(maximumLODLevel);
            
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Target water objects", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(moveWithTransform, new GUIContent("Move bounds with transform", moveWithTransform.tooltip));

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(waterObjects);
            if (EditorGUI.EndChangeCheck())
            {
                curBounds = renderer.CalculateBounds();
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if(GUILayout.Button(new GUIContent("Auto-find", "Assigns all active water objects currently in the scene"), EditorStyles.miniButton))
                {
                    renderer.waterObjects = new List<WaterObject>(WaterObject.Instances);
 
                    renderer.RecalculateBounds();
                    curBounds = renderer.bounds;
                    renderer.EnableMaterialReflectionSampling();

                    ValidateWaterObjectLayer();
                    
                    EditorUtility.SetDirty(target);
                }
                if(GUILayout.Button("Clear", EditorStyles.miniButton))
                {
                    renderer.ToggleMaterialReflectionSampling(false);
                    renderer.waterObjects.Clear();
                    renderer.RecalculateBounds();
                    
                    EditorUtility.SetDirty(target);
                }
            }
            
            if (renderer.waterObjects != null)
            {
                UI.DrawNotification(renderer.waterObjects.Count == 0, "Assign at least one Water Object", MessageType.Info);
                
                if (renderer.waterObjects.Count > 0)
                {
                    UI.DrawNotification(curBounds.size != renderer.bounds.size || (moveWithTransform.boolValue == false && curBounds.center != renderer.bounds.center), "Water objects have changed or moved, bounds needs to be recalculated", "Recalculate",() => RecalculateBounds(), MessageType.Error);
                }
                
                UI.DrawNotification(waterLayerError, "One or more Water Objects aren't on the \"Water\" layer.\n\nThis causes recursive reflections", "Fix", () => SetObjectsOnWaterLayer(), MessageType.Error);
            }

#endif
            
            UI.DrawFooter();
        }
        
#if URP

        private void CreateRenderer()
        {
            int index = -1;
            string path = "";

            PipelineUtilities.CreateAndAssignNewRenderer(out index, out path);

            if (index >= 0)
            {
                rendererIndex.intValue = index;

                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                
                renderer.SetRendererIndex(rendererIndex.intValue);

                if (path != string.Empty)
                {
                    Debug.Log("New renderer created at path <i>" + path + "</i>");
                }
            }
        }
        
        public override bool HasPreviewGUI()
        {
            return previewReflection && previewTexture;
        }
        
        public override bool RequiresConstantRepaint()
        {
            return HasPreviewGUI();
        }

        public override GUIContent GetPreviewTitle()
        {
            return currentCamera ? new GUIContent(currentCameraName + " reflection") : new GUIContent("Reflection");
        }

        public override void OnPreviewSettings()
        {
            if (HasPreviewGUI() == false) return;

            GUILayout.Label($"Resolution ({previewTexture.width}x{previewTexture.height})");
        }

        private bool drawAlpha;

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (drawAlpha)
            {
                EditorGUI.DrawTextureAlpha(r, previewTexture, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.DrawTexture(r, previewTexture, ScaleMode.ScaleToFit, false);
            }
            
            Rect btnRect = r;
            btnRect.x += 10f;
            btnRect.y += 10f;
            btnRect.width = 150f;
            btnRect.height = 20f;

            drawAlpha = GUI.Toggle(btnRect, drawAlpha, new GUIContent(" Alpha channel"));
        }

        private void ValidateWaterObjectLayer()
        {
            if (renderer.waterObjects == null) return;

            waterLayerError = false;
            int layerID = LayerMask.NameToLayer("Water");

            foreach (WaterObject obj in renderer.waterObjects)
            {
                //Is not on "Water" layer?
                if (obj.gameObject.layer != layerID)
                {
                    waterLayerError = true;
                    return;
                }
            }
        }

        private void SetObjectsOnWaterLayer()
        {
            int layerID = LayerMask.NameToLayer("Water");

            foreach (WaterObject obj in renderer.waterObjects)
            {
                //Is not on "Water" layer?
                if (obj.gameObject.layer != layerID)
                {
                    obj.gameObject.layer = layerID;
                    EditorUtility.SetDirty(obj);
                }
            }
            
            waterLayerError = false;
        }
    #endif

        private void RecalculateBounds()
        {
#if URP
            renderer.RecalculateBounds();
            curBounds = renderer.bounds;
            EditorUtility.SetDirty(target);
#endif
        }
    }
}
