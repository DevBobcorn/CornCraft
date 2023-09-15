// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 頂点ペイントウインドウ
    /// </summary>
    [InitializeOnLoad]
    public class ClothPainter
    {
        public enum PaintMode
        {
            None,

            /// <summary>
            /// Move/Fixed/Ignore/Invalid
            /// </summary>
            Attribute,

            /// <summary>
            /// Max Distance/Backstop
            /// </summary>
            Motion,
        }

        static PaintMode paintMode = PaintMode.None;

        /// <summary>
        /// 編集対象のクロスコンポーネント
        /// </summary>
        static MagicaCloth cloth = null;

        /// <summary>
        /// 編集対象のクロスEditorクラス
        /// </summary>
        static MagicaClothEditor clothEditor = null;

        /// <summary>
        /// 編集対象のエディットメッシュ
        /// </summary>
        static VirtualMesh editMesh = null;

        /// <summary>
        /// 編集対象のセレクションデータ
        /// </summary>
        static SelectionData selectionData = null;

        /// <summary>
        /// 編集開始時のセレクションデータ（コピー）
        /// </summary>
        static SelectionData initSelectionData = null;

        internal const int WindowWidth = 200;
        internal const int WindowHeight = 200;

        //=========================================================================================
        const int PointFlag_Selecting = 1; // 選択中

        internal struct Point : IComparable<Point>
        {
            public int vindex;
            public float distance;
            public BitField32 flag;

            public int CompareTo(Point other)
            {
                if (distance != other.distance)
                    return distance < other.distance ? 1 : -1;
                else if (vindex != other.vindex)
                    return vindex < other.vindex ? 1 : -1;
                else
                    return 0;
            }
        }
        static NativeList<Point> dispPointList;
        static NativeArray<float3> pointWorldPositions;
        static VirtualMeshRaycastHit rayhit = default;
        static bool oldShowAll = false;
        static bool forceUpdate = false;

        //=========================================================================================
        static ClothPainter()
        {
            // シーンビューにGUIを描画するためのコールバック
            SceneView.duringSceneGui += OnGUI;
        }

        /// <summary>
        /// ペイント開始
        /// </summary>
        /// <param name="clothComponent"></param>
        public static void EnterPaint(PaintMode mode, MagicaClothEditor editor, MagicaCloth clothComponent, VirtualMesh vmesh, SelectionData sdata)
        {
            Develop.DebugLog($"EnterPaint");

            paintMode = mode;
            cloth = clothComponent;
            clothEditor = editor;
            editMesh = vmesh;
            selectionData = sdata;
            initSelectionData = sdata.Clone();
            rayhit = default;
            forceUpdate = true;

            // ポイントバッファ
            dispPointList = new NativeList<Point>(vmesh.VertexCount, Allocator.Persistent);
            pointWorldPositions = new NativeArray<float3>(vmesh.VertexCount, Allocator.Persistent);

            // UndoRedoコールバック
            Undo.undoRedoPerformed += UndoRedoCallback;
        }

        /// <summary>
        /// ペイント終了
        /// </summary>
        public static void ExitPaint()
        {
            Develop.DebugLog($"ExitPaint");

            cloth = null;
            clothEditor = null;
            editMesh = null;
            selectionData = null;
            initSelectionData = null;
            rayhit = default;

            if (dispPointList.IsCreated)
                dispPointList.Dispose();
            if (pointWorldPositions.IsCreated)
                pointWorldPositions.Dispose();

            // UndoRedoコールバック
            Undo.undoRedoPerformed -= UndoRedoCallback;
        }

        /// <summary>
        /// Undo/Redo実行後のコールバック
        /// </summary>
        static void UndoRedoCallback()
        {
            //Develop.DebugLog($"Undo Redo!");

            if (EditorApplication.isPlaying)
                return;

            if (cloth == null || editMesh == null || selectionData == null || editMesh.IsSuccess == false)
                return;

            // セレクションデータを取り直す
            selectionData = clothEditor.GetSelectionData(cloth, editMesh);

            forceUpdate = true;
        }

        //=========================================================================================
        /// <summary>
        /// 指定クロスコンポーネントを編集中かどうか
        /// </summary>
        /// <param name="clothComponent"></param>
        /// <returns></returns>
        public static bool HasEditCloth(MagicaCloth clothComponent)
        {
            return cloth == clothComponent;
        }

        /// <summary>
        /// 編集中かどうか
        /// </summary>
        /// <returns></returns>
        public static bool IsPainting()
        {
            return cloth != null;
        }

        //=========================================================================================
        static void OnGUI(SceneView sceneView)
        {
            if (EditorApplication.isPlaying)
                return;

            // アクティブシーンビュー判定
            if (SceneView.lastActiveSceneView != sceneView)
                return;

            if (cloth == null || editMesh == null || selectionData == null)
                return;
            if (editMesh.IsSuccess == false || selectionData.IsValid() == false)
                return;

            var windata = ScriptableSingleton<ClothPainterWindowData>.instance;

            // シーンビューカメラ
            Camera cam = SceneView.currentDrawingSceneView.camera;
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Vector3 spos = ray.origin;
            Vector3 epos = spos + ray.direction * 1000.0f;

            // 透過カーソル用
            float rayAngle = Vector3.Angle(cam.transform.forward, ray.direction);
            float nearPlaneDistance = cam.orthographic ? cam.nearClipPlane : cam.nearClipPlane / math.cos(math.radians(rayAngle));
            float nearPlaneVSize = cam.orthographic ? cam.orthographicSize : math.tan(math.radians(cam.fieldOfView * 0.5f)) * cam.nearClipPlane * 2;
            float brushSizeThrough = nearPlaneVSize * windata.brushSizeThrough;

            // カーソルのviewportポイント
            var sp = HandleUtility.GUIPointToScreenPixelCoordinate(Event.current.mousePosition);
            var cursorViewpos = cam.ScreenToViewportPoint(sp);
            cursorViewpos = math.remap(new float3(0), new float3(1), new float3(-1), new float3(1), cursorViewpos); // 射影行列(-1 ~ +1)に変換

            // カメラ座標をローカル空間に変換する
            var t = cloth.ClothTransform; // コンポーネント空間

            bool repaint = false;
            bool updatePoint = false;

            // ポイントサイズ
            float pointSize = windata.drawPointSize;

            bool showAll = windata.backFaceCulling == false;

            // マウス移動中は常にメッシュとの交差判定
            if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDrag || oldShowAll != showAll || forceUpdate)
            {
                oldShowAll = showAll;
                forceUpdate = false;
                updatePoint = true;
            }

            // マウス選択
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && !Event.current.alt)
            {
                // ペイント適用
                ApplyPaint(windata, dispPointList);

                // シーンビューのエリア選択を出さないために、とりあえずこうするらしい
                int controlId = GUIUtility.GetControlID(FocusType.Passive);
                GUIUtility.hotControl = controlId;
                Event.current.Use(); // ?
            }
            if (Event.current.type == EventType.MouseDrag && Event.current.button == 0 && !Event.current.alt)
            {
                // ペイント適用
                ApplyPaint(windata, dispPointList);

                Event.current.Use();
            }
            if (Event.current.type == EventType.MouseUp && Event.current.button == 0 && !Event.current.alt)
            {
                // ペイント結果を適用する
                ApplySelectionData();

                // マウスボタンUPでコントロールロックを解除するらしい
                GUIUtility.hotControl = 0;
                Event.current.Use(); // ?
            }

            // Handlesの描画はGUIの前でなければならない.理由は不明
            if (Event.current.type == EventType.Repaint)
            {
                // ディスクの描画
                if (windata.through)
                {
                    // 透過モード
                    Handles.matrix = Matrix4x4.identity;
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                    var dpos = spos + ray.direction * nearPlaneDistance;
                    var dnor = cam.transform.forward;
                    Handles.color = new Color(0.6f, 0.2f, 1.0f, 0.3f);
                    Handles.DrawSolidDisc(dpos, dnor, brushSizeThrough);
                }
                else if (rayhit.IsValid())
                {
                    Handles.matrix = Matrix4x4.identity;
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                    var dpos = t.TransformPoint(rayhit.position);
                    var dnor = t.TransformDirection(rayhit.normal);
                    Handles.color = new Color(0.3f, 0.5f, 1.0f, 0.4f);
                    Handles.DrawSolidDisc(dpos, dnor, windata.brushSize);
                }

                // ポイント表示
                Handles.lighting = true;
                //Handles.zTest = showAll ? UnityEngine.Rendering.CompareFunction.Always : UnityEngine.Rendering.CompareFunction.LessEqual;
                //Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                Handles.zTest = windata.zTest ? UnityEngine.Rendering.CompareFunction.LessEqual : UnityEngine.Rendering.CompareFunction.Always;
                Handles.matrix = Matrix4x4.identity;
                int cnt = dispPointList.Length;
                for (int i = 0; i < cnt; i++)
                {
                    var point = dispPointList[i];

                    Color col = Color.black;
                    var attr = selectionData.attributes[point.vindex];
                    switch (paintMode)
                    {
                        case PaintMode.Attribute:
                            if (attr.IsMove()) col = Color.green;
                            else if (attr.IsFixed()) col = Color.red;
                            //else if (attr.IsIgnore()) col = Color.blue;
                            else col = Color.gray;
                            break;
                        case PaintMode.Motion:
                            if (attr.IsMotion()) col = Color.cyan;
                            else col = Color.gray;
                            break;
                    }

                    // 選択中
                    if (point.flag.IsSet(PointFlag_Selecting))
                    {
                        col = Color.yellow;
                    }

                    var pos = pointWorldPositions[point.vindex];
                    Handles.color = col;
                    Handles.SphereHandleCap(0, pos, Quaternion.identity, windata.drawPointSize, EventType.Repaint);
                }

                // ペイント中のギズモ表示
                if (windata.showShape)
                    ClothEditorUtility.DrawClothEditor(editMesh, ClothEditorUtility.PaintSettings, cloth.SerializeData, true, windata.backFaceCulling, true);
            }

            // GUI
            //sceneView.BeginWindows();
            string title = string.Empty;
            switch (paintMode)
            {
                case PaintMode.Attribute:
                    title = "Movement attribute paint";
                    break;
                case PaintMode.Motion:
                    title = "Max Distance/Backstop paint";
                    break;
            }
            var rect = GUILayout.Window(71903439, windata.windowRect, DoWindow, title);
            //Debug.Log(windata.windowRect);
            // ウインドウをSceneViewの領域内にクランプする
            // sceneView.positionでrectが取れる
            rect.x = Mathf.Clamp(rect.x, 0, Mathf.Max(sceneView.position.width - rect.width, 0));
            rect.y = Mathf.Clamp(rect.y, 0, Mathf.Max(sceneView.position.height - rect.height - 25, 0));
            rect.width = WindowWidth;
            rect.height = WindowHeight;
            windata.windowRect = rect;
            //sceneView.EndWindows();

            // ポイントデータ更新
            if (updatePoint)
            {
                UpdatePoint(
                    windata.through,
                    t, cam, ray, showAll,
                    windata.through ? windata.brushSizeThrough : windata.brushSize,
                    windata.through ? cursorViewpos : t.TransformPoint(rayhit.position),
                    windata.drawPointSize
                    );
                repaint = true;
            }

            // 画面リフレッシュ
            if (repaint)
                sceneView.Repaint();
        }

        /// <summary>
        /// ポイントデータを更新する
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="cam"></param>
        /// <param name="ray"></param>
        /// <param name="showAll"></param>
        /// <param name="brushSize"></param>
        static void UpdatePoint(bool through, Transform ct, Camera cam, Ray ray, bool showAll, float brushSize, float3 brushPos, float pointSize)
        {
            // 表示頂点選別
            CreateDispPointList(through, ct, cam, showAll, brushSize, brushPos).Complete();

            // サーフェース交差判定
            rayhit = editMesh.IntersectRayMesh(ray.origin, ray.direction, showAll, pointSize);
        }

        static JobHandle CreateDispPointList(
            bool through, Transform ct, Camera cam,
            bool showAll, float brushSize, float3 brushPosition, JobHandle jobHandle = default
            )
        {
            dispPointList.Clear();

            int vcnt = editMesh.VertexCount;
            if (vcnt == 0)
                return jobHandle;

            // ローカルカメラ
            var localCameraDirection = ct.InverseTransformDirection(cam.transform.forward);

            // 頂点のワールド変換と表示頂点の選別
            var job = new CreateDispPointListJob()
            {
                through = through,
                LtoW = ct.localToWorldMatrix,
                showAll = showAll,

                cameraPosition = cam.transform.position,
                cameraDirection = localCameraDirection,

                cameraProjectionMatrix = cam.projectionMatrix,
                worldToCameraMatrix = cam.worldToCameraMatrix,
                cameraAspectRatio = cam.aspect,

                useBrush = through || rayhit.IsValid(),
                brushPosition = brushPosition,
                brushSize = brushSize,

                localPositions = editMesh.localPositions.GetNativeArray(),
                localNormals = editMesh.localNormals.GetNativeArray(),
                vertexToTriangles = editMesh.vertexToTriangles,

                pointWorldPositions = pointWorldPositions,
                dispPointList = dispPointList.AsParallelWriter(),
            };
            jobHandle = job.Schedule(vcnt, 16, jobHandle);

            // 距離ソート
            var job2 = new SortDispPointJob()
            {
                dispPointList = dispPointList,
            };
            jobHandle = job2.Schedule(jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct CreateDispPointListJob : IJobParallelFor
        {
            public bool through;
            public float4x4 LtoW;
            public bool showAll;

            public float3 cameraPosition;
            public float3 cameraDirection;
            public float4x4 cameraProjectionMatrix;
            public float4x4 worldToCameraMatrix;
            public float cameraAspectRatio;

            public bool useBrush;
            public float3 brushPosition;
            public float brushSize;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<FixedList32Bytes<uint>> vertexToTriangles;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> pointWorldPositions;
            [Unity.Collections.WriteOnly]
            public NativeList<Point>.ParallelWriter dispPointList;

            public void Execute(int vindex)
            {
                // ワールド座標
                var lpos = localPositions[vindex];
                var wpos = math.transform(LtoW, lpos);
                pointWorldPositions[vindex] = wpos;

                // カメラ距離
                float dist = math.distance(cameraPosition, wpos);

                // 表示選別
                bool show;
                if (showAll)
                {
                    show = true;
                }
                else
                {
                    // 頂点がトライアングルに属さない場合は無条件で表示する
                    if (vertexToTriangles[vindex].Length == 0)
                        show = true;
                    else
                    {
                        // トライアングルに属する頂点は法線が画面に向いているもののみ表示
                        show = math.dot(cameraDirection, localNormals[vindex]) < 0.0f;
                    }
                }

                if (show)
                {
                    var flag = new BitField32();

                    // ブラシ範囲内に存在するか判定
                    if (useBrush)
                    {
                        if (through)
                        {
                            // 透過モード
                            // 射影変換
                            var cpos = math.mul(worldToCameraMatrix, new float4(wpos, 1));
                            var vpos = math.mul(cameraProjectionMatrix, new float4(cpos.xyz, 1));
                            vpos /= vpos.w; // wで割る必要あり

                            // ブラシはアスペクト比を考慮した楕円で判定する必要あり
                            // brushPositionはviewport座標を指す
                            float2 v = vpos.xy - brushPosition.xy;
                            v.x *= cameraAspectRatio;
                            var bdist = math.length(v);
                            if (bdist <= brushSize)
                                flag.SetBits(PointFlag_Selecting, true);
                        }
                        else
                        {
                            // サーフェス選択モード
                            var bdist = math.distance(brushPosition, wpos);
                            if (bdist <= brushSize)
                                flag.SetBits(PointFlag_Selecting, true);
                        }
                    }

                    dispPointList.AddNoResize(new Point() { vindex = vindex, distance = dist, flag = flag });
                }
            }
        }

        [BurstCompile]
        struct SortDispPointJob : IJob
        {
            public NativeList<Point> dispPointList;

            public void Execute()
            {
                if (dispPointList.Length > 1)
                    dispPointList.Sort();
            }
        }

        /// <summary>
        /// 渡されたpointListをカメラからの距離の降順にソートして返す
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="positonOffset"></param>
        /// <param name="LtoW">positionをワールド座標変換するマトリックス</param>
        /// <param name="camPos">カメラワールド座標</param>
        /// <param name="pointList"></param>
        internal static void CalcPointCameraDistance(NativeArray<float3> positions, int positonOffset, float4x4 LtoW, float3 camPos, NativeList<Point> pointList)
        {
            // ポイントのカメラからの距離を求める
            int cnt = pointList.Length;
            if (cnt <= 1)
                return;

            var job = new CalcCameraDistanceJob()
            {
                LtoW = LtoW,
                cameraPosition = camPos,
                positions = positions,
                pointList = pointList,
                positionOffset = positonOffset,
            };
            job.Run(cnt);

            // 距離ソート
            var job2 = new SortDispPointJob()
            {
                dispPointList = pointList,
            };
            job2.Run();
        }


        [BurstCompile]
        struct CalcCameraDistanceJob : IJobParallelFor
        {
            public float4x4 LtoW;

            public float3 cameraPosition;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;

            [NativeDisableParallelForRestriction]
            public NativeList<Point> pointList;
            public int positionOffset;

            public void Execute(int index)
            {
                var point = pointList[index];

                // ワールド座標
                var lpos = positions[positionOffset + point.vindex];
                var wpos = math.transform(LtoW, lpos);

                // カメラ距離
                float dist = math.distance(cameraPosition, wpos);
                point.distance = dist;

                pointList[index] = point;
            }
        }

        /// <summary>
        /// 選択中ポイントに属性を付与する
        /// </summary>
        /// <param name="attribute"></param>
        static void ApplyPaint(ClothPainterWindowData windata, NativeList<Point> applyPointList)
        {
            int cnt = applyPointList.Length;
            for (int i = 0; i < cnt; i++)
            {
                var point = applyPointList[i];
                if (point.flag.IsSet(PointFlag_Selecting) == false)
                    continue;

                var attr = selectionData.attributes[point.vindex];
                bool change = false;

                switch (paintMode)
                {
                    case PaintMode.Attribute:
                        // 移動/固定
                        if (windata.editAttribute == 0 && attr.IsMove() == false)
                        {
                            attr.SetFlag(VertexAttribute.Flag_Move, true);
                            attr.SetFlag(VertexAttribute.Flag_Fixed, false);
                            //attr.SetFlag(VertexAttribute.Flag_Ignore, false);
                            change = true;
                        }
                        else if (windata.editAttribute == 1 && attr.IsFixed() == false)
                        {
                            attr.SetFlag(VertexAttribute.Flag_Move, false);
                            attr.SetFlag(VertexAttribute.Flag_Fixed, true);
                            //attr.SetFlag(VertexAttribute.Flag_Ignore, false);
                            change = true;
                        }
                        //else if (windata.editAttribute == 2 && attr.IsIgnore() == false)
                        //{
                        //    attr.SetFlag(VertexAttribute.Flag_Move, false);
                        //    attr.SetFlag(VertexAttribute.Flag_Fixed, false);
                        //    attr.SetFlag(VertexAttribute.Flag_Ignore, true);
                        //    change = true;
                        //}
                        else if (windata.editAttribute == 3 && attr.IsInvalid() == false)
                        {
                            attr.SetFlag(VertexAttribute.Flag_Move, false);
                            attr.SetFlag(VertexAttribute.Flag_Fixed, false);
                            //attr.SetFlag(VertexAttribute.Flag_Ignore, false);
                            change = true;
                        }

                        break;
                    case PaintMode.Motion:
                        // MaxDistance/Backstop
                        if (windata.editMotion == 0 && attr.IsMotion() == false)
                        {
                            attr.SetFlag(VertexAttribute.Flag_InvalidMotion, false);
                            change = true;
                        }
                        else if (windata.editMotion == 1 && attr.IsMotion())
                        {
                            attr.SetFlag(VertexAttribute.Flag_InvalidMotion, true);
                            change = true;
                        }
                        break;
                }

                if (change)
                    selectionData.attributes[point.vindex] = attr;
            }
        }

        /// <summary>
        /// セレクションデータをシリアライズする
        /// </summary>
        static void ApplySelectionData()
        {
            // セレクションデータデータをシリアライズ化する
            selectionData.userEdit = true; // ユーザー編集フラグを立てる
            clothEditor?.ApplyClothPainter(selectionData);

            // Undo/Redoの状態を切り替えるためセレクションデータのクローンを作成して切り替える
            selectionData = selectionData.Clone();
        }

        /// <summary>
        /// 塗りつぶし
        /// </summary>
        /// <param name="windata"></param>
        static void Fill(ClothPainterWindowData windata)
        {
            // 塗りつぶし用のポイントデータを作成する
            int vcnt = editMesh.VertexCount;
            using var fillDispPointList = new NativeList<Point>(vcnt, Allocator.TempJob);
            BitField32 flag = new();
            flag.SetBits(PointFlag_Selecting, true);
            for (int i = 0; i < vcnt; i++)
            {
                var p = new Point()
                {
                    vindex = i,
                    distance = 0,
                    flag = flag,
                };
                fillDispPointList.Add(p);
            }

            // セレクションデータへ反映
            ApplyPaint(windata, fillDispPointList);

            // シリアライズ
            ApplySelectionData();
        }


        static void DoWindow(int unusedWindowID)
        {
            var windata = ScriptableSingleton<ClothPainterWindowData>.instance;

            //Debug.Log(windata.windowRect);

            //float lineHight = EditorGUIUtility.singleLineHeight;

            // ポイントサイズスライダー
            const float sliderLableWidth = 80;
            const float sliderWidth = 180;
            using (var h = new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Point Size", GUILayout.Width(sliderLableWidth));
                windata.drawPointSize = EditorGUILayout.Slider(GUIContent.none, windata.drawPointSize, 0.001f, 0.1f, GUILayout.Width(sliderWidth));
            }

            // ブラシサイズスライダー
            using (var h = new EditorGUILayout.HorizontalScope())
            {
                if (windata.through)
                {
                    EditorGUILayout.LabelField("Brush Size T", GUILayout.Width(sliderLableWidth));
                    windata.brushSizeThrough = EditorGUILayout.Slider(GUIContent.none, windata.brushSizeThrough, 0.01f, 0.3f, GUILayout.Width(sliderWidth));
                }
                else
                {
                    EditorGUILayout.LabelField("Brush Size", GUILayout.Width(sliderLableWidth));
                    windata.brushSize = EditorGUILayout.Slider(GUIContent.none, windata.brushSize, 0.001f, 0.2f, GUILayout.Width(sliderWidth));
                }
            }

            EditorGUILayout.Space();

            const float toggleLableWidth = 80;
            const float toggleButtonWidth = 30;
            using (var h = new EditorGUILayout.HorizontalScope())
            {
                // 形状表示
                EditorGUILayout.LabelField("Shape", GUILayout.Width(toggleLableWidth));
                windata.showShape = EditorGUILayout.Toggle(GUIContent.none, windata.showShape, GUILayout.Width(toggleButtonWidth));

                // 全表示
                EditorGUILayout.LabelField("Culling", GUILayout.Width(toggleLableWidth));
                windata.backFaceCulling = EditorGUILayout.Toggle(GUIContent.none, windata.backFaceCulling, GUILayout.Width(toggleButtonWidth));
            }

            using (var h = new EditorGUILayout.HorizontalScope())
            {
                // Zテスト
                EditorGUILayout.LabelField("Z-Test", GUILayout.Width(toggleLableWidth));
                windata.zTest = EditorGUILayout.Toggle(GUIContent.none, windata.zTest, GUILayout.Width(toggleButtonWidth));

                // 透過
                EditorGUILayout.LabelField("Through", GUILayout.Width(toggleLableWidth));
                windata.through = EditorGUILayout.Toggle(GUIContent.none, windata.through, GUILayout.Width(toggleButtonWidth));
            }

            // 属性ボタン
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Paint Attribute");
            Color bcol = GUI.backgroundColor;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (paintMode == PaintMode.Attribute)
                {
                    // Move/Fixed
                    int nowAttr = windata.editAttribute;
                    for (int i = 0; i < 4; i++)
                    {
                        // !現在Ignoreはオミット
                        if (i == 2)
                            continue;

                        Color col = Color.black;
                        string title = string.Empty;
                        switch (i)
                        {
                            case 0: // move
                                col = Color.green;
                                title = "Move";
                                break;
                            case 1: // fixed
                                col = Color.red;
                                title = "Fixed";
                                break;
                            case 2: // ignore
                                col = Color.blue;
                                title = "Ignore";
                                break;
                            case 3: // invalid
                                col = Color.gray;
                                title = "Invalid";
                                break;
                        }
                        GUI.backgroundColor = i == nowAttr ? col : Color.gray;
                        bool ret = GUILayout.Toggle(i == nowAttr, title, EditorStyles.miniButton);
                        if (ret)
                        {
                            nowAttr = i;
                        }
                    }
                    windata.editAttribute = nowAttr;
                }
                else if (paintMode == PaintMode.Motion)
                {
                    // MaxDistance/Backstop
                    int nowAttr = windata.editMotion;
                    for (int i = 0; i < 2; i++)
                    {
                        // カラー
                        var col = i == 0 ? Color.cyan : Color.red;
                        GUI.backgroundColor = i == nowAttr ? col : Color.gray;
                        bool ret = GUILayout.Toggle(i == nowAttr, i == 0 ? "Valid" : "Invalid", EditorStyles.miniButton);
                        if (ret)
                        {
                            nowAttr = i;
                        }
                    }
                    windata.editMotion = nowAttr;
                }
            }
            GUI.backgroundColor = bcol;

            // 適用/キャンセルボタン
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                // 塗りつぶしボタン
                if (GUILayout.Button("Fill", GUILayout.Width(70)))
                {
                    Fill(windata);
                }

                GUILayout.FlexibleSpace();

                // ペイント終了ボタン
                if (GUILayout.Button("Exit"))
                {
                    // ペイント終了フラグ
                    cloth = null;

                    // この編集によりセレクションデータに変更があった場合はProxyMeshをリビルドする
                    if (initSelectionData.Compare(selectionData) == false)
                    {
                        Develop.DebugLog($"Change selection data!");
                        clothEditor?.UpdateEditMesh();
                    }

                    ExitPaint();
                }
                EditorGUILayout.Space();
            }

            GUI.DragWindow();
        }
    }

    //=============================================================================================
    /// <summary>
    /// ペイントウインドウの保持データ
    /// ScriptableSingletonを利用することによりエディタ終了時までデータを保持できる
    /// </summary>
    public class ClothPainterWindowData : ScriptableSingleton<ClothPainterWindowData>
    {
        /// <summary>
        /// ウインドウの位置とサイズ
        /// </summary>
        public Rect windowRect = new Rect(100, 100, ClothPainter.WindowWidth, ClothPainter.WindowHeight);

        /// <summary>
        /// 表示ポイントサイズ
        /// </summary>
        public float drawPointSize = 0.02f;

        /// <summary>
        /// ブラシサイズ
        /// </summary>
        public float brushSize = 0.05f;

        /// <summary>
        /// ブラシサイズ（透過モード時）
        /// シーンカメラ垂直サイズの(%)
        /// </summary>
        public float brushSizeThrough = 0.1f;

        /// <summary>
        /// 裏面カリング
        /// </summary>
        public bool backFaceCulling = true;

        /// <summary>
        /// 形状を表示
        /// </summary>
        public bool showShape = true;

        /// <summary>
        /// Zテスト
        /// </summary>
        public bool zTest = false;

        /// <summary>
        /// 透過モード
        /// </summary>
        public bool through = false;

        /// <summary>
        /// 現在アクティブなポイント属性(0=Move/1=Fixed/2=Ignore/3=Invalid)
        /// VertexAttributeとは異なるので注意！
        /// </summary>
        public int editAttribute = 0;

        /// <summary>
        /// 現在アクティブなモーション制約(0=Valid, 1=Invalid)
        /// </summary>
        public int editMotion = 0;
    }
}
