// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// MagicaClothのMonoBehaviour基礎クラス
    /// すべてのコンポーネントはここから派生する
    /// </summary>
    public abstract class ClothBehaviour : MonoBehaviour
    {
        /// <summary>
        /// Hash code for checking changes when editing.
        /// </summary>
        /// <returns></returns>
        public virtual int GetMagicaHashCode()
        {
            return 0;
        }
    }
}
