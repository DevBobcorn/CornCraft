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
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// MagicaClothコンポーネントの処理
    /// </summary>
    public partial class ClothProcess
    {
        //=========================================================================================
        /// <summary>
        /// 初期化（★必ずアニメーションの実行前に行う）
        /// </summary>
        internal void Init()
        {
            Debug.Assert(cloth);
            Develop.DebugLog($"Init start :{cloth.name}");

            // すでに初期化済みならスキップ
            if (IsState(State_InitComplete))
            {
                return;
            }

            SetState(State_Valid, false);
            result.SetProcess();
            var sdata = cloth.SerializeData;

            // クロスを生成するための最低限の情報が揃っているかチェックする
            if (sdata.IsValid() == false)
            {
                result.SetResult(Define.Result.Empty);
                return;
            }

            // 基本情報
            clothType = sdata.clothType;
            reductionSettings = sdata.reductionSetting;
            parameters = sdata.GetClothParameters();

            // 初期トランスフォーム状態
            clothTransformRecord = new TransformRecord(cloth.ClothTransform);

            // 法線調整用トランスフォーム
            normalAdjustmentTransformRecord = new TransformRecord(
                sdata.normalAlignmentSetting.adjustmentTransform ?
                sdata.normalAlignmentSetting.adjustmentTransform :
                cloth.ClothTransform);

            // レンダラーとセットアップ情報の初期化
            if (clothType == ClothType.MeshCloth)
            {
                // MeshCloth
                // 必要なレンダラーを登録する
                foreach (var ren in sdata.sourceRenderers)
                {
                    if (ren)
                    {
                        int handle = AddRenderer(ren);
                        if (handle == 0)
                        {
                            result.SetError(Define.Result.ClothInit_FailedAddRenderer);
                            return;
                        }
                        var rdata = MagicaManager.Render.GetRendererData(handle);
                        result.Merge(rdata.Result);
                        if (rdata.Result.IsFaild())
                        {
                            return;
                        }
                    }
                }
            }
            else if (clothType == ClothType.BoneCloth)
            {
                // BoneCloth
                // 必要なボーンを登録する
                AddBoneCloth(sdata.rootBones, sdata.connectionMode);
            }

            // カスタムスキニングのボーン情報
            int bcnt = sdata.customSkinningSetting.skinningBones.Count;
            for (int i = 0; i < bcnt; i++)
            {
                customSkinningBoneRecords.Add(new TransformRecord(sdata.customSkinningSetting.skinningBones[i]));
            }

            result.SetSuccess();
            SetState(State_Valid, true);
            SetState(State_InitComplete, true);
            Develop.DebugLog($"Init finish :{cloth.name}");
        }

        /// <summary>
        /// MeshClothの利用を登録する（メインスレッドのみ）
        /// これはAwake()などのアニメーションの前に実行すること
        /// </summary>
        /// <param name="ren"></param>
        /// <returns>レンダー情報ハンドル</returns>
        int AddRenderer(Renderer ren)
        {
            if (ren == null)
                return 0;
            if (renderHandleList == null)
                return 0;

            int handle = ren.GetInstanceID();
            if (renderHandleList.Contains(handle) == false)
            {
                // レンダラーの利用開始
                handle = MagicaManager.Render.AddRenderer(ren);
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
        void AddBoneCloth(List<Transform> rootTransforms, RenderSetupData.BoneConnectionMode connectionMode)
        {
            // BoneCloth用のセットアップデータ作成
            boneClothSetupData = new RenderSetupData(clothTransformRecord.transform, rootTransforms, connectionMode, cloth.name);
        }

#if false
        /// <summary>
        /// セレクションデータハッシュとレンダラーハッシュを１つに結合したものを返す
        /// </summary>
        /// <param name="selectionHash"></param>
        /// <param name="renderHash"></param>
        /// <returns></returns>
        int GetSelectionAndRenderMixHash(int selectionHash, int renderHash)
        {
            return selectionHash + renderHash;
        }
#endif

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
        internal bool StartBuild()
        {
            // ビルド開始
            // -コンポーネントが有効であること
            // -初期化済みであること
            // -ビルドがまだ実行されていないこと
            if (IsValid() && IsState(State_InitComplete) && IsState(State_Build) == false)
            {
                result.SetProcess();
                SetState(State_Build, true);
                var _ = BuildAsync(cts.Token);
                return true;
            }
            else
            {
                Develop.LogError($"Cloth build failure!: {cloth.name}");

                // ビルド完了イベント
                cloth?.OnBuildComplete?.Invoke(false);

                return false;
            }
        }

        /// <summary>
        /// 自動構築（コンポーネントのStart()で呼ばれる）
        /// </summary>
        /// <returns></returns>
        internal bool AutoBuild()
        {
            if (IsState(State_DisableAutoBuild) == false)
                return StartBuild();
            else
                return false;
        }

        /// <summary>
        /// 構築タスク
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        async Task BuildAsync(CancellationToken ct)
        {
            isBuild = true;
            Develop.DebugLog($"Build start : {Name}");
            result.SetProcess();
#if MC2_DEBUG
            var span = new TimeSpan("Build Cloth");
#endif

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

                        // セレクションデータ
                        // ペイントマップ指定の場合は空で初期化
                        //SelectionData selectionData = usePaintMap ? new SelectionData() : sdata2.selectionData;

                        // セレクションデータの有無
                        bool isValidSelection = selectionData?.IsValid() ?? false;
                        //Develop.Log($"セレクションデータの有無:{isValidSelection}");

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
                                info.renderMesh = renderMesh;
                                renderMesh = null;
                                renderMeshInfos.Add(info);
                            }
                            Develop.DebugLog($"(MERGE) {proxyMesh}");
                        }
                        else if (clothType == ClothType.BoneCloth)
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
#if false
                    // pitch/yaw個別制限はv1.0では実装しないので一旦停止
                    // 角度制限計算用回転を作成
                    ct.ThrowIfCancellationRequested();
                    proxyMesh.CreateAngleCalcLocalRotation(normalCalculation, normalCalculationCenter);
                    if (proxyMesh.IsError)
                        throw new InvalidOperationException();
#endif

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
                                var vmesh = info.renderMesh;
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

                // 自チームと同期チームのデータ（コピー）
                //var teamData = MagicaManager.Team.GetTeamData(TeamId);
                //var syncTeamData = syncCloth != null ? MagicaManager.Team.GetTeamData(syncCloth.Process.TeamId) : default;

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

                        // セルフコリジョン２(SelfCollision2)
                        //ct.ThrowIfCancellationRequested();
                        //self2ConstraintData = SelfCollisionConstraint2.CreateData(TeamId, teamData, ProxyMesh, parameters, syncCloth?.Process?.TeamId ?? 0, syncTeamData, syncCloth?.Process?.ProxyMesh);
                        //if (self2ConstraintData != null && self2ConstraintData.result.IsError())
                        //    result = self2ConstraintData.result;

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
                    ProxyMesh = proxyMesh;
                    proxyMesh = null;

                    // チーム登録
                    TeamId = MagicaManager.Cloth.AddCloth(this, parameters);
                    if (TeamId <= 0)
                    {
                        result.SetError(Define.Result.ClothProcess_OverflowTeamCount4096);
                        throw new MagicaClothProcessingException();
                    }

                    // プロキシメッシュ登録
                    MagicaManager.VMesh.RegisterProxyMesh(TeamId, ProxyMesh);
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
                            if (info.renderMesh.IsError == false && info.renderMesh.IsMapping)
                            {
                                // マッピングメッシュのデータ検証
                                // ここまでの時間経過でRendererが消滅しているなどの状況があり得るため
                                if (info.renderMesh.IsValid())
                                {
                                    // MappingMesh登録
                                    info.mappingChunk = MagicaManager.VMesh.RegisterMappingMesh(TeamId, info.renderMesh);

                                    // 完了
                                    info.renderMesh.result.SetSuccess();

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

                // 初期化完了
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
                    info?.renderMesh?.Dispose();
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
#if MC2_DEBUG
                span.DebugLog();
#endif

                // この時点でコンポーネントが削除されている場合は破棄する
                if (isDestory)
                {
                    DisposeInternal();
                    //Debug.LogWarning($"Delay Dispose!");
                }
                else if (cloth != null)
                {
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
        // 単体マッピング
        // メモ
        // ・レンダラーの情報はすべてスレッドローカルで作成して処理する
        // ・成功した場合は最後に書き戻す

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
                || cullingSettings.cameraCullingMethod == CullingSettings.CameraCullingMethod.AutomaticRenderer)
            {
                cullingAnimator = cloth.GetComponentInParent<Animator>();
            }

            // 連動レンダラー更新
            if (cullingSettings.cameraCullingMethod == CullingSettings.CameraCullingMethod.AutomaticRenderer && cullingAnimator)
            {
                // ★GetComponentsInChildrenのコストはキャラクタ100体で1msほど。
                // ★もしコストが問題となるようならばキャッシュする
                cullingAnimatorRenderers.Clear();
                cullingAnimator.GetComponentsInChildren<Renderer>(cullingAnimatorRenderers);
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
