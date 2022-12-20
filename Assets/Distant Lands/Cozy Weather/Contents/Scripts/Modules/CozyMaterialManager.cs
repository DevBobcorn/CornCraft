// Distant Lands 2022.




using DistantLands.Cozy.Data;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy
{

    [ExecuteAlways]
    public class CozyMaterialManager : CozyModule
    {

        [SerializeField]
        [Range(0, 1)]
        public float m_SnowAmount;
        [SerializeField]
        public float m_SnowMeltSpeed = 0.35f;
        [SerializeField]
        [Range(0, 1)]
        public float m_Wetness;
        [SerializeField]
        public float m_DryingSpeed = 0.5f;
        public float snowSpeed;
        public float rainSpeed;

        public MaterialManagerProfile profile;
        public List<PrecipitationFX> precipitationFXes = new List<PrecipitationFX>();

        void OnEnable()
        {

            if (GetComponent<CozyWeather>())
            {

                GetComponent<CozyWeather>().IntitializeModule(typeof(CozyMaterialManager));
                DestroyImmediate(this);
                Debug.LogWarning("Add modules in the settings tab in COZY 2!");
                return;

            }
        }


        // Start is called before the first frame update
        void Awake()
        {

            if (!enabled)
                return;
            base.SetupModule();


            if (profile == null)
                return;

            SetupStaticGlobalVariables();



        }

        // Update is called once per frame                              
        void Update()
        {

            if (weatherSphere == null)
                base.SetupModule();

            if (profile == null)
                return;

            m_SnowAmount += Time.deltaTime * snowSpeed;

            if (snowSpeed <= 0)
                if (weatherSphere.currentTemperature > 32)
                {
                    m_SnowAmount -= Time.deltaTime * m_SnowMeltSpeed * 0.03f;
                }
                else
                    m_SnowAmount -= Time.deltaTime * m_SnowMeltSpeed * 0.001f;

            m_Wetness += (Time.deltaTime * rainSpeed) + (-1 * m_DryingSpeed * 0.001f);

            m_SnowAmount = Mathf.Clamp01(m_SnowAmount);
            m_Wetness = Mathf.Clamp01(m_Wetness);

            SetupStaticGlobalVariables();

            Shader.SetGlobalFloat("CZY_SnowAmount", m_SnowAmount);
            Shader.SetGlobalFloat("CZY_WetnessAmount", m_Wetness);

            foreach (MaterialManagerProfile.ModulatedValue i in profile.modulatedValues)
            {
                switch (i.modulationTarget)
                {
                    case (MaterialManagerProfile.ModulatedValue.ModulationTarget.globalColor):
                        Shader.SetGlobalColor(i.targetVariableName, i.mappedGradient.Evaluate(GetPercentage(i.modulationSource)));
                        break;
                    case (MaterialManagerProfile.ModulatedValue.ModulationTarget.globalValue):
                        Shader.SetGlobalFloat(i.targetVariableName, i.mappedCurve.Evaluate(GetPercentage(i.modulationSource)));
                        break;
                    case (MaterialManagerProfile.ModulatedValue.ModulationTarget.materialColor):
                        if (i.targetMaterial)
                            i.targetMaterial.SetColor(i.targetVariableName, i.mappedGradient.Evaluate(GetPercentage(i.modulationSource)));
                        break;
                    case (MaterialManagerProfile.ModulatedValue.ModulationTarget.materialValue):
                        if (i.targetMaterial)
                            i.targetMaterial.SetFloat(i.targetVariableName, i.mappedCurve.Evaluate(GetPercentage(i.modulationSource)));
                        break;
                    case (MaterialManagerProfile.ModulatedValue.ModulationTarget.terrainLayerColor):
                        if (i.targetLayer)
                            i.targetLayer.specular = i.mappedGradient.Evaluate(GetPercentage(i.modulationSource));
                        break;

                }
            }

        }

        float GetPercentage(MaterialManagerProfile.ModulatedValue.ModulationSource modulationSource)
        {

            float i = 0;

            switch (modulationSource)
            {
                case (MaterialManagerProfile.ModulatedValue.ModulationSource.dayPercent):
                    i = weatherSphere.GetCurrentDayPercentage();
                    break;
                case (MaterialManagerProfile.ModulatedValue.ModulationSource.precipitation):
                    i = Mathf.Clamp01(weatherSphere.GetPrecipitation() / 100);
                    break;
                case (MaterialManagerProfile.ModulatedValue.ModulationSource.rainAmount):
                    i = m_Wetness;
                    break;
                case (MaterialManagerProfile.ModulatedValue.ModulationSource.snowAmount):
                    i = m_SnowAmount;
                    break;
                case (MaterialManagerProfile.ModulatedValue.ModulationSource.temperature):
                    i = Mathf.Clamp01(weatherSphere.GetTemperature(false) / 100);
                    break;
                case (MaterialManagerProfile.ModulatedValue.ModulationSource.yearPercent):
                    i = weatherSphere.GetCurrentYearPercentage();
                    break;

            }

            return i;

        }

        public void SetupStaticGlobalVariables()
        {

            Shader.SetGlobalFloat("CZY_SnowScale", profile.snowNoiseSize);
            Shader.SetGlobalTexture("CZY_SnowTexture", profile.snowTexture);
            Shader.SetGlobalColor("CZY_SnowColor", profile.snowColor);
            Shader.SetGlobalFloat("CZY_PuddleScale", profile.puddleScale);


        }

    }
}