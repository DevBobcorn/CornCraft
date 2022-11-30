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
    /// 浸透制限拘束
    /// </summary>
    public class PenetrationConstraint : PhysicsManagerConstraint
    {
        /// <summary>
        /// 浸透制限データ
        /// todo:共有可能
        /// </summary>
        [System.Serializable]
        public struct PenetrationData
        {
            /// <summary>
            /// 計算頂点インデックス
            /// </summary>
            public short vertexIndex;

            /// <summary>
            /// コライダー配列インデックス
            /// </summary>
            public short colliderIndex;

            /// <summary>
            /// コライダーローカル座標（中心軸）
            /// </summary>
            public float3 localPos;

            /// <summary>
            /// 押し出しローカル方向（単位ベクトル）
            /// </summary>
            public float3 localDir;

            /// <summary>
            /// パーティクルへの距離（オリジナル位置）
            /// </summary>
            public float distance;

            public bool IsValid()
            {
                return vertexIndex >= 0;
            }
        }
        FixedChunkNativeArray<PenetrationData> dataList;

        /// <summary>
        /// ローカルパーティクルインデックスごとのデータ参照情報
        /// </summary>
        FixedChunkNativeArray<ReferenceDataIndex> refDataList;

        /// <summary>
        /// BonePenetration用データ
        /// 頂点に対するローカル浸透方向
        /// </summary>
        FixedChunkNativeArray<float3> bonePenetrationDataList;

        /// <summary>
        /// グループごとの拘束データ
        /// </summary>
        public struct GroupData
        {
            public int teamId;
            public int active;

            /// <summary>
            /// (0=Surface, 1=Collider, 2=Bone)
            /// </summary>
            public int mode;

            public float maxDepth;
            public CurveParam radius;
            public CurveParam distance;

            public ChunkData dataChunk;
            public ChunkData refDataChunk;
            public ChunkData bonePenetrationDataChunk;
        }
        public FixedNativeList<GroupData> groupList;

        //=========================================================================================
        public override void Create()
        {
            groupList = new FixedNativeList<GroupData>();
            dataList = new FixedChunkNativeArray<PenetrationData>();
            refDataList = new FixedChunkNativeArray<ReferenceDataIndex>();
            bonePenetrationDataList = new FixedChunkNativeArray<float3>();
        }

        public override void Release()
        {
            groupList.Dispose();
            dataList.Dispose();
            refDataList.Dispose();
            bonePenetrationDataList.Dispose();
        }

        public int AddGroup(
            int teamId,
            bool active,
            ClothParams.PenetrationMode mode,
            BezierParam distance,
            BezierParam radius,
            float maxDepth,
            PenetrationData[] moveLimitDataList,
            ReferenceDataIndex[] refDataArray,
            float3[] bonePenetrationDataArray
            )
        {
            //var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];

            var gdata = new GroupData();
            gdata.teamId = teamId;
            gdata.active = active ? 1 : 0;
            gdata.mode = (int)mode;
            gdata.distance.Setup(distance);
            gdata.radius.Setup(radius);
            gdata.maxDepth = maxDepth;
            if (moveLimitDataList != null && moveLimitDataList.Length > 0)
            {
                gdata.dataChunk = dataList.AddChunk(moveLimitDataList.Length);
                gdata.refDataChunk = refDataList.AddChunk(refDataArray.Length);

                // チャンクデータコピー
                dataList.ToJobArray().CopyFromFast(gdata.dataChunk.startIndex, moveLimitDataList);
                refDataList.ToJobArray().CopyFromFast(gdata.refDataChunk.startIndex, refDataArray);
            }
            if (bonePenetrationDataArray != null && bonePenetrationDataArray.Length > 0)
            {
                gdata.bonePenetrationDataChunk = bonePenetrationDataList.AddChunk(bonePenetrationDataArray.Length);
                // チャンクデータコピー
                bonePenetrationDataList.ToJobArray().CopyFromFast(gdata.bonePenetrationDataChunk.startIndex, bonePenetrationDataArray);
            }

            int group = groupList.Add(gdata);
            return group;
        }


        public override void RemoveTeam(int teamId)
        {
            var teamData = MagicaPhysicsManager.Instance.Team.teamDataList[teamId];
            int group = teamData.penetrationGroupIndex;
            if (group < 0)
                return;

            var gdata = groupList[group];

            // チャンクデータ削除
            dataList.RemoveChunk(gdata.dataChunk);
            refDataList.RemoveChunk(gdata.refDataChunk);
            bonePenetrationDataList.RemoveChunk(gdata.bonePenetrationDataChunk);

            // データ削除
            groupList.Remove(group);
        }

        public void ChangeParam(int teamId, bool active, BezierParam distance, BezierParam radius, float maxDepth)
        {
            var teamData = Manager.Team.teamDataList[teamId];
            int group = teamData.penetrationGroupIndex;
            if (group < 0)
                return;

            var gdata = groupList[group];
            gdata.active = active ? 1 : 0;
            gdata.distance.Setup(distance);
            gdata.radius.Setup(radius);
            gdata.maxDepth = maxDepth;
            groupList[group] = gdata;
        }

        //=========================================================================================
        public override JobHandle SolverConstraint(int runCount, float dtime, float updatePower, int iteration, JobHandle jobHandle)
        {
            if (groupList.Count == 0)
                return jobHandle;

            // 移動制限拘束
            var job1 = new PenetrationJob()
            {
                runCount = runCount,

                groupList = groupList.ToJobArray(),
                dataList = dataList.ToJobArray(),
                refDataList = refDataList.ToJobArray(),
                bonePenetrationDataList = bonePenetrationDataList.ToJobArray(),

                flagList = Manager.Particle.flagList.ToJobArray(),
                teamIdList = Manager.Particle.teamIdList.ToJobArray(),
                nextPosList = Manager.Particle.InNextPosList.ToJobArray(),
                nextRotList = Manager.Particle.InNextRotList.ToJobArray(),
                transformIndexList = Manager.Particle.transformIndexList.ToJobArray(),
                depthList = Manager.Particle.depthList.ToJobArray(),
                basePosList = Manager.Particle.basePosList.ToJobArray(),
                baseRotList = Manager.Particle.baseRotList.ToJobArray(),

                colliderList = Manager.Team.colliderList.ToJobArray(),

                bonePosList = Manager.Bone.bonePosList.ToJobArray(),
                boneRotList = Manager.Bone.boneRotList.ToJobArray(),
                boneSclList = Manager.Bone.boneSclList.ToJobArray(),

                teamDataList = Manager.Team.teamDataList.ToJobArray(),
                skinningBoneList = Manager.Team.skinningBoneList.ToJobArray(),

                outNextPosList = Manager.Particle.OutNextPosList.ToJobArray(),
                posList = Manager.Particle.posList.ToJobArray(),
                //frictionList = Manager.Particle.frictionList.ToJobArray(),
            };
            jobHandle = job1.Schedule(Manager.Particle.Length, 64, jobHandle);
            Manager.Particle.SwitchingNextPosList();

            return jobHandle;
        }

        //=========================================================================================
        /// <summary>
        /// 浸透制限拘束ジョブ
        /// パーティクルごとに計算
        /// </summary>
        [BurstCompile]
        struct PenetrationJob : IJobParallelFor
        {
            public int runCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<GroupData> groupList;
            [Unity.Collections.ReadOnly]
            public NativeArray<PenetrationData> dataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<ReferenceDataIndex> refDataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> bonePenetrationDataList;

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> teamIdList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> nextRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> transformIndexList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> baseRotList;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> colliderList;

            // bone
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> bonePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> boneRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> boneSclList;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> skinningBoneList;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> outNextPosList;
            public NativeArray<float3> posList;
            //public NativeArray<float> frictionList;

            // パーティクルごと
            public void Execute(int index)
            {
                // 初期化コピー
                float3 nextpos = nextPosList[index];
                outNextPosList[index] = nextpos;

                var flag = flagList[index];
                if (flag.IsValid() == false || flag.IsFixed() || flag.IsCollider())
                    return;

                // チーム
                var team = teamIdList[index];
                var teamData = teamDataList[team];
                if (teamData.IsActive() == false)
                    return;
                if (teamData.penetrationGroupIndex < 0)
                    return;
                // 更新確認
                if (teamData.IsUpdate(runCount) == false)
                    return;

                // グループデータ
                var gdata = groupList[teamData.penetrationGroupIndex];
                if (gdata.active == 0)
                    return;

                int vindex = index - teamData.particleChunk.startIndex;
                var oldpos = nextpos;

                // depth
                var depth = depthList[index];

                // move radius
                var moveradius = gdata.radius.Evaluate(depth);

                // 浸透距離
                float distance = gdata.distance.Evaluate(depth);

                // チームスケール倍率
                float3 scaleDirection = teamData.scaleDirection;
                float teamScale = teamData.scaleRatio;
                distance *= teamScale;
                moveradius *= teamScale;
                //Debug.Log(teamScale);

                // モード別処理
                if (gdata.mode == 0)
                {
                    // Surface Penetration
                    // データ参照情報
                    var refdata = refDataList[gdata.refDataChunk.startIndex + vindex];
                    if (refdata.count > 0)
                    {
                        // ベース位置から算出する
                        var bpos = basePosList[index];
                        var brot = baseRotList[index];
                        int dindex = refdata.startIndex;
                        var data = dataList[gdata.dataChunk.startIndex + dindex];

                        if (data.IsValid())
                        {
                            //float3 n = math.mul(brot, data.localDir);
                            float3 n = math.mul(brot, data.localDir * scaleDirection); // マイナススケール対応

                            // 球の位置
                            var c = bpos + n * (distance - moveradius);

                            // 球内部制限
                            var v = nextpos - c;
                            var len = math.length(v);
                            if (len > moveradius)
                            {
                                v *= (moveradius / len);
                                nextpos = c + v;
                            }
                        }
                    }
                }
                else if (gdata.mode == 1)
                {
                    // Collider Penetration
                    // データ参照情報
                    var refdata = refDataList[gdata.refDataChunk.startIndex + vindex];
                    if (refdata.count > 0)
                    {
                        // 球内制限
                        float3 c = 0;
                        int ccnt = 0;

                        int dindex = refdata.startIndex;
                        for (int i = 0; i < refdata.count; i++, dindex++)
                        {
                            var data = dataList[gdata.dataChunk.startIndex + dindex];
                            if (data.IsValid())
                            {
                                int cindex = colliderList[teamData.colliderChunk.startIndex + data.colliderIndex];

                                var cflag = flagList[cindex];
                                if (cflag.IsValid() == false)
                                    continue;

                                // 球内部制限
                                c += InverseSpherePosition(ref data, teamScale, scaleDirection, distance, cindex, moveradius);
                                ccnt++;
                            }
                        }

                        if (ccnt > 0)
                        {
                            c /= ccnt;
                            var opos = InverseSpherePenetration(c, moveradius, nextpos);
                            var addv = (opos - nextpos);

                            // stiffness test
                            //addv *= 0.25f;

                            // 摩擦を入れてみる
                            //float friction = math.length(addv) * 10.0f;
                            //frictionList[index] = math.max(friction, frictionList[index]); // 大きい方

                            nextpos += addv;
                        }
                    }
                }
                else if (gdata.mode == 2)
                {
                    // Bone Penetration
                    if (depth <= gdata.maxDepth)
                    {
                        float3 basePos = basePosList[index];
                        quaternion baseRot = baseRotList[index];
                        float3 ln = bonePenetrationDataList[gdata.bonePenetrationDataChunk.startIndex + vindex];
                        float3 n = math.mul(baseRot, ln);

#if true
                        // 球の位置
                        var c = basePos + n * (moveradius - math.min(distance, moveradius));
                        //var c = basePos + n * (-distance + moveradius);

                        // 球内部制限
                        var v = nextpos - c;
                        var len = math.length(v);
                        if (len > moveradius)
                        {
                            v *= (moveradius / len);
                            nextpos = c + v;
                        }
#endif
#if false
                        // 平面押し出し
                        var c = basePos - n * (distance);
                        MathUtility.IntersectPointPlane(c, n, nextpos, out nextpos);
#endif
#if false
                        // 逆バンク
                        // 球の位置
                        var c = basePos - n * (distance + moveradius);

                        // 球外部制限
                        var v = nextpos - c;
                        var len = math.length(v);
                        if (len < moveradius)
                        {
                            v *= (moveradius / len);
                            nextpos = c + v;
                        }
#endif

                        // test
                        //nextpos = basePos;

                        // 速度影響
                        const float velocityInfluence = (1.0f - 0.3f); // 0.2f?
                        posList[index] += (nextpos - oldpos) * velocityInfluence;
                    }
                }

                // 書き戻し
                outNextPosList[index] = nextpos;
            }

            //=====================================================================================
            /// <summary>
            /// 内球制限
            /// </summary>
            /// <param name="data"></param>
            /// <param name="distance"></param>
            /// <param name="cindex"></param>
            /// <param name="cr"></param>
            /// <param name="nextpos"></param>
            /// <param name="outpos"></param>
            /// <returns></returns>
            /*private bool InverseSpherePenetration(ref PenetrationData data, float teamScale, float distance, int cindex, float cr, float3 nextpos, out float3 outpos)
            {
                var cpos = nextPosList[cindex];
                var crot = nextRotList[cindex];

                // スケール
                var tindex = transformIndexList[cindex];
                var cscl = boneSclList[tindex];

                // 中心軸
                var d = math.mul(crot, data.localPos * cscl) + cpos;

                // 方向
                var n = math.mul(crot, data.localDir);

                // 球の位置
                var c = d + n * (data.distance * teamScale - distance + cr);

                // 球内部制限
                var v = nextpos - c;
                var len = math.length(v);
                if (len > cr)
                {
                    v *= (cr / len);
                    outpos = c + v;
                    return true;
                }
                else
                {
                    outpos = nextpos;
                    return false;
                }
            }*/

            /// <summary>
            /// 内球制限の中心位置を求める
            /// </summary>
            /// <param name="data"></param>
            /// <param name="teamScale"></param>
            /// <param name="distance">チームスケール済み</param>
            /// <param name="cindex"></param>
            /// <param name="cr">チームスケール済み</param>
            /// <returns></returns>
            private float3 InverseSpherePosition(ref PenetrationData data, float teamScale, float3 scaleDirection, float distance, int cindex, float cr)
            {
                var cpos = nextPosList[cindex];
                var crot = nextRotList[cindex];

                // スケール
                var tindex = transformIndexList[cindex];
                var cscl = boneSclList[tindex];

                // 中心軸
                var d = math.mul(crot, data.localPos * cscl) + cpos;

                // 方向
                //var n = math.mul(crot, data.localDir);
                var n = math.mul(crot, data.localDir * scaleDirection); // マイナススケール対応

                // 球の位置
                var c = d + n * (data.distance * teamScale - distance + cr);

                return c;
            }

            /// <summary>
            /// 内球移動制限をかける
            /// </summary>
            /// <param name="c"></param>
            /// <param name="cr"></param>
            /// <param name="nextpos"></param>
            /// <returns></returns>
            private float3 InverseSpherePenetration(float3 c, float cr, float3 nextpos)
            {
                // 球内部制限
                var v = nextpos - c;
                var len = math.length(v);
                if (len > cr)
                {
                    v *= (cr / len);
                    return c + v;
                }
                else
                {
                    return nextpos;
                }
            }

#if false
            /// <summary>
            /// 平面制限
            /// </summary>
            /// <param name="data"></param>
            /// <param name="cindex"></param>
            /// <param name="nextpos"></param>
            /// <param name="outpos"></param>
            /// <returns></returns>
            private bool PlanePenetration(ref PenetrationData data, float teamScale, float distance, int cindex, float3 nextpos, out float3 outpos)
            {
                var cpos = nextPosList[cindex];
                var crot = nextRotList[cindex];

                // スケール
                var tindex = transformIndexList[cindex];
                var cscl = boneSclList[tindex];

                // 中心軸
                var d = math.mul(crot, data.localPos * cscl) + cpos;

                // 方向
                var n = math.mul(crot, data.localDir);

                // 押し出し平面を求める
                var c = d + n * (data.distance * teamScale - distance);

                // c = 平面位置
                // n = 平面方向
                // 平面衝突判定と押し出し
                return MathUtility.IntersectPointPlane(c, n, nextpos, out outpos);
            }

            private void InversePlanePosition(ref PenetrationData data, float teamScale, float distance, int cindex, out float3 center, out float3 dir)
            {
                var cpos = nextPosList[cindex];
                var crot = nextRotList[cindex];

                // スケール
                var tindex = transformIndexList[cindex];
                var cscl = boneSclList[tindex];

                // 中心軸
                var d = math.mul(crot, data.localPos * cscl) + cpos;

                // 方向
                var n = math.mul(crot, data.localDir);

                // プレーン位置
                var c = d + n * (data.distance * teamScale - distance);

                center = c;
                dir = n;
            }
#endif

#if false
            /// <summary>
            /// 角度制限
            /// </summary>
            /// <param name="data"></param>
            /// <param name="cindex"></param>
            /// <param name="nextpos"></param>
            /// <param name="outpos"></param>
            /// <param name="ang"></param>
            /// <returns></returns>
            private bool AnglePenetration(ref PenetrationData data, int cindex, float3 nextpos, out float3 outpos, float ang)
            {
                var cpos = nextPosList[cindex];
                var crot = nextRotList[cindex];

                // スケール
                var tindex = transformIndexList[cindex];
                var cscl = boneSclList[tindex];
                //float scl = cscl.x; // X軸を採用（基本的には均等スケールのみを想定）

                // 押し出し平面を求める
                var c = math.mul(crot, data.localPos * cscl) + cpos;
                var n = math.mul(crot, data.localDir);

                var v = nextpos - c;

                float3 v2;
                if (MathUtility.ClampAngle(v, n, ang, out v2))
                {
                    outpos = c + v2;
                    return true;
                }

                outpos = nextpos;
                return false;
            }
#endif
        }
    }
}
