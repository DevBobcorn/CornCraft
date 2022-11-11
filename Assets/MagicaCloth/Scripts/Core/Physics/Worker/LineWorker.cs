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
    /// ライン回転調整ワーカー
    /// </summary>
    public class LineWorker : PhysicsManagerWorker
    {
        /// <summary>
        /// データ
        /// </summary>
        [System.Serializable]
        public struct LineRotationData
        {
            /// <summary>
            /// 頂点インデックス
            /// </summary>
            public int vertexIndex;

            /// <summary>
            /// 親頂点インデックス
            /// </summary>
            //public int parentVertexIndex;

            /// <summary>
            /// 子頂点の数
            /// </summary>
            public int childCount;

            /// <summary>
            /// 子頂点の開始データ配列インデックス
            /// </summary>
            public int childStartDataIndex;

            /// <summary>
            /// 親姿勢からのローカル位置(Transform.localPositionと同様)
            /// </summary>
            public float3 localPos;

            /// <summary>
            /// 親姿勢からのローカル回転(Transform.localRotationと同様)
            /// </summary>
            public quaternion localRot;

            /// <summary>
            /// データが有効か判定する
            /// </summary>
            /// <returns></returns>
            //public bool IsValid()
            //{
            //    return vertexIndex != 0 || parentVertexIndex != 0;
            //}
        }
        FixedChunkNativeArray<LineRotationData> dataList;

        [System.Serializable]
        public struct LineRotationRootInfo
        {
            public ushort startIndex;
            public ushort dataLength;
        }
        FixedChunkNativeArray<LineRotationRootInfo> rootInfoList;

        /// <summary>
        /// グループごとの拘束データ
        /// </summary>
        public struct GroupData
        {
            public int teamId;

            /// <summary>
            /// 回転補間
            /// </summary>
            public int avarage;

            public ChunkData dataChunk;
            public ChunkData rootInfoChunk;
        }
        public FixedNativeList<GroupData> groupList;

        /// <summary>
        /// ルートごとのチームインデックス
        /// </summary>
        FixedChunkNativeArray<int> rootTeamList;

        //=========================================================================================
        public override void Create()
        {
            dataList = new FixedChunkNativeArray<LineRotationData>();
            rootInfoList = new FixedChunkNativeArray<LineRotationRootInfo>();
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

        public int AddGroup(
            int teamId,
            bool avarage,
            LineRotationData[] dataArray,
            LineRotationRootInfo[] rootInfoArray
            )
        {
            if (dataArray == null || dataArray.Length == 0 || rootInfoArray == null || rootInfoArray.Length == 0)
                return -1;

            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];

            var gdata = new GroupData();
            gdata.teamId = teamId;
            gdata.avarage = avarage ? 1 : 0;
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

        public override void RemoveGroup(int teamId)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.lineWorkerGroupIndex;
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

        public void ChangeParam(int teamId, bool avarage)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.lineWorkerGroupIndex;
            if (group < 0)
                return;

            var gdata = groupList[group];
            gdata.avarage = avarage ? 1 : 0;
            groupList[group] = gdata;
        }

        //=========================================================================================
        /// <summary>
        /// トランスフォームリード中に実行する処理
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public override void Warmup()
        {
        }

        //=========================================================================================
        /// <summary>
        /// 物理更新前処理
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public override JobHandle PreUpdate(JobHandle jobHandle)
        {
            return jobHandle;
        }

        //=========================================================================================
        /// <summary>
        /// 物理更新後処理
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public override JobHandle PostUpdate(JobHandle jobHandle)
        {
            if (groupList.Count == 0)
                return jobHandle;

            // ラインの回転調整（ルートラインごと）
            var job1 = new LineRotationJob()
            {
                fixedUpdateCount = Manager.UpdateTime.FixedUpdateCount,

                dataList = dataList.ToJobArray(),
                rootInfoList = rootInfoList.ToJobArray(),
                rootTeamList = rootTeamList.ToJobArray(),
                groupList = groupList.ToJobArray(),

                teamDataList = Manager.Team.teamDataList.ToJobArray(),

                flagList = Manager.Particle.flagList.ToJobArray(),

                posList = Manager.Particle.posList.ToJobArray(),
                rotList = Manager.Particle.rotList.ToJobArray(),
            };
            jobHandle = job1.Schedule(rootTeamList.Length, 8, jobHandle);

            return jobHandle;
        }

        /// <summary>
        /// ラインの回転調整
        /// </summary>
        [BurstCompile]
        struct LineRotationJob : IJobParallelFor
        {
            public int fixedUpdateCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<LineRotationData> dataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<LineRotationRootInfo> rootInfoList;
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
            public NativeArray<float3> posList;

            [NativeDisableParallelForRestriction]
            public NativeArray<quaternion> rotList;

            // ルートラインごと
            public void Execute(int rootIndex)
            {
                // チーム
                int teamIndex = rootTeamList[rootIndex];
                if (teamIndex == 0)
                    return;
                var team = teamDataList[teamIndex];
                if (team.IsActive() == false || team.lineWorkerGroupIndex < 0)
                    return;
                // 一時停止スキップ
                if (team.IsPause())
                    return;

                // このチームがUnityPhysicsでの更新かどうか
                bool isPhysicsUpdate = team.IsPhysicsUpdate();

                // グループデータ
                var gdata = groupList[team.lineWorkerGroupIndex];

                // データ
                var rootInfo = rootInfoList[rootIndex];
                int dstart = gdata.dataChunk.startIndex;
                int dataIndex = rootInfo.startIndex + dstart;
                int dataCount = rootInfo.dataLength;
                int pstart = team.particleChunk.startIndex;

                if (dataCount <= 1)
                    return;

                for (int i = 0; i < dataCount; i++)
                {
                    var data = dataList[dataIndex + i];

                    var pindex = data.vertexIndex;
                    pindex += pstart;

                    var flag = flagList[pindex];
                    if (flag.IsValid() == false)
                        continue;

                    // 自身の現在姿勢
                    var pos = posList[pindex];
                    var rot = rotList[pindex];

                    // 子の回転調整
                    if (data.childCount > 0)
                    {
                        // 子への平均ベクトル
                        float3 ctv = 0;
                        float3 cv = 0;

                        for (int j = 0; j < data.childCount; j++)
                        {
                            var cdata = dataList[data.childStartDataIndex + dstart + j];
                            int cindex = cdata.vertexIndex + pstart;

                            // 子のフラグ
                            var cflag = flagList[cindex];


                            // 子の現在位置
                            var cpos = posList[cindex];

                            // 子の本来のベクトル
                            //float3 tv = math.normalize(math.mul(rot, cdata.localPos));
                            float3 tv = math.normalize(math.mul(rot, cdata.localPos * team.scaleDirection)); // マイナススケール対応(v1.7.6)
                            ctv += tv;

                            // 子の現在ベクトル
                            float3 v = math.normalize(cpos - pos);
                            cv += v;

                            // 子頂点がトライアングル回転姿勢制御されている場合はスキップする
                            if (cflag.IsFlag(PhysicsManagerParticleData.Flag_TriangleRotation))
                                continue;

                            // 回転
                            var q = MathUtility.FromToRotation(tv, v);

                            // 子の仮姿勢を決定
                            //var crot = math.mul(rot, cdata.localRot);
                            var crot = math.mul(rot, new quaternion(cdata.localRot.value * team.quaternionScale)); // マイナススケール対応(v1.7.6)
                            crot = math.mul(q, crot);
                            rotList[cindex] = crot;
                        }

                        // 頂点がトライアングル回転姿勢制御されている場合はスキップする
                        if (flag.IsFlag(PhysicsManagerParticleData.Flag_TriangleRotation))
                            continue;

                        // 固定は回転させない判定(v1.5.2)
                        if (team.IsFlag(PhysicsManagerTeamData.Flag_FixedNonRotation) && flag.IsKinematic())
                            continue;

                        // 子の移動方向変化に伴う回転調整
                        var cq = MathUtility.FromToRotation(ctv, cv);

                        // 回転補間
                        if (gdata.avarage == 1)
                        {
                            cq = math.slerp(quaternion.identity, cq, 0.5f); // 50%
                        }

                        // 自身の姿勢を確定させる
                        rot = math.mul(cq, rot);
                        rotList[pindex] = math.normalize(rot); // 正規化しないとエラーになる場合がある
                    }
                }
            }
        }
    }
}
