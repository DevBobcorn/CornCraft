// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// ClothParamクラスのプリセットに関するユーティリティ
    /// </summary>
    public static class EditorPresetUtility
    {
        const string configName = "preset folder";

        public static void DrawPresetButton(MonoBehaviour owner, ClothParams clothParam)
        {
            using (var horizontalScope = new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);

                GUI.backgroundColor = Color.green;
                if (EditorGUILayout.DropdownButton(new GUIContent("Preset"), FocusType.Keyboard, GUILayout.Width(70), GUILayout.Height(16)))
                {
                    CreatePresetPopupMenu(owner, clothParam);
                    GUI.backgroundColor = Color.white;
                    GUIUtility.ExitGUI();
                }
                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("Save", GUILayout.Width(40), GUILayout.Height(16)))
                {
                    SavePreset(owner, clothParam);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Load", GUILayout.Width(40), GUILayout.Height(16)))
                {
                    LoadPreset(owner, clothParam);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private static string GetComponentTypeName(MonoBehaviour owner)
        {
            string componentTypeName = string.Empty;
            if (owner is MagicaBoneCloth)
                componentTypeName = "BoneCloth";
            else if (owner is MagicaBoneSpring)
                componentTypeName = "BoneSpring";
            else if (owner is MagicaMeshCloth)
                componentTypeName = "MeshCloth";
            else if (owner is MagicaMeshSpring)
                componentTypeName = "MeshSpring";

            return componentTypeName;
        }


        private class PresetInfo
        {
            public string presetPath;
            public string presetName;
            public TextAsset text;
        }

        private static void CreatePresetPopupMenu(MonoBehaviour owner, ClothParams clothParam)
        {
            // コンポーネントにより検索するプリセット名を変更する
            string presetTypeName = GetComponentTypeName(owner);
            if (string.IsNullOrEmpty(presetTypeName))
                return;

            var guidArray = AssetDatabase.FindAssets($"{presetTypeName} t:" + nameof(TextAsset));
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
                    fname = fname.Replace(presetTypeName, "");
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
                        var json = textAsset.text;
                        //Debug.Log(json);

                        // load
                        Debug.Log("Load preset file:" + presetPath);
                        LoadClothParam(owner, clothParam, json);
                        Debug.Log("Complete.");
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
        private static void SavePreset(MonoBehaviour owner, ClothParams clothParam)
        {
            // フォルダを読み込み
            string folder = EditorUserSettings.GetConfigValue(configName);

            // 接頭語
            string presetTypeName = GetComponentTypeName(owner);

            // 保存ダイアログ
            string path = UnityEditor.EditorUtility.SaveFilePanelInProject(
                "Save Preset",
                $"{presetTypeName}_xxx",
                "json",
                "Enter a name for the preset json.",
                folder
                );
            if (string.IsNullOrEmpty(path))
                return;

            // フォルダを記録
            folder = Path.GetDirectoryName(path);
            EditorUserSettings.SetConfigValue(configName, folder);

            Debug.Log("Save preset file:" + path);

            // json
            string json = JsonUtility.ToJson(clothParam);

            // save
            File.WriteAllText(path, json);

            AssetDatabase.Refresh();

            Debug.Log("Complete.");
        }

        /// <summary>
        /// プリセットファイル読み込み
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="clothParam"></param>
        private static void LoadPreset(MonoBehaviour owner, ClothParams clothParam)
        {
            // フォルダを読み込み
            string folder = EditorUserSettings.GetConfigValue(configName);

            // 読み込みダイアログ
            string path = UnityEditor.EditorUtility.OpenFilePanel("Load Preset", folder, "json");
            if (string.IsNullOrEmpty(path))
                return;

            // フォルダを記録
            folder = Path.GetDirectoryName(path);
            EditorUserSettings.SetConfigValue(configName, folder);

            // json
            Debug.Log("Load preset file:" + path);
            string json = File.ReadAllText(path);

            // load
            LoadClothParam(owner, clothParam, json);

            Debug.Log("Complete.");
        }

        /// <summary>
        /// jsonテキストからパラメータを復元する
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="clothParam"></param>
        /// <param name="json"></param>
        private static void LoadClothParam(MonoBehaviour owner, ClothParams clothParam, string json)
        {
            if (string.IsNullOrEmpty(json) == false)
            {
                // 上書きしないプロパティを保持
                Transform influenceTarget = clothParam.GetInfluenceTarget();
                Transform disableReferenceObject = clothParam.DisableReferenceObject;
                //Transform directionalDampingObject = clothParam.DirectionalDampingObject;

                // undo
                Undo.RecordObject(owner, "Load preset");

                JsonUtility.FromJsonOverwrite(json, clothParam);

                // 上書きしないプロパティを書き戻し
                clothParam.SetInfluenceTarget(influenceTarget);
                clothParam.DisableReferenceObject = disableReferenceObject;
                //clothParam.DirectionalDampingObject = directionalDampingObject;
            }
        }
    }
}
