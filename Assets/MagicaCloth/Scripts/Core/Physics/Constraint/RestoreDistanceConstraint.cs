// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MagicaCloth
{
    /// <summary>
    /// 距離復元拘束
    /// </summary>
    public class RestoreDistanceConstraint : PhysicsManagerConstraint
    {
        /// <summary>
        /// 接続タイプ
        /// </summary>
        public const int StructType = 0;
        public const int BendType = 1;
        public const int NearType = 2;
        public const int TypeCount = 3; // カウンタ

        /// <summary>
        /// 距離復元拘束データ
        /// todo:共有化可能
        /// </summary>
        [System.Serializable]
        public struct RestoreDistanceData
        {
            /// <summary>
            /// 計算頂点インデックス
            /// </summary>
            public ushort vertexIndex;

            /// <summary>
            /// ターゲット頂点インデックス
            /// </summary>
            public ushort targetVertexIndex;

            /// <summary>
            /// パーティクル距離(v1.7.0)
            /// </summary>
            public float length;

            /// <summary>
            /// データが有効か判定する
            /// </summary>
            /// <returns></returns>
            public bool IsValid()
            {
                return vertexIndex > 0 || targetVertexIndex > 0;
            }
        }

        FixedChunkNativeArray<RestoreDistanceData>[] dataList;

        /// <summary>
        /// 頂点インデックスごとの書き込みバッファ参照
        /// </summary>
        FixedChunkNativeArray<ReferenceDataIndex>[] refDataList;


        /// <summary>
        /// グループごとの拘束データ
        /// </summary>
        public struct RestoreDistanceGroupData
        {
            public int teamId;

            /// <summary>
            /// 重量
            /// </summary>
            public CurveParam mass;

            /// <summary>
            /// 速度影響
            /// </summary>
            public float velocityInfluence;

            /// <summary>
            /// 構造接続
            /// </summary>
            public CurveParam structStiffness;
            public ChunkData structDataChunk;
            public ChunkData structRefChunk;

            /// <summary>
            /// ベンド接続
            /// </summary>
            public int useBend;
            public CurveParam bendStiffness;
            public ChunkData bendDataChunk;
            public ChunkData bendRefChunk;

            /// <summary>
            /// 近接続
            /// </summary>
            public int useNear;
            public CurveParam nearStiffness;
            public ChunkData nearDataChunk;
            public ChunkData nearRefChunk;

            public bool IsValid(int type)
            {
                if (type == StructType)
                    return true; // 常にON
                else if (type == BendType)
                    return useBend == 1;
                else
                    return useNear == 1;
            }

            public CurveParam GetStiffness(int type)
            {
                if (type == StructType)
                    return structStiffness;
                else if (type == BendType)
                    return bendStiffness;
                else
                    return nearStiffness;
            }

            public ChunkData GetDataChunk(int type)
            {
                //return structDataChunk;
                if (type == StructType)
                    return structDataChunk;
                else if (type == BendType)
                    return bendDataChunk;
                else
                    return nearDataChunk;
            }

            public ChunkData GetRefChunk(int type)
            {
                //return structRefChunk;
                if (type == StructType)
                    return structRefChunk;
                else if (type == BendType)
                    return bendRefChunk;
                else
                    return nearRefChunk;
            }
        }
        public FixedNativeList<RestoreDistanceGroupData> groupList;


        //=========================================================================================
        public override void Create()
        {
            groupList = new FixedNativeList<RestoreDistanceGroupData>();
            dataList = new FixedChunkNativeArray<RestoreDistanceData>[TypeCount];
            refDataList = new FixedChunkNativeArray<ReferenceDataIndex>[TypeCount];
            for (int i = 0; i < TypeCount; i++)
            {
                dataList[i] = new FixedChunkNativeArray<RestoreDistanceData>();
                refDataList[i] = new FixedChunkNativeArray<ReferenceDataIndex>();
            }
        }

        public override void Release()
        {
            groupList.Dispose();
            for (int i = 0; i < TypeCount; i++)
            {
                dataList[i].Dispose();
                refDataList[i].Dispose();
            }
            dataList = null;
            refDataList = null;
        }

        //=========================================================================================
        public int AddGroup(
            int teamId,
            BezierParam mass,
            float velocityInfluence,
            BezierParam structStiffness,
            RestoreDistanceData[] structDataArray,
            ReferenceDataIndex[] structRefDataArray,
            bool useBend,
            BezierParam bendStiffness,
            RestoreDistanceData[] bendDataArray,
            ReferenceDataIndex[] bendRefDataArray,
            bool useNear,
            BezierParam nearStiffness,
            RestoreDistanceData[] nearDataArray,
            ReferenceDataIndex[] nearRefDataArray
            )
        {
            if (structDataArray == null || structDataArray.Length == 0 || structRefDataArray == null || structRefDataArray.Length == 0)
                return -1;

            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];

            var gdata = new RestoreDistanceGroupData();
            gdata.teamId = teamId;
            gdata.mass.Setup(mass);
            gdata.velocityInfluence = velocityInfluence;
            gdata.useBend = useBend ? 1 : 0;
            gdata.useNear = useNear ? 1 : 0;

            gdata.structStiffness.Setup(structStiffness);
            gdata.structDataChunk = dataList[StructType].AddChunk(structDataArray.Length);
            gdata.structRefChunk = refDataList[StructType].AddChunk(structRefDataArray.Length);
            // チャンクデータコピー
            dataList[StructType].ToJobArray().CopyFromFast(gdata.structDataChunk.startIndex, structDataArray);
            refDataList[StructType].ToJobArray().CopyFromFast(gdata.structRefChunk.startIndex, structRefDataArray);

            if (bendDataArray != null && bendDataArray.Length > 0)
            {
                gdata.bendStiffness.Setup(bendStiffness);
                gdata.bendDataChunk = dataList[BendType].AddChunk(bendDataArray.Length);
                gdata.bendRefChunk = refDataList[BendType].AddChunk(bendRefDataArray.Length);
                // チャンクデータコピー
                dataList[BendType].ToJobArray().CopyFromFast(gdata.bendDataChunk.startIndex, bendDataArray);
                refDataList[BendType].ToJobArray().CopyFromFast(gdata.bendRefChunk.startIndex, bendRefDataArray);
            }

            if (nearDataArray != null && nearDataArray.Length > 0)
            {
                gdata.nearStiffness.Setup(nearStiffness);
                gdata.nearDataChunk = dataList[NearType].AddChunk(nearDataArray.Length);
                gdata.nearRefChunk = refDataList[NearType].AddChunk(nearRefDataArray.Length);
                // チャンクデータコピー
                dataList[NearType].ToJobArray().CopyFromFast(gdata.nearDataChunk.startIndex, nearDataArray);
                refDataList[NearType].ToJobArray().CopyFromFast(gdata.nearRefChunk.startIndex, nearRefDataArray);
            }

            return groupList.Add(gdata);
        }

        /// <summary>
        /// 削除（チーム）
        /// </summary>
        /// <param name="group"></param>
        public override void RemoveTeam(int teamId)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.restoreDistanceGroupIndex;
            if (group < 0)
                return;

            var cdata = groupList[group];

            // チャンクデータ削除
            for (int i = 0; i < TypeCount; i++)
            {
                var dc = cdata.GetDataChunk(i);
                var rc = cdata.GetRefChunk(i);
                if (dc.dataLength > 0)
                {
                    dataList[i].RemoveChunk(dc);
                }
                if (rc.dataLength > 0)
                {
                    refDataList[i].RemoveChunk(rc);
                }
            }

            // データ削除
            groupList.Remove(group);
        }

        public void ChangeParam(
            int teamId,
            BezierParam mass,
            float velocityInfluence,
            BezierParam structStiffness,
            bool useBend,
            BezierParam bendStiffness,
            bool useNear,
            BezierParam nearStiffness
            )
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.restoreDistanceGroupIndex;
            if (group < 0)
                return;

            var cdata = groupList[group];
            cdata.mass.Setup(mass);
            cdata.velocityInfluence = velocityInfluence;
            cdata.structStiffness.Setup(structStiffness);
            cdata.bendStiffness.Setup(bendStiffness);
            cdata.nearStiffness.Setup(nearStiffness);
            cdata.useBend = useBend ? 1 : 0;
            cdata.useNear = useNear ? 1 : 0;
            groupList[group] = cdata;
        }

        //=========================================================================================
        /// <summary>
        /// 拘束の更新回数
        /// </summary>
        /// <returns></returns>
        public override int GetIterationCount()
        {
            return base.GetIterationCount();
            //return 2;
        }

        /// <summary>
        /// 拘束の解決
        /// </summary>
        /// <param name="dtime"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public override JobHandle SolverConstraint(int runCount, float dtime, float updatePower, int iteration, JobHandle jobHandle)
        {
            if (groupList.Count == 0)
                return jobHandle;

            // 距離拘束（パーティクルごとに実行する）
            //for (int type = 0; type < TypeCount; type++)
            for (int type = TypeCount - 1; type >= 0; type--) // こちらのほうが安定する気がする
            {
                if (dataList[type].Count == 0)
                    continue;

                var job1 = new DistanceJob()
                {
                    updatePower = updatePower,
                    runCount = runCount,
                    type = type,

                    restoreDistanceDataList = dataList[type].ToJobArray(),
                    restoreDistanceGroupDataList = groupList.ToJobArray(),
                    refDataList = refDataList[type].ToJobArray(),

                    teamDataList = Manager.Team.teamDataList.ToJobArray(),
                    teamIdList = Manager.Particle.teamIdList.ToJobArray(),

                    flagList = Manager.Particle.flagList.ToJobArray(),
                    depthList = Manager.Particle.depthList.ToJobArray(),
                    frictionList = Manager.Particle.frictionList.ToJobArray(),
                    basePosList = Manager.Particle.basePosList.ToJobArray(),
                    nextPosList = Manager.Particle.InNextPosList.ToJobArray(),

                    outNextPosList = Manager.Particle.OutNextPosList.ToJobArray(),

                    posList = Manager.Particle.posList.ToJobArray(),
                };
                jobHandle = job1.Schedule(Manager.Particle.Length, 64, jobHandle);
                Manager.Particle.SwitchingNextPosList();
            }

            return jobHandle;
        }

        /// <summary>
        /// 距離拘束ジョブ
        /// パーティクルごとに計算
        /// </summary>
        [BurstCompile]
        struct DistanceJob : IJobParallelFor
        {
            public float updatePower;
            public int runCount;
            public int type;

            [Unity.Collections.ReadOnly]
            public NativeArray<RestoreDistanceData> restoreDistanceDataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<RestoreDistanceGroupData> restoreDistanceGroupDataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<ReferenceDataIndex> refDataList;

            // チーム
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> teamIdList;

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosList;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<quaternion> baseRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosList;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> outNextPosList;

            public NativeArray<float3> posList;

            // パーティクルごと
            public void Execute(int index)
            {
                // 初期化コピー
                var nextpos = nextPosList[index];
                outNextPosList[index] = nextpos;

                var flag = flagList[index];
                if (flag.IsValid() == false || flag.IsFixed())
                    return;

                var team = teamDataList[teamIdList[index]];
                if (team.restoreDistanceGroupIndex < 0)
                    return;

                // 更新確認
                if (team.IsUpdate(runCount) == false)
                    return;

                int pstart = team.particleChunk.startIndex;
                int vindex = index - pstart;

                // クロスごとの拘束データ
                var gdata = restoreDistanceGroupDataList[team.restoreDistanceGroupIndex];
                if (gdata.IsValid(type) == false)
                    return;

                // 参照情報
                var dataChunk = gdata.GetDataChunk(type);
                var refChunk = gdata.GetRefChunk(type);
                var stiffnessData = gdata.GetStiffness(type);

                // アニメーションされた姿勢の使用
                bool useAnimatedPose = team.IsFlag(PhysicsManagerTeamData.Flag_AnimatedPose);

                // 摩擦係数１に対する重量増加倍率
                // この係数は重要！
                // 10以下だと突き抜けが発生する、30だと突き抜けは防止でいるがジッタリングがひどくなる
                const float FrictionMass = 5.0f; // 20(v1.6.1)
                float friction = frictionList[index];

                var refdata = refDataList[refChunk.startIndex + vindex];
                if (refdata.count > 0)
                {
                    float depth = depthList[index];
                    float stiffness = stiffnessData.Evaluate(depth);
                    stiffness = math.saturate(stiffness * updatePower);
                    float3 addpos = 0;
                    float mass = gdata.mass.Evaluate(depth);
                    // 摩擦分重量を上げ移動しにくくする
                    mass += friction * FrictionMass;
                    float3 bpos = basePosList[index];

                    int dataIndex = dataChunk.startIndex + refdata.startIndex;
                    for (int i = 0; i < refdata.count; i++, dataIndex++)
                    {
                        var data = restoreDistanceDataList[dataIndex];

                        if (data.IsValid() == false)
                            continue;

                        // ターゲットは拘束データの自身でない方
                        int tindex = pstart + data.targetVertexIndex;
                        float3 tnextpos = nextPosList[tindex];

                        // 現在の距離
                        float3 v = tnextpos - nextpos;
                        float vlen = math.length(v);
                        if (vlen < 0.00001f)
                            continue;


                        // 復元距離
                        float rlen = data.length; // v1.7.0
                        // チームスケール倍率
                        rlen *= team.scaleRatio;

                        if (useAnimatedPose)
                        {
                            // アニメーションされた距離を利用する
                            //rlen = math.distance(bpos, basePosList[tindex]); // 現在のオリジナル距離
                            //rlen = math.max(math.distance(bpos, basePosList[tindex]), rlen); // 長い方を採用する
                            rlen = (math.distance(bpos, basePosList[tindex]) + rlen) * 0.5f; // 平均
                        }

                        float clen = vlen - rlen;

                        // 重量差
                        float tdepth = depthList[tindex];
                        float tmass = gdata.mass.Evaluate(tdepth);
                        float tfriction = frictionList[tindex];
                        // 摩擦分重量を上げ移動しにくくする
                        tmass += tfriction * FrictionMass;

                        float m1 = tmass / (tmass + mass);

                        // 強さ
                        m1 *= stiffness;

                        // 移動ベクトル
                        float3 add1 = v * (m1 * clen / vlen);

                        // 加算位置
                        addpos += add1;
                    }

                    // 最終位置
                    var opos = nextpos;
                    nextpos += addpos / refdata.count;

                    // 書き出し
                    outNextPosList[index] = nextpos;

                    // 速度影響
                    var av = (nextpos - opos) * (1.0f - gdata.velocityInfluence);
                    posList[index] = posList[index] + av;
                }
            }
        }
    }
}
