//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace StylizedWater2
{
    [ScriptedImporter(3, FILE_EXTENSION)]
    public class WaterMeshImporter : ScriptedImporter
    {
        private const string FILE_EXTENSION = "watermesh";
        
        [SerializeField] public WaterMesh waterMesh = new WaterMesh();

        public override void OnImportAsset(AssetImportContext context)
        {
            waterMesh.Rebuild();

            context.AddObjectToAsset("mesh", waterMesh.mesh);
            context.SetMainObject(waterMesh.mesh);
        }
        
        //Handles correct behaviour when double-clicking a .watermesh asset assigned to a field
        //Otherwise the OS prompts to open it
        [UnityEditor.Callbacks.OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            Object target = EditorUtility.InstanceIDToObject(instanceID);

            if (target is Mesh)
            {
                var path = AssetDatabase.GetAssetPath(instanceID);
                
                if (Path.GetExtension(path) != "." + FILE_EXTENSION) return false;

                Selection.activeObject = target;
                return true;
            }
            
            return false;
        }

    }
	
	[CustomEditor(typeof(WaterMeshImporter))]
    public class WaterMeshImporterEditor: ScriptedImporterEditor
    {
        private SerializedProperty waterMesh;
        
        private SerializedProperty shape;
        
        private SerializedProperty scale;
        private SerializedProperty UVTiling;
        
        private SerializedProperty vertexDistance;
        
        private SerializedProperty noise;
        private SerializedProperty boundsPadding;
        
        private WaterMeshImporter importer;
        
		private bool autoApplyChanges;
        private bool previewInSceneView
        {
            get => EditorPrefs.GetBool("SWS2_PREVIEW_WATER_MESH_ENABLED", true);
            set => EditorPrefs.SetBool("SWS2_PREVIEW_WATER_MESH_ENABLED", value);
        }

        public override void OnEnable()
        {
			base.OnEnable();
			
            importer = (WaterMeshImporter)target;
            
            waterMesh = serializedObject.FindProperty("waterMesh");
            
            shape = waterMesh.FindPropertyRelative("shape");
            scale = waterMesh.FindPropertyRelative("scale");
            UVTiling = waterMesh.FindPropertyRelative("UVTiling");
            vertexDistance = waterMesh.FindPropertyRelative("vertexDistance");
            noise = waterMesh.FindPropertyRelative("noise");
            boundsPadding = waterMesh.FindPropertyRelative("boundsPadding");

            SceneView.duringSceneGui += OnSceneGUI;
        }

        public override void OnInspectorGUI()
        {
            UI.DrawHeader();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.labelWidth);
                previewInSceneView =
                    GUILayout.Toggle(previewInSceneView, new GUIContent("  Preview in scene view", EditorGUIUtility.IconContent(
                        (previewInSceneView ? "animationvisibilitytoggleon" : "animationvisibilitytoggleoff")).image), "Button");
            }
            if (previewInSceneView && WaterObject.Instances.Count > 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(EditorGUIUtility.labelWidth);
                    EditorGUILayout.HelpBox($"Drawing on WaterObject instances in the scene ({WaterObject.Instances.Count})", MessageType.None);
                }
            }
            
            EditorGUILayout.Space();

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.PropertyField(shape);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(scale);
            EditorGUILayout.PropertyField(vertexDistance);
            
            int subdivisions = Mathf.FloorToInt(scale.floatValue / vertexDistance.floatValue);
            int vertexCount = Mathf.FloorToInt(subdivisions * subdivisions);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.labelWidth);
                
                EditorGUILayout.HelpBox($"Vertex count: {vertexCount:N1}", MessageType.None);
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(UVTiling);
            EditorGUILayout.PropertyField(noise);
            EditorGUILayout.PropertyField(boundsPadding);
            
            EditorGUILayout.Space();

            autoApplyChanges = EditorGUILayout.Toggle("Auto-apply changes", autoApplyChanges);
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                
                if (autoApplyChanges && HasModified())
                {
                    #if UNITY_2022_2_OR_NEWER
                    this.SaveChanges();
                    #else
                    this.ApplyAndImport();
                    #endif
                    
                    importer = (WaterMeshImporter)target;
                }
            }
            
            this.ApplyRevertGUI();
            
            UI.DrawFooter();
        }
        
        private void OnDestroy()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private Material mat;
        private void OnSceneGUI(SceneView obj)
        {
            if (!previewInSceneView)
            {
                GL.wireframe = false;
                return;
            }

            if (!mat)
            {
                mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = new Color(0,0,0, 0.25f);
                mat.mainTexture = Texture2D.whiteTexture;
            }
            mat.SetPass(0);
            
            if (importer.waterMesh.mesh)
            {
                GL.wireframe = true;
                if (WaterObject.Instances.Count > 0)
                {
                    foreach (WaterObject waterObject in WaterObject.Instances)
                    {
                        Graphics.DrawMeshNow(importer.waterMesh.mesh, waterObject.transform.localToWorldMatrix);
                    }
                }
                else
                {
                    if (SceneView.lastActiveSceneView)
                    {
                        //Position in view
                        Vector3 position = SceneView.lastActiveSceneView.camera.transform.position + (SceneView.lastActiveSceneView.camera.transform.forward * importer.waterMesh.scale * 0.5f);

                        Graphics.DrawMeshNow(importer.waterMesh.mesh, position, Quaternion.identity);
                    }
                }
                GL.wireframe = false;
            }
        }
    }
}
