using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Enviro
{
    [CustomEditor(typeof(EnviroFogModule))]
    public class EnviroFogModuleEditor : EnviroModuleEditor
    {  
        private EnviroFogModule myTarget; 

        //Properties
        private SerializedProperty fog, volumetrics, ditheringTex, quality, steps, scattering, extinction, anistropy, maxRange,maxRangePointSpot, noiseIntensity, noiseScale, windDirection, noise;
        private SerializedProperty fogDensity, fogHeightFalloff, fogHeight, fogDensity2, fogHeightFalloff2, fogHeight2, fogMaxOpacity, startDistance, fogColorBlend,ambientColorGradient;
    #if ENVIRO_HDRP
        private SerializedProperty controlHDRPFog, fogAttenuationDistance, baseHeight, maxHeight, fogColorTint;
        private SerializedProperty controlHDRPVolumetrics, volumetricsColorTint, ambientDimmer, directLightMultiplier, directLightShadowdimmer;
    #endif
        public override void OnEnable()
        {
            if(!target)
                return;

            myTarget = (EnviroFogModule)target;
            serializedObj = new SerializedObject(myTarget);
            preset = serializedObj.FindProperty("preset");

            //Volumetrics
            volumetrics = serializedObj.FindProperty("Settings.volumetrics");
            quality = serializedObj.FindProperty("Settings.quality");
            steps = serializedObj.FindProperty("Settings.steps");
            scattering = serializedObj.FindProperty("Settings.scattering");
            extinction = serializedObj.FindProperty("Settings.extinction");
            anistropy = serializedObj.FindProperty("Settings.anistropy");
            maxRange = serializedObj.FindProperty("Settings.maxRange");
            maxRangePointSpot = serializedObj.FindProperty("Settings.maxRangePointSpot");
            noiseIntensity = serializedObj.FindProperty("Settings.noiseIntensity");
            noiseScale = serializedObj.FindProperty("Settings.noiseScale");
            windDirection= serializedObj.FindProperty("Settings.windDirection");
            noise = serializedObj.FindProperty("Settings.noise");
            ditheringTex = serializedObj.FindProperty("Settings.ditheringTex");

            //Height Fog
            fog = serializedObj.FindProperty("Settings.fog");
            fogDensity = serializedObj.FindProperty("Settings.fogDensity");
            fogHeightFalloff = serializedObj.FindProperty("Settings.fogHeightFalloff");
            fogHeight = serializedObj.FindProperty("Settings.fogHeight");
            fogDensity2 = serializedObj.FindProperty("Settings.fogDensity2");
            fogHeightFalloff2 = serializedObj.FindProperty("Settings.fogHeightFalloff2");
            fogHeight2 = serializedObj.FindProperty("Settings.fogHeight2");
            fogMaxOpacity = serializedObj.FindProperty("Settings.fogMaxOpacity");
            startDistance = serializedObj.FindProperty("Settings.startDistance");
            fogColorBlend = serializedObj.FindProperty("Settings.fogColorBlend");
            ambientColorGradient = serializedObj.FindProperty("Settings.ambientColorGradient");

            //HDRP
  #if ENVIRO_HDRP
            controlHDRPFog = serializedObj.FindProperty("Settings.controlHDRPFog");
            fogAttenuationDistance = serializedObj.FindProperty("Settings.fogAttenuationDistance");
            baseHeight = serializedObj.FindProperty("Settings.baseHeight");
            maxHeight = serializedObj.FindProperty("Settings.maxHeight");
            fogColorTint= serializedObj.FindProperty("Settings.fogColorTint");

            controlHDRPVolumetrics= serializedObj.FindProperty("Settings.controlHDRPVolumetrics");
            volumetricsColorTint = serializedObj.FindProperty("Settings.volumetricsColorTint");
            ambientDimmer = serializedObj.FindProperty("Settings.ambientDimmer");
            directLightMultiplier = serializedObj.FindProperty("Settings.directLightMultiplier");
            directLightShadowdimmer = serializedObj.FindProperty("Settings.directLightShadowdimmer");
  #endif

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
            myTarget.showModuleInspector = GUILayout.Toggle(myTarget.showModuleInspector, "Fog", headerFoldout);
            
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
            {
                EnviroManager.instance.RemoveModule(EnviroManager.ModuleType.Fog); //Add Remove
                DestroyImmediate(this);
                return;
            } 
            
            EditorGUILayout.EndHorizontal();
            
            if(myTarget.showModuleInspector)
            {
                RenderDisableInputBox();
                serializedObj.UpdateIfRequiredOrScript ();
                EditorGUI.BeginChangeCheck();
                
                // Enviro Fog
                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showFogControls = GUILayout.Toggle(myTarget.showFogControls, "Fog Controls", headerFoldout);               
                if(myTarget.showFogControls)
                { 
                    GUILayout.Space(5);
                    DisableInputStartQuality();
                    EditorGUILayout.PropertyField(fog);
                    DisableInputEndQuality();
                    GUILayout.Space(5);
                    if(myTarget.Settings.fog)
                    {
                    DisableInputStart();
                    EditorGUILayout.LabelField("Fog Layer 1",headerStyle);
                    GUILayout.Space(5);
                    EditorGUILayout.PropertyField(fogDensity);
                    EditorGUILayout.PropertyField(fogHeightFalloff);
                    EditorGUILayout.PropertyField(fogHeight);
                    GUILayout.Space(10);

                    EditorGUILayout.LabelField("Fog Layer 2",headerStyle);
                    GUILayout.Space(5);
                    EditorGUILayout.PropertyField(fogDensity2);
                    EditorGUILayout.PropertyField(fogHeightFalloff2);
                    EditorGUILayout.PropertyField(fogHeight2);
                    GUILayout.Space(10);
                    DisableInputEnd();

                    EditorGUILayout.LabelField("Opacity and Distance",headerStyle);
                    GUILayout.Space(5);
                    EditorGUILayout.PropertyField(fogMaxOpacity);
                    //EditorGUILayout.PropertyField(startDistance);
                    GUILayout.Space(10); 
                    DisableInputStart();
                    EditorGUILayout.LabelField("Color",headerStyle);
                    GUILayout.Space(5);
                    EditorGUILayout.PropertyField(fogColorBlend);
                    DisableInputEnd();        
                    EditorGUILayout.PropertyField(ambientColorGradient);          
                    GUILayout.Space(10); 
                    }                 
                }
                GUILayout.EndVertical();

                //HDRP Fog
            #if ENVIRO_HDRP
                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showHDRPFogControls = GUILayout.Toggle(myTarget.showHDRPFogControls, "HDRP Fog Controls", headerFoldout);               
                if(myTarget.showHDRPFogControls)
                { 
                    GUILayout.Space(5);
                    EditorGUILayout.PropertyField(controlHDRPFog);
                    GUILayout.Space(5);
                    if(myTarget.Settings.controlHDRPFog)
                    {
                    EditorGUILayout.LabelField("Density",headerStyle);
                    DisableInputStart();
                    EditorGUILayout.PropertyField(fogAttenuationDistance); 
                    EditorGUILayout.PropertyField(baseHeight); 
                    EditorGUILayout.PropertyField(maxHeight);
                    DisableInputEnd();
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Color",headerStyle);
                    EditorGUILayout.PropertyField(fogColorTint);         
                    }                 
                }
                GUILayout.EndVertical();
            #endif

                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showVolumetricsControls = GUILayout.Toggle(myTarget.showVolumetricsControls, "Volumetrics Controls", headerFoldout);               
                if(myTarget.showVolumetricsControls)
                { 
                    GUILayout.Space(5);
        #if !ENVIRO_HDRP
                    DisableInputStartQuality();
                    EditorGUILayout.PropertyField(volumetrics);
                    DisableInputEndQuality();
                    GUILayout.Space(5);
                    if(myTarget.Settings.volumetrics && myTarget.Settings.fog)
                    {
                        DisableInputStartQuality();
                        EditorGUILayout.PropertyField(quality);
                        EditorGUILayout.PropertyField(steps);
                        DisableInputEndQuality();
                        DisableInputStart();
                        EditorGUILayout.PropertyField(scattering);
                        EditorGUILayout.PropertyField(extinction);
                        EditorGUILayout.PropertyField(anistropy);
                        DisableInputEnd();
                        EditorGUILayout.PropertyField(maxRange);
                        EditorGUILayout.PropertyField(maxRangePointSpot);     
                        //EditorGUILayout.PropertyField(noiseIntensity);
                        //EditorGUILayout.PropertyField(noiseScale);
                        //EditorGUILayout.PropertyField(windDirection);
                        //EditorGUILayout.PropertyField(noise);
                        EditorGUILayout.PropertyField(ditheringTex);            
                    } 

        #else       
                    EditorGUILayout.PropertyField(controlHDRPVolumetrics);
                    GUILayout.Space(5);
                    if(myTarget.Settings.controlHDRPVolumetrics && myTarget.Settings.controlHDRPFog)
                    {
                        EditorGUILayout.LabelField("Global",headerStyle);
                        EditorGUILayout.PropertyField(volumetricsColorTint);
                        DisableInputStart();
                        EditorGUILayout.PropertyField(ambientDimmer);
                        DisableInputEnd();
                        GUILayout.Space(5);
                        EditorGUILayout.LabelField("Directional Lights",headerStyle);
                        DisableInputStart();
                        EditorGUILayout.PropertyField(directLightMultiplier);
                        EditorGUILayout.PropertyField(directLightShadowdimmer);
                        DisableInputEnd();
                    }
        #endif 
                }
                GUILayout.EndVertical();

                /// Save Load
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
