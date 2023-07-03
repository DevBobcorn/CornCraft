// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// MagicaCloth manager API
    /// </summary>
    public static partial class MagicaManager
    {
        /// <summary>
        /// シミュレーション開始前イベント
        /// Pre-simulation event.
        /// </summary>
        public static Action OnPreSimulation;

        /// <summary>
        /// シミュレーション完了後イベント
        /// Post-simulation event.
        /// </summary>
        public static Action OnPostSimulation;


        /// <summary>
        /// グローバルタイムスケールを変更します
        /// Change the global time scale.
        /// </summary>
        /// <param name="timeScale">0.0-1.0</param>
        public static void SetGlobalTimeScale(float timeScale)
        {
            if (IsPlaying())
            {
                //Team.globalTimeScale = Mathf.Clamp01(timeScale);
                Time.GlobalTimeScale = Mathf.Clamp01(timeScale);
            }
        }

        /// <summary>
        /// グローバルタイムスケールを取得します
        /// Get the global time scale.
        /// </summary>
        /// <returns></returns>
        public static float GetGlobalTimeScale()
        {
            if (IsPlaying())
            {
                //return Team.globalTimeScale;
                return Time.GlobalTimeScale;
            }
            else
                return 1.0f;
        }

        /// <summary>
        /// シミュレーションの周波数を設定します(30~150, 初期値90)
        /// 周波数を上げると精度が高くなりますが負荷が上がります、下げるげると精度が低くなりますが負荷が下がります
        /// そのため60以下に下げる場合には精度問題に十分注意してください
        /// 
        /// Sets the simulation frequency (30~150, default 90).
        /// Increasing the frequency increases the accuracy but increases the load, and decreasing the frequency decreases the accuracy but reduces the load.
        /// Therefore, if you lower it below 60, be very careful about accuracy issues.
        /// </summary>
        /// <param name="freq"></param>
        public static void SetSimulationFrequency(int freq)
        {
            if (IsPlaying())
                Time.simulationFrequency = Mathf.Clamp(freq, Define.System.SimulationFrequency_Low, Define.System.SimulationFrequency_Hi);
        }

        /// <summary>
        /// 現在のシミュレーション周波数を取得します
        /// Get current simulation frequency.
        /// </summary>
        /// <returns></returns>
        public static int GetSimulationFrequency()
        {
            if (IsPlaying())
                return Time.simulationFrequency;
            else
                return 0;
        }

        /// <summary>
        /// １フレームで実行される最大のシミュレーション回数を設定します(1~5, 初期値3)
        /// シミュレーションはフレームレート(fps)とは非同期に実行されます
        /// そのためfpsが下がると１フレームに実行されるシミュレーション回数が増えて負荷が高くなります
        /// これはモバイル端末などで問題になる場合があります
        /// １フレームで実行されるシミュレーション回数を下げることで最大負荷を調整できます
        /// 制限によりシミュレーションがスキップされた場合は補間機能により動作が補われます
        /// 
        /// Set the maximum number of simulations to be executed in one frame (1 to 5, initial value 3)
        /// The simulation runs asynchronously with the frame rate(fps).
        /// Therefore, when the fps decreases, the number of simulations executed in one frame increases and the load increases.
        /// This can be a problem on mobile devices, for example.
        /// You can adjust the maximum load by lowering the number of simulations executed in one frame.
        /// If the simulation is skipped due to restrictions, the interpolation function compensates for the motion.
        /// </summary>
        /// <param name="count"></param>
        public static void SetMaxSimulationCountPerFrame(int count)
        {
            if (IsPlaying())
                Time.maxSimulationCountPerFrame = Mathf.Clamp(count, Define.System.MaxSimulationCountPerFrame_Low, Define.System.MaxSimulationCountPerFrame_Hi);
        }

        /// <summary>
        /// 現在のフレームごとの最大シミュレーション回数を取得します
        /// Gets the current maximum number of simulations per frame.
        /// </summary>
        /// <returns></returns>
        public static int GetMaxSimulationCountPerFrame()
        {
            if (IsPlaying())
                return Time.maxSimulationCountPerFrame;
            else
                return 0;
        }
    }
}
