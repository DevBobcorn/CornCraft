// Distant Lands 2022.



using DistantLands.Cozy.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy
{

    [ExecuteAlways]
    public class CozyWeather : CozyEcosystem
    {

        #region Weather    
        private float cumulus;
        private float cirrus;
        private float altocumulus;
        private float cirrostratus;
        private float chemtrails;
        private float nimbus;
        private float nimbusHeight;
        private float nimbusVariation;
        private float border;
        private float borderEffect;
        private float borderVariation;
        public float fogDensity;

        public float cloudCoverage { get { return cumulus; } }

        #endregion

        #region Atmosphere    

        [Tooltip("Should the atmosphere be set using the physical sun height or the time of day")]
        public bool usePhysicalSunHeight;
        public float sunDirection;
        public float sunPitch;
        [ColorUsage(true, true)] public Color skyZenithColor;
        [ColorUsage(true, true)] public Color skyHorizonColor;
        [ColorUsage(true, true)] public Color cloudColor;
        [ColorUsage(true, true)] public Color cloudHighlightColor;
        [ColorUsage(true, true)] public Color highAltitudeCloudColor;
        [ColorUsage(true, true)] public Color sunlightColor;
        [ColorUsage(true, true)] public Color starColor;
        [ColorUsage(true, true)] public Color ambientLightHorizonColor;
        [ColorUsage(true, true)] public Color ambientLightZenithColor;
        public float ambientLightMultiplier;
        public float galaxyIntensity;
        [ColorUsage(true, true)] public Color fogColor1;
        [ColorUsage(true, true)] public Color fogColor2;
        [ColorUsage(true, true)] public Color fogColor3;
        [ColorUsage(true, true)] public Color fogColor4;
        [ColorUsage(true, true)] public Color fogColor5;
        [ColorUsage(true, true)] public Color fogFlareColor;
        public float gradientExponent = 0.364f;
        public float atmosphereVariationMin;
        public float atmosphereVariationMax;
        public float atmosphereBias = 1;
        public float sunSize = 0.7f;
        [ColorUsage(true, true)] public Color sunColor;
        public float sunFalloff = 43.7f;
        [ColorUsage(true, true)] public Color sunFlareColor;
        public float moonFalloff = 24.4f;
        [ColorUsage(true, true)] public Color moonlightColor;
        [ColorUsage(true, true)] public Color moonFlareColor;
        [ColorUsage(true, true)] public Color galaxy1Color;
        [ColorUsage(true, true)] public Color galaxy2Color;
        [ColorUsage(true, true)] public Color galaxy3Color;
        [ColorUsage(true, true)] public Color lightScatteringColor;
        public float rainbowPosition = 78.7f;
        public float rainbowWidth = 11;
        public float fogStart1 = 2;
        public float fogStart2 = 5;
        public float fogStart3 = 10;
        public float fogStart4 = 30;
        public float fogHeight = 0.85f;
        public float fogDensityMultiplier;
        public float fogLightFlareIntensity = 1;
        public float fogLightFlareFalloff = 21;
        public float fogLightFlareSquish = 1;
        [ColorUsage(true, true)] public Color cloudMoonColor;
        public float cloudSunHighlightFalloff = 14.1f;
        public float cloudMoonHighlightFalloff = 22.9f;
        public float cloudWindSpeed = 3;
        public float clippingThreshold = 0.5f;
        public float cloudMainScale = 20;
        public float cloudDetailScale = 2.3f;
        public float cloudDetailAmount = 30;
        public float acScale = 1;
        public float cirroMoveSpeed = 0.5f;
        public float cirrusMoveSpeed = 0.5f;
        public float chemtrailsMoveSpeed = 0.5f;
        [ColorUsage(true, true)] public Color cloudTextureColor = Color.white;
        public float cloudCohesion = 0.75f;
        public float spherize = 0.361f;
        public float shadowDistance = 0.0288f;
        public float cloudThickness = 2f;
        public float textureAmount = 1f;
        public Texture cloudTexture;
        public Vector3 texturePanDirection;

        public float rainbowIntensity;
        public bool useRainbow;

        #endregion

        #region Filter   
        public float filterSaturation;
        public float filterValue;
        public Color filterColor;
        public Color sunFilter;
        public Color cloudFilter;

        #endregion

        #region Runtime Variables   

        private float adjustedScale;
        private float dayPercentage;
        private AtmosphereProfile checkAtmosProfChange;

        public enum LockToCameraStyle { useMainCamera, useCustomCamera, DontLockToCamera }

        [Tooltip("Should the weather sphere always follow the camera and automatically rescale to the scene size?")]
        public LockToCameraStyle lockToCamera;
        public bool continuousUpdate = true;

        #endregion

        #region References

        public AtmosphereProfile atmosphereProfile;
        public PerennialProfile perennialProfile;
        private FilterFX defaultFilter;


        public Light sunLight;
        public MeshRenderer cloudMesh;
        public MeshRenderer skyMesh;
        public MeshRenderer fogMesh;
        public Camera cozyCamera;


        #endregion

        #region Modules

        public List<Type> activeModules;

        [HideInInspector]
        public CozyMaterialManager cozyMaterials;
        [HideInInspector]
        public VFXModule VFX;

        #endregion

        #region Editor

        public int window;
        public Texture icon1;
        public Texture icon2;
        public Texture icon3;
        public Texture icon4;
        public bool atmosWindow;
        public bool atmosSettingsWindow;
        public bool timeCurrentWindow;
        public bool tickMovementWindow;
        public bool curveWindow;
        public bool tickLengthWindow;
        public bool currentWeatherWindow;
        public bool forecastWindow;
        public bool climateWindow;
        public bool win1;
        public bool win2;
        public bool win3;
        public bool win4;

        #endregion

        #region Events

        [System.Serializable]
        public class Events
        {

            public float timeToCheckFor;
            public int currentTick;
            public int currentHour;
            public bool useEvents;

            public delegate void OnEvening();
            public static event OnEvening onEvening;
            public void RaiseOnEvening()
            {
                if (onEvening != null)
                    onEvening();
            }
            public delegate void OnMorning();
            public static event OnMorning onMorning;
            public void RaiseOnMorning()
            {
                if (onMorning != null)
                    onMorning();
            }
            public delegate void OnNewHour();
            public static event OnNewHour onNewHour;
            public void RaiseOnNewHour()
            {
                if (onNewHour != null)
                    onNewHour();
            }
            public delegate void OnTickPass();
            public static event OnTickPass onNewTick;
            public void RaiseOnTickPass()
            {
                if (onNewTick != null)
                    onNewTick();
            }
            public delegate void OnMidnight();
            public static event OnMidnight onMidnight;
            public void RaiseOnMidnight()
            {
                if (onMidnight != null)
                    onMidnight();
            }
            public delegate void OnNoon();
            public static event OnNoon onNoon;
            public void RaiseOnNoon()
            {
                if (onNoon != null)
                    onNoon();

            }

            public delegate void OnWeatherChange();
            public static event OnWeatherChange onWeatherChange;
            public void RaiseOnWeatherChange()
            {
                if (onWeatherChange != null)
                    onWeatherChange();
            }

            public delegate void OnDayChange();
            public static event OnDayChange onNewDay;
            public void RaiseOnDayChange()
            {
                if (onNewDay != null)
                    onNewDay();
            }
            public delegate void OnYearChange();
            public static event OnYearChange onNewYear;
            public void RaiseOnYearChange()
            {
                if (onNewYear != null)
                    onNewYear();
            }
            public delegate void OnRaining();
            public static event OnRaining onRaining;
            public void RaiseOnRaining()
            {
                if (onRaining != null)
                    onRaining();
            }
            public delegate void OnSnowing();
            public static event OnSnowing onSnowing;
            public void RaiseOnSnowing()
            {
                if (onSnowing != null)
                    onSnowing();
            }
            public delegate void OnSunny();
            public static event OnSunny onSunny;
            public void RaiseOnSunny()
            {
                if (onSunny != null)
                    onSunny();
            }


        }

        public Events events;


        #endregion

        #region Triggers   
        [Tooltip("The tag that contains all triggers that stop weather FX from playing.")]
        public string cozyTriggerTag = "FX Block Zone";

        [HideInInspector]
        public List<Collider> cozyTriggers;

        #endregion

        #region Time 

        public float currentTicks
        {
            get { return timeControl == TimeControl.native ? calendar.currentTicks : perennialProfile.currentTicks; }
            set
            {
                if (timeControl == TimeControl.native)
                {
                    calendar.currentTicks = value;
                }
                else
                {
                    perennialProfile.currentTicks = value;
                }

                MeridiemTime.DayPercentToMeridiemTime(GetCurrentDayPercentage(), ref calendar.meridiemTime);

            }

        }

        public int currentDay
        {
            get { return timeControl == TimeControl.native ? calendar.currentDay : perennialProfile.currentDay; }
            set
            {
                if (timeControl == TimeControl.native)
                {
                    calendar.currentDay = value;
                }
                else
                {
                    perennialProfile.currentDay = value;
                }
            }

        }

        public int currentYear
        {
            get { return timeControl == TimeControl.native ? calendar.currentYear : perennialProfile.currentYear; }
            set
            {
                if (timeControl == TimeControl.native)
                {
                    calendar.currentYear = value;
                }
                else
                {
                    perennialProfile.currentYear = value;
                }
            }

        }

        [System.Serializable]
        public class CozyCalendar
        {


            public float currentTicks;
            [FormatTime]
            public MeridiemTime meridiemTime;
            public int currentDay;
            public int currentYear;


        }
        public CozyCalendar calendar;


        #endregion

        #region Ecosystem 


        public List<CozyEcosystem> ecosystems;

        [WeightedWeather]
        public List<WeightedWeather> currentWeatherProfiles;

        private WeatherProfile weatherCheck;
        public WeatherProfile currentLocalWeather { get; private set; }

        [SerializeField]
        private List<WeatherProfile> setupProfiles = new List<WeatherProfile>();
        public List<WeatherProfile> queuedWeather = new List<WeatherProfile>();
        public List<FilterFX> possibleFilters = new List<FilterFX>();

        #endregion

        public enum AtmosphereSelection { native, profile }
        [Tooltip("How should this weather system manage atmosphere settings? Native sets all settings locally to this system, Profile sets global settings on the atmosphere profile.")]
        public AtmosphereSelection atmosphereControl;

        public enum TimeControl { native, profile }
        [Tooltip("How should this weather system manage time settings? Native sets all settings locally to this system, Profile sets global settings on the perennial profile.")]
        public TimeControl timeControl;

        public enum SkyStyle { desktop, mobile, off }
        public SkyStyle skyStyle;

        public enum CloudStyle { cozyDesktop, cozyMobile, soft, paintedSkies, ghibliDesktop, ghibliMobile, singleTexture, off }
        public CloudStyle cloudStyle;

        public enum FogStyle { unity, stylized, heightFog, off }
        public FogStyle fogStyle = FogStyle.stylized;

        public bool transitioningAtmosphere;
        public bool transitioningWeather;
        public bool transitioningTime;

        public CozyModule overrideAtmosphere;
        public CozyModule overrideWeather;
        public CozyModule overrideTime;

        public GameObject moduleHolder;
        public List<CozyModule> modules = new List<CozyModule>();

        void SetupReferences()
        {

            sunLight = GetChild("Sun").GetComponent<Light>();
            skyMesh = GetChild("Skydome").GetComponent<MeshRenderer>();
            cloudMesh = GetChild("Foreground Clouds").GetComponent<MeshRenderer>();
            fogMesh = GetChild("Fog").GetComponent<MeshRenderer>();

        }

        Transform GetChild(string name)
        {

            foreach (Transform i in transform.GetComponentsInChildren<Transform>())
                if (i.name == name)
                    return i;

            return null;



        }

        new void Awake()
        {


            SetupReferences();
            ResetModules();
            SetShaderVariables();
            SetupTime();

            defaultFilter = (FilterFX)Resources.Load("Default Filter");


            if (Application.isPlaying)
            {

                ecosystems = new List<CozyEcosystem>() { this };
                base.Awake();


                foreach (Collider i in FindObjectsOfType<Collider>())
                {

                    if (i.gameObject.tag == cozyTriggerTag)
                        cozyTriggers.Add(i);

                }
            }
        }

        // Start is called before the first frame update
        void Start()
        {

            ResetVariables();
            CalculateFilterColors();
            ResetQuality();

            currentLocalWeather = GetCurrentWeatherProfile();
            weatherCheck = currentLocalWeather;

        }

        void SetupTime()
        {
            if (perennialProfile.resetTicksOnStart)
                currentTicks = perennialProfile.startTicks;


            if (perennialProfile.realisticYear) { perennialProfile.daysPerYear = perennialProfile.RealisticDaysPerYear(); }

            SetupTimeEvents();
        }

        /// <summary>
        /// Resets all of the material shaders to the shaders in the atmosphere profiles.
        /// </summary> 
        public void ResetQuality()
        {

            SetupReferences();


            switch (cloudStyle)
            {
                case CloudStyle.cozyDesktop:
                    cloudMesh.sharedMaterial.shader = ((Material)Resources.Load("Materials/Desktop Clouds Reference")).shader;
                    break;
                case CloudStyle.cozyMobile:
                    cloudMesh.sharedMaterial.shader = ((Material)Resources.Load("Materials/Mobile Clouds Reference")).shader;
                    break;
                case CloudStyle.ghibliDesktop:
                    cloudMesh.sharedMaterial.shader = ((Material)Resources.Load("Materials/Desktop Ghibli Clouds Reference")).shader;
                    break;
                case CloudStyle.ghibliMobile:
                    cloudMesh.sharedMaterial.shader = ((Material)Resources.Load("Materials/Mobile Ghibli Clouds Reference")).shader;
                    break;
                case CloudStyle.paintedSkies:
                    cloudMesh.sharedMaterial.shader = ((Material)Resources.Load("Materials/Painted Clouds Reference")).shader;
                    break;
                case CloudStyle.soft:
                    cloudMesh.sharedMaterial.shader = ((Material)Resources.Load("Materials/Soft Clouds Reference")).shader;
                    break;
                case CloudStyle.singleTexture:
                    cloudMesh.sharedMaterial.shader = ((Material)Resources.Load("Materials/Single Texture Reference")).shader;
                    break;
                case CloudStyle.off:
                    cloudMesh.sharedMaterial.shader = ((Material)Resources.Load("Materials/Disabled")).shader;
                    break;
            }


            switch (skyStyle)
            {
                case SkyStyle.desktop:
                    skyMesh.sharedMaterial.shader = ((Material)Resources.Load("Materials/Desktop Sky Reference")).shader;
                    break;
                case SkyStyle.mobile:
                    skyMesh.sharedMaterial.shader = ((Material)Resources.Load("Materials/Mobile Sky Reference")).shader;
                    break;
                case SkyStyle.off:
                    skyMesh.sharedMaterial.shader = ((Material)Resources.Load("Materials/Disabled")).shader;
                    break;

            }

            switch (fogStyle)
            {
                case FogStyle.stylized:
                    fogMesh.sharedMaterial.shader = ((Material)Resources.Load("Materials/Default Fog Reference")).shader;
                    RenderSettings.fog = false;
                    break;
                case FogStyle.heightFog:
                    fogMesh.sharedMaterial.shader = ((Material)Resources.Load("Materials/Height Fog Reference")).shader;
                    RenderSettings.fog = false;
                    break;
                case FogStyle.unity:
                    fogMesh.sharedMaterial.shader = ((Material)Resources.Load("Materials/Disabled")).shader;
                    RenderSettings.fog = true;
                    break;
                case FogStyle.off:
                    fogMesh.sharedMaterial.shader = ((Material)Resources.Load("Materials/Disabled")).shader;
                    RenderSettings.fog = false;
                    break;

            }

            CheckSystemProperties();


        }

        // Update is called once per frame
        public new void Update()
        {

            base.Update();
            dayPercentage = GetCurrentDayPercentage();

            if (!overrideTime)
                ManageTime();

            if (!Application.isPlaying)
            {
                ResetModules();

                ResetVariables();

                if (checkAtmosProfChange != atmosphereProfile)
                {
                    ResetQuality();
                    checkAtmosProfChange = atmosphereProfile;

                }

                currentWeatherProfiles = new List<WeightedWeather>() { new WeightedWeather() { profile = currentWeather, weight = 1 } };
            }

            if (currentWeather == null || atmosphereProfile == null || perennialProfile == null)
            {

                Debug.LogWarning("Cozy Weather requires an active weather profile, an active perennial profile and an active atmosphere profile to function properly.\nPlease ensure that the active CozyWeather script contains all necessary profile references.");
                return;
            }


            if (!overrideAtmosphere)
            {
                if (atmosphereControl == AtmosphereSelection.profile)
                    if (!transitioningAtmosphere)
                        SetAtmosphereVariables();
            }


            SetShaderVariables();
            SetGlobalVariables();


            if (perennialProfile.realisticYear)
            {

                perennialProfile.daysPerYear = perennialProfile.RealisticDaysPerYear();

            }


            if (lockToCamera != LockToCameraStyle.DontLockToCamera)
            {

                if (lockToCamera == LockToCameraStyle.useMainCamera)
                    cozyCamera = Camera.main;

                if (cozyCamera != null)
                {

                    transform.position = cozyCamera.transform.position;
                    adjustedScale = cozyCamera.farClipPlane / 1000;

                    transform.localScale = Vector3.one * adjustedScale;

                    Material fogMat = fogMesh.sharedMaterial;
                    fogMat.SetFloat("_FogSmoothness", 100 * adjustedScale);

                }

            }

            ManageWeatherWeights();

            if (Application.isPlaying)
            {

                GlobalEcosystem();
                UpdateWeatherByWeight();

#if UNITY_EDITOR
                if (!VFX)
                    if (currentWeatherProfiles[0].profile.FX.Length > 0)
                        Debug.LogWarning("VFX requires an active VFX module on the COZY system. Be sure to add it in the modules portion of the settings tab!");
#endif

                currentLocalWeather = GetCurrentWeatherProfile();
                if (weatherCheck != currentLocalWeather)
                {
                    if (events.useEvents)
                    {
                        events.RaiseOnWeatherChange();

                    }

                    weatherCheck = currentLocalWeather;
                }

            }
        }

        /// <summary>
        /// Updates the visual weather by the properties of the array of current weather profiles.
        /// </summary> 
        void UpdateWeatherByWeight()
        {

            float k = 0;

            foreach (WeightedWeather i in currentWeatherProfiles) k += i.weight;

            if (k == 0)
                k = 1;

            altocumulus = 0;
            borderEffect = 0;
            border = 0;
            borderVariation = 0;
            chemtrails = 0;
            cirrostratus = 0;
            cirrus = 0;
            cumulus = 0;
            nimbus = 0;
            nimbusHeight = 0;
            nimbusVariation = 0;
            fogDensity = 0;



            foreach (WeightedWeather i in currentWeatherProfiles)
            {
                i.weight /= k;

                WeatherProfile j = i.profile;

                altocumulus += j.cloudSettings.altocumulusCoverage * i.weight;
                borderEffect += j.cloudSettings.borderEffect * i.weight;
                border += j.cloudSettings.borderHeight * i.weight;
                borderVariation += j.cloudSettings.borderVariation * i.weight;
                chemtrails += j.cloudSettings.chemtrailCoverage * i.weight;
                cirrostratus += j.cloudSettings.cirrostratusCoverage * i.weight;
                cirrus += j.cloudSettings.cirrusCoverage * i.weight;
                cumulus += j.cloudSettings.cumulusCoverage * i.weight;
                nimbus += j.cloudSettings.nimbusCoverage * i.weight;
                nimbusHeight += j.cloudSettings.nimbusHeightEffect * i.weight;
                nimbusVariation += j.cloudSettings.nimbusVariation * i.weight;
                fogDensity += j.fogDensity * i.weight;

                j.SetWeatherWeight(i.weight);

            }
        }

        /// <summary>
        /// Makes sure that all module connections are setup properly. Run this after adding or removing a module.
        /// </summary> 
        public void ResetModules()
        {

            moduleHolder = GetChild("Modules").gameObject;

            if (!cozyMaterials)
                cozyMaterials = GetModule<CozyMaterialManager>();

            if (!VFX)
                VFX = GetModule<VFXModule>();


            List<int> toRemove = new List<int>();
            int j = 0;

            foreach (CozyModule i in modules)
            {
                if (i == null)
                    toRemove.Add(j);

                j++;
            }

            foreach (int k in toRemove)
                modules.RemoveAt(k);


        }

        /// <summary>
        /// Immediately sets all of the atmosphere variables.
        /// </summary> 
        void SetAtmosphereVariables()
        {

            float i = usePhysicalSunHeight ? perennialProfile.sunMovementCurve.Evaluate(dayPercentage) / 360 : dayPercentage;

            gradientExponent = atmosphereProfile.gradientExponent.GetFloatValue(i);
            acScale = atmosphereProfile.acScale.GetFloatValue(i);
            ambientLightHorizonColor = atmosphereProfile.ambientLightHorizonColor.GetColorValue(i);
            ambientLightZenithColor = atmosphereProfile.ambientLightZenithColor.GetColorValue(i);
            ambientLightMultiplier = atmosphereProfile.ambientLightMultiplier.GetFloatValue(i);
            atmosphereBias = atmosphereProfile.atmosphereBias.GetFloatValue(i);
            atmosphereVariationMax = atmosphereProfile.atmosphereVariationMax.GetFloatValue(i);
            atmosphereVariationMin = atmosphereProfile.atmosphereVariationMin.GetFloatValue(i);
            chemtrailsMoveSpeed = atmosphereProfile.chemtrailsMoveSpeed.GetFloatValue(i);
            cirroMoveSpeed = atmosphereProfile.cirroMoveSpeed.GetFloatValue(i);
            cirrusMoveSpeed = atmosphereProfile.cirrusMoveSpeed.GetFloatValue(i);
            clippingThreshold = atmosphereProfile.clippingThreshold.GetFloatValue(i);
            cloudCohesion = atmosphereProfile.cloudCohesion.GetFloatValue(i);
            cloudColor = atmosphereProfile.cloudColor.GetColorValue(i);
            cloudDetailAmount = atmosphereProfile.cloudDetailAmount.GetFloatValue(i);
            cloudDetailScale = atmosphereProfile.cloudDetailScale.GetFloatValue(i);
            cloudHighlightColor = atmosphereProfile.cloudHighlightColor.GetColorValue(i);
            cloudMainScale = atmosphereProfile.cloudMainScale.GetFloatValue(i);
            cloudMoonColor = atmosphereProfile.cloudMoonColor.GetColorValue(i);
            cloudMoonHighlightFalloff = atmosphereProfile.cloudMoonHighlightFalloff.GetFloatValue(i);
            cloudSunHighlightFalloff = atmosphereProfile.cloudSunHighlightFalloff.GetFloatValue(i);
            cloudTextureColor = atmosphereProfile.cloudTextureColor.GetColorValue(i);
            cloudThickness = atmosphereProfile.cloudThickness.GetFloatValue(i);
            cloudWindSpeed = atmosphereProfile.cloudWindSpeed.GetFloatValue(i);
            fogColor1 = atmosphereProfile.fogColor1.GetColorValue(i);
            fogColor2 = atmosphereProfile.fogColor2.GetColorValue(i);
            fogColor3 = atmosphereProfile.fogColor3.GetColorValue(i);
            fogColor4 = atmosphereProfile.fogColor4.GetColorValue(i);
            fogColor5 = atmosphereProfile.fogColor5.GetColorValue(i);
            fogStart1 = atmosphereProfile.fogStart1;
            fogStart2 = atmosphereProfile.fogStart2;
            fogStart3 = atmosphereProfile.fogStart3;
            fogStart4 = atmosphereProfile.fogStart4;
            fogDensityMultiplier = atmosphereProfile.fogDensityMultiplier.GetFloatValue(i);
            fogFlareColor = atmosphereProfile.fogFlareColor.GetColorValue(i);
            fogHeight = atmosphereProfile.fogHeight.GetFloatValue(i);
            fogLightFlareFalloff = atmosphereProfile.fogLightFlareFalloff.GetFloatValue(i);
            fogLightFlareIntensity = atmosphereProfile.fogLightFlareIntensity.GetFloatValue(i);
            fogLightFlareSquish = atmosphereProfile.fogLightFlareSquish.GetFloatValue(i);
            galaxy1Color = atmosphereProfile.galaxy1Color.GetColorValue(i);
            galaxy2Color = atmosphereProfile.galaxy2Color.GetColorValue(i);
            galaxy3Color = atmosphereProfile.galaxy3Color.GetColorValue(i);
            galaxyIntensity = atmosphereProfile.galaxyIntensity.GetFloatValue(i);
            highAltitudeCloudColor = atmosphereProfile.highAltitudeCloudColor.GetColorValue(i);
            lightScatteringColor = atmosphereProfile.lightScatteringColor.GetColorValue(i);
            moonlightColor = atmosphereProfile.moonlightColor.GetColorValue(i);
            moonFalloff = atmosphereProfile.moonFalloff.GetFloatValue(i);
            moonFlareColor = atmosphereProfile.moonFlareColor.GetColorValue(i);
            useRainbow = atmosphereProfile.useRainbow;
            rainbowPosition = atmosphereProfile.rainbowPosition.GetFloatValue(i);
            rainbowWidth = atmosphereProfile.rainbowWidth.GetFloatValue(i);
            shadowDistance = atmosphereProfile.shadowDistance.GetFloatValue(i);
            skyHorizonColor = atmosphereProfile.skyHorizonColor.GetColorValue(i);
            skyZenithColor = atmosphereProfile.skyZenithColor.GetColorValue(i);
            spherize = atmosphereProfile.spherize.GetFloatValue(i);
            starColor = atmosphereProfile.starColor.GetColorValue(i);
            sunColor = atmosphereProfile.sunColor.GetColorValue(i);
            sunDirection = atmosphereProfile.sunDirection.GetFloatValue(i);
            sunFalloff = atmosphereProfile.sunFalloff.GetFloatValue(i);
            sunFlareColor = atmosphereProfile.sunFlareColor.GetColorValue(i);
            sunlightColor = atmosphereProfile.sunlightColor.GetColorValue(i);
            sunPitch = atmosphereProfile.sunPitch.GetFloatValue(i);
            sunSize = atmosphereProfile.sunSize.GetFloatValue(i);
            textureAmount = atmosphereProfile.textureAmount.GetFloatValue(i);
            cloudTexture = atmosphereProfile.cloudTexture;
            texturePanDirection = atmosphereProfile.texturePanDirection;



        }

        public float ModifiedDayPercentage()
        {

            return usePhysicalSunHeight ? perennialProfile.sunMovementCurve.Evaluate(dayPercentage) / 360 : dayPercentage;

        }

        /// <summary>
        /// Sets the global shader variables.
        /// </summary> 
        public void SetGlobalVariables()
        {

            Material i = fogMesh.sharedMaterial;

            if (fogStyle == FogStyle.stylized || fogStyle == FogStyle.heightFog)
            {
                Shader.SetGlobalColor("CZY_FogColor1", i.GetColor("_FogColor1"));
                Shader.SetGlobalColor("CZY_FogColor2", i.GetColor("_FogColor2"));
                Shader.SetGlobalColor("CZY_FogColor3", i.GetColor("_FogColor3"));
                Shader.SetGlobalColor("CZY_FogColor4", i.GetColor("_FogColor4"));
                Shader.SetGlobalColor("CZY_FogColor5", i.GetColor("_FogColor5"));

                Shader.SetGlobalFloat("CZY_FogColorStart1", atmosphereProfile.fogStart1);
                Shader.SetGlobalFloat("CZY_FogColorStart2", atmosphereProfile.fogStart2);
                Shader.SetGlobalFloat("CZY_FogColorStart3", atmosphereProfile.fogStart3);
                Shader.SetGlobalFloat("CZY_FogColorStart4", atmosphereProfile.fogStart4);


                Shader.SetGlobalFloat("CZY_FogIntensity", i.GetFloat("_FogIntensity"));
                Shader.SetGlobalFloat("CZY_FogOffset", i.GetFloat("_FogOffset"));
                Shader.SetGlobalFloat("CZY_FogSmoothness", i.GetFloat("_FogSmoothness"));
                Shader.SetGlobalFloat("CZY_FogDepthMultiplier", i.GetFloat("_FogDepthMultiplier"));
            }

            Shader.SetGlobalColor("CZY_LightColor", i.GetColor("_LightColor"));
            Shader.SetGlobalVector("CZY_SunDirection", -sunLight.transform.forward);
            Shader.SetGlobalFloat("CZY_LightFalloff", fogLightFlareFalloff);
            Shader.SetGlobalFloat("CZY_LightIntensity", fogLightFlareIntensity);


        }

        /// <summary>
        /// Sets the actual shader variables of the sky dome, fog dome, and cloud dome.
        /// </summary> 
        public void SetShaderVariables()
        {

            if (!skyMesh || !cloudMesh || !fogMesh)
                SetupReferences();


            Material skybox = skyMesh.sharedMaterial;
            Material clouds = cloudMesh.sharedMaterial;
            Material fog = fogMesh.sharedMaterial;

            if (clouds.HasProperty("_MinCloudCover"))
                clouds.SetFloat("_MinCloudCover", 0.9f);
            if (clouds.HasProperty("_MaxCloudCover"))
                clouds.SetFloat("_MaxCloudCover", 1.1f);
            if (clouds.HasProperty("_CumulusCoverageMultiplier"))
                clouds.SetFloat("_CumulusCoverageMultiplier", cumulus);
            if (clouds.HasProperty("_NimbusMultiplier"))
                clouds.SetFloat("_NimbusMultiplier", nimbus);
            if (clouds.HasProperty("_NimbusHeight"))
                clouds.SetFloat("_NimbusHeight", nimbusHeight);
            if (clouds.HasProperty("_NimbusVariation"))
                clouds.SetFloat("_NimbusVariation", nimbusVariation);
            if (clouds.HasProperty("_BorderHeight"))
                clouds.SetFloat("_BorderHeight", border);
            if (clouds.HasProperty("_BorderEffect"))
                clouds.SetFloat("_BorderEffect", borderEffect);
            if (clouds.HasProperty("_BorderVariation"))
                clouds.SetFloat("_BorderVariation", borderVariation);
            if (clouds.HasProperty("_AltocumulusMultiplier"))
                clouds.SetFloat("_AltocumulusMultiplier", altocumulus);
            if (clouds.HasProperty("_CirrostratusMultiplier"))
                clouds.SetFloat("_CirrostratusMultiplier", cirrostratus);
            if (clouds.HasProperty("_ChemtrailsMultiplier"))
                clouds.SetFloat("_ChemtrailsMultiplier", chemtrails);
            if (clouds.HasProperty("_CirrusMultiplier"))
                clouds.SetFloat("_CirrusMultiplier", cirrus);
            if (clouds.HasProperty("_CloudTexture"))
                clouds.SetTexture("_CloudTexture", cloudTexture);
            if (clouds.HasProperty("_TexturePanDirection"))
                clouds.SetVector("_TexturePanDirection", texturePanDirection);



            if (atmosphereProfile.skyZenithColor.systemContainsProp)
                skybox.SetColor("_ZenithColor", FilterColor(skyZenithColor));
            if (atmosphereProfile.skyZenithColor.systemContainsProp)
                skybox.SetColor("_HorizonColor", FilterColor(skyHorizonColor));
            if (atmosphereProfile.starColor.systemContainsProp)
                skybox.SetColor("_StarColor", starColor);
            if (atmosphereProfile.galaxyIntensity.systemContainsProp)
                skybox.SetFloat("_GalaxyMultiplier", galaxyIntensity);
            if (skybox.HasProperty("_RainbowIntensity"))
                skybox.SetFloat("_RainbowIntensity", rainbowIntensity);
            if (clouds.HasProperty("_SunDirection"))
                clouds.SetVector("_SunDirection", -sunLight.transform.forward);

            if (atmosphereProfile.gradientExponent.systemContainsProp)
                skybox.SetFloat("_Power", gradientExponent);
            if (atmosphereProfile.atmosphereVariationMax.systemContainsProp)
                skybox.SetFloat("_PatchworkHeight", atmosphereVariationMax);
            if (atmosphereProfile.atmosphereVariationMin.systemContainsProp)
                skybox.SetFloat("_PatchworkVariation", atmosphereVariationMin);
            if (atmosphereProfile.atmosphereBias.systemContainsProp)
                skybox.SetFloat("_PatchworkBias", atmosphereBias);
            if (atmosphereProfile.sunSize.systemContainsProp)
                skybox.SetFloat("_SunSize", sunSize);
            if (atmosphereProfile.sunColor.systemContainsProp)
                skybox.SetColor("_SunColor", FilterColor(sunColor) * sunFilter);
            if (atmosphereProfile.sunFalloff.systemContainsProp)
                skybox.SetFloat("_SunFlareFalloff", sunFalloff);
            if (atmosphereProfile.sunFlareColor.systemContainsProp)
                skybox.SetColor("_SunFlareColor", FilterColor(sunFlareColor) * sunFilter);
            if (atmosphereProfile.moonFlareColor.systemContainsProp)
                skybox.SetColor("_MoonFlareColor", FilterColor(moonFlareColor));
            if (atmosphereProfile.moonFalloff.systemContainsProp)
                skybox.SetFloat("_MoonFlareFalloff", moonFalloff);
            if (atmosphereProfile.galaxy1Color.systemContainsProp)
                skybox.SetColor("_GalaxyColor1", galaxy1Color);
            if (atmosphereProfile.galaxy2Color.systemContainsProp)
                skybox.SetColor("_GalaxyColor2", galaxy2Color);
            if (atmosphereProfile.galaxy3Color.systemContainsProp)
                skybox.SetColor("_GalaxyColor3", galaxy3Color);
            if (atmosphereProfile.lightScatteringColor.systemContainsProp)
                skybox.SetColor("_LightColumnColor", lightScatteringColor);
            if (atmosphereProfile.rainbowPosition.systemContainsProp)
                skybox.SetFloat("_RainbowSize", rainbowPosition);
            if (atmosphereProfile.rainbowWidth.systemContainsProp)
                skybox.SetFloat("_RainbowWidth", rainbowWidth);


            if (VFX)
                if (clouds.HasProperty("_StormDirection"))
                    clouds.SetVector("_StormDirection", -VFX.windManager.windDirection);

            if (atmosphereProfile.cloudColor.systemContainsProp)
                clouds.SetColor("_CloudColor", FilterColor(cloudColor) * cloudFilter);
            if (atmosphereProfile.cloudHighlightColor.systemContainsProp)
                clouds.SetColor("_CloudHighlightColor", FilterColor(cloudHighlightColor) * sunFilter);
            if (atmosphereProfile.highAltitudeCloudColor.systemContainsProp)
                clouds.SetColor("_AltoCloudColor", FilterColor(highAltitudeCloudColor) * cloudFilter);
            if (atmosphereProfile.cloudTextureColor.systemContainsProp)
                clouds.SetColor("_CloudTextureColor", FilterColor(cloudTextureColor) * cloudFilter);
            if (atmosphereProfile.cloudMoonColor.systemContainsProp)
                clouds.SetColor("_MoonColor", FilterColor(cloudMoonColor));
            if (atmosphereProfile.cloudSunHighlightFalloff.systemContainsProp)
                clouds.SetFloat("_SunFlareFalloff", cloudSunHighlightFalloff);
            if (atmosphereProfile.cloudMoonHighlightFalloff.systemContainsProp)
                clouds.SetFloat("_MoonFlareFalloff", cloudMoonHighlightFalloff);
            if (atmosphereProfile.cloudWindSpeed.systemContainsProp)
                clouds.SetFloat("_WindSpeed", cloudWindSpeed);
            if (atmosphereProfile.cloudCohesion.systemContainsProp)
                clouds.SetFloat("_CloudCohesion", cloudCohesion);
            if (atmosphereProfile.spherize.systemContainsProp)
                clouds.SetFloat("_Spherize", spherize);
            if (atmosphereProfile.shadowDistance.systemContainsProp)
                clouds.SetFloat("_ShadowingDistance", shadowDistance);
            if (atmosphereProfile.clippingThreshold.systemContainsProp)
                clouds.SetFloat("_ClippingThreshold", clippingThreshold);
            if (atmosphereProfile.cloudThickness.systemContainsProp)
                clouds.SetFloat("_CloudThickness", cloudThickness);
            if (atmosphereProfile.cloudMainScale.systemContainsProp)
                clouds.SetFloat("_MainCloudScale", cloudMainScale);
            if (atmosphereProfile.cloudDetailScale.systemContainsProp)
                clouds.SetFloat("_DetailScale", cloudDetailScale);
            if (atmosphereProfile.cloudDetailAmount.systemContainsProp)
                clouds.SetFloat("_DetailAmount", cloudDetailAmount);
            if (atmosphereProfile.textureAmount.systemContainsProp)
                clouds.SetFloat("_TextureAmount", textureAmount);
            if (atmosphereProfile.acScale.systemContainsProp)
                clouds.SetFloat("_AltocumulusScale", acScale);
            if (atmosphereProfile.cirroMoveSpeed.systemContainsProp)
                clouds.SetFloat("_CirrostratusMoveSpeed", cirroMoveSpeed);
            if (atmosphereProfile.cirrusMoveSpeed.systemContainsProp)
                clouds.SetFloat("_CirrusMoveSpeed", cirrusMoveSpeed);
            if (atmosphereProfile.chemtrailsMoveSpeed.systemContainsProp)
                clouds.SetFloat("_ChemtrailsMoveSpeed", chemtrailsMoveSpeed);

            if (fogStyle == FogStyle.stylized || fogStyle == FogStyle.heightFog)
            {
                fog.SetFloat("LightIntensity", fogLightFlareIntensity);
                fog.SetFloat("_LightFalloff", fogLightFlareFalloff);
                fog.SetFloat("_FlareSquish", fogLightFlareSquish);
                fog.SetFloat("_FogOffset", fogHeight);
                fog.SetFloat("_FogColorStart1", atmosphereProfile.fogStart1);
                fog.SetFloat("_FogColorStart2", atmosphereProfile.fogStart2);
                fog.SetFloat("_FogColorStart3", atmosphereProfile.fogStart3);
                fog.SetFloat("_FogColorStart4", atmosphereProfile.fogStart4);
                fog.SetFloat("_FogDepthMultiplier", fogDensity * fogDensityMultiplier);
                fog.SetColor("_LightColor", FilterColor(fogFlareColor));
                fog.SetColor("_FogColor1", FilterColor(fogColor1));
                fog.SetColor("_FogColor2", FilterColor(fogColor2));
                fog.SetColor("_FogColor3", FilterColor(fogColor3));
                fog.SetColor("_FogColor4", FilterColor(fogColor4));
                fog.SetColor("_FogColor5", FilterColor(fogColor5));
                fog.SetVector("_SunDirection", -sunLight.transform.forward);

            }
            else if (fogStyle == FogStyle.unity)
            {

                RenderSettings.fogColor = FilterColor(fogColor5);
                RenderSettings.fogDensity = 0.003f * fogDensity * fogDensityMultiplier;

            }


            sunLight.transform.parent.eulerAngles = new Vector3(0, sunDirection, sunPitch);
            sunLight.transform.localEulerAngles = new Vector3((perennialProfile.sunMovementCurve.Evaluate(dayPercentage)) - 90, 0, 0);
            sunLight.color = sunlightColor * sunFilter;

            if (cozyMaterials)
                rainbowIntensity = useRainbow ? cozyMaterials.m_Wetness * (1 - starColor.a) : 0;


            RenderSettings.sun = sunLight;
            RenderSettings.skybox = null;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.ambientSkyColor = FilterColor(ambientLightZenithColor * ambientLightMultiplier);
            RenderSettings.ambientEquatorColor = FilterColor(ambientLightHorizonColor * (1 - (cumulus / 2)) * ambientLightMultiplier);
            RenderSettings.ambientGroundColor = FilterColor(ambientLightHorizonColor * Color.gray * (1 - (cumulus / 2)) * ambientLightMultiplier);



        }

        /// <summary>
        /// Statically sets variables. Only used on awake or in the editor.
        /// </summary> 
        public void ResetVariables()
        {


            if (cozyMaterials)
                rainbowIntensity = useRainbow ? cozyMaterials.m_Wetness * (1 - starColor.a) : 0;
            else
                rainbowIntensity = 0;


            cumulus = currentWeather.cloudSettings.cumulusCoverage;
            altocumulus = currentWeather.cloudSettings.altocumulusCoverage;
            cirrus = currentWeather.cloudSettings.cirrusCoverage;
            cirrostratus = currentWeather.cloudSettings.cirrostratusCoverage;
            chemtrails = currentWeather.cloudSettings.chemtrailCoverage;
            nimbus = currentWeather.cloudSettings.nimbusCoverage;
            nimbusHeight = currentWeather.cloudSettings.nimbusHeightEffect;
            nimbusVariation = currentWeather.cloudSettings.nimbusVariation;
            border = currentWeather.cloudSettings.borderHeight;
            borderEffect = currentWeather.cloudSettings.borderEffect;
            borderVariation = currentWeather.cloudSettings.borderVariation;


            ambientLightHorizonColor = FilterColor(ambientLightHorizonColor);
            ambientLightZenithColor = FilterColor(ambientLightZenithColor);

            fogDensity = currentWeather.fogDensity;



        }

        /// <summary>
        /// Runs a check to verify that only properties used by the current shader settings are set at runtime.
        /// </summary> 
        void CheckSystemProperties()
        {


            Material skyMat = skyMesh.sharedMaterial;
            Material cloudsMat = cloudMesh.sharedMaterial;


            atmosphereProfile.skyZenithColor.systemContainsProp = skyMat.HasProperty("_ZenithColor");
            atmosphereProfile.skyHorizonColor.systemContainsProp = skyMat.HasProperty("_HorizonColor");
            atmosphereProfile.gradientExponent.systemContainsProp = skyMat.HasProperty("_Power");
            atmosphereProfile.atmosphereVariationMin.systemContainsProp = skyMat.HasProperty("_PatchworkHeight");
            atmosphereProfile.atmosphereVariationMax.systemContainsProp = skyMat.HasProperty("_PatchworkVariation");
            atmosphereProfile.atmosphereBias.systemContainsProp = skyMat.HasProperty("_PatchworkBias");
            atmosphereProfile.sunColor.systemContainsProp = skyMat.HasProperty("_SunColor");
            atmosphereProfile.sunSize.systemContainsProp = skyMat.HasProperty("_SunSize");
            atmosphereProfile.sunFalloff.systemContainsProp = skyMat.HasProperty("_SunFlareFalloff");
            atmosphereProfile.sunFlareColor.systemContainsProp = skyMat.HasProperty("_SunFlareColor");
            atmosphereProfile.galaxy1Color.systemContainsProp = skyMat.HasProperty("_GalaxyColor1");
            atmosphereProfile.galaxy2Color.systemContainsProp = skyMat.HasProperty("_GalaxyColor2");
            atmosphereProfile.galaxy3Color.systemContainsProp = skyMat.HasProperty("_GalaxyColor3");
            atmosphereProfile.moonFlareColor.systemContainsProp = skyMat.HasProperty("_MoonFlareColor");
            atmosphereProfile.moonFalloff.systemContainsProp = skyMat.HasProperty("_MoonFlareFalloff");
            atmosphereProfile.rainbowWidth.systemContainsProp = skyMat.HasProperty("_RainbowWidth");
            atmosphereProfile.rainbowPosition.systemContainsProp = skyMat.HasProperty("_RainbowSize");
            atmosphereProfile.starColor.systemContainsProp = skyMat.HasProperty("_StarColor");
            atmosphereProfile.lightScatteringColor.systemContainsProp = skyMat.HasProperty("_LightColumnColor");
            atmosphereProfile.galaxyIntensity.systemContainsProp = skyMat.HasProperty("_GalaxyMultiplier");


            atmosphereProfile.cloudColor.systemContainsProp = cloudsMat.HasProperty("_CloudColor");
            atmosphereProfile.cloudHighlightColor.systemContainsProp = cloudsMat.HasProperty("_CloudHighlightColor");
            atmosphereProfile.highAltitudeCloudColor.systemContainsProp = cloudsMat.HasProperty("_AltoCloudColor");
            atmosphereProfile.clippingThreshold.systemContainsProp = cloudsMat.HasProperty("_ClippingThreshold");
            atmosphereProfile.highAltitudeCloudColor.systemContainsProp = cloudsMat.HasProperty("_AltoCloudColor");
            atmosphereProfile.cloudMoonHighlightFalloff.systemContainsProp = cloudsMat.HasProperty("_MoonFlareFalloff");
            atmosphereProfile.cloudMoonColor.systemContainsProp = cloudsMat.HasProperty("_MoonColor");
            atmosphereProfile.cloudMainScale.systemContainsProp = cloudsMat.HasProperty("_MainCloudScale");
            atmosphereProfile.cloudDetailScale.systemContainsProp = cloudsMat.HasProperty("_DetailScale");
            atmosphereProfile.cloudDetailAmount.systemContainsProp = cloudsMat.HasProperty("_DetailAmount");
            atmosphereProfile.cloudSunHighlightFalloff.systemContainsProp = cloudsMat.HasProperty("_SunFlareFalloff");
            atmosphereProfile.cloudWindSpeed.systemContainsProp = cloudsMat.HasProperty("_WindSpeed");
            atmosphereProfile.cloudDetailScale.systemContainsProp = cloudsMat.HasProperty("_DetailScale");
            atmosphereProfile.cloudDetailAmount.systemContainsProp = cloudsMat.HasProperty("_DetailAmount");
            atmosphereProfile.acScale.systemContainsProp = cloudsMat.HasProperty("_AltocumulusScale");
            atmosphereProfile.chemtrailsMoveSpeed.systemContainsProp = cloudsMat.HasProperty("_ChemtrailsMoveSpeed");
            atmosphereProfile.cirroMoveSpeed.systemContainsProp = cloudsMat.HasProperty("_CirrostratusMoveSpeed");
            atmosphereProfile.cirrusMoveSpeed.systemContainsProp = cloudsMat.HasProperty("_CirrusMoveSpeed");
            atmosphereProfile.cloudCohesion.systemContainsProp = cloudsMat.HasProperty("_CloudCohesion");
            atmosphereProfile.spherize.systemContainsProp = cloudsMat.HasProperty("_Spherize");
            atmosphereProfile.shadowDistance.systemContainsProp = cloudsMat.HasProperty("_ShadowingDistance");
            atmosphereProfile.cloudThickness.systemContainsProp = cloudsMat.HasProperty("_CloudThickness");



        }

        /// <summary>
        /// Smoothly interpolates the current atmosphere profile and all of the impacted settings by the transition time.
        /// </summary> 
        public void ChangeAtmosphere(AtmosphereProfile start, AtmosphereProfile end, float time)
        {

            if (atmosphereControl == AtmosphereSelection.native || transitioningAtmosphere)
            {

                StartCoroutine(TransitionAtmosphere(end, time));

            }
            else
                StartCoroutine(TransitionAtmosphere(start, end, time));

        }

        IEnumerator TransitionAtmosphere(AtmosphereProfile start, AtmosphereProfile end, float time)
        {

            if (atmosphereControl == AtmosphereSelection.native)
            {
                Debug.LogWarning("Atmosphere transitioning requires the atmosphere selection to be set to profile.");
                yield break;
            }

            transitioningAtmosphere = true;
            float t = time;

            while (t > 0)
            {

                float div = 1 - (t / time);
                yield return new WaitForEndOfFrame();

                gradientExponent = Mathf.Lerp(start.gradientExponent.GetFloatValue(dayPercentage),
                    end.gradientExponent.GetFloatValue(dayPercentage), div);
                acScale = Mathf.Lerp(start.acScale.GetFloatValue(dayPercentage),
                    end.acScale.GetFloatValue(dayPercentage), div);
                ambientLightHorizonColor = Color.Lerp(start.ambientLightHorizonColor.GetColorValue(dayPercentage),
                    end.ambientLightHorizonColor.GetColorValue(dayPercentage), div);
                ambientLightZenithColor = Color.Lerp(start.ambientLightZenithColor.GetColorValue(dayPercentage),
                    end.ambientLightZenithColor.GetColorValue(dayPercentage), div);
                ambientLightMultiplier = Mathf.Lerp(start.ambientLightMultiplier.GetFloatValue(dayPercentage),
                    end.ambientLightMultiplier.GetFloatValue(dayPercentage), div);
                atmosphereBias = Mathf.Lerp(start.atmosphereBias.GetFloatValue(dayPercentage),
                    end.atmosphereBias.GetFloatValue(dayPercentage), div);
                atmosphereVariationMax = Mathf.Lerp(start.atmosphereVariationMax.GetFloatValue(dayPercentage),
                    end.atmosphereVariationMax.GetFloatValue(dayPercentage), div);
                atmosphereVariationMin = Mathf.Lerp(start.atmosphereVariationMin.GetFloatValue(dayPercentage),
                    end.atmosphereVariationMin.GetFloatValue(dayPercentage), div);
                chemtrailsMoveSpeed = Mathf.Lerp(start.chemtrailsMoveSpeed.GetFloatValue(dayPercentage),
                    end.chemtrailsMoveSpeed.GetFloatValue(dayPercentage), div);
                cirroMoveSpeed = Mathf.Lerp(start.cirroMoveSpeed.GetFloatValue(dayPercentage),
                    end.cirroMoveSpeed.GetFloatValue(dayPercentage), div);
                cirrusMoveSpeed = Mathf.Lerp(start.cirrusMoveSpeed.GetFloatValue(dayPercentage),
                    end.cirrusMoveSpeed.GetFloatValue(dayPercentage), div);
                clippingThreshold = Mathf.Lerp(start.clippingThreshold.GetFloatValue(dayPercentage),
                    end.clippingThreshold.GetFloatValue(dayPercentage), div);
                cloudCohesion = Mathf.Lerp(start.cloudCohesion.GetFloatValue(dayPercentage),
                    end.cloudCohesion.GetFloatValue(dayPercentage), div);
                cloudColor = Color.Lerp(start.cloudColor.GetColorValue(dayPercentage),
                    end.cloudColor.GetColorValue(dayPercentage), div);
                cloudDetailAmount = Mathf.Lerp(start.cloudDetailAmount.GetFloatValue(dayPercentage),
                    end.cloudDetailAmount.GetFloatValue(dayPercentage), div);
                cloudDetailScale = Mathf.Lerp(start.cloudDetailScale.GetFloatValue(dayPercentage),
                    end.cloudDetailScale.GetFloatValue(dayPercentage), div);
                cloudHighlightColor = Color.Lerp(start.cloudHighlightColor.GetColorValue(dayPercentage),
                    end.cloudHighlightColor.GetColorValue(dayPercentage), div);
                cloudMainScale = Mathf.Lerp(start.cloudMainScale.GetFloatValue(dayPercentage),
                    end.cloudMainScale.GetFloatValue(dayPercentage), div);
                cloudMoonColor = Color.Lerp(start.cloudMoonColor.GetColorValue(dayPercentage),
                    end.cloudMoonColor.GetColorValue(dayPercentage), div);
                cloudMoonHighlightFalloff = Mathf.Lerp(start.cloudMoonHighlightFalloff.GetFloatValue(dayPercentage),
                    end.cloudMoonHighlightFalloff.GetFloatValue(dayPercentage), div);
                cloudSunHighlightFalloff = Mathf.Lerp(start.cloudSunHighlightFalloff.GetFloatValue(dayPercentage),
                    end.cloudSunHighlightFalloff.GetFloatValue(dayPercentage), div);
                cloudTextureColor = Color.Lerp(start.cloudTextureColor.GetColorValue(dayPercentage),
                    end.cloudTextureColor.GetColorValue(dayPercentage), div);
                cloudThickness = Mathf.Lerp(start.cloudThickness.GetFloatValue(dayPercentage),
                    end.cloudThickness.GetFloatValue(dayPercentage), div);
                cloudWindSpeed = Mathf.Lerp(start.cloudWindSpeed.GetFloatValue(dayPercentage),
                    end.cloudWindSpeed.GetFloatValue(dayPercentage), div);
                fogColor1 = Color.Lerp(start.fogColor1.GetColorValue(dayPercentage),
                    end.fogColor1.GetColorValue(dayPercentage), div);
                fogColor2 = Color.Lerp(start.fogColor2.GetColorValue(dayPercentage),
                    end.fogColor2.GetColorValue(dayPercentage), div);
                fogColor3 = Color.Lerp(start.fogColor3.GetColorValue(dayPercentage),
                    end.fogColor3.GetColorValue(dayPercentage), div);
                fogColor4 = Color.Lerp(start.fogColor4.GetColorValue(dayPercentage),
                    end.fogColor4.GetColorValue(dayPercentage), div);
                fogColor5 = Color.Lerp(start.fogColor5.GetColorValue(dayPercentage),
                    end.fogColor5.GetColorValue(dayPercentage), div);
                fogStart1 = Mathf.Lerp(start.fogStart1,
                    end.fogStart1, div);
                fogStart2 = Mathf.Lerp(start.fogStart2,
                    end.fogStart2, div);
                fogStart3 = Mathf.Lerp(start.fogStart3,
                    end.fogStart3, div);
                fogStart4 = Mathf.Lerp(start.fogStart4,
                    end.fogStart4, div);
                fogDensityMultiplier = Mathf.Lerp(start.fogDensityMultiplier.GetFloatValue(dayPercentage),
                    end.fogDensityMultiplier.GetFloatValue(dayPercentage), div);
                fogFlareColor = Color.Lerp(start.fogFlareColor.GetColorValue(dayPercentage),
                    end.fogFlareColor.GetColorValue(dayPercentage), div);
                fogHeight = Mathf.Lerp(start.fogHeight.GetFloatValue(dayPercentage),
                    end.fogHeight.GetFloatValue(dayPercentage), div);
                fogLightFlareFalloff = Mathf.Lerp(start.fogLightFlareFalloff.GetFloatValue(dayPercentage),
                    end.fogLightFlareFalloff.GetFloatValue(dayPercentage), div);
                fogLightFlareIntensity = Mathf.Lerp(start.fogLightFlareIntensity.GetFloatValue(dayPercentage),
                    end.fogLightFlareIntensity.GetFloatValue(dayPercentage), div);
                fogLightFlareSquish = Mathf.Lerp(start.fogLightFlareSquish.GetFloatValue(dayPercentage),
                    end.fogLightFlareSquish.GetFloatValue(dayPercentage), div);
                galaxy1Color = Color.Lerp(start.galaxy1Color.GetColorValue(dayPercentage),
                    end.galaxy1Color.GetColorValue(dayPercentage), div);
                galaxy2Color = Color.Lerp(start.galaxy2Color.GetColorValue(dayPercentage),
                    end.galaxy2Color.GetColorValue(dayPercentage), div);
                galaxy3Color = Color.Lerp(start.galaxy3Color.GetColorValue(dayPercentage),
                    end.galaxy3Color.GetColorValue(dayPercentage), div);
                galaxyIntensity = Mathf.Lerp(start.galaxyIntensity.GetFloatValue(dayPercentage),
                    end.galaxyIntensity.GetFloatValue(dayPercentage), div);
                highAltitudeCloudColor = Color.Lerp(start.highAltitudeCloudColor.GetColorValue(dayPercentage),
                    end.highAltitudeCloudColor.GetColorValue(dayPercentage), div);
                lightScatteringColor = Color.Lerp(start.lightScatteringColor.GetColorValue(dayPercentage),
                    end.lightScatteringColor.GetColorValue(dayPercentage), div);
                moonlightColor = Color.Lerp(start.moonlightColor.GetColorValue(dayPercentage),
                    end.moonlightColor.GetColorValue(dayPercentage), div);
                moonFalloff = Mathf.Lerp(start.moonFalloff.GetFloatValue(dayPercentage),
                    end.moonFalloff.GetFloatValue(dayPercentage), div);
                moonFlareColor = Color.Lerp(start.moonFlareColor.GetColorValue(dayPercentage),
                    end.moonFlareColor.GetColorValue(dayPercentage), div);
                rainbowPosition = Mathf.Lerp(start.rainbowPosition.GetFloatValue(dayPercentage),
                    end.rainbowPosition.GetFloatValue(dayPercentage), div);
                rainbowWidth = Mathf.Lerp(start.rainbowWidth.GetFloatValue(dayPercentage),
                    end.rainbowWidth.GetFloatValue(dayPercentage), div);
                shadowDistance = Mathf.Lerp(start.shadowDistance.GetFloatValue(dayPercentage),
                    end.shadowDistance.GetFloatValue(dayPercentage), div);
                skyHorizonColor = Color.Lerp(start.skyHorizonColor.GetColorValue(dayPercentage),
                    end.skyHorizonColor.GetColorValue(dayPercentage), div);
                skyZenithColor = Color.Lerp(start.skyZenithColor.GetColorValue(dayPercentage),
                    end.skyZenithColor.GetColorValue(dayPercentage), div);
                spherize = Mathf.Lerp(start.spherize.GetFloatValue(dayPercentage),
                    end.spherize.GetFloatValue(dayPercentage), div);
                starColor = Color.Lerp(start.starColor.GetColorValue(dayPercentage),
                    end.starColor.GetColorValue(dayPercentage), div);
                sunColor = Color.Lerp(start.sunColor.GetColorValue(dayPercentage),
                    end.sunColor.GetColorValue(dayPercentage), div);
                sunDirection = Mathf.Lerp(start.sunDirection.GetFloatValue(dayPercentage),
                    end.sunDirection.GetFloatValue(dayPercentage), div);
                sunFalloff = Mathf.Lerp(start.sunFalloff.GetFloatValue(dayPercentage),
                    end.sunFalloff.GetFloatValue(dayPercentage), div);
                sunFlareColor = Color.Lerp(start.sunFlareColor.GetColorValue(dayPercentage),
                    end.sunFlareColor.GetColorValue(dayPercentage), div);
                sunlightColor = Color.Lerp(start.sunlightColor.GetColorValue(dayPercentage),
                    end.sunlightColor.GetColorValue(dayPercentage), div);
                sunPitch = Mathf.Lerp(start.sunPitch.GetFloatValue(dayPercentage),
                    end.sunPitch.GetFloatValue(dayPercentage), div);
                sunSize = Mathf.Lerp(start.sunSize.GetFloatValue(dayPercentage),
                    end.sunSize.GetFloatValue(dayPercentage), div);
                textureAmount = Mathf.Lerp(start.textureAmount.GetFloatValue(dayPercentage),
                    end.textureAmount.GetFloatValue(dayPercentage), div);

                t -= Time.deltaTime;

            }

            transitioningAtmosphere = false;

        }

        IEnumerator TransitionAtmosphere(AtmosphereProfile end, float time)
        {


            float gradientExponentStart = gradientExponent;
            float acScaleStart = acScale;
            Color ambientLightHorizonColorStart = ambientLightHorizonColor;
            Color ambientLightZenithColorStart = ambientLightZenithColor;
            float ambientLightMultiplierStart = ambientLightMultiplier;
            float atmosphereBiasStart = atmosphereBias;
            float atmosphereVariationMaxStart = atmosphereVariationMax;
            float atmosphereVariationMinStart = atmosphereVariationMin;
            float chemtrailsMoveSpeedStart = chemtrailsMoveSpeed;
            float cirroMoveSpeedStart = cirroMoveSpeed;
            float cirrusMoveSpeedStart = cirrusMoveSpeed;
            float clippingThresholdStart = clippingThreshold;
            float cloudCohesionStart = cloudCohesion;
            Color cloudColorStart = cloudColor;
            float cloudDetailAmountStart = cloudDetailAmount;
            float cloudDetailScaleStart = cloudDetailScale;
            Color cloudHighlightColorStart = cloudHighlightColor;
            float cloudMainScaleStart = cloudMainScale;
            Color cloudMoonColorStart = cloudMoonColor;
            float cloudMoonHighlightFalloffStart = cloudMoonHighlightFalloff;
            float cloudSunHighlightFalloffStart = cloudSunHighlightFalloff;
            Color cloudTextureColorStart = cloudTextureColor;
            float cloudThicknessStart = cloudThickness;
            float cloudWindSpeedStart = cloudWindSpeed;
            Color fogColor1Start = fogColor1;
            Color fogColor2Start = fogColor2;
            Color fogColor3Start = fogColor3;
            Color fogColor4Start = fogColor4;
            Color fogColor5Start = fogColor5;
            float fogStart1Start = fogStart1;
            float fogStart2Start = fogStart2;
            float fogStart3Start = fogStart3;
            float fogStart4Start = fogStart4;
            float fogDensityMultiplierStart = fogDensityMultiplier;
            Color fogFlareColorStart = fogFlareColor;
            float fogHeightStart = fogHeight;
            float fogLightFlareFalloffStart = fogLightFlareFalloff;
            float fogLightFlareIntensityStart = fogLightFlareIntensity;
            float fogLightFlareSquishStart = fogLightFlareSquish;
            Color galaxy1ColorStart = galaxy1Color;
            Color galaxy2ColorStart = galaxy2Color;
            Color galaxy3ColorStart = galaxy3Color;
            Color highAltitudeCloudColorStart = highAltitudeCloudColor;
            Color lightScatteringColorStart = lightScatteringColor;
            Color moonlightColorStart = moonlightColor;
            Color moonFlareColorStart = moonFlareColor;
            Color skyHorizonColorStart = skyHorizonColor;
            Color skyZenithColorStart = skyZenithColor;
            Color starColorStart = starColor;
            Color sunColorStart = sunColor;
            Color sunFlareColorStart = sunFlareColor;
            Color sunlightColorStart = sunlightColor;
            float galaxyIntensityStart = galaxyIntensity;
            float moonFalloffStart = moonFalloff;
            float rainbowPositionStart = rainbowPosition;
            float rainbowWidthStart = rainbowWidth;
            float shadowDistanceStart = shadowDistance;
            float spherizeStart = spherize;
            float sunDirectionStart = sunDirection;
            float sunFalloffStart = sunFalloff;
            float sunPitchStart = sunPitch;
            float sunSizeStart = sunSize;
            float textureAmountStart = textureAmount;


            transitioningAtmosphere = true;
            float t = time;

            while (t > 0)
            {

                float div = 1 - (t / time);
                yield return new WaitForEndOfFrame();

                gradientExponent = Mathf.Lerp(gradientExponentStart, end.gradientExponent.GetFloatValue(dayPercentage), div);
                acScale = Mathf.Lerp(acScaleStart, end.acScale.GetFloatValue(dayPercentage), div);
                ambientLightHorizonColor = Color.Lerp(ambientLightHorizonColorStart, end.ambientLightHorizonColor.GetColorValue(dayPercentage), div);
                ambientLightZenithColor = Color.Lerp(ambientLightZenithColorStart, end.ambientLightZenithColor.GetColorValue(dayPercentage), div);
                ambientLightMultiplier = Mathf.Lerp(ambientLightMultiplierStart, end.ambientLightMultiplier.GetFloatValue(dayPercentage), div);
                atmosphereBias = Mathf.Lerp(atmosphereBiasStart, end.atmosphereBias.GetFloatValue(dayPercentage), div);
                atmosphereVariationMax = Mathf.Lerp(atmosphereVariationMaxStart, end.atmosphereVariationMax.GetFloatValue(dayPercentage), div);
                atmosphereVariationMin = Mathf.Lerp(atmosphereVariationMinStart, end.atmosphereVariationMin.GetFloatValue(dayPercentage), div);
                chemtrailsMoveSpeed = Mathf.Lerp(chemtrailsMoveSpeedStart, end.chemtrailsMoveSpeed.GetFloatValue(dayPercentage), div);
                cirroMoveSpeed = Mathf.Lerp(cirroMoveSpeedStart, end.cirroMoveSpeed.GetFloatValue(dayPercentage), div);
                cirrusMoveSpeed = Mathf.Lerp(cirrusMoveSpeedStart, end.cirrusMoveSpeed.GetFloatValue(dayPercentage), div);
                clippingThreshold = Mathf.Lerp(clippingThresholdStart, end.clippingThreshold.GetFloatValue(dayPercentage), div);
                cloudCohesion = Mathf.Lerp(cloudCohesionStart, end.cloudCohesion.GetFloatValue(dayPercentage), div);
                cloudColor = Color.Lerp(cloudColorStart, end.cloudColor.GetColorValue(dayPercentage), div);
                cloudDetailAmount = Mathf.Lerp(cloudDetailAmountStart, end.cloudDetailAmount.GetFloatValue(dayPercentage), div);
                cloudDetailScale = Mathf.Lerp(cloudDetailScaleStart, end.cloudDetailScale.GetFloatValue(dayPercentage), div);
                cloudHighlightColor = Color.Lerp(cloudHighlightColorStart, end.cloudHighlightColor.GetColorValue(dayPercentage), div);
                cloudMainScale = Mathf.Lerp(cloudMainScaleStart, end.cloudMainScale.GetFloatValue(dayPercentage), div);
                cloudMoonColor = Color.Lerp(cloudMoonColorStart, end.cloudMoonColor.GetColorValue(dayPercentage), div);
                cloudMoonHighlightFalloff = Mathf.Lerp(cloudMoonHighlightFalloffStart, end.cloudMoonHighlightFalloff.GetFloatValue(dayPercentage), div);
                cloudSunHighlightFalloff = Mathf.Lerp(cloudSunHighlightFalloffStart, end.cloudSunHighlightFalloff.GetFloatValue(dayPercentage), div);
                cloudTextureColor = Color.Lerp(cloudTextureColorStart, end.cloudTextureColor.GetColorValue(dayPercentage), div);
                cloudThickness = Mathf.Lerp(cloudThicknessStart, end.cloudThickness.GetFloatValue(dayPercentage), div);
                cloudWindSpeed = Mathf.Lerp(cloudWindSpeedStart, end.cloudWindSpeed.GetFloatValue(dayPercentage), div);
                fogColor1 = Color.Lerp(fogColor1Start, end.fogColor1.GetColorValue(dayPercentage), div);
                fogColor2 = Color.Lerp(fogColor2Start, end.fogColor2.GetColorValue(dayPercentage), div);
                fogColor3 = Color.Lerp(fogColor3Start, end.fogColor3.GetColorValue(dayPercentage), div);
                fogColor4 = Color.Lerp(fogColor4Start, end.fogColor4.GetColorValue(dayPercentage), div);
                fogColor5 = Color.Lerp(fogColor5Start, end.fogColor5.GetColorValue(dayPercentage), div);
                fogStart1 = Mathf.Lerp(fogStart1Start, end.fogStart1, div);
                fogStart2 = Mathf.Lerp(fogStart2Start, end.fogStart2, div);
                fogStart3 = Mathf.Lerp(fogStart3Start, end.fogStart3, div);
                fogStart4 = Mathf.Lerp(fogStart4Start, end.fogStart4, div);
                fogDensityMultiplier = Mathf.Lerp(fogDensityMultiplierStart, end.fogDensityMultiplier.GetFloatValue(dayPercentage), div);
                fogFlareColor = Color.Lerp(fogFlareColorStart, end.fogFlareColor.GetColorValue(dayPercentage), div);
                fogHeight = Mathf.Lerp(fogHeightStart, end.fogHeight.GetFloatValue(dayPercentage), div);
                fogLightFlareFalloff = Mathf.Lerp(fogLightFlareFalloffStart, end.fogLightFlareFalloff.GetFloatValue(dayPercentage), div);
                fogLightFlareIntensity = Mathf.Lerp(fogLightFlareIntensityStart, end.fogLightFlareIntensity.GetFloatValue(dayPercentage), div);
                fogLightFlareSquish = Mathf.Lerp(fogLightFlareSquishStart, end.fogLightFlareSquish.GetFloatValue(dayPercentage), div);
                galaxy1Color = Color.Lerp(galaxy1ColorStart, end.galaxy1Color.GetColorValue(dayPercentage), div);
                galaxy2Color = Color.Lerp(galaxy2ColorStart, end.galaxy2Color.GetColorValue(dayPercentage), div);
                galaxy3Color = Color.Lerp(galaxy3ColorStart, end.galaxy3Color.GetColorValue(dayPercentage), div);
                galaxyIntensity = Mathf.Lerp(galaxyIntensityStart, end.galaxyIntensity.GetFloatValue(dayPercentage), div);
                highAltitudeCloudColor = Color.Lerp(highAltitudeCloudColorStart, end.highAltitudeCloudColor.GetColorValue(dayPercentage), div);
                lightScatteringColor = Color.Lerp(lightScatteringColorStart, end.lightScatteringColor.GetColorValue(dayPercentage), div);
                moonlightColor = Color.Lerp(moonlightColorStart, end.moonlightColor.GetColorValue(dayPercentage), div);
                moonFalloff = Mathf.Lerp(moonFalloffStart, end.moonFalloff.GetFloatValue(dayPercentage), div);
                moonFlareColor = Color.Lerp(moonFlareColorStart, end.moonFlareColor.GetColorValue(dayPercentage), div);
                rainbowPosition = Mathf.Lerp(rainbowPositionStart, end.rainbowPosition.GetFloatValue(dayPercentage), div);
                rainbowWidth = Mathf.Lerp(rainbowWidthStart, end.rainbowWidth.GetFloatValue(dayPercentage), div);
                shadowDistance = Mathf.Lerp(shadowDistanceStart, end.shadowDistance.GetFloatValue(dayPercentage), div);
                skyHorizonColor = Color.Lerp(skyHorizonColorStart, end.skyHorizonColor.GetColorValue(dayPercentage), div);
                skyZenithColor = Color.Lerp(skyZenithColorStart, end.skyZenithColor.GetColorValue(dayPercentage), div);
                spherize = Mathf.Lerp(spherizeStart, end.spherize.GetFloatValue(dayPercentage), div);
                starColor = Color.Lerp(starColorStart, end.starColor.GetColorValue(dayPercentage), div);
                sunColor = Color.Lerp(sunColorStart, end.sunColor.GetColorValue(dayPercentage), div);
                sunDirection = Mathf.Lerp(sunDirectionStart, end.sunDirection.GetFloatValue(dayPercentage), div);
                sunFalloff = Mathf.Lerp(sunFalloffStart, end.sunFalloff.GetFloatValue(dayPercentage), div);
                sunFlareColor = Color.Lerp(sunFlareColorStart, end.sunFlareColor.GetColorValue(dayPercentage), div);
                sunlightColor = Color.Lerp(sunlightColorStart, end.sunlightColor.GetColorValue(dayPercentage), div);
                sunPitch = Mathf.Lerp(sunPitchStart, end.sunPitch.GetFloatValue(dayPercentage), div);
                sunSize = Mathf.Lerp(sunSizeStart, end.sunSize.GetFloatValue(dayPercentage), div);
                textureAmount = Mathf.Lerp(textureAmountStart, end.textureAmount.GetFloatValue(dayPercentage), div);

                t -= Time.deltaTime;

            }

            transitioningAtmosphere = false;

        }

        /// <summary>
        /// Returns the input color filtered by the current weather filter.
        /// </summary> 
        public Color FilterColor(Color color)
        {

            float h;
            float s;
            float v;
            float a = color.a;
            Color j;

            Color.RGBToHSV(color, out h, out s, out v);

            s = Mathf.Clamp(s + filterSaturation, 0, 10);
            v = Mathf.Clamp(v + filterValue, 0, 10);

            j = Color.HSVToRGB(h, s, v);

            j *= filterColor;
            j.a = a;

            return j;

        }

        /// <summary>
        /// Calculates the weather color filter based on the currently active filter FX profiles.
        /// </summary> 
        public void CalculateFilterColors()
        {


            if (!Application.isPlaying)
            {
                foreach (FXProfile l in currentWeather.FX)
                    try
                    {
                        FilterFX m = (FilterFX)l;
                        filterSaturation = m.filterSaturation;
                        filterValue = m.filterValue;
                        filterColor = m.filterColor;
                        sunFilter = m.sunFilter;
                        cloudFilter = m.cloudFilter;
                        return;

                    }
                    catch
                    {
                        continue;
                    }
            }


            if (defaultFilter == null)
                defaultFilter = (FilterFX)Resources.Load("Default Filter");

            FilterFX i = defaultFilter;

            List<FilterFX> currentFilters = new List<FilterFX>();
            float total = 0;

            foreach (FilterFX j in possibleFilters)
            {
                if (j.weight > 0)
                {

                    currentFilters.Add(j);
                    total += j.weight;

                }
            }

            if (currentFilters.Count == 0)
            {

                filterSaturation = i.filterSaturation;
                filterValue = i.filterValue;
                filterColor = i.filterColor;
                sunFilter = i.sunFilter;
                cloudFilter = i.cloudFilter;
                return;

            }

            i.weight = Mathf.Clamp01(1 - total);

            if (i.weight > 0)
            {
                currentFilters.Add(i);
                total += i.weight;
            }


            filterSaturation = currentFilters[0].filterSaturation;
            filterValue = currentFilters[0].filterValue;
            filterColor = currentFilters[0].filterColor;
            sunFilter = currentFilters[0].sunFilter;
            cloudFilter = currentFilters[0].cloudFilter;

            foreach (FilterFX j in currentFilters)
            {

                float weight = j.weight / total;

                filterSaturation = Mathf.Lerp(filterSaturation, j.filterSaturation, weight);
                filterValue = Mathf.Lerp(filterValue, j.filterValue, weight);
                filterColor = Color.Lerp(filterColor, j.filterColor, weight);
                sunFilter = Color.Lerp(sunFilter, j.sunFilter, weight);
                cloudFilter = Color.Lerp(cloudFilter, j.cloudFilter, weight);

            }
        }

        /// <summary>
        /// Get the current instance of Cozy in the scene. Returns null if no weather sphere is found.
        /// </summary> 
        static public CozyWeather instance
        {

            get
            {
                if (cachedInstance)
                    return cachedInstance;

                cachedInstance = FindObjectOfType<CozyWeather>();
                return cachedInstance;


            }

        }

        static CozyWeather cachedInstance;

        /// <summary>
        /// Returns the current weather with the highest weight.
        /// </summary> 
        public WeatherProfile GetCurrentWeatherProfile()
        {

            WeatherProfile i = null;
            float k = 0;

            foreach (WeightedWeather j in currentWeatherProfiles) if (j.weight > k) { i = j.profile; k = j.weight; }

            return i;

        }


        #region TIME

        /// <summary>
        /// Constrains the time and day to fit within the length parameters set on the perennial profile.
        /// </summary> 
        private void ConstrainTime()
        {
            if (timeControl == TimeControl.native)
            {
                if (calendar.currentTicks > perennialProfile.ticksPerDay)
                {
                    calendar.currentTicks -= perennialProfile.ticksPerDay;
                    calendar.currentDay++;
                    events.RaiseOnDayChange();
                }

                if (calendar.currentTicks < 0)
                {
                    calendar.currentTicks += perennialProfile.ticksPerDay;
                    calendar.currentDay--;
                    events.RaiseOnDayChange();
                }

                if (calendar.currentDay >= perennialProfile.daysPerYear)
                {
                    calendar.currentDay -= perennialProfile.daysPerYear;
                    calendar.currentYear++;
                    events.RaiseOnYearChange();
                }

                if (calendar.currentDay < 0)
                {
                    calendar.currentDay += perennialProfile.daysPerYear;
                    calendar.currentYear--;
                    events.RaiseOnYearChange();
                }
            }
            else
            {
                if (perennialProfile.currentTicks > perennialProfile.ticksPerDay)
                {
                    perennialProfile.currentTicks -= perennialProfile.ticksPerDay;
                    perennialProfile.currentDay++;
                    events.RaiseOnDayChange();
                }

                if (perennialProfile.currentTicks < 0)
                {
                    perennialProfile.currentTicks += perennialProfile.ticksPerDay;
                    perennialProfile.currentDay--;
                    events.RaiseOnDayChange();
                }

                if (perennialProfile.currentDay >= perennialProfile.daysPerYear)
                {
                    perennialProfile.currentDay -= perennialProfile.daysPerYear;
                    perennialProfile.currentYear++;
                    events.RaiseOnYearChange();
                }

                if (perennialProfile.currentDay < 0)
                {
                    perennialProfile.currentDay += perennialProfile.daysPerYear;
                    perennialProfile.currentYear--;
                    events.RaiseOnYearChange();
                }
            }
        }

        /// <summary>
        /// Returns the current time in percentage (0 - 1).
        /// </summary> 
        public float GetCurrentDayPercentage()
        {


            return currentTicks / perennialProfile.ticksPerDay;


        }
        /// <summary>
        /// Returns the current day percentage after being modified by a curve. Used to set the sun rotation and colors .
        /// </summary> 
        public float GetModifiedDayPercentage()
        {


            return usePhysicalSunHeight ? perennialProfile.sunMovementCurve.Evaluate(dayPercentage) / 360 : dayPercentage;


        }

        /// <summary>
        /// Returns the current year percentage (0 - 1).
        /// </summary> 
        public float GetCurrentYearPercentage()
        {
            float dat = DayAndTime();
            return dat / perennialProfile.daysPerYear;
        }

        /// <summary>
        /// Returns the current year percentage (0 - 1) after a number of ticks has passed.
        /// </summary> 
        public float GetCurrentYearPercentage(float inTicks)
        {
            float dat = DayAndTime() + (inTicks / perennialProfile.ticksPerDay);
            return dat / perennialProfile.daysPerYear;
        }

        /// <summary>
        /// Gets the current day plus the current day percentage (0-1). 
        /// </summary> 
        public float DayAndTime()
        {

            if (timeControl == TimeControl.native)
                return calendar.currentDay + (calendar.currentTicks / perennialProfile.ticksPerDay);
            else
                return perennialProfile.currentDay + (perennialProfile.currentTicks / perennialProfile.ticksPerDay);

        }


        /// <summary>
        /// Manages the movement of time in the scene.
        /// </summary> 
        public void ManageTime()
        {

            if (Application.isPlaying && !perennialProfile.pauseTime)
                currentTicks += perennialProfile.ModifiedTickSpeed() * Time.deltaTime;

            if (events.useEvents)
                ManageTimeEvents();
            ConstrainTime();

        }

        private void ManageTimeEvents()
        {

            if (dayPercentage > events.timeToCheckFor)
                switch (events.timeToCheckFor)
                {
                    case (0.25f):
                        events.RaiseOnMorning();
                        events.timeToCheckFor = 0.5f;
                        break;
                    case (0.5f):
                        events.RaiseOnNoon();
                        events.timeToCheckFor = 0.75f;
                        break;
                    case (0.75f):
                        events.RaiseOnEvening();
                        events.timeToCheckFor = 1f;
                        break;
                    case (1):
                        events.RaiseOnMidnight();
                        events.timeToCheckFor = 0.25f;
                        break;
                }

            if (dayPercentage < events.timeToCheckFor - 0.25f) { SetupTimeEvents(); }

            if (Mathf.FloorToInt(currentTicks) != events.currentTick)
            {
                events.currentTick = Mathf.FloorToInt(currentTicks);
                events.RaiseOnTickPass();
            }
            if (Mathf.FloorToInt(dayPercentage * 24) != events.currentHour)
            {
                events.currentHour = Mathf.FloorToInt(dayPercentage * 24);
                events.RaiseOnNewHour();
            }



        }

        private void SetupTimeEvents()
        {

            float i = GetCurrentDayPercentage();

            if (i > 0)
                events.timeToCheckFor = 0.25f;
            if (i > 0.25f)
                events.timeToCheckFor = 0.5f;
            if (i > 0.5f)
                events.timeToCheckFor = 0.75f;
            if (i > 0.75f)
                events.timeToCheckFor = 1;

            events.currentTick = Mathf.FloorToInt(currentTicks);
            events.currentHour = Mathf.FloorToInt(dayPercentage * 24);


        }

        /// <summary>
        /// Skips the weather system forward by the ticksToSkip value.
        /// </summary> 
        public void SkipTime(float ticksToSkip)
        {


            currentTicks += ticksToSkip;

            if (GetModule<CozyAmbienceManager>())
                GetModule<CozyAmbienceManager>().SkipTicks(ticksToSkip);

            foreach (CozyEcosystem i in ecosystems)
            {
                i.SkipTicks(ticksToSkip);
            }

            ResetVariables();

        }

        public void SkipTime(float ticksToSkip, int daysToSkip)
        {


            currentTicks += ticksToSkip;
            currentDay += daysToSkip;

            if (GetModule<CozyAmbienceManager>())
                GetModule<CozyAmbienceManager>().SkipTicks(ticksToSkip + (weatherSphere.perennialProfile.ticksPerDay * daysToSkip));

            foreach (CozyEcosystem i in weatherSphere.ecosystems)
            {
                i.SkipTicks(ticksToSkip + (weatherSphere.perennialProfile.ticksPerDay * daysToSkip));
            }

            weatherSphere.ResetVariables();

        }

        /// <summary>
        /// Returns the title for the current month.
        /// </summary> 
        public string MonthTitle(float month)
        {


            if (perennialProfile.realisticYear)
            {

                GetCurrentMonth(out string monthName, out int monthDay, out float monthPercentage);
                return monthName + " " + monthDay;

            }
            else
            {

                float j = Mathf.Floor(month * 12);
                float monthLength = perennialProfile.daysPerYear / 12;
                float monthTime = DayAndTime() - (j * monthLength);

                PerennialProfile.DefaultYear monthName = (PerennialProfile.DefaultYear)j;
                PerennialProfile.TimeDivisors monthTimeName = PerennialProfile.TimeDivisors.Mid;

                if ((monthTime / monthLength) < 0.33f)
                    monthTimeName = PerennialProfile.TimeDivisors.Early;
                else if ((monthTime / monthLength) > 0.66f)
                    monthTimeName = PerennialProfile.TimeDivisors.Late;
                else
                    monthTimeName = PerennialProfile.TimeDivisors.Mid;


                return $"{monthTimeName} {monthName}";
            }
        }

        public void GetCurrentMonth(out string monthName, out int monthDay, out float monthPercentage)
        {

            int i = currentDay;
            int j = 0;

            while (i > ((perennialProfile.useLeapYear && currentYear % 4 == 0) ? perennialProfile.leapYear[j].days : perennialProfile.standardYear[j].days))
            {

                i -= (perennialProfile.useLeapYear && currentYear % 4 == 0) ? perennialProfile.leapYear[j].days : perennialProfile.standardYear[j].days;

                j++;

                if (j >= ((perennialProfile.useLeapYear && currentYear % 4 == 0) ? perennialProfile.leapYear.Length : perennialProfile.standardYear.Length))
                    break;

            }

            PerennialProfile.Month k = (perennialProfile.useLeapYear && currentYear % 4 == 0) ? perennialProfile.leapYear[j] : perennialProfile.standardYear[j];

            monthName = k.name;
            monthDay = i;
            monthPercentage = k.days;


        }


        /// <summary>
        /// Smoothly skips time.
        /// </summary> 
        public void TransitionTime(float ticksToSkip, float time)
        {

            StartCoroutine(TransitionTime(currentTicks, ticksToSkip, time));

        }

        IEnumerator TransitionTime(float startTicks, float ticksToSkip, float time)
        {

            transitioningTime = true;
            float t = time;
            float targetTime = (ticksToSkip / perennialProfile.ticksPerDay - Mathf.Floor(ticksToSkip / perennialProfile.ticksPerDay)) * perennialProfile.ticksPerDay;
            float targetDay = Mathf.Floor(ticksToSkip / perennialProfile.ticksPerDay);
            float transitionSpeed = ticksToSkip / time;

            while (t > 0)
            {

                float div = 1 - (t / time);
                yield return new WaitForEndOfFrame();

                currentTicks += Time.deltaTime * transitionSpeed;

                t -= Time.deltaTime;

            }

            transitioningTime = false;

        }

        #endregion

        #region WEATHER

        void ManageWeatherWeights()
        {

            float j = 0;

            ecosystems.RemoveAll(x => x == null);

            foreach (CozyEcosystem i in ecosystems)
            {

                if (i != this) j += i.weight;

            }


            weight = Mathf.Clamp01(1 - j);

        }

        void GlobalEcosystem()
        {

            currentWeatherProfiles.Clear();

            foreach (CozyEcosystem j in ecosystems)
                if (j.weight > 0)
                {
                    foreach (WeightedWeather i in j.weightedWeatherProfiles)
                    {
                        if (i.weight * j.weight == 0)
                        {
                            i.profile.StopWeather();
                            continue;
                        }

                        if (currentWeatherProfiles.Contains(i))
                        {
                            currentWeatherProfiles.Find(x => x == i).weight += i.weight * j.weight;
                            continue;
                        }

                        WeightedWeather l = new WeightedWeather();
                        l.profile = i.profile;
                        l.weight = i.weight * j.weight;
                        currentWeatherProfiles.Add(l);

                    }
                }
                else
                {
                    foreach (WeightedWeather i in j.weightedWeatherProfiles)
                    {
                        i.profile.StopWeather();
                    }
                }
        }


        #endregion

        #region MODULES

        public void IntitializeModule(Type module)
        {



            if (GetModule(module))
                return;

            modules.Add((CozyModule)moduleHolder.AddComponent(module));
            ResetModules();

        }

        public void DeintitializeModule(CozyModule module)
        {

            modules.Remove(module);
            DestroyImmediate(module);
            ResetModules();

        }

        #endregion

        #region GENERIC

        public T GetModule<T>() where T : CozyModule
        {

            Type type = typeof(T);

            foreach (CozyModule j in modules)
                if (j.GetType() == type)
                    return j as T;

            return null;

        }

        public CozyModule GetModule(Type type)
        {


            foreach (CozyModule j in modules)
                if (j.GetType() == type)
                    return j;

            return null;

        }

        #endregion


    }


    [System.Serializable]
    public class MeridiemTime
    {

        public int hours;
        public int minutes;
        public enum Meridiem { AM, PM }
        public Meridiem meridiem;


        public static float MeridiemTimeToDeyPercent(int hours, int minutes, Meridiem meridiem)
        {

            return (((float)ConvertTo12to1(hours) + (float)minutes / 60) / 24) + (meridiem == Meridiem.PM ? 0.5f : 0);

        }
        public static float MeridiemTimeToDeyPercent(MeridiemTime time)
        {

            return (((float)time.hours + (float)time.minutes / 60) / 12) + (time.meridiem == Meridiem.PM ? 0.5f : 0);

        }

        public static int ConvertTo12to1(int value)
        {

            if (value == 0)
                return 12;
            if (value == 12)
                return 0;

            return value;

        }

        public static void DayPercentToMeridiemTime(float dayPercent, ref MeridiemTime time)
        {



            time.minutes = Mathf.RoundToInt(dayPercent * 1440);
            time.hours = (time.minutes - time.minutes % 60) / 60;

            time.minutes -= time.hours * 60;

            if (time.hours > 11)
            {
                time.meridiem = Meridiem.PM;
                time.hours -= 12;
            }
            else
                time.meridiem = Meridiem.AM;

            if (time.hours == 0)
                time.hours = 12;

        }

    }


#if UNITY_EDITOR

    [CustomEditor(typeof(CozyWeather))]
    [CanEditMultipleObjects]
    public class E_CozyWeather : Editor
    {

        public List<Type> mods;

        public int windowNum;
        protected static bool climate;
        protected static bool satWindow;
        protected static bool windWindow;
        protected static bool thunderWindow;
        protected static bool matProfileWindow;
        protected static bool matOptionsWindow;
        protected static bool modules;
        protected static bool options;

        public bool tooltips;

        Color proCol = (Color)new Color32(50, 50, 50, 255);
        Color unityCol = (Color)new Color32(194, 194, 194, 255);
        CozyWeather t;

        Editor atmosEditor;

        void OnEnable()
        {

            serializedObject.Update();

            serializedObject.FindProperty("icon1").objectReferenceValue = Resources.Load<Texture>("Atmosphere");
            serializedObject.FindProperty("icon2").objectReferenceValue = Resources.Load<Texture>("CozyCalendar");
            serializedObject.FindProperty("icon3").objectReferenceValue = Resources.Load<Texture>("Weather Profile-01");
            serializedObject.FindProperty("icon4").objectReferenceValue = Resources.Load<Texture>("MoreOptions");
            t = (CozyWeather)target;

            serializedObject.ApplyModifiedProperties();

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();


            tooltips = EditorPrefs.GetBool("CZY_Tooltips", true);

            List<GUIContent> icons = new List<GUIContent>();
            Rect position = EditorGUILayout.GetControlRect(GUILayout.Height(0));

            icons.Add(new GUIContent("    Atmosphere", (Texture)serializedObject.FindProperty("icon1").objectReferenceValue, "Manage skydome, fog, and lighting settings."));
            icons.Add(new GUIContent("    Time", (Texture)serializedObject.FindProperty("icon2").objectReferenceValue, "Setup time settings, calendars, and manage current settings."));
            icons.Add(new GUIContent("    Ecosystem", (Texture)serializedObject.FindProperty("icon3").objectReferenceValue, "Manage weather, climate, and year settings."));
            icons.Add(new GUIContent("    Settings \n   & Modules", (Texture)serializedObject.FindProperty("icon4").objectReferenceValue, "Adjust the functions of COZY to get the most out of your system."));

            if (tooltips)
            {

                EditorGUILayout.HelpBox("Welcome to the COZY system! This is your one-stop-shop to managing all the weather parameters for this system. " +
                "COZY organizes parameters into various components called modules. You can add or remove modules in the settings tab. Check out the various modules on the system to edit the parameters", MessageType.Info, true);
                if (GUILayout.Button("Disable Tooltips"))
                    EditorPrefs.SetBool("CZY_Tooltips", !EditorPrefs.GetBool("CZY_Tooltips", true));
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("Atmosphere tab controls skydome, fog, and lighting settings.", MessageType.Info);
                EditorGUILayout.HelpBox("Setup time settings, calendars, and manage current settings.", MessageType.Info);
                EditorGUILayout.HelpBox("Manage weather profiles, forecast and ecosystem.", MessageType.Info);
                EditorGUILayout.HelpBox("Control options and add and remove modules.", MessageType.Info);
                EditorGUILayout.EndHorizontal();

            }

            foreach (CozyModule module in t.modules)
            {
                if (module != null)
                {
                    icons.Add((CreateEditor(module) as E_CozyModule).GetGUIContent());
                }
            }

            GUIStyle iconStyle = new GUIStyle(GUI.skin.GetStyle("Button"));
            iconStyle.fixedHeight = 40;
            iconStyle.fixedWidth = (position.width / 4) - 5;
            iconStyle.margin = new RectOffset(5, 5, 5, 5);
            iconStyle.fontStyle = FontStyle.Bold;

            windowNum = serializedObject.FindProperty("window").intValue;
            int j = GUILayout.SelectionGrid(windowNum, icons.ToArray(), 4, iconStyle);

            if (j != windowNum)
            {

                if (j == 3)
                    mods = EditorUtilities.ResetModuleList();

                windowNum = j;

            }

            serializedObject.FindProperty("window").intValue = windowNum;

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            switch (icons[windowNum].text)
            {
                case ("    Atmosphere"):
                    Atmos();
                    break;
                case ("    Time"):
                    Time();
                    break;
                case ("    Ecosystem"):
                    Weather();
                    break;
                case ("    Settings \n   & Modules"):
                    Settings();
                    break;
                default:
                    CustomModule(t.modules[windowNum - 4]);
                    break;

            }


            serializedObject.ApplyModifiedProperties();


        }

        #region Atmosphere


        public void Atmos()
        {

            GUIStyle labelStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            labelStyle.fontStyle = FontStyle.Bold;

            if (t.overrideAtmosphere)
            {
                EditorGUILayout.HelpBox($"Atmosphere control is currently being overriden by {t.overrideAtmosphere.GetType().Name}", MessageType.Warning);
                DrawNativeSettingsTab();
                return;
            }

            serializedObject.FindProperty("atmosSettingsWindow").boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(serializedObject.FindProperty("atmosSettingsWindow").boolValue,
                new GUIContent("    Selection Settings"), EditorUtilities.FoldoutStyle());


            if (serializedObject.FindProperty("atmosSettingsWindow").boolValue)
            {
                if (tooltips)
                    EditorGUILayout.HelpBox("How should this weather system manage atmosphere settings? Native sets all settings locally to this system, Profile sets global settings on the atmosphere profile.", MessageType.Info);


                if (serializedObject.FindProperty("atmosphereControl").enumValueIndex == (int)CozyWeather.AtmosphereSelection.profile)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("atmosphereControl"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("atmosphereProfile"));
                    if (serializedObject.hasModifiedProperties)
                        atmosEditor = CreateEditor(serializedObject.FindProperty("atmosphereProfile").objectReferenceValue);
                }
                else
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("atmosphereControl"));
                }
                EditorGUILayout.Space();

                if (tooltips)
                    EditorGUILayout.HelpBox("Set the shader model used for the sky, clouds, and fog here.", MessageType.Info);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("skyStyle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudStyle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fogStyle"));
                serializedObject.ApplyModifiedProperties();

                if (EditorGUI.EndChangeCheck())
                    t.ResetQuality();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUI.indentLevel = 0;

            if (serializedObject.FindProperty("atmosphereControl").enumValueIndex == (int)CozyWeather.AtmosphereSelection.native)
            {

                DrawNativeAtmosphere();

            }
            else if (serializedObject.FindProperty("atmosphereProfile").objectReferenceValue)
                if (atmosEditor == null)
                {
                    atmosEditor = CreateEditor(serializedObject.FindProperty("atmosphereProfile").objectReferenceValue);
                    (atmosEditor as E_AtmosphereProfile).OnInspectorGUIInline(t);
                }
                else
                    (atmosEditor as E_AtmosphereProfile).OnInspectorGUIInline(t);
            else
            {
                EditorGUILayout.HelpBox("Assign an atmosphere profile!", MessageType.Error);

            }

        }

        public void DrawNativeAtmosphere()
        {

            serializedObject.FindProperty("win1").boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(serializedObject.FindProperty("win1").boolValue,
              new GUIContent("    Atmosphere & Lighting", "Skydome, fog, and lighting settings."), EditorUtilities.FoldoutStyle());

            if (serializedObject.FindProperty("win1").boolValue)
            {

                DrawAtmosphereTab();

            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.FindProperty("win2").boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(serializedObject.FindProperty("win2").boolValue,
                            new GUIContent("    Clouds", "Cloud color, generation, and variation settings."), EditorUtilities.FoldoutStyle());

            if (serializedObject.FindProperty("win2").boolValue)
            {

                DrawCloudsTab();

            }

            EditorGUILayout.EndFoldoutHeaderGroup();


            serializedObject.FindProperty("win3").boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(serializedObject.FindProperty("win3").boolValue,
                            new GUIContent("    Celestials & VFX", "Sun, moon, and light FX settings."), EditorUtilities.FoldoutStyle());

            if (serializedObject.FindProperty("win3").boolValue)
            {

                DrawCelestialsTab();

            }

            EditorGUILayout.EndFoldoutHeaderGroup();

        }

        void DrawAtmosphereTab()
        {

            GUIStyle labelStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            labelStyle.fontStyle = FontStyle.Bold;

            AtmosphereProfile atmos = t.atmosphereProfile;

            bool advancedSky = t.skyStyle == CozyWeather.SkyStyle.desktop;


            EditorGUILayout.LabelField(" Skydome Settings", labelStyle);
            EditorGUI.indentLevel++;

            if (tooltips)
                EditorGUILayout.HelpBox("In native mode, all values are set to static references that must be interpolated manually. For automatic interpolation based on the time of day, switch to profile mode.", MessageType.Info);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("skyZenithColor"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("skyHorizonColor"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gradientExponent"), false);

            if (advancedSky)
            {

                EditorGUILayout.Space(5);
                float min = serializedObject.FindProperty("atmosphereVariationMin").floatValue;
                float max = serializedObject.FindProperty("atmosphereVariationMax").floatValue;

                Rect position = EditorGUILayout.GetControlRect();
                float startPos = position.width / 2.5f;
                var titleRect = new Rect(position.x, position.y, 70, position.height);
                EditorGUI.PrefixLabel(titleRect, new GUIContent("Atmosphere Variation"));
                var label1Rect = new Rect();
                var label2Rect = new Rect();
                var sliderRect = new Rect();

                if (position.width > 359)
                {
                    label1Rect = new Rect(startPos, position.y, 64, position.height);
                    label2Rect = new Rect(position.width - 71, position.y, 64, position.height);
                    sliderRect = new Rect(startPos + 56, position.y, (position.width - startPos) - 135, position.height);
                    EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, 0, 1);
                }
                else
                {

                    label1Rect = new Rect(position.width - 110, position.y, 50, position.height);
                    label2Rect = new Rect(position.width - 72, position.y, 50, position.height);

                }

                min = EditorGUI.FloatField(label1Rect, (Mathf.Round(min * 100) / 100));
                max = EditorGUI.FloatField(label2Rect, (Mathf.Round(max * 100) / 100));

                if (min > max)
                    min = max;

                serializedObject.FindProperty("atmosphereVariationMin").floatValue = min;
                serializedObject.FindProperty("atmosphereVariationMax").floatValue = max;

                EditorGUILayout.PropertyField(serializedObject.FindProperty("atmosphereBias"), false);
            }

            EditorGUILayout.Space(5);
            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField(" Fog Settings", labelStyle);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogColor1"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogColor2"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogColor3"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogColor4"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogColor5"), false);
            EditorGUILayout.Space(5);

            float fogStart1 = serializedObject.FindProperty("fogStart1").floatValue;
            float fogStart2 = serializedObject.FindProperty("fogStart2").floatValue;
            float fogStart3 = serializedObject.FindProperty("fogStart3").floatValue;
            float fogStart4 = serializedObject.FindProperty("fogStart4").floatValue;

            fogStart1 = Mathf.Clamp(EditorGUILayout.Slider("Fog Start 2", fogStart1, 0, 50), 0, fogStart2 - 0.1f);
            fogStart2 = Mathf.Clamp(EditorGUILayout.Slider("Fog Start 3", fogStart2, 0, 50), fogStart1 + 0.1f, fogStart3 - 0.1f);
            fogStart3 = Mathf.Clamp(EditorGUILayout.Slider("Fog Start 4", fogStart3, 0, 50), fogStart2 + 0.1f, fogStart4 - 0.1f);
            fogStart4 = Mathf.Clamp(EditorGUILayout.Slider("Fog Start 5", fogStart4, 0, 50), fogStart3 + 0.1f, 50);

            serializedObject.FindProperty("fogStart1").floatValue = fogStart1;
            serializedObject.FindProperty("fogStart2").floatValue = fogStart2;
            serializedObject.FindProperty("fogStart3").floatValue = fogStart3;
            serializedObject.FindProperty("fogStart4").floatValue = fogStart4;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogHeight"), false);

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogDensityMultiplier"), false);


            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogFlareColor"), new GUIContent("Light Flare Color",
                "Sets the color of the fog for a false \"light flare\" around the main sun directional light."), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogLightFlareIntensity"), new GUIContent("Light Flare Intensity",
                "Modulates the brightness of the light flare."), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogLightFlareFalloff"), new GUIContent("Light Flare Falloff",
                "Sets the falloff speed for the light flare."), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogLightFlareSquish"), new GUIContent("Light Flare Squish",
                "Sets the height divisor for the fog flare. High values sit the flare closer to the horizon, small values extend the flare into the sky."), false);

            EditorGUILayout.Space(5);
            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField(" Lighting Settings", labelStyle);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("sunlightColor"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("moonlightColor"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientLightHorizonColor"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientLightZenithColor"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientLightMultiplier"), false);
            EditorGUI.indentLevel--;


        }

        void DrawNativeSettingsTab()
        {

            serializedObject.FindProperty("atmosSettingsWindow").boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(serializedObject.FindProperty("atmosSettingsWindow").boolValue,
                new GUIContent("    Global Settings"), EditorUtilities.FoldoutStyle());

            if (!serializedObject.FindProperty("atmosSettingsWindow").boolValue)
                return;

            Material cloudShader = t.cloudMesh.sharedMaterial;

            if (tooltips)
                EditorGUILayout.HelpBox("In native mode, all values are set to static references that must be interpolated manually. For automatic interpolation based on the time of day, switch to profile mode.", MessageType.Info);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            labelStyle.fontStyle = FontStyle.Bold;

            EditorGUILayout.LabelField(" Sun Settings", labelStyle);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sunDirection"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sunPitch"), false);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(" Atmosphere Settings", labelStyle);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientLightMultiplier"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useRainbow"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rainbowPosition"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rainbowWidth"), false);
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(" Cloud Settings", labelStyle);
            EditorGUI.indentLevel++;
            if (cloudShader.HasProperty("_WindSpeed"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudWindSpeed"), new GUIContent("Wind Speed", "The speed at which the cloud generation will progress."), false);
            if (cloudShader.HasProperty("_ClippingThreshold"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("clippingThreshold"), new GUIContent("Clipping Threshold", "The alpha that the clouds will clip to full alpha at. Default is 0.5"), false);
            if (cloudShader.HasProperty("_MainCloudScale"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudMainScale"), new GUIContent("Main Scale", "The scale of the main perlin noise for the cumulus cloud type."), false);
            if (cloudShader.HasProperty("_DetailScale"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudDetailScale"), new GUIContent("Detail Scale", "The scale of the secondary voronoi noise functions for the cumulus cloud type."), false);
            if (cloudShader.HasProperty("_DetailAmount"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudDetailAmount"), new GUIContent("Detail Amount", "The multiplier for the secondary voronoi noise functions for the cumulus cloud type. Lower values give more cohesive cloud types."), false);
            EditorGUILayout.Space(10);
            if (cloudShader.HasProperty("_AltocumulusScale"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("acScale"), new GUIContent("Altocumulus Scale"), false);
            if (cloudShader.HasProperty("_CirrostratusMoveSpeed"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cirroMoveSpeed"), new GUIContent("Cirrostratus Movement Speed"), false);
            if (cloudShader.HasProperty("_CirrusMoveSpeed"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cirrusMoveSpeed"), new GUIContent("Cirrus Movement Speed"), false);
            if (cloudShader.HasProperty("_ChemtrailsMoveSpeed"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chemtrailsMoveSpeed"), new GUIContent("Chemtrails Movement Speed"), false);


            if (cloudShader.HasProperty("_CloudTextureColor"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudTextureColor"), new GUIContent("Texture Color Multiplier"), false);
            if (cloudShader.HasProperty("_CloudTexture"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudTexture"), new GUIContent("Cloud Texture"), false);
            if (cloudShader.HasProperty("_TexturePanDirection"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("texturePanDirection"), new GUIContent("Cloud Texture Pan Direction"), false);
            if (cloudShader.HasProperty("_TextureAmount"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("textureAmount"), new GUIContent("Texture Amount"), false);
            if (cloudShader.HasProperty("_CloudCohesion"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudCohesion"), new GUIContent("Cloud Cohesion"), false);
            if (cloudShader.HasProperty("_Spherize"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spherize"), new GUIContent("Sphere Distortion"), false);
            if (cloudShader.HasProperty("_ShadowingDistance"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("shadowDistance"), new GUIContent("Shadow Distance"), false);
            if (cloudShader.HasProperty("_CloudThickness"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudThickness"), new GUIContent("Cloud Thickness"), false);

            EditorGUI.indentLevel--;

        }

        void DrawCloudsTab()
        {

            Material cloudShader = t.cloudMesh.sharedMaterial;

            if (tooltips)
                EditorGUILayout.HelpBox("In native mode, all values are set to static references that must be interpolated manually. For automatic interpolation based on the time of day, switch to profile mode.", MessageType.Info);


            GUIStyle labelStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            labelStyle.fontStyle = FontStyle.Bold;

            EditorGUILayout.LabelField(" Color Settings", labelStyle);
            EditorGUI.indentLevel++;


            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudColor"), new GUIContent("Cloud Color", "The main color of the unlit clouds."), false);
            if (cloudShader.HasProperty("_AltoCloudColor"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("highAltitudeCloudColor"), new GUIContent("High Altitude Color", "The main color multiplier of the high altitude clouds. The cloud types affected are the cirrostratus and the altocumulus types."), false);
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudHighlightColor"), new GUIContent("Sun Highlight Color", "The color multiplier for the clouds in a \"dot\" around the sun."), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSunHighlightFalloff"), new GUIContent("Sun Highlight Falloff", "The falloff for the \"dot\" around the sun."), false);
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudMoonColor"), new GUIContent("Moon Highlight Color", "The color multiplier for the clouds in a \"dot\" around the moon."), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudMoonHighlightFalloff"), new GUIContent("Moon Highlight Falloff", "The falloff for the \"dot\" around the moon."), false);


            EditorGUI.indentLevel--;
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(" Generation Settings", labelStyle);
            EditorGUI.indentLevel++;
            if (cloudShader.HasProperty("_WindSpeed"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudWindSpeed"), new GUIContent("Wind Speed", "The speed at which the cloud generation will progress."), false);
            if (cloudShader.HasProperty("_ClippingThreshold"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("clippingThreshold"), new GUIContent("Clipping Threshold", "The alpha that the clouds will clip to full alpha at. Default is 0.5"), false);
            if (cloudShader.HasProperty("_MainCloudScale"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudMainScale"), new GUIContent("Main Scale", "The scale of the main perlin noise for the cumulus cloud type."), false);
            if (cloudShader.HasProperty("_DetailScale"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudDetailScale"), new GUIContent("Detail Scale", "The scale of the secondary voronoi noise functions for the cumulus cloud type."), false);
            if (cloudShader.HasProperty("_DetailAmount"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudDetailAmount"), new GUIContent("Detail Amount", "The multiplier for the secondary voronoi noise functions for the cumulus cloud type. Lower values give more cohesive cloud types."), false);
            EditorGUILayout.Space(10);
            if (cloudShader.HasProperty("_AltocumulusScale"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("acScale"), new GUIContent("Altocumulus Scale"), false);
            if (cloudShader.HasProperty("_CirrostratusMoveSpeed"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cirroMoveSpeed"), new GUIContent("Cirrostratus Movement Speed"), false);
            if (cloudShader.HasProperty("_CirrusMoveSpeed"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cirrusMoveSpeed"), new GUIContent("Cirrus Movement Speed"), false);
            if (cloudShader.HasProperty("_ChemtrailsMoveSpeed"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chemtrailsMoveSpeed"), new GUIContent("Chemtrails Movement Speed"), false);


            if (cloudShader.HasProperty("_CloudTextureColor"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudTextureColor"), new GUIContent("Texture Color Multiplier"), false);
            if (cloudShader.HasProperty("_CloudTexture"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudTexture"), new GUIContent("Cloud Texture"), false);
            if (cloudShader.HasProperty("_TexturePanDirection"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("texturePanDirection"), new GUIContent("Cloud Texture Pan Direction"), false);
            if (cloudShader.HasProperty("_TextureAmount"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("textureAmount"), new GUIContent("Texture Amount"), false);
            if (cloudShader.HasProperty("_CloudCohesion"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudCohesion"), new GUIContent("Cloud Cohesion"), false);
            if (cloudShader.HasProperty("_Spherize"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spherize"), new GUIContent("Sphere Distortion"), false);
            if (cloudShader.HasProperty("_ShadowingDistance"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("shadowDistance"), new GUIContent("Shadow Distance"), false);
            if (cloudShader.HasProperty("_CloudThickness"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudThickness"), new GUIContent("Cloud Thickness"), false);

            EditorGUI.indentLevel--;


        }

        void DrawCelestialsTab()
        {

            if (tooltips)
                EditorGUILayout.HelpBox("In native mode, all values are set to static references that must be interpolated manually. For automatic interpolation based on the time of day, switch to profile mode.", MessageType.Info);


            bool advancedSky = t.skyStyle == CozyWeather.SkyStyle.desktop;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            labelStyle.fontStyle = FontStyle.Bold;

            EditorGUILayout.LabelField(" Sun Settings", labelStyle);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sunColor"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sunSize"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sunDirection"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sunPitch"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sunFalloff"), new GUIContent("Sun Halo Falloff"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sunFlareColor"), new GUIContent("Sun Halo Color"), false);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(16);

            if (advancedSky)
                if (t.GetModule<CozySatelliteManager>())
                {
                    EditorGUILayout.LabelField(" Moon Settings", labelStyle);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("moonFalloff"), new GUIContent("Moon Halo Falloff"), false);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("moonFlareColor"), new GUIContent("Moon Halo Color"), false);
                    EditorGUI.indentLevel--;
                }


            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField(" VFX", labelStyle);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("starColor"), false);

            if (advancedSky)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("galaxyIntensity"), false);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("galaxy1Color"), false);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("galaxy2Color"), false);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("galaxy3Color"), false);
                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lightScatteringColor"), false);
                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useRainbow"), false);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rainbowPosition"), false);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rainbowWidth"), false);
            }
            EditorGUI.indentLevel--;


        }

        #endregion

        #region Time
        public void Time()
        {


            if (t.overrideTime)
            {
                EditorGUILayout.HelpBox($"Time control is currently being overriden by {t.overrideTime.GetType().Name}", MessageType.Warning);
                return;
            }
            bool timeControl = serializedObject.FindProperty("timeControl").enumValueIndex == (int)CozyWeather.TimeControl.profile;
            SerializedProperty calendar = serializedObject.FindProperty("calendar");
            PerennialProfile perennial = serializedObject.FindProperty("perennialProfile").objectReferenceValue as PerennialProfile;
            E_PerennialProfile timeEditor = CreateEditor(serializedObject.FindProperty("perennialProfile").objectReferenceValue) as E_PerennialProfile;

            serializedObject.FindProperty("atmosSettingsWindow").boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(serializedObject.FindProperty("atmosSettingsWindow").boolValue,
                new GUIContent("    Selection Settings"), EditorUtilities.FoldoutStyle());

            if (serializedObject.FindProperty("atmosSettingsWindow").boolValue)
            {

                if (tooltips)
                    EditorGUILayout.HelpBox("How should this weather system manage time settings? Native sets the time locally to this system, Profile mode sets global settings on the perennial profile.", MessageType.Info);

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("timeControl"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("perennialProfile"));

            }

            EditorGUILayout.EndFoldoutHeaderGroup();


            serializedObject.FindProperty("timeCurrentWindow").boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(serializedObject.FindProperty("timeCurrentWindow").boolValue,
                new GUIContent("    Current Settings"), EditorUtilities.FoldoutStyle());

            if (serializedObject.FindProperty("timeCurrentWindow").boolValue)
            {
                EditorGUI.indentLevel++;
                if (tooltips)
                {
                    EditorGUILayout.HelpBox("COZY uses a tick system to tell time instead of a minutes/hours system. By default, a day is set to 360 ticks long (one for every degree of rotation that the sun will make). Converting the ticks to a time of day is easy! Midnight is 0 ticks, 6:00 AM is 90, noon is 180, 6:00 PM is 270, and then the cycle restarts.", MessageType.Info);
                    EditorGUILayout.HelpBox("You can also change the length of the year! The default profile uses 48 days in a year to create a shorter year to improve contrast.", MessageType.Info);
                    EditorGUILayout.HelpBox("Don't like the proportions of the current time system? Not to worry! Check out the 2400 tick perennial profile for a more realistic year!", MessageType.Info);
                }

                if (timeControl)
                {
                    timeEditor.OnRuntimeMeasureGUI();
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    calendar.FindPropertyRelative("currentTicks").floatValue = EditorGUILayout.Slider("Current Ticks", calendar.FindPropertyRelative("currentTicks").floatValue, 0, perennial.ticksPerDay);
                    if (EditorGUI.EndChangeCheck())
                    {

                        MeridiemTime i = new MeridiemTime();
                        MeridiemTime.DayPercentToMeridiemTime(t.GetCurrentDayPercentage(), ref i);
                        calendar.FindPropertyRelative("meridiemTime").FindPropertyRelative("hours").intValue = i.hours;
                        calendar.FindPropertyRelative("meridiemTime").FindPropertyRelative("minutes").intValue = i.minutes;
                        calendar.FindPropertyRelative("meridiemTime").FindPropertyRelative("meridiem").intValue = (int)i.meridiem;

                    }

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(calendar.FindPropertyRelative("meridiemTime"));
                    if (EditorGUI.EndChangeCheck())
                    {

                        calendar.FindPropertyRelative("currentTicks").floatValue = t.perennialProfile.ticksPerDay * MeridiemTime.MeridiemTimeToDeyPercent(
                        calendar.FindPropertyRelative("meridiemTime").FindPropertyRelative("hours").intValue,
                        calendar.FindPropertyRelative("meridiemTime").FindPropertyRelative("minutes").intValue,
                        (MeridiemTime.Meridiem)calendar.FindPropertyRelative("meridiemTime").FindPropertyRelative("meridiem").intValue);


                    }
                    calendar.FindPropertyRelative("currentDay").intValue = EditorGUILayout.IntSlider("Current Day", calendar.FindPropertyRelative("currentDay").intValue, 0, perennial.daysPerYear);
                    calendar.FindPropertyRelative("currentYear").intValue = EditorGUILayout.IntField("Current Year", calendar.FindPropertyRelative("currentYear").intValue);

                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            bool lengthWindow = serializedObject.FindProperty("tickLengthWindow").boolValue;
            bool movementWindow = serializedObject.FindProperty("tickMovementWindow").boolValue;
            bool curveWindow = serializedObject.FindProperty("curveWindow").boolValue;

            timeEditor.OnStaticMeasureGUI(EditorUtilities.FoldoutStyle(), ref lengthWindow, ref movementWindow, ref curveWindow);


            serializedObject.FindProperty("tickLengthWindow").boolValue = lengthWindow;
            serializedObject.FindProperty("tickMovementWindow").boolValue = movementWindow;
            serializedObject.FindProperty("curveWindow").boolValue = curveWindow;


        }

        #endregion

        #region Weather

        public void Weather()
        {

            if (t.overrideWeather)
            {
                EditorGUILayout.HelpBox($"Weather property control is currently being overriden by {t.overrideWeather.GetType().Name}", MessageType.Warning);
                return;
            }
            serializedObject.FindProperty("atmosSettingsWindow").boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(serializedObject.FindProperty("atmosSettingsWindow").boolValue,
                new GUIContent("    Selection Settings"), EditorUtilities.FoldoutStyle());

            EditorGUILayout.EndFoldoutHeaderGroup();

            bool useSingle = (CozyEcosystem.EcosystemStyle)serializedObject.FindProperty("weatherSelectionMode").enumValueIndex == CozyEcosystem.EcosystemStyle.manual;
            serializedObject.ApplyModifiedProperties();

            if (serializedObject.FindProperty("atmosSettingsWindow").boolValue)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("weatherSelectionMode"));

                EditorGUILayout.PropertyField(serializedObject.FindProperty("currentWeather"), new GUIContent("Global Weather"));
                if (t.GetCurrentWeatherProfile())
                    EditorGUILayout.LabelField(new GUIContent($"Current Weather is {t.GetCurrentWeatherProfile().name}"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("weatherTransitionTime"));


                if (serializedObject.hasModifiedProperties)
                {

                    serializedObject.ApplyModifiedProperties();
                    t.CalculateFilterColors();

                }

                EditorGUI.indentLevel--;

            }
            if (useSingle)
            {

                serializedObject.FindProperty("currentWeatherWindow").boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(serializedObject.FindProperty("currentWeatherWindow").boolValue,
                    new GUIContent("    Profile Settings"), EditorUtilities.FoldoutStyle());

                EditorGUILayout.EndFoldoutHeaderGroup();

                if (serializedObject.FindProperty("currentWeatherWindow").boolValue)
                {

                    EditorGUI.indentLevel++;
                    (CreateEditor(serializedObject.FindProperty("currentWeather").objectReferenceValue) as E_WeatherProfile).DisplayInCozyWindow(t);
                    EditorGUI.indentLevel--;

                }

            }
            else
            {

                serializedObject.FindProperty("forecastWindow").boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(serializedObject.FindProperty("forecastWindow").boolValue,
                    new GUIContent("    Forecasting Behaviors"), EditorUtilities.FoldoutStyle());

                EditorGUILayout.EndFoldoutHeaderGroup();

                if (serializedObject.FindProperty("forecastWindow").boolValue)
                {

                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("forecastProfile"));
                    EditorGUILayout.Space();
                    EditorGUI.indentLevel++;
                    if (serializedObject.FindProperty("forecastProfile").objectReferenceValue)
                        CreateEditor(serializedObject.FindProperty("forecastProfile").objectReferenceValue).OnInspectorGUI();
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("weatherTransitionTime"));
                    EditorGUI.indentLevel--;

                }

                climate = EditorGUILayout.BeginFoldoutHeaderGroup(climate, new GUIContent("    Climate Settings"), EditorUtilities.FoldoutStyle());

                EditorGUILayout.EndFoldoutHeaderGroup();

                if (climate)
                {

                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("climateProfile"));
                    EditorGUILayout.Space();
                    EditorGUI.indentLevel++;
                    CreateEditor(serializedObject.FindProperty("climateProfile").objectReferenceValue).OnInspectorGUI();
                    EditorGUI.indentLevel--;
                    EditorGUI.indentLevel--;

                }



            }
        }

        #endregion

        public void Settings()
        {

            if (tooltips)
                EditorGUILayout.HelpBox("Add modules using this foldout! Use the reset module list buttone to search for any scripts that derive from CozyModule and add them to the dropdown.", MessageType.Info, true);

            modules = EditorGUILayout.BeginFoldoutHeaderGroup(modules, "    Modules", EditorUtilities.FoldoutStyle());
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (modules)
            {

                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();

                List<string> modNames = new List<string>() { "Select..." };

                if (mods == null)
                    mods = EditorUtilities.ResetModuleList();

                if (mods.Contains(typeof(CozyModule)))
                    mods.Remove(typeof(CozyModule));

                if (mods.Contains(typeof(ExampleModule)))
                    mods.Remove(typeof(ExampleModule));

                foreach (CozyModule a in t.modules)
                    if (mods.Contains(a.GetType()))
                        mods.Remove(a.GetType());

                foreach (Type a in mods)
                {

                    modNames.Add(a.Name);
                }


                int moduleNumber = 0;
                moduleNumber = EditorGUILayout.Popup(new GUIContent("Add New Module"), 0, modNames.ToArray());

                if (GUILayout.Button("Reset Module List"))
                {
                    EditorUtilities.ResetModuleList();

                }

                EditorGUILayout.EndHorizontal();


                if (moduleNumber != 0)
                    t.IntitializeModule(mods[moduleNumber - 1]);

                EditorGUI.indentLevel++;

                CozyModule j = null;

                foreach (CozyModule i in t.modules)
                {
                    if (i == null)
                        continue;

                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.PrefixLabel((CreateEditor(i) as E_CozyModule).GetGUIContent());
                    if (GUILayout.Button("Remove"))
                    {
                        j = i;
                        mods = EditorUtilities.ResetModuleList();
                    }

                    EditorGUILayout.EndHorizontal();

                }

                t.DeintitializeModule(j);

                EditorGUI.indentLevel--;

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            options = EditorGUILayout.BeginFoldoutHeaderGroup(options, "    Options", EditorUtilities.FoldoutStyle());
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (options)
            {

                if (tooltips)
                {
                    EditorGUILayout.HelpBox("COZY automatically aligns itself to a camera. By default the main camera is used, however you can set it up to use a different camera or to not lock to a camera by using the enumerator below.", MessageType.Info, true);
                    EditorGUILayout.Space();
                }

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lockToCamera"));

                if (t.lockToCamera == CozyWeather.LockToCameraStyle.useCustomCamera)
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("cozyCamera"));

                EditorGUILayout.Space();

                if (tooltips)
                {
                    EditorGUILayout.HelpBox("Should the properties and colors of the system be set via a procedural profile that applies to all systems (default) or via a native direct setting that only applies to this system.", MessageType.Info, true);
                    EditorGUILayout.Space();
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("atmosphereControl"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("timeControl"));
                EditorGUILayout.Space();

                if (tooltips)
                {
                    EditorGUILayout.HelpBox("Change the shader used for the sky, clouds and fog. You can also disable all the features individually here!", MessageType.Info, true);
                    EditorGUILayout.Space();
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("skyStyle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudStyle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fogStyle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("usePhysicalSunHeight"));
                EditorGUI.indentLevel--;
                serializedObject.ApplyModifiedProperties();

                if (EditorGUI.EndChangeCheck())
                    t.ResetQuality();

            }

            if (GUILayout.Button("Toggle Tooltips"))
                EditorPrefs.SetBool("CZY_Tooltips", !EditorPrefs.GetBool("CZY_Tooltips", true));



        }

        public void CustomModule(CozyModule module)
        {

            if (t.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene())
                if (module != null && module.isActiveAndEnabled)
                {
                    EditorGUI.indentLevel++;
                    (CreateEditor(module) as E_CozyModule).DisplayInCozyWindow();
                    EditorGUI.indentLevel--;

                }
                else
                    EditorGUILayout.HelpBox("Something went wrong! Try removing and readding the module!", MessageType.Error);
            else
                EditorGUILayout.HelpBox("Modules may only be edited in the scene!", MessageType.Info);

        }
    }


#endif

}
