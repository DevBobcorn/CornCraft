// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    [CustomEditor(typeof(MagicaWindZone))]
    [CanEditMultipleObjects]
    public class MagicaWindZoneEditor : Editor
    {
        //=========================================================================================
        /// <summary>
        /// インスペクターGUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            var wind = target as MagicaWindZone;

            serializedObject.Update();
            Undo.RecordObject(wind, "MagicaWindZone");

            WindInspector();

            serializedObject.ApplyModifiedProperties();
        }

        void WindInspector()
        {
            var wind = target as MagicaWindZone;

            var modeProperty = serializedObject.FindProperty("mode");
            EditorGUILayout.PropertyField(modeProperty);

            //var mode = (MagicaWindZone.Mode)modeProperty.enumValueIndex;
            if (wind.mode == MagicaWindZone.Mode.SphereDirection || wind.mode == MagicaWindZone.Mode.SphereRadial)
            {
                var radiusProperty = serializedObject.FindProperty("radius");
                var radius = radiusProperty.floatValue;
                float nr = EditorGUILayout.FloatField(new GUIContent("Radius"), radius);
                nr = Mathf.Max(nr, 0.0f);
                if (radius != nr)
                {
                    radiusProperty.floatValue = nr;
                }
            }
            else if (wind.mode == MagicaWindZone.Mode.BoxDirection)
            {
                var sizeProperty = serializedObject.FindProperty("size");
                var size = sizeProperty.vector3Value;
                var ns = EditorGUILayout.Vector3Field(new GUIContent("Box Size"), size);
                ns = Vector3.Max(ns, Vector3.zero);
                if (size != ns)
                {
                    sizeProperty.vector3Value = ns;
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("main"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("turbulence"));

            if (wind.IsDirection())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("directionAngleX"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("directionAngleY"));
            }
            else
            {
                EditorGUILayout.CurveField(serializedObject.FindProperty("attenuation"), Color.green, new Rect(0, 0, 1, 1));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isAddition"));
        }
    }
}
