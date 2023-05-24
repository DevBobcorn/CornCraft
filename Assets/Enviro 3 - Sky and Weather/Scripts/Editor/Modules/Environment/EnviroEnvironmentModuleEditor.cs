using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Enviro
{
    [CustomEditor(typeof(EnviroEnvironmentModule))]
    public class EnviroEnvironmentModuleEditor : EnviroModuleEditor
    {  
        private EnviroEnvironmentModule myTarget; 

        //Properties Seasons
        private SerializedProperty season, changeSeason, springStart, springEnd, summerStart, summerEnd, autumnStart, autumnEnd, winterStart, winterEnd;
        //Properties Temperature
        private SerializedProperty springBaseTemperature, summerBaseTemperature, autumnBaseTemperature, winterBaseTemperature, temperatureWeatherMod, temperature, temperatureChangingSpeed;
        //Properties Weather State
        private SerializedProperty wetness, snow, wetnessTarget, snowTarget, wetnessAccumulationSpeed, wetnessDrySpeed, snowAccumulationSpeed, snowMeltSpeed ,snowMeltingTresholdTemperature;
        //Properties Wind
        private SerializedProperty windDirectionX,windDirectionY,windSpeed,windTurbulence;

        //On Enable
        public override void OnEnable()
        {
            base.OnEnable();

            if(!target)
                return;

            myTarget = (EnviroEnvironmentModule)target;
            serializedObj = new SerializedObject(myTarget);
            preset = serializedObj.FindProperty("preset");

            season = serializedObj.FindProperty("Settings.season");
            changeSeason = serializedObj.FindProperty("Settings.changeSeason");
            springStart = serializedObj.FindProperty("Settings.springStart");
            springEnd = serializedObj.FindProperty("Settings.springEnd");
            summerStart = serializedObj.FindProperty("Settings.summerStart");
            summerEnd = serializedObj.FindProperty("Settings.summerEnd");
            autumnStart = serializedObj.FindProperty("Settings.autumnStart");
            autumnEnd = serializedObj.FindProperty("Settings.autumnEnd");
            winterStart = serializedObj.FindProperty("Settings.winterStart");
            winterEnd = serializedObj.FindProperty("Settings.winterEnd");

            springBaseTemperature = serializedObj.FindProperty("Settings.springBaseTemperature");
            summerBaseTemperature = serializedObj.FindProperty("Settings.summerBaseTemperature");
            autumnBaseTemperature = serializedObj.FindProperty("Settings.autumnBaseTemperature");
            winterBaseTemperature = serializedObj.FindProperty("Settings.winterBaseTemperature");
            temperatureWeatherMod = serializedObj.FindProperty("Settings.temperatureWeatherMod");
            temperature = serializedObj.FindProperty("Settings.temperature");
            temperatureChangingSpeed = serializedObj.FindProperty("Settings.temperatureChangingSpeed");

            wetness = serializedObj.FindProperty("Settings.wetness");
            snow = serializedObj.FindProperty("Settings.snow");
            wetnessTarget = serializedObj.FindProperty("Settings.wetnessTarget");
            snowTarget = serializedObj.FindProperty("Settings.snowTarget");
            wetnessAccumulationSpeed = serializedObj.FindProperty("Settings.wetnessAccumulationSpeed");
            wetnessDrySpeed = serializedObj.FindProperty("Settings.wetnessDrySpeed");
            snowAccumulationSpeed = serializedObj.FindProperty("Settings.snowAccumulationSpeed");
            snowMeltSpeed = serializedObj.FindProperty("Settings.snowMeltSpeed");
            snowMeltingTresholdTemperature = serializedObj.FindProperty("Settings.snowMeltingTresholdTemperature"); 

            windDirectionX = serializedObj.FindProperty("Settings.windDirectionX"); 
            windDirectionY = serializedObj.FindProperty("Settings.windDirectionY"); 
            windSpeed = serializedObj.FindProperty("Settings.windSpeed"); 
            windTurbulence = serializedObj.FindProperty("Settings.windTurbulence");           
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
            myTarget.showModuleInspector = GUILayout.Toggle(myTarget.showModuleInspector, "Environment", headerFoldout);
            
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
            {
                EnviroManager.instance.RemoveModule(EnviroManager.ModuleType.Environment); //Add Remove
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
                myTarget.showSeasonControls = GUILayout.Toggle(myTarget.showSeasonControls, "Season Controls", headerFoldout);               
                if(myTarget.showSeasonControls)
                {
                   
                   GUILayout.Space(5);   
                   EditorGUILayout.PropertyField(season);
                   EditorGUILayout.PropertyField(changeSeason);
                   GUILayout.Space(10);
                   EditorGUILayout.PropertyField(springStart);
                   EditorGUILayout.PropertyField(springEnd);
                   GUILayout.Space(5);
                   EditorGUILayout.PropertyField(summerStart);
                   EditorGUILayout.PropertyField(summerEnd);
                   GUILayout.Space(5);
                   EditorGUILayout.PropertyField(autumnStart);
                   EditorGUILayout.PropertyField(autumnEnd);
                   GUILayout.Space(5);
                   EditorGUILayout.PropertyField(winterStart);
                   EditorGUILayout.PropertyField(winterEnd); 
                }
                GUILayout.EndVertical();

                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showTemperatureControls = GUILayout.Toggle(myTarget.showTemperatureControls, "Temperature Controls", headerFoldout);               
                if(myTarget.showTemperatureControls)
                {
                   GUILayout.Space(5);      
                   EditorGUILayout.PropertyField(temperature);
                   DisableInputStart();
                   EditorGUILayout.PropertyField(temperatureWeatherMod);
                   DisableInputEnd();
                   EditorGUILayout.PropertyField(temperatureChangingSpeed);
                   GUILayout.Space(10);
                   EditorGUILayout.PropertyField(springBaseTemperature);
                   EditorGUILayout.PropertyField(summerBaseTemperature);
                   EditorGUILayout.PropertyField(autumnBaseTemperature);
                   EditorGUILayout.PropertyField(winterBaseTemperature);
                }
                GUILayout.EndVertical();

                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showWeatherStateControls = GUILayout.Toggle(myTarget.showWeatherStateControls, "Weather State Controls", headerFoldout);               
                if(myTarget.showWeatherStateControls)
                {
                   GUILayout.Space(5);     
                   EditorGUILayout.PropertyField(wetness);
                   EditorGUILayout.PropertyField(snow);
                   GUILayout.Space(5);
                   DisableInputStart();
                   EditorGUILayout.PropertyField(wetnessTarget);
                   EditorGUILayout.PropertyField(snowTarget);
                   DisableInputEnd();
                   GUILayout.Space(10);
                   EditorGUILayout.PropertyField(wetnessAccumulationSpeed);
                   EditorGUILayout.PropertyField(wetnessDrySpeed);
                   GUILayout.Space(5);
                   EditorGUILayout.PropertyField(snowAccumulationSpeed);
                   EditorGUILayout.PropertyField(snowMeltSpeed);
                   EditorGUILayout.PropertyField(snowMeltingTresholdTemperature);
                }
                GUILayout.EndVertical();

                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showWindControls = GUILayout.Toggle(myTarget.showWindControls, "Wind Controls", headerFoldout);               
                if(myTarget.showWindControls)
                {
                   GUILayout.Space(5);
                   DisableInputStart();  
                   EditorGUILayout.PropertyField(windDirectionX); 
                   EditorGUILayout.PropertyField(windDirectionY);
                   GUILayout.Space(5);
                   EditorGUILayout.PropertyField(windSpeed);
                   EditorGUILayout.PropertyField(windTurbulence);
                   DisableInputEnd();
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
