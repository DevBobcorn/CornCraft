using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Enviro
{

 [CustomEditor(typeof(EnviroModule))] 
 public class EnviroModuleEditor : EnviroBaseInspector
 { 
    public SerializedProperty preset;
 
    public virtual void OnEnable() 
    {
        //SetupGUIStyles (); 
    }

    public void SetActiveGUIColor(bool active)
    {
        if(active)
        GUI.backgroundColor = new Color(1f,1f,2f,1f);
    }

    public void UnsetActiveGUIColor()
    {
        GUI.backgroundColor = Color.white;
    }
    
    public void DisableInputStart()
    {
        if(EnviroManager.instance != null && EnviroManager.instance.Weather != null)
        {
            if(EnviroManager.instance.Weather.targetWeatherType != null)
                EditorGUI.BeginDisabledGroup(true);
        }
    }

    public void DisableInputEnd()
    {
        EditorGUI.EndDisabledGroup();
    }

    public void DisableInputStartQuality()
    {
        if(EnviroManager.instance != null && EnviroManager.instance.Quality != null)
        {
            if(EnviroManager.instance.Quality.Settings.defaultQuality != null)
                EditorGUI.BeginDisabledGroup(true);
        }
    }

    public void DisableInputEndQuality()
    {
        EditorGUI.EndDisabledGroup();
    }

    public override void OnInspectorGUI()
    {
         SetupGUIStyles (); 
    }
 }
}
