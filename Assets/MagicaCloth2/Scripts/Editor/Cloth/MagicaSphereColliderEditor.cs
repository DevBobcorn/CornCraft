// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;

namespace MagicaCloth2
{
    /// <summary>
    /// SphereColliderのインスペクター拡張
    /// </summary>
    [CustomEditor(typeof(MagicaSphereCollider))]
    public class MagicaSphereColliderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var scr = target as MagicaSphereCollider;

            serializedObject.Update();
            Undo.RecordObject(scr, "SphereCollider");

            // radius
            var sizeValue = serializedObject.FindProperty("size");
            var size = sizeValue.vector3Value;
            size.x = EditorGUILayout.Slider("Radius", size.x, 0.001f, 0.5f);
            sizeValue.vector3Value = size;

            // center
            var centerValue = serializedObject.FindProperty("center");
            centerValue.vector3Value = EditorGUILayout.Vector3Field("Center", centerValue.vector3Value);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
