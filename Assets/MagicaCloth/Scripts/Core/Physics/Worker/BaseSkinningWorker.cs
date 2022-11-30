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
    /// コライダー移動制限拘束
    /// </summary>
    public class BaseSkinningWorker : PhysicsManagerWorker
    {
        /// <summary>
        /// 移動制限
        /// todo:共有可能
        /// </summary>
        [System.Serializable]
        public struct BaseSkinningData
        {
            /// <summary>
            /// ローカル座標
            /// </summary>
            public float3 localPos;
            public float3 localNor;
            public float3 localTan;

            /// <summary>
            /// スキニングボーン配列インデックス
            /// </summary>
            public int4 boneIndices;

            /// <summary>
            /// ウエイト
            /// </summary>
            public float4 weights;

            /// <summary>
            /// ウエイト数
            /// </summary>
            public short weightCount;

            public bool IsValid()
            {
                return weightCount > 0;
            }
        }
        FixedChunkNativeArray<BaseSkinningData> dataList;

        /// <summary>
        /// バインドポーズ
        /// </summary>
        FixedChunkNativeArray<float4x4> bindPoseList;

        /// <summary>
        /// グループごとの拘束データ
        /// </summary>
        public struct GroupData
        {
            public int teamId;
            public int active;
            public int updateFixed;

            public ChunkData dataChunk;
            public ChunkData bindPoseChunk;
        }
        public FixedNativeList<GroupData> groupList;

        //=========================================================================================
        public override void Create()
        {
            groupList = new FixedNativeList<GroupData>();
            dataList = new FixedChunkNativeArray<BaseSkinningData>();
            bindPoseList = new FixedChunkNativeArray<float4x4>();
        }

        public override void Release()
        {
            groupList.Dispose();
            dataList.Dispose();
            bindPoseList.Dispose();
        }

        public int AddGroup(int teamId, bool active, bool updateFixed, BaseSkinningData[] skinningDataList, float4x4[] skinningBindPoseList)
        {
            var gdata = new GroupData();
            gdata.teamId = teamId;
            gdata.active = active ? 1 : 0;
            gdata.updateFixed = updateFixed ? 1 : 0;
            gdata.dataChunk = dataList.AddChunk(skinningDataList.Length);
            gdata.bindPoseChunk = bindPoseList.AddChunk(skinningBindPoseList.Length);

            // チャンクデータコピー
            dataList.ToJobArray().CopyFromFast(gdata.dataChunk.startIndex, skinningDataList);
            bindPoseList.ToJobArray().CopyFromFast(gdata.bindPoseChunk.startIndex, skinningBindPoseList);

            int group = groupList.Add(gdata);
            return group;
        }


        public override void RemoveGroup(int teamId)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.baseSkinningGroupIndex;
            if (group < 0)
                return;

            var gdata = groupList[group];

            // チャンクデータ削除
            dataList.RemoveChunk(gdata.dataChunk);
            bindPoseList.RemoveChunk(gdata.bindPoseChunk);

            // データ削除
            groupList.Remove(group);
        }

        public void ChangeParam(int teamId, bool updateFixed)
        {
            var teamData = Manager.Team.teamDataList[teamId];
            int group = teamData.baseSkinningGroupIndex;
            if (group < 0)
                return;

            var gdata = groupList[group];
            //gdata.active = active ? 1 : 0;
            gdata.updateFixed = updateFixed ? 1 : 0;
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
            if (groupList.Count == 0)
                return jobHandle;

            var job = new BaseSkinningJob()
            {
                groupList = groupList.ToJobArray(),
                dataList = dataList.ToJobArray(),
                bindPoseList = bindPoseList.ToJobArray(),

                // team
                teamDataList = Manager.Team.teamDataList.ToJobArray(),
                skinningBoneList = Manager.Team.skinningBoneList.ToJobArray(),

                // bone
                bonePosList = Manager.Bone.bonePosList.ToJobArray(),
                boneRotList = Manager.Bone.boneRotList.ToJobArray(),
                boneSclList = Manager.Bone.boneSclList.ToJobArray(),

                // particle
                flagList = Manager.Particle.flagList.ToJobArray(),
                teamIdList = Manager.Particle.teamIdList.ToJobArray(),
                snapBasePosList = Manager.Particle.snapBasePosList.ToJobArray(),
                snapBaseRotList = Manager.Particle.snapBaseRotList.ToJobArray(),
            };
            jobHandle = job.Schedule(Manager.Particle.Length, 64, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct BaseSkinningJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<GroupData> groupList;
            [Unity.Collections.ReadOnly]
            public NativeArray<BaseSkinningData> dataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float4x4> bindPoseList;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> skinningBoneList;

            // bone
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> bonePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> boneRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> boneSclList;


            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> teamIdList;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> snapBasePosList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> snapBaseRotList;

            // パーティクルごと
            public void Execute(int index)
            {
                var flag = flagList[index];
                if (flag.IsValid() == false || flag.IsCollider())
                    return;


                // チーム
                var team = teamIdList[index];
                var teamData = teamDataList[team];
                if (teamData.IsActive() == false)
                    return;
                if (teamData.baseSkinningGroupIndex < 0)
                    return;
                // 更新確認
                //if (teamData.IsUpdate() == false)
                //    return;

                // グループデータ
                var gdata = groupList[teamData.baseSkinningGroupIndex];
                if (gdata.active == 0)
                    return;

                // 固定パーティクルのスキニング判定
                if (gdata.updateFixed == 0 && flag.IsFixed())
                    return;

                // スキニング
                float3 spos = 0;
                float3 snor = 0;
                float3 stan = 0;
                int vindex = index - teamData.particleChunk.startIndex;
                int bindStart = gdata.bindPoseChunk.startIndex;
                int boneStart = teamData.skinningBoneChunk.startIndex;
                var data = dataList[gdata.dataChunk.startIndex + vindex];
                if (data.IsValid())
                {
                    for (int i = 0; i < data.weightCount; i++)
                    {
                        int localBoneIndex = data.boneIndices[i];
                        float weight = data.weights[i];

                        // bind pose
                        float4x4 bindPose = bindPoseList[bindStart + localBoneIndex];
                        float4 lpos = math.mul(bindPose, new float4(data.localPos, 1));
                        float4 lnor = math.mul(bindPose, new float4(data.localNor, 0));
                        float4 ltan = math.mul(bindPose, new float4(data.localTan, 0));

                        // world pose
                        int boneIndex = skinningBoneList[boneStart + localBoneIndex];
                        var bonePos = bonePosList[boneIndex];
                        var boneRot = boneRotList[boneIndex];
                        var boneScl = boneSclList[boneIndex];
                        float3 pos = (bonePos + math.mul(boneRot, lpos.xyz * boneScl)) * weight;
                        float3 nor = math.normalize(math.mul(boneRot, lnor.xyz)) * weight;
                        float3 tan = math.normalize(math.mul(boneRot, ltan.xyz)) * weight;

                        spos += pos;
                        snor += nor;
                        stan += tan;
                    }

                    // 書き込み
                    snapBasePosList[index] = spos;
                    snapBaseRotList[index] = quaternion.LookRotation(snor, stan);
                }
            }
        }

        //=========================================================================================
        public override JobHandle PostUpdate(JobHandle jobHandle)
        {
            return jobHandle;
        }
    }
}
