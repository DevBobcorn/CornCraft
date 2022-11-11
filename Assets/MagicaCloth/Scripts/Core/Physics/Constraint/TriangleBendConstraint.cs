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
    /// トライアングル曲げ復元拘束
    /// </summary>
    public class TriangleBendConstraint : PhysicsManagerConstraint
    {
        /// <summary>
        /// 拘束データ
        /// todo:共有化可能
        /// </summary>
        [System.Serializable]
        public struct TriangleBendData
        {
            /// <summary>
            /// トライアングル形成パーティクルインデックスx4
            /// ２つの三角形、p2-p3が共通辺で、p0/p1が端の独立点
            ///   p2 +
            ///     /|\
            /// p0 + | + p1
            ///     \|/
            ///   p3 +
            ///   
            /// A(p0-p2-p3) B(p1-p3-p2)
            /// 
            /// ただしvindex3が-1の場合は次のように単純なトライアングルの固定位置復元となる
            ///   v0 +
            ///     / \
            /// v1 +---+ v2
            ///     
            ///   v3 = -1 
            public int vindex0;
            public int vindex1;
            public int vindex2;
            public int vindex3;

            /// <summary>
            /// 復元角度(ラジアン)
            /// </summary>
            public float restAngle;

            /// <summary>
            /// 初期方向性 [Algorithm 2]
            /// </summary>
            public float direction;

            /// <summary>
            /// ベンド影響を取得するデプス値(0.0-1.0)
            /// </summary>
            public float depth;

            /// <summary>
            /// 書き込みバッファインデックス
            /// </summary>
            public int writeIndex0;
            public int writeIndex1;
            public int writeIndex2;
            public int writeIndex3;

            /// <summary>
            /// データが有効か判定する
            /// </summary>
            /// <returns></returns>
            public bool IsValid()
            {
                return vindex0 > 0 && vindex1 > 0;
            }

            /// <summary>
            /// データが固定位置復元かどうか
            /// </summary>
            /// <returns></returns>
            public bool IsPositionBend()
            {
                return vindex3 < 0;
            }

            /// <summary>
            /// データが厳密版か判定する
            /// </summary>
            /// <returns></returns>
            //public bool IsStrict()
            //{
            //    return math.abs(direction) > 1e-06f;
            //}
        }
        FixedChunkNativeArray<TriangleBendData> dataList;

        /// <summary>
        /// データごとのグループインデックス
        /// </summary>
        FixedChunkNativeArray<short> groupIndexList;

        /// <summary>
        /// 内部パーティクルインデックスごとの書き込みバッファ参照
        /// </summary>
        FixedChunkNativeArray<ReferenceDataIndex> refDataList;

        /// <summary>
        /// 頂点計算結果書き込みバッファ
        /// </summary>
        FixedChunkNativeArray<float3> writeBuffer;

        /// <summary>
        /// グループごとの拘束データ
        /// </summary>
        public struct TriangleBendGroupData
        {
            public int teamId;

            public int active;

            /// <summary>
            /// アルゴリズム(ClothParams.Algorithmを参照)
            /// </summary>
            public int algorithm;

            /// <summary>
            /// 固定点を含むトライアングルの計算の有無
            /// </summary>
            //public int useIncludeFixed;

            /// <summary>
            /// 曲げの戻り効果量(0.0-1.0)
            /// </summary>
            public CurveParam stiffness;

            /// <summary>
            /// データチャンク
            /// </summary>
            public ChunkData dataChunk;

            /// <summary>
            /// グループデータチャンク
            /// </summary>
            public ChunkData groupIndexChunk;

            /// <summary>
            /// 内部インデックス用チャンク
            /// </summary>
            public ChunkData refDataChunk;

            /// <summary>
            /// 頂点計算結果書き込み用チャンク
            /// </summary>
            public ChunkData writeDataChunk;
        }
        FixedNativeList<TriangleBendGroupData> groupList;

        //=========================================================================================
        public override void Create()
        {
            dataList = new FixedChunkNativeArray<TriangleBendData>();
            groupIndexList = new FixedChunkNativeArray<short>();
            refDataList = new FixedChunkNativeArray<ReferenceDataIndex>();
            writeBuffer = new FixedChunkNativeArray<float3>();
            groupList = new FixedNativeList<TriangleBendGroupData>();
        }

        public override void Release()
        {
            dataList.Dispose();
            groupIndexList.Dispose();
            refDataList.Dispose();
            writeBuffer.Dispose();
            groupList.Dispose();
        }

        //=========================================================================================
        public int AddGroup(
            int teamId, bool active,
            ClothParams.Algorithm algorithm,
            BezierParam stiffness,
            //bool useIncludeFixed,
            TriangleBendData[] dataArray, ReferenceDataIndex[] refDataArray, int writeBufferCount)
        {
            if (dataArray == null || dataArray.Length == 0 || refDataArray == null || refDataArray.Length == 0 || writeBufferCount == 0)
                return -1;

            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];

            // グループデータ作成
            var gdata = new TriangleBendGroupData();
            gdata.teamId = teamId;
            gdata.active = active ? 1 : 0;
            gdata.algorithm = (int)algorithm;
            //gdata.useIncludeFixed = useIncludeFixed ? 1 : 0;
            gdata.stiffness.Setup(stiffness);
            gdata.dataChunk = dataList.AddChunk(dataArray.Length);
            gdata.groupIndexChunk = groupIndexList.AddChunk(dataArray.Length);
            gdata.refDataChunk = refDataList.AddChunk(refDataArray.Length);
            gdata.writeDataChunk = writeBuffer.AddChunk(writeBufferCount);

            // チャンクデータコピー
            dataList.ToJobArray().CopyFromFast(gdata.dataChunk.startIndex, dataArray);
            refDataList.ToJobArray().CopyFromFast(gdata.refDataChunk.startIndex, refDataArray);

            int group = groupList.Add(gdata);

            // データごとのグループインデックス
            groupIndexList.Fill(gdata.groupIndexChunk, (short)group);


            return group;
        }

        public override void RemoveTeam(int teamId)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.triangleBendGroupIndex;
            if (group < 0)
                return;

            var cdata = groupList[group];

            // チャンクデータ削除
            dataList.RemoveChunk(cdata.dataChunk);
            refDataList.RemoveChunk(cdata.refDataChunk);
            writeBuffer.RemoveChunk(cdata.writeDataChunk);
            groupIndexList.RemoveChunk(cdata.groupIndexChunk);

            // データ削除
            groupList.Remove(group);
        }

        public void ChangeParam(int teamId, bool active, BezierParam stiffness/*, bool useIncludeFixed*/)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.triangleBendGroupIndex;
            if (group < 0)
                return;

            var gdata = groupList[group];
            gdata.active = active ? 1 : 0;
            //gdata.useIncludeFixed = useIncludeFixed ? 1 : 0;
            gdata.stiffness.Setup(stiffness);
            groupList[group] = gdata;
        }

        //=========================================================================================
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

            // ステップ１：ベンドの計算
            var job = new TriangleBendCalcJob()
            {
                updatePower = updatePower,
                runCount = runCount,

                triangleBendGroupDataList = groupList.ToJobArray(),
                triangleBendList = dataList.ToJobArray(),
                groupIndexList = groupIndexList.ToJobArray(),

                teamDataList = Manager.Team.teamDataList.ToJobArray(),

                nextPosList = Manager.Particle.InNextPosList.ToJobArray(),
                basePosList = Manager.Particle.basePosList.ToJobArray(),

                writeBuffer = writeBuffer.ToJobArray(),
            };
            jobHandle = job.Schedule(dataList.Length, 64, jobHandle);

            // ステップ２：ベンド結果の集計
            var job2 = new TriangleBendSumJob()
            {
                runCount = runCount,

                triangleBendGroupDataList = groupList.ToJobArray(),
                refDataList = refDataList.ToJobArray(),
                writeBuffer = writeBuffer.ToJobArray(),

                teamDataList = Manager.Team.teamDataList.ToJobArray(),

                teamIdList = Manager.Particle.teamIdList.ToJobArray(),
                flagList = Manager.Particle.flagList.ToJobArray(),

                inoutNextPosList = Manager.Particle.InNextPosList.ToJobArray(),
                posList = Manager.Particle.posList.ToJobArray(),
            };
            jobHandle = job2.Schedule(Manager.Particle.Length, 64, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct TriangleBendCalcJob : IJobParallelFor
        {
            public float updatePower;
            public int runCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<TriangleBendGroupData> triangleBendGroupDataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<TriangleBendData> triangleBendList;
            [Unity.Collections.ReadOnly]
            public NativeArray<short> groupIndexList;

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosList;

            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> writeBuffer;

            // ベンドデータごと
            public void Execute(int index)
            {
                var data = triangleBendList[index];
                if (data.IsValid() == false)
                    return;

                int gindex = groupIndexList[index];
                var gdata = triangleBendGroupDataList[gindex];
                if (gdata.teamId == 0 || gdata.active == 0)
                    return;

                var tdata = teamDataList[gdata.teamId];
                if (tdata.IsActive() == false)
                    return;
                // 更新確認
                if (tdata.IsUpdate(runCount) == false)
                    return;

                int pstart = tdata.particleChunk.startIndex;

                float3 corr0 = 0;
                float3 corr1 = 0;
                float3 corr2 = 0;
                float3 corr3 = 0;

                int pindex0 = data.vindex0 + pstart;
                int pindex1 = data.vindex1 + pstart;
                int pindex2 = data.vindex2 + pstart;
                int pindex3 = data.IsPositionBend() ? -1 : (data.vindex3 + pstart);

                float3 nextpos0 = nextPosList[pindex0];
                float3 nextpos1 = nextPosList[pindex1];
                float3 nextpos2 = nextPosList[pindex2];
                float3 nextpos3 = data.IsPositionBend() ? 0 : nextPosList[pindex3];

                // 厳密正
                //bool isStrict = data.IsStrict();
                bool isStrict = gdata.algorithm == 1;

                // 復元率
                float stiffness = gdata.stiffness.Evaluate(data.depth);
                stiffness = (1.0f - math.pow(1.0f - stiffness, updatePower));

                // 復元方法判定
                if (data.IsPositionBend() == false)
                {
                    // トライアングルベンド
                    float3 e = nextpos3 - nextpos2;
                    float elen = math.length(e);
                    if (elen > 1e-06f)
                    {
                        float invElen = 1.0f / elen;

                        float3 n1 = math.cross(nextpos2 - nextpos0, nextpos3 - nextpos0);
                        float n1_lsq = math.lengthsq(n1);
                        float3 n2 = math.cross(nextpos3 - nextpos1, nextpos2 - nextpos1);
                        float n2_lsq = math.lengthsq(n2);

                        if (n1_lsq > 0 && n2_lsq > 0) // v1.12.0
                        {
                            n1 /= n1_lsq;
                            n2 /= n2_lsq;

                            float3 d0 = elen * n1;
                            float3 d1 = elen * n2;
                            float3 d2 = math.dot(nextpos0 - nextpos3, e) * invElen * n1 + math.dot(nextpos1 - nextpos3, e) * invElen * n2;
                            float3 d3 = math.dot(nextpos2 - nextpos0, e) * invElen * n1 + math.dot(nextpos2 - nextpos1, e) * invElen * n2;

                            n1 = math.normalize(n1);
                            n2 = math.normalize(n2);
                            float dot = math.dot(n1, n2);
                            dot = math.clamp(dot, -1.0f, 1.0f);
                            float phi = math.acos(dot);

                            float lambda =
                                math.lengthsq(d0) +
                                math.lengthsq(d1) +
                                math.lengthsq(d2) +
                                math.lengthsq(d3);

                            // 方向性
                            float direction = math.dot(math.cross(n1, n2), e);

                            // Strictでは方向を考慮した角度で計算
                            phi = math.select(phi, phi * math.sign(direction), isStrict);

                            // 安定性
                            float rest = math.abs(phi - data.restAngle);

                            //if (stiffness > 0.5f && rest > 1.5f)
                            //    stiffness = 0.5f;

                            // 角度による安定化
                            if (isStrict)
                            {
                                // 復元角度が大きいほどstiffnessが強くなる
                                float ratio = math.max(math.pow(math.saturate(rest / math.PI), 0.5f), 0.1f);
                                stiffness *= ratio;
                            }

                            lambda = (phi - data.restAngle) / lambda * stiffness;

                            // 旧来処理
                            lambda = math.select(lambda * math.sign(direction), lambda, isStrict);

                            corr0 = lambda * d0;
                            corr1 = lambda * d1;
                            corr2 = lambda * d2;
                            corr3 = lambda * d3;
                        }
                    }
                }
                // v1.11.0では一旦オミット
                //else
                //{
                //    // 固定頂点を含む特殊復元
                //    if (gdata.useIncludeFixed == 1)
                //    {
                //        // 単純にベース位置に復元させる
                //        stiffness *= 0.2f; // 調整
                //        float3 basepos0 = basePosList[pindex0];
                //        float3 basepos1 = basePosList[pindex1];
                //        float3 basepos2 = basePosList[pindex2];
                //        corr0 = (basepos0 - nextpos0) * stiffness;
                //        corr1 = (basepos1 - nextpos1) * stiffness;
                //        corr2 = (basepos2 - nextpos2) * stiffness;

                //        // todo:Interlockに切り替わった場合はここで処理を抜ける
                //    }
                //}

                // 作業バッファへ格納
                int wstart = gdata.writeDataChunk.startIndex;
                int windex0 = data.writeIndex0 + wstart;
                int windex1 = data.writeIndex1 + wstart;
                int windex2 = data.writeIndex2 + wstart;
                writeBuffer[windex0] = corr0;
                writeBuffer[windex1] = corr1;
                writeBuffer[windex2] = corr2;
                if (data.IsPositionBend() == false)
                {
                    int windex3 = data.writeIndex3 + wstart;
                    writeBuffer[windex3] = corr3;
                }
            }
        }

        [BurstCompile]
        struct TriangleBendSumJob : IJobParallelFor
        {
            public int runCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<TriangleBendGroupData> triangleBendGroupDataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<ReferenceDataIndex> refDataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> writeBuffer;

            // チーム
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> teamIdList;
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;

            public NativeArray<float3> inoutNextPosList;
            public NativeArray<float3> posList;

            // パーティクルごと
            public void Execute(int pindex)
            {
                var flag = flagList[pindex];
                if (flag.IsValid() == false || flag.IsFixed())
                    return;

                // チーム
                var team = teamDataList[teamIdList[pindex]];
                if (team.IsActive() == false)
                    return;
                if (team.triangleBendGroupIndex < 0)
                    return;

                // 更新確認
                if (team.IsUpdate(runCount) == false)
                    return;

                // グループデータ
                var gdata = triangleBendGroupDataList[team.triangleBendGroupIndex];
                if (gdata.active == 0)
                    return;

                // 集計
                int start = team.particleChunk.startIndex;
                int index = pindex - start;

                var refdata = refDataList[gdata.refDataChunk.startIndex + index];
                if (refdata.count > 0)
                {
                    float3 corr = 0;
                    var bindex = gdata.writeDataChunk.startIndex + refdata.startIndex;
                    for (int i = 0; i < refdata.count; i++)
                    {
                        corr += writeBuffer[bindex];
                        bindex++;
                    }
                    corr /= refdata.count;

                    // 加算
                    inoutNextPosList[pindex] = inoutNextPosList[pindex] + corr;

                    // 速度影響(Strictに合わせる)
                    var av = corr * (1.0f - Define.Compute.TriangleBendVelocityInfluence); // 実験結果より(0.5)
                    posList[pindex] = posList[pindex] + av;
                }
            }
        }
    }
}
