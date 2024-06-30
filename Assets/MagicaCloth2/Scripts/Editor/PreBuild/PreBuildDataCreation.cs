// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// PreBuildDataの作成
    /// </summary>
    public static class PreBuildDataCreation
    {
        /// <summary>
        /// PreBuildDataを作成しアセットとして保存する.
        /// Create PreBuildData and save it as an asset.
        /// </summary>
        /// <param name="cloth"></param>
        /// <param name="useNewSaveDialog">Show save dialog if ScriptableObject does not exist.</param>
        /// <returns></returns>
        public static ResultCode CreatePreBuildData(MagicaCloth cloth, bool useNewSaveDialog = true)
        {
            var sdata = cloth.SerializeData;
            var preBuildData = cloth.GetSerializeData2().preBuildData;
            string buildId = preBuildData.buildId;

            // ビルドIDが存在しない場合はここで新規作成する
            if (string.IsNullOrEmpty(buildId))
            {
                buildId = PreBuildSerializeData.GenerateBuildID();
            }

            // スクリプタブルオブジェクトへ保存
            if (preBuildData.preBuildScriptableObject == null && useNewSaveDialog)
            {
                string assetName = $"MagicaPreBuild_{buildId}";

                // 保存フォルダ読み込み
                const string StateKey = "MagicaCloth2_PreBuild_Folder";
                string path = SessionState.GetString(StateKey, "Assets/");

                string assetPath = EditorUtility.SaveFilePanelInProject("Saving MagicaCloth pre-build data", assetName, "asset", "MagicaCloth pre-build data name", path);
                //Debug.Log($"AssetPath:{assetPath}");
                if (string.IsNullOrEmpty(assetPath))
                    return new ResultCode(Define.Result.Cancel);

                // 保存フォルダ書き込み
                SessionState.SetString(StateKey, System.IO.Path.GetDirectoryName(assetPath));

                var sobj = ScriptableObject.CreateInstance<PreBuildScriptableObject>();
                AssetDatabase.CreateAsset(sobj, assetPath);
                AssetDatabase.Refresh();

                preBuildData.preBuildScriptableObject = sobj;
            }

            var preBuildScriptableObject = preBuildData.preBuildScriptableObject;
            if (preBuildScriptableObject == null)
                return new ResultCode(Define.Result.PreBuild_InvalidPreBuildData);

            // 構築
            var sharePreBuildData = new SharePreBuildData();
            var uniquePreBuildData = new UniquePreBuildData();
            if (sdata.IsValid())
            {
                MakePreBuildData(cloth, buildId, sharePreBuildData, uniquePreBuildData);
            }
            else
            {
                sharePreBuildData.buildResult.SetError(Define.Result.PreBuildData_InvalidClothData);
            }

            // データシリアライズ
            preBuildData.buildId = buildId;
            preBuildScriptableObject.AddPreBuildData(sharePreBuildData);
            preBuildData.uniquePreBuildData = uniquePreBuildData;

            EditorUtility.SetDirty(preBuildScriptableObject);
            EditorUtility.SetDirty(cloth);
            AssetDatabase.Refresh();

            return sharePreBuildData.buildResult;
        }

        static void MakePreBuildData(MagicaCloth cloth, string buildId, SharePreBuildData sharePreBuildData, UniquePreBuildData uniquePreBuildData)
        {
            //Debug.Log($"MakePreBuildData().start");
            //var span = new TimeSpan("PreBuild");

            sharePreBuildData.version = Define.System.LatestPreBuildVersion;
            sharePreBuildData.buildId = buildId;
            sharePreBuildData.buildResult.SetProcess();
            uniquePreBuildData.version = Define.System.LatestPreBuildVersion;
            uniquePreBuildData.buildResult.SetProcess();

            var setupDataList = new List<RenderSetupData>();
            VirtualMesh proxyMesh = null;
            var renderMeshList = new List<VirtualMesh>();

            try
            {
                var sdata = cloth.SerializeData;
                var sdata2 = cloth.GetSerializeData2();
                var clothType = sdata.clothType;

                //======================== Initialize ============================
                // クロスを生成するための最低限の情報が揃っているかチェックする
                if (sdata.IsValid() == false)
                {
                    sharePreBuildData.buildResult.SetResult(sdata.VerificationResult);
                    throw new MagicaClothProcessingException();
                }

                // 初期トランスフォーム状態
                var clothTransformRecord = new TransformRecord(cloth.ClothTransform);

                // 法線調整用トランスフォーム
                var normalAdjustmentTransformRecord = new TransformRecord(
                    sdata.normalAlignmentSetting.adjustmentTransform ?
                    sdata.normalAlignmentSetting.adjustmentTransform :
                    cloth.ClothTransform);

                // セットアップ情報の初期化
                if (clothType == ClothProcess.ClothType.MeshCloth)
                {
                    foreach (var ren in sdata.sourceRenderers)
                    {
                        if (ren)
                        {
                            var setupData = new RenderSetupData(ren);
                            if (setupData.IsFaild())
                            {
                                sharePreBuildData.buildResult.Merge(setupData.result);
                                throw new MagicaClothProcessingException();
                            }
                            setupDataList.Add(setupData);

                            // セットアップ情報のシリアライズ
                            sharePreBuildData.renderSetupDataList.Add(setupData.ShareSerialize());
                            uniquePreBuildData.renderSetupDataList.Add(setupData.UniqueSerialize());
                        }
                    }
                }
                else if (clothType == ClothProcess.ClothType.BoneCloth || clothType == ClothProcess.ClothType.BoneSpring)
                {
                    var setupData = new RenderSetupData(
                        clothType == ClothProcess.ClothType.BoneCloth ? RenderSetupData.SetupType.BoneCloth : RenderSetupData.SetupType.BoneSpring,
                        clothTransformRecord.transform,
                        sdata.rootBones,
                        clothType == ClothProcess.ClothType.BoneCloth ? null : sdata.colliderCollisionConstraint.collisionBones,
                        clothType == ClothProcess.ClothType.BoneCloth ? sdata.connectionMode : RenderSetupData.BoneConnectionMode.Line,
                        cloth.name
                        );
                    if (setupData.IsFaild())
                    {
                        sharePreBuildData.buildResult.Merge(setupData.result);
                        throw new MagicaClothProcessingException();
                    }
                    setupDataList.Add(setupData);
                }

                // カスタムスキニングのボーン情報
                List<TransformRecord> customSkinningBoneRecords = new List<TransformRecord>();
                int bcnt = sdata.customSkinningSetting.skinningBones.Count;
                for (int i = 0; i < bcnt; i++)
                {
                    customSkinningBoneRecords.Add(new TransformRecord(sdata.customSkinningSetting.skinningBones[i]));
                }

                //======================== Proxy/Mapping Mesh ============================
                // ペイントマップ情報
                bool usePaintMap = false;
                var paintMapDataList = new List<ClothProcess.PaintMapData>();
                if (clothType == ClothProcess.ClothType.MeshCloth && sdata.paintMode != ClothSerializeData.PaintMode.Manual)
                {
                    var ret = cloth.Process.GeneratePaintMapDataList(paintMapDataList);
                    Develop.DebugLog($"Generate paint map data list. {ret.GetResultString()}");
                    if (ret.IsError())
                    {
                        sharePreBuildData.buildResult.Merge(ret);
                        throw new MagicaClothProcessingException();
                    }
                    //if (paintMapDataList.Count != renderHandleList.Count)
                    if (paintMapDataList.Count != setupDataList.Count)
                    {
                        sharePreBuildData.buildResult.SetError(Define.Result.CreateCloth_PaintMapCountMismatch);
                        throw new MagicaClothProcessingException();
                    }
                    usePaintMap = true;
                }

                // セレクションデータ
                SelectionData selectionData = usePaintMap ? new SelectionData() : sdata2.selectionData.Clone();
                bool isValidSelection = selectionData?.IsValid() ?? false;

                // プロキシメッシュ作成
                proxyMesh = new VirtualMesh("Proxy");
                proxyMesh.result.SetProcess();
                if (clothType == ClothProcess.ClothType.MeshCloth)
                {

                    // MeshClothではクロストランスフォームを追加しておく
                    proxyMesh.SetTransform(clothTransformRecord);

                    if (setupDataList.Count == 0)
                    {
                        sharePreBuildData.buildResult.SetError(Define.Result.ClothProcess_InvalidRenderHandleList);
                        throw new MagicaClothProcessingException();
                    }

                    // render mesh import + selection + merge
                    for (int i = 0; i < setupDataList.Count; i++)
                    {
                        VirtualMesh renderMesh = null;
                        try
                        {
                            // レンダーメッシュ作成
                            var renderSetupData = setupDataList[i];
                            renderMesh = new VirtualMesh($"[{renderSetupData.name}]");
                            renderMesh.result.SetProcess();

                            // import -------------------------------------------------
                            renderMesh.ImportFrom(renderSetupData);
                            if (renderMesh.IsError)
                            {
                                sharePreBuildData.buildResult.Merge(renderMesh.result);
                                throw new MagicaClothProcessingException();
                            }
                            Develop.DebugLog($"(IMPORT) {renderMesh}");

                            // selection ----------------------------------------------
                            // MeshClothでペイントテクスチャ指定の場合はセレクションデータを生成する
                            SelectionData renderSelectionData = selectionData;
                            if (usePaintMap)
                            {
                                // セレクションデータ生成
                                var ret = cloth.Process.GenerateSelectionDataFromPaintMap(clothTransformRecord, renderMesh, paintMapDataList[i], out renderSelectionData);
                                Develop.DebugLog($"Generate selection from paint map. {ret.GetResultString()}");
                                if (ret.IsError())
                                {
                                    sharePreBuildData.buildResult.Merge(ret);
                                    throw new MagicaClothProcessingException();
                                }

                                // セレクションデータ結合
                                selectionData.Merge(renderSelectionData);
                            }
                            isValidSelection = selectionData?.IsValid() ?? false;

                            // メッシュの切り取り
                            if (renderSelectionData?.IsValid() ?? false)
                            {
                                // 余白
                                float mergin = renderMesh.CalcSelectionMergin(sdata.reductionSetting);
                                mergin = math.max(mergin, Define.System.MinimumGridSize);

                                // セレクション情報から切り取りの実行
                                // ペイントマップの場合はレンダラーごとのセレクションデータで切り取り
                                renderMesh.SelectionMesh(renderSelectionData, clothTransformRecord.localToWorldMatrix, mergin);
                                if (renderMesh.IsError)
                                {
                                    sharePreBuildData.buildResult.Merge(renderMesh.result);
                                    throw new MagicaClothProcessingException();
                                }
                                Develop.DebugLog($"(SELECTION) {renderMesh}");
                            }

                            // レンダーメッシュの作成完了
                            renderMesh.result.SetSuccess();

                            // merge --------------------------------------------------
                            proxyMesh.AddMesh(renderMesh);

                            // レンダーメッシュ情報を記録
                            renderMeshList.Add(renderMesh);
                            renderMesh = null;
                        }
                        catch (MagicaClothProcessingException)
                        {
                            throw;
                        }
                        catch (Exception exception)
                        {
                            Debug.LogException(exception);
                            sharePreBuildData.buildResult.SetError(Define.Result.ClothProcess_Exception);
                            throw;
                        }
                        finally
                        {
                            // この時点で作業用renderMeshが存在する場合は中断されているので開放する
                            renderMesh?.Dispose();
                        }
                    }
                    Develop.DebugLog($"(MERGE) {proxyMesh}");

                    // リダクション
                    if (proxyMesh.VertexCount > 1)
                    {
                        if (sdata.reductionSetting.IsEnabled)
                        {
                            proxyMesh.Reduction(sdata.reductionSetting, System.Threading.CancellationToken.None);
                            if (proxyMesh.IsError)
                            {
                                sharePreBuildData.buildResult.Merge(proxyMesh.result);
                                throw new MagicaClothProcessingException();
                            }

                            Develop.DebugLog($"(REDUCTION) {proxyMesh}");
                        }
                    }
                }
                else if (clothType == ClothProcess.ClothType.BoneCloth || clothType == ClothProcess.ClothType.BoneSpring)
                {
                    // import
                    var boneClothSetupData = setupDataList[0];
                    proxyMesh.ImportFrom(boneClothSetupData);
                    if (proxyMesh.IsError)
                    {
                        sharePreBuildData.buildResult.Merge(proxyMesh.result);
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

                // 元の頂点から結合頂点へのインデックスを初期化
                if (proxyMesh.joinIndices.IsCreated == false)
                {
                    proxyMesh.joinIndices = new Unity.Collections.NativeArray<int>(proxyMesh.VertexCount, Unity.Collections.Allocator.Persistent);
                    JobUtility.SerialNumberRun(proxyMesh.joinIndices, proxyMesh.VertexCount); // 連番をつける
                }

                // optimization
                proxyMesh.Optimization();
                if (proxyMesh.IsError)
                {
                    sharePreBuildData.buildResult.Merge(proxyMesh.result);
                    throw new MagicaClothProcessingException();
                }
                Develop.DebugLog($"(OPTIMIZE) {proxyMesh}");

                // attribute
                if (isValidSelection)
                {
                    // セレクションデータから頂点属性を付与する
                    proxyMesh.ApplySelectionAttribute(selectionData);
                    if (proxyMesh.IsError)
                    {
                        sharePreBuildData.buildResult.Merge(proxyMesh.result);
                        throw new MagicaClothProcessingException();
                    }
                }

                // proxy mesh（属性決定後に実行）
                proxyMesh.ConvertProxyMesh(sdata, clothTransformRecord, customSkinningBoneRecords, normalAdjustmentTransformRecord);
                if (proxyMesh.IsError)
                {
                    sharePreBuildData.buildResult.Merge(proxyMesh.result);
                    throw new MagicaClothProcessingException();
                }
                Develop.DebugLog($"(PROXY) {proxyMesh}");

                // ProxyMeshの最終チェック
                if (proxyMesh.VertexCount > Define.System.MaxProxyMeshVertexCount)
                {
                    sharePreBuildData.buildResult.SetError(Define.Result.ProxyMesh_Over32767Vertices);
                    throw new MagicaClothProcessingException();
                }
                if (proxyMesh.EdgeCount > Define.System.MaxProxyMeshEdgeCount)
                {
                    sharePreBuildData.buildResult.SetError(Define.Result.ProxyMesh_Over32767Edges);
                    throw new MagicaClothProcessingException();
                }
                if (proxyMesh.TriangleCount > Define.System.MaxProxyMeshTriangleCount)
                {
                    sharePreBuildData.buildResult.SetError(Define.Result.ProxyMesh_Over32767Triangles);
                    throw new MagicaClothProcessingException();
                }

                // finish
                if (proxyMesh.IsError)
                {
                    sharePreBuildData.buildResult.Merge(proxyMesh.result);
                    throw new MagicaClothProcessingException();
                }
                proxyMesh.result.SetSuccess();
                Develop.DebugLog("CreateProxyMesh finish!");

                // Mapping(MeshClothのみ)
                if (clothType == ClothProcess.ClothType.MeshCloth)
                {
                    foreach (VirtualMesh renderMesh in renderMeshList)
                    {
                        renderMesh.Mapping(proxyMesh);
                        if (renderMesh.IsError)
                        {
                            sharePreBuildData.buildResult.Merge(renderMesh.result);
                            throw new MagicaClothProcessingException();
                        }
                        Develop.DebugLog($"(MAPPING) {renderMesh}");
                    }
                }

                // ======================= Cloth Data ===============================
                // クロスデータ作成
                var parameters = cloth.SerializeData.GetClothParameters();
                var distanceConstraintData = DistanceConstraint.CreateData(proxyMesh, parameters);
                if (distanceConstraintData != null)
                {
                    if (distanceConstraintData.result.IsSuccess())
                    {
                        sharePreBuildData.distanceConstraintData = distanceConstraintData;
                    }
                    else
                    {
                        sharePreBuildData.buildResult.Merge(distanceConstraintData.result);
                        throw new MagicaClothProcessingException();
                    }
                }
                var bendingConstraintData = TriangleBendingConstraint.CreateData(proxyMesh, parameters);
                if (bendingConstraintData != null)
                {
                    if (bendingConstraintData.result.IsSuccess())
                    {
                        sharePreBuildData.bendingConstraintData = bendingConstraintData;
                    }
                    else
                    {
                        sharePreBuildData.buildResult.Merge(bendingConstraintData.result);
                        throw new MagicaClothProcessingException();
                    }
                }
                var inertiaConstraintData = InertiaConstraint.CreateData(proxyMesh, parameters);
                if (inertiaConstraintData != null)
                {
                    if (inertiaConstraintData.result.IsSuccess())
                    {
                        sharePreBuildData.inertiaConstraintData = inertiaConstraintData;
                    }
                    else
                    {
                        sharePreBuildData.buildResult.Merge(inertiaConstraintData.result);
                        throw new MagicaClothProcessingException();
                    }
                }

                // ======================= Serialize ===============================
                sharePreBuildData.buildScale = clothTransformRecord.scale;
                sharePreBuildData.proxyMesh = proxyMesh.ShareSerialize();
                uniquePreBuildData.proxyMesh = proxyMesh.UniqueSerialize();
                foreach (VirtualMesh renderMesh in renderMeshList)
                {
                    sharePreBuildData.renderMeshList.Add(renderMesh.ShareSerialize());
                    uniquePreBuildData.renderMeshList.Add(renderMesh.UniqueSerialize());
                }

                // 成功
                sharePreBuildData.buildResult.SetSuccess();

                Develop.DebugLog(sharePreBuildData);
            }
            catch (MagicaClothProcessingException)
            {
                if (sharePreBuildData.buildResult.IsError() == false)
                    sharePreBuildData.buildResult.SetError(Define.Result.PreBuildData_MagicaClothException);
                sharePreBuildData.buildResult.DebugLog();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                sharePreBuildData.buildResult.SetError(Define.Result.PreBuildData_UnknownError);
            }
            finally
            {
                setupDataList.ForEach(x => x.Dispose());
                setupDataList.Clear();

                renderMeshList.ForEach(x => x?.Dispose());
                renderMeshList.Clear();

                proxyMesh?.Dispose();
                proxyMesh = null;

                // 内部情報は外部情報の結果をコピー
                uniquePreBuildData.buildResult = sharePreBuildData.buildResult;
            }

            //Debug.Log(span);
            //Debug.Log($"MakePreBuildData().end");
        }
    }
}
