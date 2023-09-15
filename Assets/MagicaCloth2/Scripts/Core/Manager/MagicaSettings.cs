// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Magica manger settings component
    /// </summary>
    [AddComponentMenu("MagicaCloth2/MagicaSettings")]
    [HelpURL("https://magicasoft.jp/en/mc2_settings_component/")]
    public class MagicaSettings : ClothBehaviour
    {
        public enum RefreshMode
        {
            /// <summary>
            /// コンポーネント生成時に１度だけ送信する
            /// Send only once when the component is created.
            /// </summary>
            OnAwake = 0,

            /// <summary>
            /// 毎フレーム内容を送信する
            /// Send content every frame.
            /// </summary>
            EveryFrame = 1,
        }

        /// <summary>
        /// コンポーネントの内容をマネージャに送信する方法
        /// How to send the contents of a component to the manager.
        /// Refresh mode
        /// </summary>
        public RefreshMode refreshMode = RefreshMode.OnAwake;

        /// <summary>
        /// シミュレーション周波数(30~150, 初期値90)
        /// 周波数を上げると精度が高くなりますが負荷が上がります、下げるげると精度が低くなりますが負荷が下がります
        /// そのため60以下に下げる場合には精度問題に十分注意してください
        /// 
        /// Simulation frequency (30~150, default 90).
        /// Increasing the frequency increases the accuracy but increases the load, and decreasing the frequency decreases the accuracy but reduces the load.
        /// Therefore, if you lower it below 60, be very careful about accuracy issues.
        /// </summary>
        [Range(Define.System.SimulationFrequency_Low, Define.System.SimulationFrequency_Hi)]
        public int simulationFrequency = Define.System.DefaultSimulationFrequency;

        /// <summary>
        /// １フレームの最大シミュレーション回数(1~5, 初期値3)
        /// シミュレーションはフレームレート(fps)とは非同期に実行されます
        /// そのためfpsが下がると１フレームに実行されるシミュレーション回数が増えて負荷が高くなります
        /// これはモバイル端末などで問題になる場合があります
        /// １フレームで実行されるシミュレーション回数を下げることで最大負荷を調整できます
        /// 制限によりシミュレーションがスキップされた場合は補間機能により動作が補われます
        /// 
        /// Maximum number of simulations per frame (1 to 5, default value 3).
        /// The simulation runs asynchronously with the frame rate(fps).
        /// Therefore, when the fps decreases, the number of simulations executed in one frame increases and the load increases.
        /// This can be a problem on mobile devices, for example.
        /// You can adjust the maximum load by lowering the number of simulations executed in one frame.
        /// If the simulation is skipped due to restrictions, the interpolation function compensates for the motion.
        /// </summary>
        [Range(Define.System.MaxSimulationCountPerFrame_Low, Define.System.MaxSimulationCountPerFrame_Hi)]
        public int maxSimulationCountPerFrame = Define.System.DefaultMaxSimulationCountPerFrame;

        //=========================================================================================
        public void Awake()
        {
            if (refreshMode == RefreshMode.OnAwake)
                Refresh();
        }

        public void Update()
        {
            if (refreshMode == RefreshMode.EveryFrame)
                Refresh();
        }

        //=========================================================================================
        /// <summary>
        /// コンポーネントの内容をマネージャに送信します
        /// Sends the contents of the component to the manager.
        /// </summary>
        public void Refresh()
        {
            if (MagicaManager.IsPlaying())
            {
                simulationFrequency = Mathf.Clamp(simulationFrequency, Define.System.SimulationFrequency_Low, Define.System.SimulationFrequency_Hi);
                maxSimulationCountPerFrame = Mathf.Clamp(maxSimulationCountPerFrame, Define.System.MaxSimulationCountPerFrame_Low, Define.System.MaxSimulationCountPerFrame_Hi);

                MagicaManager.SetSimulationFrequency(simulationFrequency);
                MagicaManager.SetMaxSimulationCountPerFrame(maxSimulationCountPerFrame);
            }
        }
    }
}
