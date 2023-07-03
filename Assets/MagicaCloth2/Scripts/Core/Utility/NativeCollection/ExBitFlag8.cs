// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Runtime.CompilerServices;

namespace MagicaCloth2
{
    /// <summary>
    /// ８ビットフラグ
    /// </summary>
    [System.Serializable]
    public struct ExBitFlag8
    {
        public byte Value;

        public ExBitFlag8(byte initialValue = 0)
        {
            Value = initialValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Value = 0;
        }

        /// <summary>
        /// フラグ設定
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="sw"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFlag(byte flag, bool sw)
        {
            if (sw)
                Value |= flag;
            else
                Value = (byte)(Value & ~flag);
        }

        /// <summary>
        /// フラグ判定
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(byte flag)
        {
            return (Value & flag) != 0;
        }
    }
}
