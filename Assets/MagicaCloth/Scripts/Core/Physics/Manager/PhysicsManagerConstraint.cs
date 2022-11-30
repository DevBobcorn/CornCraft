// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using Unity.Jobs;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 拘束のベースクラス
    /// </summary>
    public abstract class PhysicsManagerConstraint
    {
        // 反復１回の実行回数
        [Range(1, 4)]
        public int iteration = 1;

        /// <summary>
        /// 親マネージャ
        /// </summary>
        public MagicaPhysicsManager Manager { get; set; }

        //=========================================================================================
        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="manager"></param>
        public void Init(MagicaPhysicsManager manager)
        {
            Manager = manager;
            Create();
        }

        /// <summary>
        /// データ作成
        /// </summary>
        public abstract void Create();

        /// <summary>
        /// チームIDのデータを削除
        /// </summary>
        /// <param name="teamId"></param>
        public abstract void RemoveTeam(int teamId);

        /// <summary>
        /// データ開放
        /// </summary>
        public abstract void Release();

        /// <summary>
        /// 拘束の更新回数取得
        /// </summary>
        /// <returns></returns>
        public virtual int GetIterationCount()
        {
            return iteration;
        }

        /// <summary>
        /// 拘束の解決（ステップ回数実行される）
        /// </summary>
        /// <param name="dtime">ステップ時間</param>
        /// <param name="updatePower">90upsを基準とした更新力</param>
        /// <param name="iteration">同フレームでの実行カウント(0～)</param>
        public abstract JobHandle SolverConstraint(int runCount, float dtime, float updatePower, int iteration, JobHandle jobHandle);
    }
}
