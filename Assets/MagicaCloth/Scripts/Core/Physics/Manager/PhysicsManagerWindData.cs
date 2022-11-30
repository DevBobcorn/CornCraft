// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 風データ
    /// </summary>
    public class PhysicsManagerWindData : PhysicsManagerAccess
    {
        /// <summary>
        /// 風タイプ
        /// </summary>
        public enum WindType
        {
            None = 0,
            Direction,
            Area,
        }

        /// <summary>
        /// 形状タイプ
        /// </summary>
        public enum ShapeType
        {
            Box = 0,
            Sphere = 1,
        }

        /// <summary>
        /// 風向き
        /// </summary>
        public enum DirectionType
        {
            OneDirection = 0,   // 一定方向
            Radial = 1,         // 放射状
        }


        /// <summary>
        /// 風フラグビット
        /// </summary>
        public const uint Flag_Enable = 0x00000001; // 有効フラグ
        public const uint Flag_Addition = 0x00000002; // 加算モード

        /// <summary>
        /// 風データ
        /// </summary>
        public struct WindData
        {
            /// <summary>
            /// フラグビットデータ
            /// </summary>
            public uint flag;

            /// <summary>
            /// 風タイプ
            /// </summary>
            public WindType windType;

            /// <summary>
            /// 形状
            /// </summary>
            public ShapeType shapeType;

            /// <summary>
            /// 連動トランスフォームインデックス
            /// </summary>
            public int transformIndex;

            /// <summary>
            /// 風エリアのサイズ（トランスフォームのローカル軸サイズ）
            /// 球形の場合はxに半径
            /// </summary>
            public float3 areaSize;

            /// <summary>
            /// 風量
            /// </summary>
            public float main;

            /// <summary>
            /// 乱流率(0.0-1.0)
            /// </summary>
            public float turbulence;

            /// <summary>
            /// 振動の周期(1.0が基準)
            /// </summary>
            public float frequency;

            /// <summary>
            /// 風の中心位置（-1.0 - +1.0)
            /// </summary>
            //public float3 anchor;

            /// <summary>
            /// 現在の風の方向（ローカル）
            /// </summary>
            public float3 direction;

            /// <summary>
            /// 風向きのタイプ
            /// </summary>
            public DirectionType directionType;

            /// <summary>
            /// 風エリアの体積
            /// </summary>
            public float areaVolume;

            /// <summary>
            /// 風エリアの最大距離
            /// </summary>
            public float areaLength;

            /// <summary>
            /// 減衰カーブ
            /// </summary>
            public CurveParam attenuation;

            /// <summary>
            /// フラグ判定
            /// </summary>
            /// <param name="flag"></param>
            /// <returns></returns>
            public bool IsFlag(uint flag)
            {
                return (this.flag & flag) != 0;
            }

            /// <summary>
            /// フラグ設定
            /// </summary>
            /// <param name="flag"></param>
            /// <param name="sw"></param>
            public void SetFlag(uint flag, bool sw)
            {
                if (sw)
                    this.flag |= flag;
                else
                    this.flag &= ~flag;
            }

            /// <summary>
            /// 有効フラグの設定
            /// </summary>
            /// <param name="sw"></param>
            public void SetEnable(bool sw)
            {
                if (sw)
                    flag |= Flag_Enable;
                else
                    flag &= ~Flag_Enable;
            }

            /// <summary>
            /// データが有効か判定する
            /// </summary>
            /// <returns></returns>
            public bool IsActive()
            {
                return (flag & Flag_Enable) != 0;
            }
        }

        //=========================================================================================
        /// <summary>
        /// 風データリスト
        /// </summary>
        public FixedNativeList<WindData> windDataList;

        //=========================================================================================
        /// <summary>
        /// 初期設定
        /// </summary>
        public override void Create()
        {
            windDataList = new FixedNativeList<WindData>();
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public override void Dispose()
        {
            if (windDataList == null)
                return;

            windDataList.Dispose();
        }

        //=========================================================================================
        public int CreateWind(
            WindType windType, ShapeType shapeType, float3 areaSize, bool addition, float main, float turbulence, float frequency,
            float3 direction, DirectionType directinType, float areaVolume, float areaLength, BezierParam attenuation
            )
        {
            var data = new WindData();

            uint flag = Flag_Enable;
            flag |= addition ? Flag_Addition : 0;
            data.flag = flag;
            data.windType = windType;
            data.shapeType = shapeType;
            data.transformIndex = -1;
            data.areaSize = areaSize;
            data.main = main;
            data.turbulence = turbulence;
            data.frequency = frequency;
            //data.anchor = math.clamp(anchor, -1, 1);
            data.direction = direction; // local
            data.directionType = directinType;
            data.areaVolume = areaVolume;
            data.areaLength = areaLength;
            data.attenuation.Setup(attenuation);

            int windId = windDataList.Add(data);

            return windId;
        }

        public void RemoveWind(int windId)
        {
            if (windId >= 0)
            {
                windDataList.Remove(windId);
            }
        }

        /// <summary>
        /// 風の有効フラグ切り替え
        /// </summary>
        /// <param name="windId"></param>
        /// <param name="sw"></param>
        public void SetEnable(int windId, bool sw, Transform target)
        {
            if (windId >= 0)
            {
                WindData data = windDataList[windId];
                data.SetEnable(sw);

                // 連動トランスフォームを登録／解除
                if (sw)
                {
                    if (data.transformIndex == -1)
                    {
                        data.transformIndex = Bone.AddBone(target);
                    }
                }
                else
                {
                    if (data.transformIndex >= 0)
                    {
                        Bone.RemoveBone(data.transformIndex);
                        data.transformIndex = -1;
                    }
                }

                windDataList[windId] = data;
            }
        }

        /// <summary>
        /// 風が有効状態か判定する
        /// </summary>
        /// <param name="windId"></param>
        /// <returns></returns>
        public bool IsActive(int windId)
        {
            if (windId >= 0)
                return windDataList[windId].IsActive();
            else
                return false;
        }

        /// <summary>
        /// 風の状態フラグ設定
        /// </summary>
        /// <param name="windId"></param>
        /// <param name="flag"></param>
        /// <param name="sw"></param>
        public void SetFlag(int windId, uint flag, bool sw)
        {
            if (windId < 0)
                return;
            WindData data = windDataList[windId];
            data.SetFlag(flag, sw);
            windDataList[windId] = data;
        }

        public void SetParameter(
            int windId, float3 areaSize, bool addition, float main, float turbulence, float frequency,
            float3 direction, float areaVolume, float areaLength, BezierParam attenuation
            )
        {
            if (windId < 0)
                return;
            WindData data = windDataList[windId];
            data.SetFlag(Flag_Addition, addition);
            data.areaSize = areaSize;
            data.main = main;
            data.turbulence = turbulence;
            data.frequency = frequency;
            //data.anchor = math.clamp(anchor, -1, 1);
            data.direction = direction; // local
            data.areaVolume = areaVolume;
            data.areaLength = areaLength;
            data.attenuation.Setup(attenuation);
            windDataList[windId] = data;
        }

        public int Count
        {
            get
            {
                if (windDataList == null)
                    return 0;
                return windDataList.Count;
            }
        }

        //=========================================================================================
        /// <summary>
        /// 座標をもとに風の力を計算して返す
        /// </summary>
        /// <param name="time"></param>
        /// <param name="noiseBasePos"></param>
        /// <param name="mainDir"></param>
        /// <param name="main"></param>
        /// <param name="turbulence"></param>
        /// <param name="frequency"></param>
        /// <param name="randomScale"></param>
        /// <returns></returns>
        internal static float3 CalcWindForce(float time, float2 noiseBasePos, float3 mainDir, float main, float turbulence, float frequency, float randomScale)
        {
            // 風量による計算比率
            float ratio = main / 30.0f; // 風速30を基準

            // 風向きのランダム角度（風量に比例する）
            float rang = 15.0f + 15.0f * ratio;

            // 風向きの周期
            float dirFreq = 1.0f + 2.0f * ratio; // 1.0 - 3.0
            dirFreq *= frequency;

            // 方向ノイズ
            var noisePos1 = noiseBasePos.xy;
            var noisePos2 = noiseBasePos.yx;
            noisePos1.x += time * dirFreq; // 周期（数値を高くするとランダム性が増す）2.0f?
            noisePos2.y += time * dirFreq; // 周期（数値を高くするとランダム性が増す）2.0f?
            var nv1 = noise.snoise(noisePos1); // -1.0f～1.0f
            var nv2 = noise.snoise(noisePos2); // -1.0f～1.0f

            // 方向のランダム性
            var ang1 = math.radians(nv1 * rang);
            var ang2 = math.radians(nv2 * rang);
            ang1 *= turbulence; // 乱流率
            ang2 *= turbulence; // 乱流率
            var rq = quaternion.Euler(ang1, ang2, 0.0f); // XY
            var dirq = MathUtility.AxisQuaternion(mainDir);
            float3 wdir = math.forward(math.mul(dirq, rq));

            // 風力ノイズ
            var noisePos3 = noiseBasePos * 6.36913f;
            //noisePos3.x += time * frequency;
            noisePos3.x += time * (1.0f + 1.0f * ratio) * frequency;
            //float nv = noise.snoise(noisePos3); // -1.0f～1.0f
            float nv = noise.cnoise(noisePos3); // -1.0f～1.0f

            // 風力のランダム性
            float scl = math.max(nv * randomScale, -1.0f); // scale
            main += main * scl;

            // 最終合成
            float3 force = wdir * main;

            return force;
        }

        //=========================================================================================
#if false // 風の計算はすべてチーム処理へ移動
        /// <summary>
        /// 風の更新
        /// </summary>
        public void UpdateWind()
        {
            var job = new UpdateWindJob()
            {
                dtime = manager.UpdateTime.DeltaTime,
                elapsedTime = Time.time,

                bonePosList = Bone.bonePosList.ToJobArray(),
                boneRotList = Bone.boneRotList.ToJobArray(),

                windData = windDataList.ToJobArray(),
            };
            Compute.MasterJob = job.Schedule(windDataList.Length, 1, Compute.MasterJob);
        }

        [BurstCompile]
        struct UpdateWindJob : IJobParallelFor
        {
            public float dtime;
            public float elapsedTime;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> bonePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> boneRotList;

            public NativeArray<WindData> windData;

            // 風データごと
            public void Execute(int index)
            {
                var wdata = windData[index];
                if (wdata.IsActive() == false || wdata.transformIndex < 0)
                    return;

                // コンポーネント姿勢
                var bpos = bonePosList[wdata.transformIndex];
                var brot = boneRotList[wdata.transformIndex];

                // 風量による計算比率
                float ratio = wdata.main / 30.0f; // 風速30を基準

                // 周期（風向きが変わる速度）
                float freq = 1.0f + 2.0f * ratio; // 1.0 - 3.0

                // 風向きのランダム角度
                float rang = 15.0f + 15.0f * ratio; // 15 - 30

                // ノイズ参照
                var noisePos1 = new float2(bpos.x, bpos.z) * 0.1f;
                var noisePos2 = new float2(bpos.x, bpos.z) * 0.1f;
                noisePos1.x += elapsedTime * freq; // 周期（数値を高くするとランダム性が増す）2.0f?
                noisePos2.y += elapsedTime * freq; // 周期（数値を高くするとランダム性が増す）2.0f?
                var nv1 = noise.snoise(noisePos1); // -1.0f～1.0f
                var nv2 = noise.snoise(noisePos2); // -1.0f～1.0f

                // 方向のランダム性
                var ang1 = math.radians(nv1 * rang);
                var ang2 = math.radians(nv2 * rang);
                ang1 *= wdata.turbulence; // 乱流率
                ang2 *= wdata.turbulence; // 乱流率
                var rq = quaternion.Euler(ang1, ang2, 0.0f); // XY
                var dir = math.forward(math.mul(brot, rq)); // ランダムはローカル回転
                wdata.direction = dir;

                // 書き戻し
                windData[index] = wdata;
            }
        }
#endif
    }
}
