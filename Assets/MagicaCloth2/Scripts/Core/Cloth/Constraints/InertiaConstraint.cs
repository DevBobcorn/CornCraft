// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Text;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace MagicaCloth2
{
    /// <summary>
    /// Inertia and travel/rotation limits.
    /// 慣性と移動/回転制限
    /// </summary>
    public class InertiaConstraint : IDisposable
    {
        /// <summary>
        /// テレポートモード
        /// Teleport processing mode.
        /// </summary>
        public enum TeleportMode
        {
            None = 0,

            /// <summary>
            /// シミュレーションをリセットします
            /// Reset the simulation.
            /// </summary>
            Reset = 1,

            /// <summary>
            /// テレポート前の状態を継続します
            /// Continue the state before the teleport.
            /// </summary>
            Keep = 2,
        }

        [System.Serializable]
        public class SerializeData : IDataValidate
        {
            /// <summary>
            /// World Influence (0.0 ~ 1.0).
            /// ワールド移動影響(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [FormerlySerializedAs("movementInertia")]
            [Range(0.0f, 1.0f)]
            public float worldInertia;

            /// <summary>
            /// World movement speed limit (m/s).
            /// ワールド移動速度制限(m/s)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CheckSliderSerializeData movementSpeedLimit;

            /// <summary>
            /// World rotation speed limit (deg/s).
            /// ワールド回転速度制限(deg/s)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CheckSliderSerializeData rotationSpeedLimit;

            /// <summary>
            /// Local Influence (0.0 ~ 1.0).
            /// ローカル慣性影響(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float localInertia;

            /// <summary>
            /// depth inertia (0.0 ~ 1.0).
            /// Increasing the effect weakens the inertia near the root (makes it difficult to move).
            /// 深度慣性(0.0 ~ 1.0)
            /// 影響を大きくするとルート付近の慣性が弱くなる（動きにくくなる）
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float depthInertia;

            /// <summary>
            /// Centrifugal acceleration (0.0 ~ 1.0).
            /// 遠心力加速(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float centrifualAcceleration;

            /// <summary>
            /// Particle Velocity Limit (m/s).
            /// パーティクル速度制限(m/s)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CheckSliderSerializeData particleSpeedLimit;

            /// <summary>
            /// Teleport determination method.
            /// テレポート判定モード
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public TeleportMode teleportMode;

            /// <summary>
            /// Teleport detection distance.
            /// テレポート判定距離
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public float teleportDistance;

            /// <summary>
            /// Teleport detection angle(deg).
            /// テレポート判定回転角度(deg)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public float teleportRotation;

            public SerializeData()
            {
                worldInertia = 1.0f;
                movementSpeedLimit = new CheckSliderSerializeData(true, 5.0f);
                rotationSpeedLimit = new CheckSliderSerializeData(true, 720.0f);
                localInertia = 1.0f;
                depthInertia = 0.0f;
                centrifualAcceleration = 0.0f;
                particleSpeedLimit = new CheckSliderSerializeData(true, 4.0f);
                teleportMode = TeleportMode.None;
                teleportDistance = 0.5f;
                teleportRotation = 90.0f;
            }

            public SerializeData Clone()
            {
                return new SerializeData()
                {
                    worldInertia = worldInertia,
                    movementSpeedLimit = movementSpeedLimit.Clone(),
                    rotationSpeedLimit = rotationSpeedLimit.Clone(),
                    localInertia = localInertia,
                    depthInertia = depthInertia,
                    centrifualAcceleration = centrifualAcceleration,
                    particleSpeedLimit = particleSpeedLimit.Clone(),
                    teleportMode = teleportMode,
                    teleportDistance = teleportDistance,
                    teleportRotation = teleportRotation,
                };
            }

            public void DataValidate()
            {
                worldInertia = Mathf.Clamp01(worldInertia);
                movementSpeedLimit.DataValidate(0.0f, Define.System.MaxMovementSpeedLimit);
                rotationSpeedLimit.DataValidate(0.0f, Define.System.MaxRotationSpeedLimit);
                localInertia = Mathf.Clamp01(localInertia);
                centrifualAcceleration = Mathf.Clamp01(centrifualAcceleration);
                depthInertia = Mathf.Clamp01(depthInertia);
                particleSpeedLimit.DataValidate(0.0f, Define.System.MaxParticleSpeedLimit);
                teleportDistance = Mathf.Max(teleportDistance, 0.0f);
                teleportRotation = Mathf.Max(teleportRotation, 0.0f);
            }
        }

        public struct InertiaConstraintParams
        {
            /// <summary>
            /// ワールド慣性影響(0.0 ~ 1.0)
            /// </summary>
            public float worldInertia;

            /// <summary>
            /// ワールド移動速度制限(m/s)
            /// </summary>
            public float movementSpeedLimit;

            /// <summary>
            /// ワールド回転速度制限(deg/s)
            /// </summary>
            public float rotationSpeedLimit;

            /// <summary>
            /// ローカル慣性影響(0.0 ~ 1.0)
            /// </summary>
            public float localInertia;

            /// <summary>
            /// 深度慣性(0.0 ~ 1.0)
            /// 影響を大きくするとルート付近の慣性が弱くなる（動きにくくなる）
            /// </summary>
            public float depthInertia;

            /// <summary>
            /// 遠心力加速(0.0 ~ 1.0)
            /// </summary>
            public float centrifualAcceleration;

            /// <summary>
            /// パーティクル速度制限(m/s)
            /// </summary>
            public float particleSpeedLimit;

            /// <summary>
            /// テレポートモード
            /// </summary>
            public TeleportMode teleportMode;

            /// <summary>
            /// テレポート判定距離
            /// </summary>
            public float teleportDistance;

            /// <summary>
            /// テレポート判定角度(deg)
            /// </summary>
            public float teleportRotation;

            public void Convert(SerializeData sdata)
            {
                worldInertia = sdata.worldInertia;
                movementSpeedLimit = sdata.movementSpeedLimit.GetValue(-1);
                rotationSpeedLimit = sdata.rotationSpeedLimit.GetValue(-1);
                localInertia = sdata.localInertia;
                depthInertia = sdata.depthInertia;
                centrifualAcceleration = sdata.centrifualAcceleration;
                particleSpeedLimit = sdata.particleSpeedLimit.GetValue(-1);
                teleportMode = sdata.teleportMode;
                teleportDistance = sdata.teleportDistance;
                teleportRotation = sdata.teleportRotation;
            }
        }

        //=========================================================================================
        /// <summary>
        /// センタートランスフォームのデータ
        /// </summary>
        public struct CenterData
        {
            /// <summary>
            /// 参照すべきセンタートランスフォームインデックス
            /// 同期時は同期先チームのもにになる
            /// </summary>
            public int centerTransformIndex;

            /// <summary>
            /// 現フレームのコンポーネント姿勢
            /// </summary>
            public float3 componentWorldPosition;
            public quaternion componentWorldRotation;

            /// <summary>
            /// 前フレームのコンポーネント姿勢
            /// </summary>
            public float3 oldComponentWorldPosition;
            public quaternion oldComponentWorldRotation;

            /// <summary>
            /// 現フレームのコンポーネント移動量
            /// </summary>
            public float3 frameComponentShiftVector;
            public quaternion frameComponentShiftRotation;

            /// <summary>
            /// 現フレームのコンポーネント移動速度と方向
            /// </summary>
            public float frameMovingSpeed;
            public float3 frameMovingDirection;

            /// <summary>
            /// 現フレームの姿勢
            /// </summary>
            public float3 frameWorldPosition;
            public quaternion frameWorldRotation;
            public float3 frameWorldScale;
            public float3 frameLocalPosition;

            /// <summary>
            /// 前フレームの姿勢
            /// </summary>
            public float3 oldFrameWorldPosition;
            public quaternion oldFrameWorldRotation;
            public float3 oldFrameWorldScale;

            /// <summary>
            /// 現ステップでの姿勢
            /// </summary>
            public float3 nowWorldPosition;
            public quaternion nowWorldRotation;
            public float3 nowWorldScale; // ※現在未使用

            /// <summary>
            /// 前回ステップでの姿勢
            /// </summary>
            public float3 oldWorldPosition;
            public quaternion oldWorldRotation;

            /// <summary>
            /// ステップごとの移動力削減割合(0.0~1.0)
            /// </summary>
            public float stepMoveInertiaRatio;

            /// <summary>
            /// ステップごとの回転力削減割合(0.0~1.0)
            /// </summary>
            public float stepRotationInertiaRatio;

            /// <summary>
            /// ステップごとの移動ベクトル
            /// これは削減前の純粋なワールドベクトル
            /// </summary>
            public float3 stepVector;

            /// <summary>
            /// ステップごとの回転ベクトル
            /// これは削減前の純粋なワールド回転
            /// </summary>
            public quaternion stepRotation;

            /// <summary>
            /// ステップごとの慣性全体移動シフトベクトル
            /// </summary>
            public float3 inertiaVector;

            /// <summary>
            /// ステップごとの慣性全体シフト回転
            /// </summary>
            public quaternion inertiaRotation;

            /// <summary>
            /// ステップごとの慣性削減後の移動速度(m/s)
            /// </summary>
            public float stepMovingSpeed;

            /// <summary>
            /// ステップごとの慣性削減後の移動方向
            /// </summary>
            public float3 stepMovingDirection;

            /// <summary>
            /// 回転の角速度(rad/s)
            /// </summary>
            public float angularVelocity;

            /// <summary>
            /// 回転軸(角速度0の場合は(0,0,0))
            /// </summary>
            public float3 rotationAxis;

            /// <summary>
            /// 初期化時の慣性中心姿勢でのローカル重力方向
            /// 重力falloff計算で使用
            /// </summary>
            public float3 initLocalGravityDirection;

            internal void Initialize()
            {
                componentWorldRotation = quaternion.identity;
                oldComponentWorldRotation = quaternion.identity;
                frameComponentShiftRotation = quaternion.identity;

                frameWorldRotation = quaternion.identity;
                oldFrameWorldRotation = quaternion.identity;
                nowWorldRotation = quaternion.identity;
                oldWorldRotation = quaternion.identity;
                stepRotation = quaternion.identity;
            }
        }

        /// <summary>
        /// 制約データ
        /// </summary>
        public class ConstraintData
        {
            public ResultCode result;
            public CenterData centerData;
            public float3 initLocalGravityDirection;
        }

        /// <summary>
        /// チームごとの固定点リスト
        /// </summary>
        internal ExNativeArray<ushort> fixedArray;

        //=========================================================================================
        public InertiaConstraint()
        {
            fixedArray = new ExNativeArray<ushort>(0, true);
        }

        public void Dispose()
        {
            fixedArray?.Dispose();
            fixedArray = null;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[InertiaConstraint]");
            sb.AppendLine($"  -fixedArray:{fixedArray.ToSummary()}");

            return sb.ToString();
        }

        //=========================================================================================
        /// <summary>
        /// 制約データの作成
        /// </summary>
        /// <param name="cbase"></param>
        internal static ConstraintData CreateData(VirtualMesh proxyMesh, in ClothParameters parameters)
        {
            var constraintData = new ConstraintData();

            try
            {
                // ■センター
                var cdata = new CenterData();
                cdata.Initialize();
                cdata.centerTransformIndex = proxyMesh.centerTransformIndex;
                constraintData.centerData = cdata;

                // 固定点リストはすでにproxyMeshのcenterFixedListに格納されている
                float3 nor = 0;
                float3 tan = 0;
                int ccnt = proxyMesh.CenterFixedPointCount;
                if (ccnt > 0)
                {
                    for (int i = 0; i < ccnt; i++)
                    {
                        int fixedIndex = proxyMesh.centerFixedList[i];

                        // 初期姿勢を求める
                        var lnor = proxyMesh.localNormals[fixedIndex];
                        var ltan = proxyMesh.localTangents[fixedIndex];
                        var lrot = MathUtility.ToRotation(lnor, ltan);
                        var bindRot = proxyMesh.vertexBindPoseRotations[fixedIndex];
                        var q = math.mul(lrot, bindRot);
                        nor += MathUtility.ToNormal(q);
                        tan += MathUtility.ToTangent(q);
                    }
                }

                float3 localGravityDirection = new float3(0, -1, 0);
                if (ccnt > 0)
                {
                    // 初期センター姿勢からローカル重力方向を算出する
                    var rot = MathUtility.ToRotation(math.normalize(nor), math.normalize(tan));
                    var irot = math.inverse(rot);
                    localGravityDirection = math.mul(irot, parameters.gravityDirection);
                }
                constraintData.initLocalGravityDirection = localGravityDirection;

                constraintData.result.SetSuccess();

                //Develop.Log($"Create [InertiaConstraint].");
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                constraintData.result.SetError(Define.Result.Constraint_CreateInertiaException);
                throw;
            }
            finally
            {
            }

            return constraintData;
        }

        internal void Register(ClothProcess cprocess)
        {
            // センターデータのセンタートランスフォームインデックスをダイレクト値に変更
            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(cprocess.TeamId);
            ref var cdata = ref MagicaManager.Team.centerDataArray.GetRef(cprocess.TeamId);
            cdata.centerTransformIndex = tdata.centerTransformIndex;

            // 初期化時のローカル重力方向
            cdata.initLocalGravityDirection = cprocess.inertiaConstraintData.initLocalGravityDirection;

            // 固定点リスト
            var c = new DataChunk();
            if (cprocess.ProxyMesh.CenterFixedPointCount > 0)
            {
                c = fixedArray.AddRange(cprocess.ProxyMesh.centerFixedList);
            }
            tdata.fixedDataChunk = c;
        }

        internal void Exit(ClothProcess cprocess)
        {
            if (cprocess != null && cprocess.TeamId > 0)
            {
                ref var tdata = ref MagicaManager.Team.GetTeamDataRef(cprocess.TeamId);

                fixedArray.Remove(tdata.fixedDataChunk);
                tdata.fixedDataChunk.Clear();
            }
        }
    }
}
