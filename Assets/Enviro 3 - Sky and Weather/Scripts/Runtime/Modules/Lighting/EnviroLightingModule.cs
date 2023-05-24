using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Enviro
{
    [Serializable]
    public class EnviroLighting 
    {
        // DirectLighting
        public enum LightingMode
        {
            Single,
            Dual
        };

        public LightingMode lightingMode;
        public bool setDirectLighting = true;
        public int updateIntervallFrames = 2;
        public AnimationCurve sunIntensityCurve;
        public AnimationCurve moonIntensityCurve;
        public Gradient sunColorGradient;
        public Gradient moonColorGradient;

        public AnimationCurve sunIntensityCurveHDRP = new AnimationCurve();
        public AnimationCurve moonIntensityCurveHDRP  = new AnimationCurve();
        public AnimationCurve lightColorTemperatureHDRP = new AnimationCurve();
        [GradientUsageAttribute(true)]
        public Gradient lightColorTintHDRP;
        [GradientUsageAttribute(true)]
        public Gradient ambientColorTintHDRP;
        public bool controlExposure = true;
        public AnimationCurve sceneExposure = new AnimationCurve();
        public bool controlIndirectLighting = true;
        public AnimationCurve diffuseIndirectIntensity = new AnimationCurve();
        public AnimationCurve reflectionIndirectIntensity = new AnimationCurve();

        [Range(0f,2f)]
        public float directLightIntensityModifier = 1f;

        //Ambient Lighting 
        public bool setAmbientLighting = true;
        public UnityEngine.Rendering.AmbientMode ambientMode;
        [GradientUsage(true)]
        public Gradient ambientSkyColorGradient;
        [GradientUsage(true)]
        public Gradient ambientEquatorColorGradient;
        [GradientUsage(true)]
        public Gradient ambientGroundColorGradient;
        public AnimationCurve ambientIntensityCurve; 

        [Range(0f,2f)]
        public float ambientIntensityModifier = 1f;
        
        [Range(0f,2f)]
        public float ambientSkyboxUpdateIntervall = 0.1f;

        //Reflections
   
        public enum GlobalReflectionResolution
        {
            R16,
            R32,
            R64,
            R128,
            R256,
            R512,
            R1024,
            R2048
        }

        [Tooltip("Enable/disable enviro reflection probe..")]
        public bool updateReflectionProbe = true;
        [Tooltip("Enable/disable if enviro reflection probe should render in custom mode to support clouds and other Enviro effects.")]
        public bool globalReflectionCustomRendering = true;
        [Tooltip("Enable/disable if enviro reflection probe should render with fog.")] 
        public bool globalReflectionUseFog = false;
        [Tooltip("Set if enviro reflection probe should update faces individual on different frames.")]
        public bool globalReflectionTimeSlicing = true;
        [Tooltip("Enable/disable enviro reflection probe updates based on gametime changes..")]
        public bool globalReflectionsUpdateOnGameTime = true;
        [Tooltip("Enable/disable enviro reflection probe updates based on transform position changes..")]
        public bool globalReflectionsUpdateOnPosition = true;
        [Tooltip("Reflection probe intensity.")]
        [Range(0f, 2f)]
        public float globalReflectionsIntensity = 1.0f;
        [Tooltip("Reflection probe update rate based on game time.")]
        public float globalReflectionsTimeTreshold = 0.025f;
        [Tooltip("Reflection probe update rate based on camera position.")]
        public float globalReflectionsPositionTreshold = 0.25f;
        [Tooltip("Reflection probe scale. Increase that one to increase the area where reflection probe will influence your scene.")]
        [Range(10f, 10000f)]
        public float globalReflectionsScale = 1f;
        [Tooltip("Reflection probe resolution.")]
        public GlobalReflectionResolution globalReflectionResolution = GlobalReflectionResolution.R256;
        [Tooltip("Reflection probe rendered Layers.")]
        public LayerMask globalReflectionLayers;
        [Tooltip("Enable this option to update the default reflection with global reflection probes cubemap. This can be needed for material that might not support direct reflection probes. (Instanced Indirect Rendering)")]
        public bool updateDefaultEnvironmentReflections = true;
        [Tooltip("Reflection cubemap used for default scene sky reflections.")]
        public Cubemap defaultSkyReflectionTex;
    }

    [Serializable] 
    [ExecuteInEditMode]
    public class EnviroLightingModule : EnviroModule
    {  
        public Enviro.EnviroLighting Settings;
        public EnviroLightingModule preset;

        private int currentFrame;
        private float lastAmbientSkyboxUpdate;
        public float lastReflectionUpdate;
        public Vector3 lastReflectionUpdatePos;

        //Inspector
        public bool showDirectLightingControls;
        public bool showAmbientLightingControls;
        public bool showReflectionControls;

        #if ENVIRO_HDRP
        public UnityEngine.Rendering.HighDefinition.HDAdditionalLightData directionalLightHDRP;
        public UnityEngine.Rendering.HighDefinition.HDAdditionalLightData additionalLightHDRP;
        public UnityEngine.Rendering.HighDefinition.Exposure exposureHDRP;
        public UnityEngine.Rendering.HighDefinition.IndirectLightingController indirectLightingHDRP;
        #endif

        public override void Enable ()
        { 
            if(EnviroManager.instance == null)
               return;

            Setup();

            // Update global reflections once on enable.
            if(Settings.updateReflectionProbe && EnviroManager.instance.Objects.globalReflectionProbe != null)
            { 
                EnviroManager.instance.Objects.globalReflectionProbe.RefreshReflection(false);
                UpdateDefaultReflection(EnviroManager.instance.Objects.globalReflectionProbe, false);
            }
        }     
 
        public override void Disable ()
        { 
            if(EnviroManager.instance == null)
               return;

            Cleanup();  
        } 

        //Applies changes when you switch the lighting mode.
        public void ApplyLightingChanges ()
        {
            Cleanup();
            Setup();
        }

        private void Setup()
        {
            if(EnviroManager.instance.Objects.directionalLight == null)
            {
                GameObject newLight = new GameObject();

                if(Settings.lightingMode == EnviroLighting.LightingMode.Single)
                    newLight.name = "Sun and Moon Directional Light";
                else
                    newLight.name = "Sun Directional Light";

                newLight.transform.SetParent(EnviroManager.instance.transform);
                newLight.transform.localPosition = Vector3.zero;
                EnviroManager.instance.Objects.directionalLight = newLight.AddComponent<Light>();
                EnviroManager.instance.Objects.directionalLight.type = LightType.Directional;
                EnviroManager.instance.Objects.directionalLight.shadows = LightShadows.Soft;
            }

            if(EnviroManager.instance.Objects.additionalDirectionalLight == null && Settings.lightingMode == EnviroLighting.LightingMode.Dual)
            {
                GameObject newLight = new GameObject();
                newLight.name = "Moon Directional Light";
                newLight.transform.SetParent(EnviroManager.instance.transform);
                newLight.transform.localPosition = Vector3.zero;
                EnviroManager.instance.Objects.additionalDirectionalLight = newLight.AddComponent<Light>();
                EnviroManager.instance.Objects.additionalDirectionalLight.type = LightType.Directional;
                EnviroManager.instance.Objects.additionalDirectionalLight.shadows = LightShadows.Soft;
            }
            else if (EnviroManager.instance.Objects.additionalDirectionalLight != null && Settings.lightingMode == EnviroLighting.LightingMode.Single)
            {
                DestroyImmediate(EnviroManager.instance.Objects.additionalDirectionalLight.gameObject);
            }
            
            if(EnviroManager.instance.Objects.globalReflectionProbe == null)
            {
                GameObject newReflectionProbe = new GameObject();
                newReflectionProbe.name = "Global Reflection Probe";
                newReflectionProbe.transform.SetParent(EnviroManager.instance.transform);
                newReflectionProbe.transform.localPosition = Vector3.zero;
                EnviroManager.instance.Objects.globalReflectionProbe = newReflectionProbe.AddComponent<EnviroReflectionProbe>();
            }
        }  
 
        private void Cleanup()
        {
            if(EnviroManager.instance == null)
               return;

            if(EnviroManager.instance.Objects.directionalLight != null)
               DestroyImmediate(EnviroManager.instance.Objects.directionalLight.gameObject);

            if(EnviroManager.instance.Objects.additionalDirectionalLight != null)
               DestroyImmediate(EnviroManager.instance.Objects.additionalDirectionalLight.gameObject);

            if(EnviroManager.instance.Objects.globalReflectionProbe != null)
               DestroyImmediate(EnviroManager.instance.Objects.globalReflectionProbe.gameObject);
        }

        // Update Method
        public override void UpdateModule ()
        { 
             if(EnviroManager.instance == null)
               return;

            currentFrame++;

            if(currentFrame >= Settings.updateIntervallFrames)
            {
                EnviroManager.instance.updateSkyAndLighting = true;
                currentFrame = 0;
            }
            else 
            {
                EnviroManager.instance.updateSkyAndLighting = false;
            }
            

            //Update Direct Lighting
            if(EnviroManager.instance.Objects.directionalLight != null && Settings.setDirectLighting)
            {   
                #if !ENVIRO_HDRP
                if(EnviroManager.instance.updateSkyAndLighting)
                   UpdateDirectLighting ();
                #else
                if(EnviroManager.instance.updateSkyAndLighting && EnviroManager.instance.updateSkyAndLightingHDRP)
                    UpdateDirectLightingHDRP();
                #endif 
            }

            if (Settings.setAmbientLighting)
            {
                #if !ENVIRO_HDRP
                UpdateAmbientLighting ();
                #else
                if(EnviroManager.instance.updateSkyAndLighting && EnviroManager.instance.updateSkyAndLightingHDRP)
                   UpdateAmbientLightingHDRP ();
                #endif
            }

            if(EnviroManager.instance.Objects.globalReflectionProbe != null && Settings.updateReflectionProbe)
            {
                UpdateReflection();
            }

            #if ENVIRO_HDRP
            if(EnviroManager.instance.updateSkyAndLighting && EnviroManager.instance.updateSkyAndLightingHDRP)
                UpdateExposureHDRP ();
            #endif
        }

        private void UpdateDirectLighting ()
        {
            if(Settings.lightingMode == EnviroLighting.LightingMode.Single)
            {
                if(!EnviroManager.instance.isNight)
                {
                    //Set light to sun
                    EnviroManager.instance.Objects.directionalLight.transform.rotation = EnviroManager.instance.Objects.sun.transform.rotation;
                    EnviroManager.instance.Objects.directionalLight.intensity = Settings.sunIntensityCurve.Evaluate(EnviroManager.instance.solarTime) * Settings.directLightIntensityModifier;
                    EnviroManager.instance.Objects.directionalLight.color = Settings.sunColorGradient.Evaluate(EnviroManager.instance.solarTime);
                }
                else
                {
                    //Set light to moon
                    EnviroManager.instance.Objects.directionalLight.transform.rotation = EnviroManager.instance.Objects.moon.transform.rotation;
                    EnviroManager.instance.Objects.directionalLight.intensity = Settings.moonIntensityCurve.Evaluate(EnviroManager.instance.lunarTime) * Settings.directLightIntensityModifier;
                    EnviroManager.instance.Objects.directionalLight.color = Settings.moonColorGradient.Evaluate(EnviroManager.instance.lunarTime);
                }
            }
            else
            {
                //Sun
                EnviroManager.instance.Objects.directionalLight.transform.rotation = EnviroManager.instance.Objects.sun.transform.rotation;
                EnviroManager.instance.Objects.directionalLight.intensity = Settings.sunIntensityCurve.Evaluate(EnviroManager.instance.solarTime) * Settings.directLightIntensityModifier;
                EnviroManager.instance.Objects.directionalLight.color = Settings.sunColorGradient.Evaluate(EnviroManager.instance.solarTime);

                //Moon
                EnviroManager.instance.Objects.additionalDirectionalLight.transform.rotation = EnviroManager.instance.Objects.moon.transform.rotation;
                EnviroManager.instance.Objects.additionalDirectionalLight.intensity = Settings.moonIntensityCurve.Evaluate(EnviroManager.instance.lunarTime) * Settings.directLightIntensityModifier;
                EnviroManager.instance.Objects.additionalDirectionalLight.color = Settings.moonColorGradient.Evaluate(EnviroManager.instance.lunarTime);
            }
        }

#if ENVIRO_HDRP
        private void UpdateDirectLightingHDRP ()
        {
            if(directionalLightHDRP == null && EnviroManager.instance.Objects.directionalLight != null)
               directionalLightHDRP = EnviroManager.instance.Objects.directionalLight.gameObject.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
 
            if(additionalLightHDRP == null && EnviroManager.instance.Objects.additionalDirectionalLight != null)
               additionalLightHDRP = EnviroManager.instance.Objects.additionalDirectionalLight.gameObject.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
 
            EnviroManager.instance.Objects.directionalLight.transform.position = Vector3.zero;
            
            if(Settings.lightingMode == EnviroLighting.LightingMode.Single)
            {   
                if(!EnviroManager.instance.isNight)
                {
                    //Set light to sun
                    EnviroManager.instance.Objects.directionalLight.transform.rotation = EnviroManager.instance.Objects.sun.transform.rotation;
                    EnviroManager.instance.Objects.directionalLight.color = Settings.lightColorTintHDRP.Evaluate(EnviroManager.instance.solarTime);
                    EnviroManager.instance.Objects.directionalLight.useColorTemperature = true;
                    EnviroManager.instance.Objects.directionalLight.colorTemperature = Settings.lightColorTemperatureHDRP.Evaluate(EnviroManager.instance.solarTime);
                    
                    if(directionalLightHDRP != null)
                       directionalLightHDRP.SetIntensity(Settings.sunIntensityCurveHDRP.Evaluate(EnviroManager.instance.solarTime) * Settings.directLightIntensityModifier);
                }
                else
                {
                    //Set light to moon
                    EnviroManager.instance.Objects.directionalLight.transform.rotation = EnviroManager.instance.Objects.moon.transform.rotation;
                    EnviroManager.instance.Objects.directionalLight.color = Settings.lightColorTintHDRP.Evaluate(EnviroManager.instance.solarTime);
                    EnviroManager.instance.Objects.directionalLight.useColorTemperature = true;
                    EnviroManager.instance.Objects.directionalLight.colorTemperature = Settings.lightColorTemperatureHDRP.Evaluate(EnviroManager.instance.solarTime);
                    
                    if(directionalLightHDRP != null)
                       directionalLightHDRP.SetIntensity(Settings.moonIntensityCurveHDRP.Evaluate(EnviroManager.instance.lunarTime) * Settings.directLightIntensityModifier);
                }
            }
            else
            { 
                //Sun
                EnviroManager.instance.Objects.directionalLight.transform.rotation = EnviroManager.instance.Objects.sun.transform.rotation;
                EnviroManager.instance.Objects.directionalLight.color = Settings.lightColorTintHDRP.Evaluate(EnviroManager.instance.solarTime);
                EnviroManager.instance.Objects.directionalLight.useColorTemperature = true;
                EnviroManager.instance.Objects.directionalLight.colorTemperature = Settings.lightColorTemperatureHDRP.Evaluate(EnviroManager.instance.solarTime);
                
                if(directionalLightHDRP != null)
                   directionalLightHDRP.SetIntensity(Settings.sunIntensityCurveHDRP.Evaluate(EnviroManager.instance.solarTime) * Settings.directLightIntensityModifier);
 
                //Moon
                if(EnviroManager.instance.Objects.additionalDirectionalLight != null)
                {
                    EnviroManager.instance.Objects.additionalDirectionalLight.transform.rotation = EnviroManager.instance.Objects.moon.transform.rotation;
                    EnviroManager.instance.Objects.additionalDirectionalLight.color = Settings.lightColorTintHDRP.Evaluate(EnviroManager.instance.solarTime);
                    EnviroManager.instance.Objects.additionalDirectionalLight.useColorTemperature = true;
                    EnviroManager.instance.Objects.additionalDirectionalLight.colorTemperature = Settings.lightColorTemperatureHDRP.Evaluate(EnviroManager.instance.solarTime);
                }

                if(additionalLightHDRP != null)
                    additionalLightHDRP.SetIntensity(Settings.moonIntensityCurveHDRP.Evaluate(EnviroManager.instance.lunarTime) * Settings.directLightIntensityModifier);
            }
        } 

        private void UpdateAmbientLightingHDRP ()
        {
            if(EnviroManager.instance.Sky != null && EnviroManager.instance.Sky.mySkyboxMat != null)
            {
                EnviroManager.instance.Sky.mySkyboxMat.SetColor("_AmbientColorTintHDRP", Settings.ambientColorTintHDRP.Evaluate(EnviroManager.instance.solarTime));
            } 

            if(EnviroManager.instance.volumeHDRP != null)
            {
                if(indirectLightingHDRP == null)
                {
                    UnityEngine.Rendering.HighDefinition.IndirectLightingController TempIndirectLight;

                    if (EnviroManager.instance.volumeHDRP.sharedProfile != null && EnviroManager.instance.volumeHDRP.sharedProfile.TryGet<UnityEngine.Rendering.HighDefinition.IndirectLightingController>(out TempIndirectLight))
                    {
                        indirectLightingHDRP = TempIndirectLight;
                    }
                    else 
                    {
                        EnviroManager.instance.volumeHDRP.sharedProfile.Add<UnityEngine.Rendering.HighDefinition.IndirectLightingController>();

                        if (EnviroManager.instance.volumeHDRP.sharedProfile.TryGet<UnityEngine.Rendering.HighDefinition.IndirectLightingController>(out TempIndirectLight))
                        {
                            indirectLightingHDRP = TempIndirectLight;
                        }
                    }
                }
                else
                {
                    if(Settings.controlIndirectLighting)
                    {
                        indirectLightingHDRP.active = true;
                        indirectLightingHDRP.indirectDiffuseLightingMultiplier.overrideState = true;
                        indirectLightingHDRP.indirectDiffuseLightingMultiplier.value = Settings.diffuseIndirectIntensity.Evaluate(EnviroManager.instance.solarTime);
                        indirectLightingHDRP.reflectionLightingMultiplier.overrideState = true;
                        indirectLightingHDRP.reflectionLightingMultiplier.value = Settings.reflectionIndirectIntensity.Evaluate(EnviroManager.instance.solarTime);
                    }
                    else 
                    {
                        indirectLightingHDRP.active = false;
                    }
                }


            }
        }

        private void UpdateExposureHDRP ()
        {
            if(EnviroManager.instance.volumeHDRP != null)
            { 
                if(exposureHDRP == null)
                {
                    UnityEngine.Rendering.HighDefinition.Exposure TempExposure;

                    if (EnviroManager.instance.volumeHDRP.sharedProfile != null && EnviroManager.instance.volumeHDRP.sharedProfile.TryGet<UnityEngine.Rendering.HighDefinition.Exposure>(out TempExposure))
                    {
                        exposureHDRP = TempExposure;
                    }
                    else 
                    {
                        EnviroManager.instance.volumeHDRP.sharedProfile.Add<UnityEngine.Rendering.HighDefinition.Exposure>();

                        if (EnviroManager.instance.volumeHDRP.sharedProfile.TryGet<UnityEngine.Rendering.HighDefinition.Exposure>(out TempExposure))
                        {
                            exposureHDRP = TempExposure;
                        }
                    }
                }
                else
                {
                    if(Settings.controlExposure)
                    {
                        exposureHDRP.active = true;
                        exposureHDRP.mode.overrideState = true;
                        exposureHDRP.mode.value = UnityEngine.Rendering.HighDefinition.ExposureMode.Fixed;
                        exposureHDRP.fixedExposure.overrideState = true;
                        exposureHDRP.fixedExposure.value = Settings.sceneExposure.Evaluate(EnviroManager.instance.solarTime);
                    }
                    else
                    {
                        exposureHDRP.active = false;
                    }
                }
            }
        }
#endif

        private void UpdateAmbientLighting ()
        {
            RenderSettings.ambientMode = Settings.ambientMode;

            float intensity = Settings.ambientIntensityCurve.Evaluate(EnviroManager.instance.solarTime) *  Settings.ambientIntensityModifier;
 
            switch (Settings.ambientMode)
            {
                case UnityEngine.Rendering.AmbientMode.Flat:
                    RenderSettings.ambientSkyColor = Settings.ambientSkyColorGradient.Evaluate(EnviroManager.instance.solarTime) * intensity;
                break; 

                case UnityEngine.Rendering.AmbientMode.Trilight:
                    RenderSettings.ambientSkyColor = Settings.ambientSkyColorGradient.Evaluate(EnviroManager.instance.solarTime) * intensity;
                    RenderSettings.ambientEquatorColor = Settings.ambientEquatorColorGradient.Evaluate(EnviroManager.instance.solarTime) * intensity;      
                    RenderSettings.ambientGroundColor = Settings.ambientGroundColorGradient.Evaluate(EnviroManager.instance.solarTime) * intensity;
                break;

                case UnityEngine.Rendering.AmbientMode.Skybox:

                RenderSettings.ambientIntensity = intensity;

                if(EnviroManager.instance.Time != null)
                {
                    if (lastAmbientSkyboxUpdate < EnviroManager.instance.Time.Settings.timeOfDay || lastAmbientSkyboxUpdate > EnviroManager.instance.Time.Settings.timeOfDay + (Settings.ambientSkyboxUpdateIntervall + 0.01f))
                    { 
                        DynamicGI.UpdateEnvironment();
                        lastAmbientSkyboxUpdate = EnviroManager.instance.Time.Settings.timeOfDay + Settings.ambientSkyboxUpdateIntervall;
                    }
                }
                else
                {
                    if (lastAmbientSkyboxUpdate < Time.realtimeSinceStartup || lastAmbientSkyboxUpdate > Time.realtimeSinceStartup + (Settings.ambientSkyboxUpdateIntervall + 0.01f))
                    { 
                        DynamicGI.UpdateEnvironment();
                        lastAmbientSkyboxUpdate = Time.realtimeSinceStartup + Settings.ambientSkyboxUpdateIntervall;
                    }
                }
                break;
            }
        }

        public void UpdateReflectionForced ()
        {
            if(EnviroManager.instance.Objects.globalReflectionProbe == null)
                return;

        #if !ENVIRO_HDRP       
                    EnviroManager.instance.Objects.globalReflectionProbe.RefreshReflection(false);
                    UpdateDefaultReflection(EnviroManager.instance.Objects.globalReflectionProbe,false);
        #else
                    EnviroManager.instance.Objects.globalReflectionProbe.RefreshReflection(false);
        #endif
        }

        private void UpdateReflection()
        {
            EnviroReflectionProbe probe = EnviroManager.instance.Objects.globalReflectionProbe;
            int res = 128;

            switch (Settings.globalReflectionResolution)
            {
                case EnviroLighting.GlobalReflectionResolution.R16:
                     res = 16;
                break;
                case EnviroLighting.GlobalReflectionResolution.R32:
                     res = 32;
                break;
                case EnviroLighting.GlobalReflectionResolution.R64:
                     res = 64;
                break;
                case EnviroLighting.GlobalReflectionResolution.R128:
                     res = 128;
                break;
                case EnviroLighting.GlobalReflectionResolution.R256:
                     res = 256;
                break;
                case EnviroLighting.GlobalReflectionResolution.R512:
                     res = 512;
                break;
                case EnviroLighting.GlobalReflectionResolution.R1024:
                     res = 1024;
                break;
                case EnviroLighting.GlobalReflectionResolution.R2048:
                     res = 2048;
                break;
            }
#if !ENVIRO_HDRP
            probe.customRendering = Settings.globalReflectionCustomRendering;
            probe.myProbe.cullingMask = Settings.globalReflectionLayers;
            probe.myProbe.intensity = Settings.globalReflectionsIntensity;
            probe.myProbe.size = new Vector3 (Settings.globalReflectionsScale,Settings.globalReflectionsScale,Settings.globalReflectionsScale);
            probe.myProbe.resolution = res;

            //Set the Sky Reflection Intensity
            if(Settings.updateDefaultEnvironmentReflections)
                RenderSettings.reflectionIntensity = Settings.globalReflectionsIntensity;
#else
            probe.customRendering = false;
            probe.myProbe.resolution = res;

            if(probe.hdprobe != null)
            {
                probe.hdprobe.settingsRaw.cameraSettings.culling.cullingMask = Settings.globalReflectionLayers;
                probe.hdprobe.settingsRaw.influence.boxSize = new Vector3 (Settings.globalReflectionsScale,Settings.globalReflectionsScale,Settings.globalReflectionsScale);
                probe.hdprobe.settingsRaw.influence.sphereRadius = Settings.globalReflectionsScale; 
                probe.hdprobe.settingsRaw.lighting.multiplier = Settings.globalReflectionsIntensity;
            }
#endif

            if(EnviroManager.instance.Time != null)
            {
                if ((lastReflectionUpdate < EnviroManager.instance.Time.Settings.timeOfDay || lastReflectionUpdate > EnviroManager.instance.Time.Settings.timeOfDay + (Settings.globalReflectionsTimeTreshold + 0.01f)) && Settings.globalReflectionsUpdateOnGameTime)
                { 
#if !ENVIRO_HDRP       
                    probe.RefreshReflection(Settings.globalReflectionTimeSlicing);
                    UpdateDefaultReflection(probe,Settings.globalReflectionTimeSlicing);
#else
                   EnviroManager.instance.StartCoroutine(RefreshReflection (probe));
#endif
                    
                    lastReflectionUpdate = EnviroManager.instance.Time.Settings.timeOfDay + Settings.globalReflectionsTimeTreshold;
                }
            }

            if ((probe.transform.position.magnitude > lastReflectionUpdatePos.magnitude + Settings.globalReflectionsPositionTreshold || probe.transform.position.magnitude < lastReflectionUpdatePos.magnitude - Settings.globalReflectionsPositionTreshold) && Settings.globalReflectionsUpdateOnPosition)
            {
#if !ENVIRO_HDRP
                probe.RefreshReflection(Settings.globalReflectionTimeSlicing);
                UpdateDefaultReflection(probe,Settings.globalReflectionTimeSlicing);
#else
                EnviroManager.instance.StartCoroutine(RefreshReflection (probe));  
#endif
                lastReflectionUpdatePos = probe.transform.position;
            }
        }

#if ENVIRO_HDRP
        IEnumerator RefreshReflection (EnviroReflectionProbe probe)
        {
            EnviroManager.instance.updateSkyAndLightingHDRP = false;
            yield return null;       
            probe.RefreshReflection(false);
            yield return null;
            EnviroManager.instance.updateSkyAndLightingHDRP = true;
        }
#endif

        //Main method to update default reflections.
        private void UpdateDefaultReflection (EnviroReflectionProbe probe, bool timeSlice)
        {
            //Update Default Reflections
#if UNITY_EDITOR
            if((Settings.updateDefaultEnvironmentReflections && UnityEngine.SceneManagement.SceneManager.GetActiveScene() == EnviroManager.instance.gameObject.scene) || (Settings.updateDefaultEnvironmentReflections && EnviroManager.instance != null && EnviroManager.instance.dontDestroyOnLoad))
#else
            if(Settings.updateDefaultEnvironmentReflections)
#endif
            {   
                if(timeSlice)
                    EnviroManager.instance.StartCoroutine(CopyDefaultReflectionSliced(probe));
                else
                    EnviroManager.instance.StartCoroutine(CopyDefaultReflectionInstant(probe)); 
            }
            else 
            {
                RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
            }
        }

        //Checks if custom rendering is activated, otherwise check if unity probe is finished rendering to copy the cubemap.
        private void CopyDefaultReflection (EnviroReflectionProbe probe)
        {
            if(!Settings.globalReflectionCustomRendering)
            {
                if(probe.renderId == -1 || probe.myProbe.IsFinishedRendering(probe.renderId))
                {
                    CopyDefaultReflectionCubemap(probe);
                }
                else
                {
                    EnviroManager.instance.StartCoroutine(WaitForProbeFinish(probe));
                }     
            }
            else
            {
                CopyDefaultReflectionCubemap(probe);
            }
        } 

        //Copy reflection probe to cubemap and assign as default reflections.
        private void CopyDefaultReflectionCubemap (EnviroReflectionProbe probe)
        {
             if(Settings.defaultSkyReflectionTex == null || Settings.defaultSkyReflectionTex.height != probe.myProbe.resolution || Settings.defaultSkyReflectionTex.width != probe.myProbe.resolution)
                {
                    if(Settings.defaultSkyReflectionTex != null)
                    DestroyImmediate(Settings.defaultSkyReflectionTex);
                
                    Settings.defaultSkyReflectionTex = new Cubemap(probe.myProbe.resolution, probe.myProbe.hdr ? TextureFormat.RGBAHalf : TextureFormat.RGBA32, true);
                    Settings.defaultSkyReflectionTex.name = "Enviro Default Sky Reflection";
                }   

                if(probe.myProbe.texture != null)   
                   Graphics.CopyTexture(probe.myProbe.texture, Settings.defaultSkyReflectionTex as Texture);

                RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
                RenderSettings.customReflection = Settings.defaultSkyReflectionTex;
        }

        //Wait a frame and check again if unity probe finished now. Set to DefaultReflectionMode to Skybox in meantime.
        private IEnumerator WaitForProbeFinish (EnviroReflectionProbe probe)
        {
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
            yield return null;
            CopyDefaultReflection(probe);
        }

        private IEnumerator CopyDefaultReflectionInstant(EnviroReflectionProbe probe)
        {
            //Wait one frame for probe to finish rendering in case of timeslicing
            yield return null;  

            CopyDefaultReflection(probe);
        } 

        //Wait 7 frames for custom rendered time sliced probes.
        private IEnumerator CopyDefaultReflectionSliced(EnviroReflectionProbe probe)
        {
            //Wait for 7 frames in case of timeslicing
            for (int i = 0; i < 8; i++)
            {
                yield return null;
            }

            CopyDefaultReflection(probe);
        } 

        //Save and Load 
        public void LoadModuleValues ()
        {
            if(preset != null)
            {
                Settings = JsonUtility.FromJson<Enviro.EnviroLighting>(JsonUtility.ToJson(preset.Settings));
            }
            else
            {
                Debug.Log("Please assign a saved module to load from!");
            }
        }

        public void SaveModuleValues ()
        {
#if UNITY_EDITOR
        EnviroLightingModule t =  ScriptableObject.CreateInstance<EnviroLightingModule>();
        t.name = "Lighting Module";
        t.Settings = JsonUtility.FromJson<Enviro.EnviroLighting>(JsonUtility.ToJson(Settings));
 
        string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(EnviroHelper.assetPath + "/New " + t.name + ".asset");
        UnityEditor.AssetDatabase.CreateAsset(t, assetPathAndName);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public void SaveModuleValues (EnviroLightingModule module)
        {
            module.Settings = JsonUtility.FromJson<Enviro.EnviroLighting>(JsonUtility.ToJson(Settings));
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(module);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }
    }
}