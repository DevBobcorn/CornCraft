using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Enviro
{ 
    [Serializable]    
    public class EnviroAudio 
    {
        public List<EnviroAudioClip> ambientClips = new List<EnviroAudioClip>();
        public List<EnviroAudioClip> weatherClips = new List<EnviroAudioClip>();
        public List<EnviroAudioClip> thunderClips = new List<EnviroAudioClip>();
        public float ambientMasterVolume = 1f;
        public float weatherMasterVolume = 1f;
        public float thunderMasterVolume = 1f;
    }

    [Serializable]
    public class EnviroAudioClip 
    {
        public enum PlayBackType
        {
            Always,
            BasedOnSun,
            BasedOnMoon
        }

        public bool showEditor;
        public string name;
        public AudioClip audioClip;
        public UnityEngine.Audio.AudioMixerGroup audioMixerGroup;
        public PlayBackType playBackType;
        public AudioSource myAudioSource;
        public bool loop = false;
        public float volume = 0f;
        public AnimationCurve volumeCurve = new AnimationCurve();
        public float maxVolume = 1f;
    }  
 
    [Serializable]
    [ExecuteInEditMode]
    public class EnviroAudioModule : EnviroModule
    {  
        public Enviro.EnviroAudio Settings;
        public EnviroAudioModule preset;

        public float ambientVolumeModifier, weatherVolumeModifier, thunderVolumeModifier = 0f;

        //Inspector
        public bool showAmbientSetupControls,showWeatherSetupControls,showThunderSetupControls, showAudioControls;

        public override void Enable ()
        { 
            if(EnviroManager.instance == null)
               return;

            CreateAudio();
        } 

        public override void Disable ()
        { 
            if(EnviroManager.instance == null)
               return;

            Cleanup();
        }

        private void Setup()
        {
     
        }  

        private void Cleanup()
        {
            if(EnviroManager.instance == null)
               return;

            if(EnviroManager.instance.Objects.audio != null)
               DestroyImmediate(EnviroManager.instance.Objects.audio);
        }

        
        // Update Method
        public override void UpdateModule ()
        { 
            UpdateAudio();
        }

        public void CreateAudio()
        {
            if(EnviroManager.instance.Objects.audio != null)
               DestroyImmediate(EnviroManager.instance.Objects.audio);

            if(EnviroManager.instance.Objects.audio == null)
            {
                EnviroManager.instance.Objects.audio = new GameObject();
                EnviroManager.instance.Objects.audio.name = "Audio";
                EnviroManager.instance.Objects.audio.transform.SetParent(EnviroManager.instance.transform);
                EnviroManager.instance.Objects.audio.transform.localPosition = Vector3.zero;
            }

            //Ambient
            for(int i = 0; i < Settings.ambientClips.Count; i++)
            {
                if(Settings.ambientClips[i].myAudioSource != null)
                    DestroyImmediate(Settings.ambientClips[i].myAudioSource.gameObject);

                GameObject sys;
                  
                if(Settings.ambientClips[i].audioClip != null)
                {
                   sys = new GameObject();
                   sys.name = "Ambient - " +Settings.ambientClips[i].name;
                   sys.transform.SetParent(EnviroManager.instance.Objects.audio.transform);             
                   Settings.ambientClips[i].myAudioSource = sys.AddComponent<AudioSource>();
                   Settings.ambientClips[i].myAudioSource.clip = Settings.ambientClips[i].audioClip;
                   Settings.ambientClips[i].myAudioSource.loop = Settings.ambientClips[i].loop;
                   Settings.ambientClips[i].myAudioSource.volume = Settings.ambientClips[i].volume;
                   Settings.ambientClips[i].myAudioSource.outputAudioMixerGroup = Settings.ambientClips[i].audioMixerGroup;
                }
            }

            //Weather
            for(int i = 0; i < Settings.weatherClips.Count; i++)
            {
                if(Settings.weatherClips[i].myAudioSource != null)
                    DestroyImmediate(Settings.weatherClips[i].myAudioSource.gameObject);

                GameObject sys;
                  
                if(Settings.weatherClips[i].audioClip != null)
                {
                   sys = new GameObject();
                   sys.name = "Weather - " + Settings.weatherClips[i].name;
                   sys.transform.SetParent(EnviroManager.instance.Objects.audio.transform);             
                   Settings.weatherClips[i].myAudioSource = sys.AddComponent<AudioSource>();
                   Settings.weatherClips[i].myAudioSource.clip = Settings.weatherClips[i].audioClip;
                   Settings.weatherClips[i].myAudioSource.loop = Settings.weatherClips[i].loop;
                   Settings.weatherClips[i].myAudioSource.volume = Settings.weatherClips[i].volume;
                   Settings.weatherClips[i].myAudioSource.outputAudioMixerGroup = Settings.weatherClips[i].audioMixerGroup;
                }
            }

            //Tunder
            for(int i = 0; i < Settings.thunderClips.Count; i++)
            {
                if(Settings.thunderClips[i].myAudioSource != null)
                    DestroyImmediate(Settings.thunderClips[i].myAudioSource.gameObject);

                GameObject sys;
                  
                if(Settings.thunderClips[i].audioClip != null)
                {
                   sys = new GameObject();
                   sys.name = "Thunder - " + Settings.thunderClips[i].name;
                   sys.transform.SetParent(EnviroManager.instance.Objects.audio.transform);             
                   Settings.thunderClips[i].myAudioSource = sys.AddComponent<AudioSource>();
                   Settings.thunderClips[i].myAudioSource.clip = Settings.thunderClips[i].audioClip;
                   Settings.thunderClips[i].myAudioSource.loop = false;
                   Settings.thunderClips[i].myAudioSource.playOnAwake = false;
                   Settings.thunderClips[i].myAudioSource.volume = Settings.thunderClips[i].volume;
                   Settings.thunderClips[i].myAudioSource.outputAudioMixerGroup = Settings.thunderClips[i].audioMixerGroup;
                }
            }
        } 

        //Plays random thunder SFX audio.
        public void PlayRandomThunderSFX()
        {
            int thunderSFX = UnityEngine.Random.Range(0,Settings.thunderClips.Count);

            if(Settings.thunderClips[thunderSFX] != null)
            {
                Settings.thunderClips[thunderSFX].myAudioSource.volume = Settings.thunderClips[thunderSFX].volume * Settings.thunderMasterVolume + thunderVolumeModifier;
                Settings.thunderClips[thunderSFX].myAudioSource.PlayOneShot(Settings.thunderClips[thunderSFX].myAudioSource.clip);
            } 
        }

        public void UpdateAudio()
        {
            for(int i = 0; i < Settings.ambientClips.Count; i++)
            {
                UpdateEnviroAudioClip(Settings.ambientClips[i],Settings.ambientMasterVolume + ambientVolumeModifier);
            }

            for(int i = 0; i < Settings.weatherClips.Count; i++)
            {
                UpdateEnviroAudioClip(Settings.weatherClips[i],Settings.weatherMasterVolume + weatherVolumeModifier);
            }
        } 

        void UpdateEnviroAudioClip(EnviroAudioClip clip, float masterVolume)
        { 
            if(clip.audioClip != null && clip.myAudioSource != null)
            {  
                if(!Application.isPlaying)
                {
                    clip.myAudioSource.Stop();
                     return;
                } 
 
                clip.myAudioSource.loop = clip.loop;

                switch (clip.playBackType) 
                {
                    case EnviroAudioClip.PlayBackType.Always:
                    clip.myAudioSource.volume = clip.volume * masterVolume;
                    break;

                    case EnviroAudioClip.PlayBackType.BasedOnSun:
                    clip.myAudioSource.volume = clip.volumeCurve.Evaluate(EnviroManager.instance.solarTime);
                    clip.myAudioSource.volume *= clip.volume * masterVolume;
                    break;

                    case EnviroAudioClip.PlayBackType.BasedOnMoon:
                    clip.myAudioSource.volume = clip.volumeCurve.Evaluate(EnviroManager.instance.lunarTime);
                    clip.myAudioSource.volume *= clip.volume * masterVolume;
                    break;
                }
               
                //Enable or disable playback based on volume
                if(clip.myAudioSource.volume < 0.001f && clip.myAudioSource.isPlaying)
                    clip.myAudioSource.Stop();

                if(clip.myAudioSource.volume > 0f && !clip.myAudioSource.isPlaying)
                    clip.myAudioSource.Play();
            }
        }

        //Save and Load
        public void LoadModuleValues ()
        {
            if(preset != null)
            {
                Settings = JsonUtility.FromJson<Enviro.EnviroAudio>(JsonUtility.ToJson(preset.Settings));
            }
            else
            {
                Debug.Log("Please assign a saved module to load from!");
            }
        } 

        public void SaveModuleValues ()
        {
#if UNITY_EDITOR
        EnviroAudioModule t =  ScriptableObject.CreateInstance<EnviroAudioModule>();
        t.name = "Audio Module";
        t.Settings = JsonUtility.FromJson<Enviro.EnviroAudio>(JsonUtility.ToJson(Settings));
 
        string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(EnviroHelper.assetPath + "/New " + t.name + ".asset");
        UnityEditor.AssetDatabase.CreateAsset(t, assetPathAndName);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public void SaveModuleValues (EnviroAudioModule module)
        {
            module.Settings = JsonUtility.FromJson<Enviro.EnviroAudio>(JsonUtility.ToJson(Settings));
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(module);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }
    }
}