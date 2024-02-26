using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using System.Reflection;
using EditorGUIExt = DMTimeArea.EditorGUIExt;

namespace DMTimeArea
{
    public class ZoomableArea
    {
        public enum YDirection
        {
            Positive,
            Negative,
        }

        public class Styles
        {
            public GUIStyle horizontalScrollbar;
            public GUIStyle horizontalMinMaxScrollbarThumb;
            public GUIStyle horizontalScrollbarLeftButton;
            public GUIStyle horizontalScrollbarRightButton;
            public GUIStyle verticalScrollbar;
            public GUIStyle verticalMinMaxScrollbarThumb;
            public GUIStyle verticalScrollbarUpButton;
            public GUIStyle verticalScrollbarDownButton;
            public float sliderWidth;
            public float visualSliderWidth;
            public Styles(bool minimalGUI)
            {
                if (minimalGUI)
                {
                    this.visualSliderWidth = 0f;
                    this.sliderWidth = 15f;
                }
                else
                {
                    this.visualSliderWidth = 15f;
                    this.sliderWidth = 15f;
                }
            }
            public void InitGUIStyles(bool minimalGUI, bool enableSliderZoom)
            {
                if (minimalGUI)
                {
                    this.horizontalMinMaxScrollbarThumb = ((!enableSliderZoom) ? "MiniSliderhorizontal" : "MiniMinMaxSliderHorizontal");
                    this.horizontalScrollbarLeftButton = GUIStyle.none;
                    this.horizontalScrollbarRightButton = GUIStyle.none;
                    this.horizontalScrollbar = GUIStyle.none;
                    this.verticalMinMaxScrollbarThumb = ((!enableSliderZoom) ? "MiniSliderVertical" : "MiniMinMaxSlidervertical");
                    this.verticalScrollbarUpButton = GUIStyle.none;
                    this.verticalScrollbarDownButton = GUIStyle.none;
                    this.verticalScrollbar = GUIStyle.none;
                }
                else
                {
                    this.horizontalMinMaxScrollbarThumb = ((!enableSliderZoom) ? "horizontalscrollbarthumb" : "horizontalMinMaxScrollbarThumb");
                    this.horizontalScrollbarLeftButton = "horizontalScrollbarLeftbutton";
                    this.horizontalScrollbarRightButton = "horizontalScrollbarRightbutton";
                    this.horizontalScrollbar = GUI.skin.horizontalScrollbar;
                    this.verticalMinMaxScrollbarThumb = ((!enableSliderZoom) ? "verticalscrollbarthumb" : "verticalMinMaxScrollbarThumb");
                    this.verticalScrollbarUpButton = "verticalScrollbarUpbutton";
                    this.verticalScrollbarDownButton = "verticalScrollbarDownbutton";
                    this.verticalScrollbar = GUI.skin.verticalScrollbar;
                }
            }
        }

