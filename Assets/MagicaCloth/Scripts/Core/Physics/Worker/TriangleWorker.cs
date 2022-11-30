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
    /// トライアングル回転調整ワーカー
    /// BoneClothでのMesh接続時にトライアングル法線からボーン姿勢を計算するのに使用する
    /// </summary>
    public class TriangleWorker : PhysicsManagerWorker
    {
        [System.Serializable]
        public struct TriangleRotationData
        {
            /// <summary>
            /// 接線計算用頂点インデックス
            /// </summary>
            public int targetIndex;

            /// <summary>
            /// 接続トライアングル数
            /// </summary>
            public int triangleCount;

            /// <summary>
            /// 接続トライアングルの開始データ配列インデックス(triangleIndexList)
            /// </summary>
            public int triangleStartIndex;

            /// <summary>
            /// 基本姿勢からのローカル回転
            /// </summary>
            public quaternion localRot;

            public bool IsValid()
            {
                return triangleCount > 0;
            }
        }
        FixedChunkNativeArray<TriangleRotationData> triangleDataList;

        /// <summary>
        /// 接続トライアングルインデックス情報
        /// </summary>
        FixedChunkNativeArray<int> triangleIndexList;

        /// <summary>
        /// グループごとの拘束データ
        /// </summary>
        public struct GroupData
        {
            public int teamId;
            public ChunkData triangleDataChunk;
            public ChunkData triangleIndexChunk;
        }
        public FixedNativeList<GroupData> groupList;

        //=========================================================================================
        public override void Create()
        {
            triangleDataList = new FixedChunkNativeArray<TriangleRotationData>();
            triangleIndexList = new FixedChunkNativeArray<int>();
            groupList = new FixedNativeList<GroupData>();
        }

        public override void Release()
        {
            triangleDataList.Dispose();
            triangleIndexList.Dispose();
            groupList.Dispose();
        }

        public int AddGroup(
            int teamId,
            TriangleRotationData[] dataArray,
            int[] indexArray
            )
        {
            if (dataArray == null || dataArray.Length == 0 || indexArray == null || indexArray.Length == 0)
                return -1;

            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];

            var gdata = new GroupData();
            gdata.teamId = teamId;
            gdata.triangleDataChunk = triangleDataList.AddChunk(dataArray.Length);
            gdata.triangleIndexChunk = triangleIndexList.AddChunk(indexArray.Length);

            // チャンクデータコピー
            triangleDataList.ToJobArray().CopyFromFast(gdata.triangleDataChunk.startIndex, dataArray);
            triangleIndexList.ToJobArray().CopyFromFast(gdata.triangleIndexChunk.startIndex, indexArray);

            int group = groupList.Add(gdata);

            return group;
        }

        public override void RemoveGroup(int teamId)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.triangleWorkerGroupIndex;
            if (group < 0)
                return;

            var cdata = groupList[group];

            // チャンクデータ削除
            triangleDataList.RemoveChunk(cdata.triangleDataChunk);
            triangleIndexList.RemoveChunk(cdata.triangleIndexChunk);

            // データ削除
            groupList.Remove(group);
        }

        /*
        public void ChangeParam(int teamId, bool avarage)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.lineWorkerGroupIndex;
            if (group < 0)
                return;

            var gdata = groupList[group];
            gdata.avarage = avarage ? 1 : 0;
            groupList[group] = gdata;
        }*/

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

            // トライアングルの回転調整（パーティクルごと）
            var job1 = new TriangleRotationJob()
            {
                triangleDataList = triangleDataList.ToJobArray(),
                triangleIndexList = triangleIndexList.ToJobArray(),
                groupList = groupList.ToJobArray(),

                teamDataList = Manager.Team.teamDataList.ToJobArray(),

                teamIdList = Manager.Particle.teamIdList.ToJobArray(),
                flagList = Manager.Particle.flagList.ToJobArray(),

                posList = Manager.Particle.posList.ToJobArray(),
                rotList = Manager.Particle.rotList.ToJobArray(),
            };
            jobHandle = job1.Schedule(Manager.Particle.Length, 64, jobHandle);

            return jobHandle;
        }

        /// <summary>
        /// トライアングルの回転調整
        /// </summary>
        [BurstCompile]
        struct TriangleRotationJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<TriangleRotationData> triangleDataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> triangleIndexList;
            [Unity.Collections.ReadOnly]
            public NativeArray<GroupData> groupList;

            // チーム
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> teamIdList;
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> posList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> rotList;

            // パーティクルごと
            public void Execute(int index)
            {
                // トライアングル回転調整を行う頂点のみ
                var flag = flagList[index];
                if (flag.IsValid() == false || flag.IsFlag(PhysicsManagerParticleData.Flag_TriangleRotation) == false)
                    return;


                // チーム
                int teamIndex = teamIdList[index];
                var team = teamDataList[teamIndex];
                if (team.IsActive() == false || team.triangleWorkerGroupIndex < 0)
                    return;
                // 一時停止スキップ
                if (team.IsPause())
                    return;

                // 固定は回転させない判定(v1.5.2)
                if (team.IsFlag(PhysicsManagerTeamData.Flag_FixedNonRotation) && flag.IsKinematic())
                    return;

                int pstart = team.particleChunk.startIndex;
                int vindex = index - pstart;
                if (vindex < 0)
                    return;

                // データ
                var gdata = groupList[team.triangleWorkerGroupIndex];
                var data = triangleDataList[gdata.triangleDataChunk.startIndex + vindex];
                if (data.IsValid() == false)
                    return;

                // 接続トライアングルからパーティクル法線を計算する
                float3 nor = 0;
                int tindex = data.triangleStartIndex;
                int tbase = gdata.triangleIndexChunk.startIndex;
                for (int i = 0; i < data.triangleCount; i++)
                {
                    int v0 = triangleIndexList[tbase + tindex];
                    int v1 = triangleIndexList[tbase + tindex + 1];
                    int v2 = triangleIndexList[tbase + tindex + 2];
                    tindex += 3;

                    var pos0 = posList[pstart + v0];
                    var pos1 = posList[pstart + v1];
                    var pos2 = posList[pstart + v2];

                    var n = math.cross(pos1 - pos0, pos2 - pos0);
                    nor += math.normalize(n);
                }
                nor = math.normalize(nor);

                // パーティクル接線を計算する
                var pos = posList[index];
                var tpos = posList[pstart + data.targetIndex];
                var tan = math.normalize(tpos - pos);

                // マイナススケール対応
                nor *= (team.scaleDirection.x * team.scaleDirection.y); // XorYフリップ時に反転
                tan *= team.scaleDirection.y; // Yフリップ時に反転

                // パーティクル姿勢
                var rot = quaternion.LookRotation(nor, tan);
                rot = math.mul(rot, new quaternion(data.localRot.value * team.quaternionScale)); // マイナススケール対応

                rotList[index] = rot;
            }
        }
    }
}
