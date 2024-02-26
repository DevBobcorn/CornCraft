using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using TickHandler = DMTimeArea.TickHandler;

namespace DMTimeArea
{
    public class TimeArea : ZoomableArea
    {
        public enum TimeFormat
        {
            None,
            TimeFrame,
            Frame
        }
        private class Styles2
        {
            public GUIStyle TimelineTick = "AnimationTimelineTick";
            public GUIStyle labelTickMarks = "CurveEditorLabelTickMarks";
        }
        public enum TimeRulerDragMode
        {
            None,
            Start,
            End,
            Dragging,
            Cancel
        }

        [SerializeField]
        private TickHandler m_HTicks;
        [SerializeField]
        private TickHandler m_VTicks;
        internal const int kTickRulerDistMin = 3;
        internal const int kTickRulerDistFull = 80;
        internal const int kTickRulerDistLabel = 40;
        internal const float kTickRulerHeightMax = 0.7f;
        internal const float kTickRulerFatThreshold = 0.5f;
        private static TimeArea.Styles2 styles;
        private static float s_OriginalTime;
        private static float s_PickOffset;
        public TickHandler hTicks
        {
            get
            {
                return this.m_HTicks;
            }
            set
            {
                this.m_HTicks = value;
            }
        }
        public TickHandler vTicks
        {
            get
            {
                return this.m_VTicks;
            }
            set
            {
                this.m_VTicks = value;
            }
        }
        public TimeArea(bool minimalGUI, bool enableSliderZoom) : base(minimalGUI, enableSliderZoom)
        {
            float[] tickModulos = new float[]
            {
                1E-07f,
                5E-07f,
                1E-06f,
                5E-06f,
                1E-05f,
                5E-05f,
                0.0001f,
                0.0005f,
                0.001f,
                0.005f,
                0.01f,
                0.05f,
                0.1f,
                0.5f,
                1f,
                5f,
                10f,
                50f,
                100f,
                500f,
                1000f,
                5000f,
                10000f,
                50000f,
                100000f,
                500000f,
                1000000f,
                5000000f,
                1E+07f
            };
            this.hTicks = new TickHandler();
            this.hTicks.SetTickModulos(tickModulos);
            this.vTicks = new TickHandler();
            this.vTicks.SetTickModulos(tickModulos);
        }
        private static void InitStyles()
        {
            if (TimeArea.styles == null)
            {
                TimeArea.styles = new TimeArea.Styles2();
            }
        }

        public void SetTickMarkerRanges()
        {
            this.hTicks.SetRanges(base.shownArea.xMin, base.shownArea.xMax, base.drawRect.xMin, base.drawRect.xMax);
            this.vTicks.SetRanges(base.shownArea.yMin, base.shownArea.yMax, base.drawRect.yMin, base.drawRect.yMax);
        }

        public void DrawMajorTicks(Rect position, float frameRate)
        {
            Color color = Handles.color;
            GUI.BeginGroup(position);
            if (Event.current.type != EventType.Repaint)
            {
                GUI.EndGroup();
            }
            else
            {
                TimeArea.InitStyles();
                this.SetTickMarkerRanges();
                this.hTicks.SetTickStrengths(3f, 80f, true);
                Color textColor = TimeArea.styles.TimelineTick.normal.textColor;
                textColor.a = 0.1f;
                Handles.color = textColor;
                Rect shownArea = base.shownArea;
                for (int i = 0; i < this.hTicks.tickLevels; i++)
                {
                    float num = this.hTicks.GetStrengthOfLevel(i) * 0.9f;
                    if (num > 0.5f)
                    {
                        float[] ticksAtLevel = this.hTicks.GetTicksAtLevel(i, true);
                        for (int j = 0; j < ticksAtLevel.Length; j++)
                        {
                            if (ticksAtLevel[j] >= 0f)
                            {
                                int num2 = Mathf.RoundToInt(ticksAtLevel[j] * frameRate);
                                float x = this.FrameToPixel((float)num2, frameRate, position, shownArea);
                                Handles.DrawLine(new Vector3(x, 0f, 0f), new Vector3(x, position.height, 0f));
                            }
                        }
                    }
                }
                GUI.EndGroup();
                Handles.color = color;
            }
        }

        public void TimeRuler(Rect position, float frameRate)
        {
            this.TimeRuler(position, frameRate, true, false, 1f, TimeArea.TimeFormat.TimeFrame);
        }

        public void TimeRuler(Rect position, float frameRate, bool labels, bool useEntireHeight, float alpha)
        {
            this.TimeRuler(position, frameRate, labels, useEntireHeight, alpha, TimeArea.TimeFormat.TimeFrame);
        }

