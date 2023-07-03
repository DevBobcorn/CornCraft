// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp

using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Planeコライダーコンポーネント
    /// Y軸方向に対する無限平面
    /// Plane Collider.
    /// Infinite plane for the Y-axis direction.
    /// </summary>
    [AddComponentMenu("MagicaCloth2/MagicaPlaneCollider")]
    [HelpURL("https://magicasoft.jp/en/mc2_planecollidercomponent/")]
    public class MagicaPlaneCollider : ColliderComponent
    {
        public override ColliderManager.ColliderType GetColliderType()
        {
            return ColliderManager.ColliderType.Plane;
        }

        public override void DataValidate()
        {
        }
    }
}
