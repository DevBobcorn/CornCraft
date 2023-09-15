// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// コライダーによる衝突判定制約
    /// </summary>
    public class ColliderCollisionConstraint : IDisposable
    {
        /// <summary>
        /// Collision judgment mode.
        /// 衝突判定モード
        /// </summary>
        public enum Mode
        {
            None = 0,
            Point = 1,
            Edge = 2,
        }

        [System.Serializable]
        public class SerializeData : IDataValidate
        {
            /// <summary>
            /// Collision judgment mode.
            /// 衝突判定モード
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public Mode mode;

            /// <summary>
            /// Friction (0.0 ~ 1.0).
            /// Dynamic friction/stationary friction combined use.
            /// 摩擦(0.0 ~ 1.0)
            /// 動摩擦／静止摩擦兼用
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 0.3f)]
            public float friction;

            /// <summary>
            /// Collider list.
            /// コライダーリスト
            /// [OK] Runtime changes.
            /// [NG] Export/Import with Presets
            /// </summary>
            public List<ColliderComponent> colliderList = new List<ColliderComponent>();

            public SerializeData()
            {
                mode = Mode.Point;
                friction = 0.05f;
            }

            public void DataValidate()
            {
                friction = Mathf.Clamp(friction, 0.0f, 0.3f);
            }

            public SerializeData Clone()
            {
                return new SerializeData()
                {
                    mode = mode,
                    friction = friction,
                    colliderList = new List<ColliderComponent>(colliderList),
                };
            }

            public int ColliderLength => colliderList.Count;
        }

        public struct ColliderCollisionConstraintParams
        {
            /// <summary>
            /// 衝突判定モード
            /// </summary>
            public Mode mode;

            /// <summary>
            /// 動摩擦係数(0.0 ~ 1.0)
            /// 摩擦1.0に対するステップごとの接線方向の速度減速率
            /// </summary>
            public float dynamicFriction;

            /// <summary>
            /// 静止摩擦係数(0.0 ~ 1.0)
            /// 静止速度(m/s)
            /// </summary>
            public float staticFriction;

            public void Convert(SerializeData sdata)
            {
                mode = sdata.mode;
                // 動摩擦/静止摩擦は設定摩擦に係数を掛けたものを使用する
                dynamicFriction = sdata.friction * Define.System.ColliderCollisionDynamicFrictionRatio;
                staticFriction = sdata.friction * Define.System.ColliderCollisionStaticFrictionRatio;
            }
        }

        NativeArray<int> tempFrictionArray;
        NativeArray<int> tempNormalArray;

        //=========================================================================================
        public ColliderCollisionConstraint()
        {
        }


        public void Dispose()
        {
            tempFrictionArray.DisposeSafe();
            tempNormalArray.DisposeSafe();
        }

        /// <summary>
        /// 作業バッファ更新
        /// </summary>
        internal void WorkBufferUpdate()
        {
            int cnt = MagicaManager.Team.edgeColliderCollisionCount;
            if (cnt == 0)
                return;

            int pcnt = MagicaManager.Simulation.ParticleCount;
            tempFrictionArray.Resize(pcnt);
            tempNormalArray.Resize(pcnt * 3);
            //if (tempFrictionArray.IsCreated == false || tempFrictionArray.Length < pcnt)
            //{
            //    if (tempFrictionArray.IsCreated)
            //        tempFrictionArray.Dispose();
            //    tempFrictionArray = new NativeArray<int>(pcnt, Allocator.Persistent);
            //}
            //if (tempNormalArray.IsCreated == false || tempNormalArray.Length < pcnt * 3)
            //{
            //    if (tempNormalArray.IsCreated)
            //        tempNormalArray.Dispose();
            //    tempNormalArray = new NativeArray<int>(pcnt * 3, Allocator.Persistent);
            //}
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[ColliderCollisionConstraint]");
            sb.AppendLine($"  -tempFrictionArray:{(tempFrictionArray.IsCreated ? tempFrictionArray.Length : 0)}");
            sb.AppendLine($"  -tempNormalArray:{(tempNormalArray.IsCreated ? tempNormalArray.Length : 0)}");

            return sb.ToString();
        }

        //=========================================================================================
        /// <summary>
        /// 制約の解決
        /// </summary>
        /// <param name="clothBase"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        unsafe internal JobHandle SolverConstraint(JobHandle jobHandle)
        {
            var tm = MagicaManager.Team;
            var sm = MagicaManager.Simulation;
            var vm = MagicaManager.VMesh;
            var cm = MagicaManager.Collider;

            // Point
            var job = new PointColliderCollisionConstraintJob()
            {
                stepParticleIndexArray = sm.processingStepParticle.Buffer,

                teamDataArray = tm.teamDataArray.GetNativeArray(),
                parameterArray = tm.parameterArray.GetNativeArray(),

                attributes = vm.attributes.GetNativeArray(),
                vertexDepths = vm.vertexDepths.GetNativeArray(),

                teamIdArray = sm.teamIdArray.GetNativeArray(),
                nextPosArray = sm.nextPosArray.GetNativeArray(),
                frictionArray = sm.frictionArray.GetNativeArray(),
                collisionNormalArray = sm.collisionNormalArray.GetNativeArray(),
                velocityPosArray = sm.velocityPosArray.GetNativeArray(),

                colliderFlagArray = cm.flagArray.GetNativeArray(),
                colliderWorkDataArray = cm.workDataArray.GetNativeArray(),
            };
            jobHandle = job.Schedule(sm.processingStepParticle.GetJobSchedulePtr(), 32, jobHandle);

            // Edge
            if (tm.edgeColliderCollisionCount > 0)
            {
                var job2 = new EdgeColliderCollisionConstraintJob()
                {
                    stepEdgeCollisionIndexArray = sm.processingStepEdgeCollision.Buffer,

                    teamDataArray = tm.teamDataArray.GetNativeArray(),
                    parameterArray = tm.parameterArray.GetNativeArray(),

                    attributes = vm.attributes.GetNativeArray(),
                    vertexDepths = vm.vertexDepths.GetNativeArray(),
                    edgeTeamIdArray = vm.edgeTeamIdArray.GetNativeArray(),
                    edges = vm.edges.GetNativeArray(),

                    nextPosArray = sm.nextPosArray.GetNativeArray(),
                    frictionArray = sm.frictionArray.GetNativeArray(),
                    collisionNormalArray = sm.collisionNormalArray.GetNativeArray(),
                    velocityPosArray = sm.velocityPosArray.GetNativeArray(),

                    colliderFlagArray = cm.flagArray.GetNativeArray(),
                    colliderWorkDataArray = cm.workDataArray.GetNativeArray(),

                    countArray = sm.countArray,
                    sumArray = sm.sumArray,
                    tempFrictionArray = tempFrictionArray,
                    tempNormalArray = tempNormalArray,
                };
                jobHandle = job2.Schedule(sm.processingStepEdgeCollision.GetJobSchedulePtr(), 32, jobHandle);

                // 集計
                var job3 = new SolveEdgeBufferAndClearJob()
                {
                    jobParticleIndexList = sm.processingStepParticle.Buffer,

                    nextPosArray = sm.nextPosArray.GetNativeArray(),
                    frictionArray = sm.frictionArray.GetNativeArray(),
                    velocityPosArray = sm.velocityPosArray.GetNativeArray(),
                    collisionNormalArray = sm.collisionNormalArray.GetNativeArray(),

                    countArray = sm.countArray,
                    sumArray = sm.sumArray,
                    tempFrictionArray = tempFrictionArray,
                    tempNormalArray = tempNormalArray,
                };
                jobHandle = job3.Schedule(sm.processingStepParticle.GetJobSchedulePtr(), 32, jobHandle);
            }

            return jobHandle;
        }

        //=========================================================================================
        /// <summary>
        /// Pointコライダー衝突判定
        /// </summary>
        [BurstCompile]
        struct PointColliderCollisionConstraintJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> stepParticleIndexArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> vertexDepths;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float> frictionArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> collisionNormalArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> velocityPosArray;

            // collider
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> colliderFlagArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ColliderManager.WorkData> colliderWorkDataArray;

            // ステップ実行パーティクルごと
            public void Execute(int index)
            {
                // このパーティクルは有効であることが保証されている
                int pindex = stepParticleIndexArray[index];
                int teamId = teamIdArray[pindex];
                var tdata = teamDataArray[teamId];
                if (tdata.colliderCount == 0)
                    return;

                // パラメータ
                var param = parameterArray[teamId];

                // モード判定
                var mode = param.colliderCollisionConstraint.mode;
                if (mode != Mode.Point)
                    return;

                // パーティクル情報
                var nextPos = nextPosArray[pindex];
                int l_index = pindex - tdata.particleChunk.startIndex;
                int vindex = tdata.proxyCommonChunk.startIndex + l_index;
                var attr = attributes[vindex];
                if (attr.IsMove() == false)
                    return;
                float depth = vertexDepths[vindex];

                // パーティクル半径
                float radius = math.max(param.radiusCurveData.EvaluateCurve(depth), 0.0001f); // safe;

                // チームスケール倍率
                radius *= tdata.scaleRatio;

                // コライダーとの距離
                float mindist = float.MaxValue;

                // 接触コライダー情報
                int collisionColliderId = -1;
                float3 collisionNormal = 0;
                float3 n = 0;

                // パーティクル押し出し情報
                float3 addPos = 0;
                int addCnt = 0;
                float3 addN = 0;

                // 接触判定を行うコライダーからの最大距離(collisionFrictionRange)
                // パーティクルサイズから算出する
                float cfr = radius * 1.0f; // 1.0f?

                // パーティクルAABB
                var aabb = new AABB(nextPos - radius, nextPos + radius);
                aabb.Expand(cfr);

                // チーム内のコライダーをループ
                int cindex = tdata.colliderChunk.startIndex;
                int ccnt = tdata.colliderCount;
                for (int i = 0; i < ccnt; i++, cindex++)
                {
                    var cflag = colliderFlagArray[cindex];
                    if (cflag.IsSet(ColliderManager.Flag_Valid) == false)
                        continue;
                    if (cflag.IsSet(ColliderManager.Flag_Enable) == false)
                        continue;

                    var ctype = DataUtility.GetColliderType(cflag);
                    var cwork = colliderWorkDataArray[cindex];
                    float dist = 100.0f;
                    float3 _nextPos = nextPos;
                    switch (ctype)
                    {
                        case ColliderManager.ColliderType.Sphere:
                            dist = PointSphereColliderDetection(ref _nextPos, radius, aabb, cwork, out n);
                            break;
                        case ColliderManager.ColliderType.CapsuleX_Center:
                        case ColliderManager.ColliderType.CapsuleY_Center:
                        case ColliderManager.ColliderType.CapsuleZ_Center:
                        case ColliderManager.ColliderType.CapsuleX_Start:
                        case ColliderManager.ColliderType.CapsuleY_Start:
                        case ColliderManager.ColliderType.CapsuleZ_Start:
                            dist = PointCapsuleColliderDetection(ref _nextPos, radius, aabb, cwork, out n);
                            break;
                        case ColliderManager.ColliderType.Plane:
                            dist = PointPlaneColliderDetction(ref _nextPos, radius, cwork, out n);
                            break;
                        default:
                            Debug.LogError($"unknown collider type:{ctype}");
                            break;
                    }

                    // 明確な接触と押し出しあり
                    if (dist <= 0.0f)
                    {
                        // 押し出されたベクトルと接触法線をすべて加算する
                        addPos += (_nextPos - nextPos);
                        addN += n;
                        addCnt++;
                    }

                    // コライダーに一定距離近づいている場合（動摩擦／静止摩擦が影響する）
                    if (dist <= cfr)
                    {
                        // 接触法線をすべて加算し、またコライダーまでの最近距離を記録する
                        collisionColliderId = cindex;
                        collisionNormal += n; // すべて加算する
                        mindist = math.min(mindist, dist);
                    }
                }

                // 最終位置
                // 平均化する
                if (addCnt > 0)
                {
                    // 合成された接触法線の長さに比例してパーティクルの移動を制限する
                    // これにより２つのコライダーに挟まれたパーティクルが暴れなくなる
                    addN /= addCnt;
                    float len = math.length(addN);
                    if (len < Define.System.Epsilon)
                    {
                        addPos = 0;
                    }
                    else
                    {
                        float t = math.min(len, 1.0f);
                        addPos /= addCnt;
                        nextPos += addPos * t;
                    }
                }

                // 摩擦係数(friction)計算
                if (collisionColliderId >= 0 && cfr > 0.0f && math.lengthsq(collisionNormal) > 1e-06f)
                {
                    // コライダーからの距離により変化(0.0～接地面1.0)
                    //Develop.Assert(cfr > 0.0f);
                    var friction = 1.0f - math.saturate(mindist / cfr);
                    frictionArray[pindex] = math.max(friction, frictionArray[pindex]); // 大きい方

                    // 摩擦用接触法線平均化
                    //Develop.Assert(math.length(collisionNormal) > 0.0f);
                    collisionNormal = math.normalize(collisionNormal);
                }
                collisionNormalArray[pindex] = collisionNormal;
                // todo:一応コライダーIDを記録しているが現在未使用!
                //colliderIdArray[pindex] = collisionColliderId + 1; // +1するので注意！

                // 書き戻し
                nextPosArray[pindex] = nextPos;

                // 速度影響
                //if (addCnt > 0)
                //{
                //    float attn = param.colliderCollisionConstraint.colliderVelocityAttenuation;
                //    velocityPosArray[pindex] = velocityPosArray[pindex] + addPos * attn;
                //}
            }

            /// <summary>
            /// Point球衝突判定
            /// </summary>
            /// <param name="nextpos"></param>
            /// <param name="pos"></param>
            /// <param name="radius"></param>
            /// <param name="cindex"></param>
            /// <param name="friction"></param>
            /// <returns></returns>
            float PointSphereColliderDetection(ref float3 nextpos, float radius, in AABB aabb, in ColliderManager.WorkData cwork, out float3 normal)
            {
                // ★たとえ接触していなくともコライダーまでの距離と法線を返さなければならない！
                normal = 0;

                //=========================================================
                // AABB判定
                //=========================================================
                if (aabb.Overlaps(cwork.aabb) == false)
                    return float.MaxValue;

                //=========================================================
                // 衝突解決
                //=========================================================
                float3 coldpos = cwork.oldPos.c0;
                float3 cpos = cwork.nextPos.c0;
                float cradius = cwork.radius.x;

                // 移動前のコライダーに対するローカル位置から移動後コライダーの押し出し平面を求める
                float3 c, n, v;
                v = nextpos - coldpos;
                Develop.Assert(math.length(v) > 0.0f);
                n = math.normalize(v);
                c = cpos + n * (cradius + radius);

                // 衝突法線
                normal = n;

                // c = 平面位置
                // n = 平面方向
                // 平面衝突判定と押し出し
                return MathUtility.IntersectPointPlaneDist(c, n, nextpos, out nextpos);
            }

            /// <summary>
            /// Point平面衝突判定（無限平面）
            /// </summary>
            /// <param name="nextpos"></param>
            /// <param name="radius"></param>
            /// <param name="cindex"></param>
            /// <param name="normal"></param>
            /// <returns></returns>
            float PointPlaneColliderDetction(ref float3 nextpos, float radius, in ColliderManager.WorkData cwork, out float3 normal)
            {
                // ★たとえ接触していなくともコライダーまでの距離と法線を返さなければならない！

                // コライダー情報
                var cpos = cwork.nextPos.c0;
                var n = cwork.oldPos.c0; // ここに押し出し法線

                // 衝突法線
                normal = n;

                // c = 平面位置（パーティクル半径分オフセット）
                // n = 平面方向
                // 平面衝突判定と押し出し
                // 平面との距離を返す（押し出しの場合は0.0）
                return MathUtility.IntersectPointPlaneDist(cpos + n * radius, n, nextpos, out nextpos);
            }

            /// <summary>
            /// Pointカプセル衝突判定
            /// </summary>
            /// <param name="nextpos"></param>
            /// <param name="pos"></param>
            /// <param name="radius"></param>
            /// <param name="cindex"></param>
            /// <param name="dir"></param>
            /// <param name="friction"></param>
            /// <returns></returns>
            float PointCapsuleColliderDetection(ref float3 nextpos, float radius, in AABB aabb, in ColliderManager.WorkData cwork, out float3 normal)
            {
                // ★たとえ接触していなくともコライダーまでの距離と法線を返さなければならない！
                normal = 0;

                //=========================================================
                // AABB判定
                //=========================================================
                if (aabb.Overlaps(cwork.aabb) == false)
                    return float.MaxValue;

                // コライダー情報
                float3 soldpos = cwork.oldPos.c0;
                float3 eoldpos = cwork.oldPos.c1;
                float3 spos = cwork.nextPos.c0;
                float3 epos = cwork.nextPos.c1;
                float sr = cwork.radius.x;
                float er = cwork.radius.y;

                //=========================================================
                // 衝突解決
                //=========================================================
                // 移動前のコライダー位置から押し出し平面を割り出す
                float t = MathUtility.ClosestPtPointSegmentRatio(nextpos, soldpos, eoldpos);
                float r = math.lerp(sr, er, t);
                float3 d = math.lerp(soldpos, eoldpos, t);
                float3 v = nextpos - d;

                // 移動前コライダーのローカルベクトル
                float3 lv = math.mul(cwork.inverseOldRot, v);

                // 移動後コライダーに変換
                d = math.lerp(spos, epos, t);
                v = math.mul(cwork.rot, lv);
                Develop.Assert(math.length(v) > 0.0f);
                float3 n = math.normalize(v);
                float3 c = d + n * (r + radius);

                // 衝突法線
                normal = n;

                // c = 平面位置
                // n = 平面方向
                // 平面衝突判定と押し出し
                return MathUtility.IntersectPointPlaneDist(c, n, nextpos, out nextpos);
            }
        }

        //=========================================================================================
        /// <summary>
        /// Edgeコライダー衝突判定
        /// </summary>
        [BurstCompile]
        unsafe struct EdgeColliderCollisionConstraintJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> stepEdgeCollisionIndexArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> vertexDepths;
            [Unity.Collections.ReadOnly]
            public NativeArray<short> edgeTeamIdArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<int2> edges;

            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float> frictionArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> collisionNormalArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> velocityPosArray;

            // collider
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> colliderFlagArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ColliderManager.WorkData> colliderWorkDataArray;

            // output
            [NativeDisableParallelForRestriction]
            public NativeArray<int> countArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> sumArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> tempFrictionArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> tempNormalArray;

            // ステップ実行エッジごと
            public void Execute(int index)
            {
                // このエッジは有効であることが保証されている
                int eindex = stepEdgeCollisionIndexArray[index];
                int teamId = edgeTeamIdArray[eindex];
                var tdata = teamDataArray[teamId];
                if (tdata.colliderCount == 0)
                    return;

                // パラメータ
                var param = parameterArray[teamId];

                // モード判定
                var mode = param.colliderCollisionConstraint.mode;
                if (mode != Mode.Edge)
                    return;

                // エッジ情報
                int vstart = tdata.proxyCommonChunk.startIndex;
                int2 edge = edges[eindex];
                int2 vE = edge + vstart;
                var attrE0 = attributes[vE.x];
                var attrE1 = attributes[vE.y];
                // 両方とも固定なら不要
                if (attrE0.IsMove() == false && attrE1.IsMove() == false)
                    return;
                int pstart = tdata.particleChunk.startIndex;
                int2 pE = edge + pstart;
                float3x2 nextPosE = new float3x2(nextPosArray[pE.x], nextPosArray[pE.y]);
                float2 depthE = new float2(vertexDepths[vE.x], vertexDepths[vE.y]);
                float2 radiusE = new float2(param.radiusCurveData.EvaluateCurve(depthE.x), param.radiusCurveData.EvaluateCurve(depthE.y));

                // チームスケール倍率
                radiusE *= tdata.scaleRatio;

                // 接触判定を行うコライダーからの最大距離
                // パーティクルサイズから算出する
                float cfr = (radiusE.x + radiusE.y) * 0.5f * 1.0f; // 1.0f?

                // 接触コライダー情報
                float mindist = float.MaxValue;
                int collisionColliderId = -1;
                float3 collisionNormal = 0;
                float3 n = 0;

                // 書き込みポインタ
                int* cntPt = (int*)countArray.GetUnsafePtr();
                int* sumPt = (int*)sumArray.GetUnsafePtr();
                int* frictionPt = (int*)tempFrictionArray.GetUnsafePtr();
                int* normalPt = (int*)tempNormalArray.GetUnsafePtr();

                // エッジAABB
                var aabbE = new AABB(nextPosE.c0 - radiusE.x, nextPosE.c0 + radiusE.x);
                var aabbE1 = new AABB(nextPosE.c1 - radiusE.y, nextPosE.c1 + radiusE.y);
                aabbE.Encapsulate(aabbE1);
                aabbE.Expand(cfr);

                // パーティクル押し出し情報
                float3x2 addPos = 0;
                int addCnt = 0;
                float3 addN = 0;

                // チーム内のコライダーをループ
                int cindex = tdata.colliderChunk.startIndex;
                int ccnt = tdata.colliderCount;
                for (int i = 0; i < ccnt; i++, cindex++)
                {
                    var cflag = colliderFlagArray[cindex];
                    if (cflag.IsSet(ColliderManager.Flag_Valid) == false)
                        continue;
                    if (cflag.IsSet(ColliderManager.Flag_Enable) == false)
                        continue;

                    var ctype = DataUtility.GetColliderType(cflag);
                    var cwork = colliderWorkDataArray[cindex];
                    float dist = 100.0f;
                    float3x2 _nextPos = nextPosE;
                    switch (ctype)
                    {
                        case ColliderManager.ColliderType.Sphere:
                            dist = EdgeSphereColliderDetection(ref _nextPos, radiusE, aabbE, cfr, cwork, out n);
                            break;
                        case ColliderManager.ColliderType.CapsuleX_Center:
                        case ColliderManager.ColliderType.CapsuleY_Center:
                        case ColliderManager.ColliderType.CapsuleZ_Center:
                        case ColliderManager.ColliderType.CapsuleX_Start:
                        case ColliderManager.ColliderType.CapsuleY_Start:
                        case ColliderManager.ColliderType.CapsuleZ_Start:
                            dist = EdgeCapsuleColliderDetection(ref _nextPos, radiusE, aabbE, cfr, cwork, out n);
                            break;
                        case ColliderManager.ColliderType.Plane:
                            dist = EdgePlaneColliderDetection(ref _nextPos, radiusE, cwork, out n);
                            break;
                        default:
                            Debug.LogError($"Unknown collider type:{ctype}");
                            break;
                    }

                    // 明確な接触と押し出しあり
                    if (dist <= 0.0f)
                    {
                        // 押し出されたベクトルと接触法線をすべて加算する
                        addPos += (_nextPos - nextPosE);
                        addN += n;
                        addCnt++;
                    }

                    // コライダーに一定距離近づいている場合（動摩擦／静止摩擦が影響する）
                    if (dist <= cfr)
                    {
                        // 接触法線をすべて加算し、またコライダーまでの最近距離を記録する
                        collisionColliderId = cindex;
                        collisionNormal += n; // すべて加算する
                        mindist = math.min(mindist, dist);
                    }
                }

                // 最終位置
                // 平均化する
                if (addCnt > 0)
                {
                    // 合成された接触法線の長さに比例してパーティクルの移動を制限する
                    // これにより２つのコライダーに挟まれたパーティクルが暴れなくなる
                    addN /= addCnt;
                    float len = math.length(addN);
                    if (len > Define.System.Epsilon)
                    {
                        float t = math.min(len, 1.0f);
                        addPos /= addCnt;
                        addPos *= t;

                        // 書き戻し
                        InterlockUtility.AddFloat3(pE.x, addPos.c0, cntPt, sumPt);
                        InterlockUtility.AddFloat3(pE.y, addPos.c1, cntPt, sumPt);
                    }
                }

                // 摩擦係数(friction)集計
                if (collisionColliderId >= 0 && cfr > 0.0f && math.lengthsq(collisionNormal) > 1e-06f)
                {
                    // コライダーからの距離により変化(0.0～接地面1.0)
                    //Develop.Assert(cfr > 0.0f);
                    var friction = 1.0f - math.saturate(mindist / cfr);

                    // 大きい場合のみ上書き
                    InterlockUtility.Max(pE.x, friction, frictionPt);
                    InterlockUtility.Max(pE.y, friction, frictionPt);

                    // 摩擦用接触法線平均化
                    //Develop.Assert(math.length(collisionNormal) > 0.0f);
                    collisionNormal = math.normalize(collisionNormal);

                    // 接触法線集計（すべて加算する）
                    InterlockUtility.AddFloat3(pE.x, collisionNormal, normalPt);
                    InterlockUtility.AddFloat3(pE.y, collisionNormal, normalPt);
                }
            }

            float EdgeSphereColliderDetection(ref float3x2 nextPosE, in float2 radiusE, in AABB aabbE, float cfr, in ColliderManager.WorkData cwork, out float3 normal)
            {
                // ★たとえ接触していなくともコライダーまでの距離と法線を返さなければならない！
                normal = 0;

                //=========================================================
                // AABB判定
                //=========================================================
                if (aabbE.Overlaps(cwork.aabb) == false)
                    return float.MaxValue;

                // コライダー情報
                float3 coldpos = cwork.oldPos.c0;
                float3 cpos = cwork.nextPos.c0;
                float cradius = cwork.radius.x;

                //=========================================================
                // 衝突判定
                //=========================================================
                // 移動前球に対する線分の最近接点
                float s;
                s = MathUtility.ClosestPtPointSegmentRatio(coldpos, nextPosE.c0, nextPosE.c1);
                float3 c = math.lerp(nextPosE.c0, nextPosE.c1, s);

                // 最近接点の距離
                var v = c - coldpos;
                float clen = math.length(v);
                if (clen < 1e-09f)
                    return float.MaxValue;

                // 押し出し法線
                float3 n = v / clen;
                normal = n;

                // 変位
                float3 db = cpos - coldpos;

                // 変位をnに投影して距離チェック
                float l1 = math.dot(n, db);
                float l = clen - l1;

                // 厚み
                float rA = math.lerp(radiusE.x, radiusE.y, s);
                float rB = cradius;
                float thickness = rA + rB;

                // 接触判定
                if (l > (thickness + cfr))
                    return float.MaxValue;

                //=========================================================
                // 衝突解決
                //=========================================================
                // 接触法線に現在の距離を投影させる
                v = c - cpos;
                l = math.dot(n, v);
                if (l > thickness)
                {
                    // 接触なし
                    // 接触面までの距離を返す
                    return l - thickness;
                }

                // 離す距離
                float C = thickness - l;

                // エッジのみを引き離す
                //float b0 = 1.0f - t;
                //float b1 = t;
                float2 b = new float2(1.0f - s, s);

                //float3 grad0 = n * b0;
                //float3 grad1 = n * b1;
                float3x2 grad = new float3x2(n * b.x, n * b.y);

                //float S = b0 * b0 + b1 * b1;
                float S = math.dot(b, b);
                if (S == 0.0f)
                    return float.MaxValue;

                S = C / S;

                //float3 corr0 = S * grad0;
                //float3 corr1 = S * grad1;
                float3x2 corr = grad * S;

                //=========================================================
                // 反映
                //=========================================================
                nextPosE += corr;

                // 押し出し距離を返す
                return -C;
            }

            float EdgeCapsuleColliderDetection(ref float3x2 nextPosE, in float2 radiusE, in AABB aabbE, float cfr, in ColliderManager.WorkData cwork, out float3 normal)
            {
                // ★たとえ接触していなくともコライダーまでの距離と法線を返さなければならない！
                normal = 0;

                //=========================================================
                // AABB判定
                //=========================================================
                if (aabbE.Overlaps(cwork.aabb) == false)
                    return float.MaxValue;

                // コライダー情報
                float3 soldpos = cwork.oldPos.c0;
                float3 eoldpos = cwork.oldPos.c1;
                float3 spos = cwork.nextPos.c0;
                float3 epos = cwork.nextPos.c1;
                float sr = cwork.radius.x;
                float er = cwork.radius.y;

                //=========================================================
                // 衝突判定
                //=========================================================
                // 移動前の２つの線分の最近接点
                float s, t;
                float3 cA, cB;
                float csqlen = MathUtility.ClosestPtSegmentSegment(nextPosE.c0, nextPosE.c1, soldpos, eoldpos, out s, out t, out cA, out cB);
                float clen = math.sqrt(csqlen); // 最近接点の距離
                if (clen < 1e-09f)
                    return float.MaxValue;

                // 押出法線
                var v = cA - cB;
                Develop.Assert(math.length(v) > 0.0f);
                float3 n = math.normalize(v);
                normal = n;

                // 変位
                float3 dB0 = spos - soldpos;
                float3 dB1 = epos - eoldpos;


                // 最近接点での変位
                float3 db = math.lerp(dB0, dB1, t);

                // 変位da,dbをnに投影して距離チェック
                float l1 = math.dot(n, db);
                float l = clen - l1;

                // 厚み
                float rA = math.lerp(radiusE.x, radiusE.y, s);
                float rB = math.lerp(sr, er, t);
                float thickness = rA + rB;

                // 接触判定
                if (l > (thickness + cfr))
                    return float.MaxValue;

                //=========================================================
                // 衝突解決
                //=========================================================
                // 接触法線に現在の距離を投影させる
                var d = math.lerp(spos, epos, t);
                v = cA - d;
                l = math.dot(n, v);
                //Debug.Log($"l:{l}");
                if (l > thickness)
                {
                    // 接触なし
                    // 接触面までの距離を返す
                    return l - thickness;
                }

                // 離す距離
                float C = thickness - l;
                //Debug.Log($"C:{C}");

                // エッジのみを引き離す
                //float b0 = 1.0f - s;
                //float b1 = s;
                float2 b = new float2(1.0f - s, s);

                //float3 grad0 = n * b0;
                //float3 grad1 = n * b1;
                float3x2 grad = new float3x2(n * b.x, n * b.y);

                //float S = invMass0 * b0 * b0 + invMass1 * b1 * b1;
                float S = math.dot(b, b);
                if (S == 0.0f)
                    return float.MaxValue;

                S = C / S;

                //float3 corr0 = S * invMass0 * grad0;
                //float3 corr1 = S * invMass1 * grad1;
                float3x2 corr = grad * S;

                //=========================================================
                // 反映
                //=========================================================
                nextPosE += corr;

                // 押し出し距離を返す
                return -C;
            }

            float EdgePlaneColliderDetection(ref float3x2 nextPosE, in float2 radiusE, in ColliderManager.WorkData cwork, out float3 normal)
            {
                // ★たとえ接触していなくともコライダーまでの距離と法線を返さなければならない！

                // コライダー情報
                var cpos = cwork.nextPos.c0;
                var n = cwork.oldPos.c0; // ここに押し出し法線

                // 衝突法線
                normal = n;

                // c = 平面位置
                // n = 平面方向
                // 平面衝突判定と押し出し
                // 平面との距離を返す（押し出しの場合は0.0）
                float dist0 = MathUtility.IntersectPointPlaneDist(cpos + n * radiusE.x, n, nextPosE.c0, out nextPosE.c0);
                float dist1 = MathUtility.IntersectPointPlaneDist(cpos + n * radiusE.y, n, nextPosE.c1, out nextPosE.c1);

                return math.min(dist0, dist1);
            }
        }

        /// <summary>
        /// エッジコライダーコリジョン結果の集計
        /// </summary>
        [BurstCompile]
        struct SolveEdgeBufferAndClearJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> jobParticleIndexList;

            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float> frictionArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> collisionNormalArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> velocityPosArray;

            // aggregate
            [NativeDisableParallelForRestriction]
            public NativeArray<int> countArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> sumArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> tempFrictionArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> tempNormalArray;

            // ステップ有効パーティクルごと
            public void Execute(int index)
            {
                int pindex = jobParticleIndexList[index];

                // nextpos
                int count = countArray[pindex];
                int dataIndex = pindex * 3;
                if (count > 0)
                {
                    float3 add = InterlockUtility.ReadAverageFloat3(pindex, countArray, sumArray);

                    // 書き出し
                    nextPosArray[pindex] = nextPosArray[pindex] + add;

                    // 速度影響
                    //float attn = param.colliderCollisionConstraint.colliderVelocityAttenuation;
                    //float attn = Define.System.ColliderCollisionVelocityAttenuation;
                    //velocityPosArray[pindex] = velocityPosArray[pindex] + add * attn;

                    // バッファクリア
                    countArray[pindex] = 0;
                    sumArray[dataIndex] = 0;
                    sumArray[dataIndex + 1] = 0;
                    sumArray[dataIndex + 2] = 0;
                }

                // friction
                float f = InterlockUtility.ReadFloat(pindex, tempFrictionArray);
                if (f > 0.0f && f > frictionArray[pindex])
                {
                    frictionArray[pindex] = f;
                    tempFrictionArray[pindex] = 0;
                }

                // collision normal
                float3 n = InterlockUtility.ReadFloat3(pindex, tempNormalArray);
                if (math.lengthsq(n) > 0.0f)
                {
                    n = math.normalize(n);
                    collisionNormalArray[pindex] = n;
                    tempNormalArray[dataIndex] = 0;
                    tempNormalArray[dataIndex + 1] = 0;
                    tempNormalArray[dataIndex + 2] = 0;
                }
            }
        }
    }
}
