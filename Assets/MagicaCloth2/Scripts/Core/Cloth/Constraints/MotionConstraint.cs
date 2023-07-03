// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public class MotionConstraint : IDisposable
    {
        [System.Serializable]
        public class SerializeData : IDataValidate
        {
            /// <summary>
            /// Whether or not to use maximum travel range
            /// 最大移動範囲
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public bool useMaxDistance;

            /// <summary>
            /// Maximum travel range.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CurveSerializeData maxDistance;

            /// <summary>
            /// Use of backstop.
            /// バックストップ使用の有無
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public bool useBackstop;

            /// <summary>
            /// Backstop sphere radius.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.1f, 10.0f)]
            public float backstopRadius;

            /// <summary>
            /// Distance from vertex to backstop sphere.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CurveSerializeData backstopDistance;

            /// <summary>
            /// repulsive force(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float stiffness;

            public SerializeData()
            {
                useMaxDistance = false;
                maxDistance = new CurveSerializeData(0.3f);
                useBackstop = false;
                backstopRadius = 10.0f;
                backstopDistance = new CurveSerializeData(0.0f);
                stiffness = 1.0f;
            }

            public void DataValidate()
            {
                maxDistance.DataValidate(0.0f, 5.0f);
                //maxDistanceOffset = Mathf.Clamp01(maxDistanceOffset);

                backstopRadius = Mathf.Clamp(backstopRadius, 0.0f, 10.0f);
                backstopDistance.DataValidate(0.0f, 1.0f);

                stiffness = Mathf.Clamp01(stiffness);
            }

            public SerializeData Clone()
            {
                return new SerializeData()
                {
                    useMaxDistance = useMaxDistance,
                    maxDistance = maxDistance.Clone(),
                    useBackstop = useBackstop,
                    backstopRadius = backstopRadius,
                    backstopDistance = backstopDistance.Clone(),
                    stiffness = stiffness,
                };
            }
        }

        public struct MotionConstraintParams
        {
            /// <summary>
            /// 最大移動範囲
            /// </summary>
            public bool useMaxDistance;
            public float4x4 maxDistanceCurveData;
            //public float maxDistanceOffset;

            /// <summary>
            /// バックストップ距離
            /// </summary>
            public bool useBackstop;
            public float backstopRadius;
            public float4x4 backstopDistanceCurveData;

            // stiffness
            public float stiffness;

            public void Convert(SerializeData sdata)
            {
                useMaxDistance = sdata.useMaxDistance;
                maxDistanceCurveData = sdata.maxDistance.ConvertFloatArray();
                //maxDistanceOffset = sdata.maxDistanceOffset;

                useBackstop = sdata.useBackstop;
                backstopRadius = sdata.backstopRadius;
                backstopDistanceCurveData = sdata.backstopDistance.ConvertFloatArray();

                stiffness = sdata.stiffness;
            }
        }

        public void Dispose()
        {
        }

        //=========================================================================================
        /// <summary>
        /// 制約の解決
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        unsafe internal JobHandle SolverConstraint(JobHandle jobHandle)
        {
            var tm = MagicaManager.Team;
            var sm = MagicaManager.Simulation;
            var vm = MagicaManager.VMesh;

            var job = new MotionConstraintJob()
            {
                stepParticleIndexArray = sm.processingStepMotionParticle.Buffer,

                teamDataArray = tm.teamDataArray.GetNativeArray(),
                parameterArray = tm.parameterArray.GetNativeArray(),

                attributes = vm.attributes.GetNativeArray(),
                vertexDepths = vm.vertexDepths.GetNativeArray(),

                teamIdArray = sm.teamIdArray.GetNativeArray(),
                basePosArray = sm.basePosArray.GetNativeArray(),
                baseRotArray = sm.baseRotArray.GetNativeArray(),
                nextPosArray = sm.nextPosArray.GetNativeArray(),
                velocityPosArray = sm.velocityPosArray.GetNativeArray(),
                frictionArray = sm.frictionArray.GetNativeArray(),
                collisionNormalArray = sm.collisionNormalArray.GetNativeArray(),
            };
            jobHandle = job.Schedule(sm.processingStepMotionParticle.GetJobSchedulePtr(), 32, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct MotionConstraintJob : IJobParallelForDefer
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
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> baseRotArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> velocityPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float> frictionArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> collisionNormalArray;


            public void Execute(int index)
            {
                // pindexのチームは有効であることが保証されている
                int pindex = stepParticleIndexArray[index];

                int teamId = teamIdArray[pindex];
                var tdata = teamDataArray[teamId];
                var param = parameterArray[teamId];
                var motionParam = param.motionConstraint;
                var normalAxis = param.normalAxis;
                if (motionParam.useMaxDistance == false && motionParam.useBackstop == false)
                    return;

                int p_start = tdata.particleChunk.startIndex;
                int l_index = pindex - p_start;
                int v_start = tdata.proxyCommonChunk.startIndex;
                int vindex = v_start + l_index;

                // 移動パーティクルのみ
                var attr = attributes[vindex];
                if (attr.IsMove() == false)
                    return;

                var nextPos = nextPosArray[pindex];
                var basePos = basePosArray[pindex];
                float depth = vertexDepths[vindex];

                // !MaxDistanceとBackstop制約は常にアニメーション姿勢(basePose)から計算されるので注意！
                // !そのためAnimationBlendRatioは影響しない。

                // stiffness
                float stiffness = motionParam.stiffness;

                // 適用頂点属性チェック
                if (attr.IsMotion())
                {
                    var opos = nextPos;

                    // パーティクル半径
                    float radius = math.max(param.radiusCurveData.EvaluateCurve(depth), 0.0001f); // safe
                    //radius *= tdata.scaleRatio;

                    // 摩擦影響距離
                    float cfr = radius * 1.0f;

                    // 深さは二次曲線にする(test)
                    depth = depth * depth;

                    //=========================================================
                    // axis
                    //=========================================================
                    var baseRot = baseRotArray[pindex];
                    float3 dir = math.up();
                    switch (normalAxis)
                    {
                        case ClothNormalAxis.Right:
                            dir = math.right();
                            break;
                        case ClothNormalAxis.Up:
                            dir = math.up();
                            break;
                        case ClothNormalAxis.Forward:
                            dir = math.forward();
                            break;
                        case ClothNormalAxis.InverseRight:
                            dir = -math.right();
                            break;
                        case ClothNormalAxis.InverseUp:
                            dir = -math.up();
                            break;
                        case ClothNormalAxis.InverseForward:
                            dir = -math.forward();
                            break;
                    }
                    dir = math.mul(baseRot, dir);

                    //=========================================================
                    // Max Distance
                    //=========================================================
                    if (motionParam.useMaxDistance)
                    {
                        float maxDistance = motionParam.maxDistanceCurveData.EvaluateCurve(depth);
                        //var cen = basePos + dir * (motionParam.maxDistanceOffset * maxDistance);
                        var cen = basePos;
                        var v = MathUtility.ClampVector(nextPos - cen, maxDistance);
                        nextPos = cen + v;
                    }

                    //=========================================================
                    // Backstop
                    //=========================================================
                    if (motionParam.useBackstop)
                    {
                        float backstopRadius = motionParam.backstopRadius;
                        float backstopDistance = motionParam.backstopDistanceCurveData.EvaluateCurve(depth);
                        if (backstopRadius > 0.0f)
                        {
                            // バックストップは法線逆方向
                            float3 cen = basePos + -dir * (backstopDistance + backstopRadius);
                            var v = nextPos - cen;
                            float len = math.length(v);
                            if (len > Define.System.Epsilon && len < (backstopRadius + cfr))
                            {
                                var n = v / len;
                                if (len < backstopRadius)
                                {
                                    nextPos = cen + n * backstopRadius;
                                }

#if false // 摩擦はあまり良くない気がするのでとりあえずOFF
                                // 摩擦
                                float friction = 1.0f - math.saturate((len - backstopRadius) / cfr);
                                float nowFriction = frictionArray[pindex];
                                if (friction > nowFriction)
                                {
                                    // 大きければ更新
                                    frictionArray[pindex] = friction;

                                    // 接触法線
                                    collisionNormalArray[pindex] = n;
                                }
#endif
                            }
                        }
                    }

                    //=========================================================
                    // Stiffness
                    //=========================================================
                    nextPos = math.lerp(opos, nextPos, stiffness);

                    //=========================================================
                    // 格納
                    //=========================================================
                    // 位置
                    nextPosArray[pindex] = nextPos;

                    // 速度影響
                    var add = nextPos - opos;
                    const float attn = 0.95f;
                    velocityPosArray[pindex] = velocityPosArray[pindex] + add * attn;
                }
            }
        }
    }
}
