using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace Enviro
{

    [Serializable]
    public class EnviroZoneWeather
    {
        public bool showEditor;
        public EnviroWeatherType weatherType;
        public float probability;

    }
    [AddComponentMenu("Enviro 3/Weather Zone")]
    public class EnviroZone : MonoBehaviour
    {

        public EnviroWeatherType currentWeatherType;
        public EnviroWeatherType nextWeatherType;

        public bool autoWeatherChanges = true;
        public float weatherChangeIntervall = 2f;
        public double nextWeatherUpdate;

        public List<EnviroZoneWeather> weatherTypeList = new List<EnviroZoneWeather>();
        public Vector3 zoneScale = Vector3.one;
        public Color zoneGizmoColor;

        private BoxCollider zoneCollider;

        void Start()
        {
            zoneCollider = gameObject.AddComponent<BoxCollider>();
            zoneCollider.isTrigger = true;
            UpdateZoneScale ();
        }

        public void UpdateZoneScale ()
        {
            zoneCollider.size = zoneScale;
        }

        // Adds a new weather type to the zone.
        public void AddWeatherType(EnviroWeatherType wType)
        {
            EnviroZoneWeather weatherTypeEntry = new EnviroZoneWeather();
            weatherTypeEntry.weatherType = wType;
            weatherTypeList.Add(weatherTypeEntry);
        }

        // Removes a weather type from the zone.
        public void RemoveWeatherZoneType(EnviroZoneWeather wType)
        {
            weatherTypeList.Remove(wType);
        }

        // Changes the weather of the zone instantly.
        public void ChangeZoneWeatherInstant (EnviroWeatherType type)
        {
            if(EnviroManager.instance != null && currentWeatherType != type)
            {
                EnviroManager.instance.NotifyZoneWeatherChanged(type,this);
            }
            
            currentWeatherType = type;
        }

        // Changes the weather of the zone to the type for next weather update.
        public void ChangeZoneWeather (EnviroWeatherType type)
        {
            nextWeatherType = type;
        }

        private void ChooseNextWeatherRandom ()
        {
            float rand = UnityEngine.Random.Range(0f,100f);
            bool nextWeatherFound = false;

            for (int i = 0; i < weatherTypeList.Count; i++)
            {
                if(rand <= weatherTypeList[i].probability)
                {
                    ChangeZoneWeather(weatherTypeList[i].weatherType);
                    nextWeatherFound = true;
                    return;
                }
            }

            if(!nextWeatherFound)
               ChangeZoneWeather(currentWeatherType);
        }

        private void UpdateZoneWeather()
        {
            if(EnviroManager.instance.Time != null)
            {
               double currentDate = EnviroManager.instance.Time.GetDateInHours();

               if(currentDate >= nextWeatherUpdate)
               {
                 if(nextWeatherType != null)
                  ChangeZoneWeatherInstant(nextWeatherType);
                 else
                  ChangeZoneWeatherInstant(currentWeatherType);
                 
                 //Get next weather
                 ChooseNextWeatherRandom ();
                 nextWeatherUpdate = currentDate + weatherChangeIntervall;
               }
            }
        }

        void Update()
        {
            if (EnviroManager.instance == null || EnviroManager.instance.Weather == null)
                return;

            if(autoWeatherChanges)
                UpdateZoneWeather();

            //Forces the weather change in Enviro when this zone is currently the active one.
            if(EnviroManager.instance.Weather.currentZone == this && EnviroManager.instance.Weather.targetWeatherType != currentWeatherType)
               EnviroManager.instance.Weather.ChangeWeather(currentWeatherType);
        }

        void OnTriggerEnter (Collider col)
        {
            if (EnviroManager.instance == null || EnviroManager.instance.Weather == null)
                return;

            //Change Weather to Zone Weather:
            if(col.gameObject.GetComponent<EnviroManager>())
               EnviroManager.instance.Weather.currentZone = this;
               
            //EnviroManager.instance.Weather.ChangeWeather(currentWeatherType);
        }

        void OnTriggerExit (Collider col)
        {
             if (EnviroManager.instance == null || EnviroManager.instance.Weather == null)
                 return;
        
             if(col.gameObject.GetComponent<EnviroManager>())
                EnviroManager.instance.Weather.currentZone = null;
        } 

        void OnDrawGizmos () 
        {
            Gizmos.color = zoneGizmoColor;
            
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.matrix = rotationMatrix;

            Gizmos.DrawCube(Vector3.zero, new Vector3(zoneScale.x, zoneScale.y, zoneScale.z));
        }
    }
}
