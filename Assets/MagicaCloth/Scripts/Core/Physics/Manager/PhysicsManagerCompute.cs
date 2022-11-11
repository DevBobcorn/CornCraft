// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

namespace MagicaCloth
{
    /// <summary>
    /// 計算処理
    /// </summary>
    public class PhysicsManagerCompute : PhysicsManagerAccess
    {
        /// <summary>
        /// 拘束判定繰り返し回数
        /// </summary>
        //[Header("拘束全体の反復回数")]
        //[Range(1, 8)]
        //public int solverIteration = 2;
        private int solverIteration = 1;

        /// <summary>
        /// 拘束条件
        /// </summary>
        List<PhysicsManagerConstraint> constraints = new List<PhysicsManagerConstraint>();

        public ClampPositionConstraint ClampPosition { get; private set; }
        public ClampDistanceConstraint ClampDistance { get; private set; }
        //public ClampDistance2Constraint ClampDistance2 { get; private set; }
        public ClampRotationConstraint ClampRotation { get; private set; }
        public SpringConstraint Spring { get; private set; }
        public RestoreDistanceConstraint RestoreDistance { get; private set; }
        public RestoreRotationConstraint RestoreRotation { get; private set; }
        public TriangleBendConstraint TriangleBend { get; private set; }
        public ColliderCollisionConstraint Collision { get; private set; }
        public PenetrationConstraint Penetration { get; private set; }
        public ColliderExtrusionConstraint ColliderExtrusion { get; private set; }
        public TwistConstraint Twist { get; private set; }
        public CompositeRotationConstraint CompositeRotation { get; private set; }
        //public ColliderAfterCollisionConstraint AfterCollision { get; private set; }
        //public EdgeCollisionConstraint EdgeCollision { get; private set; }
        //public VolumeConstraint Volume { get; private set; }

        /// <summary>
        /// ワーカーリスト
        /// </summary>
        List<PhysicsManagerWorker> workers = new List<PhysicsManagerWorker>();
        public RenderMeshWorker RenderMeshWorker { get; private set; }
        public VirtualMeshWorker VirtualMeshWorker { get; private set; }
        public MeshParticleWorker MeshParticleWorker { get; private set; }
        public SpringMeshWorker SpringMeshWorker { get; private set; }
        public AdjustRotationWorker AdjustRotationWorker { get; private set; }
        public LineWorker LineWorker { get; private set; }
        public TriangleWorker TriangleWorker { get; private set; }
        public BaseSkinningWorker BaseSkinningWorker { get; private set; }

        /// <summary>
        /// マスタージョブハンドル
        /// すべてのジョブはこのハンドルに連結される
        /// </summary>
        JobHandle jobHandle;
        private bool runMasterJob = false;

        private int swapIndex = 0;

        /// <summary>
        /// プロファイラ用
        /// </summary>
        public CustomSampler SamplerCalcMesh { get; set; }
        public CustomSampler SamplerWriteMesh { get; set; }

