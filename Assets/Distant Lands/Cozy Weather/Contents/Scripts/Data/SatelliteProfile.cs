using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy.Data
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Distant Lands/Cozy/Satellite Profile", order = 361)]
    public class SatelliteProfile : ScriptableObject
    {

        public GameObject satelliteReference;
        public Transform orbitRef;
        public Transform moonRef;
        public Light lightRef;
        public float size = 1;
        [Range(0, 1)]
        public float distance = 1;
        public bool useLight = true;
        public Flare flare;
        public Color lightColorMultiplier = Color.white;
        public LightShadows castShadows;
        public float orbitOffset;
        public Vector3 initialRotation;
        public float satelliteRotateSpeed;
        public Vector3 satelliteRotateAxis;
        public float satelliteDirection;
        public float satelliteRotation;
        public float satellitePitch;
        public bool changedLastFrame;
        public bool open;

    }
#if UNITY_EDITOR
    [CustomEditor(typeof(SatelliteProfile))]
    [CanEditMultipleObjects]
    public class E_SatelliteProfile : Editor
    {


        public override void OnInspectorGUI()
        {

            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("satelliteReference"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("distance"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useLight"));
            EditorGUI.BeginDisabledGroup(!serializedObject.FindProperty("useLight").boolValue);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("flare"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lightColorMultiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("castShadows"));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("satelliteRotateSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("satelliteRotateAxis"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("initialRotation"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("orbitOffset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("satelliteDirection"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("satellitePitch"));

            if (serializedObject.hasModifiedProperties)
                serializedObject.FindProperty("changedLastFrame").boolValue = true;

            serializedObject.ApplyModifiedProperties();

        }

        public void NestedGUI()
        {

            serializedObject.Update();

            serializedObject.FindProperty("open").boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(serializedObject.FindProperty("open").boolValue, $"    {target.name}", EditorUtilities.FoldoutStyle());
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (serializedObject.FindProperty("open").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("satelliteReference"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("size"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("distance"));
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useLight"));
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(!serializedObject.FindProperty("useLight").boolValue);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("flare"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lightColorMultiplier"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("castShadows"));
                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("satelliteRotateSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("satelliteRotateAxis"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("initialRotation"));
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("orbitOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("satelliteDirection"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("satellitePitch"));

                if (serializedObject.hasModifiedProperties)
                    serializedObject.FindProperty("changedLastFrame").boolValue = true;
                EditorGUI.indentLevel--;

            }
            serializedObject.ApplyModifiedProperties();

        }

    }

#endif
}