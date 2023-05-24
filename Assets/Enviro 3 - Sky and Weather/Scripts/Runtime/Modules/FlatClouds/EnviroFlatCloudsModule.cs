using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Enviro
{
    [Serializable]
    public class EnviroFlatClouds
    {  
        // Cirrus
        public bool useCirrusClouds = true;
        public Texture2D cirrusCloudsTex;
        [Range(0f,1f)]
        public float cirrusCloudsAlpha;
         [Range(0f,2f)]
        public float cirrusCloudsColorPower;
         [Range(0f,1f)]
        public float cirrusCloudsCoverage;
        [GradientUsage(true)]
        public Gradient  cirrusCloudsColor;
        [Range(0f,1f)]
        public float cirrusCloudsWindIntensity = 0.5f;

        // Flat Clouds 
        public bool useFlatClouds = true;
        public Texture2D flatCloudsBaseTex; 
        public Texture2D flatCloudsDetailTex;
 
        [GradientUsage(true)]
        public Gradient flatCloudsLightColor;
        [GradientUsage(true)]
        public Gradient flatCloudsAmbientColor;

        [Range(0f,2f)]
        public float flatCloudsLightIntensity = 1.0f;
        [Range(0f,2f)]
        public float flatCloudsAmbientIntensity = 1.0f;
        [Range(0f,2f)]
        public float flatCloudsAbsorbtion = 0.6f;
        [Range(0f,1f)]
        public float flatCloudsHGPhase = 0.6f;
        [Range(0f,2f)]
        public float flatCloudsCoverage = 1f;
        [Range(0f,2f)]
        public float flatCloudsDensity = 1f;
        public float flatCloudsAltitude = 10f;
        public bool flatCloudsTonemapping;
        public float flatCloudsBaseTiling = 4f;
        public float flatCloudsDetailTiling = 10f;
        [Range(0f,1f)]
        public float flatCloudsWindIntensity = 0.2f;
        [Range(0f,1f)]
        public float flatCloudsDetailWindIntensity = 0.5f;
    } 

    [Serializable]
    public class EnviroFlatCloudsModule : EnviroModule
    {   
        public Enviro.EnviroFlatClouds settings;
        public EnviroFlatCloudsModule preset;
        [HideInInspector]
        public bool showCirrusCloudsControls;
        [HideInInspector]
        public bool show2DCloudsControls; 
        [HideInInspector]
        public Vector2 cloudFlatBaseAnim;
        [HideInInspector]
        public Vector2 cloudFlatDetailAnim;
        [HideInInspector]
        public Vector2 cirrusAnim;
         
        // Update Method
        public override void UpdateModule ()
        { 
            if(EnviroManager.instance == null)
               return;

            UpdateWind ();

            if(settings.useCirrusClouds)
            {
                Shader.SetGlobalFloat("_CirrusClouds",1f);

                if(settings.cirrusCloudsTex != null)
                Shader.SetGlobalTexture("_CirrusCloudMap",settings.cirrusCloudsTex);
    
                Shader.SetGlobalFloat("_CirrusCloudAlpha",settings.cirrusCloudsAlpha);
                Shader.SetGlobalFloat("_CirrusCloudCoverage",settings.cirrusCloudsCoverage);
                Shader.SetGlobalFloat("_CirrusCloudColorPower",settings.cirrusCloudsColorPower);
                Shader.SetGlobalColor("_CirrusCloudColor",settings.cirrusCloudsColor.Evaluate(EnviroManager.instance.solarTime));
                Shader.SetGlobalVector("_CirrusCloudAnimation", new Vector4(cirrusAnim.x, cirrusAnim.y, 0f, 0f));
            }
            else
            {
                 Shader.SetGlobalFloat("_CirrusClouds",0f);
            }

            if(settings.useFlatClouds)
            {
                Shader.SetGlobalFloat("_FlatClouds",1f);

                if(settings.flatCloudsBaseTex != null)
                Shader.SetGlobalTexture("_FlatCloudsBaseTexture",settings.flatCloudsBaseTex);

                if(settings.flatCloudsDetailTex != null)
                Shader.SetGlobalTexture("_FlatCloudsDetailTexture",settings.flatCloudsDetailTex);

                //_FlatCloudsAnimation;
                Shader.SetGlobalColor("_FlatCloudsLightColor", settings.flatCloudsLightColor.Evaluate(EnviroManager.instance.solarTime));
                Shader.SetGlobalColor("_FlatCloudsAmbientColor", settings.flatCloudsAmbientColor.Evaluate(EnviroManager.instance.solarTime));
                
                Vector3 lightDirection = Vector3.forward;
                if(EnviroManager.instance.Objects.directionalLight != null)
                lightDirection = EnviroManager.instance.Objects.directionalLight.transform.forward;
 
                Shader.SetGlobalVector("_FlatCloudsLightDirection",lightDirection);
                Shader.SetGlobalVector("_FlatCloudsLightingParams",new Vector4(settings.flatCloudsLightIntensity * 10f, settings.flatCloudsAmbientIntensity, settings.flatCloudsAbsorbtion,settings.flatCloudsHGPhase));
                Shader.SetGlobalVector("_FlatCloudsParams",new Vector4(settings.flatCloudsCoverage, settings.flatCloudsDensity * 5f, settings.flatCloudsAltitude,0f));
                Shader.SetGlobalVector("_FlatCloudsTiling",new Vector4(settings.flatCloudsBaseTiling, settings.flatCloudsDetailTiling, 0f,0f));      
                Shader.SetGlobalVector("_FlatCloudsAnimation", new Vector4(cloudFlatBaseAnim.x, cloudFlatBaseAnim.y, cloudFlatDetailAnim.x, cloudFlatDetailAnim.y));
            }
            else
            {
                 Shader.SetGlobalFloat("_FlatClouds",0f);
            }
        }

        //Save and Load 
        public void LoadModuleValues ()
        {
            if(preset != null)
            {
                settings = JsonUtility.FromJson<Enviro.EnviroFlatClouds>(JsonUtility.ToJson(preset.settings));
            }
            else
            {
                Debug.Log("Please assign a saved module to load from!");
            }
        }

        private void UpdateWind ()
        {
          

            if(EnviroManager.instance.Environment != null)
            {
                cloudFlatBaseAnim += new Vector2(((EnviroManager.instance.Environment.Settings.windSpeed * EnviroManager.instance.Environment.Settings.windDirectionX) * settings.flatCloudsWindIntensity) * Time.deltaTime * 0.01f, ((EnviroManager.instance.Environment.Settings.windSpeed * EnviroManager.instance.Environment.Settings.windDirectionY) * settings.flatCloudsWindIntensity) * Time.deltaTime * 0.01f);
                cloudFlatDetailAnim += new Vector2(((EnviroManager.instance.Environment.Settings.windSpeed * EnviroManager.instance.Environment.Settings.windDirectionX) * settings.flatCloudsDetailWindIntensity) * Time.deltaTime * 0.1f, ((EnviroManager.instance.Environment.Settings.windSpeed * EnviroManager.instance.Environment.Settings.windDirectionY) * settings.flatCloudsDetailWindIntensity) * Time.deltaTime * 0.1f);
                cirrusAnim += new Vector2(((EnviroManager.instance.Environment.Settings.windSpeed * EnviroManager.instance.Environment.Settings.windDirectionX) * settings.cirrusCloudsWindIntensity) * Time.deltaTime * 0.01f, ((EnviroManager.instance.Environment.Settings.windSpeed * EnviroManager.instance.Environment.Settings.windDirectionY) * settings.cirrusCloudsWindIntensity) * Time.deltaTime * 0.01f);
            }
            else
            {
                cloudFlatBaseAnim += new Vector2(settings.flatCloudsWindIntensity * Time.deltaTime * 0.01f,settings.flatCloudsWindIntensity * Time.deltaTime * 0.01f);
                cloudFlatDetailAnim += new Vector2(settings.flatCloudsDetailWindIntensity * Time.deltaTime * 0.1f,settings.flatCloudsDetailWindIntensity * Time.deltaTime * 0.1f);
                cirrusAnim += new Vector2(settings.cirrusCloudsWindIntensity * Time.deltaTime * 0.01f,settings.cirrusCloudsWindIntensity * Time.deltaTime * 0.01f);
            }

            cirrusAnim = EnviroHelper.PingPong(cirrusAnim);
            cloudFlatBaseAnim = EnviroHelper.PingPong(cloudFlatBaseAnim);
            cloudFlatDetailAnim = EnviroHelper.PingPong(cloudFlatDetailAnim);

        }
 
        public void SaveModuleValues ()
        {
#if UNITY_EDITOR
        EnviroFlatCloudsModule t =  ScriptableObject.CreateInstance<EnviroFlatCloudsModule>();
        t.name = "Flat Clouds Preset";
        t.settings = JsonUtility.FromJson<Enviro.EnviroFlatClouds>(JsonUtility.ToJson(settings));
 
        string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(EnviroHelper.assetPath + "/New " + t.name + ".asset");
        UnityEditor.AssetDatabase.CreateAsset(t, assetPathAndName);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif
        }
        public void SaveModuleValues (EnviroFlatCloudsModule module)
        {
            module.settings = JsonUtility.FromJson<Enviro.EnviroFlatClouds>(JsonUtility.ToJson(settings));

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(module);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }
    }
}