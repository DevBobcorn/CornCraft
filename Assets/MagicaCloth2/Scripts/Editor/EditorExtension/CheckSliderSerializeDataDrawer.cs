// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// CheckSliderSerializeDataプロパティのカスタムGUI描画
    /// </summary>
    [CustomPropertyDrawer(typeof(CheckSliderSerializeData))]
    public class CheckSliderSerializeDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // サイズ
            float lineHight = EditorGUIUtility.singleLineHeight;
            float y = position.y;

            EditorGUI.BeginProperty(position, label, property);

            // プロパティ
            var useProperty = property.FindPropertyRelative("use");
            var valueProperty = property.FindPropertyRelative("value");

            // ラベルを描画
            Rect positionA;
            using (new EditorGUI.DisabledScope(!useProperty.boolValue))
            {
                positionA = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            }

            // 子のフィールドをインデントしない 
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // 設定値の範囲。プロパティ名から判定する
            var minmax = MagicaClothEditor.GetPropertyMinMax(property.name);

            // 矩形を計算
            float w = positionA.width;
            const float toggleSize = 20;
            var buttonRect = new Rect(positionA.x, y, toggleSize, lineHight);
            var sliderRect = new Rect(positionA.x + toggleSize, y, Mathf.Max(w - toggleSize, 0), lineHight);

            // GUIContent.none をそれぞれに渡すと、ラベルなしに描画されます
            EditorGUI.PropertyField(buttonRect, useProperty, GUIContent.none);
            using (new EditorGUI.DisabledScope(!useProperty.boolValue))
            {
                EditorGUI.Slider(sliderRect, valueProperty, minmax.x, minmax.y, GUIContent.none);
            }

            // インデントを元通りに戻します
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
}
