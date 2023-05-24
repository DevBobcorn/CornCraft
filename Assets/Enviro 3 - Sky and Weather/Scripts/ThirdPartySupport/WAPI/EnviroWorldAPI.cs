using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if WORLDAPI_PRESENT
using WAPI;
#endif

#if WORLDAPI_PRESENT

namespace Enviro
{ 
    [ExecuteInEditMode]
    [AddComponentMenu("Enviro 3/Integrations/WAPI Integration")]
    public class EnviroWorldAPI : MonoBehaviour, IWorldApiChangeHandler
    {
        public enum GetSet
        {
            None,
            GetFromWAPI,
            SendToWAPI
        }

        public enum Get
        {
            None,
            GetFromWAPI
        }
            
        public enum Set
        {
            None,
            SendToWAPI
        }

        // Controls
        public GetSet snowPower;
        public GetSet wetnessPower;
        public GetSet fogPower;
        public GetSet temperature;
        //public float fogPowerMult = 1000f;
        //public Set windDirection;
        //public Set windSpeed;
        public GetSet seasons;
        public GetSet time;
        public GetSet cloudCover;
        public GetSet location;

        private List<EnviroWeatherType> weatherPresets = new List<EnviroWeatherType>();
        private List<EnviroWeatherType> clearWeatherPresets = new List<EnviroWeatherType>();
        private List<EnviroWeatherType> cloudyWeatherPresets = new List<EnviroWeatherType>();
        private List<EnviroWeatherType> rainWeatherPresets = new List<EnviroWeatherType>();
        private List<EnviroWeatherType> snowWeatherPresets = new List<EnviroWeatherType>();




        private float timeOfDayChached;


        void OnEnable()
        {
            ConnectToWorldAPI();
        }

        void OnDisable()
        {
            DisconnectFromWorldAPI();
        }

        void Start()
        {
            if (EnviroManager.instance == null)
            {
                Debug.LogWarning("Enviro 3 Manager not found!");
                return;
            }

            if(EnviroManager.instance.Time != null)
               timeOfDayChached = EnviroManager.instance.Time.GetTimeOfDay();

            if(EnviroManager.instance.Weather != null)
            {
                //Create Lists of weather presets
                for (int i = 0; i < EnviroManager.instance.Weather.Settings.weatherTypes.Count; i++) 
                {
                    weatherPresets.Add(EnviroManager.instance.Weather.Settings.weatherTypes[i]);
                }
            
                for (int i = 0; i < weatherPresets.Count; i++) 
                {
                //Clear Weather List
                if (weatherPresets [i].cloudsOverride.coverageLayer1 <= -0.5)
                    clearWeatherPresets.Add (weatherPresets [i]);

                //Cloudy Weather List
                if (weatherPresets [i].cloudsOverride.coverageLayer1 >= -0.5) {
                    if (weatherPresets [i].environmentOverride.wetnessTarget == 0f && weatherPresets [i].environmentOverride.snowTarget == 0f)
                        cloudyWeatherPresets.Add (weatherPresets [i]);
                }

                // Rainy Weather List
                if (weatherPresets [i].environmentOverride.wetnessTarget > 0f)
                    rainWeatherPresets.Add (weatherPresets [i]);

                //Snowy Weather List
                if (weatherPresets [i].environmentOverride.snowTarget > 0f)
                    snowWeatherPresets.Add (weatherPresets [i]);
            }
		}


            ConnectToWorldAPI();
        }

