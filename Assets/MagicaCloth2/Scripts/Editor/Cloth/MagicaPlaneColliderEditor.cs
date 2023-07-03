// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;

namespace MagicaCloth2
{
    /// <summary>
    /// PlaneColliderのインスペクター拡張
    /// </summary>
    [CustomEditor(typeof(MagicaPlaneCollider))]
    public class MagicaPlaneColliderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var scr = target as MagicaPlaneCollider;

            serializedObject.Update();
            Undo.RecordObject(scr, "PlaneCollider");

            // center
            var centerValue = serializedObject.FindProperty("center");
            centerValue.vector3Value = EditorGUILayout.Vector3Field("Center", centerValue.vector3Value);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
