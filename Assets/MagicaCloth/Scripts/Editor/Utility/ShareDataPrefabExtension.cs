// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif

namespace MagicaCloth
{
    /// <summary>
    /// 共有データオブジェクトのプレハブ化処理
    /// プレハブがApplyされた場合に、自動でスクリプタブルオブジェクをプレハブのサブアセットとして保存します。
    /// 該当するコンポーネントにIShareDataObjectを継承し、GetAllShareDataObject()で該当する共有データを返す必要があります。
    /// </summary>
    [InitializeOnLoad]
    internal class ShareDataPrefabExtension
    {
        private enum Mode
        {
            Saving = 1,
            Update = 2,
        }
        static List<GameObject> prefabInstanceList = new List<GameObject>();
        static List<Mode> prefabModeList = new List<Mode>();

        /// <summary>
        /// プレハブ更新コールバック登録
        /// </summary>
        static ShareDataPrefabExtension()
        {
            PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdate;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            PrefabStage.prefabSaving += OnPrefabSaving;
        }

        /// <summary>
        /// プレハブステージが閉じる時
        /// </summary>
        /// <param name="obj"></param>
        static void OnPrefabStageClosing(PrefabStage pstage)
        {
            //#if UNITY_2020_1_OR_NEWER
            //            Debug.Log($"OnPrefabStageClosing() root:[{pstage.prefabContentsRoot.name}] id:{pstage.prefabContentsRoot.GetInstanceID()} path:{pstage.assetPath}");
            //#else
            //            Debug.Log($"OnPrefabStageClosing() root:[{pstage.prefabContentsRoot.name}] id:{pstage.prefabContentsRoot.GetInstanceID()} path:{pstage.prefabAssetPath}");
            //#endif
            if (prefabInstanceList.Count > 0)
            {
                DelayAnalyze();
            }
        }


        /// <summary>
        /// プレハブモードでプレハブが保存される直前
        /// </summary>
        /// <param name="instance"></param>
        static void OnPrefabSaving(GameObject instance)
        {
            //Debug.Log($"OnPrefabSaving() instance:[{instance.name}] id:{instance.GetInstanceID()}");
            if (prefabInstanceList.Contains(instance) == false)
            {
                prefabInstanceList.Add(instance);
                prefabModeList.Add(Mode.Saving);
                DelayAnalyze();
            }
        }

        /// <summary>
        /// プレハブがApplyされた場合に呼ばれる
        /// instanceはヒエラルキーにあるゲームオブジェクト
        /// プレハブが更新された場合、スクリプタブルオブジェクをプレハブのサブアセットとして自動保存する
        /// </summary>
        /// <param name="instance"></param>
        static void OnPrefabInstanceUpdate(GameObject instance)
        {
            //Debug.Log($"OnPrefabInstanceUpdate() instance:{instance.name} id:{instance.GetInstanceID()}");
            if (prefabInstanceList.Contains(instance))
                return;
            prefabInstanceList.Add(instance);
            prefabModeList.Add(Mode.Update);
            EditorApplication.delayCall += DelayAnalyze;
        }

        static void DelayAnalyze()
        {
            //Debug.Log($"DelayAnalyze.start:{prefabInstanceList.Count}");

            EditorApplication.delayCall -= DelayAnalyze;
            for (int i = 0; i < prefabInstanceList.Count; i++)
            {
                var instance = prefabInstanceList[i];
                var mode = prefabModeList[i];

                if (instance)
                {
                    Analyze(instance, mode);
                }
            }

            prefabInstanceList.Clear();
            prefabModeList.Clear();

            //Debug.Log("DelayAnalyze.end.");
        }

