// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MagicaCloth
{
    /// <summary>
    /// 入力マネージャ
    /// ・簡単なタップやフリック判定
    /// ・PCの場合はマウスによる自動エミュレーション
    /// </summary>
    public class SimpleInputManager : CreateSingleton<SimpleInputManager>
    {
        // 最大タッチ数
        private const int MaxFinger = 3;

        /// <summary>
        /// タップ有効半径(cm)
        /// </summary>
        public float tapRadiusCm = 0.5f;

        /// <summary>
        /// フリック判定距離(cm)
        /// </summary>
        public float flickRangeCm = 0.01f;

        /// <summary>
        /// フリック判定速度(cm/s)
        /// </summary>
        public float flickCheckSpeed = 1.0f;

        /// <summary>
        /// マウスホイールのピンチイン・ピンチアウト速度係数
        /// </summary>
        public float mouseWheelSpeed = 5.0f;

        // 入力情報管理
        private int mainFingerId = -1;
        private int subFingerId = -1;
        private Vector2[] downPos;              // 入力開始座標（スクリーン）
        private Vector2[] lastPos;
        private Vector2[] flickDownPos;         // 入力開始座標（スクリーン）
        private float[] flickDownTime;
        private float lastTime = 0;             // バックボタンの連続入力防止用

        // モバイル情報管理
        private bool mobilePlatform = false;

        // マウスエミュレーション情報管理
        private bool[] mouseDown;
        private Vector2[] mouseOldMovePos;

        // モニタ情報
        private float screenDpi;                // スクリーンDPI値
        private float screenDpc;                // スクリーンDots per cm値（１ｃｍ当たりのピクセル数）

        //------------------------------ モバイルタッチパネル／マウスエミュレーション ------------------
        // タッチ開始通知
        // タッチされた時に、フィンガーID、その位置（スクリーン）を通知します。
        public static UnityAction<int, Vector2> OnTouchDown;

        // 移動通知
        // タッチされたまま移動された場合に、フィンガーID、その位置（スクリーン）、速度(スクリーン比率/s)、速度(cm/s)を通知します。
        public static UnityAction<int, Vector2, Vector2, Vector2> OnTouchMove;

        // ダブルタッチされたまま移動された場合に、フィンガーID、その位置（スクリーン）、速度(スクリーン比率/s)、速度(cm/s)を通知します。
        public static UnityAction<int, Vector2, Vector2, Vector2> OnDoubleTouchMove;

        // タッチ終了通知
        // タッチが離されたフィンガーID、位置（スクリーン）を通知します。
        public static UnityAction<int, Vector2> OnTouchUp;

        // タッチキャンセル通知
        // タッチ移動がキャンセル（主に画面外に移動）された場合に、フィンガーID、その最終位置（スクリーン）を通知します。
        public static UnityAction<int, Vector2> OnTouchMoveCancel;

        // タップ通知
        // タップされた時に、フィンガーID、その位置（スクリーン）を通知します。
        public static UnityAction<int, Vector2> OnTouchTap;

        // フリック通知
        // フリック判定された場合に、フィンガーID、その位置（スクリーン）、フリック速度(スクリーン比率/s)、速度(cm/s)を通知します。
        public static UnityAction<int, Vector2, Vector2, Vector2> OnTouchFlick;

        // ピンチイン／アウト通知
        // ピンチイン／アウトの速度（スクリーン比率/s）、速度(cm/s)を通知します。
        public static UnityAction<float, float> OnTouchPinch;

        // バックボタン通知（Androiddeでは戻るボタン、PCでは BackSpace ボタン）
        public static UnityAction OnBackButton;

        //=========================================================================================
        /// <summary>
        /// Reload Domain 対策
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            InitMember();
        }

        //=========================================================================================
        protected override void InitSingleton()
        {
            // スクリーン情報
            CalcScreenDpi();

            // 情報初期化
            downPos = new Vector2[MaxFinger];
            lastPos = new Vector2[MaxFinger];
            flickDownPos = new Vector2[MaxFinger];
            flickDownTime = new float[MaxFinger];

            // マウス用
            mouseDown = new bool[3];
            mouseOldMovePos = new Vector2[3];

            AllResetTouchInfo();

            // モバイルプラットフォーム判定 
            mobilePlatform = Application.isMobilePlatform;
        }

        void Update()
        {
            // 入力タイプ別更新処理
            if (mobilePlatform)
            {
                // モバイル用タッチ入力 
                UpdateMobile();
            }
            else
            {
                // マウスエミュレーション 
                UpdateMouse();
            }
        }

        //=========================================================================================
        /// <summary>
        /// スクリーンのDPI値(Dots per inchi)１インチ当たりのピクセル数を取得する
        /// </summary>
        public static float ScreenDpi
        {
            get
            {
                return Instance.screenDpi;
            }
        }

        /// <summary>
        /// スクリーンのDPC値(Dots per cm)１ｃｍ当たりのピクセル数を取得する
        /// </summary>
        public static float ScreenDpc
        {
            get
            {
                return Instance.screenDpc;
            }
        }

        /// <summary>
        /// スクリーンDpi/Dpcの再計算
        /// </summary>
        private void CalcScreenDpi()
        {
            screenDpi = Screen.dpi;
            if (screenDpi == 0.0f)
            {
                screenDpi = 96; // ダミー
            }
            screenDpc = screenDpi / 2.54f; // インチをcmに変換
        }

        // タッチ入力情報リセット
        private void AllResetTouchInfo()
        {
            mainFingerId = -1;
            subFingerId = -1;
            for (int i = 0; i < 3; i++)
            {
                mouseDown[i] = false;
            }
        }

        public int GetTouchCount()
        {
            return Input.touchCount;
        }

        public bool IsUI()
        {
            if (mobilePlatform)
            {
                // モバイル用タッチ入力 
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            }
            else
            {
                // マウスエミュレーション 
                return EventSystem.current.IsPointerOverGameObject();
            }
        }

        //=========================================================================================
        /// <summary>
        /// モバイル用入力更新
        /// </summary>
        private void UpdateMobile()
        {
            int count = Input.touchCount;

            if (count == 0)
            {
                AllResetTouchInfo();

                // バックボタン
                if (Application.platform == RuntimePlatform.Android)
                {
                    if (Input.GetKey(KeyCode.Escape) && lastTime + 0.2f < Time.time)
                    {
                        lastTime = Time.time;
                        if (OnBackButton != null)
                        {
                            OnBackButton();
                        }
                        return;
                    }
                }
            }
            else
            {
                // メイン 
                for (int i = 0; i < count; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    int fid = touch.fingerId;

                    // フィンガーIDが０と１以外は無視する 
                    if (fid >= 2)
                    {
                        continue;
                    }

                    if (touch.phase == TouchPhase.Began)
                    {
                        if (IsUI())
                            continue;
                        // down pos
                        downPos[fid] = touch.position;
                        lastPos[fid] = touch.position;
                        flickDownPos[fid] = touch.position;

                        if (fid == 0)
                        {
                            mainFingerId = fid;
                        }
                        else
                        {
                            subFingerId = fid;
                        }

                        // Downはメインフィンガーのみ 
                        if (fid == 0)
                        {
                            flickDownTime[fid] = Time.time;
                            if (OnTouchDown != null)
                            {
                                OnTouchDown(fid, touch.position);
                            }
                        }
                    }
                    else if (touch.phase == TouchPhase.Moved)
                    {
                        // ピンチイン／アウト判定 
                        if (mainFingerId >= 0 && subFingerId >= 0)
                        {
                            Vector2 t1pos = Vector2.zero;
                            Vector2 t2pos = Vector2.zero;
                            Vector2 t1delta = Vector2.zero;
                            Vector2 t2delta = Vector2.zero;

                            int setcnt = 0;
                            for (int j = 0; j < count; j++)
                            {
                                Touch t = Input.GetTouch(j);
                                if (mainFingerId == t.fingerId)
                                {
                                    t1pos = t.position;
                                    t1delta = t.deltaPosition;
                                    setcnt++;
                                }
                                else if (subFingerId == t.fingerId)
                                {
                                    t2pos = t.position;
                                    t2delta = t.deltaPosition;
                                    setcnt++;
                                }
                            }

                            if (setcnt == 2)
                            {
                                float nowdist = Vector2.Distance(t1pos, t2pos);
                                float olddist = Vector2.Distance(t1pos - t1delta, t2pos - t2delta);
                                float dist = nowdist - olddist;

                                // cm/sに変換
                                float distcm = dist / screenDpc; // 移動量(cm)
                                float speedcm = distcm / Time.deltaTime; // 速度(cm/s)

                                // スクリーン比率の速度
                                float speedscr = (dist / (Screen.width + Screen.height) * 0.5f) / Time.deltaTime;

                                // ピンチ通知(移動量(cm), 速度(cm/s))
                                if (OnTouchPinch != null)
                                {
                                    OnTouchPinch(speedscr, speedcm);
                                }
                            }

                            if (fid == 0)
                            {
                                Vector2 distVec2 = touch.position - lastPos[fid];
                                Vector2 distcm = distVec2 / screenDpc; // 移動量(cm)
                                Vector2 speedcm = distcm / Time.deltaTime; // 速度(cm/s)

                                // 速度(スクリーン比率)
                                Vector2 speedscr = CalcScreenRatioVector(distVec2) / Time.deltaTime;

                                // 移動通知(現在スクリーン座標、速度(スクリーン比率), 速度(cm/s))
                                if (OnDoubleTouchMove != null)
                                {
                                    OnDoubleTouchMove(fid, touch.position, speedscr, speedcm);
                                }

                                lastPos[fid] = touch.position;
                            }
                        }
                        else
                        {
                            // Moveはメインフィンガーのみ 
                            if (fid == 0 && mainFingerId >= 0)
                            {
                                Vector2 distVec2 = touch.position - lastPos[fid];
                                Vector2 distcm = distVec2 / screenDpc; // 移動量(cm)
                                Vector2 speedcm = distcm / Time.deltaTime; // 速度(cm/s)

                                // 速度(スクリーン比率)
                                Vector2 speedscr = CalcScreenRatioVector(distVec2) / Time.deltaTime;

                                // 移動通知(現在スクリーン座標、速度(スクリーン比率), 速度(cm/s))
                                if (OnTouchMove != null)
                                {
                                    OnTouchMove(fid, touch.position, speedscr, speedcm);
                                }

                                // フリックダウン位置更新
                                flickDownPos[fid] = (flickDownPos[fid] + touch.position) * 0.5f;
                                flickDownTime[fid] = Time.time;
                            }

                            lastPos[fid] = touch.position;
                        }
                    }
                    else if (touch.phase == TouchPhase.Ended)
                    {
                        // フィンガーIDのリリース 
                        if (fid == 0)
                        {
                            mainFingerId = -1;
                            subFingerId = -1;
                        }
                        else
                        {
                            subFingerId = -1;
                        }

                        // End, Tap はメインフィンガーのみ 
                        if (fid == 0)
                        {
                            // タップ判定
                            float dist = Vector2.Distance(downPos[fid], touch.position);
                            float distcm = dist / screenDpc;

                            if (distcm <= tapRadiusCm)
                            {
                                // タップ通知
                                if (OnTouchTap != null)
                                {
                                    OnTouchTap(fid, touch.position);
                                }
                            }
                            // フリック判定
                            else
                            {
                                CheckFlic(fid, downPos[fid], touch.position, flickDownPos[fid], flickDownTime[fid]);
                            }

                            // タップアップ通知
                            if (OnTouchUp != null)
                            {
                                OnTouchUp(fid, touch.position);
                            }
                        }
                    }
                    else if (touch.phase == TouchPhase.Canceled)
                    {
                        // フィンガーIDのリリース 
                        if (fid == 0)
                        {
                            mainFingerId = -1;
                            subFingerId = -1;
                        }
                        else
                        {
                            subFingerId = -1;
                        }

                        // Cancelはメインフィンガーのみ 
                        if (fid == 0)
                        {
                            if (OnTouchMoveCancel != null)
                            {
                                OnTouchMoveCancel(fid, touch.position);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// スクリーン比率に変換したベクトルを求める
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        private Vector2 CalcScreenRatioVector(Vector2 vec)
        {
            return new Vector2(vec.x / Screen.width, vec.y / Screen.height);
        }

        /// <summary>
        /// フリック判定
        /// </summary>
        /// <param name="oldpos"></param>
        /// <param name="nowpos"></param>
        /// <param name="downpos"></param>
        /// <param name="flicktime"></param>
        /// <returns></returns>
        private bool CheckFlic(int fid, Vector2 oldpos, Vector2 nowpos, Vector2 downpos, float flicktime)
        {
            // フリック判定
            float dist = Vector2.Distance(nowpos, downpos);
            float distcm = dist / screenDpc;
            if (distcm > flickRangeCm)
            {
                {
                    // 移動ピクセルをcm変換し、速度cm/sを割り出す
                    Vector2 distVec = (nowpos - downpos);
                    Vector2 distVec2 = distVec / screenDpc; // cmへ変換(移動量(cm))
                    float timeInterval = Time.time - flicktime;
                    float speedX = distVec2.x / timeInterval; // 速度(cm/s)
                    float speedY = distVec2.y / timeInterval; // 速度(cm/s)

                    //Develop.Log("distVec", distVec * 100);
                    //Develop.Log("sppedX:", speedX, " speedY:", speedY);

                    if (Mathf.Abs(speedX) >= flickCheckSpeed || Mathf.Abs(speedY) >= flickCheckSpeed)
                    {
                        // フリック通知(スクリーン位置,速度（スクリーン比率/s）,速度(cm/s))
                        if (OnTouchFlick != null)
                        {
                            OnTouchFlick(fid, nowpos, CalcScreenRatioVector(distVec) / timeInterval, new Vector2(speedX, speedY));
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        //=========================================================================================
        /// <summary>
        /// 入力情報更新（PC用）
        /// マウスエミュレーション
        /// ・右クリックは使わない。
        /// ・ピンチイン／アウトはマウスホイール。
        /// </summary>
        private void UpdateMouse()
        {
            // BackSpace を Android 端末のバックボタンに割り当てる
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                if (OnBackButton != null)
                    OnBackButton();
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                // マウスボタンダウン
                if (Input.GetMouseButtonDown(i))
                {
                    if (IsUI())
                        continue;

                    if (mouseDown[i] == false && i == 0)
                    {
                        flickDownTime[i] = Time.time;
                    }
                    mouseDown[i] = true;

                    // 入力位置を記録
                    downPos[i] = Input.mousePosition;
                    mouseOldMovePos[i] = Input.mousePosition;
                    if (i == 0)
                        flickDownPos[i] = Input.mousePosition;

                    // タッチダウンイベント発行
                    if (OnTouchDown != null)
                        OnTouchDown(i, Input.mousePosition);
                }

                // マウスボタンアップ
                if (Input.GetMouseButtonUp(i) && mouseDown[i])
                {
                    mouseDown[i] = false;

                    // フリック判定
                    if (i == 0)
                    {
                        CheckFlic(i, mouseOldMovePos[i], Input.mousePosition, flickDownPos[i], flickDownTime[i]);
                    }

                    mouseOldMovePos[i] = Vector2.zero;

                    // タッチアップイベント
                    if (OnTouchUp != null)
                        OnTouchUp(i, Input.mousePosition);

                    // タップ判定
                    float distcm = Vector2.Distance(downPos[0], Input.mousePosition) / screenDpc;
                    if (distcm <= tapRadiusCm)
                    {
                        if (OnTouchTap != null)
                            OnTouchTap(i, Input.mousePosition);
                    }
                }

                // 移動
                if (mouseDown[i])
                {
                    Vector2 spos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                    Vector2 delta = spos - mouseOldMovePos[i];

                    if (spos != mouseOldMovePos[i])
                    {
                        // 速度
                        Vector3 deltacm = delta / screenDpc; // 移動量(cm)
                        Vector2 speedcm = deltacm / Time.deltaTime; // 速度(cm/s)

                        // 移動通知(現在スクリーン座標、速度(スクリーン比率/s)、速度(cm/s))
                        if (OnTouchMove != null)
                            OnTouchMove(i, Input.mousePosition, CalcScreenRatioVector(delta) / Time.deltaTime, speedcm);
                    }

                    mouseOldMovePos[i] = Input.mousePosition;

                    // フリックダウン位置更新
                    flickDownPos[i] = (flickDownPos[i] + spos) * 0.5f;
                    flickDownTime[i] = Time.time;
                }

            }

            // ピンチイン／アウト 
            float w = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(w) > 0.01f)
            {
                // モバイル入力とスケール感を合わせるために係数を掛ける 
                w *= mouseWheelSpeed;

                float speedcm = w / Time.deltaTime;
                float speedscr = (w / (Screen.width + Screen.height) * 0.5f) / Time.deltaTime;

                // 通知（速度(スクリーン比率/s)、速度(cm/s)
                if (OnTouchPinch != null)
                    OnTouchPinch(speedscr, speedcm);
            }
        }
    }
}
