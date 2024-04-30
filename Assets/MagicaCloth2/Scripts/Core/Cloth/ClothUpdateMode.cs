// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp

namespace MagicaCloth2
{
    /// <summary>
    /// Simulation Update Mode
    /// </summary>
    public enum ClothUpdateMode
    {
        /// <summary>
        /// This mode assumes that normal Update() will perform the move and animation.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// This mode assumes that FixedUpdate() is used to perform movement and animation.
        /// </summary>
        UnityPhysics = 1,

        /// <summary>
        /// Updates are independent of Unity's Time.timeScale.
        /// </summary>
        Unscaled = 2,

        /// <summary>
        /// Automatically set from linked animator.
        /// 連動アニメーターから自動設定する
        /// - Animator.UpdateMode.Normal -> Normal
        /// - Animator.UpdateMode.AnimatePhysics -> UnityPhysics
        /// - Animator.UpdateMode.UnscaledTime -> Unscaled
        /// </summary>
        AnimatorLinkage = 10,
    }
}
