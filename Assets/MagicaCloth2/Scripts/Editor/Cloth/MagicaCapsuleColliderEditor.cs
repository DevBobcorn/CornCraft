// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// CapsuleColliderのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(MagicaCapsuleCollider))]
    public class MagicaCapsuleColliderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var scr = target as MagicaCapsuleCollider;

            serializedObject.Update();
            Undo.RecordObject(scr, "CapsuleCollider");

            // separation
            var separationValue = serializedObject.FindProperty("radiusSeparation");

            // direction
            var directionValue = serializedObject.FindProperty("direction");
            EditorGUILayout.PropertyField(directionValue);

            // aligned on center
            var alignedOnCenterValue = serializedObject.FindProperty("alignedOnCenter");
            EditorGUILayout.PropertyField(alignedOnCenterValue);

            var sizeValue = serializedObject.FindProperty("size");
            var size = sizeValue.vector3Value;

            // length
            size.z = EditorGUILayout.Slider("Length", size.z, 0.0f, 2.0f);

            // radius
            float lineHight = EditorGUIUtility.singleLineHeight;

            // start
            {
                Rect r = EditorGUILayout.GetControlRect();

                // ラベルを描画
                var positionA = EditorGUI.PrefixLabel(r, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(scr.radiusSeparation ? "Start Radius" : "Radius"));

                // 矩形を計算
                float w = positionA.width;
                var buttonRect = new Rect(positionA.x + w - 30, r.y, 30, lineHight);
                var sliderRect = new Rect(positionA.x, r.y, Mathf.Max(w - 35, 0), lineHight);

                // Slider
                size.x = EditorGUI.Slider(sliderRect, size.x, 0.001f, 0.5f);

                // 分割ボタン
                if (GUI.Button(buttonRect, scr.radiusSeparation ? "X" : "S"))
                {
                    // 切り替え
                    separationValue.boolValue = !separationValue.boolValue;
                }
            }

            // end
            if (separationValue.boolValue)
            {
                Rect r = EditorGUILayout.GetControlRect();

                // ラベルを描画
                var positionA = EditorGUI.PrefixLabel(r, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("End Radius"));

                // 矩形を計算
                float w = positionA.width;
                var sliderRect = new Rect(positionA.x, r.y, Mathf.Max(w - 35, 0), lineHight);

                // Slider
                size.y = EditorGUI.Slider(sliderRect, size.y, 0.001f, 0.5f);
            }

            //size.x = EditorGUILayout.Slider("Start Radius", size.x, 0.001f, 0.5f);
            //size.y = EditorGUILayout.Slider("End Radius", size.y, 0.001f, 0.5f);

            // サイズ格納
            sizeValue.vector3Value = size;

            // center
            var centerValue = serializedObject.FindProperty("center");
            centerValue.vector3Value = EditorGUILayout.Vector3Field("Center", centerValue.vector3Value);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
