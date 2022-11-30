// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// MagicaPhysicsManager API
    /// </summary>
    public partial class MagicaPhysicsManager : CreateSingleton<MagicaPhysicsManager>
    {
        /// <summary>
        /// １秒あたりの更新回数
        /// Updates per second.
        /// </summary>
        public UpdateTimeManager.UpdateCount UpdatePerSeccond
        {
            get
            {
                return (UpdateTimeManager.UpdateCount)UpdateTime.UpdatePerSecond;
            }
            set
            {
                UpdateTime.SetUpdatePerSecond(value);
            }
        }

        /// <summary>
        /// 更新モード
        /// Update mode.
        /// </summary>
        public UpdateTimeManager.UpdateMode UpdateMode
        {
            get
            {
                return UpdateTime.GetUpdateMode();
            }
            set
            {
                UpdateTime.SetUpdateMode(value);
            }
        }


        /// <summary>
        /// グローバルタイムスケールを設定する
        /// Set the global time scale.
        /// </summary>
        /// <param name="timeScale">0.0-1.0</param>
        public void SetGlobalTimeScale(float timeScale)
        {
            UpdateTime.TimeScale = Mathf.Clamp01(timeScale);
        }

        /// <summary>
        /// グローバルタイムスケールを取得する
        /// Get global time scale.
        /// </summary>
        /// <returns></returns>
        public float GetGlobalTimeScale()
        {
            return UpdateTime.TimeScale;
        }

        /// <summary>
        /// 遅延実行時の未来予測率(0.0-1.0)
        /// Future prediction rate at the time of delayed execution (0.0-1.0).
        /// </summary>
        public float FuturePredictionRate
        {
            get
            {
                return UpdateTime.FuturePredictionRate;
            }
            set
            {
                UpdateTime.FuturePredictionRate = value;
            }
        }
    }
}
