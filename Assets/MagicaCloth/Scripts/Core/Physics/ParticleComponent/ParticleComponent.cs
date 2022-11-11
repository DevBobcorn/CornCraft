// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// パーティクルゲームオブジェクト基底クラス
    /// オブジェクトは複数のチームから利用される可能性があり、その場合はチームごとにパーティクルを生成する
    /// </summary>
    public abstract class ParticleComponent : BaseComponent, IDataHash
    {
        /// <summary>
        /// パーティクルID
        /// チームごと(自身は0)
        /// </summary>
        protected Dictionary<int, ChunkData> particleDict = new Dictionary<int, ChunkData>();

        /// <summary>
        /// 実行状態
        /// </summary>
        protected RuntimeStatus status = new RuntimeStatus();

        public RuntimeStatus Status
        {
            get
            {
                return status;
            }
        }

        //=========================================================================================
        /// <summary>
        /// データを識別するハッシュコードを作成して返す
        /// </summary>
        /// <returns></returns>
        public abstract int GetDataHash();

        //=========================================================================================
        public int CenterParticleIndex
        {
            get
            {
                if (particleDict.ContainsKey(0))
                    return particleDict[0].startIndex;
                return -1;
            }
        }

        //=========================================================================================
        protected virtual void Start()
        {
            Init();
        }

        public virtual void OnEnable()
        {
            status.SetEnable(true);
            status.UpdateStatus();
        }

        public virtual void OnDisable()
        {
            status.SetEnable(false);
            status.UpdateStatus();
        }

        protected virtual void OnDestroy()
        {
            OnDispose();
            status.SetDispose();
        }

        // 基本的にVerifyData()は常にtrueなので更新の必要なし
        //protected virtual void Update()
        //{
        //    if (status.IsInitSuccess)
        //    {
        //        var error = !VerifyData();
        //        status.SetRuntimeError(error);
        //        UpdateStatus();

        //        if (status.IsActive)
        //            OnUpdate();
        //    }
        //}

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

        // 実行状態の更新通知
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
        public virtual bool VerifyData()
        {
            return true;
        }

        //=========================================================================================
        /// <summary>
        /// 初期化
        /// </summary>
        protected virtual void OnInit() { }

        /// <summary>
        /// 破棄
        /// </summary>
        protected virtual void OnDispose()
        {
            if (MagicaPhysicsManager.IsInstance() == false)
                return;

            // パーティクルを破棄する
            RemoveParticle();
        }

        /// <summary>
        /// 更新
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// 実行状態に入った場合に呼ばれます
        /// </summary>
        protected virtual void OnActive()
        {
            // パーティクル有効化
            EnableParticle();
        }

        /// <summary>
        /// 実行状態から抜けた場合に呼ばれます
        /// </summary>
        protected virtual void OnInactive()
        {
            // パーティクル無効化
            DisableParticle();
        }

        //=========================================================================================
        /// <summary>
        /// パーティクル有効化
        /// </summary>
        protected void EnableParticle()
        {
            foreach (var teamId in particleDict.Keys)
            {
                EnableTeamParticle(teamId);
            }
        }

        /// <summary>
        /// パーティクル無効化
        /// </summary>
        protected void DisableParticle()
        {
            if (MagicaPhysicsManager.IsInstance())
            {
                foreach (var teamId in particleDict.Keys)
                {
                    DisableTeamParticle(teamId);
                }
            }
        }

        /// <summary>
        /// チームパーティクル有効化
        /// </summary>
        /// <param name="teamId"></param>
        protected void EnableTeamParticle(int teamId)
        {
            var c = particleDict[teamId];
            MagicaPhysicsManager.Instance.Particle.SetEnable(
                c,
                true,
                UserTransform,
                UserTransformLocalPosition,
                UserTransformLocalRotation
                );
        }

        /// <summary>
        /// チームパーティクル無効化
        /// </summary>
        /// <param name="teamId"></param>
        protected void DisableTeamParticle(int teamId)
        {
            var c = particleDict[teamId];
            MagicaPhysicsManager.Instance.Particle.SetEnable(
                c,
                false,
                UserTransform,
                UserTransformLocalPosition,
                UserTransformLocalRotation
                );
        }

        /// <summary>
        /// パーティクルのデータ更新を予約する
        /// </summary>
        protected void ReserveDataUpdate()
        {
            if (MagicaPhysicsManager.IsInstance())
                MagicaPhysicsManager.Instance.Component.ReserveDataUpdateParticleComponent(this);
        }

        /// <summary>
        /// パーティクルのデータ更新処理
        /// </summary>
        internal virtual void DataUpdate() { }

        /// <summary>
        /// 状態の更新
        /// </summary>
        internal void UpdateStatus()
        {
            status.UpdateStatus();
        }

        //=========================================================================================
        /// <summary>
        /// 指定チームのパーティクルを１つ作成
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="mass"></param>
        /// <param name="gravity"></param>
        /// <param name="drag"></param>
        /// <param name="maxVelocity"></param>
        /// <param name="depth"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        protected ChunkData CreateParticle(
            uint flag,
            int teamId,
            float depth,
            float3 radius,
            float3 localPos
            )
        {
            // すでに登録済みならば無効(v1.9.3)
            if (particleDict.ContainsKey(teamId))
            {
                return new ChunkData();
            }

            // 自動追加フラグ
            if (MagicaPhysicsManager.Instance.Team.IsFlag(teamId, PhysicsManagerTeamData.Flag_UpdatePhysics))
                flag |= PhysicsManagerParticleData.Flag_Transform_UnityPhysics;

            var t = transform;
            var c = MagicaPhysicsManager.Instance.Particle.CreateParticle(
                flag,
                teamId,
                t.position,
                t.rotation,
                depth,
                radius,
                localPos
                );
            particleDict.Add(teamId, c);

            // 初期状態はDisable
            DisableTeamParticle(teamId);

            return c;
        }

        /// <summary>
        /// 指定チームのパーティクルを削除
        /// </summary>
        /// <param name="teamId"></param>
        protected void RemoveTeamParticle(int teamId)
        {
            if (MagicaPhysicsManager.IsInstance())
            {
                // パーティクル無効化
                DisableTeamParticle(teamId);

                // パーティクル削除
                var c = particleDict[teamId];
                MagicaPhysicsManager.Instance.Particle.RemoveParticle(c);
                particleDict.Remove(teamId);
            }
        }

        /// <summary>
        /// パーティクル削除
        /// </summary>
        protected void RemoveParticle()
        {
            if (MagicaPhysicsManager.IsInstance())
            {
                foreach (var teamId in particleDict.Keys)
                {
                    RemoveTeamParticle(teamId);
                }
            }
            particleDict.Clear();
        }

        /// <summary>
        /// 頂点ごとの連動トランスフォーム設定（不要な場合はnull）
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected Transform UserTransform(int vindex)
        {
            return transform;
        }

        /// <summary>
        /// 頂点ごとの連動トランスフォームのLocalPositionを返す（不要な場合は0）
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected float3 UserTransformLocalPosition(int vindex)
        {
            return transform.localPosition;
        }

        /// <summary>
        /// 頂点ごとの連動トランスフォームのLocalRotationを返す（不要な場合はquaternion.identity)
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected quaternion UserTransformLocalRotation(int vindex)
        {
            return transform.localRotation;
        }
    }
}
