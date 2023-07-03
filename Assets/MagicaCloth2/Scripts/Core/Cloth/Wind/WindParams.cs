// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp

namespace MagicaCloth2
{
    /// <summary>
    /// 風の調整用パラメータ
    /// </summary>
    public struct WindParams : IValid
    {
        public float influence;
        public float frequency;
        public float turbulence;
        public float blend;
        public float synchronization;
        public float depthWeight;
        public float movingWind;

        public void Convert(WindSettings sdata)
        {
            influence = sdata.influence;
            frequency = sdata.frequency;
            turbulence = sdata.turbulence;
            blend = sdata.blend;
            synchronization = sdata.synchronization;
            depthWeight = sdata.depthWeight;
            movingWind = sdata.movingWind;
        }

        public bool IsValid()
        {
            return influence > Define.System.Epsilon;
        }
    }
}
