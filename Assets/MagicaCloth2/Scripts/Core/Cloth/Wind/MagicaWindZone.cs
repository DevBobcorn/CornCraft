// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Wind zone 
    /// </summary>
    [AddComponentMenu("MagicaCloth2/MagicaWindZone")]
    [HelpURL("https://magicasoft.jp/en/mc2_windzone_component/")]
    public class MagicaWindZone : ClothBehaviour
    {
        public enum Mode
        {
            /// <summary>
            /// 領域を持たない全体に影響する風
            /// Wind that affects the whole without area.
            /// </summary>
            GlobalDirection = 0,

            /// <summary>
            /// 球型の領域を持つ方向風
            /// Directional wind with spherical area.
            /// </summary>
            SphereDirection = 1,

            /// <summary>
            /// ボックス型の領域を持つ方向風
            /// </summary>
            BoxDirection = 2,

            /// <summary>
            /// 球型の領域を持つ放射風
            /// Directional wind with box area.
            /// </summary>
            SphereRadial = 10,
        }

        /// <summary>
        /// Zone mode.
        /// [OK] Runtime changes.
        /// </summary>
        public Mode mode = Mode.GlobalDirection;

        /// <summary>
        /// Box size.
        /// [OK] Runtime changes.
        /// </summary>
        public Vector3 size = new Vector3(10.0f, 10.0f, 10.0f);

        /// <summary>
        /// Sphere size.
        /// [OK] Runtime changes.
        /// </summary>
        public float radius = 10.0f;

        /// <summary>
        /// メイン風力
        /// main wind (m/s).
        /// [OK] Runtime changes.
        /// </summary>
        [Range(0, 30)]
        public float main = 5.0f;

        /// <summary>
        /// 乱流率
        /// turbulence rate.
        /// [OK] Runtime changes.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float turbulence = 1.0f;

        /// <summary>
        /// 風の方向X角度（ローカル角度）
        /// wind direction x angle (local angle).
        /// [OK] Runtime changes.
        /// </summary>
        [Range(-180, 180)]
        public float directionAngleX = 0;

        /// <summary>
        /// 風の方向Y角度（ローカル角度）
        /// wind direction y angle (local angle).
        /// [OK] Runtime changes.
        /// </summary>
        [Range(-180, 180)]
        public float directionAngleY = 0;

        /// <summary>
        /// 放射風の減衰
        /// Radiation wind attenuation.
        /// [OK] Runtime changes.
        /// </summary>
        public AnimationCurve attenuation = AnimationCurve.EaseInOut(0.0f, 1.0f, 1.0f, 0.0f);

        /// <summary>
        /// 他の風の影響を無効にせずに追加するフラグ
        /// Flags to add without disabling other wind effects.
        /// [OK] Runtime changes.
        /// </summary>
        public bool isAddition = false;

        //=========================================================================================
        /// <summary>
        /// 風マネージャデータへの参照インデックス(-1=無効)
        /// Reference index to wind manager data (-1=disabled).
        /// </summary>
        public int WindId { get; private set; } = -1;


        //=========================================================================================
        public void Awake()
        {
            WindId = MagicaManager.Wind.AddWind(this);
        }

        public void Start()
        {
        }

        public void OnEnable()
        {
            MagicaManager.Wind.SetEnable(WindId, true);
        }

        public void OnDisable()
        {
            MagicaManager.Wind.SetEnable(WindId, false);
        }

        public void OnDestroy()
        {
            MagicaManager.Wind.RemoveWind(WindId);
            WindId = -1;
        }

        //=========================================================================================
        /// <summary>
        /// 方向風か判定する
        /// whether the zone has directional winds.
        /// </summary>
        /// <returns></returns>
        public bool IsDirection()
        {
            switch (mode)
            {
                case Mode.GlobalDirection:
                case Mode.SphereDirection:
                case Mode.BoxDirection:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 放射風か判定する
        /// whether the zone has radiant winds.
        /// </summary>
        /// <returns></returns>
        public bool IsRadial()
        {
            switch (mode)
            {
                case Mode.SphereRadial:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 追加される風か判定する
        /// Whether it is a wind of additional system.
        /// </summary>
        /// <returns></returns>
        public bool IsAddition() => isAddition;

        /// <summary>
        /// 方向性風の風向きを取得する
        /// Get the direction of the directional wind.
        /// </summary>
        /// <param name="localSpace"></param>
        /// <returns></returns>
        public Vector3 GetWindDirection(bool localSpace = false)
        {
            var lq = Quaternion.Euler(directionAngleX, directionAngleY, 0.0f);
            var ldir = lq * Vector3.forward;
            return localSpace ? ldir : transform.TransformDirection(ldir);
        }

        /// <summary>
        /// 方向性風の風向きを設定する
        /// Set the wind direction for directional wind.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="localSpace"></param>
        public void SetWindDirection(Vector3 dir, bool localSpace = false)
        {
            Vector3 lv = localSpace ? dir : transform.InverseTransformDirection(dir);
            directionAngleX = Mathf.Atan2(lv.z, lv.x) * Mathf.Rad2Deg;
            directionAngleY = Mathf.Atan2(lv.z, lv.y) * Mathf.Rad2Deg;
        }
    }
}
