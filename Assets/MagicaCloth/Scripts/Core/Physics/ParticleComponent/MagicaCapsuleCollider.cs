// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// カプセルコライダー
    /// </summary>
    [HelpURL("https://magicasoft.jp/magica-cloth-capsule-collider/")]
    [AddComponentMenu("MagicaCloth/MagicaCapsuleCollider")]
    public class MagicaCapsuleCollider : ColliderComponent
    {
        // 軸
        public enum Axis
        {
            X,
            Y,
            Z,
        }

        [SerializeField]
        private Axis axis = Axis.X;

        [SerializeField]
        [Range(0, 1)]
        private float length = 0.2f;

        [SerializeField]
        [Range(0.0f, 0.5f)]
        private float startRadius = 0.1f;

        [SerializeField]
        [Range(0.0f, 0.5f)]
        private float endRadius = 0.1f;


        //=========================================================================================
        public override ComponentType GetComponentType()
        {
            return ComponentType.CapsuleCollider;
        }


        private void OnValidate()
        {
            if (Application.isPlaying)
                DataUpdate();
        }

        /// <summary>
        /// パーティクルのデータ更新処理
        /// </summary>
        internal override void DataUpdate()
        {
            base.DataUpdate();

            foreach (var c in particleDict.Values)
            {
                for (int i = 0; i < c.dataLength; i++)
                {
                    int pindex = c.startIndex + i;

                    // カプセルデータ
                    float3 radius = new float3(length, startRadius, endRadius);
                    MagicaPhysicsManager.Instance.Particle.SetRadius(pindex, radius);

                    // localPos
                    MagicaPhysicsManager.Instance.Particle.SetLocalPos(pindex, Center);

                    // カプセルフラグ再設定
                    var flag = MagicaPhysicsManager.Instance.Particle.flagList[pindex];
                    flag.SetFlag(PhysicsManagerParticleData.Flag_CapsuleX, false);
                    flag.SetFlag(PhysicsManagerParticleData.Flag_CapsuleY, false);
                    flag.SetFlag(PhysicsManagerParticleData.Flag_CapsuleZ, false);
                    flag.SetFlag(GetCapsuleFlag(), true);
                    MagicaPhysicsManager.Instance.Particle.flagList[pindex] = flag;
                }
            }
        }

        /// <summary>
        /// データハッシュ計算
        /// </summary>
        /// <returns></returns>
        public override int GetDataHash()
        {
            int hash = base.GetDataHash();
            hash += axis.GetDataHash();
            hash += length.GetDataHash();
            hash += startRadius.GetDataHash();
            hash += endRadius.GetDataHash();
            return hash;
        }

        //=========================================================================================
        public Axis AxisMode
        {
            get
            {
                return axis;
            }
            set
            {
                axis = value;
                ReserveDataUpdate();
            }
        }

        public float Length
        {
            get
            {
                return length;
            }
            set
            {
                length = value;
                ReserveDataUpdate();
            }
        }

        public float StartRadius
        {
            get
            {
                return startRadius;
            }
            set
            {
                startRadius = value;
                ReserveDataUpdate();
            }
        }

        public float EndRadius
        {
            get
            {
                return endRadius;
            }
            set
            {
                endRadius = value;
                ReserveDataUpdate();
            }
        }

        protected override ChunkData CreateColliderParticleReal(int teamId)
        {
            uint flag = 0;
            flag |= PhysicsManagerParticleData.Flag_Kinematic;
            flag |= PhysicsManagerParticleData.Flag_Collider;
            flag |= GetCapsuleFlag();
            flag |= PhysicsManagerParticleData.Flag_Transform_Read_Base;
            flag |= PhysicsManagerParticleData.Flag_Step_Update;
            flag |= PhysicsManagerParticleData.Flag_Reset_Position;
            flag |= PhysicsManagerParticleData.Flag_Transform_Read_Local;
            //flag |= PhysicsManagerParticleData.Flag_Transform_Read_Scl; // 現在スケールは見ていない

            // radiusにカプセルのデータを入れる
            float3 radius = new float3(length, startRadius, endRadius);

            var c = CreateParticle(
                flag,
                teamId, // team
                0.0f, // depth
                radius,
                Center
                );

            if (c.IsValid())
                MagicaPhysicsManager.Instance.Team.AddColliderParticle(teamId, c.startIndex);

            return c;
        }

        /// <summary>
        /// 設定軸に対応するカプセルフラグを返す
        /// </summary>
        /// <returns></returns>
        uint GetCapsuleFlag()
        {
            if (axis == Axis.X)
                return PhysicsManagerParticleData.Flag_CapsuleX;
            else if (axis == Axis.Y)
                return PhysicsManagerParticleData.Flag_CapsuleY;
            else
                return PhysicsManagerParticleData.Flag_CapsuleZ;
        }

        /// <summary>
        /// カプセルのローカル方向を返す
        /// </summary>
        /// <returns></returns>
        public Vector3 GetLocalDir()
        {
            if (axis == Axis.X)
                return Vector3.right;
            else if (axis == Axis.Y)
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
            if (axis == Axis.X)
                return Vector3.up;
            else if (axis == Axis.Y)
                return Vector3.forward;
            else
                return Vector3.up;
        }

        /// <summary>
        /// カプセルのスケール値を取得
        /// 方向軸のスケール値を採用する
        /// </summary>
        /// <returns></returns>
        public float GetScale()
        {
            var scl = transform.lossyScale;
            if (axis == Axis.X)
                return scl.x;
            else if (axis == Axis.Y)
                return scl.y;
            else
                return scl.z;
        }

        /// <summary>
        /// 指定座標に最も近い衝突点pと、中心軸からのpへの方向dirを返す。
        /// ※エディタ計算用
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="p"></param>
        /// <param name="dir"></param>
        public override bool CalcNearPoint(Vector3 pos, out Vector3 p, out Vector3 dir, out Vector3 d, bool skinning)
        {
            dir = Vector3.zero;

            var ldir = GetLocalDir();
            var l = ldir * Length;

            //var tpos = transform.position;
            var tpos = transform.TransformPoint(Center);
            var trot = transform.rotation;
            float scl = GetScale();
            l *= scl;

            var spos = trot * -l + tpos;
            var epos = trot * l + tpos;

#if true
            // 半径分長さ拡張
            if (skinning == false)
            {
                const float ratio = 0.5f;
                spos = trot * (-l - ldir * StartRadius * scl * ratio) + tpos;
                epos = trot * (l + ldir * EndRadius * scl * ratio) + tpos;
            }
#endif

            float t = MathUtility.ClosestPtPointSegmentRatio(pos, spos, epos);

#if true
            // 蓋の部分は無効とする
            if (skinning == false)
            {
                if (t < 0.0001f || t > 0.9999f)
                {
                    p = Vector3.zero;
                    d = Vector3.zero;
                    return false;
                }
            }
#endif

            float cr = Mathf.Lerp(StartRadius * scl, EndRadius * scl, t);
            d = spos + (epos - spos) * t; // 中心軸位置
            var v = pos - d;
            float vlen = v.magnitude;
            if (vlen < cr)
            {
                // 衝突している
                p = pos;
                if (vlen > 0.0f)
                    dir = v.normalized;
            }
            else
            {
                dir = v.normalized;
                p = d + dir * cr;
            }

            return true;
        }
    }
}
