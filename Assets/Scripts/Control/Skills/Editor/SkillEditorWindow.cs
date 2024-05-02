#nullable enable
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;
using DMTimeArea;
using System;
using System.Collections.Generic;

namespace CraftSharp.Control
{
    public class SkillEditorWindow : TimeAreaWindow
    {
        private static readonly string SKILL_CLIP_NAME = "DummySkill";
        private Rect rectTopBar;

        private Rect rectMainBodyArea;
        private Rect rectTotalArea;
        private Rect rectContent;
        private Rect rectTimeRuler;

        private Rect rectLeft;
        private Rect rectSplitter;
        public Rect rectLeftTopToolBar;

        private double _runningTime = 0f;
        private float m_PlaybackSpeed = 1f;
        private float _lastUpdateTime = 0f;
        private float _currentLeftWidth = 250f;
        private const float MIN_LEFT_WIDTH = 200f;
        private const float MAX_LEFT_WIDTH = 400f;
        private bool resizingLeft = false;

        #region Property Access
        
        protected override double RunningTime
        {
            get { return _runningTime; }
            set
            {
                _runningTime = value;
            }
        }

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
            get { return _currentLeftWidth; }
        }

        #endregion

        private Animator? previewAnimator = null;
        private AnimatorOverrideController? animatorOverrideController;
        private AnimationModeDriver? m_Driver;
        private bool m_FixedTransform = false;
        private GameObject? charaPreview = null;
        //private Vector3 charaPreviewPos = Vector3.zero;
        //private Quaternion charaPreviewRot = Quaternion.identity;
        private Transform? charaPreviewOrigin = null;
        private Transform CharaPreviewOrigin
        {
            get {
                if (charaPreviewOrigin == null)
                {
                    var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.hideFlags = HideFlags.HideAndDontSave;

                    charaPreviewOrigin = obj.transform;
                    charaPreviewOrigin.localScale = Vector3.one * 0.1F;
                }

                return charaPreviewOrigin;
            }
        }

        private PlayerStagedSkill? currentSkill = null;

        private readonly List<SkillStageTrack> tracks = new();
        private SkillStageTrack? activeTrack = null;

        [MenuItem("Tools/Unicorn Skill Editor", false, 2024)]
        static void Init()
        {
            var window = GetWindow<SkillEditorWindow>(false, "Unicorn Skill Editor");
            window.minSize = new Vector3(500f, 200f);
            window.Show();
        }

        private void InitializeTracks(PlayerStagedSkill newSkill)
        {
            tracks.Clear();
            var start = 0F;

            for (int i = 0; i < newSkill.Stages.Length; i++)
            {
                tracks.Add(new SkillStageTrack(start, i, newSkill.Stages[i]));
                start += newSkill.Stages[i].Duration;
            }
        }

        private void ClearTracks()
        {
            tracks.Clear();
        }

        /// <summary>
        /// Called when the selected object for preview is changed
        /// </summary>
        void HandleSkillChange(PlayerStagedSkill? newSkill)
        {
            currentSkill = newSkill;

            if (newSkill == null || charaPreview == null)
            {
                ClearTracks();
            }
            else
            {
                InitializeTracks(newSkill);
            }
        }

        void AssignAnimatorOverride()
        {
            if (previewAnimator != null)
            {
                if (previewAnimator.runtimeAnimatorController is AnimatorOverrideController)
                {
                    animatorOverrideController = previewAnimator.runtimeAnimatorController as AnimatorOverrideController;
                }
                else
                {
                    animatorOverrideController = new AnimatorOverrideController(previewAnimator.runtimeAnimatorController)
                    {
                        name = previewAnimator.runtimeAnimatorController.name + " (Overriden)"
                    };
                    previewAnimator.runtimeAnimatorController = animatorOverrideController;
                }
            }
        }

