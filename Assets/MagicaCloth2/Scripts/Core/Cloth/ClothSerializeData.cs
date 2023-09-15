// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Serialize data (1)
    /// Contains all parameters that can be changed during execution.
    /// The part that can be exported externally as Json.
    /// </summary>
    [System.Serializable]
    public partial class ClothSerializeData
    {
        /// <summary>
        /// simulation type.
        /// [NG] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public ClothProcess.ClothType clothType = ClothProcess.ClothType.MeshCloth;

        /// <summary>
        /// Renderer list used in MeshCloth.
        /// [NG] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public List<Renderer> sourceRenderers = new List<Renderer>();

        public enum PaintMode
        {
            Manual = 0,

            [InspectorName("Texture Fixed(RD) Move(GR) Ignore(BK)")]
            Texture_Fixed_Move = 1,

            [InspectorName("Texture Fixed(RD) Move(GR) Limit(BL) Ignore(BK)")]
            Texture_Fixed_Move_Limit = 2,
        }

        /// <summary>
        /// vertex paint mode.
        /// [NG] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public PaintMode paintMode = PaintMode.Manual;

        /// <summary>
        /// texture for painting.
        /// Sync to sourceRenderers.
        /// [NG] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public List<Texture2D> paintMaps = new List<Texture2D>();

        /// <summary>
        /// Root bone list used in BoneCloth.
        /// [NG] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public List<Transform> rootBones = new List<Transform>();

        /// <summary>
        /// BoneCloth connection method.
        /// [NG] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public RenderSetupData.BoneConnectionMode connectionMode = RenderSetupData.BoneConnectionMode.Line;

        /// <summary>
        /// Transform rotation interpolation rate in BoneCloth.(0.0 ~ 1.0)
        /// (0.0=parent-based, 0.5=middle, 1.0=child-based)
        /// [OK] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float rotationalInterpolation = 0.5f;

        /// <summary>
        /// Rotation interpolation rate of Root Transform in BoneCloth.(0.0 ~ 1.0)
        /// (0.0=does not rotate, 0.5=middle, 1.0=child-based)
        /// [OK] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float rootRotation = 0.5f;

        /// <summary>
        /// Set the update timing.
        /// [OK] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public ClothUpdateMode updateMode = ClothUpdateMode.Normal;

        /// <summary>
        /// Blend ratio between initial pose and animation pose.
        /// [OK] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float animationPoseRatio = 0.0f;

        /// <summary>
        /// vertex reduction parameters.
        /// </summary>
        public ReductionSettings reductionSetting = new ReductionSettings();

        /// <summary>
        /// custom skinning parameters.
        /// </summary>
        public CustomSkinningSettings customSkinningSetting = new CustomSkinningSettings();

        /// <summary>
        /// Normal definition.
        /// </summary>
        public NormalAlignmentSettings normalAlignmentSetting = new NormalAlignmentSettings();

        /// <summary>
        /// culling settings.
        /// </summary>
        public CullingSettings cullingSettings = new CullingSettings();

        /// <summary>
        /// axis to use as normal.
        /// [OK] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public ClothNormalAxis normalAxis = ClothNormalAxis.Up;

        /// <summary>
        /// Gravity.
        /// [OK] Runtime changes.
        /// [OK] Export/Import with Presets
        /// </summary>
        [Range(0.0f, 10.0f)]
        public float gravity = 5.0f;

        /// <summary>
        /// Gravity world direction.
        /// [OK] Runtime changes.
        /// [OK] Export/Import with Presets
        /// </summary>
        public float3 gravityDirection = new float3(0, -1, 0);

        /// <summary>
        /// 初期姿勢での重力の減衰率(0.0 ~ 1.0)
        /// 1.0にすることで初期姿勢では重力係数が０になる。
        /// 0.0では常にどの姿勢でも重力が100%発生する。
        /// 
        /// Attenuation rate of gravity at initial pose (0.0 ~ 1.0)
        /// By setting it to 1.0, the gravity coefficient becomes 0 in the initial posture.
        /// At 0.0, gravity is always 100% in any pose.
        /// 
        /// [OK] Runtime changes.
        /// [OK] Export/Import with Presets
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float gravityFalloff = 0.0f;

        /// <summary>
        /// リセット後の速度安定化時間(s)
        /// 急激な速度変化を抑えます。
        /// 
        /// Speed stabilization time after reset (s).
        /// Avoid sudden speed changes.
        /// 
        /// [OK] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float stablizationTimeAfterReset = 0.1f;

        /// <summary>
        /// 元の姿勢とシミュレーション結果のブレンド割合(0.0 ~ 1.0)
        /// 
        /// Blend ratio of original posture and simulation result (0.0 ~ 1.0).
        /// 
        /// [OK] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        [System.NonSerialized]
        public float blendWeight = 1.0f;

        /// <summary>
        /// air resistance.
        /// [OK] Runtime changes.
        /// [OK] Export/Import with Presets
        /// </summary>
        public CurveSerializeData damping = new CurveSerializeData(0.05f);

        /// <summary>
        /// Particle radius.
        /// [OK] Runtime changes.
        /// [OK] Export/Import with Presets
        /// </summary>
        public CurveSerializeData radius = new CurveSerializeData(0.02f);

        /// <summary>
        /// Inertia.
        /// </summary>
        public InertiaConstraint.SerializeData inertiaConstraint = new InertiaConstraint.SerializeData();

        /// <summary>
        /// Tether.
        /// </summary>
        public TetherConstraint.SerializeData tetherConstraint = new TetherConstraint.SerializeData();

        /// <summary>
        /// Distance restoration.
        /// </summary>
        public DistanceConstraint.SerializeData distanceConstraint = new DistanceConstraint.SerializeData();

        /// <summary>
        /// Triangle bending / volume.
        /// </summary>
        public TriangleBendingConstraint.SerializeData triangleBendingConstraint = new TriangleBendingConstraint.SerializeData();

        /// <summary>
        /// Angle restoration.
        /// </summary>
        public AngleConstraint.RestorationSerializeData angleRestorationConstraint = new AngleConstraint.RestorationSerializeData();

        /// <summary>
        /// Angle Limit.
        /// </summary>
        public AngleConstraint.LimitSerializeData angleLimitConstraint = new AngleConstraint.LimitSerializeData();

        /// <summary>
        /// Max distance / Backstop
        /// </summary>
        public MotionConstraint.SerializeData motionConstraint = new MotionConstraint.SerializeData();

        /// <summary>
        /// Collider collision.
        /// </summary>
        public ColliderCollisionConstraint.SerializeData colliderCollisionConstraint = new ColliderCollisionConstraint.SerializeData();

        /// <summary>
        /// Self collision
        /// </summary>
        public SelfCollisionConstraint.SerializeData selfCollisionConstraint = new SelfCollisionConstraint.SerializeData();

        /// <summary>
        /// Wind
        /// </summary>
        public WindSettings wind = new WindSettings();
    }
}
