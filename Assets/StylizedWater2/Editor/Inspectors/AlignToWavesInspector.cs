//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if UNITY_2021_2_OR_NEWER
using PrefabStageUtility = UnityEditor.SceneManagement.PrefabStageUtility;
#else
using PrefabStageUtility = UnityEditor.Experimental.SceneManagement.PrefabStageUtility;
#endif

namespace StylizedWater2
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AlignToWaves))]
    public class AlignToWavesInspector : Editor
    {
        AlignToWaves script;

        SerializedProperty waterObject;
        SerializedProperty autoFind;
        SerializedProperty dynamicMaterial;
        SerializedProperty waterLevelSource;
        SerializedProperty waterLevel;
        SerializedProperty childTransform;

        SerializedProperty heightOffset;
        SerializedProperty rollAmount;

        SerializedProperty samples;

        private bool editSamples;
        private bool isRiver;
        private bool wavesEnabled;
        
        private string proSkinPrefix => EditorGUIUtility.isProSkin ? "d_" : "";
        
        private void OnEnable()
        {
            script = (AlignToWaves)target;

            waterObject = serializedObject.FindProperty("waterObject");
            autoFind = serializedObject.FindProperty("autoFind");
            dynamicMaterial = serializedObject.FindProperty("dynamicMaterial");
            waterLevelSource = serializedObject.FindProperty("waterLevelSource");
            waterLevel = serializedObject.FindProperty("waterLevel");
            childTransform = serializedObject.FindProperty("childTransform");
            heightOffset = serializedObject.FindProperty("heightOffset");
            rollAmount = serializedObject.FindProperty("rollAmount");
            samples = serializedObject.FindProperty("samples");
            
            //Auto fetch if there is only one water body in the scene
            if (waterObject.objectReferenceValue == null && WaterObject.Instances.Count == 1)
            {
                serializedObject.Update();
                waterObject.objectReferenceValue = WaterObject.Instances[0];
                EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            
            ValidateMaterial();
        }

        private void OnDisable()
        {
            AlignToWaves.Disable = false;
            Tools.hidden = false;
        }

        public override void OnInspectorGUI()
        {
            UI.DrawHeader();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.labelWidth);
                AlignToWaves.EnableInEditor =
                    GUILayout.Toggle(AlignToWaves.EnableInEditor, new GUIContent(" Run in edit-mode (global)", EditorGUIUtility.IconContent(
                        (AlignToWaves.EnableInEditor ? "animationvisibilitytoggleon" : "animationvisibilitytoggleoff")).image), "Button");
            }
            
            EditorGUILayout.Space();

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.PropertyField(waterObject);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(autoFind);
            EditorGUILayout.PropertyField(dynamicMaterial);
            EditorGUI.indentLevel--;

            UI.DrawNotification(isRiver, "Material has river mode enabled, buoyancy only works for flat water bodies", MessageType.Error);
            UI.DrawNotification(!wavesEnabled && !isRiver, "Material used on the water object does not have waves enabled.", MessageType.Error);
            
            if (script.waterObject && script.waterObject.material)
            {
                UI.DrawNotification((script.waterObject.material.GetFloat("_WorldSpaceUV") == 0f), "Material must use world-projected UV", "Change", ()=> script.waterObject.material.SetFloat("_WorldSpaceUV", 1f), MessageType.Error);
            }

            if(!autoFind.boolValue && waterObject.objectReferenceValue == null)
            {
                UI.DrawNotification("A water object must be assigned!", MessageType.Error);
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Water level source");
                waterLevelSource.intValue = GUILayout.Toolbar(waterLevelSource.intValue, new GUIContent[] { new GUIContent("Fixed Value"), new GUIContent("Water Object") });
            }
            if (waterLevelSource.intValue == (int)AlignToWaves.WaterLevelSource.FixedValue) EditorGUILayout.PropertyField(waterLevel);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(heightOffset);
            EditorGUILayout.PropertyField(rollAmount);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Sample positions", EditorStyles.boldLabel);

            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox("Cannot be modified for a multi-selection", MessageType.Info);
            }
            else
            {
                if (samples.arraySize > 0)
                {
                    editSamples =
                        GUILayout.Toggle(editSamples,
                            new GUIContent(" Edit samples", EditorGUIUtility.IconContent("sv_icon_dot0_pix16_gizmo").image),
                            "Button", GUILayout.MaxWidth(125f), GUILayout.MaxHeight(30f));
                }
                else
                {
                    EditorGUILayout.HelpBox("No sample positions added. The transform's pivot position is used", MessageType.None);
                }

                for (int i = 0; i < samples.arraySize; i++)
                {
                    SerializedProperty param = samples.GetArrayElementAtIndex(i);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(param, true);

                        if (GUILayout.Button(new GUIContent("",
                            EditorGUIUtility.IconContent(proSkinPrefix + "TreeEditor.Trash").image, "Delete item"), GUILayout.MaxWidth(30f)))
                        {
                            samples.DeleteArrayElementAtIndex(i);
                            selectedSampleIndex = -1;

                            EditorUtility.SetDirty(target);
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(new GUIContent("Add", EditorGUIUtility.IconContent(proSkinPrefix + "Toolbar Plus").image, "Add new sample point")))
                    {
                        samples.InsertArrayElementAtIndex(samples.arraySize);
                        selectedSampleIndex = samples.arraySize - 1;

                        EditorUtility.SetDirty(target);
                    }
                }
            }

            EditorGUILayout.PropertyField(childTransform);
            if (childTransform.objectReferenceValue == null && samples.arraySize > 0)
                UI.DrawNotification("Assign a transform to rotate/scale the sample positions with");

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                ValidateMaterial();
            }
            
            UI.DrawFooter();
        }

        private void ValidateMaterial()
        {
            if (script.waterObject && script.waterObject.material)
            {
                if (script.waterObject.material != script.waterObject.meshRenderer.sharedMaterial) script.waterObject.material = script.waterObject.meshRenderer.sharedMaterial;
                
                wavesEnabled = WaveParameters.WavesEnabled(script.waterObject.material);
                isRiver = script.waterObject.material.IsKeywordEnabled("_RIVER");
            }
        }

        private int selectedSampleIndex;
        Vector3 sampleWorldPos;
        Vector3 prevSampleWorldPos;

        private void OnSceneGUI()
        {
            if (!script) return;
            
            AlignToWaves.Disable = PrefabStageUtility.GetCurrentPrefabStage() != null || editSamples;
            
            if (editSamples)
            {
                //Mute default controls
                Tools.hidden = true;
                
                Handles.color = new Color(0.66f, 0.66f, 0.66f, 1);
                
                for (int i = 0; i < script.samples.Count; i++)
                {
                    sampleWorldPos = script.ConvertToWorldSpace(script.samples[i]);

                    float size = HandleUtility.GetHandleSize(sampleWorldPos) * 0.25f;
                    if (Handles.Button(sampleWorldPos, Quaternion.identity, size, size, Handles.SphereHandleCap))
                    {
                        selectedSampleIndex = i;
                    }
                }

                if (selectedSampleIndex > -1)
                {
                    sampleWorldPos = script.ConvertToWorldSpace(script.samples[selectedSampleIndex]);
                    prevSampleWorldPos = sampleWorldPos;
                    
                    sampleWorldPos = Handles.PositionHandle(sampleWorldPos, script.childTransform ? script.childTransform.rotation : script.transform.rotation );
                    script.samples[selectedSampleIndex] = script.ConvertToLocalSpace(sampleWorldPos);

                    //If moved
                    if (sampleWorldPos != prevSampleWorldPos)
                    {
                        prevSampleWorldPos = sampleWorldPos;
                        EditorUtility.SetDirty(target);
                    }
                }
            }
            else
            {
                selectedSampleIndex = -1;
                Tools.hidden = false;
                
                if (script.samples == null) return;

                Handles.color = new Color(1,1,1, 0.25f);
                for (int i = 0; i < script.samples.Count; i++)
                {
                    sampleWorldPos = script.ConvertToWorldSpace(script.samples[i]);
                    Handles.SphereHandleCap(0, sampleWorldPos, SceneView.lastActiveSceneView.camera.transform.rotation, HandleUtility.GetHandleSize(sampleWorldPos) * 0.25f, EventType.Repaint);
                }
            }
        }
    }
}