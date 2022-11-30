// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 方向性の風（これはワールド全体に影響を与える）
    /// </summary>
    [HelpURL("https://magicasoft.jp/directional-wind/")]
    [AddComponentMenu("MagicaCloth/MagicaDirectionalWind")]
    public partial class MagicaDirectionalWind : WindComponent
    {
        public override ComponentType GetComponentType()
        {
            return ComponentType.DirectionalWind;
        }

        /// <summary>
        /// 風タイプを返す
        /// </summary>
        /// <returns></returns>
        public override PhysicsManagerWindData.WindType GetWindType()
        {
            return PhysicsManagerWindData.WindType.Direction;
        }

        /// <summary>
        /// 形状タイプを返す
        /// </summary>
        /// <returns></returns>
        public override PhysicsManagerWindData.ShapeType GetShapeType()
        {
            return PhysicsManagerWindData.ShapeType.Box;
        }

        /// <summary>
        /// 風向きタイプを返す
        /// </summary>
        /// <returns></returns>
        public override PhysicsManagerWindData.DirectionType GetDirectionType()
        {
            return PhysicsManagerWindData.DirectionType.OneDirection;
        }

        /// <summary>
        /// 風が加算モードか返す
        /// </summary>
        /// <returns></returns>
        public override bool IsAddition()
        {
            return false;
        }

        /// <summary>
        /// エリアサイズを返す
        /// </summary>
        /// <returns></returns>
        public override Vector3 GetAreaSize()
        {
            return new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        }

        /// <summary>
        /// アンカー位置を返す
        /// </summary>
        /// <returns></returns>
        //public override Vector3 GetAnchor()
        //{
        //    return Vector3.zero;
        //}

        /// <summary>
        /// 風エリアの体積を返す
        /// </summary>
        /// <returns></returns>
        public override float GetAreaVolume()
        {
            return 100000000;
        }

        /// <summary>
        /// 風エリアの最大距離を返す
        /// </summary>
        /// <returns></returns>
        public override float GetAreaLength()
        {
            return float.MaxValue;
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
            areaSize = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            //anchor = Vector3.zero;
            directionAngleX = 0;
            directionAngleY = 0;
            directionType = PhysicsManagerWindData.DirectionType.OneDirection;
            attenuation.SetParam(1, 1, false, 0, false);
        }
    }
}
