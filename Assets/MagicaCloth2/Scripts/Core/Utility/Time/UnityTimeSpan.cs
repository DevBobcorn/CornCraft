// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 時間計測クラス(UnityEngine.Timeを使用する）
    /// </summary>
    public class UnityTimeSpan
    {
        string name = string.Empty;
        float stime;
        float etime;
        bool isFinish;

        public UnityTimeSpan(string name)
        {
            this.name = name;
            stime = Time.realtimeSinceStartup;
        }

        public void Finish()
        {
            if (isFinish == false)
            {
                etime = Time.realtimeSinceStartup;
                isFinish = true;
            }
        }

        public float TotalSeconds()
        {
            Finish();
            return (etime - stime);
        }

        public float TotalMilliSeconds()
        {
            Finish();
            return (etime - stime) * 1000.0f;
        }

        public override string ToString()
        {
            //return $"TimeSpan [{name}] : {TotalSeconds()}(s)";
            return $"UnityTimeSpan [{name}] : {TotalMilliSeconds()}(ms)";
        }

        public void DebugLog()
        {
            Debug.Log(this);
        }
    }
}