        private static Vector2 m_MouseDownPosition = new Vector2(-1000000f, -1000000f);
        private static int zoomableAreaHash = "ZoomableArea".GetHashCode();
        [SerializeField]
        private bool m_HRangeLocked;
        [SerializeField]
        private bool m_VRangeLocked;
        [SerializeField]
        private float m_HBaseRangeMin = 0f;
        [SerializeField]
        private float m_HBaseRangeMax = 1f;
        [SerializeField]
        private float m_VBaseRangeMin = 0f;
        [SerializeField]
        private float m_VBaseRangeMax = 1f;
        [SerializeField]
        private bool m_HAllowExceedBaseRangeMin = true;
        [SerializeField]
        private bool m_HAllowExceedBaseRangeMax = true;
        [SerializeField]
        private bool m_VAllowExceedBaseRangeMin = true;
        [SerializeField]
        private bool m_VAllowExceedBaseRangeMax = true;
        private const float kMinScale = 1E-05f;
        private const float kMaxScale = 100000f;
        private float m_HScaleMin = 1E-05f;
        private float m_HScaleMax = 100000f;
        private float m_VScaleMin = 1E-05f;
        private float m_VScaleMax = 100000f;
        private const float kMinWidth = 0.1f;
        private const float kMinHeight = 0.1f;
        [SerializeField]
        private bool m_ScaleWithWindow = false;
        [SerializeField]
        private bool m_HSlider = true;
        [SerializeField]
        private bool m_VSlider = true;
        [SerializeField]
        private bool m_IgnoreScrollWheelUntilClicked = false;
        [SerializeField]
        private bool m_EnableMouseInput = true;
        [SerializeField]
        private bool m_EnableSliderZoom = true;
        public bool m_UniformScale;
        [SerializeField]
        private ZoomableArea.YDirection m_UpDirection = ZoomableArea.YDirection.Positive;
        [SerializeField]
        private Rect m_DrawArea = new Rect(0f, 0f, 100f, 100f);
        [SerializeField]
        internal Vector2 m_Scale = new Vector2(1f, -1f);
        [SerializeField]
        internal Vector2 m_Translation = new Vector2(0f, 0f);
        [SerializeField]
        private float m_MarginLeft;
        [SerializeField]
        private float m_MarginRight;
        [SerializeField]
        private float m_MarginTop;
        [SerializeField]
        private float m_MarginBottom;
        [SerializeField]
        private Rect m_LastShownAreaInsideMargins = new Rect(0f, 0f, 100f, 100f);
        private int verticalScrollbarID;
        private int horizontalScrollbarID;
        [SerializeField]
        private bool m_MinimalGUI;
        private ZoomableArea.Styles m_Styles;
        public bool hRangeLocked
        {
            get
            {
                return this.m_HRangeLocked;
            }
            set
            {
                this.m_HRangeLocked = value;
            }
        }
        public bool vRangeLocked
        {
            get
            {
                return this.m_VRangeLocked;
            }
            set
            {
                this.m_VRangeLocked = value;
            }
        }
        public float hBaseRangeMin
        {
            get
            {
                return this.m_HBaseRangeMin;
            }
            set
            {
                this.m_HBaseRangeMin = value;
            }
        }
        public float hBaseRangeMax
        {
            get
            {
                return this.m_HBaseRangeMax;
            }
            set
            {
                this.m_HBaseRangeMax = value;
            }
        }
        public float vBaseRangeMin
        {
            get
            {
                return this.m_VBaseRangeMin;
            }
            set
            {
                this.m_VBaseRangeMin = value;
            }
        }
        public float vBaseRangeMax
        {
            get
            {
                return this.m_VBaseRangeMax;
            }
            set
            {
                this.m_VBaseRangeMax = value;
            }
        }
        public bool hAllowExceedBaseRangeMin
        {
            get
            {
                return this.m_HAllowExceedBaseRangeMin;
            }
            set
            {
                this.m_HAllowExceedBaseRangeMin = value;
            }
        }
        public bool hAllowExceedBaseRangeMax
        {
            get
            {
                return this.m_HAllowExceedBaseRangeMax;
            }
            set
            {
                this.m_HAllowExceedBaseRangeMax = value;
            }
        }
        public bool vAllowExceedBaseRangeMin
        {
            get
            {
                return this.m_VAllowExceedBaseRangeMin;
            }
            set
            {
                this.m_VAllowExceedBaseRangeMin = value;
            }
        }
        public bool vAllowExceedBaseRangeMax
        {
            get
            {
                return this.m_VAllowExceedBaseRangeMax;
            }
            set
            {
                this.m_VAllowExceedBaseRangeMax = value;
            }
        }
        public float hRangeMin
        {
            get
            {
                return (!this.hAllowExceedBaseRangeMin) ? this.hBaseRangeMin : float.NegativeInfinity;
            }
            set
            {
                this.SetAllowExceed(ref this.m_HBaseRangeMin, ref this.m_HAllowExceedBaseRangeMin, value);
            }
        }
        public float hRangeMax
        {
            get
            {
                return (!this.hAllowExceedBaseRangeMax) ? this.hBaseRangeMax : float.PositiveInfinity;
            }
            set
            {
                this.SetAllowExceed(ref this.m_HBaseRangeMax, ref this.m_HAllowExceedBaseRangeMax, value);
            }
        }
        public float vRangeMin
        {
            get
            {
                return (!this.vAllowExceedBaseRangeMin) ? this.vBaseRangeMin : float.NegativeInfinity;
            }
            set
            {
                this.SetAllowExceed(ref this.m_VBaseRangeMin, ref this.m_VAllowExceedBaseRangeMin, value);
            }
        }
        public float vRangeMax
        {
            get
            {
                return (!this.vAllowExceedBaseRangeMax) ? this.vBaseRangeMax : float.PositiveInfinity;
            }
            set
            {
                this.SetAllowExceed(ref this.m_VBaseRangeMax, ref this.m_VAllowExceedBaseRangeMax, value);
            }
        }
        public float hScaleMin
        {
            get
            {
                return this.m_HScaleMin;
            }
            set
            {
                this.m_HScaleMin = Mathf.Clamp(value, 1E-05f, 100000f);
            }
        }
        public float hScaleMax
        {
            get
            {
                return this.m_HScaleMax;
            }
            set
            {
                this.m_HScaleMax = Mathf.Clamp(value, 1E-05f, 100000f);
            }
        }
        public float vScaleMin
        {
            get
            {
                return this.m_VScaleMin;
            }
            set
            {
                this.m_VScaleMin = Mathf.Clamp(value, 1E-05f, 100000f);
            }
        }
        public float vScaleMax
        {
            get
            {
                return this.m_VScaleMax;
            }
            set
            {
                this.m_VScaleMax = Mathf.Clamp(value, 1E-05f, 100000f);
            }
        }
        public bool scaleWithWindow
        {
            get
            {
                return this.m_ScaleWithWindow;
            }
            set
            {
                this.m_ScaleWithWindow = value;
            }
        }
        public bool hSlider
        {
            get
            {
                return this.m_HSlider;
            }
            set
            {
                Rect rect = this.mRect;
                this.m_HSlider = value;
                this.mRect = rect;
            }
        }
        public bool vSlider
        {
            get
            {
                return this.m_VSlider;
            }
            set
            {
                Rect rect = this.mRect;
                this.m_VSlider = value;
                this.mRect = rect;
            }
        }
        public bool ignoreScrollWheelUntilClicked
        {
            get
            {
                return this.m_IgnoreScrollWheelUntilClicked;
            }
            set
            {
                this.m_IgnoreScrollWheelUntilClicked = value;
            }
        }
        public bool enableMouseInput
        {
            get
            {
                return this.m_EnableMouseInput;
            }
            set
            {
                this.m_EnableMouseInput = value;
            }
        }
        public bool uniformScale
        {
            get
            {
                return this.m_UniformScale;
            }
            set
            {
                this.m_UniformScale = value;
            }
        }
        public ZoomableArea.YDirection upDirection
        {
            get
            {
                return this.m_UpDirection;
            }
            set
            {
                if (this.m_UpDirection != value)
                {
                    this.m_UpDirection = value;
                    this.m_Scale.y = -this.m_Scale.y;
                }
            }
        }
        public Vector2 scale
        {
            get
            {
                return this.m_Scale;
            }
        }

