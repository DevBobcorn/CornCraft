// Distant Lands 2022.



using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DistantLands.Cozy.Data;

namespace DistantLands.Cozy.EditorScripts
{
    public class E_CozyEditor : EditorWindow
    {


        public Texture titleWindow;
        public Vector2 scrollPos;

        public CozyWeather headUnit;
        public Editor editor;

        [MenuItem("Distant Lands/Cozy/Open Cozy Editor", false, 0)]
        static void Init()
        {

            E_CozyEditor window = (E_CozyEditor)EditorWindow.GetWindow(typeof(E_CozyEditor), false, "COZY: Weather");
            window.minSize = new Vector2(400, 500);
            window.Show();

        }


        private void OnGUI()
        {



            GUI.DrawTexture(new Rect(0, 0, position.width, position.width * 1 / 3), titleWindow);
            EditorGUILayout.Space(position.width * 1 / 3);
            EditorGUILayout.Space(10);
            EditorGUILayout.Separator();
            EditorGUI.indentLevel = 1;

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            if (headUnit == null)
            {
                if (CozyWeather.instance)
                {
                    headUnit = CozyWeather.instance;
                    editor = Editor.CreateEditor(headUnit);

                }
                else
                {

                    if (GUILayout.Button("Setup COZY"))
                        E_CozyMenuItems.CozySetupScene();


                    EditorGUILayout.EndScrollView();
                    return;

                }
            }

            if (editor)
                editor.OnInspectorGUI();
            else if (headUnit)
                editor = Editor.CreateEditor(headUnit);


            EditorGUILayout.EndScrollView();

        }

        public static List<T> GetAssets<T>(string[] _foldersToSearch, string _filter) where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets(_filter, _foldersToSearch);
            List<T> a = new List<T>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                a.Add(AssetDatabase.LoadAssetAtPath<T>(path));
            }
            return a;
        }

    }
}