        //=========================================================================================
        /// <summary>
        /// 初期設定
        /// </summary>
        public override void Create()
        {
            // 拘束の作成
            // ※この並び順が実行順番となります。

            // コリジョン
            ColliderExtrusion = new ColliderExtrusionConstraint();
            constraints.Add(ColliderExtrusion);
            Penetration = new PenetrationConstraint();
            constraints.Add(Penetration);
            Collision = new ColliderCollisionConstraint();
            constraints.Add(Collision);

            // 移動制限
            ClampDistance = new ClampDistanceConstraint();
            constraints.Add(ClampDistance);

            // 主なクロスシミュレーション
            Spring = new SpringConstraint();
            constraints.Add(Spring);
            Twist = new TwistConstraint();
            constraints.Add(Twist);
            RestoreDistance = new RestoreDistanceConstraint();
            constraints.Add(RestoreDistance);
            RestoreRotation = new RestoreRotationConstraint();
            constraints.Add(RestoreRotation);
            CompositeRotation = new CompositeRotationConstraint();
            constraints.Add(CompositeRotation);

            // 形状維持
            TriangleBend = new TriangleBendConstraint();
            constraints.Add(TriangleBend);
            //Volume = new VolumeConstraint();
            //constraints.Add(Volume);

            // 移動制限2
            ClampPosition = new ClampPositionConstraint();
            constraints.Add(ClampPosition);
            ClampRotation = new ClampRotationConstraint();
            constraints.Add(ClampRotation);

            foreach (var con in constraints)
                con.Init(manager);

            // ワーカーの作成
            // ※この並び順は変更してはいけません。
            RenderMeshWorker = new RenderMeshWorker();
            workers.Add(RenderMeshWorker);
            VirtualMeshWorker = new VirtualMeshWorker();
            workers.Add(VirtualMeshWorker);
            MeshParticleWorker = new MeshParticleWorker();
            workers.Add(MeshParticleWorker);
            SpringMeshWorker = new SpringMeshWorker();
            workers.Add(SpringMeshWorker);
            AdjustRotationWorker = new AdjustRotationWorker();
            workers.Add(AdjustRotationWorker);
            LineWorker = new LineWorker();
            workers.Add(LineWorker);
            TriangleWorker = new TriangleWorker();
            workers.Add(TriangleWorker);
            BaseSkinningWorker = new BaseSkinningWorker();
            workers.Add(BaseSkinningWorker);
            foreach (var worker in workers)
                worker.Init(manager);


            // プロファイラ用
            SamplerCalcMesh = CustomSampler.Create("CalcMesh");
            SamplerWriteMesh = CustomSampler.Create("WriteMesh");
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public override void Dispose()
        {
            if (constraints != null)
            {
                foreach (var con in constraints)
                    con.Release();
            }
            if (workers != null)
            {
                foreach (var worker in workers)
                    worker.Release();
            }
        }

        /// <summary>
        /// 各コンストレイント／ワーカーから指定グループのデータを削除する
        /// </summary>
        /// <param name="teamId"></param>
        public void RemoveTeam(int teamId)
        {
            if (MagicaPhysicsManager.Instance.Team.IsValidData(teamId) == false)
                return;

            if (constraints != null)
            {
                foreach (var con in constraints)
                    con.RemoveTeam(teamId);
            }
            if (workers != null)
            {
                foreach (var worker in workers)
                    worker.RemoveGroup(teamId);
            }
        }

        //=========================================================================================
        /// <summary>
        /// ボーン姿勢を元の位置に復元する
        /// </summary>
        internal void UpdateRestoreBone(PhysicsTeam.TeamUpdateMode updateMode)
        {
            // 活動チームが１つ以上ある場合のみ更新
            if (Team.ActiveTeamCount > 0)
            {
                // トランスフォーム姿勢のリセット
                Bone.ResetBoneFromTransform(updateMode == PhysicsTeam.TeamUpdateMode.UnityPhysics);
            }
        }

        /// <summary>
        /// ボーン姿勢を読み込む
        /// </summary>
        internal void UpdateReadBone()
        {
            // 活動チームが１つ以上ある場合のみ更新
            if (Team.ActiveTeamCount > 0)
            {
                // トランスフォーム姿勢の読み込み
                Bone.ReadBoneFromTransform();
            }
        }

        /// <summary>
        /// メインスレッドで行うチームデータ更新処理
        /// </summary>
        internal void UpdateTeamAlways()
        {
            // 常に実行するチームデータ更新
            Team.PreUpdateTeamAlways();
        }

        /// <summary>
        /// クロスシミュレーション計算開始
        /// </summary>
        /// <param name="update"></param>
        internal void UpdateStartSimulation(UpdateTimeManager update)
        {
            // マネージャ非アクティブ時にはシミュレーション計算を完全に停止させる
            if (MagicaPhysicsManager.Instance.IsActive == false)
                return;

            // 時間
            float deltaTime = update.DeltaTime;
            float physicsDeltaTime = update.PhysicsDeltaTime;
            float updatePower = update.UpdatePower;
            float updateDeltaTime = update.UpdateIntervalTime;
            int ups = update.UpdatePerSecond;

            // 活動チームが１つ以上ある場合のみ更新
            if (Team.ActiveTeamCount > 0)
            {
                // 今回フレームの更新回数
                int updateCount = Team.CalcMaxUpdateCount(ups, deltaTime, physicsDeltaTime, updateDeltaTime);
                //Debug.Log($"updateCount:{updateCount} dtime:{deltaTime} pdtime:{physicsDeltaTime} fixedCount:{update.FixedUpdateCount}");

                // 風更新
                //Wind.UpdateWind();

                // チームデータ更新、更新回数確定、ワールド移動影響、テレポート
                Team.PreUpdateTeamData(deltaTime, physicsDeltaTime, updateDeltaTime, ups, updateCount);

                // ワーカー処理
                WarmupWorker();

                // ボーン姿勢をパーティクルにコピーする
                Particle.UpdateBoneToParticle();

                // 物理更新前ワーカー処理
                //MasterJob = RenderMeshWorker.PreUpdate(MasterJob); // 何もなし
                MasterJob = VirtualMeshWorker.PreUpdate(MasterJob); // 仮想メッシュをスキニングしワールド姿勢を求める
                MasterJob = MeshParticleWorker.PreUpdate(MasterJob); // 仮想メッシュ頂点姿勢を連動パーティクルにコピーする
                //MasterJob = SpringMeshWorker.PreUpdate(MasterJob); // 何もなし
                //MasterJob = AdjustRotationWorker.PreUpdate(MasterJob); // 何もなし
                //MasterJob = LineWorker.PreUpdate(MasterJob); // 何もなし
                MasterJob = BaseSkinningWorker.PreUpdate(MasterJob); // ベーススキニングによりbasePos/baseRotをスキニング

                // パーティクルのリセット判定
                Particle.UpdateResetParticle();

                // 物理更新
                for (int i = 0, cnt = updateCount; i < cnt; i++)
                {
                    UpdatePhysics(updateCount, i, updatePower, updateDeltaTime);
                }

                // 物理演算後処理
                PostUpdatePhysics(updateDeltaTime);

                // 物理更新後ワーカー処理
                MasterJob = TriangleWorker.PostUpdate(MasterJob); // トライアングル回転調整
                MasterJob = LineWorker.PostUpdate(MasterJob); // ラインの回転調整
                MasterJob = AdjustRotationWorker.PostUpdate(MasterJob); // パーティクル回転調整(Adjust Rotation)
                Particle.UpdateParticleToBone(); // パーティクル姿勢をボーン姿勢に書き戻す（ここに挟まないと駄目）
                MasterJob = SpringMeshWorker.PostUpdate(MasterJob); // メッシュスプリング
                MasterJob = MeshParticleWorker.PostUpdate(MasterJob); // パーティクル姿勢を仮想メッシュに書き出す
                MasterJob = VirtualMeshWorker.PostUpdate(MasterJob); // 仮想メッシュ座標書き込み（仮想メッシュトライアングル法線計算）
                MasterJob = RenderMeshWorker.PostUpdate(MasterJob); // レンダーメッシュ座標書き込み（仮想メッシュからレンダーメッシュ座標計算）

                // 書き込みボーン姿勢をローカル姿勢に変換する
                Bone.ConvertWorldToLocal();

                // チームデータ後処理
                Team.PostUpdateTeamData();

            }
        }

        /// <summary>
        /// クロスシミュレーション完了待ち
        /// </summary>
        internal void UpdateCompleteSimulation()
        {
            // マスタージョブ完了待機
            CompleteJob();
            runMasterJob = true;

#if UNITY_2021_2_OR_NEWER
            // 高速書き込みバッファの作業終了
            Mesh.FinishVertexBuffer();
#endif

            //Debug.Log($"runMasterJob = true! F:{Time.frameCount}");
        }

        /// <summary>
        /// ボーン姿勢をトランスフォームに書き込む
        /// </summary>
        internal void UpdateWriteBone()
        {
            // ボーン姿勢をトランスフォームに書き出す
            Bone.WriteBoneToTransform(manager.IsDelay ? 1 : 0);
        }

        /// <summary>
        /// メッシュ書き込みの事前判定
        /// </summary>
        internal void MeshCalculation()
        {
            // プロファイラ計測開始
            SamplerCalcMesh.Begin();

            Mesh.ClearWritingList();

            if (Mesh.VirtualMeshCount > 0 && runMasterJob)
            {
                Mesh.MeshCalculation(manager.IsDelay ? 1 : 0);
            }

            // プロファイラ計測終了
            SamplerCalcMesh.End();
        }

        /// <summary>
        /// メッシュ姿勢をメッシュに書き込む
        /// </summary>
        internal void NormalWritingMesh()
        {
            // プロファイラ計測開始
            SamplerWriteMesh.Begin();

            // メッシュへの頂点書き戻し
            if (Mesh.VirtualMeshCount > 0 && runMasterJob)
            {
                Mesh.NormalWriting(manager.IsDelay ? 1 : 0);
#if UNITY_2021_2_OR_NEWER
                Mesh.FasterWriting(manager.IsDelay ? 1 : 0);
#endif
            }

            // プロファイラ計測終了
            SamplerWriteMesh.End();
        }

        /// <summary>
        /// 遅延実行時のボーン読み込みと前回のボーン結果の書き込み
        /// </summary>
        internal void UpdateReadWriteBone()
        {
            // 活動チームが１つ以上ある場合のみ更新
            if (Team.ActiveTeamCount > 0)
            {
                // トランスフォーム姿勢の読み込み
                Bone.ReadBoneFromTransform();

                if (runMasterJob)
                {
                    // ボーン姿勢をトランスフォームに書き出す
                    Bone.WriteBoneToTransform(manager.IsDelay ? 1 : 0);
                }
            }
        }

        /// <summary>
        /// 遅延実行時のみボーンの計算結果を書き込みバッファにコピーする
        /// </summary>
        internal void UpdateSyncBuffer()
        {
            Bone.writeBoneIndexList.SyncBuffer();
            Bone.writeBonePosList.SyncBuffer();
            Bone.writeBoneRotList.SyncBuffer();
            Bone.boneFlagList.SyncBuffer();

            InitJob();
            Bone.CopyBoneBuffer();
            CompleteJob();
        }

        /// <summary>
        /// 遅延実行時のみメッシュの計算結果をスワップする
        /// </summary>
        internal void UpdateSwapBuffer()
        {
            Mesh.renderPosList.SwapBuffer();
            Mesh.renderNormalList.SwapBuffer();
            Mesh.renderTangentList.SwapBuffer();
            Mesh.renderBoneWeightList.SwapBuffer();
#if UNITY_2021_2_OR_NEWER
            // 高速書き込み用コンピュートバッファをスワップ
            Mesh.renderPosBuffer.Swap();
            Mesh.renderNormalBuffer.Swap();
#endif

            swapIndex ^= 1;

            // 遅延実行計算済みフラグを立てる
            Mesh.SetDelayedCalculatedFlag();
        }

        //=========================================================================================
        public JobHandle MasterJob
        {
            get
            {
                return jobHandle;
            }
            set
            {
                jobHandle = value;
            }
        }

        /// <summary>
        /// マスタージョブハンドル初期化
        /// </summary>
        public void InitJob()
        {
            jobHandle = default(JobHandle);
        }

        public void ScheduleJob()
        {
            JobHandle.ScheduleBatchedJobs();
        }

        /// <summary>
        /// マスタージョブハンドル完了待機
        /// </summary>
        public void CompleteJob()
        {
            jobHandle.Complete();
            jobHandle = default(JobHandle);
        }

        /// <summary>
        /// 遅延実行時のダブルバッファのフロントインデックス
        /// </summary>
        public int SwapIndex
        {
            get
            {
                return swapIndex;
            }
        }

        //=========================================================================================
        /// <summary>
        /// 物理エンジン更新ループ処理
        /// これは１フレームにステップ回数分呼び出される
        /// 場合によっては１回も呼ばれないフレームも発生するので注意！
        /// </summary>
        /// <param name="updateCount"></param>
        /// <param name="runCount"></param>
        /// <param name="dtime"></param>
        void UpdatePhysics(int updateCount, int runCount, float updatePower, float updateDeltaTime)
        {
            if (Particle.Count == 0)
                return;

            // フォース影響＋速度更新
            var job1 = new ForceAndVelocityJob()
            {
                updateDeltaTime = updateDeltaTime,
                updatePower = updatePower,
                runCount = runCount,

                teamDataList = Team.teamDataList.ToJobArray(),
                teamMassList = Team.teamMassList.ToJobArray(),
                teamGravityList = Team.teamGravityList.ToJobArray(),
                teamDragList = Team.teamDragList.ToJobArray(),
                teamDepthInfluenceList = Team.teamDepthInfluenceList.ToJobArray(),
                teamWindInfoList = Team.teamWindInfoList.ToJobArray(),
                //teamMaxVelocityList = Team.teamMaxVelocityList.ToJobArray(),
                //teamDirectionalDampingList = Team.teamDirectionalDampingList.ToJobArray(),

                flagList = Particle.flagList.ToJobArray(),
                teamIdList = Particle.teamIdList.ToJobArray(),
                depthList = Particle.depthList.ToJobArray(),

                snapBasePosList = Particle.snapBasePosList.ToJobArray(),
                snapBaseRotList = Particle.snapBaseRotList.ToJobArray(),
                basePosList = Particle.basePosList.ToJobArray(),
                baseRotList = Particle.baseRotList.ToJobArray(),
                oldBasePosList = Particle.oldBasePosList.ToJobArray(),
                oldBaseRotList = Particle.oldBaseRotList.ToJobArray(),

                nextPosList = Particle.InNextPosList.ToJobArray(),
                nextRotList = Particle.InNextRotList.ToJobArray(),
                oldPosList = Particle.oldPosList.ToJobArray(),
                oldRotList = Particle.oldRotList.ToJobArray(),
                frictionList = Particle.frictionList.ToJobArray(),
                //oldSlowPosList = Particle.oldSlowPosList.ToJobArray(),

                posList = Particle.posList.ToJobArray(),
                rotList = Particle.rotList.ToJobArray(),
                velocityList = Particle.velocityList.ToJobArray(),

                //boneRotList = Bone.boneRotList.ToJobArray(),

                // wind
                windDataList = Wind.windDataList.ToJobArray(),

                // bone
                bonePosList = Bone.bonePosList.ToJobArray(),
                boneRotList = Bone.boneRotList.ToJobArray(),
            };
            jobHandle = job1.Schedule(Particle.Length, 64, jobHandle);

            // 拘束条件解決
            if (constraints != null)
            {
                // 拘束解決反復数分ループ
                for (int i = 0; i < solverIteration; i++)
                {
                    foreach (var con in constraints)
                    {
                        if (con != null /*&& con.enabled*/)
                        {
                            // 拘束ごとの反復回数
                            for (int j = 0; j < con.GetIterationCount(); j++)
                            {
                                jobHandle = con.SolverConstraint(runCount, updateDeltaTime, updatePower, j, jobHandle);
                            }
                        }
                    }
                }
            }

            // 座標確定
            var job2 = new FixPositionJob()
            {
                updatePower = updatePower,
                updateDeltaTime = updateDeltaTime,
                runCount = runCount,

                teamDataList = Team.teamDataList.ToJobArray(),
                teamMaxVelocityList = Team.teamMaxVelocityList.ToJobArray(),

                flagList = Particle.flagList.ToJobArray(),
                teamIdList = Particle.teamIdList.ToJobArray(),
                depthList = Particle.depthList.ToJobArray(),

                nextPosList = Particle.InNextPosList.ToJobArray(),
                nextRotList = Particle.InNextRotList.ToJobArray(),

                //basePosList = Particle.basePosList.ToJobArray(),
                //baseRotList = Particle.baseRotList.ToJobArray(),

                oldPosList = Particle.oldPosList.ToJobArray(),
                oldRotList = Particle.oldRotList.ToJobArray(),

                frictionList = Particle.frictionList.ToJobArray(),

                velocityList = Particle.velocityList.ToJobArray(),
                rotList = Particle.rotList.ToJobArray(),
                posList = Particle.posList.ToJobArray(),
                localPosList = Particle.localPosList.ToJobArray(),

                collisionNormalList = Particle.collisionNormalList.ToJobArray(),
                staticFrictionList = Particle.staticFrictionList.ToJobArray(),
            };
            jobHandle = job2.Schedule(Particle.Length, 64, jobHandle);
        }

        [BurstCompile]
        struct ForceAndVelocityJob : IJobParallelFor
        {
            public float updateDeltaTime;
            public float updatePower;
            public int runCount;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<CurveParam> teamMassList;
            [Unity.Collections.ReadOnly]
            public NativeArray<CurveParam> teamGravityList;
            [Unity.Collections.ReadOnly]
            public NativeArray<CurveParam> teamDragList;
            [Unity.Collections.ReadOnly]
            public NativeArray<CurveParam> teamDepthInfluenceList;
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.WindInfo> teamWindInfoList;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<CurveParam> teamMaxVelocityList;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<CurveParam> teamDirectionalDampingList;

            // particle
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> teamIdList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> snapBasePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> snapBaseRotList;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> basePosList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> baseRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldBasePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> oldBaseRotList;

            public NativeArray<float3> nextPosList;
            public NativeArray<quaternion> nextRotList;
            public NativeArray<float> frictionList;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> posList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> rotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> oldRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> velocityList;

            // wind
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerWindData.WindData> windDataList;

            // bone
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> bonePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> boneRotList;

            // パーティクルごと
            public void Execute(int index)
            {
                var flag = flagList[index];
                if (flag.IsValid() == false)
                    return;

                // チームデータ
                int teamId = teamIdList[index];
                var teamData = teamDataList[teamId];

                // ここからは更新がある場合のみ実行（グローバルチームは除く）
                if (teamId != 0 && teamData.IsUpdate(runCount) == false)
                    return;

                var oldpos = oldPosList[index];
                var oldrot = oldRotList[index];
                float3 nextPos = oldpos;
                quaternion nextRot = oldrot;
                var friction = frictionList[index];

                // 基準姿勢のステップ補間(v1.11.1)
                var oldBasePos = oldBasePosList[index];
                var oldBaseRot = oldBaseRotList[index];
                var snapBasePos = snapBasePosList[index];
                var snapBaseRot = snapBaseRotList[index];
                float stime = teamData.startTime + updateDeltaTime * runCount;
                float oldtime = teamData.startTime - updateDeltaTime;
                float interval = teamData.time - oldtime;
                float step = interval >= 1e-06f ? math.saturate((stime - oldtime) / interval) : 0.0f;
                float3 basePos = math.lerp(oldBasePos, snapBasePos, step);
                quaternion baseRot = math.slerp(oldBaseRot, snapBaseRot, step);
                baseRot = math.normalize(baseRot); // 必要
                basePosList[index] = basePos;
                baseRotList[index] = baseRot;


                if (flag.IsFixed())
                {
                    // キネマティックパーティクル
                    nextPos = basePos;
                    nextRot = baseRot;

                    // nextPos/nextRotが１ステップ前の姿勢
                    var oldNextPos = nextPosList[index];
                    var oldNextRot = nextRotList[index];

                    // 前回の姿勢をoldpos/rotとしてposList/rotListに格納する
                    if (flag.IsCollider() && teamId == 0)
                    {
                        // グローバルコライダー
                        // 移動量と回転量に制限をかける(1.7.5)
                        // 制限をかけないと高速移動／回転時に遠く離れたパーティクルが押し出されてしまう問題が発生する。
                        oldpos = MathUtility.ClampDistance(nextPos, oldNextPos, Define.Compute.GlobalColliderMaxMoveDistance);
                        oldrot = MathUtility.ClampAngle(nextRot, oldNextRot, math.radians(Define.Compute.GlobalColliderMaxRotationAngle));
                    }
                    else
                    {
                        oldpos = oldNextPos;
                        oldrot = oldNextRot;
                    }

#if false
                    // nextPos/nextRotが１ステップ前の姿勢
                    var oldNextPos = nextPosList[index];
                    var oldNextRot = nextRotList[index];

                    // oldpos/rotが前フレームの最終計算姿勢
                    // oldpos/rot から BasePos/Rot に step で補間して現在姿勢とする
                    float stime = teamData.startTime + updateDeltaTime * runCount;
                    float oldtime = teamData.startTime - updateDeltaTime;
                    float interval = teamData.time - oldtime;
                    float step = interval >= 1e-06f ? math.saturate((stime - oldtime) / interval) : 0.0f;

                    nextPos = math.lerp(oldpos, basePosList[index], step);
                    nextRot = math.slerp(oldrot, baseRotList[index], step);
                    nextRot = math.normalize(nextRot);

                    // 前回の姿勢をoldpos/rotとしてposList/rotListに格納する
                    if (flag.IsCollider() && teamId == 0)
                    {
                        // グローバルコライダー
                        // 移動量と回転量に制限をかける(1.7.5)
                        // 制限をかけないと高速移動／回転時に遠く離れたパーティクルが押し出されてしまう問題が発生する。
                        oldpos = MathUtility.ClampDistance(nextPos, oldNextPos, Define.Compute.GlobalColliderMaxMoveDistance);
                        oldrot = MathUtility.ClampAngle(nextRot, oldNextRot, math.radians(Define.Compute.GlobalColliderMaxRotationAngle));
                    }
                    else
                    {
                        oldpos = oldNextPos;
                        oldrot = oldNextRot;
                    }
#endif

                    // debug
                    //nextPos = basePosList[index];
                    //nextRot = baseRotList[index];
                }
                else
                {
                    // 動的パーティクル
                    var depth = depthList[index];
                    //var maxVelocity = teamMaxVelocityList[teamId].Evaluate(depth);
                    var drag = teamDragList[teamId].Evaluate(depth);
                    var gravity = teamGravityList[teamId].Evaluate(depth);
                    var gravityDirection = teamData.gravityDirection;
                    var mass = teamMassList[teamId].Evaluate(depth);
                    var depthInfluence = teamDepthInfluenceList[teamId].Evaluate(depth);
                    var velocity = velocityList[index];

                    // チームスケール倍率
                    //maxVelocity *= teamData.scaleRatio;

                    // massは主に伸縮を中心に調整されるので、フォース適用時は少し調整する
                    //mass = (mass - 1.0f) * teamData.forceMassInfluence + 1.0f;

                    // 安定化用の速度ウエイト
                    velocity *= teamData.velocityWeight;

                    // 最大速度
                    //velocity = MathUtility.ClampVector(velocity, 0.0f, maxVelocity);

                    // 空気抵抗(90ups基準)
                    // 重力に影響させたくないので先に計算する（※通常はforce適用後に行うのが一般的）
                    velocity *= math.pow(1.0f - drag, updatePower);

                    // フォース
                    // フォースは空気抵抗を無視して加算する
                    float3 force = 0;

                    // 重力（質量に関係なく一定）
                    // (最後に質量で割るためここでは質量をかける）
                    force += gravityDirection * (gravity * mass);

                    // 外部フォース
                    {
                        float3 exForce = 0;
                        switch (teamData.forceMode)
                        {
                            case PhysicsManagerTeamData.ForceMode.VelocityAdd:
                                exForce += teamData.impactForce;
                                break;
                            case PhysicsManagerTeamData.ForceMode.VelocityAddWithoutMass:
                                exForce += teamData.impactForce * mass;
                                break;
                            case PhysicsManagerTeamData.ForceMode.VelocityChange:
                                exForce += teamData.impactForce;
                                velocity = 0;
                                break;
                            case PhysicsManagerTeamData.ForceMode.VelocityChangeWithoutMass:
                                exForce += teamData.impactForce * mass;
                                velocity = 0;
                                break;
                        }

                        // 外力
                        exForce += teamData.externalForce;

                        // 風（重量に関係なく一定）
                        if (teamData.IsFlag(PhysicsManagerTeamData.Flag_Wind))
                            exForce += Wind(teamId, teamData, snapBasePos) * mass;

                        // 外力深さ影響率
                        exForce *= depthInfluence;

                        force += exForce;
                    }

                    // 外力チームスケール倍率
                    force *= teamData.scaleRatio;

                    // 速度計算(質量で割る)
                    velocity += (force / mass) * updateDeltaTime;

                    // 速度を理想位置に反映させる
                    nextPos = oldpos + velocity * updateDeltaTime;
                }

                // 予定座標更新 ==============================================================
                // 摩擦減衰
                friction = friction * Define.Compute.FrictionDampingRate;
                frictionList[index] = friction;
                //frictionList[index] = 0;

                // 移動前の姿勢
                posList[index] = oldpos;
                rotList[index] = oldrot;

                // 予測位置
                nextPosList[index] = nextPos;
                nextRotList[index] = nextRot;
            }

            /// <summary>
            /// 風の計算
            /// </summary>
            /// <param name="teamId"></param>
            /// <param name="teamData"></param>
            /// <param name="pos"></param>
            /// <returns></returns>
            float3 Wind(int teamId, in PhysicsManagerTeamData.TeamData teamData, in float3 pos)
            {
                var windInfo = teamWindInfoList[teamId];

                // ノイズ起点
                // ここをずらすと他のパーティクルと非同期になっていく
                float sync = math.lerp(3.0f, 0.1f, teamData.forceWindSynchronization);
                var noiseBasePos = new float2(pos.x, pos.z) * sync;

                float3 externalForce = 0;
                for (int i = 0; i < 4; i++)
                {
                    int windId = windInfo.windDataIndexList[i];
                    if (windId < 0)
                        continue;

                    var windData = windDataList[windId];
                    float3 windForce = PhysicsManagerWindData.CalcWindForce(
                        teamData.time,
                        noiseBasePos,
                        windInfo.windDirectionList[i],
                        windInfo.windMainList[i],
                        windData.turbulence,
                        windData.frequency,
                        teamData.forceWindRandomScale
                        );

                    externalForce += windForce;
                }

                // チームの風の影響率
                externalForce *= teamData.forceWindInfluence;

                return externalForce;
            }
        }

        [BurstCompile]
        struct FixPositionJob : IJobParallelFor
        {
            public float updatePower;
            public float updateDeltaTime;
            public int runCount;

            // チーム
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<CurveParam> teamMaxVelocityList;

            // パーティクルごと
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> teamIdList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> nextRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionList;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<float3> basePosList;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<quaternion> baseRotList;

            public NativeArray<float3> velocityList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> rotList;

            public NativeArray<float3> oldPosList;
            public NativeArray<quaternion> oldRotList;

            public NativeArray<float3> posList;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> localPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> collisionNormalList;
            public NativeArray<float> staticFrictionList;

            // パーティクルごと
            public void Execute(int index)
            {
                var flag = flagList[index];
                if (flag.IsValid() == false)
                    return;

                // チームデータ
                int teamId = teamIdList[index];
                var teamData = teamDataList[teamId];

                // ここからは更新がある場合のみ実行
                if (teamData.IsUpdate(runCount) == false)
                    return;

                // 速度更新(m/s)
                if (flag.IsFixed() == false)
                {
                    // 移動パーティクルのみ
                    var nextPos = nextPosList[index];
                    var nextRot = nextRotList[index];
                    nextRot = math.normalize(nextRot); // 回転蓄積で精度が落ちていくので正規化しておく

                    float3 velocity = 0;

                    // posListには移動影響を考慮した最終座標が入っている
                    var pos = posList[index];
                    var oldpos = oldPosList[index];

                    // コライダー接触情報
                    float friction = frictionList[index];
                    var cn = collisionNormalList[index];
                    bool isCollision = math.lengthsq(cn) > Define.Compute.Epsilon; // 接触の有無

#if true
                    // 静止摩擦
                    float staticFriction = staticFrictionList[index];
                    if (isCollision && friction > 0.0f)
                    {
                        // 接線方向の移動速度から計算する
                        var v = nextPos - oldpos;
                        v = v - MathUtility.Project(v, cn);
                        float tangentVelocity = math.length(v) / updateDeltaTime; // 接線方向の移動速度
                        float stopVelocity = teamData.staticFriction * teamData.scaleRatio; // 静止速度

                        if (tangentVelocity < stopVelocity)
                        {
                            staticFriction = math.saturate(staticFriction + 0.02f * updatePower); // 係数増加
                        }
                        else
                        {
                            // 接線速度に応じて係数を減少
                            var vel = tangentVelocity - stopVelocity;
                            var value = math.max(vel / 0.2f, 0.05f) * updatePower;
                            staticFriction = math.saturate(staticFriction - value);
                        }

                        // 現在の静止摩擦係数を使い接線方向の移動にブレーキをかける
                        v *= staticFriction;
                        nextPos -= v;
                        pos -= v;
                    }
                    else
                    {
                        staticFriction = math.saturate(staticFriction - 0.05f * updatePower); // 係数減少
                    }
                    staticFrictionList[index] = staticFriction;
#endif

                    // 速度更新(m/s)
                    velocity = (nextPos - pos) / updateDeltaTime;
                    velocity *= teamData.velocityWeight; // 安定化用の速度ウエイト

#if true
                    // 動摩擦による速度減衰（衝突面との角度が大きいほど減衰が強くなる）
                    if (friction > Define.Compute.Epsilon && isCollision && math.lengthsq(velocity) >= Define.Compute.Epsilon)
                    {
                        var dot = math.dot(cn, math.normalize(velocity));
                        dot = 0.5f + 0.5f * dot; // 1.0(front) - 0.5(side) - 0.0(back)
                        dot *= dot; // サイドを強めに
                        dot = 1.0f - dot; // 0.0(front) - 0.75(side) - 1.0(back)
                        velocity -= velocity * (dot * math.saturate(friction * teamData.dynamicFriction * 1.5f)); // 以前と同程度になるように補正
                    }
#else
                    // 摩擦による速度減衰(旧)
                    friction *= teamData.friction; // チームごとの摩擦係数
                    velocity *= math.pow(1.0f - math.saturate(friction), updatePower);
#endif

                    // 最大速度
                    var depth = depthList[index];
                    var maxVelocity = teamMaxVelocityList[teamId].Evaluate(depth);
                    maxVelocity *= teamData.scaleRatio; // チームスケール
                    velocity = MathUtility.ClampVector(velocity, 0.0f, maxVelocity);

                    // 実際の移動速度(localPosに格納)
                    var realVelocity = (nextPos - oldpos) / updateDeltaTime;
                    realVelocity = MathUtility.ClampVector(realVelocity, 0.0f, maxVelocity); // 最大速度は考慮する
                    localPosList[index] = realVelocity;

                    // 書き戻し
                    velocityList[index] = velocity;

                    oldPosList[index] = nextPos;
                    oldRotList[index] = nextRot;

                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// 物理演算後処理
        /// </summary>
        /// <param name="updateDeltaTime"></param>
        void PostUpdatePhysics(float updateDeltaTime)
        {
            if (Particle.Count == 0)
                return;

            var job = new PostUpdatePhysicsJob()
            {
                updateDeltaTime = updateDeltaTime,

                teamDataList = Team.teamDataList.ToJobArray(),

                flagList = Particle.flagList.ToJobArray(),
                teamIdList = Particle.teamIdList.ToJobArray(),

                snapBasePosList = Particle.snapBasePosList.ToJobArray(),
                snapBaseRotList = Particle.snapBaseRotList.ToJobArray(),
                basePosList = Particle.basePosList.ToJobArray(),
                baseRotList = Particle.baseRotList.ToJobArray(),
                oldBasePosList = Particle.oldBasePosList.ToJobArray(),
                oldBaseRotList = Particle.oldBaseRotList.ToJobArray(),

                oldPosList = Particle.oldPosList.ToJobArray(),
                oldRotList = Particle.oldRotList.ToJobArray(),

                velocityList = Particle.velocityList.ToJobArray(),
                localPosList = Particle.localPosList.ToJobArray(),

                posList = Particle.posList.ToJobArray(),
                rotList = Particle.rotList.ToJobArray(),
                nextPosList = Particle.InNextPosList.ToJobArray(),
                nextRotList = Particle.InNextRotList.ToJobArray(),

                oldSlowPosList = Particle.oldSlowPosList.ToJobArray(),
            };
            jobHandle = job.Schedule(Particle.Length, 64, jobHandle);
        }

        [BurstCompile]
        struct PostUpdatePhysicsJob : IJobParallelFor
        {
            public float updateDeltaTime;

            // チーム
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;

            // パーティクルごと
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> teamIdList;

            // パーティクルごと
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> snapBasePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> snapBaseRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> baseRotList;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> oldBasePosList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> oldBaseRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> velocityList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPosList;

            public NativeArray<float3> oldPosList;
            public NativeArray<quaternion> oldRotList;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> posList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> rotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> nextRotList;

            public NativeArray<float3> oldSlowPosList;

            // パーティクルごと
            public void Execute(int index)
            {
                var flag = flagList[index];
                if (flag.IsValid() == false)
                    return;

                // チームデータ
                int teamId = teamIdList[index];
                var teamData = teamDataList[teamId];

                float3 viewPos = 0;
                quaternion viewRot = quaternion.identity;

                //var basePos = basePosList[index];
                //var baseRot = baseRotList[index];
                var snapBasePos = snapBasePosList[index];
                var snapBaseRot = snapBaseRotList[index];

                if (flag.IsFixed() == false)
                {
                    // 未来予測
                    // １フレーム前の表示位置と将来の予測位置を、現在のフレーム位置で線形補間する
                    //var velocity = velocityList[index]; // 従来
                    //var velocity = posList[index]; // 実際の速度（どうもこっちだとカクつき？があるぞ）
                    var velocity = localPosList[index]; // 実際の速度

                    var futurePos = oldPosList[index] + velocity * updateDeltaTime;
                    var oldViewPos = oldSlowPosList[index];
                    float addTime = teamData.addTime;
                    float oldTime = teamData.time - addTime;
                    float futureTime = teamData.time + (updateDeltaTime - teamData.nowTime);
                    float interval = futureTime - oldTime;
                    //Debug.Log($"addTime:{teamData.addTime} interval:{interval}");
                    if (addTime > 1e-06f && interval > 1e-06f)
                    {
                        float ratio = teamData.addTime / interval;
                        viewPos = math.lerp(oldViewPos, futurePos, ratio);
                    }
                    else
                    {
                        viewPos = oldViewPos;
                    }
                    viewRot = oldRotList[index];
                    viewRot = math.normalize(viewRot); // 回転蓄積で精度が落ちていくので正規化しておく
#if false
                    // 未来予測を切る
                    futurePos = oldPosList[index];
                    viewPos = futurePos;
#endif

                    oldSlowPosList[index] = viewPos;
                }
                else
                {
                    // 固定パーティクルの表示位置は常にベース位置
                    //viewPos = basePos;
                    //viewRot = baseRot;
                    viewPos = snapBasePos;
                    viewRot = snapBaseRot;

                    // 固定パーティクルは今回のbasePosを記録する（更新時のみ）
                    if (teamData.IsRunning())
                    {
                        // 最終計算位置を格納する
                        oldPosList[index] = nextPosList[index];
                        oldRotList[index] = nextRotList[index];
                    }
                }

                // ブレンド
                if (teamData.blendRatio < 0.99f)
                {
                    //viewPos = math.lerp(basePos, viewPos, teamData.blendRatio);
                    //viewRot = math.slerp(baseRot, viewRot, teamData.blendRatio);
                    viewPos = math.lerp(snapBasePos, viewPos, teamData.blendRatio);
                    viewRot = math.slerp(snapBaseRot, viewRot, teamData.blendRatio);
                    viewRot = math.normalize(viewRot); // 回転蓄積で精度が落ちていくので正規化しておく
                }

                // test
                //viewPos = snapBasePos;
                //viewRot = snapBaseRot;


                // 表示位置
                posList[index] = viewPos;
                rotList[index] = viewRot;

                // １つ前の基準位置を記録
                if (teamData.IsRunning())
                {
                    oldBasePosList[index] = basePosList[index];
                    oldBaseRotList[index] = baseRotList[index];
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// ワーカーウォームアップ処理実行
        /// </summary>
        void WarmupWorker()
        {
            if (workers == null || workers.Count == 0)
                return;

            for (int i = 0; i < workers.Count; i++)
            {
                var worker = workers[i];
                worker.Warmup();
            }
        }
    }
}
