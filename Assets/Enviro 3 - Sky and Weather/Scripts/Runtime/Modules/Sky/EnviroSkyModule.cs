using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Enviro
{
    [Serializable]
    public class EnviroSky
    {
        public enum MoonMode
        { 
            Realistic,
            Simple,
            Off
        }
        public MoonMode moonMode;
        //Front Colors
        [GradientUsage(true)]
        public Gradient frontColorGradient0,frontColorGradient1,frontColorGradient2,frontColorGradient3,frontColorGradient4,frontColorGradient5;
         
        //Back Colors
        [GradientUsage(true)]
        public Gradient backColorGradient0,backColorGradient1,backColorGradient2,backColorGradient3,backColorGradient4,backColorGradient5;
        
        //Other Colors
        [GradientUsage(true)]
        public Gradient sunDiscColorGradient, moonColorGradient, moonGlowColorGradient;

        //Textures
        public Cubemap starsTex;
        public Cubemap galaxyTex;
        public Texture2D sunTex;
        public Texture2D moonTex;
        public Texture2D moonGlowTex;

        //Distribution 
        [Range(-0.1f,1f)]
        public float distribution0,distribution1,distribution2,distribution3;
 
        public AnimationCurve mieScatteringIntensityCurve,moonGlowIntensityCurve,starIntensityCurve,galaxyIntensityCurve;
        public AnimationCurve intensityCurve = new AnimationCurve(new Keyframe(0,1), new Keyframe(1,1));
        public float intensity, sunScale, moonScale;
        
        [Range(-2f,2f)]
        public float moonPhase;
        public AnimationCurve skyExposureHDRP;
    #if ENVIRO_HDRP 
        public UnityEngine.Rendering.HighDefinition.SkyAmbientMode skyAmbientModeHDRP = UnityEngine.Rendering.HighDefinition.SkyAmbientMode.Dynamic;
    #endif
    }
 
    [Serializable]
    [ExecuteInEditMode]
    public class EnviroSkyModule : EnviroModule
    {  
        public Enviro.EnviroSky Settings;
        public EnviroSkyModule preset;

        public bool showSkyControls;
        public bool showSkySunControls;
        public bool showSkyMoonControls;
        public bool showSkyStarsControls;

        #if ENVIRO_HDRP
        UnityEngine.Rendering.HighDefinition.VisualEnvironment visualEnvironment;
        UnityEngine.Rendering.HighDefinition.EnviroHDRPSky enviroHDRPSky;
        #endif

        public Material mySkyboxMat;

        public override void Enable()
        {
            if(EnviroManager.instance == null)
               return;

    #if !ENVIRO_HDRP
            SetupSkybox ();
    #endif
        }

        public override void Disable()
        {
    

    #if !ENVIRO_HDRP
        if(mySkyboxMat != null)
           DestroyImmediate(mySkyboxMat);
    #endif
        }

        // Update Method
        public override void UpdateModule ()
        { 
            if(EnviroManager.instance == null)
               return;
    #if !ENVIRO_HDRP

            if(mySkyboxMat == null)
                SetupSkybox ();

            UpdateSkybox (mySkyboxMat);
    #else
            UpdateHDRPSky ();
    #endif

            if(EnviroManager.instance != null && EnviroManager.instance.Time != null && Settings.moonMode == EnviroSky.MoonMode.Realistic)
            UpdateMoonPhase ();
        }

        public void SetupSkybox ()
        {
            if(mySkyboxMat == null)
            {
                mySkyboxMat = new Material (Shader.Find("Enviro/Skybox"));
                RenderSettings.skybox = mySkyboxMat;
            }
            else
            {
                RenderSettings.skybox = mySkyboxMat;
            }
        }

        public void UpdateSkybox (Material mat)
        {
            float solarTime = EnviroManager.instance.solarTime;
          
            Shader.SetGlobalColor("_FrontColor0",Settings.frontColorGradient0.Evaluate(solarTime));
            Shader.SetGlobalColor("_FrontColor1",Settings.frontColorGradient1.Evaluate(solarTime));
            Shader.SetGlobalColor("_FrontColor2",Settings.frontColorGradient2.Evaluate(solarTime));
            Shader.SetGlobalColor("_FrontColor3",Settings.frontColorGradient3.Evaluate(solarTime));
            Shader.SetGlobalColor("_FrontColor4",Settings.frontColorGradient4.Evaluate(solarTime));
            Shader.SetGlobalColor("_FrontColor5",Settings.frontColorGradient5.Evaluate(solarTime));
              
            Shader.SetGlobalColor("_BackColor0",Settings.backColorGradient0.Evaluate(solarTime));
            Shader.SetGlobalColor("_BackColor1",Settings.backColorGradient1.Evaluate(solarTime));
            Shader.SetGlobalColor("_BackColor2",Settings.backColorGradient2.Evaluate(solarTime));
            Shader.SetGlobalColor("_BackColor3",Settings.backColorGradient3.Evaluate(solarTime));
            Shader.SetGlobalColor("_BackColor4",Settings.backColorGradient4.Evaluate(solarTime));
            Shader.SetGlobalColor("_BackColor5",Settings.backColorGradient5.Evaluate(solarTime));            

            Shader.SetGlobalColor("_SunColor",Settings.sunDiscColorGradient.Evaluate(solarTime));
            mat.SetColor("_MoonColor",Settings.moonColorGradient.Evaluate(solarTime));
            mat.SetColor("_MoonGlowColor",Settings.moonGlowColorGradient.Evaluate(solarTime));

            Shader.SetGlobalFloat("_Intensity", Settings.intensity * Settings.intensityCurve.Evaluate(solarTime));
            Shader.SetGlobalFloat("_MieScatteringIntensity", Settings.mieScatteringIntensityCurve.Evaluate(solarTime));
            mat.SetFloat("_MoonGlowIntensity", Settings.moonGlowIntensityCurve.Evaluate(solarTime));
            mat.SetFloat("_StarIntensity", Settings.starIntensityCurve.Evaluate(solarTime)); 
            mat.SetFloat("_GalaxyIntensity", Settings.galaxyIntensityCurve.Evaluate(solarTime));
             
            Shader.SetGlobalFloat("_frontBackDistribution0",Settings.distribution0);
            Shader.SetGlobalFloat("_frontBackDistribution1",Settings.distribution1);
            Shader.SetGlobalFloat("_frontBackDistribution2",Settings.distribution2);
            Shader.SetGlobalFloat("_frontBackDistribution3",Settings.distribution3);

            if(Settings.moonMode == EnviroSky.MoonMode.Off)
               mat.SetVector("_SkyMoonParameters", new Vector4(Settings.moonPhase,Settings.moonScale,Settings.moonScale,0f));
            else
               mat.SetVector("_SkyMoonParameters", new Vector4(Settings.moonPhase,Settings.moonScale,Settings.moonScale,1f));

            mat.SetVector("_SkySunParameters", new Vector4(Settings.sunScale,Settings.sunScale,Settings.sunScale,Settings.sunScale));  

            if(Settings.starsTex != null)
            mat.SetTexture("_StarsTex",Settings.starsTex);
            if(Settings.galaxyTex != null)
            mat.SetTexture("_GalaxyTex",Settings.galaxyTex);
            if(Settings.sunTex != null)
            mat.SetTexture("_SunTex",Settings.sunTex);
            if(Settings.moonTex != null)
            mat.SetTexture("_MoonTex",Settings.moonTex);
            if(Settings.moonGlowTex != null)
            mat.SetTexture("_MoonGlowTex",Settings.moonGlowTex);

            Shader.SetGlobalVector("_SunDir",-EnviroManager.instance.Objects.sun.transform.forward);
            Shader.SetGlobalVector("_MoonDir",EnviroManager.instance.Objects.moon.transform.forward);
 
            //Deactivate flat and cirrus clouds when no flat clouds module found.
            if(EnviroManager.instance.FlatClouds == null)
            {
                Shader.SetGlobalFloat("_CirrusClouds",0f);
                Shader.SetGlobalFloat("_FlatClouds",0f);
            }
            //Deactivate auroira when no flat clouds module found.
            if(EnviroManager.instance.Aurora == null)
            {
               Shader.SetGlobalFloat("_Aurora",0f);
            }
        }

        private void UpdateMoonPhase ()
        {
            float angle = Vector3.SignedAngle(EnviroManager.instance.Objects.moon.transform.forward, EnviroManager.instance.Objects.sun.transform.forward, -EnviroManager.instance.transform.forward);
        
            if (EnviroManager.instance.Time.Settings.latitude >= 0)
            {
                if (angle < 0)
                {
                    Settings.moonPhase = EnviroHelper.Remap(angle, 0f, -180f, -2f, 0f);
                }
                else
                {
                    Settings.moonPhase = EnviroHelper.Remap(angle, 0f, 180f, 2f, 0f);
                }
            }
            else
            {
                if (angle < 0)
                {
                    Settings.moonPhase = EnviroHelper.Remap(angle, 0f, -180f, 2f, 0f);
                }
                else
                {
                    Settings.moonPhase = EnviroHelper.Remap(angle, 0f, 180f, -2f, 0f);
                }
            }
        }

#if ENVIRO_HDRP
        public void UpdateHDRPSky ()
        {
            if(EnviroManager.instance.volumeHDRP != null)
            {           
                if(visualEnvironment == null)
                {
                    UnityEngine.Rendering.HighDefinition.VisualEnvironment TempEnv;

                    if (EnviroManager.instance.volumeHDRP.sharedProfile != null && EnviroManager.instance.volumeHDRP.sharedProfile.TryGet<UnityEngine.Rendering.HighDefinition.VisualEnvironment>(out TempEnv))
                    {
                        visualEnvironment = TempEnv;
                    }
                    else 
                    {
                        EnviroManager.instance.volumeHDRP.sharedProfile.Add<UnityEngine.Rendering.HighDefinition.VisualEnvironment>();

                        if (EnviroManager.instance.volumeHDRP.sharedProfile.TryGet<UnityEngine.Rendering.HighDefinition.VisualEnvironment>(out TempEnv))
                        {
                            visualEnvironment = TempEnv;
                        }
                    }
                }
                else
                {
                    visualEnvironment.skyType.value = 990;
                    visualEnvironment.skyType.overrideState = true;
                    visualEnvironment.skyAmbientMode.value = Settings.skyAmbientModeHDRP;
                    visualEnvironment.skyAmbientMode.overrideState = true;
                }

                if(enviroHDRPSky == null)
                {
                    UnityEngine.Rendering.HighDefinition.EnviroHDRPSky TempSky;
                    if (EnviroManager.instance.volumeHDRP.sharedProfile != null && EnviroManager.instance.volumeHDRP.sharedProfile.TryGet<UnityEngine.Rendering.HighDefinition.EnviroHDRPSky>(out TempSky))
                    {
                        enviroHDRPSky = TempSky;
                    }
                    else
                    {
                        EnviroManager.instance.volumeHDRP.sharedProfile.Add<UnityEngine.Rendering.HighDefinition.EnviroHDRPSky>();

                        if (EnviroManager.instance.volumeHDRP.sharedProfile.TryGet<UnityEngine.Rendering.HighDefinition.EnviroHDRPSky>(out TempSky))
                        {
                            enviroHDRPSky = TempSky;
                        } 
                    }
                }  
                else
                { 
                    enviroHDRPSky.skyIntensityMode.overrideState = true;
                    enviroHDRPSky.skyIntensityMode.value = UnityEngine.Rendering.HighDefinition.SkyIntensityMode.Exposure;
                    enviroHDRPSky.exposure.overrideState = true;

                    if(EnviroManager.instance.updateSkyAndLighting && EnviroManager.instance.updateSkyAndLightingHDRP)
                       enviroHDRPSky.exposure.value = Settings.skyExposureHDRP.Evaluate(EnviroManager.instance.solarTime);

                    enviroHDRPSky.updateMode.overrideState = true;
                    enviroHDRPSky.updateMode.value = UnityEngine.Rendering.HighDefinition.EnvironmentUpdateMode.OnDemand;

                    if (UnityEngine.Rendering.RenderPipelineManager.currentPipeline is UnityEngine.Rendering.HighDefinition.HDRenderPipeline) 
                    {
                        if(EnviroManager.instance.updateSkyAndLightingHDRP)
                        {
                            UnityEngine.Rendering.HighDefinition.HDRenderPipeline hd = (UnityEngine.Rendering.HighDefinition.HDRenderPipeline)UnityEngine.Rendering.RenderPipelineManager.currentPipeline;
                            hd.RequestSkyEnvironmentUpdate();
                        }            
                    }                   

                } 
            }
        }
#endif


        //Save and Load
        public void LoadModuleValues ()
        {
            if(preset != null)
            {
                Settings = JsonUtility.FromJson<Enviro.EnviroSky>(JsonUtility.ToJson(preset.Settings));
            }
            else
            {
                Debug.Log("Please assign a saved module to load from!");
            }
        } 

        public void SaveModuleValues ()
        {
#if UNITY_EDITOR
        EnviroSkyModule t =  ScriptableObject.CreateInstance<EnviroSkyModule>();
        t.name = "Sky Module";
        t.Settings = JsonUtility.FromJson<Enviro.EnviroSky>(JsonUtility.ToJson(Settings));

        string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(EnviroHelper.assetPath + "/New " + t.name + ".asset");
        UnityEditor.AssetDatabase.CreateAsset(t, assetPathAndName);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public void SaveModuleValues (EnviroSkyModule module)
        {
            module.Settings = JsonUtility.FromJson<Enviro.EnviroSky>(JsonUtility.ToJson(Settings));
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(module);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif

        }
    }
}