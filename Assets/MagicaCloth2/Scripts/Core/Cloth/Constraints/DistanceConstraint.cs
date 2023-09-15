// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 距離制約
    /// </summary>
    public class DistanceConstraint : IDisposable
    {
        [System.Serializable]
        public class SerializeData : IDataValidate
        {
            /// <summary>
            /// Overall connection stiffness (0.0 ~ 1.0).
            /// 全体的な接続の剛性(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CurveSerializeData stiffness;

            public SerializeData()
            {
                stiffness = new CurveSerializeData(1.0f, 1.0f, 0.5f, false);
            }

            public void DataValidate()
            {
                stiffness.DataValidate(0.0f, 1.0f);
            }

            public SerializeData Clone()
            {
                return new SerializeData()
                {
                    stiffness = stiffness.Clone(),
                };
            }
        }

        public struct DistanceConstraintParams
        {
            /// <summary>
            /// 剛性
            /// </summary>
            public float4x4 restorationStiffness;

            /// <summary>
            /// 距離制約の速度減衰率(0.0 ~ 1.0)
            /// </summary>
            public float velocityAttenuation;

            public void Convert(SerializeData sdata)
            {
                restorationStiffness = sdata.stiffness.ConvertFloatArray();
                velocityAttenuation = Define.System.DistanceVelocityAttenuation;
            }
        }

        //=========================================================================================
        /// <summary>
        /// 接続タイプ数
        /// </summary>
        public const int TypeCount = 2;

        /// <summary>
        /// 制約データ
        /// </summary>
        internal class ConstraintData : IValid
        {
            internal ResultCode result;

            public uint[] indexArray;
            public ushort[] dataArray;
            public float[] distanceArray;

            public bool IsValid()
            {
                return indexArray != null && indexArray.Length > 0;
            }
        }

        /// <summary>
        /// パーティクルごとのデータ開始インデックスとデータ数を１つのuint(10-22)にパックしたもの
        /// </summary>
        public ExNativeArray<uint> indexArray;

        /// <summary>
        /// 接続パーティクルリスト
        /// </summary>
        public ExNativeArray<ushort> dataArray;

        /// <summary>
        /// 対象への基準距離リスト
        /// ただし符号によりタイプを示す(+:Vertical, -:Horizontal)
        /// </summary>
        public ExNativeArray<float> distanceArray;

        /// <summary>
        /// 登録データ数を返す
        /// </summary>
        public int DataCount => indexArray?.Count ?? 0;

        //=========================================================================================
        public DistanceConstraint()
        {
            indexArray = new ExNativeArray<uint>(0, true);
            dataArray = new ExNativeArray<ushort>(0, true);
            distanceArray = new ExNativeArray<float>(0, true);
        }

        public void Dispose()
        {
            indexArray?.Dispose();
            dataArray?.Dispose();
            distanceArray?.Dispose();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[DistanceConstraint]");
            sb.AppendLine($"  -indexArray:{indexArray.ToSummary()}");
            sb.AppendLine($"  -dataArray:{dataArray.ToSummary()}");
            sb.AppendLine($"  -distanceArray:{distanceArray.ToSummary()}");

            return sb.ToString();
        }

        //=========================================================================================
        /// <summary>
        /// 制約データの作成
        /// </summary>
        /// <param name="cbase"></param>
        internal static ConstraintData CreateData(VirtualMesh proxyMesh, in ClothParameters parameters)
        {
            var constraintData = new ConstraintData();

            NativeParallelMultiHashMap<int, ushort> vvMap = default;

            try
            {
                int vcnt = proxyMesh.VertexCount;

                // 頂点の接続頂点配列をMultiHashMapに変換する
                vvMap = JobUtility.ToNativeMultiHashMap(proxyMesh.vertexToVertexIndexArray, proxyMesh.vertexToVertexDataArray);

                var connectSet = new HashSet<uint>();
                using var verticalConnection = new MultiDataBuilder<ushort>(vcnt, vcnt * 2);
                using var horizontalConnection = new MultiDataBuilder<ushort>(vcnt, vcnt * 2);

                // 構造接続
                for (int i = 0; i < vcnt; i++)
                {
                    if (vvMap.ContainsKey(i) == false)
                        continue;

                    var attr = proxyMesh.attributes[i];
                    int pindex = proxyMesh.vertexParentIndices[i];

                    foreach (var data in vvMap.GetValuesForKey(i))
                    {
                        int tindex = data;
                        var t_attr = proxyMesh.attributes[tindex];
                        int t_pindex = proxyMesh.vertexParentIndices[tindex];

                        // 両方とも固定ならば無効
                        if (attr.IsMove() == false && t_attr.IsMove() == false)
                            continue;

                        // １点でも無効なら除外する
                        if (attr.IsInvalid() || t_attr.IsInvalid())
                            continue;

                        // 登録
                        if (tindex == pindex || i == t_pindex)
                        {
                            // ベースライン
                            verticalConnection.Add(i, data);
                        }
                        else
                        {
                            // それ以外
                            horizontalConnection.Add(i, data);
                        }
                        uint pack = DataUtility.Pack32Sort(i, tindex);
                        connectSet.Add(pack);
                        //Debug.Log($"({i} - {tindex})");
                    }
                }

#if true
                // shear接続（横接続として登録）
                if (proxyMesh.edgeToTriangles.IsCreated)
                {
                    int ecnt = proxyMesh.EdgeCount;
                    for (int l = 0; l < ecnt; l++)
                    {
                        int2 edge = proxyMesh.edges[l];
                        var tset = proxyMesh.edgeToTriangles.ToFixedList128Bytes(edge);
                        int tcnt = tset.Length;
                        if (tcnt < 2)
                            continue;

                        //   p3 +
                        //     / \
                        // p1 +---+ p2
                        //     \ /
                        //   p4 +
                        var p1 = proxyMesh.localPositions[edge.x];
                        var p2 = proxyMesh.localPositions[edge.y];
                        float edgeLength1 = math.length(p1 - p2);
                        if (edgeLength1 < Define.System.Epsilon)
                            continue;
                        var v1 = math.normalize(p1 - p2);
                        var cen = (p1 + p2) * 0.5f;

                        for (int i = 0; i < tcnt - 1; i++)
                        {
                            int3 tri1 = proxyMesh.triangles[tset[i]];
                            int e3 = MathUtility.GetUnuseTriangleIndex(tri1, edge);
                            var p3 = proxyMesh.localPositions[e3];
                            var attr3 = proxyMesh.attributes[e3];
                            var n1 = MathUtility.TriangleNormal(p1, p2, p3);
                            for (int j = i + 1; j < tcnt; j++)
                            {
                                int3 tri2 = proxyMesh.triangles[tset[j]];
                                int e4 = MathUtility.GetUnuseTriangleIndex(tri2, edge);
                                var p4 = proxyMesh.localPositions[e4];
                                var attr4 = proxyMesh.attributes[e4];
                                var n2 = MathUtility.TriangleNormal(p1, p2, p4);

                                // 両方とも固定ならば無効
                                if (attr3.IsMove() == false && attr4.IsMove() == false)
                                    continue;

                                // (1)トライアングルの内角がほぼ水平かチェック
                                // 20度:0.9396926f
                                float dot = math.abs(math.dot(n1, n2));
                                if (dot < 0.9396926f)
                                    continue;

                                // (2)トライアングルペアの２つの対角線の長さが一定以内なら正方形と判定する
                                float edgeLength2 = math.length(p3 - p4);
                                float ratio = math.abs(edgeLength2 / edgeLength1 - 1.0f);
                                if (ratio <= 0.3f)
                                {
                                    // 正方形
                                    // 対角線(p3-p4)をShearとして接続する
                                    uint pack = DataUtility.Pack32Sort(e3, e4);
                                    if (connectSet.Contains(pack) == false)
                                    {
                                        connectSet.Add(pack);
                                        horizontalConnection.Add(e3, (ushort)e4);
                                        horizontalConnection.Add(e4, (ushort)e3);
                                    }
                                }
                            }
                        }
                    }
                }
#endif
                // 制約データ登録
                (var dataArryaV, var indexArrayV) = verticalConnection.ToArray();
                (var dataArryaH, var indexArrayH) = horizontalConnection.ToArray();

                // すべて無効属性などデータがnullの場合もある
                int total = (dataArryaV?.Length ?? 0) + (dataArryaH?.Length ?? 0);
                if (total > 0)
                {
                    var indexList = new List<uint>(vcnt);
                    var dataList = new List<ushort>(total);
                    var distanceList = new List<float>(total);
                    for (int i = 0; i < vcnt; i++)
                    {
                        int start = dataList.Count;
                        int cnt = 0;

                        float3 pos = proxyMesh.localPositions[i];

                        for (int k = 0; k < TypeCount; k++)
                        {
                            DataUtility.Unpack12_20(k == 0 ? indexArrayV[i] : indexArrayH[i], out var dcnt, out var dstart);
                            for (int j = 0; j < dcnt; j++)
                            {
                                ushort data = k == 0 ? dataArryaV[dstart + j] : dataArryaH[dstart + j];
                                var tpos = proxyMesh.localPositions[data];
                                float dist = math.distance(pos, tpos);
                                if (dist < 1e-06f)
                                    continue;

                                dataList.Add(data);
                                distanceList.Add(k == 0 ? dist : -dist); // マイナスはhorizontalタイプ
                                cnt++;
                            }
                        }

                        uint pack = DataUtility.Pack12_20(cnt, start);
                        indexList.Add(pack);
                    }

                    constraintData.indexArray = indexList.ToArray();
                    constraintData.dataArray = dataList.ToArray();
                    constraintData.distanceArray = distanceList.ToArray();
                }

                constraintData.result.SetSuccess();
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                constraintData.result.SetError(Define.Result.Constraint_CreateDistanceException);
                throw;
            }
            finally
            {
                if (vvMap.IsCreated)
                    vvMap.Dispose();
            }

            return constraintData;
        }

        //=========================================================================================
        /// <summary>
        /// 制約データを登録する
        /// </summary>
        /// <param name="cprocess"></param>
        internal void Register(ClothProcess cprocess)
        {
            if (cprocess?.distanceConstraintData?.IsValid() ?? false)
            {
                ref var tdata = ref MagicaManager.Team.GetTeamDataRef(cprocess.TeamId);

                tdata.distanceStartChunk = indexArray.AddRange(cprocess.distanceConstraintData.indexArray);
                tdata.distanceDataChunk = dataArray.AddRange(cprocess.distanceConstraintData.dataArray);
                distanceArray.AddRange(cprocess.distanceConstraintData.distanceArray);
            }
        }

        /// <summary>
        /// 制約データを解除する
        /// </summary>
        /// <param name="cprocess"></param>
        internal void Exit(ClothProcess cprocess)
        {
            if (cprocess != null && cprocess.TeamId > 0)
            {
                ref var tdata = ref MagicaManager.Team.GetTeamDataRef(cprocess.TeamId);

                indexArray.Remove(tdata.distanceStartChunk);
                dataArray.Remove(tdata.distanceDataChunk);
                distanceArray.Remove(tdata.distanceDataChunk);

                tdata.distanceStartChunk.Clear();
                tdata.distanceDataChunk.Clear();
            }
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

            var job = new DistanceConstraintJob()
            {
                simulationPower = MagicaManager.Time.SimulationPower,

                stepParticleIndexArray = sm.processingStepParticle.Buffer,

                teamDataArray = tm.teamDataArray.GetNativeArray(),
                parameterArray = tm.parameterArray.GetNativeArray(),

                attributes = vm.attributes.GetNativeArray(),
                depthArray = vm.vertexDepths.GetNativeArray(),

                teamIdArray = sm.teamIdArray.GetNativeArray(),
                nextPosArray = sm.nextPosArray.GetNativeArray(),
                basePosArray = sm.basePosArray.GetNativeArray(),
                velocityPosArray = sm.velocityPosArray.GetNativeArray(),
                frictionArray = sm.frictionArray.GetNativeArray(),

                //stepBasicPositionBuffer = sm.stepBasicPositionBuffer,

                indexArray = indexArray.GetNativeArray(),
                dataArray = dataArray.GetNativeArray(),
                distanceArray = distanceArray.GetNativeArray(),
            };
            jobHandle = job.Schedule(sm.processingStepParticle.GetJobSchedulePtr(), 32, jobHandle);

            return jobHandle;
        }

        /// <summary>
        /// 距離制約の解決
        /// </summary>
        [BurstCompile]
        struct DistanceConstraintJob : IJobParallelForDefer
        {
            public float4 simulationPower;

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
            public NativeArray<float> depthArray;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> velocityPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionArray;

            // buffer
            //[Unity.Collections.ReadOnly]
            //public NativeArray<float3> stepBasicPositionBuffer;

            // constrants
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> indexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> dataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> distanceArray;

            // ステップ有効パーティクルごと
            public void Execute(int index)
            {
                // pindexのチームは有効であることが保証されている
                int pindex = stepParticleIndexArray[index];

                int teamId = teamIdArray[pindex];
                var tdata = teamDataArray[teamId];
                var parameter = parameterArray[teamId];

                // 復元を基本姿勢で行うかアニメーション後の姿勢で行うかの判定
                float blendRatio = tdata.animationPoseRatio;

                // スケール倍率
                float scl = tdata.InitScale * tdata.scaleRatio;

                int p_start = tdata.particleChunk.startIndex;
                int l_index = pindex - p_start;

                var sc = tdata.distanceStartChunk;
                var dc = tdata.distanceDataChunk;

                int c_start = sc.startIndex;
                int d_start = dc.startIndex;
                int v_start = tdata.proxyCommonChunk.startIndex;
                int vindex = v_start + l_index;

                // パーティクル情報
                var nextPos = nextPosArray[pindex];
                var attr = attributes[vindex];
                float depth = depthArray[vindex];
                float friction = frictionArray[pindex];

                if (attr.IsDontMove())
                    return;

                // 重量
                float invMass = MathUtility.CalcInverseMass(friction, depth, attr.IsDontMove());

                // 基本剛性
                float stiffness = parameter.distanceConstraint.restorationStiffness.EvaluateCurveClamp01(depth);
                //stiffness *= simulationPower;
                //stiffness *= (simulationPower * simulationPower);
                stiffness *= simulationPower.y;

                var pack = indexArray[c_start + l_index];
                DataUtility.Unpack12_20(pack, out int dcnt, out int dstart);

                if (dcnt > 0)
                {
                    // 基準座標を切り替え
                    float3 basePos = basePosArray[pindex];
                    //float3 basicPos = stepBasicPositionBuffer[pindex];

                    float3 addPos = 0;
                    int addCnt = 0;

                    int start = d_start + dstart;
                    for (int i = 0; i < dcnt; i++)
                    {
                        int t_l_index = dataArray[start + i];
                        float restDist = distanceArray[start + i];

                        // タイプ別剛性
                        float finalStiffness = math.saturate(restDist >= 0.0f ? stiffness : stiffness * Define.System.DistanceHorizontalStiffness);

                        // 相手パーティクル情報
                        int tpindex = p_start + t_l_index;
                        int tvindex = v_start + t_l_index;
                        var t_nextPos = nextPosArray[tpindex];
                        float3 t_basePos = basePosArray[tpindex];
                        //float3 t_basicPos = stepBasicPositionBuffer[tpindex];
                        float t_depth = depthArray[tvindex];
                        float t_friction = frictionArray[tpindex];
                        var t_attr = attributes[tvindex];

                        // 重量
                        float t_invMass = MathUtility.CalcInverseMass(t_friction, t_depth, t_attr.IsDontMove());

                        // 復元する長さ
                        // !Distance制約は初期化時に保存した距離を見るようにしないと駄目
                        // フラグにより初期値かアニメーション後の姿勢かを切り替える
                        float restLength = math.lerp(math.abs(restDist) * scl, math.distance(basePos, t_basePos), blendRatio);
                        //float restLength = math.distance(basicPos, t_basicPos);

                        var v = t_nextPos - nextPos;

                        // 現在の長さ
                        float distance = math.length(v);

                        // 距離がほぼ０ならば処理をスキップする（エラーの回避）
                        if (distance < Define.System.Epsilon)
                            continue;

                        // 伸縮
                        float3 n = math.normalize(v);
                        float3 corr = finalStiffness * n * (distance - restLength) / (invMass + t_invMass);
                        float3 corr0 = invMass * corr;
                        //float3 corr1 = -t_invMass * corr; // 相手側(使用しない)

                        // すべて加算する
                        addPos += corr0;
                        addCnt++;
                    }

                    // 最終位置
                    if (addCnt > 0)
                    {
                        addPos = addPos / addCnt; // 平均化
                        nextPos += addPos;
                        nextPosArray[pindex] = nextPos;

                        // 速度影響
                        float attn = parameter.distanceConstraint.velocityAttenuation;
                        velocityPosArray[pindex] = velocityPosArray[pindex] + addPos * attn;
                    }
                }
            }
        }
    }
}
