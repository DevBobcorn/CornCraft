// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp

namespace MagicaCloth2
{

    public enum ClothForceMode
    {
        None,

        /// <summary>
        /// 速度に加算（深さの影響を受ける）
        /// Add to velocity (affected by depth).
        /// </summary>
        VelocityAdd,

        /// <summary>
        /// 速度を変更（深さの影響を受ける）
        /// Change velocity (affected by depth).
        /// </summary>
        VelocityChange,

        /// <summary>
        /// 速度に加算（深さ無視）
        /// Add to velocity (ignoring depth).
        /// </summary>
        VelocityAddWithoutDepth = 10,

        /// <summary>
        /// 速度を変更（深さ無視）
        /// Change velocity (ignoring depth).
        /// </summary>
        VelocityChangeWithoutDepth,
    }
}
