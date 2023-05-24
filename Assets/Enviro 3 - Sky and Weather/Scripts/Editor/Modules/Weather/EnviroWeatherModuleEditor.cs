using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Enviro
{
    [CustomEditor(typeof(EnviroWeatherModule))]
    public class EnviroWeatherModuleEditor : EnviroModuleEditor
    {  
        private EnviroWeatherModule myTarget; 

        //Properties
        private SerializedProperty cloudsTransitionSpeed,fogTransitionSpeed,lightingTransitionSpeed,effectsTransitionSpeed,auroraTransitionSpeed,environmentTransitionSpeed,audioTransitionSpeed;  
      
        //On Enable
        public override void OnEnable()
        { 
            if(!target)
                return; 

            myTarget = (EnviroWeatherModule)target;
            serializedObj = new SerializedObject(myTarget);
            preset = serializedObj.FindProperty("preset");
            cloudsTransitionSpeed = serializedObj.FindProperty("Settings.cloudsTransitionSpeed");
            fogTransitionSpeed = serializedObj.FindProperty("Settings.fogTransitionSpeed");
            lightingTransitionSpeed = serializedObj.FindProperty("Settings.lightingTransitionSpeed");
            effectsTransitionSpeed = serializedObj.FindProperty("Settings.effectsTransitionSpeed");
            auroraTransitionSpeed = serializedObj.FindProperty("Settings.auroraTransitionSpeed"); 
            audioTransitionSpeed = serializedObj.FindProperty("Settings.audioTransitionSpeed");
            environmentTransitionSpeed = serializedObj.FindProperty("Settings.environmentTransitionSpeed");
 
            //weatherTypeToAdd = serializedObj.FindProperty("weatherTypeToAdd");
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
            myTarget.showModuleInspector = GUILayout.Toggle(myTarget.showModuleInspector, "Weather", headerFoldout);
            
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
            {
                EnviroManager.instance.RemoveModule(EnviroManager.ModuleType.Weather);
                DestroyImmediate(this);
                return;
            } 
            
            EditorGUILayout.EndHorizontal();
            
            if(myTarget.showModuleInspector) 
            {
                serializedObj.UpdateIfRequiredOrScript ();
                EditorGUI.BeginChangeCheck();

                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                GUILayout.Space(5);
                GUILayout.Label("Weather Presets", headerStyle);
                GUILayout.Space(5);
                Object selectedObject = null;
                         
                if(GUILayout.Button("Add"))
                {
                   int controlID = EditorGUIUtility.GetControlID (FocusType.Passive);
                   EditorGUIUtility.ShowObjectPicker<EnviroWeatherType>(null,false,"",controlID);
                }
 
                string commandName = Event.current.commandName;

                if (commandName == "ObjectSelectorClosed") 
                {
                    selectedObject = EditorGUIUtility.GetObjectPickerObject ();
                    
                    bool add = true;
                    
                    for (int i = 0; i < myTarget.Settings.weatherTypes.Count; i++)
                    {
                        if((EnviroWeatherType)selectedObject == myTarget.Settings.weatherTypes[i])
                        add = false;
                    }

                    if(add)
                      myTarget.Settings.weatherTypes.Add((EnviroWeatherType)selectedObject);
                }

                if(GUILayout.Button("Create New"))
                {
                   myTarget.CreateNewWeatherType();
                } 


                GUILayout.Space(15);
                //Make sure that we remove old empty entries where user deleted the scriptable object.
                myTarget.CleanupList();
 
                for (int i = 0; i < myTarget.Settings.weatherTypes.Count; i++) 
                    {      
                          EnviroWeatherType curWT = myTarget.Settings.weatherTypes[i];

                          if(curWT == myTarget.targetWeatherType)
                             GUI.backgroundColor = new Color(0.0f,0.5f,0.0f,1f);

                            GUILayout.BeginVertical ("", boxStyleModified);
                            GUI.backgroundColor = Color.white;

                            EditorGUILayout.BeginHorizontal();
                            curWT.showEditor = GUILayout.Toggle(curWT.showEditor, curWT.name, headerFoldout);
                            GUILayout.FlexibleSpace();
                            if(curWT != myTarget.targetWeatherType)
                            {
                                if(GUILayout.Button("Set Active", EditorStyles.miniButtonRight,GUILayout.Width(70), GUILayout.Height(18)))
                                {
                                    myTarget.ChangeWeather(curWT);
                                    EditorUtility.SetDirty(curWT);
                                } 
                            }

                            if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
                            {
                                myTarget.RemoveWeatherType(curWT);
                            } 
                            
                            EditorGUILayout.EndHorizontal();
                            //GUILayout.Space(15);
                            if(curWT.showEditor)
                            {
                                curWT.name = EditorGUILayout.TextField ("Name", curWT.name);

                                //Lighting
                                if(EnviroManager.instance.Lighting != null)
                                {
                                GUILayout.BeginVertical ("", boxStyleModified);
                                curWT.showLightingControls = GUILayout.Toggle(curWT.showLightingControls, "Lighting", headerFoldout);
                                
                                if(curWT.showLightingControls)
                                {
                                    GUILayout.Space(5);
                                    curWT.lightingOverride.directLightIntensityModifier = EditorGUILayout.Slider("Direct Light Intensity", curWT.lightingOverride.directLightIntensityModifier,0f,2f);
                                    curWT.lightingOverride.ambientIntensityModifier = EditorGUILayout.Slider("Ambient Light Intensity", curWT.lightingOverride.ambientIntensityModifier,0f,2f);
                                }
                                GUILayout.EndVertical();
                                }

                                //Volumetric Clouds
                                if(EnviroManager.instance.VolumetricClouds != null)
                                {     
                                GUILayout.BeginVertical ("", boxStyleModified);
                                curWT.showCloudControls = GUILayout.Toggle(curWT.showCloudControls, "Volumetric Clouds", headerFoldout);
                                if(curWT.showCloudControls)
                                {
                                    GUILayout.Space(5);
                                    GUILayout.BeginVertical ("", boxStyleModified);
                                    curWT.cloudsOverride.showLayer1 = GUILayout.Toggle(curWT.cloudsOverride.showLayer1, "Layer 1", headerFoldout);
                                    if(curWT.cloudsOverride.showLayer1)
                                    {
                                        curWT.cloudsOverride.coverageLayer1 = EditorGUILayout.Slider("Coverage", curWT.cloudsOverride.coverageLayer1,-1f,1f);
                                        curWT.cloudsOverride.dilateCoverageLayer1 = EditorGUILayout.Slider("Dilate Coverage", curWT.cloudsOverride.dilateCoverageLayer1,0f,1f);
                                        curWT.cloudsOverride.dilateTypeLayer1 = EditorGUILayout.Slider("Dilate Type", curWT.cloudsOverride.dilateTypeLayer1,0f,1f);
                                        curWT.cloudsOverride.typeModifierLayer1 = EditorGUILayout.Slider("Type Modifier", curWT.cloudsOverride.typeModifierLayer1,0f,1f);
                                        curWT.cloudsOverride.anvilBiasLayer1 = EditorGUILayout.Slider("Anvil Bias", curWT.cloudsOverride.anvilBiasLayer1,0f,1f);
                                        GUILayout.Space(10);
                                        curWT.cloudsOverride.scatteringIntensityLayer1 = EditorGUILayout.Slider("Scattering Intensity", curWT.cloudsOverride.scatteringIntensityLayer1,0f,2f);
                                        curWT.cloudsOverride.multiScatteringALayer1 = EditorGUILayout.Slider("Multi Scattering A", curWT.cloudsOverride.multiScatteringALayer1,0f,1f);
                                        curWT.cloudsOverride.multiScatteringBLayer1 = EditorGUILayout.Slider("Multi Scattering B", curWT.cloudsOverride.multiScatteringBLayer1,0f,1f);
                                        curWT.cloudsOverride.multiScatteringCLayer1 = EditorGUILayout.Slider("Multi Scattering C", curWT.cloudsOverride.multiScatteringCLayer1,0f,1f);
                                        curWT.cloudsOverride.powderIntensityLayer1 = EditorGUILayout.Slider("Powder Intensity", curWT.cloudsOverride.powderIntensityLayer1,0f,1f);
                                        curWT.cloudsOverride.silverLiningSpreadLayer1 = EditorGUILayout.Slider("Silver Lining Spread", curWT.cloudsOverride.silverLiningSpreadLayer1,0f,1f);
                                        curWT.cloudsOverride.ligthAbsorbtionLayer1 = EditorGUILayout.Slider("Light Absorbtion", curWT.cloudsOverride.ligthAbsorbtionLayer1,0f,2f);
                                        GUILayout.Space(10);
                                        curWT.cloudsOverride.densityLayer1 = EditorGUILayout.Slider("Density", curWT.cloudsOverride.densityLayer1,0f,1f);
                                        curWT.cloudsOverride.baseErosionIntensityLayer1 = EditorGUILayout.Slider("Base Erosion Intensity", curWT.cloudsOverride.baseErosionIntensityLayer1,0f,1f);
                                        curWT.cloudsOverride.detailErosionIntensityLayer1 = EditorGUILayout.Slider("Detail Erosion Intensity", curWT.cloudsOverride.detailErosionIntensityLayer1,0f,1f);
                                        curWT.cloudsOverride.curlIntensityLayer1 = EditorGUILayout.Slider("Curl Intensity", curWT.cloudsOverride.curlIntensityLayer1,0f,1f);
                                        GUILayout.Space(10);
                                    }
                                    GUILayout.EndVertical();

                                    if(EnviroManager.instance.VolumetricClouds.settingsGlobal.dualLayer)
                                    {
                                        GUILayout.BeginVertical ("", boxStyleModified);
                                        curWT.cloudsOverride.showLayer2 = GUILayout.Toggle(curWT.cloudsOverride.showLayer2, "Layer 2", headerFoldout);
                                        if(curWT.cloudsOverride.showLayer2)
                                        {
                                            curWT.cloudsOverride.coverageLayer2 = EditorGUILayout.Slider("Coverage", curWT.cloudsOverride.coverageLayer2,-1f,1f);
                                            curWT.cloudsOverride.dilateCoverageLayer2 = EditorGUILayout.Slider("Dilate Coverage", curWT.cloudsOverride.dilateCoverageLayer2,0f,1f);
                                            curWT.cloudsOverride.dilateTypeLayer2 = EditorGUILayout.Slider("Dilate Type", curWT.cloudsOverride.dilateTypeLayer2,0f,1f);
                                            curWT.cloudsOverride.typeModifierLayer2 = EditorGUILayout.Slider("Type Modifier", curWT.cloudsOverride.typeModifierLayer2,0f,1f);
                                            curWT.cloudsOverride.anvilBiasLayer2 = EditorGUILayout.Slider("Anvil Bias", curWT.cloudsOverride.anvilBiasLayer2,0f,1f);
                                            GUILayout.Space(10);
                                            curWT.cloudsOverride.scatteringIntensityLayer2 = EditorGUILayout.Slider("Scattering Intensity", curWT.cloudsOverride.scatteringIntensityLayer2,0f,2f);
                                            curWT.cloudsOverride.multiScatteringALayer2 = EditorGUILayout.Slider("Multi Scattering A", curWT.cloudsOverride.multiScatteringALayer2,0f,1f);
                                            curWT.cloudsOverride.multiScatteringBLayer2 = EditorGUILayout.Slider("Multi Scattering B", curWT.cloudsOverride.multiScatteringBLayer2,0f,1f);
                                            curWT.cloudsOverride.multiScatteringCLayer2 = EditorGUILayout.Slider("Multi Scattering C", curWT.cloudsOverride.multiScatteringCLayer2,0f,1f);
                                            curWT.cloudsOverride.powderIntensityLayer2 = EditorGUILayout.Slider("Powder Intensity", curWT.cloudsOverride.powderIntensityLayer2,0f,1f);
                                            curWT.cloudsOverride.silverLiningSpreadLayer2 = EditorGUILayout.Slider("Silver Lining Spread", curWT.cloudsOverride.silverLiningSpreadLayer2,0f,1f);
                                            curWT.cloudsOverride.ligthAbsorbtionLayer2 = EditorGUILayout.Slider("Light Absorbtion", curWT.cloudsOverride.ligthAbsorbtionLayer2,0f,2f);
                                            GUILayout.Space(10);
                                            curWT.cloudsOverride.densityLayer2 = EditorGUILayout.Slider("Density", curWT.cloudsOverride.densityLayer2,0f,1f);
                                            curWT.cloudsOverride.baseErosionIntensityLayer2 = EditorGUILayout.Slider("Base Erosion Intensity", curWT.cloudsOverride.baseErosionIntensityLayer2,0f,1f);
                                            curWT.cloudsOverride.detailErosionIntensityLayer2 = EditorGUILayout.Slider("Detail Erosion Intensity", curWT.cloudsOverride.detailErosionIntensityLayer2,0f,1f);
                                            curWT.cloudsOverride.curlIntensityLayer2 = EditorGUILayout.Slider("Curl Intensity", curWT.cloudsOverride.curlIntensityLayer2,0f,1f);
                                        }
                                        GUILayout.EndVertical();
                                    }                    
                                }
                                GUILayout.EndVertical(); 
                                }

                                if(EnviroManager.instance.FlatClouds != null)
                                {
                                //Flat Clouds
                                GUILayout.BeginVertical ("", boxStyleModified);
                                curWT.showFlatCloudControls = GUILayout.Toggle(curWT.showFlatCloudControls, "Flat Clouds", headerFoldout);
                                
                                if(curWT.showFlatCloudControls)
                                { 
                                    GUILayout.Space(5);
                                    EditorGUILayout.LabelField("Cirrus Clouds", headerStyle);
                                    curWT.flatCloudsOverride.cirrusCloudsCoverage = EditorGUILayout.Slider("Cirrus Clouds Coverage", curWT.flatCloudsOverride.cirrusCloudsCoverage,0f,1f);
                                    curWT.flatCloudsOverride.cirrusCloudsAlpha = EditorGUILayout.Slider("Cirrus Clouds Alpha", curWT.flatCloudsOverride.cirrusCloudsAlpha,0f,1f);
                                    curWT.flatCloudsOverride.cirrusCloudsColorPower = EditorGUILayout.Slider("Cirrus Clouds Color", curWT.flatCloudsOverride.cirrusCloudsColorPower,0f,2f);
                                    GUILayout.Space(10);
                                    EditorGUILayout.LabelField("Flat Clouds", headerStyle);
                                    curWT.flatCloudsOverride.flatCloudsCoverage = EditorGUILayout.Slider("Flat Clouds Coverage", curWT.flatCloudsOverride.flatCloudsCoverage,0f,2f);
                                    curWT.flatCloudsOverride.flatCloudsLightIntensity = EditorGUILayout.Slider("Flat Clouds Light Intensity", curWT.flatCloudsOverride.flatCloudsLightIntensity,0f,2f);
                                    curWT.flatCloudsOverride.flatCloudsAmbientIntensity = EditorGUILayout.Slider("Flat Clouds Ambient Intensity", curWT.flatCloudsOverride.flatCloudsAmbientIntensity,0f,2f);
                                    curWT.flatCloudsOverride.flatCloudsAbsorbtion = EditorGUILayout.Slider("Flat Clouds Light Absorbtion", curWT.flatCloudsOverride.flatCloudsAbsorbtion,0f,2f);
                                }
                                GUILayout.EndVertical();
                                }

                                if(EnviroManager.instance.Fog != null)
                                {
                                //Fog
                                GUILayout.BeginVertical ("", boxStyleModified);
                                curWT.showFogControls = GUILayout.Toggle(curWT.showFogControls, "Fog", headerFoldout);
                                
                                if(curWT.showFogControls)
                                { 
                                    GUILayout.Space(5);
                                    EditorGUILayout.LabelField("Layer 1", headerStyle);
                                    curWT.fogOverride.fogDensity = EditorGUILayout.Slider("Fog Density 1", curWT.fogOverride.fogDensity,0f,1f);
                                    curWT.fogOverride.fogHeightFalloff = EditorGUILayout.Slider("Fog Height Falloff 1", curWT.fogOverride.fogHeightFalloff,0f,0.05f);
                                    curWT.fogOverride.fogHeight = EditorGUILayout.FloatField("Fog Height 1 ", curWT.fogOverride.fogHeight);
                                    GUILayout.Space(10);
                                    EditorGUILayout.LabelField("Layer 2", headerStyle);
                                    curWT.fogOverride.fogDensity2 = EditorGUILayout.Slider("Fog Density 2", curWT.fogOverride.fogDensity2,0f,1f);
                                    curWT.fogOverride.fogHeightFalloff2 = EditorGUILayout.Slider("Fog Height Falloff 2", curWT.fogOverride.fogHeightFalloff2,0f,0.05f);
                                    curWT.fogOverride.fogHeight2 = EditorGUILayout.FloatField("Fog Height 2", curWT.fogOverride.fogHeight2);
                                    GUILayout.Space(10);
                                    EditorGUILayout.LabelField("Color", headerStyle);
                                    curWT.fogOverride.fogColorBlend = EditorGUILayout.Slider("Fog Sky-Color Blending", curWT.fogOverride.fogColorBlend,0f,1.0f);
                                    GUILayout.Space(10);
                            #if !ENVIRO_HDRP
                                    EditorGUILayout.LabelField("Volumetrics", headerStyle);
                                    curWT.fogOverride.scattering = EditorGUILayout.Slider("Scattering Intensity", curWT.fogOverride.scattering,0f,2.0f);
                                    curWT.fogOverride.extinction = EditorGUILayout.Slider("Extinction Intensity", curWT.fogOverride.extinction,0f,1.0f);
                                    curWT.fogOverride.anistropy = EditorGUILayout.Slider("Anistropy", curWT.fogOverride.anistropy,0f,1.0f);
                            #else
                                    EditorGUILayout.LabelField("HDRP Fog", headerStyle);
                                    curWT.fogOverride.fogAttenuationDistance = EditorGUILayout.Slider("Attenuation Distance", curWT.fogOverride.fogAttenuationDistance,0f,400f);
                                    curWT.fogOverride.baseHeight = EditorGUILayout.FloatField("Base Height", curWT.fogOverride.baseHeight);
                                    curWT.fogOverride.maxHeight = EditorGUILayout.FloatField("Max Height", curWT.fogOverride.maxHeight);
                                    GUILayout.Space(10);
                                    EditorGUILayout.LabelField("HDRP Volumetrics", headerStyle);
                                    curWT.fogOverride.ambientDimmer = EditorGUILayout.Slider("Ambient Dimmer", curWT.fogOverride.ambientDimmer,0f,1f);
                                    curWT.fogOverride.directLightMultiplier = EditorGUILayout.Slider("Direct Light Multiplier", curWT.fogOverride.directLightMultiplier,0f,16f);
                                    curWT.fogOverride.directLightShadowdimmer = EditorGUILayout.Slider("Direct Light Shadow gimmer", curWT.fogOverride.directLightShadowdimmer,0f,1f);
                            #endif 
                                }
                                GUILayout.EndVertical();
                                }

                                if(EnviroManager.instance.Effects != null)
                                {
                                //Effects
                                GUILayout.BeginVertical ("", boxStyleModified);
                                curWT.showEffectControls = GUILayout.Toggle(curWT.showEffectControls, "Effects", headerFoldout);
                                
                                if(curWT.showEffectControls)
                                { 
                                    GUILayout.Space(5);
                                    curWT.effectsOverride.rain1Emission = EditorGUILayout.Slider("Rain1 Emission", curWT.effectsOverride.rain1Emission,0f,1f);
                                    curWT.effectsOverride.rain2Emission = EditorGUILayout.Slider("Rain2 Emission", curWT.effectsOverride.rain2Emission,0f,1f);
                                    GUILayout.Space(5);
                                    curWT.effectsOverride.snow1Emission = EditorGUILayout.Slider("Snow1 Emission", curWT.effectsOverride.snow1Emission,0f,1f);
                                    curWT.effectsOverride.snow2Emission = EditorGUILayout.Slider("Snow2 Emission", curWT.effectsOverride.snow2Emission,0f,1f);
                                    GUILayout.Space(5);
                                    curWT.effectsOverride.custom1Emission = EditorGUILayout.Slider("Custom1 Emission", curWT.effectsOverride.custom1Emission,0f,1f);
                                    curWT.effectsOverride.custom2Emission = EditorGUILayout.Slider("Custom2 Emission", curWT.effectsOverride.custom2Emission,0f,1f);
                                }
                                GUILayout.EndVertical();
                                }
                                if(EnviroManager.instance.Aurora != null)
                                {
                                //Aurora
                                GUILayout.BeginVertical ("", boxStyleModified);
                                curWT.showAuroraControls = GUILayout.Toggle(curWT.showAuroraControls, "Aurora", headerFoldout);
                                
                                if(curWT.showAuroraControls) 
                                { 
                                    GUILayout.Space(5);
                                    curWT.auroraOverride.auroraIntensity = EditorGUILayout.Slider("Aurora Intensity Modifier", curWT.auroraOverride.auroraIntensity,0f,1f);
                                } 
                                GUILayout.EndVertical();
                                }
                                if(EnviroManager.instance.Environment != null)
                                {
                                //Environment
                                GUILayout.BeginVertical ("", boxStyleModified);
                                curWT.showEnvironmentControls = GUILayout.Toggle(curWT.showEnvironmentControls, "Environment", headerFoldout);
                                
                                if(curWT.showEnvironmentControls) 
                                { 
                                    GUILayout.Space(5);
                                    curWT.environmentOverride.temperatureWeatherMod = EditorGUILayout.Slider("Temperature Modifier", curWT.environmentOverride.temperatureWeatherMod,-20f,20f);
                                    GUILayout.Space(5);
                                    curWT.environmentOverride.wetnessTarget = EditorGUILayout.Slider("Wetness Target", curWT.environmentOverride.wetnessTarget,0f,1f);
                                    curWT.environmentOverride.snowTarget = EditorGUILayout.Slider("Snow Target", curWT.environmentOverride.snowTarget,0f,1f);
                                    GUILayout.Space(10);
                                    curWT.environmentOverride.windDirectionX = EditorGUILayout.Slider("Wind Direction X", curWT.environmentOverride.windDirectionX,-1f,1f);
                                    curWT.environmentOverride.windDirectionY = EditorGUILayout.Slider("Wind Direction Y", curWT.environmentOverride.windDirectionY,-1f,1f);
                                    GUILayout.Space(5);
                                    curWT.environmentOverride.windSpeed = EditorGUILayout.Slider("Wind Speed", curWT.environmentOverride.windSpeed,0f,1f);
                                    curWT.environmentOverride.windTurbulence = EditorGUILayout.Slider("Wind Turbulence", curWT.environmentOverride.windTurbulence,0f,1f);                                } 
                                    GUILayout.EndVertical();
                                }
                                if(EnviroManager.instance.Lightning != null)
                                {
                                //Lightning
                                GUILayout.BeginVertical ("", boxStyleModified);
                                curWT.showLightningControls = GUILayout.Toggle(curWT.showLightningControls, "Lightning", headerFoldout);
                                
                                if(curWT.showLightningControls) 
                                {
                                    GUILayout.Space(5);
                                    curWT.lightningOverride.lightningStorm = EditorGUILayout.Toggle("Lightning Storm", curWT.lightningOverride.lightningStorm);
                                    curWT.lightningOverride.randomLightningDelay = EditorGUILayout.Slider("Lightning Delay", curWT.lightningOverride.randomLightningDelay,1f,60f);
                                }
                                GUILayout.EndVertical();
                                }
                                if(EnviroManager.instance.Audio != null)
                                {
                                //Audio
                                GUILayout.BeginVertical ("", boxStyleModified);
                                curWT.showAudioControls = GUILayout.Toggle(curWT.showAudioControls, "Audio", headerFoldout);
                                
                                if(curWT.showAudioControls)
                                {        
                                    GUILayout.Space(5);
                                    //Ambient SFX
                                    GUILayout.BeginVertical ("", boxStyleModified);
                                    curWT.showAmbientAudioControls = GUILayout.Toggle(curWT.showAmbientAudioControls, "Ambient", headerFoldout);         
                                    if(curWT.showAmbientAudioControls)
                                    {    
                                        GUILayout.Space(10);
                                        if (GUILayout.Button ("Add")) 
                                        {
                                            curWT.audioOverride.ambientOverride.Add (new EnviroAudioOverrideType());
                                        }
                    
                                        GUILayout.Space(10);
                                        
                                        for (int a = 0; a < curWT.audioOverride.ambientOverride.Count; a++) 
                                        {      
                                            GUILayout.BeginVertical ("", boxStyleModified);
                                            EditorGUILayout.BeginHorizontal();
                                            curWT.audioOverride.ambientOverride[a].showEditor = GUILayout.Toggle(curWT.audioOverride.ambientOverride[a].showEditor, curWT.audioOverride.ambientOverride[a].name, headerFoldout);
                                            GUILayout.FlexibleSpace();
                                            if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
                                            { 
                                                curWT.audioOverride.ambientOverride.Remove (curWT.audioOverride.ambientOverride[a]);
                                                return;
                                            }           
                                            EditorGUILayout.EndHorizontal();

                                            if(curWT.audioOverride.ambientOverride[a].showEditor)
                                            {
                                                curWT.audioOverride.ambientOverride[a].name = EditorGUILayout.TextField ("Audio Name", curWT.audioOverride.ambientOverride[a].name);
                                                curWT.audioOverride.ambientOverride[a].volume = EditorGUILayout.Slider ("Volume", curWT.audioOverride.ambientOverride[a].volume,0f,1f);                                      
                                            } 
                                            GUILayout.EndVertical ();
                                        }
                                    }
                                    GUILayout.EndVertical ();

                                    //Weather SFX
                                    GUILayout.BeginVertical ("", boxStyleModified);
                                    curWT.showWeatherAudioControls = GUILayout.Toggle(curWT.showWeatherAudioControls, "Weather", headerFoldout);         
                                    if(curWT.showWeatherAudioControls)
                                    {     
                                        GUILayout.Space(10);
                                        if (GUILayout.Button ("Add")) 
                                        {
                                            curWT.audioOverride.weatherOverride.Add (new EnviroAudioOverrideType());
                                        }
                    
                                        GUILayout.Space(10);
                                        
                                        for (int a = 0; a < curWT.audioOverride.weatherOverride.Count; a++) 
                                        {      
                                            GUILayout.BeginVertical ("", boxStyleModified);
                                            EditorGUILayout.BeginHorizontal();
                                            curWT.audioOverride.weatherOverride[a].showEditor = GUILayout.Toggle(curWT.audioOverride.weatherOverride[a].showEditor, curWT.audioOverride.weatherOverride[a].name, headerFoldout);
                                            GUILayout.FlexibleSpace();
                                            if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
                                            { 
                                                curWT.audioOverride.weatherOverride.Remove (curWT.audioOverride.weatherOverride[a]);
                                                return;
                                            }           
                                            EditorGUILayout.EndHorizontal();

                                            if(curWT.audioOverride.weatherOverride[a].showEditor)
                                            {
                                                curWT.audioOverride.weatherOverride[a].name = EditorGUILayout.TextField ("Audio Name", curWT.audioOverride.weatherOverride[a].name);
                                                curWT.audioOverride.weatherOverride[a].volume = EditorGUILayout.Slider ("Volume", curWT.audioOverride.weatherOverride[a].volume,0f,1f);                                      
                                            } 
                                            GUILayout.EndVertical ();
                                        }
                                    }
                                    GUILayout.EndVertical ();
                                }
                                GUILayout.EndVertical();
                                }
                                //END
                            }
                            GUILayout.EndVertical ();
                            GUILayout.Space(2.5f);
                    }

                    GUILayout.Space(10);
                    GUILayout.Label("Transition", headerStyle);
                    EditorGUILayout.PropertyField(cloudsTransitionSpeed);
                    EditorGUILayout.PropertyField(fogTransitionSpeed);
                    EditorGUILayout.PropertyField(lightingTransitionSpeed);
                    EditorGUILayout.PropertyField(effectsTransitionSpeed);
                    EditorGUILayout.PropertyField(auroraTransitionSpeed); 
                    EditorGUILayout.PropertyField(environmentTransitionSpeed);           
                    EditorGUILayout.PropertyField(audioTransitionSpeed);

                GUILayout.EndVertical ();

               
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
