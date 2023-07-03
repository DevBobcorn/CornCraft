// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Capsuleコライダーコンポーネント
    /// </summary>
    [AddComponentMenu("MagicaCloth2/MagicaCapsuleCollider")]
    [HelpURL("https://magicasoft.jp/en/mc2_capsulecollidercomponent/")]
    public class MagicaCapsuleCollider : ColliderComponent
    {
        public enum Direction
        {
            [InspectorName("X-Axis")]
            X = 0,

            [InspectorName("Y-Axis")]
            Y = 1,

            [InspectorName("Z-Axis")]
            Z = 2,
        }

        /// <summary>
        /// Reference transform axis.
        /// </summary>
        public Direction direction = Direction.X;

        /// <summary>
        /// 半径をStart/End別々に設定
        /// Set radius separately for Start/End.
        /// </summary>
        public bool radiusSeparation = false;


        public override ColliderManager.ColliderType GetColliderType()
        {
            if (direction == Direction.X)
                return ColliderManager.ColliderType.CapsuleX;
            else if (direction == Direction.Y)
                return ColliderManager.ColliderType.CapsuleY;
            else
                return ColliderManager.ColliderType.CapsuleZ;
        }

        /// <summary>
        /// set size.
        /// </summary>
        /// <param name="startRadius"></param>
        /// <param name="endRadius"></param>
        /// <param name="length"></param>
        public void SetSize(float startRadius, float endRadius, float length)
        {
            SetSize(new Vector3(startRadius, endRadius, length));
            radiusSeparation = startRadius != endRadius;
        }

        /// <summary>
        /// get size.
        /// (x:start radius, y:end radius, z:length)
        /// </summary>
        /// <returns></returns>
        public override Vector3 GetSize()
        {
            if (radiusSeparation)
                return size;
            else
            {
                // (始点半径, 終点半径, 長さ)
                return new Vector3(size.x, size.x, size.z);
            }
        }

        /// <summary>
        /// カプセルのローカル方向を返す
        /// </summary>
        /// <returns></returns>
        public Vector3 GetLocalDir()
        {
            if (direction == Direction.X)
                return Vector3.right;
            else if (direction == Direction.Y)
                return Vector3.up;
            else
                return Vector3.forward;
        }

        /// <summary>
        /// カプセルのローカル上方向を返す
        /// </summary>
        /// <returns></returns>
        public Vector3 GetLocalUp()
        {
            if (direction == Direction.X)
                return Vector3.up;
            else if (direction == Direction.Y)
                return Vector3.forward;
            else
                return Vector3.up;
        }

        public override void DataValidate()
        {
            size.x = Mathf.Max(size.x, 0.001f);
            size.y = Mathf.Max(size.y, 0.001f);
            size.z = Mathf.Max(size.z, 0.001f);
        }
    }
}