        void Update()
        {
            if (EnviroManager.instance == null)
                return;

                   
            if (snowPower == GetSet.SendToWAPI) 
            {
                if(EnviroManager.instance.Environment != null)
                WorldManager.Instance.Snow = new Vector4 (EnviroManager.instance.Environment.Settings.snowTarget, EnviroManager.instance.Environment.Settings.snow, WorldManager.Instance.SnowMinHeight, WorldManager.Instance.SnowAge);
            }

            if (wetnessPower == GetSet.SendToWAPI) 
            {
                if(EnviroManager.instance.Environment != null)
                WorldManager.Instance.Rain = new Vector4 (EnviroManager.instance.Environment.Settings.wetnessTarget, EnviroManager.instance.Environment.Settings.wetness, WorldManager.Instance.RainMinHeight, WorldManager.Instance.RainMaxHeight);
            } 

            if (fogPower == GetSet.SendToWAPI) 
            {
                if(EnviroManager.instance.Fog != null)
                   WorldManager.Instance.Fog = new Vector4 (EnviroManager.instance.Fog.Settings.fogDensity2, EnviroManager.instance.Fog.Settings.fogHeight2, EnviroManager.instance.Fog.Settings.fogDensity, EnviroManager.instance.Fog.Settings.fogHeight);
            }
            
            if (seasons  == GetSet.SendToWAPI)
            {
                if(EnviroManager.instance.Time != null)
                WorldManager.Instance.Season = Mathf.Lerp(0f, 4f, EnviroManager.instance.Time.Settings.date.DayOfYear / 366);
            }

            if (time == GetSet.SendToWAPI)
            {
                if(EnviroManager.instance.Time != null)
                WorldManager.Instance.SetDecimalTime(EnviroManager.instance.Time.GetTimeOfDay());
            }

            if (temperature == GetSet.SendToWAPI)
            {
                if(EnviroManager.instance.Environment != null)
                WorldManager.Instance.Temperature = EnviroManager.instance.Environment.Settings.temperature;
            }

            if (location == GetSet.SendToWAPI)
            {
                if(EnviroManager.instance.Time != null)
                {
                    WorldManager.Instance.Latitude = EnviroManager.instance.Time.Settings.latitude;
                    WorldManager.Instance.Longitude = EnviroManager.instance.Time.Settings.longitude;
                }
            }

            if (cloudCover == GetSet.SendToWAPI)
            { 
                if(EnviroManager.instance.VolumetricClouds != null)
                   WorldManager.Instance.CloudPower = Mathf.Clamp01(EnviroManager.instance.VolumetricClouds.settingsLayer1.coverage + EnviroManager.instance.VolumetricClouds.settingsLayer2.coverage);
                else if (EnviroManager.instance.FlatClouds != null)
                    WorldManager.Instance.CloudPower = Mathf.Clamp01(EnviroManager.instance.FlatClouds.settings.flatCloudsCoverage);
            }
        }

        void ConnectToWorldAPI()
        {
            WorldManager.Instance.AddListener(this);
        }

        void DisconnectFromWorldAPI()
        {
            WorldManager.Instance.RemoveListener(this);
        }

