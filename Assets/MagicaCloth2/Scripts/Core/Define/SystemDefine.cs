// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using Unity.Mathematics;

namespace MagicaCloth2
{
    public static partial class Define
    {
        /// <summary>
        /// システム関連デファイン
        /// </summary>
        public static class System
        {
            /// <summary>
            /// プロジェクトセッティングに登録するDefineシンボル
            /// </summary>
            public const string DefineSymbol = "MAGICACLOTH2";

            /// <summary>
            /// 計算を省略する最小の浮動小数点数
            /// </summary>
            public const float Epsilon = 1e-8f;

            /// <summary>
            /// 最小のグリッドサイズ定義(GridSize=0は動作しないため)
            /// </summary>
            public const float MinimumGridSize = 0.00001f;

            /// <summary>
            /// 生成できるチーム数
            /// </summary>
            public const int MaximumTeamCount = 4096;

            /// <summary>
            /// シミュレーション周波数初期値
            /// </summary>
            public const int DefaultSimulationFrequency = 90;

            /// <summary>
            /// シミュレーション周波数最小値
            /// </summary>
            public const int SimulationFrequency_Low = 30;

            /// <summary>
            /// シミュレーション周波数最大値
            /// </summary>
            public const int SimulationFrequency_Hi = 150;

            /// <summary>
            /// 1フレームでの最大更新回数初期値
            /// </summary>
            public const int DefaultMaxSimulationCountPerFrame = 3;

            /// <summary>
            /// 1フレームでの最大更新回数最小
            /// </summary>
            public const int MaxSimulationCountPerFrame_Low = 1;

            /// <summary>
            /// 1フレームでの最大更新回数最大
            /// </summary>
            public const int MaxSimulationCountPerFrame_Hi = 5;

            /// <summary>
            /// 法線整列時に同一の面（レイヤー）として判定する隣接トライアングルのなす角（デグリー）
            /// </summary>
            public const float SameSurfaceAngle = 80.0f;

            /// <summary>
            /// [Reduction]
            /// 有効フラグ。常にtrueとする。
            /// </summary>
            public const bool ReductionEnable = true;

            /// <summary>
            /// [Reduction]
            /// 同一頂点として判定する距離(AABB最大距離の%)(0.0 ~ 1.0)
            /// </summary>
            public const float ReductionSameDistance = 0.001f;

            /// <summary>
            /// [Reduction]
            /// 極力ラインを作らない接続を行う
            /// </summary>
            public const bool ReductionDontMakeLine = true;

            /// <summary>
            /// [Reduction]
            /// 結合頂点位置の調整
            /// (0.0=接続頂点数の多いほど動かない, 1.0=完全に平均化)
            /// </summary>
            public const float ReductionJoinPositionAdjustment = 1.0f;

            /// <summary>
            /// [Reduction]
            /// 最大ステップ回数
            /// </summary>
            public const int ReductionMaxStep = 100;

            /// <summary>
            /// [ProxyMesh]
            /// 有効な最大頂点数
            /// </summary>
            public const int MaxProxyMeshVertexCount = 32767;

            /// <summary>
            /// [ProxyMesh]
            /// 有効な最大エッジ数
            /// </summary>
            public const int MaxProxyMeshEdgeCount = 32767;

            /// <summary>
            /// [ProxyMesh]
            /// 有効な最大トライアングル数
            /// </summary>
            public const int MaxProxyMeshTriangleCount = 32767;

            /// <summary>
            /// [ProxyMesh]
            /// トライアングルのペアと判定する角度(Deg)
            /// </summary>
            public const float ProxyMeshTrianglePairAngle = 20.0f;

            /// <summary>
            /// [ProxyMesh]
            /// BoneClothのMesh接続時にトライアングルとして判断される内角
            /// </summary>
            public const float ProxyMeshBoneClothTriangleAngle = 120.0f;

