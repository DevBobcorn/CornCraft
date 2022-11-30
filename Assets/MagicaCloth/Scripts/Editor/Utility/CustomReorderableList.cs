// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEditor;
using UnityEditorInternal;

namespace MagicaCloth
{
    /// <summary>
    /// オブジェクト参照リストインスペクタ表示クラス
    /// </summary>
    public class CustomReorderableList : ReorderableList
    {
        public CustomReorderableList(SerializedObject serializedObject, SerializedProperty elements, string title)
            : base(serializedObject, elements, true, false, true, true)
        {
            // 要素の描画コールバック登録
            drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = elements.GetArrayElementAtIndex(index);
                rect.height -= 4;
                rect.y += 2;
                EditorGUI.PropertyField(rect, element);
            };

            // ヘッダ名表示
            drawHeaderCallback = (rect) =>
                     EditorGUI.LabelField(rect, title);
            //EditorGUI.LabelField(rect, elements.displayName);

            // ＋ボタンの追加方法設定
            onAddCallback += (list) =>
            {
                //要素を追加
                elements.arraySize++;

                //最後の要素を選択状態にする
                list.index = elements.arraySize - 1;

                //追加した要素にnullを追加する
                var element = elements.GetArrayElementAtIndex(list.index);
                element.objectReferenceValue = null;
            };
        }
    }
}
