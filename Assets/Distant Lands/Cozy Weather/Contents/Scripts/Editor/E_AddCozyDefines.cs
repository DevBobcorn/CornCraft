using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class E_AddCozyDefines : Editor
{

    /// <summary>
    /// Symbols that will be added to the editor
    /// </summary>
    public static readonly string[] Symbols = new string[] {
        "DISTANT_LANDS",
        "COZY_WEATHER"
    };

    /// <summary>
    /// Add define symbols as soon as Unity gets done compiling.
    /// </summary>
    static E_AddCozyDefines()
    {
        string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        List<string> allDefines = definesString.Split(';').ToList();
        allDefines.AddRange(Symbols.Except(allDefines));
        PlayerSettings.SetScriptingDefineSymbolsForGroup(
            EditorUserBuildSettings.selectedBuildTargetGroup,
            string.Join(";", allDefines.ToArray()));


    }

}