// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public class SimulationManager : IManager, IValid
    {
        /// <summary>
        /// チームID
        /// </summary>
        public ExNativeArray<short> teamIdArray;

        /// <summary>
        /// 現在のシミュレーション座標
        /// </summary>
        public ExNativeArray<float3> nextPosArray;

        /// <summary>
        /// １つ前のシミュレーション座標
        /// </summary>
        public ExNativeArray<float3> oldPosArray;

        /// <summary>
        /// １つ前のシミュレーション回転(todo:現在未使用）
        /// </summary>
        public ExNativeArray<quaternion> oldRotArray;

        /// <summary>
        /// 現在のアニメーション姿勢座標
        /// カスタムスキニングの結果も反映されている
        /// </summary>
        public ExNativeArray<float3> basePosArray;

        /// <summary>
        /// 現在のアニメーション姿勢回転
        /// カスタムスキニングの結果も反映されている
        /// </summary>
        public ExNativeArray<quaternion> baseRotArray;

        /// <summary>
        /// １つ前の原点座標
        /// </summary>
        public ExNativeArray<float3> oldPositionArray;

        /// <summary>
        /// １つ前の原点回転
        /// </summary>
        public ExNativeArray<quaternion> oldRotationArray;

        /// <summary>
        /// 速度計算用座標
        /// </summary>
        public ExNativeArray<float3> velocityPosArray;

        /// <summary>
        /// 表示座標
        /// </summary>
        public ExNativeArray<float3> dispPosArray;

        /// <summary>
        /// 速度
        /// </summary>
        public ExNativeArray<float3> velocityArray;

        /// <summary>
        /// 実速度
        /// </summary>
        public ExNativeArray<float3> realVelocityArray;

        /// <summary>
        /// 摩擦(0.0 ~ 1.0)
        /// </summary>
        public ExNativeArray<float> frictionArray;

        /// <summary>
        /// 静止摩擦係数
        /// </summary>
        public ExNativeArray<float> staticFrictionArray;

        /// <summary>
        /// 接触コライダーの衝突法線
        /// </summary>
        public ExNativeArray<float3> collisionNormalArray;

        /// <summary>
        /// 接触中コライダーID
        /// 接触コライダーID+1が格納されているので注意！(0=なし)
        /// todo:現在未使用!
        /// </summary>
        //public ExNativeArray<int> colliderIdArray;

        public int ParticleCount => nextPosArray?.Count ?? 0;

        //=========================================================================================
        /// <summary>
        /// 制約
        /// </summary>
        public DistanceConstraint distanceConstraint;
        public TriangleBendingConstraint bendingConstraint;
        public TetherConstraint tetherConstraint;
        public AngleConstraint angleConstraint;
        public InertiaConstraint inertiaConstraint;
        public ColliderCollisionConstraint colliderCollisionConstraint;
        public MotionConstraint motionConstraint;
        public SelfCollisionConstraint selfCollisionConstraint;

        //=========================================================================================
        /// <summary>
        /// フレームもしくはステップごとに変動するリストを管理するための汎用バッファ。用途は様々
        /// </summary>
        internal ExProcessingList<int> processingStepParticle;
        internal ExProcessingList<int> processingStepTriangleBending;
        internal ExProcessingList<int> processingStepEdgeCollision;
        internal ExProcessingList<int> processingStepCollider;
        internal ExProcessingList<int> processingStepBaseLine;
        //internal ExProcessingList<int> processingIntList5;
        internal ExProcessingList<int> processingStepMotionParticle;

        internal ExProcessingList<int> processingSelfParticle;
        internal ExProcessingList<uint> processingSelfPointTriangle;
        internal ExProcessingList<uint> processingSelfEdgeEdge;
        internal ExProcessingList<uint> processingSelfTrianglePoint;

        //---------------------------------------------------------------------
        /// <summary>
        /// 汎用float3作業バッファ
        /// </summary>
        internal NativeArray<float3> tempFloat3Buffer;

        /// <summary>
        /// パーティクルごとのfloat3集計カウンタ（排他制御用）
        /// </summary>
        internal NativeArray<int> countArray;

        /// <summary>
        /// パーティクルごとのfloat3蓄積リスト、内部は固定小数点。パーティクル数x3。（排他制御用）
        /// </summary>
        internal NativeArray<int> sumArray;

        /// <summary>
        /// ステップごとのシミュレーションの基準となる姿勢座標
        /// 初期姿勢とアニメーション姿勢をAnimatinBlendRatioで補間したもの
        /// </summary>
        public NativeArray<float3> stepBasicPositionBuffer;

        /// <summary>
        /// ステップごとのシミュレーションの基準となる姿勢回転
        /// 初期姿勢とアニメーション姿勢をAnimatinBlendRatioで補間したもの
        /// </summary>
        public NativeArray<quaternion> stepBasicRotationBuffer;

        /// <summary>
        /// ステップ実行カウンター
        /// </summary>
        internal int SimulationStepCount { get; private set; }

        /// <summary>
        /// 実行環境で利用できるワーカースレッド数
        /// </summary>
        internal int WorkerCount => Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount;

        bool isValid = false;

        //=========================================================================================
        public void Dispose()
        {
            isValid = false;

            teamIdArray?.Dispose();
            nextPosArray?.Dispose();
            oldPosArray?.Dispose();
            oldRotArray?.Dispose();
            basePosArray?.Dispose();
            baseRotArray?.Dispose();
            oldPositionArray?.Dispose();
            oldRotationArray?.Dispose();
            velocityPosArray?.Dispose();
            dispPosArray?.Dispose();
            velocityArray?.Dispose();
            realVelocityArray?.Dispose();
            frictionArray?.Dispose();
            staticFrictionArray?.Dispose();
            collisionNormalArray?.Dispose();
            //colliderIdArray?.Dispose();

            teamIdArray = null;
            nextPosArray = null;
            oldPosArray = null;
            oldRotArray = null;
            basePosArray = null;
            baseRotArray = null;
            oldPositionArray = null;
            oldRotationArray = null;
            velocityPosArray = null;
            dispPosArray = null;
            velocityArray = null;
            realVelocityArray = null;
            frictionArray = null;
            staticFrictionArray = null;
            collisionNormalArray = null;
            //colliderIdArray = null;

            processingStepParticle?.Dispose();
            processingStepTriangleBending?.Dispose();
            processingStepEdgeCollision?.Dispose();
            processingStepCollider?.Dispose();
            processingStepBaseLine?.Dispose();
            //processingIntList5?.Dispose();
            processingStepMotionParticle?.Dispose();
            processingSelfParticle?.Dispose();
            processingSelfPointTriangle?.Dispose();
            processingSelfEdgeEdge?.Dispose();
            processingSelfTrianglePoint?.Dispose();

            if (tempFloat3Buffer.IsCreated)
                tempFloat3Buffer.Dispose();
            if (countArray.IsCreated)
                countArray.Dispose();
            if (sumArray.IsCreated)
                sumArray.Dispose();
            if (stepBasicPositionBuffer.IsCreated)
                stepBasicPositionBuffer.Dispose();
            if (stepBasicRotationBuffer.IsCreated)
                stepBasicRotationBuffer.Dispose();

            distanceConstraint?.Dispose();
            bendingConstraint?.Dispose();
            tetherConstraint?.Dispose();
            angleConstraint?.Dispose();
            inertiaConstraint?.Dispose();
            colliderCollisionConstraint?.Dispose();
            motionConstraint?.Dispose();
            selfCollisionConstraint?.Dispose();
            distanceConstraint = null;
            bendingConstraint = null;
            tetherConstraint = null;
            angleConstraint = null;
            inertiaConstraint = null;
            colliderCollisionConstraint = null;
            motionConstraint = null;
            selfCollisionConstraint = null;
        }

        public void EnterdEditMode()
        {
            Dispose();
        }

        public void Initialize()
        {
            Dispose();

            const int capacity = 0; // 1024?
            teamIdArray = new ExNativeArray<short>(capacity);
            nextPosArray = new ExNativeArray<float3>(capacity);
            oldPosArray = new ExNativeArray<float3>(capacity);
            oldRotArray = new ExNativeArray<quaternion>(capacity);
            basePosArray = new ExNativeArray<float3>(capacity);
            baseRotArray = new ExNativeArray<quaternion>(capacity);
            oldPositionArray = new ExNativeArray<float3>(capacity);
            oldRotationArray = new ExNativeArray<quaternion>(capacity);
            velocityPosArray = new ExNativeArray<float3>(capacity);
            dispPosArray = new ExNativeArray<float3>(capacity);
            velocityArray = new ExNativeArray<float3>(capacity);
            realVelocityArray = new ExNativeArray<float3>(capacity);
            frictionArray = new ExNativeArray<float>(capacity);
            staticFrictionArray = new ExNativeArray<float>(capacity);
            collisionNormalArray = new ExNativeArray<float3>(capacity);
            //colliderIdArray = new ExNativeArray<int>(capacity);

            processingStepParticle = new ExProcessingList<int>();
            processingStepTriangleBending = new ExProcessingList<int>();
            processingStepEdgeCollision = new ExProcessingList<int>();
            processingStepCollider = new ExProcessingList<int>();
            processingStepBaseLine = new ExProcessingList<int>();
            //processingIntList5 = new ExProcessingList<int>();
            processingStepMotionParticle = new ExProcessingList<int>();
            processingSelfParticle = new ExProcessingList<int>();
            processingSelfPointTriangle = new ExProcessingList<uint>();
            processingSelfEdgeEdge = new ExProcessingList<uint>();
            processingSelfTrianglePoint = new ExProcessingList<uint>();

            tempFloat3Buffer = new NativeArray<float3>(capacity, Allocator.Persistent);

            // 制約
            distanceConstraint = new DistanceConstraint();
            bendingConstraint = new TriangleBendingConstraint();
            tetherConstraint = new TetherConstraint();
            angleConstraint = new AngleConstraint();
            inertiaConstraint = new InertiaConstraint();
            colliderCollisionConstraint = new ColliderCollisionConstraint();
            motionConstraint = new MotionConstraint();
            selfCollisionConstraint = new SelfCollisionConstraint();

            SimulationStepCount = 0;

            isValid = true;

            Develop.DebugLog($"JobWorkerCount:{Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount}");
            //Develop.DebugLog($"MaxJobThreadCount:{Unity.Jobs.LowLevel.Unsafe.JobsUtility.MaxJobThreadCount}");
        }

        public bool IsValid()
        {
            return isValid;
        }

        //=========================================================================================
        /// <summary>
        /// プロキシメッシュをマネージャに登録する
        /// </summary>
        internal void RegisterProxyMesh(ClothProcess cprocess)
        {
            if (isValid == false)
                return;

            int teamId = cprocess.TeamId;
            var proxyMesh = cprocess.ProxyMesh;
            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);

            int pcnt = proxyMesh.VertexCount;
            tdata.particleChunk = teamIdArray.AddRange(pcnt, (short)teamId);
            nextPosArray.AddRange(pcnt);
            oldPosArray.AddRange(pcnt);
            oldRotArray.AddRange(pcnt);
            basePosArray.AddRange(pcnt);
            baseRotArray.AddRange(pcnt);
            oldPositionArray.AddRange(pcnt);
            oldRotationArray.AddRange(pcnt);
            velocityPosArray.AddRange(pcnt);
            dispPosArray.AddRange(pcnt);
            velocityArray.AddRange(pcnt);
            realVelocityArray.AddRange(pcnt);
            frictionArray.AddRange(pcnt);
            staticFrictionArray.AddRange(pcnt);
            collisionNormalArray.AddRange(pcnt);
            //colliderIdArray.AddRange(pcnt);
        }

        /// <summary>
        /// 制約データを登録する
        /// </summary>
        /// <param name="cprocess"></param>
        internal void RegisterConstraint(ClothProcess cprocess)
        {
            if (isValid == false)
                return;

            int teamId = cprocess.TeamId;

            // 慣性制約データをコピー（すでに領域は確保済みなのでコピーする）
            MagicaManager.Team.centerDataArray[teamId] = cprocess.inertiaConstraintData.centerData;

            // 制約データを登録する
            distanceConstraint.Register(cprocess);
            bendingConstraint.Register(cprocess);
            inertiaConstraint.Register(cprocess);
            selfCollisionConstraint.Register(cprocess);
        }


        /// <summary>
        /// プロキシメッシュをマネージャから解除する
        /// </summary>
        internal void ExitProxyMesh(ClothProcess cprocess)
        {
            if (isValid == false)
                return;

            int teamId = cprocess.TeamId;
            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);
            tdata.flag.SetBits(TeamManager.Flag_Exit, true); // 消滅フラグ

            var c = tdata.particleChunk;
            teamIdArray.RemoveAndFill(c);
            nextPosArray.Remove(c);
            oldPosArray.Remove(c);
            oldRotArray.Remove(c);
            basePosArray.Remove(c);
            baseRotArray.Remove(c);
            oldPositionArray.Remove(c);
            oldRotationArray.Remove(c);
            velocityPosArray.Remove(c);
            dispPosArray.Remove(c);
            velocityArray.Remove(c);
            realVelocityArray.Remove(c);
            frictionArray.Remove(c);
            staticFrictionArray.Remove(c);
            collisionNormalArray.Remove(c);
            //colliderIdArray.Remove(c);

            tdata.particleChunk.Clear();

            // 制約データを解除する
            distanceConstraint.Exit(cprocess);
            bendingConstraint.Exit(cprocess);
            inertiaConstraint.Exit(cprocess);
            selfCollisionConstraint.Exit(cprocess);
        }

        //=========================================================================================
        /// <summary>
        /// 作業バッファの更新
        /// </summary>
        internal void WorkBufferUpdate()
        {
            int pcnt = ParticleCount;
            //int ecnt = MagicaManager.VMesh.EdgeCount;
            //int tcnt = MagicaManager.VMesh.TriangleCount;
            int bcnt = MagicaManager.VMesh.BaseLineCount;
            int ccnt = MagicaManager.Collider.DataCount;
            int bendCnt = bendingConstraint.DataCount;

            // ステップ処理パーティクル全般
            processingStepParticle.UpdateBuffer(pcnt);

            // ステップ処理トライアングルベンド
            processingStepTriangleBending.UpdateBuffer(bendCnt);

            // ステップ処理コリジョン用エッジ
            int edgeColliderCount = MagicaManager.Team.edgeColliderCollisionCount;
            processingStepEdgeCollision.UpdateBuffer(edgeColliderCount);

            // 処理コライダー
            processingStepCollider.UpdateBuffer(ccnt);

            // ステップ処理ベースライン
            processingStepBaseLine.UpdateBuffer(bcnt);

            // ステップ処理セルフコリジョンパーティクル
            //processingIntList5.UpdateBuffer(pcnt);

            // ステップ実行モーション制約パーティクル
            processingStepMotionParticle.UpdateBuffer(pcnt);

            // セルフコリジョン
            processingSelfParticle.UpdateBuffer(pcnt);
            processingSelfPointTriangle.UpdateBuffer(selfCollisionConstraint.PointPrimitiveCount);
            processingSelfEdgeEdge.UpdateBuffer(selfCollisionConstraint.EdgePrimitiveCount);
            processingSelfTrianglePoint.UpdateBuffer(selfCollisionConstraint.TrianglePrimitiveCount);

            // 汎用作業バッファ
            tempFloat3Buffer.Resize(pcnt);
            stepBasicPositionBuffer.Resize(pcnt);
            stepBasicRotationBuffer.Resize(pcnt);

            // 加算バッファ
            countArray.Resize(pcnt);
            sumArray.Resize(pcnt * 3);

            // 制約
            angleConstraint.WorkBufferUpdate();
            colliderCollisionConstraint.WorkBufferUpdate();
            selfCollisionConstraint.WorkBufferUpdate();
        }

        //=========================================================================================
        /// <summary>
        /// シミュレーション実行前処理
        /// -リセット
        /// -移動影響
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle PreSimulationUpdate(JobHandle jobHandle)
        {
            // パーティクルのリセットおよび慣性の適用
            var job = new PreSimulationUpdateJob()
            {
                teamDataArray = MagicaManager.Team.teamDataArray.GetNativeArray(),
                parameterArray = MagicaManager.Team.parameterArray.GetNativeArray(),
                centerDataArray = MagicaManager.Team.centerDataArray.GetNativeArray(),

                positions = MagicaManager.VMesh.positions.GetNativeArray(),
                rotations = MagicaManager.VMesh.rotations.GetNativeArray(),
                vertexDepths = MagicaManager.VMesh.vertexDepths.GetNativeArray(),

                teamIdArray = teamIdArray.GetNativeArray(),
                nextPosArray = nextPosArray.GetNativeArray(),
                oldPosArray = oldPosArray.GetNativeArray(),
                oldRotArray = oldRotArray.GetNativeArray(),
                basePosArray = basePosArray.GetNativeArray(),
                baseRotArray = baseRotArray.GetNativeArray(),
                oldPositionArray = oldPositionArray.GetNativeArray(),
                oldRotationArray = oldRotationArray.GetNativeArray(),
                velocityPosArray = velocityPosArray.GetNativeArray(),
                dispPosArray = dispPosArray.GetNativeArray(),
                velocityArray = velocityArray.GetNativeArray(),
                realVelocityArray = realVelocityArray.GetNativeArray(),
                frictionArray = frictionArray.GetNativeArray(),
                staticFrictionArray = staticFrictionArray.GetNativeArray(),
                collisionNormalArray = collisionNormalArray.GetNativeArray(),
                //colliderIdArray = colliderIdArray.GetNativeArray(),
            };
            jobHandle = job.Schedule(ParticleCount, 32, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct PreSimulationUpdateJob : IJobParallelFor
        {
            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> rotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> vertexDepths;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> nextPosArray;
            public NativeArray<float3> oldPosArray;
            public NativeArray<quaternion> oldRotArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> basePosArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> baseRotArray;
            public NativeArray<float3> oldPositionArray;
            public NativeArray<quaternion> oldRotationArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> velocityPosArray;
            public NativeArray<float3> dispPosArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> velocityArray;
            public NativeArray<float3> realVelocityArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float> frictionArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float> staticFrictionArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> collisionNormalArray;
            //[Unity.Collections.WriteOnly]
            //public NativeArray<int> colliderIdArray;

            // パーティクルごと
            public void Execute(int pindex)
            {
                int teamId = teamIdArray[pindex];
                if (teamId == 0)
                    return;

                var tdata = teamDataArray[teamId];
                if (tdata.IsProcess == false)
                    return;

                int l_index = pindex - tdata.particleChunk.startIndex;
                int vindex = tdata.proxyCommonChunk.startIndex + l_index;

                if (tdata.IsReset)
                {
                    // リセット
                    var pos = positions[vindex];
                    var rot = rotations[vindex];

                    nextPosArray[pindex] = pos;
                    oldPosArray[pindex] = pos;
                    oldRotArray[pindex] = rot;
                    basePosArray[pindex] = pos;
                    baseRotArray[pindex] = rot;
                    oldPositionArray[pindex] = pos;
                    oldRotationArray[pindex] = rot;
                    velocityPosArray[pindex] = pos;
                    dispPosArray[pindex] = pos;
                    velocityArray[pindex] = 0;
                    realVelocityArray[pindex] = 0;
                    frictionArray[pindex] = 0;
                    staticFrictionArray[pindex] = 0;
                    collisionNormalArray[pindex] = 0;
                    //colliderIdArray[pindex] = 0;
                }
                else if (tdata.IsInertiaShift)
                {
                    // 慣性全体シフト
                    var cdata = centerDataArray[teamId];

                    // cdata.frameComponentShiftVector : 全体シフトベクトル
                    // cdata.frameComponentShiftRotation : 全体シフト回転
                    // cdata.oldComponentWorldPosition : フレーム移動前のコンポーネント中心位置

                    float3 prevFrameWorldPosition = cdata.oldComponentWorldPosition;

                    oldPosArray[pindex] = MathUtility.ShiftPosition(oldPosArray[pindex], prevFrameWorldPosition, cdata.frameComponentShiftVector, cdata.frameComponentShiftRotation);
                    oldRotArray[pindex] = math.mul(cdata.frameComponentShiftRotation, oldRotArray[pindex]);

                    oldPositionArray[pindex] = MathUtility.ShiftPosition(oldPositionArray[pindex], prevFrameWorldPosition, cdata.frameComponentShiftVector, cdata.frameComponentShiftRotation);
                    oldRotationArray[pindex] = math.mul(cdata.frameComponentShiftRotation, oldRotationArray[pindex]);

                    dispPosArray[pindex] = MathUtility.ShiftPosition(dispPosArray[pindex], prevFrameWorldPosition, cdata.frameComponentShiftVector, cdata.frameComponentShiftRotation);

                    realVelocityArray[pindex] = math.mul(cdata.frameComponentShiftRotation, realVelocityArray[pindex]);
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// クロスシミュレーションの１ステップ実行
        /// </summary>
        /// <param name="updateCount"></param>
        /// <param name="updateIndex"></param>
        /// <param name="simulationDeltaTime"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        unsafe internal JobHandle SimulationStepUpdate(int updateCount, int updateIndex, JobHandle jobHandle)
        {
            //Debug.Log($"Step:{updateIndex}/{updateCount}");

            var tm = MagicaManager.Team;
            var vm = MagicaManager.VMesh;
            var wm = MagicaManager.Wind;

            // シミュレーションステップカウンター
            SimulationStepCount++;

            // ステップごとのチーム更新
            jobHandle = tm.SimulationStepTeamUpdate(updateIndex, jobHandle);

            // 今回のステップで計算が必要な作業リストを作成する
            var clearStepCounterJob = new ClearStepCounter()
            {
                processingStepParticle = processingStepParticle.Counter, // ステップ実行パーティクル
                processingStepTriangleBending = processingStepTriangleBending.Counter, // ステップ実行トライアングルベンド
                processingStepEdgeCollision = processingStepEdgeCollision.Counter, // ステップ実行エッジコリジョン
                processingStepCollider = processingStepCollider.Counter, // ステップ実行コライダーリスト
                processingStepBaseLine = processingStepBaseLine.Counter, // ステップ実行ベースライン
                //processingCounter5 = processingIntList5.Counter, // (reserve)
                processingStepMotionParticle = processingStepMotionParticle.Counter, // ステップ実行モーション制約パーティクル

                processingSelfParticle = processingSelfParticle.Counter,
                processingSelfPointTriangle = processingSelfPointTriangle.Counter,
                processingSelfEdgeEdge = processingSelfEdgeEdge.Counter,
                processingSelfTrianglePoint = processingSelfTrianglePoint.Counter,
            };
            jobHandle = clearStepCounterJob.Schedule(jobHandle);

            var createUpdateParticleJob = new CreateUpdateParticleList()
            {
                teamDataArray = tm.teamDataArray.GetNativeArray(),
                parameterArray = tm.parameterArray.GetNativeArray(),

                stepParticleIndexCounter = processingStepParticle.Counter,
                stepParticleIndexArray = processingStepParticle.Buffer,

                stepBaseLineIndexCounter = processingStepBaseLine.Counter,
                stepBaseLineIndexArray = processingStepBaseLine.Buffer,

                stepTriangleBendIndexCounter = processingStepTriangleBending.Counter,
                stepTriangleBendIndexArray = processingStepTriangleBending.Buffer,

                stepEdgeCollisionIndexCounter = processingStepEdgeCollision.Counter,
                stepEdgeCollisionIndexArray = processingStepEdgeCollision.Buffer,

                motionParticleIndexCounter = processingStepMotionParticle.Counter,
                motionParticleIndexArray = processingStepMotionParticle.Buffer,

                selfParticleCounter = processingSelfParticle.Counter,
                selfParticleIndexArray = processingSelfParticle.Buffer,
                selfPointTriangleCounter = processingSelfPointTriangle.Counter,
                selfPointTriangleIndexArray = processingSelfPointTriangle.Buffer,
                selfEdgeEdgeCounter = processingSelfEdgeEdge.Counter,
                selfEdgeEdgeIndexArray = processingSelfEdgeEdge.Buffer,
                selfTrianglePointCounter = processingSelfTrianglePoint.Counter,
                selfTrianglePointIndexArray = processingSelfTrianglePoint.Buffer,
            };
            jobHandle = createUpdateParticleJob.Schedule(tm.TeamCount, 1, jobHandle);

            // 今回のステップで計算が必要なコライダーリストを作成する
            jobHandle = MagicaManager.Collider.CreateUpdateColliderList(updateIndex, jobHandle);

            // コライダーの更新
            jobHandle = MagicaManager.Collider.StartSimulationStep(jobHandle);

            // 速度更新、外力の影響、慣性シフト
            var startStepJob = new StartSimulationStepJob()
            {
                simulationPower = MagicaManager.Time.SimulationPower,
                simulationDeltaTime = MagicaManager.Time.SimulationDeltaTime,

                stepParticleIndexArray = processingStepParticle.Buffer,

                attributes = vm.attributes.GetNativeArray(),
                depthArray = vm.vertexDepths.GetNativeArray(),
                positions = vm.positions.GetNativeArray(),
                rotations = vm.rotations.GetNativeArray(),
                vertexRootIndices = vm.vertexRootIndices.GetNativeArray(),

                teamDataArray = tm.teamDataArray.GetNativeArray(),
                parameterArray = tm.parameterArray.GetNativeArray(),
                centerDataArray = tm.centerDataArray.GetNativeArray(),
                teamWindArray = tm.teamWindArray.GetNativeArray(),

                windDataArray = wm.windDataArray.GetNativeArray(),

                teamIdArray = teamIdArray.GetNativeArray(),
                oldPosArray = oldPosArray.GetNativeArray(),
                velocityArray = velocityArray.GetNativeArray(),
                nextPosArray = nextPosArray.GetNativeArray(),
                basePosArray = basePosArray.GetNativeArray(),
                baseRotArray = baseRotArray.GetNativeArray(),
                oldPositionArray = oldPositionArray.GetNativeArray(),
                oldRotationArray = oldRotationArray.GetNativeArray(),
                velocityPosArray = velocityPosArray.GetNativeArray(),
                frictionArray = frictionArray.GetNativeArray(),

                stepBasicPositionArray = stepBasicPositionBuffer,
                stepBasicRotationArray = stepBasicRotationBuffer,
            };
            jobHandle = startStepJob.Schedule(processingStepParticle.GetJobSchedulePtr(), 32, jobHandle);

            // 制約解決のためのステップごとの基準姿勢を計算（ベースラインから）
            var updateStepBasicPotureJob = new UpdateStepBasicPotureJob()
            {
                stepBaseLineIndexArray = processingStepBaseLine.Buffer,

                teamDataArray = tm.teamDataArray.GetNativeArray(),

                attributes = MagicaManager.VMesh.attributes.GetNativeArray(),
                vertexParentIndices = vm.vertexParentIndices.GetNativeArray(),
                vertexLocalPositions = vm.vertexLocalPositions.GetNativeArray(),
                vertexLocalRotations = vm.vertexLocalRotations.GetNativeArray(),
                baseLineStartDataIndices = vm.baseLineStartDataIndices.GetNativeArray(),
                baseLineDataCounts = vm.baseLineDataCounts.GetNativeArray(),
                baseLineData = vm.baseLineData.GetNativeArray(),
                //vertexToTransformRotations = vm.vertexToTransformRotations.GetNativeArray(),

                basePosArray = basePosArray.GetNativeArray(),
                baseRotArray = baseRotArray.GetNativeArray(),

                stepBasicPositionArray = stepBasicPositionBuffer,
                stepBasicRotationArray = stepBasicRotationBuffer,
            };
            jobHandle = updateStepBasicPotureJob.Schedule(processingStepBaseLine.GetJobSchedulePtr(), 2, jobHandle);

            // 制約の解決
            //for (int i = 0; i < 2; i++)
            {
                // 一般制約
                jobHandle = tetherConstraint.SolverConstraint(jobHandle);
                jobHandle = distanceConstraint.SolverConstraint(jobHandle);
                jobHandle = angleConstraint.SolverConstraint(jobHandle);
                jobHandle = bendingConstraint.SolverConstraint(jobHandle);
                // コライダーコリジョン
                jobHandle = colliderCollisionConstraint.SolverConstraint(jobHandle);
                // コライダー衝突後はパーティクルが乱れる可能性があるためもう一度距離制約で整える。
                // これは裏返り防止などに効果大。
                jobHandle = distanceConstraint.SolverConstraint(jobHandle);
                // モーション制約はコライダーより優先
                jobHandle = motionConstraint.SolverConstraint(jobHandle);
                // セルフコリジョンは最後
                jobHandle = selfCollisionConstraint.SolverConstraint(updateIndex, jobHandle);
            }

            // 座標確定
            var endStepJob = new EndSimulationStepJob()
            {
                simulationDeltaTime = MagicaManager.Time.SimulationDeltaTime,

                stepParticleIndexArray = processingStepParticle.Buffer,

                teamDataArray = tm.teamDataArray.GetNativeArray(),
                parameterArray = tm.parameterArray.GetNativeArray(),
                centerDataArray = tm.centerDataArray.GetNativeArray(),

                attributes = vm.attributes.GetNativeArray(),
                vertexDepths = vm.vertexDepths.GetNativeArray(),

                teamIdArray = teamIdArray.GetNativeArray(),
                nextPosArray = nextPosArray.GetNativeArray(),
                oldPosArray = oldPosArray.GetNativeArray(),
                velocityArray = velocityArray.GetNativeArray(),
                realVelocityArray = realVelocityArray.GetNativeArray(),
                velocityPosArray = velocityPosArray.GetNativeArray(),
                frictionArray = frictionArray.GetNativeArray(),
                staticFrictionArray = staticFrictionArray.GetNativeArray(),
                collisionNormalArray = collisionNormalArray.GetNativeArray(),
            };
            jobHandle = endStepJob.Schedule(processingStepParticle.GetJobSchedulePtr(), 32, jobHandle);

            // コライダーの後更新
            jobHandle = MagicaManager.Collider.EndSimulationStep(jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct ClearStepCounter : IJob
        {
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingStepParticle;
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingStepTriangleBending;
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingStepEdgeCollision;
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingStepCollider;
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingStepBaseLine;
            //[Unity.Collections.WriteOnly]
            //public NativeReference<int> processingCounter5;
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingStepMotionParticle;

            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingSelfParticle;
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingSelfPointTriangle;
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingSelfEdgeEdge;
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingSelfTrianglePoint;

            public void Execute()
            {
                processingStepParticle.Value = 0;
                processingStepTriangleBending.Value = 0;
                processingStepEdgeCollision.Value = 0;
                processingStepCollider.Value = 0;
                processingStepBaseLine.Value = 0;
                //processingCounter5.Value = 0;
                processingStepMotionParticle.Value = 0;

                processingSelfParticle.Value = 0;
                processingSelfPointTriangle.Value = 0;
                processingSelfEdgeEdge.Value = 0;
                processingSelfTrianglePoint.Value = 0;
            }
        }

        [BurstCompile]
        struct CreateUpdateParticleList : IJobParallelFor
        {
            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // buffer
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> stepParticleIndexCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> stepParticleIndexArray;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> stepBaseLineIndexCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> stepBaseLineIndexArray;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> stepTriangleBendIndexCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> stepTriangleBendIndexArray;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> stepEdgeCollisionIndexCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> stepEdgeCollisionIndexArray;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> motionParticleIndexCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> motionParticleIndexArray;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> selfParticleCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> selfParticleIndexArray;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> selfPointTriangleCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<uint> selfPointTriangleIndexArray;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> selfEdgeEdgeCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<uint> selfEdgeEdgeIndexArray;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> selfTrianglePointCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<uint> selfTrianglePointIndexArray;

            // チームごと
            public void Execute(int teamId)
            {
                if (teamId == 0)
                    return;

                var tdata = teamDataArray[teamId];
                if (tdata.IsProcess == false || tdata.IsRunning == false)
                    return;

                // このステップでの更新があるか判定する
                if (tdata.IsStepRunning == false)
                    return;

                var parameter = parameterArray[teamId];

                // パーティクルリスト
                int pcnt = tdata.particleChunk.dataLength;
                int pstart = tdata.particleChunk.startIndex;
                int start = stepParticleIndexCounter.InterlockedStartIndex(pcnt);
                for (int i = 0; i < pcnt; i++)
                {
                    stepParticleIndexArray[start + i] = pstart + i;
                }

                // ベースライン
                int bcnt = tdata.BaseLineCount;
                int bstart = tdata.baseLineChunk.startIndex;
                start = stepBaseLineIndexCounter.InterlockedStartIndex(bcnt);
                for (int i = 0; i < bcnt; i++)
                {
                    // 上位16bit:チームID, 下位16bit:ベースラインインデックス
                    uint pack = DataUtility.Pack32(teamId, bstart + i);
                    stepBaseLineIndexArray[start + i] = (int)pack;
                }

                // トライアングルベンド
                if (parameter.triangleBendingConstraint.method != TriangleBendingConstraint.Method.None)
                {
                    int bendCnt = tdata.bendingPairChunk.dataLength;
                    int bendIndex = tdata.bendingPairChunk.startIndex;
                    start = stepTriangleBendIndexCounter.InterlockedStartIndex(bendCnt);
                    for (int i = 0; i < bendCnt; i++, bendIndex++)
                    {
                        uint pack = DataUtility.Pack12_20(teamId, bendIndex);
                        stepTriangleBendIndexArray[start + i] = (int)pack;
                    }
                }

                // エッジコライダーコリジョン
                int colliderCount = tdata.ColliderCount;
                if (parameter.colliderCollisionConstraint.mode == ColliderCollisionConstraint.Mode.Edge && tdata.proxyEdgeChunk.IsValid && colliderCount > 0)
                {
                    int ecnt = tdata.proxyEdgeChunk.dataLength;
                    int estart = tdata.proxyEdgeChunk.startIndex;
                    start = stepEdgeCollisionIndexCounter.InterlockedStartIndex(ecnt);
                    for (int i = 0; i < ecnt; i++)
                    {
                        stepEdgeCollisionIndexArray[start + i] = estart + i;
                    }
                }

                // モーション制約パーティクル
                if (parameter.motionConstraint.useMaxDistance || parameter.motionConstraint.useBackstop)
                {
                    start = motionParticleIndexCounter.InterlockedStartIndex(pcnt);
                    for (int i = 0; i < pcnt; i++)
                    {
                        motionParticleIndexArray[start + i] = pstart + i;
                    }
                }

                // セルフコリジョン
                bool useSelfEdgeEdge = tdata.flag.TestAny(TeamManager.Flag_Self_EdgeEdge, 3);
                bool useSelfPointTriangle = tdata.flag.TestAny(TeamManager.Flag_Self_PointTriangle, 3);
                bool useSelfTrianglePoint = tdata.flag.TestAny(TeamManager.Flag_Self_TrianglePoint, 3);
                if (useSelfEdgeEdge)
                {
                    int ecnt = tdata.EdgeCount;
                    start = selfEdgeEdgeCounter.InterlockedStartIndex(ecnt);
                    for (int i = 0; i < ecnt; i++)
                    {
                        // 上位16bit:チームID, 下位16bit:Edgeインデックス
                        uint pack = DataUtility.Pack32(teamId, i);
                        selfEdgeEdgeIndexArray[start + i] = pack;
                    }
                }
                if (useSelfPointTriangle)
                {
                    start = selfPointTriangleCounter.InterlockedStartIndex(pcnt);
                    for (int i = 0; i < pcnt; i++)
                    {
                        // 上位16bit:チームID, 下位16bit:Pointインデックス
                        uint pack = DataUtility.Pack32(teamId, i);
                        selfPointTriangleIndexArray[start + i] = pack;
                    }
                }
                if (useSelfTrianglePoint)
                {
                    int tcnt = tdata.TriangleCount;
                    start = selfTrianglePointCounter.InterlockedStartIndex(tcnt);
                    for (int i = 0; i < tcnt; i++)
                    {
                        // 上位16bit:チームID, 下位16bit:Triangleインデックス
                        uint pack = DataUtility.Pack32(teamId, i);
                        selfTrianglePointIndexArray[start + i] = pack;
                    }
                }
                if (useSelfEdgeEdge || useSelfPointTriangle || useSelfTrianglePoint)
                {
                    start = selfParticleCounter.InterlockedStartIndex(pcnt);
                    for (int i = 0; i < pcnt; i++)
                    {
                        selfParticleIndexArray[start + i] = pstart + i;
                    }
                }

                //Debug.Log($"Step:{updateIndex}, updateParticleCount:{jobParticleIndexList.Length}");
            }
        }

        [BurstCompile]
        struct StartSimulationStepJob : IJobParallelForDefer
        {
            public float4 simulationPower;
            public float simulationDeltaTime;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> stepParticleIndexArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> rotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexRootIndices;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamWindData> teamWindArray;

            // wind
            [Unity.Collections.ReadOnly]
            public NativeArray<WindManager.WindData> windDataArray;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> velocityArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> basePosArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> baseRotArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> oldRotationArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> velocityPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionArray;

            // buffer
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> stepBasicPositionArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> stepBasicRotationArray;


            // ステップパーティクルごと
            public void Execute(int index)
            {
                int pindex = stepParticleIndexArray[index];
                int teamId = teamIdArray[pindex];
                var tdata = teamDataArray[teamId];
                int l_index = pindex - tdata.particleChunk.startIndex;

                // 各カテゴリのデータインデックス
                int vindex = tdata.proxyCommonChunk.startIndex + l_index;

                // パラメータ
                var param = parameterArray[teamId];

                // nextPosSwap
                var attr = attributes[vindex];
                float depth = depthArray[vindex];
                var oldPos = oldPosArray[pindex];

                var nextPos = oldPos;
                var velocityPos = oldPos;

                // 基準姿勢のステップ補間
                var oldPosition = oldPositionArray[pindex];
                var oldRotation = oldRotationArray[pindex];
                var position = positions[vindex];
                var rotation = rotations[vindex];

                // ベース位置補間
                float3 basePos = math.lerp(oldPosition, position, tdata.frameInterpolation);
                quaternion baseRot = math.slerp(oldRotation, rotation, tdata.frameInterpolation);
                baseRot = math.normalize(baseRot); // 必要
                basePosArray[pindex] = basePos;
                baseRotArray[pindex] = baseRot;

                // ステップ基本位置
                stepBasicPositionArray[pindex] = basePos;
                stepBasicRotationArray[pindex] = baseRot;

                // 移動パーティクル
                if (attr.IsMove())
                {
                    var cdata = centerDataArray[teamId];

                    // 重量
                    //float mass = MathUtility.CalcMass(depth);

                    // 速度
                    var velocity = velocityArray[pindex];

#if true
                    // ■ローカル慣性シフト
                    // シフト量
                    float3 inertiaVector = cdata.inertiaVector;
                    quaternion inertiaRotation = cdata.inertiaRotation;

                    // 慣性の深さ影響
                    float inertiaDepth = param.inertiaConstraint.depthInertia * (1.0f - depth * depth); // 二次曲線
                    inertiaVector = math.lerp(inertiaVector, cdata.stepVector, inertiaDepth);
                    inertiaRotation = math.slerp(inertiaRotation, cdata.stepRotation, inertiaDepth);

                    // たぶんこっちが正しい
                    float3 lpos = oldPos - cdata.oldWorldPosition;
                    lpos = math.mul(inertiaRotation, lpos);
                    lpos += inertiaVector;
                    float3 wpos = cdata.oldWorldPosition + lpos;
                    var inertiaOffset = wpos - nextPos;

                    // nextPos
                    nextPos = wpos;

                    // 速度位置も調整
                    velocityPos += inertiaOffset;

                    // 速度に慣性回転を加える
                    velocity = math.mul(inertiaRotation, velocity);
#endif

                    // 安定化用の速度割合
                    velocity *= tdata.velocityWeight;

                    // 抵抗
                    // 重力に影響させたくないので先に計算する（※通常はforce適用後に行うのが一般的）
                    float damping = param.dampingCurveData.EvaluateCurveClamp01(depth);
                    velocity *= math.saturate(1.0f - damping * simulationPower.z);

                    // 外力
                    float3 force = 0;

                    // 重力
                    float3 gforce = param.gravityDirection * (param.gravity * tdata.gravityRatio);
                    force += gforce;

                    // 外力
                    float3 exForce = 0;
                    float mass = MathUtility.CalcMass(depth);
                    switch (tdata.forceMode)
                    {
                        case ClothForceMode.VelocityAdd:
                            exForce = tdata.impactForce / mass;
                            break;
                        case ClothForceMode.VelocityAddWithoutDepth:
                            exForce = tdata.impactForce;
                            break;
                        case ClothForceMode.VelocityChange:
                            exForce = tdata.impactForce / mass;
                            velocity = 0;
                            break;
                        case ClothForceMode.VelocityChangeWithoutDepth:
                            exForce = tdata.impactForce;
                            velocity = 0;
                            break;
                    }
                    force += exForce;

                    // 風力
                    force += Wind(teamId, tdata, param.wind, cdata, vindex, pindex, depth);

                    // 外力チームスケール倍率
                    force *= tdata.scaleRatio;

                    // 速度更新
                    velocity += force * simulationDeltaTime;

                    // 予測位置更新
                    nextPos += velocity * simulationDeltaTime;
                }
                else
                {
                    // 固定パーティクル
                    nextPos = basePos;
                    velocityPos = basePos;
                }

                // 速度計算用の移動前の位置
                velocityPosArray[pindex] = velocityPos;

                // 予測位置格納
                nextPosArray[pindex] = nextPos;
            }

            float3 Wind(int teamId, in TeamManager.TeamData tdata, in WindParams windParams, in InertiaConstraint.CenterData cdata, int vindex, int pindex, float depth)
            {
                float3 windForce = 0;

                // 基準ルート座標
                // (1)チームごとにずらす
                // (2)同期率によりルートラインごとにずらす
                // (3)チームの座標やパーティクルの座標は計算に入れない
                int rootIndex = vertexRootIndices[vindex];
                float3 windPos = (teamId + 1) * 4.19230645f + (rootIndex * 0.0023963f * (1.0f - windParams.synchronization) * 100);

                // ゾーンごとの風影響計算
                var teamWindData = teamWindArray[teamId];
                int cnt = teamWindData.ZoneCount;
                for (int i = 0; i < cnt; i++)
                {
                    var windInfo = teamWindData.windZoneList[i];
                    var windData = windDataArray[windInfo.windId];
                    windForce += WindForceBlend(windInfo, windParams, windPos, windData.turbulence);
                }

#if true
                // 移動風影響計算
                if (windParams.movingWind > 0.01f)
                {
                    windForce += WindForceBlend(teamWindData.movingWind, windParams, windPos, 1.0f);
                }
#endif

                //Debug.Log($"windForce:{windForce}");

                // その他影響
                // チーム風影響
                float influence = windParams.influence; // 0.0 ~ 2.0

                // 摩擦による影響
                float friction = frictionArray[pindex];
                influence *= (1.0f - friction);

                // 深さ影響
                float depthScale = depth * depth;
                influence *= math.lerp(1.0f, depthScale, windParams.depthWeight);

                // 最終影響
                windForce *= influence;

                //Debug.Log($"windForce:{windForce}");

                return windForce;
            }

            float3 WindForceBlend(in TeamWindInfo windInfo, in WindParams windParams, in float3 windPos, float windTurbulence)
            {
                float windMain = windInfo.main;
                if (windMain < 0.01f)
                    return 0;

                // 風速係数
                float mainRatio = windMain / Define.System.WindBaseSpeed; // 0.0 ~ 

                // Sin波形
                var sinPos = windPos + windInfo.time * 10.0f;
                float2 sinXY = math.sin(sinPos.xy);

                // Noise波形
                var noisePos = windPos + windInfo.time * 2.3132f; // Sin波形との調整用
                float2 noiseXY = new float2(noise.cnoise(noisePos.xy), noise.cnoise(noisePos.yx));
                noiseXY *= 2.3f; // cnoiseは弱いので補強 2.0?

                // 波形ブレンド
                float2 waveXY = math.lerp(sinXY, noiseXY, windParams.blend);

                // 基本乱流率
                windTurbulence *= windParams.turbulence; // 0.0 ~ 2.0

                // 風向き
                const float rangAng = 45.0f; // 乱流角度
                var ang = math.radians(waveXY * rangAng);
                ang.y *= math.lerp(0.1f, 0.5f, windParams.blend); // 横方向は抑える。そうしないと円運動になってしまうため。0.3 - 0.5?
                ang *= windTurbulence; // 乱流率
                var rq = quaternion.Euler(ang.x, ang.y, 0.0f); // XY
                var dirq = MathUtility.AxisQuaternion(windInfo.direction);
                float3 wdir = math.forward(math.mul(dirq, rq));

                // 風速
                // 風速が低いと大きくなり、風速が高いと0.0になる
                float mainScale = math.saturate(1.0f - mainRatio * 1.0f);
                float mainWave = math.unlerp(-1.0f, 1.0f, waveXY.x); // 0.0 ~ 1.0
                mainWave *= mainScale * windTurbulence;
                windMain -= windMain * mainWave;

                // 合成
                float3 windForce = wdir * windMain;

                return windForce;
            }
        }

        /// <summary>
        /// ベースラインごとに初期姿勢を求める
        /// これは制約の解決で利用される
        /// AnimationPoseRatioが1.0ならば不要なのでスキップされる
        /// </summary>
        [BurstCompile]
        struct UpdateStepBasicPotureJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> stepBaseLineIndexArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexParentIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> vertexLocalPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> vertexLocalRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineStartDataIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineDataCounts;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineData;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<quaternion> vertexToTransformRotations;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> baseRotArray;

            // buffer
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> stepBasicPositionArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<quaternion> stepBasicRotationArray;

            // ステップ実行ベースラインごと
            public void Execute(int index)
            {
                // チームは有効であることが保証されている
                uint pack = (uint)stepBaseLineIndexArray[index];
                int teamId = DataUtility.Unpack32Hi(pack);
                int bindex = DataUtility.Unpack32Low(pack);

                var tdata = teamDataArray[teamId];

                // アニメーションポーズ使用の有無
                // 初期姿勢の計算が不要なら抜ける
                float blendRatio = tdata.animationPoseRatio;
                //bool isBasePose = blendRatio > 0.99f;
                if (blendRatio > 0.99f)
                    return;

                int b_datastart = tdata.baseLineDataChunk.startIndex;
                int p_start = tdata.particleChunk.startIndex;
                int v_start = tdata.proxyCommonChunk.startIndex;
                //int vt_start = tdata.proxyBoneChunk.startIndex;

                // チームスケール
                float3 scl = tdata.initScale * tdata.scaleRatio;

                int b_start = baseLineStartDataIndices[bindex];
                int b_cnt = baseLineDataCounts[bindex];
                int b_dataindex = b_start + b_datastart;
                //if (isBasePose == false)
                {
                    for (int i = 0; i < b_cnt; i++, b_dataindex++)
                    {
                        int l_index = baseLineData[b_dataindex];
                        int pindex = p_start + l_index;
                        int vindex = v_start + l_index;

                        // 親
                        int p_index = vertexParentIndices[vindex];
                        int p_pindex = p_index + p_start;

                        var attr = attributes[vindex];
                        //if (attr.IsMove() == false || p_index < 0)
                        //{
                        //    // 固定もしくはアニメーションポーズを使用
                        //    // basePos/baseRotをそのままコピーする
                        //    var bpos = basePosArray[pindex];
                        //    var brot = baseRotArray[pindex];

                        //    stepBasicPositionArray[pindex] = bpos;
                        //    stepBasicRotationArray[pindex] = brot;
                        //}
                        //else
                        if (attr.IsMove() && p_index >= 0)
                        {
                            // 移動
                            // 親から姿勢を算出する
                            var lpos = vertexLocalPositions[vindex];
                            var lrot = vertexLocalRotations[vindex];
                            var ppos = stepBasicPositionArray[p_pindex];
                            var prot = stepBasicRotationArray[p_pindex];
                            stepBasicPositionArray[pindex] = math.mul(prot, lpos * scl) + ppos;
                            stepBasicRotationArray[pindex] = math.mul(prot, lrot);
                        }
                    }
                }

                // アニメーション姿勢とブレンド
                if (blendRatio > Define.System.Epsilon)
                {
                    b_dataindex = b_start + b_datastart;
                    for (int i = 0; i < b_cnt; i++, b_dataindex++)
                    {
                        int l_index = baseLineData[b_dataindex];
                        int pindex = p_start + l_index;

                        var bpos = basePosArray[pindex];
                        var brot = baseRotArray[pindex];

                        stepBasicPositionArray[pindex] = math.lerp(stepBasicPositionArray[pindex], bpos, blendRatio);
                        stepBasicRotationArray[pindex] = math.slerp(stepBasicRotationArray[pindex], brot, blendRatio);
                        //stepBasicPositionArray[pindex] = isBasePose ? bpos : math.lerp(stepBasicPositionArray[pindex], bpos, blendRatio);
                        //stepBasicRotationArray[pindex] = isBasePose ? brot : math.slerp(stepBasicRotationArray[pindex], brot, blendRatio);
                    }
                }
            }
        }

        /// <summary>
        /// ステップ終了後の座標確定処理
        /// </summary>
        [BurstCompile]
        struct EndSimulationStepJob : IJobParallelForDefer
        {
            public float simulationDeltaTime;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> stepParticleIndexArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> vertexDepths;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> oldPosArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> velocityArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> realVelocityArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> velocityPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float> frictionArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float> staticFrictionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> collisionNormalArray;

            // ステップ有効パーティクルごと
            public void Execute(int index)
            {
                // パーティクルは有効であることが保証されている
                int pindex = stepParticleIndexArray[index];
                int teamId = teamIdArray[pindex];
                var tdata = teamDataArray[teamId];
                var cdata = centerDataArray[teamId];
                var param = parameterArray[teamId];

                int pstart = tdata.particleChunk.startIndex;
                int l_index = pindex - pstart;

                // 各カテゴリのデータインデックス
                int vindex = tdata.proxyCommonChunk.startIndex + l_index;

                var attr = attributes[vindex];
                var depth = vertexDepths[vindex];
                var nextPos = nextPosArray[pindex];
                var oldPos = oldPosArray[pindex];

                if (attr.IsMove())
                {
                    // 移動パーティクル
                    var velocityOldPos = velocityPosArray[pindex];

#if true
                    // ■摩擦
                    float friction = frictionArray[pindex];
                    float3 cn = collisionNormalArray[pindex];
                    bool isCollision = math.lengthsq(cn) > Define.System.Epsilon; // 接触の有無
                    float staticFrictionParam = param.colliderCollisionConstraint.staticFriction * tdata.scaleRatio;
                    float dynamicFrictionParam = param.colliderCollisionConstraint.dynamicFriction;
#endif

#if true
                    // ■静止摩擦
                    float staticFriction = staticFrictionArray[pindex];
                    if (isCollision && friction > 0.0f && staticFrictionParam > 0.0f)
                    {
                        // 接線方向の移動速度から計算する
                        var v = nextPos - oldPos;
                        var tanv = v - MathUtility.Project(v, cn); // 接線方向の移動ベクトル
                        float tangentVelocity = math.length(tanv) / simulationDeltaTime; // 接線方向の移動速度

                        // 静止速度以下ならば係数を上げる
                        if (tangentVelocity < staticFrictionParam)
                        {
                            staticFriction = math.saturate(staticFriction + 0.04f); // 係数増加(0.02?)
                        }
                        else
                        {
                            // 接線速度に応じて係数を減少
                            var vel = tangentVelocity - staticFrictionParam;
                            var value = math.max(vel / 0.2f, 0.05f);
                            staticFriction = math.saturate(staticFriction - value);
                        }

                        // 接線方向に位置を巻き戻す
                        tanv *= staticFriction;
                        nextPos -= tanv;
                        velocityOldPos -= tanv;
                    }
                    else
                    {
                        // 減衰
                        staticFriction = math.saturate(staticFriction - 0.05f);
                    }
                    staticFrictionArray[pindex] = staticFriction;
#endif

                    // ■速度更新(m/s) ------------------------------------------
                    // 速度計算用の位置から割り出す（制約ごとの速度調整用）
                    float3 velocity = (nextPos - velocityOldPos) / simulationDeltaTime;
                    float sqVel = math.lengthsq(velocity);
                    float3 normalVelocity = sqVel > Define.System.Epsilon ? math.normalize(velocity) : 0;

#if true
                    // ■動摩擦
                    // 衝突面との角度が大きいほど減衰が強くなる(MC1)
                    if (friction > Define.System.Epsilon && isCollision && dynamicFrictionParam > 0.0f && sqVel >= Define.System.Epsilon)
                    {
                        //float dot = math.dot(cn, math.normalize(velocity));
                        float dot = math.dot(cn, normalVelocity);
                        dot = 0.5f + 0.5f * dot; // 1.0(front) - 0.5(side) - 0.0(back)
                        dot *= dot; // サイドを強めに
                        dot = 1.0f - dot; // 0.0(front) - 0.75(side) - 1.0(back)
                        velocity -= velocity * (dot * math.saturate(friction * dynamicFrictionParam));
                    }

                    // 摩擦減衰
                    friction *= Define.System.FrictionDampingRate;
                    frictionArray[pindex] = friction;
#endif

#if true
                    // 最大速度
                    // 最大速度はある程度制限したほうが動きが良くなるので入れるべき。
                    // 特に回転時の髪などの動きが柔らかくなる。
                    // しかし制限しすぎるとコライダーの押し出し制度がさがるので注意。
                    if (param.inertiaConstraint.particleSpeedLimit >= 0.0f)
                    {
                        velocity = MathUtility.ClampVector(velocity, param.inertiaConstraint.particleSpeedLimit * tdata.scaleRatio);
                    }
#endif
#if true
                    // ■遠心力加速 ---------------------------------------------
                    if (cdata.angularVelocity > Define.System.Epsilon && param.inertiaConstraint.centrifualAcceleration > Define.System.Epsilon && sqVel >= Define.System.Epsilon)
                    {
                        // 回転中心のローカル座標
                        var lpos = nextPos - cdata.nowWorldPosition;

                        // 回転軸平面に投影
                        var v = MathUtility.ProjectOnPlane(lpos, cdata.rotationAxis);
                        var r = math.length(v);
                        if (r > Define.System.Epsilon)
                        {
                            float3 n = v / r;

                            // 角速度(rad/s)
                            float w = cdata.angularVelocity;

                            // 重量（重いほど遠心力は強くなる）
                            // ここでは末端に行くほど軽くする
                            //float m = (1.0f - depth) * 3.0f;
                            //float m = 1.0f + (1.0f - depth) * 2.0f;
                            float m = 1.0f + (1.0f - depth); // fix
                            //float m = 1.0f + depth * 3.0f;
                            //const float m = 1;

                            // 遠心力
                            var f = m * w * w * r;

                            // 回転方向uと速度方向が同じ場合のみ力を加える（内積による乗算）
                            // 実際の物理では遠心力は紐が張った状態でなければ発生しないがこの状態を判別する方法は簡単ではない
                            // そのためこのような近似で代用する
                            // 回転と速度が逆方向の場合は紐が緩んでいると判断し遠心力の増強を適用しない
                            float3 u = math.normalize(math.cross(cdata.rotationAxis, n));
                            f *= math.saturate(math.dot(normalVelocity, u));

                            // 遠心力を速度に加算する
                            velocity += n * (f * param.inertiaConstraint.centrifualAcceleration * 0.02f);
                        }
                    }
#endif
                    // 安定化用の速度割合
                    velocity *= tdata.velocityWeight;

                    // 書き戻し
                    velocityArray[pindex] = velocity;
                }

                // 実速度
                float3 realVelocity = (nextPos - oldPos) / simulationDeltaTime;
                realVelocityArray[pindex] = realVelocity;
                //Debug.Log($"[{pindex}] realVelocity:{realVelocity}");

                // 今回の予測位置を記録
                oldPosArray[pindex] = nextPos;
            }
        }

        //=========================================================================================
        /// <summary>
        /// シミュレーション完了後の表示位置の計算
        /// - 未来予測
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle CalcDisplayPosition(JobHandle jobHandle)
        {
            // ここではproxyMeshのpositionsのみを更新する
            // rotationsは自動で計算されるため
            var job = new CalcDisplayPositionJob()
            {
                simulationDeltaTime = MagicaManager.Time.SimulationDeltaTime,

                teamDataArray = MagicaManager.Team.teamDataArray.GetNativeArray(),

                teamIdArray = teamIdArray.GetNativeArray(),
                oldPosArray = oldPosArray.GetNativeArray(),
                realVelocityArray = realVelocityArray.GetNativeArray(),
                oldPositionArray = oldPositionArray.GetNativeArray(),
                oldRotationArray = oldRotationArray.GetNativeArray(),
                dispPosArray = dispPosArray.GetNativeArray(),

                attributes = MagicaManager.VMesh.attributes.GetNativeArray(),
                positions = MagicaManager.VMesh.positions.GetNativeArray(),
                rotations = MagicaManager.VMesh.rotations.GetNativeArray(),
            };
            jobHandle = job.Schedule(ParticleCount, 32, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct CalcDisplayPositionJob : IJobParallelFor
        {
            public float simulationDeltaTime;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> realVelocityArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> oldPositionArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> oldRotationArray;
            public NativeArray<float3> dispPosArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> rotations;

            // すべてのパーティクルごと
            public void Execute(int pindex)
            {
                int teamId = teamIdArray[pindex];
                if (teamId == 0)
                    return;

                var tdata = teamDataArray[teamId];
                if (tdata.IsProcess == false)
                    return;

                // ■この処理は更新に関係なく実行する

                int l_index = pindex - tdata.particleChunk.startIndex;
                int vindex = tdata.proxyCommonChunk.startIndex + l_index;

                var attr = attributes[vindex];
                //if (attr.IsInvalid())
                //    return;

                var pos = positions[vindex];
                var rot = rotations[vindex];

                if (attr.IsMove())
                {
                    // 移動パーティクル
                    var dpos = oldPosArray[pindex];

#if true
                    // 未来予測
                    // 最終計算位置と実速度から次のステップ位置を予測し、その間のフレーム時間位置を表示位置とする
                    float3 velocity = realVelocityArray[pindex] * simulationDeltaTime;
                    float3 fpos = dpos + velocity;
                    float interval = (tdata.nowUpdateTime + simulationDeltaTime) - tdata.oldTime;
                    //float t = (tdata.time - tdata.oldTime) / interval;
                    float t = interval > 0.0f ? (tdata.time - tdata.oldTime) / interval : 0.0f;
                    fpos = math.lerp(dispPosArray[pindex], fpos, t);
                    dpos = fpos;
#endif

                    // 表示位置
                    var dispPos = dpos;

                    // 表示位置を記録
                    dispPosArray[pindex] = dispPos;

                    // ブレンドウエイト
                    var vpos = math.lerp(positions[vindex], dispPos, tdata.blendWeight);

                    // vmeshに反映
                    positions[vindex] = vpos;
                }
                else
                {
                    // 固定パーティクル
                    // 表示位置は常にオリジナル位置
                    var dispPos = positions[vindex];
                    dispPosArray[pindex] = dispPos;
                }

                // １つ前の原点位置を記録
                if (tdata.IsRunning)
                {
                    oldPositionArray[pindex] = pos;
                    oldRotationArray[pindex] = rot;
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// tempFloat3Bufferの内容をnextPosArrayに書き戻す
        /// </summary>
        /// <param name="particleList"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle FeedbackTempFloat3Buffer(in NativeList<int> particleList, JobHandle jobHandle)
        {
            var job = new FeedbackTempPosJob()
            {
                jobParticleIndexList = particleList,
                tempFloat3Buffer = tempFloat3Buffer,
                nextPosArray = nextPosArray.GetNativeArray(),
            };
            jobHandle = job.Schedule(particleList, 32, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct FeedbackTempPosJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeList<int> jobParticleIndexList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> tempFloat3Buffer;

            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;

            public void Execute(int index)
            {
                int pindex = jobParticleIndexList[index];
                nextPosArray[pindex] = tempFloat3Buffer[pindex];
            }
        }

        internal JobHandle FeedbackTempFloat3Buffer(in ExProcessingList<int> processingList, JobHandle jobHandle)
        {
            return FeedbackTempFloat3Buffer(processingList.Buffer, processingList.Counter, jobHandle);
        }

        unsafe internal JobHandle FeedbackTempFloat3Buffer(in NativeArray<int> particleArray, in NativeReference<int> counter, JobHandle jobHandle)
        {
            var job = new FeedbackTempPosJob2()
            {
                particleIndexArray = particleArray,
                tempFloat3Buffer = tempFloat3Buffer,
                nextPosArray = nextPosArray.GetNativeArray(),
            };
            jobHandle = job.Schedule((int*)counter.GetUnsafePtrWithoutChecks(), 32, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct FeedbackTempPosJob2 : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> particleIndexArray;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> tempFloat3Buffer;

            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;

            public void Execute(int index)
            {
                int pindex = particleIndexArray[index];
                nextPosArray[pindex] = tempFloat3Buffer[pindex];
            }
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"========== Simulation Manager ==========");
            if (IsValid() == false)
            {
                sb.AppendLine($"Simulation Manager. Invalid");
            }
            else
            {
                sb.AppendLine($"Simulation Manager. Particle:{ParticleCount}");
                sb.AppendLine($"  -teamIdArray:{teamIdArray.ToSummary()}");
                sb.AppendLine($"  -nextPosArray:{nextPosArray.ToSummary()}");
                sb.AppendLine($"  -oldPosArray:{oldPosArray.ToSummary()}");
                sb.AppendLine($"  -oldRotArray:{oldRotArray.ToSummary()}");
                sb.AppendLine($"  -basePosArray:{basePosArray.ToSummary()}");
                sb.AppendLine($"  -baseRotArray:{baseRotArray.ToSummary()}");
                sb.AppendLine($"  -oldPositionArray:{oldPositionArray.ToSummary()}");
                sb.AppendLine($"  -oldRotationArray:{oldRotationArray.ToSummary()}");
                sb.AppendLine($"  -velocityPosArray:{velocityPosArray.ToSummary()}");
                sb.AppendLine($"  -dispPosArray:{dispPosArray.ToSummary()}");
                sb.AppendLine($"  -velocityArray:{velocityArray.ToSummary()}");
                sb.AppendLine($"  -realVelocityArray:{realVelocityArray.ToSummary()}");
                sb.AppendLine($"  -frictionArray:{frictionArray.ToSummary()}");
                sb.AppendLine($"  -staticFrictionArray:{staticFrictionArray.ToSummary()}");
                sb.AppendLine($"  -collisionNormalArray:{collisionNormalArray.ToSummary()}");

                // 制約
                sb.Append(distanceConstraint.ToString());
                sb.Append(bendingConstraint.ToString());
                sb.Append(angleConstraint.ToString());
                sb.Append(inertiaConstraint.ToString());
                sb.Append(colliderCollisionConstraint.ToString());
                sb.Append(selfCollisionConstraint.ToString());

                // 汎用バッファ
                sb.AppendLine($"[Step Buffer]");
                sb.AppendLine($"  -processingStepParticle:{processingStepParticle}");
                sb.AppendLine($"  -processingStepTriangleBending:{processingStepTriangleBending}");
                sb.AppendLine($"  -processingStepEdgeCollision:{processingStepEdgeCollision}");
                sb.AppendLine($"  -processingStepCollider:{processingStepCollider}");
                sb.AppendLine($"  -processingStepBaseLine:{processingStepBaseLine}");
                sb.AppendLine($"  -processingStepMotionParticle:{processingStepMotionParticle}");
                sb.AppendLine($"  -processingSelfParticle:{processingSelfParticle}");
                sb.AppendLine($"  -processingSelfPointTriangle:{processingSelfPointTriangle}");
                sb.AppendLine($"  -processingSelfEdgeEdge:{processingSelfEdgeEdge}");
                sb.AppendLine($"  -processingSelfTrianglePoint:{processingSelfTrianglePoint}");
                sb.AppendLine($"[Buffer]");
                sb.AppendLine($"  -tempFloat3Buffer:{(tempFloat3Buffer.IsCreated ? tempFloat3Buffer.Length : 0)}");
                sb.AppendLine($"  -countArray:{(countArray.IsCreated ? countArray.Length : 0)}");
                sb.AppendLine($"  -sumArray:{(sumArray.IsCreated ? sumArray.Length : 0)}");
                sb.AppendLine($"  -stepBasicPositionBuffer:{(stepBasicPositionBuffer.IsCreated ? stepBasicPositionBuffer.Length : 0)}");
                sb.AppendLine($"  -stepBasicRotationBuffer:{(stepBasicRotationBuffer.IsCreated ? stepBasicRotationBuffer.Length : 0)}");

                sb.AppendLine();
            }
            sb.AppendLine();
            Debug.Log(sb.ToString());
            allsb.Append(sb);
        }
    }
}
