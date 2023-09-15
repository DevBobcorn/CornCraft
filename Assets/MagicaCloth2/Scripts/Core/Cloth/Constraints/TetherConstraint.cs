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
    /// <summary>
    /// 最大距離制約
    /// 移動パーティクルが移動できる距離を自身のルートパーティクルとの距離から制限する
    /// </summary>
    public class TetherConstraint : IDisposable
    {
        [System.Serializable]
        public class SerializeData : IDataValidate
        {
            /// <summary>
            /// Maximum shrink limit (0.0 ~ 1.0).
            /// 0.0=do not shrink.
            /// 最大縮小限界(0.0 ~ 1.0)
            /// 0.0=縮小しない
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float distanceCompression;

            public SerializeData()
            {
                distanceCompression = 0.9f;
            }

            public void DataValidate()
            {
                distanceCompression = Mathf.Clamp(distanceCompression, 0.0f, 1.0f);
            }

            public SerializeData Clone()
            {
                return new SerializeData()
                {
                    distanceCompression = distanceCompression,
                };
            }
        }

        public struct TetherConstraintParams
        {
            //public float stiffness;

            /// <summary>
            /// 最大縮小割合(0.0 ~ 1.0)
            /// 0.0=縮小しない
            /// </summary>
            public float compressionLimit;

            /// <summary>
            /// 最大拡大割合(0.0 ~ 1.0)
            /// 0.0=拡大しない
            /// </summary>
            public float stretchLimit;

            /// <summary>
            /// stiffnessのフェード範囲(0.0 ~ 1.0)
            /// </summary>
            //public float stiffnessWidth;

            /// <summary>
            /// 速度減衰
            /// </summary>
            //public float velocityAttenuation;

            public void Convert(SerializeData sdata)
            {
                //stiffness = Define.System.TetherStiffness;
                compressionLimit = sdata.distanceCompression;
                stretchLimit = Define.System.TetherStretchLimit;
                //stiffnessWidth = Define.System.TetherStiffnessWidth;
                //velocityAttenuation = Define.System.TehterVelocityAttenuation;
            }
        }

        public void Dispose()
        {
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

            var job = new TethreConstraintJob()
            {
                stepParticleIndexArray = sm.processingStepParticle.Buffer,

                teamDataArray = tm.teamDataArray.GetNativeArray(),
                parameterArray = tm.parameterArray.GetNativeArray(),
                centerDataArray = tm.centerDataArray.GetNativeArray(),

                attributes = vm.attributes.GetNativeArray(),
                vertexDepths = vm.vertexDepths.GetNativeArray(),
                vertexRootIndices = vm.vertexRootIndices.GetNativeArray(),

                teamIdArray = sm.teamIdArray.GetNativeArray(),
                nextPosArray = sm.nextPosArray.GetNativeArray(),
                velocityPosArray = sm.velocityPosArray.GetNativeArray(),
                frictionArray = sm.frictionArray.GetNativeArray(),

                stepBasicPositionBuffer = sm.stepBasicPositionBuffer,
            };
            jobHandle = job.Schedule(sm.processingStepParticle.GetJobSchedulePtr(), 32, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct TethreConstraintJob : IJobParallelForDefer
        {
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
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexRootIndices;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> velocityPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionArray;

            // buffer
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> stepBasicPositionBuffer;

            // パーティクルごと
            public void Execute(int index)
            {
                // pindexのチームは有効であることが保証されている
                int pindex = stepParticleIndexArray[index];

                int teamId = teamIdArray[pindex];
                var tdata = teamDataArray[teamId];
                var param = parameterArray[teamId].tetherConstraint;
                //if (param.stiffness < 1e-06f)
                //    return;

                int p_start = tdata.particleChunk.startIndex;
                int l_index = pindex - p_start;
                int v_start = tdata.proxyCommonChunk.startIndex;
                int vindex = v_start + l_index;

                var attr = attributes[vindex];
                if (attr.IsMove() == false)
                    return;

                int rootIndex = vertexRootIndices[vindex];
                if (rootIndex < 0)
                    return;

                //Debug.Log($"Tether [{pindex}] root:{rootIndex + p_start}");

                var nextPos = nextPosArray[pindex];
                var rootPos = nextPosArray[rootIndex + p_start];
                float depth = vertexDepths[vindex];
                float friction = frictionArray[pindex];
                //float invMass = MathUtility.CalcInverseMass(friction);

                // 現在のベクトル
                float3 v = rootPos - nextPos;

                // 現在の長さ
                float distance = math.length(v);

                // 距離がほぼ０ならば処理をスキップする（エラーの回避）
                if (distance < Define.System.Epsilon)
                    return;

                // 復元距離
                // フラグにより初期姿勢かアニメーション後姿勢かを切り替える
                float3 calcPos = stepBasicPositionBuffer[pindex];
                float3 calcRootPos = stepBasicPositionBuffer[rootIndex + p_start];
                float calcDistance = math.distance(calcPos, calcRootPos);

                //Debug.Log($"[{pindex}] calcPos:{calcPos}, calcRootPos:{calcRootPos}, calcDistance:{calcDistance}");

                // 初期位置がまったく同じ状況を考慮
                if (calcDistance == 0.0f)
                    return;

                // 現在の伸縮割合
                //Develop.Assert(calcDistance > 0.0f);
                float ratio = distance / calcDistance;
#if true
                // 距離が範囲内なら伸縮しない
                float dist = 0;
                float stiffness;
                float attn;
                float compressionLimit = 1.0f - param.compressionLimit;
                float stretchLimit = 1.0f + param.stretchLimit;
                //float widthRatio = math.max(param.stiffnessWidth, 0.001f); // 0.2?
                //float widthRatio = 0.1f; // 0.2?
                if (ratio < compressionLimit)
                {
                    // 縮んでいる場合は戻りを比較的緩やかにする。これは振動の防止につながる。
                    dist = distance - compressionLimit * calcDistance;
                    float t = math.saturate((compressionLimit - ratio) / Define.System.TetherStiffnessWidth);
                    stiffness = Define.System.TetherCompressionStiffness * t;
                    //stiffness = Define.System.TetherCompressionStiffness;
                    attn = Define.System.TetherCompressionVelocityAttenuation;
                }
                else if (ratio > stretchLimit)
                {
                    // 伸びている場合は戻りを急速に行う。その代わり速度影響は弱くする。
                    dist = distance - stretchLimit * calcDistance;
                    float t = math.saturate((ratio - stretchLimit) / Define.System.TetherStiffnessWidth);
                    stiffness = Define.System.TetherStretchStiffness * t;
                    //stiffness = Define.System.TetherStretchStiffness;
                    attn = Define.System.TetherStretchVelocityAttenuation;
                }
                else
                    return;

                // 移動量
                float3 add = (v / distance) * (dist * stiffness);
#endif

                // 摩擦による移動減衰
                //add *= invMass;

                // 位置
                var oldPos = nextPos;
                nextPos += add;
                nextPosArray[pindex] = nextPos;

                // 速度影響
                //float attn = param.velocityAttenuation;
                velocityPosArray[pindex] = velocityPosArray[pindex] + add * attn;
            }
        }
    }
}
