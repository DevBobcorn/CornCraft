using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Enviro{
    [CustomEditor(typeof(EnviroManager))]
    public class EnviroManagerInspector : EnviroBaseInspector
    {
        private EnviroManager myTarget;

        private Editor currentTimeModuleEditor, currentSkyModuleEditor, currentLightingModuleEditor, currentReflectionsModuleEditor, currentFogModuleEditor, currentVolumetricCloudModuleEditor,currentFlatCloudModuleEditor,currentWeatherModuleEditor,currentAuroraModuleEditor,currentLightningModuleEditor, currentAudioModuleEditor,currentEnvironmentModuleEditor,currentEffectsModuleEditor ,currentQualityModuleEditor;
        private SerializedProperty configuration, modules, Camera, CameraTag, dontDestroyOnLoad ,worldAnchor;
        private SerializedProperty sunRotationX,sunRotationY,moonRotationX,moonRotationY,dayNightSwitch;
        //Events
        private SerializedProperty onHourPassedActions, onDayPassedActions, onYearPassedActions, onWeatherChangedActions, onSeasonChangedActions, onNightActions, onDayActions;

        void OnEnable()
        {
            myTarget = (EnviroManager)target;
            serializedObj = new SerializedObject(myTarget);
            configuration = serializedObj.FindProperty("configuration");
            Camera = serializedObj.FindProperty("Camera");
            CameraTag = serializedObj.FindProperty("CameraTag");
            dontDestroyOnLoad = serializedObj.FindProperty("dontDestroyOnLoad");
            sunRotationX = serializedObj.FindProperty("sunRotationX");
            sunRotationY = serializedObj.FindProperty("sunRotationY");
            moonRotationX = serializedObj.FindProperty("moonRotationX");
            moonRotationY = serializedObj.FindProperty("moonRotationY");
            dayNightSwitch = serializedObj.FindProperty("dayNightSwitch");
            worldAnchor = serializedObj.FindProperty("Objects.worldAnchor");
            //Events
            onHourPassedActions = serializedObj.FindProperty("Events.onHourPassedActions");
            onDayPassedActions = serializedObj.FindProperty("Events.onDayPassedActions");
            onYearPassedActions = serializedObj.FindProperty("Events.onYearPassedActions");
            onWeatherChangedActions = serializedObj.FindProperty("Events.onWeatherChangedActions");
            onSeasonChangedActions = serializedObj.FindProperty("Events.onSeasonChangedActions");
            onNightActions = serializedObj.FindProperty("Events.onNightActions");
            onDayActions = serializedObj.FindProperty("Events.onDayActions");
        }

        public override void OnInspectorGUI()
        {
            SetupGUIStyles();

            GUILayout.BeginVertical("", boxStyle);
            GUILayout.Label("Enviro - Sky and Weather Manager",headerStyleMid);
            GUILayout.Space(5);
            GUILayout.Label("Version: 3.0.6", headerStyleMid);


            //Help Box Button
            //RenderHelpBoxButton();

           // if(showHelpBox)
           // RenderHelpBox("This is a help text test!");

            GUILayout.EndVertical();

            GUILayout.BeginVertical("",boxStyle);
            myTarget.showSetup = GUILayout.Toggle(myTarget.showSetup, "Setup", headerFoldout);

            EditorGUI.BeginChangeCheck();

            if(myTarget.showSetup)
            {
                GUILayout.BeginVertical("",boxStyleModified);
                GUILayout.Label("Camera Setup", headerStyle);

               // GUILayout.Space(10);
               // GUILayout.Label("Main Camera", headerStyle);
                EditorGUILayout.PropertyField(Camera);

                if(myTarget.Camera == null)
                   CameraTag.stringValue = EditorGUILayout.TagField("Camera Tag", CameraTag.stringValue);

                GUILayout.Space(10);
                GUILayout.Label("Additional Cameras", headerStyle);
                GUILayout.Space(5);
                if (GUILayout.Button ("Add"))
                {
                        myTarget.Cameras.Add (null);
                }
                GUILayout.Space(5);
                for (int i = 0; i < myTarget.Cameras.Count; i++)
                {
                    GUILayout.BeginVertical("", boxStyleModified);
                    myTarget.Cameras[i].camera = (Camera)EditorGUILayout.ObjectField ("Camera", myTarget.Cameras[i].camera, typeof(Camera), true);
                    myTarget.Cameras[i].quality = (EnviroQuality)EditorGUILayout.ObjectField ("Quality", myTarget.Cameras[i].quality, typeof(EnviroQuality), true);
                    if (GUILayout.Button ("Remove"))
                    {
                        myTarget.Cameras.RemoveAt (i);
                    }

                    GUILayout.EndVertical();
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical("",boxStyleModified);
                GUILayout.Label("General Setup", headerStyle);
                EditorGUILayout.PropertyField(dontDestroyOnLoad);
                EditorGUILayout.PropertyField(worldAnchor);
                //GUILayout.Space(10);
                //GUILayout.Label("Objects", headerStyle);
                GUILayout.EndVertical();

                GUILayout.BeginVertical("", boxStyleModified);
                //GUILayout.Space(10);

    #if ENVIRO_HDRP
                GUILayout.Label("Render Pipeline:   HDRP", headerStyle);
    #elif ENVIRO_URP
                GUILayout.Label("Render Pipeline:   URP", headerStyle);
    #else
                GUILayout.Label("Render Pipeline:   Legacy", headerStyle);
    #endif

                GUILayout.Space(10);
    #if !ENVIRO_HDRP
                if (GUILayout.Button("Activate HDRP Support"))
                    {
                        AddDefineSymbol("ENVIRO_HDRP");
                        RemoveDefineSymbol("ENVIRO_URP");
                    }
    #endif

    #if !ENVIRO_URP
                    if (GUILayout.Button("Activate URP Support"))
                    {
                        AddDefineSymbol("ENVIRO_URP");
                        RemoveDefineSymbol("ENVIRO_HDRP");
                    }
    #endif

    #if ENVIRO_URP || ENVIRO_HDRP
                if (GUILayout.Button("Activate Legacy Support"))
                    {
                        RemoveDefineSymbol("ENVIRO_URP");
                        RemoveDefineSymbol("ENVIRO_HDRP");
                    }
    #endif
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("", boxStyle);
            myTarget.showModules = GUILayout.Toggle(myTarget.showModules, "Modules", headerFoldout);
            if(myTarget.showModules)
            {

            if(myTarget.configuration == null)
            {
                GUILayout.Label("Please assign or create a new configuration.");
                EditorGUILayout.PropertyField(configuration);
                    if(GUILayout.Button("Create new Configuration"))
                    {
                        myTarget.configuration = EnviroConfigurationCreation.CreateMyAsset();
                        serializedObj.Update();
                    }
            }
            else
            {
                GUILayout.BeginVertical("", boxStyleModified);

                if(!Application.isPlaying)
                EditorGUILayout.PropertyField(configuration);
                  if(GUILayout.Button("Save all Modules"))
                    {
                        myTarget.SaveAllModules();
                    }
                if(GUILayout.Button("Load all Modules"))
                    {
                        myTarget.LoadAllModules();
                    }

                GUILayout.EndVertical();

                GUILayout.BeginVertical("", wrapStyle);
                GUILayout.BeginHorizontal("", headerStyle);

                EditorGUI.BeginDisabledGroup(myTarget.Time != null);
                if(GUILayout.Button("Time"))
                {
                    if (myTarget.Time == null)
                    myTarget.AddModule(EnviroManager.ModuleType.Time);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(myTarget.Sky != null);
                if(GUILayout.Button("Sky"))
                {
                    if (myTarget.Sky == null)
                    myTarget.AddModule(EnviroManager.ModuleType.Sky);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(myTarget.Lighting != null);
                if(GUILayout.Button("Lighting"))
                {
                    if (myTarget.Lighting == null)
                    myTarget.AddModule(EnviroManager.ModuleType.Lighting);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(myTarget.Reflections != null);
                if(GUILayout.Button("Reflections"))
                {
                    if (myTarget.Reflections == null)
                    myTarget.AddModule(EnviroManager.ModuleType.Reflections);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(myTarget.Fog != null);
                if(GUILayout.Button("Fog"))
                {
                    if (myTarget.Fog == null)
                    myTarget.AddModule(EnviroManager.ModuleType.Fog);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(myTarget.VolumetricClouds != null);
                if(GUILayout.Button("Volumetric Clouds"))
                { 
                    if (myTarget.VolumetricClouds == null)
                    myTarget.AddModule(EnviroManager.ModuleType.VolumetricClouds);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(myTarget.FlatClouds != null);
                if(GUILayout.Button("Flat Clouds"))
                {
                    if (myTarget.FlatClouds == null)
                    myTarget.AddModule(EnviroManager.ModuleType.FlatClouds);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(myTarget.Aurora != null);
                if(GUILayout.Button("Aurora"))
                {
                    if (myTarget.Aurora == null)
                    myTarget.AddModule(EnviroManager.ModuleType.Aurora);
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.EndHorizontal();

                //////////////////////////////////////

                GUILayout.BeginHorizontal("", headerStyle);


                EditorGUI.BeginDisabledGroup(myTarget.Environment != null);
                if(GUILayout.Button("Environment"))
                {
                    if (myTarget.Environment == null)
                    myTarget.AddModule(EnviroManager.ModuleType.Environment);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(myTarget.Lightning != null);
                if(GUILayout.Button("Lightning"))
                {
                    if (myTarget.Lightning == null)
                    myTarget.AddModule(EnviroManager.ModuleType.Lightning);
                }
                EditorGUI.EndDisabledGroup();


                EditorGUI.BeginDisabledGroup(myTarget.Weather != null);
                if(GUILayout.Button("Weather"))
                {
                    if (myTarget.Weather == null)
                    myTarget.AddModule(EnviroManager.ModuleType.Weather);
                }
                EditorGUI.EndDisabledGroup();


                EditorGUI.BeginDisabledGroup(myTarget.Audio != null);
                if(GUILayout.Button("Audio"))
                {
                    if (myTarget.Audio == null)
                    myTarget.AddModule(EnviroManager.ModuleType.Audio);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(myTarget.Effects != null);
                if(GUILayout.Button("Effects"))
                {
                    if (myTarget.Effects == null)
                    myTarget.AddModule(EnviroManager.ModuleType.Effects);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(myTarget.Quality != null);
                if(GUILayout.Button("Quality"))
                {
                    if (myTarget.Quality == null)
                    myTarget.AddModule(EnviroManager.ModuleType.Quality);
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();


    /////////Modules Start
                GUILayout.Space(10);
                if(myTarget.Time != null)
                {
                    if(currentTimeModuleEditor == null)
                    currentTimeModuleEditor = Editor.CreateEditor(myTarget.Time);

                    currentTimeModuleEditor.OnInspectorGUI();
                }
                else
                {

                    GUI.backgroundColor = baseModuleColor;
                    GUILayout.BeginVertical("",boxStyleModified);
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.BeginHorizontal();
                    myTarget.showNonTimeControls = GUILayout.Toggle(myTarget.showNonTimeControls, "Sun and Moon Controls", headerFoldout);
                    EditorGUILayout.EndHorizontal();
                    if(myTarget.showNonTimeControls)
                    {
                        EditorGUILayout.LabelField("This module will control your sun and moon position when no time module is used.");
                        serializedObj.UpdateIfRequiredOrScript ();
                        EditorGUI.BeginChangeCheck();
                        GUI.backgroundColor = categoryModuleColor;
                        GUILayout.BeginVertical("",boxStyleModified);
                        GUI.backgroundColor = Color.white;

                        EditorGUILayout.PropertyField(sunRotationX);
                        EditorGUILayout.PropertyField(sunRotationY);
                        EditorGUILayout.PropertyField(moonRotationX);
                        EditorGUILayout.PropertyField(moonRotationY);
                        GUILayout.Space(5);
                        EditorGUILayout.PropertyField(dayNightSwitch);
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndVertical();

                    if(myTarget.showNonTimeControls)
                    GUILayout.Space(10);
                }

                if(myTarget.Lighting != null)
                {
                    if(currentLightingModuleEditor == null)
                    currentLightingModuleEditor = Editor.CreateEditor(myTarget.Lighting);

                    currentLightingModuleEditor.OnInspectorGUI();
                }

                if(myTarget.Reflections != null)
                {
                    if(currentReflectionsModuleEditor == null)
                    currentReflectionsModuleEditor = Editor.CreateEditor(myTarget.Reflections);

                    currentReflectionsModuleEditor.OnInspectorGUI();
                }

                if(myTarget.Sky != null)
                {
                    if(currentSkyModuleEditor == null)
                    currentSkyModuleEditor = Editor.CreateEditor(myTarget.Sky);

                    currentSkyModuleEditor.OnInspectorGUI();
                }

                if(myTarget.Fog != null)
                {
                    if(currentFogModuleEditor == null)
                    currentFogModuleEditor = Editor.CreateEditor(myTarget.Fog);

                    currentFogModuleEditor.OnInspectorGUI();
                }

                if(myTarget.VolumetricClouds != null)
                {
                    if(currentVolumetricCloudModuleEditor == null)
                    currentVolumetricCloudModuleEditor = Editor.CreateEditor(myTarget.VolumetricClouds);

                    currentVolumetricCloudModuleEditor.OnInspectorGUI();
                }

                if(myTarget.FlatClouds != null)
                {
                    if(currentFlatCloudModuleEditor == null)
                    currentFlatCloudModuleEditor = Editor.CreateEditor(myTarget.FlatClouds);

                    currentFlatCloudModuleEditor.OnInspectorGUI();
                }

                if(myTarget.Aurora != null)
                {
                    if(currentAuroraModuleEditor == null)
                    currentAuroraModuleEditor = Editor.CreateEditor(myTarget.Aurora);

                    currentAuroraModuleEditor.OnInspectorGUI();
                }

                if(myTarget.Lightning != null)
                {
                    if(currentLightningModuleEditor == null)
                    currentLightningModuleEditor = Editor.CreateEditor(myTarget.Lightning);

                    currentLightningModuleEditor.OnInspectorGUI();
                }

                if(myTarget.Environment != null)
                {
                    if(currentEnvironmentModuleEditor == null)
                    currentEnvironmentModuleEditor = Editor.CreateEditor(myTarget.Environment);

                    currentEnvironmentModuleEditor.OnInspectorGUI();
                }

                if(myTarget.Weather != null)
                {
                    if(currentWeatherModuleEditor == null)
                    currentWeatherModuleEditor = Editor.CreateEditor(myTarget.Weather);

                    currentWeatherModuleEditor.OnInspectorGUI();
                }

                if(myTarget.Audio != null)
                {
                    if(currentAudioModuleEditor == null)
                    currentAudioModuleEditor = Editor.CreateEditor(myTarget.Audio);

                    currentAudioModuleEditor.OnInspectorGUI();
                }

                if(myTarget.Effects != null)
                {
                    if(currentEffectsModuleEditor == null)
                    currentEffectsModuleEditor = Editor.CreateEditor(myTarget.Effects);

                    currentEffectsModuleEditor.OnInspectorGUI();
                }

                if(myTarget.Quality != null)
                {
                    if(currentQualityModuleEditor == null)
                    currentQualityModuleEditor = Editor.CreateEditor(myTarget.Quality);

                    currentQualityModuleEditor.OnInspectorGUI();
                }
            }
            }
            GUILayout.EndVertical();

            //Modules End

            GUILayout.BeginVertical("",boxStyle);
            myTarget.showEvents = GUILayout.Toggle(myTarget.showEvents, "Events", headerFoldout);

            if(myTarget.showEvents)
            {
                GUI.backgroundColor = thirdPartyModuleColor;
                GUILayout.BeginVertical("", boxStyleModified);
                GUI.backgroundColor = Color.white;
                GUILayout.Space(5);
                EditorGUILayout.PropertyField(onHourPassedActions);
                EditorGUILayout.PropertyField(onDayPassedActions);
                EditorGUILayout.PropertyField(onYearPassedActions);
                GUILayout.Space(5);
                EditorGUILayout.PropertyField(onWeatherChangedActions);
                EditorGUILayout.PropertyField(onSeasonChangedActions);
                GUILayout.Space(5);
                EditorGUILayout.PropertyField(onDayActions);
                EditorGUILayout.PropertyField(onNightActions);
                 GUILayout.EndVertical();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("",boxStyle);
            myTarget.showThirdParty = GUILayout.Toggle(myTarget.showThirdParty, "Third Party Support", headerFoldout);

            if(myTarget.showThirdParty)
            {
                GUILayout.Space(5);

                //WAPI
                GUI.backgroundColor = thirdPartyModuleColor;
                GUILayout.BeginVertical("World Manager API", boxStyleModified);
                GUI.backgroundColor = Color.white;
                GUILayout.Space(20);
#if WORLDAPI_PRESENT

                //GUILayout.Label("World Manager API detected!", headerStyle);
                //GUILayout.Space(5);
                RenderIntegrationTextBox("You can add support for WAPI from the ('Components' -> 'Enviro 3' -> 'Integrations' -> 'WAPI') menu.");
#else
                GUILayout.Label("World Manager API no found!", headerStyle);
#endif
                GUILayout.EndVertical();

                //MicroSplat
                GUI.backgroundColor = thirdPartyModuleColor;
                GUILayout.BeginVertical("MicroSplat", boxStyleModified);
                GUI.backgroundColor = Color.white;
                GUILayout.Space(20);
                RenderIntegrationTextBox("You can add support for MicroSplat and Better Lit Shaders from the ('Components' -> 'Enviro 3' -> 'Integrations' -> 'Microsplat') menu.");
                GUILayout.EndVertical();
                //////////

                //Mirror
                GUI.backgroundColor = thirdPartyModuleColor;
                GUILayout.BeginVertical("Mirror Networking", boxStyleModified);
                GUI.backgroundColor = Color.white;
                GUILayout.Space(20);
#if ENVIRO_MIRROR_SUPPORT
                //GUILayout.Label("Mirror Networking support activated.", headerStyle);
                //GUILayout.Space(5);
                RenderIntegrationTextBox("Please add the 'Mirror Server' component to a new GameObject in your scene. ('Components' -> 'Enviro 3' -> 'Integrations' -> 'Mirror Server')");
                RenderIntegrationTextBox("Please add the 'Mirror Player' component to your player prefab. ('Components' -> 'Enviro 3' -> 'Integrations' -> 'Mirror Player')");
                GUILayout.Space(10);
                if (GUILayout.Button("Deactivate Mirror Support"))
                {
                    RemoveDefineSymbol("ENVIRO_MIRROR_SUPPORT");
                }
#else
                if (GUILayout.Button("Activate Mirror Support"))
                {
                    AddDefineSymbol("ENVIRO_MIRROR_SUPPORT");
                }
                if (GUILayout.Button("Deactivate Mirror Support"))
                {
                    RemoveDefineSymbol("ENVIRO_MIRROR_SUPPORT");
                }
#endif
                GUILayout.EndVertical();
                //////////

            }
            GUILayout.EndVertical();
            ApplyChanges();
        }
    }
}
