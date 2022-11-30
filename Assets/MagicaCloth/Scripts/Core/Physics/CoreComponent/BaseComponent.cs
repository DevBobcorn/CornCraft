// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// MonoBehaviourを継承するコンポーネントのベースクラス
    /// </summary>
    public abstract partial class BaseComponent : MonoBehaviour
    {
        //=========================================================================================
        /// <summary>
        /// コンポーネント種類を返す
        /// </summary>
        /// <returns></returns>
        public abstract ComponentType GetComponentType();
    }
}
