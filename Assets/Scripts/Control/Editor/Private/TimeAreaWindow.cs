using System;
using UnityEngine;
using UnityEditor;

namespace DMTimeArea
{
    public abstract class TimeAreaWindow : EditorWindow
    {
        protected TimeArea m_TimeArea;
        public TimeArea TimeArea
        {
            get { return m_TimeArea; }
        }

        private static readonly float kTimeAreaMinWidth = 50f;

        protected const float ARROW_WIDTH = 6f;
        protected const int TIMELINETIMELABEL_HEIGHT = 18;
        private const int TIMERULER_HEIGHT = 28;

        protected virtual int timeRulerHeight
        {
            get { return TIMERULER_HEIGHT; }
        }

        protected virtual int toolbarHeight
        {
            get
            {
                return 18;
            }
        }

        public abstract Rect _rectTimeAreaTotal
        {
            get;
        }

        public abstract Rect _rectTimeAreaContent
        {
            get;
        }

        public abstract Rect _rectTimeAreaRuler
        {
            get;
        }

        protected virtual double RunningTime
        {
            get { return 0f; }
            set { }
        }

        protected abstract bool IsLockedMoveFrame
        {
            get;
        }

        protected abstract bool IsLockDragHeaderArrow
        {
            get;
        }

        protected abstract float sequencerHeaderWidth
        {
            get;
        }

        public Rect _timeAreaBounds
        {
            get
            {
                float width = base.position.width - sequencerHeaderWidth;
                return new Rect(_rectTimeAreaContent.x, _rectTimeAreaContent.y, Mathf.Max(width, kTimeAreaMinWidth), _rectTimeAreaContent.height);
            }
        }

        //
        // Time Area settings
        //
        // Frame rate
        public float _frameRate = 30f;
        // Frame Snap
        public bool _frameSnap = true;
        // Time ruler format
        public bool _timeInFrames = false;

        public int RunningFrame
        {
            get
            {
                return TimeUtility.ToFrames(this.RunningTime, (double)this._frameRate);
            }
            set
            {
                this.RunningTime = (float)TimeUtility.FromFrames(Mathf.Max(0, value), (double)this._frameRate);
            }
        }

        public void PreviousTimeFrame()
        {
            if (!IsLockedMoveFrame)
            {
                this.RunningFrame--;
            }
        }

        public void NextTimeFrame()
        {
            if (!IsLockedMoveFrame)
            {
                this.RunningFrame++;
            }
        }

        public Vector2 TimeAreaScale
        {
            get
            {
                return this.m_TimeArea.scale;
            }
        }

        public Vector2 TimeAreaTranslation
        {
            get
            {
                return this.m_TimeArea.translation;
            }
        }

        public Vector2 TimeAreaTimeShownRange
        {
            get
            {
                float x = PixelToTime(_timeAreaBounds.xMin);
                float y = PixelToTime(_timeAreaBounds.xMax);
                return new Vector2(x, y);
            }
        }

        protected virtual void DrawTimeAreaBackGround()
        {
            GUI.Box(_rectTimeAreaContent, GUIContent.none, new GUIStyle("CurveEditorBackground"));
            // EditorGUI.DrawRect(_rectTimeAreaContent, new Color(0.16f, 0.16f, 0.16f, 1f));
            // EditorGUI.DrawRect(_rectTimeAreaContent, DMTimeLineStyles)
            m_TimeArea.mRect = this._timeAreaBounds;
            m_TimeArea.BeginViewGUI();
            m_TimeArea.SetTickMarkerRanges();
            m_TimeArea.DrawMajorTicks(this._rectTimeAreaTotal, (float)_frameRate);
            // DrawVerticalTickLine();
            m_TimeArea.EndViewGUI();

            // Mouse Event for zoom area
            m_TimeArea.OnAreaEvent();
        }

        protected virtual void DrawVerticalTickLine()
        {
        }

