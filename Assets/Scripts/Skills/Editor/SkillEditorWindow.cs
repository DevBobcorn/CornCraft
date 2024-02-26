using UnityEngine;
using UnityEditor;
using DMTimeArea;
using System;

public class SkillEditorWindow : SimpleTimeArea
{
    private Rect rectTotalArea;
    private Rect rectContent;
    private Rect rectTimeRuler;

    private Rect rectTopBar;
    private Rect rectLeft;
    public Rect rectLeftTopToolBar;

    private float _lastUpdateTime = 0f;
    #region Used
    private double runningTime = 10.0f;
    protected override double RunningTime
    {
        get { return runningTime; }
        set
        {
            runningTime = value;
        }
    }

    private readonly float leftWidth = 250f;

    public bool IsPlaying
    {
        get;
        set;
    }

    protected override bool IsLockedMoveFrame
    {
        get { return (IsPlaying || Application.isPlaying); }
    }

    protected override bool IsLockDragHeaderArrow
    {
        get { return IsPlaying; }
    }

    public override Rect _rectTimeAreaTotal
    {
        get { return rectTotalArea; }
    }

    public override Rect _rectTimeAreaContent
    {
        get { return rectContent; }
    }

    public override Rect _rectTimeAreaRuler
    {
        get { return rectTimeRuler; }
    }

    protected override float sequencerHeaderWidth
    {
        get { return leftWidth; }
    }

    #endregion

    [MenuItem("Tools/Skill Editor", false, 2024)]
    public static void DoWindow()
    {
        var window = GetWindow<SkillEditorWindow>(false, "Skill Editor");
        window.minSize = new Vector3(400f, 200f);
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update = (EditorApplication.CallbackFunction)System.Delegate.Combine(EditorApplication.update, new EditorApplication.CallbackFunction(OnEditorUpdate));
        _lastUpdateTime = (float)EditorApplication.timeSinceStartup;
    }

    private void OnDisable()
    {
        EditorApplication.update = (EditorApplication.CallbackFunction)System.Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(OnEditorUpdate));
    }

    private void OnEditorUpdate()
    {
        if (!Application.isPlaying && this.IsPlaying)
        {
            double fTime = (float)EditorApplication.timeSinceStartup - _lastUpdateTime;
            this.RunningTime += Math.Abs(fTime) * 1.0f;
        }

        _lastUpdateTime = (float)EditorApplication.timeSinceStartup;
        Repaint();
    }

    private void OnGUI()
    {
        Rect rectMainBodyArea = new Rect(0, toolbarHeight, base.position.width, this.position.height - toolbarHeight);
        rectTopBar = new Rect(0, 0, this.position.width, toolbarHeight);
        rectLeft = new Rect(rectMainBodyArea.x, rectMainBodyArea.y + timeRulerHeight, leftWidth, rectMainBodyArea.height);
        rectLeftTopToolBar = new Rect(rectMainBodyArea.x, rectMainBodyArea.y, leftWidth, timeRulerHeight);

        rectTotalArea = new Rect(rectMainBodyArea.x + leftWidth, rectMainBodyArea.y, base.position.width - leftWidth, rectMainBodyArea.height);
        rectTimeRuler = new Rect(rectMainBodyArea.x + leftWidth, rectMainBodyArea.y, base.position.width - leftWidth, timeRulerHeight);
        rectContent = new Rect(rectMainBodyArea.x + leftWidth, rectMainBodyArea.y + timeRulerHeight, base.position.width - leftWidth, rectMainBodyArea.height - timeRulerHeight);

        InitTimeArea(false, false, true, true);
        DrawTimeAreaBackGround();
        OnTimeRulerCursorAndCutOffCursorInput();
        DrawTimeRulerArea();

        // Draw top tool bar
        DrawTopToolBar();
        // Draw left content
        DrawLeftContent();
        // Draw left tool bar
        DrawLeftTopToolBar();

        GUILayout.BeginArea(rectContent);
        

        GUILayout.EndArea();
    }


    protected override void DrawVerticalTickLine()
    {
        Color preColor = Handles.color;
        Color color = Color.white;
        color.a = 0.3f;
        Handles.color = color;
        // draw vertical ticks
        float step = 10;
        float preStep = GetTimeArea.drawRect.height / 20f;
        // step = GetTimeArea.drawRect.y;
        step = 0f;
        while (step <= GetTimeArea.drawRect.height + GetTimeArea.drawRect.y)
        {
            Vector2 pos = new Vector2(rectContent.x, step + GetTimeArea.drawRect.y);
            Vector2 endPos = new Vector2(position.width, step + GetTimeArea.drawRect.y);
            step += preStep;
            float height = PixelToY(step);
            Rect rect = new Rect(rectContent.x + 5f, step - 10f + GetTimeArea.drawRect.y, 100f, 20f);
            GUI.Label(rect, height.ToString("0"));
            Handles.DrawLine(pos, endPos);
        }
        Handles.color = preColor;
    }

    protected virtual void DrawLeftContent()
    {
        GUILayout.BeginArea(rectLeft);
        GUILayout.Label("Draw your left content");
        


        GUILayout.EndArea();
    }

    protected virtual void DrawTopToolBar()
    {
        GUILayout.BeginArea(rectTopBar);
        Rect rect = new Rect(rectTopBar.width - 32, rectTopBar.y, 30, 30);
        if (!Application.isPlaying && GUI.Button(rect, ResManager.SettingIcon, EditorStyles.toolbarDropDown))
        {
            OnClickSettingButton();
        }
        GUILayout.EndArea();
    }

    private void DrawLeftTopToolBar()
    {
        // left top tool bar
        GUILayout.BeginArea(rectLeftTopToolBar, string.Empty, EditorStyles.toolbarButton);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button(ResManager.prevKeyContent, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
        {
            PreviousTimeFrame();
        }

        bool playing = IsPlaying;
        playing = GUILayout.Toggle(playing, ResManager.playContent, EditorStyles.toolbarButton, new GUILayoutOption[0]);
        if (!Application.isPlaying)
        {
            if (IsPlaying != playing)
            {
                IsPlaying = playing;
                if (IsPlaying)
                    PlayPreview();
                else
                    PausePreview();
            }
        }

        if (GUILayout.Button(ResManager.nextKeyContent, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
        {
            NextTimeFrame();
        }

        if (GUILayout.Button(ResManager.StopIcon, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false))
            && !Application.isPlaying)
        {
            PausePreview();
            this.RunningTime = 0.0f;
        }

        GUILayout.FlexibleSpace();
        string timeStr = TimeAsString((double)this.RunningTime, "F2");
        GUILayout.Label(timeStr);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void PlayPreview()
    {
        IsPlaying = true;
    }

    private void PausePreview()
    {
        IsPlaying = false;
    }
}