        public void TimeRuler(Rect position, float frameRate, bool labels, bool useEntireHeight, float alpha, TimeArea.TimeFormat timeFormat)
        {
            Color color = GUI.color;
            GUI.BeginGroup(position);
            TimeArea.InitStyles();
            // HandleUtility.ApplyWireMaterial();
            TimeAreaTools.ReflectionApplyWireMaterial();
            Color backgroundColor = GUI.backgroundColor;
            this.SetTickMarkerRanges();
            this.hTicks.SetTickStrengths(3f, 80f, true);
            Color textColor = TimeArea.styles.TimelineTick.normal.textColor;
            textColor.a = 0.75f * alpha;
            if (Event.current.type == EventType.Repaint)
            {
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    GL.Begin(7);
                }
                else
                {
                    GL.Begin(1);
                }
                // Rect shownArea = base.shownArea;
                float a = ShowAreaXMin * frameRate * position.width;
                float b = ShowAreaWidth * frameRate;
                float width = position.width;

                float horRangeMax = base.hRangeMax;
                float horRangeMin = base.hRangeMin;

                for (int i = 0; i < this.hTicks.tickLevels; i++)
                {
                    float num = this.hTicks.GetStrengthOfLevel(i) * 0.9f;
                    Color c1 = new Color(1f, 1f, 1f, num / 0.5f) * textColor;
                    float[] ticksAtLevel = this.hTicks.GetTicksAtLevel(i, true);
                    for (int j = 0; j < ticksAtLevel.Length; j++)
                    {
                        if (ticksAtLevel[j] >= horRangeMin && ticksAtLevel[j] <= horRangeMax)
                        {
                            int num2 = Mathf.RoundToInt(ticksAtLevel[j] * frameRate);
                            float num3 = (!useEntireHeight) ? (position.height * Mathf.Min(1f, num) * 0.7f) : position.height;
                            // float x = this.FrameToPixel((float)num2, frameRate, position, shownArea);
                            float x = this.FrameToPixelForTimeRuler((float)num2, width, a, b);
                            TimeArea.DrawVerticalLineFast(x, position.height - num3 + 0.5f, position.height - 0.5f, c1);
                        }
                    }
                }
                GL.End();
            }
            if (labels)
            {
                int levelWithMinSeparation = this.hTicks.GetLevelWithMinSeparation(40f);
                float[] ticksAtLevel2 = this.hTicks.GetTicksAtLevel(levelWithMinSeparation, false);
                for (int k = 0; k < ticksAtLevel2.Length; k++)
                {
                    if (ticksAtLevel2[k] >= base.hRangeMin && ticksAtLevel2[k] <= base.hRangeMax)
                    {
                        int num4 = Mathf.RoundToInt(ticksAtLevel2[k] * frameRate);
                        float num5 = Mathf.Floor(this.FrameToPixel((float)num4, frameRate, position));
                        string text = this.FormatTime(ticksAtLevel2[k], frameRate, timeFormat);
                        GUI.Label(new Rect(num5 + 3f, -3f, 40f, 20f), text, TimeArea.styles.TimelineTick);
                    }
                }
            }
            GUI.EndGroup();
            GUI.backgroundColor = backgroundColor;
            GUI.color = color;
        }
        public static void DrawVerticalLine(float x, float minY, float maxY, Color color)
        {
            if (Event.current.type == EventType.Repaint)
            {
                // HandleUtility.ApplyWireMaterial();
                TimeAreaTools.ReflectionApplyWireMaterial();
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    GL.Begin(7);
                }
                else
                {
                    GL.Begin(1);
                }
                TimeArea.DrawVerticalLineFast(x, minY, maxY, color);
                GL.End();
            }
        }
        public static void DrawVerticalLineFast(float x, float minY, float maxY, Color color)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                GL.Color(color);
                GL.Vertex(new Vector3(x - 0.5f, minY, 0f));
                GL.Vertex(new Vector3(x + 0.5f, minY, 0f));
                GL.Vertex(new Vector3(x + 0.5f, maxY, 0f));
                GL.Vertex(new Vector3(x - 0.5f, maxY, 0f));
            }
            else
            {
                GL.Color(color);
                GL.Vertex(new Vector3(x, minY, 0f));
                GL.Vertex(new Vector3(x, maxY, 0f));
            }
        }
        public TimeArea.TimeRulerDragMode BrowseRuler(Rect position, ref float time, float frameRate, bool pickAnywhere, GUIStyle thumbStyle)
        {
            int controlID = GUIUtility.GetControlID(3126789, FocusType.Passive);
            return this.BrowseRuler(position, controlID, ref time, frameRate, pickAnywhere, thumbStyle);
        }
        public TimeArea.TimeRulerDragMode BrowseRuler(Rect position, int id, ref float time, float frameRate, bool pickAnywhere, GUIStyle thumbStyle)
        {
            Event current = Event.current;
            Rect position2 = position;
            if (time != -1f)
            {
                position2.x = Mathf.Round(base.TimeToPixel(time, position)) - (float)thumbStyle.overflow.left;
                position2.width = thumbStyle.fixedWidth + (float)thumbStyle.overflow.horizontal;
            }
            TimeArea.TimeRulerDragMode result;
            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (position2.Contains(current.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        TimeArea.s_PickOffset = current.mousePosition.x - base.TimeToPixel(time, position);
                        current.Use();
                        result = TimeArea.TimeRulerDragMode.Start;
                        return result;
                    }
                    if (pickAnywhere && position.Contains(current.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        float num = this.SnapTimeToWholeFPS(base.PixelToTime(current.mousePosition.x, position), frameRate);
                        TimeArea.s_OriginalTime = time;
                        if (num != time)
                        {
                            GUI.changed = true;
                        }
                        time = num;
                        TimeArea.s_PickOffset = 0f;
                        current.Use();
                        result = TimeArea.TimeRulerDragMode.Start;
                        return result;
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        current.Use();
                        result = TimeArea.TimeRulerDragMode.End;
                        return result;
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        float num2 = this.SnapTimeToWholeFPS(base.PixelToTime(current.mousePosition.x - TimeArea.s_PickOffset, position), frameRate);
                        if (num2 != time)
                        {
                            GUI.changed = true;
                        }
                        time = num2;
                        current.Use();
                        result = TimeArea.TimeRulerDragMode.Dragging;
                        return result;
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.hotControl == id && current.keyCode == KeyCode.Escape)
                    {
                        if (time != TimeArea.s_OriginalTime)
                        {
                            GUI.changed = true;
                        }
                        time = TimeArea.s_OriginalTime;
                        GUIUtility.hotControl = 0;
                        current.Use();
                        result = TimeArea.TimeRulerDragMode.Cancel;
                        return result;
                    }
                    break;
                case EventType.Repaint:
                    if (time != -1f)
                    {
                        bool flag = position.Contains(current.mousePosition);
                        position2.x += (float)thumbStyle.overflow.left;
                        thumbStyle.Draw(position2, id == GUIUtility.hotControl, flag || id == GUIUtility.hotControl, false, false);
                    }
                    break;
            }
            result = TimeArea.TimeRulerDragMode.None;
            return result;
        }
        private void DrawLine(Vector2 lhs, Vector2 rhs)
        {
            GL.Vertex(base.DrawingToViewTransformPoint(new Vector3(lhs.x, lhs.y, 0f)));
            GL.Vertex(base.DrawingToViewTransformPoint(new Vector3(rhs.x, rhs.y, 0f)));
        }
        private float FrameToPixel(float i, float frameRate, Rect rect, Rect theShownArea)
        {
            return (i - theShownArea.xMin * frameRate) * rect.width / (theShownArea.width * frameRate);
        }
        public float FrameToPixel(float i, float frameRate, Rect rect)
        {
            return this.FrameToPixel(i, frameRate, rect, base.shownArea);
        }

        public float FrameToPixelForTimeRuler(float i, float width, float a, float b)
        {
            return (i * width - a) / b;
        }
        //public float TimeField(Rect rect, int id, float time, float frameRate, TimeArea.TimeFormat timeFormat)
        //{
        //    float result;
        //    if (timeFormat == TimeArea.TimeFormat.None)
        //    {
        //        float time2 = EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor, rect, new Rect(0f, 0f, 0f, 0f), id, time, EditorGUI.kFloatFieldFormatString, EditorStyles.numberField, false);
        //        result = this.SnapTimeToWholeFPS(time2, frameRate);
        //    }
        //    else
        //    {
        //        if (timeFormat == TimeArea.TimeFormat.Frame)
        //        {
        //            int value = Mathf.RoundToInt(time * frameRate);
        //            int num = EditorGUI.DoIntField(EditorGUI.s_RecycledEditor, rect, new Rect(0f, 0f, 0f, 0f), id, value, EditorGUI.kIntFieldFormatString, EditorStyles.numberField, false, 0f);
        //            result = (float)num / frameRate;
        //        }
        //        else
        //        {
        //            string text = this.FormatTime(time, frameRate, TimeArea.TimeFormat.TimeFrame);
        //            string allowedletters = "0123456789.,:";
        //            bool flag;
        //            text = EditorGUI.DoTextField(EditorGUI.s_RecycledEditor, id, rect, text, EditorStyles.numberField, allowedletters, out flag, false, false, false);
        //            if (flag)
        //            {
        //                if (GUIUtility.keyboardControl == id)
        //                {
        //                    GUI.changed = true;
        //                    text = text.Replace(',', '.');
        //                    int num2 = text.IndexOf(':');
        //                    if (num2 >= 0)
        //                    {
        //                        string s = text.Substring(0, num2);
        //                        string s2 = text.Substring(num2 + 1);
        //                        int num3;
        //                        int num4;
        //                        if (int.TryParse(s, out num3) && int.TryParse(s2, out num4))
        //                        {
        //                            float num5 = (float)num3 + (float)num4 / frameRate;
        //                            result = num5;
        //                            return result;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        float time3;
        //                        if (float.TryParse(text, out time3))
        //                        {
        //                            result = this.SnapTimeToWholeFPS(time3, frameRate);
        //                            return result;
        //                        }
        //                    }
        //                }
        //            }
        //            result = time;
        //        }
        //    }
        //    return result;
        //}
        //public float ValueField(Rect rect, int id, float value)
        //{
        //    return EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor, rect, new Rect(0f, 0f, 0f, 0f), id, value, EditorGUI.kFloatFieldFormatString, EditorStyles.numberField, false);
        //}
        public string FormatTime(float time, float frameRate, TimeArea.TimeFormat timeFormat)
        {
            string result;
            if (timeFormat == TimeArea.TimeFormat.None)
            {
                int numberOfDecimalsForMinimumDifference;
                if (frameRate != 0f)
                {
                    numberOfDecimalsForMinimumDifference = this.ReflectionGetNumberOfDecimalsForMinimumDifference(1f / frameRate);
                }
                else
                {
                    numberOfDecimalsForMinimumDifference = this.ReflectionGetNumberOfDecimalsForMinimumDifference(base.shownArea.width / base.drawRect.width);
                }
                result = time.ToString("N" + numberOfDecimalsForMinimumDifference);
            }
            else
            {
                int num = Mathf.RoundToInt(time * frameRate);
                if (timeFormat == TimeArea.TimeFormat.TimeFrame)
                {
                    int length = ((int)frameRate).ToString().Length;
                    string str = string.Empty;
                    if (num < 0)
                    {
                        str = "-";
                        num = -num;
                    }
                    result = str + (num / (int)frameRate).ToString() + ":" + ((float)num % frameRate).ToString().PadLeft(length, '0');
                }
                else
                {
                    result = num.ToString();
                }
            }
            return result;
        }
        //public string FormatValue(float value)
        //{
        //    int numberOfDecimalsForMinimumDifference = MathUtils.GetNumberOfDecimalsForMinimumDifference(base.shownArea.height / base.drawRect.height);
        //    return value.ToString("N" + numberOfDecimalsForMinimumDifference);
        //}
        public float SnapTimeToWholeFPS(float time, float frameRate)
        {
            float result;
            if (frameRate == 0f)
            {
                result = time;
            }
            else
            {
                result = Mathf.Round(time * frameRate) / frameRate;
            }
            return result;
        }

        public int ReflectionGetNumberOfDecimalsForMinimumDifference(float miniDifference)
        {
            Type type = typeof(MathUtils);
            MethodInfo mth = type.GetMethod("GetNumberOfDecimalsForMinimumDifference", new Type[] { typeof(float) });
            object ret = mth.Invoke(null, new object[] { miniDifference });
            return (int)ret;
        }

        public int ReflectionGetNumberOfDecimalsForMinimumDifference(double miniDifference)
        {
            Type type = typeof(MathUtils);
            MethodInfo mth = type.GetMethod("GetNumberOfDecimalsForMinimumDifference", new Type[] { typeof(double) });
            object ret = mth.Invoke(null, new object[] { miniDifference });
            return (int)ret;
        }


        //ADD BY LITAO
        //获取当前画布的最小最大时间
        public float[] GetMinMaxAreaTime(float frameRate)
        {
            int levelWithMinSeparation = this.hTicks.GetLevelWithMinSeparation(40f);
            float[] ticksAtLevel2 = this.hTicks.GetTicksAtLevel(levelWithMinSeparation, false);
            return new float[2] { ticksAtLevel2[0], ticksAtLevel2[ticksAtLevel2.Length - 1] };
        }
    }
}
