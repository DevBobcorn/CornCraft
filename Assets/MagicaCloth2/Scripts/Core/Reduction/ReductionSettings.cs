// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Configuration data for reduction.
    /// リダクション用の設定データ
    /// </summary>
    [System.Serializable]
    public class ReductionSettings : IDataValidate
    {
        /// <summary>
        /// Simple distance reduction (% of AABB maximum distance) (0.0 ~ 1.0).
        /// 単純な距離による削減(AABB最大距離の%)(0.0 ~ 1.0)
        /// [NG] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        [Range(0.0f, 0.2f)]
        public float simpleDistance = 0.0f;

        /// <summary>
        /// Reduction by distance considering geometry (% of AABB maximum distance) (0.0 ~ 1.0).
        /// 形状を考慮した距離による削減(AABB最大距離の%)(0.0 ~ 1.0)
        /// [NG] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        [Range(0.0f, 0.2f)]
        public float shapeDistance = 0.0f;

        //=========================================================================================
        public bool IsEnabled => Define.System.ReductionEnable;

        public float GetMaxConnectionDistance()
        {
            return math.max(math.max(Define.System.ReductionSameDistance, simpleDistance), shapeDistance);
        }

        public ReductionSettings Clone()
        {
            return new ReductionSettings()
            {
                simpleDistance = simpleDistance,
                shapeDistance = shapeDistance,
            };
        }

        public void DataValidate()
        {
            simpleDistance = Mathf.Clamp(simpleDistance, 0.0f, 0.2f);
            shapeDistance = Mathf.Clamp(shapeDistance, 0.0f, 0.2f);
        }

        /// <summary>
        /// エディタメッシュの更新を判定するためのハッシュコード
        /// （このハッシュは実行時には利用されない編集用のもの）
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = 0;
            hash += simpleDistance.GetHashCode();
            hash += shapeDistance.GetHashCode();

            return hash;
        }

        public override string ToString()
        {
            return $"ReductionSettings. sameDist:{Define.System.ReductionSameDistance}, simpleDist:{simpleDistance}, shapeDist:{shapeDistance} maxStep:{Define.System.ReductionMaxStep}";
        }

    }
}
