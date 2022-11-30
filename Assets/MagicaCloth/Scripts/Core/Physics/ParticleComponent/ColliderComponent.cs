// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// コライダーパーティクルオブジェクト基底クラス
    /// </summary>
    public abstract class ColliderComponent : ParticleComponent
    {
        /// <summary>
        /// グローバルフラグ
        /// </summary>
        [SerializeField]
        protected bool isGlobal;

        [SerializeField]
        private Vector3 center;

        //=========================================================================================
        public Vector3 Center
        {
            get
            {
                return center;
            }
            set
            {
                center = value;
                ReserveDataUpdate();
            }
        }

        //=========================================================================================
        /// <summary>
        /// 初期化
        /// </summary>
        protected override void OnInit()
        {
            base.OnInit();

            // パーティクル初期化
            if (isGlobal)
            {
                CreateColliderParticle(0);
            }
        }

        /// <summary>
        /// 破棄
        /// </summary>
        protected override void OnDispose()
        {
            // チームからコライダーを解除する
            List<int> teamList = new List<int>();
            foreach (var teamId in particleDict.Keys)
            {
                teamList.Add(teamId);
            }
            foreach (var teamId in teamList)
            {
                RemoveColliderParticle(teamId);
            }
            base.OnDispose();
        }

        //=========================================================================================
        /// <summary>
        /// データハッシュ計算
        /// </summary>
        /// <returns></returns>
        public override int GetDataHash()
        {
            return isGlobal.GetDataHash();
        }

        /// <summary>
        /// 指定座標に最も近い衝突点pと、中心軸からのpへの方向dirを返す。
        /// ※エディタ計算用
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="p">衝突点</param>
        /// <param name="dir">中心軸位置から衝突点へのベクトル</param>
        /// <param name="d">最も近い中心軸位置</param>
        public abstract bool CalcNearPoint(Vector3 pos, out Vector3 p, out Vector3 dir, out Vector3 d, bool skinning);

        /// <summary>
        /// 指定座標のローカル位置を返す
        /// ※エディタ計算用
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public Vector3 CalcLocalPos(Vector3 pos)
        {
            // スケールは含めない
            var rot = transform.rotation;
            var v = pos - transform.position;
            return Quaternion.Inverse(rot) * v;
        }

        /// <summary>
        /// 指定方向のローカル方向を返す
        /// ※エディタ計算用
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public Vector3 CalcLocalDir(Vector3 dir)
        {
            return transform.InverseTransformDirection(dir);
        }

        /// <summary>
        /// 指定チーム用のコライダーパーティクルを作成
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public ChunkData CreateColliderParticle(int teamId)
        {
            var c = CreateColliderParticleReal(teamId);

            // すでにアクティブならばパーティクル有効化
            if (c.IsValid() && Status.IsActive)
                EnableTeamParticle(teamId);

            return c;
        }

        /// <summary>
        /// 指定チームのコライダーパーティクルを削除
        /// </summary>
        /// <param name="teamId"></param>
        public void RemoveColliderParticle(int teamId)
        {
            if (MagicaPhysicsManager.IsInstance() == false)
                return;

            if (particleDict.ContainsKey(teamId))
            {
                var c = particleDict[teamId];
                for (int i = 0; i < c.dataLength; i++)
                {
                    int pindex = c.startIndex + i;
                    MagicaPhysicsManager.Instance.Team.RemoveColliderParticle(teamId, pindex);
                }

                RemoveTeamParticle(teamId);
            }
        }

        //=========================================================================================
        /// <summary>
        /// 指定チーム用のコライダーパーティクル作成
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        protected abstract ChunkData CreateColliderParticleReal(int teamId);
    }
}
