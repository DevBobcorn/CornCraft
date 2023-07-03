// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Text;

namespace MagicaCloth2
{
    public interface IManager : IDisposable
    {
        /// <summary>
        /// 初期化
        /// 実行状態に入ったときに呼ばれる
        /// </summary>
        void Initialize();

        /// <summary>
        /// ゲームプレイの実行が停止したときに呼ばれる（エディタ環境のみ）
        /// </summary>
        void EnterdEditMode();

        /// <summary>
        /// 情報ログ収集
        /// </summary>
        /// <param name="allsb"></param>
        void InformationLog(StringBuilder allsb);
    }
}