        public Vector2 translation
        {
            get
            {
                return this.m_Translation;
            }
            set
            {
                this.m_Translation = value;
            }
        }
        public float margin
        {
            set
            {
                this.m_MarginBottom = value;
                this.m_MarginTop = value;
                this.m_MarginRight = value;
                this.m_MarginLeft = value;
            }
        }
        public float leftmargin
        {
            get
            {
                return this.m_MarginLeft;
            }
            set
            {
                this.m_MarginLeft = value;
            }
        }
        public float rightmargin
        {
            get
            {
                return this.m_MarginRight;
            }
            set
            {
                this.m_MarginRight = value;
            }
        }
        public float topmargin
        {
            get
            {
                return this.m_MarginTop;
            }
            set
            {
                this.m_MarginTop = value;
            }
        }
        public float bottommargin
        {
            get
            {
                return this.m_MarginBottom;
            }
            set
            {
                this.m_MarginBottom = value;
            }
        }
        private ZoomableArea.Styles styles
        {
            get
            {
                if (this.m_Styles == null)
                {
                    this.m_Styles = new ZoomableArea.Styles(this.m_MinimalGUI);
                }
                return this.m_Styles;
            }
        }
        public Rect mRect
        {
            get
            {
                return new Rect(this.drawRect.x, this.drawRect.y, this.drawRect.width + ((!this.m_VSlider) ? 0f : this.styles.visualSliderWidth), this.drawRect.height + ((!this.m_HSlider) ? 0f : this.styles.visualSliderWidth));
            }
            set
            {
                Rect rect = new Rect(value.x, value.y, value.width - ((!this.m_VSlider) ? 0f : this.styles.visualSliderWidth), value.height - ((!this.m_HSlider) ? 0f : this.styles.visualSliderWidth));
                if (rect != this.m_DrawArea)
                {
                    if (this.m_ScaleWithWindow)
                    {
                        this.m_DrawArea = rect;
                        this.shownAreaInsideMargins = this.m_LastShownAreaInsideMargins;
                    }
                    else
                    {
                        this.m_Translation += new Vector2((rect.width - this.m_DrawArea.width) / 2f, (rect.height - this.m_DrawArea.height) / 2f);
                        this.m_DrawArea = rect;
                    }
                }
                this.EnforceScaleAndRange();
            }
        }
        public Rect drawRect
        {
            get
            {
                return this.m_DrawArea;
            }
        }
        public Rect shownArea
        {
            get
            {
                Rect result;
                if (this.m_UpDirection == ZoomableArea.YDirection.Positive)
                {
                    result = new Rect(-this.m_Translation.x / this.m_Scale.x, -(this.m_Translation.y - this.drawRect.height) / this.m_Scale.y, this.m_DrawArea.width / this.m_Scale.x, this.m_DrawArea.height / -this.m_Scale.y);
                }
                else
                {
                    result = new Rect(-this.m_Translation.x / this.m_Scale.x, -this.m_Translation.y / this.m_Scale.y, this.m_DrawArea.width / this.m_Scale.x, this.m_DrawArea.height / this.m_Scale.y);
                }
                return result;
            }
            set
            {
                float num = (value.width >= 0.1f) ? value.width : 0.1f;
                float num2 = (value.height >= 0.1f) ? value.height : 0.1f;
                if (this.m_UpDirection == ZoomableArea.YDirection.Positive)
                {
                    this.m_Scale.x = this.drawRect.width / num;
                    this.m_Scale.y = -this.drawRect.height / num2;
                    this.m_Translation.x = -value.x * this.m_Scale.x;
                    this.m_Translation.y = this.drawRect.height - value.y * this.m_Scale.y;
                }
                else
                {
                    this.m_Scale.x = this.drawRect.width / num;
                    this.m_Scale.y = this.drawRect.height / num2;
                    this.m_Translation.x = -value.x * this.m_Scale.x;
                    this.m_Translation.y = -value.y * this.m_Scale.y;
                }
                this.EnforceScaleAndRange();
            }
        }
        public Rect shownAreaInsideMargins
        {
            get
            {
                return this.shownAreaInsideMarginsInternal;
            }
            set
            {
                this.shownAreaInsideMarginsInternal = value;
                this.EnforceScaleAndRange();
            }
        }
        private Rect shownAreaInsideMarginsInternal
        {
            get
            {
                float num = this.leftmargin / this.m_Scale.x;
                float num2 = this.rightmargin / this.m_Scale.x;
                float num3 = this.topmargin / this.m_Scale.y;
                float num4 = this.bottommargin / this.m_Scale.y;
                Rect shownArea = this.shownArea;
                shownArea.x += num;
                shownArea.y -= num3;
                shownArea.width -= num + num2;
                shownArea.height += num3 + num4;
                return shownArea;
            }
            set
            {
                float num = (value.width >= 0.1f) ? value.width : 0.1f;
                float num2 = (value.height >= 0.1f) ? value.height : 0.1f;
                float num3 = this.drawRect.width - this.leftmargin - this.rightmargin;
                if (num3 < 0.1f)
                {
                    num3 = 0.1f;
                }
                float num4 = this.drawRect.height - this.topmargin - this.bottommargin;
                if (num4 < 0.1f)
                {
                    num4 = 0.1f;
                }
                if (this.m_UpDirection == ZoomableArea.YDirection.Positive)
                {
                    this.m_Scale.x = num3 / num;
                    this.m_Scale.y = -num4 / num2;
                    this.m_Translation.x = -value.x * this.m_Scale.x + this.leftmargin;
                    this.m_Translation.y = this.drawRect.height - value.y * this.m_Scale.y - this.topmargin;
                }
                else
                {
                    this.m_Scale.x = num3 / num;
                    this.m_Scale.y = num4 / num2;
                    this.m_Translation.x = -value.x * this.m_Scale.x + this.leftmargin;
                    this.m_Translation.y = -value.y * this.m_Scale.y + this.topmargin;
                }
            }
        }
        public virtual Bounds drawingBounds
        {
            get
            {
                return new Bounds(new Vector3((this.hBaseRangeMin + this.hBaseRangeMax) * 0.5f, (this.vBaseRangeMin + this.vBaseRangeMax) * 0.5f, 0f), new Vector3(this.hBaseRangeMax - this.hBaseRangeMin, this.vBaseRangeMax - this.vBaseRangeMin, 1f));
            }
        }
        public Matrix4x4 drawingToViewMatrix
        {
            get
            {
                return Matrix4x4.TRS(this.m_Translation, Quaternion.identity, new Vector3(this.m_Scale.x, this.m_Scale.y, 1f));
            }
        }
        public Vector2 mousePositionInDrawing
        {
            get
            {
                return this.ViewToDrawingTransformPoint(Event.current.mousePosition);
            }
        }
        public ZoomableArea()
        {
            this.m_MinimalGUI = false;
        }
        public ZoomableArea(bool minimalGUI)
        {
            this.m_MinimalGUI = minimalGUI;
        }
        public ZoomableArea(bool minimalGUI, bool enableSliderZoom)
        {
            this.m_MinimalGUI = minimalGUI;
            this.m_EnableSliderZoom = enableSliderZoom;
        }
        private void SetAllowExceed(ref float rangeEnd, ref bool allowExceed, float value)
        {
            if (value == float.NegativeInfinity || value == float.PositiveInfinity)
            {
                rangeEnd = (float)((value != float.NegativeInfinity) ? 1 : 0);
                allowExceed = true;
            }
            else
            {
                rangeEnd = value;
                allowExceed = false;
            }
        }
        internal void SetDrawRectHack(Rect r)
        {
            this.m_DrawArea = r;
        }
        public void SetShownHRangeInsideMargins(float min, float max)
        {
            float num = this.drawRect.width - this.leftmargin - this.rightmargin;
            if (num < 0.1f)
            {
                num = 0.1f;
            }
            float num2 = max - min;
            if (num2 < 0.1f)
            {
                num2 = 0.1f;
            }
            this.m_Scale.x = num / num2;
            this.m_Translation.x = -min * this.m_Scale.x + this.leftmargin;
            this.EnforceScaleAndRange();
        }
        public void SetShownHRange(float min, float max)
        {
            float num = max - min;
            if (num < 0.1f)
            {
                num = 0.1f;
            }
            this.m_Scale.x = this.drawRect.width / num;
            this.m_Translation.x = -min * this.m_Scale.x;
            this.EnforceScaleAndRange();
        }
        public void SetShownVRangeInsideMargins(float min, float max)
        {
            if (this.m_UpDirection == ZoomableArea.YDirection.Positive)
            {
                this.m_Scale.y = -(this.drawRect.height - this.topmargin - this.bottommargin) / (max - min);
                this.m_Translation.y = this.drawRect.height - min * this.m_Scale.y - this.topmargin;
            }
            else
            {
                this.m_Scale.y = (this.drawRect.height - this.topmargin - this.bottommargin) / (max - min);
                this.m_Translation.y = -min * this.m_Scale.y - this.bottommargin;
            }
            this.EnforceScaleAndRange();
        }
        public void SetShownVRange(float min, float max)
        {
            if (this.m_UpDirection == ZoomableArea.YDirection.Positive)
            {
                this.m_Scale.y = -this.drawRect.height / (max - min);
                this.m_Translation.y = this.drawRect.height - min * this.m_Scale.y;
            }
            else
            {
                this.m_Scale.y = this.drawRect.height / (max - min);
                this.m_Translation.y = -min * this.m_Scale.y;
            }
            this.EnforceScaleAndRange();
        }
        public Vector2 DrawingToViewTransformPoint(Vector2 lhs)
        {
            return new Vector2(lhs.x * this.m_Scale.x + this.m_Translation.x, lhs.y * this.m_Scale.y + this.m_Translation.y);
        }
        public Vector3 DrawingToViewTransformPoint(Vector3 lhs)
        {
            return new Vector3(lhs.x * this.m_Scale.x + this.m_Translation.x, lhs.y * this.m_Scale.y + this.m_Translation.y, 0f);
        }
        public Vector2 ViewToDrawingTransformPoint(Vector2 lhs)
        {
            return new Vector2((lhs.x - this.m_Translation.x) / this.m_Scale.x, (lhs.y - this.m_Translation.y) / this.m_Scale.y);
        }
        public Vector3 ViewToDrawingTransformPoint(Vector3 lhs)
        {
            return new Vector3((lhs.x - this.m_Translation.x) / this.m_Scale.x, (lhs.y - this.m_Translation.y) / this.m_Scale.y, 0f);
        }
        public Vector2 DrawingToViewTransformVector(Vector2 lhs)
        {
            return new Vector2(lhs.x * this.m_Scale.x, lhs.y * this.m_Scale.y);
        }
        public Vector3 DrawingToViewTransformVector(Vector3 lhs)
        {
            return new Vector3(lhs.x * this.m_Scale.x, lhs.y * this.m_Scale.y, 0f);
        }
        public Vector2 ViewToDrawingTransformVector(Vector2 lhs)
        {
            return new Vector2(lhs.x / this.m_Scale.x, lhs.y / this.m_Scale.y);
        }
        public Vector3 ViewToDrawingTransformVector(Vector3 lhs)
        {
            return new Vector3(lhs.x / this.m_Scale.x, lhs.y / this.m_Scale.y, 0f);
        }
        public Vector2 NormalizeInViewSpace(Vector2 vec)
        {
            vec = Vector2.Scale(vec, this.m_Scale);
            vec /= vec.magnitude;
            return Vector2.Scale(vec, new Vector2(1f / this.m_Scale.x, 1f / this.m_Scale.y));
        }
        private bool IsZoomEvent()
        {
            return Event.current.button == 1 && Event.current.alt;
        }
        private bool IsPanEvent()
        {
            return (Event.current.button == 0 && Event.current.alt) || (Event.current.button == 2 && !Event.current.command);
        }
        public void BeginViewGUI()
        {
            if (this.styles.horizontalScrollbar == null)
            {
                this.styles.InitGUIStyles(this.m_MinimalGUI, this.m_EnableSliderZoom);
            }
            this.horizontalScrollbarID = GUIUtility.GetControlID(EditorGUIExt.s_MinMaxSliderHash, FocusType.Passive);
            this.verticalScrollbarID = GUIUtility.GetControlID(EditorGUIExt.s_MinMaxSliderHash, FocusType.Passive);
            if (!this.m_MinimalGUI || Event.current.type != EventType.Repaint)
            {
                this.SliderGUI();
            }
        }

