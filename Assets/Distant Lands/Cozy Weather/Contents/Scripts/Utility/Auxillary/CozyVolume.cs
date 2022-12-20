// Distant Lands 2022.



using DistantLands.Cozy.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace DistantLands.Cozy
{
    [RequireComponent(typeof(Collider))]
    public class CozyVolume : MonoBehaviour
    {


        public enum TriggerType { setWeather, triggerEvent, setTicks, setDay, setAtmosphere, setAmbience }
        public enum SetType { setInstantly, transition }
        public enum TriggerState { onEnter, onStay, onExit }

        [SerializeField]
        private TriggerType m_TriggerType;
        [SerializeField]
        private TriggerState m_TriggerState;
        [SerializeField]
        private SetType m_SetType;
        [SerializeField]
        private string m_Tag = "Untagged";
        private CozyWeather m_CozyWeather;


        [SerializeField]
        private WeatherProfile m_WeatherProfile;
        [SerializeField]
        private float m_TransitionTime;
        [SerializeField]
        private UnityEvent m_Event;
        [SerializeField]
        private AtmosphereProfile m_AtmosphereProfile;
        [SerializeField]
        private AmbienceProfile m_AmbienceProfile;
        [SerializeField]
        private float ticks;
        [SerializeField]
        private int day;
        [SerializeField]
        private float transitionTime;


        private void Awake()
        {

            m_CozyWeather = CozyWeather.instance;

        }


        public void Run()
        {

            if (m_SetType == SetType.setInstantly)
                Set();
            else
                Transition();


        }


        public void Transition()
        {

            switch (m_TriggerType)
            {
                case (TriggerType.setWeather):
                    m_CozyWeather.SetWeather(m_WeatherProfile, m_TransitionTime);
                    break;
                case (TriggerType.triggerEvent):
                    m_Event.Invoke();
                    break;
                case (TriggerType.setAtmosphere):
                    m_CozyWeather.ChangeAtmosphere(m_AtmosphereProfile, m_AtmosphereProfile, m_TransitionTime);
                    break;
                case (TriggerType.setDay):
                    m_CozyWeather.TransitionTime(ticks, day);
                    break;
                case (TriggerType.setTicks):
                    m_CozyWeather.TransitionTime(ticks, m_CozyWeather.currentDay);
                    break;
                case (TriggerType.setAmbience):
                    m_CozyWeather.GetModule<CozyAmbienceManager>().SetAmbience(m_AmbienceProfile, m_TransitionTime);
                    break;
            }

        }


        public void Set()
        {

            switch (m_TriggerType)
            {
                case (TriggerType.setWeather):
                    m_CozyWeather.currentWeather = m_WeatherProfile;
                    break;
                case (TriggerType.triggerEvent):
                    m_Event.Invoke();
                    break;
                case (TriggerType.setAtmosphere):
                    m_CozyWeather.atmosphereProfile = m_AtmosphereProfile;
                    m_CozyWeather.ResetQuality();
                    break;
                case (TriggerType.setDay):
                    m_CozyWeather.currentDay = day;
                    break;
                case (TriggerType.setTicks):
                    m_CozyWeather.currentTicks = ticks;
                    break;
                case (TriggerType.setAmbience):
                    if (m_CozyWeather.GetModule<CozyAmbienceManager>())
                        m_CozyWeather.GetModule<CozyAmbienceManager>().SetAmbience(m_AmbienceProfile, 0);
                    break;
            }

        }


        private void OnTriggerEnter(Collider other)
        {

            if (m_TriggerState != TriggerState.onEnter)
                return;

            if (other.gameObject.tag == m_Tag)
                Run();


        }

        private void OnTriggerStay(Collider other)
        {
            if (m_TriggerState != TriggerState.onStay)
                return;

            if (other.gameObject.tag == m_Tag)
                Run();


        }

        private void OnTriggerExit(Collider other)
        {
            if (m_TriggerState != TriggerState.onExit)
                return;

            if (other.gameObject.tag == m_Tag)
                Run();


        }


    }
}