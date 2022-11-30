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
    /// 最大最小距離拘束
    /// ルートではなく自身の親から正確に計算する
    /// ※実験の結果一長一短のため今は不採用とする(1.8.0)
    /// </summary>
    public class ClampDistance2Constraint : PhysicsManagerConstraint
    {
        /// <summary>
        /// 拘束データ
        /// </summary>
        [System.Serializable]
        public struct ClampDistance2Data
        {
            /// <summary>
            /// 計算頂点インデックス
            /// </summary>
            public int vertexIndex;

            /// <summary>
            /// 親頂点インデックス
            /// </summary>
            public int parentVertexIndex;

            /// <summary>
            /// オリジナル距離
            /// </summary>
            public float length;
        }
        FixedChunkNativeArray<ClampDistance2Data> dataList;

        [System.Serializable]
        public struct ClampDistance2RootInfo
        {
            public ushort startIndex;
            public ushort dataLength;
        }
        FixedChunkNativeArray<ClampDistance2RootInfo> rootInfoList;

        /// <summary>
        /// グループごとの拘束データ
        /// </summary>
        public struct GroupData
        {
            public int teamId;
            public int active;

            /// <summary>
            /// 最小距離割合
            /// </summary>
            public float minRatio;

            /// <summary>
            /// 最大距離割合
            /// </summary>
            public float maxRatio;

            /// <summary>
            /// 速度影響
            /// </summary>
            public float velocityInfluence;

            public ChunkData dataChunk;
            public ChunkData rootInfoChunk;
        }
        public FixedNativeList<GroupData> groupList;

        /// <summary>
        /// ルートごとのチームインデックス
        /// </summary>
        FixedChunkNativeArray<int> rootTeamList;

        /// <summary>
        /// 拘束データごとの作業バッファ
        /// </summary>
        //FixedChunkNativeArray<float> lengthBuffer;

        //=========================================================================================
        public override void Create()
        {
            dataList = new FixedChunkNativeArray<ClampDistance2Data>();
            rootInfoList = new FixedChunkNativeArray<ClampDistance2RootInfo>();
            groupList = new FixedNativeList<GroupData>();
            rootTeamList = new FixedChunkNativeArray<int>();
        }

        public override void Release()
        {
            dataList.Dispose();
            rootInfoList.Dispose();
            groupList.Dispose();
            rootTeamList.Dispose();
        }

        //=========================================================================================
        public int AddGroup(
            int teamId,
            bool active,
            float minRatio,
            float maxRatio,
            float velocityInfluence,
            ClampDistance2Data[] dataArray,
            ClampDistance2RootInfo[] rootInfoArray
            )
        {
            if (dataArray == null || dataArray.Length == 0 || rootInfoArray == null || rootInfoArray.Length == 0)
                return -1;

            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];

            var gdata = new GroupData();
            gdata.teamId = teamId;
            gdata.active = active ? 1 : 0;
            gdata.minRatio = minRatio;
            gdata.maxRatio = maxRatio;
            gdata.velocityInfluence = velocityInfluence;
            gdata.dataChunk = dataList.AddChunk(dataArray.Length);
            gdata.rootInfoChunk = rootInfoList.AddChunk(rootInfoArray.Length);

            // チャンクデータコピー
            dataList.ToJobArray().CopyFromFast(gdata.dataChunk.startIndex, dataArray);
            rootInfoList.ToJobArray().CopyFromFast(gdata.rootInfoChunk.startIndex, rootInfoArray);

            int group = groupList.Add(gdata);

            // ルートごとのチームインデックス
            var c = rootTeamList.AddChunk(rootInfoArray.Length);
            rootTeamList.Fill(c, teamId);

            return group;
        }

        public override void RemoveTeam(int teamId)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.clampDistance2GroupIndex;
            if (group < 0)
                return;

            var cdata = groupList[group];

            // チャンクデータ削除
            dataList.RemoveChunk(cdata.dataChunk);
            rootInfoList.RemoveChunk(cdata.rootInfoChunk);
            rootTeamList.RemoveChunk(cdata.rootInfoChunk);

            // データ削除
            groupList.Remove(group);
        }

        public void ChangeParam(
            int teamId,
            bool active,
            float minRatio,
            float maxRatio,
            float velocityInfluence
            )
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.clampDistance2GroupIndex;
            if (group < 0)
                return;

            var gdata = groupList[group];
            gdata.active = active ? 1 : 0;
            gdata.minRatio = minRatio;
            gdata.maxRatio = maxRatio;
            gdata.velocityInfluence = velocityInfluence;
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

            // 最大距離拘束（ルートラインごと）
            var job1 = new ClampDistance2Job()
            {
                runCount = runCount,

                dataList = dataList.ToJobArray(),
                rootInfoList = rootInfoList.ToJobArray(),
                rootTeamList = rootTeamList.ToJobArray(),
                groupList = groupList.ToJobArray(),

                teamDataList = Manager.Team.teamDataList.ToJobArray(),

                flagList = Manager.Particle.flagList.ToJobArray(),
                frictionList = Manager.Particle.frictionList.ToJobArray(),
                nextPosList = Manager.Particle.InNextPosList.ToJobArray(),
                posList = Manager.Particle.posList.ToJobArray(),
            };
            jobHandle = job1.Schedule(rootTeamList.Length, 8, jobHandle);

            return jobHandle;
        }


        /// <summary>
        /// 最大距離拘束ジョブ
        /// </summary>
        [BurstCompile]
        struct ClampDistance2Job : IJobParallelFor
        {
            public int runCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<ClampDistance2Data> dataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClampDistance2RootInfo> rootInfoList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> rootTeamList;
            [Unity.Collections.ReadOnly]
            public NativeArray<GroupData> groupList;

            // チーム
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionList;

            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosList;

            [NativeDisableParallelForRestriction]
            public NativeArray<float3> posList;

            // ルートラインごと
            public void Execute(int rootIndex)
            {
                // チーム
                int teamIndex = rootTeamList[rootIndex];
                if (teamIndex == 0)
                    return;
                var team = teamDataList[teamIndex];
                if (team.IsActive() == false || team.clampDistance2GroupIndex < 0)
                    return;

                // 更新確認
                if (team.IsUpdate(runCount) == false)
                    return;

                // グループデータ
                var gdata = groupList[team.clampDistance2GroupIndex];
                if (gdata.active == 0)
                    return;

                // データ
                var rootInfo = rootInfoList[rootIndex];
                int dataIndex = rootInfo.startIndex + gdata.dataChunk.startIndex;
                int dataCount = rootInfo.dataLength;
                int pstart = team.particleChunk.startIndex;

                for (int i = 0; i < dataCount; i++)
                {
                    var data = dataList[dataIndex + i];
                    int pindex = data.parentVertexIndex;
                    if (pindex < 0)
                        continue;

                    var index = data.vertexIndex;

                    index += pstart;
                    pindex += pstart;

                    var flag = flagList[index];
                    if (flag.IsValid() == false || flag.IsFixed())
                        continue;

                    var npos = nextPosList[index];
                    var opos = npos;

                    var ppos = nextPosList[pindex];

                    // 現在のベクトル
                    var v = npos - ppos;

                    // 長さクランプ
                    var len = data.length * team.scaleRatio;
                    v = MathUtility.ClampVector(v, len * gdata.minRatio, len * gdata.maxRatio);

                    npos = ppos + v;

                    // 格納
                    nextPosList[index] = npos;

                    // 速度影響
                    var av = (npos - opos) * (1.0f - gdata.velocityInfluence);
                    posList[index] = posList[index] + av;
                }
            }
        }
    }
}
