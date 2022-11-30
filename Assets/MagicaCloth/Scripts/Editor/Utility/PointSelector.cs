// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#pragma warning disable 0436

namespace MagicaCloth
{
    /// <summary>
    /// シーンビュー内での汎用ポイント選択ツール
    /// 使い方は PointSelectorTest / PointSelectorTestInspector を参照
    /// </summary>
    public class PointSelector
    {
        /// <summary>
        /// 編集状態
        /// </summary>
        public static bool EditEnable { get; private set; }
        private static int EditInstanceId = 0;
        private static UnityEngine.Object EditObject = null;

        //=========================================================================================
        /// <summary>
        /// Reload Domain 対応
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            EditEnable = false;
            EditInstanceId = 0;
            EditObject = null;
        }

        //=========================================================================================
        /// <summary>
        /// ポイントタイプ情報
        /// </summary>
        private class PointType
        {
            /// <summary>
            /// 表示名
            /// </summary>
            public string label;

            /// <summary>
            /// 表示カラー
            /// </summary>
            public Color col;

            /// <summary>
            /// 設定値
            /// </summary>
            public int value;
        }

        /// <summary>
        /// ポイントタイプリスト
        /// </summary>
        List<PointType> pointTypeList = new List<PointType>();

        /// <summary>
        /// ポイントタイプ辞書
        /// </summary>
        Dictionary<int, PointType> value2typeDict = new Dictionary<int, PointType>(); // 設定値からの逆引き辞書

        /// <summary>
        /// ポイントデータ
        /// </summary>
        public class PointData
        {
            /// <summary>
            /// ポイント座標（ワールド）
            /// </summary>
            public Vector3 pos;

            /// <summary>
            /// データインデックス
            /// </summary>
            public int index;

            /// <summary>
            /// データ値
            /// </summary>
            public int value;

            /// <summary>
            /// Z距離（ソート用）
            /// </summary>
            public float distance;
        }

        /// <summary>
        /// ポイントデータリスト
        /// </summary>
        List<PointData> pointList = new List<PointData>();


        /// <summary>
        /// ポイントサイズ
        /// </summary>
        float pointSize = 0.01f;

        /// <summary>
        /// 最近点のみ選択
        /// </summary>
        bool selectNearest = false;

        /// <summary>
        /// 現在の編集値
        /// </summary>
        int selectPointType = 0;

        //=========================================================================================
        /// <summary>
        /// ウインドウ有効設定
        /// </summary>
        public void EnableEdit()
        {
            // データは毎回クリアする
            //Clear();
            //            SceneView.duringSceneGui += OnSceneView;
            //            SceneView.RepaintAll();
        }

        /// <summary>
        /// ウインドウ無効設定
        /// </summary>
        public void DisableEdit(UnityEngine.Object obj)
        {
            EndEdit(obj);
            //Clear();
            //            SceneView.duringSceneGui -= OnSceneView;
            //            SceneView.RepaintAll();
        }

        /// <summary>
        /// 選択開始
        /// </summary>
        void StartEdit(UnityEngine.Object obj)
        {
            if (EditEnable)
                return;
            Clear();
            EditEnable = true;
            EditInstanceId = obj.GetInstanceID();
            EditObject = obj;

            pointSize = EditorPrefs.GetFloat("PointSelector_PointSize", 0.01f);
            selectNearest = EditorPrefs.GetBool("PointSelector_SelectNearest", false);

            SceneView.duringSceneGui += OnSceneView;
            SceneView.RepaintAll();
        }

        /// <summary>
        /// 選択終了
        /// </summary>
        void EndEdit(UnityEngine.Object obj)
        {
            if (EditEnable == false)
                return;
            if (IsEdit(obj) == false)
                return;
            EditEnable = false;
            EditInstanceId = 0;
            EditObject = null;
            Clear();

            EditorPrefs.SetFloat("PointSelector_PointSize", pointSize);
            EditorPrefs.SetBool("PointSelector_SelectNearest", selectNearest);

            SceneView.duringSceneGui -= OnSceneView;
            SceneView.RepaintAll();
        }

        public bool IsEdit(UnityEngine.Object obj)
        {
            return EditEnable && EditInstanceId == obj.GetInstanceID();
        }

