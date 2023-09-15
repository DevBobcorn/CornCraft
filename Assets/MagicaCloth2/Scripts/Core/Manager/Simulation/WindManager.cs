// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 風ゾーン管理マネージャ
    /// </summary>
    public class WindManager : IManager, IValid
    {
        public const int Flag_Valid = 0; // データの有効性
        public const int Flag_Enable = 1; // 動作状態
        public const int Flag_Addition = 2; // 加算風

        /// <summary>
        /// 風ゾーンの管理データ
        /// </summary>
        public struct WindData
        {
            public BitField32 flag;

            public MagicaWindZone.Mode mode;

            /// <summary>
            /// Global:(none)
            /// Box   :(x, y, z)
            /// Sphere:(radius, radius, radius)
            /// </summary>
            public float3 size;

            public float main;
            public float turbulence;
            public float zoneVolume;

            public float3 worldWindDirection;

            public float3 worldPositin;
            public quaternion worldRotation;
            public float3 worldScale;
            public float4x4 worldToLocalMatrix;
            public float4x4 attenuation;

            public bool IsValid() => flag.IsSet(Flag_Valid);
            public bool IsEnable() => flag.IsSet(Flag_Enable);
            public bool IsAddition() => flag.IsSet(Flag_Addition);
        }
        public ExNativeArray<WindData> windDataArray;

        public int WindCount => windDataArray?.Count ?? 0;

        bool isValid;

        /// <summary>
        /// WindIDとゾーンコンポーネントの関連辞書
        /// </summary>
        Dictionary<int, MagicaWindZone> windZoneDict = new Dictionary<int, MagicaWindZone>();

        //=========================================================================================
        public void Dispose()
        {
            isValid = false;

            windDataArray?.Dispose();
        }

        public void EnterdEditMode()
        {
            Dispose();
        }

        public void Initialize()
        {
            Dispose();

            const int capacity = 64;
            windDataArray = new ExNativeArray<WindData>(capacity);

            isValid = true;
        }

        public bool IsValid()
        {
            return isValid;
        }

        //=========================================================================================
        public int AddWind(MagicaWindZone windZone)
        {
            if (isValid == false || windZone == null)
                return -1;

            var wind = new WindData();
            // ★Enableフラグは立てない
            wind.flag.SetBits(Flag_Valid, true);
            var c = windDataArray.Add(wind);

            windZoneDict.Add(c.startIndex, windZone);

            return c.startIndex;
        }

        public void RemoveWind(int windId)
        {
            if (isValid == false || windId < 0)
                return;

            var c = new DataChunk(windId);
            windDataArray.RemoveAndFill(c);

            windZoneDict.Remove(c.startIndex);
        }

        public void SetEnable(int windId, bool sw)
        {
            if (isValid == false || windId < 0)
                return;

            var wind = windDataArray[windId];
            wind.flag.SetBits(Flag_Enable, sw);
            windDataArray[windId] = wind;
        }

        //=========================================================================================
        /// <summary>
        /// 毎フレーム常に実行する更新
        /// </summary>
        internal void AlwaysWindUpdate()
        {
            // ジョブでは実行できない風ゾーンの更新
            foreach (var kv in windZoneDict)
            {
                int windId = kv.Key;
                var windZone = kv.Value;

                if (windId < 0 || windZone == null)
                    continue;

                var t = windZone.transform;

                // コンポーネントデータのコピー
                ref var wind = ref windDataArray.GetRef(windId);
                wind.mode = windZone.mode;
                switch (windZone.mode)
                {
                    case MagicaWindZone.Mode.BoxDirection:
                        wind.size = windZone.size;
                        break;
                    case MagicaWindZone.Mode.SphereDirection:
                    case MagicaWindZone.Mode.SphereRadial:
                        wind.size = windZone.radius;
                        break;
                }
                wind.main = windZone.main;
                wind.turbulence = windZone.turbulence;

                wind.flag.SetBits(Flag_Addition, windZone.IsAddition());

                wind.worldPositin = t.position;
                wind.worldRotation = t.rotation;
                wind.worldScale = t.lossyScale;
                wind.worldToLocalMatrix = t.worldToLocalMatrix;

                // volume
                float volume = 0;
                switch (wind.mode)
                {
                    case MagicaWindZone.Mode.GlobalDirection:
                        volume = float.MaxValue;
                        break;
                    case MagicaWindZone.Mode.BoxDirection:
                        float3 gsize = wind.size * wind.worldScale;
                        volume = gsize.x * gsize.y * gsize.z;
                        break;
                    case MagicaWindZone.Mode.SphereDirection:
                    case MagicaWindZone.Mode.SphereRadial:
                        float r = wind.size.x * wind.worldScale.x;
                        volume = (4.0f / 3.0f) * r * r * r * math.PI;
                        break;
                }
                wind.zoneVolume = volume;

                if (windZone.IsDirection())
                {
                    wind.worldWindDirection = windZone.GetWindDirection();
                    //Debug.Log($"wdir:{wind.worldWindDirection}");
                }
                else
                {
                    // 減衰カーブ
                    wind.attenuation = DataUtility.ConvertAnimationCurve(windZone.attenuation);
                }
            }
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"========== Wind Manager ==========");
            if (IsValid() == false)
            {
                sb.AppendLine($"Wind Manager. Invalid.");
            }
            else
            {
                sb.AppendLine($"Wind Manager. Count:{WindCount}");

                int cnt = WindCount;
                for (int i = 0; i < cnt; i++)
                {
                    var wind = windDataArray[i];
                    if (wind.flag.IsSet(Flag_Valid) == false)
                        continue;

                    sb.AppendLine($"  [{i}] flag:0x{wind.flag.Value:X}, mode:{wind.mode}");
                }
            }
            sb.AppendLine();
            Debug.Log(sb.ToString());
            allsb.Append(sb);
        }
    }
}
