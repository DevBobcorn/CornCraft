using System;
using System.Collections;
using System.Collections.Generic;
using Codice.Client.BaseCommands;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.Rendering.FernRenderPipeline
{
    public class FernRPGUIUtility : Editor
    {
        public static void DrawActionBox(string text, string label, MessageType messageType, Action action)
        {
            Assert.IsNotNull(action);

            EditorGUILayout.HelpBox(text, messageType);

            GUILayout.Space(-32);
            EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(label, GUILayout.Width(60)))
                    action();

                GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(11);
        }
        
        public static void DrawWarning(string text, MessageType messageType)
        {
            EditorGUILayout.HelpBox(text, messageType);

            EditorGUILayout.Space(-32);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                EditorGUILayout.Space(8);
            }
            EditorGUILayout.Space(14);
        }
    }
}

