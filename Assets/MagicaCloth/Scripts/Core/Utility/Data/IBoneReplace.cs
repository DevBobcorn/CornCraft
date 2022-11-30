// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// ボーン置換インターフェース
    /// </summary>
    public interface IBoneReplace
    {
        /// <summary>
        /// 現在使用しているボーンを格納して返す
        /// </summary>
        /// <returns></returns>
        HashSet<Transform> GetUsedBones();

        /// <summary>
        /// ボーンを置換する
        /// </summary>
        /// <param name="boneReplaceDict"></param>
        void ReplaceBone<T>(Dictionary<T, Transform> boneReplaceDict) where T : class;
    }
}
