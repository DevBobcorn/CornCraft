// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

namespace MagicaCloth
{
    public static partial class Define
    {
        /// <summary>
        /// 計算用デファイン
        /// </summary>
        public static class Compute
        {
            /// <summary>
            /// 計算を省略する最小の浮動小数点数
            /// </summary>
            public const float Epsilon = 1e-6f;

            /// <summary>
            /// 摩擦係数を計算するコライダーとの距離
            /// </summary>
            public const float CollisionFrictionRange = 0.03f; // 0.05(v1.6.1) 0.01(v1.7.0) 0.03(v1.7.5)

            /// <summary>
            /// 摩擦の減衰率
            /// </summary>
            public const float FrictionDampingRate = 0.6f; // 0.6(v1.6.1) 0.6(v1.7.0)

            /// <summary>
            /// 摩擦係数による移動率
            /// 0.9f = 最大摩擦でも10%は移動する
            /// </summary>
            public const float FrictionMoveRatio = 0.5f; // 0.5(v1.6.1) 0.9(v1.7.0) 1.0(v1.8.0)

            /// <summary>
            /// 摩擦係数の距離による減衰力
            /// </summary>
            public const float FrictionPower = 4.0f; // 4.0(v1.7.0)

            /// <summary>
            /// ClampPosition拘束でのパーティクル最大速度(m/s)
            /// </summary>
            public const float ClampPositionMaxVelocity = 1.0f;

            /// <summary>
            /// グローバルコライダーの１ステップの最大移動距離
            /// </summary>
            public const float GlobalColliderMaxMoveDistance = 0.2f;

            /// <summary>
            /// グローバルコライダーの１ステップの最大回転角度(deg)
            /// </summary>
            public const float GlobalColliderMaxRotationAngle = 10.0f;

            /// <summary>
            /// コライダー押し出し時の最大移動距離
            /// 移動時の振動を抑えるために0.8程度に抑える
            /// </summary>
            public const float ColliderExtrusionMaxPower = 0.4f; // 1.0(v1.8.4)

            /// <summary>
            /// コライダー押し出し時に方向内積に累乗する補正値
            /// </summary>
            public const float ColliderExtrusionDirectionPower = 0.3f; // 0.5(v1.8.0)

            /// <summary>
            /// コライダー押し出し時にコライダーとの距離に累乗する補正値
            /// </summary>
            public const float ColliderExtrusionDistPower = 2.0f;

            /// <summary>
            /// コライダー押し出し時の速度影響
            /// </summary>
            public const float ColliderExtrusionVelocityInfluence = 0.25f; // 0.5(v1.8.0) 0.25(v1.8.1)

            /// <summary>
            /// 最大風力
            /// </summary>
            public const float MaxWindMain = 100;

            //=================================================================
            // Algorithm 1
            //=================================================================
            /// <summary>
            /// ClampRotation拘束でのパーティクル最大速度(m/s)
            /// </summary>
            public const float ClampRotationMaxVelocity = 1.0f;


            //=================================================================
            // Algorithm 2
            //=================================================================
            /// <summary>
            /// ClampRotation2でのパーティクル最大速度(m/s)
            /// </summary>
            public const float ClampRotationMaxVelocity2 = 2.0f;

            /// <summary>
            /// ClampRotation2での親からの回転中心割合(0.0-1.0)
            /// (0.4が安定)(0.3がぎりぎり)
            /// </summary>
            //public const float ClampRotationPivotRatio = 0.3f;

            /// <summary>
            /// TriangleBendの速度影響(0.0-1.0)
            /// </summary>
            public const float TriangleBendVelocityInfluence = 0.5f;
        }
    }
}
