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
    /// 回転調整ワーカー
    /// </summary>
    public class AdjustRotationWorker : PhysicsManagerWorker
    {
        // 回転調整モード
        const int AdjustMode_Fixed = 0; // 回転はBaseRot固定とする(v1.7.3)
        const int AdjustMode_XYMove = 1;
        const int AdjustMode_XZMove = 2;
        const int AdjustMode_YZMove = 3;

        /// <summary>
        /// 拘束データ
        /// このデータは調整モードがRotationLineの場合のみ必要
        /// </summary>
        [System.Serializable]
        public struct AdjustRotationData
        {
            /// <summary>
            /// キー頂点インデックス
            /// </summary>
            public int keyIndex;

            /// <summary>
            /// ターゲット頂点インデックス
            /// ターゲット頂点インデックスがプラスの場合は子をターゲット、マイナスの場合は親をターゲットとする
            /// マイナスの場合は０を表現するためさらに(-1)されているので注意！
            /// </summary>
            public int targetIndex;

            /// <summary>
            /// ターゲットへのローカル位置（単位ベクトル）
            /// </summary>
            public float3 localPos;

            /// <summary>
            /// データが有効か判定する
            /// </summary>
            /// <returns></returns>
            public bool IsValid()
            {
                return keyIndex != 0 || targetIndex != 0;
            }
        }
        FixedChunkNativeArray<AdjustRotationData> dataList;

        /// <summary>
        /// グループごとの拘束データ
        /// </summary>
        public struct GroupData
        {
            public int teamId;
            public int active;

            /// <summary>
            /// 調整方法
            /// </summary>
            public int adjustMode;

            /// <summary>
            /// AdjustModeがXY/XZ/YZMoveのときの各軸ごとの回転力
            /// </summary>
            public float3 axisRotationPower;

            public ChunkData chunk;
        }
        public FixedNativeList<GroupData> groupList;

        /// <summary>
        /// パーティクルごとの拘束データ
        /// </summary>
        ExNativeMultiHashMap<int, int> particleMap;

        //=========================================================================================
        public override void Create()
        {
            dataList = new FixedChunkNativeArray<AdjustRotationData>();
            groupList = new FixedNativeList<GroupData>();
            particleMap = new ExNativeMultiHashMap<int, int>();
        }

        public override void Release()
        {
            dataList.Dispose();
            groupList.Dispose();
            particleMap.Dispose();
        }

        public int AddGroup(int teamId, bool active, int adjustMode, float3 axisRotationPower, AdjustRotationData[] dataArray)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];

            var gdata = new GroupData();
            gdata.teamId = teamId;
            gdata.active = active ? 1 : 0;
            gdata.adjustMode = adjustMode;
            gdata.axisRotationPower = axisRotationPower;
            if (dataArray != null && dataArray.Length > 0)
            {
                // モードがRotationLineのみデータがある
                var c = this.dataList.AddChunk(dataArray.Length);
                gdata.chunk = c;

                // チャンクデータコピー
                dataList.ToJobArray().CopyFromFast(c.startIndex, dataArray);

                // パーティクルごとのデータリンク
                int pstart = teamData.particleChunk.startIndex;
                for (int i = 0; i < dataArray.Length; i++)
                {
                    var data = dataArray[i];
                    int dindex = c.startIndex + i;
                    particleMap.Add(pstart + data.keyIndex, dindex);
                }
            }

            int group = groupList.Add(gdata);
            return group;
        }

        public override void RemoveGroup(int teamId)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.adjustRotationGroupIndex;
            if (group < 0)
                return;

            var cdata = groupList[group];

            // パーティクルごとのデータリンク解除
            if (cdata.chunk.dataLength > 0)
            {
                int dstart = cdata.chunk.startIndex;
                int pstart = teamData.particleChunk.startIndex;
                for (int i = 0; i < cdata.chunk.dataLength; i++)
                {
                    int dindex = dstart + i;
                    var data = dataList[dindex];
                    particleMap.Remove(pstart + data.keyIndex, dindex);
                }

                // チャンクデータ削除
                dataList.RemoveChunk(cdata.chunk);
            }

            // データ削除
            groupList.Remove(group);
        }

        public void ChangeParam(int teamId, bool active, int adjustMode, float3 axisRotationPower)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.adjustRotationGroupIndex;
            if (group < 0)
                return;

            var gdata = groupList[group];
            gdata.active = active ? 1 : 0;
            gdata.adjustMode = adjustMode;
            gdata.axisRotationPower = axisRotationPower;
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

            // 回転調整拘束（パーティクルごとに実行する）
            var job1 = new AdjustRotationJob()
            {
                dataList = dataList.ToJobArray(),
                groupList = groupList.ToJobArray(),
                particleMap = particleMap.Map,

                teamDataList = Manager.Team.teamDataList.ToJobArray(),
                teamIdList = Manager.Particle.teamIdList.ToJobArray(),

                flagList = Manager.Particle.flagList.ToJobArray(),
                //basePosList = Manager.Particle.basePosList.ToJobArray(),
                //baseRotList = Manager.Particle.baseRotList.ToJobArray(),
                snapBasePosList = Manager.Particle.snapBasePosList.ToJobArray(),
                snapBaseRotList = Manager.Particle.snapBaseRotList.ToJobArray(),
                posList = Manager.Particle.posList.ToJobArray(),

                rotList = Manager.Particle.rotList.ToJobArray(),
            };
            jobHandle = job1.Schedule(Manager.Particle.Length, 64, jobHandle);

            return jobHandle;
        }

        /// <summary>
        /// 回転調整ジョブ
        /// パーティクルごとに計算
        /// </summary>
        [BurstCompile]
        struct AdjustRotationJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<AdjustRotationData> dataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<GroupData> groupList;
            [Unity.Collections.ReadOnly]
