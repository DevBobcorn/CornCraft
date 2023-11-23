// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp

using System;
using Unity.Jobs;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// スプリング制約
    /// 固定パーティクルにスプリングの機能を追加する
    /// 現在はBoneSpringのみで利用
    /// </summary>
    public class SpringConstraint : IDisposable
    {
        [System.Serializable]
        public class SerializeData : IDataValidate
        {
            /// <summary>
            /// Use of springs
            /// スプリングの利用
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public bool useSpring;

            /// <summary>
            /// spring strength.(0.0 ~ 1.0)
            /// スプリングの強さ(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.001f, 0.2f)]
            public float springPower;

            /// <summary>
            /// Distance that can be moved from the origin.
            /// 原点から移動可能な距離
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 0.5f)]
            public float limitDistance;

            /// <summary>
            /// Movement restriction in normal direction.(0.0 ~ 1.0)
            /// 法線方向に対しての移動制限(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float normalLimitRatio;

            /// <summary>
            /// De-synchronize each spring.(0.0 ~ 1.0)
            /// 各スプリングの非同期化(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float springNoise;

            public SerializeData()
            {
                useSpring = true;
                springPower = 0.04f;
                limitDistance = 0.1f;
                normalLimitRatio = 1.0f;
                springNoise = 0.0f;
            }

            public void DataValidate()
            {
                springPower = Math.Clamp(springPower, 0.001f, 1.0f);
                limitDistance = Mathf.Max(limitDistance, 0);
                normalLimitRatio = Mathf.Clamp01(normalLimitRatio);
                springNoise = Mathf.Clamp01(springNoise);
            }

            public SerializeData Clone()
            {
                return new SerializeData()
                {
                    useSpring = useSpring,
                    springPower = springPower,
                    limitDistance = limitDistance,
                    normalLimitRatio = normalLimitRatio,
                    springNoise = springNoise,
                };
            }
        }

        public struct SpringConstraintParams
        {
            /// <summary>
            /// スプリングの強さ(0.0 ~ 1.0)
            /// スプリング未使用時は0.0
            /// </summary>
            public float springPower;

            /// <summary>
            /// 原点からの移動制限距離
            /// </summary>
            public float limitDistance;

            /// <summary>
            /// 法線方向に対する移動制限
            /// </summary>
            public float normalLimitRatio;

            /// <summary>
            /// 各スプリングの非同期率(0.0 ~ 1.0)
            /// </summary>
            public float springNoise;

            public void Convert(SerializeData sdata, ClothProcess.ClothType clothType)
            {
                springPower = clothType == ClothProcess.ClothType.BoneSpring && sdata.useSpring ? sdata.springPower : 0.0f;
                limitDistance = sdata.limitDistance;
                normalLimitRatio = sdata.normalLimitRatio;
                springNoise = sdata.springNoise;
            }
        }

        public void Dispose()
        {
        }

        //=========================================================================================
        unsafe internal JobHandle SolverConstraint(JobHandle jobHandle)
        {
            return jobHandle;
        }
    }
}