        static void Analyze(GameObject instance, Mode mode)
        {
            var pstage = PrefabStageUtility.GetCurrentPrefabStage();
            bool isVariant = PrefabUtility.IsPartOfVariantPrefab(instance);
            bool onStage = pstage != null ? pstage.IsPartOfPrefabContents(instance) : false;
            //Debug.Log($"Analyze instance:{instance.name} id:{instance.GetInstanceID()} IsVariant:{isVariant} Mode:{mode} PStage:{pstage != null} OnStage:{onStage}");

            string pstageAssetPath = string.Empty;
            if (pstage != null)
            {
#if UNITY_2020_1_OR_NEWER
                pstageAssetPath = pstage.assetPath;
#else
                pstageAssetPath = pstage.prefabAssetPath;
#endif
                //Debug.Log($"pstage root:{pstage.prefabContentsRoot.name} id:{pstage.prefabContentsRoot.GetInstanceID()} path:{pstageAssetPath}");
            }
            else
            {
                //Debug.Log($"pstage = (null)");
            }

            string prefabAssetPath = string.Empty;
            string baseAssetPath = string.Empty;

            if (mode == Mode.Saving)
            {
                // 自身のプレハブアセット
                prefabAssetPath = pstageAssetPath;
                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);

                // 派生元のプレハブアセット
                var baseAsset = PrefabUtility.GetCorrespondingObjectFromSource(prefabAsset);
                baseAssetPath = AssetDatabase.GetAssetPath(baseAsset);
            }
            else
            {
                if (pstage != null)
                {
                    if (pstage.prefabContentsRoot == instance)
                    {
                        // 自身のプレハブアセット
                        prefabAssetPath = pstageAssetPath;
                        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);

                        // 派生元のプレハブアセット
                        var baseAsset = PrefabUtility.GetCorrespondingObjectFromSource(prefabAsset);
                        baseAssetPath = AssetDatabase.GetAssetPath(baseAsset);
                    }
                    else
                    {
                        // 自身のプレハブアセット
                        var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(instance);
                        prefabAssetPath = AssetDatabase.GetAssetPath(prefabAsset);

                        // 派生元のプレハブアセット
                        var baseAsset = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefabAsset);
                        baseAssetPath = AssetDatabase.GetAssetPath(baseAsset);
                    }
                }
                else
                {
                    // 自身のプレハブアセット
                    var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(instance);
                    prefabAssetPath = AssetDatabase.GetAssetPath(prefabAsset);

                    // 派生元のプレハブアセット
                    if (prefabAsset)
                    {
                        var baseAsset = PrefabUtility.GetCorrespondingObjectFromSource(prefabAsset);
                        baseAssetPath = AssetDatabase.GetAssetPath(baseAsset);
                    }
                }
            }
            //Debug.Log($"prefabPath:{prefabAssetPath}");
            //Debug.Log($"basePath:{baseAssetPath}");

            // 判定
            string saveAssetPath = prefabAssetPath;
            if (pstage == null && isVariant)
            {
                //Debug.Log($"instance1[{instance.name}]の変更を->[{prefabAssetPath}]に反映する.");
            }
            else if (string.IsNullOrEmpty(prefabAssetPath))
            {
                //Debug.Log($"Skip1");
                return;
            }
            else if (mode == Mode.Saving)
            {
                //Debug.Log($"instance2[{instance.name}]の変更を->[{prefabAssetPath}]に反映する.");
            }
            else
            {
                if (isVariant)
                {
                    //Debug.Log("Skip2");
                    return;
                }
                else if (string.IsNullOrEmpty(baseAssetPath))
                {
                    //Debug.Log($"instance4[{instance.name}]の変更を->[{prefabAssetPath}]に反映する.");
                }
                else
                {
                    //Debug.Log($"instance5[{instance.name}]の変更を->[{baseAssetPath}]に反映する.");
                    saveAssetPath = baseAssetPath;
                }
            }

            // 強制保存判定
            bool forceCopy = false;
            if (isVariant && pstage != null && instance != pstage.prefabContentsRoot)
                forceCopy = true;
            if (pstage != null && prefabAssetPath == saveAssetPath && saveAssetPath != pstageAssetPath && onStage)
                forceCopy = true;

            // 保存実行
            SavePrefab(instance, prefabAssetPath, saveAssetPath, isVariant, forceCopy, mode);
        }

        static void SavePrefab(GameObject instance, string prefabPath, string savePrefabPath, bool isVariant, bool forceCopy, Mode mode)
        {
            //Debug.Log($"SavePrefab instance:{instance.name} forceCopy:{forceCopy} isVariant:{isVariant}\npath:{prefabPath}\nsavePath:{savePrefabPath} ");

            // 保存先プレハブアセット
            var savePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(savePrefabPath);

            // 編集不可のプレハブならば保存できないため処理を行わない
            if (PrefabUtility.IsPartOfImmutablePrefab(savePrefab))
            {
                //Debug.Log("Skip3");
                return;
            }

            // 不要な共有データを削除するためのリスト
            bool change = false;
            List<ShareDataObject> removeDatas = new List<ShareDataObject>();

            // 現在アセットとして保存されているすべてのShareDataObjectサブアセットを削除対象としてリスト化する
            List<Object> subassets = new List<Object>(AssetDatabase.LoadAllAssetRepresentationsAtPath(savePrefabPath));
            if (subassets != null)
            {
                foreach (var obj in subassets)
                {
                    // ShareDataObjectのみ
                    ShareDataObject sdata = obj as ShareDataObject;
                    if (sdata && removeDatas.Contains(sdata) == false)
                    {
                        //Debug.Log("remove reserve sub asset:" + obj.name + " type:" + obj + " test:" + AssetDatabase.IsSubAsset(sdata));
                        // 削除対象として一旦追加
                        removeDatas.Add(sdata);
                    }
                }
            }

            // データコンポーネント収集
            var coreList = instance.GetComponentsInChildren<CoreComponent>(true);
            if (coreList != null)
            {
                foreach (var core in coreList)
                {
                    // 共有データ収集
                    var shareDataInterfaces = core.GetComponentsInChildren<IShareDataObject>(true);
                    if (shareDataInterfaces != null)
                    {
                        foreach (var sdataInterface in shareDataInterfaces)
                        {
                            List<ShareDataObject> shareDatas = sdataInterface.GetAllShareDataObject();
                            if (shareDatas != null)
                            {
                                foreach (var sdata in shareDatas)
                                {
                                    if (sdata)
                                    {
                                        //Debug.Log($"target shareData:{sdata.name}");

                                        if (removeDatas.Contains(sdata))
                                        {
                                            //Debug.Log($"Ignore:{sdata.name}");
                                            removeDatas.Remove(sdata);
                                        }
                                        else if (AssetDatabase.Contains(sdata))
                                        {
                                            // アセットのプレハブパスを取得
                                            var sdataPrefabPath = AssetDatabase.GetAssetPath(sdata);
                                            //Debug.Log($"sdataPrefabPath:{sdataPrefabPath}");

                                            if (forceCopy || prefabPath != savePrefabPath)
                                            {
                                                var newdata = sdataInterface.DuplicateShareDataObject(sdata);
                                                if (newdata != null)
                                                {
                                                    //Debug.Log($"+Duplicate sub asset:{newdata.name} -> [{savePrefab.name}]");
                                                    AssetDatabase.AddObjectToAsset(newdata, savePrefab);
                                                    change = true;
                                                }
                                            }
                                            else
                                            {
                                                removeDatas.Remove(sdata);
                                            }
                                        }
                                        else
                                        {
                                            //Debug.Log($"+Add sub asset:{sdata.name} -> [{savePrefab.name}]");
                                            AssetDatabase.AddObjectToAsset(sdata, savePrefab);
                                            change = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 不要な共有データは削除する
            foreach (var sdata in removeDatas)
            {
                //Debug.Log($"-Remove sub asset:{sdata.name} path:{AssetDatabase.GetAssetPath(sdata)}");
                UnityEngine.Object.DestroyImmediate(sdata, true);
                change = true;
            }

            // 変更を反映する
            if (change)
            {
                //Debug.Log("save!");

                // どうもこの手順を踏まないと保存した共有データが正しくアタッチされない
                if (mode == Mode.Saving)
                {
                    PrefabUtility.SaveAsPrefabAsset(instance, savePrefabPath);
                }
                else
                {
                    PrefabUtility.SaveAsPrefabAssetAndConnect(instance, savePrefabPath, InteractionMode.AutomatedAction);
                }
            }
        }

        //=========================================================================================
        public static bool CleanUpSubAssets(GameObject savePrefab, bool log = true)
        {
            // 編集不可のプレハブならば保存できないため処理を行わない
            if (PrefabUtility.IsPartOfImmutablePrefab(savePrefab))
            {
                return false;
            }

            string savePrefabPath = AssetDatabase.GetAssetPath(savePrefab);
            //Debug.Log($"PrefabPath:{savePrefabPath}");
            if (string.IsNullOrEmpty(savePrefabPath))
                return false;

            // 不要な共有データを削除するためのリスト
            List<ShareDataObject> removeDatas = new List<ShareDataObject>();

            // 現在アセットとして保存されているすべてのShareDataObjectサブアセットを削除対象としてリスト化する
            List<Object> subassets = new List<Object>(AssetDatabase.LoadAllAssetRepresentationsAtPath(savePrefabPath));
            if (subassets != null)
            {
                foreach (var obj in subassets)
                {
                    // ShareDataObjectのみ
                    ShareDataObject sdata = obj as ShareDataObject;
                    if (sdata && removeDatas.Contains(sdata) == false)
                    {
                        //Debug.Log("remove reserve sub asset:" + obj.name + " type:" + obj + " test:" + AssetDatabase.IsSubAsset(sdata));
                        // 削除対象として一旦追加
                        removeDatas.Add(sdata);
                    }
                }
            }

            // データコンポーネント収集
            var coreList = savePrefab.GetComponentsInChildren<CoreComponent>(true);
            if (coreList != null)
            {
                foreach (var core in coreList)
                {
                    // 共有データ収集
                    var shareDataInterfaces = core.GetComponentsInChildren<IShareDataObject>(true);
                    if (shareDataInterfaces != null)
                    {
                        foreach (var sdataInterface in shareDataInterfaces)
                        {
                            List<ShareDataObject> shareDatas = sdataInterface.GetAllShareDataObject();
                            if (shareDatas != null)
                            {
                                foreach (var sdata in shareDatas)
                                {
                                    if (sdata)
                                    {
                                        //Debug.Log($"target shareData:{sdata.name}");
                                        if (removeDatas.Contains(sdata))
                                        {
                                            //Debug.Log($"Ignore:{sdata.name}");
                                            removeDatas.Remove(sdata);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 不要な共有データは削除する
            if (removeDatas.Count > 0)
            {
                foreach (var sdata in removeDatas)
                {
                    //Debug.Log($"-Remove sub asset:{sdata.name} path:{AssetDatabase.GetAssetPath(sdata)}");
                    if (log)
                        Debug.Log($"Remove sub-asset : {sdata.name}");
                    UnityEngine.Object.DestroyImmediate(sdata, true);
                }
                AssetDatabase.SaveAssets();
            }
            if (log)
                Debug.Log($"Remove Count : {removeDatas.Count}");

            return true;
        }
    }
}
