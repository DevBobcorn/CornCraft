// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;

namespace MagicaCloth
{
    /// <summary>
    /// ShareDataObject共有データクラスに関する操作インターフェイス
    /// </summary>
    public interface IShareDataObject
    {
        /// <summary>
        /// オブジェクトが管理する、すべてのShareDataObjectをリストで返す。無い場合はnull
        /// これは主にエディタ環境でのサブアセット保存処理で使用される。
        /// </summary>
        /// <returns></returns>
        List<ShareDataObject> GetAllShareDataObject();

        /// <summary>
        /// sourceの共有データを複製して再セットする
        /// 再セットした共有データを返す
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        ShareDataObject DuplicateShareDataObject(ShareDataObject source);
    }
}
