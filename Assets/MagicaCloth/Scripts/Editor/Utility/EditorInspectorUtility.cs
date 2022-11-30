// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// Editorインスペクタ表示に関するユーティリティ
    /// </summary>
    public static class EditorInspectorUtility
    {
        /// <summary>
        /// 現在のデータ状態をインスペクタにヘルプボックスで表示する
        /// </summary>
        /// <param name="istatus"></param>
        public static void DispDataStatus(IDataVerify verify)
        {
            if (verify == null)
                return;

            var code = verify.VerifyData();
            //bool valid = verify.VerifyData() == Define.Error.None;
            //var mestype = valid ? MessageType.Info : MessageType.Error;
            var mestype = MessageType.Info;
            if (Define.IsWarning(code))
                mestype = MessageType.Warning;
            if (Define.IsError(code))
                mestype = MessageType.Error;

            EditorGUILayout.HelpBox(verify.GetInformation(), mestype);
        }

        /// <summary>
        /// 現在のデータバージョン状態をインスペクタにヘルプボックスで表示する
        /// </summary>
        /// <param name="core"></param>
        public static void DispVersionStatus(CoreComponent core)
        {
            // Data Version
            var code = core.VerifyDataVersion();
            if (Define.IsNormal(code) == false)
                EditorGUILayout.HelpBox(Define.GetErrorMessage(code), MessageType.Warning);

            // Algorithm Version
            var cloth = core as BaseCloth;
            if (cloth != null)
            {
                code = cloth.VerifyAlgorithmVersion();
                if (Define.IsNormal(code) == false)
                    EditorGUILayout.HelpBox(Define.GetErrorMessage(code), MessageType.Warning);
            }
        }

        //===============================================================================
        /// <summary>
        /// ベジェ曲線パラメータのインスペクタ描画と変更操作
        /// </summary>
        /// <param name="title">パラメータ名</param>
        /// <param name="bval">ベジェ曲線パラメータクラス</param>
        /// <param name="minVal">ベジェ曲線の最小値</param>
        /// <param name="maxVal">ベジェ曲線の最大値</param>
        /// <param name="valFmt">パラメータ表示浮動小数点数フォーマット</param>
        /// <returns></returns>
        public static void BezierInspector(
            string title,
            SerializedProperty bval,
            float minVal,
            float maxVal,
            string valFmt = "F2"
            )
        {
            var startValue = bval.FindPropertyRelative("startValue");
            var endValue = bval.FindPropertyRelative("endValue");
            var curveValue = bval.FindPropertyRelative("curveValue");
            var useEndValue = bval.FindPropertyRelative("useEndValue");
            var useCurveValue = bval.FindPropertyRelative("useCurveValue");

            // グラフ描画
            float sv, ev, cv;
            GetBezierValue(bval, out sv, out ev, out cv);
            DrawGraph(sv, ev, cv, minVal, maxVal, valFmt);

            // パラメータ
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(32));
            EditorGUILayout.Slider(startValue, minVal, maxVal, "start");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            useEndValue.boolValue = EditorGUILayout.Toggle(useEndValue.boolValue, GUILayout.Width(32));
            EditorGUI.BeginDisabledGroup(!useEndValue.boolValue);
            EditorGUILayout.Slider(endValue, minVal, maxVal, "end");
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            useCurveValue.boolValue = EditorGUILayout.Toggle(useCurveValue.boolValue, GUILayout.Width(32));
            EditorGUI.BeginDisabledGroup(!useCurveValue.boolValue || !useEndValue.boolValue);
            EditorGUILayout.Slider(curveValue, -1.0f, 1.0f, "curve");
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        static void GetBezierValue(SerializedProperty bval, out float start, out float end, out float curve)
        {
            var startValue = bval.FindPropertyRelative("startValue");
            var endValue = bval.FindPropertyRelative("endValue");
            var curveValue = bval.FindPropertyRelative("curveValue");
            var useEndValue = bval.FindPropertyRelative("useEndValue");
            var useCurveValue = bval.FindPropertyRelative("useCurveValue");

            start = startValue.floatValue;
            end = useEndValue.boolValue ? endValue.floatValue : start;
            curve = useCurveValue.boolValue && useEndValue.boolValue ? curveValue.floatValue : 0.0f;
        }

        // ベジェ曲線グラフ描画
        static void DrawGraph(float startVal, float endVal, float curveVal, float minVal, float maxVal, string valFmt)
        {
            EditorGUILayout.Space();

            // 表示領域
            const float headOffsetX = 40;
            const float tailOffsetX = 10;
            float w = GUILayoutUtility.GetLastRect().width;
            //Rect drect = GUILayoutUtility.GetRect(w, 100f);
            Rect drect = GUILayoutUtility.GetRect(w, 120f);
            //float indentWidth = EditorGUI.indentLevel * 16;
            //drect.x += indentWidth;
            //drect.width -= indentWidth;
            Rect area = new Rect(drect.x + headOffsetX, drect.y, drect.width - headOffsetX - tailOffsetX, drect.height);

            // grid
            Handles.color = new Color(1f, 1f, 1f, 0.5f);
            Handles.DrawLine(new Vector2(area.xMin, area.yMin), new Vector2(area.xMax, area.yMin));
            Handles.DrawLine(new Vector2(area.xMin, area.yMax), new Vector2(area.xMax, area.yMax));
            Handles.DrawLine(new Vector2(area.xMin, area.yMin), new Vector2(area.xMin, area.yMax));
            Handles.DrawLine(new Vector2(area.xMax, area.yMin), new Vector2(area.xMax, area.yMax));

            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            Handles.DrawLine(new Vector2(area.xMin, (area.yMin + area.yMax) * 0.5f), new Vector2(area.xMax, (area.yMin + area.yMax) * 0.5f));
            Handles.DrawLine(new Vector2((area.xMin + area.xMax) * 0.5f, area.yMin), new Vector2((area.xMin + area.xMax) * 0.5f, area.yMax));


            // grid数値
            Handles.Label(new Vector3(drect.xMin, drect.yMin - 4), maxVal.ToString("F1"));
            Handles.Label(new Vector3(drect.xMin, drect.yMax - 12), minVal.ToString("F1"));

            // データ領域
            Rect vrect = new Rect(0.0f, minVal, 1.0f, maxVal - minVal);

            // データ領域での位置
            Vector3 svpos = Rect.PointToNormalized(vrect, new Vector2(0.0f, startVal));
            Vector3 evpos = Rect.PointToNormalized(vrect, new Vector2(1.0f, endVal));

            // 表示のためyを逆転
            svpos.y = 1.0f - svpos.y;
            evpos.y = 1.0f - evpos.y;

            // グラフ上の座標
            Vector3 spos = Rect.NormalizedToPoint(area, svpos);
            Vector3 epos = Rect.NormalizedToPoint(area, evpos);

            // 対角線計算用
            Vector3 spos0 = Rect.NormalizedToPoint(area, new Vector2(0.0f, evpos.y));
            Vector3 epos0 = Rect.NormalizedToPoint(area, new Vector2(1.0f, svpos.y));

            // ベジェ制御点（対角線を補間）
            Vector3 stan = Vector3.Lerp(spos0, epos0, curveVal * 0.5f + 0.5f);
            Vector3 etan = stan;

            // ベジェ描画
            Color bcol = GUI.enabled ? Color.green : new Color(0.0f, 0.5f, 0.0f, 1.0f);
            Handles.DrawBezier(spos, epos, stan, etan, bcol, null, 2.0f);

            // 両端値
            Handles.Label(spos, startVal.ToString(valFmt));
            Handles.Label(epos + new Vector3(-38, 0, 0), endVal.ToString(valFmt));

#if false
            // 制御点描画
            Handles.color = Color.red;
            Handles.DrawWireCube(stan, Vector3.one * 2);
#endif
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        //===============================================================================
        /// <summary>
        /// 折りたたみ制御
        /// </summary>
        /// <param name="foldKey">折りたたみ保存キー</param>
        /// <param name="title"></param>
        /// <param name="drawAct">内容描画アクション</param>
        /// <param name="enableAct">有効フラグアクション(null=無効)</param>
        /// <param name="enable">現在の有効フラグ</param>
        public static void Foldout(
            string foldKey,
            string title = null,
            System.Action drawAct = null,
            System.Action<bool> enableAct = null,
            bool enable = false,
            bool warning = false
            )
        {
            var style = new GUIStyle("ShurikenModuleTitle");
            style.font = new GUIStyle(EditorStyles.label).font;
            style.border = new RectOffset(15, 7, 4, 4);
            style.fixedHeight = 22;
            style.contentOffset = new Vector2(20f, -2f);

            var rect = GUILayoutUtility.GetRect(16f, 22f, style);

            GUI.backgroundColor = warning ? Color.yellow : Color.white;
            GUI.Box(rect, title, style);
            GUI.backgroundColor = Color.white;

            var e = Event.current;
            bool foldOut = EditorPrefs.GetBool(foldKey);

            if (enableAct == null)
            {
                if (e.type == EventType.Repaint)
                {
                    var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
                    EditorStyles.foldout.Draw(toggleRect, false, false, foldOut, false);
                }
            }
            else
            {
                // 有効チェック
                var toggleRect = new Rect(rect.x + 4f, rect.y + 4f, 13f, 13f);
                bool sw = GUI.Toggle(toggleRect, enable, string.Empty, new GUIStyle("ShurikenCheckMark"));
                if (sw != enable)
                {
                    enableAct(sw);
                }
            }

            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                foldOut = !foldOut;
                EditorPrefs.SetBool(foldKey, foldOut);
                e.Use();
            }

            if (foldOut && drawAct != null)
            {
                drawAct();
            }
        }

        /// <summary>
        /// タイトルバーのみ表示
        /// </summary>
        /// <param name="title"></param>
        /// <param name="warning"></param>
        public static void TitleBar(string title, bool warning)
        {
            var style = new GUIStyle("ShurikenModuleTitle");
            style.font = new GUIStyle(EditorStyles.label).font;
            style.border = new RectOffset(15, 7, 4, 4);
            style.fixedHeight = 22;
            style.contentOffset = new Vector2(20f, -2f);

            var rect = GUILayoutUtility.GetRect(16f, 22f, style);

            GUI.backgroundColor = warning ? Color.yellow : Color.white;
            GUI.Box(rect, title, style);
            GUI.backgroundColor = Color.white;
        }

        //===============================================================================
        static bool MinMaxCurveInspector(string title, string valueName, SerializedProperty bval, float minval, float maxval)
        {
            EditorGUI.BeginChangeCheck();

            float sv, ev, cv;
            GetBezierValue(bval, out sv, out ev, out cv);

            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", sv, "/", ev, "]");

            Foldout(title, StaticStringBuilder.ToString(), () =>
            {
                if (string.IsNullOrEmpty(valueName) == false)
                    EditorGUILayout.LabelField(valueName);
                BezierInspector(title, bval, minval, maxval);
            });

            return EditorGUI.EndChangeCheck();
        }

        static bool UseMinMaxCurveInspector(
            string title,
            SerializedProperty use,
            string valueName,
            SerializedProperty bval, float minval, float maxval,
            string valFmt = "F2",
            bool warning = false
            )
        {
            EditorGUI.BeginChangeCheck();

            float sv, ev, cv;
            GetBezierValue(bval, out sv, out ev, out cv);

            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", sv, "/", ev, "]");

            bool wuse = use.boolValue;
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    if (string.IsNullOrEmpty(valueName) == false)
                        EditorGUILayout.LabelField(valueName);
                    EditorGUI.BeginDisabledGroup(!wuse);
                    BezierInspector(title, bval, minval, maxval, valFmt);
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    wuse = sw;
                },
                wuse,
                warning
                );
            use.boolValue = wuse;

            return EditorGUI.EndChangeCheck();
        }

        public static bool OneSliderInspector(
            string title,
            string name1, SerializedProperty property1, float min1, float max1
            )
        {
            EditorGUI.BeginChangeCheck();

            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", property1.floatValue, "]");
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUILayout.Slider(property1, min1, max1, name1);
                }
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool TwoSliderInspector(
            string title,
            string name1, SerializedProperty property1, float min1, float max1,
            string name2, SerializedProperty property2, float min2, float max2
            )
        {
            EditorGUI.BeginChangeCheck();

            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", property1.floatValue, "/", property2.floatValue, "]");
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUILayout.Slider(property1, min1, max1, name1);
                    EditorGUILayout.Slider(property2, min2, max2, name2);
                }
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool UseOneSliderInspector(
            string title, SerializedProperty use,
            string name1, SerializedProperty val1, float min1, float max1
            )
        {
            EditorGUI.BeginChangeCheck();

            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", val1.floatValue, "]");
            bool workuse = use.boolValue;
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!workuse);
                    EditorGUILayout.Slider(val1, min1, max1, name1);
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    workuse = sw;
                },
                workuse
                );
            use.boolValue = workuse;

            return EditorGUI.EndChangeCheck();
        }

        public static bool UseTwoSliderInspector(
            string title, SerializedProperty use,
            string name1, SerializedProperty val1, float min1, float max1,
            string name2, SerializedProperty val2, float min2, float max2
            )
        {
            EditorGUI.BeginChangeCheck();

            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", val1.floatValue, "/", val2.floatValue, "]");
            bool workuse = use.boolValue;
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!workuse);
                    EditorGUILayout.Slider(val1, min1, max1, name1);
                    EditorGUILayout.Slider(val2, min2, max2, name2);
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    workuse = sw;
                },
                workuse
                );
            use.boolValue = workuse;

            return EditorGUI.EndChangeCheck();
        }

        //===============================================================================
        public static bool AlgorithmInspector(SerializedProperty cparam, bool changed, System.Action convertAction)
        {
            EditorGUI.BeginChangeCheck();

            //TitleBar("Algorithm", changed);

            var algo = cparam.FindPropertyRelative("algorithm");
            var algoType = (ClothParams.Algorithm)algo.enumValueIndex;

            bool isPlaying = EditorApplication.isPlaying;

            const string title = "Algorithm";
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", algoType, "]");
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(isPlaying);
                    EditorGUILayout.PropertyField(algo, new GUIContent("Algorithm"));
                    EditorGUI.EndDisabledGroup();
                    switch (algoType)
                    {
                        case ClothParams.Algorithm.Algorithm_1:
                            EditorGUILayout.HelpBox("This algorithm is deprecated.\nPlease use the more stable algorithm 2.\nAlgorithm 1 will be removed in the future.", MessageType.Warning);
                            break;
                        case ClothParams.Algorithm.Algorithm_2:
                            EditorGUILayout.HelpBox("Algorithm 2 was introduced from v1.11.0.\nClampRotation / RestoreRotation / TriangleBend will be more stable.\nHowever, the parameters need to be readjusted.", MessageType.Info);
                            break;
                    }

                    // Convert
                    if (algoType == ClothParams.Algorithm.Algorithm_2)
                    {
                        EditorGUI.BeginDisabledGroup(isPlaying);
                        EditorGUILayout.Space();
                        using (var horizontalScope = new GUILayout.HorizontalScope())
                        {
                            EditorGUILayout.Space();
                            if (GUILayout.Button("Convert To Latest Algorithm Parameters", GUILayout.Width(300)))
                            {
                                if (EditorUtility.DisplayDialog("Parameter conversion", "Converts the parameters of an old algorithm to the latest algorithm.", "Ok", "Cancel"))
                                {
                                    //Debug.Log("OK!");
                                    convertAction?.Invoke();
                                }
                            }
                            EditorGUILayout.Space();
                        }
                        EditorGUILayout.Space();
                        EditorGUILayout.HelpBox("Parameters of old algorithms can be converted to the latest algorithms.\nNote, however, that the same parameter may work slightly differently depending on the algorithm.", MessageType.Info);
                        EditorGUILayout.Space();
                        EditorGUI.EndDisabledGroup();
                    }
                },
                warning: changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool WorldInfluenceInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var influenceTarget = cparam.FindPropertyRelative("influenceTarget");
            var moveInfluence = cparam.FindPropertyRelative("worldMoveInfluence");
            var rotInfluence = cparam.FindPropertyRelative("worldRotationInfluence");
            var maxSpeed = cparam.FindPropertyRelative("maxMoveSpeed");
            var maxRotationSpeed = cparam.FindPropertyRelative("maxRotationSpeed");

            var useTeleport = cparam.FindPropertyRelative("useResetTeleport");
            var teleportDistance = cparam.FindPropertyRelative("teleportDistance");
            var teleportRotation = cparam.FindPropertyRelative("teleportRotation");
            var teleportMode = cparam.FindPropertyRelative("teleportMode");

            var stabilizationTime = cparam.FindPropertyRelative("resetStabilizationTime");

            float ms, me, mc;
            GetBezierValue(moveInfluence, out ms, out me, out mc);
            float rs, re, rc;
            GetBezierValue(rotInfluence, out rs, out re, out rc);

            const string title = "World Influence";
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", ms, "/", me, "] [", rs, "/", re, "]");
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUILayout.PropertyField(influenceTarget);
                    EditorGUILayout.Slider(maxSpeed, 0.0f, 20.0f, "Max Move Speed");
                    EditorGUILayout.LabelField("Movement Influence");
                    BezierInspector("Move Influence", moveInfluence, 0.0f, 1.0f);

                    EditorGUILayout.Slider(maxRotationSpeed, 0.0f, 720.0f, "Max Rotation Speed");
                    EditorGUILayout.LabelField("Rotation Influence");
                    BezierInspector("Rotation Influence", rotInfluence, 0.0f, 1.0f);

                    useTeleport.boolValue = EditorGUILayout.Toggle("Reset After Teleport", useTeleport.boolValue);

                    EditorGUI.BeginDisabledGroup(!useTeleport.boolValue);
                    EditorGUILayout.PropertyField(teleportMode);
                    EditorGUILayout.Slider(teleportDistance, 0.0f, 1.0f, "Teleport Distance");
                    EditorGUILayout.Slider(teleportRotation, 0.0f, 180.0f, "Teleport Rotation");
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.Space();
                    //EditorGUILayout.LabelField("Stabilize After Reset");
                    //EditorGUILayout.Slider(stabilizationTime, 0.0f, 3.0f, "Stabilization Time");
                    EditorGUILayout.Slider(stabilizationTime, 0.0f, 1.0f, "Stabilization Time After Reset");
                },
                warning: changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool DistanceDisableInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var use = cparam.FindPropertyRelative("useDistanceDisable");
            var referenceObject = cparam.FindPropertyRelative("disableReferenceObject");
            var disableDisance = cparam.FindPropertyRelative("disableDistance");
            var fadeDistance = cparam.FindPropertyRelative("disableFadeDistance");

            const string title = "Distance Disable";
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title);
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!use.boolValue);

                    EditorGUILayout.HelpBox("If Reference Object is [None], the main camera is referred.", MessageType.None);

                    //EditorGUILayout.PropertyField(referenceObject);
                    referenceObject.objectReferenceValue = EditorGUILayout.ObjectField("Reference Object", referenceObject.objectReferenceValue, typeof(Transform), true);
                    EditorGUILayout.Slider(disableDisance, 0.0f, 100.0f, "Distance");
                    EditorGUILayout.Slider(fadeDistance, 0.0f, 10.0f, "Fade Distance");

                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    use.boolValue = sw;
                },
                use.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool ExternalForceInspector(SerializedProperty cparam)
        {
            EditorGUI.BeginChangeCheck();

            //var massInfluence = cparam.FindPropertyRelative("massInfluence");
            var depthInfluence = cparam.FindPropertyRelative("depthInfluence");
            var windInfluence = cparam.FindPropertyRelative("windInfluence");
            var windRandomScale = cparam.FindPropertyRelative("windRandomScale");
            var windSynchronization = cparam.FindPropertyRelative("windSynchronization");

            const string title = "External Force";
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title);
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    //EditorGUILayout.Slider(massInfluence, 0.0f, 1.0f, "Mass Influence");
                    //EditorGUILayout.LabelField("Wind");
                    EditorGUILayout.LabelField("Depth Influence");
                    BezierInspector("Depth Influence", depthInfluence, 0.0f, 1.0f, "F2");
                    EditorGUILayout.Space();
                    EditorGUILayout.Slider(windInfluence, 0.0f, 2.0f, "Wind Influence");
                    EditorGUILayout.Slider(windSynchronization, 0.0f, 1.0f, "Wind Synchronization");
                    EditorGUILayout.Slider(windRandomScale, 0.0f, 1.0f, "Wind Random Scale");
                }
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool RadiusInspector(SerializedProperty cparam)
        {
            return MinMaxCurveInspector("Radius", "Radius", cparam.FindPropertyRelative("radius"), 0.001f, 0.3f);
        }

        public static bool MassInspector(SerializedProperty cparam)
        {
            return MinMaxCurveInspector("Mass", "Mass", cparam.FindPropertyRelative("mass"), 1.0f, 20.0f);
        }

        public static bool GravityInspector(SerializedProperty cparam)
        {
            //return UseMinMaxCurveInspector("Gravity", cparam.FindPropertyRelative("useGravity"), "Gravity Acceleration", cparam.FindPropertyRelative("gravity"), -20.0f, 0.0f);

            EditorGUI.BeginChangeCheck();

            var useGravity = cparam.FindPropertyRelative("useGravity");
            var gravity = cparam.FindPropertyRelative("gravity");
            var gravityDirection = cparam.FindPropertyRelative("gravityDirection");
            //var useDirectional = cparam.FindPropertyRelative("useDirectionalDamping");
            //var refObject = cparam.FindPropertyRelative("directionalDampingObject");
            //var directionaDamping = cparam.FindPropertyRelative("directionalDamping");

            float sv, ev, cv;
            GetBezierValue(gravity, out sv, out ev, out cv);

            StaticStringBuilder.Clear();
            StaticStringBuilder.Append("Gravity", " [", sv, "/", ev, "]");

            bool wuse = useGravity.boolValue;
            Foldout("Gravity", StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!wuse);
                    EditorGUILayout.LabelField("Gravity Acceleration");
                    BezierInspector("Gravity", gravity, -10.0f, 0.0f, "F2");
                    EditorGUILayout.PropertyField(gravityDirection, true);

                    //useDirectional.boolValue = EditorGUILayout.Toggle("Directional Damping", useDirectional.boolValue);
                    //refObject.objectReferenceValue = EditorGUILayout.ObjectField("Reference Object", refObject.objectReferenceValue, typeof(Transform), true);
                    //EditorGUILayout.LabelField("Angular Damping");
                    //EditorGUILayout.HelpBox("The horizontal axis is the angle 0-90-180.", MessageType.None);
                    //BezierInspector("Angular Damping", directionaDamping, 0.0f, 1.0f, "F2");

                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    wuse = sw;
                },
                wuse
                );
            useGravity.boolValue = wuse;

            return EditorGUI.EndChangeCheck();
        }

        public static bool DragInspector(SerializedProperty cparam)
        {
            return UseMinMaxCurveInspector("Drag", cparam.FindPropertyRelative("useDrag"), "Drag", cparam.FindPropertyRelative("drag"), 0.0f, 0.3f);
        }

        public static bool MaxVelocityInspector(SerializedProperty cparam)
        {
            return UseMinMaxCurveInspector("Max Velocity", cparam.FindPropertyRelative("useMaxVelocity"), "Max Velocity", cparam.FindPropertyRelative("maxVelocity"), 0.01f, 10.0f);
        }

        public static bool TriangleBendInspector(SerializedProperty cparam, bool changed, ClothData clothData)
        {
            EditorGUI.BeginChangeCheck();

            var use = cparam.FindPropertyRelative("useTriangleBend");
            //var useIncludeFixed = cparam.FindPropertyRelative("useTriangleBendIncludeFixed");
            var useTiwstCorrection = cparam.FindPropertyRelative("useTwistCorrection");
            var tiwstPower = cparam.FindPropertyRelative("twistRecoveryPower");

            var algo = cparam.FindPropertyRelative("algorithm");
            var algoType = EditorApplication.isPlaying ? clothData.triangleBendAlgorithm : (ClothParams.Algorithm)algo.enumValueIndex;

            string powerStr = string.Empty;
            switch (algoType)
            {
                case ClothParams.Algorithm.Algorithm_1:
                    powerStr = "triangleBend";
                    break;
                case ClothParams.Algorithm.Algorithm_2:
                    powerStr = "triangleBend2";
                    break;
            }
            var power = cparam.FindPropertyRelative(powerStr);

            const string title = "Triangle Bend";
            StaticStringBuilder.Clear();
            float sv = 0, ev = 0, cv = 0;
            GetBezierValue(power, out sv, out ev, out cv);
            StaticStringBuilder.Append("Triangle Bend", " [", sv, "/", ev, "]");
            StaticStringBuilder.Append(" (", algoType, ")");

            bool isPlaying = EditorApplication.isPlaying;

            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!use.boolValue);
                    //useIncludeFixed.boolValue = EditorGUILayout.Toggle("Include Fixed", useIncludeFixed.boolValue);
                    EditorGUILayout.LabelField("Bend Power");
                    BezierInspector("Triangle Bend", power, 0.0f, 1.0f);
                    EditorGUI.EndDisabledGroup();

                    // Twist
                    if (algoType == ClothParams.Algorithm.Algorithm_2)
                    {
                        EditorGUI.BeginDisabledGroup(isPlaying);
                        useTiwstCorrection.boolValue = EditorGUILayout.Toggle("Twist Correction (Experimental)", useTiwstCorrection.boolValue);
                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.Slider(tiwstPower, 0.0f, 1.0f, "Twist Recovery Power");
                    }
                },
                (sw) =>
                {
                    use.boolValue = sw;
                },
                use.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool DirectionMoveLimitInspector(SerializedProperty cparam)
        {
            return UseMinMaxCurveInspector("Limit Move To Hits", cparam.FindPropertyRelative("useDirectionMoveLimit"), "Move Limit", cparam.FindPropertyRelative("directionMoveLimit"), -0.2f, 0.2f);
        }

        public static bool RestoreRotationInspector(SerializedProperty cparam, bool changed, ClothData clothData)
        {
            EditorGUI.BeginChangeCheck();

            var use = cparam.FindPropertyRelative("useRestoreRotation");
            var algo = cparam.FindPropertyRelative("algorithm");
            var algoType = EditorApplication.isPlaying ? clothData.restoreRotationAlgorithm : (ClothParams.Algorithm)algo.enumValueIndex;

            string powerStr = string.Empty;
            string influenceStr = string.Empty;
            switch (algoType)
            {
                case ClothParams.Algorithm.Algorithm_1:
                    powerStr = "restoreRotation";
                    influenceStr = "restoreRotationVelocityInfluence";
                    break;
                case ClothParams.Algorithm.Algorithm_2:
                    powerStr = "restoreRotation2";
                    influenceStr = "restoreRotationVelocityInfluence2";
                    break;
            }
            var power = cparam.FindPropertyRelative(powerStr);
            var influence = cparam.FindPropertyRelative(influenceStr);

            const string title = "Restore Rotation";
            float sv, ev, cv;
            GetBezierValue(power, out sv, out ev, out cv);
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append("Restore Rotation", " [", sv, "/", ev, "] (", algoType, ")");

            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!use.boolValue);

                    EditorGUILayout.LabelField("Restore Power");
                    BezierInspector("Restore Power", power, 0.0f, 1.0f);
                    EditorGUILayout.Slider(influence, 0.0f, 1.0f, "Velocity Influence");

                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    use.boolValue = sw;
                },
                use.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool ClampRotationInspector(SerializedProperty cparam, bool changed, ClothData clothData)
        {
            EditorGUI.BeginChangeCheck();

            var use = cparam.FindPropertyRelative("useClampRotation");
            var algo = cparam.FindPropertyRelative("algorithm");
            var algoType = EditorApplication.isPlaying ? clothData.clampRotationAlgorithm : (ClothParams.Algorithm)algo.enumValueIndex;

            string angleStr = string.Empty;
            switch (algoType)
            {
                case ClothParams.Algorithm.Algorithm_1:
                    angleStr = "clampRotationAngle";
                    break;
                case ClothParams.Algorithm.Algorithm_2:
                    angleStr = "clampRotationAngle2";
                    break;
            }
            var angle = cparam.FindPropertyRelative(angleStr);
            var influence = cparam.FindPropertyRelative("clampRotationVelocityInfluence");
            var limit = cparam.FindPropertyRelative("clampRotationVelocityLimit");

            const string title = "Clamp Rotation";
            float sv, ev, cv;
            GetBezierValue(angle, out sv, out ev, out cv);
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append("Clamp Rotation", " [", sv, "/", ev, "] (", algoType, ")");

            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!use.boolValue);
                    EditorGUILayout.LabelField("Clamp Angle");
                    BezierInspector("Angle", angle, 0.0f, 180.0f);
                    if (algoType == ClothParams.Algorithm.Algorithm_1)
                    {
                        EditorGUILayout.Slider(limit, 0.0f, 2.0f, "Velocity Limit");
                        EditorGUILayout.Slider(influence, 0.0f, 1.0f, "Velocity Influence");
                    }

                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    use.boolValue = sw;
                },
                use.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool CollisionInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var use = cparam.FindPropertyRelative("useCollision");
            var dynamicFriction = cparam.FindPropertyRelative("friction");
            var staticFriction = cparam.FindPropertyRelative("staticFriction");

            const string title = "Collider Collision";
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", dynamicFriction.floatValue, ", ", staticFriction.floatValue, "]");
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!use.boolValue);
                    EditorGUILayout.Slider(dynamicFriction, 0.0f, 0.5f, "Dynamic Friction");
                    EditorGUILayout.Slider(staticFriction, 0.0f, 0.5f, "Static Friction");
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    use.boolValue = sw;
                },
                use.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool PenetrationInspector(SerializedObject team, SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var use = cparam.FindPropertyRelative("usePenetration");
            var mode = cparam.FindPropertyRelative("penetrationMode");
            var axis = cparam.FindPropertyRelative("penetrationAxis");
            var maxDepth = cparam.FindPropertyRelative("penetrationMaxDepth");
            var connectDistance = cparam.FindPropertyRelative("penetrationConnectDistance");
            //var stiffness = cparam.FindPropertyRelative("penetrationStiffness");
            var radius = cparam.FindPropertyRelative("penetrationRadius");
            var ignoreCollider = team.FindProperty("teamData.penetrationIgnoreColliderList");
            var distance = cparam.FindPropertyRelative("penetrationDistance");

            bool isPlaying = EditorApplication.isPlaying;

            const string title = "Penetration";
            Foldout(title, title,
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!use.boolValue);

                    EditorGUI.BeginDisabledGroup(isPlaying);

                    EditorGUILayout.PropertyField(mode);

                    if (mode.enumValueIndex == (int)ClothParams.PenetrationMode.SurfacePenetration)
                    {
                        EditorGUILayout.Slider(maxDepth, 0.0f, 1.0f, "Max Connection Depth");
                        EditorGUILayout.PropertyField(axis);
                        EditorGUI.EndDisabledGroup();

                        EditorGUILayout.LabelField("Penetration Distance");
                        BezierInspector("Penetration Distance", distance, 0.0f, 1.0f);
                        EditorGUILayout.LabelField("Moving Radius");
                        BezierInspector("Moving Radius", radius, 0.0f, 5.0f);
                    }
                    else if (mode.enumValueIndex == (int)ClothParams.PenetrationMode.ColliderPenetration)
                    {
                        EditorGUILayout.Slider(maxDepth, 0.0f, 1.0f, "Max Connection Depth");
                        EditorGUILayout.LabelField("Connection Distance");
                        BezierInspector("Connection Distance", connectDistance, 0.0f, 1.0f);
                        EditorGUI.EndDisabledGroup();

                        //EditorGUILayout.LabelField("Stiffness");
                        //BezierInspector("Connection Stiffness", stiffness, 0.0f, 1.0f);
                        EditorGUILayout.LabelField("Penetration Distance");
                        BezierInspector("Penetration Distance", distance, 0.0f, 1.0f);
                        EditorGUILayout.LabelField("Moving Radius");
                        BezierInspector("Moving Radius", radius, 0.0f, 5.0f);

                        EditorGUI.BeginDisabledGroup(isPlaying);
                        //EditorGUILayout.LabelField("Ignore Collider List");
                        EditorGUILayout.PropertyField(ignoreCollider, true);
                        EditorGUI.EndDisabledGroup();
                    }
#if false // 一旦休眠
                    else if (mode.enumValueIndex == (int)ClothParams.PenetrationMode.BonePenetration)
                    {
                        // help
                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.HelpBox("Bone Penetration controls the penetration in the direction of the bones registered in the [Skinning Bone List].\nThe particles must be skinning by[User Animation] or[Generate From Bones].", MessageType.Info);

                        //EditorGUI.BeginDisabledGroup(isPlaying);
                        EditorGUILayout.Slider(maxDepth, 0.0f, 1.0f, "Max Connection Depth");
                        //EditorGUI.EndDisabledGroup();

                        EditorGUILayout.LabelField("Penetration Distance");
                        BezierInspector("Penetration Distance", distance, 0.0f, 1.0f);
                        EditorGUILayout.LabelField("Moving Radius");
                        BezierInspector("Moving Radius", radius, 0.0f, 5.0f);
                    }
#endif

                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    use.boolValue = sw;
                },
                use.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool BaseSkinningInspector(SerializedObject team, SerializedProperty cparam)
        {
            EditorGUI.BeginChangeCheck();

            var use = cparam.FindPropertyRelative("useBaseSkinning");
            //var boneList = team.FindProperty("teamData.skinningBoneList");
            var ignoreCollider = team.FindProperty("teamData.skinningIgnoreColliderList");

            const string title = "Skinning";
            Foldout(title, title,
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!use.boolValue);

                    //EditorGUILayout.PropertyField(boneList, true);
                    EditorGUILayout.PropertyField(ignoreCollider, true);

                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    use.boolValue = sw;
                },
                use.boolValue
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool ClampDistanceInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var use = cparam.FindPropertyRelative("useClampDistanceRatio");
            var minRatio = cparam.FindPropertyRelative("clampDistanceMinRatio");
            var maxRatio = cparam.FindPropertyRelative("clampDistanceMaxRatio");
            var influence = cparam.FindPropertyRelative("clampDistanceVelocityInfluence");

            const string title = "Clamp Distance";
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append("Clamp Distance", " [", minRatio.floatValue, "/", maxRatio.floatValue, "]");

            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!use.boolValue);

                    EditorGUILayout.Slider(minRatio, 0.0f, 1.0f, "Min Distance Ratio");
                    EditorGUILayout.Slider(maxRatio, 1.0f, 2.0f, "Max Distance Ratio");
                    EditorGUILayout.Slider(influence, 0.0f, 1.0f, "Velocity Influence");

                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    use.boolValue = sw;
                },
                use.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool RestoreDistanceInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var influence = cparam.FindPropertyRelative("restoreDistanceVelocityInfluence");
            var structStiffness = cparam.FindPropertyRelative("structDistanceStiffness");
            var useBend = cparam.FindPropertyRelative("useBendDistance");
            var bendMaxCount = cparam.FindPropertyRelative("bendDistanceMaxCount");
            var bendStiffness = cparam.FindPropertyRelative("bendDistanceStiffness");
            var useNear = cparam.FindPropertyRelative("useNearDistance");
            var nearLength = cparam.FindPropertyRelative("nearDistanceLength");
            var nearStiffness = cparam.FindPropertyRelative("nearDistanceStiffness");
            var nearMaxCount = cparam.FindPropertyRelative("nearDistanceMaxCount");
            var nearMaxDepth = cparam.FindPropertyRelative("nearDistanceMaxDepth");

            const string title = "Restore Distance";
            Foldout(title, title,
                () =>
                {
                    EditorGUILayout.LabelField("Struct Point [Always ON]");

                    EditorGUILayout.LabelField("Struct Stiffness");
                    BezierInspector("Struct Stiffness", structStiffness, 0.0f, 1.0f);

                    useBend.boolValue = EditorGUILayout.Toggle("Bend Point", useBend.boolValue);
                    EditorGUILayout.IntSlider(bendMaxCount, 1, 6, "Bend Max Connection");
                    EditorGUILayout.LabelField("Bend Stiffness");
                    BezierInspector("Bend Stiffness", bendStiffness, 0.0f, 1.0f);

                    useNear.boolValue = EditorGUILayout.Toggle("Near Point", useNear.boolValue);
                    EditorGUILayout.IntSlider(nearMaxCount, 1, 6, "Near Max Connection");
                    EditorGUILayout.Slider(nearMaxDepth, 0.0f, 1.0f, "Near Max Depth");
                    EditorGUILayout.LabelField("Near Point Length");
                    BezierInspector("Near Point Length", nearLength, 0.0f, 0.5f);
                    EditorGUILayout.LabelField("Near Stiffness");
                    BezierInspector("Near Stiffness", nearStiffness, 0.0f, 1.0f);

                    EditorGUILayout.Slider(influence, 0.0f, 1.0f, "Velocity Influence");
                },
                warning: changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool FullSpringInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var springPower = cparam.FindPropertyRelative("springPower");
            var useSpring = cparam.FindPropertyRelative("useSpring");
            var springRadius = cparam.FindPropertyRelative("springRadius");
            var springScaleX = cparam.FindPropertyRelative("springScaleX");
            var springScaleY = cparam.FindPropertyRelative("springScaleY");
            var springScaleZ = cparam.FindPropertyRelative("springScaleZ");
            var springDirectionAtten = cparam.FindPropertyRelative("springDirectionAtten");
            var springDistanceAtten = cparam.FindPropertyRelative("springDistanceAtten");
            var springIntensity = cparam.FindPropertyRelative("springIntensity");

            bool isPlaying = EditorApplication.isPlaying;

            const string title = "Spring";
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", springPower.floatValue, "]");
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!useSpring.boolValue);
                    {
                        EditorGUI.BeginDisabledGroup(isPlaying);
                        EditorGUILayout.Slider(springRadius, 0.01f, 0.5f, "Spring Radius");
                        EditorGUILayout.Slider(springScaleX, 0.01f, 1.0f, "Spring Radius Scale X");
                        EditorGUILayout.Slider(springScaleY, 0.01f, 1.0f, "Spring Radius Scale Y");
                        EditorGUILayout.Slider(springScaleZ, 0.01f, 1.0f, "Spring Radius Scale Z");
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.Slider(springPower, 0.0f, 0.1f, "Spring Power");
                    {
                        EditorGUI.BeginDisabledGroup(isPlaying);
                        EditorGUILayout.LabelField("Spring Direction Atten");
                        BezierInspector("Spring Direction Atten", springDirectionAtten, 0.0f, 1.0f);
                        EditorGUILayout.LabelField("Spring Distance Atten");
                        BezierInspector("Spring Distance Atten", springDistanceAtten, 0.0f, 1.0f);
                        EditorGUILayout.Slider(springIntensity, 0.1f, 3.0f, "Spring Atten Intensity");
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    useSpring.boolValue = sw;
                },
                useSpring.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool SimpleSpringInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var springPower = cparam.FindPropertyRelative("springPower");
            var useSpring = cparam.FindPropertyRelative("useSpring");

            const string title = "Spring";
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", springPower.floatValue, "]");
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!useSpring.boolValue);
                    EditorGUILayout.Slider(springPower, 0.0f, 0.1f, "Spring Power");
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    useSpring.boolValue = sw;
                },
                useSpring.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool AdjustRotationInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            //var useAdjustRotation = cparam.FindPropertyRelative("useAdjustRotation");
            var adjustMode = cparam.FindPropertyRelative("adjustMode");
            var adjustRotationPower = cparam.FindPropertyRelative("adjustRotationPower");
            var enumName = adjustMode.enumDisplayNames[adjustMode.enumValueIndex];

            const string title = "Adjust Rotation";
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", enumName, "]");
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUILayout.PropertyField(adjustMode);
                    EditorGUI.BeginDisabledGroup(adjustMode.enumValueIndex == 0);
                    EditorGUILayout.Slider(adjustRotationPower, -20.0f, 20.0f, "Adjust Rotation Power");
                    EditorGUI.EndDisabledGroup();
                },
                //(sw) =>
                //{
                //    useAdjustRotation.boolValue = sw;
                //},
                //useAdjustRotation.boolValue,
                warning: changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool ClampPositionInspector(SerializedProperty cparam, bool full, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var clampPositionLength = cparam.FindPropertyRelative("clampPositionLength");
            var useClampPositionLength = cparam.FindPropertyRelative("useClampPositionLength");
            var clampPositionRatioX = cparam.FindPropertyRelative("clampPositionRatioX");
            var clampPositionRatioY = cparam.FindPropertyRelative("clampPositionRatioY");
            var clampPositionRatioZ = cparam.FindPropertyRelative("clampPositionRatioZ");
            var influence = cparam.FindPropertyRelative("clampPositionVelocityInfluence");

            float sv, ev, cv;
            GetBezierValue(clampPositionLength, out sv, out ev, out cv);

            const string title = "Clamp Position";
            StaticStringBuilder.Clear();
            StaticStringBuilder.Append(title, " [", sv, "/", ev, "]");
            Foldout(title, StaticStringBuilder.ToString(),
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!useClampPositionLength.boolValue);
                    EditorGUILayout.LabelField("Clamp Position Length");
                    BezierInspector("Clamp Position Length", clampPositionLength, 0.0f, 1.0f);
                    if (full)
                    {
                        EditorGUILayout.Slider(clampPositionRatioX, 0.0f, 1.0f, "Clamp Position Ratio X");
                        EditorGUILayout.Slider(clampPositionRatioY, 0.0f, 1.0f, "Clamp Position Ratio Y");
                        EditorGUILayout.Slider(clampPositionRatioZ, 0.0f, 1.0f, "Clamp Position Ratio Z");
                    }
                    EditorGUILayout.Slider(influence, 0.0f, 1.0f, "Velocity Influence");
                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    useClampPositionLength.boolValue = sw;
                },
                useClampPositionLength.boolValue,
                changed
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool VolumeInspector(SerializedProperty cparam)
        {
            EditorGUI.BeginChangeCheck();

            var useVolume = cparam.FindPropertyRelative("useVolume");
            var maxVolumeLength = cparam.FindPropertyRelative("maxVolumeLength");
            var volumeStretchStiffness = cparam.FindPropertyRelative("volumeStretchStiffness");
            var volumeShearStiffness = cparam.FindPropertyRelative("volumeShearStiffness");

            const string title = "Volume";
            Foldout(title, title,
                () =>
                {
                    EditorGUI.BeginDisabledGroup(!useVolume.boolValue);

                    EditorGUILayout.Slider(maxVolumeLength, 0.0f, 0.5f, "Max Volume Length");
                    EditorGUILayout.LabelField("Stretch Stiffness");
                    BezierInspector("Stretch Stiffness", volumeStretchStiffness, 0.0f, 1.0f);
                    EditorGUILayout.LabelField("Shear Stiffness");
                    BezierInspector("Shear Stiffness", volumeShearStiffness, 0.0f, 1.0f);

                    EditorGUI.EndDisabledGroup();
                },
                (sw) =>
                {
                    useVolume.boolValue = sw;
                },
                useVolume.boolValue
                );

            return EditorGUI.EndChangeCheck();
        }

        public static bool RotationInterpolationInspector(SerializedProperty cparam, bool changed)
        {
            EditorGUI.BeginChangeCheck();

            var avarage = cparam.FindPropertyRelative("useLineAvarageRotation");
            var fixnonrot = cparam.FindPropertyRelative("useFixedNonRotation");

            const string title = "Rotation Interpolation";
            Foldout(title, title,
                () =>
                {
                    fixnonrot.boolValue = EditorGUILayout.Toggle("Fixed Non-Rotation", fixnonrot.boolValue);
                    avarage.boolValue = EditorGUILayout.Toggle("Line Avarage Rotation", avarage.boolValue);
                },
                warning: changed
                );

            return EditorGUI.EndChangeCheck();
        }

        //===============================================================================
        public static void WindComponentInspector(WindComponent wind, SerializedObject so)
        {
            var areaWind = wind as MagicaAreaWind;
            var windType = wind.GetWindType();
            var shapeType = wind.GetShapeType();
            var dirType = wind.GetDirectionType();

            EditorGUILayout.PropertyField(so.FindProperty("main"));
            EditorGUILayout.PropertyField(so.FindProperty("turbulence"));
            EditorGUILayout.PropertyField(so.FindProperty("frequency"));
            if (windType == PhysicsManagerWindData.WindType.Area)
            {
                if (areaWind)
                {
                    EditorGUILayout.PropertyField(so.FindProperty("shapeType"));
                }

                switch (shapeType)
                {
                    case PhysicsManagerWindData.ShapeType.Box:
                        EditorGUILayout.PropertyField(so.FindProperty("areaSize"));
                        break;
                    case PhysicsManagerWindData.ShapeType.Sphere:
                        EditorGUILayout.PropertyField(so.FindProperty("areaRadius"));
                        break;
                }
                //EditorGUILayout.PropertyField(so.FindProperty("anchor"));
            }

            if (windType == PhysicsManagerWindData.WindType.Area && shapeType == PhysicsManagerWindData.ShapeType.Sphere)
            {
                EditorGUILayout.PropertyField(so.FindProperty("directionType"));
            }

            if (windType == PhysicsManagerWindData.WindType.Direction || dirType == PhysicsManagerWindData.DirectionType.OneDirection)
            {
                EditorGUILayout.PropertyField(so.FindProperty("directionAngleX"));
                EditorGUILayout.PropertyField(so.FindProperty("directionAngleY"));
            }

            if (windType == PhysicsManagerWindData.WindType.Area && shapeType == PhysicsManagerWindData.ShapeType.Sphere && dirType == PhysicsManagerWindData.DirectionType.Radial)
            {
                EditorGUILayout.LabelField("Attenuation");
                BezierInspector("Attenuation", so.FindProperty("attenuation"), 0.0f, 1.0f);
            }

            if (areaWind)
            {
                EditorGUILayout.PropertyField(so.FindProperty("isAddition"));
            }
        }

        //===============================================================================
        /// <summary>
        /// 水平線を引く
        /// </summary>
        public static void DrawHorizoneLine()
        {
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
        }

        /// <summary>
        /// インスペクタにオブジェクトリストと全選択ボタンを表示する
        /// </summary>
        /// <param name="dlist"></param>
        /// <param name="obj"></param>
        public static void DrawObjectList<T>(
            SerializedProperty dlist, GameObject obj, bool allselect, bool allclear, System.Func<T[]> func,
            string allSelectName = null, string allClearName = null
            )
            where T : UnityEngine.Object
        {
            // リスト表示
            EditorGUILayout.PropertyField(dlist, true);

            // 全選択/削除ボタン
            if (allselect || allclear)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (allselect)
                {
                    if (GUILayout.Button(allSelectName ?? "All Select", GUILayout.Width(80), GUILayout.Height(16)))
                    {
                        //var newlist = obj.transform.root.GetComponentsInChildren<T>();
                        var newlist = func();
                        int cnt = newlist == null ? 0 : newlist.Length;
                        dlist.arraySize = cnt;
                        for (int i = 0; i < cnt; i++)
                        {
                            dlist.GetArrayElementAtIndex(i).objectReferenceValue = newlist[i];
                        }
                    }
                }

                if (allclear)
                {
                    if (GUILayout.Button(allClearName ?? "Clear", GUILayout.Width(70), GUILayout.Height(16)))
                    {
                        dlist.arraySize = 0;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        //===============================================================================
        /// <summary>
        /// クロスモニターを開くボタン
        /// </summary>
        public static void MonitorButtonInspector()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Open Cloth Monitor", GUILayout.Width(150)))
            {
                ClothMonitorMenu.InitWindow();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

    }
}
