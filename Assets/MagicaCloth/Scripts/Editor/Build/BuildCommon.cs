// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    public static partial class BuildManager
    {
        /// <summary>
        /// 共有データの作成
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataName"></param>
        /// <param name="savePrefabPath">共有データ保存先プレハブパス.null=保存しない</param>
        /// <returns></returns>
        static T CreateShareData<T>(string dataName, string savePrefabPath) where T : ShareDataObject
        {
            // 共有データ作成
            var sdata = ShareDataObject.CreateShareData<T>(dataName);

            // 同時にプレハブにデータを保存するか判定
            // （データをアセットデータベースで直接作成する場合）
            if (string.IsNullOrEmpty(savePrefabPath) == false)
            {
                SaveShareDataSubAsset(sdata, savePrefabPath);
            }

            return sdata;
        }

        /// <summary>
        /// 共有データを指定プレハブのサブアセットとして保存する
        /// </summary>
        /// <param name="sdata"></param>
        /// <param name="savePrefabPath"></param>
        /// <returns></returns>
        static bool SaveShareDataSubAsset(ShareDataObject sdata, string savePrefabPath)
        {
            // 保存先プレハブアセット
            var savePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(savePrefabPath);
            if (savePrefab == null)
                return false;

            // 編集不可のプレハブならば保存できないため処理を行わない
            if (PrefabUtility.IsPartOfImmutablePrefab(savePrefab))
            {
                return false;
            }

            // サブアセットとしてプレハブに保存する
            AssetDatabase.AddObjectToAsset(sdata, savePrefab);

            return true;
        }

        /// <summary>
        /// 指定コンポーネントの共有データが外部にアセットとして保存されているか判定する
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        static bool IsExternalShareDataObject(CoreComponent core)
        {
            Debug.Assert(core);

            bool ret = false;
            try
            {
                if (core is BaseCloth)
                {
                    var cloth = core as BaseCloth;

                    if (cloth.ClothData != null)
                        ret = AssetDatabase.IsForeignAsset(cloth.ClothData) ? true : ret;
                    if (cloth is MagicaMeshSpring)
                        ret = AssetDatabase.IsForeignAsset((cloth as MagicaMeshSpring).SpringData) ? true : ret;
                }
                else if (core is MagicaRenderDeformer)
                {
                    ret = AssetDatabase.IsForeignAsset((core as MagicaRenderDeformer).Deformer.MeshData) ? true : ret;
                }
                else if (core is MagicaVirtualDeformer)
                {
                    ret = AssetDatabase.IsForeignAsset((core as MagicaVirtualDeformer).Deformer.MeshData) ? true : ret;
                }
            }
            catch (Exception)
            {
                // Reference is missing!
                ret = false;
            }

            return ret;
        }

        /// <summary>
        /// データが未作成か判定する
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        static bool IsNotCreated(CoreComponent core)
        {
            Debug.Assert(core);
            return Define.IsError(core.VerifyData());
        }

        /// <summary>
        /// データが古いフォーマットか判定する
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        static bool IsOldFormat(CoreComponent core)
        {
            Debug.Assert(core);
            return core.IsOldDataVertion();
        }

        /// <summary>
        /// 古いアルゴリズムを使用しているか判定する
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        static bool IsOldAlgorithm(CoreComponent core)
        {
            Debug.Assert(core);
            if (core is BaseCloth)
            {
                var cloth = core as BaseCloth;

                // パラメータの設定が古いアルゴリズムを指している
                if (cloth.Params.AlgorithmType != ClothParams.Algorithm.Algorithm_2)
                    return true;

                // すでに作成されたデータのアルゴリズムが古い
                if (cloth.ClothData != null && Define.IsError(cloth.VerifyAlgorithmVersion()))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 指定ゲームオブジェクトからビルドに必要なコンポーネントを検索しリストに追加する
        /// </summary>
        /// <param name="gobj"></param>
        /// <param name="options"></param>
        /// <param name="coreComponents"></param>
        /// <returns></returns>
        static void GetBuildComponents(GameObject gobj, BuildOptions options, List<CoreComponent> coreComponents)
        {
            if (gobj == null)
                return;

            // シーンに配置されているか
            bool isScene = gobj.scene.IsValid();

            // 全コンポーネント取得
            var components = new List<CoreComponent>(
                gobj.GetComponentsInChildren<CoreComponent>(options.includeInactive)
                // コンバートコンポーネントの選別
                .Where(x =>
                    x is MagicaBoneCloth && options.buildBoneCloth
                    || x is MagicaBoneSpring && options.buildBoneSpring
                    || x is MagicaMeshCloth && options.buildMeshCloth
                    || x is MagicaMeshSpring && options.buildMeshSpring
                    || x is MagicaRenderDeformer && options.buildRenderDeformer
                    || x is MagicaVirtualDeformer && options.buildVirtualDeformer
                )
                // シーン配置の場合は共有データが外部に保存されているなら無効とする
                .Where(x => isScene == false || IsExternalShareDataObject(x) == false)
                // ビルド条件
                .Where(x =>
                    options.forceBuild
                    || options.verificationOnly
                    || options.notCreated && IsNotCreated(x)
                    || options.upgradeFormatAndAlgorithm && (IsOldFormat(x) || IsOldAlgorithm(x))
                )
                );

            coreComponents.AddRange(components);
        }

        /// <summary>
        /// コンポーネントリストをビルド順番にソートする
        /// </summary>
        /// <param name="coreComponents"></param>
        static void SortCoreComponents(List<CoreComponent> coreComponents)
        {
            // RenderDeformer > VirtualDeformer > ClothComponent の順でソートする
            // この順でデータを作成しないと駄目!
            coreComponents.Sort((a, b) => a.GetComponentType() < b.GetComponentType() ? -1 : 1);
        }

        /// <summary>
        /// データを保存するアセットパスを取得する
        /// プレハブかつシーンに配置されていない場合のみ有効なパスが帰る
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        static string GetAssetSavePath(CoreComponent core)
        {
            if (core == null)
                return null;

            return EditorUtility.IsPersistent(core) ? PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(core) : null;
        }

        /// <summary>
        /// 指定オブジェクト（プレハブ）にMissingスクリプトが含まれているかチェックする
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        static bool CheckMissingScripts(GameObject go)
        {
            return go.GetComponentsInChildren<Component>().Contains(null);
        }
    }
}
