// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using Unity.Jobs;

namespace MagicaCloth
{
    /// <summary>
    /// ワーカーのベースクラス
    /// </summary>
    public abstract class PhysicsManagerWorker
    {
        /// <summary>
        /// 親マネージャ
        /// </summary>
        public MagicaPhysicsManager Manager { get; set; }

        //=========================================================================================
        protected virtual void Start()
        {
        }

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
        /// グループIDのデータを削除
        /// </summary>
        /// <param name="group"></param>
        public abstract void RemoveGroup(int group);

        /// <summary>
        /// データ開放
        /// </summary>
        public abstract void Release();

        /// <summary>
        /// トランスフォームリード中に実行するウォームアップ処理
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public abstract void Warmup();

        /// <summary>
        /// 物理更新前のワーカー処理
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public abstract JobHandle PreUpdate(JobHandle jobHandle);

        /// <summary>
        /// 物理更新後のワーカー処理
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public abstract JobHandle PostUpdate(JobHandle jobHandle);
    }
}
