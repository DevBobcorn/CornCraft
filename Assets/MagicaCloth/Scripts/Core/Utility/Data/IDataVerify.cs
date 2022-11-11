// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
namespace MagicaCloth
{
    /// <summary>
    /// データ検証インターフェース
    /// </summary>
    public interface IDataVerify
    {
        /// <summary>
        /// データバージョンを取得する
        /// </summary>
        /// <returns></returns>
        int GetVersion();

        /// <summary>
        /// データを検証して結果を格納する
        /// </summary>
        /// <returns></returns>
        void CreateVerifyData();

        /// <summary>
        /// 現在のデータが正常（実行できる状態）か返す
        /// </summary>
        /// <returns></returns>
        Define.Error VerifyData();

        /// <summary>
        /// データ検証の結果テキストを取得する
        /// </summary>
        /// <returns></returns>
        string GetInformation();
    }
}
