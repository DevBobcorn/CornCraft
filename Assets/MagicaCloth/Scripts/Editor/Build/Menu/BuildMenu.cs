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
    /// ビルドメニューウインドウ
    /// </summary>
    public class BuildMenu : EditorWindow
    {
        [SerializeField]
        BuildMenuData settingData = new BuildMenuData();


        //=========================================================================================
        [MenuItem("Tools/Magica Cloth/Build Menu", false)]
        public static void InitWindow()
        {
            OpenBuildMenu();
        }

        public static void OpenBuildMenu()
        {
            // 全体で１つのみ起動を許可する
            var settingMenu = EditorWindow.GetWindow<BuildMenu>(true, "Magica Cloth Build Menu", true);
            if (settingMenu != null)
            {
                settingMenu.Init();
            }
        }

        //=========================================================================================
        public void Init()
        {
            // サイズ変更不可
            maxSize = minSize = new Vector2(350, 520);

            // 設定データ
            settingData.Load();

            // 現在アセットが選択中ならば対象を選択に変更する
            //settingData.targetMode = Selection.activeObject ? 1 : 0;
        }

        void OnDestroy()
        {
            //Debug.Log("BuildMenu.OnDestroy()");
            // 念の為
            EditorUtility.ClearProgressBar();
        }

        void OnGUI()
        {
            if (settingData == null)
                return;

            GUILayout.Space(10);

            GeneralGUI();

            GUILayout.Space(10);

            using (var horizontalScope = new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(settingData.VerificationOnly ? "Verify" : "Build", GUILayout.Width(80)))
                {
                    Save();
                    Confirm();
                }
                GUILayout.Space(20);
                if (GUILayout.Button("Close", GUILayout.Width(80)))
                {
                    Save();
                    Close();
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.Space(10);
        }

        //=========================================================================================
        void GeneralGUI()
        {
            EditorGUIUtility.labelWidth = 200;

            // Target Object
            EditorGUILayout.LabelField("Target Object", EditorStyles.boldLabel);
            using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                settingData.TargetMode = GUILayout.SelectionGrid(settingData.TargetMode, new string[] { "All Assets", "Selected Assets" }, 1, new GUIStyle(EditorStyles.radioButton));
                settingData.Prefab = EditorGUILayout.Toggle("Prefab", settingData.Prefab);
                settingData.Scene = EditorGUILayout.Toggle("Scene", settingData.Scene);
            }
            GUILayout.Space(10);

            // Target Component
            EditorGUILayout.LabelField("Target Component", EditorStyles.boldLabel);
            using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                settingData.BoneCloth = EditorGUILayout.Toggle("Bone Cloth", settingData.BoneCloth);
                settingData.BoneSpring = EditorGUILayout.Toggle("Bone Spring", settingData.BoneSpring);
                settingData.MeshCloth = EditorGUILayout.Toggle("Mesh Cloth", settingData.MeshCloth);
                settingData.MeshSpring = EditorGUILayout.Toggle("Mesh Spring", settingData.MeshSpring);

                settingData.RenderDeformer = EditorGUILayout.Toggle("Render Deformer", settingData.RenderDeformer);
                settingData.VirtualDeformer = EditorGUILayout.Toggle("Virtual Deformer", settingData.VirtualDeformer);
            }
            GUILayout.Space(10);

            // Build Conditions
            EditorGUILayout.LabelField("Build Conditions", EditorStyles.boldLabel);
            using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                using (var disableScope = new EditorGUI.DisabledGroupScope(settingData.VerificationOnly))
                {
                    settingData.ForceBuild = EditorGUILayout.Toggle("Force Build", settingData.ForceBuild);
                    settingData.NotCreated = EditorGUILayout.Toggle("Not Created Or In Error", settingData.NotCreated);
                    settingData.UpgradeFormatAndAlgorithm = EditorGUILayout.Toggle("Upgrade Format And Algorithm", settingData.UpgradeFormatAndAlgorithm);
                }
                settingData.VerificationOnly = EditorGUILayout.Toggle("Verification Only (no build)", settingData.VerificationOnly);
            }
            GUILayout.Space(10);

            // Option
            EditorGUILayout.LabelField("Option", EditorStyles.boldLabel);
            using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                settingData.ErrorStop = EditorGUILayout.Toggle("Error Stop", settingData.ErrorStop);
            }
            GUILayout.FlexibleSpace();
        }

        /// <summary>
        /// 設定保存
        /// </summary>
        void Save()
        {
            settingData.Save();
        }

        //=========================================================================================
        /// <summary>
        /// 実行確認
        /// </summary>
        void Confirm()
        {
            // 対象guids
            var guidList = new List<string>();
            if (settingData.TargetMode == 0)
            {
                // All Assets
                if (settingData.Prefab)
                    guidList.AddRange(AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }));
                if (settingData.Scene)
                    guidList.AddRange(AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }));
            }
            else if (settingData.TargetMode == 1)
            {
                // Selected
                foreach (var guid in Selection.assetGUIDs)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.StartsWith("Assets"))
                    {
                        if (settingData.Prefab && Path.GetExtension(path) == ".prefab")
                            guidList.Add(guid);
                        if (settingData.Scene && Path.GetExtension(path) == ".unity")
                            guidList.Add(guid);
                    }
                }
            }

            // 実行確認
            if (guidList.Count == 0)
            {
                EditorUtility.DisplayDialog("Confirm", "Not applicable!", "OK");
            }
            else
            {
                if (EditorUtility.DisplayDialog("Confirm", $"Select [{guidList.Count}] Assets.\nDo you want to run it?\n\n<< Attention! >>\nThis operation cannot be undone.", "OK", "Cancel"))
                {
                    Build(guidList);
                    GUIUtility.ExitGUI();
                }
            }
        }

        /// <summary>
        /// ビルド開始
        /// </summary>
        void Build(List<string> guidList)
        {
            Debug.Log("Start Build.");

            // option
            var option = new BuildOptions()
            {
                includeInactive = true,
                buildBoneCloth = settingData.BoneCloth,
                buildBoneSpring = settingData.BoneSpring,
                buildMeshCloth = settingData.MeshCloth,
                buildMeshSpring = settingData.MeshSpring,
                buildRenderDeformer = settingData.RenderDeformer,
                buildVirtualDeformer = settingData.VirtualDeformer,

                forceBuild = settingData.ForceBuild,
                notCreated = settingData.NotCreated,
                upgradeFormatAndAlgorithm = settingData.UpgradeFormatAndAlgorithm,
                verificationOnly = settingData.VerificationOnly,

                errorStop = settingData.ErrorStop,
            };

            int cnt = guidList.Count;
            var totalResult = new BuildResult();
            for (int i = 0; i < cnt; i++)
            {
                // progress bar
                float progress = (float)i / cnt;
                bool isCancel = EditorUtility.DisplayCancelableProgressBar("Build", $"{i} / {cnt}", progress);
                if (isCancel)
                {
                    Debug.Log("Cancel Build!");
                    break;
                }

                var path = AssetDatabase.GUIDToAssetPath(guidList[i]);
                var result = new BuildResult();
                if (Path.GetExtension(path) == ".prefab")
                {
                    result = BuildManager.BuildFromAssetPath(path, option);
                }
                if (Path.GetExtension(path) == ".unity")
                {
                    result = BuildManager.BuildFromScenePath(path, option);
                }

                // total
                totalResult.Merge(result);

                if (result.IsError())
                {
                    Debug.LogError($"<color=red>Failed!</color> [{path}]\n{result.GetErrorMessage()}");
                    if (option.errorStop)
                    {
                        Debug.Log("Error Stop!");
                        break;
                    }
                }
            }

            EditorUtility.ClearProgressBar();

            Debug.Log($"End Build. (<color=#BFFF00>Success</color>={totalResult.SuccessCount}, <color=red>Failed</color>={totalResult.FailedCount})");
        }
    }
}
