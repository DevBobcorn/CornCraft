// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;

namespace MagicaCloth2
{
    /// <summary>
    /// 処理例外
    /// これは予期されたエラーにより処理を中断する場合に用いる
    /// </summary>
    [Serializable]
    public class MagicaClothProcessingException : Exception
    {
        public MagicaClothProcessingException() : base() { }
        public MagicaClothProcessingException(string message) : base(message) { }
        public MagicaClothProcessingException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// キャンセル例外
    /// これはエラー無しに処理を打ち切る場合に用いる
    /// </summary>
    [Serializable]
    public class MagicaClothCanceledException : Exception
    {
        public MagicaClothCanceledException() : base() { }
        public MagicaClothCanceledException(string message) : base(message) { }
        public MagicaClothCanceledException(string message, Exception inner) : base(message, inner) { }
    }
}
