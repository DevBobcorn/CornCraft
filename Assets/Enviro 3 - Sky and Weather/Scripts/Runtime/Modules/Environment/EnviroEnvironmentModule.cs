using System.Collections;
using System.Collections.Generic; 
using UnityEngine;
using System;

namespace Enviro
{

    [Serializable]
    public class EnviroEnvironment
    {
        //Seasons
        public enum Seasons
        {
            Spring,
            Summer,
            Autumn,
            Winter
        } 
        public Seasons season;
        public bool changeSeason = true;

        [Tooltip("Start Day of Year for Spring")]
        [Range(0, 366)]
        public int springStart = 60;
        [Tooltip("End Day of Year for Spring")]
        [Range(0, 366)]
        public int springEnd = 92;
        [Tooltip("Start Day of Year for Summer")]
        [Range(0, 366)]
        public int summerStart = 93;
        [Tooltip("End Day of Year for Summer")]
        [Range(0, 366)]
        public int summerEnd = 185;
        [Tooltip("Start Day of Year for Autumn")]
        [Range(0, 366)]
        public int autumnStart = 186;
        [Tooltip("End Day of Year for Autumn")]
        [Range(0, 366)]
        public int autumnEnd = 276;
        [Tooltip("Start Day of Year for Winter")]
        [Range(0, 366)]
        public int winterStart = 277;
        [Tooltip("End Day of Year for Winter")]
        [Range(0, 366)]
        public int winterEnd = 59;

        //Temperature
        [Tooltip("Base Temperature in Spring")]
        public AnimationCurve springBaseTemperature = new AnimationCurve();
        [Tooltip("Base Temperature in Summer")]
        public AnimationCurve summerBaseTemperature = new AnimationCurve();
        [Tooltip("Base Temperature in Autumn")]
        public AnimationCurve autumnBaseTemperature = new AnimationCurve();
        [Tooltip("Base Temperature in Winter")] 
        public AnimationCurve winterBaseTemperature = new AnimationCurve();
        [Tooltip("Current temperature.")]
        [Range(-50f, 50f)]
        public float temperature = 0f;
        [Tooltip("Temperature mod used for different weather types.")]
        [Range(-50f, 50f)]
        public float temperatureWeatherMod = 0f;
        [Tooltip("Custom temperature mod for gameplay use.")]
        [Range(-50f, 50f)]
        public float temperatureCustomMod = 0f;

        [Tooltip("Temperature changing speed.")]
        public float temperatureChangingSpeed = 1f;

        //Weather State
        [Tooltip("Current wetness for third party shader or gameplay.")]
        [Range(0f, 1f)]
        public float wetness = 0f;
        [Tooltip("Target wetness for third party shader or gameplay.")]
        [Range(0f, 1f)]
        public float wetnessTarget = 0f;
        [Tooltip("Current snow for third party shader or gameplay.")]
        [Range(0f, 1f)]
        public float snow = 0f;
        [Tooltip("Target snow for third party shader or gameplay.")]
        [Range(0f, 1f)]
        public float snowTarget = 0f;

         [Tooltip("Speed of wetness accumulation.")]
        public float wetnessAccumulationSpeed = 1f;
        [Tooltip("Speed of wetness dries.")]
        public float wetnessDrySpeed = 1f; 

        [Tooltip("Speed of snow buildup.")]
        public float snowAccumulationSpeed = 1f;
        [Tooltip("Speed of how fast snow melts.")]
        public float snowMeltSpeed = 1f;

        [Tooltip("Temperature when snow starts to melt.")]
        [Range(-20f, 20f)]
        public float snowMeltingTresholdTemperature = 1f;
 
        //Wind 
        [Range(-1f,1f)]
        public float windDirectionX, windDirectionY;
        [Range(0f,1f)]
        public float windSpeed = 0.1f;
        [Range(0f,1f)]
        public float windTurbulence = 0.1f;

    } 

    [Serializable]
    public class EnviroEnvironmentModule : EnviroModule
    {  
        public Enviro.EnviroEnvironment Settings;
        public EnviroEnvironmentModule preset;
        public bool showSeasonControls,showTemperatureControls,showWeatherStateControls,showWindControls;

        public override void Enable() 
        {
              if(EnviroManager.instance == null)
                 return;
            
            CreateWindZone ();

        }

        public override void Disable() 
        {
            if(EnviroManager.instance == null)
               return;

            if(EnviroManager.instance.Objects.windZone != null)
                DestroyImmediate(EnviroManager.instance.Objects.windZone.gameObject);

        }

        private void CreateWindZone ()
        {
            if(EnviroManager.instance.Objects.windZone == null)
            {
                GameObject wZ = new GameObject();
                wZ.name = "Wind Zone";
                wZ.transform.SetParent(EnviroManager.instance.transform);
                wZ.transform.localPosition = Vector3.zero;
                EnviroManager.instance.Objects.windZone = wZ.AddComponent<WindZone>();
            }
        }

        // Update Method
        public override void UpdateModule ()
        { 
             if(EnviroManager.instance == null)
               return;
               
            if(EnviroManager.instance.Time != null)
            {
                UpdateTemperature(EnviroManager.instance.Time.GetUniversalTimeOfDay() / 24f);
                UpdateSeason();
            }
            else
            {
                UpdateTemperature(1f);
            }

            UpdateWindZone();
            UpdateWeatherState();
        }

