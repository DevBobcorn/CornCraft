// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 風コンポーネントの基底クラス
    /// </summary>
    public abstract partial class WindComponent : BaseComponent
    {
        [SerializeField]
        [Range(0.0f, Define.Compute.MaxWindMain)]
        protected float main = 5.0f;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        protected float turbulence = 1.0f;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        protected float frequency = 1.0f;

        [SerializeField]
        protected Vector3 areaSize = new Vector3(5.0f, 5.0f, 5.0f);

        [SerializeField]
        protected float areaRadius = 5.0f;

        //[SerializeField]
        //protected Vector3 anchor;

        [SerializeField]
        [Range(-180, 180)]
        protected float directionAngleX = 0;

        [SerializeField]
        [Range(-180, 180)]
        protected float directionAngleY = 0;

        [SerializeField]
        protected PhysicsManagerWindData.DirectionType directionType;

        [SerializeField]
        protected BezierParam attenuation = new BezierParam(1f, 1f, false, 0.0f, false);

        //=========================================================================================
        /// <summary>
        /// 風データID
        /// </summary>
        protected int windId = -1;

        /// <summary>
        /// 実行状態
        /// </summary>
        protected RuntimeStatus status = new RuntimeStatus();

        internal RuntimeStatus Status
        {
            get
            {
                return status;
            }
        }

        //=========================================================================================
        protected virtual void Reset()
        {
            ResetParams();
        }

        protected virtual void OnValidate()
        {
            //anchor = math.clamp(anchor, -1, 1);
            areaSize = math.max(areaSize, 0.1f);
            areaRadius = math.max(areaRadius, 0.1f);

            if (Application.isPlaying)
                status.SetDirty();
        }

        // Animator/Animationによるプロパティ変更時コールバック
        void OnDidApplyAnimationProperties()
        {
            if (Application.isPlaying)
            {
                status.SetDirty();
            }
        }

        protected virtual void Start()
        {
            Init();
        }

        internal virtual void OnEnable()
        {
            status.SetEnable(true);
            status.UpdateStatus();
        }

        internal virtual void OnDisable()
        {
            status.SetEnable(false);
            status.UpdateStatus();
        }

        protected virtual void OnDestroy()
        {
            OnDispose();
            status.SetDispose();
        }

        protected virtual void Update()
        {
            if (status.IsInitSuccess)
            {
                var error = !VerifyData();
                status.SetRuntimeError(error);
                status.UpdateStatus();

                if (status.IsActive)
                    OnUpdate();
            }
        }

        //=========================================================================================
        /// <summary>
        /// 初期化
        /// 通常はStart()で呼ぶ
        /// </summary>
        /// <param name="vcnt"></param>
        void Init()
        {
            status.UpdateStatusAction = OnUpdateStatus;
            status.OwnerFunc = () => this;
            if (status.IsInitComplete || status.IsInitStart)
                return;
            status.SetInitStart();

            if (VerifyData() == false)
            {
                status.SetInitError();
                return;
            }

            OnInit();
            if (status.IsInitError)
                return;

            status.SetInitComplete();

            status.UpdateStatus();
        }

        // 実行状態の更新
        protected void OnUpdateStatus()
        {
            if (status.IsActive)
            {
                // 実行状態に入った
                OnActive();
            }
            else
            {
                // 実行状態から抜けた
                OnInactive();
            }
        }

        /// <summary>
        /// 現在のデータが正常（実行できる状態）か返す
        /// </summary>
        /// <returns></returns>
        internal virtual bool VerifyData()
        {
            return true;
        }

        //=========================================================================================
        /// <summary>
        /// 初期化
        /// </summary>
        protected virtual void OnInit()
        {
            // 風作成
            CreateWind();

            // すでにアクティブならば有効化
            if (Status.IsActive)
                EnableWind();
        }

        /// <summary>
        /// 破棄
        /// </summary>
        protected virtual void OnDispose()
        {
            if (MagicaPhysicsManager.IsInstance() == false)
                return;

            // 風を破棄する
            RemoveWind();
        }

        /// <summary>
        /// 更新
        /// </summary>
        protected virtual void OnUpdate()
        {
            // 内容変更に伴う再設定
            if (status.IsDirty)
            {
                status.ClearDirty();
                ChangeParameter();
            }
        }

        /// <summary>
        /// 実行状態に入った場合に呼ばれます
        /// </summary>
        protected virtual void OnActive()
        {
            // 風有効化
            EnableWind();
        }

        /// <summary>
        /// 実行状態から抜けた場合に呼ばれます
        /// </summary>
        protected virtual void OnInactive()
        {
            // 風無効化
            DisableWind();
        }

        //=========================================================================================
        /// <summary>
        /// 風有効化
        /// </summary>
        protected void EnableWind()
        {
            if (windId >= 0)
                MagicaPhysicsManager.Instance.Wind.SetEnable(windId, true, transform);
        }

        /// <summary>
        /// 風無効化
        /// </summary>
        protected void DisableWind()
        {
            if (MagicaPhysicsManager.IsInstance() == false)
                return;

            if (windId >= 0)
                MagicaPhysicsManager.Instance.Wind.SetEnable(windId, false, transform);
        }

        //=========================================================================================
        /// <summary>
        /// 風削除
        /// </summary>
        private void RemoveWind()
        {
            if (MagicaPhysicsManager.IsInstance())
            {
                if (windId >= 0)
                {
                    MagicaPhysicsManager.Instance.Wind.RemoveWind(windId);
                }
            }
            windId = -1;
        }

        /// <summary>
        /// 風作成
        /// </summary>
        private void CreateWind()
        {
            windId = MagicaPhysicsManager.Instance.Wind.CreateWind(
                GetWindType(), GetShapeType(), GetAreaSize(), IsAddition(), main, turbulence, frequency,
                GetLocalDirection(), GetDirectionType(), GetAreaVolume(), GetAreaLength(),
                attenuation
                );
            status.ClearDirty();
        }

        /// <summary>
        /// 現在の風方向(ローカル)
        /// </summary>
        /// <returns></returns>
        internal Vector3 GetLocalDirection()
        {
            var q = Quaternion.Euler(directionAngleX, directionAngleY, 0.0f);
            return q * Vector3.forward;
            //var rot = transform.rotation * q;
            //return rot * Vector3.forward;
        }

        /// <summary>
        /// 風パラメータの変更設定
        /// </summary>
        private void ChangeParameter()
        {
            if (windId >= 0)
            {
                MagicaPhysicsManager.Instance.Wind.SetParameter(
                    windId, GetAreaSize(), IsAddition(), main, turbulence, frequency,
                    GetLocalDirection(), GetAreaVolume(), GetAreaLength(),
                    attenuation
                    );
            }
        }

        //=========================================================================================
        /// <summary>
        /// 風タイプを返す
        /// </summary>
        /// <returns></returns>
        public abstract PhysicsManagerWindData.WindType GetWindType();

        /// <summary>
        /// 形状タイプを返す
        /// </summary>
        /// <returns></returns>
        public abstract PhysicsManagerWindData.ShapeType GetShapeType();

        /// <summary>
        /// 風向きタイプを返す
        /// </summary>
        /// <returns></returns>
        public abstract PhysicsManagerWindData.DirectionType GetDirectionType();

        /// <summary>
        /// 風が加算モードか返す
        /// </summary>
        /// <returns></returns>
        public abstract bool IsAddition();

        /// <summary>
        /// エリアサイズを返す
        /// </summary>
        /// <returns></returns>
        public abstract Vector3 GetAreaSize();

        /// <summary>
        /// アンカー位置を返す
        /// </summary>
        /// <returns></returns>
        //public abstract Vector3 GetAnchor();

        /// <summary>
        /// 風エリアの体積を返す
        /// </summary>
        /// <returns></returns>
        public abstract float GetAreaVolume();

        /// <summary>
        /// 風エリアの最大距離を返す
        /// </summary>
        /// <returns></returns>
        public abstract float GetAreaLength();

        /// <summary>
        /// パラメータ初期化
        /// </summary>
        protected abstract void ResetParams();
    }
}
