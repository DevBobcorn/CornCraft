using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Enviro
{
    [CustomEditor(typeof(EnviroLightingModule))]
    public class EnviroLightingModuleEditor : EnviroModuleEditor
    {  
        private EnviroLightingModule myTarget; 

        //Properties 
        //Direct Lighting
        private SerializedProperty updateIntervallFrames,directLightIntensityModifier,sunIntensityCurve, moonIntensityCurve, sunColorGradient, moonColorGradient, lightingMode;  
        //Ambient Lighting
        private SerializedProperty ambientIntensityModifier,ambientMode, ambientSkyboxUpdateIntervall, ambientSkyColorGradient, ambientEquatorColorGradient, ambientGroundColorGradient, ambientIntensityCurve;
        //Reflection Probe
        private SerializedProperty updateReflectionProbe,updateDefaultEnvironmentReflections,globalReflectionCustomRendering, globalReflectionUseFog, globalReflectionTimeSlicing, globalReflectionsUpdateOnGameTime, globalReflectionsUpdateOnPosition, globalReflectionsIntensity, globalReflectionsTimeTreshold, globalReflectionsPositionTreshold, globalReflectionsScale, globalReflectionResolution, globalReflectionLayers;

#if ENVIRO_HDRP
        private SerializedProperty sunIntensityCurveHDRP, moonIntensityCurveHDRP, lightColorTemperatureHDRP, lightColorTintHDRP,ambientColorTintHDRP, controlExposure, sceneExposure, controlIndirectLighting, diffuseIndirectIntensity, reflectionIndirectIntensity;
#endif
        //On Enable
        public override void OnEnable()
        {
            
            if(!target)
                return; 
 
            base.OnEnable();

            myTarget = (EnviroLightingModule)target;
            serializedObj = new SerializedObject(myTarget);
            preset = serializedObj.FindProperty("preset");
            //Direct Lighting
            updateIntervallFrames = serializedObj.FindProperty("Settings.updateIntervallFrames");      
            lightingMode = serializedObj.FindProperty("Settings.lightingMode");      
            sunIntensityCurve = serializedObj.FindProperty("Settings.sunIntensityCurve");      
            moonIntensityCurve = serializedObj.FindProperty("Settings.moonIntensityCurve");         
            sunColorGradient = serializedObj.FindProperty("Settings.sunColorGradient");       
            moonColorGradient = serializedObj.FindProperty("Settings.moonColorGradient");      
            directLightIntensityModifier = serializedObj.FindProperty("Settings.directLightIntensityModifier");
            //Ambient Lighting
            ambientMode = serializedObj.FindProperty("Settings.ambientMode");
            ambientSkyColorGradient = serializedObj.FindProperty("Settings.ambientSkyColorGradient");          
            ambientEquatorColorGradient = serializedObj.FindProperty("Settings.ambientEquatorColorGradient");          
            ambientGroundColorGradient = serializedObj.FindProperty("Settings.ambientGroundColorGradient");          
            ambientIntensityCurve = serializedObj.FindProperty("Settings.ambientIntensityCurve");
            ambientIntensityModifier = serializedObj.FindProperty("Settings.ambientIntensityModifier");
            ambientSkyboxUpdateIntervall = serializedObj.FindProperty("Settings.ambientSkyboxUpdateIntervall");
            //Reflection Probe
            updateReflectionProbe = serializedObj.FindProperty("Settings.updateReflectionProbe"); 
            updateDefaultEnvironmentReflections = serializedObj.FindProperty("Settings.updateDefaultEnvironmentReflections"); 
            globalReflectionCustomRendering = serializedObj.FindProperty("Settings.globalReflectionCustomRendering");
            globalReflectionUseFog = serializedObj.FindProperty("Settings.globalReflectionUseFog");
            globalReflectionTimeSlicing = serializedObj.FindProperty("Settings.globalReflectionTimeSlicing");
            globalReflectionsUpdateOnGameTime = serializedObj.FindProperty("Settings.globalReflectionsUpdateOnGameTime");
            globalReflectionsUpdateOnPosition = serializedObj.FindProperty("Settings.globalReflectionsUpdateOnPosition");
            globalReflectionsIntensity = serializedObj.FindProperty("Settings.globalReflectionsIntensity");
            globalReflectionsTimeTreshold = serializedObj.FindProperty("Settings.globalReflectionsTimeTreshold");
            globalReflectionsPositionTreshold = serializedObj.FindProperty("Settings.globalReflectionsPositionTreshold");
            globalReflectionsScale = serializedObj.FindProperty("Settings.globalReflectionsScale");
            globalReflectionResolution = serializedObj.FindProperty("Settings.globalReflectionResolution");
            globalReflectionLayers = serializedObj.FindProperty("Settings.globalReflectionLayers");
            #if ENVIRO_HDRP
            sunIntensityCurveHDRP = serializedObj.FindProperty("Settings.sunIntensityCurveHDRP");
            moonIntensityCurveHDRP = serializedObj.FindProperty("Settings.moonIntensityCurveHDRP");
            lightColorTemperatureHDRP = serializedObj.FindProperty("Settings.lightColorTemperatureHDRP");
            lightColorTintHDRP = serializedObj.FindProperty("Settings.lightColorTintHDRP");
            ambientColorTintHDRP = serializedObj.FindProperty("Settings.ambientColorTintHDRP");
            controlExposure = serializedObj.FindProperty("Settings.controlExposure");
            sceneExposure = serializedObj.FindProperty("Settings.sceneExposure");
            controlIndirectLighting = serializedObj.FindProperty("Settings.controlIndirectLighting");
            diffuseIndirectIntensity = serializedObj.FindProperty("Settings.diffuseIndirectIntensity");
            reflectionIndirectIntensity = serializedObj.FindProperty("Settings.reflectionIndirectIntensity");
            #endif
        } 
/*

*/
        public override void OnInspectorGUI()
        {
            if(!target)
                return;

            base.OnInspectorGUI();

            GUI.backgroundColor = new Color(0.0f,0.0f,0.5f,1f);
            GUILayout.BeginVertical("",boxStyleModified);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.BeginHorizontal();
            myTarget.showModuleInspector = GUILayout.Toggle(myTarget.showModuleInspector, "Lighting", headerFoldout);
            
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
            {
                EnviroManager.instance.RemoveModule(EnviroManager.ModuleType.Lighting);
                DestroyImmediate(this);
                return;
            } 
            
            EditorGUILayout.EndHorizontal();
            
            if(myTarget.showModuleInspector)
            {
                RenderDisableInputBox();
                serializedObj.UpdateIfRequiredOrScript ();
                EditorGUI.BeginChangeCheck();

                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showDirectLightingControls = GUILayout.Toggle(myTarget.showDirectLightingControls, "Direct Lighting Controls", headerFoldout);              
                if(myTarget.showDirectLightingControls)
                {
                    GUILayout.Space(10);
                    myTarget.Settings.setDirectLighting = EditorGUILayout.BeginToggleGroup("Set Direct Lighting",myTarget.Settings.setDirectLighting);
                    EditorGUILayout.PropertyField(lightingMode);
                    if (GUILayout.Button("Apply Lighting Mode Changes"))
                    {
                        myTarget.ApplyLightingChanges();
                    }
                    GUILayout.Space(10);
                    
                    EditorGUILayout.LabelField("Lighting Updates",headerStyle);
                    EditorGUILayout.PropertyField(updateIntervallFrames);
                     GUILayout.Space(5);
                    EditorGUILayout.LabelField("Light Intensity",headerStyle);
                    #if !ENVIRO_HDRP
                    EditorGUILayout.PropertyField(sunIntensityCurve);
                    EditorGUILayout.PropertyField(moonIntensityCurve);
                    #else
                    EditorGUILayout.PropertyField(sunIntensityCurveHDRP);
                    EditorGUILayout.PropertyField(moonIntensityCurveHDRP);
                    #endif
                    DisableInputStart();
                    EditorGUILayout.PropertyField(directLightIntensityModifier);
                    DisableInputEnd();
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField("Light Color",headerStyle);
                    #if !ENVIRO_HDRP
                    EditorGUILayout.PropertyField(sunColorGradient);
                    EditorGUILayout.PropertyField(moonColorGradient);
                    #else
                    EditorGUILayout.PropertyField(lightColorTintHDRP);
                    EditorGUILayout.PropertyField(lightColorTemperatureHDRP);
                    #endif 
                    #if ENVIRO_HDRP
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField("Exposure",headerStyle);
                    EditorGUILayout.PropertyField(controlExposure);
                    EditorGUILayout.PropertyField(sceneExposure);
                    #endif
                    EditorGUILayout.EndToggleGroup();
                }
                GUILayout.EndVertical();

                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showAmbientLightingControls = GUILayout.Toggle(myTarget.showAmbientLightingControls, "Ambient Lighting Controls", headerFoldout);              
                if(myTarget.showAmbientLightingControls)
                {
                    GUILayout.Space(10);

                    myTarget.Settings.setAmbientLighting = EditorGUILayout.BeginToggleGroup("Set Ambient Lighting",myTarget.Settings.setAmbientLighting);           
               #if !ENVIRO_HDRP 
                    EditorGUILayout.PropertyField(ambientMode);
                    GUILayout.Space(10);
                        if(myTarget.Settings.ambientMode == UnityEngine.Rendering.AmbientMode.Flat)
                        {
                            EditorGUILayout.LabelField("Ambient Color",headerStyle);
                            EditorGUILayout.PropertyField(ambientSkyColorGradient);
                            GUILayout.Space(5); 
                            EditorGUILayout.LabelField("Ambient Intensity",headerStyle);
                            EditorGUILayout.PropertyField(ambientIntensityCurve);
                            DisableInputStart();
                            EditorGUILayout.PropertyField(ambientIntensityModifier);
                            DisableInputEnd();
                        }
                        else if(myTarget.Settings.ambientMode == UnityEngine.Rendering.AmbientMode.Trilight)
                        {
                            EditorGUILayout.LabelField("Ambient Color",headerStyle);
                            EditorGUILayout.PropertyField(ambientSkyColorGradient);
                            EditorGUILayout.PropertyField(ambientEquatorColorGradient);
                            EditorGUILayout.PropertyField(ambientGroundColorGradient);
                            GUILayout.Space(5); 
                            EditorGUILayout.LabelField("Ambient Intensity",headerStyle);
                            EditorGUILayout.PropertyField(ambientIntensityCurve);
                            DisableInputStart();
                            EditorGUILayout.PropertyField(ambientIntensityModifier);
                            DisableInputEnd();
                        }
                        else 
                        {
                             EditorGUILayout.LabelField("Ambient Updates",headerStyle);
                             EditorGUILayout.PropertyField(ambientSkyboxUpdateIntervall); 
                             GUILayout.Space(5); 
                             EditorGUILayout.LabelField("Ambient Intensity",headerStyle);
                             EditorGUILayout.PropertyField(ambientIntensityCurve);
                             DisableInputStart();
                             EditorGUILayout.PropertyField(ambientIntensityModifier);
                             DisableInputEnd();
                        }             
                #else
                GUILayout.Space(5);
                EditorGUILayout.PropertyField(controlIndirectLighting);
                if(myTarget.Settings.controlIndirectLighting)
                {
                    EditorGUILayout.PropertyField(diffuseIndirectIntensity);
                    EditorGUILayout.PropertyField(reflectionIndirectIntensity);
                }
                GUILayout.Space(5);
                if(EnviroManager.instance.Sky != null)
                {
                    EditorGUILayout.LabelField("Indirect Color",headerStyle);
                    EditorGUILayout.PropertyField(ambientColorTintHDRP);    
                }       
                #endif   
                 
                    EditorGUILayout.EndToggleGroup();
                }
                GUILayout.EndVertical();

                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showReflectionControls = GUILayout.Toggle(myTarget.showReflectionControls, "Reflection Controls", headerFoldout);              
                if(myTarget.showReflectionControls)
                {
                    EditorGUILayout.PropertyField(updateReflectionProbe);      
                    if(myTarget.Settings.updateReflectionProbe)
                    {
                        EditorGUILayout.PropertyField(globalReflectionsIntensity);
                    #if !ENVIRO_HDRP
                        EditorGUILayout.PropertyField(updateDefaultEnvironmentReflections);
                        GUILayout.Space(5);

                        EditorGUILayout.PropertyField(globalReflectionResolution);
                    #endif
                        EditorGUILayout.PropertyField(globalReflectionLayers);
                        EditorGUILayout.PropertyField(globalReflectionsScale);
                    #if !ENVIRO_HDRP
                        GUILayout.Space(10);
                        EditorGUILayout.PropertyField(globalReflectionCustomRendering);
                        if(myTarget.Settings.globalReflectionCustomRendering)
                        {
                            //EditorGUILayout.PropertyField(globalReflectionUseFog);
                            EditorGUILayout.PropertyField(globalReflectionTimeSlicing);
                        }
                    #endif
                        GUILayout.Space(10);
                        EditorGUILayout.PropertyField(globalReflectionsUpdateOnGameTime);
                        if(myTarget.Settings.globalReflectionsUpdateOnGameTime)
                        EditorGUILayout.PropertyField(globalReflectionsTimeTreshold);
                         GUILayout.Space(5);
                        EditorGUILayout.PropertyField(globalReflectionsUpdateOnPosition);
                        if(myTarget.Settings.globalReflectionsUpdateOnPosition)
                        EditorGUILayout.PropertyField(globalReflectionsPositionTreshold);
                    }
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
