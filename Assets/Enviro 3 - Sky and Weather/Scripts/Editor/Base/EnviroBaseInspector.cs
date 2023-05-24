using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
using UnityEditor;
using System;
using System.Linq;

public class EnviroBaseInspector : Editor
{ 
    public SerializedObject serializedObj;
    public GUIStyle boxStyle;
    public GUIStyle boxStyleModified;
    public GUIStyle wrapStyle;
    public GUIStyle headerStyle;
    public GUIStyle headerStyleMid;
    public GUIStyle headerFoldout;
    public GUIStyle popUpStyle;
    public GUIStyle integrationBox;
    public GUIStyle helpButton; 
    public bool showHelpBox;

    public Color baseModuleColor = new Color(0.0f,0.0f,0.5f,1f);
    public Color categoryModuleColor = new Color(0.5f,0.5f,0.0f,1f);
    public Color thirdPartyModuleColor = new Color(0.0f,0.5f,0.5f,1f);

    public void SetupGUIStyles ()
    {
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

        if (integrationBox == null)
        {
            integrationBox = new GUIStyle(EditorStyles.helpBox);
            integrationBox.fontStyle = FontStyle.Bold;
            integrationBox.fontSize = 11;
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
            headerStyle.alignment = TextAnchor.UpperLeft;
        }

        if (headerStyleMid == null)
        {
            headerStyleMid = new GUIStyle(GUI.skin.label);
            headerStyleMid.fontStyle = FontStyle.Bold;
            headerStyleMid.alignment = TextAnchor.MiddleCenter;
        }

        if (headerFoldout == null)
        {
            headerFoldout = new GUIStyle(EditorStyles.foldout);
            headerFoldout.fontStyle = FontStyle.Bold;
        }

        if (popUpStyle == null)
        {
            popUpStyle = new GUIStyle(EditorStyles.popup);
            popUpStyle.alignment = TextAnchor.MiddleCenter;
            popUpStyle.fixedHeight = 20f;
            popUpStyle.fontStyle = FontStyle.Bold;
        }

        if (helpButton == null)
        {
            helpButton = new GUIStyle(EditorStyles.miniButtonRight);
            //helpButton.alignment = TextAnchor.UpperRight;
            helpButton.margin = new RectOffset(100,0,0,0);

        }
    }

    public void RenderHelpBoxButton()
    {
        //Help Box Button
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if(GUILayout.Button("?", EditorStyles.miniButton,GUILayout.Width(20), GUILayout.Height(20)))
        {
            if(showHelpBox)
                showHelpBox = false;
            else
                showHelpBox = true;
        }
        EditorGUILayout.EndHorizontal();
        //End Help Box Button
    }

    public void RenderHelpBox(string content)
    {
       // GUILayout.BeginVertical("",EditorStyles.helpBox);
        GUILayout.Label(content,EditorStyles.helpBox);
    }

     public void RenderIntegrationTextBox(string content)
    {
       // GUILayout.BeginVertical("",EditorStyles.helpBox);
        GUILayout.Label(content,integrationBox);
    }

    public void RenderDisableInputBox()
    {  
        if(Enviro.EnviroManager.instance != null)
        {
           if (Enviro.EnviroManager.instance.Weather != null && Enviro.EnviroManager.instance.Quality != null)
           {
            //both
             GUILayout.Label("Some settings are controlled from weather and quality modules!",EditorStyles.helpBox);
           }
           else if(Enviro.EnviroManager.instance.Weather != null && Enviro.EnviroManager.instance.Quality == null)
           {
            //Weather Only
            GUILayout.Label("Some settings are controlled from weather modules!",EditorStyles.helpBox);
           }
           else if(Enviro.EnviroManager.instance.Weather == null && Enviro.EnviroManager.instance.Quality != null)
           {
            // Quality Only
            GUILayout.Label("Some settings are controlled from quality modules!",EditorStyles.helpBox);
           }
           else
           {
            //Show Nothing
           }
     
        }
    }

    public void ApplyChanges ()
	{
		if (EditorGUI.EndChangeCheck ()) {
			serializedObj.ApplyModifiedProperties ();
		}
	}


    public void AddDefineSymbol(string symbol)
    {
        var targets = Enum.GetValues(typeof(BuildTargetGroup))
        .Cast<BuildTargetGroup>()
        .Where(x => x != BuildTargetGroup.Unknown)
        .Where(x => !IsObsolete(x));

        foreach (var target in targets)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target).Trim();

            var list = defines.Split(';', ' ')
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            if (list.Contains(symbol))
                continue;

            list.Add(symbol);
            defines = list.Aggregate((a, b) => a + ";" + b);

            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
        }
    }


    bool IsObsolete(BuildTargetGroup group)
    {
        var attrs = typeof(BuildTargetGroup)
            .GetField(group.ToString())
            .GetCustomAttributes(typeof(ObsoleteAttribute), false);

        return attrs != null && attrs.Length > 0;
    }

    public void RemoveDefineSymbol(string symbol)
    {
        string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

        var targets = Enum.GetValues(typeof(BuildTargetGroup))
        .Cast<BuildTargetGroup>()
        .Where(x => x != BuildTargetGroup.Unknown)
        .Where(x => !IsObsolete(x));

        foreach (var target in targets)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target).Trim();

            if (defines.Contains(symbol))
            {
                defines = defines.Replace(symbol + "; ", "");
                defines = defines.Replace(symbol, "");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
            }
        }
    }
}
