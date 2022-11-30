// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MagicaCloth
{
    /// <summary>
    /// コンポーネント全体の管理
    /// </summary>
    public class PhysicsManagerComponent : PhysicsManagerAccess
    {
        /// <summary>
        /// すべてのコンポーネントのセット
        /// これは初期化の成否に関係なく無条件で登録されるので注意！
        /// 初期化完了の有無は comp.Status.IsInitSuccess で判定する
        /// </summary>
        private readonly HashSet<CoreComponent> componentSet = new HashSet<CoreComponent>();

        /// <summary>
        /// データ更新が必要なパーティクルコンポーネントセット
        /// </summary>
        private HashSet<ParticleComponent> dataUpdateParticleSet = new HashSet<ParticleComponent>();

        //=========================================================================================
        /// <summary>
        /// 初期設定
        /// </summary>
        public override void Create()
        {
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public override void Dispose()
        {
        }

        //=========================================================================================
        /// <summary>
        /// 登録コンポーネント数を返す
        /// </summary>
        public int ComponentCount
        {
            get
            {
                return componentSet.Count;
            }
        }

        /// <summary>
        /// 登録コンポーネントのコピーをリストで返す.
        /// Returns a list of copies of the registration component.
        /// </summary>
        /// <returns></returns>
        public List<CoreComponent> GetComponentList()
        {
            return new List<CoreComponent>(componentSet);
        }

        /// <summary>
        /// 登録コンポーネントに対してアクションを実行します
        /// </summary>
        /// <param name="act"></param>
        public void ComponentAction(System.Action<CoreComponent> act)
        {
            foreach (var comp in componentSet)
            {
                if (comp != null)
                    act(comp);
            }
        }

        /// <summary>
        /// 登録コンポーネントの実行状態を更新する
        /// </summary>
        public void UpdateComponentStatus()
        {
            foreach (var comp in componentSet)
            {
                if (comp == null)
                    continue;

                if (comp.Status.IsInitSuccess == false)
                    continue;

                comp.Status.UpdateStatus();
            }
        }

        //=========================================================================================
        public void AddComponent(CoreComponent comp)
        {
            //Debug.Log($"AddComponent:{comp.name}");
            componentSet.Add(comp);
        }

        public void RemoveComponent(CoreComponent comp)
        {
            //Debug.Log($"RemoveComponent:{comp.name}");
            if (componentSet.Contains(comp))
                componentSet.Remove(comp);
        }

        //=========================================================================================
        /// <summary>
        /// パーティクルコンポーネントをデータ更新予約に追加する
        /// </summary>
        /// <param name="comp"></param>
        internal void ReserveDataUpdateParticleComponent(ParticleComponent comp)
        {
            dataUpdateParticleSet.Add(comp);
        }

        /// <summary>
        /// 予約されたパーティクルコンポーネントのデータを変更し予約リストをクリアする
        /// </summary>
        internal void DataUpdateParticleComponent()
        {
            if (dataUpdateParticleSet.Count > 0)
            {
                foreach (var comp in dataUpdateParticleSet)
                {
                    comp?.DataUpdate();
                }
                dataUpdateParticleSet.Clear();
            }
        }
    }
}
