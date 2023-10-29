#nullable enable
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class CornEnvironmentManager : BaseEnvironmentManager
    {
        private const float TICK_SECONDS = 0.05F;

        [SerializeField] private Transform? sunTransform;
        [SerializeField] private Light? sunLight;
        [SerializeField] AnimationCurve? lightIntensity;

        private Material? skyboxMaterial;
        [SerializeField] AnimationCurve? skyboxExposure;

        private int ticks;
        private int lastRecTicks;
        private bool simulate = false;

        private float deltaSeconds = 0F;

        public override void SetRain(bool raining)
        {
            // TODO: Implement
        }

        public override void SetTime(long timeRaw)
        {
            // Simulate if time is not paused (dayLightCycle set to true)
            var shouldSimulate = timeRaw >= 0L;
            // time value is negative if time is paused
            if (timeRaw < 0L) timeRaw = -timeRaw;

            lastRecTicks = (int)(timeRaw % 24000L);

            if (simulate != shouldSimulate)
            {
                simulate = shouldSimulate;

                // Make sure to update time if pause is toggled
                UpdateTime(lastRecTicks);
            }
            else if (Mathf.Abs(ticks - lastRecTicks) > 25F)
            {
                UpdateTime(lastRecTicks);
            }
        }

        void Update()
        {
            if (simulate) // Simulate time passing
            {
                deltaSeconds += Time.unscaledDeltaTime;

                if (deltaSeconds > TICK_SECONDS)
                {
                    while (deltaSeconds > TICK_SECONDS)
                    {
                        deltaSeconds -= TICK_SECONDS;
                        ticks++;
                    }

                    UpdateTimeRelated();
                }
            }
        }

        private void UpdateTime(int serverTicks)
        {
            ticks = serverTicks;
            // Reset delta seconds
            deltaSeconds = 0F;

            UpdateTimeRelated();
        }

        private void UpdateTimeRelated()
        {
            float normalizedTOD = (ticks + 6000) % 24000 / 24000F;

            // Update directional light
            // 00:00 - 0.00 - 270
            // 06:00 - 0.25 - 180
            // 12:00 - 0.50 -  90
            // 18:00 - 0.75 -   0
            // 24:00 - 1.00 - -90

            sunTransform!.localEulerAngles = new Vector3(270F - normalizedTOD * 360F, 0F, 0F);
            sunLight!.intensity = lightIntensity!.Evaluate(normalizedTOD);

            if (skyboxMaterial == null)
            {
                skyboxMaterial = new Material(RenderSettings.skybox);
                RenderSettings.skybox = skyboxMaterial;
            }

            skyboxMaterial!.SetFloat("_Exposure", skyboxExposure!.Evaluate(normalizedTOD));
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

            return $"{GetTimeStringFromTicks(ticks)} ({ticks} / {lastRecTicks})";
        }
    }
}