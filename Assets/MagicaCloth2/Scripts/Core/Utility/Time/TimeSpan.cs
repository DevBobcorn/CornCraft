// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;

namespace MagicaCloth2
{
    /// <summary>
    /// 時間計測クラス
    /// </summary>
    public class TimeSpan
    {
        string name = string.Empty;
        DateTime stime;
        DateTime etime;
        //bool isFinish;

        public TimeSpan() { }

        public TimeSpan(string name)
        {
            this.name = name;
            stime = DateTime.Now;
        }

        public void Start()
        {
            stime = DateTime.Now;
        }

        public void Finish()
        {
            etime = DateTime.Now;
            //if (isFinish == false)
            //{
            //    etime = DateTime.Now;
            //    isFinish = true;
            //}
        }

        public double TotalSeconds()
        {
            Finish();
            return (etime - stime).TotalSeconds;
        }

        public double TotalMilliSeconds()
        {
            Finish();
            return (etime - stime).TotalMilliseconds;
        }

        public override string ToString()
        {
            //return $"TimeSpan [{name}] : {TotalSeconds()}(s)";
            return $"TimeSpan [{name}] : {TotalMilliSeconds()}(ms)";
        }

        public void DebugLog()
        {
            Develop.DebugLog(this);
        }
    }
}
