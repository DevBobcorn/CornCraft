using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if WORLDAPI_PRESENT

namespace Enviro
{
[CustomEditor(typeof(EnviroWorldAPI))]
public class EnviroWAPIEditor : Editor {

	private GUIStyle boxStyle;
	private GUIStyle wrapStyle;
	private GUIStyle headerStyle;

	SerializedObject serializedObj;
	private EnviroWorldAPI myTarget;

	SerializedProperty snowPower, wetnessPower, fogPower, seasons, time, cloudCover, location, temperature;

	void OnEnable()
	{
		myTarget = (EnviroWorldAPI)target;
		serializedObj = new SerializedObject (myTarget);
		snowPower = serializedObj.FindProperty ("snowPower");
		wetnessPower = serializedObj.FindProperty ("wetnessPower");
        temperature = serializedObj.FindProperty("temperature");
        fogPower = serializedObj.FindProperty ("fogPower");
		//windDirection = serializedObj.FindProperty ("windDirection");
		//windSpeed = serializedObj.FindProperty ("windSpeed");
		seasons = serializedObj.FindProperty ("seasons");
		time = serializedObj.FindProperty ("time");
		cloudCover = serializedObj.FindProperty ("cloudCover");
		location = serializedObj.FindProperty ("location");
	}


	public override void OnInspectorGUI ()
	{
		if (boxStyle == null)
		{
			boxStyle = new GUIStyle(GUI.skin.box);
			boxStyle.normal.textColor = GUI.skin.label.normal.textColor;
			boxStyle.fontStyle = FontStyle.Bold;
			boxStyle.alignment = TextAnchor.UpperLeft;
		}

		if (wrapStyle == null)
		{
			wrapStyle = new GUIStyle(GUI.skin.label);
			wrapStyle.fontStyle = FontStyle.Normal;
			wrapStyle.wordWrap = true;
		}

		if (headerStyle == null)
		{
			headerStyle = new GUIStyle(GUI.skin.label);
			headerStyle.fontStyle = FontStyle.Bold;
			headerStyle.wordWrap = true;
		}

		EditorGUI.BeginChangeCheck ();
		GUILayout.BeginVertical("Enviro 3 - WAPI Integration", boxStyle);
		GUILayout.Space(20);
		EditorGUILayout.LabelField("Welcome to the World Manager Integration for Enviro 3 - Sky and Weather!", wrapStyle);
		GUILayout.EndVertical ();
		GUILayout.BeginVertical("Controls", boxStyle);
		GUILayout.Space(20);
		GUILayout.BeginVertical("Time, Season and Location", boxStyle);
		GUILayout.Space(20);
		EditorGUILayout.PropertyField (time, true, null);
		EditorGUILayout.PropertyField (location, true, null);
		EditorGUILayout.PropertyField (seasons, true, null);
		GUILayout.EndVertical ();
		GUILayout.BeginVertical("Weather", boxStyle);
		GUILayout.Space(20);
		EditorGUILayout.LabelField("Enviro will change weather when using GetFromWAPI mode here to match WAPI values!", wrapStyle);
		EditorGUI.indentLevel++;
		EditorGUILayout.PropertyField (cloudCover, true, null);
		EditorGUILayout.PropertyField (snowPower, true, null);
		EditorGUILayout.PropertyField (wetnessPower, true, null);
        EditorGUILayout.PropertyField (temperature, true, null);
        EditorGUI.indentLevel--;
		GUILayout.Space(10);
		//GUILayout.Label ("Wind",headerStyle);
		//EditorGUI.indentLevel++;
		//EditorGUILayout.PropertyField (windSpeed, true, null);
		//EditorGUILayout.PropertyField (windDirection, true, null);
		//EditorGUI.indentLevel--;
		GUILayout.Label ("Fog",headerStyle);
		EditorGUI.indentLevel++;
		EditorGUILayout.PropertyField (fogPower, true, null);
		EditorGUI.indentLevel--; 
		GUILayout.EndVertical (); 
		GUILayout.EndVertical ();
		if (EditorGUI.EndChangeCheck ()) {
			serializedObj.ApplyModifiedProperties ();
		}
}
}
}
#endif