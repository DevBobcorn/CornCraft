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
    /// コライダーによるパーティクル押し出し拘束
    /// </summary>
    public class ColliderExtrusionConstraint : PhysicsManagerConstraint
    {
        public override void Create()
        {
        }

        public override void RemoveTeam(int teamId)
        {
        }

        // public void ChangeParam(int teamId, bool useCollision)
        // {
        //     Manager.Team.SetFlag(teamId, PhysicsManagerTeamData.Flag_Collision, useCollision);
        // }

        public override void Release()
        {
        }

        //=========================================================================================
        public override JobHandle SolverConstraint(int runCount, float dtime, float updatePower, int iteration, JobHandle jobHandle)
        {
            if (Manager.Particle.ColliderCount <= 0)
                return jobHandle;

#if true
            // コリジョン押し出し拘束
            var job1 = new CollisionExtrusionJob()
            {
                runCount = runCount,

                flagList = Manager.Particle.flagList.ToJobArray(),
                teamIdList = Manager.Particle.teamIdList.ToJobArray(),
                nextPosList = Manager.Particle.InNextPosList.ToJobArray(),
                nextRotList = Manager.Particle.InNextRotList.ToJobArray(),

                collisionLinkIdList = Manager.Particle.collisionLinkIdList.ToJobArray(),
                posList = Manager.Particle.posList.ToJobArray(),
                rotList = Manager.Particle.rotList.ToJobArray(),
                frictionList = Manager.Particle.frictionList.ToJobArray(),
                collisionNormalList = Manager.Particle.collisionNormalList.ToJobArray(),

                teamDataList = Manager.Team.teamDataList.ToJobArray(),
            };
            jobHandle = job1.Schedule(Manager.Particle.Length, 64, jobHandle);
#endif

            return jobHandle;
        }

        //=========================================================================================
        /// <summary>
        /// コリジョン押し出し拘束ジョブ
        /// 移動パーティクルごとに計算
        /// </summary>
        [BurstCompile]
        struct CollisionExtrusionJob : IJobParallelFor
        {
            public int runCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerParticleData.ParticleFlag> flagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> teamIdList;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> nextRotList;

            public NativeArray<int> collisionLinkIdList;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> posList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> rotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> collisionNormalList;

            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerTeamData.TeamData> teamDataList;

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

                // 接続コライダー
                int cindex = collisionLinkIdList[index];
                collisionLinkIdList[index] = 0; // リセット
                if (cindex <= 0)
                    return;
                var cflag = flagList[cindex];
                if (cflag.IsValid() == false || cflag.IsCollider() == false)
                    return;

                var friction = frictionList[index];
                if (friction < Define.Compute.Epsilon)
                    return;

                float3 nextpos = nextPosList[index];

                //int vindex = index - teamData.particleChunk.startIndex;
                //Debug.Log($"vindex:{vindex}");

                // 移動前コライダー姿勢
                var oldcpos = posList[cindex];
                var oldcrot = rotList[cindex];
                var v = nextpos - oldcpos; // nextposでないとダメ(oldPosList[index]ではまずい)
                var ioldcrot = math.inverse(oldcrot);
                var lpos = math.mul(ioldcrot, v);

                // 移動後コライダー姿勢
                var cpos = nextPosList[cindex];
                var crot = nextRotList[cindex];
                var fpos = math.mul(crot, lpos) + cpos;

                // 押し出しベクトル
                var ev = fpos - nextpos;
                var elen = math.length(ev);
                if (elen < 1e-06f)
                {
                    // コライダーが動いていない
                    return;
                }

                // アルゴリズム変更(v1.9.2)
                // 衝突面法線と押し出しベクトルの角度に応じて強さを変更
                var cn = collisionNormalList[index];
                var dot = math.dot(cn, ev / elen);
                dot = math.pow(dot, 0.5f); // 角度（内積）に対して押し出し力を強めにする
                float d = math.max(dot, 0.0f);
                // 摩擦係数に応じて強さを変更
                d *= math.saturate(friction);

                // 押し出し
                var opos = nextpos;
                nextpos = math.lerp(nextpos, fpos, d);
                nextPosList[index] = nextpos;

                // 速度影響
                var av = (nextpos - opos) * (1.0f - Define.Compute.ColliderExtrusionVelocityInfluence); // 跳ねを抑えるため少し抑える（※抑えすぎると突き抜けやすくなるので注意）
                posList[index] = posList[index] + av;
            }
        }
    }
}