        //=========================================================================================
        /// <summary>
        /// 情報クリア
        /// </summary>
        void Clear()
        {
            // ポイントタイプ情報
            pointTypeList.Clear();
            value2typeDict.Clear();
            selectPointType = 0;

            // ポイント情報
            pointList.Clear();
        }

        //=========================================================================================
        /// <summary>
        /// ポイントタイプ追加
        /// </summary>
        /// <param name="label"></param>
        /// <param name="col"></param>
        /// <param name="value"></param>
        public void AddPointType(string label, Color col, int value)
        {
            if (value2typeDict.ContainsKey(value))
                return;

            PointType pt = new PointType();
            pt.label = label;
            pt.col = col;
            pt.value = value;
            pointTypeList.Add(pt);

            // 逆引き辞書登録
            value2typeDict[value] = pt;
        }

        /// <summary>
        /// ポイント追加
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="normal"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void AddPoint(Vector3 pos, int index, int value)
        {
            PointData p = new PointData();
            p.pos = pos;
            p.index = index;
            p.value = value;
            pointList.Add(p);
        }

        /// <summary>
        /// 現在のポイントリストを返す
        /// 最後のデータ反映に利用する
        /// </summary>
        /// <returns></returns>
        public List<PointData> GetPointList()
        {
            return pointList;
        }

        /// <summary>
        /// ポイントカラー取得
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        Color GetPointColor(int value)
        {
            PointType pt;
            if (value2typeDict.TryGetValue(value, out pt))
            {
                return pt.col;
            }
            return Color.black;
        }

        //=========================================================================================
        /// <summary>
        /// シーンビューハンドラ
        /// </summary>
        /// <param name="sceneView"></param>
        void OnSceneView(SceneView sceneView)
        {
            if (EditorApplication.isPlaying)
                return;

            if (EditEnable == false)
                return;

            if (EditObject == null)
            {
                return;
            }

            // シーンビュー・カメラ
            Camera cam = SceneView.currentDrawingSceneView.camera;
            Vector3 campos = cam.transform.position;
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Vector3 spos = ray.origin;
            Vector3 epos = spos + ray.direction * 1000.0f;

            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            // マウス選択
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && !Event.current.alt)
            {
                hitTest(spos, epos, pointSize * 0.5f);

                // シーンビューのエリア選択を出さないために、とりあえずこうするらしい
                GUIUtility.hotControl = controlId;
                Event.current.Use(); // ?
            }
            if (Event.current.type == EventType.MouseDrag && Event.current.button == 0 && !Event.current.alt)
            {
                hitTest(spos, epos, pointSize * 0.5f);

                Event.current.Use();
            }
            if (Event.current.type == EventType.MouseUp && Event.current.button == 0 && !Event.current.alt)
            {
                // マウスボタンUPでコントロールロックを解除するらしい
                GUIUtility.hotControl = 0;
                Event.current.Use(); // ?
            }

            if (Event.current.type == EventType.Repaint)
            {
                // Zソート
                ZSort(campos, cam.transform.forward);

                if (selectNearest == false)
                {
                    // Z test off
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                }
                else
                {
                    // Z test on
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                }

                // ポイント表示
                int pcnt = pointList.Count;
                for (int i = 0; i < pcnt; i++)
                {
                    var p = pointList[i];
                    Handles.color = GetPointColor(p.value);
                    Handles.SphereHandleCap(0, p.pos, Quaternion.identity, pointSize, EventType.Repaint);
                }
            }