        protected void DrawTimeCodeGUI()
        {
            string text;
            if (m_TimeArea != null)
            {
                double time01 = this.RunningTime;
                text = this.TimeAsString(time01, "F2");
                bool flag = TimeUtility.OnFrameBoundary(time01, (double)this._frameRate);
                if (this._timeInFrames)
                {
                    if (flag)
                    {
                        text = RunningFrame.ToString();
                    }
                    else
                        text = TimeUtility.ToExactFrames(time01, (double)this._frameRate).ToString("F2");
                }
            }
            else
                text = "0";
            EditorGUI.BeginChangeCheck();
            string text2 = EditorGUILayout.DelayedTextField(text, EditorStyles.toolbarTextField, new GUILayoutOption[]
            {
                GUILayout.Width(70f)
            });

            bool flag2 = EditorGUI.EndChangeCheck();
            if (flag2)
            {
                if (_timeInFrames)
                {
                    int frame = RunningFrame;
                    double d = 0.0;
                    if (double.TryParse(text2, out d))
                        frame = Math.Max(0, (int)Math.Floor(d));

                    RunningFrame = frame;
                }
                else
                {
                    double num = TimeUtility.ParseTimeCode(text2, (double)this._frameRate, -1.0);
                    if (num > 0.0)
                    {
                        RunningTime = (float)num;
                    }
                }
            }
        }

        public float TimeToTimeAreaPixel(double time)
        {
            float num = (float)time;
            num *= this.TimeAreaScale.x;
            return num + (this.TimeAreaTranslation.x + this.sequencerHeaderWidth);
        }

        public float TimeToScreenSpacePixel(double time)
        {
            float num = (float)time;
            num *= this.TimeAreaScale.x;
            return num + this.TimeAreaTranslation.x;
        }

        public string TimeAsString(double timeValue, string format = "F2")
        {
            string result;
            if (this._timeInFrames)
            {
                result = TimeUtility.TimeAsFrames(timeValue, (double)this._frameRate, format);
            }
            else
            {
                result = TimeUtility.TimeAsTimeCode(timeValue, (double)this._frameRate, format);
            }
            return result;
        }

        public float TimeToPixel(double time)
        {
            return m_TimeArea.TimeToPixel((float)time, _timeAreaBounds);
        }

        public float TimeToPixel(double time, float rectWidth, float rectX, float x, float y, float width)
        {
            return m_TimeArea.TimeToPixel((float)time, rectWidth, rectX, x, y, width);
        }

        public float YToPixel(float y)
        {
            return m_TimeArea.YToPixel(y, _timeAreaBounds);
        }

        public float PixelToY(float pixel)
        {
            return m_TimeArea.PixelToY(pixel);
        }

        public float PixelToTime(float pixel)
        {
            return m_TimeArea.PixelToTime(pixel, _timeAreaBounds);
        }

        public float TimeAreaPixelToTime(float pixel)
        {
            return this.PixelToTime(pixel);
        }

        public double GetSnappedTimeAtMousePosition(Vector2 mousePos)
        {
            return this.SnapToFrameIfRequired((double)this.ScreenSpacePixelToTimeAreaTime(mousePos.x));
        }

        public double SnapToFrameIfRequired(double time)
        {
            double result;
            if (this._frameSnap)
            {
                result = TimeUtility.FromFrames(TimeUtility.ToFrames(time, (double)this._frameRate), (double)this._frameRate);
            }
            else
            {
                result = time;
            }
            return result;
        }

        public float ScreenSpacePixelToTimeAreaTime(float p)
        {
            p -= this._timeAreaBounds.x;
            return this.TrackSpacePixelToTimeAreaTime(p);
        }

        public float TrackSpacePixelToTimeAreaTime(float p)
        {
            p -= this.TimeAreaTranslation.x;
            float result;
            if (this.TimeAreaScale.x > 0f)
            {
                result = p / this.TimeAreaScale.x;
            }
            else
            {
                result = p;
            }
            return result;
        }

