#nullable enable
using CraftSharp.Control;
using UnityEngine;
using UnityEngine.Playables;

namespace CraftSharp.Rendering
{
    public class TimelineEnvironmentManager : BaseEnvironmentManager
    {
        private const float TICK_SECONDS = 0.05F;

        private PlayableDirector? playableDirector;

        [SerializeField] private long startTime;

        private Camera? mainCamera;

        [SerializeField] private AnimeSunDirection? animeSunControl;

#if ATMOSPHERIC_HEIGHT_FOG
        [SerializeField] AtmosphericHeightFog.HeightFogGlobal? fogGlobal;
#endif

        private int ticks;
        private int lastRecTicks = int.MinValue;
        private bool simulate = false;

        private float deltaSeconds = 0F;

        public override void SetCamera(Camera mainCamera)
        {
            this.mainCamera = mainCamera;

#if ATMOSPHERIC_HEIGHT_FOG
            if (fogGlobal != null)
            {
                fogGlobal.mainCamera = mainCamera;

                if (!fogGlobal.gameObject.activeSelf)
                {
                    fogGlobal.gameObject.SetActive(true);
                }
            }
#endif
        }

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

                if (simulate)
                {
                    playableDirector!.Resume();
                }
                else
                {
                    playableDirector!.Pause();
                }
            }
            else if (Mathf.Abs(ticks - lastRecTicks) > 25F)
            {
                UpdateTime(lastRecTicks);
            }
        }

        private void Start()
        {
            playableDirector = GetComponent<PlayableDirector>();
            SetPlayableSpeed(4D / 1200D);

            SetTime(startTime);

            if (simulate)
            {
                playableDirector!.Resume();
            }
            else
            {
                playableDirector!.Pause();
            }
        }

        private void Update()
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

            if (mainCamera)
            {
                transform.position = mainCamera.transform.position;

#if ATMOSPHERIC_HEIGHT_FOG
                if (fogGlobal)
                {
                    fogGlobal.fogHeightStart = transform.position.y - 10F;
                    fogGlobal.fogHeightEnd = transform.position.y + 300F;
                }
            }
            else // Main camera temporarily unavailable, disable fog to avoid it flooding the console
            {
                if (fogGlobal && fogGlobal.gameObject.activeSelf)
                {
                    fogGlobal.gameObject.SetActive(false);
                }

#endif
            }
        }

        private void SetPlayableSpeed(double speed)
        {
            if (playableDirector)
            {
                var playableGraph = playableDirector.playableGraph;
                
                if (!playableGraph.IsValid())
                {
                    playableDirector.RebuildGraph();
                }

                if (playableGraph.IsValid())
                {
                    playableDirector.playableGraph.GetRootPlayable(0).SetSpeed(speed);
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

        private float GetDayNightLerp(float normalizedTOD)
        {
            float halfGap = 0.1F;
            float gap = 2 * halfGap;
            float keepTime = 0.5F - gap;

            if (normalizedTOD < halfGap)
            {
                return 0.5F - (normalizedTOD / halfGap) * 0.5F;
            }
            else if (normalizedTOD < halfGap + keepTime)
            {
                return 0F;
            }
            else if (normalizedTOD < halfGap + keepTime + gap)
            {
                return (normalizedTOD - halfGap - keepTime) / gap;
            }
            else if (normalizedTOD < halfGap + keepTime + gap + keepTime)
            {
                return 1F;
            }
            else
            {
                return ((normalizedTOD - halfGap - keepTime - gap - keepTime) / halfGap) * 0.5F;
            }
        }

        private void UpdateTimeRelated()
        {
            double playableTOD = ticks / 24000D;
            playableDirector!.time = playableDirector.duration * playableTOD;

            // Update directional light
            // 00:00 - 0.00 - 270
            // 06:00 - 0.25 - 180
            // 12:00 - 0.50 -  90
            // 18:00 - 0.75 -   0
            // 24:00 - 1.00 - -90

            if (animeSunControl != null)
            {
                animeSunControl.SetTime((float) playableTOD);
            }

#if ATMOSPHERIC_HEIGHT_FOG
            if (fogGlobal != null)
            {
                fogGlobal.timeOfDay = GetDayNightLerp((float) playableTOD);
            }
#endif

            DynamicGI.UpdateEnvironment();
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