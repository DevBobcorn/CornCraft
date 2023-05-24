using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Enviro
{
    [CustomEditor(typeof(EnviroFlatCloudsModule))]
    public class EnviroFlatCloudsModuleEditor : EnviroModuleEditor
    {  
        private EnviroFlatCloudsModule myTarget; 

        //Properties Cirrus
        private SerializedProperty useCirrusClouds,cirrusCloudsTex, cirrusCloudsAlpha,cirrusCloudsCoverage, cirrusCloudsColorPower, cirrusCloudsColor, cirrusCloudsWindIntensity;

        //Properties 2D
        private SerializedProperty useFlatClouds, flatCloudsBaseTex, flatCloudsDetailTex, flatCloudsLightColor, flatCloudsAmbientColor, flatCloudsLightIntensity, flatCloudsAmbientIntensity, 
        flatCloudsAbsorbtion, flatCloudsHGPhase, flatCloudsCoverage, flatCloudsDensity, flatCloudsAltitude, flatCloudsTonemapping, flatCloudsBaseTiling, flatCloudsDetailTiling, flatCloudsWindIntensity,flatCloudsDetailWindIntensity;

        //On Enable
        public override void OnEnable()
        {
            base.OnEnable();

            if(!target)
                return;

            myTarget = (EnviroFlatCloudsModule)target;
            serializedObj = new SerializedObject(myTarget);
            preset = serializedObj.FindProperty("preset");

            useCirrusClouds = serializedObj.FindProperty("settings.useCirrusClouds");
            cirrusCloudsTex = serializedObj.FindProperty("settings.cirrusCloudsTex");
            cirrusCloudsAlpha = serializedObj.FindProperty("settings.cirrusCloudsAlpha");
            cirrusCloudsCoverage = serializedObj.FindProperty("settings.cirrusCloudsCoverage");
            cirrusCloudsColorPower = serializedObj.FindProperty("settings.cirrusCloudsColorPower");
            cirrusCloudsColor = serializedObj.FindProperty("settings.cirrusCloudsColor");
            cirrusCloudsWindIntensity = serializedObj.FindProperty("settings.cirrusCloudsWindIntensity");

            //2D Clouds
            useFlatClouds = serializedObj.FindProperty("settings.useFlatClouds");
            flatCloudsBaseTex = serializedObj.FindProperty("settings.flatCloudsBaseTex");
            flatCloudsDetailTex = serializedObj.FindProperty("settings.flatCloudsDetailTex");
            flatCloudsLightColor  = serializedObj.FindProperty("settings.flatCloudsLightColor");
            flatCloudsAmbientColor = serializedObj.FindProperty("settings.flatCloudsAmbientColor"); 
            flatCloudsLightIntensity = serializedObj.FindProperty("settings.flatCloudsLightIntensity"); 
            flatCloudsAmbientIntensity = serializedObj.FindProperty("settings.flatCloudsAmbientIntensity");
            flatCloudsAbsorbtion = serializedObj.FindProperty("settings.flatCloudsAbsorbtion");
            flatCloudsHGPhase = serializedObj.FindProperty("settings.flatCloudsHGPhase");
            flatCloudsCoverage = serializedObj.FindProperty("settings.flatCloudsCoverage");
            flatCloudsDensity = serializedObj.FindProperty("settings.flatCloudsDensity");
            flatCloudsAltitude  = serializedObj.FindProperty("settings.flatCloudsAltitude");
            flatCloudsTonemapping  = serializedObj.FindProperty("settings.flatCloudsTonemapping");
            flatCloudsBaseTiling = serializedObj.FindProperty("settings.flatCloudsBaseTiling");
            flatCloudsDetailTiling = serializedObj.FindProperty("settings.flatCloudsDetailTiling");
            flatCloudsWindIntensity = serializedObj.FindProperty("settings.flatCloudsWindIntensity");
            flatCloudsDetailWindIntensity = serializedObj.FindProperty("settings.flatCloudsDetailWindIntensity");

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
            myTarget.showModuleInspector = GUILayout.Toggle(myTarget.showModuleInspector, "Flat Clouds", headerFoldout);
             
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
            {
                EnviroManager.instance.RemoveModule(EnviroManager.ModuleType.FlatClouds); 
                DestroyImmediate(this);
                return;
            }  
            
            EditorGUILayout.EndHorizontal();
            
            if(myTarget.showModuleInspector)
            { 
                RenderDisableInputBox();
                serializedObj.UpdateIfRequiredOrScript ();
                EditorGUI.BeginChangeCheck();
                
                // Cirrus Clouds
                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showCirrusCloudsControls = GUILayout.Toggle(myTarget.showCirrusCloudsControls, "Cirrus Clouds", headerFoldout);               
                if(myTarget.showCirrusCloudsControls)
                {
                    GUILayout.Space(10);
                    DisableInputStartQuality();
                    EditorGUILayout.PropertyField(useCirrusClouds);
                    DisableInputEndQuality();
                    EditorGUILayout.PropertyField(cirrusCloudsTex);
                    DisableInputStart();
                    EditorGUILayout.PropertyField(cirrusCloudsAlpha);
                    EditorGUILayout.PropertyField(cirrusCloudsCoverage);              
                    EditorGUILayout.PropertyField(cirrusCloudsColorPower);
                    DisableInputEnd();
                    EditorGUILayout.PropertyField(cirrusCloudsColor);
                    GUILayout.Space(5);
                    EditorGUILayout.PropertyField(cirrusCloudsWindIntensity);
                    
                } 
                GUILayout.EndVertical();

                if( myTarget.showCirrusCloudsControls)
                    GUILayout.Space(10);

                // 2D Clouds
                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.show2DCloudsControls = GUILayout.Toggle(myTarget.show2DCloudsControls, "2D Clouds", headerFoldout);               
                if(myTarget.show2DCloudsControls)
                {
                    GUILayout.Space(10);
                    DisableInputStartQuality();
                    EditorGUILayout.PropertyField(useFlatClouds);
                    DisableInputEndQuality();
                    GUILayout.Space(5);
                    EditorGUILayout.PropertyField(flatCloudsBaseTex);
                    EditorGUILayout.PropertyField(flatCloudsBaseTiling);
                    EditorGUILayout.PropertyField(flatCloudsDetailTex);
                    EditorGUILayout.PropertyField(flatCloudsDetailTiling);
                    GUILayout.Space(5);
                    EditorGUILayout.PropertyField(flatCloudsLightColor);
                    DisableInputStart();
                    EditorGUILayout.PropertyField(flatCloudsLightIntensity);
                    DisableInputEnd();
                    EditorGUILayout.PropertyField(flatCloudsAmbientColor);
                    DisableInputStart();
                    EditorGUILayout.PropertyField(flatCloudsAmbientIntensity);        
                    EditorGUILayout.PropertyField(flatCloudsAbsorbtion);
                    DisableInputEnd();
                    EditorGUILayout.PropertyField(flatCloudsHGPhase);
                    GUILayout.Space(5);
                    DisableInputStart();
                    EditorGUILayout.PropertyField(flatCloudsCoverage);
                    EditorGUILayout.PropertyField(flatCloudsDensity);
                    DisableInputEnd();
                    EditorGUILayout.PropertyField(flatCloudsAltitude);
                    EditorGUILayout.PropertyField(flatCloudsTonemapping);
                    GUILayout.Space(5);
                    EditorGUILayout.PropertyField(flatCloudsWindIntensity);
                    EditorGUILayout.PropertyField(flatCloudsDetailWindIntensity);
                }  
                GUILayout.EndVertical();

                   if( myTarget.show2DCloudsControls)
                    GUILayout.Space(10);



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
