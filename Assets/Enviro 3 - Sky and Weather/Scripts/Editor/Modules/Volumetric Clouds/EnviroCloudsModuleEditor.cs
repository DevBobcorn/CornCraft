using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Enviro
{
    [CustomEditor(typeof(EnviroVolumetricCloudsModule))]
    public class EnviroCloudsModuleEditor : EnviroModuleEditor
    {  
        private EnviroVolumetricCloudsModule myTarget; 
        private SerializedProperty dualLayer,depthBlending,sunLightColorGradient,moonLightColorGradient, ambientColorGradient,ambientLighIntensity,cloudShadows, cloudShadowsIntensity,
        
        noise, detailNoise, curlTex, blueNoise, cloudsWorldScale, atmosphereColorSaturateDistance, cloudsTravelSpeed;
        //Properties Layer 1
        private SerializedProperty bottomCloudsHeightLayer1,topCloudsHeightLayer1,densityLayer1, coverageLayer1,worleyFreq1Layer1, worleyFreq2Layer1, dilateCoverageLayer1, dilateTypeLayer1,cloudsTypeModifierLayer1, locationOffsetLayer1,
        scatteringIntensityLayer1, silverLiningSpreadLayer1, powderIntensityLayer1, 
        curlIntensityLayer1, lightStepModifierLayer1, lightAbsorbtionLayer1,baseNoiseUVLayer1, detailNoiseUVLayer1,
        baseErosionIntensityLayer1, detailErosionIntensityLayer1, multiScatteringALayer1, multiScatteringBLayer1,multiScatteringCLayer1,anvilBiasLayer1;  
    
        //Properties Layer 2
        private SerializedProperty bottomCloudsHeightLayer2,topCloudsHeightLayer2,densityLayer2,coverageLayer2,worleyFreq1Layer2, worleyFreq2Layer2, dilateCoverageLayer2, dilateTypeLayer2,cloudsTypeModifierLayer2, locationOffsetLayer2,
        scatteringIntensityLayer2, silverLiningSpreadLayer2, powderIntensityLayer2, 
        curlIntensityLayer2, lightStepModifierLayer2, lightAbsorbtionLayer2, baseNoiseUVLayer2, detailNoiseUVLayer2,
        baseErosionIntensityLayer2, detailErosionIntensityLayer2, multiScatteringALayer2, multiScatteringBLayer2,multiScatteringCLayer2, anvilBiasLayer2;  
        //Properties Quality
        private SerializedProperty volumetricClouds, downsampling, stepsLayer1, stepsLayer2, blueNoiseIntensity, reprojectionBlendTime, lodDistance;

        private SerializedProperty windSpeedModifierLayer1, windUpwardsLayer1, cloudsWindDirectionXModifierLayer1, cloudsWindDirectionYModifierLayer1;
        private SerializedProperty windSpeedModifierLayer2, windUpwardsLayer2, cloudsWindDirectionXModifierLayer2, cloudsWindDirectionYModifierLayer2;
        //On Enable
        public override void OnEnable()
        {
            if(!target)
                return;

            myTarget = (EnviroVolumetricCloudsModule)target;
            serializedObj = new SerializedObject(myTarget);
            preset = serializedObj.FindProperty("preset");

            ambientColorGradient = serializedObj.FindProperty("settingsGlobal.ambientColorGradient");
            ambientLighIntensity = serializedObj.FindProperty("settingsGlobal.ambientLighIntensity"); 
            sunLightColorGradient = serializedObj.FindProperty("settingsGlobal.sunLightColorGradient");
            moonLightColorGradient = serializedObj.FindProperty("settingsGlobal.moonLightColorGradient");
            depthBlending = serializedObj.FindProperty("settingsGlobal.depthBlending"); 
            dualLayer = serializedObj.FindProperty("settingsGlobal.dualLayer"); 
            cloudShadows = serializedObj.FindProperty("settingsGlobal.cloudShadows");      
            cloudShadowsIntensity = serializedObj.FindProperty("settingsGlobal.cloudShadowsIntensity"); 
            noise = serializedObj.FindProperty("settingsGlobal.noise"); 
            detailNoise = serializedObj.FindProperty("settingsGlobal.detailNoise"); 
            curlTex = serializedObj.FindProperty("settingsGlobal.curlTex"); 
            blueNoise = serializedObj.FindProperty("settingsGlobal.blueNoise"); 
            cloudsWorldScale = serializedObj.FindProperty("settingsGlobal.cloudsWorldScale"); 
            atmosphereColorSaturateDistance = serializedObj.FindProperty("settingsGlobal.atmosphereColorSaturateDistance");         
            cloudsTravelSpeed = serializedObj.FindProperty("settingsGlobal.cloudsTravelSpeed");         
             
            //Quality
            volumetricClouds = serializedObj.FindProperty("settingsQuality.volumetricClouds"); 
            downsampling = serializedObj.FindProperty("settingsQuality.downsampling"); 
            stepsLayer1 = serializedObj.FindProperty("settingsQuality.stepsLayer1"); 
            stepsLayer2 = serializedObj.FindProperty("settingsQuality.stepsLayer2"); 
            blueNoiseIntensity = serializedObj.FindProperty("settingsQuality.blueNoiseIntensity"); 
            reprojectionBlendTime = serializedObj.FindProperty("settingsQuality.reprojectionBlendTime"); 
            lodDistance = serializedObj.FindProperty("settingsQuality.lodDistance"); 

            //Layer 1
            bottomCloudsHeightLayer1 = serializedObj.FindProperty("settingsLayer1.bottomCloudsHeight"); 
            topCloudsHeightLayer1 = serializedObj.FindProperty("settingsLayer1.topCloudsHeight");           
            coverageLayer1 = serializedObj.FindProperty("settingsLayer1.coverage"); 
            worleyFreq1Layer1 = serializedObj.FindProperty("settingsLayer1.worleyFreq1"); 
            worleyFreq2Layer1 = serializedObj.FindProperty("settingsLayer1.worleyFreq2"); 
            dilateCoverageLayer1 = serializedObj.FindProperty("settingsLayer1.dilateCoverage"); 
            dilateTypeLayer1 = serializedObj.FindProperty("settingsLayer1.dilateType"); 
            cloudsTypeModifierLayer1 = serializedObj.FindProperty("settingsLayer1.cloudsTypeModifier"); 
            locationOffsetLayer1 = serializedObj.FindProperty("settingsLayer1.locationOffset"); 
            densityLayer1 = serializedObj.FindProperty("settingsLayer1.density");  
            scatteringIntensityLayer1 = serializedObj.FindProperty("settingsLayer1.scatteringIntensity");  
            silverLiningSpreadLayer1 = serializedObj.FindProperty("settingsLayer1.silverLiningSpread");  
            powderIntensityLayer1 = serializedObj.FindProperty("settingsLayer1.powderIntensity");  
            curlIntensityLayer1 = serializedObj.FindProperty("settingsLayer1.curlIntensity");  
            lightStepModifierLayer1 = serializedObj.FindProperty("settingsLayer1.lightStepModifier");  
            lightAbsorbtionLayer1 = serializedObj.FindProperty("settingsLayer1.lightAbsorbtion");
            baseNoiseUVLayer1 = serializedObj.FindProperty("settingsLayer1.baseNoiseUV");
            detailNoiseUVLayer1 = serializedObj.FindProperty("settingsLayer1.detailNoiseUV");
            baseErosionIntensityLayer1 = serializedObj.FindProperty("settingsLayer1.baseErosionIntensity");
            detailErosionIntensityLayer1 = serializedObj.FindProperty("settingsLayer1.detailErosionIntensity");
            multiScatteringALayer1 = serializedObj.FindProperty("settingsLayer1.multiScatteringA");
            multiScatteringBLayer1 = serializedObj.FindProperty("settingsLayer1.multiScatteringB");
            multiScatteringCLayer1 = serializedObj.FindProperty("settingsLayer1.multiScatteringC");
            anvilBiasLayer1 = serializedObj.FindProperty("settingsLayer1.anvilBias");

            windSpeedModifierLayer1 = serializedObj.FindProperty("settingsLayer1.windSpeedModifier"); 
            windUpwardsLayer1 = serializedObj.FindProperty("settingsLayer1.windUpwards"); 
            cloudsWindDirectionXModifierLayer1 = serializedObj.FindProperty("settingsLayer1.cloudsWindDirectionXModifier"); 
            cloudsWindDirectionYModifierLayer1 = serializedObj.FindProperty("settingsLayer1.cloudsWindDirectionYModifier"); 

            //Layer 2
            bottomCloudsHeightLayer2= serializedObj.FindProperty("settingsLayer2.bottomCloudsHeight"); 
            topCloudsHeightLayer2= serializedObj.FindProperty("settingsLayer2.topCloudsHeight"); 
            coverageLayer2 = serializedObj.FindProperty("settingsLayer2.coverage"); 
            worleyFreq1Layer2 = serializedObj.FindProperty("settingsLayer2.worleyFreq1"); 
            worleyFreq2Layer2 = serializedObj.FindProperty("settingsLayer2.worleyFreq2"); 
            dilateCoverageLayer2 = serializedObj.FindProperty("settingsLayer2.dilateCoverage"); 
            dilateTypeLayer2 = serializedObj.FindProperty("settingsLayer2.dilateType"); 
            cloudsTypeModifierLayer2 = serializedObj.FindProperty("settingsLayer2.cloudsTypeModifier"); 
            locationOffsetLayer2 = serializedObj.FindProperty("settingsLayer2.locationOffset"); 
            densityLayer2 = serializedObj.FindProperty("settingsLayer2.density");
            scatteringIntensityLayer2 = serializedObj.FindProperty("settingsLayer2.scatteringIntensity");  
            silverLiningSpreadLayer2 = serializedObj.FindProperty("settingsLayer2.silverLiningSpread");  
            powderIntensityLayer2 = serializedObj.FindProperty("settingsLayer2.powderIntensity");  
            curlIntensityLayer2 = serializedObj.FindProperty("settingsLayer2.curlIntensity");  
            lightStepModifierLayer2 = serializedObj.FindProperty("settingsLayer2.lightStepModifier");  
            lightAbsorbtionLayer2  = serializedObj.FindProperty("settingsLayer2.lightAbsorbtion");
            baseNoiseUVLayer2 = serializedObj.FindProperty("settingsLayer2.baseNoiseUV");
            detailNoiseUVLayer2 = serializedObj.FindProperty("settingsLayer2.detailNoiseUV");
            baseErosionIntensityLayer2 = serializedObj.FindProperty("settingsLayer2.baseErosionIntensity");
            detailErosionIntensityLayer2 = serializedObj.FindProperty("settingsLayer2.detailErosionIntensity");
            multiScatteringALayer2 = serializedObj.FindProperty("settingsLayer2.multiScatteringA");
            multiScatteringBLayer2 = serializedObj.FindProperty("settingsLayer2.multiScatteringB");
            multiScatteringCLayer2 = serializedObj.FindProperty("settingsLayer2.multiScatteringC");
            anvilBiasLayer2 = serializedObj.FindProperty("settingsLayer2.anvilBias");

            windSpeedModifierLayer2 = serializedObj.FindProperty("settingsLayer2.windSpeedModifier"); 
            windUpwardsLayer2 = serializedObj.FindProperty("settingsLayer2.windUpwards"); 
            cloudsWindDirectionXModifierLayer2 = serializedObj.FindProperty("settingsLayer2.cloudsWindDirectionXModifier"); 
            cloudsWindDirectionYModifierLayer2 = serializedObj.FindProperty("settingsLayer2.cloudsWindDirectionYModifier"); 
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
            myTarget.showModuleInspector = GUILayout.Toggle(myTarget.showModuleInspector, "Volumetric Clouds", headerFoldout);
            
  
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
            {
                EnviroManager.instance.RemoveModule(EnviroManager.ModuleType.VolumetricClouds);
                DestroyImmediate(this);
                return;
            }                      
            EditorGUILayout.EndHorizontal();
            
            if(myTarget.showModuleInspector)
            {
                RenderDisableInputBox();
                serializedObj.UpdateIfRequiredOrScript ();
                EditorGUI.BeginChangeCheck();


                GUILayout.Space(10);

                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showGlobalControls = GUILayout.Toggle(myTarget.showGlobalControls, "Global Settings", headerFoldout);            
                if(myTarget.showGlobalControls)
                { 
                    GUILayout.Space(5);
                    GUILayout.Label("Quality", headerStyle);
                    DisableInputStartQuality();
                    EditorGUILayout.PropertyField(volumetricClouds);
                    DisableInputEndQuality(); 
                    EditorGUILayout.PropertyField(depthBlending);
                    DisableInputStartQuality();
                    GUILayout.Space(5);
                    EditorGUILayout.PropertyField(downsampling);  
                    EditorGUILayout.PropertyField(dualLayer);              
                    GUILayout.Space(5);
                    EditorGUILayout.PropertyField(stepsLayer1);
                    EditorGUILayout.PropertyField(stepsLayer2);
                    GUILayout.Space(5);
                    EditorGUILayout.PropertyField(blueNoiseIntensity);
                    EditorGUILayout.PropertyField(reprojectionBlendTime);
                    GUILayout.Space(5);
                    EditorGUILayout.PropertyField(lodDistance);
                    DisableInputEndQuality();
                    EditorGUILayout.PropertyField(cloudsWorldScale); 
                    GUILayout.Space(10);
                    GUILayout.Label("Textures", headerStyle);
                    EditorGUILayout.PropertyField(noise);
                    EditorGUILayout.PropertyField(detailNoise); 
                    EditorGUILayout.PropertyField(curlTex);
                    EditorGUILayout.PropertyField(blueNoise);
                    GUILayout.Space(10);
                    GUILayout.Label("Lighting", headerStyle);
                    EditorGUILayout.PropertyField(sunLightColorGradient);
                    EditorGUILayout.PropertyField(moonLightColorGradient);
                    EditorGUILayout.PropertyField(ambientColorGradient);
                    EditorGUILayout.PropertyField(ambientLighIntensity);
                    EditorGUILayout.PropertyField(atmosphereColorSaturateDistance);
                    GUILayout.Space(10);
                    GUILayout.Label("Wind", headerStyle);
                    EditorGUILayout.PropertyField(cloudsTravelSpeed);                 
                    GUILayout.Space(10);
                    GUILayout.Label("Shadows", headerStyle);
                    EditorGUILayout.PropertyField(cloudShadows);
                    EditorGUILayout.PropertyField(cloudShadowsIntensity);
                    
                }
                GUILayout.EndVertical();
                
                //Layer 1
                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showLayer1Controls = GUILayout.Toggle(myTarget.showLayer1Controls, "Settings: Layer 1", headerFoldout);            
                if(myTarget.showLayer1Controls)
                {
                    //Coverage
                    GUILayout.BeginVertical("",boxStyleModified);
                    myTarget.showCoverageControls = GUILayout.Toggle(myTarget.showCoverageControls, "Coverage", headerFoldout);
                    
                    if(myTarget.showCoverageControls)
                    {                          
                        EditorGUILayout.PropertyField(bottomCloudsHeightLayer1);
                        EditorGUILayout.PropertyField(topCloudsHeightLayer1);

                        GUILayout.Space(10);
                        DisableInputStart();
                        EditorGUILayout.PropertyField(coverageLayer1);
                        DisableInputEnd();
                        EditorGUILayout.PropertyField(worleyFreq1Layer1);
                        EditorGUILayout.PropertyField(worleyFreq2Layer1);
                        DisableInputStart();
                        EditorGUILayout.PropertyField(dilateCoverageLayer1);
                        EditorGUILayout.PropertyField(dilateTypeLayer1);
                        EditorGUILayout.PropertyField(cloudsTypeModifierLayer1);
                        EditorGUILayout.PropertyField(anvilBiasLayer1);              
                        DisableInputEnd();
                        EditorGUILayout.PropertyField(locationOffsetLayer1);
                    }
                    GUILayout.EndVertical(); 

                    //Lighting
                    GUILayout.BeginVertical("",boxStyleModified);
                    myTarget.showLightingControls = GUILayout.Toggle(myTarget.showLightingControls, "Lighting", headerFoldout);
                    
                    if(myTarget.showLightingControls)
                    {
                        DisableInputStart();
                        EditorGUILayout.PropertyField(scatteringIntensityLayer1);
                        
                        EditorGUILayout.PropertyField(multiScatteringALayer1); 
                        EditorGUILayout.PropertyField(multiScatteringBLayer1); 
                        EditorGUILayout.PropertyField(multiScatteringCLayer1);               
                        GUILayout.Space(10);       
                        EditorGUILayout.PropertyField(silverLiningSpreadLayer1);         
                        EditorGUILayout.PropertyField(powderIntensityLayer1);         
                        GUILayout.Space(10);
                        EditorGUILayout.PropertyField(lightAbsorbtionLayer1);  
                        DisableInputEnd(); 
                        EditorGUILayout.PropertyField(lightStepModifierLayer1);   
                    }
                    GUILayout.EndVertical(); 

                    //Density
                    GUILayout.BeginVertical("",boxStyleModified);
                    myTarget.showDensityControls = GUILayout.Toggle(myTarget.showDensityControls, "Density", headerFoldout);
                    
                    if(myTarget.showDensityControls)
                    {
                        DisableInputStart();
                        EditorGUILayout.PropertyField(densityLayer1); 
                        DisableInputEnd();   
                        EditorGUILayout.PropertyField(baseNoiseUVLayer1);   
                        EditorGUILayout.PropertyField(detailNoiseUVLayer1);
                        DisableInputStart();
                        EditorGUILayout.PropertyField(baseErosionIntensityLayer1);   
                        EditorGUILayout.PropertyField(detailErosionIntensityLayer1);
                        EditorGUILayout.PropertyField(curlIntensityLayer1);  
                        DisableInputEnd();    
                    }
                    GUILayout.EndVertical();    

                    //Wind
                    GUILayout.BeginVertical("",boxStyleModified);
                    myTarget.showWindControls = GUILayout.Toggle(myTarget.showWindControls, "Wind", headerFoldout);
                    
                    if(myTarget.showWindControls)
                    {
                        EditorGUILayout.PropertyField(windSpeedModifierLayer1);
                        EditorGUILayout.PropertyField(windUpwardsLayer1);
                        GUILayout.Space(5);
                        EditorGUILayout.PropertyField(cloudsWindDirectionXModifierLayer1);    
                        EditorGUILayout.PropertyField(cloudsWindDirectionYModifierLayer1);    
                    }
                    GUILayout.EndVertical();   
                }
                GUILayout.EndVertical(); 
                //Layer 1 End

                if(myTarget.settingsGlobal.dualLayer)   {                    
                //Layer 2
                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showLayer2Controls = GUILayout.Toggle(myTarget.showLayer2Controls, "Settings: Layer 2", headerFoldout);            
                if(myTarget.showLayer2Controls)
                { 
                    //Coverage
                    GUILayout.BeginVertical("",boxStyleModified);
                    myTarget.showCoverageControls = GUILayout.Toggle(myTarget.showCoverageControls, "Coverage", headerFoldout);
                    
                    if(myTarget.showCoverageControls)
                    {
                        EditorGUILayout.PropertyField(bottomCloudsHeightLayer2);
                        EditorGUILayout.PropertyField(topCloudsHeightLayer2);
                        GUILayout.Space(10);
                        DisableInputStart();
                        EditorGUILayout.PropertyField(coverageLayer2);
                        DisableInputEnd();
                        EditorGUILayout.PropertyField(worleyFreq1Layer2);
                        EditorGUILayout.PropertyField(worleyFreq2Layer2);
                        DisableInputStart();
                        EditorGUILayout.PropertyField(dilateCoverageLayer2);
                        EditorGUILayout.PropertyField(dilateTypeLayer2);
                        EditorGUILayout.PropertyField(cloudsTypeModifierLayer2);
                        EditorGUILayout.PropertyField(anvilBiasLayer2);
                        DisableInputEnd();
                        EditorGUILayout.PropertyField(locationOffsetLayer2);
                    }
                    GUILayout.EndVertical(); 

                    //Lighting
                    GUILayout.BeginVertical("",boxStyleModified);
                    myTarget.showLightingControls = GUILayout.Toggle(myTarget.showLightingControls, "Lighting", headerFoldout);
                    
                    if(myTarget.showLightingControls)
                    {
                        DisableInputStart();
                        EditorGUILayout.PropertyField(scatteringIntensityLayer2);
                        EditorGUILayout.PropertyField(multiScatteringALayer2); 
                        EditorGUILayout.PropertyField(multiScatteringBLayer2); 
                        EditorGUILayout.PropertyField(multiScatteringCLayer2); 
                        GUILayout.Space(10);          
                        EditorGUILayout.PropertyField(silverLiningSpreadLayer2);         
                        EditorGUILayout.PropertyField(powderIntensityLayer2);         
                        GUILayout.Space(10);   
                        EditorGUILayout.PropertyField(lightAbsorbtionLayer2);
                        DisableInputEnd();   
                        EditorGUILayout.PropertyField(lightStepModifierLayer2);
                    }
                    GUILayout.EndVertical(); 

                    //Density
                    GUILayout.BeginVertical("",boxStyleModified);
                    myTarget.showDensityControls = GUILayout.Toggle(myTarget.showDensityControls, "Density", headerFoldout);
                    
                    if(myTarget.showDensityControls)
                    {
                        DisableInputStart();
                        EditorGUILayout.PropertyField(densityLayer2);    
                        DisableInputEnd();
                        EditorGUILayout.PropertyField(baseNoiseUVLayer2);   
                        EditorGUILayout.PropertyField(detailNoiseUVLayer2); 
                        DisableInputStart();  
                        EditorGUILayout.PropertyField(baseErosionIntensityLayer2);   
                        EditorGUILayout.PropertyField(detailErosionIntensityLayer2); 
                        EditorGUILayout.PropertyField(curlIntensityLayer2);  
                        DisableInputEnd();   
                    }
                    GUILayout.EndVertical(); 

                    //Wind
                    GUILayout.BeginVertical("",boxStyleModified);
                    myTarget.showWindControls = GUILayout.Toggle(myTarget.showWindControls, "Wind", headerFoldout);
                    
                    if(myTarget.showWindControls)
                    {
                        EditorGUILayout.PropertyField(windSpeedModifierLayer2);
                        EditorGUILayout.PropertyField(windUpwardsLayer2);
                        GUILayout.Space(5); 
                        EditorGUILayout.PropertyField(cloudsWindDirectionXModifierLayer2);    
                        EditorGUILayout.PropertyField(cloudsWindDirectionYModifierLayer2);   
                    }
                    GUILayout.EndVertical();      
                } 
                GUILayout.EndVertical();
                //Layer 2 End
                }
                
                
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

                //Apply
                ApplyChanges ();
            }
            GUILayout.EndVertical();

            if(myTarget.showModuleInspector)
             GUILayout.Space(20);
        }
    }
}
