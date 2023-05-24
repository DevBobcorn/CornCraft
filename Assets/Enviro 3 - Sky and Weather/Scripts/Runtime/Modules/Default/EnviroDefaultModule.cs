using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Enviro
{
    [Serializable]
    public class EnviroDefault
    {
 
    } 

    [Serializable]
    public class EnviroDefaultModule : EnviroModule
    {  
        public Enviro.EnviroDefault settings;
        public EnviroDefaultModule preset;
        public bool showDefaultControls;

        
        // Update Method
        public override void UpdateModule ()
        { 
  
        }

        //Save and Load
        public void LoadModuleValues ()
        {
            if(preset != null)
            {
                settings = JsonUtility.FromJson<Enviro.EnviroDefault>(JsonUtility.ToJson(preset.settings));
            }
            else
            {
                Debug.Log("Please assign a saved module to load from!");
            }
        }

        public void SaveModuleValues ()
        {
#if UNITY_EDITOR 
        EnviroDefaultModule t =  ScriptableObject.CreateInstance<EnviroDefaultModule>();
        t.name = "Default Preset";
        t.settings = JsonUtility.FromJson<Enviro.EnviroDefault>(JsonUtility.ToJson(settings));
 
        string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(EnviroHelper.assetPath + "/New " + t.name + ".asset");
        UnityEditor.AssetDatabase.CreateAsset(t, assetPathAndName);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif
        }
        public void SaveModuleValues (EnviroDefaultModule module)
        {
            module.settings = JsonUtility.FromJson<Enviro.EnviroDefault>(JsonUtility.ToJson(settings));

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(module);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }
    }
}