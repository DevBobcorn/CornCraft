// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// BaseCloth API
    /// </summary>
    public abstract partial class BaseCloth : PhysicsTeam
    {
        /// <summary>
        /// クロスの物理シミュレーションをリセットします
        /// Reset cloth physics simulation.
        /// </summary>
        public void ResetCloth()
        {
            ResetClothInternal(clothParams.TeleportResetMode, -1.0f);
        }

        /// <summary>
        /// クロスの物理シミュレーションをリセットします
        /// Reset cloth physics simulation.
        /// </summary>
        /// <param name="resetStabilizationTime">Time to stabilize simulation (s)</param>
        public void ResetCloth(float resetStabilizationTime)
        {
            ResetClothInternal(clothParams.TeleportResetMode, Mathf.Max(resetStabilizationTime, 0.0f));
        }

        /// <summary>
        /// クロスの物理シミュレーションをリセットします
        /// Reset cloth physics simulation.
        /// </summary>
        /// <param name="teleportMode">RESET...Resets all simulations. KEEP...Keep the simulation.</param>
        /// <param name="resetStabilizationTime">Time to stabilize simulation(s).If negative, the time set in the inspector.</param>
        public void ResetCloth(ClothParams.TeleportMode teleportMode, float resetStabilizationTime = -1.0f)
        {
            ResetClothInternal(teleportMode, resetStabilizationTime);
        }

        /// <summary>
        /// タイムスケールを変更します
        /// Change the time scale.
        /// </summary>
        /// <param name="timeScale">0.0-1.0</param>
        public void SetTimeScale(float timeScale)
        {
            if (IsValid())
                MagicaPhysicsManager.Instance.Team.SetTimeScale(teamId, Mathf.Clamp01(timeScale));
        }

        /// <summary>
        /// タイムスケールを取得します
        /// Get the time scale.
        /// </summary>
        /// <returns></returns>
        public float GetTimeScale()
        {
            if (IsValid())
                return MagicaPhysicsManager.Instance.Team.GetTimeScale(teamId);
            else
                return 1.0f;
        }

        /// <summary>
        /// 外力を与えます
        /// Add external force.
        /// </summary>
        /// <param name="force"></param>
        public void AddForce(Vector3 force, PhysicsManagerTeamData.ForceMode mode)
        {
            if (IsValid() && IsActive())
                MagicaPhysicsManager.Instance.Team.SetImpactForce(teamId, force, mode);
        }

        /// <summary>
        /// 元の姿勢とシミュレーション結果とのブレンド率
        /// Blend ratio between original posture and simulation result.
        /// (0.0 = 0%, 1.0 = 100%)
        /// </summary>
        public float BlendWeight
        {
            get
            {
                return UserBlendWeight;
            }
            set
            {
                UserBlendWeight = value;
            }
        }

        /// <summary>
        /// コライダーをチームに追加します
        /// Add collider to the team.
        /// </summary>
        /// <param name="collider"></param>
        public void AddCollider(ColliderComponent collider)
        {
            Init(); // チームが初期化されていない可能性があるので.
            if (IsValid() && collider)
            {
                var c = collider.CreateColliderParticle(teamId);
                if (c.IsValid())
                    TeamData.AddCollider(collider);
            }
        }

        /// <summary>
        /// コライダーをチームから削除します
        /// Remove collider from the team.
        /// </summary>
        /// <param name="collider"></param>
        public void RemoveCollider(ColliderComponent collider)
        {
            if (IsValid() && collider)
            {
                collider.RemoveColliderParticle(teamId);
                TeamData.RemoveCollider(collider);
            }
        }

        /// <summary>
        /// 更新モードを変更します
        /// Change the update mode.
        /// </summary>
        /// <param name="updateMode"></param>
        public void SetUpdateMode(TeamUpdateMode updateMode)
        {
            UpdateMode = updateMode;
        }

        /// <summary>
        /// カリングモードを変更します
        /// Change the culling mode.
        /// </summary>
        /// <param name="cullingMode"></param>
        public void SetCullingMode(TeamCullingMode cullingMode)
        {
            CullingMode = cullingMode;
        }

        //=========================================================================================
        // [Radius] Parameters access.
        //=========================================================================================
        /// <summary>
        /// パーティクル半径の設定
        /// Setting up a particle radius.
        /// </summary>
        /// <param name="startVal">0.001 ~ </param>
        /// <param name="endVal">0.001 ~ </param>
        /// <param name="curveVal">-1.0 ~ +1.0</param>
        public void Radius_SetRadius(float startVal, float endVal, float curveVal = 0)
        {
            var b = clothParams.GetRadius().AutoSetup(Mathf.Max(startVal, 0.001f), Mathf.Max(endVal, 0.001f), curveVal);

            // update team particles.
            var manager = MagicaPhysicsManager.Instance;
            for (int i = 0; i < ParticleChunk.dataLength; i++)
            {
                int pindex = ParticleChunk.startIndex + i;
                float depth = manager.Particle.depthList[pindex];
                float radius = b.Evaluate(depth);
                manager.Particle.SetRadius(pindex, radius);
            }
        }

        //=========================================================================================
        // [Mass] Parameters access.
        //=========================================================================================
        /// <summary>
        /// 重量の設定
        /// Setting up a mass.
        /// </summary>
        /// <param name="startVal">1.0 ~ </param>
        /// <param name="endVal">1.0 ~ </param>
        /// <param name="curveVal">-1.0 ~ +1.0</param>
        public void Mass_SetMass(float startVal, float endVal, float curveVal = 0)
        {
            var b = clothParams.GetMass().AutoSetup(Mathf.Max(startVal, 1.0f), Mathf.Max(endVal, 1.0f), curveVal);

            if (IsValid())
            {
                MagicaPhysicsManager.Instance.Team.SetMass(TeamId, b);

                // Parameters related to mass
                MagicaPhysicsManager.Instance.Compute.RestoreDistance.ChangeParam(
                    TeamId,
                    clothParams.GetMass(),
                    clothParams.RestoreDistanceVelocityInfluence,
                    clothParams.GetStructDistanceStiffness(),
                    clothParams.UseBendDistance,
                    clothParams.GetBendDistanceStiffness(),
                    clothParams.UseNearDistance,
                    clothParams.GetNearDistanceStiffness()
                    );
            }
        }

        //=========================================================================================
        // [Clamp Position] Parameters access.
        //=========================================================================================
        /// <summary>
        /// 移動範囲距離の設定
        /// Movement range distance setting.
        /// </summary>
        /// <param name="startVal">0.0 ~ 1.0</param>
        /// <param name="endVal">0.0 ~ 1.0</param>
        /// <param name="curveVal">-1.0 ~ +1.0</param>
        public void ClampPosition_SetPositionLength(float startVal, float endVal, float curveVal = 0)
        {
            var b = clothParams.GetClampPositionLength().AutoSetup(Mathf.Max(startVal, 1.0f), Mathf.Max(endVal, 1.0f), curveVal);

            if (IsValid())
            {
                MagicaPhysicsManager.Instance.Compute.ClampPosition.ChangeParam(
                    TeamId,
                    clothParams.UseClampPositionLength,
                    clothParams.GetClampPositionLength(),
                    clothParams.ClampPositionAxisRatio,
                    clothParams.ClampPositionVelocityInfluence
                    );
            }
        }

        //=========================================================================================
        // [Gravity] Parameters access.
        //=========================================================================================
        /// <summary>
        /// 重力加速度の設定
        /// Setting up a gravity.
        /// </summary>
        /// <param name="startVal"></param>
        /// <param name="endVal"></param>
        /// <param name="curveVal">-1.0 ~ +1.0</param>
        public void Gravity_SetGravity(float startVal, float endVal, float curveVal = 0)
        {
            var b = clothParams.GetGravity().AutoSetup(startVal, endVal, curveVal);
            if (IsValid())
            {
                MagicaPhysicsManager.Instance.Team.SetGravity(TeamId, b);
            }
        }

        /// <summary>
        /// 重力方向の設定.天井方向を指定します.
        /// Gravity direction setting. Specify the ceiling direction.
        /// </summary>
        public Vector3 Gravity_GravityDirection
        {
            get
            {
                return clothParams.GravityDirection;
            }
            set
            {
                clothParams.GravityDirection = value;
                if (IsValid())
                {
                    MagicaPhysicsManager.Instance.Team.SetGravityDirection(TeamId, value);
                }
            }
        }

        //=========================================================================================
        // [Drag] Parameters access.
        //=========================================================================================
        /// <summary>
        /// 空気抵抗の設定
        /// Setting up a drag.
        /// </summary>
        /// <param name="startVal">0.0 ~ 1.0</param>
        /// <param name="endVal">0.0 ~ 1.0</param>
        /// <param name="curveVal">-1.0 ~ +1.0</param>
        public void Drag_SetDrag(float startVal, float endVal, float curveVal = 0)
        {
            var b = clothParams.GetDrag().AutoSetup(startVal, endVal, curveVal);
            if (IsValid())
            {
                MagicaPhysicsManager.Instance.Team.SetDrag(TeamId, b);
            }
        }

        //=========================================================================================
        // [Distance Disable] Parameters access.
        //=========================================================================================
        /// <summary>
        /// アクティブ設定
        /// Active settings.
        /// </summary>
        public bool DistanceDisable_Active
        {
            get
            {
                return clothParams.UseDistanceDisable;
            }
            set
            {
                clothParams.UseDistanceDisable = value;
            }
        }

        /// <summary>
        /// 距離計測の対象設定
        /// nullを指定するとメインカメラが参照されます。
        /// Target setting for distance measurement.
        /// If null is specified, the main camera is referred.
        /// </summary>
        public Transform DistanceDisable_ReferenceObject
        {
            get
            {
                return clothParams.DisableReferenceObject;
            }
            set
            {
                clothParams.DisableReferenceObject = value;
            }
        }

        /// <summary>
        /// シミュレーションを無効化する距離
        /// Distance to disable simulation.
        /// </summary>
        public float DistanceDisable_Distance
        {
            get
            {
                return clothParams.DisableDistance;
            }
            set
            {
                clothParams.DisableDistance = Mathf.Max(value, 0.0f);
            }
        }

        /// <summary>
        /// シミュレーションを無効化するフェード距離
        /// DistanceDisable_DistanceからDistanceDisable_FadeDistanceの距離を引いた位置からフェードが開始します。
        /// Fade distance to disable simulation.
        /// Fade from DistanceDisable_Distance minus DistanceDisable_FadeDistance distance.
        /// </summary>
        public float DistanceDisable_FadeDistance
        {
            get
            {
                return clothParams.DisableFadeDistance;
            }
            set
            {
                clothParams.DisableFadeDistance = Mathf.Max(value, 0.0f);
            }
        }

        //=========================================================================================
        // [External Force] Parameter access.
        //=========================================================================================
        /// <summary>
        /// パーティクル重量の影響率(0.0-1.0).v1.12.0で廃止
        /// Particle weight effect rate (0.0-1.0).
        /// Abolished in v1.12.0.
        /// </summary>
        //public float ExternalForce_MassInfluence
        //{
        //    get
        //    {
        //        return clothParams.MassInfluence;
        //    }
        //    set
        //    {
        //        clothParams.MassInfluence = value;
        //        if (IsValid())
        //        {
        //            MagicaPhysicsManager.Instance.Team.SetExternalForce(TeamId, clothParams.MassInfluence, clothParams.WindInfluence, clothParams.WindRandomScale, clothParams.WindSynchronization);
        //        }
        //    }
        //}

        /// <summary>
        /// 深さによる外力の影響率(0.0-1.0)
        /// Impact of external force on depth.(0.0 - 1.0)
        /// </summary>
        /// <param name="startVal">0.0 ~ 1.0</param>
        /// <param name="endVal">0.0 ~ 1.0</param>
        /// <param name="curveVal">-1.0 ~ +1.0</param>
        public void ExternalForce_DepthInfluence(float startVal, float endVal, float curveVal = 0)
        {
            var b = clothParams.GetDepthInfluence().AutoSetup(startVal, endVal, curveVal);
            if (IsValid())
            {
                MagicaPhysicsManager.Instance.Team.SetDepthInfluence(TeamId, b);
            }
        }

        /// <summary>
        /// 風の影響率(1.0 = 100%)
        /// Wind influence rate (1.0 = 100%).
        /// </summary>
        public float ExternalForce_WindInfluence
        {
            get
            {
                return clothParams.WindInfluence;
            }
            set
            {
                clothParams.WindInfluence = value;
                if (IsValid())
                {
                    MagicaPhysicsManager.Instance.Team.SetExternalForce(TeamId, clothParams.MassInfluence, clothParams.WindInfluence, clothParams.WindRandomScale, clothParams.WindSynchronization);
                }
            }
        }

        /// <summary>
        /// 風のランダム率(1.0 = 100%)
        /// Wind random rate (1.0 = 100%).
        /// </summary>
        public float ExternalForce_WindRandomScale
        {
            get
            {
                return clothParams.WindRandomScale;
            }
            set
            {
                clothParams.WindRandomScale = value;
                if (IsValid())
                {
                    MagicaPhysicsManager.Instance.Team.SetExternalForce(TeamId, clothParams.MassInfluence, clothParams.WindInfluence, clothParams.WindRandomScale, clothParams.WindSynchronization);
                }
            }
        }

        //=========================================================================================
        // [World Influence] Parameter access.
        //=========================================================================================
        /// <summary>
        /// 移動影響の設定
        /// Setting up a moving influence.
        /// </summary>
        /// <param name="startVal">0.0 ~ 1.0</param>
        /// <param name="endVal">0.0 ~ 1.0</param>
        /// <param name="curveVal">-1.0 ~ +1.0</param>
        public void WorldInfluence_SetMovementInfluence(float startVal, float endVal, float curveVal = 0)
        {
            var b = clothParams.GetWorldMoveInfluence().AutoSetup(startVal, endVal, curveVal);
            if (IsValid())
            {
                MagicaPhysicsManager.Instance.Team.SetWorldInfluence(TeamId, clothParams.MaxMoveSpeed, clothParams.MaxRotationSpeed, b, clothParams.GetWorldRotationInfluence());
            }
        }

        /// <summary>
        /// 回転影響の設定
        /// Setting up a rotation influence.
        /// </summary>
        /// <param name="startVal">0.0 ~ 1.0</param>
        /// <param name="endVal">0.0 ~ 1.0</param>
        /// <param name="curveVal">-1.0 ~ +1.0</param>
        public void WorldInfluence_SetRotationInfluence(float startVal, float endVal, float curveVal = 0)
        {
            var b = clothParams.GetWorldRotationInfluence().AutoSetup(startVal, endVal, curveVal);
            if (IsValid())
            {
                MagicaPhysicsManager.Instance.Team.SetWorldInfluence(TeamId, clothParams.MaxMoveSpeed, clothParams.MaxRotationSpeed, clothParams.GetWorldMoveInfluence(), b);
            }
        }

        /// <summary>
        /// 最大速度の設定
        /// Setting up a max move speed.(m/s)
        /// </summary>
        public float WorldInfluence_MaxMoveSpeed
        {
            get
            {
                return clothParams.MaxMoveSpeed;
            }
            set
            {
                clothParams.MaxMoveSpeed = Mathf.Max(value, 0.0f);
                if (IsValid())
                {
                    MagicaPhysicsManager.Instance.Team.SetWorldInfluence(TeamId, clothParams.MaxMoveSpeed, clothParams.MaxRotationSpeed, clothParams.GetWorldMoveInfluence(), clothParams.GetWorldRotationInfluence());
                }
            }
        }

        /// <summary>
        /// 自動テレポートの有効設定
        /// Enable automatic teleportation.
        /// </summary>
        public bool WorldInfluence_ResetAfterTeleport
        {
            get
            {
                return clothParams.UseResetTeleport;
            }
            set
            {
                clothParams.UseResetTeleport = value;
                if (IsValid())
                {
                    MagicaPhysicsManager.Instance.Team.SetAfterTeleport(TeamId, clothParams.UseResetTeleport, clothParams.TeleportDistance, clothParams.TeleportRotation, clothParams.TeleportResetMode);
                }
            }
        }

        /// <summary>
        /// 自動テレポートと検出する１フレームの移動距離
        /// Travel distance in one frame to be judged as automatic teleport.
        /// </summary>
        public float WorldInfluence_TeleportDistance
        {
            get
            {
                return clothParams.TeleportDistance;
            }
            set
            {
                clothParams.TeleportDistance = value;
                if (IsValid())
                {
                    MagicaPhysicsManager.Instance.Team.SetAfterTeleport(TeamId, clothParams.UseResetTeleport, clothParams.TeleportDistance, clothParams.TeleportRotation, clothParams.TeleportResetMode);
                }
            }
        }

        /// <summary>
        /// 自動テレポートと検出する１フレームの回転角度(0.0 ~ 360.0)
        /// Rotation angle of one frame to be judged as automatic teleport.(0.0 ~ 360.0)
        /// </summary>
        public float WorldInfluence_TeleportRotation
        {
            get
            {
                return clothParams.TeleportRotation;
            }
            set
            {
                clothParams.TeleportRotation = value;
                if (IsValid())
                {
                    MagicaPhysicsManager.Instance.Team.SetAfterTeleport(TeamId, clothParams.UseResetTeleport, clothParams.TeleportDistance, clothParams.TeleportRotation, clothParams.TeleportResetMode);
                }
            }
        }

        /// <summary>
        /// テレポートのモードを設定
        /// Setting up a teleport mode.
        /// </summary>
        public ClothParams.TeleportMode WorldInfluence_TeleportMode
        {
            get
            {
                return clothParams.TeleportResetMode;
            }
            set
            {
                clothParams.TeleportResetMode = value;
                if (IsValid())
                {
                    MagicaPhysicsManager.Instance.Team.SetAfterTeleport(TeamId, clothParams.UseResetTeleport, clothParams.TeleportDistance, clothParams.TeleportRotation, clothParams.TeleportResetMode);
                }
            }
        }

        /// <summary>
        /// リセット後の安定時間を設定(s)
        /// Set stabilization time after reset.
        /// </summary>
        public float WorldInfluence_StabilizationTime
        {
            get
            {
                return clothParams.ResetStabilizationTime;
            }
            set
            {
                clothParams.ResetStabilizationTime = Mathf.Max(value, 0.0f);
                if (IsValid())
                {
                    MagicaPhysicsManager.Instance.Team.SetStabilizationTime(TeamId, clothParams.ResetStabilizationTime);
                }
            }
        }

        /// <summary>
        /// InfluenceTargetを置換する
        /// Replace InfluenceTarget.
        /// </summary>
        /// <param name="target"></param>
        public void WorldInfluence_ReplaceInfluenceTarget(Transform target)
        {
            bool active = status.IsActive;
            if (active)
            {
                Setup.ClothInactive(this);
            }

            // InfluenceTarget
            Params.SetInfluenceTarget(target);
            MagicaPhysicsManager.Instance.Team.ResetWorldInfluenceTarget(TeamId, target ? target : transform);

            if (active)
            {
                Setup.ClothActive(this, Params, ClothData);
            }
        }

        //=========================================================================================
        // [Collider Collision] Parameter access.
        //=========================================================================================
        /// <summary>
        /// アクティブ設定
        /// Active settings.
        /// </summary>
        public bool ColliderCollision_Active
        {
            get
            {
                return clothParams.UseCollision;
            }
            set
            {
                clothParams.SetCollision(value, clothParams.DynamicFriction, clothParams.StaticFriction);
                if (IsValid())
                {
                    MagicaPhysicsManager.Instance.Compute.Collision.ChangeParam(TeamId, clothParams.UseCollision);
                }
            }
        }

        //=========================================================================================
        // [Penetration] Parameter access.
        //=========================================================================================
        /// <summary>
        /// アクティブ設定
        /// Active settings.
        /// </summary>
        public bool Penetration_Active
        {
            get
            {
                return clothParams.UsePenetration;
            }
            set
            {
                clothParams.UsePenetration = value;
                if (IsValid())
                {
                    MagicaPhysicsManager.Instance.Compute.Penetration.ChangeParam(
                        TeamId,
                        clothParams.UsePenetration,
                        clothParams.GetPenetrationDistance(),
                        clothParams.GetPenetrationRadius(),
                        clothParams.PenetrationMaxDepth
                        );
                }
            }
        }

        /// <summary>
        /// 移動範囲球の設定
        /// Setting up a moving radius.
        /// </summary>
        /// <param name="startVal">0.0 ~ </param>
        /// <param name="endVal">0.0 ~ </param>
        /// <param name="curveVal">-1.0 ~ +1.0</param>
        public void Penetration_SetMovingRadius(float startVal, float endVal, float curveVal = 0)
        {
            clothParams.GetPenetrationRadius().AutoSetup(Mathf.Max(startVal, 0.0f), Mathf.Max(endVal, 0.0f), curveVal);
            if (IsValid())
            {
                MagicaPhysicsManager.Instance.Compute.Penetration.ChangeParam(
                    TeamId,
                    clothParams.UsePenetration,
                    clothParams.GetPenetrationDistance(),
                    clothParams.GetPenetrationRadius(),
                    clothParams.PenetrationMaxDepth
                    );
            }
        }

        //=========================================================================================
        // [Spring] Parameter access.
        //=========================================================================================
        /// <summary>
        /// アクティブ設定
        /// Active settings.
        /// </summary>
        public bool Spring_Active
        {
            get
            {
                return clothParams.UseSpring;
            }
            set
            {
                clothParams.UseSpring = value;
                if(IsValid())
                {
                    MagicaPhysicsManager.Instance.Compute.Spring.ChangeParam(TeamId, clothParams.UseSpring, clothParams.GetSpringPower());
                }
            }
        }

        /// <summary>
        /// スプリング力の設定
        /// Setting up a spring power.
        /// </summary>
        public float Spring_Power
        {
            get
            {
                return clothParams.SpringPowr;
            }
            set
            {
                clothParams.SpringPowr = value;
                if (IsValid())
                {
                    MagicaPhysicsManager.Instance.Compute.Spring.ChangeParam(TeamId, clothParams.UseSpring, clothParams.GetSpringPower());
                }
            }
        }
    }
}
