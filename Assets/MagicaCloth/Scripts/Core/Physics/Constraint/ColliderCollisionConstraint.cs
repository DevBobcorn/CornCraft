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
    /// コライダーコリジョン拘束
    /// </summary>
    public class ColliderCollisionConstraint : PhysicsManagerConstraint
    {
        public override void Create()
        {
        }

        public override void RemoveTeam(int teamId)
        {
        }

        public void ChangeParam(int teamId, bool useCollision)
        {
            Manager.Team.SetFlag(teamId, PhysicsManagerTeamData.Flag_Collision, useCollision);
        }

        public override void Release()
        {
        }

        //=========================================================================================
        public override JobHandle SolverConstraint(int runCount, float dtime, float updatePower, int iteration, JobHandle jobHandle)
        {
            if (Manager.Particle.ColliderCount <= 0)
                return jobHandle;

            // コリジョン拘束
            var job1 = new CollisionJob()
            {
                runCount = runCount,

                flagList = Manager.Particle.flagList.ToJobArray(),
                teamIdList = Manager.Particle.teamIdList.ToJobArray(),
                radiusList = Manager.Particle.radiusList.ToJobArray(),
                nextPosList = Manager.Particle.InNextPosList.ToJobArray(),
                nextRotList = Manager.Particle.InNextRotList.ToJobArray(),
                posList = Manager.Particle.posList.ToJobArray(),
                rotList = Manager.Particle.rotList.ToJobArray(),
                localPosList = Manager.Particle.localPosList.ToJobArray(),
                basePosList = Manager.Particle.basePosList.ToJobArray(),
                baseRotList = Manager.Particle.baseRotList.ToJobArray(),
                transformIndexList = Manager.Particle.transformIndexList.ToJobArray(),

                colliderList = Manager.Team.colliderList.ToJobArray(),

                boneSclList = Manager.Bone.boneSclList.ToJobArray(),

                teamDataList = Manager.Team.teamDataList.ToJobArray(),

                frictionList = Manager.Particle.frictionList.ToJobArray(),

                collisionLinkIdList = Manager.Particle.collisionLinkIdList.ToJobArray(),
                collisionNormalList = Manager.Particle.collisionNormalList.ToJobArray(),
            };
            jobHandle = job1.Schedule(Manager.Particle.Length, 64, jobHandle);

            return jobHandle;
        }

        //=========================================================================================
        /// <summary>
        /// コリジョン拘束ジョブ
        /// 移動パーティクルごとに計算
        /// </summary>
        [BurstCompile]
        struct CollisionJob : IJobParallelFor
        {
            public int runCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> teamIdList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> radiusList;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> nextRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> posList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> rotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> baseRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> transformIndexList;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> colliderList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> boneSclList;

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;

            public NativeArray<float> frictionList;

            [Unity.Collections.WriteOnly]
            public NativeArray<int> collisionLinkIdList;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> collisionNormalList;

            // パーティクルごと
            public void Execute(int index)
            {
                var flag = flagList[index];
                if (flag.IsValid() == false || flag.IsFixed() || flag.IsCollider())
                    return;

                // チーム
                var team = teamIdList[index];
                var teamData = teamDataList[team];
                if (teamData.IsActive() == false)
                    return;
                if (teamData.IsFlag(PhysicsManagerTeamData.Flag_Collision) == false)
                    return;
                // 更新確認
                if (teamData.IsUpdate(runCount) == false)
                    return;

                float3 nextpos = nextPosList[index];
                var radius = radiusList[index].x;
                //var basepos = basePosList[index];

                // チームスケール倍率
                radius *= teamData.scaleRatio;

                // チームごとに判定[グローバル(0)]->[自身のチーム(team)]
                int colliderTeam = 0;

                // コライダーとの距離
                float mindist = 100.0f;

                // 接触コライダー情報
                int collisionColliderId = 0;
                float3 collisionNormal = 0;
                float3 n = 0;

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

                        float dist = 100.0f;
                        if (cflag.IsFlag(PhysicsManagerParticleData.Flag_Plane))
                        {
                            // 平面コライダー判定
                            dist = PlaneColliderDetection(ref nextpos, radius, cindex, out n);
                        }
                        else if (cflag.IsFlag(PhysicsManagerParticleData.Flag_CapsuleX))
                        {
                            // カプセルコライダー判定
                            dist = CapsuleColliderDetection(ref nextpos, radius, cindex, new float3(1, 0, 0), out n);
                        }
                        else if (cflag.IsFlag(PhysicsManagerParticleData.Flag_CapsuleY))
                        {
                            // カプセルコライダー判定
                            dist = CapsuleColliderDetection(ref nextpos, radius, cindex, new float3(0, 1, 0), out n);
                        }
                        else if (cflag.IsFlag(PhysicsManagerParticleData.Flag_CapsuleZ))
                        {
                            // カプセルコライダー判定
                            dist = CapsuleColliderDetection(ref nextpos, radius, cindex, new float3(0, 0, 1), out n);
                        }
                        else if (cflag.IsFlag(PhysicsManagerParticleData.Flag_Box))
                        {
                            // ボックスコライダー判定
                            // ★まだ未実装
                        }
                        else
                        {
                            // 球コライダー判定
                            dist = SphereColliderDetection(ref nextpos, radius, cindex, out n);
                        }

                        // 押し出し（接触）あり
                        if (dist < mindist && dist <= Define.Compute.CollisionFrictionRange)
                        {
                            collisionColliderId = cindex;
                            collisionNormal = n;
                            mindist = dist;
                        }
                    }

                    // 自身のチームに切り替え
                    if (team > 0)
                        colliderTeam = team;
                    else
                        break;
                }

                // 摩擦係数(friction)計算
                if (collisionColliderId > 0)
                {
                    // コライダーから一定距離内にいる
                    // 摩擦係数計算（コライダーからの距離により変化.0.0～接地面1.0～深くめり込む場合は1.0を超える)
                    //var friction = math.max(1.0f - mindist / Define.Compute.CollisionFrictionRange, 0.0f); // ★この実装では振動が酷くなるので却下！

                    // コライダーからの距離により変化(0.0～接地面1.0)
                    var friction = 1.0f - math.saturate(mindist / Define.Compute.CollisionFrictionRange);
                    frictionList[index] = math.max(friction, frictionList[index]); // 大きい方
                }
                collisionLinkIdList[index] = collisionColliderId;
                collisionNormalList[index] = collisionNormal;

                // 書き戻し
                nextPosList[index] = nextpos;

                // コリジョンの速度影響は100%にしておく
                // コリジョン衝突による速度影響は非常に重要！
                // 速度影響を抑えると容易に突き抜けるようになってしまう
            }

            //=====================================================================================
            /// <summary>
            /// 球衝突判定
            /// </summary>
            /// <param name="nextpos"></param>
            /// <param name="pos"></param>
            /// <param name="radius"></param>
            /// <param name="cindex"></param>
            /// <param name="friction"></param>
            /// <returns></returns>
            float SphereColliderDetection(ref float3 nextpos, float radius, int cindex, out float3 normal)
            {
                var cpos = nextPosList[cindex];
                var cradius = radiusList[cindex];

                // スケール
                var tindex = transformIndexList[cindex];
                var cscl = boneSclList[tindex];
                cradius *= math.abs(cscl.x); // X軸のみを見る

                // 移動前のコライダーに対するローカル位置から移動後コライダーの押し出し平面を求める
                float3 c = 0, n = 0, v = 0;
                var coldpos = posList[cindex];
                v = nextpos - coldpos;
                n = math.normalize(v);
                c = cpos + n * (cradius.x + radius);

                // 衝突法線
                normal = n;

                // c = 平面位置
                // n = 平面方向
                // 平面衝突判定と押し出し
                return MathUtility.IntersectPointPlaneDist(c, n, nextpos, out nextpos);
            }

            /// <summary>
            /// カプセル衝突判定
            /// </summary>
            /// <param name="nextpos"></param>
            /// <param name="pos"></param>
            /// <param name="radius"></param>
            /// <param name="cindex"></param>
            /// <param name="dir"></param>
            /// <param name="friction"></param>
            /// <returns></returns>
            float CapsuleColliderDetection(ref float3 nextpos, float radius, int cindex, float3 dir, out float3 normal)
            {
                var cpos = nextPosList[cindex];
                var crot = nextRotList[cindex];

                // x = 長さ（片側）
                // y = 始点半径
                // z = 終点半径
                var cradius = radiusList[cindex];

                // スケール
                var tindex = transformIndexList[cindex];
                var cscl = boneSclList[tindex];
                float scl = math.dot(math.abs(cscl), dir); // dirの軸のスケールを使用する
                cradius *= scl;

                float3 c = 0, n = 0;

                var coldpos = posList[cindex];
                var coldrot = rotList[cindex];

                // カプセル始点と終点
                float3 l = math.mul(coldrot, dir * cradius.x);
                float3 spos = coldpos - l;
                float3 epos = coldpos + l;
                float sr = cradius.y;
                float er = cradius.z;

                // 移動前のコライダー位置から押し出し平面を割り出す
                float t = MathUtility.ClosestPtPointSegmentRatio(nextpos, spos, epos);
                float r = math.lerp(sr, er, t);
                float3 d = math.lerp(spos, epos, t);
                float3 v = nextpos - d;

                // 移動前コライダーのローカルベクトル
                var iq = math.inverse(coldrot);
                float3 lv = math.mul(iq, v);

                // 移動後コライダーに変換
                l = math.mul(crot, dir * cradius.x);
                spos = cpos - l;
                epos = cpos + l;
                d = math.lerp(spos, epos, t);
                v = math.mul(crot, lv);
                n = math.normalize(v);
                c = d + n * (r + radius);

                // 衝突法線
                normal = n;

                // c = 平面位置
                // n = 平面方向
                // 平面衝突判定と押し出し
                return MathUtility.IntersectPointPlaneDist(c, n, nextpos, out nextpos);
            }

            /// <summary>
            /// 平面衝突判定
            /// </summary>
            /// <param name="nextpos"></param>
            /// <param name="radius"></param>
            /// <param name="cindex"></param>
            float PlaneColliderDetection(ref float3 nextpos, float radius, int cindex, out float3 normal)
            {
                // 平面姿勢
                var cpos = nextPosList[cindex];
                var crot = nextRotList[cindex];

                // 平面法線
                float3 n = math.mul(crot, math.up());

                // パーティクル半径分オフセット
                cpos += n * radius;

                // 衝突法線
                normal = n;

                // c = 平面位置
                // n = 平面方向
                // 平面衝突判定と押し出し
                // 平面との距離を返す（押し出しの場合は0.0）
                return MathUtility.IntersectPointPlaneDist(cpos, n, nextpos, out nextpos);
            }
        }
    }
}
