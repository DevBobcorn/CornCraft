// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Text;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Inertia and travel/rotation limits.
    /// 慣性と移動/回転制限
    /// </summary>
    public class InertiaConstraint : IDisposable
    {
        [System.Serializable]
        public class SerializeData : IDataValidate
        {
            /// <summary>
            /// Movement Influence (0.0 ~ 1.0).
            /// 移動影響(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float movementInertia;

            /// <summary>
            /// Rotation influence (0.0 ~ 1.0).
            /// 回転影響(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float rotationInertia;

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
            /// Movement speed limit (m/s).
            /// 移動速度制限(m/s)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CheckSliderSerializeData movementSpeedLimit;

            /// <summary>
            /// Rotation speed limit (deg/s).
            /// 回転速度制限(deg/s)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CheckSliderSerializeData rotationSpeedLimit;

            /// <summary>
            /// Particle Velocity Limit (m/s).
            /// パーティクル速度制限(m/s)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CheckSliderSerializeData particleSpeedLimit;

            public SerializeData()
            {
                movementInertia = 1.0f;
                rotationInertia = 1.0f;
                depthInertia = 0.0f;
                centrifualAcceleration = 0.0f;
                movementSpeedLimit = new CheckSliderSerializeData(true, 5.0f);
                rotationSpeedLimit = new CheckSliderSerializeData(true, 720.0f);
                particleSpeedLimit = new CheckSliderSerializeData(true, 4.0f);
            }

            public SerializeData Clone()
            {
                return new SerializeData()
                {
                    movementInertia = movementInertia,
                    rotationInertia = rotationInertia,
                    depthInertia = depthInertia,
                    centrifualAcceleration = centrifualAcceleration,
                    movementSpeedLimit = movementSpeedLimit.Clone(),
                    rotationSpeedLimit = rotationSpeedLimit.Clone(),
                };
            }

            public void DataValidate()
            {
                movementInertia = Mathf.Clamp01(movementInertia);
                rotationInertia = Mathf.Clamp01(rotationInertia);
                centrifualAcceleration = Mathf.Clamp01(centrifualAcceleration);
                depthInertia = Mathf.Clamp01(depthInertia);
                movementSpeedLimit.DataValidate(0.0f, Define.System.MaxMovementSpeedLimit);
                rotationSpeedLimit.DataValidate(0.0f, Define.System.MaxRotationSpeedLimit);
                particleSpeedLimit.DataValidate(0.0f, Define.System.MaxParticleSpeedLimit);
            }
        }

        public struct InertiaConstraintParams
        {
            /// <summary>
            /// 移動影響(0.0 ~ 1.0)
            /// </summary>
            public float movementInertia;

            /// <summary>
            /// 回転影響(0.0 ~ 1.0)
            /// </summary>
            public float rotationInertia;

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
            /// 移動速度制限(m/s)
            /// </summary>
            public float movementSpeedLimit;

            /// <summary>
            /// 回転速度制限(deg/s)
            /// </summary>
            public float rotationSpeedLimit;

            /// <summary>
            /// パーティクル速度制限(m/s)
            /// </summary>
            public float particleSpeedLimit;

            public void Convert(SerializeData sdata)
            {
                movementInertia = sdata.movementInertia;
                rotationInertia = sdata.rotationInertia;
                depthInertia = sdata.depthInertia;
                centrifualAcceleration = sdata.centrifualAcceleration;
                movementSpeedLimit = sdata.movementSpeedLimit.GetValue(-1);
                rotationSpeedLimit = sdata.rotationSpeedLimit.GetValue(-1);
                particleSpeedLimit = sdata.particleSpeedLimit.GetValue(-1);
            }
        }

        //=========================================================================================
        /// <summary>
        /// センタートランスフォームのデータ
        /// </summary>
        public struct CenterData
        {
            public int centerTransformIndex;

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
            /// 実行ごとの移動ベクトル
            /// </summary>
            public float3 frameVector;

            /// <summary>
            /// 実行ごとの回転
            /// </summary>
            public quaternion frameRotation;

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

            /// <summary>
            /// 初期化時の慣性中心ローカル位置
            /// </summary>
            public float3 initLocalCenterPosition;
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
            var tdata = MagicaManager.Team.GetTeamData(cprocess.TeamId);
            var cdata = MagicaManager.Team.centerDataArray[cprocess.TeamId];
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

            MagicaManager.Team.centerDataArray[cprocess.TeamId] = cdata;
            MagicaManager.Team.SetTeamData(cprocess.TeamId, tdata);
        }

        internal void Exit(ClothProcess cprocess)
        {
            if (cprocess != null && cprocess.TeamId > 0)
            {
                var tdata = MagicaManager.Team.GetTeamData(cprocess.TeamId);

                fixedArray.Remove(tdata.fixedDataChunk);
                tdata.fixedDataChunk.Clear();

                MagicaManager.Team.SetTeamData(cprocess.TeamId, tdata);
            }
        }
    }
}
