// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEditor;

namespace MagicaCloth
{
    /// <summary>
    /// グローバル方向風
    /// </summary>
    [CustomEditor(typeof(MagicaDirectionalWind))]
    public class MagicaDirectionalWindInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            //DrawDefaultInspector();

            WindComponent scr = target as WindComponent;
            serializedObject.Update();
            Undo.RecordObject(scr, "WindComponent");

            EditorInspectorUtility.WindComponentInspector(scr, serializedObject);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
