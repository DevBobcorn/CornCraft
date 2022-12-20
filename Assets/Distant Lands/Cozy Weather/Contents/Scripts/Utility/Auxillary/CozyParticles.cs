// Distant Lands 2022.



using DistantLands.Cozy.Data;
using System.Collections.Generic;
using UnityEngine;


namespace DistantLands.Cozy
{
    public class CozyParticles : MonoBehaviour
    {


        private CozyWeather weatherSphere;
        [HideInInspector]
        public float weight;
        [HideInInspector]
        public CozyParticleManager particleManager;

        [System.Serializable]
        public class ParticleType
        {

            public ParticleSystem particleSystem;
            public float emissionAmount;



        }

        [HideInInspector]
        public List<ParticleType> m_ParticleTypes;


        // Start is called before the first frame update
        void Awake()
        {

            weatherSphere = FindObjectOfType<CozyWeather>();

            foreach (ParticleSystem i in GetComponentsInChildren<ParticleSystem>())
            {
                ParticleType j = new ParticleType();
                j.particleSystem = i;
                j.emissionAmount = i.emission.rateOverTime.constant;
                m_ParticleTypes.Add(j);
            }


            foreach (ParticleType i in m_ParticleTypes)
            {

                ParticleSystem.EmissionModule k = i.particleSystem.emission;
                ParticleSystem.MinMaxCurve j = k.rateOverTime;

                j.constant = 0;
                k.rateOverTime = j;


            }



        }

        public void SetupTriggers()
        {

            foreach (ParticleType particle in m_ParticleTypes)
            {
                ParticleSystem.TriggerModule triggers = particle.particleSystem.trigger;

                triggers.enter = ParticleSystemOverlapAction.Kill;
                triggers.inside = ParticleSystemOverlapAction.Kill;
                for (int j = 0; j < weatherSphere.cozyTriggers.Count; j++)
                    triggers.SetCollider(j, weatherSphere.cozyTriggers[j]);
            }
        }

        public void Play()
        {

            if (this == null)
                return;

            foreach (ParticleType particle in m_ParticleTypes)
            {
                ParticleSystem.EmissionModule i = particle.particleSystem.emission;
                ParticleSystem.MinMaxCurve j = i.rateOverTime;

                j.constant = particle.emissionAmount * particleManager.multiplier;
                i.rateOverTime = j;
                if (particle.particleSystem.isStopped)
                    particle.particleSystem.Play();
            }
        }

        public void Stop()
        {

            if (m_ParticleTypes != null)
            foreach (ParticleType particle in m_ParticleTypes)
            {

                if (particle.particleSystem != null)
                    if (particle.particleSystem.isPlaying)
                        particle.particleSystem.Stop();
            }
        }

        public void Play(float weight)
        {

            if (this == null)
                return;

            foreach (ParticleType particle in m_ParticleTypes)
            {
                ParticleSystem.EmissionModule i = particle.particleSystem.emission;
                ParticleSystem.MinMaxCurve j = i.rateOverTime;

                j.constant = Mathf.Lerp(0, particle.emissionAmount * particleManager.multiplier, weight);
                i.rateOverTime = j;
                if (particle.particleSystem.isStopped)
                    particle.particleSystem.Play();
            }


        }
    }
}