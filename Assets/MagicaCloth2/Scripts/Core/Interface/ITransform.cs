// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    interface ITransform
    {
        /// <summary>
        /// 利用しているすべてのトランスフォームをtransformSetに追加する
        /// </summary>
        /// <param name="transformSet"></param>
        void GetUsedTransform(HashSet<Transform> transformSet);

        /// <summary>
        /// 利用しているトランスフォームを置換する
        /// key:置換対象トランスフォームのインスタンスID
        /// value:入れ替えるトランスフォーム
        /// </summary>
        /// <param name="replaceDict"></param>
        void ReplaceTransform(Dictionary<int, Transform> replaceDict);
    }
}
