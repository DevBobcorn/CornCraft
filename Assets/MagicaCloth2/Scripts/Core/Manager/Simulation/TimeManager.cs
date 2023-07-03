// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Text;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public class TimeManager : IManager, IValid
    {
        /// <summary>
        /// シミュレーションの更新周期
        /// １ステップで(1.0 / simulationFrequency)時間進みます
        /// </summary>
        internal int simulationFrequency = Define.System.DefaultSimulationFrequency;

        /// <summary>
        /// 1フレームに実行される最大シミュレーション回数
        /// </summary>
        internal int maxSimulationCountPerFrame = Define.System.DefaultMaxSimulationCountPerFrame;

        //=========================================================================================
        bool isValid = false;

        /// <summary>
        /// フレームのFixedUpdate回数
        /// </summary>
        internal int FixedUpdateCount { get; private set; }

        /// <summary>
        /// グローバルタイムスケール(0.0 ~ 1.0)
        /// </summary>
        internal float GlobalTimeScale = 1.0f;

        /// <summary>
        /// シミュレーション1回の時間
        /// </summary>
        internal float SimulationDeltaTime { get; private set; }

        /// <summary>
        /// 1フレームの最大更新時間
        /// </summary>
        internal float MaxDeltaTime { get; private set; }

        /// <summary>
        /// 制約解決係数（周波数により変動）
        /// </summary>
        internal float4 SimulationPower { get; private set; }

        //=========================================================================================
        public void Dispose()
        {
            isValid = true;

            GlobalTimeScale = 1.0f;
            FixedUpdateCount = 0;
            SimulationPower = 1.0f;

            MagicaManager.afterFixedUpdateDelegate -= AfterFixedUpdate;
            MagicaManager.afterRenderingDelegate -= AfterRenderring;
        }

        public void EnterdEditMode()
        {
            Dispose();
        }

        public void Initialize()
        {
            GlobalTimeScale = 1.0f;
            FixedUpdateCount = 0;
            SimulationPower = 1.0f;

            MagicaManager.afterFixedUpdateDelegate += AfterFixedUpdate;
            MagicaManager.afterRenderingDelegate += AfterRenderring;

            isValid = true;
        }

        public bool IsValid()
        {
            return isValid;
        }

        //=========================================================================================
        void AfterFixedUpdate()
        {
            //Debug.Log($"AF. F:{Time.frameCount}");
            FixedUpdateCount++;
        }

        void AfterRenderring()
        {
            //Debug.Log($"AfterRenderring. F:{Time.frameCount}");
            FixedUpdateCount = 0;
        }

        //=========================================================================================
        internal void FrameUpdate()
        {
            simulationFrequency = Mathf.Clamp(simulationFrequency, Define.System.SimulationFrequency_Low, Define.System.SimulationFrequency_Hi);
            maxSimulationCountPerFrame = Mathf.Clamp(maxSimulationCountPerFrame, Define.System.MaxSimulationCountPerFrame_Low, Define.System.MaxSimulationCountPerFrame_Hi);
            GlobalTimeScale = Mathf.Clamp01(GlobalTimeScale);

            // 1ステップのシミュレーション更新時間
            SimulationDeltaTime = 1.0f / simulationFrequency;

            // 1フレームの最大更新時間
            MaxDeltaTime = SimulationDeltaTime * maxSimulationCountPerFrame;

            // 制約解決係数
            float t = Define.System.DefaultSimulationFrequency / (float)simulationFrequency;
            SimulationPower = new float4(
                t, // (3.0 ~ 1.0 ~ 0.6)
                t > 1.0f ? Mathf.Pow(t, 0.5f) : t, // (1.73 ~ 1.0 ~ 0.6)
                t > 1.0f ? Mathf.Pow(t, 0.3f) : t, // (1.39 ~ 1.0 ~ 0.6)
                                                   //Mathf.Pow(t, 1.5f) // (5.19 ~ 1.0 ~ 0.46)
                Mathf.Pow(t, 1.8f) // (7.22 ~ 1.0 ~ 0.39)
                );
            //Debug.Log($"SimulationPower:{SimulationPower}");
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"========== Time Manager ==========");
            if (IsValid() == false)
            {
                sb.AppendLine($"Time Manager. Invalid");
            }
            else
            {
                sb.AppendLine($"SimulationFrequency:{simulationFrequency}");
                sb.AppendLine($"MaxSimulationCountPerFrame:{maxSimulationCountPerFrame}");
                sb.AppendLine($"GlobalTimeScale:{GlobalTimeScale}");
                sb.AppendLine($"SimulationDeltaTime:{SimulationDeltaTime}");
                sb.AppendLine($"MaxDeltaTime:{MaxDeltaTime}");
                sb.AppendLine($"SimulationPower:{SimulationPower}");
            }
            sb.AppendLine();

            Debug.Log(sb.ToString());
            allsb.Append(sb);
        }
    }
}
