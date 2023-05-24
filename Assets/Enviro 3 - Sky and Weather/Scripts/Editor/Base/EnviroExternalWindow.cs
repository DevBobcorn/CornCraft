using UnityEditor;
using UnityEngine;

public class EnviroExternalWindow : EditorWindow
{
    private Editor currentTimeModuleEditor, currentSkyModuleEditor, currentLightingModuleEditor, currentFogModuleEditor, currentVolumetricCloudModuleEditor,currentFlatCloudModuleEditor,currentWeatherModuleEditor,currentAuroraModuleEditor,currentLightningModuleEditor, currentAudioModuleEditor,currentEnvironmentModuleEditor,currentEffectsModuleEditor ,currentQualityModuleEditor,currentEventModuleEditor ;   
    private Vector2 scrollPosition = Vector2.zero;
    // Add menu item named "My Window" to the Window menu
    [MenuItem("Window/Enviro/Enviro Window")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(EnviroExternalWindow));
    }
    
    void OnGUI()
    {
        if (Enviro.EnviroManager.instance == null)
        {
            GUILayout.Label ("Enviro 3 not in Scene. Please use this window in a Scene with Enviro 3.", EditorStyles.boldLabel);
            return;
        }

        GUILayout.Label ("Enviro 3", EditorStyles.boldLabel);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false,  GUILayout.Width(400),  GUILayout.Height(600)); 

            if(Enviro.EnviroManager.instance.Time != null)
            {
                if(currentTimeModuleEditor == null)
                currentTimeModuleEditor = Editor.CreateEditor(Enviro.EnviroManager.instance.Time);

                currentTimeModuleEditor.OnInspectorGUI();
            }

            if(Enviro.EnviroManager.instance.Lighting != null)
                {
                    if(currentLightingModuleEditor == null)
                    currentLightingModuleEditor = Editor.CreateEditor(Enviro.EnviroManager.instance.Lighting);

                    currentLightingModuleEditor.OnInspectorGUI();
                } 

                if(Enviro.EnviroManager.instance.Sky != null)
                {
                    if(currentSkyModuleEditor == null)
                    currentSkyModuleEditor = Editor.CreateEditor(Enviro.EnviroManager.instance.Sky);

                    currentSkyModuleEditor.OnInspectorGUI();
                } 

                if(Enviro.EnviroManager.instance.Fog != null)
                {
                    if(currentFogModuleEditor == null)
                    currentFogModuleEditor = Editor.CreateEditor(Enviro.EnviroManager.instance.Fog);

                    currentFogModuleEditor.OnInspectorGUI();
                } 

                if(Enviro.EnviroManager.instance.VolumetricClouds != null)
                {
                    if(currentVolumetricCloudModuleEditor == null)
                    currentVolumetricCloudModuleEditor = Editor.CreateEditor(Enviro.EnviroManager.instance.VolumetricClouds);

                    currentVolumetricCloudModuleEditor.OnInspectorGUI();
                } 
         
                if(Enviro.EnviroManager.instance.FlatClouds != null)
                {
                    if(currentFlatCloudModuleEditor == null)
                    currentFlatCloudModuleEditor = Editor.CreateEditor(Enviro.EnviroManager.instance.FlatClouds);

                    currentFlatCloudModuleEditor.OnInspectorGUI();
                } 

                if(Enviro.EnviroManager.instance.Aurora != null)
                {
                    if(currentAuroraModuleEditor == null)
                    currentAuroraModuleEditor = Editor.CreateEditor(Enviro.EnviroManager.instance.Aurora);

                    currentAuroraModuleEditor.OnInspectorGUI();
                } 

                if(Enviro.EnviroManager.instance.Lightning != null)
                {
                    if(currentLightningModuleEditor == null)
                    currentLightningModuleEditor = Editor.CreateEditor(Enviro.EnviroManager.instance.Lightning);

                    currentLightningModuleEditor.OnInspectorGUI();
                } 

                if(Enviro.EnviroManager.instance.Environment != null)
                {
                    if(currentEnvironmentModuleEditor == null)
                    currentEnvironmentModuleEditor = Editor.CreateEditor(Enviro.EnviroManager.instance.Environment);

                    currentEnvironmentModuleEditor.OnInspectorGUI();
                } 

                if(Enviro.EnviroManager.instance.Weather != null)
                {
                    if(currentWeatherModuleEditor == null)
                    currentWeatherModuleEditor = Editor.CreateEditor(Enviro.EnviroManager.instance.Weather);

                    currentWeatherModuleEditor.OnInspectorGUI();
                } 

                if(Enviro.EnviroManager.instance.Audio != null) 
                {
                    if(currentAudioModuleEditor == null)
                    currentAudioModuleEditor = Editor.CreateEditor(Enviro.EnviroManager.instance.Audio);

                    currentAudioModuleEditor.OnInspectorGUI();
                } 

                if(Enviro.EnviroManager.instance.Effects != null)
                {
                    if(currentEffectsModuleEditor == null)
                    currentEffectsModuleEditor = Editor.CreateEditor(Enviro.EnviroManager.instance.Effects);

                    currentEffectsModuleEditor.OnInspectorGUI();
                } 
    
                if(Enviro.EnviroManager.instance.Quality != null)
                {
                    if(currentQualityModuleEditor == null)
                    currentQualityModuleEditor = Editor.CreateEditor(Enviro.EnviroManager.instance.Quality);

                    currentQualityModuleEditor.OnInspectorGUI();
                }

        GUILayout.EndScrollView();
    }
}