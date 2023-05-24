using System.Collections;
using System.Collections.Generic; 
using UnityEngine;
using System;

namespace Enviro
{
    [Serializable]
    public class EnviroLightning
    {
        public Enviro.Lightning prefab;
        public bool lightningStorm = false;
        [Range(1f,60f)]
        public float randomLightingDelay = 10.0f;
        [Range(0f,10000f)]
        public float randomSpawnRange = 5000.0f;
        [Range(0f,10000f)]
        public float randomTargetRange = 5000.0f;
    } 

    [Serializable]
    public class EnviroLightningModule : EnviroModule
    {  
        public Enviro.EnviroLightning Settings;
        public EnviroLightningModule preset;
        public bool showLightningControls;
        private bool spawned = false;

        // Update Method
        public override void UpdateModule ()
        { 
            if(Application.isPlaying && Settings.lightningStorm && Settings.prefab != null)
            {
                CastLightningBoltRandom();
            }
        }

        public void CastLightningBolt(Vector3 from, Vector3 to)
        {
            if(Settings.prefab != null)
            {
                Enviro.Lightning lightn = (Enviro.Lightning)Instantiate(Settings.prefab,from,Quaternion.identity);
                lightn.target = to;

                //Play Thunder SFX with delay if Audio module is used.
                if(EnviroManager.instance.Audio != null)
                {
                    EnviroManager.instance.StartCoroutine(PlayThunderSFX(0.05f));
                }
            }
            else
            {
                Debug.Log("Please assign a lightning prefab in your Enviro Ligthning module!");
            }
        }

        public void CastLightningBoltRandom()
        {
            if(!spawned)
            {
                //Calculate some random spawn and target locations.
                Vector2 circlSpawn = UnityEngine.Random.insideUnitCircle * Settings.randomSpawnRange;
                Vector2 circlTarget = UnityEngine.Random.insideUnitCircle * Settings.randomTargetRange;
                Vector3 spawnPosition = new Vector3(circlSpawn.x + EnviroManager.instance.transform.position.x,2500f,circlSpawn.y + EnviroManager.instance.transform.position.z);
                Vector3 targetPosition = new Vector3(circlTarget.x + spawnPosition.x,0f,circlTarget.y + spawnPosition.z);
                EnviroManager.instance.StartCoroutine(LightningStorm(spawnPosition,targetPosition));
            }
        } 
    
        private IEnumerator LightningStorm(Vector3 spwn, Vector3 targ)
        {
            spawned = true;
            yield return new WaitForSeconds(Settings.randomLightingDelay);
            CastLightningBolt(spwn,targ);
            spawned = false;
        }

        private IEnumerator PlayThunderSFX(float delay)
        {
            yield return new WaitForSeconds(delay);
            EnviroManager.instance.Audio.PlayRandomThunderSFX();
        }

        //Save and Load
        public void LoadModuleValues ()
        {
            if(preset != null)
            {
                Settings = JsonUtility.FromJson<Enviro.EnviroLightning>(JsonUtility.ToJson(preset.Settings));
            }
            else
            {
                Debug.Log("Please assign a saved module to load from!");
            } 
        }

        public void SaveModuleValues ()
        {
#if UNITY_EDITOR
        EnviroLightningModule t =  ScriptableObject.CreateInstance<EnviroLightningModule>();
        t.name = "Lightning Preset";
        t.Settings = JsonUtility.FromJson<Enviro.EnviroLightning>(JsonUtility.ToJson(Settings));
 
        string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(EnviroHelper.assetPath + "/New " + t.name + ".asset");
        UnityEditor.AssetDatabase.CreateAsset(t, assetPathAndName);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif
        }
        public void SaveModuleValues (EnviroLightningModule module)
        {
            module.Settings = JsonUtility.FromJson<Enviro.EnviroLightning>(JsonUtility.ToJson(Settings));

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(module);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }
    }
}