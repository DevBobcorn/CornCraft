// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// プリセットに関するユーティリティ
    /// </summary>
    public static class ClothPresetUtility
    {
        const string prefix = "MC2_Preset";
        const string configName = "MC2 preset folder";

        public static void DrawPresetButton(MagicaCloth cloth, ClothSerializeData sdata)
        {
            using (var horizontalScope = new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);

                GUI.backgroundColor = Color.green;
                if (EditorGUILayout.DropdownButton(new GUIContent("Preset"), FocusType.Keyboard, GUILayout.Width(70), GUILayout.Height(16)))
                {
                    CreatePresetPopupMenu(cloth, sdata);
                    GUI.backgroundColor = Color.white;
                    //GUIUtility.ExitGUI();
                }
                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("Save", GUILayout.Width(40), GUILayout.Height(16)))
                {
                    SavePreset(sdata);
                    //GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Load", GUILayout.Width(40), GUILayout.Height(16)))
                {
                    LoadPreset(cloth, sdata);
                    //GUIUtility.ExitGUI();
                }
            }
        }

        static string GetComponentTypeName(ClothSerializeData sdata)
        {
            //if (sdata.clothType == ClothProcess.ClothType.BoneCloth)
            //    return prefix + "BoneCloth";
            //else if (sdata.clothType == ClothProcess.ClothType.MeshCloth)
            //    return prefix + "MeshCloth";
            //return prefix;

            return prefix;
        }


        class PresetInfo
        {
            public string presetPath;
            public string presetName;
            public TextAsset text;
        }

        private static void CreatePresetPopupMenu(MagicaCloth cloth, ClothSerializeData sdata)
        {
            var guidArray = AssetDatabase.FindAssets($"{prefix} t:{nameof(TextAsset)}");
            if (guidArray == null)
                return;

            Dictionary<string, List<PresetInfo>> dict = new Dictionary<string, List<PresetInfo>>();
            foreach (var guid in guidArray)
            {
                var filePath = AssetDatabase.GUIDToAssetPath(guid);

                // json確認
                if (filePath.EndsWith(".json") == false)
                    continue;

                var text = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
                if (text)
                {
                    var info = new PresetInfo();
                    info.presetPath = filePath;
                    var fname = Path.GetFileNameWithoutExtension(filePath);
                    fname = fname.Replace(prefix, "");
                    if (fname.StartsWith("_"))
                        fname = fname.Remove(0, 1); // 頭の_は削除する
                    info.presetName = fname;
                    info.text = text;

                    // ディレクトリごとに記録する
                    var dirName = Path.GetDirectoryName(filePath);
                    if (dict.ContainsKey(dirName) == false)
                    {
                        dict.Add(dirName, new List<PresetInfo>());
                    }
                    dict[dirName].Add(info);
                }
            }

            // ポップアップメニューの作成
            // ディレクトリごとにセパレータで分けて表示する
            var menu = new GenericMenu();
            int line = 0;
            foreach (var kv in dict)
            {
                if (line > 0)
                {
                    menu.AddSeparator("");
                }
                foreach (var info in kv.Value)
                {
                    var textAsset = info.text;
                    var presetName = info.presetName;
                    var presetPath = info.presetPath;
                    menu.AddItem(new GUIContent(presetName), false, () =>
                    {
                        // load
                        Develop.Log("Load preset file:" + presetPath);
                        if (sdata.ImportJson(textAsset.text))
                            Develop.Log("Completed.");
                        else
                            Develop.LogError("Preset load error!");

                        LoadPresetFinish(cloth);
                    });
                }
                line++;
            }
            menu.ShowAsContext();
        }

        /// <summary>
        /// プリセットファイル保存
        /// </summary>
        /// <param name="clothParam"></param>
        private static void SavePreset(ClothSerializeData sdata)
        {
            // フォルダを読み込み
            string folder = EditorUserSettings.GetConfigValue(configName);

            // 接頭語
            string presetTypeName = GetComponentTypeName(sdata);

            // 保存ダイアログ
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Preset",
                $"{presetTypeName}_(name)",
                "json",
                "Enter a name for the preset json.",
                folder
                );
            if (string.IsNullOrEmpty(path))
                return;

            // フォルダを記録
            folder = Path.GetDirectoryName(path);
            EditorUserSettings.SetConfigValue(configName, folder);

            Develop.Log("Save preset file:" + path);

            // export json
            string json = sdata.ExportJson();

            // save
            File.WriteAllText(path, json);

            AssetDatabase.Refresh();

            Develop.Log("Completed.");
        }

        /// <summary>
        /// プリセットファイル読み込み
        /// </summary>
        /// <param name="clothParam"></param>
        private static void LoadPreset(MagicaCloth cloth, ClothSerializeData sdata)
        {
            // フォルダを読み込み
            string folder = EditorUserSettings.GetConfigValue(configName);

            // 読み込みダイアログ
            string path = EditorUtility.OpenFilePanel("Load Preset", folder, "json");
            if (string.IsNullOrEmpty(path))
                return;

            // フォルダを記録
            folder = Path.GetDirectoryName(path);
            EditorUserSettings.SetConfigValue(configName, folder);

            // import json
            Develop.Log("Load preset file:" + path);
            string json = File.ReadAllText(path);

            // load
            if (sdata.ImportJson(json))
                Develop.Log("Completed.");
            else
                Develop.LogError("Preset load error!");

            LoadPresetFinish(cloth);
        }

        /// <summary>
        /// プリセットファイル読み込み後処理
        /// </summary>
        /// <param name="cloth"></param>
        private static void LoadPresetFinish(MagicaCloth cloth)
        {
            if (EditorApplication.isPlaying)
            {
                // パラメータ更新通知
                cloth.SetParameterChange();
            }
            else
            {
                // シリアライズ変更通知
                EditorUtility.SetDirty(cloth);
            }
        }
    }
}
