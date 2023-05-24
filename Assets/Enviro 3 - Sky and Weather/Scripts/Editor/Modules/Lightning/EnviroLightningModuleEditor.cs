using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Enviro
{
    [CustomEditor(typeof(EnviroLightningModule))]
    public class EnviroLightningModuleEditor : EnviroModuleEditor
    {  
        private EnviroLightningModule myTarget; 

        //Properties
        private SerializedProperty prefab, lightningStorm,randomLightingDelay, randomSpawnRange, randomTargetRange;


        //On Enable
        public override void OnEnable()
        {
            base.OnEnable();

            if(!target)
                return;

            myTarget = (EnviroLightningModule)target;
            serializedObj = new SerializedObject(myTarget);
            preset = serializedObj.FindProperty("preset");
            prefab = serializedObj.FindProperty("Settings.prefab"); 
            lightningStorm = serializedObj.FindProperty("Settings.lightningStorm");  
            randomLightingDelay = serializedObj.FindProperty("Settings.randomLightingDelay"); 
            randomSpawnRange = serializedObj.FindProperty("Settings.randomSpawnRange"); 
            randomTargetRange = serializedObj.FindProperty("Settings.randomTargetRange"); 
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
            myTarget.showModuleInspector = GUILayout.Toggle(myTarget.showModuleInspector, "Lightning", headerFoldout);
            
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
            {
                EnviroManager.instance.RemoveModule(EnviroManager.ModuleType.Lightning); //Add Remove
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
                myTarget.showLightningControls = GUILayout.Toggle(myTarget.showLightningControls, "Lightning Controls", headerFoldout);               
                if(myTarget.showLightningControls)
                {
                    GUILayout.Space(10);
                    DisableInputStart();
                    EditorGUILayout.PropertyField(lightningStorm);
                    DisableInputEnd();
                    GUILayout.Space(5);
                    EditorGUILayout.PropertyField(prefab);      
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Random Lighting Storm", headerStyle);
                    DisableInputStart();
                    EditorGUILayout.PropertyField(randomLightingDelay);
                    DisableInputEnd();
                    EditorGUILayout.PropertyField(randomSpawnRange);
                    EditorGUILayout.PropertyField(randomTargetRange);
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
