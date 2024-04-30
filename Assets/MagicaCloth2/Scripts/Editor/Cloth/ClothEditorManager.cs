// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
#if UNITY_2020
using UnityEditor.Experimental.SceneManagement;
#else
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// エディタ編集時のコンポーネント管理
    /// </summary>
    [InitializeOnLoad]
    public class ClothEditorManager
    {
        /// <summary>
        /// コンポーネント情報
        /// </summary>
        class ClothInfo
        {
            public ResultCode result = ResultCode.Empty;
            public bool building;
            public GizmoType gizmoType;
            public ClothBehaviour component;
            public int componentHash;
            public VirtualMeshContainer editMeshContainer;
            public int nextBuildHash;
            public int importCount;
        }

        static Dictionary<int, ClothInfo> editClothDict = new Dictionary<int, ClothInfo>();

        static List<int> destroyList = new List<int>();
        static List<ClothInfo> drawList = new List<ClothInfo>();
        static CancellationTokenSource cancelToken = new CancellationTokenSource();

        static bool isValid = false;

        static internal Action OnEditMeshBuildComplete;

        //=========================================================================================
        static ClothEditorManager()
        {
            Develop.DebugLog($"ClothEditorManager Initialize!");

            Dispose();

            // スクリプトコンパイルコールバック
            CompilationPipeline.compilationStarted -= OnStartCompile;
            CompilationPipeline.compilationStarted += OnStartCompile;

            // シーンビューにGUIを描画するためのコールバック
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;

            // プレハブステージ
            PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;

            // Undo/Redo
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            isValid = true;
        }

        /// <summary>
        /// エディタの実行状態が変更された場合に呼び出される
        /// </summary>
        [InitializeOnLoadMethod]
        static void PlayModeStateChange()
        {
            EditorApplication.playModeStateChanged += (mode) =>
            {
                Develop.DebugLog($"PlayModeStateChanged:{mode}");

                if (mode == UnityEditor.PlayModeStateChange.ExitingEditMode || mode == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                {
                    //Develop.DebugLog($"PlayModeStateChange. Exit Editor mode!");
                    Dispose();
                    isValid = false;
                }
                if (mode == UnityEditor.PlayModeStateChange.EnteredEditMode || mode == UnityEditor.PlayModeStateChange.EnteredPlayMode)
                {
                    //Develop.DebugLog($"PlayModeStateChange. Enter Editor mode!");
                    isValid = true;
                }
            };
        }

        /// <summary>
        /// スクリプトコンパイル開始
        /// </summary>
        /// <param name="obj"></param>
        static void OnStartCompile(object obj)
        {
            Develop.DebugLog($"start compile.");
            Dispose();
            isValid = false;
        }

        /// <summary>
        /// ビルド完了時
        /// </summary>
        /// <param name="target"></param>
        /// <param name="pathToBuiltProject"></param>
        [PostProcessBuildAttribute(1)]
        static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            Develop.DebugLog($"build compile.");
            isValid = true;
        }

        /// <summary>
        /// プレハブステージが閉じる時
        /// </summary>
        /// <param name="obj"></param>
        static void OnPrefabStageClosing(PrefabStage pstage)
        {
            ForceUpdateAllComponents();
        }

        /// <summary>
        /// Undo/Redo実行時
        /// </summary>
        static void OnUndoRedoPerformed()
        {
            ForceUpdateAllComponents();
        }

        /// <summary>
        /// MagidaClothコンポーネントの登録および編集メッシュの作成/更新
        /// </summary>
        /// <param name="component"></param>
        public static void RegisterComponent(ClothBehaviour component, GizmoType gizmoType, bool forceUpdate = false)
        {
            //Develop.DebugLog($"RegisterComponent:{component.name}, isValid:{isValid}");
            if (isValid == false)
                return;
            if (component == null)
                return;

            // ペイント中は無効
            if (ClothPainter.IsPainting())
                return;

            int id = component.GetInstanceID();
            MagicaCloth cloth = component as MagicaCloth;

            //if (cloth)
            //    Develop.DebugLog($"Register Cloth:{component.name}, gizmoType:{gizmoType}");

            // ギズモ表示判定
            if (gizmoType.HasFlag(GizmoType.Active))
            {
                gizmoType = GizmoType.Active;
            }
            else
            {
                // ★何故かGizmoType.InSelectionHierarchyが正常に判定できないので手動で解決する！(2022/10/14)
                // ★GizmoType.InSelectionHierarchyがバグっている？
                gizmoType = 0;
                var t = component.transform.parent;
                var activeT = Selection.activeTransform;
                if (activeT)
                {
                    while (t)
                    {
                        if (t == activeT)
                        {
                            gizmoType = GizmoType.Selected;
                            break;
                        }
                        else
                            t = t.parent;
                    }
                }

                // 常に表示
                if (cloth && cloth.GizmoSerializeData.IsAlways())
                    gizmoType = GizmoType.Active;
            }

            // クロス指定のコライダー表示
            if (cloth && cloth.GizmoSerializeData.clothDebugSettings.collider && gizmoType != 0)
            {
                foreach (var col in cloth.SerializeData.colliderCollisionConstraint.colliderList)
                {
                    RegisterComponent(col, gizmoType);
                }
            }

            lock (editClothDict)
            {
                if (editClothDict.ContainsKey(id) == false)
                {
                    //Debug.Log($"登録:{component.name}");
                    var info = new ClothInfo();
                    info.building = false;
                    info.result = ResultCode.Empty;
                    info.component = component;
                    info.editMeshContainer = null;
                    info.gizmoType = gizmoType;
                    info.componentHash = 0;
                    info.nextBuildHash = 0;
                    info.importCount = 1;
                    editClothDict.Add(id, info);
                }
                else
                {
                    if (gizmoType > editClothDict[id].gizmoType)
                        editClothDict[id].gizmoType = gizmoType;
                }
            }

            // EditMesh作成(MagicaClothコンポーネントのみ)
            if (cloth && EditorApplication.isPlaying == false)
            {
                int hash = component.GetMagicaHashCode();
                //Debug.Log($"Hash:{hash}");

                lock (editClothDict)
                {
                    if (editClothDict.ContainsKey(id))
                    {
                        var info = editClothDict[id];

                        // ハッシュにはインポート回数を乗算する
                        hash *= (info.importCount + 1);

                        // ハッシュが異なる場合のみ再構築/もしくは強制更新
                        if (info.componentHash != hash || forceUpdate)
                        {
                            // 現在作成中ならば次の作成候補として登録
                            if (info.building)
                            {
                                info.nextBuildHash = hash;
                            }
                            // そうでなければ作成を開始する
                            else
                            {
                                info.componentHash = hash;
                                info.nextBuildHash = 0;

                                if (cloth.isActiveAndEnabled)
                                {
                                    // スレッドで作成
                                    info.building = true;
                                    var _ = CreateOrUpdateEditMesh(id, cloth, cancelToken.Token);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static VirtualMeshContainer GetEditMeshContainer(ClothBehaviour comp)
        {
            if (isValid == false)
                return null;
            if (comp == null)
                return null;
            int id = comp.GetInstanceID();
            VirtualMeshContainer cmesh = null;
            lock (editClothDict)
            {
                cmesh = editClothDict.ContainsKey(id) ? editClothDict[id].editMeshContainer : null;
            }
            return cmesh;
        }

        /// <summary>
        /// 現在のコンポーネント状態を返す
        /// </summary>
        /// <param name="comp"></param>
        /// <returns></returns>
        public static ResultCode GetResultCode(ClothBehaviour comp)
        {
            if (comp == null)
                return ResultCode.Empty;
            int id = comp.GetInstanceID();
            lock (editClothDict)
            {
                if (editClothDict.ContainsKey(id))
                    return editClothDict[id].result;
                else
                    return ResultCode.Empty;
            }
        }

        static void Dispose()
        {
            cancelToken.Cancel();
            cancelToken.Dispose();
            cancelToken = new CancellationTokenSource();

            destroyList.Clear();
            drawList.Clear();
            lock (editClothDict)
            {
                foreach (var info in editClothDict.Values)
                {
                    info?.editMeshContainer?.Dispose();
                }
                editClothDict.Clear();
            }
        }

        /// <summary>
        /// コンポーネントの削除チェック
        /// </summary>
        static void DestroyCheck()
        {
            lock (editClothDict)
            {
                destroyList.Clear();
                foreach (var kv in editClothDict)
                {
                    if (kv.Value.component == null)
                    {
                        destroyList.Add(kv.Key);
                    }
                }
                foreach (var id in destroyList)
                {
                    //Debug.Log($"削除");
                    editClothDict[id].editMeshContainer?.Dispose();
                    editClothDict.Remove(id);
                }
                destroyList.Clear();
            }
        }

        /// <summary>
        /// アセット更新にともなう編集用メッシュの更新
        /// </summary>
        /// <param name="importedAssets"></param>
        public static void UpdateFromAssetImport(string[] importedAssets)
        {
            if (importedAssets == null || importedAssets.Length == 0)
                return;

            var importedAssetSet = new HashSet<string>(importedAssets);

            // クロスコンポーネントがインポートされたアセットを参照している場合は再構築フラグを立てる
            var updateDict = new Dictionary<ClothBehaviour, GizmoType>();
            lock (editClothDict)
            {
                foreach (var cinfo in editClothDict.Values)
                {
                    var cloth = cinfo.component as MagicaCloth;
                    if (cloth)
                    {
                        bool update = false;
                        var sdata = cloth.SerializeData;

                        // source renderes
                        if (sdata.clothType == ClothProcess.ClothType.MeshCloth)
                        {
                            foreach (var ren in sdata.sourceRenderers)
                            {
                                if (ren && importedAssetSet.Contains(AssetDatabase.GetAssetPath(ren)))
                                    update = true;
                            }
                        }

                        // paint maps
                        if (sdata.clothType == ClothProcess.ClothType.MeshCloth && sdata.paintMode != ClothSerializeData.PaintMode.Manual)
                        {
                            foreach (var map in sdata.paintMaps)
                            {
                                if (map && importedAssetSet.Contains(AssetDatabase.GetAssetPath(map)))
                                    update = true;
                            }
                        }

                        if (update)
                        {
                            // 再構築フラグ
                            // インポートカウントを増加させることでハッシュ値を変化させる
                            cinfo.importCount++;
                            updateDict.Add(cinfo.component, cinfo.gizmoType);
                        }
                    }
                }
            }

            // 再構築開始
            foreach (var kv in updateDict)
            {
                RegisterComponent(kv.Key, kv.Value);
            }
        }

        /// <summary>
        /// 強制的にすべてのコンポーネントの更新フラグを立てる
        /// </summary>
        static void ForceUpdateAllComponents()
        {
            lock (editClothDict)
            {
                foreach (var cinfo in editClothDict.Values)
                {
                    var cloth = cinfo.component as MagicaCloth;
                    if (cloth)
                    {
                        // 再構築フラグ
                        // インポートカウントを増加させることでハッシュ値を変化させる
                        // 次のギズモ表示もしくはペイント時に再構築される
                        cinfo.importCount++;
                    }
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// 編集用メッシュの作成/更新
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cloth"></param>
        /// <param name="createSelectionData"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        static async Task CreateOrUpdateEditMesh(int id, MagicaCloth cloth, CancellationToken ct)
        {
            // ■メインスレッド
            Develop.DebugLog($"Create and update edit meshes: {cloth.name}");
            var sdata = cloth.SerializeData;
            var sdata2 = cloth.GetSerializeData2();
            var setupList = new List<RenderSetupData>();
            VirtualMeshContainer editMeshContainer = null;
            ResultCode result = new ResultCode();

            try
            {
                // ■メインスレッド
                if (cloth == null || isValid == false)
                {
                    result.SetError(Define.Result.CreateCloth_InvalidCloth);
                    throw new MagicaClothProcessingException();
                }

                // メッシュを構築するための最低限のデータが揃っているか確認
                if (sdata.IsValid() == false)
                {
                    result.SetResult(sdata.VerificationResult);
                    throw new MagicaClothProcessingException();
                }

                // セットアップデータの作成
                if (sdata.clothType == ClothProcess.ClothType.MeshCloth)
                {
                    foreach (var ren in sdata.sourceRenderers)
                    {
                        if (ren)
                        {
                            var setup = new RenderSetupData(ren);
                            result.Merge(setup.result);
                            if (setup.IsFaild())
                            {
                                setup.Dispose();
                                throw new MagicaClothProcessingException();
                            }
                            setupList.Add(setup);
                        }
                        else
                        {
                            result.SetError(Define.Result.CreateCloth_NoRenderer);
                            throw new MagicaClothProcessingException();
                        }
                    }
                }
                else if (sdata.clothType == ClothProcess.ClothType.BoneCloth)
                {
                    var setup = new RenderSetupData(RenderSetupData.SetupType.BoneCloth, cloth.ClothTransform, sdata.rootBones, null, sdata.connectionMode, cloth.name);
                    setupList.Add(setup);
                }
                else if (sdata.clothType == ClothProcess.ClothType.BoneSpring)
                {
                    // BoneSpringではLine接続のみ
                    var setup = new RenderSetupData(RenderSetupData.SetupType.BoneSpring, cloth.ClothTransform, sdata.rootBones, sdata.colliderCollisionConstraint.collisionBones, RenderSetupData.BoneConnectionMode.Line, cloth.name);
                    setupList.Add(setup);
                }

                if (setupList.Count == 0)
                {
                    result.SetError(Define.Result.CreateCloth_InvalidSetupList);
                    throw new MagicaClothProcessingException();
                }

                // クロスコンポーネントトランスフォーム情報
                var clothTransformRecord = new TransformRecord(cloth.ClothTransform);

                // 法線調整用トランスフォーム
                var normalAdjustmentTransformRecored = new TransformRecord(
                    sdata.normalAlignmentSetting.adjustmentTransform ?
                    sdata.normalAlignmentSetting.adjustmentTransform :
                    cloth.ClothTransform
                    );

                // ペイントマップデータの作成（これはメインスレッドでのみ作成可能）
                var paintMapDataList = new List<ClothProcess.PaintMapData>();
                bool usePaintMap = false;
                if (sdata.clothType == ClothProcess.ClothType.MeshCloth && sdata.paintMode != ClothSerializeData.PaintMode.Manual)
                {
                    var ret = cloth.Process.GeneratePaintMapDataList(paintMapDataList);
                    if (ret.IsError())
                    {
                        result.Merge(ret);
                        throw new MagicaClothProcessingException();
                    }
                    if (paintMapDataList.Count != setupList.Count)
                    {
                        result.SetError(Define.Result.CreateCloth_PaintMapCountMismatch);
                        throw new MagicaClothProcessingException();
                    }
                    usePaintMap = true;
                }

                // エディットメッシュ作成
                // メッシュはセンター空間で作成される
                ct.ThrowIfCancellationRequested();
                var editMesh = new VirtualMesh("EditMesh");
                editMeshContainer = new VirtualMeshContainer(editMesh);
                if (sdata.clothType == ClothProcess.ClothType.MeshCloth)
                {
                    // MeshClothではクロストランスフォームを追加しておく
                    editMesh.SetTransform(cloth.ClothTransform);
                }
                editMesh.result.SetProcess();

                // セレクションデータ
                // ペイントマップ指定の場合は空で初期化
                SelectionData selectionData = usePaintMap ? new SelectionData() : sdata2.selectionData.Clone();

                // ■スレッド
                await Task.Run(() =>
                {
                    try
                    {
                        // MeshCloth/BoneClothで処理が一部異なる
                        ct.ThrowIfCancellationRequested();
                        if (sdata.clothType == ClothProcess.ClothType.MeshCloth)
                        {
                            for (int i = 0; i < setupList.Count; i++)
                            {
                                var setup = setupList[i];

                                // レンダーメッシュ作成
                                using var renderMesh = new VirtualMesh($"[{setup.name}]");
                                renderMesh.result.SetProcess();

                                // インポート
                                renderMesh.ImportFrom(setup);
                                //Debug.Log($"(IMPORT) {renderMesh}");
                                if (renderMesh.IsError)
                                    continue;
                                renderMesh.result.SetSuccess();

                                // MeshClothでペイントテクスチャ指定の場合はセレクションデータを生成する
                                if (usePaintMap)
                                {
                                    var ret = cloth.Process.GenerateSelectionDataFromPaintMap(clothTransformRecord, renderMesh, paintMapDataList[i], out SelectionData renderSelectionData);
                                    if (ret.IsError())
                                    {
                                        result.Merge(ret);
                                        throw new MagicaClothProcessingException();
                                    }

                                    // セレクションデータ結合
                                    selectionData.Merge(renderSelectionData);
                                }

                                // マージ
                                editMesh.AddMesh(renderMesh);
                            }
                            //Debug.Log($"(MERGE) {editMesh}");

                            // リダクション
                            if (editMesh.VertexCount > 1 && sdata.reductionSetting.IsEnabled)
                            {
                                editMesh.Reduction(sdata.reductionSetting, ct);
                                if (editMesh.IsError)
                                {
                                    result.Merge(editMesh.result);
                                    throw new MagicaClothProcessingException();
                                }

                                //Debug.Log($"(REDUCTION) {editMesh}");
                            }
                        }
                        else if (sdata.clothType == ClothProcess.ClothType.BoneCloth || sdata.clothType == ClothProcess.ClothType.BoneSpring)
                        {
                            // import
                            editMesh.ImportFrom(setupList[0]);
                            if (editMesh.IsError)
                            {
                                result.Merge(editMesh.result);
                                throw new MagicaClothProcessingException();
                            }
                            //Debug.Log($"(IMPORT) {editMesh}");
                        }

                        // 元の頂点から結合頂点へのインデックスを初期化
                        if (editMesh.joinIndices.IsCreated == false)
                        {
                            editMesh.joinIndices = new Unity.Collections.NativeArray<int>(editMesh.VertexCount, Unity.Collections.Allocator.Persistent);
                            JobUtility.SerialNumberRun(editMesh.joinIndices, editMesh.VertexCount); // 連番をつける
                        }

                        // 最適化
                        ct.ThrowIfCancellationRequested();
                        editMesh.Optimization();
                        if (editMesh.IsError)
                        {
                            result.Merge(editMesh.result);
                            throw new MagicaClothProcessingException();
                        }
                        //Debug.Log($"(OPTIMIZE) {editMesh}");
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (MagicaClothProcessingException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                        result.SetError(Define.Result.CreateCloth_Exception);
                        throw;
                    }
                }, ct);

                // ■メインスレッド
                // セレクションデータの作成
                // まだセレクションデータが未編集の場合は作り直す
                ct.ThrowIfCancellationRequested();
                if (cloth == null || isValid == false)
                {
                    result.SetError(Define.Result.CreateCloth_InvalidCloth);
                    throw new MagicaClothProcessingException();
                }
                if (usePaintMap == false)
                {
                    if (sdata2.selectionData == null || sdata2.selectionData.IsValid() == false || sdata2.selectionData.userEdit == false)
                    {
                        // 新規作成
                        selectionData = CreateAutoSelectionData(cloth, sdata, editMesh);

                        // 格納
                        sdata2.selectionData = selectionData;
                    }
                }

                // ■スレッド
                await Task.Run(() =>
                {
                    try
                    {
                        // セレクションデータから頂点属性を付与する
                        ct.ThrowIfCancellationRequested();
                        if (selectionData.IsValid())
                        {
                            editMesh.ApplySelectionAttribute(selectionData);
                        }

                        // ProxyMeshへの変換（属性決定後に実行）
                        ct.ThrowIfCancellationRequested();
                        editMesh.ConvertProxyMesh(cloth.SerializeData, null, null, normalAdjustmentTransformRecored);
                        if (editMesh.IsError)
                        {
                            result.Merge(editMesh.result);
                            throw new MagicaClothProcessingException();
                        }
                        //Debug.Log($"(PROXY) {editMesh}");

#if false
                    // pitch/yaw個別制限はv1.0では実装しないので一旦停止
                    // 角度制限計算用回転を作成
                    ct.ThrowIfCancellationRequested();
                    editMesh.CreateAngleCalcLocalRotation(sdata.normalCalculation, sdata.normalCalculationCenter);
                    if (editMesh.IsError)
                        throw new InvalidOperationException();
#endif

                        // 完了
                        ct.ThrowIfCancellationRequested();
                        editMesh.result.SetSuccess();
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (MagicaClothProcessingException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                        result.SetError(Define.Result.CreateCloth_Exception);
                        throw;
                    }
                }, ct);

                // ■メインスレッド
                ct.ThrowIfCancellationRequested();
                if (cloth == null || isValid == false)
                {
                    // キャンセル扱いにする
                    throw new OperationCanceledException();
                }

                // 成功
                Develop.DebugLog($"(FINAL PROXY) {editMesh}");
                result.SetSuccess();
            }
            catch (MagicaClothProcessingException)
            {
                if (result.IsNone()) result.SetError(Define.Result.CreateCloth_UnknownError);
                result.DebugLog();
            }
            catch (OperationCanceledException)
            {
                Develop.DebugLog($"Editor mesh build canceled!");
                result.SetCancel();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                result.SetError(Define.Result.CreateCloth_Exception);
            }
            finally
            {
                // 状態変更
                if (isValid)
                {
                    lock (editClothDict)
                    {
                        if (editClothDict.ContainsKey(id))
                        {
                            var info = editClothDict[id];
                            info.building = false;
                            info.result = result;
                            info.editMeshContainer?.Dispose();
                            if (result.IsSuccess())
                            {
                                info.editMeshContainer = editMeshContainer;
                                editMeshContainer = null;
                                Develop.DebugLog($"Registration Complete : {cloth.name}");
                            }
                            else
                            {
                                info.editMeshContainer = null;
                            }
                        }
                    }
                }

                // dispose
                foreach (var setup in setupList)
                {
                    setup?.Dispose();
                }
                editMeshContainer?.Dispose();

                //span.DebugLog();

                // 引き続き再構築の実行判定
                if (isValid)
                {
                    lock (editClothDict)
                    {
                        if (editClothDict.ContainsKey(id))
                        {
                            var info = editClothDict[id];
                            info.building = false;

                            if (info.nextBuildHash != 0 && cloth)
                            {
                                if (info.nextBuildHash == info.componentHash)
                                {
                                    info.nextBuildHash = 0;
                                }
                                else
                                {
                                    info.componentHash = info.nextBuildHash;
                                    info.nextBuildHash = 0;
                                    info.building = true;
                                    info.result.Clear();

                                    var _ = CreateOrUpdateEditMesh(id, cloth, ct);
                                }
                            }
                        }
                    }
                }

                SceneView.RepaintAll();

                // ビルド完了通知
                OnEditMeshBuildComplete?.Invoke();
            }
        }

        /// <summary>
        /// セレクションデータをシリアライズ化する
        /// </summary>
        /// <param name="cloth"></param>
        /// <param name="selectionData"></param>
        public static void ApplySelectionData(MagicaCloth cloth, SelectionData selectionData)
        {
            if (cloth == null || selectionData == null || selectionData.IsValid() == false)
                return;

            if (cloth.GetSerializeData2().selectionData != null && cloth.GetSerializeData2().selectionData.Compare(selectionData))
                return; // 変更なし

            //Debug.Log($"セレクションデータ格納!");
            Undo.RecordObject(cloth, "Paint");

            // 最終的に[SerializeReference]から[SerializeField]に変更(2022/12/18)
            cloth.GetSerializeData2().selectionData = selectionData;

            EditorUtility.SetDirty(cloth);
        }


        /// <summary>
        /// 編集メッシュから自動でセレクションデータを生成する（メインスレッドのみ）
        /// </summary>
        /// <param name="sdata"></param>
        /// <param name="emesh"></param>
        /// <param name="setupList"></param>
        /// <returns></returns>
        public static SelectionData CreateAutoSelectionData(MagicaCloth cloth, ClothSerializeData sdata, VirtualMesh emesh)
        {
            var selectionData = new SelectionData(emesh, float4x4.identity);
            int cnt = selectionData.Count;
            if (cnt == 0)
                return selectionData;

            // BoneClothはRootをFixedに定義する、それ以外はMove
            if (sdata.clothType == ClothProcess.ClothType.BoneCloth || sdata.clothType == ClothProcess.ClothType.BoneSpring)
            {
                selectionData.Fill(VertexAttribute.Move);

                // BoneClothではセットアップデータのrootのみ固定に設定する
                // BoneSpringではLine接続のみとなる
                var connectionMode = sdata.clothType == ClothProcess.ClothType.BoneSpring ? RenderSetupData.BoneConnectionMode.Line : sdata.connectionMode;
                var setupType = sdata.clothType == ClothProcess.ClothType.BoneSpring ? RenderSetupData.SetupType.BoneSpring : RenderSetupData.SetupType.BoneCloth;
                using var setup = new RenderSetupData(setupType, cloth.ClothTransform, sdata.rootBones, null, connectionMode, cloth.name);
                foreach (int id in setup.rootTransformIdList)
                {
                    int rootIndex = setup.GetTransformIndexFromId(id);
                    selectionData.attributes[rootIndex] = VertexAttribute.Fixed;
                }
            }
            // MeshClothではすべて固定で定義する
            else
            {
                selectionData.Fill(VertexAttribute.Invalid);
            }

            return selectionData;
        }

        //=========================================================================================
        /// <summary>
        /// シーンビューへのギズモ描画
        /// </summary>
        /// <param name="sceneView"></param>
        static void OnSceneGUI(SceneView sceneView)
        {
            if (isValid == false)
                return;

            // コンポーネント削除チェック
            DestroyCheck();

            // コンポーネントギズモ描画
            if (Event.current.type == EventType.Repaint)
            {
                //Develop.DebugLog($"Repaint. F:{Time.frameCount}");

                drawList.Clear();
                bool isPainting = ClothPainter.IsPainting();
                bool isPlaying = EditorApplication.isPlaying;
                var camPos = sceneView.camera.transform.position;
                Quaternion camRot = sceneView.camera.transform.rotation;

                lock (editClothDict)
                {
                    foreach (var info in editClothDict.Values)
                    {
                        if (info == null || info.component == null)
                            continue;

                        if (info.component.isActiveAndEnabled == false)
                            continue;

                        // ペイント中は表示しない
                        if (isPainting)
                            continue;

                        // アクティブ状態
                        bool active = info.gizmoType.HasFlag(GizmoType.Active);

                        // 選択状態
                        bool selected = Selection.Contains(info.component.gameObject);
                        float dist = Vector3.Distance(camPos, info.component.transform.position);

                        // Collider
                        if (info.component is ColliderComponent)
                        {
                            if (active == false && info.gizmoType.HasFlag(GizmoType.Selected) == false)
                                continue;
                            if (selected == false && dist >= 20.0f)
                                continue;

                            GizmoUtility.DrawCollider(info.component as ColliderComponent, camRot, true, active);
                        }
                        // MagicaCloth
                        else if (info.component is MagicaCloth)
                        {
                            if (active == false)
                                continue;
                            if (selected == false && dist >= 20.0f)
                                continue;

                            var cloth = info.component as MagicaCloth;

                            // Cloth
                            if (cloth.GizmoSerializeData.clothDebugSettings.enable)
                            {
                                //Debug.Log($"ペイントくろす");
                                if (isPlaying)
                                    ClothEditorUtility.DrawClothRuntime(cloth.Process, cloth.GizmoSerializeData.clothDebugSettings, active);
                                else
                                    ClothEditorUtility.DrawClothEditor(info.editMeshContainer, cloth.GizmoSerializeData.clothDebugSettings, cloth.SerializeData, active, false, false);
                            }

#if MC2_DEBUG
                            // Proxy Mesh
                            if (cloth.GizmoSerializeData.proxyDebugSettings.enable)
                            {
                                if (isPlaying)
                                    VirtualMeshEditorUtility.DrawRuntimeGizmos(cloth.Process, false, cloth.Process.ProxyMeshContainer, cloth.GizmoSerializeData.proxyDebugSettings, active, true);
                                else
                                    VirtualMeshEditorUtility.DrawGizmos(info.editMeshContainer, cloth.GizmoSerializeData.proxyDebugSettings, active, true);
                            }

                            // Mapping Mesh
                            if (cloth.GizmoSerializeData.mappingDebugSettings.enable)
                            {
                                if (isPlaying)
                                {
                                    var renderMeshInfo = cloth.Process.GetRenderMeshInfo(cloth.GizmoSerializeData.debugMappingIndex);
                                    if (renderMeshInfo != null && renderMeshInfo.renderMeshContainer != null)
                                        VirtualMeshEditorUtility.DrawRuntimeGizmos(cloth.Process, true, renderMeshInfo.renderMeshContainer, cloth.GizmoSerializeData.mappingDebugSettings, active, true);
                                }
                            }
#endif // MC2_DEBUG
                        }
                        // WindZone
                        else if (info.component is MagicaWindZone)
                        {
                            GizmoUtility.DrawWindZone(info.component as MagicaWindZone, camRot, active);
                        }

                        // 描画フラグoff
                        info.gizmoType = 0;
                    }
                }
            }
        }
    }
}
