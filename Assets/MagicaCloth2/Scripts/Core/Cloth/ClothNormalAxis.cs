// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Axis to use as normal.
    /// 法線として利用する軸
    /// </summary>
    public enum ClothNormalAxis
    {
        [InspectorName("Right (red)")]
        Right = 0,
        [InspectorName("Up (green)")]
        Up = 1,
        [InspectorName("Forward (blue)")]
        Forward = 2,
        [InspectorName("Inverse Right (red)")]
        InverseRight = 3,
        [InspectorName("Inverse Up (green)")]
        InverseUp = 4,
        [InspectorName("Inverse Forward (blue)")]
        InverseForward = 5,
    }
}
