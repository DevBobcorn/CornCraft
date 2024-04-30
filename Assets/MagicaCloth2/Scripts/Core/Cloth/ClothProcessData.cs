// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// MagicaClothコンポーネント処理のデータ部分
    /// </summary>
    public partial class ClothProcess : IDisposable, IValid, ITransform
    {
        public MagicaCloth cloth { get; internal set; }

        /// <summary>
        /// 同期中クロス
        /// </summary>
        public MagicaCloth SyncCloth { get; internal set; }

        /// <summary>
        /// 状態フラグ(0 ~ 31)
        /// </summary>
        public const int State_Valid = 0;
        public const int State_Enable = 1;
        public const int State_ParameterDirty = 2;
        public const int State_InitSuccess = 3;
        public const int State_InitComplete = 4;
        public const int State_Build = 5;
        public const int State_Running = 6;
        public const int State_DisableAutoBuild = 7;
        public const int State_CullingInvisible = 8; // チームデータの同フラグのコピー
        public const int State_CullingKeep = 9; // チームデータの同フラグのコピー
        public const int State_SkipWriting = 10; // 書き込み停止（ストップモーション用）
        public const int State_SkipWritingDirty = 11; // 書き込み停止フラグ更新サイン
        public const int State_UsePreBuild = 12; // PreBuildを利用

        /// <summary>
        /// 現在の状態
        /// </summary>
        internal BitField32 stateFlag;

        /// <summary>
        /// 初期クロスコンポーネントトランスフォーム状態
        /// </summary>
        internal TransformRecord clothTransformRecord { get; private set; } = null;

        /// <summary>
        /// レンダー情報へのハンドル
        /// （レンダラーのセットアップデータ）
        /// </summary>
        List<int> renderHandleList = new List<int>();

        /// <summary>
        /// BoneClothのセットアップデータ
        /// </summary>
        internal RenderSetupData boneClothSetupData;

        /// <summary>
        /// レンダーメッシュの管理
        /// </summary>
        public class RenderMeshInfo
        {
            public int renderHandle;
            public VirtualMeshContainer renderMeshContainer;
            public DataChunk mappingChunk;
        }
        internal List<RenderMeshInfo> renderMeshInfoList = new List<RenderMeshInfo>();

        /// <summary>
        /// カスタムスキニングのボーン情報
        /// </summary>
        internal List<TransformRecord> customSkinningBoneRecords = new List<TransformRecord>();

        /// <summary>
        /// 法線調整用のトランスフォーム状態
        /// </summary>
        internal TransformRecord normalAdjustmentTransformRecord { get; private set; } = null;

        //=========================================================================================
        /// <summary>
        /// ペイントマップ情報
        /// </summary>
        public class PaintMapData
        {
            public const byte ReadFlag_Fixed = 0x01;
            public const byte ReadFlag_Move = 0x02;
            public const byte ReadFlag_Limit = 0x04;

            public Color32[] paintData;
            public int paintMapWidth;
            public int paintMapHeight;
            public ExBitFlag8 paintReadFlag;
        }

        //=========================================================================================
        /// <summary>
        /// 処理結果
        /// </summary>
        internal ResultCode result;
        public ResultCode Result => result;

        /// <summary>
        /// Cloth Type
        /// </summary>
        public enum ClothType
        {
            MeshCloth = 0,
            BoneCloth = 1,
            BoneSpring = 10,
        }
        internal ClothType clothType { get; private set; }

        /// <summary>
        /// リダクション設定（外部から設定する）
        /// </summary>
        ReductionSettings reductionSettings;

        /// <summary>
        /// シミュレーションパラメータ
        /// </summary>
        public ClothParameters parameters { get; private set; }

        /// <summary>
        /// プロキシメッシュ
        /// </summary>
        public VirtualMeshContainer ProxyMeshContainer { get; private set; } = null;

        /// <summary>
        /// コライダーリスト
        /// コライダーが格納されるインデックスは他のデータのインデックスと一致している
        /// </summary>
        internal List<ColliderComponent> colliderList = new List<ColliderComponent>();

        /// <summary>
        /// コライダー配列数
        /// </summary>
        internal int ColliderCapacity => colliderList.Count;

        //=========================================================================================
        /// <summary>
        /// チームID
        /// </summary>
        public int TeamId { get; private set; } = 0;

        /// <summary>
        /// 慣性制約データ
        /// </summary>
        internal InertiaConstraint.ConstraintData inertiaConstraintData;

        /// <summary>
        /// 距離制約データ
        /// </summary>
        internal DistanceConstraint.ConstraintData distanceConstraintData;

        /// <summary>
        /// 曲げ制約データ
        /// </summary>
        internal TriangleBendingConstraint.ConstraintData bendingConstraintData;

        //=========================================================================================
        /// <summary>
        /// 連動アニメーター
        /// ・カリング
        /// ・更新モード
        /// </summary>
        internal Animator interlockingAnimator = null;

        /// <summary>
        /// カリング用アニメーター配下のレンダラーリスト
        /// </summary>
        internal List<Renderer> interlockingAnimatorRenderers = new List<Renderer>();

        //=========================================================================================
        /// <summary>
        /// キャンセルトークン
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();
        volatile object lockObject = new object();
        volatile object lockState = new object();

        /// <summary>
        /// 初期化待機カウンター
        /// </summary>
        volatile int suspendCounter = 0;

        /// <summary>
        /// 破棄フラグ
        /// </summary>
        volatile bool isDestory = false;

        /// <summary>
        /// 内部データまで完全に破棄されたかどうか
        /// </summary>
        volatile bool isDestoryInternal = false;

        /// <summary>
        /// 構築中フラグ
        /// </summary>
        volatile bool isBuild = false;

        public BitField32 GetStateFlag()
        {
            lock (lockState)
            {
                // copy
                var state = stateFlag;
                return state;
            }
        }

        public bool IsState(int state)
        {
            lock (lockState)
            {
                return stateFlag.IsSet(state);
            }
        }

        public void SetState(int state, bool sw)
        {
            lock (lockState)
            {
                stateFlag.SetBits(state, sw);
            }
        }

        public bool IsValid() => IsState(State_Valid);
        public bool IsCullingInvisible() => IsState(State_CullingInvisible);
        public bool IsCullingKeep() => IsState(State_CullingKeep);
        public bool IsSkipWriting() => IsState(State_SkipWriting);

        public bool IsEnable
        {
            get
            {
                if (IsValid() == false || TeamId == 0)
                    return false;
                return MagicaManager.Team.IsEnable(TeamId);
            }
        }

        public bool HasProxyMesh
        {
            get
            {
                if (IsValid() == false || TeamId == 0)
                    return false;
                return ProxyMeshContainer?.shareVirtualMesh?.IsSuccess ?? false;
            }
        }

        public string Name => cloth != null ? cloth.name : "(none)";

        //=========================================================================================
        public ClothProcess()
        {
            // 初期状態
            result = ResultCode.Empty;
        }

        public void Dispose()
        {
            lock (lockObject)
            {
                isDestory = true;
                SetState(State_Valid, false);
                result.Clear();
                cts.Cancel();
            }

            DisposeInternal();
            //Debug.Log($"ClothProcessData.Dispose()!");
        }

        void DisposeInternal()
        {
            lock (lockObject)
            {
                // すでに破棄完了ならば不要
                if (isDestoryInternal)
                    return;

                // ビルド中は破棄を保留する
                if (isBuild)
                    return;

                // マネージャから削除
                MagicaManager.Simulation?.ExitProxyMesh(this);
                MagicaManager.VMesh?.ExitProxyMesh(TeamId); // マッピングメッシュも解放される
                MagicaManager.Collider?.Exit(this);
                MagicaManager.Cloth?.RemoveCloth(this);

                // レンダーメッシュの破棄
                foreach (var info in renderMeshInfoList)
                {
                    if (info == null)
                        continue;

                    // 仮想メッシュ破棄
                    info.renderMeshContainer?.Dispose();
                }
                renderMeshInfoList.Clear();
                renderMeshInfoList = null;

                // レンダーデータの利用終了
                foreach (int renderHandle in renderHandleList)
                {
                    MagicaManager.Render?.RemoveRenderer(renderHandle);
                }
                renderHandleList.Clear();
                renderHandleList = null;

                // BoneClothセットアップデータ
                boneClothSetupData?.Dispose();
                boneClothSetupData = null;

                // プロキシメッシュ破棄
                ProxyMeshContainer?.Dispose();
                ProxyMeshContainer = null;

                colliderList.Clear();

                interlockingAnimator = null;
                interlockingAnimatorRenderers.Clear();

                // PreBuildデータ解除
                MagicaManager.PreBuild?.UnregisterPreBuildData(cloth?.GetSerializeData2()?.preBuildData.GetSharePreBuildData());

                SyncCloth = null;

                // 完全破棄フラグ
                isDestoryInternal = true;
            }
            Develop.DebugLog($"Cloth dispose internal.");

            // 破棄監視リストから削除する
            MagicaManager.Team.RemoveMonitoringProcess(this);
        }

        internal void IncrementSuspendCounter()
        {
            lock (lockObject)
            {
                suspendCounter++;
            }
        }

        internal void DecrementSuspendCounter()
        {
            lock (lockObject)
            {
                suspendCounter--;
            }
        }

        internal int GetSuspendCounter()
        {
            return suspendCounter;
        }

        public RenderMeshInfo GetRenderMeshInfo(int index)
        {
            if (index >= 0 && index < renderMeshInfoList.Count)
                return renderMeshInfoList[index];
            else
                return null;
        }

        internal void SyncParameters()
        {
            parameters = cloth.SerializeData.GetClothParameters();
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
            cloth.SerializeData.GetUsedTransform(transformSet);
            cloth.serializeData2.GetUsedTransform(transformSet);
            clothTransformRecord?.GetUsedTransform(transformSet);
            boneClothSetupData?.GetUsedTransform(transformSet);
            renderHandleList.ForEach(handle => MagicaManager.Render.GetRendererData(handle).GetUsedTransform(transformSet));
            customSkinningBoneRecords.ForEach(rd => rd.GetUsedTransform(transformSet));
            normalAdjustmentTransformRecord?.GetUsedTransform(transformSet);
        }

        public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
        {
            cloth.SerializeData.ReplaceTransform(replaceDict);
            cloth.serializeData2.ReplaceTransform(replaceDict);
            clothTransformRecord?.ReplaceTransform(replaceDict);
            boneClothSetupData?.ReplaceTransform(replaceDict);
            renderHandleList.ForEach(handle => MagicaManager.Render.GetRendererData(handle).ReplaceTransform(replaceDict));
            customSkinningBoneRecords.ForEach(rd => rd.ReplaceTransform(replaceDict));
            normalAdjustmentTransformRecord?.ReplaceTransform(replaceDict);
        }

        internal void SetSkipWriting(bool sw)
        {
            // ここではフラグのみ更新する
            // 実際の更新はチームのAlwaysTeamUpdate()で行われる
            SetState(State_SkipWriting, sw);
            SetState(State_SkipWritingDirty, true);
        }

        internal ClothUpdateMode GetClothUpdateMode()
        {
            switch (cloth.SerializeData.updateMode)
            {
                case ClothUpdateMode.Normal:
                case ClothUpdateMode.UnityPhysics:
                case ClothUpdateMode.Unscaled:
                    return cloth.SerializeData.updateMode;
                case ClothUpdateMode.AnimatorLinkage:
                    if (interlockingAnimator)
                    {
                        switch (interlockingAnimator.updateMode)
                        {
                            case AnimatorUpdateMode.Normal:
                                return ClothUpdateMode.Normal;
#if UNITY_2023_1_OR_NEWER
                            case AnimatorUpdateMode.Fixed:
                                return ClothUpdateMode.UnityPhysics;
#else
                            case AnimatorUpdateMode.AnimatePhysics:
                                return ClothUpdateMode.UnityPhysics;
#endif
                            case AnimatorUpdateMode.UnscaledTime:
                                return ClothUpdateMode.Unscaled;
                            default:
                                Develop.DebugLogWarning($"[{cloth.name}] Unknown Animator UpdateMode:{interlockingAnimator.updateMode}");
                                break;
                        }
                    }
                    return ClothUpdateMode.Normal;
                default:
                    Develop.LogError($"[{cloth.name}] Unknown Cloth Update Mode:{cloth.SerializeData.updateMode}");
                    return ClothUpdateMode.Normal;
            }
        }
    }
}
