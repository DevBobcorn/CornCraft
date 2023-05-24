using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;

namespace Enviro
{
    [CustomEditor(typeof(Enviro.EnviroReflectionProbe))]
    public class EnviroReflectionProbeEditor : Editor {

        GUIStyle boxStyle;
        GUIStyle boxStyleModified;
        GUIStyle wrapStyle;
        GUIStyle wrapStyle2;
        GUIStyle clearStyle;

        Enviro.EnviroReflectionProbe myTarget;

        public bool showAudio = false;
        public bool showFog = false;
        public bool showSeason = false;
        public bool showClouds = false;
        public bool showGeneral = false;
        public bool showPostProcessing = false;
        public bool showThirdParty = false;

        private Color boxColor1;

        SerializedObject serializedObj;

        void OnEnable()
        {
            myTarget = (Enviro.EnviroReflectionProbe)target;
            serializedObj = new SerializedObject (myTarget);
            boxColor1 = new Color(0.95f, 0.95f, 0.95f,1f);
        }

        public override void OnInspectorGUI ()
        {
            myTarget = (Enviro.EnviroReflectionProbe)target;
            serializedObj.UpdateIfRequiredOrScript ();

            //Set up the box style
            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.normal.textColor = GUI.skin.label.normal.textColor;
                boxStyle.fontStyle = FontStyle.Bold;
                boxStyle.alignment = TextAnchor.UpperLeft;
            }

            if (boxStyleModified == null)
            {
                boxStyleModified = new GUIStyle(EditorStyles.helpBox);
                boxStyleModified.normal.textColor = GUI.skin.label.normal.textColor;
                boxStyleModified.fontStyle = FontStyle.Bold;
                boxStyleModified.fontSize = 11;
                boxStyleModified.alignment = TextAnchor.UpperLeft;
            }

            //Setup the wrap style
            if (wrapStyle == null)
            {
                wrapStyle = new GUIStyle(GUI.skin.label);
                wrapStyle.fontStyle = FontStyle.Bold;
                wrapStyle.wordWrap = true;
            }

            if (wrapStyle2 == null)
            {
                wrapStyle2 = new GUIStyle(GUI.skin.label);
                wrapStyle2.fontStyle = FontStyle.Normal;
                wrapStyle2.wordWrap = true;
            }

            if (clearStyle == null) {
                clearStyle = new GUIStyle(GUI.skin.label);
                clearStyle.normal.textColor = GUI.skin.label.normal.textColor;
                clearStyle.fontStyle = FontStyle.Bold;
                clearStyle.alignment = TextAnchor.UpperRight;
            }


            GUILayout.BeginVertical(" Enviro - Reflection Probe", boxStyle);
            GUILayout.Space(30);
            GUI.backgroundColor = boxColor1;
            GUILayout.BeginVertical("Information", boxStyleModified);
            GUI.backgroundColor = Color.white;
            GUILayout.Space(20);
            EditorGUILayout.LabelField("Use this component to update your realtime reflection probes with Enviro Sky. You also can enable the 'Custom Rendering' to have enviro effects in your reflection probes!", wrapStyle2);
            EditorGUILayout.LabelField("Please enable 'Standalone Probe' if you use this component on your own places reflection probes.", wrapStyle2);      
            GUILayout.EndVertical();
            GUI.backgroundColor = boxColor1;
            GUILayout.BeginVertical("Setup", boxStyleModified);
            GUI.backgroundColor = Color.white;
            GUILayout.Space(20);
            myTarget.standalone = EditorGUILayout.Toggle("Standalone Probe", myTarget.standalone);
        
            if (myTarget.standalone)
            {
                GUILayout.Space(10);
    #if ENVIRO_HD
                GUI.backgroundColor = boxColor1;
                GUILayout.BeginVertical("Enviro Effects Rendering", boxStyleModified);
                GUI.backgroundColor = Color.white;
                GUILayout.Space(20);
                myTarget.customRendering = EditorGUILayout.Toggle("Render Enviro Effects", myTarget.customRendering);

                if(myTarget.customRendering)
                {
                    EditorGUI.BeginChangeCheck();
                    //myTarget.useFog = EditorGUILayout.Toggle("Use Fog", myTarget.useFog);
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObj.ApplyModifiedProperties();
                    }
                }
                GUILayout.EndVertical();
    #endif
                GUI.backgroundColor = boxColor1;
                GUILayout.BeginVertical("Update Settings", boxStyleModified);
                GUI.backgroundColor = Color.white;
                GUILayout.Space(20);
            myTarget.reflectionsUpdateTreshhold = EditorGUILayout.FloatField("Update Treshold in GameTime Hours", myTarget.reflectionsUpdateTreshhold);
            if (myTarget.customRendering)
            {
                myTarget.useTimeSlicing = EditorGUILayout.Toggle("Use Time-Slicing", myTarget.useTimeSlicing);
            }
            GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
            // END
            EditorGUILayout.EndVertical ();
            EditorUtility.SetDirty (target);
        }
    }
}