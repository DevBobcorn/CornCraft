// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    [CustomPropertyDrawer(typeof(SharePreBuildData))]
    public class SharePreBuildDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // サイズ
            float lineHight = EditorGUIUtility.singleLineHeight;

            // 子のフィールドをインデントしない 
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var buildIdProperty = property.FindPropertyRelative("buildId");
            var versionProperty = property.FindPropertyRelative("version");
            var resultProperty = property.FindPropertyRelative("buildResult.result");

            // テキスト幅調整
            EditorGUIUtility.labelWidth = position.width;

            // build Id
            string buildId = string.IsNullOrEmpty(buildIdProperty.stringValue) ? "(Empty)" : buildIdProperty.stringValue;
            EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(buildId));
            position.y += lineHight;

            // インデント＋１
            EditorGUI.indentLevel = indent + 1;

            // result
            Define.Result ret = (Define.Result)resultProperty.enumValueIndex;
            var result = new ResultCode(ret);
            if (result.IsFaild() == false)
            {
                // バージョン確認
                if (versionProperty.intValue != Define.System.LatestPreBuildVersion)
                    result.SetError(Define.Result.PreBuildData_VersionMismatch);
            }

            // result text
            var backColor = GUI.color;
            if (result.IsSuccess())
            {
                GUI.color = Color.green;
                EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent($"{result.Result}"));
            }
            else
            {
                GUI.color = Color.red;
                EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent($"Error: {result.Result}"));
            }
            GUI.color = backColor;

            // インデントを元通りに戻します
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = EditorGUIUtility.singleLineHeight;
            h *= 2;

            return h;
        }
    }
}