        public void Zoom(bool scrollwhell)
        {
            Zoom(this.mousePositionInDrawing, scrollwhell);
        }

        public void Zoom(Vector2 zoomAround, bool scrollwhell, bool lockHorizontal = false, bool lockVertical = false)
        {
            float num = Event.current.delta.x + Event.current.delta.y;
            if (scrollwhell)
            {
                num = -num;
            }
            float num2 = Mathf.Max(0.01f, 1f + num * 0.01f);
            if (!this.m_HRangeLocked && !lockHorizontal)
            {
                this.m_Translation.x = this.m_Translation.x - zoomAround.x * (num2 - 1f) * this.m_Scale.x;
                this.m_Scale.x = this.m_Scale.x * num2;
            }
            if (!this.m_VRangeLocked && !lockVertical)
            {
                this.m_Translation.y = this.m_Translation.y - zoomAround.y * (num2 - 1f) * this.m_Scale.y;
                this.m_Scale.y = this.m_Scale.y * num2;
            }
            this.EnforceScaleAndRange();
        }

        public void HandleZoomAndPanEvents(Rect area)
        {
            GUILayout.BeginArea(area);
            area.x = 0f;
            area.y = 0f;
            int controlID = GUIUtility.GetControlID(ZoomableArea.zoomableAreaHash, FocusType.Passive, area);
            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (area.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.keyboardControl = controlID;
                        if (this.IsZoomEvent() || this.IsPanEvent())
                        {
                            GUIUtility.hotControl = controlID;
                            ZoomableArea.m_MouseDownPosition = this.mousePositionInDrawing;
                            Event.current.Use();
                        }
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        ZoomableArea.m_MouseDownPosition = new Vector2(-1000000f, -1000000f);
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        if (this.IsZoomEvent())
                        {
                            this.HandleZoomEvent(ZoomableArea.m_MouseDownPosition, false);
                            Event.current.Use();
                        }
                        else
                        {
                            if (this.IsPanEvent())
                            {
                                this.Pan();
                                Event.current.Use();
                            }
                        }
                    }
                    break;
                case EventType.ScrollWheel:
                    if (area.Contains(Event.current.mousePosition))
                    {
                        if (!this.m_IgnoreScrollWheelUntilClicked || GUIUtility.keyboardControl == controlID)
                        {
                            this.HandleZoomEvent(this.mousePositionInDrawing, true);
                            Event.current.Use();
                        }
                    }
                    break;
            }
            GUILayout.EndArea();
        }
        public void EndViewGUI()
        {
            if (this.m_MinimalGUI && Event.current.type == EventType.Repaint)
            {
                this.SliderGUI();
            }
        }
        private void SliderGUI()
        {
            if (this.m_HSlider || this.m_VSlider)
            {
                using (new EditorGUI.DisabledScope(!this.enableMouseInput))
                {
                    Bounds drawingBounds = this.drawingBounds;
                    Rect shownAreaInsideMargins = this.shownAreaInsideMargins;
                    float num = this.styles.sliderWidth - this.styles.visualSliderWidth;
                    float num2 = (!this.vSlider || !this.hSlider) ? 0f : num;
                    Vector2 a = this.m_Scale;
                    if (this.m_HSlider)
                    {
                        Rect position = new Rect(this.drawRect.x + 1f, this.drawRect.yMax - num, this.drawRect.width - num2, this.styles.sliderWidth);
                        float width = shownAreaInsideMargins.width;
                        float num3 = shownAreaInsideMargins.xMin;
                        if (this.m_EnableSliderZoom)
                        {
                            EditorGUIExt.MinMaxScroller(position, this.horizontalScrollbarID, ref num3, ref width, drawingBounds.min.x, drawingBounds.max.x, float.NegativeInfinity, float.PositiveInfinity, this.styles.horizontalScrollbar, this.styles.horizontalMinMaxScrollbarThumb, this.styles.horizontalScrollbarLeftButton, this.styles.horizontalScrollbarRightButton, true);
                        }
                        else
                        {
                            num3 = ReflectionGUIScroller(position, num3, width, drawingBounds.min.x, drawingBounds.max.x, this.styles.horizontalScrollbar, this.styles.horizontalMinMaxScrollbarThumb, this.styles.horizontalScrollbarLeftButton, this.styles.horizontalScrollbarRightButton, true);
                        }
                        float num4 = num3;
                        float num5 = num3 + width;
                        if (num4 > shownAreaInsideMargins.xMin)
                        {
                            num4 = Mathf.Min(num4, num5 - this.mRect.width / this.m_HScaleMax);
                        }
                        if (num5 < shownAreaInsideMargins.xMax)
                        {
                            num5 = Mathf.Max(num5, num4 + this.mRect.width / this.m_HScaleMax);
                        }
                        this.SetShownHRangeInsideMargins(num4, num5);
                    }
                    if (this.m_VSlider)
                    {
                        if (this.m_UpDirection == ZoomableArea.YDirection.Positive)
                        {
                            Rect position2 = new Rect(this.drawRect.xMax - num, this.drawRect.y, this.styles.sliderWidth, this.drawRect.height - num2);
                            float height = shownAreaInsideMargins.height;
                            float num6 = -shownAreaInsideMargins.yMax;
                            if (this.m_EnableSliderZoom)
                            {
                                EditorGUIExt.MinMaxScroller(position2, this.verticalScrollbarID, ref num6, ref height, -drawingBounds.max.y, -drawingBounds.min.y, float.NegativeInfinity, float.PositiveInfinity, this.styles.verticalScrollbar, this.styles.verticalMinMaxScrollbarThumb, this.styles.verticalScrollbarUpButton, this.styles.verticalScrollbarDownButton, false);
                            }
                            else
                            {
                                num6 = ReflectionGUIScroller(position2, num6, height, -drawingBounds.max.y, -drawingBounds.min.y, this.styles.verticalScrollbar, this.styles.verticalMinMaxScrollbarThumb, this.styles.verticalScrollbarUpButton, this.styles.verticalScrollbarDownButton, false);
                            }
                            float num4 = -(num6 + height);
                            float num5 = -num6;
                            if (num4 > shownAreaInsideMargins.yMin)
                            {
                                num4 = Mathf.Min(num4, num5 - this.mRect.height / this.m_VScaleMax);
                            }
                            if (num5 < shownAreaInsideMargins.yMax)
                            {
                                num5 = Mathf.Max(num5, num4 + this.mRect.height / this.m_VScaleMax);
                            }
                            this.SetShownVRangeInsideMargins(num4, num5);
                        }
                        else
                        {
                            Rect position3 = new Rect(this.drawRect.xMax - num, this.drawRect.y, this.styles.sliderWidth, this.drawRect.height - num2);
                            float height2 = shownAreaInsideMargins.height;
                            float num7 = shownAreaInsideMargins.yMin;
                            if (this.m_EnableSliderZoom)
                            {
                                EditorGUIExt.MinMaxScroller(position3, this.verticalScrollbarID, ref num7, ref height2, drawingBounds.min.y, drawingBounds.max.y, float.NegativeInfinity, float.PositiveInfinity, this.styles.verticalScrollbar, this.styles.verticalMinMaxScrollbarThumb, this.styles.verticalScrollbarUpButton, this.styles.verticalScrollbarDownButton, false);
                            }
                            else
                            {
                                num7 = ReflectionGUIScroller(position3, num7, height2, drawingBounds.min.y, drawingBounds.max.y, this.styles.verticalScrollbar, this.styles.verticalMinMaxScrollbarThumb, this.styles.verticalScrollbarUpButton, this.styles.verticalScrollbarDownButton, false);
                            }
                            float num4 = num7;
                            float num5 = num7 + height2;
                            if (num4 > shownAreaInsideMargins.yMin)
                            {
                                num4 = Mathf.Min(num4, num5 - this.mRect.height / this.m_VScaleMax);
                            }
                            if (num5 < shownAreaInsideMargins.yMax)
                            {
                                num5 = Mathf.Max(num5, num4 + this.mRect.height / this.m_VScaleMax);
                            }
                            this.SetShownVRangeInsideMargins(num4, num5);
                        }
                    }
                    if (this.uniformScale)
                    {
                        float num8 = this.drawRect.width / this.drawRect.height;
                        a -= this.m_Scale;
                        Vector2 b = new Vector2(-a.y * num8, -a.x / num8);
                        this.m_Scale -= b;
                        this.m_Translation.x = this.m_Translation.x - a.y / 2f;
                        this.m_Translation.y = this.m_Translation.y - a.x / 2f;
                        this.EnforceScaleAndRange();
                    }
                }
            }
        }

