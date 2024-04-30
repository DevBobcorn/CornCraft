using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CraftSharp.Control
{
    public class SkillStageTrack
    {
        private readonly int stageIndex;

        private float stageStart;    // Seconds
        private float stageDuration; // Seconds
        private float damageStart;   // Seconds
        private float damageEnd;     // Seconds

        private AnimationClip animationClip;

        private Rect rectHeader = new();
        private Rect rectContent = new();
        private Rect rectAnimation = new();

        public float TrackHeight { get; private set; } = 42F;

        public float AnimationHeight { get; private set; } = 12F;

        public SkillStageTrack(float start, int index, PlayerAttackStage stage)
        {
            stageStart = start;
            stageDuration = stage.Duration;

            stageIndex = index;

            animationClip = stage.AnimationClip;
        }

        public void DrawHeader(float startY, float width)
        {
            rectHeader.Set(5, startY, width, TrackHeight);
            GUI.DrawTexture(rectHeader, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 0, TRACK_HEADER, 0, 3);

            var defColor = GUI.backgroundColor;

            GUILayout.BeginArea(rectHeader);
            //GUILayout.Space(10);
            GUILayout.BeginHorizontal();
                GUILayout.Space(5);
                GUILayout.Label($"Stage {stageIndex}", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
                GUILayout.Space(5);
                GUI.backgroundColor = TRACK_BODY[stageIndex % TRACK_BODY.Length] * 2; // Intensify the color
                animationClip =  EditorGUILayout.ObjectField(animationClip, typeof (AnimationClip), false,
                        GUILayout.Width(width - 50)) as AnimationClip;
                GUI.backgroundColor = defColor; // Restore background color

                if (animationClip != null)
                {
                    GUILayout.Label($"{animationClip.length:0.00}", GUILayout.Width(30));
                }
                else
                {
                    GUILayout.Label("UwU", GUILayout.Width(30));
                }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private static readonly Color TRACK_HEADER = new Color32(40, 40, 40, 255);
        private static readonly Color TRACK_EDGE = new Color32(20, 20, 20, 255);
        private static readonly Color[] TRACK_BODY = Enumerable.Range(0, 7).Select(x =>
        {
            var color = Color.HSVToRGB(x / 7F, 1F, 1F);
            color.a = 0.1F;
            return color;
        }).ToArray();

        public void DrawContent(float startY, float fullWidth, Vector2 timeRange)
        {
            rectContent.Set(0, startY, fullWidth, 8);
            GUI.DrawTexture(rectContent, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0, TRACK_EDGE, 0, 0);
            rectContent.Set(0, startY + TrackHeight - 8, fullWidth, 8);
            GUI.DrawTexture(rectContent, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0, TRACK_EDGE, 0, 0);

            rectContent.Set(0, startY, fullWidth, TrackHeight);
            GUI.DrawTexture(rectContent, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill,
                    true, 0, TRACK_BODY[stageIndex % TRACK_BODY.Length], 0, 0);
            
            // Draw stage duration
            float minTime = timeRange.x;
            float maxTime = timeRange.y;
            float totalSpan = maxTime - minTime;

            float minTimeShown = Mathf.Clamp(stageStart, minTime, maxTime);
            float maxTimeShown = Mathf.Clamp(stageStart + stageDuration, minTime, maxTime);

            if (minTimeShown < maxTimeShown) // Draw duration
            {
                var minPixel = fullWidth * ( (minTimeShown - minTime) / totalSpan );
                //var maxPixel = fullWidth * ( (maxTimeShown - minTime) / totalSpan );
                var widthPixel = fullWidth * ( (maxTimeShown - minTimeShown) / totalSpan );

                if (animationClip != null)
                {
                    var animMaxTime = Mathf.Clamp(stageStart + animationClip.length, minTime, maxTime);
                    var animWidthPixel = fullWidth * ( (animMaxTime - minTimeShown) / totalSpan );

                    rectAnimation.Set(minPixel, startY, animWidthPixel, AnimationHeight);

                    GUI.DrawTexture(rectAnimation, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.white, 1, 0);
                    GUI.DrawTexture(rectAnimation, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.white * 0.8F, 0, 0);
                }

                var bodyStartY = startY + AnimationHeight;
                var bodyHeight = TrackHeight - AnimationHeight;

                rectContent.Set(minPixel, bodyStartY, widthPixel, bodyHeight);

                if (rectContent.Contains(UnityEngine.Event.current.mousePosition))
                {
                    // The mouse is on this rect
                    GUI.DrawTexture(rectContent, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.white * 0.7F, 0, 0);
                    
                    // Draw exact start and end time
                    GUILayout.BeginHorizontal();
                        GUI.color = Color.black;
                        GUI.Label(rectContent, $"Start\n{stageStart}", EditorStyles.boldLabel);
                        GUI.Label(new(minPixel + widthPixel / 2, bodyStartY, widthPixel, bodyHeight), $"{stageDuration}");
                        GUI.color = Color.white;
                        GUI.Label(new(minPixel + widthPixel, bodyStartY, widthPixel, bodyHeight), $"End\n{stageStart + stageDuration}", EditorStyles.boldLabel);
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUI.DrawTexture(rectContent, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.white * 0.5F, 0, 0);
                }

                // Reset GUI color
                GUI.color = Color.white;
            }

            GUILayout.BeginArea(rectContent);
            // Draw detailed information...

            GUILayout.EndArea();
        }
    }
}