// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// CurveSerializeDataプロパティのカスタムGUI描画
    /// </summary>
    [CustomPropertyDrawer(typeof(CurveSerializeData))]
    public class CurveSerializeDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // サイズ
            float lineHight = EditorGUIUtility.singleLineHeight;
            float y = position.y;

            EditorGUI.BeginProperty(position, label, property);

            // ラベルを描画
            var positionA = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // 子のフィールドをインデントしない 
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // プロパティ
            var useProperty = property.FindPropertyRelative("useCurve");
            var valueProperty = property.FindPropertyRelative("value");
            bool useCurve = useProperty.boolValue;

            // 設定値の範囲。プロパティ名から判定する
            //var minmax = ClothSerializeData.GetMinMax(property.name);
            var minmax = MagicaClothEditor.GetPropertyMinMax(property.name);

            // 矩形を計算
            float w = positionA.width;
            var buttonRect = new Rect(positionA.x + w - 30, y, 30, lineHight);
            var sliderRect = new Rect(positionA.x, y, Mathf.Max(w - 35, 0), lineHight);

            // GUIContent.none をそれぞれに渡すと、ラベルなしに描画されます
            EditorGUI.Slider(sliderRect, valueProperty, minmax.x, minmax.y, GUIContent.none);
            if (GUI.Button(buttonRect, useProperty.boolValue ? "X" : "C"))
            {
                // カーブ切り替え
                useProperty.boolValue = !useProperty.boolValue;
            }

            // カーブ
            if (useCurve)
            {
                // カーブプロパティ
                var curveProperty = property.FindPropertyRelative("curve");
                y += lineHight + 3;
                //var curveRect = new Rect(positionA.x - 15, y, w + 15, lineHight);
                var curveRect = new Rect(positionA.x, y, w, lineHight);
                EditorGUI.CurveField(curveRect, curveProperty, Color.green, new Rect(0, 0, 1, 1), GUIContent.none);
            }

            // インデントを元通りに戻します
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = EditorGUIUtility.singleLineHeight;

            var useProperty = property.FindPropertyRelative("useCurve");
            if (useProperty.boolValue)
                h += (h + 3 + 2);

            return h;
        }
    }
}
