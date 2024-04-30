// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// MagicaClothコンポーネントの処理
    /// </summary>
    public partial class ClothProcess
    {
        //=========================================================================================
        static readonly ProfilerMarker initProfiler = new ProfilerMarker("ClothProcess.Init");

        /// <summary>
        /// 初期化（必ずアニメーションの実行前に行う）
        /// </summary>
        internal void Init()
        {
            Debug.Assert(cloth);
            Develop.DebugLog($"Init start [{cloth.name}]");
            initProfiler.Begin();
            result.SetSuccess();

            try
            {
                // すでに破棄されている場合はエラーとする。再初期化はできない
                if (isDestory)
                {
                    Develop.LogError($"Already destroyed components cannot be reinitialized.");
                    throw new OperationCanceledException();
                }

                // すでに初期化済みならスキップ
                if (IsState(State_InitComplete))
                {
                    throw new OperationCanceledException();
                }

                var sdata = cloth.SerializeData;
                var sdata2 = cloth.GetSerializeData2();

                SetState(State_Valid, false);

                // アニメーション用プロパティ初期化
                cloth.InitAnimationProperty();

                // クロスを生成するための最低限の情報が揃っているかチェックする
                if (sdata.IsValid() == false)
                {
                    result.SetResult(sdata.VerificationResult);
                    throw new OperationCanceledException();
                }

                SetState(State_InitComplete, true);

                // PreBuildデータの利用と検証
                bool usePreBuildData = sdata2.preBuildData.UsePreBuild();
                SharePreBuildData sharePreBuildData = null;
                if (usePreBuildData)
                {
                    SetState(State_UsePreBuild, true);
                    var r = sdata2.preBuildData.DataValidate();
                    if (r.IsFaild())
                    {
                        result.Merge(r);
                        throw new OperationCanceledException();
                    }

                    sharePreBuildData = sdata2.preBuildData.GetSharePreBuildData();
                }

                // 基本情報
                clothType = sdata.clothType;
                reductionSettings = sdata.reductionSetting;
                parameters = sdata.GetClothParameters();

                // 初期トランスフォーム状態
                clothTransformRecord = new TransformRecord(cloth.ClothTransform);
                if (usePreBuildData)
                {
                    // Pre-Buildでは編集時スケールを復元する
                    clothTransformRecord.scale = sharePreBuildData.buildScale;
                }

                // 法線調整用トランスフォーム
                normalAdjustmentTransformRecord = new TransformRecord(
                    sdata.normalAlignmentSetting.adjustmentTransform ?
                    sdata.normalAlignmentSetting.adjustmentTransform :
                    cloth.ClothTransform);

                // PreBuildデータの登録
                PreBuildManager.ShareDeserializationData sharePreBuildDeserializeData = usePreBuildData ? MagicaManager.PreBuild.RegisterPreBuildData(sharePreBuildData, true) : null;
                UniquePreBuildData uniquePreBuildData = usePreBuildData ? sdata2.preBuildData.uniquePreBuildData : null;

                // レンダラーとセットアップ情報の初期化
                // なおセットアップ情報はVirtualMeshを生成するためのものなのでベイク構築時は不要
                if (clothType == ClothType.MeshCloth)
                {
                    // MeshCloth
                    // 必要なレンダラーを登録する
                    for (int i = 0; i < sdata.sourceRenderers.Count; i++)
                    {
                        var ren = sdata.sourceRenderers[i];
                        if (ren)
                        {
                            // PreBuildではセットアップ情報を復元する
                            RenderSetupData setup = null;
                            RenderSetupData.UniqueSerializationData uniquePreBuildSetupData = null;
                            if (usePreBuildData)
                            {
                                setup = sharePreBuildDeserializeData.renderSetupDataList[i];
                                uniquePreBuildSetupData = uniquePreBuildData.renderSetupDataList[i];

                                if (setup.result.IsFaild())
                                {
                                    setup.Dispose();
                                    result.SetError(Define.Result.PreBuild_SetupDeserializationError);
                                    throw new OperationCanceledException();
                                }
                            }

                            int handle = AddRenderer(ren, setup, uniquePreBuildSetupData);
                            if (handle == 0)
                            {
                                result.SetError(Define.Result.ClothInit_FailedAddRenderer);
                                throw new OperationCanceledException();
                            }
                            var rdata = MagicaManager.Render.GetRendererData(handle);
                            result.Merge(rdata.Result);
                            if (rdata.Result.IsFaild())
                            {
                                throw new OperationCanceledException();
                            }
                        }
                    }
                }
                else if (clothType == ClothType.BoneCloth && usePreBuildData == false)
                {
                    // BoneCloth
                    CreateBoneRenderSetupData(clothType, sdata.rootBones, null, sdata.connectionMode);
                }
                else if (clothType == ClothType.BoneSpring && usePreBuildData == false)
                {
                    // BoneSpring
                    // BoneSpringではLine接続のみ
                    CreateBoneRenderSetupData(clothType, sdata.rootBones, sdata.colliderCollisionConstraint.collisionBones, RenderSetupData.BoneConnectionMode.Line);
                }

                // カスタムスキニングのボーン情報
                int bcnt = sdata.customSkinningSetting.skinningBones.Count;
                for (int i = 0; i < bcnt; i++)
                {
                    customSkinningBoneRecords.Add(new TransformRecord(sdata.customSkinningSetting.skinningBones[i]));
                }

                result.SetSuccess();
                SetState(State_Valid, true);
                SetState(State_InitSuccess, true);

                // この時点でクロスコンポーネントが非アクティブの場合は破棄監視リストに登録する
                if (cloth.isActiveAndEnabled == false)
                    MagicaManager.Team.AddMonitoringProcess(this);
            }
            catch (OperationCanceledException)
            {
            }
            catch (MagicaClothProcessingException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                result.SetError(Define.Result.ClothProcess_Exception);
            }

            initProfiler.End();

            if (result.IsSuccess())
                Develop.DebugLog($"Cloth Initialize Success! [{cloth.name}]");
            else
                Develop.DebugLogError($"Cloth Initialize failure! [{cloth.name}] : {result.GetResultString()}");
        }

        /// <summary>
        /// MeshClothの利用を登録する（メインスレッドのみ）
        /// これはAwake()などのアニメーションの前に実行すること
        /// </summary>
        /// <param name="ren"></param>
        /// <returns>レンダー情報ハンドル</returns>
        int AddRenderer(Renderer ren, RenderSetupData referenceSetupData, RenderSetupData.UniqueSerializationData referenceUniqueSetupData)
        {
            if (ren == null)
                return 0;
            if (renderHandleList == null)
                return 0;

            int handle = ren.GetInstanceID();
            if (renderHandleList.Contains(handle) == false)
            {
                // レンダラーの利用開始
                handle = MagicaManager.Render.AddRenderer(ren, referenceSetupData, referenceUniqueSetupData);
                if (handle != 0)
                {
                    lock (lockObject)
                    {
                        if (renderHandleList.Contains(handle) == false)
                            renderHandleList.Add(handle);
                    }
                }
            }

            return handle;
        }

        /// <summary>
        /// BoneClothの利用を開始する（メインスレッドのみ）
        /// これはAwake()などのアニメーションの前に実行すること
        /// </summary>
        /// <param name="rootTransforms"></param>
        /// <param name="connectionMode"></param>
        void CreateBoneRenderSetupData(ClothType ctype, List<Transform> rootTransforms, List<Transform> collisionBones, RenderSetupData.BoneConnectionMode connectionMode)
        {
            // BoneCloth用のセットアップデータ作成
            boneClothSetupData = new RenderSetupData(
                ctype == ClothType.BoneSpring ? RenderSetupData.SetupType.BoneSpring : RenderSetupData.SetupType.BoneCloth,
                clothTransformRecord.transform,
                rootTransforms,
                collisionBones,
                connectionMode,
                cloth.name
                );
        }

        /// <summary>
        /// 有効化
        /// </summary>
        internal void StartUse()
        {
            if (MagicaManager.IsPlaying() == false)
                return;

            // 有効化
            SetState(State_Enable, true);

            // チーム有効化
            MagicaManager.Team.SetEnable(TeamId, true);

            // レンダラー有効化
            if (renderHandleList != null)
            {
                foreach (int renderHandle in renderHandleList)
                {
                    MagicaManager.Render.StartUse(this, renderHandle);
                }
            }
        }

        /// <summary>
        /// 無効化
        /// </summary>
        internal void EndUse()
        {
            if (MagicaManager.IsPlaying() == false)
                return;

            // 無効化
            SetState(State_Enable, false);

            // チーム無効化
            MagicaManager.Team.SetEnable(TeamId, false);

            // レンダラー無効化
            if (renderHandleList != null)
            {
                foreach (int renderHandle in renderHandleList)
                {
                    MagicaManager.Render.EndUse(this, renderHandle);
                }
            }
        }

        /// <summary>
        /// パラメータ/データの変更通知
        /// </summary>
        internal void DataUpdate()
        {
            // パラメータ検証
            cloth.SerializeData.DataValidate();
            cloth.serializeData2.DataValidate();

            // パラメータ変更（実行時のみ）
            if (Application.isPlaying)
            {
                // ここでは変更フラグのみ立てる
                SetState(State_ParameterDirty, true);
            }
        }

        //=========================================================================================
        /// <summary>
        /// 構築を開始し完了後に自動実行する
        /// </summary>
        internal bool StartRuntimeBuild()
        {
            // ビルド開始
            // -コンポーネントが有効であること
            // -初期化済みであること
            // -ビルドがまだ実行されていないこと
            // -ベイクデータを利用しないこと
            if (IsValid() && IsState(State_InitSuccess) && IsState(State_Build) == false && IsState(State_UsePreBuild) == false)
            {
                result.SetProcess();
                SetState(State_Build, true);
                var _ = RuntimeBuildAsync(cts.Token);
                return true;
            }
            else
            {
                result.SetError(Define.Result.CreateCloth_CanNotStart);
                Develop.LogError($"Cloth runtime build failure! [{cloth.name}] : {result.GetResultString()}");
                return false;
            }
        }

        /// <summary>
        /// 自動構築（コンポーネントのStart()で呼ばれる）
        /// </summary>
        /// <returns></returns>
        internal bool AutoBuild()
        {
            bool ret;
            bool buildComplate = true;

            if (IsState(State_DisableAutoBuild))
            {
                ret = false;
            }
            else
            {
                if (IsState(State_UsePreBuild))
                    ret = PreBuildDataConstruction();
                else
                {
                    ret = StartRuntimeBuild();
                    if (ret)
                        buildComplate = false; // OnBuildCompleteはランタイム構築後に呼ばれる
                }
            }

            // ビルド完了イベント
            if (buildComplate)
                cloth?.OnBuildComplete?.Invoke(ret);

            return ret;
        }

        /// <summary>
        /// 実行時構築タスク
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        async Task RuntimeBuildAsync(CancellationToken ct)
        {
            isBuild = true;
            Develop.DebugLog($"Build start : {Name}");
            result.SetProcess();

            // 作成されたレンダラー情報
            var renderMeshInfos = new List<RenderMeshInfo>();

            // ProxyMesh
            VirtualMesh proxyMesh = null;

            try
            {
                // ■メインスレッド
                var sdata = cloth.SerializeData;
                var sdata2 = cloth.GetSerializeData2();

                // 同期対象がいる場合は相手の一時停止カウンターを加算する
                if (cloth.SyncCloth)
                {
                    var sync = cloth.SyncCloth;
                    while (sync != cloth && sync != null)
                    {
                        sync.Process.IncrementSuspendCounter();
                        sync = sync.SyncCloth;
                    }
                }

                // ペイントマップデータの作成（これはメインスレッドでのみ作成可能）
                bool usePaintMap = false;
                var paintMapDataList = new List<PaintMapData>();
                if (sdata.clothType == ClothType.MeshCloth && sdata.paintMode != ClothSerializeData.PaintMode.Manual)
                {
                    var ret = GeneratePaintMapDataList(paintMapDataList);
                    Develop.DebugLog($"Generate paint map data list. {ret.GetResultString()}");
                    if (ret.IsError())
                    {
                        result.Merge(ret);
                        throw new MagicaClothProcessingException();
                    }
                    if (paintMapDataList.Count != renderHandleList.Count)
                    {
                        result.SetError(Define.Result.CreateCloth_PaintMapCountMismatch);
                        throw new MagicaClothProcessingException();
                    }
                    usePaintMap = true;
                }

                // セレクションデータ
                // ペイントマップ指定の場合は空で初期化
                SelectionData selectionData = usePaintMap ? new SelectionData() : sdata2.selectionData.Clone();

                // BoneCloth/BoneSpringでシリアライズ２にTransformと属性辞書がある場合はIDと属性の辞書に変換（スレッドではアクセスできないため）
                Dictionary<int, VertexAttribute> boneAttributeDict = null;
                if (sdata2.boneAttributeDict.Count > 0)
                {
                    boneAttributeDict = new Dictionary<int, VertexAttribute>(sdata2.boneAttributeDict.Count);
                    foreach (var kv in sdata2.boneAttributeDict)
                    {
                        if (kv.Key)
                        {
                            boneAttributeDict.Add(kv.Key.GetInstanceID(), kv.Value);
                        }
                    }
                }

                // ■スレッド
                await Task.Run(() =>
                {
                    // 作業用メッシュ
                    VirtualMesh renderMesh = null;

                    try
                    {
                        // プロキシメッシュ作成
                        ct.ThrowIfCancellationRequested();
                        proxyMesh = new VirtualMesh("Proxy");
                        proxyMesh.result.SetProcess();
                        List<int> copyRenderHandleList = null;
                        if (clothType == ClothType.MeshCloth)
                        {
                            // MeshClothではクロストランスフォームを追加しておく
                            proxyMesh.SetTransform(clothTransformRecord);

                            lock (lockObject)
                            {
                                copyRenderHandleList = new List<int>(renderHandleList);
                            }
                        }

                        // セレクションデータの有無
                        bool isValidSelection = selectionData?.IsValid() ?? false;

                        // MeshCloth/BoneClothで処理が一部異なる
                        if (clothType == ClothType.MeshCloth)
                        {
                            // ■MeshCloth
                            if (renderHandleList.Count == 0)
                            {
                                result.SetError(Define.Result.ClothProcess_InvalidRenderHandleList);
                                throw new MagicaClothProcessingException();
                            }

                            //--------------------------------------------------------------------
                            // mesh import + selection + merge
                            for (int i = 0; i < copyRenderHandleList.Count; i++)
                            {
                                ct.ThrowIfCancellationRequested();

                                int renderHandle = copyRenderHandleList[i];

                                // レンダーメッシュ作成
                                var renderData = MagicaManager.Render.GetRendererData(renderHandle);
                                renderMesh = new VirtualMesh($"[{renderData.Name}]");
                                renderMesh.result.SetProcess();

                                // import -------------------------------------------------
                                renderMesh.ImportFrom(renderData);
                                if (renderMesh.IsError)
                                {
                                    result.Merge(renderMesh.result);
                                    throw new MagicaClothProcessingException();
                                }
                                Develop.DebugLog($"(IMPORT) {renderMesh}");

                                // selection ----------------------------------------------
                                // MeshClothでペイントテクスチャ指定の場合はセレクションデータを生成する
                                SelectionData renderSelectionData = selectionData;
                                if (usePaintMap)
                                {
                                    // renderMeshからセレクションデータ生成
                                    var ret = GenerateSelectionDataFromPaintMap(clothTransformRecord, renderMesh, paintMapDataList[i], out renderSelectionData);
                                    Develop.DebugLog($"Generate selection from paint map. {ret.GetResultString()}");
                                    if (ret.IsError())
                                    {
                                        result.Merge(ret);
                                        throw new MagicaClothProcessingException();
                                    }

                                    // セレクションデータ結合
                                    selectionData.Merge(renderSelectionData);
                                }
                                isValidSelection = selectionData?.IsValid() ?? false;

                                // メッシュの切り取り
                                ct.ThrowIfCancellationRequested();
                                if (renderSelectionData?.IsValid() ?? false)
                                {
                                    // 余白
                                    float mergin = renderMesh.CalcSelectionMergin(reductionSettings);
                                    mergin = math.max(mergin, Define.System.MinimumGridSize);

                                    // セレクション情報から切り取りの実行
                                    // ペイントマップの場合はレンダラーごとのセレクションデータで切り取り
                                    renderMesh.SelectionMesh(renderSelectionData, clothTransformRecord.localToWorldMatrix, mergin);
                                    if (renderMesh.IsError)
                                    {
                                        result.Merge(renderMesh.result);
                                        throw new MagicaClothProcessingException();
                                    }
                                    Develop.DebugLog($"(SELECTION) {renderMesh}");
                                }
                                ct.ThrowIfCancellationRequested();

                                // レンダーメッシュの作成完了
                                renderMesh.result.SetSuccess();

                                // merge --------------------------------------------------
                                proxyMesh.AddMesh(renderMesh);

                                // レンダーメッシュ情報を作成
                                var info = new RenderMeshInfo();
                                //info.mixHash = mixHash;
                                info.renderHandle = renderHandle;
                                info.renderMeshContainer = new VirtualMeshContainer(renderMesh);
                                renderMesh = null;
                                renderMeshInfos.Add(info);
                            }
                            Develop.DebugLog($"(MERGE) {proxyMesh}");
                        }
                        else if (clothType == ClothType.BoneCloth || clothType == ClothType.BoneSpring)
                        {
                            // ■BoneCloth
                            // import
                            proxyMesh.ImportFrom(boneClothSetupData);
                            if (proxyMesh.IsError)
                            {
                                result.Merge(proxyMesh.result);
                                throw new MagicaClothProcessingException();
                            }
                            Develop.DebugLog($"(IMPORT) {proxyMesh}");

                            // セレクションデータが存在しない場合は簡易作成する
                            if (isValidSelection == false)
                            {
                                selectionData = new SelectionData(proxyMesh, float4x4.identity);
                                if (selectionData.Count > 0)
                                {
                                    // まずすべて移動設定
                                    selectionData.Fill(VertexAttribute.Move);

                                    // 次にルートのみ固定
                                    foreach (int id in boneClothSetupData.rootTransformIdList)
                                    {
                                        int rootIndex = boneClothSetupData.GetTransformIndexFromId(id);
                                        selectionData.attributes[rootIndex] = VertexAttribute.Fixed;
                                    }
                                    isValidSelection = selectionData.IsValid();
                                }
                            }

                            // Transformと属性辞書がある場合はそれに従って属性を書き換える
                            if (boneAttributeDict != null)
                            {
                                foreach (var kv in boneAttributeDict)
                                {
                                    int index = boneClothSetupData.GetTransformIndexFromId(kv.Key);
                                    if (index >= 0)
                                    {
                                        selectionData.attributes[index] = kv.Value;
                                    }
                                }
                            }
                        }

                        //--------------------------------------------------------------------
                        // reduction (MeshClothのみ)
                        if (clothType == ClothType.MeshCloth && proxyMesh.VertexCount > 1)
                        {
                            ct.ThrowIfCancellationRequested();
                            if (reductionSettings.IsEnabled)
                            {
                                proxyMesh.Reduction(reductionSettings, ct);
                                if (proxyMesh.IsError)
                                {
                                    result.Merge(proxyMesh.result);
                                    throw new MagicaClothProcessingException();
                                }

                                Develop.DebugLog($"(REDUCTION) {proxyMesh}");
                            }
                        }
                        if (proxyMesh.joinIndices.IsCreated == false)
                        {
                            // 元の頂点から結合頂点へのインデックスを初期化
                            ct.ThrowIfCancellationRequested();
                            proxyMesh.joinIndices = new Unity.Collections.NativeArray<int>(proxyMesh.VertexCount, Unity.Collections.Allocator.Persistent);
                            JobUtility.SerialNumberRun(proxyMesh.joinIndices, proxyMesh.VertexCount); // 連番をつける
                        }

                        //--------------------------------------------------------------------
                        // optimization
                        ct.ThrowIfCancellationRequested();
                        proxyMesh.Optimization();
                        if (proxyMesh.IsError)
                        {
                            result.Merge(proxyMesh.result);
                            throw new MagicaClothProcessingException();
                        }
                        Develop.DebugLog($"(OPTIMIZE) {proxyMesh}");

                        //--------------------------------------------------------------------
                        // attribute
                        if (isValidSelection)
                        {
                            // セレクションデータから頂点属性を付与する
                            proxyMesh.ApplySelectionAttribute(selectionData);
                            if (proxyMesh.IsError)
                            {
                                result.Merge(proxyMesh.result);
                                throw new MagicaClothProcessingException();
                            }
                        }

                        //--------------------------------------------------------------------
                        // proxy mesh（属性決定後に実行）
                        ct.ThrowIfCancellationRequested();
                        {
                            proxyMesh.ConvertProxyMesh(sdata, clothTransformRecord, customSkinningBoneRecords, normalAdjustmentTransformRecord);
                            if (proxyMesh.IsError)
                            {
                                result.Merge(proxyMesh.result);
                                throw new MagicaClothProcessingException();
                            }
                            Develop.DebugLog($"(PROXY) {proxyMesh}");
                        }

                        //--------------------------------------------------------------------
                        // ProxyMeshの最終チェック
                        if (proxyMesh.VertexCount > Define.System.MaxProxyMeshVertexCount)
                        {
                            result.SetError(Define.Result.ProxyMesh_Over32767Vertices);
                            throw new MagicaClothProcessingException();
                        }
                        if (proxyMesh.EdgeCount > Define.System.MaxProxyMeshEdgeCount)
                        {
                            result.SetError(Define.Result.ProxyMesh_Over32767Edges);
                            throw new MagicaClothProcessingException();
                        }
                        if (proxyMesh.TriangleCount > Define.System.MaxProxyMeshTriangleCount)
                        {
                            result.SetError(Define.Result.ProxyMesh_Over32767Triangles);
                            throw new MagicaClothProcessingException();
                        }

                        //--------------------------------------------------------------------
                        // finish
                        ct.ThrowIfCancellationRequested();
                        if (proxyMesh.IsError)
                        {
                            result.Merge(proxyMesh.result);
                            throw new MagicaClothProcessingException();
                        }
                        proxyMesh.result.SetSuccess();
                        Develop.DebugLog("CreateProxyMesh finish!");

                        //-------------------------------------------------------------------
                        // Mapping(MeshClothのみ)
                        if (clothType == ClothType.MeshCloth)
                        {
                            foreach (var info in renderMeshInfos)
                            {
                                ct.ThrowIfCancellationRequested();
                                var cmesh = info.renderMeshContainer;
                                var vmesh = cmesh.shareVirtualMesh;
                                vmesh.Mapping(proxyMesh);
                                if (vmesh.IsError)
                                {
                                    result.Merge(vmesh.result);
                                    throw new MagicaClothProcessingException();
                                }
                                Develop.DebugLog($"(MAPPING) {vmesh}");
                            }
                        }
                    }
                    catch (MagicaClothProcessingException)
                    {
                        throw;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                        result.SetError(Define.Result.ClothProcess_Exception);
                        throw;
                    }
                    finally
                    {
                        // この時点で作業用renderMeshが存在する場合は中断されているので開放する
                        renderMesh?.Dispose();
                    }
                }, ct);

                // ■メインスレッド
                // 同期対象がいる場合は相手の初期化完了を待つ
                ct.ThrowIfCancellationRequested();
                if (cloth == null)
                    throw new OperationCanceledException(); // キャンセル扱いにする
                var syncCloth = cloth.SyncCloth;
                if (syncCloth != null)
                {
                    int timeOutCount = 100;
                    while (syncCloth != null && syncCloth.Process.IsEnable == false && timeOutCount >= 0)
                    {
                        await Task.Delay(20);
                        ct.ThrowIfCancellationRequested();
                        timeOutCount--;
                    }
                    if (syncCloth == null || syncCloth.Process.IsEnable == false)
                    {
                        syncCloth = null;
                        Develop.LogWarning($"Sync timeout! Is there a deadlock between synchronous cloths?");
                    }
                }

                // ■メインスレッド
                ct.ThrowIfCancellationRequested();
                if (cloth == null)
                    throw new OperationCanceledException(); // キャンセル扱いにする
                if (IsValid() == false)
                {
                    result.SetError(Define.Result.ClothProcess_Invalid);
                    throw new MagicaClothProcessingException();
                }
                if (MagicaManager.IsPlaying() == false)
                {
                    result.SetError(Define.Result.ClothProcess_Invalid);
                    throw new MagicaClothProcessingException();
                }

                // パラメータ変更フラグ
                SetState(State_ParameterDirty, true);

                // ■スレッド
                ct.ThrowIfCancellationRequested();
                await Task.Run(() =>
                {
                    // ■クロスデータの作成
                    try
                    {
                        // 距離制約(Distance)
                        ct.ThrowIfCancellationRequested();
                        distanceConstraintData = DistanceConstraint.CreateData(proxyMesh, parameters);
                        if (distanceConstraintData != null && distanceConstraintData.result.IsError())
                        {
                            result.Merge(distanceConstraintData.result);
                            throw new MagicaClothProcessingException();
                        }

                        // 曲げ制約(Bending)
                        ct.ThrowIfCancellationRequested();
                        bendingConstraintData = TriangleBendingConstraint.CreateData(proxyMesh, parameters);
                        if (bendingConstraintData != null && bendingConstraintData.result.IsError())
                        {
                            result.Merge(bendingConstraintData.result);
                            throw new MagicaClothProcessingException();
                        }

                        // 慣性制約(Inertia)
                        ct.ThrowIfCancellationRequested();
                        inertiaConstraintData = InertiaConstraint.CreateData(proxyMesh, parameters);
                        if (inertiaConstraintData != null && inertiaConstraintData.result.IsError())
                        {
                            result.Merge(inertiaConstraintData.result);
                            throw new MagicaClothProcessingException();
                        }

                        if (result.IsError())
                            throw new MagicaClothProcessingException();
                    }
                    catch (MagicaClothProcessingException)
                    {
                        throw;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                        result.SetError(Define.Result.Constraint_Exception);
                        throw;
                    }
                }, ct);


                // ■メインスレッド
                ct.ThrowIfCancellationRequested();
                if (cloth == null)
                    throw new OperationCanceledException(); // キャンセル扱いにする

                // 登録
                lock (lockObject)
                {
                    // ProxyMesh登録
                    ProxyMeshContainer = new VirtualMeshContainer(proxyMesh);
                    proxyMesh = null;

                    // チーム登録
                    TeamId = MagicaManager.Cloth.AddCloth(this, parameters);
                    if (TeamId <= 0)
                    {
                        result.SetError(Define.Result.ClothProcess_OverflowTeamCount4096);
                        throw new MagicaClothProcessingException();
                    }

                    // プロキシメッシュ登録
                    MagicaManager.VMesh.RegisterProxyMesh(TeamId, ProxyMeshContainer);
                    MagicaManager.Simulation.RegisterProxyMesh(this);

                    // コライダー登録
                    MagicaManager.Collider.Register(this);
                }

                // 制約データ登録
                ct.ThrowIfCancellationRequested();
                MagicaManager.Simulation.RegisterConstraint(this);

                lock (lockObject)
                {
                    // マッピングメッシュ登録
                    if (clothType == ClothType.MeshCloth)
                    {
                        foreach (var info in renderMeshInfos)
                        {
                            var renderMesh = info.renderMeshContainer.shareVirtualMesh;

                            if (renderMesh.IsError == false && renderMesh.IsMapping)
                            {
                                // マッピングメッシュのデータ検証
                                // ここまでの時間経過でRendererが消滅しているなどの状況があり得るため
                                if (renderMesh.IsValid())
                                {
                                    // MappingMesh登録
                                    info.mappingChunk = MagicaManager.VMesh.RegisterMappingMesh(TeamId, info.renderMeshContainer);

                                    // コンポーネントがすでに有効状態ならば利用開始
                                    if (IsState(State_Enable))
                                    {
                                        MagicaManager.Render.StartUse(this, info.renderHandle);
                                    }
                                }
                            }
                        }
                    }

                    // レンダラー情報を登録
                    foreach (var info in renderMeshInfos)
                    {
                        renderMeshInfoList.Add(info);
                    }
                    renderMeshInfos.Clear();
                }
                ct.ThrowIfCancellationRequested();

                // チームの有効状態の設定
                MagicaManager.Team.SetEnable(TeamId, IsState(State_Enable));

                // ビルド完了
                result.SetSuccess();
                SetState(State_Running, true);

                Develop.DebugLog($"Build Complate : {Name}");
            }
            catch (MagicaClothProcessingException)
            {
                if (result.IsError() == false)
                    result.SetError(Define.Result.ClothProcess_UnknownError);
                result.DebugLog();
            }
            catch (OperationCanceledException)
            {
                result.SetCancel();
                result.DebugLog();
                //Debug.LogWarning($"Cancel!");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                result.SetError(Define.Result.ClothProcess_Exception);
            }
            finally
            {
                // この時点でデータが存在する場合は失敗しているので破棄する
                foreach (var info in renderMeshInfos)
                {
                    info?.renderMeshContainer?.Dispose();
                }
                proxyMesh?.Dispose();

                // 同期対象がいる場合は相手の一時停止カウンターを減算する
                if (cloth != null && cloth.SyncCloth)
                {
                    var sync = cloth.SyncCloth;
                    while (sync != cloth && sync != null)
                    {
                        sync.Process.DecrementSuspendCounter();
                        sync = sync.SyncCloth;
                    }
                }

                // ビルド完了
                isBuild = false;

                // この時点でコンポーネントが削除されている場合は破棄する
                if (isDestory)
                {
                    DisposeInternal();
                    //Debug.LogWarning($"Delay Dispose!");
                }
                else if (cloth != null)
                {
                    if (result.IsFaild())
                        Develop.LogError($"Cloth runtime build failure! [{cloth.name}] : {result.GetResultString()}");

                    // ビルド完了イベント
                    cloth.OnBuildComplete?.Invoke(result.IsSuccess());
                }
            }
        }

        /// <summary>
        /// ペイントマップからセレクションデータを構築する
        /// </summary>
        /// <param name="clothTransformRecord"></param>
        /// <param name="renderMesh"></param>
        /// <param name="paintMapData"></param>
        /// <param name="selectionData"></param>
        /// <returns></returns>
        public ResultCode GenerateSelectionDataFromPaintMap(
            TransformRecord clothTransformRecord, VirtualMesh renderMesh, PaintMapData paintMapData, out SelectionData selectionData
            )
        {
            ResultCode result = new ResultCode();
            result.SetProcess();
            selectionData = new SelectionData();

            try
            {
                // ペイントマップのチェック
                if (paintMapData == null)
                {
                    result.SetError(Define.Result.CreateCloth_PaintMapCountMismatch);
                    throw new MagicaClothProcessingException();
                }

                // セレクションデータバッファ作成
                int vcnt = renderMesh.VertexCount;
                using var positionList = new NativeArray<float3>(vcnt, Allocator.TempJob);
                using var attributeList = new NativeArray<VertexAttribute>(vcnt, Allocator.TempJob);

                // レンダーメッシュのUV値からペイントマップをフェッチしてセレクションデータを作成
                // 座標はクロス空間に変換する
                var toM = MathUtility.Transform(renderMesh.initLocalToWorld, clothTransformRecord.worldToLocalMatrix);
                int2 xySize = new int2(paintMapData.paintMapWidth, paintMapData.paintMapHeight);
                using var paintData = new NativeArray<Color32>(paintMapData.paintData, Allocator.TempJob);

                // Burstにより属性マップからセレクションデータを設定
                var job = new GenerateSelectionJob()
                {
                    offset = 0,
                    positionList = positionList,
                    attributeList = attributeList,

                    attributeMapWidth = paintMapData.paintMapWidth,
                    toM = toM,
                    xySize = xySize,
                    attributeReadFlag = paintMapData.paintReadFlag,
                    attributeMapData = paintData,

                    uvs = renderMesh.uv.GetNativeArray(), // レンダーメッシュインポート直後は元のメッシュuvが入っている
                    vertexs = renderMesh.localPositions.GetNativeArray(),
                };
                job.Run(vcnt);

                // セレクションデータ設定
                selectionData.positions = positionList.ToArray();
                selectionData.attributes = attributeList.ToArray();
                // 最大距離はプロキシメッシュの座標空間に変換する
                selectionData.maxConnectionDistance = MathUtility.TransformDistance(renderMesh.maxVertexDistance.Value, toM);
                //Develop.DebugLog($"GenerateSelectionDataFromPaintMap. maxConnectionDistance:{selectionData.maxConnectionDistance}, renderMesh.maxVertexDistance:{renderMesh.maxVertexDistance.Value}");
                selectionData.userEdit = true;

                result.SetSuccess();
            }
            catch (MagicaClothProcessingException)
            {
                if (result.IsNone()) result.SetError(Define.Result.CreateCloth_InvalidPaintMap);
                result.DebugLog();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                result.SetError(Define.Result.CreateCloth_InvalidPaintMap);
            }

            return result;
        }

        [BurstCompile]
        struct GenerateSelectionJob : IJobParallelFor
        {
            public int offset;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> positionList;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<VertexAttribute> attributeList;

            public int attributeMapWidth;
            public float4x4 toM;
            public int2 xySize;
            public ExBitFlag8 attributeReadFlag;
            [Unity.Collections.ReadOnly]
            public NativeArray<Color32> attributeMapData;

            [Unity.Collections.ReadOnly]
            public NativeArray<float2> uvs;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> vertexs;

            public void Execute(int vindex)
            {
                float2 uv = uvs[vindex];

                // uv値を(0.0 ~ 0.99999..)範囲に変換する
                uv = (uv % 1.0f) + 1.0f;
                uv = uv % 1.0f;

                int2 xy = (int2)(uv * xySize);
                Color32 col = attributeMapData[xy.y * attributeMapWidth + xy.x];

                // 属性判定
                const byte border = 32; // 127
                var attr = new VertexAttribute();
                if (attributeReadFlag.IsSet(PaintMapData.ReadFlag_Move) && col.g > border)
                    attr.SetFlag(VertexAttribute.Flag_Move, true);
                else if (attributeReadFlag.IsSet(PaintMapData.ReadFlag_Fixed) && col.r > border)
                    attr.SetFlag(VertexAttribute.Flag_Fixed, true);
                if (attributeReadFlag.IsSet(PaintMapData.ReadFlag_Limit) && col.b <= border)
                    attr.SetFlag(VertexAttribute.Flag_InvalidMotion, true); // 塗りつぶしていない箇所が無効

                // 設定
                var pos = math.transform(toM, vertexs[vindex]); // クロスコンポーネント空間に変換
                positionList[offset + vindex] = pos;
                attributeList[offset + vindex] = attr;
            }
        }

        /// <summary>
        /// ペイントマップからテクスチャデータを取得してその情報をリストで返す
        /// ミップマップが存在する場合は約128x128サイズ以下のミップマップを採用する
        /// この処理はメインスレッドでしか動作せず、またそれなりの負荷がかかるので注意！
        /// </summary>
        /// <returns></returns>
        public ResultCode GeneratePaintMapDataList(List<PaintMapData> dataList)
        {
            var result = new ResultCode();
            result.SetProcess();

            try
            {
                int mapCount = cloth.SerializeData.paintMaps.Count;

                // テクスチャ読み込みフラグ
                var readFlag = new ExBitFlag8(PaintMapData.ReadFlag_Fixed | PaintMapData.ReadFlag_Move);
                if (cloth.SerializeData.paintMode == ClothSerializeData.PaintMode.Texture_Fixed_Move_Limit)
                    readFlag.SetFlag(PaintMapData.ReadFlag_Limit, true);

                for (int i = 0; i < mapCount; i++)
                {
                    var paintMap = cloth.SerializeData.paintMaps[i];
                    if (paintMap == null)
                    {
                        result.SetError(Define.Result.Init_InvalidPaintMap);
                        throw new MagicaClothProcessingException();
                    }
                    if (paintMap.isReadable == false)
                    {
                        result.SetError(Define.Result.Init_PaintMapNotReadable);
                        throw new MagicaClothProcessingException();
                    }
                    int width = paintMap.width;
                    int height = paintMap.height;
                    int pixelCount = width * height;
                    int mip = 1;
                    while (mip < paintMap.mipmapCount && pixelCount > 16384) // 128 x 128 = 16384
                    {
                        mip++;
                        pixelCount /= 4;
                        width /= 2;
                        height /= 2;
                    }
                    Develop.DebugLog($"[{paintMap.name}] target mipmap:{mip - 1} ,pixelCount:{pixelCount}, w:{width}, h:{height}");

                    // ピクセルデータの取得
                    // !現状CPU側から圧縮テクスチャのピクセルデータを取得するにはこの方法しかない。
                    // !GetPixelData()ではRGB32/RGBA32以外のフォーマットに対応できない。
                    // !この関数はメインスレッドでしか動作せず、また処理負荷もそれなりにあるので注意！(128x128で0.3msほど)
                    var data = new PaintMapData();
                    data.paintData = paintMap.GetPixels32(mip - 1);
                    Develop.DebugLog($"paintMapData:{data.paintData.Length}");
                    data.paintMapWidth = width;
                    data.paintMapHeight = height;
                    data.paintReadFlag = readFlag;
                    dataList.Add(data);
                }

                result.SetSuccess();
            }
            catch (MagicaClothProcessingException)
            {
                result.DebugLog();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                result.SetError(Define.Result.Init_InvalidPaintMap);
            }

            return result;
        }

        //=========================================================================================
        static readonly ProfilerMarker preBuildProfiler = new ProfilerMarker("ClothProcess.PreBuild");
        static readonly ProfilerMarker preBuildDeserializationProfiler = new ProfilerMarker("ClothProcess.PreBuild.Deserialization");
        static readonly ProfilerMarker preBuildRegistrationProfiler = new ProfilerMarker("ClothProcess.PreBuild.Registration");

        /// <summary>
        /// PreBuildデータによる即時構築
        /// </summary>
        /// <returns></returns>
        internal bool PreBuildDataConstruction()
        {
            if (IsState(State_UsePreBuild) == false)
                return false;
            if (IsState(State_InitSuccess) == false)
                return false;

            // 構築開始
            Develop.DebugLog($"Pre-Build start [{cloth.name}]");
            preBuildProfiler.Begin();

            result.SetProcess();
            var sdata = cloth.SerializeData;
            var sdata2 = cloth.GetSerializeData2();

            VirtualMeshContainer proxyMeshContainer = null;
            List<VirtualMeshContainer> renderMeshContainerList = new List<VirtualMeshContainer>();

            try
            {
                // 固有部分データ
                var uniquePreBuildData = sdata2.preBuildData.uniquePreBuildData;

                // 共有部分データ
                var preBuildDeserializeData = MagicaManager.PreBuild.GetPreBuildData(sdata2.preBuildData.GetSharePreBuildData());

                try
                {
                    preBuildDeserializationProfiler.Begin();

                    // ProxyMesh復元
                    proxyMeshContainer = new VirtualMeshContainer(preBuildDeserializeData.proxyMesh);
                    if (proxyMeshContainer.shareVirtualMesh.IsError)
                    {
                        result.Merge(proxyMeshContainer.shareVirtualMesh.result);
                        throw new MagicaClothProcessingException();
                    }
                    proxyMeshContainer.uniqueData = uniquePreBuildData.proxyMesh;

                    // RenderMesh復元
                    for (int i = 0; i < preBuildDeserializeData.renderMeshList.Count; i++)
                    {
                        var renderMeshContainer = new VirtualMeshContainer(preBuildDeserializeData.renderMeshList[i]);
                        renderMeshContainerList.Add(renderMeshContainer);
                        if (renderMeshContainer.shareVirtualMesh.IsError)
                        {
                            result.Merge(renderMeshContainer.shareVirtualMesh.result);
                            throw new MagicaClothProcessingException();
                        }
                        renderMeshContainer.uniqueData = uniquePreBuildData.renderMeshList[i];
                    }

                    // 制約データ復元
                    inertiaConstraintData = preBuildDeserializeData.inertiaConstraintData;
                    distanceConstraintData = preBuildDeserializeData.distanceConstraintData;
                    bendingConstraintData = preBuildDeserializeData.bendingConstraintData;
                }
                catch
                {
                    throw;
                }
                finally
                {
                    preBuildDeserializationProfiler.End();
                }

                // パラメータ変更フラグ
                SetState(State_ParameterDirty, true);

                // 登録
                try
                {
                    preBuildRegistrationProfiler.Begin();

                    // ProxyMesh登録
                    ProxyMeshContainer = proxyMeshContainer;
                    proxyMeshContainer = null;

                    // チーム登録
                    TeamId = MagicaManager.Cloth.AddCloth(this, parameters);
                    if (TeamId <= 0)
                    {
                        result.SetError(Define.Result.ClothProcess_OverflowTeamCount4096);
                        throw new MagicaClothProcessingException();
                    }

                    // プロキシメッシュ登録
                    MagicaManager.VMesh.RegisterProxyMesh(TeamId, ProxyMeshContainer);
                    MagicaManager.Simulation.RegisterProxyMesh(this);

                    // コライダー登録
                    MagicaManager.Collider.Register(this);

                    // 制約データ登録
                    MagicaManager.Simulation.RegisterConstraint(this);

                    // マッピングメッシュ登録
                    for (int i = 0; i < renderMeshContainerList.Count; i++)
                    {
                        var renderMeshContainer = renderMeshContainerList[i];
                        var renderMesh = renderMeshContainer.shareVirtualMesh;
                        if (renderMesh.IsError == false && renderMesh.IsMapping && renderMesh.IsValid())
                        {
                            renderMeshContainerList[i] = null;

                            // レンダーハンドル
                            int renderHandle = renderHandleList[i];

                            // MappingMesh登録
                            var mappingChunk = MagicaManager.VMesh.RegisterMappingMesh(TeamId, renderMeshContainer);

                            // コンポーネントがすでに有効状態ならば利用開始
                            if (IsState(State_Enable))
                            {
                                MagicaManager.Render.StartUse(this, renderHandle);
                            }

                            // 完了
                            renderMesh.result.SetSuccess();

                            // レンダラー情報を登録
                            var info = new RenderMeshInfo()
                            {
                                renderHandle = renderHandle,
                                renderMeshContainer = renderMeshContainer,
                                mappingChunk = mappingChunk,
                            };
                            renderMeshInfoList.Add(info);
                        }
                    }

                    // チームの有効状態の設定
                    MagicaManager.Team.SetEnable(TeamId, IsState(State_Enable));
                }
                catch
                {
                    throw;
                }
                finally
                {
                    preBuildRegistrationProfiler.End();
                }

                // ビルド完了
                result.SetSuccess();
                SetState(State_Running, true);
            }
            catch (MagicaClothProcessingException)
            {
                if (result.IsError() == false)
                    result.SetError(Define.Result.PreBuild_UnknownError);
                result.DebugLog();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                result.SetError(Define.Result.PreBuild_Exception);
            }
            finally
            {
                // この時点でデータが存在する場合は失敗しているので破棄する
                renderMeshContainerList.ForEach(x => x?.Dispose());
                renderMeshContainerList.Clear();
                proxyMeshContainer?.Dispose();

                // ビルド完了
                //Develop.DebugLog($"PreBuild Construction Complate.[{cloth.name}] result:{result.GetResultString()}");
                if (result.IsFaild())
                    Develop.LogError($"Cloth Pre-Build construction failure! [{cloth.name}] : {result.GetResultString()}");
                else
                    Develop.DebugLog($"Cloth Pre-Build Success! [{cloth.name}]");
            }

            preBuildProfiler.End();

            return result.IsSuccess();
        }


        //=========================================================================================
        /// <summary>
        /// コライダーの現在のローカルインデックスを返す
        /// </summary>
        /// <param name="col"></param>
        /// <returns>(-1)存在しない</returns>
        internal int GetColliderIndex(ColliderComponent col)
        {
            return colliderList.IndexOf(col);
        }

        //=========================================================================================
        /// <summary>
        /// カリング連動アニメーターとレンダラーを更新
        /// </summary>
        internal void UpdateCullingAnimatorAndRenderers()
        {
            var cullingSettings = cloth.SerializeData.cullingSettings;

            // 連動アニメーター更新
            if (cullingSettings.cameraCullingMode == CullingSettings.CameraCullingMode.AnimatorLinkage
                || cullingSettings.cameraCullingMethod == CullingSettings.CameraCullingMethod.AutomaticRenderer
                || cloth.SerializeData.updateMode == ClothUpdateMode.AnimatorLinkage)
            {
                interlockingAnimator = cloth.GetComponentInParent<Animator>();
            }

            // 連動レンダラー更新
            if (cullingSettings.cameraCullingMethod == CullingSettings.CameraCullingMethod.AutomaticRenderer && interlockingAnimator)
            {
                // ★GetComponentsInChildrenのコストはキャラクタ100体で1msほど。
                // ★もしコストが問題となるようならばキャッシュする
                interlockingAnimatorRenderers.Clear();
                interlockingAnimator.GetComponentsInChildren<Renderer>(interlockingAnimatorRenderers);
            }
        }

        /// <summary>
        /// 保持しているレンダーデータに対して更新を指示する
        /// </summary>
        internal void UpdateRendererUse()
        {
            // 対応するレンダーデータに更新を指示する
            renderHandleList.ForEach(handle => MagicaManager.Render.GetRendererData(handle).UpdateUse(null, 0));
        }
    }
}
