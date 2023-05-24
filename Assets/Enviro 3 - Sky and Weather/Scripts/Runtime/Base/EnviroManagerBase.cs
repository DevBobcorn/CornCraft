using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enviro
{
    [System.Serializable] 
    public class GeneralObjects 
    {
        public GameObject sun;
        public GameObject moon;
        public GameObject stars;
        public Light directionalLight; 
        public Light additionalDirectionalLight; 
        public EnviroReflectionProbe globalReflectionProbe;
        public GameObject effects;
        public GameObject audio;
        public WindZone windZone;
        public GameObject worldAnchor; 
    } 

    [System.Serializable]
    public class EnviroCameras
    {
        public Camera camera;
        public EnviroQuality quality;
    }

    public class EnviroManagerBase : MonoBehaviour
    {
        //Modules
        public EnviroConfiguration configuration;
 
        [SerializeField]
        private EnviroConfiguration lastConfiguration;

        public EnviroTimeModule Time;
        public EnviroLightingModule Lighting;
        public EnviroSkyModule Sky;
        public EnviroFogModule Fog; 
        public EnviroVolumetricCloudsModule VolumetricClouds;
        public EnviroFlatCloudsModule FlatClouds;
        public EnviroWeatherModule Weather;
        public EnviroAuroraModule Aurora;
        public EnviroAudioModule Audio;
        public EnviroEffectsModule Effects;
        public EnviroLightningModule Lightning;
        public EnviroQualityModule Quality;
        public EnviroEnvironmentModule Environment;
        //public EnviroEventModule Events;

        //Enum
        public enum ModuleType
        {
            Time,
            Lighting,
            Sky,
            Fog,  
            VolumetricClouds,
            FlatClouds, 
            Weather,
            Aurora,
            Effects, 
            Lightning,
            Environment,
            Audio,
            Quality
        }

        public void EnableModules()
        {
            if(Time != null)
               Time.Enable();

            if(Sky != null)
               Sky.Enable();

            if(Lighting != null)
               Lighting.Enable();
               
            if(VolumetricClouds != null)
               VolumetricClouds.Enable();
            
            if(FlatClouds != null)
               FlatClouds.Enable();

            if(Fog != null)
               Fog.Enable();

            if(Weather != null)
               Weather.Enable();

            if(Aurora != null)
               Aurora.Enable();

            if(Environment != null)
               Environment.Enable();

            if(Audio != null)
               Audio.Enable();

            if(Effects != null)
               Effects.Enable();

            if(Lightning != null)
               Lightning.Enable();

            if(Quality != null)
               Quality.Enable();   
        }

        public void DisableModules()
        {
            if(Time != null)
               Time.Disable();

            if(Sky != null)
               Sky.Disable();

            if(Lighting != null)
               Lighting.Disable();
               
            if(VolumetricClouds != null)
               VolumetricClouds.Disable();

            if(FlatClouds != null)
               FlatClouds.Disable();

            if(Fog != null)
               Fog.Disable();

            if(Weather != null)
               Weather.Disable();

            if(Aurora != null)
               Aurora.Disable();

            if(Environment != null)
               Environment.Disable();

            if(Audio != null)
               Audio.Disable();

            if(Effects != null)
               Effects.Disable();
            
            if(Lightning != null)
               Lightning.Disable();

            if(Quality != null)
               Quality.Disable();
        }

        public void DisableAndRemoveModules()
        {
            if(Time != null)
            {
               Time.Disable();
               Time = null;
            }

            if(Sky != null)
            {
               Sky.Disable();
               Sky = null;
            }

            if(Lighting != null)
            {
                Lighting.Disable();
                Lighting = null;
            }
               
            if(VolumetricClouds != null)
            {
                VolumetricClouds.Disable();
                VolumetricClouds = null;
            }

            if(FlatClouds != null)
            {
                FlatClouds.Disable();
                FlatClouds = null;
            }

            if(Fog != null)
            {
                Fog.Disable();
                Fog = null;
            }

            if(Weather != null)
            {
                Weather.Disable();
                Weather = null;
            }

            if(Aurora != null)
            {
                Aurora.Disable();
                Aurora = null;
            }

            if(Environment != null)
            {
                Environment.Disable();
                Environment = null;
            }

            if(Audio != null)
            {
                Audio.Disable();
                Audio = null;
            }

            if(Effects != null)
            {
                Effects.Disable();
                Effects = null;
            }

            if(Lightning != null)
            {
                Lightning.Disable();
                Lightning = null;
            }

            if(Quality != null)
            {
                Quality.Disable();
                Quality = null;
            }
        }

        public void StartModules ()
        {
            if(Time != null)
            {
               Time = Instantiate(Time);
            }

            if(Sky != null)
            {
               Sky = Instantiate(Sky);
            }

            if(Lighting != null)
            { 
               Lighting = Instantiate(Lighting);
            }

            if(Fog != null)
            {
               Fog = Instantiate(Fog);
            }

            if(VolumetricClouds != null)
            {
               VolumetricClouds = Instantiate(VolumetricClouds);
            }

            if(FlatClouds != null)
            {
               FlatClouds = Instantiate(FlatClouds);
            }

            if(Weather != null)
            {
               Weather = Instantiate(Weather);
            }

            if(Aurora != null)
            {
               Aurora = Instantiate(Aurora);
            }

            if(Environment != null)
            {
               Environment = Instantiate(Environment);
            }

            if(Audio != null)
            {
               Audio = Instantiate(Audio);
            }

            if(Effects != null)
            {
               Effects = Instantiate(Effects);
            }

            if(Lightning != null)
            {
               Lightning = Instantiate(Lightning);
            }

            if(Quality != null)
            {
               Quality = Instantiate(Quality);
            }
        }

        public void UpdateModules ()
        {
            if(Time != null)
               Time.UpdateModule();

            if(Sky != null)
               Sky.UpdateModule();

            if(Lighting != null)
               Lighting.UpdateModule();

            if(Fog != null)
               Fog.UpdateModule();

            if(VolumetricClouds != null)
               VolumetricClouds.UpdateModule();

            if(FlatClouds != null)
               FlatClouds.UpdateModule();

            if(Weather != null)
               Weather.UpdateModule();
     
            if(Aurora != null)
               Aurora.UpdateModule();
          
            if(Environment != null)
               Environment.UpdateModule();

            if(Audio != null)
               Audio.UpdateModule();

            if(Effects != null)
               Effects.UpdateModule();

            if(Lightning != null)
               Lightning.UpdateModule();

            if(Quality != null)
               Quality.UpdateModule();
        }

        //Saves all the modules settings to its assigned presets.
        public void SaveAllModules()
        {
            if(Time != null && Time.preset != null)
               Time.SaveModuleValues(Time.preset);

            if(Sky != null && Sky.preset != null)
               Sky.SaveModuleValues(Sky.preset);

            if(Lighting != null && Lighting.preset != null)
               Lighting.SaveModuleValues(Lighting.preset);

            if(Fog != null && Fog.preset != null)
               Fog.SaveModuleValues(Fog.preset);

            if(VolumetricClouds != null && VolumetricClouds.preset != null)
               VolumetricClouds.SaveModuleValues(VolumetricClouds.preset);

            if(FlatClouds != null && FlatClouds.preset != null)
               FlatClouds.SaveModuleValues(FlatClouds.preset);

            if(Weather != null && Weather.preset != null)
               Weather.SaveModuleValues(Weather.preset);

            if(Aurora != null && Aurora.preset != null)
               Aurora.SaveModuleValues(Aurora.preset);

            if(Environment != null && Environment.preset != null)
               Environment.SaveModuleValues(Environment.preset);
 
            if(Audio != null && Audio.preset != null)
               Audio.SaveModuleValues(Audio.preset);

            if(Effects != null && Effects.preset != null)
               Effects.SaveModuleValues(Effects.preset);

            if(Lightning != null && Lightning.preset != null)
               Lightning.SaveModuleValues(Lightning.preset);

            if(Quality != null && Quality.preset != null)
               Quality.SaveModuleValues(Quality.preset);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(configuration);
#endif
        }

        //Loads all the modules settings from its assigned presets.
        public void LoadAllModules()
        { 
            if(Time != null)
               Time.LoadModuleValues();

            if(Sky != null)
               Sky.LoadModuleValues();

            if(Lighting != null)
               Lighting.LoadModuleValues();

            if(Fog != null) 
               Fog.LoadModuleValues();

            if(VolumetricClouds != null)
               VolumetricClouds.LoadModuleValues();

            if(FlatClouds != null)
               FlatClouds.LoadModuleValues();
 
            if(Weather != null)
               Weather.LoadModuleValues();
            
            if(Aurora != null)
               Aurora.LoadModuleValues();
                  
            if(Environment != null)
               Environment.LoadModuleValues();

            if(Audio != null)
               Audio.LoadModuleValues();

            if(Effects != null)
               Effects.LoadModuleValues();

            if(Lightning != null)
               Lightning.LoadModuleValues();

            if(Quality != null)
               Quality.LoadModuleValues();
#if UNITY_EDITOR
            //Set the head configuration dirty to not loose our child values!
            UnityEditor.EditorUtility.SetDirty(configuration);
#endif
        }

        public void LoadConfiguration()
        {
            if(configuration != null)
            {
                if(configuration != lastConfiguration)
                DisableModules();

                Time = configuration.timeModule;
                Sky = configuration.Sky; 
                Lighting = configuration.lightingModule;
                VolumetricClouds = configuration.volumetricCloudModule;
                FlatClouds = configuration.flatCloudModule;
                Fog = configuration.fogModule;
                Weather = configuration.Weather;
                Aurora = configuration.Aurora;
                Environment = configuration.Environment;
                Audio = configuration.Audio;
                Effects = configuration.Effects;
                Lightning = configuration.Lightning;
                Quality = configuration.Quality; 

                if(configuration != lastConfiguration)
                EnableModules();
                
                lastConfiguration = configuration;
            }
            else if (configuration == null)
             DisableAndRemoveModules();
        }

        //Adds a module based on ModelType
        public void AddModule (ModuleType type)
        {
            switch (type) 
            {
                case ModuleType.Time:
                    if(Time != null)
                    {
                        Debug.Log("Time module already attached!");
                        return;
                    }
                    else
                    {
                        Time = ScriptableObject.CreateInstance<EnviroTimeModule>();
                        Time.name = "Time Module";
                        Time.preset = (EnviroTimeModule)EnviroHelper.GetDefaultPreset("Default Time Preset");
                        Time.LoadModuleValues();
                        Time.Enable();

                        #if UNITY_EDITOR
                        if(configuration != null && !Application.isPlaying)
                        {
                            configuration.timeModule = Time;
                            #if UNITY_EDITOR
                            UnityEditor.AssetDatabase.AddObjectToAsset(Time,configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh();
                            #endif
                        }
                        #endif
                    }
                break;

                case ModuleType.Sky:
                    if(Sky != null)
                    {
                        Debug.Log("Sky module already attached!");
                        return;
                    }
                    else
                    {
                        Sky = ScriptableObject.CreateInstance<EnviroSkyModule>();
                        Sky.name = "Sky Module";
                        Sky.preset = (EnviroSkyModule)EnviroHelper.GetDefaultPreset("Default Sky Preset");
                        Sky.LoadModuleValues();
                        Sky.Enable();
                        
                        if(configuration != null && !Application.isPlaying)
                        {
                            configuration.Sky = Sky;
                            #if UNITY_EDITOR
                            UnityEditor.AssetDatabase.AddObjectToAsset(Sky,configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh();
                            #endif
                        }
                    }
                break;

                case ModuleType.Lighting:
                    if(Lighting != null)
                    {
                        Debug.Log("Lighting module already attached!");
                        return;
                    }
                    else 
                    {
                        Lighting = ScriptableObject.CreateInstance<EnviroLightingModule>();
                        Lighting.name = "Lighting Module";
                        Lighting.preset = (EnviroLightingModule)EnviroHelper.GetDefaultPreset("Default Lighting Preset");
                        Lighting.LoadModuleValues();
                        Lighting.Enable();

                        if(configuration != null && !Application.isPlaying)
                        {
                            configuration.lightingModule = Lighting;
                            #if UNITY_EDITOR
                            UnityEditor.AssetDatabase.AddObjectToAsset(Lighting,configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh();
                            #endif
                        }
                    }
                break;

                case ModuleType.Fog:
                    if(Fog != null)
                    {
                        Debug.Log("Fog module already attached!");
                        return;
                    }
                    else 
                    {
                        Fog = ScriptableObject.CreateInstance<EnviroFogModule>();
                        Fog.name = "Fog Module";
                        Fog.preset = (EnviroFogModule)EnviroHelper.GetDefaultPreset("Default Fog Preset");
                        Fog.LoadModuleValues();
                        Fog.Enable();

                        if(configuration != null && !Application.isPlaying)
                        {
                            configuration.fogModule = Fog;
                            #if UNITY_EDITOR
                            UnityEditor.AssetDatabase.AddObjectToAsset(Fog,configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh();
                            #endif
                        }
                    }
                break;

                case ModuleType.VolumetricClouds:
                    if(VolumetricClouds != null)
                    {
                        Debug.Log("Volumetric clouds module already attached!");
                        return;
                    }
                    else 
                    {
                        VolumetricClouds = ScriptableObject.CreateInstance<EnviroVolumetricCloudsModule>();
                        VolumetricClouds.name = "Volumetric Cloud Module";
                        VolumetricClouds.preset = (EnviroVolumetricCloudsModule)EnviroHelper.GetDefaultPreset("Default Volumetric Clouds Preset");
                        VolumetricClouds.LoadModuleValues();
                        VolumetricClouds.Enable();

                        if(configuration != null && !Application.isPlaying)
                        {
                            configuration.volumetricCloudModule = VolumetricClouds;
                            #if UNITY_EDITOR
                            UnityEditor.AssetDatabase.AddObjectToAsset(VolumetricClouds,configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh();
                            #endif
                        }
                    }
                break;

                case ModuleType.FlatClouds:
                    if(FlatClouds != null)
                    {
                        Debug.Log("Flat clouds module already attached!");
                        return;
                    }
                    else 
                    {
                        FlatClouds = ScriptableObject.CreateInstance<EnviroFlatCloudsModule>();
                        FlatClouds.name = "Flat Clouds Module";
                        FlatClouds.preset = (EnviroFlatCloudsModule)EnviroHelper.GetDefaultPreset("Default Flat Clouds Preset");
                        FlatClouds.LoadModuleValues();
                        FlatClouds.Enable();

                        if(configuration != null && !Application.isPlaying)
                        {
                            configuration.flatCloudModule = FlatClouds;
                            #if UNITY_EDITOR
                            UnityEditor.AssetDatabase.AddObjectToAsset(FlatClouds,configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh();
                            #endif
                        }
                    }
                break;

                case ModuleType.Weather:
                    if(Weather != null)
                    {
                        Debug.Log("Weather module already attached!");
                        return;
                    }
                    else 
                    {
                        Weather = ScriptableObject.CreateInstance<EnviroWeatherModule>();
                        Weather.name = "Weather Module";
                        Weather.preset = (EnviroWeatherModule)EnviroHelper.GetDefaultPreset("Default Weather Preset");
                        Weather.LoadModuleValues();
                        Weather.Enable();

                        if(configuration != null && !Application.isPlaying)
                        {
                            configuration.Weather = Weather;
                            #if UNITY_EDITOR
                            UnityEditor.AssetDatabase.AddObjectToAsset(Weather,configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh();
                            #endif
                        }
                    }
                break;

                case ModuleType.Aurora:
                    if(Aurora != null)
                    {
                        Debug.Log("Aurora module already attached!");
                        return;
                    }
                    else 
                    {
                        Aurora = ScriptableObject.CreateInstance<EnviroAuroraModule>();
                        Aurora.name = "Aurora Module";
                        Aurora.preset = (EnviroAuroraModule)EnviroHelper.GetDefaultPreset("Default Aurora Preset");
                        Aurora.LoadModuleValues();
                        Aurora.Enable();

                        if(configuration != null && !Application.isPlaying)
                        {
                            configuration.Aurora = Aurora;
                            #if UNITY_EDITOR
                            UnityEditor.AssetDatabase.AddObjectToAsset(Aurora,configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh();
                            #endif
                        }
                    }
                break;

                case ModuleType.Environment:
                    if(Environment != null)
                    {
                        Debug.Log("Environment module already attached!");
                        return;
                    }
                    else 
                    {
                        Environment = ScriptableObject.CreateInstance<EnviroEnvironmentModule>();
                        Environment.name = "Environment Module";
                        Environment.preset = (EnviroEnvironmentModule)EnviroHelper.GetDefaultPreset("Default Environment Preset");
                        Environment.LoadModuleValues();
                        Environment.Enable();

                        if(configuration != null && !Application.isPlaying)
                        {
                            configuration.Environment = Environment;
                            #if UNITY_EDITOR
                            UnityEditor.AssetDatabase.AddObjectToAsset(Environment,configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh();
                            #endif
                        }
                    }
                break;

                case ModuleType.Audio:
                    if(Audio != null)
                    {
                        Debug.Log("Audio module already attached!");
                        return;
                    }
                    else 
                    {
                        Audio = ScriptableObject.CreateInstance<EnviroAudioModule>();
                        Audio.name = "Audio Module";
                        Audio.preset = (EnviroAudioModule)EnviroHelper.GetDefaultPreset("Default Audio Preset");
                        Audio.LoadModuleValues();
                        Audio.Enable();
           
                        if(configuration != null && !Application.isPlaying)
                        {
                            configuration.Audio = Audio;
                            #if UNITY_EDITOR
                            UnityEditor.AssetDatabase.AddObjectToAsset(Audio,configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh();
                            #endif
                        }
                    }
                break;

                case ModuleType.Effects:
                    if(Effects != null)
                    {
                        Debug.Log("Effects module already attached!");
                        return;
                    }
                    else 
                    {
                        Effects = ScriptableObject.CreateInstance<EnviroEffectsModule>();
                        Effects.name = "Effects Module";
                        Effects.preset = (EnviroEffectsModule)EnviroHelper.GetDefaultPreset("Default Effects Preset");
                        Effects.LoadModuleValues();
                        Effects.Enable();

                        if(configuration != null && !Application.isPlaying)
                        {
                            configuration.Effects = Effects;
                            #if UNITY_EDITOR
                            UnityEditor.AssetDatabase.AddObjectToAsset(Effects,configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh();
                            #endif
                        }
                    }
                break;

                case ModuleType.Lightning:
                    if(Lightning != null)
                    {
                        Debug.Log("Lighting module already attached!");
                        return;
                    }
                    else 
                    {
                        Lightning = ScriptableObject.CreateInstance<EnviroLightningModule>();
                        Lightning.name = "Lightning Module";
                        Lightning.preset = (EnviroLightningModule)EnviroHelper.GetDefaultPreset("Default Lightning Preset");
                        Lightning.LoadModuleValues();
                        Lightning.Enable();

                        if(configuration != null && !Application.isPlaying)
                        {
                            configuration.Lightning = Lightning;
                            #if UNITY_EDITOR
                            UnityEditor.AssetDatabase.AddObjectToAsset(Lightning,configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh();
                            #endif
                        }
                    }
                break;

                case ModuleType.Quality:
                    if(Quality != null)
                    {
                        Debug.Log("Quality module already attached!");
                        return;
                    }
                    else 
                    {
                        Quality = ScriptableObject.CreateInstance<EnviroQualityModule>();
                        Quality.name = "Quality Module";
                        Quality.preset = (EnviroQualityModule)EnviroHelper.GetDefaultPreset("Default Quality Module Preset");
                        Quality.LoadModuleValues();
                        Quality.Enable();

                        if(configuration != null && !Application.isPlaying)
                        {
                            configuration.Quality = Quality;
                            #if UNITY_EDITOR
                            UnityEditor.AssetDatabase.AddObjectToAsset(Quality,configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh();
                            #endif
                        }
                    }
                break;
            }
        }

        //Removes a module
        public void RemoveModule (ModuleType type)
        {
            switch (type)
            { 
                case ModuleType.Time:
                    if(Time != null)
                    {
                        Time.Disable();
                        DestroyImmediate(Time,true);
                        
                        if(!Application.isPlaying)
                        {
                            #if UNITY_EDITOR
                            DestroyImmediate(configuration.timeModule,true);
                            UnityEditor.EditorUtility.SetDirty(configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh(); 
                            #endif
                        }

                    }
                    else
                    {
                        Debug.Log("No time module attached!");
                        return;
                    }
                break;

                case ModuleType.Sky:
                    if(Sky != null)
                    {
                        Sky.Disable();
                        DestroyImmediate(Sky,true);
                        if(!Application.isPlaying)
                        {
                            DestroyImmediate(configuration.Sky,true);
                            #if UNITY_EDITOR
                            UnityEditor.EditorUtility.SetDirty(configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh(); 
                            #endif
                        }
                    }
                    else
                    {
                        Debug.Log("No sky module attached!");
                        return;
                    }
                break;

                case ModuleType.Lighting:
                    if(Lighting != null)
                    {
                        Lighting.Disable();
                        DestroyImmediate(Lighting,true);
                       
                        if(!Application.isPlaying)
                        {
                            if(configuration != null)
                            DestroyImmediate(configuration.lightingModule,true);
                            #if UNITY_EDITOR
                            UnityEditor.EditorUtility.SetDirty(configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh(); 
                            #endif
                        }
                    }
                    else
                    {
                        Debug.Log("No lighting module attached!");
                        return;
                    }
                break;

                case ModuleType.Fog:
                    if(Fog != null)
                    {
                        Fog.Disable();
                        DestroyImmediate(Fog,true);
                       
                        if(!Application.isPlaying)
                        {
                            if(configuration != null)
                            DestroyImmediate(configuration.fogModule,true);
                            #if UNITY_EDITOR
                            UnityEditor.EditorUtility.SetDirty(configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh(); 
                            #endif
                        }
                    }
                    else
                    {
                        Debug.Log("No fog module attached!");
                        return;
                    }
                break;

                case ModuleType.VolumetricClouds:
                    if(VolumetricClouds != null)
                    {
                        VolumetricClouds.Disable();
                        DestroyImmediate(VolumetricClouds,true);

                        if(!Application.isPlaying)
                        {
                            if(configuration != null)
                            DestroyImmediate(configuration.volumetricCloudModule,true);
                            #if UNITY_EDITOR
                            UnityEditor.EditorUtility.SetDirty(configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh(); 
                            #endif
                        }
                    }
                    else
                    {
                        Debug.Log("No volumetric cloud module attached!");
                        return;
                    }
                break;

                case ModuleType.FlatClouds:
                    if(FlatClouds != null)
                    {
                        FlatClouds.Disable();
                        DestroyImmediate(FlatClouds,true);

                        if(!Application.isPlaying)
                        {
                            if(configuration != null)
                            DestroyImmediate(configuration.flatCloudModule,true);
                            #if UNITY_EDITOR
                            UnityEditor.EditorUtility.SetDirty(configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh(); 
                            #endif
                        }
                    }
                    else
                    {
                        Debug.Log("No flat cloud module attached!");
                        return;
                    }
                break;

                case ModuleType.Weather:
                    if(Weather != null)
                    {
                        Weather.Disable();
                        DestroyImmediate(Weather,true);

                        if(!Application.isPlaying)
                        {                     
                            if(configuration != null)
                            DestroyImmediate(configuration.Weather,true);
                            #if UNITY_EDITOR
                            UnityEditor.EditorUtility.SetDirty(configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh(); 
                            #endif
                        }
                    }
                    else
                    {
                        Debug.Log("No weather module attached!");
                        return;
                    }
                break;

                case ModuleType.Aurora:
                    if(Aurora != null)
                    {
                        Aurora.Disable();
                        DestroyImmediate(Aurora,true);

                        if(!Application.isPlaying)
                        {                     
                            if(configuration != null)
                            DestroyImmediate(configuration.Aurora,true);
                            #if UNITY_EDITOR
                            UnityEditor.EditorUtility.SetDirty(configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh(); 
                            #endif
                        }
                    }
                    else
                    {
                        Debug.Log("No aurora module attached!");
                        return;
                    }
                break;

                case ModuleType.Environment:
                    if(Environment != null)
                    {
                        Environment.Disable();
                        DestroyImmediate(Environment,true);

                        if(!Application.isPlaying)
                        {                     
                            if(configuration != null)
                            DestroyImmediate(configuration.Environment,true);
                            #if UNITY_EDITOR
                            UnityEditor.EditorUtility.SetDirty(configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh(); 
                            #endif
                        }
                    }
                    else
                    {
                        Debug.Log("No environment module attached!");
                        return;
                    }
                break;

                case ModuleType.Audio:
                    if(Audio != null)
                    {
                        Audio.Disable();
                        DestroyImmediate(Audio,true);
                        if(!Application.isPlaying)
                        {
                            if(configuration != null)
                            DestroyImmediate(configuration.Audio,true);
                            #if UNITY_EDITOR
                            UnityEditor.EditorUtility.SetDirty(configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh(); 
                            #endif
                        }
                    }
                    else
                    {
                        Debug.Log("No audio module attached!");
                        return;
                    }
                break;

                case ModuleType.Effects:
                    if(Effects != null)
                    {
                        Effects.Disable();
                        DestroyImmediate(Effects,true);
                        if(!Application.isPlaying)
                        {
                            if(configuration != null)
                            DestroyImmediate(configuration.Effects,true);
                            #if UNITY_EDITOR
                            UnityEditor.EditorUtility.SetDirty(configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh(); 
                            #endif
                        }
                    }
                    else
                    {
                        Debug.Log("No effects module attached!");
                        return;
                    }
                break; 

                case ModuleType.Lightning:
                    if(Lightning != null)
                    {
                        Lightning.Disable();
                        DestroyImmediate(Lightning,true);
                        if(!Application.isPlaying)
                        {
                            if(configuration != null)
                            DestroyImmediate(configuration.Lightning,true);
                            #if UNITY_EDITOR
                            UnityEditor.EditorUtility.SetDirty(configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh(); 
                            #endif
                        }
                    }
                    else
                    {
                        Debug.Log("No lightning module attached!");
                        return;
                    }
                break;

                case ModuleType.Quality:
                    if(Quality != null)
                    {
                        Quality.Disable();
                        DestroyImmediate(Quality,true);
                        if(!Application.isPlaying)
                        {
                            if(configuration != null)
                            DestroyImmediate(configuration.Quality,true);
                            #if UNITY_EDITOR
                            UnityEditor.EditorUtility.SetDirty(configuration);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh(); 
                            #endif
                        }
                    }
                    else
                    {
                        Debug.Log("No quality module attached!");
                        return;
                    }
                break; 
            }
        }


    }
}