        public void OnAreaEvent()
        {
            if (this.styles.horizontalScrollbar == null)
            {
                this.styles.InitGUIStyles(this.m_MinimalGUI, this.m_EnableSliderZoom);
            }

            Rect drawArea = this.m_DrawArea;
            drawArea.x = 0f;
            drawArea.y = 0f;
            GUILayout.BeginArea(this.drawRect);

            int controlID = GUIUtility.GetControlID(ZoomableArea.zoomableAreaHash, FocusType.Passive, drawArea);
            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (drawArea.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.keyboardControl = controlID;
                        if (this.IsZoomEvent() || this.IsPanEvent())
                        {
                            GUIUtility.hotControl = controlID;
                            ZoomableArea.m_MouseDownPosition = this.mousePositionInDrawing;
                            Event.current.Use();
                        }
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        ZoomableArea.m_MouseDownPosition = new Vector2(-1000000f, -1000000f);
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        if (this.IsZoomEvent())
                        {
                            this.Zoom(ZoomableArea.m_MouseDownPosition, false);
                            Event.current.Use();
                        }
                        else
                        {
                            if (this.IsPanEvent())
                            {
                                this.Pan();
                                Event.current.Use();
                            }
                        }
                    }
                    break;
                case EventType.ScrollWheel:
                    if (drawArea.Contains(Event.current.mousePosition)
                        && GUIUtility.keyboardControl == controlID
                        && Event.current.control)
                    {
                        this.Zoom(this.mousePositionInDrawing, true, false, true);
                        Event.current.Use();
                    }
                    else if (drawArea.Contains(Event.current.mousePosition)
                        && GUIUtility.keyboardControl == controlID
                        && Event.current.shift)
                    {
                        this.Zoom(this.mousePositionInDrawing, true, true, false);
                        Event.current.Use();
                    }
                    break;
            }

