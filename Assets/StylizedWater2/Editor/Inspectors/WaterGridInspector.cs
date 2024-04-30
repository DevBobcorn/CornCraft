//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

using UnityEditor;
using UnityEngine;

namespace StylizedWater2
{
    [CustomEditor(typeof(WaterGrid))]
    public class CreateWaterGridInspector : Editor
    {
        private WaterGrid script;

        private SerializedProperty material;
        private SerializedProperty followSceneCamera;
        private SerializedProperty autoAssignCamera;
        private SerializedProperty followTarget;
        
        private SerializedProperty scale;
        private SerializedProperty vertexDistance;
        private SerializedProperty rowsColumns;
        
        private int vertexCount;

        private void OnEnable()
        {
            script = (WaterGrid) target;
            script.m_rowsColumns = script.rowsColumns;

            material = serializedObject.FindProperty("material");
            followSceneCamera = serializedObject.FindProperty("followSceneCamera");
            autoAssignCamera = serializedObject.FindProperty("autoAssignCamera");
            followTarget = serializedObject.FindProperty("followTarget");
            
            scale = serializedObject.FindProperty("scale");
            vertexDistance = serializedObject.FindProperty("vertexDistance");
            rowsColumns = serializedObject.FindProperty("rowsColumns");
        }
        
        public override void OnInspectorGUI()
        {
            UI.DrawHeader();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.labelWidth);
                WaterGrid.DisplayGrid = GUILayout.Toggle(WaterGrid.DisplayGrid , new GUIContent("  Display Grid", EditorGUIUtility.IconContent((WaterGrid.DisplayGrid ? "animationvisibilitytoggleon" : "animationvisibilitytoggleoff")).image), "Button");
                WaterGrid.DisplayWireframe = GUILayout.Toggle(WaterGrid.DisplayWireframe, new GUIContent("  Show Wireframe", EditorGUIUtility.IconContent((WaterGrid.DisplayWireframe ? "animationvisibilitytoggleon" : "animationvisibilitytoggleoff")).image), "Button");
            }
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(material);
            if(material.objectReferenceValue == null) EditorGUILayout.HelpBox("A material must be assigned", MessageType.Error);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(followSceneCamera);
            using (new EditorGUI.DisabledScope(autoAssignCamera.boolValue))
            {
                EditorGUILayout.PropertyField(followTarget);
            }
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(autoAssignCamera);
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Grid geometry", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(scale, GUILayout.MaxWidth(EditorGUIUtility.labelWidth + 95f));

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(rowsColumns.displayName);
                using (new EditorGUI.DisabledScope(rowsColumns.intValue <= 0))
                {
                    if (GUILayout.Button("-", EditorStyles.miniButtonLeft, GUILayout.Width(25f)))
                    {
                        rowsColumns.intValue--;
                    }
                }
                EditorGUILayout.PropertyField(rowsColumns, GUIContent.none, GUILayout.MaxWidth(40f));
                if (GUILayout.Button("+", EditorStyles.miniButtonRight, GUILayout.Width(25f)))
                {
                    rowsColumns.intValue++;
                }
                EditorGUILayout.LabelField($"= {rowsColumns.intValue * rowsColumns.intValue} tiles", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(vertexDistance, new GUIContent("Min. vertex distance", vertexDistance.tooltip));
            vertexCount = Mathf.FloorToInt(((scale.floatValue / rowsColumns.intValue) / vertexDistance.floatValue) * ((scale.floatValue / rowsColumns.intValue) / vertexDistance.floatValue));
            //EditorGUILayout.HelpBox($"Vertex count: {vertexCount}", MessageType.None);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                
                //Executed here since objects can't be destroyed from OnValidate
                script.Recreate();
            }
            
            UI.DrawFooter();
        }
    }
}