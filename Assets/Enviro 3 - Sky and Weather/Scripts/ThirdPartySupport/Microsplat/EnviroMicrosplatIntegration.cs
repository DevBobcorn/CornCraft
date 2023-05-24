using UnityEngine;
using System.Collections;


namespace Enviro
{

    [ExecuteInEditMode] 
    [AddComponentMenu("Enviro 3/Integrations/MicroSplat Integration")]
    public class EnviroMicrosplatIntegration : MonoBehaviour  
    { 
        [Header("Wetness")]
        public bool UpdateWetness = true;
        [Range(0f, 1f)]
        public float minWetness = 0f;
        [Range(0f, 1f)]
        public float maxWetness = 1f;
        [Header("Rain Ripples")]
        public bool UpdateRainRipples = true;
        [Header("Puddle Settings")]
        public bool UpdatePuddles = true;
        [Header("Stream Settings")] 
        public bool UpdateStreams = true;
        [Header("Snow Settings")]
        public bool UpdateSnow = true;
      //  [Header("Wind Settings")]
      //  public bool UpdateWindStrength = true;
      //  public bool UpdateWindRotation = true;

        void Update () 
        {
            if (EnviroManager.instance == null || EnviroManager.instance.Environment == null)
                return;

            if (UpdateSnow){
                Shader.SetGlobalFloat ("_Global_SnowLevel", EnviroManager.instance.Environment.Settings.snow);
            }

            if (UpdateWetness) {
                float currWetness = Mathf.Clamp(EnviroManager.instance.Environment.Settings.wetness, minWetness, maxWetness);
                Shader.SetGlobalVector("_Global_WetnessParams", new Vector2(currWetness, maxWetness));
            }
                
            if (UpdatePuddles) {
                Shader.SetGlobalFloat("_Global_PuddleParams", EnviroManager.instance.Environment.Settings.wetness);
            }

            if (UpdateRainRipples) 
            {
                if(EnviroManager.instance.Effects != null)
                {
                    float rainIntensity = Mathf.Clamp(EnviroManager.instance.Effects.Settings.rain1Emission + EnviroManager.instance.Effects.Settings.rain2Emission,0f,1f);
                    Shader.SetGlobalFloat("_Global_RainIntensity", rainIntensity);
                }
            }

            if (UpdateStreams) {
                Shader.SetGlobalFloat("_Global_StreamMax", EnviroManager.instance.Environment.Settings.wetness);
            }
        }
    }
} 