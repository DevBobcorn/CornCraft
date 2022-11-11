using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 時間計測クラス
    /// </summary>
    public class StopWatch
    {
        private float startTime;
        private float endTime;

        public StopWatch Start()
        {
            startTime = Time.realtimeSinceStartup;
            return this;
        }

        public StopWatch Stop()
        {
            endTime = Time.realtimeSinceStartup;
            return this;
        }

        public float ElapsedSeconds
        {
            get
            {
                return (endTime - startTime);
            }
        }

        public float ElapsedMilliseconds
        {
            get
            {
                return (endTime - startTime) * 1000.0f;
            }
        }
    }
}
