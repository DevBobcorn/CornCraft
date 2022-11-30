// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// チームデータ
    /// チーム０はグローバルとして扱う
    /// </summary>
    public class PhysicsManagerTeamData : PhysicsManagerAccess
    {
        /// <summary>
        /// チームフラグビット
        /// </summary>
        public const uint Flag_Enable = 0x00000001; // 有効フラグ
        public const uint Flag_Interpolate = 0x00000002; // 補間処理適用
        public const uint Flag_FixedNonRotation = 0x00000004; // 固定パーティクルは回転させない
        public const uint Flag_AnimatedPose = 0x00000008; // アニメーションされた姿勢を使用する
        public const uint Flag_IgnoreClampPositionVelocity = 0x00000010; // ClampPositionの最大移動速度制限を無視する(Spring系)
        public const uint Flag_Collision = 0x00000020;
        public const uint Flag_AfterCollision = 0x00000040;
        public const uint Flag_UpdatePhysics = 0x00000080; // 更新をUnityPhysicsに合わせる
        public const uint Flag_Pause = 0x00000100; // 停止中
        //public const uint Flag_Update = 0x00000004; // 更新フラグ
        public const uint Flag_Reset_WorldInfluence = 0x00010000; // ワールド影響をリセットする
        public const uint Flag_Reset_Position = 0x00020000; // クロスパーティクル姿勢をリセット
        //public const uint Flag_Collision_KeepShape = 0x00040000; // 当たり判定時の初期姿勢をキープ(v1.8.8で廃止)
        public const uint Flag_Reset_Keep = 0x00080000; // ワールド影響を最大にしてリセットする
        public const uint Flag_Wind = 0x00100000; // 風の計算あり

        /// <summary>
        /// 速度変更モード
        /// </summary>
        public enum ForceMode
        {
            None,

            VelocityAdd,                    // 速度に加算（質量の影響を受ける）
            VelocityChange,                 // 速度を変更（質量の影響を受ける）

            VelocityAddWithoutMass = 10,    // 速度に加算（質量無視）
            VelocityChangeWithoutMass,      // 速度を変更（質量無視）
        }

        /// <summary>
        /// チーム状態
        /// </summary>
        public struct TeamData
        {
            /// <summary>
            /// チームが生成したパーティクル（コライダーパーティクルは除くので注意）
            /// </summary>
            public ChunkData particleChunk;

            /// <summary>
            /// チームが生成したコライダーパーティクルリスト
            /// </summary>
            public ChunkData colliderChunk;

            public ChunkData skinningBoneChunk;

            /// <summary>
            /// フラグビットデータ
            /// </summary>
            public uint flag;

            /// <summary>
            /// 動摩擦係数(0.0-1.0)
            /// </summary>
            public float dynamicFriction;

            /// <summary>
            /// 静止摩擦係数(0.0-1.0)
            /// </summary>
            public float staticFriction;

            /// <summary>
            /// セルフコリジョンの影響範囲
            /// </summary>
            public float selfCollisionRange;

            /// <summary>
            /// 自身のボーンインデックス
            /// </summary>
            public int boneIndex;

            /// <summary>
            /// 現在のチームスケール倍率
            /// </summary>
            public float3 initScale;            // 自身のボーンのクロスデータ作成時のスケール長
            public float scaleRatio;            // スケール倍率
            public float3 scaleDirection;       // スケール値方向(xyz)：(1/-1)のみ
            public float4 quaternionScale;      // 回転フリップ用スケール

            /// <summary>
            /// チーム固有の現在時間
            /// </summary>
            public float time;

            /// <summary>
            /// チームが最後に更新処理を行った時間
            /// </summary>
            public float oldTime;

            /// <summary>
            /// チーム固有の更新時間(deltaTime)
            /// </summary>
            public float addTime;

            /// <summary>
            /// チーム固有のタイムスケール(0.0-1.0)
            /// </summary>
            public float timeScale;

            /// <summary>
            /// チーム内更新時間
            /// </summary>
            public float nowTime;

            /// <summary>
            /// チームでの最初の物理更新が実行される時間
            /// </summary>
            public float startTime;

            /// <summary>
            /// チーム更新回数
            /// </summary>
            public int updateCount;

            /// <summary>
            /// ブレンド率(0.0-1.0)
            /// </summary>
            public float blendRatio;

            /// <summary>
            /// 外力（計算結果）
            /// </summary>
            public float3 externalForce;

            /// <summary>
            /// 外力の重力影響率
            /// </summary>
            public float forceMassInfluence;

            /// <summary>
            /// 風の影響率
            /// </summary>
            public float forceWindInfluence;

            /// <summary>
            /// 風のランダム率
            /// </summary>
            public float forceWindRandomScale;

            /// <summary>
            /// 風の同期率
            /// </summary>
            public float forceWindSynchronization;

            /// <summary>
            /// 現在の速度ウエイト
            /// </summary>
            public float velocityWeight;

            /// <summary>
            /// 速度ウエイトの回復速度(s)
            /// </summary>
            public float velocityRecoverySpeed;

            /// <summary>
            /// 重力方向（単位ベクトルかつ天井方向）
            /// </summary>
            public float3 gravityDirection;


            public ForceMode forceMode;
            public float3 impactForce;

            /// <summary>
            /// 計算実行カウント
            /// </summary>
            public int calcCount;

            /// <summary>
            /// 距離拘束データへのインデックス
            /// </summary>
            public short restoreDistanceGroupIndex;
            public short triangleBendGroupIndex;
            public short clampDistanceGroupIndex;
            public short clampDistance2GroupIndex;
            public short clampPositionGroupIndex;
            public short clampRotationGroupIndex;  // Algorithm 1
            public short restoreRotationGroupIndex; // Algorithm 1
            public short adjustRotationGroupIndex;
            public short springGroupIndex;
            public short volumeGroupIndex;
            public short airLineGroupIndex;
            public short lineWorkerGroupIndex;
            public short triangleWorkerGroupIndex;
            public short selfCollisionGroupIndex;
            public short edgeCollisionGroupIndex;
            public short penetrationGroupIndex;
            public short baseSkinningGroupIndex;
            public short twistGroupIndex;
            public short compositeRotationGroupIndex; // Algorithm 2

            /// <summary>
            /// データが有効か判定する
            /// </summary>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsActive()
            {
                return (flag & Flag_Enable) != 0;
            }

            /// <summary>
            /// 今回のフレームで更新があるか判定する
            /// </summary>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsRunning()
            {
                return updateCount > 0;
            }

            /// <summary>
            /// このステップを更新すべきか判定する
            /// </summary>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsUpdate(int runCount)
            {
                return runCount < updateCount;
            }

            /// <summary>
            /// 補間を行うか判定する
            /// </summary>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsInterpolate()
            {
                return (flag & Flag_Interpolate) != 0;
            }

            /// <summary>
            /// UnityPhysicsのタイミングで更新するか判定する
            /// </summary>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsPhysicsUpdate()
            {
                return (flag & Flag_UpdatePhysics) != 0;
            }

            /// <summary>
            /// フラグ判定
            /// </summary>
            /// <param name="flag"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsFlag(uint flag)
            {
                return (this.flag & flag) != 0;
            }

            /// <summary>
            /// フラグ設定
            /// </summary>
            /// <param name="flag"></param>
            /// <param name="sw"></param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetFlag(uint flag, bool sw)
            {
                if (sw)
                    this.flag |= flag;
                else
                    this.flag &= ~flag;
            }

            /// <summary>
            /// パーティクル座標リセットが必要か判定する
            /// </summary>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsReset()
            {
                return (flag & (Flag_Reset_WorldInfluence | Flag_Reset_Position)) != 0;
            }

            /// <summary>
            /// 計算停止中判定
            /// </summary>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsPause()
            {
                return (flag & Flag_Pause) != 0;
            }
        }

        /// <summary>
        /// チームデータリスト
        /// </summary>
        public FixedNativeList<TeamData> teamDataList;

        public FixedNativeList<CurveParam> teamMassList;
        public FixedNativeList<CurveParam> teamGravityList;
        public FixedNativeList<CurveParam> teamDragList;
        public FixedNativeList<CurveParam> teamMaxVelocityList;
        public FixedNativeList<CurveParam> teamDepthInfluenceList;
        //public FixedNativeList<CurveParam> teamDirectionalDampingList;

        /// <summary>
        /// チームのワールド移動回転影響
        /// </summary>
        public struct WorldInfluence
        {
            /// <summary>
            /// 影響力(0.0-1.0)
            /// </summary>
            public CurveParam moveInfluence;
            public CurveParam rotInfluence;
            public float maxMoveSpeed;      // (m/s)
            public float maxRotationSpeed;  // (deg/s)

            /// <summary>
            /// ワールド移動量
            /// </summary>
            public float3 nowPosition;
            public float3 oldPosition;
            public float3 moveOffset;       // 今回の移動
            public float moveIgnoreRatio;   // 移動を無視する割合

            /// <summary>
            /// ワールド回転量
            /// </summary>
            public quaternion nowRotation;
            public quaternion oldRotation;
            public quaternion rotationOffset;   // 今回の回転
            public float rotationIgnoreRatio;   // 回転を無視する割合

            /// <summary>
            /// テレポート
            /// </summary>
            public int resetTeleport;
            public float teleportDistance;
            public float teleportRotation;
            public ClothParams.TeleportMode teleportMode;

            /// <summary>
            /// リセット
            /// </summary>
            public float stabilizationTime; // 安定化時間(s)

            /// <summary>
            /// ClampRotationの設定
            /// 速度制限を行うため必要
            /// </summary>
            public float clampRotationLimit; // (0.0-1.0)
        }
        public FixedNativeList<WorldInfluence> teamWorldInfluenceList;

        /// <summary>
        /// 風の影響情報
        /// </summary>
        public struct WindInfo
        {
            public int windCount;
            public int4 windDataIndexList;
            public float3x4 windDirectionList;
            public float4 windMainList;
        }
        public FixedNativeList<WindInfo> teamWindInfoList;

        /// <summary>
        /// チームごとの判定コライダー
        /// </summary>
        public FixedMultiNativeList<int> colliderList;

        /// <summary>
        /// チームごとのスキニング用ボーンリスト
        /// </summary>
        public FixedMultiNativeList<int> skinningBoneList;

        /// <summary>
        /// チームごとのチームコンポーネント参照への辞書（キー：チームID）
        /// nullはグローバルチーム
        /// </summary>
        private Dictionary<int, PhysicsTeam> teamComponentDict = new Dictionary<int, PhysicsTeam>();


        /// <summary>
        /// 稼働中のチーム数
        /// </summary>
        int activeTeamCount;

        /// <summary>
        /// 各更新モードのチーム数
        /// </summary>
        int normalUpdateCount = 0;
        int physicsUpdateCount = 0;

        //=========================================================================================
        /// <summary>
        /// 初期設定
        /// </summary>
        public override void Create()
        {
            teamDataList = new FixedNativeList<TeamData>();
            teamMassList = new FixedNativeList<CurveParam>();
            teamGravityList = new FixedNativeList<CurveParam>();
            teamDragList = new FixedNativeList<CurveParam>();
            teamMaxVelocityList = new FixedNativeList<CurveParam>();
            teamDepthInfluenceList = new FixedNativeList<CurveParam>();
            teamWorldInfluenceList = new FixedNativeList<WorldInfluence>();
            teamWindInfoList = new FixedNativeList<WindInfo>();
            //teamDirectionalDampingList = new FixedNativeList<CurveParam>();
            colliderList = new FixedMultiNativeList<int>();
            skinningBoneList = new FixedMultiNativeList<int>();

            // グローバルチーム[0]を作成し常に有効にしておく
            CreateTeam(null, Flag_Enable);
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public override void Dispose()
        {
            if (teamDataList == null)
                return;

            skinningBoneList.Dispose();
            colliderList.Dispose();
            teamMassList.Dispose();
            teamGravityList.Dispose();
            teamDragList.Dispose();
            teamMaxVelocityList.Dispose();
            teamDepthInfluenceList.Dispose();
            teamWorldInfluenceList.Dispose();
            teamWindInfoList.Dispose();
            //teamDirectionalDampingList.Dispose();
            teamDataList.Dispose();
            teamComponentDict.Clear();
        }

        //=========================================================================================
        /// <summary>
        /// 登録チーム数を返す
        /// [0]はグローバルチームなので-1する
        /// </summary>
        public int TeamCount
        {
            get
            {
                return teamDataList.Count - 1;
            }
        }

        /// <summary>
        /// チーム配列数を返す
        /// </summary>
        public int TeamLength
        {
            get
            {
                return teamDataList.Length;
            }
        }

        /// <summary>
        /// 現在活動中のチーム数を返す
        /// これが0の場合はチームが無いか、すべて停止中となっている
        /// </summary>
        public int ActiveTeamCount
        {
            get
            {
                return activeTeamCount;
            }
        }

        /// <summary>
        /// コライダーの数を返す
        /// </summary>
        public int ColliderCount
        {
            get
            {
                if (colliderList == null)
                    return 0;

                return colliderList.Count;
            }
        }

        public int NormalUpdateCount
        {
            get
            {
                return normalUpdateCount;
            }
        }

        public int PhysicsUpdateCount
        {
            get
            {
                return physicsUpdateCount;
            }
        }

        public int PauseCount
        {
            get
            {
                int cnt = 0;
                for (int i = 0; i < teamDataList.Length; i++)
                {
                    var team = teamDataList[i];
                    if (team.IsActive() && team.IsPause())
                        cnt++;
                }
                return cnt;
            }
        }

        //=========================================================================================
        /// <summary>
        /// チームを作成する
        /// </summary>
        /// <returns></returns>
        public int CreateTeam(PhysicsTeam team, uint flag)
        {
            var data = new TeamData();
            //flag |= Flag_Enable; // 作成時はActiveにしない(v1.12.0)
            flag |= Flag_Reset_WorldInfluence; // 移動影響リセット
            if (team != null)
            {
                // 更新モード設定
                switch (team.UpdateMode)
                {
                    case PhysicsTeam.TeamUpdateMode.UnityPhysics:
                        flag |= Flag_UpdatePhysics;
                        break;
                }
            }
            data.flag = flag;

            data.dynamicFriction = 0;
            data.boneIndex = team != null ? 0 : -1; // グローバルチームはボーン無し
            data.initScale = 0;
            data.scaleDirection = 1;
            data.scaleRatio = 1;
            data.quaternionScale = 1;
            //data.directionalDampingBoneIndex = team != null ? 0 : -1; // グローバルチームはボーン無し
            //data.directionalDampingLocalDir = new float3(0, 1, 0);
            data.timeScale = 1.0f;
            data.blendRatio = 1.0f;
            data.forceMassInfluence = 1.0f;
            data.forceWindInfluence = 1.0f;
            data.forceWindRandomScale = 0.0f;
            data.gravityDirection = new float3(0, 1, 0);

            // 拘束チームインデックス
            data.restoreDistanceGroupIndex = -1;
            data.triangleBendGroupIndex = -1;
            data.clampDistanceGroupIndex = -1;
            data.clampDistance2GroupIndex = -1;
            data.clampPositionGroupIndex = -1;
            data.clampRotationGroupIndex = -1;
            data.restoreRotationGroupIndex = -1;
            data.adjustRotationGroupIndex = -1;
            data.springGroupIndex = -1;
            data.volumeGroupIndex = -1;
            data.airLineGroupIndex = -1;
            data.lineWorkerGroupIndex = -1;
            data.triangleWorkerGroupIndex = -1;
            data.selfCollisionGroupIndex = -1;
            data.edgeCollisionGroupIndex = -1;
            data.penetrationGroupIndex = -1;
            data.baseSkinningGroupIndex = -1;
            data.twistGroupIndex = -1;
            data.compositeRotationGroupIndex = -1;

            int teamId = teamDataList.Add(data);
            teamMassList.Add(new CurveParam(1.0f));
            teamGravityList.Add(new CurveParam());
            teamDragList.Add(new CurveParam());
            teamMaxVelocityList.Add(new CurveParam());
            teamDepthInfluenceList.Add(new CurveParam());
            //teamDirectionalDampingList.Add(new CurveParam());

            var worldInfluence = new WorldInfluence();
            if (team == null)
            {
                // グローバルチーム設定
                worldInfluence.moveInfluence = new CurveParam(1.0f);
                worldInfluence.rotInfluence = new CurveParam(1.0f);
                worldInfluence.maxMoveSpeed = 10000.0f;
                worldInfluence.maxRotationSpeed = 10000.0f;
            }
            teamWorldInfluenceList.Add(worldInfluence);
            teamWindInfoList.Add(new WindInfo());

            teamComponentDict.Add(teamId, team);

            //if (team != null)
            //    activeTeamCount++;

            return teamId;
        }

        /// <summary>
        /// チームを削除する
        /// </summary>
        /// <param name="teamId"></param>
        public void RemoveTeam(int teamId)
        {
            if (teamId >= 0)
            {
                teamDataList.Remove(teamId);
                teamMassList.Remove(teamId);
                teamGravityList.Remove(teamId);
                teamDragList.Remove(teamId);
                teamMaxVelocityList.Remove(teamId);
                teamDepthInfluenceList.Remove(teamId);
                teamWorldInfluenceList.Remove(teamId);
                teamWindInfoList.Remove(teamId);
                //teamDirectionalDampingList.Remove(teamId);
                teamComponentDict.Remove(teamId);
            }
        }

        /// <summary>
        /// チームの有効フラグ切り替え
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="sw"></param>
        public void SetEnable(int teamId, bool sw)
        {
            if (teamId >= 0)
            {
                // 有効化 | 位置リセット | 移動影響リセット
                uint flag = Flag_Enable | Flag_Reset_Position | Flag_Reset_WorldInfluence;
                SetFlag(teamId, flag, sw);
                //SetFlag(teamId, Flag_Enable, sw);
                //SetFlag(teamId, Flag_Reset_Position, sw); // 位置リセット
                //SetFlag(teamId, Flag_Reset_WorldInfluence, sw); // 移動影響リセット
            }
        }

        /// <summary>
        /// チームが存在するか判定する
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public bool IsValid(int teamId)
        {
            return teamId >= 0;
        }

        /// <summary>
        /// チームデータが存在するか判定する
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public bool IsValidData(int teamId)
        {
            return teamId >= 0 && teamComponentDict.ContainsKey(teamId);
        }

        /// <summary>
        /// チームが有効状態か判定する
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public bool IsActive(int teamId)
        {
            if (teamId >= 0)
                return teamDataList[teamId].IsActive();
            else
                return false;
        }

        /// <summary>
        /// チームの状態フラグ設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="flag"></param>
        /// <param name="sw"></param>
        public void SetFlag(int teamId, uint flag, bool sw)
        {
            if (teamId < 0)
                return;
            TeamData data = teamDataList[teamId];
            bool oldvalid = data.IsActive();
            data.SetFlag(flag, sw);
            bool newvalid = data.IsActive();
            if (oldvalid != newvalid)
            {
                // アクティブチーム数カウント
                activeTeamCount += newvalid ? 1 : -1;
            }
            teamDataList[teamId] = data;
        }

        public bool IsFlag(int teamId, uint flag)
        {
            if (teamId < 0)
                return false;
            TeamData data = teamDataList[teamId];
            return data.IsFlag(flag);
        }

        public void SetParticleChunk(int teamId, ChunkData chunk)
        {
            TeamData data = teamDataList[teamId];
            data.particleChunk = chunk;
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームの摩擦係数設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="dynamicFriction"></param>
        public void SetFriction(int teamId, float dynamicFriction, float staticFriction)
        {
            TeamData data = teamDataList[teamId];
            data.dynamicFriction = dynamicFriction;
            data.staticFriction = staticFriction;
            teamDataList[teamId] = data;
        }

        public void SetMass(int teamId, BezierParam mass)
        {
            teamMassList[teamId] = new CurveParam(mass);
        }

        public void SetGravity(int teamId, BezierParam gravity)
        {
            teamGravityList[teamId] = new CurveParam(gravity);
        }

        /// <summary>
        /// 重力（天井）方向を設定する
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="dir">天井方向を指定</param>
        public void SetGravityDirection(int teamId, float3 dir)
        {
            TeamData data = teamDataList[teamId];
            if (math.lengthsq(dir) >= Define.Compute.Epsilon)
                dir = math.normalize(dir);
            data.gravityDirection = dir;
            teamDataList[teamId] = data;
        }
        public void SetDrag(int teamId, BezierParam drag)
        {
            teamDragList[teamId] = new CurveParam(drag);
        }

        public void SetMaxVelocity(int teamId, BezierParam maxVelocity)
        {
            teamMaxVelocityList[teamId] = new CurveParam(maxVelocity);
        }

        public void SetExternalForce(int teamId, float massInfluence, float windInfluence, float windRandomScale, float windSynchronization)
        {
            TeamData data = teamDataList[teamId];
            data.forceMassInfluence = massInfluence;
            data.forceWindInfluence = windInfluence;
            data.forceWindRandomScale = windRandomScale;
            data.forceWindSynchronization = windSynchronization;
            teamDataList[teamId] = data;
        }

        public void SetDepthInfluence(int teamId, BezierParam depthInfluence)
        {
            teamDepthInfluenceList[teamId] = new CurveParam(depthInfluence);
        }


        /// <summary>
        /// ワールド移動影響設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="worldMoveInfluence"></param>
        public void SetWorldInfluence(int teamId, float maxSpeed, float maxRotatinSpeed, BezierParam moveInfluence, BezierParam rotInfluence,
            bool resetTeleport, float teleportDistance, float teleportRotation, float resetStabilizationTime, ClothParams.TeleportMode teleportMode,
            bool useClampRotation, BezierParam clampRotation
            )
        {
            var data = teamWorldInfluenceList[teamId];
            data.maxMoveSpeed = maxSpeed;
            data.maxRotationSpeed = maxRotatinSpeed;
            data.moveInfluence = new CurveParam(moveInfluence);
            data.rotInfluence = new CurveParam(rotInfluence);
            data.resetTeleport = resetTeleport ? 1 : 0;
            data.teleportDistance = teleportDistance;
            data.teleportRotation = teleportRotation;
            data.stabilizationTime = resetStabilizationTime;
            data.teleportMode = teleportMode;
            // ClampRotationによる最大移動制限値
            data.clampRotationLimit = CalcClampRotationLimit(useClampRotation, clampRotation);
            teamWorldInfluenceList[teamId] = data;
        }

        /// <summary>
        /// ClampRotationによる最大移動制限値を計算する
        /// </summary>
        /// <param name="useClampRotation"></param>
        /// <param name="clampRotation"></param>
        /// <returns></returns>
        float CalcClampRotationLimit(bool useClampRotation, BezierParam clampRotation)
        {
            var averageAngle = useClampRotation ? clampRotation.Evaluate(0.5f) : 180.0f;
            var clampLimit = math.pow(math.saturate(averageAngle / 90.0f), 0.5f);
            //Debug.Log($"ClampLimit:{clampLimit}");
            return clampLimit;
        }

        public void SetWorldInfluence(int teamId, float maxSpeed, float maxRotationSpeed, BezierParam moveInfluence, BezierParam rotInfluence)
        {
            var data = teamWorldInfluenceList[teamId];
            data.maxMoveSpeed = maxSpeed;
            data.maxRotationSpeed = maxRotationSpeed;
            data.moveInfluence = new CurveParam(moveInfluence);
            data.rotInfluence = new CurveParam(rotInfluence);
            teamWorldInfluenceList[teamId] = data;
        }

        public void SetAfterTeleport(int teamId, bool resetTeleport, float teleportDistance, float teleportRotation, ClothParams.TeleportMode teleportMode)
        {
            var data = teamWorldInfluenceList[teamId];
            data.resetTeleport = resetTeleport ? 1 : 0;
            data.teleportDistance = teleportDistance;
            data.teleportRotation = teleportRotation;
            data.teleportMode = teleportMode;
            teamWorldInfluenceList[teamId] = data;
        }

        public void SetStabilizationTime(int teamId, float resetStabilizationTime)
        {
            var data = teamWorldInfluenceList[teamId];
            data.stabilizationTime = resetStabilizationTime;
            teamWorldInfluenceList[teamId] = data;
        }

        public void ResetWorldInfluenceTarget(int teamId, Transform target)
        {
            float3 pos = target.position;
            quaternion rot = target.rotation;
            var data = teamWorldInfluenceList[teamId];
            data.nowPosition = pos;
            data.oldPosition = pos;
            data.nowRotation = rot;
            data.oldRotation = rot;
            teamWorldInfluenceList[teamId] = data;
        }

        public void SetClampRotation(int teamId, bool useClampRotation, BezierParam clampRotation)
        {
            var data = teamWorldInfluenceList[teamId];
            // ClampRotationによる最大移動制限値
            data.clampRotationLimit = CalcClampRotationLimit(useClampRotation, clampRotation);
            teamWorldInfluenceList[teamId] = data;
        }

        /// <summary>
        /// セルフコリジョンの影響範囲設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="range"></param>
        public void SetSelfCollisionRange(int teamId, float range)
        {
            TeamData data = teamDataList[teamId];
            data.selfCollisionRange = range;
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームのボーンインデックスを設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="boneIndex"></param>
        public void SetBoneIndex(int teamId, int boneIndex, Vector3 initScale)
        {
            TeamData data = teamDataList[teamId];
            data.boneIndex = boneIndex;
            data.initScale = initScale;
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームにコライダーパーティクルを追加
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="particleIndex"></param>
        internal void AddColliderParticle(int teamId, int particleIndex)
        {
            //Develop.Log($"AddColliderParticle team:{teamId} pindex:{particleIndex}");
            TeamData data = teamDataList[teamId];
            var c = data.colliderChunk;
            if (c.IsValid() == false)
            {
                // 新規
                c = colliderList.AddChunk(4);
            }
            // 追加
            c = colliderList.AddData(c, particleIndex);

            data.colliderChunk = c;
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームからコライダーパーティクルを削除
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="particleIndex"></param>
        internal void RemoveColliderParticle(int teamId, int particleIndex)
        {
            //Develop.Log($"RemoveColliderParticle team:{teamId} pindex:{particleIndex}");
            TeamData data = teamDataList[teamId];
            var c = data.colliderChunk;
            if (c.IsValid())
            {
                c = colliderList.RemoveData(c, particleIndex);
                data.colliderChunk = c;
                teamDataList[teamId] = data;
            }
        }

        /// <summary>
        /// チームからコライダーコンポーネントを削除する
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="collider"></param>
        internal void RemoveCollider(int teamId, ColliderComponent collider)
        {
            if (collider == null)
                return;
            if (teamComponentDict.ContainsKey(teamId))
            {
                var team = teamComponentDict[teamId];
                team.TeamData.RemoveCollider(collider);
            }
        }

        /// <summary>
        /// チームのコライダーをすべて削除
        /// </summary>
        /// <param name="teamId"></param>
        //public void RemoveCollider(int teamId)
        //{
        //    //Develop.Log($"RemoveAllCollider team:{teamId}");
        //    TeamData data = teamDataList[teamId];
        //    var c = data.colliderChunk;
        //    if (c.IsValid())
        //    {
        //        colliderList.RemoveChunk(c);
        //        data.colliderChunk = new ChunkData();
        //        teamDataList[teamId] = data;
        //    }
        //}

        /// <summary>
        /// チームのコライダートランスフォームの未来予測をリセットする
        /// </summary>
        /// <param name="teamId"></param>
        public void ResetFuturePredictionCollidere(int teamId)
        {
            TeamData data = teamDataList[teamId];
            var c = data.colliderChunk;
            if (c.IsValid())
            {
                colliderList.Process(c, (pindex) =>
                {
                    MagicaPhysicsManager.Instance.Particle.ResetFuturePredictionTransform(pindex);
                });
            }
        }

        /// <summary>
        /// チームにスキニングボーンインデックスを追加
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="particleIndex"></param>
        public void AddSkinningBoneIndex(int teamId, List<Transform> boneList)
        {
            if (boneList.Count == 0)
                return;
            TeamData data = teamDataList[teamId];
            var c = data.skinningBoneChunk;
            if (c.IsValid() == false)
            {
                // 新規
                c = skinningBoneList.AddChunk(boneList.Count);
            }

            // 追加
            foreach (var bone in boneList)
            {
                if (bone)
                {
                    int boneIndex = Bone.AddBone(bone);
                    c = skinningBoneList.AddData(c, boneIndex);
                }
            }

            data.skinningBoneChunk = c;
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームのスキニングボーンインデックスをすべて削除
        /// </summary>
        /// <param name="teamId"></param>
        public void RemoveSkinningBoneIndex(int teamId)
        {
            TeamData data = teamDataList[teamId];
            var c = data.skinningBoneChunk;
            if (c.IsValid())
            {
                for (int i = 0, index = c.startIndex; i < c.dataLength; i++, index++)
                {
                    int boneIndex = skinningBoneList[index];
                    Bone.RemoveBone(boneIndex);
                }

                skinningBoneList.RemoveChunk(c);
                data.skinningBoneChunk = new ChunkData();
                teamDataList[teamId] = data;
            }
        }


        /// <summary>
        /// チームのタイムスケールを設定する
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="timeScale">0.0-1.0</param>
        public void SetTimeScale(int teamId, float timeScale)
        {
            TeamData data = teamDataList[teamId];
            data.timeScale = Mathf.Clamp01(timeScale);
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームのタイムスケールを取得する
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public float GetTimeScale(int teamId)
        {
            return teamDataList[teamId].timeScale;
        }

        /// <summary>
        /// チームのブレンド率を設定する
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="blendRatio"></param>
        public void SetBlendRatio(int teamId, float blendRatio)
        {
            TeamData data = teamDataList[teamId];
            data.blendRatio = Mathf.Clamp01(blendRatio);
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームのブレンド率を取得する
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public float GetBlendRatio(int teamId)
        {
            return teamDataList[teamId].blendRatio;
        }

        /// <summary>
        /// 外力を与える
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="force">１秒あたりの外力</param>
        public void SetImpactForce(int teamId, float3 force, ForceMode mode)
        {
            TeamData data = teamDataList[teamId];
            data.impactForce = force;
            data.forceMode = mode;
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームのリセット後の安定化時間を設定する
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="resetStabilizationTime">安定時間(s).マイナスの場合はインスペクタで設定された時間</param>
        public void ResetStabilizationTime(int teamId, float resetStabilizationTime = -1.0f)
        {
            TeamData data = teamDataList[teamId];
            data.velocityWeight = 0;
            if (resetStabilizationTime >= 0.0f)
            {
                data.velocityRecoverySpeed = resetStabilizationTime;
            }
            else
            {
                // インスペクタで設定された値
                var wdata = teamWorldInfluenceList[teamId];
                data.velocityRecoverySpeed = wdata.stabilizationTime;
            }
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームの更新モードを変更する
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="updateMode"></param>
        public void SetUpdateMode(int teamId, PhysicsTeam.TeamUpdateMode updateMode)
        {
            //Debug.Log($"SetUpdateMode:{updateMode}");
            TeamData data = teamDataList[teamId];
            switch (updateMode)
            {
                case PhysicsTeam.TeamUpdateMode.Normal:
                    data.SetFlag(Flag_UpdatePhysics, false);
                    break;
                case PhysicsTeam.TeamUpdateMode.UnityPhysics:
                    data.SetFlag(Flag_UpdatePhysics, true);
                    break;
            }
            teamDataList[teamId] = data;
        }

        /// <summary>
        /// チームが参照するすべてのボーンに対してUnityPhysicsでの更新状態を変更する
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="sw"></param>
        public void ChangeUseUnityPhysics(int teamId, bool sw)
        {
            TeamData data = teamDataList[teamId];

            // チームのボーンインデックス
            if (data.boneIndex >= 0)
            {
                Bone.ChangeUnityPhysicsCount(data.boneIndex, sw);
            }

            // パーティクルと連動するボーンのUnityPhysicsフラグを切り替える
            // ただしMeshClothは除く（パフォーマンスに配慮）
            var team = teamComponentDict[teamId];
            if (team.GetComponentType() != ComponentType.MeshCloth)
            {
                var chunk = data.particleChunk;
                for (int i = 0; i < chunk.dataLength; i++)
                {
                    int pindex = chunk.startIndex + i;
                    ChangeParticleUseUnityPhysics(pindex, sw);
                }
            }

            // コライダーパーティクルと連動するボーンのUnityPhysicsフラグを切り替える
            var chunk2 = data.colliderChunk;
            for (int i = 0; i < chunk2.useLength; i++) // FixedMultiNativeListはuseLengthなので注意！
            {
                int pindex = colliderList[chunk2.startIndex + i];
                ChangeParticleUseUnityPhysics(pindex, sw);
            }
        }

        private void ChangeParticleUseUnityPhysics(int pindex, bool unityPhysics)
        {
            var flag = Particle.flagList[pindex];
            if (flag.IsReadTransform()) // ※ここはIsValid()フラグを見ない(Disable中のコライダーなどを考慮)
            {
                // particle flag
                flag.SetFlag(PhysicsManagerParticleData.Flag_Transform_UnityPhysics, unityPhysics);
                Particle.flagList[pindex] = flag;

                // bone UnityPhysics count
                int transformIndex = Particle.transformIndexList[pindex];
                if (transformIndex >= 0)
                {
                    Bone.ChangeUnityPhysicsCount(transformIndex, unityPhysics);
                }
            }
        }

        /// <summary>
        /// カリングモードに応じてボーンの計算フラグを変更する
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="cullingMode"></param>
        /// <param name="isCalculation"></param>
        internal void ChangeBoneFlag(int teamId, PhysicsTeam.TeamCullingMode cullingMode, bool isCalculation)
        {
            TeamData data = teamDataList[teamId];

            var boneFlagList = Bone.boneFlagList.ToJobArray();
            var boneFlagList1 = Bone.boneFlagList.ToJobArray(1);

            // 遅延実行判定
            var delayed = UpdateTime.IsDelay && boneFlagList1.IsCreated && boneFlagList1.Length == boneFlagList.Length;

            // 設定フラグ構築
            byte flag = 0;
            switch (cullingMode)
            {
                case PhysicsTeam.TeamCullingMode.Off:
                    flag |= PhysicsManagerBoneData.Flag_Restore;
                    flag |= PhysicsManagerBoneData.Flag_Write;
                    delayed = false;
                    break;
                case PhysicsTeam.TeamCullingMode.Reset:
                    flag |= PhysicsManagerBoneData.Flag_Restore;
                    if (isCalculation)
                        flag |= PhysicsManagerBoneData.Flag_Write;
                    delayed = false;
                    break;
                case PhysicsTeam.TeamCullingMode.Pause:
                    if (isCalculation)
                    {
                        flag |= PhysicsManagerBoneData.Flag_Restore;
                        flag |= PhysicsManagerBoneData.Flag_Write;
                    }
                    if (data.calcCount < 4) // ※エディタ環境の問題なのか4程度必要！
                    {
                        // 計算されていない場合はダブルバッファへの書き込みをスキップする
                        delayed = false;
                    }
                    break;
            }

            var c = data.particleChunk;
            for (int i = 0, pindex = c.startIndex; i < c.dataLength; i++, pindex++)
            {
                var boneIndex = Particle.transformIndexList[pindex];
                if (boneIndex >= 0)
                {
                    var bflag = boneFlagList[boneIndex];
                    bflag = (byte)(bflag & 0xf); // 上位4bitをクリア
                    bflag |= flag;
                    boneFlagList[boneIndex] = bflag;

                    // 遅延実行用ダブルバッファにも書き込み
                    if (delayed)
                        boneFlagList1[boneIndex] = bflag;
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// フレーム開始時に行うチーム更新
        /// アクティブ状態に限らず行う（メインスレッド）
        /// </summary>
        internal void EarlyUpdateTeamAlways()
        {
            foreach (var team in teamComponentDict.Values)
            {
                // BoneCloth/BoneSpringの表示判定
                if (team != null && team.isActiveAndEnabled && team.CullingMode != PhysicsTeam.TeamCullingMode.Off)
                {
                    switch (team.GetComponentType())
                    {
                        case ComponentType.BoneCloth:
                        case ComponentType.BoneSpring:
                            team.UpdateCullingMode(team);
                            break;
                    }
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// シミュレーション計算前に行うチーム更新
        /// アクティブ状態に限らず行う（メインスレッド）
        /// </summary>
        internal void PreUpdateTeamAlways()
        {
            normalUpdateCount = 0;
            physicsUpdateCount = 0;

            // マネージャー停止中ならばこれ以降は処理しない(v1.12.0)
            if (manager.IsActive == false)
                return;

            var mainCamera = Camera.main != null ? Camera.main.transform : manager.transform;
            foreach (var team in teamComponentDict.Values)
            {
                var baseCloth = team as BaseCloth;
                if (baseCloth == null)
                    continue;

                // 距離ブレンド率更新
                float blend = 1.0f;
                if (baseCloth.Params.UseDistanceDisable)
                {
                    var refObject = baseCloth.Params.DisableReferenceObject;
                    if (refObject == null)
                        refObject = mainCamera;

                    float dist = Vector3.Distance(team.transform.position, refObject.position);
                    float disableDist = baseCloth.Params.DisableDistance;
                    float fadeDist = Mathf.Max(disableDist - (baseCloth.Params.DisableFadeDistance + Define.Compute.Epsilon/*FadeDistance=0の対処*/), 0.0f);
                    blend = Mathf.InverseLerp(disableDist, fadeDist, dist);
                }
                baseCloth.Setup.DistanceBlendRatio = blend;

                // ブレンド率更新
                baseCloth.UpdateBlend();

                // 各更新モード別のチーム数
                if (baseCloth.Status.IsActive)
                {
                    switch (baseCloth.UpdateMode)
                    {
                        case PhysicsTeam.TeamUpdateMode.Normal:
                            normalUpdateCount++;
                            break;
                        case PhysicsTeam.TeamUpdateMode.UnityPhysics:
                            physicsUpdateCount++;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 今回フレームの最大実効回数を求める
        /// </summary>
        /// <param name="ups"></param>
        /// <returns></returns>
        internal int CalcMaxUpdateCount(int ups, float deltaTime, float physicsDeltaTime, float updateDeltaTime)
        {
            // マネージャの固定更新判定
            bool once = manager.UpdateTime.GetUpdateMode() == UpdateTimeManager.UpdateMode.OncePerFrame;

            // 全チームの最大実行回数を求める
            float globalTimeScale = manager.GetGlobalTimeScale();
            int maxcnt = 0;
            foreach (var kv in teamComponentDict)
            {
                if (kv.Value == null)
                    continue;

                int tid = kv.Key;
                if (tid <= 0)
                    continue;

                var tdata = teamDataList[tid];
                bool updatePhysics = tdata.IsPhysicsUpdate();

                int cnt = 0;
                if (once && updatePhysics == false)
                {
                    // 固定更新は１回（ただしUnityPhysics更新は除く）
                    cnt = 1;
                }
                else
                {
                    float timeScale = tdata.timeScale * globalTimeScale;
                    timeScale = tdata.IsPause() ? 0.0f : timeScale; // 一時停止
                    float addTime = (updatePhysics ? physicsDeltaTime : deltaTime) * timeScale;
                    float nowTime = tdata.nowTime + addTime;
                    cnt = (int)(nowTime / updateDeltaTime);
                }

                // リセットフラグがONならば最低１回更新とする(ただしUnityPhysics更新は除く)
                if (tdata.IsReset() && updatePhysics == false)
                    cnt = Mathf.Max(cnt, 1);

                maxcnt = Mathf.Max(maxcnt, cnt);
            }

            // upsに関係なく１フレームの最大回数は4回
            maxcnt = Mathf.Min(maxcnt, 4);

            return maxcnt;
        }

        //=========================================================================================
        internal void PreUpdateTeamData(float deltaTime, float physicsDeltaTime, float updateDeltaTime, int ups, int maxUpdateCount)
        {
            bool unscaledUpdate = manager.UpdateTime.IsUnscaledUpdate;
            float globalTimeScale = manager.GetGlobalTimeScale();

            // 固定更新では１回の更新時間をupdateDeltaTimeに設定する
            if (unscaledUpdate == false)
                deltaTime = updateDeltaTime;

            // チームデータ前処理
            var job = new PreProcessTeamDataJob()
            {
                //time = Time.time,
                deltaTime = deltaTime,
                physicsDeltaTime = physicsDeltaTime,
                updateDeltaTime = updateDeltaTime,
                globalTimeScale = globalTimeScale,
                //unscaledUpdate = unscaledUpdate,
                //ups = ups,
                maxUpdateCount = maxUpdateCount,
                unityTimeScale = Time.timeScale,
                elapsedTime = Time.time,

                teamData = teamDataList.ToJobArray(),
                teamWorldInfluenceList = teamWorldInfluenceList.ToJobArray(),
                teamWindInfoList = teamWindInfoList.ToJobArray(),

                bonePosList = Bone.bonePosList.ToJobArray(),
                boneRotList = Bone.boneRotList.ToJobArray(),
                boneSclList = Bone.boneSclList.ToJobArray(),

                windCount = Wind.windDataList.Count,
                windData = Wind.windDataList.ToJobArray(),
                //directionalWindId = Wind.DirectionalWindId,
            };
            Compute.MasterJob = job.Schedule(teamDataList.Length, 2, Compute.MasterJob);
        }

        [BurstCompile]
        struct PreProcessTeamDataJob : IJobParallelFor
        {
            //public float time;
            public float deltaTime;
            public float physicsDeltaTime;
            public float updateDeltaTime;
            public float globalTimeScale;
            //public bool unscaledUpdate;
            //public int ups;
            public int maxUpdateCount;
            public float unityTimeScale;
            public float elapsedTime;

            // team
            public NativeArray<TeamData> teamData;
            public NativeArray<WorldInfluence> teamWorldInfluenceList;
            public NativeArray<WindInfo> teamWindInfoList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> bonePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> boneRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> boneSclList;

            // wind
            public int windCount;
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerWindData.WindData> windData;
            //[Unity.Collections.ReadOnly]
            //public int directionalWindId;

            // チームデータごと
            public void Execute(int teamId)
            {
                var tdata = teamData[teamId];

                // グローバルチーム判定
                bool isGlobal = teamId == 0;

                if (tdata.IsActive() == false || (isGlobal == false && tdata.boneIndex < 0))
                {
                    tdata.updateCount = 0;
                    teamData[teamId] = tdata;
                    return;
                }

                // このチームの今回のdeltaTime
                float dtime = tdata.IsPhysicsUpdate() ? physicsDeltaTime : deltaTime;

                if (isGlobal)
                {
                    // グローバルチーム
                    // 時間更新（タイムスケール対応）
                    UpdateTime(ref tdata, false, dtime);
                }
                else
                {
                    // チームボーン情報
                    var bpos = bonePosList[tdata.boneIndex];
                    var brot = boneRotList[tdata.boneIndex];
                    var bscl = boneSclList[tdata.boneIndex];

                    // チームスケール倍率算出
                    if (tdata.initScale.x > 0.0f)
                    {
                        tdata.scaleRatio = math.length(bscl) / math.length(tdata.initScale);
                    }

                    // マイナススケール対応
                    tdata.scaleDirection = math.sign(bscl);
                    if (bscl.x < 0 || bscl.y < 0 || bscl.z < 0)
                        tdata.quaternionScale = new float4(-math.sign(bscl), 1);
                    else
                        tdata.quaternionScale = 1;

                    // ワールド移動影響
                    WorldInfluence wdata = teamWorldInfluenceList[teamId];

                    // 移動量算出
                    float3 moveVector = bpos - wdata.oldPosition;
                    quaternion moveRot = MathUtility.FromToRotation(wdata.oldRotation, brot);

                    var moveLen = math.length(moveVector); // 移動距離
                    var moveAng = math.degrees(MathUtility.Angle(wdata.oldRotation, brot)); // 移動角度

                    var maxMoveSpeed = wdata.maxMoveSpeed;          // 最大移動速度(m/s)
                    var maxRotationSpeed = wdata.maxRotationSpeed;  // 最大回転角度(deg/s)

                    // ClampRotationによるワールド移動影響補正(v1.11.2)
                    // あまり強い移動影響が掛かると制限を超えて曲がってしまうため、それを抑え安定化させる
                    // 角度制限がきついほど移動影響も低く抑えられる
                    {
                        maxMoveSpeed *= wdata.clampRotationLimit;
                        //maxRotationSpeed *= wdata.clampRotationLimit;
                        maxRotationSpeed *= math.lerp(0.5f, 1.0f, wdata.clampRotationLimit);
                    }

                    // テレポート判定
                    if (wdata.resetTeleport == 1)
                    {
                        // テレポート距離はチームスケールを乗算する
                        if (moveLen >= wdata.teleportDistance * tdata.scaleRatio || moveAng >= wdata.teleportRotation)
                        {
                            // テレポートモードにより処理を分岐
                            switch (wdata.teleportMode)
                            {
                                case ClothParams.TeleportMode.Reset:
                                    tdata.SetFlag(Flag_Reset_WorldInfluence, true);
                                    tdata.SetFlag(Flag_Reset_Position, true);
                                    break;
                                case ClothParams.TeleportMode.Keep:
                                    tdata.SetFlag(Flag_Reset_Keep, true);
                                    break;
                            }
                        }
                    }

                    // 移動影響
                    if (tdata.IsFlag(Flag_Reset_Keep))
                    {
                        // 移動影響を最大にするテレポート
                        wdata.moveOffset = moveVector;
                        wdata.moveIgnoreRatio = 0;
                    }
                    else if (moveLen > 1e-06f && dtime > 1e-06f)
                    {
                        // 最大移動
                        float speed = moveLen / dtime;
                        float ratio = math.max(speed - maxMoveSpeed, 0.0f) / speed;
                        wdata.moveIgnoreRatio = ratio;
                        wdata.moveOffset = moveVector;
                    }
                    else
                    {
                        wdata.moveOffset = 0;
                        wdata.moveIgnoreRatio = 0;
                    }

                    // 回転影響
                    if (tdata.IsFlag(Flag_Reset_Keep))
                    {
                        wdata.rotationOffset = moveRot;
                        wdata.rotationIgnoreRatio = 0;
                    }
                    if (moveAng > 1e-06f && dtime > 1e-06f)
                    {
                        // 最大回転
                        float speed = moveAng / dtime;
                        float ratio = math.max(speed - maxRotationSpeed, 0.0f) / speed;
                        wdata.rotationOffset = moveRot;
                        wdata.rotationIgnoreRatio = ratio;
                    }
                    else
                    {
                        wdata.rotationOffset = quaternion.identity;
                        wdata.rotationIgnoreRatio = 0;
                    }

                    // 速度ウエイト更新
                    if (tdata.velocityWeight < 1.0f)
                    {
                        float addw = tdata.velocityRecoverySpeed > 1e-6f ? dtime / tdata.velocityRecoverySpeed : 1.0f;
                        tdata.velocityWeight = math.saturate(tdata.velocityWeight + addw);
                    }

                    bool reset = false;
                    if (tdata.IsFlag(Flag_Reset_WorldInfluence) || tdata.IsFlag(Flag_Reset_Position))
                    {
                        // リセット
                        wdata.moveOffset = 0;
                        wdata.moveIgnoreRatio = 0;
                        wdata.rotationOffset = quaternion.identity;
                        wdata.rotationIgnoreRatio = 0;
                        wdata.oldPosition = bpos;
                        wdata.oldRotation = brot;

                        // チームタイムリセット（強制更新）
                        tdata.nowTime = updateDeltaTime;

                        // 速度ウエイト
                        tdata.velocityWeight = wdata.stabilizationTime > 1e-6f ? 0.0f : 1.0f;
                        tdata.velocityRecoverySpeed = wdata.stabilizationTime;
                        reset = true;
                    }
                    wdata.nowPosition = bpos;
                    wdata.nowRotation = brot;

                    // 書き戻し
                    teamWorldInfluenceList[teamId] = wdata;

                    // 時間更新（タイムスケール対応）
                    UpdateTime(ref tdata, reset, dtime);

                    // リセットフラグOFF
                    tdata.SetFlag(Flag_Reset_WorldInfluence, false);

                    // 風の影響を計算
                    Wind(ref tdata, bpos, teamId);
                }

                // 書き戻し
                teamData[teamId] = tdata;
            }

            /// <summary>
            /// チーム時間更新
            /// </summary>
            /// <param name="tdata"></param>
            /// <param name="reset"></param>
            void UpdateTime(ref TeamData tdata, bool reset, float dtime)
            {
                // 時間更新（タイムスケール対応）
                // チームごとに固有の時間で動作する
                tdata.updateCount = 0;
                float timeScale = tdata.timeScale * globalTimeScale;
                timeScale = tdata.IsPause() ? 0.0f : timeScale; // 一時停止
                float addTime = dtime * timeScale;

                tdata.time += addTime;
                tdata.addTime = addTime;

                // 時間ステップ
                float nowTime = tdata.nowTime + addTime;
                while (nowTime >= updateDeltaTime)
                {
                    nowTime -= updateDeltaTime;
                    tdata.updateCount++;
                }

                // リセットフラグがONならば最低１回更新とする
                if (reset)
                {
                    // ただしUnityPhysics更新は除く
                    if (tdata.IsPhysicsUpdate() == false)
                        tdata.updateCount = Mathf.Max(tdata.updateCount, 1);

                    // 最終更新時間もリセット
                    tdata.oldTime = tdata.time;
                }

                // 最大実行回数
                //tdata.updateCount = math.min(tdata.updateCount, ups / 30);
                tdata.updateCount = math.min(tdata.updateCount, maxUpdateCount);

                tdata.nowTime = nowTime;

                // スタート時間（最初の物理更新が実行される時間）
                tdata.startTime = tdata.time - nowTime - updateDeltaTime * (math.max(tdata.updateCount - 1, 0));

                // 補間再生判定
                if (timeScale < 0.99f || unityTimeScale < 0.99f)
                {
                    tdata.SetFlag(Flag_Interpolate, true);
                }
                else
                {
                    tdata.SetFlag(Flag_Interpolate, false);
                }
            }

            /// <summary>
            /// コンポーネントの風影響を事前計算する
            /// 実際の詳細はパーティクルごとに行う
            /// </summary>
            /// <param name="tdata"></param>
            /// <param name="pos"></param>
            /// <param name="teamId"></param>
            void Wind(ref TeamData tdata, float3 pos, int teamId)
            {
                var windInfo = new WindInfo();
                windInfo.windDataIndexList = -1;
                if (windCount > 0 && tdata.forceWindInfluence >= 0.01f)
                {
                    float minVolume = float.MaxValue;
                    int areaWindCount = 0;
                    int addWindCount = 0;

                    for (int i = 0; i < windData.Length; i++)
                    {
                        var wdata = windData[i];
                        if (wdata.IsActive())
                        {
                            // このチームが風エリアに入っているか判定する
                            // 加算風は最大３つまで
                            bool isAddition = wdata.IsFlag(PhysicsManagerWindData.Flag_Addition);
                            if (isAddition && addWindCount >= 3)
                                continue;

                            // 風コンポーネントの姿勢
                            var bpos = bonePosList[wdata.transformIndex];
                            var brot = boneRotList[wdata.transformIndex];

                            // ローカル座標
                            float3 v = pos - bpos;
                            float3 lpos = math.mul(math.inverse(brot), v);
                            float len = math.length(v);

                            // エリア判定
                            float3 areaSize = wdata.areaSize;
                            switch (wdata.shapeType)
                            {
                                case PhysicsManagerWindData.ShapeType.Box:
                                    var lv = math.abs(lpos);
                                    if (lv.x > areaSize.x || lv.y > areaSize.y || lv.z > areaSize.z)
                                        continue;
                                    break;
                                case PhysicsManagerWindData.ShapeType.Sphere:
                                    if (math.length(v) > wdata.areaSize.x)
                                        continue;
                                    break;
                                default:
                                    continue;
                            }

                            // エリア風の場合はボリューム判定（体積が小さいものが優先）
                            if (isAddition == false && wdata.areaVolume > minVolume)
                                continue;

                            // 風の方向
                            float3 mainDirection; // world
                            switch (wdata.directionType)
                            {
                                case PhysicsManagerWindData.DirectionType.OneDirection:
                                    mainDirection = math.mul(brot, wdata.direction);
                                    break;
                                case PhysicsManagerWindData.DirectionType.Radial:
                                    if (len < 1e-06f)
                                        continue;
                                    mainDirection = math.normalize(v);
                                    break;
                                default:
                                    continue;
                            }

                            // 距離による風力減衰
                            float windMain = wdata.main;
                            if (wdata.windType == PhysicsManagerWindData.WindType.Area && wdata.directionType == PhysicsManagerWindData.DirectionType.Radial && wdata.areaLength > 0.01f)
                            {
                                float depth = math.saturate(len / wdata.areaLength);
                                float attenuation = wdata.attenuation.Evaluate(depth);
                                windMain *= attenuation;
                                //Debug.Log($"len:{len}, areaLength:{wdata.areaLength}, depth:{depth}, atten:{attenuation}");
                            }
                            if (windMain < 0.01f)
                                continue;

                            // 実行する風として登録する
                            int index = isAddition ? 1 + addWindCount : 0;
                            windInfo.windDataIndexList[index] = i;
                            windInfo.windDirectionList[index] = mainDirection;
                            windInfo.windMainList[index] = windMain;
                            if (isAddition)
                                addWindCount++;
                            else
                            {
                                areaWindCount = 1;
                                minVolume = wdata.areaVolume;
                            }
                        }
                    }
                    windInfo.windCount = areaWindCount + addWindCount;
                }

                teamWindInfoList[teamId] = windInfo;
                tdata.SetFlag(Flag_Wind, windInfo.windCount > 0);
            }
        }

        //=========================================================================================
        internal void PostUpdateTeamData()
        {
            // チームデータ後処理
            var job = new PostProcessTeamDataJob()
            {
                fixedUpdateCount = UpdateTime.FixedUpdateCount,

                teamData = teamDataList.ToJobArray(),
                teamWorldInfluenceList = teamWorldInfluenceList.ToJobArray(),
            };
            Compute.MasterJob = job.Schedule(teamDataList.Length, 8, Compute.MasterJob);
        }

        [BurstCompile]
        struct PostProcessTeamDataJob : IJobParallelFor
        {
            public int fixedUpdateCount;

            public NativeArray<TeamData> teamData;
            public NativeArray<WorldInfluence> teamWorldInfluenceList;

            // チームデータごと
            public void Execute(int index)
            {
                var tdata = teamData[index];
                if (tdata.IsActive() == false)
                    return;

                var wdata = teamWorldInfluenceList[index];

                wdata.oldPosition = wdata.nowPosition;
                wdata.oldRotation = wdata.nowRotation;

                if (tdata.IsRunning())
                {
                    // 外部フォースをリセット
                    tdata.impactForce = 0;
                    tdata.forceMode = ForceMode.None;

                    // 最終更新時間
                    tdata.oldTime = tdata.time;

                    // 姿勢リセットフラグクリア（チーム更新時のみ）
                    tdata.SetFlag(Flag_Reset_Position, false);
                    tdata.SetFlag(Flag_Reset_Keep, false);

                    // 計算実行カウント
                    tdata.calcCount++;
                }

                // 書き戻し
                teamData[index] = tdata;
                teamWorldInfluenceList[index] = wdata;
            }
        }

        //=========================================================================================
        /*
        public void UpdateTeamUpdateCount()
        {
            // チームデータ後処理
            var job = new UpdateTeamUpdateCountJob()
            {
                teamData = Team.teamDataList.ToJobArray(),
            };
            Compute.MasterJob = job.Schedule(Team.teamDataList.Length, 8, Compute.MasterJob);
        }

        [BurstCompile]
        struct UpdateTeamUpdateCountJob : IJobParallelFor
        {
            public NativeArray<TeamData> teamData;

            // チームデータごと
            public void Execute(int index)
            {
                var tdata = teamData[index];
                if (tdata.IsActive() == false)
                    return;

                tdata.runCount++;

                // 書き戻し
                teamData[index] = tdata;
            }
        }
        */
    }
}