            GUILayout.EndArea();

            this.horizontalScrollbarID = GUIUtility.GetControlID(EditorGUIExt.s_MinMaxSliderHash, FocusType.Passive);
            this.verticalScrollbarID = GUIUtility.GetControlID(EditorGUIExt.s_MinMaxSliderHash, FocusType.Passive);
            if (!this.m_MinimalGUI || Event.current.type != EventType.Repaint)
            {
                this.SliderGUI();
            }
        }
        private void Pan()
        {
            if (!this.m_HRangeLocked)
            {
                this.m_Translation.x = this.m_Translation.x + Event.current.delta.x;
            }
            if (!this.m_VRangeLocked)
            {
                this.m_Translation.y = this.m_Translation.y + Event.current.delta.y;
            }
            this.EnforceScaleAndRange();
        }
        private void HandleZoomEvent(Vector2 zoomAround, bool scrollwhell)
        {
            float num = Event.current.delta.x + Event.current.delta.y;
            if (scrollwhell)
            {
                num = -num;
            }
            float d = Mathf.Max(0.01f, 1f + num * 0.01f);
            this.SetScaleFocused(zoomAround, d * this.m_Scale, Event.current.shift, EditorGUI.actionKey);
        }
        public void SetScaleFocused(Vector2 focalPoint, Vector2 newScale)
        {
            this.SetScaleFocused(focalPoint, newScale, false, false);
        }

        public void SetScaleFocused(Vector2 focalPoint, Vector2 newScale, bool lockHorizontal, bool lockVertical)
        {
            if (!this.m_HRangeLocked && !lockHorizontal)
            {
                this.m_Translation.x = this.m_Translation.x - focalPoint.x * (newScale.x - this.m_Scale.x);
                this.m_Scale.x = newScale.x;
            }
            if (!this.m_VRangeLocked && !lockVertical)
            {
                this.m_Translation.y = this.m_Translation.y - focalPoint.y * (newScale.y - this.m_Scale.y);
                this.m_Scale.y = newScale.y;
            }
            this.EnforceScaleAndRange();
        }

        // Set new Transform
        public void SetTransform(Vector2 newTranslation, Vector2 newScale)
        {
            this.m_Scale = newScale;
            this.m_Translation = newTranslation;
            this.EnforceScaleAndRange();
        }

        public void EnforceScaleAndRange()
        {
            float num = this.mRect.width / this.m_HScaleMin;
            float num2 = this.mRect.height / this.m_VScaleMin;
            if (this.hRangeMax != float.PositiveInfinity && this.hRangeMin != float.NegativeInfinity)
            {
                num = Mathf.Min(num, this.hRangeMax - this.hRangeMin);
            }
            if (this.vRangeMax != float.PositiveInfinity && this.vRangeMin != float.NegativeInfinity)
            {
                num2 = Mathf.Min(num2, this.vRangeMax - this.vRangeMin);
            }
            Rect lastShownAreaInsideMargins = this.m_LastShownAreaInsideMargins;
            Rect shownAreaInsideMargins = this.shownAreaInsideMargins;
            if (!(shownAreaInsideMargins == lastShownAreaInsideMargins))
            {
                float num3 = 1E-05f;
                if (shownAreaInsideMargins.width < lastShownAreaInsideMargins.width - num3)
                {
                    float t = Mathf.InverseLerp(lastShownAreaInsideMargins.width, shownAreaInsideMargins.width, this.mRect.width / this.m_HScaleMax);
                    shownAreaInsideMargins = new Rect(Mathf.Lerp(lastShownAreaInsideMargins.x, shownAreaInsideMargins.x, t), shownAreaInsideMargins.y, Mathf.Lerp(lastShownAreaInsideMargins.width, shownAreaInsideMargins.width, t), shownAreaInsideMargins.height);
                }
                if (shownAreaInsideMargins.height < lastShownAreaInsideMargins.height - num3)
                {
                    float t2 = Mathf.InverseLerp(lastShownAreaInsideMargins.height, shownAreaInsideMargins.height, this.mRect.height / this.m_VScaleMax);
                    shownAreaInsideMargins = new Rect(shownAreaInsideMargins.x, Mathf.Lerp(lastShownAreaInsideMargins.y, shownAreaInsideMargins.y, t2), shownAreaInsideMargins.width, Mathf.Lerp(lastShownAreaInsideMargins.height, shownAreaInsideMargins.height, t2));
                }
                if (shownAreaInsideMargins.width > lastShownAreaInsideMargins.width + num3)
                {
                    float t3 = Mathf.InverseLerp(lastShownAreaInsideMargins.width, shownAreaInsideMargins.width, num);
                    shownAreaInsideMargins = new Rect(Mathf.Lerp(lastShownAreaInsideMargins.x, shownAreaInsideMargins.x, t3), shownAreaInsideMargins.y, Mathf.Lerp(lastShownAreaInsideMargins.width, shownAreaInsideMargins.width, t3), shownAreaInsideMargins.height);
                }
                if (shownAreaInsideMargins.height > lastShownAreaInsideMargins.height + num3)
                {
                    float t4 = Mathf.InverseLerp(lastShownAreaInsideMargins.height, shownAreaInsideMargins.height, num2);
                    shownAreaInsideMargins = new Rect(shownAreaInsideMargins.x, Mathf.Lerp(lastShownAreaInsideMargins.y, shownAreaInsideMargins.y, t4), shownAreaInsideMargins.width, Mathf.Lerp(lastShownAreaInsideMargins.height, shownAreaInsideMargins.height, t4));
                }
                if (shownAreaInsideMargins.xMin < this.hRangeMin)
                {
                    shownAreaInsideMargins.x = this.hRangeMin;
                }
                if (shownAreaInsideMargins.xMax > this.hRangeMax)
                {
                    shownAreaInsideMargins.x = this.hRangeMax - shownAreaInsideMargins.width;
                }
                if (shownAreaInsideMargins.yMin < this.vRangeMin)
                {
                    shownAreaInsideMargins.y = this.vRangeMin;
                }
                if (shownAreaInsideMargins.yMax > this.vRangeMax)
                {
                    shownAreaInsideMargins.y = this.vRangeMax - shownAreaInsideMargins.height;
                }
                this.shownAreaInsideMarginsInternal = shownAreaInsideMargins;
                this.m_LastShownAreaInsideMargins = shownAreaInsideMargins;
            }
        }
        public float PixelToTime(float pixelX, Rect rect)
        {
            // return (pixelX - rect.x) * this.shownArea.width / rect.width + this.shownArea.x;
            return (pixelX - rect.x) * this.ShowAreaWidth / rect.width + ShowAreaX;
        }

        public float ShowAreaXMin
        {
            get
            {
                if (this.m_UpDirection == ZoomableArea.YDirection.Positive)
                {
                    return (-this.m_Translation.x / this.m_Scale.x);
                }
                else
                {
                    return (-this.m_Translation.x / this.m_Scale.x);
                }
            }
        }

        public float ShowAreaX
        {
            get
            {
                if (this.m_UpDirection == ZoomableArea.YDirection.Positive)
                {
                    return (-this.m_Translation.x / this.m_Scale.x);
                }
                else
                {
                    return (-this.m_Translation.x / this.m_Scale.x);
                }
            }
        }

        public float ShowAreaY
        {
            get
            {
                if (this.m_UpDirection == ZoomableArea.YDirection.Positive)
                {
                    return -(this.m_Translation.y - this.drawRect.height) / this.m_Scale.y;
                }
                else
                {
                    return -this.m_Translation.y / this.m_Scale.y;
                }
            }
        }
        public float ShowAreaWidth
        {
            get
            {
                return this.m_DrawArea.width / this.m_Scale.x;
            }
        }

        public float ShowAreaHeight
        {
            get
            {
                if (this.m_UpDirection == ZoomableArea.YDirection.Positive)
                {
                    return this.m_DrawArea.height / -this.m_Scale.y;
                }
                else
                {
                    return this.m_DrawArea.height / this.m_Scale.y;
                }
            }
        }

        public float TimeToPixel(float time, Rect rect)
        {
            // return (time - this.shownArea.x) / this.shownArea.width * rect.width + rect.x;
            float x = ShowAreaX;
            float y = ShowAreaY;
            float width = ShowAreaWidth;
            return (time - x) / width * rect.width + rect.x;
        }

        public float TimeToPixel(
            float time,
            float rectWidth,
            float rectX,
            float x,
            float y,
            float width)
        {
            return (time - x) / width * rectWidth + rectX;
        }

        public float YToPixel(float y, Rect rect)
        {
            return y * this.m_Scale.y + this.m_Translation.y;
        }

        public float PixelToY(float pixel)
        {
            return (pixel - this.m_Translation.y) / m_Scale.y;
        }

        //public float PixelDeltaToTime(Rect rect)
        //{
        //    return this.shownArea.width / rect.width;
        //}

        private MethodInfo _reflectionScroller;
        public float ReflectionGUIScroller(Rect position, float value, float size, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb, GUIStyle leftButton, GUIStyle rightButton, bool horiz)
        {
            if (_reflectionScroller == null)
            {
                Type guiType = typeof(GUI);
                _reflectionScroller = guiType.GetMethod("Scroller", BindingFlags.Static);
            }
            object ret = _reflectionScroller.Invoke(null, new object[] { position, value, size, leftValue, rightValue, slider, thumb, leftButton, rightButton, horiz });
            return (float)ret;
        }
    }
}