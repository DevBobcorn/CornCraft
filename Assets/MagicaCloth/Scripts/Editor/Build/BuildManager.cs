// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagicaCloth
{
    public static partial class BuildManager
    {
        //=========================================================================================
        /// <summary>
        /// MagicaClothコンポーネントの古い形式を最新にアップグレードする
        /// すでに最新の場合は何もしません
        /// Upgrading old formats of MagicaCloth components to the latest.
        /// If it is already up-to-date, do nothing.
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static Define.Error UpgradeComponent(CoreComponent core)
        {
            Define.Error result = Define.Error.None;
            if (core == null)
                result = Define.Error.BuildInvalidComponent;

            if (core)
            {
                // プレハブかつシーンに配置されていない場合のみアセットに保存する
                string savePrefabPath = GetAssetSavePath(core);
                bool isPrefab = string.IsNullOrEmpty(savePrefabPath) == false;

                if (Define.IsNormal(result))
                {
                    var serializedObject = new SerializedObject(core);
                    serializedObject.Update();

                    if (core.UpgradeFormat())
                    {
                        // 更新あり
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(core);

                        // 保存反映
                        if (isPrefab)
                            AssetDatabase.SaveAssets();

                        if (Define.IsNormal(result))
                            Debug.Log($"<color=yellow>[Upgrade]</color> {core.name}");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// MagicaClothコンポーネントのデータ作成[Create]を実行する.
        /// Execute the MagicaCloth component's data creation [Create].
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static Define.Error CreateComponent(CoreComponent core)
        {
            Define.Error result = Define.Error.None;
            if (core == null)
                result = Define.Error.BuildInvalidComponent;

            if (core)
            {
                // プレハブかつシーンに配置されていない場合のみアセットに保存する
                string savePrefabPath = GetAssetSavePath(core);
                bool isPrefab = string.IsNullOrEmpty(savePrefabPath) == false;

                if (Define.IsNormal(result))
                {
                    //Debug.Log($"Started creating. [{core.name}] isPrefab:{isPrefab} path:{savePrefabPath}");
                    var serializedObject = new SerializedObject(core);
                    serializedObject.Update();

                    // コンポーネント別データ作成
                    if (core is MagicaBoneCloth)
                        result = CreateBoneCloth(core, serializedObject, savePrefabPath);
                    else if (core is MagicaBoneSpring)
                        result = CreateBoneSpring(core, serializedObject, savePrefabPath);
                    else if (core is MagicaMeshCloth)
                        result = CreateMeshCloth(core, serializedObject, savePrefabPath);
                    else if (core is MagicaMeshSpring)
                        result = CreateMeshSpring(core, serializedObject, savePrefabPath);
                    else if (core is MagicaRenderDeformer)
                        result = CreateRenderDeformer(core, serializedObject, savePrefabPath);
                    else if (core is MagicaVirtualDeformer)
                        result = CreateVirtualDeformer(core, serializedObject, savePrefabPath);

                    // 最終検証結果
                    if (Define.IsNormal(result))
                        result = core.VerifyData();

                    // 保存反映
                    if (isPrefab)
                    {
                        AssetDatabase.SaveAssets();
                    }
                }
            }

            // 結果
            if (result == Define.Error.None)
                Debug.Log($"<color=cyan>[Creation]</color> {core.name}");
            else
                Debug.LogError($"<color=cyan>[Creation]</color> <color=red>Failed!</color> {core.name}\n{Define.GetErrorMessage(result)}");

            return result;
        }

        /// <summary>
        /// 指定コンポーネントリストに対してデータ作成を実行する
        /// Execute data creation for the specified component list.
        /// </summary>
        /// <param name="coreComponents"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static BuildResult BuildFromComponents(List<CoreComponent> coreComponents, BuildOptions options)
        {
            if (coreComponents.Count == 0)
                return new BuildResult(Define.Error.Cancel);

            // ビルド順にソートする（特定の順序で実行しないとエラーとなる）
            SortCoreComponents(coreComponents);

            // ソート順にデータ作成
            var result = new BuildResult();
            foreach (var core in coreComponents)
            {
                //Debug.Log(core.name);
                var err = Define.Error.None;

                if (options.verificationOnly)
                {
                    // 検証のみ
                    if (IsOldFormat(core))
                        Debug.Log($"<color=yellow>[Old Format]</color> {core.name}");
                    if (IsOldAlgorithm(core))
                        Debug.Log($"<color=yellow>[Old Algorithm]</color> {core.name}");
                    var e = core.VerifyData();
                    if (e == Define.Error.EmptyData)
                        Debug.Log($"<color=cyan>[Not Created]</color> {core.name}");
                    else if (Define.IsError(e))
                        Debug.Log($"<color=red>[In Error]</color> {core.name}\n{Define.GetErrorMessage(e)}");
                    //if (IsNotCreated(core))
                    //    Debug.Log($"<color=cyan>[Not created or in error]</color> {core.name}");
                }
                else
                {
                    // 構築
                    // アップグレード
                    if (options.upgradeFormatAndAlgorithm && (IsOldFormat(core) || IsOldAlgorithm(core)))
                    {
                        err = UpgradeComponent(core);
                        if (Define.IsError(err))
                        {
                            result.SetError(err);
                            //Debug.LogError(Define.GetErrorMessage(err));

                            // エラー時の停止
                            if (options.errorStop)
                                break;
                        }
                    }

                    // 構築
                    err = CreateComponent(core);
                    if (Define.IsError(err))
                    {
                        result.SetError(err);
                        //Debug.LogError(Define.GetErrorMessage(err));

                        // エラー時の停止
                        if (options.errorStop)
                            break;
                    }

                    if (Define.IsNormal(err))
                        result.SetSuccess();
                }
            }

            return result;
        }

        //=========================================================================================
        /// <summary>
        /// シーンのオブジェクトに対してデータ作成を実行する
        /// Perform data creation on objects in the scene.
        /// </summary>
        /// <param name="gobj"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static BuildResult BuildFromSceneObject(GameObject gobj, BuildOptions options)
        {
            if (gobj == null)
                return new BuildResult(Define.Error.BuildInvalidGameObject);
            if (gobj.scene.IsValid() == false)
                return new BuildResult(Define.Error.BuildNotSceneObject);

            var result = new BuildResult();

            // 全コンポーネント取得
            var coreComponents = new List<CoreComponent>();
            GetBuildComponents(gobj, options, coreComponents);

            if (coreComponents.Count > 0)
            {
                Debug.Log($"<color=#f39800>[GameObject]</color> {gobj.name}");

                // ビルド
                result = BuildFromComponents(coreComponents, options);
            }

            return result;
        }

        /// <summary>
        /// プレハブアセットに対してすべてのデータ作成を実行する
        /// Perform all data creation for prefab assets.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static BuildResult BuildFromAssetPath(string path, BuildOptions options)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                return new BuildResult(Define.Error.BuildInvalidPrefab);

            // 編集不可のプレハブならば保存できないため処理を行わない
            if (PrefabUtility.IsPartOfImmutablePrefab(prefab))
                return new BuildResult(Define.Error.Cancel);

            var result = new BuildResult();

            // 全コンポーネント取得
            var coreComponents = new List<CoreComponent>();
            GetBuildComponents(prefab, options, coreComponents);

            if (coreComponents.Count > 0)
            {
                Debug.Log($"<color=#f39800>[Prefab]</color> {path}");

                // スクリプトの欠損(missing)がある場合は保存できないためエラー
                if (options.verificationOnly == false && CheckMissingScripts(prefab))
                    return new BuildResult(Define.Error.BuildMissingScriptOnPrefab);

                // ビルド
                result.Merge(BuildFromComponents(coreComponents, options));

                // サブアセットクリーンアップ
                if (result.SuccessCount > 0 && options.verificationOnly == false)
                    ShareDataPrefabExtension.CleanUpSubAssets(prefab, log: false);
            }

            return result;
        }

        /// <summary>
        /// シーンの内部オブジェクトに対してすべてのデータ構築を実行する
        /// Perform all data construction for the scene's internal objects.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static BuildResult BuildFromScenePath(string path, BuildOptions options)
        {
            Scene targetScene = new Scene();
            bool isOpened = false;

            // 現在開かれているシーンから検索
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.path == path)
                {
                    targetScene = scene;
                    isOpened = true;
                }
            }

            if (isOpened == false)
            {
                targetScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }
            if (targetScene.IsValid() == false)
                return new BuildResult(Define.Error.BuildInvalidScene);

            Debug.Log($"<color=#BFFF00>[Scene]</color> {path}");

            // すべてのコンポーネントを収集する
            var coreComponents = new List<CoreComponent>();
            foreach (var go in targetScene.GetRootGameObjects())
                GetBuildComponents(go, options, coreComponents);

            var result = new BuildResult();
            if (coreComponents.Count > 0)
            {
                // ビルド
                result.Merge(BuildFromComponents(coreComponents, options));

                // １つ以上ビルドが成功した場合はシーンを保存する
                if (result.SuccessCount > 0 && options.verificationOnly == false)
                {
                    EditorSceneManager.SaveScene(targetScene);
                }
            }

            if (isOpened == false)
                EditorSceneManager.CloseScene(targetScene, true);

            return result;
        }
    }
}
