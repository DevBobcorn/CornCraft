using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Enviro
{
    [Serializable]
    public class EnviroQualities
    {   
        public EnviroQuality defaultQuality;
        public List<EnviroQuality> Qualities = new List<EnviroQuality>();
    } 

    [Serializable]
    public class EnviroQualityModule : EnviroModule
    {   
        public Enviro.EnviroQualities Settings = new EnviroQualities();
        public EnviroQualityModule preset;
        public bool showQualityControls;

 
        public override void Enable()
        {
            base.Enable();

            //Make sure that we always have at least one quality profile!
            if(Settings.defaultQuality == null)
            {
                if(Settings.Qualities.Count > 0)
                { 
                    Settings.defaultQuality = Settings.Qualities[0];
                }
                else
                {
                    CreateNewQuality();
                    Settings.defaultQuality = Settings.Qualities[0];
                }
            }
        }

        public override void UpdateModule ()
        { 
             if(EnviroManager.instance == null)
               return;
               
            if(Settings.defaultQuality != null)
            {
                if(EnviroManager.instance.VolumetricClouds != null)
                {
                    EnviroManager.instance.VolumetricClouds.settingsQuality.volumetricClouds = Settings.defaultQuality.volumetricCloudsOverride.volumetricClouds; 
                    EnviroManager.instance.VolumetricClouds.settingsGlobal.dualLayer = Settings.defaultQuality.volumetricCloudsOverride.dualLayer; 
                    EnviroManager.instance.VolumetricClouds.settingsQuality.downsampling = Settings.defaultQuality.volumetricCloudsOverride.downsampling;
                    EnviroManager.instance.VolumetricClouds.settingsQuality.stepsLayer1 = Settings.defaultQuality.volumetricCloudsOverride.stepsLayer1;
                    EnviroManager.instance.VolumetricClouds.settingsQuality.stepsLayer2 = Settings.defaultQuality.volumetricCloudsOverride.stepsLayer2;
                    EnviroManager.instance.VolumetricClouds.settingsQuality.blueNoiseIntensity = Settings.defaultQuality.volumetricCloudsOverride.blueNoiseIntensity;
                    EnviroManager.instance.VolumetricClouds.settingsQuality.reprojectionBlendTime = Settings.defaultQuality.volumetricCloudsOverride.reprojectionBlendTime;
                    EnviroManager.instance.VolumetricClouds.settingsQuality.lodDistance = Settings.defaultQuality.volumetricCloudsOverride.lodDistance;
                }  
 
                if(EnviroManager.instance.Fog != null)
                {
                    EnviroManager.instance.Fog.Settings.fog = Settings.defaultQuality.fogOverride.fog;
                    EnviroManager.instance.Fog.Settings.volumetrics = Settings.defaultQuality.fogOverride.volumetrics;
                    EnviroManager.instance.Fog.Settings.quality = Settings.defaultQuality.fogOverride.quality;
                    EnviroManager.instance.Fog.Settings.steps = Settings.defaultQuality.fogOverride.steps;
                }

                if(EnviroManager.instance.FlatClouds != null)
                {
                    EnviroManager.instance.FlatClouds.settings.useFlatClouds = Settings.defaultQuality.flatCloudsOverride.flatClouds;
                    EnviroManager.instance.FlatClouds.settings.useCirrusClouds = Settings.defaultQuality.flatCloudsOverride.cirrusClouds;
                }

                if(EnviroManager.instance.Aurora != null)
                {
                    EnviroManager.instance.Aurora.Settings.useAurora = Settings.defaultQuality.auroraOverride.aurora;
                    EnviroManager.instance.Aurora.Settings.auroraSteps = Settings.defaultQuality.auroraOverride.steps;
                }

            }
        } 

        
        public void CleanupQualityList() 
        {
            for (int i = 0; i < Settings.Qualities.Count; i++)
            {
                if(Settings.Qualities[i] == null)
                    Settings.Qualities.RemoveAt(i);
            } 
        } 
        
        //Add new or assigned quality
        public void CreateNewQuality()
        {
            EnviroQuality quality = EnviroQualityCreation.CreateMyAsset();
            Settings.Qualities.Add(quality);
        }

        /// Removes the quality from the list.
        public void RemoveQuality(EnviroQuality quality)
        { 
            Settings.Qualities.Remove(quality);
        }


        //Save and Load 
        public void LoadModuleValues ()
        {
            if(preset != null)
            {
                Settings = JsonUtility.FromJson<Enviro.EnviroQualities>(JsonUtility.ToJson(preset.Settings));
            }
            else
            {
                Debug.Log("Please assign a saved module to load from!");
            }
        }
 
        public void SaveModuleValues ()
        {
#if UNITY_EDITOR
        EnviroQualityModule t =  ScriptableObject.CreateInstance<EnviroQualityModule>();
        t.name = "Quality Module Preset";
        t.Settings = JsonUtility.FromJson<Enviro.EnviroQualities>(JsonUtility.ToJson(Settings));
 
        string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(EnviroHelper.assetPath + "/New " + t.name + ".asset");
        UnityEditor.AssetDatabase.CreateAsset(t, assetPathAndName);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif
        }
        public void SaveModuleValues (EnviroQualityModule module)
        {
            module.Settings = JsonUtility.FromJson<Enviro.EnviroQualities>(JsonUtility.ToJson(Settings));

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(module);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }
    }
}