        //Changes season based on day settings.
        public void UpdateSeason()
        {
            if(Settings.changeSeason)
            {
                int currentDay = EnviroManager.instance.Time.Settings.date.DayOfYear;

                if (currentDay >= Settings.springStart && currentDay <= Settings.springEnd)
                {
                    ChangeSeason(EnviroEnvironment.Seasons.Spring);
                }
                else if (currentDay >= Settings.summerStart && currentDay <= Settings.summerEnd)
                {
                    ChangeSeason(EnviroEnvironment.Seasons.Summer);
                }
                else if (currentDay >= Settings.autumnStart && currentDay <= Settings.autumnEnd)
                {
                    ChangeSeason(EnviroEnvironment.Seasons.Autumn);
                }
                else if (currentDay >= Settings.winterStart || currentDay <= Settings.winterEnd)
                {
                    ChangeSeason(EnviroEnvironment.Seasons.Winter);
                }
            }
        }

        //Changes Season
        public void ChangeSeason(EnviroEnvironment.Seasons season)
        {
            if(Settings.season != season) 
            {
                EnviroManager.instance.NotifySeasonChanged(season);
                Settings.season = season;
            }

        }

        //Sets temperature based on time of day.
        public void UpdateTemperature (float timeOfDay)
        {
            float temperature = 0f;

            switch (Settings.season)
            {
                case EnviroEnvironment.Seasons.Spring:
                    temperature = Settings.springBaseTemperature.Evaluate(timeOfDay);
                    break;
                case EnviroEnvironment.Seasons.Summer:
                    temperature = Settings.summerBaseTemperature.Evaluate(timeOfDay);
                    break;
                case EnviroEnvironment.Seasons.Autumn:
                    temperature = Settings.autumnBaseTemperature.Evaluate(timeOfDay);
                    break;
                case EnviroEnvironment.Seasons.Winter:
                    temperature = Settings.winterBaseTemperature.Evaluate(timeOfDay);
                    break;
            }

            //Apply temperature mods
            temperature += Settings.temperatureWeatherMod;
            temperature += Settings.temperatureCustomMod;

            //Set temperature
            Settings.temperature = Mathf.Lerp(Settings.temperature, temperature, Time.deltaTime * Settings.temperatureChangingSpeed);
        }

        public void UpdateWeatherState()
        {
            // Wetness
            if (Settings.wetness < Settings.wetnessTarget)
            {
                // Raining
                Settings.wetness = Mathf.Lerp(Settings.wetness, Settings.wetnessTarget, Settings.wetnessAccumulationSpeed * Time.deltaTime);
            }
            else
            {   // Drying
                Settings.wetness = Mathf.Lerp(Settings.wetness, Settings.wetnessTarget, Settings.wetnessDrySpeed * Time.deltaTime);
            }

            if(Settings.wetness < 0.0001f)
               Settings.wetness = 0f;

            Settings.wetness = Mathf.Clamp(Settings.wetness, 0f, 1f);

            //Snow
            if (Settings.snow < Settings.snowTarget)
            {   
                //Snowing
                Settings.snow = Mathf.Lerp(Settings.snow, Settings.snowTarget, Settings.snowAccumulationSpeed * Time.deltaTime);
            }
            else if (Settings.temperature > Settings.snowMeltingTresholdTemperature)
            {
                //Melting
                Settings.snow = Mathf.Lerp(Settings.snow, Settings.snowTarget, Settings.snowMeltSpeed * Time.deltaTime);
            }

            if(Settings.snow < 0.0001f)
               Settings.snow = 0f;

            Settings.snow = Mathf.Clamp(Settings.snow, 0f, 1f);
        }

        private void UpdateWindZone()
        {
            if(EnviroManager.instance.Objects.windZone != null)
            {
                EnviroManager.instance.Objects.windZone.windMain = Settings.windSpeed;
                EnviroManager.instance.Objects.windZone.windTurbulence = Settings.windTurbulence;

                Vector3 windDirection = new Vector3(-Settings.windDirectionX,0f,-Settings.windDirectionY);
                EnviroManager.instance.Objects.windZone.transform.forward = windDirection;
               // EnviroManager.instance.Objects.windZone.transform.Rotate(new Vector3(Settings.windDirectionX,0f,Settings.windDirectionY),Space.World);
    
            }
        }

        //Save and Load
        public void LoadModuleValues ()
        {
            if(preset != null)
            {
                Settings = JsonUtility.FromJson<Enviro.EnviroEnvironment>(JsonUtility.ToJson(preset.Settings));
            }
            else
            {
                Debug.Log("Please assign a saved module to load from!");
            }
        }

        public void SaveModuleValues () 
        {
#if UNITY_EDITOR
        EnviroEnvironmentModule t =  ScriptableObject.CreateInstance<EnviroEnvironmentModule>();
        t.name = "Environment Preset";
        t.Settings = JsonUtility.FromJson<Enviro.EnviroEnvironment>(JsonUtility.ToJson(Settings));
 
        string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(EnviroHelper.assetPath + "/New " + t.name + ".asset");
        UnityEditor.AssetDatabase.CreateAsset(t, assetPathAndName);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif
        }
        public void SaveModuleValues (EnviroEnvironmentModule module)
        {
            module.Settings = JsonUtility.FromJson<Enviro.EnviroEnvironment>(JsonUtility.ToJson(Settings));

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(module);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }
    }
}