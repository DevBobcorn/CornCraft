#nullable enable
using UnityEngine;
using Enviro;

namespace CraftSharp.Rendering
{
    public class Enviro3EnvironmentManager : BaseEnvironmentManager
    {
        [SerializeField] private EnviroManager? enviroInstance;
        [SerializeField] private EnviroWeatherType? normalWeatherType;
        [SerializeField] private EnviroWeatherType? rainWeatherType;
        private int timeOfDayRaw;

        public override void SetRain(bool raining)
        {
            if (!raining)
            {
                enviroInstance!.Weather.ChangeWeather(normalWeatherType);
            }
            else
            {
                enviroInstance!.Weather.ChangeWeather(rainWeatherType);
            }
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
            int enviroTime = ((int)(enviroInstance!.Time.GetUniversalTimeOfDay() * 1000) + 18000) % 24000;

            if (t.Settings.simulate != shouldSimulate)
            {
                t.Settings.simulate = shouldSimulate;

                // Make sure to update time if pause state is changed
                int seconds = (int)((timeOfDayRaw % 1000) * 3.6F);
                t.SetDateTime(seconds % 60, seconds / 60, hourTime, 1, 1, 1);
                //Debug.Log($"Daytime update {GetTimeString()} (Simulate switched to {shouldSimulate})");
            }
            else if (Mathf.Abs(enviroTime - timeOfDayRaw) > 50)
            {
                int seconds = (int)((timeOfDayRaw % 1000) * 3.6F);
                t.SetDateTime(seconds % 60, seconds / 60, hourTime, 1, 1, 1);
                //Debug.Log($"Daytime update {GetTimeString()}");
            }
        }

        public override string GetTimeString()
        {
            int enviroTime = ((int)(enviroInstance!.Time.GetUniversalTimeOfDay() * 1000) + 18000) % 24000;
            return $"{enviroInstance!.Time.GetTimeString()} ({enviroTime} / {timeOfDayRaw} / {enviroTime - timeOfDayRaw})";
        }
    }
}