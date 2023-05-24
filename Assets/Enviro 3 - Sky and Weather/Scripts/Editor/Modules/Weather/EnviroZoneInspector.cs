using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Enviro
{
    [CustomEditor(typeof(EnviroZone))]
    public class EnviroZoneInspector : EnviroBaseInspector
    {
        private EnviroZone myTarget;

        private SerializedProperty autoWeatherChanges, weatherChangeIntervall, zoneScale, zoneGizmoColor;

        void OnEnable()
        {
            myTarget = (EnviroZone)target;
            serializedObj = new SerializedObject(myTarget);
            autoWeatherChanges = serializedObj.FindProperty("autoWeatherChanges"); 
            weatherChangeIntervall = serializedObj.FindProperty("weatherChangeIntervall"); 
            zoneScale = serializedObj.FindProperty("zoneScale"); 
            zoneGizmoColor = serializedObj.FindProperty("zoneGizmoColor"); 
        }

        public override void OnInspectorGUI()
        {
            SetupGUIStyles();

            GUILayout.BeginVertical("", boxStyle);
            GUILayout.Label("Enviro Weather Zone",headerStyleMid); 

            //Help Box Button
            //RenderHelpBoxButton();
            // if(showHelpBox)
            // RenderHelpBox("This is a help text test!");

            GUILayout.EndVertical();
            serializedObj.UpdateIfRequiredOrScript ();
            EditorGUI.BeginChangeCheck();


            GUILayout.BeginVertical("", boxStyle);
            GUILayout.Label("Zone Setup",headerStyleMid); 
            GUILayout.BeginVertical("",boxStyleModified);
            EditorGUILayout.PropertyField(zoneScale);
            EditorGUILayout.PropertyField(zoneGizmoColor);
            GUILayout.EndVertical ();
            GUILayout.EndVertical ();


            GUILayout.BeginVertical("", boxStyle);
            GUILayout.Label("Weather Setup",headerStyleMid); 
            GUILayout.Space(5f);
            if(myTarget.currentWeatherType != null)        
            GUILayout.Label("Current Weather: " + myTarget.currentWeatherType.name,wrapStyle); 
            else
            GUILayout.Label("Current Weather: Not Set",wrapStyle); 
            GUILayout.Space(5f);
            if(myTarget.nextWeatherType != null)   
            {
            if(EnviroManager.instance != null && EnviroManager.instance.Time != null)
            {
                GUILayout.Label("Next Change in: " + (myTarget.nextWeatherUpdate - EnviroManager.instance.Time.GetDateInHours()).ToString("#.00") + " hours",wrapStyle); 
            }
            }
            else
            {
                GUILayout.Label("Next Change in: Not Set");
            }
            GUILayout.Space(5f); 
            if(myTarget.nextWeatherType != null)   
            GUILayout.Label("Next Weather: " + myTarget.nextWeatherType.name,wrapStyle);
            else
            GUILayout.Label("Next Weather: Not Set",wrapStyle);
            
            GUILayout.Space(5f);
            GUILayout.BeginVertical("", boxStyleModified);
            EditorGUILayout.PropertyField(autoWeatherChanges);
            EditorGUILayout.PropertyField(weatherChangeIntervall);
            GUILayout.EndVertical();
            GUILayout.Space(5f);      
            GUILayout.BeginVertical("",boxStyleModified);
            Object selectedObject = null;
                        
            if(GUILayout.Button("Add"))
            {
                int controlID = EditorGUIUtility.GetControlID (FocusType.Passive);
                EditorGUIUtility.ShowObjectPicker<EnviroWeatherType>(null,false,"",controlID);
            }

            string commandName = Event.current.commandName;

            if (commandName == "ObjectSelectorClosed") 
            {
                selectedObject = EditorGUIUtility.GetObjectPickerObject ();
                
                bool add = true;
                
                for (int i = 0; i < myTarget.weatherTypeList.Count; i++)
                {
                    if((EnviroWeatherType)selectedObject == myTarget.weatherTypeList[i].weatherType)
                    add = false;
                }

                if(selectedObject == null)
                    add = false;

                if(add)
                    myTarget.AddWeatherType((EnviroWeatherType)selectedObject);
            }

            GUILayout.Space(15);

            for (int i = 0; i < myTarget.weatherTypeList.Count; i++) 
                {      
                    EnviroZoneWeather curZoneWeather = myTarget.weatherTypeList[i];
                    GUILayout.BeginVertical ("", boxStyleModified);

                    EditorGUILayout.BeginHorizontal();

                    string name = "Empty";        
                    if(curZoneWeather.weatherType != null)
                        name = curZoneWeather.weatherType.name;

                    curZoneWeather.showEditor = GUILayout.Toggle(curZoneWeather.showEditor, name, headerFoldout);
                    GUILayout.FlexibleSpace();

                    if(curZoneWeather.weatherType != myTarget.currentWeatherType)
                    {
                        if(GUILayout.Button("Change Now", EditorStyles.miniButtonRight,GUILayout.Width(80), GUILayout.Height(18)))
                        {
                            myTarget.ChangeZoneWeatherInstant(curZoneWeather.weatherType);
                            //EditorUtility.SetDirty(curWT);
                        } 
                    }

                    if(GUILayout.Button("x", EditorStyles.miniButtonRight,GUILayout.Width(18), GUILayout.Height(18)))
                    {
                        myTarget.RemoveWeatherZoneType(curZoneWeather);
                    } 
                    
                    EditorGUILayout.EndHorizontal();

                    if(curZoneWeather.showEditor)
                    {
                        GUILayout.BeginVertical ("", boxStyleModified);
                        
                        curZoneWeather.probability = EditorGUILayout.Slider("Probabillity",curZoneWeather.probability,0f,100f);

                        EditorGUILayout.EndVertical ();
                    }

                    GUILayout.EndVertical ();

                }

            GUILayout.EndVertical ();
            GUILayout.EndVertical ();

            ApplyChanges();
        }
    }
}
