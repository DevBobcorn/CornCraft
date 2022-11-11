// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 形状風
    /// </summary>
    //[HelpURL("https://magicasoft.jp/directional-wind/")]
    [AddComponentMenu("MagicaCloth/MagicaAreaWind")]
    public partial class MagicaAreaWind : WindComponent
    {
        [SerializeField]
        private PhysicsManagerWindData.ShapeType shapeType = PhysicsManagerWindData.ShapeType.Box;

        [SerializeField]
        private bool isAddition = false;

        //=========================================================================================
        public override ComponentType GetComponentType()
        {
            return ComponentType.AreaWind;
        }

        /// <summary>
        /// 風タイプを返す
        /// </summary>
        /// <returns></returns>
        public override PhysicsManagerWindData.WindType GetWindType()
        {
            return PhysicsManagerWindData.WindType.Area;
        }

        /// <summary>
        /// 形状タイプを返す
        /// </summary>
        /// <returns></returns>
        public override PhysicsManagerWindData.ShapeType GetShapeType()
        {
            return shapeType;
        }

        /// <summary>
        /// 風向きタイプを返す
        /// </summary>
        /// <returns></returns>
        public override PhysicsManagerWindData.DirectionType GetDirectionType()
        {
            if (shapeType == PhysicsManagerWindData.ShapeType.Box)
                return PhysicsManagerWindData.DirectionType.OneDirection;
            else
                return directionType;
        }

        /// <summary>
        /// 風が加算モードか返す
        /// </summary>
        /// <returns></returns>
        public override bool IsAddition()
        {
            return isAddition;
        }

        /// <summary>
        /// エリアサイズを返す
        /// </summary>
        /// <returns></returns>
        public override Vector3 GetAreaSize()
        {
            if (shapeType == PhysicsManagerWindData.ShapeType.Box)
                return areaSize;
            else if (shapeType == PhysicsManagerWindData.ShapeType.Sphere)
                return new Vector3(areaRadius, areaRadius, areaRadius);

            Debug.LogError("Invalid wind shape type!");
            return Vector3.zero;
        }

        /// <summary>
        /// アンカー位置を返す
        /// </summary>
        /// <returns></returns>
        //public override Vector3 GetAnchor()
        //{
        //    return anchor;
        //}

        /// <summary>
        /// 風エリアの体積を返す
        /// </summary>
        /// <returns></returns>
        public override float GetAreaVolume()
        {
            if (shapeType == PhysicsManagerWindData.ShapeType.Box)
                return (areaSize.x * 2) * (areaSize.y * 2) * (areaSize.z * 2);
            else if (shapeType == PhysicsManagerWindData.ShapeType.Sphere)
                return (4.0f / 3.0f) * areaRadius * areaRadius * areaRadius * Mathf.PI;

            Debug.LogError("Invalid wind volume!");
            return 0;
        }

        /// <summary>
        /// 風エリアの最大距離を返す
        /// </summary>
        /// <returns></returns>
        public override float GetAreaLength()
        {
            // 基本はエリアサイズの対角線距離、アンカーが離れていればその分水増しする
            var size = GetAreaSize();
            var areaLength = shapeType == PhysicsManagerWindData.ShapeType.Sphere ? size.x : size.magnitude;
            //var anchorRatio = Mathf.Clamp01(anchor.magnitude);
            //return areaLength + areaLength * anchorRatio;

            return areaLength;
        }

        //=========================================================================================
        /// <summary>
        /// パラメータ初期化
        /// </summary>
        protected override void ResetParams()
        {
            main = 5;
            turbulence = 1;
            frequency = 1;
            areaSize = new Vector3(5, 5, 5);
            //anchor = Vector3.zero;
            directionAngleX = 0;
            directionAngleY = 0;
            directionType = PhysicsManagerWindData.DirectionType.OneDirection;
            attenuation.SetParam(1, 1, false, 0, false);
        }
    }
}
