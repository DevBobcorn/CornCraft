using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Enviro
{
    [CustomEditor(typeof(EnviroAuroraModule))]
    public class EnviroAuroraModuleEditor : EnviroModuleEditor
    {  
        private EnviroAuroraModule myTarget; 

        //Properties
        private SerializedProperty useAurora,auroraIntensity,auroraIntensityModifier, auroraColor, auroraBrightness, auroraContrast, auroraHeight, auroraScale, auroraSteps, auroraLayer1Settings, auroraLayer2Settings, auroraColorshiftSettings, auroraSpeed,
        aurora_layer_1, aurora_layer_2, aurora_colorshift;


        //On Enable
        public override void OnEnable()
        {
            base.OnEnable();

            if(!target)
                return;

            myTarget = (EnviroAuroraModule)target;
            serializedObj = new SerializedObject(myTarget);
            preset = serializedObj.FindProperty("preset");
            
            useAurora = serializedObj.FindProperty("Settings.useAurora");
            auroraIntensity = serializedObj.FindProperty("Settings.auroraIntensity");
            auroraIntensityModifier = serializedObj.FindProperty("Settings.auroraIntensityModifier");
            auroraColor = serializedObj.FindProperty("Settings.auroraColor");
            auroraBrightness = serializedObj.FindProperty("Settings.auroraBrightness");
            auroraContrast = serializedObj.FindProperty("Settings.auroraContrast");
            auroraHeight = serializedObj.FindProperty("Settings.auroraHeight");
            auroraScale = serializedObj.FindProperty("Settings.auroraScale");
            auroraSteps = serializedObj.FindProperty("Settings.auroraSteps");
            auroraLayer1Settings = serializedObj.FindProperty("Settings.auroraLayer1Settings");
            auroraLayer2Settings = serializedObj.FindProperty("Settings.auroraLayer2Settings");
            auroraColorshiftSettings = serializedObj.FindProperty("Settings.auroraColorshiftSettings");
            auroraSpeed = serializedObj.FindProperty("Settings.auroraSpeed");
            aurora_layer_1 = serializedObj.FindProperty("Settings.aurora_layer_1");
            aurora_layer_2 = serializedObj.FindProperty("Settings.aurora_layer_2");
            aurora_colorshift = serializedObj.FindProperty("Settings.aurora_colorshift");
        } 

        public override void OnInspectorGUI()
        {
            if(!target)
                return;
            
            base.OnInspectorGUI();

            GUI.backgroundColor = baseModuleColor;
            GUILayout.BeginVertical("",boxStyleModified);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.BeginHorizontal();
            myTarget.showModuleInspector = GUILayout.Toggle(myTarget.showModuleInspector, "Aurora", headerFoldout);
            
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
            {
                EnviroManager.instance.RemoveModule(EnviroManager.ModuleType.Aurora); //Add Remove
                DestroyImmediate(this);
                return; 
            }  
            
            EditorGUILayout.EndHorizontal();
            
            if(myTarget.showModuleInspector)
            {
                RenderDisableInputBox();
                serializedObj.UpdateIfRequiredOrScript ();
                EditorGUI.BeginChangeCheck();
                
                // Set Values
                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showAuroraControls = GUILayout.Toggle(myTarget.showAuroraControls, "Aurora Controls", headerFoldout);               
                if(myTarget.showAuroraControls)
                {  
                    GUILayout.Space(5);
                    DisableInputStartQuality();
                    EditorGUILayout.PropertyField(useAurora);
                    DisableInputEndQuality();
                    GUILayout.Space(5);
                    DisableInputStart();
                    EditorGUILayout.PropertyField(auroraIntensityModifier);
                    DisableInputEnd();
                    EditorGUILayout.PropertyField(auroraIntensity);
                    EditorGUILayout.PropertyField(auroraColor);
                    EditorGUILayout.PropertyField(auroraBrightness);
                    EditorGUILayout.PropertyField(auroraContrast);
                    EditorGUILayout.PropertyField(auroraHeight); 
                    EditorGUILayout.PropertyField(auroraScale);
                    EditorGUILayout.PropertyField(auroraSteps); 
                    EditorGUILayout.PropertyField(auroraLayer1Settings);
                    EditorGUILayout.PropertyField(auroraLayer2Settings);
                    EditorGUILayout.PropertyField(auroraColorshiftSettings);
                    EditorGUILayout.PropertyField(auroraSpeed);

                    EditorGUILayout.PropertyField(aurora_layer_1);
                    EditorGUILayout.PropertyField(aurora_layer_2);
                    EditorGUILayout.PropertyField(aurora_colorshift);
                }
                GUILayout.EndVertical();


                // Save Load
                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showSaveLoad = GUILayout.Toggle(myTarget.showSaveLoad, "Save/Load", headerFoldout);
                
                if(myTarget.showSaveLoad)
                {
                    EditorGUILayout.PropertyField(preset);

                    GUILayout.BeginHorizontal("",wrapStyle);

                    if(myTarget.preset != null)
                    {
                        if(GUILayout.Button("Load"))
                        {
                            myTarget.LoadModuleValues();
                        }
                        if(GUILayout.Button("Save"))
                        {
                            myTarget.SaveModuleValues(myTarget.preset);
                        }
                    }
                    if(GUILayout.Button("Save As New"))
                    {
                        myTarget.SaveModuleValues();
                    }

                    GUILayout.EndHorizontal();

     
                }
                GUILayout.EndVertical();
                /// Save Load End
                
                ApplyChanges ();
            }
            GUILayout.EndVertical();

            if(myTarget.showModuleInspector)
             GUILayout.Space(20);
        }
    }
}
