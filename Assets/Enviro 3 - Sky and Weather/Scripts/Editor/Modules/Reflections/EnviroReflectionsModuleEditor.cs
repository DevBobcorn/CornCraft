using UnityEngine;
using UnityEditor;

namespace Enviro
{
    [CustomEditor(typeof(EnviroReflectionsModule))]
    public class EnviroReflectionsModuleEditor : EnviroModuleEditor
    {
        private EnviroReflectionsModule myTarget;

        //Properties
        //Reflection Probe
        private SerializedProperty customRendering, customRenderingTimeSlicing, globalReflectionTimeSlicingMode, globalReflectionsUpdateOnGameTime, globalReflectionsUpdateOnPosition, globalReflectionsIntensity, globalReflectionsTimeTreshold, globalReflectionsPositionTreshold, globalReflectionsScale, globalReflectionResolution, globalReflectionLayers, updateDefaultEnvironmentReflections;

        //On Enable
        public override void OnEnable()
        {

            if(!target)
                return;

            base.OnEnable();

            myTarget = (EnviroReflectionsModule)target;
            serializedObj = new SerializedObject(myTarget);
            preset = serializedObj.FindProperty("preset");
            //Reflection Probe
            updateDefaultEnvironmentReflections = serializedObj.FindProperty("Settings.updateDefaultEnvironmentReflections"); 
            customRendering = serializedObj.FindProperty("Settings.customRendering"); 
            customRenderingTimeSlicing = serializedObj.FindProperty("Settings.customRenderingTimeSlicing"); 
            globalReflectionTimeSlicingMode = serializedObj.FindProperty("Settings.globalReflectionTimeSlicingMode");
            globalReflectionsUpdateOnGameTime = serializedObj.FindProperty("Settings.globalReflectionsUpdateOnGameTime");
            globalReflectionsUpdateOnPosition = serializedObj.FindProperty("Settings.globalReflectionsUpdateOnPosition");
            globalReflectionsIntensity = serializedObj.FindProperty("Settings.globalReflectionsIntensity");
            globalReflectionsTimeTreshold = serializedObj.FindProperty("Settings.globalReflectionsTimeTreshold");
            globalReflectionsPositionTreshold = serializedObj.FindProperty("Settings.globalReflectionsPositionTreshold");
            globalReflectionsScale = serializedObj.FindProperty("Settings.globalReflectionsScale");
            globalReflectionResolution = serializedObj.FindProperty("Settings.globalReflectionResolution");
            globalReflectionLayers = serializedObj.FindProperty("Settings.globalReflectionLayers");
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
            myTarget.showModuleInspector = GUILayout.Toggle(myTarget.showModuleInspector, "Reflections", headerFoldout);

            GUILayout.FlexibleSpace();
            if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
            {
                EnviroManager.instance.RemoveModule(EnviroManager.ModuleType.Reflections);
                DestroyImmediate(this);
                return;
            }

            EditorGUILayout.EndHorizontal();

            if(myTarget.showModuleInspector)
            {
                //RenderDisableInputBox();
                serializedObj.UpdateIfRequiredOrScript ();
                EditorGUI.BeginChangeCheck();

                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showReflectionControls = GUILayout.Toggle(myTarget.showReflectionControls, "Reflection Controls", headerFoldout);
                if(myTarget.showReflectionControls)
                {
                    EditorGUILayout.PropertyField(globalReflectionsIntensity);
#if !ENVIRO_HDRP
                    EditorGUILayout.PropertyField(globalReflectionResolution);
#endif
                    EditorGUILayout.PropertyField(globalReflectionLayers);
                    EditorGUILayout.PropertyField(globalReflectionsScale);
                    GUILayout.Space(5);
                    EditorGUILayout.PropertyField(updateDefaultEnvironmentReflections);
                    GUILayout.Space(5);
                    EditorGUILayout.PropertyField(customRendering);
                    if(myTarget.Settings.customRendering)
                    EditorGUILayout.PropertyField(customRenderingTimeSlicing);
                    else
                    EditorGUILayout.PropertyField(globalReflectionTimeSlicingMode);

                    GUILayout.Space(10);
                    EditorGUILayout.PropertyField(globalReflectionsUpdateOnGameTime);
                    if(myTarget.Settings.globalReflectionsUpdateOnGameTime)
                    EditorGUILayout.PropertyField(globalReflectionsTimeTreshold);
                        GUILayout.Space(5);
                    EditorGUILayout.PropertyField(globalReflectionsUpdateOnPosition);
                    if(myTarget.Settings.globalReflectionsUpdateOnPosition)
                    EditorGUILayout.PropertyField(globalReflectionsPositionTreshold);
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
