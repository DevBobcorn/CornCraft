using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Enviro
{
    [CustomEditor(typeof(EnviroAudioModule))]
    public class EnviroAudioModuleEditor : EnviroModuleEditor
    {  
        private EnviroAudioModule myTarget; 

        //Properties
      //  private SerializedProperty someProp;  
      
        //On Enable
        public override void OnEnable()
        {
            if(!target)
                return; 

            myTarget = (EnviroAudioModule)target;
            serializedObj = new SerializedObject(myTarget);
            preset = serializedObj.FindProperty("preset");
        } 
        public override void OnInspectorGUI()
        {
            if(!target)
                return; 

            base.OnInspectorGUI();

            GUI.backgroundColor = baseModuleColor;
            GUILayout.BeginVertical("",boxStyleModified);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.BeginHorizontal();
            myTarget.showModuleInspector = GUILayout.Toggle(myTarget.showModuleInspector, "Audio", headerFoldout);
            
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
            {
                EnviroManager.instance.RemoveModule(EnviroManager.ModuleType.Audio);
                DestroyImmediate(this);
                return;
            } 
            
            EditorGUILayout.EndHorizontal();
            
            if(myTarget.showModuleInspector)
            {
                serializedObj.UpdateIfRequiredOrScript ();
                EditorGUI.BeginChangeCheck(); 

                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;

                myTarget.showAudioControls = GUILayout.Toggle(myTarget.showAudioControls, "Audio Controls", headerFoldout);              
                if(myTarget.showAudioControls)
                { 
                     myTarget.Settings.ambientMasterVolume = EditorGUILayout.Slider ("Ambient Master Volume", myTarget.Settings.ambientMasterVolume,0f,1f);
                     myTarget.Settings.weatherMasterVolume = EditorGUILayout.Slider ("Weather Master Volume", myTarget.Settings.weatherMasterVolume,0f,1f);
                     myTarget.Settings.thunderMasterVolume = EditorGUILayout.Slider ("Thunder Master Volume", myTarget.Settings.thunderMasterVolume,0f,1f);
                }
                GUILayout.EndVertical ();

                //Ambient Clips Setup
                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;

                myTarget.showAmbientSetupControls = GUILayout.Toggle(myTarget.showAmbientSetupControls, "Ambient Sounds", headerFoldout);              
                if(myTarget.showAmbientSetupControls)
                {
                    GUILayout.Space(10);
                    if (!Application.isPlaying) 
                    {
                        if (GUILayout.Button ("Add")) 
                        {
                            myTarget.Settings.ambientClips.Add (new EnviroAudioClip ());
                        }
                    } 
                    else
                        EditorGUILayout.LabelField ("Can't add effects in runtime!");

                    if (GUILayout.Button ("Apply Changes")) 
                    {
                        myTarget.CreateAudio();
                    }
 
                    GUILayout.Space(10);
                    
                    for (int i = 0; i < myTarget.Settings.ambientClips.Count; i++) 
                    {      
                        GUILayout.BeginVertical ("", boxStyleModified);
                        EditorGUILayout.BeginHorizontal();
                        myTarget.Settings.ambientClips[i].showEditor = GUILayout.Toggle(myTarget.Settings.ambientClips[i].showEditor, myTarget.Settings.ambientClips[i].name, headerFoldout);
                        GUILayout.FlexibleSpace();
                        if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
                        { 
                            myTarget.Settings.ambientClips.Remove (myTarget.Settings.ambientClips[i]);
                            return;
                        }           
                        EditorGUILayout.EndHorizontal();

                        if(myTarget.Settings.ambientClips[i].showEditor)
                        {
                        myTarget.Settings.ambientClips[i].name = EditorGUILayout.TextField ("Audio Name", myTarget.Settings.ambientClips[i].name);
                        myTarget.Settings.ambientClips[i].audioClip = (AudioClip)EditorGUILayout.ObjectField ("Audio Clip", myTarget.Settings.ambientClips[i].audioClip, typeof(AudioClip), true);
                        myTarget.Settings.ambientClips[i].audioMixerGroup = (UnityEngine.Audio.AudioMixerGroup)EditorGUILayout.ObjectField ("Audio Mixer Group", myTarget.Settings.ambientClips[i].audioMixerGroup, typeof(UnityEngine.Audio.AudioMixerGroup), true);
                        GUILayout.Space(5);
                        myTarget.Settings.ambientClips[i].playBackType = (EnviroAudioClip.PlayBackType)EditorGUILayout.EnumPopup("Playback Type", myTarget.Settings.ambientClips[i].playBackType);
                        
                        if(myTarget.Settings.ambientClips[i].playBackType == EnviroAudioClip.PlayBackType.BasedOnSun || myTarget.Settings.ambientClips[i].playBackType == EnviroAudioClip.PlayBackType.BasedOnMoon)
                           {
                           myTarget.Settings.ambientClips[i].volumeCurve = EditorGUILayout.CurveField ("Volume", myTarget.Settings.ambientClips [i].volumeCurve);
                           myTarget.Settings.ambientClips[i].volume = EditorGUILayout.Slider ("Volume Modifier", myTarget.Settings.ambientClips [i].volume,0f,1f);
                           }
                        else
                           myTarget.Settings.ambientClips[i].volume = EditorGUILayout.Slider ("Volume", myTarget.Settings.ambientClips [i].volume,0f,1f);
                        
                        myTarget.Settings.ambientClips[i].loop = EditorGUILayout.Toggle("Loop",myTarget.Settings.ambientClips[i].loop);
                             
                        } 
                        GUILayout.EndVertical ();
                    }

                }
                GUILayout.EndVertical();

                //Weather sounds
                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                
                myTarget.showWeatherSetupControls = GUILayout.Toggle(myTarget.showWeatherSetupControls, "Weather Sounds", headerFoldout);              
                if(myTarget.showWeatherSetupControls)
                {
                    GUILayout.Space(10);
                    if (!Application.isPlaying) 
                    {
                        if (GUILayout.Button ("Add")) 
                        {
                            myTarget.Settings.weatherClips.Add (new EnviroAudioClip ());
                        }
                    } 
                    else
                        EditorGUILayout.LabelField ("Can't add effects in runtime!");

                    if (GUILayout.Button ("Apply Changes")) 
                    {
                        myTarget.CreateAudio();
                    }

                    GUILayout.Space(10);
                    
                    for (int i = 0; i < myTarget.Settings.weatherClips.Count; i++) 
                    {        
                        GUILayout.BeginVertical ("", boxStyleModified);
                        EditorGUILayout.BeginHorizontal();
                        myTarget.Settings.weatherClips[i].showEditor = GUILayout.Toggle(myTarget.Settings.weatherClips[i].showEditor, myTarget.Settings.weatherClips[i].name, headerFoldout);
                        GUILayout.FlexibleSpace();
                        if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
                        { 
                            myTarget.Settings.weatherClips.Remove (myTarget.Settings.weatherClips[i]);
                            return;
                        }           
                        EditorGUILayout.EndHorizontal();

                        if(myTarget.Settings.weatherClips[i].showEditor)
                        {
                        myTarget.Settings.weatherClips[i].name = EditorGUILayout.TextField ("Audio Name", myTarget.Settings.weatherClips[i].name);
                        myTarget.Settings.weatherClips[i].audioClip = (AudioClip)EditorGUILayout.ObjectField ("Audio Clip", myTarget.Settings.weatherClips[i].audioClip, typeof(AudioClip), true);
                        myTarget.Settings.weatherClips[i].audioMixerGroup = (UnityEngine.Audio.AudioMixerGroup)EditorGUILayout.ObjectField ("Audio Mixer Group", myTarget.Settings.weatherClips[i].audioMixerGroup, typeof(UnityEngine.Audio.AudioMixerGroup), true);
                        GUILayout.Space(5); 
                        myTarget.Settings.weatherClips[i].playBackType = (EnviroAudioClip.PlayBackType)EditorGUILayout.EnumPopup("Playback Type", myTarget.Settings.weatherClips[i].playBackType);
                        
                       if(myTarget.Settings.weatherClips[i].playBackType == EnviroAudioClip.PlayBackType.BasedOnSun || myTarget.Settings.weatherClips[i].playBackType == EnviroAudioClip.PlayBackType.BasedOnMoon)
                           {
                           myTarget.Settings.weatherClips[i].volumeCurve = EditorGUILayout.CurveField ("Volume", myTarget.Settings.weatherClips [i].volumeCurve);
                           myTarget.Settings.weatherClips[i].volume = EditorGUILayout.Slider ("Volume Modifier", myTarget.Settings.weatherClips [i].volume,0f,1f);
                           }
                        else
                           myTarget.Settings.weatherClips[i].volume = EditorGUILayout.Slider ("Volume", myTarget.Settings.weatherClips [i].volume,0f,1f);
                        
                        myTarget.Settings.weatherClips[i].loop = EditorGUILayout.Toggle("Loop",myTarget.Settings.weatherClips[i].loop);
                             
                        } 
                        GUILayout.EndVertical ();
                    }

                }
                GUILayout.EndVertical();

                //Thunder sounds
                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                
                myTarget.showThunderSetupControls = GUILayout.Toggle(myTarget.showThunderSetupControls, "Thunder Sounds", headerFoldout);              
                if(myTarget.showThunderSetupControls)
                {
                    GUILayout.Space(10);
                    if (!Application.isPlaying) 
                    {
                        if (GUILayout.Button ("Add")) 
                        {
                            myTarget.Settings.thunderClips.Add (new EnviroAudioClip ());
                        }
                    } 
                    else
                        EditorGUILayout.LabelField ("Can't add effects in runtime!");

                    if (GUILayout.Button ("Apply Changes")) 
                    {
                        myTarget.CreateAudio();
                    }

                    GUILayout.Space(10);
                    
                    for (int i = 0; i < myTarget.Settings.thunderClips.Count; i++) 
                    {         
                        GUILayout.BeginVertical ("", boxStyleModified);
                        EditorGUILayout.BeginHorizontal();
                        myTarget.Settings.thunderClips[i].showEditor = GUILayout.Toggle(myTarget.Settings.thunderClips[i].showEditor, myTarget.Settings.thunderClips[i].name, headerFoldout);
                        myTarget.Settings.thunderClips[i].audioMixerGroup = (UnityEngine.Audio.AudioMixerGroup)EditorGUILayout.ObjectField ("Audio Mixer Group", myTarget.Settings.thunderClips[i].audioMixerGroup, typeof(UnityEngine.Audio.AudioMixerGroup), true);
                        GUILayout.FlexibleSpace();
                        if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
                        { 
                            myTarget.Settings.thunderClips.Remove (myTarget.Settings.thunderClips[i]);
                            return;
                        }           
                        EditorGUILayout.EndHorizontal();

                        if(myTarget.Settings.thunderClips[i].showEditor)
                        {
                        myTarget.Settings.thunderClips[i].name = EditorGUILayout.TextField ("Audio Name", myTarget.Settings.thunderClips[i].name);
                        myTarget.Settings.thunderClips[i].audioClip = (AudioClip)EditorGUILayout.ObjectField ("Audio Clip", myTarget.Settings.thunderClips[i].audioClip, typeof(AudioClip), true);
                        GUILayout.Space(5);                   
                        myTarget.Settings.thunderClips[i].volume = EditorGUILayout.Slider ("Volume", myTarget.Settings.thunderClips [i].volume,0f,1f);                            
                        } 
                        GUILayout.EndVertical ();
                    }

                }
                GUILayout.EndVertical();
              
               /// Save Load
                GUI.backgroundColor = categoryModuleColor;
                GUILayout.BeginVertical("",boxStyleModified);
                GUI.backgroundColor = Color.white;
                myTarget.showSaveLoad = GUILayout.Toggle(myTarget.showSaveLoad, "Save/Load", headerFoldout);
                
                if(myTarget.showSaveLoad)
                {
                    EditorGUILayout.PropertyField(preset);

                    GUILayout.BeginHorizontal("",wrapStyle);

                    if(myTarget.preset != null)
                    {
                        if(GUILayout.Button("Load"))
                        {
                            myTarget.LoadModuleValues();
                        }
                        if(GUILayout.Button("Save"))
                        {
                            myTarget.SaveModuleValues(myTarget.preset);
                        }
                    }
                    if(GUILayout.Button("Save As New"))
                    {
                        myTarget.SaveModuleValues();
                    }
                    GUILayout.EndHorizontal();

    
                }
                GUILayout.EndVertical();
                /// Save Load End

                ApplyChanges ();
            }
            GUILayout.EndVertical();

            if(myTarget.showModuleInspector)
             GUILayout.Space(20);
        }
    }
}
