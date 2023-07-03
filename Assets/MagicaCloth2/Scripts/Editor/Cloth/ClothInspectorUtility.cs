// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    public static class ClothInspectorUtility
    {
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
    }
}
