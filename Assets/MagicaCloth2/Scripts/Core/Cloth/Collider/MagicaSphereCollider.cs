// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Sphere collider
    /// </summary>
    [AddComponentMenu("MagicaCloth2/MagicaSphereCollider")]
    [HelpURL("https://magicasoft.jp/en/mc2_spherecollidercomponent/")]
    public class MagicaSphereCollider : ColliderComponent
    {
        public override ColliderManager.ColliderType GetColliderType()
        {
            return ColliderManager.ColliderType.Sphere;
        }

        public override void DataValidate()
        {
            size.x = Mathf.Max(size.x, 0.001f);
        }

        /// <summary>
        /// resize the sphere.
        /// </summary>
        /// <param name="radius"></param>
        public void SetSize(float radius)
        {
            SetSize(new Vector3(radius, 0.0f, 0.0f));
        }
    }
}