            /// <summary>
            /// [Simulation]
            /// 摩擦(0.0 ~ 1.0)に対する増加重量
            /// </summary>
            public const float FrictionMass = 3.0f;

            /// <summary>
            /// [Simulation]
            /// 深さ(0.0 ~ 1.0)に対する増加重量(深さ0.0のときに最大になる）
            /// </summary>
            public const float DepthMass = 5.0f;

            /// <summary>
            /// [Simulation]
            /// ステップごとの摩擦減衰率
            /// </summary>
            public const float FrictionDampingRate = 0.6f;

            /// <summary>
            /// [Simulation]
            /// アトミック加算による移動ベクトルの平均化に使用する加算数に対する指数
            /// </summary>
            public const float PositionAverageExponent = 0.5f; // 0.5?

            /// <summary>
            /// [Simulation]
            /// 未来予測に用いる実速度の最大速度クランプ(m/s)
            /// これはセルフコジョンなどで意図しない表示突き抜けを防止するために必要！
            /// </summary>
            public const float MaxRealVelocity = 0.5f;

            /// <summary>
            /// [Simulation]
            /// パーティクルの最大速度(m/s)
            /// </summary>
            //public const float ParticleSpeedLimit = 3.0f;

            /// <summary>
            /// [Tether]
            /// 縮み剛性(0.0 ~ 1.0)
            /// </summary>
            public const float TetherCompressionStiffness = 1.0f; // 0.1?

            /// <summary>
            /// [Tether]
            /// 伸び剛性(0.0 ~ 1.0)
            /// </summary>
            public const float TetherStretchStiffness = 1.0f; // 1.0f?

            /// <summary>
            /// [Tether]
            /// 最大拡大割合(0.0 ~ 1.0)
            /// 0.0=拡大しない
            /// </summary>
            public const float TetherStretchLimit = 0.03f; // 0.0?

            /// <summary>
            /// [Tether]
            /// stiffnessのフェード範囲(0.0 ~ 1.0)
            /// </summary>
            public const float TetherStiffnessWidth = 0.3f; // 0.2?

            /// <summary>
            /// [Tether]
            /// 拡大時の速度減衰(0.0 ~ 1.0)
            /// </summary>
            public const float TetherCompressionVelocityAttenuation = 0.7f;

            /// <summary>
            /// [Tether]
            /// 縮小時の速度減衰(0.0 ~ 1.0)
            /// </summary>
            public const float TetherStretchVelocityAttenuation = 0.7f; // 0.9f?

            /// <summary>
            /// [Distance]
            /// 速度減衰(0.0 ~ 1.0)
            /// </summary>
            public const float DistanceVelocityAttenuation = 0.3f;

            /// <summary>
            /// [Distance]
            /// 縦接続の剛性(0.0 ~ 1.0)
            /// </summary>
            public const float DistanceVerticalStiffness = 1.0f;

            /// <summary>
            /// [Distance]
            /// 横およびShear接続の剛性(0.0 ~ 1.0)
            /// </summary>
            public const float DistanceHorizontalStiffness = 0.5f;

            /// <summary>
            /// [Triangle Bending]
            /// TriangleBendを形成する最大の角度
            /// </summary>
            public const float TriangleBendingMaxAngle = 120.0f; // 145?

            /// <summary>
            /// [Volume]
            /// Volumeを形成する最小のTriangleペア角度
            /// </summary>
            public const float VolumeMinAngle = 90.0f;

            /// <summary>
            /// [Angle Limit]
            /// 角度制限の最大角度(deg)
            /// </summary>
            public const float MaxAngleLimit = 179.0f;

            /// <summary>
            /// [Angle Limit]
            /// 反復回数
            /// </summary>
            public const int AngleLimitIteration = 3;

            /// <summary>
            /// [Angle Limit]
            /// 速度減衰(0.0 ~ 1.0)
            /// </summary>
            public const float AngleLimitAttenuation = 0.9f;

