// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    public abstract class ColliderComponent : ClothBehaviour, IDataValidate
    {
        /// <summary>
        /// トランスフォームからの中心ローカルオフセット
        /// Center local offset from transform.
        /// </summary>
        public Vector3 center;

        /// <summary>
        /// Size
        /// Sphere(x:radius)
        /// Capsule(x:start radius, y:end radius, z:length)
        /// Box(x:size x, y:size y, z:size z)
        /// </summary>
        [SerializeField]
        protected Vector3 size;


        //=========================================================================================
        /// <summary>
        /// Collider type.
        /// </summary>
        /// <returns></returns>
        public abstract ColliderManager.ColliderType GetColliderType();

        /// <summary>
        /// パラメータの検証
        /// </summary>
        public abstract void DataValidate();

        //=========================================================================================
        /// <summary>
        /// 登録チーム
        /// </summary>
        private HashSet<int> teamIdSet = new HashSet<int>();

        //=========================================================================================
        /// <summary>
        /// Get collider size.
        /// 
        /// Sphere(x:radius)
        /// Capsule(x:start radius, y:end radius, z:length)
        /// Box(x:size x, y:size y, z:size z)
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual Vector3 GetSize()
        {
            return size;
        }

        public void SetSize(Vector3 size)
        {
            this.size = size;
        }

        /// <summary>
        /// スケール値を取得
        /// </summary>
        /// <returns></returns>
        public float GetScale()
        {
            // X軸のみを見る
            return transform.lossyScale.x;
        }

        /// <summary>
        /// チームへのコライダー登録通知
        /// </summary>
        /// <param name="teamId"></param>
        internal void Register(int teamId)
        {
            teamIdSet.Add(teamId);
        }

        /// <summary>
        /// チームからのコライダー解除通知
        /// </summary>
        /// <param name="teamId"></param>
        internal void Exit(int teamId)
        {
            teamIdSet.Remove(teamId);
        }

        /// <summary>
        /// パラメータの反映
        /// すでに実行状態の場合はこの関数を呼び出さないとプロパティの変更が反映されません。
        /// Reflection of parameters.
        /// If it is already running, property changes will not be reflected unless this function is called.
        /// </summary>
        public void UpdateParameters()
        {
            // パラメータの検証
            DataValidate();

            foreach (int teamId in teamIdSet)
            {
                MagicaManager.Collider.UpdateParameters(this, teamId);
            }
        }

        //=========================================================================================
        protected virtual void Start()
        {
        }

        protected virtual void OnValidate()
        {
            UpdateParameters();
        }

        protected virtual void OnEnable()
        {
            // コライダーを有効にする
            foreach (int teamId in teamIdSet)
            {
                MagicaManager.Collider.EnableCollider(this, teamId, true);
            }
        }

        protected virtual void OnDisable()
        {
            // コライダーを無効にする
            foreach (int teamId in teamIdSet)
            {
                MagicaManager.Collider.EnableCollider(this, teamId, false);
            }
        }

        protected virtual void OnDestroy()
        {
            // コライダーを削除する
            foreach (int teamId in teamIdSet)
            {
                MagicaManager.Collider.RemoveCollider(this, teamId);
            }
            teamIdSet.Clear();
        }
    }
}
