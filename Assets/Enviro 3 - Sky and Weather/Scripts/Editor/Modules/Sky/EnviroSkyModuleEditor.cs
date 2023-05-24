using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Enviro
{
    [CustomEditor(typeof(EnviroSkyModule))]
    public class EnviroSkyModuleEditor : EnviroModuleEditor
    {  
        private EnviroSkyModule myTarget; 

        //Properties
        private SerializedProperty frontColorGradient0,frontColorGradient1,frontColorGradient2,frontColorGradient3,frontColorGradient4,frontColorGradient5;  
        private SerializedProperty frontColor0,frontColor1,frontColor2,frontColor3,frontColor4,frontColor5;  
        private SerializedProperty sunDiscColorGradient, moonColorGradient, moonGlowColorGradient;
        private SerializedProperty sunDiscColor, moonColor, moonGlowColor;
        private SerializedProperty backColorGradient0,backColorGradient1,backColorGradient2,backColorGradient3,backColorGradient4,backColorGradient5;
        private SerializedProperty backColor0,backColor1,backColor2,backColor3,backColor4,backColor5;
        private SerializedProperty distribution0,distribution1,distribution2,distribution3;
        private SerializedProperty starsTex, galaxyTex, sunTex, moonTex, moonGlowTex;

        private SerializedProperty mieScatteringIntensityCurve, moonGlowIntensityCurve, starIntensityCurve, galaxyIntensityCurve;
        private SerializedProperty intensity,intensityCurve, mieScatteringIntensity,sunScale, moonScale, moonGlowScale, moonMode, moonPhase;

#if ENVIRO_HDRP
        private SerializedProperty skyExposureHDRP,skyAmbientModeHDRP;
#endif
        //On Enable
        public override void OnEnable()
        {
            if(!target)
                return;

            myTarget = (EnviroSkyModule)target;
            serializedObj = new SerializedObject(myTarget);
            preset = serializedObj.FindProperty("preset");
            // Front Colors
            frontColorGradient0 = serializedObj.FindProperty("Settings.frontColorGradient0");
            frontColorGradient1 = serializedObj.FindProperty("Settings.frontColorGradient1");
            frontColorGradient2 = serializedObj.FindProperty("Settings.frontColorGradient2");
            frontColorGradient3 = serializedObj.FindProperty("Settings.frontColorGradient3");
            frontColorGradient4 = serializedObj.FindProperty("Settings.frontColorGradient4");
            frontColorGradient5 = serializedObj.FindProperty("Settings.frontColorGradient5");

            // Back Colors
            backColorGradient0 = serializedObj.FindProperty("Settings.backColorGradient0");
            backColorGradient1 = serializedObj.FindProperty("Settings.backColorGradient1");
            backColorGradient2 = serializedObj.FindProperty("Settings.backColorGradient2");
            backColorGradient3 = serializedObj.FindProperty("Settings.backColorGradient3");
            backColorGradient4 = serializedObj.FindProperty("Settings.backColorGradient4");
            backColorGradient5 = serializedObj.FindProperty("Settings.backColorGradient5");

            //Sund and Moon Colors 
            sunDiscColorGradient = serializedObj.FindProperty("Settings.sunDiscColorGradient"); 
            moonColorGradient = serializedObj.FindProperty("Settings.moonColorGradient"); 
            moonGlowColorGradient = serializedObj.FindProperty("Settings.moonGlowColorGradient");
            //Distribution
            distribution0 = serializedObj.FindProperty("Settings.distribution0");
            distribution1 = serializedObj.FindProperty("Settings.distribution1");
            distribution2 = serializedObj.FindProperty("Settings.distribution2");
            distribution3 = serializedObj.FindProperty("Settings.distribution3");

            //Textures
            starsTex = serializedObj.FindProperty("Settings.starsTex");
            galaxyTex = serializedObj.FindProperty("Settings.galaxyTex");
            sunTex = serializedObj.FindProperty("Settings.sunTex"); 
            moonTex = serializedObj.FindProperty("Settings.moonTex");
            moonGlowTex = serializedObj.FindProperty("Settings.moonGlowTex");
            moonMode = serializedObj.FindProperty("Settings.moonMode"); 
            //Intensity
            mieScatteringIntensityCurve = serializedObj.FindProperty("Settings.mieScatteringIntensityCurve");
            moonGlowIntensityCurve = serializedObj.FindProperty("Settings.moonGlowIntensityCurve");
            starIntensityCurve = serializedObj.FindProperty("Settings.starIntensityCurve");
            galaxyIntensityCurve = serializedObj.FindProperty("Settings.galaxyIntensityCurve");
            intensity = serializedObj.FindProperty("Settings.intensity");
            intensityCurve = serializedObj.FindProperty("Settings.intensityCurve");
            sunScale = serializedObj.FindProperty("Settings.sunScale");
            moonScale = serializedObj.FindProperty("Settings.moonScale");
            moonPhase = serializedObj.FindProperty("Settings.moonPhase"); 
           // moonGlowScale = serializedObj.FindProperty("skySettings.moonGlowScale");
           #if ENVIRO_HDRP
             skyExposureHDRP = serializedObj.FindProperty("Settings.skyExposureHDRP");
             skyAmbientModeHDRP = serializedObj.FindProperty("Settings.skyAmbientModeHDRP");
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
            myTarget.showModuleInspector = GUILayout.Toggle(myTarget.showModuleInspector, "Sky", headerFoldout);
            
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
            {
                EnviroManager.instance.RemoveModule(EnviroManager.ModuleType.Sky); //Add Remove
                DestroyImmediate(this);
                return;
            } 
            
            EditorGUILayout.EndHorizontal();
            
            if(myTarget.showModuleInspector)
            {
                //EditorGUILayout.LabelField("This module will control your skybox.");
                serializedObj.UpdateIfRequiredOrScript ();
                EditorGUI.BeginChangeCheck();
                
                // Sky Color Controls
                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showSkyControls = GUILayout.Toggle(myTarget.showSkyControls, "Sky Controls", headerFoldout);               
                if(myTarget.showSkyControls)
                {
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField("Ground Color",headerStyle);
                    EditorGUILayout.PropertyField(frontColorGradient0);
                    EditorGUILayout.PropertyField(backColorGradient0);
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Horizon Colors",headerStyle);
                    EditorGUILayout.PropertyField(frontColorGradient1);
                    EditorGUILayout.PropertyField(backColorGradient1);
                    GUILayout.Space(5);
                    EditorGUILayout.PropertyField(frontColorGradient2);
                    EditorGUILayout.PropertyField(backColorGradient2);
                    GUILayout.Space(5);   
                    EditorGUILayout.PropertyField(frontColorGradient3);
                    EditorGUILayout.PropertyField(backColorGradient3);
                    GUILayout.Space(5);   
                    EditorGUILayout.PropertyField(frontColorGradient4);
                    EditorGUILayout.PropertyField(backColorGradient4);
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Top Color",headerStyle);
                    EditorGUILayout.PropertyField(frontColorGradient5);
                    EditorGUILayout.PropertyField(backColorGradient5);
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Distribution",headerStyle);
                    EditorGUILayout.PropertyField(distribution0);
                    EditorGUILayout.PropertyField(distribution1);
                    EditorGUILayout.PropertyField(distribution2);
                    EditorGUILayout.PropertyField(distribution3);
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Intensity",headerStyle);
                    EditorGUILayout.PropertyField(intensity);
                    EditorGUILayout.PropertyField(intensityCurve);          
                     GUILayout.Space(5);
                    EditorGUILayout.PropertyField(mieScatteringIntensityCurve);
            #if ENVIRO_HDRP
                     GUILayout.Space(5);
                     EditorGUILayout.LabelField("HDRP Settings",headerStyle);
                     EditorGUILayout.PropertyField(skyAmbientModeHDRP);   
                     EditorGUILayout.PropertyField(skyExposureHDRP);
            #endif
                } 
                GUILayout.EndVertical();

                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showSkySunControls = GUILayout.Toggle(myTarget.showSkySunControls, "Sun Controls", headerFoldout);               
                if(myTarget.showSkySunControls)
                {
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField("Textures",headerStyle);
                    EditorGUILayout.PropertyField(sunTex);
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Color",headerStyle);
                    EditorGUILayout.PropertyField(sunDiscColorGradient);
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Scale",headerStyle);
                    EditorGUILayout.PropertyField(sunScale);
                } 
                GUILayout.EndVertical();

                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showSkyMoonControls = GUILayout.Toggle(myTarget.showSkyMoonControls, "Moon Controls", headerFoldout);               
                if(myTarget.showSkyMoonControls)
                {
                    GUILayout.Space(10);          
                    EditorGUILayout.PropertyField(moonMode);
                    if(myTarget.Settings.moonMode != EnviroSky.MoonMode.Simple)
                       EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(moonPhase);    

                    if(myTarget.Settings.moonMode != EnviroSky.MoonMode.Simple)
                       EditorGUI.EndDisabledGroup();  
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Textures",headerStyle);
                    EditorGUILayout.PropertyField(moonTex);
                    //EditorGUILayout.PropertyField(moonGlowTex);
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Color",headerStyle);
                    EditorGUILayout.PropertyField(moonColorGradient);
                    //EditorGUILayout.PropertyField(moonGlowColorGradient); 
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Scale",headerStyle);
                    EditorGUILayout.PropertyField(moonScale);
                    // EditorGUILayout.PropertyField(moonGlowScale);
                    //GUILayout.Space(10);
                    //EditorGUILayout.LabelField("Intensity",headerStyle);
                    //EditorGUILayout.PropertyField(moonGlowIntensityCurve);
                } 
                GUILayout.EndVertical();

                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showSkyStarsControls = GUILayout.Toggle(myTarget.showSkyStarsControls, "Stars and Galaxy Controls", headerFoldout);               
                if(myTarget.showSkyStarsControls)
                {
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField("Textures",headerStyle);
                    EditorGUILayout.PropertyField(starsTex);
                    EditorGUILayout.PropertyField(galaxyTex);
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Intensity",headerStyle);
                    EditorGUILayout.PropertyField(starIntensityCurve);
                    EditorGUILayout.PropertyField(galaxyIntensityCurve);
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