#if MAGICACLOTH_USE_COLLECTIONS_130 && !MAGICACLOTH_USE_COLLECTIONS_200
            public NativeParallelMultiHashMap<int, int> particleMap;
#else
            public NativeMultiHashMap<int, int> particleMap;
#endif

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> teamIdList;

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<float3> basePosList;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<quaternion> baseRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> snapBasePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> snapBaseRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> posList;

            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> rotList;

            /// <summary>
            /// パーティクルごと
            /// </summary>
            /// <param name="index"></param>
            public void Execute(int index)
            {
                var flag = flagList[index];
                if (flag.IsValid() == false)
                    return;

                // チーム
                var team = teamDataList[teamIdList[index]];
                if (team.IsActive() == false || team.adjustRotationGroupIndex < 0)
                    return;
                // 一時停止スキップ
                if (team.IsPause())
                    return;
                int start = team.particleChunk.startIndex;

                // グループデータ
                var gdata = groupList[team.adjustRotationGroupIndex];
                if (gdata.active == 0)
                    return;

                // 情報
                //quaternion baserot = baseRotList[index]; // 常に本来の回転から算出する
                quaternion baserot = snapBaseRotList[index]; // 常に本来の回転から算出する
                var nextrot = baserot;

                // 回転調整
                var nextpos = posList[index];

                if (gdata.adjustMode == AdjustMode_Fixed)
                {
                    // モード[Fixed]では単にBaseRotを同期する
                }
                else
                {
                    // 移動ベクトルベース
                    // 移動ローカルベクトル
                    //var lpos = nextpos - basePosList[index];
                    var lpos = nextpos - snapBasePosList[index];
                    lpos /= team.scaleRatio; // チームスケール倍率
                    lpos = math.mul(math.inverse(baserot), lpos);

                    // 軸ごとの回転力
                    lpos *= gdata.axisRotationPower;

                    // ローカル回転
                    quaternion lq = quaternion.identity;
                    if (gdata.adjustMode == AdjustMode_XYMove)
                    {
                        lq = quaternion.EulerZXY(-lpos.y, lpos.x, 0);
                    }
                    else if (gdata.adjustMode == AdjustMode_XZMove)
                    {
                        lq = quaternion.EulerZXY(lpos.z, 0, -lpos.x);
                    }
                    else if (gdata.adjustMode == AdjustMode_YZMove)
                    {
                        lq = quaternion.EulerZXY(0, lpos.z, -lpos.y);
                    }

                    // 最終回転
                    nextrot = math.mul(nextrot, lq);
                    nextrot = math.normalize(nextrot); // 正規化しないとエラーになる場合がある
                }

                // 書き出し
                rotList[index] = nextrot;
            }
        }
    }
}