        /// <summary>
        /// Handle updates from world manager
        /// </summary>
        /// <param name="changeArgs">Change to time of day</param>
        public void OnWorldChanged(WorldChangeArgs changeArgs)
        {
            if (EnviroManager.instance == null)
            {
                return;
            }
                
            // Get Time from WAPI
            if (changeArgs.HasChanged(WorldConstants.WorldChangeEvents.GameTimeChanged) && time == GetSet.GetFromWAPI && EnviroManager.instance.Time != null)
            {
                float newTimeOfDay = (float) changeArgs.manager.GetTimeDecimal();
                
                if (newTimeOfDay != timeOfDayChached)
                {
                    timeOfDayChached = newTimeOfDay;
                    EnviroManager.instance.Time.SetTimeOfDay(newTimeOfDay);
                }
            }

            //Get Season from WAPI
            if (changeArgs.HasChanged(WorldConstants.WorldChangeEvents.SeasonChanged) && seasons == GetSet.GetFromWAPI && EnviroManager.instance.Environment != null)
            {
                if (WorldManager.Instance.Season < 1f)
                    EnviroManager.instance.Environment.ChangeSeason(EnviroEnvironment.Seasons.Winter);
                else if (WorldManager.Instance.Season < 2f)
                    EnviroManager.instance.Environment.ChangeSeason(EnviroEnvironment.Seasons.Spring);
                else if (WorldManager.Instance.Season < 3f)
                    EnviroManager.instance.Environment.ChangeSeason(EnviroEnvironment.Seasons.Summer);
                else
                    EnviroManager.instance.Environment.ChangeSeason(EnviroEnvironment.Seasons.Autumn);
            }
                
            // Set Lat/Lng from WAPI
            if (changeArgs.HasChanged(WorldConstants.WorldChangeEvents.LatLngChanged) && location == GetSet.GetFromWAPI && EnviroManager.instance.Time != null)
            {
                EnviroManager.instance.Time.Settings.latitude = WorldManager.Instance.Latitude;
                EnviroManager.instance.Time.Settings.longitude = WorldManager.Instance.Longitude;
            }

            // Set Distance and Height Fog from WAPI
            if (changeArgs.HasChanged(WorldConstants.WorldChangeEvents.FogChanged) && fogPower == GetSet.GetFromWAPI && EnviroManager.instance.Fog != null)
            {
                EnviroManager.instance.Fog.Settings.fogDensity = WorldManager.Instance.FogDistancePower;
                EnviroManager.instance.Fog.Settings.fogDensity2 = WorldManager.Instance.FogHeightPower;
                EnviroManager.instance.Fog.Settings.fogHeight = WorldManager.Instance.FogHeightMax;
                EnviroManager.instance.Fog.Settings.fogHeight2 = WorldManager.Instance.FogHeightMax;
            }  

            // Set temparaute from WAPI
            if (changeArgs.HasChanged(WorldConstants.WorldChangeEvents.TempAndHumidityChanged) && temperature == GetSet.GetFromWAPI && EnviroManager.instance.Environment != null)
            {
                EnviroManager.instance.Environment.Settings.temperature = WorldManager.Instance.Temperature;
            }


            if (EnviroManager.instance.Weather == null)
            {
                // Cloud
                if (changeArgs.HasChanged(WorldConstants.WorldChangeEvents.CloudsChanged) && cloudCover == GetSet.GetFromWAPI)
                {
                    if(EnviroManager.instance.VolumetricClouds != null)
                    {
                        EnviroManager.instance.VolumetricClouds.settingsLayer1.coverage = EnviroHelper.Remap(WorldManager.Instance.CloudPower,0f,1f,-1f,1f);
                    }

                    if(EnviroManager.instance.FlatClouds != null)
                    {
                        EnviroManager.instance.FlatClouds.settings.flatCloudsCoverage = WorldManager.Instance.CloudPower;
                    }
                } 

                //Rain
                if (changeArgs.HasChanged (WorldConstants.WorldChangeEvents.RainChanged) && wetnessPower == GetSet.GetFromWAPI) 
                {
                    if(EnviroManager.instance.VolumetricClouds != null)
                    {
                        EnviroManager.instance.VolumetricClouds.settingsLayer1.scatteringIntensity = 1.25f - WorldManager.Instance.RainPower;
                    }

                    if(EnviroManager.instance.Effects != null)
                    {
                        EnviroManager.instance.Effects.Settings.rain1Emission = WorldManager.Instance.RainPower;
                        EnviroManager.instance.Effects.Settings.rain2Emission = WorldManager.Instance.RainPower;
                    }

                    if(EnviroManager.instance.Audio != null)
                    {
                        
                    }
                }

                //Snow
                if (changeArgs.HasChanged (WorldConstants.WorldChangeEvents.SnowChanged) && snowPower == GetSet.GetFromWAPI) 
                {
                    if(EnviroManager.instance.Effects != null)
                    {
                        EnviroManager.instance.Effects.Settings.snow1Emission = WorldManager.Instance.SnowPower;
                        EnviroManager.instance.Effects.Settings.snow1Emission = WorldManager.Instance.SnowPower;
                    }
    
                    if(EnviroManager.instance.Audio != null)
                    {
                        
                    }
                }
            }
            else
            {
                if (changeArgs.HasChanged(WorldConstants.WorldChangeEvents.CloudsChanged) && cloudCover == GetSet.GetFromWAPI){
                    ChangeWeatherOnCloudCoverChanged ();
                }

                //Rain
                if (changeArgs.HasChanged (WorldConstants.WorldChangeEvents.RainChanged) && wetnessPower == GetSet.GetFromWAPI) {
                    ChangeWeatherOnRainChanged (WorldManager.Instance.RainPower,WorldManager.Instance.SnowPower);
                }

                //Snow
                if (changeArgs.HasChanged (WorldConstants.WorldChangeEvents.SnowChanged) && snowPower == GetSet.GetFromWAPI) {
                    ChangeWeatherOnSnowChanged (WorldManager.Instance.RainPower,WorldManager.Instance.SnowPower);
                }
            }

            void ChangeWeatherOnCloudCoverChanged()
            {
                if (WorldManager.Instance.RainPower > 0.01f)
                    return;

                if (WorldManager.Instance.SnowPower > 0.01f)
                    return;

                float cloudCover = WorldManager.Instance.CloudPower;

                if (cloudCover <= 0.1f)
                {
                    if (clearWeatherPresets.Count > 0 && EnviroManager.instance.Weather.targetWeatherType.name != clearWeatherPresets[0].name)
                        EnviroManager.instance.Weather.ChangeWeather(clearWeatherPresets[0].name);

                }
                else if (cloudCover > 0.1f && cloudCover <= 0.3f)
                {
                    if (cloudyWeatherPresets.Count > 0 && EnviroManager.instance.Weather.targetWeatherType.name != cloudyWeatherPresets[0].name)
                        EnviroManager.instance.Weather.ChangeWeather(cloudyWeatherPresets[0].name);

                }
                else if (cloudCover > 0.3f && cloudCover <= 0.7f)
                {
                    if (cloudyWeatherPresets.Count > 1 && EnviroManager.instance.Weather.targetWeatherType.name != cloudyWeatherPresets[1].name)
                        EnviroManager.instance.Weather.ChangeWeather(cloudyWeatherPresets[1].name);

                }
                else if (cloudCover > 0.7f)
                {
                    if (cloudyWeatherPresets.Count > 2 && EnviroManager.instance.Weather.targetWeatherType.name != cloudyWeatherPresets[2].name)
                        EnviroManager.instance.Weather.ChangeWeather(cloudyWeatherPresets[2].name);

                }
            }

            void ChangeWeatherOnRainChanged(float r, float s)
            {
                if (r < s || r == 0f)
                {
                    if (s > 0)
                        ChangeWeatherOnSnowChanged(r, s);
                    else
                        ChangeWeatherOnCloudCoverChanged();
                    return;
                }

                float rainPower = r;

                if (rainPower < 0.1f)
                {
                    ChangeWeatherOnCloudCoverChanged();
                }
                else if (rainPower > 0.1f && rainPower <= 0.4f)
                {
                    if (rainWeatherPresets.Count > 0 && EnviroManager.instance.Weather.targetWeatherType.name != rainWeatherPresets[0].name)
                        EnviroManager.instance.Weather.ChangeWeather(rainWeatherPresets[0].name);

                }
                else if (rainPower > 0.4f && rainPower < 0.7f)
                {
                    if (rainWeatherPresets.Count > 1 && EnviroManager.instance.Weather.targetWeatherType.name != rainWeatherPresets[1].name)
                        EnviroManager.instance.Weather.ChangeWeather(rainWeatherPresets[1].name);

                }
                else if (rainPower > 0.7f)
                {
                    if (rainWeatherPresets.Count > 2 && EnviroManager.instance.Weather.targetWeatherType.name != rainWeatherPresets[2].name)
                        EnviroManager.instance.Weather.ChangeWeather(rainWeatherPresets[2].name);
                }
            } 

            void ChangeWeatherOnSnowChanged(float r, float s)
            {
                if (s < r || s == 0f)
                {
                    if (r > 0)
                        ChangeWeatherOnRainChanged(r, s);
                    else
                        ChangeWeatherOnCloudCoverChanged();

                    return;
                }

                float snowPower = s;

                if (snowPower <= 0.1f)
                {
                    ChangeWeatherOnCloudCoverChanged();
                }
                else if (snowPower > 0.1f && snowPower <= 0.5f)
                {
                    if (snowWeatherPresets.Count > 0 && EnviroManager.instance.Weather.targetWeatherType.name != snowWeatherPresets[0].name)
                        EnviroManager.instance.Weather.ChangeWeather(snowWeatherPresets[0].name);

                }
                else if (snowPower > 0.5f)
                {
                    if (snowWeatherPresets.Count > 1 && EnviroManager.instance.Weather.targetWeatherType.name != snowWeatherPresets[1].name)
                        EnviroManager.instance.Weather.ChangeWeather(snowWeatherPresets[1].name);

                }
            }
        }
    }
}
#endif