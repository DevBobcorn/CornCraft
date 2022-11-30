// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

namespace MagicaCloth
{
    /// <summary>
    /// データを識別ハッシュコード生成
    /// </summary>
    /// <returns></returns>
    public interface IDataHash
    {
        /// <summary>
        /// データを識別するハッシュコードを作成して返す
        /// </summary>
        /// <returns></returns>
        int GetDataHash();
    }
}