        // Change params to hrangelocked and vrangelocked
        protected void InitTimeArea(
            bool hLocked = false,
            bool vLocked = true,
            bool showhSlider = false,
            bool showVSlider = false)
        {
            if (m_TimeArea == null)
            {
                // create new timeArea
                this.m_TimeArea = new TimeArea(false, true)
                {
                    hRangeLocked = hLocked,
                    vRangeLocked = vLocked,
                    margin = 10f,
                    scaleWithWindow = true,
                    hSlider = showhSlider,
                    vSlider = showVSlider,
                    hRangeMin = 0f,
                    vRangeMin = float.NegativeInfinity,
                    vRangeMax = float.PositiveInfinity,
                    mRect = _timeAreaBounds,
                };
                this.m_TimeArea.hTicks.SetTickModulosForFrameRate(this._frameRate);
                // show time range begin seconds to end seconds(xxs - xxs)
                this.m_TimeArea.SetShownHRange(-1, 5f);
                this.m_TimeArea.SetShownVRange(0, 100f);
            }
        }

        protected void DrawTimeRulerArea()
        {
            //
            // Time ruler
            //
            m_TimeArea.TimeRuler(_rectTimeAreaRuler, _frameRate, true, false, 1f, _timeInFrames ? TimeArea.TimeFormat.Frame : TimeArea.TimeFormat.TimeFrame);

            //
            // Draw Current Running Time Cursor and red guide line
            //
            GUILayout.BeginArea(_rectTimeAreaTotal, string.Empty/*, EditorStyles.toolbarButton*/);
            Color cl01 = GUI.color;
            GUI.color = Color.white;
            float timeToPos = TimeToPixel(this.RunningTime);
            GUI.DrawTexture(new Rect(-ARROW_WIDTH + timeToPos - _rectTimeAreaRuler.x, 2, ARROW_WIDTH * 2f, ARROW_WIDTH * 2f * 1.82f), ResManager.TimeHeadTexture);
            GUI.color = cl01;
            Rect lineRect = new Rect(timeToPos - _rectTimeAreaRuler.x, TIMELINETIMELABEL_HEIGHT, 1, _rectTimeAreaContent.height + 6);
            EditorGUI.DrawRect(lineRect, Color.white);
            GUILayout.EndArea();
        }

        protected void HandleTimeRulerCursorInput()
        {
            //
            // Drag running time guide line
            //
            var evt = Event.current;
            var mousePos = evt.mousePosition;

            int redControlId = GUIUtility.GetControlID(kRedCursorControlID, FocusType.Passive);

            if (!Application.isPlaying)
            {
                switch (evt.GetTypeForControl(redControlId))
                {
                    case EventType.MouseDown:
                        {
                            if (_rectTimeAreaRuler.Contains(mousePos))
                            {
                                GUIUtility.hotControl = redControlId;
                                evt.Use();
                                double fTime = GetSnappedTimeAtMousePosition(mousePos);
                                if (fTime <= 0)
                                    fTime = 0.0;
                                this.RunningTime = fTime;
                                HandleTimeUpdate(fTime);
                            }
                        }
                        break;
                    case EventType.MouseDrag:
                        {
                            if (GUIUtility.hotControl == redControlId)
                            {
                                if (!IsLockDragHeaderArrow)
                                {
                                    double fTime = GetSnappedTimeAtMousePosition(mousePos);
                                    if (fTime <= 0)
                                        fTime = 0.0;
                                    this.RunningTime = fTime;
                                    HandleTimeUpdate(fTime);
                                }
                            }
                        }
                        break;
                    default: break;
                }
            }
        }

        protected abstract void HandleTimeUpdate(double runningTime);

        private static int kRedCursorControlID = "RedCursorControlRect".GetHashCode();

        protected void DrawLineWithTipsRectByTime(double fTime, float offSet, float yPos, bool dotLine, Color color)
        {
            float timeToPos = TimeToPixel(fTime);
            Rect drawRect = new Rect(timeToPos - offSet, yPos, 1, _rectTimeAreaContent.height + 15);
            float num = drawRect.y;
            Vector3 p = new Vector3(drawRect.x, num, 0f);
            Vector3 p2 = new Vector3(drawRect.x, num + Mathf.Min(drawRect.height, _rectTimeAreaTotal.height), 0f);
            if (true)
            {
                if (dotLine)
                {
                    TimeAreaTools.DrawDottedLine(p, p2, 5f, color);
                }
                else
                {
                    // Rect rect2 = Rect.MinMaxRect(p.x - 0.5f, p.y, p2.x + 0.5f, p2.y);
                    EditorGUI.DrawRect(drawRect, color);
                }
            }

            // Draw time ruler
            GUIStyle TimelineTick = "AnimationTimelineTick";
            string beginTime = TimeAsString(fTime);
            var lb = new GUIContent(beginTime);
            Vector2 size = TimelineTick.CalcSize(lb);
            Color pre = GUI.color;
            GUI.color = Color.white;
            var rectTip = new Rect(timeToPos - offSet, yPos, size.x, size.y);
            rectTip.x -= 4;
            rectTip.width += 8;
            GUI.Box(rectTip, GUIContent.none, "Button");
            rectTip.y = yPos - 3;
            rectTip.x += 4;
            rectTip.width -= 8;
            GUI.color = pre;
            GUI.Label(rectTip, lb, TimelineTick);
        }

