// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp

using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 風ゾーン１つの情報
    /// </summary>
    public struct TeamWindInfo : IValid
    {
        public int windId;
        public float time;
        public float main;
        public float3 direction;

        public bool IsValid()
        {
            return main > 1e-06f;
        }

        public override string ToString()
        {
            return $"windId:{windId}, time:{time}, main:{main}, direction:{direction}";
        }

        public void DebugLog()
        {
            Debug.Log(ToString());
        }
    }

    /// <summary>
    /// チームに影響する風ゾーンと移動風の情報
    /// </summary>
    public struct TeamWindData
    {
        public FixedList128Bytes<TeamWindInfo> windZoneList;
        public TeamWindInfo movingWind;


        public int ZoneCount => windZoneList.Length;

        public int IndexOf(int windId)
        {
            int cnt = windZoneList.Length;
            for (int i = 0; i < cnt; i++)
            {
                if (windZoneList[i].windId == windId)
                    return i;
            }
            return -1;
        }

        public void ClearZoneList()
        {
            windZoneList.Clear();
        }

        public void AddOrReplaceWindZone(TeamWindInfo windInfo, in TeamWindData oldWindData)
        {
            if (windInfo.IsValid() == false)
                return;

            int i = oldWindData.IndexOf(windInfo.windId);
            if (i >= 0)
            {
                // 旧データに存在する場合はtimeを引き継ぐ
                var oldWindInfo = oldWindData.windZoneList[i];
                windInfo.time = oldWindInfo.time;
            }

            windZoneList.AddNoResize(windInfo);
        }

        public void RemoveWindZone(int windId)
        {
            int i = IndexOf(windId);
            if (i >= 0)
            {
                windZoneList.RemoveAtSwapBack(i);
            }
        }

        //public void DebugLog(int teamId)
        //{
        //    Debug.Log($"TeamWindData:{teamId}, zoneCnt:{ZoneCount}");
        //    for (int i = 0; i < ZoneCount; i++)
        //    {
        //        windZoneList[i].DebugLog();
        //    }

        //    if (movingWind.IsValid())
        //    {
        //        Debug.Log("Moving");
        //        movingWind.DebugLog();
        //    }
        //}
    }
}
