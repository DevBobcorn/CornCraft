using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Enviro
{
    [CustomEditor(typeof(EnviroQualityModule))]
    public class EnviroQualityModuleEditor : EnviroModuleEditor
    {  
        private EnviroQualityModule myTarget; 

        //Properties Cirrus
       // private SerializedProperty useCirrusClouds;

        //On Enable
        public override void OnEnable()
        {
            base.OnEnable();

            if(!target)
                return;

            myTarget = (EnviroQualityModule)target;
            serializedObj = new SerializedObject(myTarget);
            preset = serializedObj.FindProperty("preset");

            //useCirrusClouds = serializedObj.FindProperty("settings.useCirrusClouds");
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
            myTarget.showModuleInspector = GUILayout.Toggle(myTarget.showModuleInspector, "Quality", headerFoldout);
             
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
            {
                EnviroManager.instance.RemoveModule(EnviroManager.ModuleType.Quality); 
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
                Object selectedObject = null;
                         
                if(GUILayout.Button("Add"))
                {
                   int controlID = EditorGUIUtility.GetControlID (FocusType.Passive);
                   EditorGUIUtility.ShowObjectPicker<EnviroQuality>(null,false,"",controlID);
                }

                 string commandName = Event.current.commandName;

                if (commandName == "ObjectSelectorClosed") 
                {
                    selectedObject = EditorGUIUtility.GetObjectPickerObject ();
                    
                    bool add = true;
                    
                    for (int i = 0; i < myTarget.Settings.Qualities.Count; i++)
                    {
                        if((EnviroQuality)selectedObject == myTarget.Settings.Qualities[i])
                        add = false; 
                    }

                    if(add)
                      myTarget.Settings.Qualities.Add((EnviroQuality)selectedObject);
                }

                if(GUILayout.Button("Create New"))
                {
                   myTarget.CreateNewQuality();
                } 

                myTarget.CleanupQualityList();
                
                for (int i = 0; i < myTarget.Settings.Qualities.Count; i++) 
                    {       
                        EnviroQuality q =  myTarget.Settings.Qualities[i];
                        
                        if(q == myTarget.Settings.defaultQuality)
                        GUI.backgroundColor = new Color(0.0f,0.5f,0.0f,1f);      
                        GUILayout.BeginVertical("",boxStyleModified);
                        GUI.backgroundColor = Color.white;

                        EditorGUILayout.BeginHorizontal();
                        q.showEditor = GUILayout.Toggle(q.showEditor, q.name, headerFoldout);
                        GUILayout.FlexibleSpace();

                        if(q != myTarget.Settings.defaultQuality)
                        {
                            if(GUILayout.Button("Set Default", EditorStyles.miniButtonRight,GUILayout.Width(75), GUILayout.Height(18)))
                            {
                                myTarget.Settings.defaultQuality = q;
                                EditorUtility.SetDirty(q);
                            } 
                        }
                        if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
                        {
                            myTarget.RemoveQuality(q);
                        } 
                                 
                        EditorGUILayout.EndHorizontal(); 

                        if(q.showEditor)
                        {
                            GUILayout.BeginVertical("",boxStyleModified);

                            EnviroQuality quality = myTarget.Settings.Qualities[i];

                            q.showVolumeClouds = GUILayout.Toggle(q.showVolumeClouds, "Volumetric Clouds", headerFoldout);
                           
                            if(q.showVolumeClouds)
                            {       
                                quality.volumetricCloudsOverride.volumetricClouds = EditorGUILayout.Toggle("Volumetric Clouds",  quality.volumetricCloudsOverride.volumetricClouds);
                                quality.volumetricCloudsOverride.dualLayer = EditorGUILayout.Toggle("Dual Layer", quality.volumetricCloudsOverride.dualLayer);
                                GUILayout.Space(5);
                                quality.volumetricCloudsOverride.downsampling = EditorGUILayout.IntSlider("Downsampling",  quality.volumetricCloudsOverride.downsampling, 1,6);
                                quality.volumetricCloudsOverride.stepsLayer1 = EditorGUILayout.IntSlider("Steps Layer 1",  quality.volumetricCloudsOverride.stepsLayer1, 32,256);
                                quality.volumetricCloudsOverride.stepsLayer2 = EditorGUILayout.IntSlider("Steps Layer 2",  quality.volumetricCloudsOverride.stepsLayer2, 32,256);
                                GUILayout.Space(5); 
                                quality.volumetricCloudsOverride.blueNoiseIntensity = EditorGUILayout.Slider("Blue Noise Intensity",  quality.volumetricCloudsOverride.blueNoiseIntensity, 0f,2f);
                                quality.volumetricCloudsOverride.reprojectionBlendTime = EditorGUILayout.Slider("Reprojection Blending",  quality.volumetricCloudsOverride.reprojectionBlendTime, 1f,20f);
                                quality.volumetricCloudsOverride.lodDistance = EditorGUILayout.Slider("LOD",  quality.volumetricCloudsOverride.lodDistance, 0f,1f); 
                            }  
                            GUILayout.EndVertical();

                            GUILayout.BeginVertical("",boxStyleModified);
                            q.showFog = GUILayout.Toggle(q.showFog, "Fog", headerFoldout);
                           
                            if(q.showFog)
                            {  
                                quality.fogOverride.fog = EditorGUILayout.Toggle("Fog",  quality.fogOverride.fog);
                                quality.fogOverride.volumetrics = EditorGUILayout.Toggle("Volumetrics",  quality.fogOverride.volumetrics);
                                quality.fogOverride.quality = (EnviroFogSettings.Quality)EditorGUILayout.EnumPopup("Quality",quality.fogOverride.quality);
                                quality.fogOverride.steps = EditorGUILayout.IntSlider("Steps",  quality.fogOverride.steps, 16,96);
                            }      
                            GUILayout.EndVertical();  

                            GUILayout.BeginVertical("",boxStyleModified);
                            q.showFlatClouds = GUILayout.Toggle(q.showFlatClouds, "Flat Clouds", headerFoldout);
                           
                            if(q.showFlatClouds)
                            {  
                                quality.flatCloudsOverride.flatClouds = EditorGUILayout.Toggle("Flat Clouds",   quality.flatCloudsOverride.flatClouds);
                                quality.flatCloudsOverride.cirrusClouds = EditorGUILayout.Toggle("Cirrus Clouds",   quality.flatCloudsOverride.cirrusClouds);
                            }      
                            GUILayout.EndVertical();  

                            GUILayout.BeginVertical("",boxStyleModified);
                            q.showAurora = GUILayout.Toggle(q.showAurora, "Aurora", headerFoldout);
                           
                            if(q.showAurora)
                            {  
                                quality.auroraOverride.aurora = EditorGUILayout.Toggle("Aurora",   quality.auroraOverride.aurora);
                                quality.auroraOverride.steps = EditorGUILayout.IntSlider("Steps",  quality.auroraOverride.steps, 6,32);
                            }      
                            GUILayout.EndVertical();


                            EditorUtility.SetDirty(quality);                                  
                        }                
                        GUILayout.EndVertical(); 
                        if(q.showEditor)                           
                          GUILayout.Space(10);   
                    }
                GUILayout.EndVertical(); 

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
