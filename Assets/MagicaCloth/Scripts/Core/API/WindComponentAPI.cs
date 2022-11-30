// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// WindComponent API
    /// </summary>
    public abstract partial class WindComponent : BaseComponent
    {
        /// <summary>
        /// 風量
        /// Air flow.
        /// </summary>
        public float Main
        {
            get => main;
            set
            {
                main = Mathf.Clamp(value, 0.0f, Define.Compute.MaxWindMain);
                status.SetDirty();
            }
        }

        /// <summary>
        /// 乱流率
        /// Turbulence rate.(0.0 - 1.0)
        /// </summary>
        public float Turbulence
        {
            get => turbulence;
            set
            {
                turbulence = Mathf.Clamp01(value);
                status.SetDirty();
            }
        }

        /// <summary>
        /// 周波数
        /// Frequency rate.(0.0 - 1.0)
        /// </summary>
        public float Frequency
        {
            get => frequency;
            set
            {
                frequency = Mathf.Clamp01(value);
                status.SetDirty();
            }
        }

        /// <summary>
        /// 基準となる風向き（ワールド）
        /// Wind world direction.
        /// </summary>
        public Vector3 MainDirection
        {
            get => transform.TransformDirection(GetLocalDirection());
            set
            {
                var lv = transform.InverseTransformDirection(value);
                directionAngleX = Mathf.Atan2(lv.z, lv.x) * Mathf.Rad2Deg;
                directionAngleY = Mathf.Atan2(lv.z, lv.y) * Mathf.Rad2Deg;
                status.SetDirty();
            }
        }

        /// <summary>
        /// 風向きのX軸角度(Degree)
        /// Wind direction X-axis angle (Degree)
        /// </summary>
        public float DirectionAngleX
        {
            get => directionAngleX;
            set
            {
                directionAngleX = value;
                status.SetDirty();
            }
        }

        /// <summary>
        /// 風向きのY軸角度(Degree)
        /// Wind direction Y-axis angle (Degree).
        /// </summary>
        public float DirectionAngleY
        {
            get => directionAngleY;
            set
            {
                directionAngleY = value;
                status.SetDirty();
            }
        }

        /// <summary>
        /// 風エリアのサイズ
        /// Wind area size.
        /// </summary>
        public Vector3 AreaSize
        {
            get => areaSize;
            set
            {
                areaSize = math.max(value, 0.1f);
                status.SetDirty();
            }
        }

        /// <summary>
        /// 風エリアの半径
        /// Wind area radius.
        /// </summary>
        public float AreaRadius
        {
            get => areaRadius;
            set
            {
                areaRadius = math.max(value, 0.1f);
                status.SetDirty();
            }
        }

        /// <summary>
        /// 風の方向性
        /// Wind direction type.
        /// </summary>
        public PhysicsManagerWindData.DirectionType DirectionType
        {
            get => directionType;
            set
            {
                directionType = value;
                status.SetDirty();
            }
        }

        /// <summary>
        /// 球形エリアの減衰率
        /// Damping factor of spherical area.
        /// </summary>
        /// <param name="sval">Influence rate of start point.(0.0 - 1.0)</param>
        /// <param name="eval">Influence rate of end point.(0.0 - 1.0)</param>
        /// <param name="useEval">Validity of endpoint value</param>
        /// <param name="cval">Strength of the curve.(-1.0 - +1.0)</param>
        /// <param name="useCval">Validity of curve value</param>
        public void SetAttenuation(float sval, float eval, bool useEval = true, float cval = 0.0f, bool useCval = false)
        {
            attenuation.SetParam(sval, eval, useEval, cval, useCval);
            status.SetDirty();
        }
    }
}
