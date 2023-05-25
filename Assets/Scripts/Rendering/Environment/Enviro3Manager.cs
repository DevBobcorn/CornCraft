#nullable enable
using UnityEngine;

namespace MinecraftClient.Rendering
{
    public class Enviro3Manager : BaseEnvironmentManager
    {
        [SerializeField] private Enviro.EnviroManager? enviroInstance;
        private int timeOfDayRaw;

        public override void SetRain(bool raining)
        {
            
        }

        public override void SetTime(long timeRaw)
        {
            // Simulate if time is not paused (dayLightCycle set to true)
            var shouldSimulate = timeRaw >= 0L;
            // time value is negative if time is paused
            if (timeRaw < 0L) timeRaw = -timeRaw;
            var t = enviroInstance!.Time;

            timeOfDayRaw = (int)(timeRaw % 24000L);

            int hourTime = (timeOfDayRaw / 1000 + 6) % 24;
            float time = hourTime + (timeOfDayRaw % 1000) / 1000F;

            if (t.Settings.simulate != shouldSimulate)
            {
                t.Settings.simulate = shouldSimulate;

                // Make sure to update time if pause state is changed
                int seconds = (int)((timeOfDayRaw % 1000) * 3.6F);
                t.SetDateTime(seconds % 60, seconds / 60, hourTime, 1, 1, 1);
            }
            else if (Mathf.Abs(time - t.GetUniversalTimeOfDay()) > 0.002F)
            {
                int seconds = (int)((timeOfDayRaw % 1000) * 3.6F);
                t.SetDateTime(seconds % 60, seconds / 60, hourTime, 1, 1, 1);
            }
            
        }

        public override string GetTimeString()
        {
            int enviroTime = ((int)(enviroInstance!.Time.GetUniversalTimeOfDay() * 1000) + 18000) % 24000;
            return $"{enviroInstance!.Time.GetTimeString()} ({enviroTime} / {timeOfDayRaw} / {enviroTime - timeOfDayRaw})";
        }
    }
}