using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Enviro 
{
    [Serializable]
    public class EnviroEffectTypes
    {
        public enum ControlType
        {
            Rain1,
            Rain2,
            Snow1,
            Snow2,
            Custom1,
            Custom2
        }
        public ControlType controlType;
        public ParticleSystem mySystem; 
        public string name;
        public GameObject prefab;
        public Vector3 localPositionOffset;
        public Vector3 localRotationOffset;
        public float maxEmission; 
    }

    [Serializable]
    public class EnviroEffects
    {
        public List<EnviroEffectTypes> effectTypes = new List<EnviroEffectTypes>();

        [Range(0f,1f)]
        public float rain1Emission, rain2Emission, snow1Emission, snow2Emission, custom1Emission, custom2Emission = 0f;
    }
 
    [Serializable]
    [ExecuteInEditMode]
    public class EnviroEffectsModule : EnviroModule
    {  
        public Enviro.EnviroEffects Settings;
        public EnviroEffectsModule preset;

        //Inspector
        public bool showSetupControls;
        public bool showEmissionControls;
        public override void Enable ()
        { 
            if(EnviroManager.instance == null)
               return;
               
            Setup();
        } 

        public override void Disable ()
        { 
            if(EnviroManager.instance == null)
               return;

            Cleanup();
        }

        private void Setup()
        {
            CreateEffects();
        }  
 
        private void Cleanup()
        {
            if(EnviroManager.instance.Objects.effects != null)
               DestroyImmediate(EnviroManager.instance.Objects.effects);
        }

        public override void UpdateModule ()
        { 
            UpdateEffects();
        }

        public void CreateEffects() 
        {
            if(EnviroManager.instance.Objects.effects != null)
               DestroyImmediate(EnviroManager.instance.Objects.effects);

            if(EnviroManager.instance.Objects.effects == null)
            {
                EnviroManager.instance.Objects.effects = new GameObject();
                EnviroManager.instance.Objects.effects.name = "Effects";
                EnviroManager.instance.Objects.effects.transform.SetParent(EnviroManager.instance.transform);
                EnviroManager.instance.Objects.effects.transform.localPosition = Vector3.zero;
            }

            for(int i = 0; i < Settings.effectTypes.Count; i++)
            {
                if(Settings.effectTypes[i].mySystem != null)
                    DestroyImmediate(Settings.effectTypes[i].mySystem.gameObject);

                GameObject sys;
                  
                if(Settings.effectTypes[i].prefab != null)
                {
                   sys = Instantiate(Settings.effectTypes[i].prefab,Settings.effectTypes[i].localPositionOffset,Quaternion.identity);
                   sys.transform.SetParent(EnviroManager.instance.Objects.effects.transform);
                   sys.name = Settings.effectTypes[i].name;
                   sys.transform.localPosition = Settings.effectTypes[i].localPositionOffset;
                   sys.transform.localEulerAngles = Settings.effectTypes[i].localRotationOffset;
                   Settings.effectTypes[i].mySystem = sys.GetComponent<ParticleSystem>();
                }
            }
        }

        public float GetEmissionRate(ParticleSystem system)
        {
            return system.emission.rateOverTime.constantMax;
        }


        public void SetEmissionRate(ParticleSystem sys, float emissionRate)
        {
            var emission = sys.emission;
            var rate = emission.rateOverTime;
            rate.constantMax = emissionRate;
            emission.rateOverTime = rate;
        }

        private void UpdateEffects()
        {
            for(int i = 0; i < Settings.effectTypes.Count; i++)
            {
                if(Settings.effectTypes[i].mySystem != null)
                {
                    float currentEmission = Settings.effectTypes[i].maxEmission;

                    switch(Settings.effectTypes[i].controlType)
                    {
                        case EnviroEffectTypes.ControlType.Rain1:
                        currentEmission *= Settings.rain1Emission;
                        break;

                        case EnviroEffectTypes.ControlType.Rain2:
                        currentEmission *= Settings.rain2Emission;
                        break;

                        case EnviroEffectTypes.ControlType.Snow1:
                        currentEmission *= Settings.snow1Emission;
                        break;
        
                        case EnviroEffectTypes.ControlType.Snow2:
                        currentEmission *= Settings.snow2Emission;
                        break;

                        case EnviroEffectTypes.ControlType.Custom1:
                        currentEmission *= Settings.custom1Emission;
                        break;

                        case EnviroEffectTypes.ControlType.Custom2:
                        currentEmission *= Settings.custom2Emission;
                        break;

                    }

                    SetEmissionRate(Settings.effectTypes[i].mySystem,currentEmission);

                    if(currentEmission > 0f && !Settings.effectTypes[i].mySystem.isPlaying)
                       Settings.effectTypes[i].mySystem.Play();
                }
            }
        }




        //Save and Load
        public void LoadModuleValues ()
        {
            if(preset != null)
            {
                Settings = JsonUtility.FromJson<Enviro.EnviroEffects>(JsonUtility.ToJson(preset.Settings));
            } 
            else
            {
                Debug.Log("Please assign a saved module to load from!");
            }
        }

        public void SaveModuleValues ()
        {
#if UNITY_EDITOR
        EnviroEffectsModule t =  ScriptableObject.CreateInstance<EnviroEffectsModule>();
        t.name = "Effects Module";
        t.Settings = JsonUtility.FromJson<Enviro.EnviroEffects>(JsonUtility.ToJson(Settings));
 
        string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(EnviroHelper.assetPath + "/New " + t.name + ".asset");
        UnityEditor.AssetDatabase.CreateAsset(t, assetPathAndName);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public void SaveModuleValues (EnviroEffectsModule module)
        {
            module.Settings = JsonUtility.FromJson<Enviro.EnviroEffects>(JsonUtility.ToJson(Settings));
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(module);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }
    }
}