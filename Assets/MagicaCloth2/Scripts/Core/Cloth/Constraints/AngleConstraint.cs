// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 角度復元/角度制限制約
    /// 内部処理がほぼ同じなため１つに統合
    /// </summary>
    public class AngleConstraint : IDisposable
    {
        /// <summary>
        /// angle restoration.
        /// 角度復元
        /// </summary>
        [System.Serializable]
        public class RestorationSerializeData : IDataValidate
        {
            /// <summary>
            /// Presence or absence of use.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public bool useAngleRestoration;

            /// <summary>
            /// resilience.
            /// 復元力
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CurveSerializeData stiffness;

            /// <summary>
            /// Velocity decay during restoration.
            /// 復元時の速度減衰
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float velocityAttenuation;

            /// <summary>
            /// Directional Attenuation of Gravity.
            /// Note that this attenuation occurs even if the gravity is 0!
            /// 復元の重力方向減衰
            /// この減衰は重力が０でも発生するので注意！
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float gravityFalloff;

            public RestorationSerializeData()
            {
                useAngleRestoration = true;
                stiffness = new CurveSerializeData(0.2f, 1.0f, 0.2f, true);
                velocityAttenuation = 0.8f;
                gravityFalloff = 0.0f;
            }

            public void DataValidate()
            {
                stiffness.DataValidate(0.0f, 1.0f);
                velocityAttenuation = Mathf.Clamp01(velocityAttenuation);
                gravityFalloff = Mathf.Clamp01(gravityFalloff);
            }

            public RestorationSerializeData Clone()
            {
                return new RestorationSerializeData()
                {
                    useAngleRestoration = useAngleRestoration,
                    stiffness = stiffness.Clone(),
                    velocityAttenuation = velocityAttenuation,
                    gravityFalloff = gravityFalloff,
                };
            }
        }

        /// <summary>
        /// angle limit.
        /// 角度制限
        /// </summary>
        [System.Serializable]
        public class LimitSerializeData : IDataValidate
        {
            /// <summary>
            /// Presence or absence of use.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public bool useAngleLimit;

            /// <summary>
            /// Limit angle (deg).
            /// 制限角度(deg)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CurveSerializeData limitAngle;

            /// <summary>
            /// Standard stiffness.
            /// 基準剛性
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float stiffness;

            public LimitSerializeData()
            {
                useAngleLimit = false;
                limitAngle = new CurveSerializeData(60.0f, 0.0f, 1.0f);
                stiffness = 1.0f;
            }

            public void DataValidate()
            {
                limitAngle.DataValidate(0.0f, 180.0f);
                stiffness = Mathf.Clamp01(stiffness);
            }

            public LimitSerializeData Clone()
            {
                return new LimitSerializeData()
                {
                    useAngleLimit = useAngleLimit,
                    limitAngle = limitAngle.Clone(),
                    stiffness = stiffness,
                };
            }
        }

        //=========================================================================================
        public struct AngleConstraintParams
        {
            public bool useAngleRestoration;

            /// <summary>
            /// 角度復元力
            /// </summary>
            public float4x4 restorationStiffness;

            /// <summary>
            /// 角度復元速度減衰
            /// </summary>
            public float restorationVelocityAttenuation;

            /// <summary>
            /// 角度復元の重力方向減衰
            /// </summary>
            public float restorationGravityFalloff;


            public bool useAngleLimit;

            /// <summary>
            /// 制限角度(deg)
            /// </summary>
            public float4x4 limitCurveData;

            /// <summary>
            /// 角度制限剛性
            /// </summary>
            public float limitstiffness;

            public void Convert(RestorationSerializeData restorationData, LimitSerializeData limitData)
            {
                useAngleRestoration = restorationData.useAngleRestoration;
                // Restoration Powerは設定値の20%とする.つまり1.0で旧0.2となる
                restorationStiffness = restorationData.stiffness.ConvertFloatArray() * 0.2f;
                restorationVelocityAttenuation = restorationData.velocityAttenuation;
                restorationGravityFalloff = restorationData.gravityFalloff;

                useAngleLimit = limitData.useAngleLimit;
                limitCurveData = limitData.limitAngle.ConvertFloatArray();
                limitstiffness = limitData.stiffness;
            }
        }

        //=========================================================================================
        NativeArray<float> lengthBuffer;
        NativeArray<float3> localPosBuffer;
        NativeArray<quaternion> localRotBuffer;
        NativeArray<quaternion> rotationBuffer;
        NativeArray<float3> restorationVectorBuffer;


        //=========================================================================================
        public AngleConstraint()
        {
        }

        public void Dispose()
        {
            lengthBuffer.DisposeSafe();
            localPosBuffer.DisposeSafe();
            localRotBuffer.DisposeSafe();
            rotationBuffer.DisposeSafe();
            restorationVectorBuffer.DisposeSafe();
        }

        internal void WorkBufferUpdate()
        {
            int pcnt = MagicaManager.Simulation.ParticleCount;
            lengthBuffer.Resize(pcnt, options: NativeArrayOptions.UninitializedMemory);
            localPosBuffer.Resize(pcnt, options: NativeArrayOptions.UninitializedMemory);
            localRotBuffer.Resize(pcnt, options: NativeArrayOptions.UninitializedMemory);
            rotationBuffer.Resize(pcnt, options: NativeArrayOptions.UninitializedMemory);
            restorationVectorBuffer.Resize(pcnt, options: NativeArrayOptions.UninitializedMemory);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[AngleConstraint]");
            sb.AppendLine($"  -lengthBuffer:{(lengthBuffer.IsCreated ? lengthBuffer.Length : 0)}");
            sb.AppendLine($"  -localPosBuffer:{(localPosBuffer.IsCreated ? localPosBuffer.Length : 0)}");
            sb.AppendLine($"  -localRotBuffer:{(localRotBuffer.IsCreated ? localRotBuffer.Length : 0)}");
            sb.AppendLine($"  -rotationBuffer:{(rotationBuffer.IsCreated ? rotationBuffer.Length : 0)}");
            sb.AppendLine($"  -restorationVectorBuffer:{(restorationVectorBuffer.IsCreated ? restorationVectorBuffer.Length : 0)}");

            return sb.ToString();
        }

        //=========================================================================================
        /// <summary>
        /// 制約の解決
        /// </summary>
        /// <param name="clothBase"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal unsafe JobHandle SolverConstraint(JobHandle jobHandle)
        {
            var tm = MagicaManager.Team;
            var sm = MagicaManager.Simulation;
            var vm = MagicaManager.VMesh;

            // 角度復元と角度制限を１つに統合したもの
            // 復元/制限ともにほぼMC1の移植。
            // 他のアルゴリズムを散々テストした結果、MC1の動きが一番映えるという結論に至る。
            // 微調整および堅牢性を上げるために反復回数を増やしている。
            var job = new AngleConstraintJob()
            {
                simulationPower = MagicaManager.Time.SimulationPower,

                stepBaseLineIndexArray = sm.processingStepBaseLine.Buffer,

                teamDataArray = tm.teamDataArray.GetNativeArray(),
                parameterArray = tm.parameterArray.GetNativeArray(),

                attributes = vm.attributes.GetNativeArray(),
                vertexDepths = vm.vertexDepths.GetNativeArray(),
                vertexParentIndices = vm.vertexParentIndices.GetNativeArray(),
                baseLineStartDataIndices = vm.baseLineStartDataIndices.GetNativeArray(),
                baseLineDataCounts = vm.baseLineDataCounts.GetNativeArray(),
                baseLineData = vm.baseLineData.GetNativeArray(),

                nextPosArray = sm.nextPosArray.GetNativeArray(),
                velocityPosArray = sm.velocityPosArray.GetNativeArray(),
                frictionArray = sm.frictionArray.GetNativeArray(),

                stepBasicPositionBuffer = sm.stepBasicPositionBuffer,
                stepBasicRotationBuffer = sm.stepBasicRotationBuffer,

                lengthBufferArray = lengthBuffer,
                localPosBufferArray = localPosBuffer,
                localRotBufferArray = localRotBuffer,
                rotationBufferArray = rotationBuffer,
                restorationVectorBufferArray = restorationVectorBuffer,
            };
            jobHandle = job.Schedule(sm.processingStepBaseLine.GetJobSchedulePtr(), 2, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct AngleConstraintJob : IJobParallelForDefer
        {
            public float4 simulationPower;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> stepBaseLineIndexArray;

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
            public NativeArray<int> vertexParentIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineStartDataIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineDataCounts;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineData;

            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> velocityPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionArray;

            // temp
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> stepBasicPositionBuffer;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> stepBasicRotationBuffer;
            [NativeDisableParallelForRestriction]
            public NativeArray<float> lengthBufferArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> localPosBufferArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<quaternion> localRotBufferArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<quaternion> rotationBufferArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> restorationVectorBufferArray;

            // ベースラインごと
            public void Execute(int index)
            {
                uint pack = (uint)stepBaseLineIndexArray[index];
                int teamId = DataUtility.Unpack32Hi(pack);
                int bindex = DataUtility.Unpack32Low(pack);

                // チームは有効であることが保証されている
                var tdata = teamDataArray[teamId];
                var param = parameterArray[teamId];
                var angleParam = param.angleConstraint;
                if (angleParam.useAngleLimit == false && angleParam.useAngleRestoration == false)
                    return;

                int d_start = tdata.baseLineDataChunk.startIndex;
                int p_start = tdata.particleChunk.startIndex;
                int v_start = tdata.proxyCommonChunk.startIndex;

                int start = baseLineStartDataIndices[bindex];
                int dcnt = baseLineDataCounts[bindex];

                bool useAngleLimit = angleParam.useAngleLimit;
                bool useAngleRestoration = angleParam.useAngleRestoration;

                // 剛性
                float limitStiffness = angleParam.limitstiffness;
                float restorationAttn = angleParam.restorationVelocityAttenuation;

                // 復元の重力減衰
                // !この減衰は重力０でも発生するので注意！
                float gravityFalloff = math.lerp(1.0f - angleParam.restorationGravityFalloff, 1.0f, tdata.gravityDot);
                //Debug.Log($"gravityFalloff:{gravityFalloff}");
                //float gravity = param.gravity;
                //float3 gravityVector = gravity > Define.System.Epsilon ? param.gravityDirection : 0;

                // バッファリング
                int dataIndex = start + d_start;
                for (int i = 0; i < dcnt; i++, dataIndex++)
                {
                    int l_index = baseLineData[dataIndex];
                    int pindex = p_start + l_index;
                    int vindex = v_start + l_index;

                    // 自身
                    var npos = nextPosArray[pindex];
                    var bpos = stepBasicPositionBuffer[pindex];
                    var brot = stepBasicRotationBuffer[pindex];

                    rotationBufferArray[pindex] = brot;

                    // 親
                    if (i > 0)
                    {
                        int p_l_index = vertexParentIndices[vindex];
                        int p_pindex = p_l_index + p_start;
                        var pnpos = nextPosArray[p_pindex];
                        var pbpos = stepBasicPositionBuffer[p_pindex];
                        var pbrot = stepBasicRotationBuffer[p_pindex];

                        if (useAngleLimit)
                        {
                            // 現在ベクトル長
                            float vlen = math.distance(npos, pnpos);

                            // 親からの基本姿勢
                            var bv = bpos - pbpos;
                            Develop.Assert(math.length(bv) > 0.0f);
                            var v = math.normalize(bv);
                            var ipq = math.inverse(pbrot);
                            float3 localPos = math.mul(ipq, v);
                            quaternion localRot = math.mul(ipq, brot);

                            lengthBufferArray[pindex] = vlen;
                            localPosBufferArray[pindex] = localPos;
                            localRotBufferArray[pindex] = localRot;
                        }

                        if (useAngleRestoration)
                        {
                            // 復元ベクトル
                            float3 rv = bpos - pbpos;
                            restorationVectorBufferArray[pindex] = rv;
                        }
                    }
                }

                // 反復
                // 角度制限は親と子の位置を徐々に修正していくため反復は必須。
                // 反復は多いほど堅牢性が増す。
                for (int k = 0; k < Define.System.AngleLimitIteration; k++)
                {
                    float iterationRatio = (float)k / (Define.System.AngleLimitIteration - 1); // 0.0 ~ 1.0

                    // 回転の中心点。
                    // 親に近い(値が小さい)ほど角度制限の効果が増すが酷い振動の温床ともなる。
                    // 中心点がちょうど真ん中(0.5)の場合は堅牢性が最大となり振動が発生しなくなるがそのかわり角度復元/制限の効果が弱くなる。
                    // そのため反復ごとに回転中心を徐々に親(0.0)から中間(0.5)に近づけることにより堅牢性と安定性を確保する
                    // この処理により振動が完全に無くなる訳では無いが許容範囲であるし何よりも角度復元/制限の効果が大幅に向上する
                    //float limitRotRatio = math.min(math.lerp(0.3f, 0.7f, iterationRatio), 0.5f);
                    //float limitRotRatio = math.min(math.lerp(0.4f, 0.7f, iterationRatio), 0.5f);
                    float limitRotRatio = 0.4f;
                    float restorationRotRatio = math.lerp(0.1f, 0.5f, iterationRatio);

                    dataIndex = start + d_start;
                    for (int i = 0; i < dcnt; i++, dataIndex++)
                    {
                        int l_index = baseLineData[dataIndex];
                        int pindex = p_start + l_index;
                        int vindex = v_start + l_index;

                        //Debug.Log($"pindex:{pindex}");

                        // 子
                        float3 cpos = nextPosArray[pindex];
                        float cdepth = vertexDepths[vindex];
                        var cattr = attributes[vindex];
                        var cInvMass = MathUtility.CalcInverseMass(frictionArray[pindex]);

                        // 子が固定ならばスキップ
                        if (cattr.IsMove() == false)
                            continue;

                        // 親
                        int p_pindex = vertexParentIndices[vindex] + p_start;
                        int p_vindex = vertexParentIndices[vindex] + v_start;
                        float3 ppos = nextPosArray[p_pindex];
                        //float pdepth = vertexDepths[p_vindex];
                        var pattr = attributes[p_vindex];
                        var pInvMass = MathUtility.CalcInverseMass(frictionArray[p_pindex]);

                        //=====================================================
                        // Angle Limit
                        //=====================================================
                        if (useAngleLimit)
                        {
                            // 親からの基準姿勢
                            quaternion prot = rotationBufferArray[p_pindex];
                            float3 localPos = localPosBufferArray[pindex];
                            quaternion localRot = localRotBufferArray[pindex];

                            // 現在のベクトル
                            float3 v = cpos - ppos;

                            // 復元すべきベクトル
                            float3 tv = math.mul(prot, localPos);

                            // ベクトル長修正
                            float vlen = math.length(v);
                            float blen = lengthBufferArray[pindex];
                            vlen = math.lerp(vlen, blen, 0.5f); // 計算前の距離に徐々に近づける
                            Develop.Assert(vlen > 0.0f);
                            v = math.normalize(v) * vlen;

                            // ベクトル角度クランプ
                            float maxAngleDeg = angleParam.limitCurveData.EvaluateCurve(cdepth);
                            float maxAngleRad = math.radians(maxAngleDeg);
                            float angle = MathUtility.Angle(v, tv);
                            float3 rv = v;
                            if (angle > maxAngleRad)
                            {
                                // stiffness
                                float recoveryAngle = math.lerp(angle, maxAngleRad, limitStiffness);

                                MathUtility.ClampAngle(v, tv, recoveryAngle, out rv);
                            }

                            // 回転中心割合
                            float3 rotPos = ppos + v * limitRotRatio;

                            // 親と子のそれぞれの更新位置
                            float3 pfpos = rotPos - rv * limitRotRatio;
                            float3 cfpos = rotPos + rv * (1.0f - limitRotRatio);

                            // 加算
                            float3 padd = pfpos - ppos;
                            float3 cadd = cfpos - cpos;

                            // 摩擦考慮
                            cadd *= cInvMass;
                            padd *= pInvMass;

                            const float attn = Define.System.AngleLimitAttenuation;

                            // 子の書き込み
                            if (cattr.IsMove())
                            {
                                cpos += cadd;
                                nextPosArray[pindex] = cpos;
                                velocityPosArray[pindex] = velocityPosArray[pindex] + cadd * attn;
                            }

                            // 親の書き込み
                            if (pattr.IsMove())
                            {
                                ppos += padd;
                                nextPosArray[p_pindex] = ppos;
                                velocityPosArray[p_pindex] = velocityPosArray[p_pindex] + padd * attn;
                            }

                            // 回転補正
                            v = cpos - ppos;
                            var nrot = math.mul(prot, localRot);
                            var q = MathUtility.FromToRotation(tv, v);
                            nrot = math.mul(q, nrot);
                            rotationBufferArray[pindex] = nrot;
                        }

                        //=====================================================
                        // Angle Restoration
                        //=====================================================
                        if (useAngleRestoration)
                        {
                            //Debug.Log($"pindex:{pindex}, p_pindex:{p_pindex}");

                            // 現在のベクトル
                            float3 v = cpos - ppos;

                            // 復元すべきベクトル
                            float3 tv = restorationVectorBufferArray[pindex];

                            // 復元力
                            float restorationStiffness = angleParam.restorationStiffness.EvaluateCurveClamp01(cdepth);
                            //restorationStiffness = math.saturate(restorationStiffness * math.pow(simulationPower.x, 1.5f));
                            restorationStiffness = math.saturate(restorationStiffness * simulationPower.w);

                            //int _pindex = indexBuffer[i] + p_start;
                            //Debug.Log($"i:{i} [{_pindex}] stiffness:{restorationStiffness} cdepth:{cdepth}");

                            // 重力方向減衰
                            restorationStiffness *= gravityFalloff;

                            // 球面線形補間
                            var q = MathUtility.FromToRotation(v, tv, restorationStiffness);
                            float3 rv = math.mul(q, v);

                            // 回転中心割合
                            //float restorationRotRatio = GetRotRatio(tv, gravityVector, gravity, gravityFalloff, iterationRatio);
                            //int _pindex = indexBuffer[i] + p_start;
                            //Debug.Log($"i:{i} [{_pindex}] ratio:{restorationRotRatio} cdepth:{cdepth}");
                            float3 rotPos = ppos + v * restorationRotRatio;

                            // 親と子のそれぞれの更新位置
                            float3 pfpos = rotPos - rv * restorationRotRatio;
                            float3 cfpos = rotPos + rv * (1.0f - restorationRotRatio);

                            // 加算
                            float3 padd = pfpos - ppos;
                            float3 cadd = cfpos - cpos;

                            // 摩擦考慮
                            padd *= cInvMass;
                            cadd *= pInvMass;

                            // 子の書き込み
                            if (cattr.IsMove())
                            {
                                cpos += cadd;
                                nextPosArray[pindex] = cpos;
                                velocityPosArray[pindex] = velocityPosArray[pindex] + cadd * restorationAttn;
                            }

                            // 親の書き込み
                            if (pattr.IsMove())
                            {
                                ppos += padd;
                                nextPosArray[p_pindex] = ppos;
                                velocityPosArray[p_pindex] = velocityPosArray[p_pindex] + padd * restorationAttn;
                            }
                        }
                    }
                }
            }
        }
    }
}