            // リペイントしないと即座に反映しない
            if (GUI.changed)
            {
                // シーンビューのリペイント
                HandleUtility.Repaint();
            }
        }

        /// <summary>
        /// ポイントを置くから表示するためZソートする
        /// </summary>
        void ZSort(Vector3 campos, Vector3 camdir)
        {
            // カメラ距離計測
            int pcnt = pointList.Count;
            for (int i = 0; i < pcnt; i++)
            {
                var p = pointList[i];
                p.distance = Vector3.Distance(p.pos, campos);
            }

            // ソート（距離の降順なので注意！）
            pointList.Sort((a, b) => a.distance > b.distance ? -1 : 1);
        }

        /// <summary>
        /// カメラレイヒットテスト
        /// </summary>
        /// <param name="spos"></param>
        /// <param name="epos"></param>
        /// <param name="hitRadius"></param>
        /// <returns></returns>
        bool hitTest(Vector3 spos, Vector3 epos, float hitRadius)
        {
            // 手前からチェック
            bool change = false;
            int pcnt = pointList.Count;
            for (int i = pcnt - 1; i >= 0; i--)
            {
                var p = pointList[i];

                // マウスレイからの距離を求める
                float sqlen = SqDistPointSegment(spos, epos, p.pos);

                if (sqlen <= hitRadius * hitRadius)
                {
                    // ヒット！
                    // 数値変更
                    p.value = pointTypeList[selectPointType].value;
                    change = true;

                    // 最近点のみの場合はここで終了
                    if (selectNearest)
                        break;
                }
            }

            return change;
        }

        /// <summary>
        /// ■点Cと線分abの間の距離の平方を返す
        /// ゲームプログラミングのためのリアルタイム衝突判定 P.130
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        float SqDistPointSegment(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 bc = c - b;
            float e = Vector3.Dot(ac, ab);

            // Cがabの外側に射影される場合を扱う
            if (e <= 0)
                return Vector3.Dot(ac, ac);
            float f = Vector3.Dot(ab, ab);
            if (e >= f)
                return Vector3.Dot(bc, bc);

            // Cがab上に射影される場合を扱う
            return Vector3.Dot(ac, ac) - e * e / f;
        }

        //=========================================================================================
        /// <summary>
        /// カスタムGUI表示
        /// </summary>
        public void DrawInspectorGUI(
            UnityEngine.Object obj,
            System.Action<PointSelector> startAction,
            System.Action<PointSelector> endAction
            )
        {
            if (EditorApplication.isPlaying)
                return;

            EditorGUILayout.Space();

            if (EditEnable && IsEdit(obj) == false)
                return;

            bool change = false;

            using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.backgroundColor = Color.cyan;
                if (EditEnable == false && GUILayout.Button("Start Point Selection"))
                {
                    GUI.backgroundColor = Color.white;
                    StartEdit(obj);
                    if (startAction != null)
                        startAction(this);
                    change = true;
                }
                else if (EditEnable && GUILayout.Button("End Point Selection"))
                {
                    GUI.backgroundColor = Color.white;
                    if (endAction != null)
                        endAction(this);
                    EndEdit(obj);
                    change = true;
                }

                GUI.backgroundColor = Color.white;
                if (EditEnable && GUILayout.Button("Cancel Point Selection"))
                {
                    // 内容は反映せずに終了
                    EndEdit(obj);
                    change = true;
                }

                if (EditEnable)
                {
                    EditorGUILayout.Space();

                    // ポイントサイズスライダー
                    float psize = EditorGUILayout.Slider("Point Size", pointSize, 0.001f, 0.1f);
                    if (psize != pointSize)
                    {
                        pointSize = psize;
                        change = true;
                    }
                    EditorGUILayout.Space();

                    // 最近点のみ選択
                    EditorGUILayout.Space();
                    var oldSelectNearest = selectNearest;
                    selectNearest = EditorGUILayout.ToggleLeft("Z Test On & Select Near Point Only", selectNearest);
                    if (oldSelectNearest != selectNearest)
                        change = true;
                    EditorGUILayout.Space();

                    // ボタンカラーを変えたいのでGUILayout.Toolbar()は使わない
                    EditorGUILayout.LabelField("Point Type");
                    int tcnt = pointTypeList.Count;
                    Color bcol = GUI.backgroundColor;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        int nowtype = selectPointType;
                        for (int i = 0; i < tcnt; i++)
                        {
                            // カラー
                            GUI.backgroundColor = pointTypeList[i].col;
                            bool ret = GUILayout.Toggle(i == nowtype, pointTypeList[i].label, EditorStyles.miniButtonLeft);
                            if (ret)
                            {
                                nowtype = i;
                            }
                        }
                        if (nowtype != selectPointType)
                        {
                            selectPointType = nowtype;
                        }
                    }
                    GUI.backgroundColor = bcol;

                    // 全塗りつぶし
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Fill"))
                    {
                        foreach (var p in pointList)
                        {
                            p.value = pointTypeList[selectPointType].value;
                        }
                        change = true;
                    }
                }
            }

            // リペイント
            if (change)
            {
                SceneView.RepaintAll();
            }
        }
    }
}
