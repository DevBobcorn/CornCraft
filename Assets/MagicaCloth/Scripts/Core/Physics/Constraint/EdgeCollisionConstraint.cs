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
    /// エッジコリジョン拘束
    /// </summary>
    public class EdgeCollisionConstraint : PhysicsManagerConstraint
    {
        /// <summary>
        /// 拘束データ
        /// todo:共有化可能
        /// </summary>
        [System.Serializable]
        public struct EdgeCollisionData
        {
            /// <summary>
            /// エッジ形成パーティクルインデックス
            /// </summary>
            public ushort vindex0;
            public ushort vindex1;

            /// <summary>
            /// 書き込みバッファインデックス
            /// </summary>
            public int writeIndex0;
            public int writeIndex1;

            /// <summary>
            /// データが有効か判定する
            /// </summary>
            /// <returns></returns>
            public bool IsValid()
            {
                return vindex0 > 0 && vindex1 > 0;
            }
        }
        FixedChunkNativeArray<EdgeCollisionData> dataList;

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
        public struct GroupData
        {
            public int teamId;

            public int active;

            public float edgeRadius;

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
        FixedNativeList<GroupData> groupList;

        //=========================================================================================
        public override void Create()
        {
            dataList = new FixedChunkNativeArray<EdgeCollisionData>();
            groupIndexList = new FixedChunkNativeArray<short>();
            refDataList = new FixedChunkNativeArray<ReferenceDataIndex>();
            writeBuffer = new FixedChunkNativeArray<float3>();
            groupList = new FixedNativeList<GroupData>();
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
        public int AddGroup(int teamId, bool active, float edgeRadius, EdgeCollisionData[] dataArray, ReferenceDataIndex[] refDataArray, int writeBufferCount)
        {
            if (dataArray == null || dataArray.Length == 0 || refDataArray == null || refDataArray.Length == 0 || writeBufferCount == 0)
                return -1;

            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];

            // グループデータ作成
            var gdata = new GroupData();
            gdata.teamId = teamId;
            gdata.active = active ? 1 : 0;
            gdata.edgeRadius = edgeRadius;
            //gdata.stiffness.Setup(stiffness);
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
            int group = teamData.edgeCollisionGroupIndex;
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

        public void ChangeParam(int teamId, bool active, float edgeRadius)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.edgeCollisionGroupIndex;
            if (group < 0)
                return;

            var gdata = groupList[group];
            gdata.active = active ? 1 : 0;
            gdata.edgeRadius = edgeRadius;
            //gdata.stiffness.Setup(stiffness);
            groupList[group] = gdata;
        }

        //public int ActiveCount
        //{
        //    get
        //    {
        //        int cnt = 0;
        //        for (int i = 0; i < groupList.Length; i++)
        //            if (groupList[i].active == 1)
        //                cnt++;
        //        return cnt;
        //    }
        //}

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

            // ステップ１：コリジョンの計算
            var job = new EdgeCollisionCalcJob()
            {
                updatePower = updatePower,
                runCount = runCount,

                groupDataList = groupList.ToJobArray(),
                dataList = dataList.ToJobArray(),
                groupIndexList = groupIndexList.ToJobArray(),

                //colliderMap = Manager.Team.colliderMap.Map,
                colliderList = Manager.Team.colliderList.ToJobArray(),

                teamDataList = Manager.Team.teamDataList.ToJobArray(),

                flagList = Manager.Particle.flagList.ToJobArray(),
                radiusList = Manager.Particle.radiusList.ToJobArray(),
                posList = Manager.Particle.posList.ToJobArray(),
                rotList = Manager.Particle.rotList.ToJobArray(),
                nextPosList = Manager.Particle.InNextPosList.ToJobArray(),
                nextRotList = Manager.Particle.InNextRotList.ToJobArray(),
                localPosList = Manager.Particle.localPosList.ToJobArray(),
                //oldPosList = Manager.Particle.oldPosList.ToJobArray(),
                transformIndexList = Manager.Particle.transformIndexList.ToJobArray(),

                boneSclList = Manager.Bone.boneSclList.ToJobArray(),

                writeBuffer = writeBuffer.ToJobArray(),
            };
            jobHandle = job.Schedule(dataList.Length, 64, jobHandle);

            // ステップ２：コリジョン結果の集計
            var job2 = new EdgeCollisionSumJob()
            {
                runCount = runCount,

                groupDataList = groupList.ToJobArray(),
                refDataList = refDataList.ToJobArray(),
                writeBuffer = writeBuffer.ToJobArray(),

                teamDataList = Manager.Team.teamDataList.ToJobArray(),

                teamIdList = Manager.Particle.teamIdList.ToJobArray(),
                flagList = Manager.Particle.flagList.ToJobArray(),

                inoutNextPosList = Manager.Particle.InNextPosList.ToJobArray(),
                frictionList = Manager.Particle.frictionList.ToJobArray(),
            };
            jobHandle = job2.Schedule(Manager.Particle.Length, 64, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct EdgeCollisionCalcJob : IJobParallelFor
        {
            public float updatePower;
            public int runCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<GroupData> groupDataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<EdgeCollisionData> dataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<short> groupIndexList;

            //[Unity.Collections.ReadOnly]
            //public NativeMultiHashMap<int, int> colliderMap;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> colliderList;

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> radiusList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> posList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> rotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> nextRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPosList;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<float3> oldPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> transformIndexList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> boneSclList;

            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> writeBuffer;

            // エッジデータごと
            public void Execute(int index)
            {
                var data = dataList[index];
                if (data.IsValid() == false)
                    return;

                int gindex = groupIndexList[index];
                var gdata = groupDataList[gindex];
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

                int pindex0 = data.vindex0 + pstart;
                int pindex1 = data.vindex1 + pstart;

                float3 nextpos0 = nextPosList[pindex0];
                float3 nextpos1 = nextPosList[pindex1];

                //float3 oldpos0 = oldPosList[pindex0];
                //float3 oldpos1 = oldPosList[pindex1];

                // エッジの太さ
                float radius = gdata.edgeRadius;

                // 計算結果の移動値をcorrに格納
                // チームごとに判定[グローバル(0)]->[自身のチーム(team)]
                int colliderTeam = 0;
                bool hitresult = false;
                for (int i = 0; i < 2; i++)
                {
                    // チーム内のコライダーをループ
                    var c = teamDataList[colliderTeam].colliderChunk;

                    int dataIndex = c.startIndex;
                    for (int j = 0; j < c.useLength; j++, dataIndex++)
                    {
                        int cindex = colliderList[dataIndex];

                        var cflag = flagList[cindex];
                        if (cflag.IsValid() == false)
                            continue;

                        bool hit = false;

                        if (cflag.IsFlag(PhysicsManagerParticleData.Flag_Plane))
                        {
                            // 平面コライダー判定
                            //hit = PlaneColliderDetection(ref nextpos, radius, cindex);
                        }
                        else if (cflag.IsFlag(PhysicsManagerParticleData.Flag_CapsuleX))
                        {
                            // カプセルコライダー判定
                            hit = CapsuleColliderDetection(nextpos0, nextpos1, ref corr0, ref corr1, radius, cindex, new float3(1, 0, 0));
                            //hit = CapsuleColliderDetection(nextpos0, nextpos1, oldpos0, oldpos1, ref corr0, ref corr1, radius, cindex, new float3(1, 0, 0));
                        }
                        else if (cflag.IsFlag(PhysicsManagerParticleData.Flag_CapsuleY))
                        {
                            // カプセルコライダー判定
                            hit = CapsuleColliderDetection(nextpos0, nextpos1, ref corr0, ref corr1, radius, cindex, new float3(0, 1, 0));
                            //hit = CapsuleColliderDetection(nextpos0, nextpos1, oldpos0, oldpos1, ref corr0, ref corr1, radius, cindex, new float3(0, 1, 0));
                        }
                        else if (cflag.IsFlag(PhysicsManagerParticleData.Flag_CapsuleZ))
                        {
                            // カプセルコライダー判定
                            hit = CapsuleColliderDetection(nextpos0, nextpos1, ref corr0, ref corr1, radius, cindex, new float3(0, 0, 1));
                            //hit = CapsuleColliderDetection(nextpos0, nextpos1, oldpos0, oldpos1, ref corr0, ref corr1, radius, cindex, new float3(0, 0, 1));
                        }
                        else if (cflag.IsFlag(PhysicsManagerParticleData.Flag_Box))
                        {
                            // ボックスコライダー判定
                            // ★まだ未実装
                        }
                        else
                        {
                            // 球コライダー判定
                            hit = SphereColliderDetection(nextpos0, nextpos1, ref corr0, ref corr1, radius, cindex);
                            //hit = SphereColliderDetection(nextpos0, nextpos1, oldpos0, oldpos1, ref corr0, ref corr1, radius, cindex);
                        }

                        hitresult = hit ? true : hitresult;

                        //if (hit)
                        //{
                        //    // 衝突あり！
                        //    // 摩擦設定
                        //    //frictionList[index] = math.max(frictionList[index], teamData.friction);
                        //}
                    }

                    // 自身のチームに切り替え
                    colliderTeam = gdata.teamId;
                }

                // 摩擦係数?

                // 作業バッファへ格納
                int wstart = gdata.writeDataChunk.startIndex;
                int windex0 = data.writeIndex0 + wstart;
                int windex1 = data.writeIndex1 + wstart;
                writeBuffer[windex0] = corr0;
                writeBuffer[windex1] = corr1;
            }

            /// <summary>
            /// 球衝突判定
            /// </summary>
            /// <param name="nextpos0">エッジの始点</param>
            /// <param name="nextpos1">エッジの終点</param>
            /// <param name="corr0"></param>
            /// <param name="corr1"></param>
            /// <param name="radius"></param>
            /// <param name="cindex"></param>
            /// <returns></returns>
            bool SphereColliderDetection(float3 nextpos0, float3 nextpos1, ref float3 corr0, ref float3 corr1, float radius, int cindex)
            //bool SphereColliderDetection(float3 nextpos0, float3 nextpos1, float3 oldpos0, float3 oldpos1, ref float3 corr0, ref float3 corr1, float radius, int cindex)
            {
                var cpos = nextPosList[cindex];
                var coldpos = posList[cindex];
                var cradius = radiusList[cindex];

                // スケール
                var tindex = transformIndexList[cindex];
                var cscl = boneSclList[tindex];
                cradius *= cscl.x; // X軸のみを見る


                // コライダー球とエッジの最接近点を求める
                float3 d = MathUtility.ClosestPtPointSegment(coldpos, nextpos0, nextpos1);
                //float3 d = MathUtility.ClosestPtPointSegment(coldpos, oldpos0, oldpos1);
                float3 n = math.normalize(d - coldpos);
                float3 c = cpos + n * (cradius.x + radius);

                // c = 平面位置
                // n = 平面方向
                // 平面衝突判定と押し出し
                float3 outpos0, outpos1;
                bool ret0 = MathUtility.IntersectPointPlane(c, n, nextpos0, out outpos0);
                bool ret1 = MathUtility.IntersectPointPlane(c, n, nextpos1, out outpos1);

                if (ret0)
                    corr0 += outpos0 - nextpos0;
                if (ret1)
                    corr1 += outpos1 - nextpos1;

                return ret0 || ret1;
            }

            /// <summary>
            /// カプセル衝突判定
            /// </summary>
            /// <param name="nextpos0">エッジの始点</param>
            /// <param name="nextpos1">エッジの終点</param>
            /// <param name="corr0"></param>
            /// <param name="corr1"></param>
            /// <param name="radius"></param>
            /// <param name="cindex"></param>
            /// <param name="dir"></param>
            /// <returns></returns>
            bool CapsuleColliderDetection(float3 nextpos0, float3 nextpos1, ref float3 corr0, ref float3 corr1, float radius, int cindex, float3 dir)
            //bool CapsuleColliderDetection(float3 nextpos0, float3 nextpos1, float3 oldpos0, float3 oldpos1, ref float3 corr0, ref float3 corr1, float radius, int cindex, float3 dir)
            {
                var cpos = nextPosList[cindex];
                var crot = nextRotList[cindex];
                var coldpos = posList[cindex];
                var coldrot = rotList[cindex];

                // x = 長さ（片側）
                // y = 始点半径
                // z = 終点半径
                //var lpos = localPosList[cindex];
                var cradius = radiusList[cindex];

                // スケール
                var tindex = transformIndexList[cindex];
                var cscl = boneSclList[tindex];
                float scl = math.dot(cscl, dir); // dirの軸のスケールを使用する
                cradius *= scl;

                // 移動前のコライダーに対するエッジの最近接点を求める
                float3 oldl = math.mul(coldrot, dir * cradius.x);
                float3 soldpos = coldpos - oldl;
                float3 eoldpos = coldpos + oldl;
                float3 c1, c2;
                float s, t;
                MathUtility.ClosestPtSegmentSegment(soldpos, eoldpos, nextpos0, nextpos1, out s, out t, out c1, out c2);
                //MathUtility.ClosestPtSegmentSegment(soldpos, eoldpos, oldpos0, oldpos1, out s, out t, out c1, out c2);
                float3 v = c2 - c1;

                // 現在のカプセル始点と終点
                float3 l = math.mul(crot, dir * cradius.x);
                float3 spos = cpos - l;
                float3 epos = cpos + l;
                float sr = cradius.y;
                float er = cradius.z;

                // 移動後のコライダーのベクトルに変換する
                var iq = math.inverse(coldrot);
                float3 lv = math.mul(iq, v);
                v = math.mul(crot, lv);

                // コライダーの半径
                float r = math.lerp(sr, er, s);

                // 平面方程式
                float3 n = math.normalize(v);
                float3 q = math.lerp(spos, epos, s);
                float3 c = q + n * (r + radius);

                // c = 平面位置
                // n = 平面方向
                // 平面衝突判定と押し出し
                float3 outpos0, outpos1;
                bool ret0 = MathUtility.IntersectPointPlane(c, n, nextpos0, out outpos0);
                bool ret1 = MathUtility.IntersectPointPlane(c, n, nextpos1, out outpos1);

                if (ret0)
                    corr0 += outpos0 - nextpos0;
                if (ret1)
                    corr1 += outpos1 - nextpos1;

                return ret0 || ret1;
            }
        }

        [BurstCompile]
        struct EdgeCollisionSumJob : IJobParallelFor
        {
            public int runCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<GroupData> groupDataList;
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
            public NativeArray<float> frictionList;

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
                if (team.edgeCollisionGroupIndex < 0)
                    return;

                // 更新確認
                if (team.IsUpdate(runCount) == false)
                    return;

                // グループデータ
                var gdata = groupDataList[team.edgeCollisionGroupIndex];
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

                    // 摩擦
                    //if (math.lengthsq(corr) > 0.00001f)
                    //if (math.lengthsq(corr) > 0.0f)
                    {
                        // 摩擦設定
                        //frictionList[pindex] = math.max(frictionList[pindex], team.friction);
                    }
                }
            }
        }
    }
}
