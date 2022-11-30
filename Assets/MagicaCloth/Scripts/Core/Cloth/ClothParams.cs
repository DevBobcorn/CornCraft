// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// クロス基本パラメータ
    /// </summary>
    [System.Serializable]
    public class ClothParams
    {
        // アルゴリズム
        public enum Algorithm
        {
            [InspectorName("Algorithm 1 (Old Style)")]
            Algorithm_1 = 0,   // 従来

            [InspectorName("Algorithm 2")]
            Algorithm_2 = 1,   // v1.11.0より
        }
        [SerializeField]
        private Algorithm algorithm = Algorithm.Algorithm_1;

        // パーティクルサイズ
        [SerializeField]
        private BezierParam radius = new BezierParam(0.02f, 0.02f, true, 0.0f, false);

        // パーティクルの重さ
        [SerializeField]
        private BezierParam mass = new BezierParam(1.0f, 1.0f, true, 0.0f, false);

        // パーティクル重力加速度(m/s)
        [SerializeField]
        private bool useGravity = true;
        [SerializeField]
        private BezierParam gravity = new BezierParam(-9.8f, -9.8f, false, 0.0f, false);
        [SerializeField]
        private Vector3 gravityDirection = new Vector3(0.0f, 1.0f, 0.0f);
        //[SerializeField]
        //private bool useDirectionalDamping = true;
        //[SerializeField]
        //private Transform directionalDampingObject = null;
        //[SerializeField]
        //private BezierParam directionalDamping = new BezierParam(1.0f, 0.1f, true, -0.5f, true);

        // パーティクル空気抵抗値
        [SerializeField]
        private bool useDrag = true;
        [SerializeField]
        private BezierParam drag = new BezierParam(0.02f, 0.02f, true, 0.0f, false);

        //  パーティクル最大速度(m/s)
        [SerializeField]
        private bool useMaxVelocity = true;
        [SerializeField]
        private BezierParam maxVelocity = new BezierParam(3.0f, 3.0f, false, 0.0f, false);

        // ワールド移動影響
        [SerializeField]
        private Transform influenceTarget = null;
        [SerializeField]
        private float maxMoveSpeed = 3.0f; // (m/s)
        [SerializeField]
        private float maxRotationSpeed = 720.0f; // (deg/s)
        [SerializeField]
        private BezierParam worldMoveInfluence = new BezierParam(0.5f, 0.5f, false, 0.0f, false);
        [SerializeField]
        private BezierParam worldRotationInfluence = new BezierParam(1.0f, 1.0f, false, 0.0f, false);

        // 外力
        [SerializeField]
        private float massInfluence = 0.3f;
        [SerializeField]
        private BezierParam depthInfluence = new BezierParam(0.1f, 1.0f, true, 0.5f, true);
        [SerializeField]
        private float windInfluence = 1.0f;
        [SerializeField]
        private float windRandomScale = 0.7f;
        [SerializeField]
        private float windSynchronization = 0.6f;

        // 距離無効化
        [SerializeField]
        private bool useDistanceDisable = false;
        [SerializeField]
        private Transform disableReferenceObject = null;
        [SerializeField]
        private float disableDistance = 20.0f;
        [SerializeField]
        private float disableFadeDistance = 5.0f;

        // テレポート
        public enum TeleportMode
        {
            Reset = 0,
            Keep = 1,
        }
        [SerializeField]
        private bool useResetTeleport = false;
        [SerializeField]
        private float teleportDistance = 0.2f;
        [SerializeField]
        private float teleportRotation = 45.0f;
        [SerializeField]
        private TeleportMode teleportMode = TeleportMode.Reset;

        // リセット後の安定化
        [SerializeField]
        private float resetStabilizationTime = 0.1f;

        // ルートからの最小最大距離拘束
        [SerializeField]
        private bool useClampDistanceRatio = true;
        [SerializeField]
        private float clampDistanceMinRatio = 0.7f;
        [SerializeField]
        private float clampDistanceMaxRatio = 1.05f;
        [SerializeField]
        private float clampDistanceVelocityInfluence = 0.1f;

        // 原点からの移動範囲拘束
        [SerializeField]
        private bool useClampPositionLength = false;
        [SerializeField]
        private BezierParam clampPositionLength = new BezierParam(0.03f, 0.2f, true, 0.0f, false);
        [SerializeField]
        private float clampPositionRatioX = 1.0f;
        [SerializeField]
        private float clampPositionRatioY = 1.0f;
        [SerializeField]
        private float clampPositionRatioZ = 1.0f;
        [SerializeField]
        private float clampPositionVelocityInfluence = 0.2f;

        // 最大回転角度拘束
        [SerializeField]
        private bool useClampRotation = false;
        [SerializeField]
        private BezierParam clampRotationAngle = new BezierParam(0.0f, 45.0f, true, 0.0f, false); // [Algorithm 1]
        [SerializeField]
        private BezierParam clampRotationAngle2 = new BezierParam(0.0f, 45.0f, true, 0.0f, false); // [Algorithm 2]
        [SerializeField]
        private float clampRotationVelocityLimit = 1.0f; // [Algorithm 1]
        [SerializeField]
        private float clampRotationVelocityInfluence = 0.2f; // [Algorithm 1]

        // 距離復元拘束
        [SerializeField]
        private float restoreDistanceVelocityInfluence = 1.0f;
        [SerializeField]
        private BezierParam structDistanceStiffness = new BezierParam(1.0f, 1.0f, false, 0.0f, false);
        [SerializeField]
        private bool useBendDistance = false;
        [SerializeField]
        private int bendDistanceMaxCount = 2;
        [SerializeField]
        private BezierParam bendDistanceStiffness = new BezierParam(0.5f, 0.5f, false, 0.0f, false);
        [SerializeField]
        private bool useNearDistance = false;
        [SerializeField]
        private int nearDistanceMaxCount = 3;
        [SerializeField]
        private float nearDistanceMaxDepth = 1.0f;
        [SerializeField]
        private BezierParam nearDistanceLength = new BezierParam(0.1f, 0.1f, true, 0.0f, false);
        [SerializeField]
        private BezierParam nearDistanceStiffness = new BezierParam(0.3f, 0.3f, false, 0.0f, false);

        // 回転復元拘束
        [SerializeField]
        private bool useRestoreRotation = false;
        [SerializeField]
        private BezierParam restoreRotation = new BezierParam(0.05f, 0.005f, true, 0.0f, false); // [Algorithm 1]
        [SerializeField]
        private BezierParam restoreRotation2 = new BezierParam(0.05f, 0.005f, true, 0.0f, false); // [Algorithm 2]
        [SerializeField]
        private float restoreRotationVelocityInfluence = 0.2f; // [Algorithm 1]
        [SerializeField]
        private float restoreRotationVelocityInfluence2 = 0.2f; // [Algorithm 2]

        // スプリング拘束
        [SerializeField]
        private bool useSpring = false;
        [SerializeField]
        private float springPower = 0.017f;
        [SerializeField]
        private float springRadius = 0.1f;
        [SerializeField]
        private float springScaleX = 1;
        [SerializeField]
        private float springScaleY = 1;
        [SerializeField]
        private float springScaleZ = 1;
        [SerializeField]
        private float springIntensity = 1.0f;
        [SerializeField]
        private BezierParam springDirectionAtten = new BezierParam(1.0f, 0.0f, true, 0.234f, true);
        [SerializeField]
        private BezierParam springDistanceAtten = new BezierParam(1.0f, 0.0f, true, 0.395f, true);

        // スプリング回転調整拘束
        public enum AdjustMode
        {
            Fixed = 0,
            XYMove = 1,
            XZMove = 2,
            YZMove = 3,
        }
        [SerializeField]
        private AdjustMode adjustMode;
        [SerializeField]
        private float adjustRotationPower = 5.0f;

        // トライアングル曲げ拘束
        [SerializeField]
        private bool useTriangleBend = false;
        //[SerializeField]
        //private bool useTriangleBendIncludeFixed = false;
        [SerializeField]
        private BezierParam triangleBend = new BezierParam(1.0f, 1.0f, true, 0.0f, false); // [Algorithm 1]
        [SerializeField]
        private BezierParam triangleBend2 = new BezierParam(1.0f, 1.0f, true, 0.0f, false); // [Algorithm 2]

        // ねじれ補正
        [SerializeField]
        private bool useTwistCorrection = false;
        [SerializeField]
        private float twistRecoveryPower = 0.2f;

        // ボリューム拘束
        [SerializeField]
        private bool useVolume = false;
        [SerializeField]
        private float maxVolumeLength = 0.1f;
        [SerializeField]
        private BezierParam volumeStretchStiffness = new BezierParam(0.5f, 0.5f, true, 0.0f, false);
        [SerializeField]
        private BezierParam volumeShearStiffness = new BezierParam(0.5f, 0.5f, true, 0.0f, false);

        // コライダーコリジョン拘束
        [SerializeField]
        private bool useCollision = false;
        [SerializeField]
        private float friction = 0.1f;
        [SerializeField]
        private float staticFriction = 0.03f;

        // エッジコリジョン
        //[SerializeField]
        //private bool useEdgeCollision = false;
        //[SerializeField]
        //private float edgeCollisionRadius = 0.02f;

        // 浸透制限
        [SerializeField]
        private bool usePenetration = false;
        public enum PenetrationMode
        {
            SurfacePenetration = 0,
            ColliderPenetration = 1,
            //BonePenetration = 2,
        }
        [SerializeField]
        private PenetrationMode penetrationMode = PenetrationMode.SurfacePenetration;
        public enum PenetrationAxis
        {
            X = 0,
            Y = 1,
            Z = 2,
            InverseX = 3,
            InverseY = 4,
            InverseZ = 5,
        }
        [SerializeField]
        private PenetrationAxis penetrationAxis = PenetrationAxis.InverseZ;
        [SerializeField]
        private float penetrationMaxDepth = 1.0f;
        [SerializeField]
        private BezierParam penetrationConnectDistance = new BezierParam(0.1f, 0.2f, true, 0.0f, false);
        //[SerializeField]
        //private BezierParam penetrationStiffness = new BezierParam(1.0f, 1.0f, false, 0.0f, false);
        [SerializeField]
        private BezierParam penetrationDistance = new BezierParam(0.02f, 0.05f, true, 0.0f, false);
        [SerializeField]
        private BezierParam penetrationRadius = new BezierParam(0.3f, 1.0f, true, 0.0f, false);

        // 回転補間
        [SerializeField]
        private bool useLineAvarageRotation = true;
        [SerializeField]
        private bool useFixedNonRotation = false;

        // ベーススキニング
        //[SerializeField]
        //private bool useBaseSkinning = false;

        // コライダー方向移動制限拘束
        //[SerializeField]
        //private bool useDirectionMoveLimit = false;
        //[SerializeField]
        //private BezierParam directionMoveLimit = new BezierParam(0.03f, 0.03f, true, 0.0f, false);

        // セルフコリジョン拘束
        //[SerializeField]
        //private bool useSelfCollision = false;
        //[SerializeField]
        //private float selfCollisionInfluenceRange = 0.2f;
        //[SerializeField]
        //private int maxSelfCollisionCount = 6;
        //[SerializeField]
        //private BezierParam selfCollisionStiffness = new BezierParam(0.3f, 0.01f, true, 0.0f, false);
        //[SerializeField]
        //private BezierParam selfCollisionThickness = new BezierParam(0.01f, 0.01f, true, 0.0f, false);


        //=========================================================================================
        /// <summary>
        /// 変更チェック用（番号は変えないこと！）
        /// </summary>
        public enum ParamType
        {
            Radius = 0,
            Mass = 1,
            Gravity = 2,
            Drag = 3,
            MaxVelocity = 4,
            WorldInfluence = 5,
            ClampDistance = 6,
            ClampPosition = 7,
            ClampRotation = 8,
            RestoreDistance = 9,
            RestoreRotation = 10,
            Spring = 11,
            AdjustRotation = 12,
            AirLine = 13,
            TriangleBend = 14,
            Volume = 15,
            ColliderCollision = 16,
            RotationInterpolation = 17,
            DistanceDisable = 18,
            ExternalForce = 19,
            Penetration = 20,
            Algorithm = 21,
            BaseSkinning = 22,
            //DirectionMoveLimit,
            //SelfCollision,

            Max, // カウンタ用
        }

        // 変更記録セット
        private HashSet<ParamType> changeSet = new HashSet<ParamType>();

        public void SetChangeParam(ParamType ptype)
        {
            changeSet.Add(ptype);
        }

        public bool ChangedParam(ParamType ptype)
        {
            return changeSet.Contains(ptype);
        }

        public void ClearChangeParam()
        {
            changeSet.Clear();
        }

        //=========================================================================================
        /// <summary>
        /// 各パラメータ別のハッシュを取得する
        /// </summary>
        /// <param name="ptype"></param>
        /// <returns></returns>
        public int GetParamHash(BaseCloth cloth, ParamType ptype)
        {
            int hash = 0;

            switch (ptype)
            {
                case ParamType.Algorithm:
                    hash += algorithm.GetDataHash();
                    break;
                case ParamType.WorldInfluence:
                    hash += influenceTarget ? influenceTarget.GetDataHash() : 0;
                    break;
                case ParamType.RestoreDistance:
                    if (useBendDistance)
                    {
                        hash += useBendDistance.GetDataHash();
                        hash += bendDistanceMaxCount.GetDataHash();
                    }
                    if (useNearDistance)
                    {
                        hash += useNearDistance.GetDataHash();
                        hash += nearDistanceMaxCount.GetDataHash();
                        hash += nearDistanceMaxDepth.GetDataHash();
                    }
                    break;
                case ParamType.ClampDistance:
                    if (useClampDistanceRatio)
                        hash += useClampDistanceRatio.GetDataHash();
                    break;
                case ParamType.ClampPosition:
                    if (useClampPositionLength)
                        hash += useClampPositionLength.GetDataHash();
                    break;
                case ParamType.RestoreRotation:
                    if (useRestoreRotation)
                    {
                        hash += algorithm.GetDataHash(); // algorithm
                        hash += useRestoreRotation.GetDataHash();
                    }
                    break;
                case ParamType.ClampRotation:
                    if (useClampRotation)
                    {
                        hash += algorithm.GetDataHash(); // algorithm
                        hash += useClampRotation.GetDataHash();
                    }
                    break;
                case ParamType.TriangleBend:
                    if (useTriangleBend)
                    {
                        hash += algorithm.GetDataHash(); // algorithm
                        hash += useTriangleBend.GetDataHash();
                        hash += useTwistCorrection.GetDataHash();
                    }
                    break;
                case ParamType.Penetration:
                    if (usePenetration)
                    {
                        hash += usePenetration.GetDataHash();
                        hash += penetrationMode.GetDataHash();
                        if (penetrationMode == PenetrationMode.SurfacePenetration)
                        {
                            hash += penetrationMaxDepth.GetDataHash();
                            hash += penetrationAxis.GetDataHash();
                        }
                        if (penetrationMode == PenetrationMode.ColliderPenetration)
                        {
                            hash += penetrationMaxDepth.GetDataHash();
                            hash += penetrationConnectDistance.GetDataHash();
                            hash += cloth.TeamData.ColliderList.GetDataHash();
                            hash += cloth.TeamData.PenetrationIgnoreColliderList.GetDataHash();
                        }
                    }
                    break;
                case ParamType.Spring:
                    if (useSpring)
                    {
                        hash += useSpring.GetDataHash();
                        hash += springRadius.GetDataHash();
                        hash += springScaleX.GetDataHash();
                        hash += springScaleY.GetDataHash();
                        hash += springScaleZ.GetDataHash();
                        hash += springDirectionAtten.GetDataHash();
                        hash += springDistanceAtten.GetDataHash();
                        hash += springIntensity.GetDataHash();
                    }
                    break;
                case ParamType.ColliderCollision:
                    if (useCollision)
                    {
                        hash += cloth.TeamData.ColliderList.GetDataHash();
                    }
                    break;
                case ParamType.BaseSkinning:
                    // 一旦休眠
                    //if (cloth.SkinningMode == PhysicsTeam.TeamSkinningMode.GenerateFromBones)
                    //{
                    //    hash += cloth.SkinningUpdateFixed.GetDataHash();
                    //}
                    break;
            }

            return hash;
        }

        //=========================================================================================
        // algorithm
        public Algorithm AlgorithmType
        {
            get => algorithm;
            set => algorithm = value;
        }

        // radius
        public void SetRadius(float sval, float eval)
        {
            radius.SetParam(sval, eval);
        }

        public float GetRadius(float depth)
        {
            return radius.Evaluate(depth);
        }

        public BezierParam GetRadius()
        {
            return radius;
        }

        // mass
        public void SetMass(float sval, float eval, bool useEval = true, float cval = 0.0f, bool useCval = false)
        {
            mass.SetParam(sval, eval, useEval, cval, useCval);
        }

        public BezierParam GetMass()
        {
            return mass;
        }

        // gravity
        public void SetGravity(bool sw, float sval = -9.8f, float eval = -9.8f)
        {
            useGravity = sw;
            gravity.SetParam(sval, eval);
        }

        public bool UseGravity
        {
            get
            {
                return useGravity;
            }
        }

        public BezierParam GetGravity()
        {
            if (useGravity)
                return gravity;
            else
                return new BezierParam(0.0f);
        }

        public Vector3 GravityDirection
        {
            get
            {
                return gravityDirection;
            }
            set
            {
                gravityDirection = value;
            }
        }

#if false
        // 重力の方向性減衰（現在は保留）
        public void SetDirectionalDamping(bool sw, float sval = 1.0f, float eval = 0.1f, float curve = -0.5f, Transform target = null)
        {
            useDirectionalDamping = sw;
            directionalDamping.SetParam(sval, eval, true, curve, true);
            directionalDampingObject = target;
        }

        public bool UseDirectionalDamping
        {
            get
            {
                return useDirectionalDamping;
            }
        }

        public Transform DirectionalDampingObject
        {
            get
            {
                return directionalDampingObject;
            }
            set
            {
                directionalDampingObject = value;
            }
        }

        public BezierParam GetDirectionalDamping()
        {
            if (useGravity)
                return directionalDamping;
            else
                return new BezierParam(0.0f);
        }
#endif


        // drag
        public void SetDrag(bool sw, float sval = 0.015f, float eval = 0.015f)
        {
            useDrag = sw;
            drag.SetParam(sval, eval);
        }

        public bool UseDrag
        {
            get
            {
                return useDrag;
            }
        }

        public BezierParam GetDrag()
        {
            if (useDrag)
                return drag;
            else
                return new BezierParam(0);
        }

        // max velocity
        public void SetMaxVelocity(bool sw, float sval = 3, float eval = 3)
        {
            useMaxVelocity = sw;
            maxVelocity.SetParam(sval, eval);
        }

        public bool UseMaxVelocity
        {
            get
            {
                return useMaxVelocity;
            }
        }

        public BezierParam GetMaxVelocity()
        {
            if (useMaxVelocity)
                return maxVelocity;
            else
                return new BezierParam(1000);
        }

        // external force
        public void SetExternalForce(float massInfluence, float windInfluence, float windRandomScale, float windSynchronization)
        {
            this.massInfluence = massInfluence;
            this.windInfluence = windInfluence;
            this.windRandomScale = windRandomScale;
            this.windSynchronization = windSynchronization;
        }

        public float MassInfluence
        {
            get => massInfluence;
            set => massInfluence = value;
        }

        public BezierParam GetDepthInfluence() => depthInfluence;

        public float WindInfluence
        {
            get => windInfluence;
            set => windInfluence = value;
        }

        public float WindRandomScale
        {
            get => windRandomScale;
            set => windRandomScale = value;
        }

        public float WindSynchronization
        {
            get => windSynchronization;
            set => windSynchronization = value;
        }

        // world move/rot influence
        public void SetWorldInfluence(float maxspeed, float moveval, float rotval)
        {
            maxMoveSpeed = maxspeed;
            worldMoveInfluence.SetParam(moveval, moveval, false);
            worldRotationInfluence.SetParam(rotval, rotval, false);
        }

        public BezierParam GetWorldMoveInfluence()
        {
            return worldMoveInfluence;
        }

        public BezierParam GetWorldRotationInfluence()
        {
            return worldRotationInfluence;
        }

        public Transform GetInfluenceTarget()
        {
            return influenceTarget;
        }

        public void SetInfluenceTarget(Transform t)
        {
            influenceTarget = t;
        }

        public float MaxMoveSpeed
        {
            get
            {
                return maxMoveSpeed;
            }
            set
            {
                maxMoveSpeed = value;
            }
        }

        public float MaxRotationSpeed
        {
            get
            {
                return maxRotationSpeed;
            }
            set
            {
                maxRotationSpeed = value;
            }
        }

        // reset teleport
        public void SetTeleport(bool sw, float distance = 0.2f, float rotation = 45.0f, TeleportMode mode = TeleportMode.Reset)
        {
            useResetTeleport = sw;
            teleportDistance = distance;
            teleportRotation = rotation;
            teleportMode = mode;
        }

        public bool UseResetTeleport
        {
            get
            {
                return useResetTeleport;
            }
            set
            {
                useResetTeleport = value;
            }
        }

        public float TeleportDistance
        {
            get
            {
                //return useResetTeleport ? teleportDistance : 100000.0f;
                return teleportDistance;
            }
            set
            {
                teleportDistance = value;
            }
        }

        public float TeleportRotation
        {
            get
            {
                //return useResetTeleport ? teleportRotation : 360.0f;
                return teleportRotation;
            }
            set
            {
                teleportRotation = value;
            }
        }

        public TeleportMode TeleportResetMode
        {
            get
            {
                return teleportMode;
            }
            set
            {
                teleportMode = value;
            }
        }

        // stabilize after reset
        public float ResetStabilizationTime
        {
            get
            {
                return resetStabilizationTime;
            }
            set
            {
                resetStabilizationTime = value;
            }
        }

        // disable distance
        public void SetDistanceDisable(bool sw, float distance = 20.0f, float fadeDistance = 5.0f, Transform referenceObject = null)
        {
            useDistanceDisable = sw;
            disableReferenceObject = referenceObject;
            disableDistance = distance;
            disableFadeDistance = fadeDistance;
        }

        public bool UseDistanceDisable
        {
            get
            {
                return useDistanceDisable;
            }
            set
            {
                useDistanceDisable = value;
            }
        }

        public Transform DisableReferenceObject
        {
            get
            {
                return disableReferenceObject;
            }
            set
            {
                disableReferenceObject = value;
            }
        }

        public float DisableDistance
        {
            get
            {
                return disableDistance;
            }
            set
            {
                disableDistance = value;
            }
        }

        public float DisableFadeDistance
        {
            get
            {
                return disableFadeDistance;
            }
            set
            {
                disableFadeDistance = value;
            }
        }

        // clamp distance
        public void SetClampDistanceRatio(bool sw, float minval = 0.1f, float maxval = 1.05f, float influence = 0.2f)
        {
            useClampDistanceRatio = sw;
            clampDistanceMinRatio = minval;
            clampDistanceMaxRatio = maxval;
            clampDistanceVelocityInfluence = influence;
        }

        public bool UseClampDistanceRatio
        {
            get
            {
                return useClampDistanceRatio;
            }
        }

        public float ClampDistanceMinRatio
        {
            get
            {
                return useClampDistanceRatio ? clampDistanceMinRatio : 0;
            }
        }

        public float ClampDistanceMaxRatio
        {
            get
            {
                return useClampDistanceRatio ? clampDistanceMaxRatio : 0;
            }
        }

        public float ClampDistanceVelocityInfluence
        {
            get
            {
                return useClampDistanceRatio ? clampDistanceVelocityInfluence : 1;
            }
        }

        // clamp position
        public void SetClampPositionLength(bool sw, float sval = 0.03f, float eval = 0.2f, float ratioX = 1, float ratioY = 1, float ratioZ = 1, float influence = 0.2f)
        {
            useClampPositionLength = sw;
            clampPositionLength.SetParam(sval, eval);
            clampPositionRatioX = ratioX;
            clampPositionRatioY = ratioY;
            clampPositionRatioZ = ratioZ;
            clampPositionVelocityInfluence = influence;
        }

        public bool UseClampPositionLength
        {
            get
            {
                return useClampPositionLength;
            }
        }

        public Vector3 ClampPositionAxisRatio
        {
            get
            {
                return new Vector3(clampPositionRatioX, clampPositionRatioY, clampPositionRatioZ);
            }
        }

        public BezierParam GetClampPositionLength()
        {
            return clampPositionLength;
        }

        public float ClampPositionVelocityInfluence
        {
            get
            {
                return useClampPositionLength ? clampPositionVelocityInfluence : 1;
            }
        }

        // clamp rotation
        public void SetClampRotationAngle(bool sw, float sval = 0.0f, float eval = 180.0f, float influence = 0.2f)
        {
            useClampRotation = sw;
            clampRotationAngle.SetParam(sval, eval);
            clampRotationAngle2.SetParam(sval, eval);
            clampRotationVelocityInfluence = influence;
        }

        public bool UseClampRotation => useClampRotation;

        public BezierParam GetClampRotationAngle(Algorithm algo)
        {
            if (algo == Algorithm.Algorithm_2)
                return clampRotationAngle2;
            else
                return clampRotationAngle;
        }

        public float ClampRotationVelocityInfluence
        {
            get
            {
                return useClampRotation ? clampRotationVelocityInfluence : 1;
            }
        }

        public float GetClampRotationVelocityLimit(Algorithm algo)
        {
            if (useClampRotation)
            {
                if (algo == Algorithm.Algorithm_2)
                    return 1.0f; // On
                else
                    return clampRotationVelocityLimit;
            }
            else
                return 0.0f;
        }

        // restore distance
        public void SetRestoreDistance(float influence = 1.0f, float structStiffness = 1.0f)
        {
            restoreDistanceVelocityInfluence = influence;
            structDistanceStiffness.SetParam(structStiffness, structStiffness, false);
        }

        public float RestoreDistanceVelocityInfluence
        {
            get
            {
                return restoreDistanceVelocityInfluence;
            }
        }

        public BezierParam GetStructDistanceStiffness()
        {
            return structDistanceStiffness;
        }

        public bool UseBendDistance
        {
            get
            {
                return useBendDistance;
            }
        }

        public int BendDistanceMaxCount
        {
            get
            {
                return bendDistanceMaxCount;
            }
        }

        public BezierParam GetBendDistanceStiffness()
        {
            return bendDistanceStiffness;
        }

        public bool UseNearDistance
        {
            get
            {
                return useNearDistance;
            }
        }

        public int NearDistanceMaxCount
        {
            get
            {
                return nearDistanceMaxCount;
            }
        }

        public float NearDistanceMaxDepth
        {
            get
            {
                return nearDistanceMaxDepth;
            }
        }

        public BezierParam GetNearDistanceLength()
        {
            return nearDistanceLength;
        }

        public BezierParam GetNearDistanceStiffness()
        {
            return nearDistanceStiffness;
        }

        // restore rotation
        public void SetRestoreRotation(bool sw, float sval = 0.02f, float eval = 0.001f, float influence = 0.3f)
        {
            useRestoreRotation = sw;
            restoreRotation.SetParam(sval, eval);
            restoreRotation2.SetParam(sval, eval);
            restoreRotationVelocityInfluence = influence;
            restoreRotationVelocityInfluence2 = influence;
        }

        public bool UseRestoreRotation
        {
            get
            {
                return useRestoreRotation;
            }
        }

        public BezierParam GetRestoreRotationPower(Algorithm algo)
        {
            if (algo == Algorithm.Algorithm_2)
                return restoreRotation2;
            else
                return restoreRotation;
        }

        public float GetRestoreRotationVelocityInfluence(Algorithm algo)
        {
            if (algo == Algorithm.Algorithm_2)
                return restoreRotationVelocityInfluence2;
            else
                return restoreRotationVelocityInfluence;
        }

        // spring
        public void SetSpring(bool sw, float power = 0.0f, float r = 0.0f, float sclx = 1, float scly = 1, float sclz = 1, float intensity = 1)
        {
            useSpring = sw;
            springPower = power;
            springRadius = r;
            springScaleX = sclx;
            springScaleY = scly;
            springScaleZ = sclz;
            springIntensity = intensity;
        }

        public void SetSpringDirectionAtten(float sval, float eval, float cval)
        {
            springDirectionAtten.SetParam(sval, eval, true, cval, true);
        }

        public void SetSpringDistanceAtten(float sval, float eval, float cval)
        {
            springDistanceAtten.SetParam(sval, eval, true, cval, true);
        }

        public bool UseSpring
        {
            get => useSpring;
            set => useSpring = value;
        }

        public float GetSpringPower()
        {
            if (useSpring)
                return springPower;
            else
                return 0;
        }

        public float SpringPowr
        {
            get => springPower;
            set => springPower = value;
        }

        public float SpringRadius
        {
            get
            {
                return springRadius;
            }
        }

        public Vector3 SpringRadiusScale
        {
            get
            {
                return new Vector3(springScaleX, springScaleY, springScaleZ);
            }
        }

        public float SpringIntensity
        {
            get
            {
                return springIntensity;
            }
        }

        public float GetSpringDirectionAtten(float ratio)
        {
            return springDirectionAtten.Evaluate(ratio);
        }

        public float GetSpringDistanceAtten(float ratio)
        {
            return springDistanceAtten.Evaluate(ratio);
        }

        // adjust spring rotation
        public void SetAdjustRotation(AdjustMode amode = AdjustMode.Fixed, float power = 0.0f)
        {
            adjustMode = amode;
            adjustRotationPower = power;
        }

        public AdjustMode AdjustRotationMode
        {
            get
            {
                return adjustMode;
            }
        }

        public Vector3 AdjustRotationVector
        {
            get
            {
                // 移動軸調整の場合は、各軸の回転力が入る
                Vector3 vec = Vector3.one;
                vec *= adjustRotationPower;
                return vec;
            }
        }

        // triangle bend
        public void SetTriangleBend(bool sw, float sval = 1.0f, float eval = 1.0f)
        {
            useTriangleBend = sw;
            triangleBend.SetParam(sval, eval);
            triangleBend2.SetParam(sval, eval);
        }

        public bool UseTriangleBend
        {
            get
            {
                return useTriangleBend;
            }
        }

        //public bool UseTrianlgeBendIncludeFixed => useTriangleBendIncludeFixed;
        public BezierParam GetTriangleBendStiffness(Algorithm algo)
        {
            if (algo == Algorithm.Algorithm_2)
                return triangleBend2;
            else
                return triangleBend;
        }

        // Twist
        internal bool GetUseTwistCorrection(Algorithm algo)
        {
            if (algo == Algorithm.Algorithm_2)
                return useTwistCorrection;
            else
                return false;
        }
        internal float TwistRecoveryPower => twistRecoveryPower;

        // volume
        public void SetVolume(bool sw, float maxLength = 0.05f, float stiffness = 0.5f, float shear = 0.5f)
        {
            useVolume = sw;
            maxVolumeLength = maxLength;
            volumeShearStiffness.SetParam(stiffness, stiffness, false);
            volumeShearStiffness.SetParam(shear, shear, false);
        }

        public bool UseVolume
        {
            get
            {
                return useVolume;
            }
        }

        public float GetMaxVolumeLength()
        {
            if (useVolume)
            {
                return maxVolumeLength;
            }
            else
                return 0;
        }

        public BezierParam GetVolumeStretchStiffness()
        {
            return volumeStretchStiffness;
        }

        public BezierParam GetVolumeShearStiffness()
        {
            return volumeShearStiffness;
        }

        // collider collision
        public void SetCollision(bool sw, float dynamicFriction = 0.1f, float staticFriction = 0.03f)
        {
            useCollision = sw;
            this.friction = dynamicFriction;
            this.staticFriction = staticFriction;
        }

        public bool UseCollision => useCollision;
        public float DynamicFriction => friction;
        public float StaticFriction => staticFriction;

        //public bool KeepInitialShape
        //{
        //    get
        //    {
        //        return keepInitialShape;
        //    }
        //}

        // penetration
        public bool UsePenetration
        {
            get
            {
                return usePenetration;
            }
            set
            {
                usePenetration = value;
            }
        }

        public PenetrationMode GetPenetrationMode()
        {
            return penetrationMode;
        }

        public PenetrationAxis GetPenetrationAxis()
        {
            return penetrationAxis;
        }

        public float PenetrationMaxDepth
        {
            get
            {
                return penetrationMaxDepth;
            }
        }

        public BezierParam GetPenetrationConnectDistance()
        {
            return penetrationConnectDistance;
        }

        //public BezierParam GetPenetrationStiffness()
        //{
        //    return penetrationStiffness;
        //}

        public BezierParam GetPenetrationRadius()
        {
            return penetrationRadius;
        }

        public BezierParam GetPenetrationDistance()
        {
            return penetrationDistance;
        }

        // rotation interpolation
        public bool UseLineAvarageRotation
        {
            get
            {
                return useLineAvarageRotation;
            }
        }

        public bool UseFixedNonRotation
        {
            get
            {
                return useFixedNonRotation;
            }
        }

        // base skinning
        //public bool UseBaseSkinning
        //{
        //    get
        //    {
        //        return useBaseSkinning;
        //    }
        //}

        // self collision
        //public void SetSelfCollision(bool sw)
        //{
        //    useSelfCollision = sw;
        //}

        //public bool UseSelfCollision
        //{
        //    get
        //    {
        //        return useSelfCollision;
        //    }
        //}

        //public float SelfCollisionINfluenceRange
        //{
        //    get
        //    {
        //        return selfCollisionInfluenceRange;
        //    }
        //}

        //public int MaxSelfCollisionCount
        //{
        //    get
        //    {
        //        return maxSelfCollisionCount;
        //    }
        //}

        //public float GetSelfCollisionStiffness(float depth)
        //{
        //    if (useSelfCollision)
        //        return selfCollisionStiffness.Evaluate(depth);
        //    else
        //        return 0.0f;
        //}

        //public float GetSelfCollisionThickness(float depth)
        //{
        //    if (useSelfCollision)
        //        return selfCollisionThickness.Evaluate(depth);
        //    else
        //        return 0.0f;
        //}

        // direction move limit
        //public void SetDirectionMoveLimit(bool sw, float sval = 0.05f, float eval = 0.05f)
        //{
        //    useDirectionMoveLimit = sw;
        //    directionMoveLimit.SetParam(sval, eval);
        //}

        //public bool UseDirectionMoveLimit
        //{
        //    get
        //    {
        //        return useDirectionMoveLimit;
        //    }
        //}

        //public float GetDirectionMoveLength(float depth)
        //{
        //    if (useDirectionMoveLimit)
        //        return directionMoveLimit.Evaluate(depth);
        //    else
        //        return -1; // 無効
        //}

        //=========================================================================================
        /// <summary>
        /// 古いパラメータを最新アルゴリズム用にコンバートする
        /// </summary>
        public void ConvertToLatestAlgorithmParameter()
        {
            // ClampRotation
            clampRotationAngle2 = clampRotationAngle.Clone();

            // RestoreRotation
            restoreRotation2 = restoreRotation.Clone();
            restoreRotationVelocityInfluence2 = restoreRotationVelocityInfluence;

            // Triangle Bend
            triangleBend2 = triangleBend.Clone();
        }
    }
}