        /// <summary>
        /// Called when the selected preview character object is changed
        /// </summary>
        /// <param name="newPreview">The new selected object</param>
        void HandlePreviewObjectChange(GameObject? newPreview)
        {
            AnimationMode.StopAnimationMode(GetAnimationModeDriver()); // Revert all changes done in preview

            if (charaPreview != null && charaPreviewOrigin != null) // Revert preview object root transform
            {
                charaPreview.transform.SetPositionAndRotation(charaPreviewOrigin.position, charaPreviewOrigin.rotation);
                GameObject.DestroyImmediate(charaPreviewOrigin.gameObject);
            }

            charaPreview = newPreview;

            if (charaPreview == null || (previewAnimator = charaPreview.GetComponentInChildren<Animator>()) == null)
            {
                charaPreview = null;
                previewAnimator = null;

                ClearTracks();
            }
            else
            {
                if (currentSkill == null)
                {
                    ClearTracks();
                }
                else
                {
                    InitializeTracks(currentSkill);

                    // Store initial position and rotation
                    CharaPreviewOrigin.transform.SetPositionAndRotation(charaPreview.transform.position, charaPreview.transform.rotation);
                    AnimationMode.StartAnimationMode(GetAnimationModeDriver());

                    // Assign overridden animator controller
                    AssignAnimatorOverride();

                    // Enable root motion
                    previewAnimator.stabilizeFeet = true;
                    previewAnimator.applyRootMotion = true;
                }
            }
        }

        void DetectPreviewObjectChange()
        {
            var newPreview = Selection.activeObject as GameObject;

            if (newPreview != charaPreview)
            {
                HandlePreviewObjectChange(newPreview);

                Repaint();
            }
        }

        protected override void HandleTimeUpdate(double runningTime)
        {
            if (activeTrack != null && (runningTime < activeTrack.StageStart ||
                    runningTime > activeTrack.StageStart + activeTrack.StageDuration) )
            {
                // Active stage expired
                activeTrack = null;
            }

            if (activeTrack == null && animatorOverrideController != null)
            {
                // Update active stage
                foreach (var track in tracks)
                {
                    if (runningTime >= track.StageStart && runningTime <= track.StageStart + track.StageDuration)
                    {
                        activeTrack = track;
                        track.ReplaceAnimation(animatorOverrideController);
                        break;
                    }
                }
            }

            if (activeTrack != null && AnimationMode.InAnimationMode())
            {
                float animTime = (float) runningTime - activeTrack.StageStart;
                animTime = math.clamp(animTime, 0F, activeTrack.StageDuration);

                var clip = animatorOverrideController![SKILL_CLIP_NAME];
                
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(charaPreview, clip, animTime);
                AnimationMode.EndSampling();

                if (m_FixedTransform)
                {
                    charaPreview!.transform.SetPositionAndRotation(CharaPreviewOrigin.position, CharaPreviewOrigin.rotation);
                }
                else
                {
                    charaPreview!.transform.SetPositionAndRotation(
                        CharaPreviewOrigin.position + charaPreview!.transform.position,
                        charaPreview!.transform.rotation
                    );
                }
            }
        }

        private AnimationModeDriver GetAnimationModeDriver()
        {
            if (m_Driver == null)
            {
                m_Driver = ScriptableObject.CreateInstance<AnimationModeDriver>();
                m_Driver.hideFlags = HideFlags.HideAndDontSave;
                m_Driver.name = "SkillAnimationWindowDriver";
            }

            return m_Driver;
        }

        void OnFocus()
        {
            DetectPreviewObjectChange();
        }

        void OnSelectionChange()
        {
            DetectPreviewObjectChange();
        }

        private void OnEnable()
        {
            EditorApplication.update = (EditorApplication.CallbackFunction) Delegate.Combine(EditorApplication.update, new EditorApplication.CallbackFunction(OnEditorUpdate));
            _lastUpdateTime = (float)EditorApplication.timeSinceStartup;

            // Update on start
            HandleSkillChange(currentSkill);
        }

