// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MagicaCloth
{
    /// <summary>
    /// スプリングメッシュワーカー
    /// </summary>
    public class SpringMeshWorker : PhysicsManagerWorker
    {
        public struct SpringData
        {
            /// <summary>
            /// 対象物理パーティクルインデックス
            /// </summary>
            public int particleIndex;

            /// <summary>
            /// ウエイト(0.0-1.0)
            /// </summary>
            public float weight;
        }

        /// <summary>
        /// 頂点ごとのスプリングデータ
        /// </summary>
        ExNativeMultiHashMap<int, SpringData> springMap;

        /// <summary>
        /// 使用する頂点リスト
        /// </summary>
        FixedNativeListWithCount<int> springVertexList;

        /// <summary>
        /// ID生成シード
        /// </summary>
        //int idSeed;

        /// <summary>
        /// グループごとの作成データ
        /// </summary>
        /// <returns></returns>
        Dictionary<int, List<int>> groupIndexDict = new Dictionary<int, List<int>>();

        //=========================================================================================
        public override void Create()
        {
            springMap = new ExNativeMultiHashMap<int, SpringData>();
            springVertexList = new FixedNativeListWithCount<int>();
            springVertexList.SetEmptyElement(-1);
        }

        public override void Release()
        {
            springMap.Dispose();
            springVertexList.Dispose();
        }

        //=========================================================================================
        /// <summary>
        /// スプリング頂点追加
        /// </summary>
        /// <param name="group"></param>
        /// <param name="vertexIndex">自身のメッシュ頂点インデックス</param>
        /// <param name="particleIndex">対象のパーティクルインデックス</param>
        /// <param name="weight">比重(0.0-1.0)</param>
        /// <returns></returns>
        public void Add(int group, int vertexIndex, int particleIndex, float weight)
        {
            var data = new SpringData()
            {
                particleIndex = particleIndex,
                weight = math.saturate(weight)
            };
            springMap.Add(vertexIndex, data);
            springVertexList.Add(vertexIndex);

            if (groupIndexDict.ContainsKey(group) == false)
            {
                groupIndexDict.Add(group, new List<int>());
            }
            groupIndexDict[group].Add(vertexIndex);
        }

        /// <summary>
        /// 削除（グループ）
        /// </summary>
        /// <param name="group"></param>
        public override void RemoveGroup(int group)
        {
            if (groupIndexDict.ContainsKey(group))
            {
                var clist = groupIndexDict[group];
                foreach (var index in clist)
                {
                    springVertexList.Remove(index);
                    springMap.Remove(index);
                }
                groupIndexDict.Remove(group);
            }
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
        public override JobHandle PreUpdate(JobHandle jobHandle)
        {
            // 何もなし
            return jobHandle;
        }

        //=========================================================================================
        public override JobHandle PostUpdate(JobHandle jobHandle)
        {
            if (springMap.Count == 0)
                return jobHandle;

            var job = new SpringJob()
            {
                springVertexList = springVertexList.ToJobArray(),
                springMap = springMap.Map,

                flagList = Manager.Particle.flagList.ToJobArray(),
                particlePosList = Manager.Particle.posList.ToJobArray(),
                particleRotList = Manager.Particle.rotList.ToJobArray(),
                //particleBasePosList = Manager.Particle.basePosList.ToJobArray(),
                //particleBaseRotList = Manager.Particle.baseRotList.ToJobArray(),
                snapBasePosList = Manager.Particle.snapBasePosList.ToJobArray(),
                snapBaseRotList = Manager.Particle.snapBaseRotList.ToJobArray(),

                virtualPosList = Manager.Mesh.virtualPosList.ToJobArray(),
                virtualVertexFlagList = Manager.Mesh.virtualVertexFlagList.ToJobArray(),
                virtualVertexMeshIndexList = Manager.Mesh.virtualVertexMeshIndexList.ToJobArray(),

                virtualMeshInfoList = Manager.Mesh.virtualMeshInfoList.ToJobArray(),
            };
            jobHandle = job.Schedule(springVertexList.Length, 64, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        private struct SpringJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> springVertexList;
            [Unity.Collections.ReadOnly]
#if MAGICACLOTH_USE_COLLECTIONS_130
            public NativeParallelMultiHashMap<int, SpringData> springMap;
#else
            public NativeMultiHashMap<int, SpringData> springMap;
#endif

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> particlePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> particleRotList;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<float3> particleBasePosList;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<quaternion> particleBaseRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> snapBasePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> snapBaseRotList;

            [NativeDisableParallelForRestriction]
            public NativeArray<float3> virtualPosList;
            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<byte> virtualVertexFlagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<short> virtualVertexMeshIndexList;

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerMeshData.VirtualMeshInfo> virtualMeshInfoList;

#if MAGICACLOTH_USE_COLLECTIONS_130
            NativeParallelMultiHashMapIterator<int> iterator;
#else
            NativeMultiHashMapIterator<int> iterator;
#endif

            // スプリング対象頂点ごと
            public void Execute(int index)
            {
                int vindex = springVertexList[index];
                if (vindex < 0)
                    return;

                // 仮想インスタンスメッシュ情報
                int mindex = virtualVertexMeshIndexList[vindex];
                var m_minfo = virtualMeshInfoList[mindex - 1]; // (-1)するので注意！
                if (m_minfo.IsUse() == false)
                    return;
                // 停止判定
                if (m_minfo.IsPause())
                    return;

                SpringData data;

                // 頂点のトータルウエイトを求める
                float totalWeight = 0;
                if (springMap.TryGetFirstValue(vindex, out data, out iterator))
                {
                    do
                    {
                        var flag = flagList[data.particleIndex];
                        if (flag.IsValid())
                            totalWeight += data.weight;
                    }
                    while (springMap.TryGetNextValue(out data, ref iterator));
                }

                if (totalWeight > 0 && springMap.TryGetFirstValue(vindex, out data, out iterator))
                {
                    var vpos = virtualPosList[vindex];
                    float3 pos = 0;

                    do
                    {
                        int pindex = data.particleIndex;
                        var flag = flagList[data.particleIndex];
                        if (flag.IsValid() == false)
                            continue;

                        // パーティクル現在姿勢
                        var ppos = particlePosList[pindex];
                        var prot = particleRotList[pindex];

                        // パーティクル原点姿勢
                        //var pbpos = particleBasePosList[pindex];
                        //var pbrot = particleBaseRotList[pindex];
                        var pbpos = snapBasePosList[pindex];
                        var pbrot = snapBaseRotList[pindex];
                        var ivpbrot = math.inverse(pbrot);

                        // (1)パーティクルBaseからの相対位置
                        var lpos = math.mul(ivpbrot, (vpos - pbpos));

                        // (2)パーティクル現在地からの頂点位置
                        var npos = math.mul(prot, lpos) + ppos;

                        // (3)ウエイト
                        npos = math.lerp(vpos, npos, data.weight);

                        // (4)ウエイト乗算
                        pos += npos * (data.weight / totalWeight);
                    }
                    while (springMap.TryGetNextValue(out data, ref iterator));

                    // 結果格納
                    virtualPosList[vindex] = pos;

                    // 仮想メッシュの法線／接線計算フラグ
                    virtualVertexFlagList[vindex] = PhysicsManagerMeshData.VirtualVertexFlag_Use;
                }
            }
        }
    }
}
