// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace MagicaCloth2
{
    public class TeamManager : IManager, IValid
    {
        /// <summary>
        /// チームフラグ(32bit)
        /// </summary>
        public const int Flag_Valid = 0; // データの有効性
        public const int Flag_Enable = 1; // 動作状態
        public const int Flag_Reset = 2; // 姿勢リセット
        public const int Flag_TimeReset = 3; // 時間リセット
        public const int Flag_Suspend = 4; // 一時停止
        public const int Flag_Running = 5; // 今回のフレームでシミュレーションが実行されたかどうか
        //public const int Flag_CustomSkinning = 6; // カスタムスキニングを使用(未使用)
        //public const int Flag_NormalAdjustment = 9; // 法線調整(未使用)
        public const int Flag_Synchronization = 6; // 同期中
        public const int Flag_StepRunning = 7; // ステップ実行中
        public const int Flag_Exit = 8; // 存在消滅時
        public const int Flag_KeepTeleport = 9; // 姿勢保持テレポート
        public const int Flag_InertiaShift = 10; // 慣性全体シフト
        public const int Flag_CullingInvisible = 11; // カリングによる非表示状態
        public const int Flag_CullingKeep = 12; // カリング時に姿勢を保つ

        // 以下セルフコリジョン
        // !これ以降の順番を変えないこと
        public const int Flag_Self_PointPrimitive = 14; // PointPrimitive+Sortを保持し更新する
        public const int Flag_Self_EdgePrimitive = 15; // EdgePrimitive+Sortを保持し更新する
        public const int Flag_Self_TrianglePrimitive = 16; // TrianglePrimitive+Sortを保持し更新する

        public const int Flag_Self_EdgeEdge = 17;
        public const int Flag_Sync_EdgeEdge = 18;
        public const int Flag_PSync_EdgeEdge = 19;

        public const int Flag_Self_PointTriangle = 20;
        public const int Flag_Sync_PointTriangle = 21;
        public const int Flag_PSync_PointTriangle = 22;

        public const int Flag_Self_TrianglePoint = 23;
        public const int Flag_Sync_TrianglePoint = 24;
        public const int Flag_PSync_TrianglePoint = 25;

        public const int Flag_Self_EdgeTriangleIntersect = 26;
        public const int Flag_Sync_EdgeTriangleIntersect = 27;
        public const int Flag_PSync_EdgeTriangleIntersect = 28;
        public const int Flag_Self_TriangleEdgeIntersect = 29;
        public const int Flag_Sync_TriangleEdgeIntersect = 30;
        public const int Flag_PSync_TriangleEdgeIntersect = 31;

        /// <summary>
        /// チーム基本データ
        /// </summary>
        public struct TeamData
        {
            /// <summary>
            /// フラグ
            /// </summary>
            public BitField32 flag;

            /// <summary>
            /// 更新モード
            /// </summary>
            public ClothUpdateMode updateMode;

            /// <summary>
            /// １秒間の更新頻度
            /// </summary>
            //public int frequency;

            public float frameDeltaTime;

            /// <summary>
            /// 更新計算用時間
            /// </summary>
            public float time;

            /// <summary>
            /// 前フレームの更新計算用時間
            /// </summary>
            public float oldTime;

            /// <summary>
            /// 現在のシミュレーション更新時間
            /// </summary>
            public float nowUpdateTime;

            /// <summary>
            /// １つ前の最後のシミュレーション更新時間
            /// </summary>
            public float oldUpdateTime;

            /// <summary>
            /// 更新がある場合のフレーム時間
            /// </summary>
            public float frameUpdateTime;

            /// <summary>
            /// 前回更新のフレーム時間
            /// </summary>
            public float frameOldTime;

            /// <summary>
            /// チーム固有のタイムスケール(0.0-1.0)
            /// </summary>
            public float timeScale;

            /// <summary>
            /// 今回のチーム更新回数（０ならばこのフレームは更新なし）
            /// </summary>
            public int updateCount;

            /// <summary>
            /// ステップごとのフレームに対するnowUpdateTime割合
            /// これは(frameStartTime ~ time)間でのnowUpdateTimeの割合
            /// </summary>
            public float frameInterpolation;

            /// <summary>
            /// 重力の影響力(0.0 ~ 1.0)
            /// 1.0は重力が100%影響する
            /// </summary>
            public float gravityRatio;

            public float gravityDot;

            /// <summary>
            /// センタートランスフォーム(ダイレクト値)
            /// </summary>
            public int centerTransformIndex;

            /// <summary>
            /// 現在の中心ワールド座標（この値はCenterData.nowWorldPositionのコピー）
            /// </summary>
            public float3 centerWorldPosition;

            /// <summary>
            /// チームスケール
            /// </summary>
            public float3 initScale;            // データ生成時のセンタートランスフォームスケール
            public float scaleRatio;            // 現在のスケール倍率
            //public float3 scaleDirection;     // フリップ用:スケール値方向(xyz)：(1/-1)のみ
            //public float4 quaternionScale;    // フリップ用:クォータニオン反転用

            /// <summary>
            /// 同期チームID(0=なし)
            /// </summary>
            public int syncTeamId;

            /// <summary>
            /// 自身を同期している親チームID(0=なし)：最大７つ
            /// </summary>
            public FixedList32Bytes<int> syncParentTeamId;

            /// <summary>
            /// 同期先チームのセンタートランスフォームインデックス（ダイレクト値）
            /// </summary>
            public int syncCenterTransformIndex;

            /// <summary>
            /// 初期姿勢とアニメーション姿勢のブレンド率（制約で利用）
            /// </summary>
            public float animationPoseRatio;

            /// <summary>
            /// 速度安定時間(StablizationTime)による速度適用割合(0.0 ~ 1.0)
            /// </summary>
            public float velocityWeight;

            /// <summary>
            /// シミュレーション結果ブレンド割合(0.0 ~ 1.0)
            /// </summary>
            public float blendWeight;

            /// <summary>
            /// 外力モード
            /// </summary>
            public ClothForceMode forceMode;

            /// <summary>
            /// 外力
            /// </summary>
            public float3 impactForce;

            //-----------------------------------------------------------------
            /// <summary>
            /// ProxyMeshのタイプ
            /// </summary>
            public VirtualMesh.MeshType proxyMeshType;

            /// <summary>
            /// ProxyMeshのTransformデータ
            /// </summary>
            public DataChunk proxyTransformChunk;

            /// <summary>
            /// ProxyMeshの共通部分
            /// -attributes
            /// -vertexToTriangles
            /// -vertexToVertexIndexArray
            /// -vertexDepths
            /// -vertexLocalPositions
            /// -vertexLocalRotations
            /// -vertexRootIndices
            /// -vertexParentIndices
            /// -vertexChildIndexArray
            /// -vertexAngleCalcLocalRotations
            /// -uv
            /// -positions
            /// -rotations
            /// -vertexBindPosePositions
            /// -vertexBindPoseRotations
            /// -normalAdjustmentRotations
            /// </summary>
            public DataChunk proxyCommonChunk;

            /// <summary>
            /// ProxyMeshの頂点接続頂点データ
            /// -vertexToVertexDataArray (-vertexToVertexIndexArrayと対)
            /// </summary>
            //public DataChunk proxyVertexToVertexDataChunk;

            /// <summary>
            /// ProxyMeshの子頂点データ
            /// -vertexChildDataArray (-vertexChildIndexArrayと対)
            /// </summary>
            public DataChunk proxyVertexChildDataChunk;

            /// <summary>
            /// ProxyMeshのTriangle部分
            /// -triangles
            /// -triangleTeamIdArray
            /// -triangleNormals
            /// -triangleTangents
            /// </summary>
            public DataChunk proxyTriangleChunk;

            /// <summary>
            /// ProxyMeshのEdge部分
            /// -edges
            /// -edgeTeamIdArray
            /// </summary>
            public DataChunk proxyEdgeChunk;

            /// <summary>
            /// ProxyMeshのBoneCloth/MeshCloth共通部分
            /// -localPositions
            /// -localNormals
            /// -localTangents
            /// -boneWeights
            /// </summary>
            public DataChunk proxyMeshChunk;

            /// <summary>
            /// ProxyMeshのBoneCloth固有部分
            /// -vertexToTransformRotations
            /// </summary>
            public DataChunk proxyBoneChunk;

            /// <summary>
            /// ProxyMeshのMeshClothのスキニングボーン部分
            /// -skinBoneTransformIndices
            /// -skinBoneBindPoses
            /// </summary>
            public DataChunk proxySkinBoneChunk;

            /// <summary>
            /// ProxyMeshのベースライン部分
            /// -baseLineFlags
            /// -baseLineStartDataIndices
            /// -baseLineDataCounts
            /// </summary>
            public DataChunk baseLineChunk;

            /// <summary>
            /// ProxyMeshのベースラインデータ配列
            /// -baseLineData
            /// </summary>
            public DataChunk baseLineDataChunk;

            /// <summary>
            /// 固定点リスト
            /// </summary>
            public DataChunk fixedDataChunk;

            //-----------------------------------------------------------------
            /// <summary>
            /// 接続しているマッピングメッシュへデータへのインデックスセット(最大15まで)
            /// </summary>
            public FixedList32Bytes<short> mappingDataIndexSet;

            //-----------------------------------------------------------------
            /// <summary>
            /// パーティクルデータ
            /// </summary>
            public DataChunk particleChunk;

            /// <summary>
            /// コライダーデータ
            /// コライダーが有効の場合は未使用であっても最大数まで確保される
            /// </summary>
            public DataChunk colliderChunk;

            /// <summary>
            /// コライダートランスフォーム
            /// コライダーが有効の場合は未使用であっても最大数まで確保される
            /// </summary>
            public DataChunk colliderTransformChunk;

            /// <summary>
            /// 現在有効なコライダー数
            /// </summary>
            public int colliderCount;

            //-----------------------------------------------------------------
            /// <summary>
            /// 距離制約
            /// </summary>
            public DataChunk distanceStartChunk;
            public DataChunk distanceDataChunk;

            /// <summary>
            /// 曲げ制約
            /// </summary>
            public DataChunk bendingPairChunk;
            //public DataChunk bendingDataChunk;
            public DataChunk bendingWriteIndexChunk;
            public DataChunk bendingBufferChunk;

            /// <summary>
            /// セルフコリジョン制約
            /// </summary>
            //public int selfQueueIndex;
            public DataChunk selfPointChunk;
            public DataChunk selfEdgeChunk;
            public DataChunk selfTriangleChunk;

            //-----------------------------------------------------------------
            /// <summary>
            /// UnityPhysicsでの更新の必要性
            /// </summary>
            public bool IsFixedUpdate => updateMode == ClothUpdateMode.UnityPhysics;

            /// <summary>
            /// タイムスケールを無視
            /// </summary>
            public bool IsUnscaled => updateMode == ClothUpdateMode.Unscaled;

            /// <summary>
            /// １回の更新間隔
            /// </summary>
            //public float SimulationDeltaTime => 1.0f / frequency;

            /// <summary>
            /// データの有効性
            /// </summary>
            public bool IsValid => flag.IsSet(Flag_Valid);

            /// <summary>
            /// 有効状態
            /// </summary>
            public bool IsEnable => flag.IsSet(Flag_Enable);

            /// <summary>
            /// 処理状態
            /// </summary>
            public bool IsProcess => flag.IsSet(Flag_Enable) && flag.IsSet(Flag_Suspend) == false && flag.IsSet(Flag_CullingInvisible) == false;

            /// <summary>
            /// 姿勢リセット有無
            /// </summary>
            public bool IsReset => flag.IsSet(Flag_Reset);

            /// <summary>
            /// 姿勢維持テレポートの有無
            /// </summary>
            public bool IsKeepReset => flag.IsSet(Flag_KeepTeleport);

            /// <summary>
            /// 慣性全体シフトの有無
            /// </summary>
            public bool IsInertiaShift => flag.IsSet(Flag_InertiaShift);

            /// <summary>
            /// 今回のフレームでシミュレーションが実行されたかどうか（１回以上実行された場合）
            /// </summary>
            public bool IsRunning => flag.IsSet(Flag_Running);

            /// <summary>
            /// ステップ実行中かどうか
            /// </summary>
            public bool IsStepRunning => flag.IsSet(Flag_StepRunning);

            public bool IsCullingInvisible => flag.IsSet(Flag_CullingInvisible);

            public bool IsCullingKeep => flag.IsSet(Flag_CullingKeep);

            public int ParticleCount => particleChunk.dataLength;

            /// <summary>
            /// 現在有効なコライダー数
            /// </summary>
            public int ColliderCount => colliderCount;

            public int BaseLineCount => baseLineChunk.dataLength;

            public int TriangleCount => proxyTriangleChunk.dataLength;

            public int EdgeCount => proxyEdgeChunk.dataLength;

            public int MappingCount => mappingDataIndexSet.Length;

            /// <summary>
            /// 初期スケール（ｘ軸のみで判定、均等スケールしか認めていない）
            /// </summary>
            public float InitScale => initScale.x;
        }
        public ExNativeArray<TeamData> teamDataArray;

        /// <summary>
        /// チームごとの風の影響情報
        /// </summary>
        public ExNativeArray<TeamWindData> teamWindArray;

        /// <summary>
        /// マッピングメッシュデータ
        /// </summary>
        public struct MappingData : IValid
        {
            public int teamId;

            /// <summary>
            /// Mappingメッシュのセンタートランスフォーム（ダイレクト値）
            /// </summary>
            public int centerTransformIndex;

            /// <summary>
            /// Mappingメッシュの基本
            /// -attributes
            /// -localPositions
            /// -localNormlas
            /// -localTangents
            /// -boneWeights
            /// -positions
            /// -rotations
            /// </summary>
            public DataChunk mappingCommonChunk;

            /// <summary>
            /// 初期状態でのプロキシメッシュへの変換マトリックスと変換回転
            /// この姿勢は初期化時に固定される
            /// </summary>
            public float4x4 toProxyMatrix;
            public quaternion toProxyRotation;

            /// <summary>
            /// プロキシメッシュとマッピングメッシュの座標空間が同じかどうか
            /// </summary>
            public bool sameSpace;

            /// <summary>
            /// プロキシメッシュからマッピングメッシュへの座標空間変換用
            /// ▲ワールド対応：ここはワールド空間からマッピングメッシュへの座標変換となる
            /// </summary>
            public float4x4 toMappingMatrix;
            public quaternion toMappingRotation;

            /// <summary>
            /// Mappingメッシュ用のスケーリング比率
            /// </summary>
            public float scaleRatio;

            public bool IsValid()
            {
                return teamId > 0;
            }

            public int VertexCount => mappingCommonChunk.dataLength;
        }
        public ExNativeArray<MappingData> mappingDataArray;

        /// <summary>
        /// チーム全体の最大更新回数
        /// </summary>
        public NativeReference<int> maxUpdateCount;

        /// <summary>
        /// パラメータ（teamDataArrayとインデックス連動）
        /// </summary>
        public ExNativeArray<ClothParameters> parameterArray;

        /// <summary>
        /// センタートランスフォームデータ
        /// </summary>
        public ExNativeArray<InertiaConstraint.CenterData> centerDataArray;

        /// <summary>
        /// 登録されているマッピングメッシュ数
        /// </summary>
        public int MappingCount => mappingDataArray?.Count ?? 0;

        /// <summary>
        /// チームの有効状態を別途記録
        /// NativeArrayはジョブ実行中にアクセスできないため。
        /// </summary>
        HashSet<int> enableTeamSet = new HashSet<int>();

        /// <summary>
        /// チームIDとClothProcessクラスの関連辞書
        /// </summary>
        Dictionary<int, ClothProcess> clothProcessDict = new Dictionary<int, ClothProcess>();

        //=========================================================================================
        bool isValid;

        /// <summary>
        /// グローバルタイムスケール(0.0 ~ 1.0)
        /// </summary>
        //internal float globalTimeScale = 1.0f;

        /// <summary>
        /// フレームのFixedUpdate回数
        /// </summary>
        //int fixedUpdateCount = 0;

        /// <summary>
        /// エッジコライダーコリジョンのエッジ数合計
        /// </summary>
        internal int edgeColliderCollisionCount;

        //=========================================================================================
        /// <summary>
        /// 登録されているチーム数（グローバルチームを含む。そのため０にはならない）
        /// </summary>
        public int TeamCount => teamDataArray?.Count ?? 0;

        /// <summary>
        /// 登録されている有効なチーム数（グローバルチームを含まない）
        /// </summary>
        public int TrueTeamCount => clothProcessDict.Count;

        /// <summary>
        /// 実行状態にあるチーム数
        /// </summary>
        public int ActiveTeamCount => enableTeamSet.Count;

        //=========================================================================================
        public void Dispose()
        {
            isValid = false;

            teamDataArray?.Dispose();
            teamWindArray?.Dispose();
            mappingDataArray?.Dispose();
            parameterArray?.Dispose();
            centerDataArray?.Dispose();

            teamDataArray = null;
            teamWindArray = null;
            mappingDataArray = null;
            parameterArray = null;
            centerDataArray = null;

            if (maxUpdateCount.IsCreated)
                maxUpdateCount.Dispose();

            enableTeamSet.Clear();
            clothProcessDict.Clear();

            //globalTimeScale = 1.0f;
            //fixedUpdateCount = 0;
        }

        public void EnterdEditMode()
        {
            Dispose();
        }

        public void Initialize()
        {
            Dispose();

            const int capacity = 32;
            teamDataArray = new ExNativeArray<TeamData>(capacity);
            teamWindArray = new ExNativeArray<TeamWindData>(capacity);
            mappingDataArray = new ExNativeArray<MappingData>(capacity);
            parameterArray = new ExNativeArray<ClothParameters>(capacity);
            centerDataArray = new ExNativeArray<InertiaConstraint.CenterData>(capacity);

            // グローバルチーム[0]を追加する
            var gteam = new TeamData();
            teamDataArray.Add(gteam);
            teamWindArray.Add(new TeamWindData());
            parameterArray.Add(new ClothParameters());
            centerDataArray.Add(new InertiaConstraint.CenterData());

            maxUpdateCount = new NativeReference<int>(Allocator.Persistent);

            //globalTimeScale = 1.0f;
            //fixedUpdateCount = 0;

            isValid = true;
        }

        public bool IsValid()
        {
            return isValid;
        }

        //=========================================================================================
        /// <summary>
        /// チームを登録する
        /// </summary>
        /// <param name="cprocess"></param>
        /// <param name="clothParams"></param>
        /// <returns></returns>
        internal int AddTeam(ClothProcess cprocess, ClothParameters clothParams)
        {
            if (isValid == false)
                return 0;

            // この段階でProxyMeshは完成している

            var team = new TeamData();
            // ★Enableフラグは立てない
            team.flag.SetBits(Flag_Valid, true);
            team.flag.SetBits(Flag_Reset, true);
            team.flag.SetBits(Flag_TimeReset, true);
            //team.flag.SetBits(Flag_CustomSkinning, cprocess.cloth.SerializeData.customSkinningSetting.enable);
            //team.flag.SetBits(Flag_NormalAdjustment, cprocess.cloth.SerializeData.normalAlignmentSetting.alignmentMode != NormalAlignmentSettings.AlignmentMode.None);
            team.updateMode = cprocess.cloth.SerializeData.updateMode;
            //team.frequency = clothParams.solverFrequency;
            team.timeScale = 1.0f;
            team.initScale = cprocess.clothTransformRecord.scale; // 初期スケール
            team.scaleRatio = 1.0f;
            team.centerWorldPosition = cprocess.clothTransformRecord.position;
            team.animationPoseRatio = cprocess.cloth.SerializeData.animationPoseRatio;
            var c = teamDataArray.Add(team);
            int teamId = c.startIndex;

            // 最大チーム数チェック
            if (teamId >= Define.System.MaximumTeamCount)
            {
                Develop.LogError($"Cannot create more than {Define.System.MaximumTeamCount} teams.");
                teamDataArray.Remove(c);
                return 0;
            }


            var wind = new TeamWindData();
            wind.movingWind.time = -Define.System.WindMaxTime;
            teamWindArray.Add(wind);

            // パラメータ
            parameterArray.Add(clothParams);

            // 慣性制約
            // 初期化時のセンターローカル位置を初期化
            var cdata = new InertiaConstraint.CenterData();
            cdata.frameLocalPosition = cprocess.ProxyMesh.localCenterPosition.Value;
            centerDataArray.Add(cdata);

            clothProcessDict.Add(teamId, cprocess);

            return teamId;
        }

        /// <summary>
        /// チームを解除する
        /// </summary>
        /// <param name="teamId"></param>
        internal void RemoveTeam(int teamId)
        {
            if (isValid == false || teamId == 0)
                return;

            // セルフコリジョン同期解除
            ref var tdata = ref GetTeamDataRef(teamId);
            if (tdata.syncTeamId > 0 && ContainsTeamData(tdata.syncTeamId))
            {
                ref var stdata = ref GetTeamDataRef(tdata.syncTeamId);
                RemoveSyncParent(ref stdata, teamId);
            }

            // 制約データなど解除

            // チームデータを破棄する
            var c = new DataChunk(teamId);
            teamDataArray.RemoveAndFill(c);
            teamWindArray.RemoveAndFill(c);
            parameterArray.Remove(c);
            centerDataArray.Remove(c);

            clothProcessDict.Remove(teamId);
        }

        /// <summary>
        /// チームの有効化設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="sw"></param>
        public void SetEnable(int teamId, bool sw)
        {
            if (isValid == false || teamId == 0)
                return;
            ref var team = ref teamDataArray.GetRef(teamId);
            team.flag.SetBits(Flag_Enable, sw);
            team.flag.SetBits(Flag_Reset, sw);

            if (sw)
                enableTeamSet.Add(teamId);
            else
                enableTeamSet.Remove(teamId);

            // コライダーの有効状態（内部でコライダートランスフォームの有効状態も設定）
            MagicaManager.Collider.EnableTeamCollider(teamId, sw);

            // センタートランスフォーム
            MagicaManager.Bone.EnableTransform(team.centerTransformIndex, sw);

            // プロキシメッシュ
            MagicaManager.Bone.EnableTransform(team.proxyTransformChunk, sw);
        }

        public bool IsEnable(int teamId)
        {
            return enableTeamSet.Contains(teamId);
        }

        public bool ContainsTeamData(int teamId)
        {
            return teamId >= 0 && clothProcessDict.ContainsKey(teamId);
        }

        public ref TeamData GetTeamDataRef(int teamId)
        {
            return ref teamDataArray.GetRef(teamId);
        }

        public ref ClothParameters GetParametersRef(int teamId)
        {
            return ref parameterArray.GetRef(teamId);
        }

        internal ref InertiaConstraint.CenterData GetCenterDataRef(int teamId)
        {
            return ref centerDataArray.GetRef(teamId);
        }


        public ClothProcess GetClothProcess(int teamId)
        {
            if (clothProcessDict.ContainsKey(teamId))
                return clothProcessDict[teamId];
            else
                return null;
        }

        //=========================================================================================
        static readonly ProfilerMarker teamUpdateCullingProfiler = new ProfilerMarker("TeamUpdateCulling");

        /// <summary>
        /// カリング状態更新
        /// </summary>
        internal void TeamCullingUpdate()
        {
            teamUpdateCullingProfiler.Begin();

            var cm = MagicaManager.Cloth;

            // ジョブでは実行できないチーム更新
            cm.ClearVisibleDict();
            var clothSet = cm.clothSet;
            foreach (var cprocess in clothSet)
            {
                int teamId = cprocess.TeamId;
                ref var tdata = ref GetTeamDataRef(teamId);

                // 動作判定 ----------------------------------------------------
                if (tdata.flag.IsSet(Flag_Enable) == false || tdata.flag.IsSet(Flag_Suspend))
                    continue;

                //-------------------------------------------------------------
                // 現在の状態
                bool oldInvisible = tdata.IsCullingInvisible;
                bool invisible;

                MagicaCloth jugeCloth = cprocess.cloth;
                CullingSettings jugeSettings = jugeCloth.SerializeData.cullingSettings;
                ClothProcess jugeProcess = cprocess;

                // 同期時は同期先を見る
                if (tdata.syncTeamId > 0)
                {
                    var syncProcess = GetClothProcess(tdata.syncTeamId);
                    if (syncProcess != null)
                    {
                        jugeCloth = syncProcess.cloth;
                        jugeSettings = jugeCloth.SerializeData.cullingSettings;
                        jugeProcess = jugeCloth.Process;
                    }
                }

                // 最終的なカリングモード
                var cullingMode = jugeSettings.cameraCullingMode;
                if (cullingMode == CullingSettings.CameraCullingMode.AnimatorLinkage)
                {
                    if (jugeProcess.cullingAnimator)
                    {
                        switch (jugeProcess.cullingAnimator.cullingMode)
                        {
                            case AnimatorCullingMode.AlwaysAnimate:
                                cullingMode = CullingSettings.CameraCullingMode.Off;
                                break;
                            case AnimatorCullingMode.CullCompletely:
                                cullingMode = CullingSettings.CameraCullingMode.Keep;
                                break;
                            case AnimatorCullingMode.CullUpdateTransforms:
                                cullingMode = CullingSettings.CameraCullingMode.Reset;
                                break;
                        }
                    }
                    else
                        cullingMode = CullingSettings.CameraCullingMode.Off;
                }

                // カリング判定
                if (jugeCloth == null || cullingMode == CullingSettings.CameraCullingMode.Off)
                {
                    invisible = false;
                }
                else
                {
                    // 参照すべきレンダラー確定
                    Animator jugeAnimator = null;
                    List<Renderer> jugeRenderers = jugeSettings.cameraCullingRenderers;
                    if (jugeSettings.cameraCullingMethod == CullingSettings.CameraCullingMethod.AutomaticRenderer)
                    {
                        jugeAnimator = jugeCloth.Process.cullingAnimator;
                        jugeRenderers = jugeCloth.Process.cullingAnimatorRenderers;
                    }

                    // レンダラー判定
                    invisible = !cm.CheckVisible(jugeAnimator, jugeRenderers);
                }

                // 状態変更
                if (oldInvisible != invisible)
                {
                    tdata.flag.SetBits(Flag_CullingInvisible, invisible);
                    tdata.flag.SetBits(Flag_CullingKeep, false);
                    //Debug.Log($"Change culling invisible:({oldInvisible}) -> ({invisible})");

                    // cprocessクラスにもコピーする
                    cprocess.SetState(ClothProcess.State_CullingInvisible, invisible);
                    cprocess.SetState(ClothProcess.State_CullingKeep, false);

                    if (invisible)
                    {
                        // (表示->非表示)時の振る舞い
                        switch (cullingMode)
                        {
                            case CullingSettings.CameraCullingMode.Reset:
                            case CullingSettings.CameraCullingMode.Off:
                                tdata.flag.SetBits(Flag_Reset, true);
                                //Debug.Log($"Culling invisible. Reset On");
                                break;
                            case CullingSettings.CameraCullingMode.Keep:
                                tdata.flag.SetBits(Flag_CullingKeep, true);
                                cprocess.SetState(ClothProcess.State_CullingKeep, true);
                                //Debug.Log($"Culling invisible. Keep On");
                                break;
                        }
                    }

                    // 対応するレンダーデータに更新を指示する
                    cprocess.UpdateRendererUse();
                }
            }

            teamUpdateCullingProfiler.End();
        }

        //=========================================================================================
        /// <summary>
        /// 毎フレーム常に実行するチーム更新
        /// - 時間の更新と実行回数の算出
        /// </summary>
        internal void AlwaysTeamUpdate()
        {
            // 集計
            edgeColliderCollisionCount = 0;

            // ジョブでは実行できないチーム更新
            var clothSet = MagicaManager.Cloth.clothSet;
            foreach (var cprocess in clothSet)
            {
                int teamId = cprocess.TeamId;
                ref var tdata = ref GetTeamDataRef(teamId);
                var cloth = cprocess.cloth;

                // 動作判定 ----------------------------------------------------
                if (tdata.flag.IsSet(Flag_Enable) == false)
                    continue;

                // 同期まち判定
                bool suspend = true;
                if (cprocess.GetSuspendCounter() == 0)
                    suspend = false;

                // 同期相手の有効状態
                if (cloth.SyncCloth != null)
                {
                    int syncTeamId = cloth.SyncCloth.Process.TeamId;
                    if (syncTeamId > 0)
                    {
                        ref var syncTeamData = ref GetTeamDataRef(syncTeamId);
                        if (syncTeamData.flag.IsSet(Flag_Enable) && cloth.SyncCloth.Process.GetSuspendCounter() == 0)
                            suspend = false; // 相手も処理可能状態
                    }
                }
                tdata.flag.SetBits(Flag_Suspend, suspend);

                // 有効状態と同期まちフラグの２つで実行判定
                if (tdata.flag.IsSet(Flag_Enable) == false || tdata.flag.IsSet(Flag_Suspend))
                    continue;

                //-------------------------------------------------------------
                bool selfCollisionUpdate = false;

                // パラメータ変更反映
                if (cprocess.IsState(ClothProcess.State_ParameterDirty) && cprocess.IsEnable)
                {
                    //Develop.DebugLog($"Update Parameters {teamId}");
                    // コライダー更新(内部でteamData更新)
                    MagicaManager.Collider.UpdateColliders(cprocess);

                    // カリング用アニメーターとレンダラー更新
                    cprocess.UpdateCullingAnimatorAndRenderers();

                    // パラメータ変更
                    cprocess.SyncParameters();
                    parameterArray[teamId] = cprocess.parameters;
                    tdata.updateMode = cloth.SerializeData.updateMode;
                    tdata.animationPoseRatio = cloth.SerializeData.animationPoseRatio;

                    // セルフコリジョン更新
                    selfCollisionUpdate = true;

                    cprocess.SetState(ClothProcess.State_ParameterDirty, false);
                }

                // チーム同期
                // 同期チェーンをたどり先端のチームを参照する
                int oldSyncTeamId = tdata.syncTeamId;
                var syncCloth = cloth.SyncCloth;
                if (syncCloth != null)
                {
                    // デッドロック対策
                    var c = syncCloth;
                    while (c)
                    {
                        if (c == cloth)
                        {
                            syncCloth = null;
                            c = null;
                        }
                        else
                            c = c.SyncCloth;
                    }
                }
                tdata.syncTeamId = syncCloth != null ? syncCloth.Process.TeamId : 0;
                tdata.flag.SetBits(Flag_Synchronization, tdata.syncTeamId != 0);
                tdata.syncCenterTransformIndex = 0;
                if (oldSyncTeamId != tdata.syncTeamId)
                {
                    // 変更あり！

                    // 同期解除
                    if (oldSyncTeamId > 0)
                    {
                        Develop.DebugLog($"Desynchronization! (1) {teamId}");
                        ref var syncTeamData = ref GetTeamDataRef(oldSyncTeamId);
                        RemoveSyncParent(ref syncTeamData, teamId);
                    }

                    // 同期変更
                    if (syncCloth != null)
                    {
                        ref var syncTeamData = ref GetTeamDataRef(syncCloth.Process.TeamId);

                        // 相手に自身を登録
                        AddSyncParent(ref syncTeamData, teamId);

                        // 時間リセットフラグクリア
                        tdata.flag.SetBits(Flag_TimeReset, false);

                        Develop.DebugLog($"Synchronization! {teamId}->{syncCloth.Process.TeamId}");
                    }
                    else
                    {
                        // 同期解除
                        cloth.SerializeData.selfCollisionConstraint.syncPartner = null;
                        //tdata.frequency = cprocess.parameters.solverFrequency;
                        Develop.DebugLog($"Desynchronization! (2) {teamId}");
                    }

                    // セルフコリジョン更新
                    selfCollisionUpdate = true;
                }

                // 時間の同期
                if (syncCloth && tdata.syncTeamId > 0)
                {
                    ref var syncTeamData = ref GetTeamDataRef(syncCloth.Process.TeamId);
                    if (syncTeamData.IsValid)
                    {
                        // 時間同期
                        tdata.updateMode = syncTeamData.updateMode;
                        //tdata.frequency = syncTeamData.frequency;
                        tdata.time = syncTeamData.time;
                        tdata.oldTime = syncTeamData.oldTime;
                        tdata.nowUpdateTime = syncTeamData.nowUpdateTime;
                        tdata.oldUpdateTime = syncTeamData.oldUpdateTime;
                        tdata.frameUpdateTime = syncTeamData.frameUpdateTime;
                        tdata.frameOldTime = syncTeamData.frameOldTime;
                        tdata.timeScale = syncTeamData.timeScale;
                        tdata.updateCount = syncTeamData.updateCount;
                        tdata.frameInterpolation = syncTeamData.frameInterpolation;
                        //Develop.DebugLog($"Team time sync:{teamId}->{syncCloth.Process.TeamId}");
                    }

                    // パラメータ同期
                    // 同期中は一部のパラメータを連動させる
                    ref var clothParam = ref GetParametersRef(teamId);
                    clothParam.inertiaConstraint.worldInertia = syncCloth.SerializeData.inertiaConstraint.worldInertia;
                    clothParam.inertiaConstraint.movementSpeedLimit = syncCloth.SerializeData.inertiaConstraint.movementSpeedLimit.GetValue(-1);
                    clothParam.inertiaConstraint.rotationSpeedLimit = syncCloth.SerializeData.inertiaConstraint.rotationSpeedLimit.GetValue(-1);
                    clothParam.inertiaConstraint.teleportMode = syncCloth.SerializeData.inertiaConstraint.teleportMode;
                    clothParam.inertiaConstraint.teleportDistance = syncCloth.SerializeData.inertiaConstraint.teleportDistance;
                    clothParam.inertiaConstraint.teleportRotation = syncCloth.SerializeData.inertiaConstraint.teleportRotation;

                    // 同期先のセンタートランスフォームインデックスを記録
                    tdata.syncCenterTransformIndex = syncTeamData.centerTransformIndex;
                }

                // 集計まわり
                ref var param = ref GetParametersRef(teamId);
                if (param.colliderCollisionConstraint.mode == ColliderCollisionConstraint.Mode.Edge)
                    edgeColliderCollisionCount += tdata.EdgeCount;

                // セルフコリジョンのフラグやバッファ更新
                if (selfCollisionUpdate)
                {
                    //Develop.DebugLog("Update Selfcollision");
                    MagicaManager.Simulation.selfCollisionConstraint.UpdateTeam(teamId);
                }
            }

#if true
            if (ActiveTeamCount > 0)
            {
                // フレーム更新時間
                float deltaTime = Time.deltaTime;
                float fixedDeltaTime = MagicaManager.Time.FixedUpdateCount * Time.fixedDeltaTime;
                float unscaledDeltaTime = Time.unscaledDeltaTime;
                //Debug.Log($"DeltaTime:{deltaTime}, FixedDeltaTime:{fixedDeltaTime}, fixedUpdateCount:{fixedUpdateCount}");

                // このJobは即時実行させる
                var job = new AlwaysTeamUpdateJob()
                {
                    teamCount = TeamCount,
                    unityFrameDeltaTime = deltaTime,
                    unityFrameFixedDeltaTime = fixedDeltaTime,
                    unityFrameUnscaledDeltaTime = unscaledDeltaTime,
                    globalTimeScale = MagicaManager.Time.GlobalTimeScale,
                    simulationDeltaTime = MagicaManager.Time.SimulationDeltaTime,
                    maxDeltaTime = MagicaManager.Time.MaxDeltaTime,

                    maxUpdateCount = maxUpdateCount,
                    teamDataArray = teamDataArray.GetNativeArray(),
                    parameterArray = parameterArray.GetNativeArray(),
                };
                job.Run();
            }
#endif
        }

        [BurstCompile]
        struct AlwaysTeamUpdateJob : IJob
        {
            public int teamCount;
            public float unityFrameDeltaTime;
            public float unityFrameFixedDeltaTime;
            public float unityFrameUnscaledDeltaTime;
            public float globalTimeScale;
            public float simulationDeltaTime;
            public float maxDeltaTime;

            public NativeReference<int> maxUpdateCount;
            public NativeArray<TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            public void Execute()
            {
                int maxCount = 0;

                for (int teamId = 0; teamId < teamCount; teamId++)
                {
                    if (teamId == 0)
                    {
                        // グローバルチーム
                        continue;
                    }

                    var tdata = teamDataArray[teamId];
                    if (tdata.IsProcess == false)
                        continue;

                    //Debug.Log($"Team Enable:{i}");

                    // 時間リセット
                    if (tdata.flag.IsSet(Flag_TimeReset))
                    {
                        //Debug.Log($"Team time Reset:{i}");

                        tdata.time = 0;
                        tdata.oldTime = 0;
                        tdata.nowUpdateTime = 0;
                        tdata.oldUpdateTime = 0;
                        tdata.frameUpdateTime = 0;
                        tdata.frameOldTime = 0;
                    }

                    // 更新時間
                    float frameDeltaTime = tdata.IsFixedUpdate ? unityFrameFixedDeltaTime : (tdata.IsUnscaled ? unityFrameUnscaledDeltaTime : unityFrameDeltaTime);
                    tdata.frameDeltaTime = frameDeltaTime;

                    // 最大更新時間
                    float deltaTime = math.min(frameDeltaTime, maxDeltaTime);

                    // タイムスケール
                    float timeScale = tdata.timeScale * (tdata.IsUnscaled ? 1.0f : globalTimeScale);
                    timeScale = tdata.flag.IsSet(Flag_Suspend) ? 0.0f : timeScale;
                    //timeScale = tdata.IsCullingInvisible ? 0.0f : timeScale;

                    // 加算時間
                    float addTime = deltaTime * timeScale; // 今回の加算時間

                    // 時間を加算
                    float time = tdata.time + addTime;

                    //Debug.Log($"[{i}] time:{time}, addTime:{addTime}, timeScale:{timeScale}, suspend:{tdata.flag.IsSet(Flag_Suspend)}");

                    float interval = time - tdata.nowUpdateTime;
                    tdata.updateCount = (int)(interval / simulationDeltaTime); // 今回の更新回数

                    if (tdata.updateCount > 0 && addTime == 0.0f)
                    {
                        // SimulationDeltaTime加算の誤差が発生！
                        // ステップ毎のnowUpdateTime += tdata.SimulationDeltaTimeが誤差を蓄積
                        // その結果addTime=0でもintervalが一回分となり処理がまわってしまう
                        // こうなると時間補間関連で0除算が発生して数値が壊れる
                        // 誤差を修正する
                        tdata.updateCount = 0;
                        tdata.nowUpdateTime = time - simulationDeltaTime + 0.0001f;
                    }

                    // 時間まわり更新
                    if (tdata.updateCount > 0)
                    {
                        // 更新時のフレーム開始時間
                        tdata.frameOldTime = tdata.frameUpdateTime;
                        tdata.frameUpdateTime = time;

                        // 前回の更新時間
                        tdata.oldUpdateTime = tdata.nowUpdateTime;

                        //Debug.Log($"TeamUpdate!:{i}");
                    }
                    tdata.oldTime = tdata.time;
                    tdata.time = time;

                    // シミュレーション実行フラグ
                    tdata.flag.SetBits(Flag_Running, tdata.updateCount > 0);

                    teamDataArray[teamId] = tdata;

                    // 全体の最大実行回数
                    maxCount = math.max(maxCount, tdata.updateCount);

                    //Debug.Log($"[{teamId}] updateCount:{tdata.updateCount}, addtime:{addTime}, t.time:{tdata.time}, t.oldtime:{tdata.oldTime}");
                }

                maxUpdateCount.Value = maxCount;
            }
        }

        bool AddSyncParent(ref TeamData tdata, int parentTeamId)
        {
            // 最大７まで
            if (tdata.syncParentTeamId.Length == tdata.syncParentTeamId.Capacity)
            {
                Develop.LogWarning($"Synchronous team number limit!");
                return false;
            }
            tdata.syncParentTeamId.Add(parentTeamId);

            return true;
        }

        void RemoveSyncParent(ref TeamData tdata, int parentTeamId)
        {
            tdata.syncParentTeamId.RemoveItemAtSwapBack(parentTeamId);
        }

        //=========================================================================================
        /// <summary>
        /// チームごとのセンター姿勢の決定と慣性用の移動量計算
        /// および風の影響を計算
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle CalcCenterAndInertiaAndWind(JobHandle jobHandle)
        {
            var bm = MagicaManager.Bone;
            var vm = MagicaManager.VMesh;
            var wm = MagicaManager.Wind;

            var job = new CalcCenterAndInertiaAndWindJob()
            {
                teamDataArray = teamDataArray.GetNativeArray(),
                centerDataArray = MagicaManager.Team.centerDataArray.GetNativeArray(),
                teamWindArray = teamWindArray.GetNativeArray(),
                parameterArray = parameterArray.GetNativeArray(),

                positions = vm.positions.GetNativeArray(),
                rotations = vm.rotations.GetNativeArray(),
                vertexBindPoseRotations = vm.vertexBindPoseRotations.GetNativeArray(),

                fixedArray = MagicaManager.Simulation.inertiaConstraint.fixedArray.GetNativeArray(),

                transformPositionArray = bm.positionArray.GetNativeArray(),
                transformRotationArray = bm.rotationArray.GetNativeArray(),
                transformScaleArray = bm.scaleArray.GetNativeArray(),

                windZoneCount = wm.WindCount,
                windDataArray = wm.windDataArray.GetNativeArray(),
            };
            jobHandle = job.Schedule(TeamCount, 1, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct CalcCenterAndInertiaAndWindJob : IJobParallelFor
        {
            // team
            public NativeArray<TeamData> teamDataArray;
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;
            public NativeArray<TeamWindData> teamWindArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> rotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> vertexBindPoseRotations;

            // inertia
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> fixedArray;

            // transform
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformScaleArray;

            // wind
            public int windZoneCount;
            [Unity.Collections.ReadOnly]
            public NativeArray<WindManager.WindData> windDataArray;

            // チームごと
            public void Execute(int teamId)
            {
                if (teamId == 0)
                    return;
                var tdata = teamDataArray[teamId];
                if (tdata.IsProcess == false)
                    return;

                var param = parameterArray[teamId];
                var cdata = centerDataArray[teamId];

                // ■コンポーネントトランスフォーム同期
                // 同期中は同期先のコンポーネントトランスフォームからワールド慣性を計算する
                int centerTransformIndex = (tdata.syncTeamId != 0 && tdata.flag.IsSet(Flag_Synchronization)) ? tdata.syncCenterTransformIndex : cdata.centerTransformIndex;

                // ■コンポーネント位置
                float3 componentWorldPos = transformPositionArray[centerTransformIndex];
                quaternion componentWorldRot = transformRotationArray[centerTransformIndex];
                float3 componentWorldScl = transformScaleArray[centerTransformIndex];
                cdata.componentWorldPosition = componentWorldPos;
                cdata.componentWorldRotation = componentWorldRot;

                // コンポーネントスケール倍率
                float componentScaleRatio = math.length(componentWorldScl) / math.length(tdata.initScale);

                // ■クロスセンター位置
                var centerWorldPos = componentWorldPos;
                var centerWorldRot = componentWorldRot;
                var centerWorldScl = componentWorldScl;

                // 固定点リストがある場合は固定点の姿勢から算出する、ない場合はクロストランスフォームを使用する
                if (tdata.fixedDataChunk.IsValid)
                {
                    float3 cen = 0;
                    float3 nor = 0;
                    float3 tan = 0;

                    int v_start = tdata.proxyCommonChunk.startIndex;

                    int fcnt = tdata.fixedDataChunk.dataLength;
                    int fstart = tdata.fixedDataChunk.startIndex;
                    for (int i = 0; i < fcnt; i++)
                    {
                        var l_findex = fixedArray[fstart + i];
                        int vindex = l_findex + v_start;

                        cen += positions[vindex];

                        var rot = rotations[vindex];
                        rot = math.mul(rot, vertexBindPoseRotations[vindex]);

                        nor += MathUtility.ToNormal(rot);
                        tan += MathUtility.ToTangent(rot);
                    }
#if MC2_DEBUG
                    Develop.Assert(math.length(nor) > 0.0f);
                    Develop.Assert(math.length(tan) > 0.0f);
#endif
                    centerWorldPos = cen / fcnt;
                    centerWorldRot = MathUtility.ToRotation(math.normalize(nor), math.normalize(tan));
                }
                var wtol = MathUtility.WorldToLocalMatrix(centerWorldPos, centerWorldRot, centerWorldScl);

                // フレーム移動量と速度
                float3 frameDeltaVector = componentWorldPos - cdata.oldComponentWorldPosition;
                float frameDeltaAngle = MathUtility.Angle(cdata.oldComponentWorldRotation, componentWorldRot);
                //Debug.Log($"frameDeltaVector:{frameDeltaVector}, frameDeltaAngle:{frameDeltaAngle}");

                // ■テレポート判定（コンポーネント姿勢から判定する）
                // 同期時は同期先のテレポートモードとパラメータが入っている
                if (param.inertiaConstraint.teleportMode != InertiaConstraint.TeleportMode.None && tdata.IsReset == false)
                {
                    // 移動と回転どちらか一方がしきい値を超えたらテレポートと判定
                    bool isTeleport = false;
                    isTeleport = math.length(frameDeltaVector) >= param.inertiaConstraint.teleportDistance * componentScaleRatio ? true : isTeleport;
                    isTeleport = math.degrees(frameDeltaAngle) >= param.inertiaConstraint.teleportRotation ? true : isTeleport;

                    if (isTeleport)
                    {
                        switch (param.inertiaConstraint.teleportMode)
                        {
                            case InertiaConstraint.TeleportMode.Reset:
                                tdata.flag.SetBits(Flag_Reset, true);
                                break;
                            case InertiaConstraint.TeleportMode.Keep:
                                tdata.flag.SetBits(Flag_KeepTeleport, true);
                                break;
                        }
                    }
                }

                // リセットおよび最新のセンター座標として格納
                if (tdata.IsReset)
                {
                    cdata.oldComponentWorldPosition = componentWorldPos;
                    cdata.oldComponentWorldRotation = componentWorldRot;

                    cdata.frameWorldPosition = centerWorldPos;
                    cdata.frameWorldRotation = centerWorldRot;
                    cdata.frameWorldScale = centerWorldScl;
                    cdata.oldFrameWorldPosition = centerWorldPos;
                    cdata.oldFrameWorldRotation = centerWorldRot;
                    cdata.oldFrameWorldScale = centerWorldScl;
                    cdata.nowWorldPosition = centerWorldPos;
                    cdata.nowWorldRotation = centerWorldRot;
                    cdata.nowWorldScale = centerWorldScl;
                    cdata.oldWorldPosition = centerWorldPos;
                    cdata.oldWorldRotation = centerWorldRot;

                    tdata.centerWorldPosition = centerWorldPos;
                }
                else
                {
                    cdata.frameWorldPosition = centerWorldPos;
                    cdata.frameWorldRotation = centerWorldRot;
                    cdata.frameWorldScale = centerWorldScl;
                }

                // ■ワールド慣性シフト
                float3 workOldComponentPosition = cdata.oldComponentWorldPosition;
                quaternion workOldComponentRotation = cdata.oldComponentWorldRotation;
                if (tdata.IsReset)
                {
                    // リセット（なし）
                    cdata.frameComponentShiftVector = 0;
                    cdata.frameComponentShiftRotation = quaternion.identity;
                }
                else
                {
                    cdata.frameComponentShiftVector = componentWorldPos - cdata.oldComponentWorldPosition;
                    cdata.frameComponentShiftRotation = MathUtility.FromToRotation(cdata.oldComponentWorldRotation, componentWorldRot);
                    float moveShiftRatio = 0.0f;
                    float rotationShiftRatio = 0.0f;

                    // ■全体慣性シフト
                    float movementShift = 1.0f - param.inertiaConstraint.worldInertia; // 同期時は同期先の値が入っている
                    float rotationShift = 1.0f - param.inertiaConstraint.worldInertia; // 同期時は同期先の値が入っている
                    // KeepテレポートもしくはCulling時はシフト量100%で実装
                    bool keep = tdata.IsKeepReset || tdata.IsCullingInvisible;
                    movementShift = keep ? 1.0f : movementShift;
                    rotationShift = keep ? 1.0f : rotationShift;
                    if (movementShift > Define.System.Epsilon || rotationShift > Define.System.Epsilon)
                    {
                        // 全体シフトあり
                        tdata.flag.SetBits(Flag_InertiaShift, true);
                        moveShiftRatio = movementShift;
                        rotationShiftRatio = rotationShift;

                        workOldComponentPosition = math.lerp(workOldComponentPosition, componentWorldPos, movementShift);
                        workOldComponentRotation = math.slerp(workOldComponentRotation, componentWorldRot, rotationShift);
                    }

                    // ■最大移動速度制限（全体シフトの結果から計算する）
                    float movementSpeedLimit = param.inertiaConstraint.movementSpeedLimit * componentScaleRatio; // 同期時は同期先の値が入っている
                    float rotationSpeedLimit = param.inertiaConstraint.rotationSpeedLimit; // 同期時は同期先の値が入っている
                    float3 deltaVector = componentWorldPos - workOldComponentPosition;
                    float deltaAngle = MathUtility.Angle(workOldComponentRotation, componentWorldRot);
                    float frameSpeed = tdata.frameDeltaTime > 0.0f ? math.length(deltaVector) / tdata.frameDeltaTime : 0.0f;
                    float frameRotationSpeed = tdata.frameDeltaTime > 0.0f ? math.degrees(deltaAngle) / tdata.frameDeltaTime : 0.0f;
                    if (frameSpeed > movementSpeedLimit && movementSpeedLimit >= 0.0f)
                    {
                        tdata.flag.SetBits(Flag_InertiaShift, true);
                        float moveLimitRatio = math.saturate(math.max(frameSpeed - movementSpeedLimit, 0.0f) / frameSpeed);
                        moveShiftRatio = math.lerp(moveShiftRatio, 1.0f, moveLimitRatio);
                        workOldComponentPosition = math.lerp(workOldComponentPosition, componentWorldPos, moveLimitRatio);
                    }
                    if (frameRotationSpeed > rotationSpeedLimit && rotationSpeedLimit >= 0.0f)
                    {
                        tdata.flag.SetBits(Flag_InertiaShift, true);
                        float rotationLimitRatio = math.saturate(math.max(frameRotationSpeed - rotationSpeedLimit, 0.0f) / frameRotationSpeed);
                        rotationShiftRatio = math.lerp(rotationShiftRatio, 1.0f, rotationLimitRatio);
                        workOldComponentRotation = math.slerp(workOldComponentRotation, componentWorldRot, rotationLimitRatio);
                    }

                    // ■慣性シフト最終設定
                    if (tdata.IsInertiaShift)
                    {
                        cdata.frameComponentShiftVector *= moveShiftRatio;
                        cdata.frameComponentShiftRotation = math.slerp(quaternion.identity, cdata.frameComponentShiftRotation, rotationShiftRatio);

                        cdata.oldFrameWorldPosition = MathUtility.ShiftPosition(cdata.oldFrameWorldPosition, cdata.oldComponentWorldPosition, cdata.frameComponentShiftVector, cdata.frameComponentShiftRotation);
                        cdata.oldFrameWorldRotation = math.mul(cdata.frameComponentShiftRotation, cdata.oldFrameWorldRotation);

                        cdata.nowWorldPosition = MathUtility.ShiftPosition(cdata.nowWorldPosition, cdata.oldComponentWorldPosition, cdata.frameComponentShiftVector, cdata.frameComponentShiftRotation);
                        cdata.nowWorldRotation = math.mul(cdata.frameComponentShiftRotation, cdata.nowWorldRotation);
                    }
                }
                //Debug.Log($"team:[{teamId}] centerTransformIndex:{centerTransformIndex}");
                //Debug.Log($"team:[{teamId}] worldInertia:{param.inertiaConstraint.worldInertia}");
                //Debug.Log($"team:[{teamId}] movementSpeedLimit:{param.inertiaConstraint.movementSpeedLimit}");
                //Debug.Log($"team:[{teamId}] rotationSpeedLimit:{param.inertiaConstraint.rotationSpeedLimit}");

                // ■ワールド移動方向と速度割り出し（慣性シフト後の移動量で計算）
                float3 movingVector = componentWorldPos - workOldComponentPosition;
                float movingLength = math.length(movingVector);
                cdata.frameMovingSpeed = tdata.frameDeltaTime > 0.0f ? movingLength / tdata.frameDeltaTime : 0.0f;
                cdata.frameMovingDirection = movingLength > 1e-06f ? movingVector / movingLength : 0;

                //Debug.Log($"frameWorldPosition:{cdata.frameWorldPosition}, framwWorldRotation:{cdata.frameWorldRotation.value}");
                //Debug.Log($"oldFrameWorldPosition:{cdata.oldFrameWorldPosition}, oldFrameWorldRotation:{cdata.oldFrameWorldRotation.value}");
                //Debug.Log($"nowWorldPosition:{cdata.nowWorldPosition}, nowWorldRotation:{cdata.nowWorldRotation.value}");
                //Debug.Log($"oldWorldPosition:{cdata.oldWorldPosition}, oldWorldRotation:{cdata.oldWorldRotation.value}");

                // センターローカル座標
                float3 localCenterPos = MathUtility.InverseTransformPoint(centerWorldPos, wtol);
                cdata.frameLocalPosition = localCenterPos;

                // 速度安定化処理
                if (tdata.flag.IsSet(Flag_Reset) || tdata.flag.IsSet(Flag_TimeReset))
                {
                    tdata.velocityWeight = param.stablizationTimeAfterReset > 1e-06f ? 0.0f : 1.0f;
                    tdata.blendWeight = tdata.velocityWeight;
                }

                // 風の影響を計算
                Wind(teamId, param, centerWorldPos);

                centerDataArray[teamId] = cdata;
                teamDataArray[teamId] = tdata;
            }

            /// <summary>
            /// チームが受ける風ゾーンのリストを作成する
            /// ゾーンが追加タイプでない場合はチームが接触する最も体積が小さいゾーンが１つ有効になる。
            /// ゾーンが追加タイプの場合は最大３つまでが有効になる。
            /// </summary>
            /// <param name="teamId"></param>
            /// <param name="param"></param>
            /// <param name="centerWorldPos"></param>
            void Wind(int teamId, in ClothParameters param, in float3 centerWorldPos)
            {
                var oldTeamWindData = teamWindArray[teamId];
                var newTeamWindData = new TeamWindData();
                if (windZoneCount > 0 && param.wind.IsValid())
                {
                    float minVolume = float.MaxValue;
                    int addWindCount = 0;
                    int latestWindId = -1;

                    for (int windId = 0; windId < windZoneCount; windId++)
                    {
                        var wdata = windDataArray[windId];
                        if (wdata.IsValid() == false || wdata.IsEnable() == false)
                            continue;

                        // チームが風エリアに入っているか判定する
                        // 加算風は最大３つまで
                        bool isAdditin = wdata.IsAddition();
                        if (isAdditin && addWindCount >= 3)
                            continue;

                        // 風ゾーンのローカル位置
                        float3 lpos = math.transform(wdata.worldToLocalMatrix, centerWorldPos);
                        float llen = math.length(lpos);

                        // エリア判定
                        switch (wdata.mode)
                        {
                            case MagicaWindZone.Mode.BoxDirection:
                                var lv = math.abs(lpos) * 2;
                                if (lv.x > wdata.size.x || lv.y > wdata.size.y || lv.z > wdata.size.z)
                                    continue;
                                break;
                            case MagicaWindZone.Mode.SphereDirection:
                            case MagicaWindZone.Mode.SphereRadial:
                                if (llen > wdata.size.x)
                                    continue;
                                break;
                        }

                        // エリア風の場合はボリューム判定（体積が小さいものが優先）
                        if (isAdditin == false && wdata.zoneVolume > minVolume)
                            continue;

                        // 風の方向(world)
                        float3 mainDirection = wdata.worldWindDirection;
                        switch (wdata.mode)
                        {
                            case MagicaWindZone.Mode.SphereRadial:
                                if (llen <= 1e-06f)
                                    continue;
                                var v = centerWorldPos - wdata.worldPositin;
                                mainDirection = math.normalize(v);
                                break;
                        }
                        //Debug.Log($"wdir:{mainDirection}");

                        // 風力
                        float windMain = wdata.main;
                        switch (wdata.mode)
                        {
                            case MagicaWindZone.Mode.SphereRadial:
                                // 減衰
                                if (llen <= 1e-06f)
                                    continue;
                                float depth = math.saturate(llen / wdata.size.x);
                                float attenuation = wdata.attenuation.EvaluateCurveClamp01(depth);
                                windMain *= attenuation;
                                break;
                        }
                        if (windMain < 0.01f)
                            continue;

                        // 計算する風として登録する
                        var windInfo = new TeamWindInfo()
                        {
                            windId = windId,
                            time = -Define.System.WindMaxTime, // マイナス値からスタート
                            main = windMain,
                            direction = mainDirection
                        };
                        if (isAdditin)
                        {
                            newTeamWindData.AddOrReplaceWindZone(windInfo, oldTeamWindData);
                            addWindCount++;
                        }
                        else
                        {
                            newTeamWindData.RemoveWindZone(latestWindId);
                            newTeamWindData.AddOrReplaceWindZone(windInfo, oldTeamWindData);
                            minVolume = wdata.zoneVolume;
                            latestWindId = windId;
                        }
                    }
                }

                // 移動風移植
                newTeamWindData.movingWind = oldTeamWindData.movingWind;
                teamWindArray[teamId] = newTeamWindData;

                // debug
                //newTeamWindData.DebugLog(teamId);
                //Debug.Log($"[{teamId}] wind:{tdata.flag.IsSet(Flag_Wind)}, windCnt:{windInfo.windCount}, zone:{windInfo.windIdList}, dir:{windInfo.windDirectionList.c0},{windInfo.windDirectionList.c1},{windInfo.windDirectionList.c2},{windInfo.windDirectionList.c3}, main:{windInfo.windMainList}");
            }
        }

        //=========================================================================================
        /// <summary>
        /// ステップごとの前処理（ステップの開始に実行される）
        /// </summary>
        /// <param name="updateIndex"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle SimulationStepTeamUpdate(int updateIndex, JobHandle jobHandle)
        {
            var job = new SimulationStepTeamUpdateJob()
            {
                updateIndex = updateIndex,
                simulationDeltaTime = MagicaManager.Time.SimulationDeltaTime,

                teamDataArray = teamDataArray.GetNativeArray(),
                parameterArray = parameterArray.GetNativeArray(),
                centerDataArray = centerDataArray.GetNativeArray(),
                teamWindArray = teamWindArray.GetNativeArray(),
            };
            jobHandle = job.Schedule(TeamCount, 1, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct SimulationStepTeamUpdateJob : IJobParallelFor
        {
            public int updateIndex;
            public float simulationDeltaTime;

            // team
            public NativeArray<TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;
            public NativeArray<TeamWindData> teamWindArray;

            // チームごと
            public void Execute(int teamId)
            {
                if (teamId == 0)
                    return;
                var tdata = teamDataArray[teamId];
                if (tdata.IsProcess == false)
                    return;

                // ■ステップ実行時のみ処理する
                bool runStep = updateIndex < tdata.updateCount;
                tdata.flag.SetBits(Flag_StepRunning, runStep);
                if (updateIndex >= tdata.updateCount)
                {
                    teamDataArray[teamId] = tdata;
                    return;
                }

                //Debug.Log($"team[{teamId}] ({updateIndex}/{tdata.updateCount})");

                // パラメータ
                var param = parameterArray[teamId];

                // ■時間更新 ---------------------------------------------------
                // nowUpdateTime更新
                tdata.nowUpdateTime += simulationDeltaTime;

                // 今回のフレーム割合を計算する
                // frameStartTimeからtime区間でのnowUpdateTimeの割合
                tdata.frameInterpolation = (tdata.nowUpdateTime - tdata.frameOldTime) / (tdata.time - tdata.frameOldTime);
                //Debug.Log($"Team[{teamId}] time.{tdata.time}, oldTime:{tdata.oldTime}, frameTime:{tdata.frameUpdateTime}, frameOldTime:{tdata.frameOldTime}, nowUpdateTime:{tdata.nowUpdateTime}, frameInterp:{tdata.frameInterpolation}");

                // ■センター ---------------------------------------------------
                // 現在ステップでのセンタートランスフォーム姿勢を求める
                var cdata = centerDataArray[teamId];
                cdata.oldWorldPosition = cdata.nowWorldPosition;
                cdata.oldWorldRotation = cdata.nowWorldRotation;
                cdata.nowWorldPosition = math.lerp(cdata.oldFrameWorldPosition, cdata.frameWorldPosition, tdata.frameInterpolation);
                cdata.nowWorldRotation = math.slerp(cdata.oldFrameWorldRotation, cdata.frameWorldRotation, tdata.frameInterpolation);
                cdata.nowWorldRotation = math.normalize(cdata.nowWorldRotation); // 必要
                float3 wscl = math.lerp(cdata.oldFrameWorldScale, cdata.frameWorldScale, tdata.frameInterpolation);
                cdata.nowWorldScale = wscl;
                //cdata.nowLocalToWorldMatrix = MathUtility.LocalToWorldMatrix(cdata.nowWorldPosition, cdata.nowWorldRotation, cdata.nowWorldScale);

                // 現在座標はteamDataにもコピーする
                tdata.centerWorldPosition = cdata.nowWorldPosition;

                // ステップごとの移動量
                cdata.stepVector = cdata.nowWorldPosition - cdata.oldWorldPosition;
                cdata.stepRotation = MathUtility.FromToRotation(cdata.oldWorldRotation, cdata.nowWorldRotation);
                float stepAngle = MathUtility.Angle(cdata.oldWorldRotation, cdata.nowWorldRotation);

                // ローカル慣性
                float localInertia = 1.0f - param.inertiaConstraint.localInertia;
                cdata.stepMoveInertiaRatio = localInertia;
                cdata.stepRotationInertiaRatio = localInertia;

                // 最終慣性
                cdata.inertiaVector = math.lerp(float3.zero, cdata.stepVector, localInertia);
                cdata.inertiaRotation = math.slerp(quaternion.identity, cdata.stepRotation, localInertia);
                //Debug.Log($"Team[{teamId}] stepSpeed:{stepSpeed}, moveInertiaRatio:{moveInertiaRatio}, inertiaVector:{cdata.inertiaVector}, rotationInertiaRatio:{rotationInertiaRatio}");

                // ■遠心力用パラメータ算出
                // 今回ステップでの回転速度と回転軸
                cdata.angularVelocity = stepAngle / simulationDeltaTime; // 回転速度(rad/s)
                if (cdata.angularVelocity > Define.System.Epsilon)
                    MathUtility.ToAngleAxis(cdata.stepRotation, out _, out cdata.rotationAxis);
                else
                    cdata.rotationAxis = 0;
                //Debug.Log($"Team[{teamId}] angularVelocity:{math.degrees(cdata.angularVelocity)}, axis:{cdata.rotationAxis}, q:{cdata.stepRotation.value}");
                //Debug.Log($"Team[{teamId}] angularVelocity:{math.degrees(cdata.angularVelocity)}, now:{cdata.nowWorldRotation.value}, old:{cdata.oldWorldRotation.value}");

                // チームスケール倍率
                tdata.scaleRatio = math.max(math.length(wscl) / math.length(tdata.initScale), 1e-06f);
                //Debug.Log($"[{teamId}] scaleRatio:{tdata.scaleRatio}");

                // ■重力方向割合 ---------------------------------------------------
                float gravityDot = 1.0f;
                if (math.lengthsq(param.gravityDirection) > Define.System.Epsilon)
                {
                    var falloffDir = math.mul(cdata.nowWorldRotation, cdata.initLocalGravityDirection);
                    gravityDot = math.dot(falloffDir, param.gravityDirection);
                    gravityDot = math.saturate(gravityDot * 0.5f + 0.5f);
                }
                tdata.gravityDot = gravityDot;
                //Develop.DebugLog($"gdot:{gravityDot}");

                // ■重力減衰 ---------------------------------------------------
                float gravityRatio = 1.0f;
                if (param.gravity > 1e-06f && param.gravityFalloff > 1e-06f)
                {
                    gravityRatio = math.lerp(math.saturate(1.0f - param.gravityFalloff), 1.0f, math.saturate(1.0f - gravityDot));
                }
                tdata.gravityRatio = gravityRatio;

                // 速度安定化時間の速度割合を更新
                if (tdata.velocityWeight < 1.0f)
                {
                    float addw = param.stablizationTimeAfterReset > 1e-06f ? simulationDeltaTime / param.stablizationTimeAfterReset : 1.0f;
                    tdata.velocityWeight = math.saturate(tdata.velocityWeight + addw);
                }
                //Debug.Log($"{tdata.velocityWeight}");

                // シミュレーション結果のブレンド割合
                tdata.blendWeight = math.saturate(tdata.velocityWeight * param.blendWeight);
                //Debug.Log($"{tdata.blendWeight}");

                // 風の時間更新
                UpdateWind(teamId, tdata, param.wind, cdata);

                // データ格納
                teamDataArray[teamId] = tdata;
                centerDataArray[teamId] = cdata;
                //Debug.Log($"[{updateIndex}/{updateCount}] frameRatio:{data.frameInterpolation}, inertiaPosition:{idata.inertiaPosition}");
            }

            // 各風ゾーンの時間更新
            void UpdateWind(int teamId, in TeamData tdata, in WindParams windParams, in InertiaConstraint.CenterData cdata)
            {
                if (windParams.IsValid() == false)
                    return;

                var teamWindData = teamWindArray[teamId];

                // ゾーン風
                int cnt = teamWindData.ZoneCount;
                for (int i = 0; i < cnt; i++)
                {
                    var windInfo = teamWindData.windZoneList[i];
                    UpdateWindTime(ref windInfo, windParams.frequency, simulationDeltaTime);
                    teamWindData.windZoneList[i] = windInfo;
                }

                // 移動風
                var movingWindInfo = teamWindData.movingWind;
                movingWindInfo.main = 0;
                if (windParams.movingWind > 0.01f)
                {
                    movingWindInfo.main = (cdata.frameMovingSpeed * windParams.movingWind) / tdata.scaleRatio;
                    movingWindInfo.direction = -cdata.frameMovingDirection;
                    UpdateWindTime(ref movingWindInfo, windParams.frequency, simulationDeltaTime);
                }
                teamWindData.movingWind = movingWindInfo;

                // 格納
                teamWindArray[teamId] = teamWindData;
            }

            void UpdateWindTime(ref TeamWindInfo windInfo, float frequency, float simulationDeltaTime)
            {
                // 風速係数
                float mainRatio = windInfo.main / Define.System.WindBaseSpeed; // 0.0 ~ 

                // 基本周期
                float freq = 0.2f + mainRatio * 0.5f;
                freq *= frequency; // 0.0 ~ 2.0f;
                freq = math.min(freq, 1.5f); // max 1.5
                freq *= simulationDeltaTime;

                // 時間加算
                windInfo.time = windInfo.time + freq;

                // timeオーバーフロー対策
                if (windInfo.time > Define.System.WindMaxTime) // 約6時間
                    windInfo.time -= Define.System.WindMaxTime * 2; // マイナス側から再スタート
            }
        }

        //=========================================================================================
        /// <summary>
        /// クロスシミュレーション更新後処理
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle PostTeamUpdate(JobHandle jobHandle)
        {
            var job = new PostTeamUpdateJob()
            {
                teamDataArray = teamDataArray.GetNativeArray(),
                centerDataArray = centerDataArray.GetNativeArray(),
            };
            jobHandle = job.Schedule(teamDataArray.Length, 1, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct PostTeamUpdateJob : IJobParallelFor
        {
            // team
            public NativeArray<TeamData> teamDataArray;
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;

            // チームごと
            public void Execute(int teamId)
            {
                var tdata = teamDataArray[teamId];
                if (tdata.IsProcess == false)
                    return;

                var cdata = centerDataArray[teamId];

                // コンポーネント位置
                cdata.oldComponentWorldPosition = cdata.componentWorldPosition;
                cdata.oldComponentWorldRotation = cdata.componentWorldRotation;

                if (tdata.IsRunning)
                {
                    // センターを更新
                    cdata.oldFrameWorldPosition = cdata.frameWorldPosition;
                    cdata.oldFrameWorldRotation = cdata.frameWorldRotation;
                    cdata.oldFrameWorldScale = cdata.frameWorldScale;

                    // 外力クリア
                    tdata.forceMode = ClothForceMode.None;
                    tdata.impactForce = 0;
                }

                // フラグリセット
                tdata.flag.SetBits(Flag_Reset, false);
                tdata.flag.SetBits(Flag_TimeReset, false);
                tdata.flag.SetBits(Flag_Running, false);
                tdata.flag.SetBits(Flag_StepRunning, false);
                tdata.flag.SetBits(Flag_KeepTeleport, false);
                tdata.flag.SetBits(Flag_InertiaShift, false);

                // 時間調整（floatの精度問題への対処）
                const float limitTime = 3600.0f; // 60min
                if (tdata.time > limitTime * 2)
                {
                    tdata.time -= limitTime;
                    tdata.oldTime -= limitTime;
                    tdata.nowUpdateTime -= limitTime;
                    tdata.oldUpdateTime -= limitTime;
                    tdata.frameUpdateTime -= limitTime;
                    tdata.frameOldTime -= limitTime;
                }

                teamDataArray[teamId] = tdata;
                centerDataArray[teamId] = cdata;
            }
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"========== Team Manager ==========");
            if (IsValid() == false)
            {
                sb.AppendLine($"Team Manager. Invalid.");
                sb.AppendLine();
                Debug.Log(sb.ToString());
                allsb.Append(sb);
            }
            else
            {
                sb.AppendLine($"Team Manager. Team:{TeamCount}, Mapping:{MappingCount}");
                sb.AppendLine($"  -teamDataArray:{teamDataArray.ToSummary()}");
                sb.AppendLine($"  -teamWindArray:{teamWindArray.ToSummary()}");
                sb.AppendLine($"  -mappingDataArray:{mappingDataArray.ToSummary()}");
                sb.AppendLine($"  -parameterArray:{parameterArray.ToSummary()}");
                sb.AppendLine($"  -centerDataArray:{centerDataArray.ToSummary()}");
                Debug.Log(sb.ToString());
                allsb.Append(sb);

                for (int i = 1; i < TeamCount; i++)
                {
                    var tdata = teamDataArray[i];
                    if (tdata.IsValid == false)
                        continue;

                    sb.Clear();

                    var cprocess = GetClothProcess(i);
                    if (cprocess == null)
                    {
                        sb.AppendLine($"ID:{i} cprocess is null!");
                        Debug.LogWarning(sb.ToString());
                        allsb.Append(sb);
                        continue;
                    }
                    var cloth = cprocess.cloth;
                    if (cloth == null)
                    {
                        sb.AppendLine($"ID:{i} cloth is null!");
                        Debug.LogWarning(sb.ToString());
                        allsb.Append(sb);
                        continue;
                    }

                    sb.AppendLine($"ID:{i} [{cprocess.Name}] state:0x{cprocess.GetStateFlag().Value:X}, Flag:0x{tdata.flag.Value:X}, Particle:{tdata.ParticleCount}, Collider:{cprocess.ColliderCapacity} Proxy:{tdata.proxyMeshType}, Mapping:{tdata.MappingCount}");
                    sb.AppendLine($"  -centerTransformIndex {tdata.centerTransformIndex}");
                    sb.AppendLine($"  -centerWorldPosition {tdata.centerWorldPosition}");
                    sb.AppendLine($"  -initScale {tdata.initScale}");
                    sb.AppendLine($"  -scaleRatio {tdata.scaleRatio}");
                    sb.AppendLine($"  -animationPoseRatio {tdata.animationPoseRatio}");
                    sb.AppendLine($"  -blendWeight {tdata.blendWeight}");

                    // 同期
                    sb.AppendLine($"  Sync:{cloth.SyncCloth}, SyncParentCount:{tdata.syncParentTeamId.Length}");

                    // chunk情報
                    sb.AppendLine($"  -ProxyTransformChunk {tdata.proxyTransformChunk}");
                    sb.AppendLine($"  -ProxyCommonChunk {tdata.proxyCommonChunk}");
                    sb.AppendLine($"  -ProxyMeshChunk {tdata.proxyMeshChunk}");
                    sb.AppendLine($"  -ProxyBoneChunk {tdata.proxyBoneChunk}");
                    sb.AppendLine($"  -ProxySkinBoneChunk {tdata.proxySkinBoneChunk}");
                    sb.AppendLine($"  -ProxyTriangleChunk {tdata.proxyTriangleChunk}");
                    sb.AppendLine($"  -ProxyEdgeChunk {tdata.proxyEdgeChunk}");
                    sb.AppendLine($"  -BaseLineChunk {tdata.baseLineChunk}");
                    sb.AppendLine($"  -BaseLineDataChunk {tdata.baseLineDataChunk}");
                    sb.AppendLine($"  -ParticleChunk {tdata.particleChunk}");
                    sb.AppendLine($"  -ColliderChunk {tdata.colliderChunk}");
                    sb.AppendLine($"  -ColliderTrnasformChunk {tdata.colliderTransformChunk}");
                    sb.AppendLine($"  -colliderCount {tdata.colliderCount}");

                    // mapping情報
                    sb.AppendLine($"  *Mapping Count {tdata.MappingCount}");
                    if (tdata.MappingCount > 0)
                    {
                        for (int j = 0; j < tdata.MappingCount; j++)
                        {
                            int mid = tdata.mappingDataIndexSet[j];
                            var mdata = mappingDataArray[mid];
                            sb.AppendLine($"  *Mapping Mid:{mid}, Vertex:{mdata.VertexCount}");
                            sb.AppendLine($"    -teamId:{mdata.teamId}");
                            sb.AppendLine($"    -centerTransformIndex:{mdata.centerTransformIndex}");
                            sb.AppendLine($"    -mappingCommonChunk:{mdata.mappingCommonChunk}");
                            sb.AppendLine($"    -toProxyMatrix:{mdata.toProxyMatrix}");
                            sb.AppendLine($"    -toProxyRotation:{mdata.toProxyRotation}");
                            sb.AppendLine($"    -sameSpace:{mdata.sameSpace}");
                            sb.AppendLine($"    -toMappingMatrix:{mdata.toMappingMatrix}");
                            sb.AppendLine($"    -scaleRatio:{mdata.scaleRatio}");
                        }
                    }

                    // constraint
                    sb.AppendLine($"  +DistanceStartChunk {tdata.distanceStartChunk}");
                    sb.AppendLine($"  +DistanceDataChunk {tdata.distanceDataChunk}");
                    sb.AppendLine($"  +BendingPairChunk {tdata.bendingPairChunk}");
                    sb.AppendLine($"  +selfPointChunk {tdata.selfPointChunk}");
                    sb.AppendLine($"  +selfEdgeChunk {tdata.selfEdgeChunk}");
                    sb.AppendLine($"  +selfTriangleChunk {tdata.selfTriangleChunk}");

                    // wind
                    var wdata = teamWindArray[i];
                    sb.AppendLine($"  #Wind ZoneCount:{wdata.ZoneCount}");
                    for (int j = 0; j < wdata.ZoneCount; j++)
                    {
                        sb.AppendLine($"    [{j}] {wdata.windZoneList[j].ToString()}");
                    }
                    sb.AppendLine($"    [Move] {wdata.movingWind.ToString()}");


                    Debug.Log(sb.ToString());
                    allsb.Append(sb);
                }
                allsb.AppendLine();
            }
        }
    }
}
