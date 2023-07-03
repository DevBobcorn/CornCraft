// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using Unity.Mathematics;

namespace MagicaCloth2
{
    /// <summary>
    /// ジョブで利用するためのクロスパラメータ構造体
    /// ジョブではclassを参照できないためunmanaged型に変換する必要がある
    /// </summary>
    public struct ClothParameters
    {
        /// <summary>
        /// １秒間の更新回数
        /// </summary>
        //public int solverFrequency;

        /// <summary>
        /// 重力
        /// </summary>
        public float gravity;

        /// <summary>
        /// 重力方向（ワールド空間）
        /// </summary>
        public float3 gravityDirection;

        /// <summary>
        /// 初期姿勢での重力の減衰率(0.0 ~ 1.0)
        /// 1.0にすることで初期姿勢では重力係数が０になる。
        /// 0.0では常にどの姿勢でも重力が100%発生する。
        /// 姿勢計算は慣性トランスフォームで行われる。
        /// </summary>
        public float gravityFalloff;

        /// <summary>
        /// リセット後の速度安定化時間(s)
        /// </summary>
        public float stablizationTimeAfterReset;

        /// <summary>
        /// 元の姿勢とシミュレーション結果のブレンド割合(0.0 ~ 1.0)
        /// </summary>
        public float blendWeight;

        /// <summary>
        /// 抵抗
        /// </summary>
        public float4x4 dampingCurveData;

        /// <summary>
        /// パーティクル半径
        /// </summary>
        public float4x4 radiusCurveData;

        /// <summary>
        /// 法線として利用する軸
        /// </summary>
        public ClothNormalAxis normalAxis;

        // BoneCloth用
        public float rotationalInterpolation;
        public float rootRotation;

        // 慣性制約(Inertia)
        public InertiaConstraint.InertiaConstraintParams inertiaConstraint;

        // 最大距離制約(Tether)
        public TetherConstraint.TetherConstraintParams tetherConstraint;

        // 距離制約(Distance)
        public DistanceConstraint.DistanceConstraintParams distanceConstraint;

        // トライアングル曲げ制約(TriangleBending)
        public TriangleBendingConstraint.TriangleBendingConstraintParams triangleBendingConstraint;

        // 角度復元/角度制限
        public AngleConstraint.AngleConstraintParams angleConstraint;

        // モーション(MaxDistance/Backstop)
        public MotionConstraint.MotionConstraintParams motionConstraint;

        // コライダーコリジョン
        public ColliderCollisionConstraint.ColliderCollisionConstraintParams colliderCollisionConstraint;

        // セルフコリジョン
        public SelfCollisionConstraint.SelfCollisionConstraintParams selfCollisionConstraint;

        // 風
        public WindParams wind;
    }
}