            /// <summary>
            /// [Inertia]
            /// 設定できる最大移動速度(m/s)
            /// </summary>
            public const float MaxMovementSpeedLimit = 10.0f;

            /// <summary>
            /// [Inertia]
            /// 設定できる最大回転速度(deg/s)
            /// </summary>
            public const float MaxRotationSpeedLimit = 1440.0f;

            /// <summary>
            /// [Inertia]
            /// 設定できる最大のパーティクル移動速度(m/s)
            /// </summary>
            public const float MaxParticleSpeedLimit = 10.0f;

            /// <summary>
            /// [Collider Collision]
            /// 一度に拡張するコライダー数
            /// </summary>
            public const int ExpandedColliderCount = 8;

            /// <summary>
            /// [Collider Collision]
            /// 設定frictionに対するDynamicFrictionの割合(0.0 ~ 1.0)
            /// </summary>
            public const float ColliderCollisionDynamicFrictionRatio = 1.0f;

            /// <summary>
            /// [Collider Collision]
            /// 設定frictionに対するStaticFrictionの割合(0.0 ~ 1.0)
            /// </summary>
            public const float ColliderCollisionStaticFrictionRatio = 1.0f;

            /// <summary>
            /// [Collider Collision]
            /// 速度減衰(0.0 ~ 1.0)
            /// </summary>
            //public const float ColliderCollisionVelocityAttenuation = 0.0f;

            /// <summary>
            /// [Custom Skinning]
            /// ボーンラインからの角度による減衰率(0.0~1.0)
            /// </summary>
            public const float CustomSkinningAngularAttenuation = 1.0f;

            /// <summary>
            /// [Custom Skinning]
            /// 最近ポイントのウエイト強度(0.0~1.0)
            /// </summary>
            public const float CustomSkinningDistanceReduction = 0.6f;

            /// <summary>
            /// [Custom Skinning]
            /// 距離によりウエイトの減衰乗数(だいたい0.1 ~ 5.0)
            /// </summary>
            public const float CustomSkinningDistancePow = 2.0f;

            /// <summary>
            /// [Self Collision]
            /// 反復回数
            /// </summary>
            public const int SelfCollisionSolverIteration = 4;

            /// <summary>
            /// [Self Collision]
            /// セルフコリジョン解決時の固定パーティクル重量
            /// </summary>
            public const float SelfCollisionFixedMass = 100.0f;

            /// <summary>
            /// [Self Collision]
            /// セルフコリジョン解決時の摩擦1.0に対する重量
            /// </summary>
            public const float SelfCollisionFrictionMass = 10.0f;

            /// <summary>
            /// [Self Collision]
            /// セルフコリジョンのチーム重量係数
            /// </summary>
            public const float SelfCollisionClothMass = 50.0f;

            /// <summary>
            /// [Self Collision]
            /// 厚み(thickness)に対するSCRスケール
            /// </summary>
            public const float SelfCollisionSCR = 2.0f; // 1.0

            /// <summary>
            /// [Self Collision]
            /// PointTriangleの有効判定角度(deg)
            /// </summary>
            public static readonly float SelfCollisionPointTriangleAngleCos = math.cos(math.radians(60.0f));

            /// <summary>
            /// [Self Collision]
            /// 交差判定の分割数
            /// </summary>
            public const int SelfCollisionIntersectDiv = 8;

            /// <summary>
            /// [Self Collision]
            /// Thicknessの最小値(m)
            /// </summary>
            public const float SelfCollisionThicknessMin = 0.001f;

            /// <summary>
            /// [Self Collision]
            /// Thicknessの最大値(m)
            /// </summary>
            public const float SelfCollisionThicknessMax = 0.05f;

            /// <summary>
            /// [Wind]
            /// 風時間の最大値
            /// </summary>
            public const float WindMaxTime = 10000.0f;

            /// <summary>
            /// [Wind]
            /// 風速係数の基準となる風速(m/s)
            /// </summary>
            public const float WindBaseSpeed = 7.5f;
        }
    }
}
