// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Runtime.CompilerServices;

namespace MagicaCloth2
{
    /// <summary>
    /// 16ビットフラグ
    /// </summary>
    [System.Serializable]
    public struct ExBitFlag16
    {
        public ushort Value;

        public ExBitFlag16(ushort initialValue = 0)
        {
            Value = initialValue;
        }

        public void Clear()
        {
            Value = 0;
        }

        /// <summary>
        /// フラグ設定（ビット直指定）
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="sw"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFlag(ushort flag, bool sw)
        {
            if (sw)
                Value |= flag;
            else
                Value = (ushort)(Value & ~flag);
        }

        /// <summary>
        /// フラグ判定（ビット直指定）
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(ushort flag)
        {
            return (Value & flag) != 0;
        }
    }
}
