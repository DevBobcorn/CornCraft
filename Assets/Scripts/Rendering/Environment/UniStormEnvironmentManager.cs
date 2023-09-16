#nullable enable
using UnityEngine;
using UniStorm;

namespace CraftSharp.Rendering
{
    public class UniStormEnvironmentManager : BaseEnvironmentManager
    {
        private const float TICK_SECONDS = 0.05F;

        [SerializeField] private UniStormSystem? unistorm;

        private int lastRecTicks;

        void Start()
        {
            // Day and night both lasts for 10 minutes
            unistorm!.DayLength = 10;
            unistorm!.DayLength = 10;
        }

        public override void SetRain(bool raining)
        {
            // TODO: Implement
        }

        public override void SetTime(long timeRaw)
        {
            // Simulate if time is not paused (dayLightCycle set to true)
            var shouldTimeFlow = timeRaw >= 0L ? UniStormSystem.EnableFeature.Enabled : UniStormSystem.EnableFeature.Disabled;
            // time value is negative if time is paused
            if (timeRaw < 0L) timeRaw = -timeRaw;

            lastRecTicks = (int)(timeRaw % 24000L);

            if (unistorm!.TimeFlow != shouldTimeFlow)
            {
                unistorm!.TimeFlow = shouldTimeFlow;

                // Make sure to update time if pause is toggled
                UpdateTime(lastRecTicks);
            }
            else
            {
                UpdateTime(lastRecTicks);
            }
        }

        private void UpdateTime(int serverTicks)
        {
            unistorm!.m_TimeFloat = (serverTicks + 6000) % 24000 / 24000F;
        }

        public static (int hours, int minutes, int seconds) Tick2HMS(int ticks)
        {
            int hours = (ticks / 1000 + 6) % 24;
            int secsInHour = (int)(ticks % 1000 * 3.6F);

            return (hours, secsInHour / 60, secsInHour % 60);
        }

        public static string GetTimeStringFromTicks(int ticks)
        {
            int hours = (ticks / 1000 + 6) % 24;
            int minutes = (int)(ticks % 1000 * 3.6F) / 60;

            return $"{hours:00}:{minutes:00}";
        }

        public override string GetTimeString()
        {
            return $"{GetTimeStringFromTicks(lastRecTicks)} ({lastRecTicks})";
        }
    }
}