        private void OnDisable()
        {
            EditorApplication.update = (EditorApplication.CallbackFunction) Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(OnEditorUpdate));
        }

        public void OnDestroy()
        {
            if (m_Driver != null)
                ScriptableObject.DestroyImmediate(m_Driver);
            
            if (charaPreviewOrigin != null)
                GameObject.DestroyImmediate(charaPreviewOrigin.gameObject);
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying && this.IsPlaying)
            {
                double fTime = (float) EditorApplication.timeSinceStartup - _lastUpdateTime;
                // Clamp fTime to avoid sudden changes
                fTime = math.clamp(fTime, 0, 0.1);

                _lastUpdateTime = (float) EditorApplication.timeSinceStartup;

                // Setting running time will trigger animation update, which
                // can take a long time, so we do this after updating timestamp
                this.RunningTime += Math.Abs(fTime) * m_PlaybackSpeed;
                HandleTimeUpdate(this.RunningTime);
            }
            
            Repaint();
        }

        private void OnGUI()
        {
            rectTopBar.Set(0, 0, this.position.width, toolbarHeight);

            // Draw top tool bar
            DrawTopToolBar();

            if (charaPreview == null || currentSkill == null)
            {
                GUILayout.BeginArea(rectMainBodyArea, string.Empty);
                GUILayout.Space(5);
                GUILayout.Label("Please select a valid character object with Animator,");
                GUILayout.Label("and also make sure a skill data asset is selected.");

                GUILayout.EndArea();
                return;
            }

            rectMainBodyArea.Set(0, toolbarHeight, base.position.width, this.position.height - toolbarHeight);

            rectLeft.Set(rectMainBodyArea.x, rectMainBodyArea.y + timeRulerHeight, _currentLeftWidth, rectMainBodyArea.height);
            rectLeftTopToolBar.Set(rectMainBodyArea.x, rectMainBodyArea.y, _currentLeftWidth, timeRulerHeight);

            var contentWidth = position.width - _currentLeftWidth;

            rectTotalArea.Set(rectMainBodyArea.x + _currentLeftWidth, rectMainBodyArea.y, contentWidth, rectMainBodyArea.height);
            rectTimeRuler.Set(rectMainBodyArea.x + _currentLeftWidth, rectMainBodyArea.y, contentWidth, timeRulerHeight);
            rectContent.Set(rectMainBodyArea.x + _currentLeftWidth, rectMainBodyArea.y + timeRulerHeight, contentWidth, rectMainBodyArea.height - timeRulerHeight);

            // Lock and hide vertical slider
            InitTimeArea(false, true, true, false);
            DrawTimeAreaBackGround();
            HandleTimeRulerCursorInput();
            

            // Draw left content
            DrawLeftContent();
            // Draw left tool bar
            DrawLeftTopToolBar();

            // Draw view splitter
            DrawViewSplitter();

            // Draw timeline items
            DrawContent();

            // Draw time ruler last so that the cursor doesn't get covered
            DrawTimeRulerArea();
        }

        protected override void DrawVerticalTickLine()
        {
            Color preColor = Handles.color;
            Color color = Color.white;
            color.a = 0.3f;
            Handles.color = color;
            // draw vertical ticks
            float step = 10;
            float preStep = TimeArea.drawRect.height / 20f;
            // step = GetTimeArea.drawRect.y;
            step = 0f;
            while (step <= TimeArea.drawRect.height + TimeArea.drawRect.y)
            {
                Vector2 pos = new Vector2(rectContent.x, step + TimeArea.drawRect.y);
                Vector2 endPos = new Vector2(position.width, step + TimeArea.drawRect.y);
                step += preStep;
                float height = PixelToY(step);
                Rect rect = new Rect(rectContent.x + 5f, step - 10f + TimeArea.drawRect.y, 100f, 20f);
                GUI.Label(rect, height.ToString("0"));
                Handles.DrawLine(pos, endPos);
            }
            Handles.color = preColor;
        }

        protected virtual void DrawLeftContent()
        {
            GUILayout.BeginArea(rectLeft);
            
            float nextStartY = 10F;

            foreach (var track in tracks)
            {
                track.DrawHeader(nextStartY, _currentLeftWidth);

                nextStartY += track.TrackHeight + 6;
            }

            GUILayout.EndArea();
        }

        protected virtual void DrawContent()
        {
            GUILayout.BeginArea(rectContent);
            
            float nextStartY = 10F;
            var contentWidth = position.width - _currentLeftWidth;

            foreach (var track in tracks)
            {
                track.DrawContent(nextStartY, contentWidth, TimeAreaTimeShownRange, track == activeTrack);

                if (track == activeTrack)
                {
                    GUI.color = Color.black;
                    // Draw cursor time inside this track
                    float pos = TimeToPixel(this.RunningTime) - rectMainBodyArea.x - _currentLeftWidth;
                    GUI.Label(new(pos, nextStartY - 5, 50, 20), $"{RunningTime - track.StageStart:0.00}", EditorStyles.boldLabel);
                    GUI.color = Color.white;
                }

                nextStartY += track.TrackHeight + 6;
            }

            GUILayout.EndArea();
        }

        protected virtual void DrawTopToolBar()
        {
            GUILayout.BeginArea(rectTopBar);

            GUILayout.BeginHorizontal();

            GUILayout.Space(10);

            GUI.enabled = false;
            EditorGUILayout.ObjectField("Target Animator", previewAnimator, typeof (Animator), true, GUILayout.Width(300));
            GUI.enabled = true;

            GUILayout.Space(10);

            var newCurrentSkill = EditorGUILayout.ObjectField("Skill", currentSkill,
                    typeof (PlayerStagedSkill), false, GUILayout.Width(300)) as PlayerStagedSkill;

            if (charaPreview != null && newCurrentSkill != currentSkill)
            {
                HandleSkillChange(newCurrentSkill);
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Reload", GUILayout.Width(70)) && currentSkill != null)
            {
                HandleSkillChange(currentSkill);
            }

            GUILayout.Space(60);

            GUILayout.Label("AnimMode: " + (AnimationMode.InAnimationMode() ? "ON" : "OFF"), GUILayout.Width(100));

            GUILayout.Label("Speed:", GUILayout.Width(50));
            m_PlaybackSpeed = EditorGUILayout.Slider(m_PlaybackSpeed, -1, 1, GUILayout.Width(135));
            GUILayout.Space(20);
            m_FixedTransform = EditorGUILayout.Toggle("Fixed:", m_FixedTransform, GUILayout.Width(60));

            GUILayout.EndHorizontal();

            var rectSettingButton = new Rect(rectTopBar.width - 32, rectTopBar.y, 32, toolbarHeight);
            if (!Application.isPlaying && GUI.Button(rectSettingButton, ResManager.SettingIcon, EditorStyles.toolbarDropDown))
            {
                OnClickSettingButton();
            }
            GUILayout.EndArea();
        }

        private static readonly Color32 TOOLBAR_GAP_COLOR = new(40, 40, 40, 255);

        private void DrawLeftTopToolBar()
        {
            GUI.DrawTexture(rectLeftTopToolBar, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 0, Color.gray * 0.5F, 0, 0);
            var rectToolBarGap = rectLeftTopToolBar;
            rectToolBarGap.y = rectLeftTopToolBar.y + rectLeftTopToolBar.height - 4;
            rectToolBarGap.height = 4;
            GUI.DrawTexture(rectToolBarGap,     EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 0, TOOLBAR_GAP_COLOR, 0, 0);

            // left top tool bar
            GUILayout.BeginArea(rectLeftTopToolBar, string.Empty/*, EditorStyles.toolbarButton*/);

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
                ResetPreview();
            }

            GUILayout.FlexibleSpace();
            string timeStr = TimeAsString((double) RunningTime, "F2");
            GUILayout.Label(timeStr);
            GUILayout.Space(5);
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawViewSplitter()
        {
            rectSplitter.Set(_currentLeftWidth - 1f, rectMainBodyArea.y, 2f, rectMainBodyArea.height);
            
            EditorGUIUtility.AddCursorRect(rectSplitter, MouseCursor.ResizeHorizontal);

            if (UnityEngine.Event.current.type == EventType.MouseDown && rectSplitter.Contains(UnityEngine.Event.current.mousePosition))
            {
                resizingLeft = true;
            }

            if (resizingLeft)
            {
                _currentLeftWidth = Mathf.Clamp(UnityEngine.Event.current.mousePosition.x, MIN_LEFT_WIDTH, MAX_LEFT_WIDTH);
                rectSplitter.Set(_currentLeftWidth - 1f, rectMainBodyArea.y, rectSplitter.width, rectMainBodyArea.height);

                GUI.DrawTexture(rectSplitter, EditorGUIUtility.whiteTexture);
            }
            else
            {
                GUI.DrawTexture(rectSplitter, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 0, TOOLBAR_GAP_COLOR, 0, 0);
            }

            if (UnityEngine.Event.current.type == EventType.MouseUp)
            {
                resizingLeft = false;
            }
        }

        private void PlayPreview()
        {
            IsPlaying = true;
        }

        private void PausePreview()
        {
            IsPlaying = false;
        }

        void ResetPreview()
        {
            IsPlaying = false;
            RunningTime = 0.0f;
        }
    }
}