        //
        // Key for Frame Movement
        //
        public delegate void UserInputKeyCodeHandler(bool ctrl, KeyCode code);
        private event UserInputKeyCodeHandler OnUserInputKeyCode;

        protected void RegisterInputKeyCodeHandler(UserInputKeyCodeHandler handler)
        {
            if (handler != null)
            {
                OnUserInputKeyCode += handler;
            }
        }

        protected void UnRegisterInputKeyCodeHandler(UserInputKeyCodeHandler handler)
        {
            if (handler != null)
            {
                OnUserInputKeyCode -= handler;
            }
        }

        protected void OnUserInput(Event evt)
        {
            if (evt.type == EventType.KeyDown)
            {
                if (!evt.control && evt.keyCode == KeyCode.P)
                {
                    if (OnUserInputKeyCode != null)
                        OnUserInputKeyCode(false, KeyCode.P);
                }
            }

            if (evt.control)
            {
                if (evt.type == EventType.KeyDown)
                {
                    if (OnUserInputKeyCode != null)
                    {
                        OnUserInputKeyCode(true, evt.keyCode);
                    }
                }
            }
        }

        private void ChangeTimeCode(object obj)
        {
            string a = obj.ToString();
            if (a == "frames")
            {
                _timeInFrames = true;
            }
            else
            {
                _timeInFrames = false;
            }
        }

        protected void OnClickSettingButton()
        {
            GenericMenu genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("Seconds"), !_timeInFrames, new GenericMenu.MenuFunction2(this.ChangeTimeCode), "seconds");
            genericMenu.AddItem(new GUIContent("Frames"), _timeInFrames, new GenericMenu.MenuFunction2(this.ChangeTimeCode), "frames");
            genericMenu.AddSeparator("");
            genericMenu.AddDisabledItem(new GUIContent("Frame rate"));
            genericMenu.AddItem(new GUIContent("Film (24)"), _frameRate.Equals(24f), delegate (object r)
            {
                this._frameRate = (float)r;
            }, 24f);
            //genericMenu.AddItem(new GUIContent("PAL (25)"), _frameRate.Equals(25f), delegate (object r)
            //{
            //    _frameRate = (float)r;
            //}, 25f);
            //genericMenu.AddItem(new GUIContent("NTSC (29.97)"), _frameRate.Equals(29.97f), delegate (object r)
            //{
            //    _frameRate = (float)r;
            //}, 29.97f);
            genericMenu.AddItem(new GUIContent("30"), _frameRate.Equals(30f), delegate (object r)
            {
                _frameRate = (float)r;
            }, 30f);
            genericMenu.AddItem(new GUIContent("50"), _frameRate.Equals(50f), delegate (object r)
            {
                _frameRate = (float)r;
            }, 50f);
            genericMenu.AddItem(new GUIContent("60"), _frameRate.Equals(60f), delegate (object r)
            {
                _frameRate = (float)r;
            }, 60f);
            genericMenu.AddDisabledItem(new GUIContent("Custom"));
            genericMenu.AddSeparator("");
            genericMenu.AddItem(new GUIContent("Snap to Frame"), this._frameSnap, delegate
            {
                this._frameSnap = !this._frameSnap;
            });

            OnCreateSettingContent(genericMenu);
            genericMenu.ShowAsContext();
        }

        protected virtual void OnCreateSettingContent(GenericMenu menu)
        {

        }
    }
}
