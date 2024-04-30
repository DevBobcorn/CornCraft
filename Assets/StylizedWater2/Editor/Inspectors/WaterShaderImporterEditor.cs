//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace StylizedWater2
{
    [CustomEditor(typeof(WaterShaderImporter))]
    [CanEditMultipleObjects]
    public class WaterShaderImporterEditor : ScriptedImporterEditor
    {
        private WaterShaderImporter importer;

        private SerializedProperty template;

        private SerializedProperty settings;

        private SerializedProperty shaderName;
        private SerializedProperty hidden;
        private SerializedProperty type;

        private SerializedProperty autoIntegration;
        private SerializedProperty fogIntegration;

        private SerializedProperty lightCookies;

        private SerializedProperty customIncludeDirectives;

        private bool underwaterRenderingInstalled;
        private bool dynamicEffectsInstalled;
        private ShaderConfigurator.Fog.Integration firstIntegration;
        private bool curvedWorldInstalled;

        private bool showDependencies;

        public override void OnEnable()
        {
            base.OnEnable();

            underwaterRenderingInstalled = StylizedWaterEditor.UnderwaterRenderingInstalled();
            dynamicEffectsInstalled = StylizedWaterEditor.DynamicEffectsInstalled();
            firstIntegration = ShaderConfigurator.Fog.GetFirstInstalled();
            curvedWorldInstalled = StylizedWaterEditor.CurvedWorldInstalled(out var _);

            importer = (WaterShaderImporter)target;

            template = serializedObject.FindProperty("template");

            settings = serializedObject.FindProperty("settings");
            //settings.isExpanded = true;

            shaderName = settings.FindPropertyRelative("shaderName");
            hidden = settings.FindPropertyRelative("hidden");
            type = settings.FindPropertyRelative("type");
            
            lightCookies = settings.FindPropertyRelative("lightCookies");

            autoIntegration = settings.FindPropertyRelative("autoIntegration");
            fogIntegration = settings.FindPropertyRelative("fogIntegration");

            customIncludeDirectives = settings.FindPropertyRelative("customIncludeDirectives");
        }

        public override bool HasPreviewGUI()
        {
            //Hide the useless sphere preview :)
            return false;
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            Color defaultColor = GUI.contentColor;

            UI.DrawHeader();

            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(importer.assetPath);
            if (shader == null)
            {
                UI.DrawNotification("Shader failed to compile, try to manually recompile it now.", MessageType.Error);
            }

            if (GUILayout.Button(new GUIContent("  Recompile", EditorGUIUtility.IconContent("RotateTool").image), GUILayout.MinHeight(30f)))
            {
                importer.SaveAndReimport();
                return;
            }

            GUILayout.Space(-2f);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledGroupScope(shader == null))
                {
                    if (GUILayout.Button(new GUIContent("  Show Generated Code", EditorGUIUtility.IconContent("align_horizontally_left_active").image), EditorStyles.miniButtonLeft, GUILayout.Height(28f)))
                    {
                        GenericMenu menu = new GenericMenu();

                        menu.AddItem(new GUIContent("With tessellation"), false, () => OpenGeneratedCode(true));
                        menu.AddItem(new GUIContent("Without tessellation"), false, () => OpenGeneratedCode(false));

                        menu.ShowAsContext();
                    }
                    if (GUILayout.Button(new GUIContent("Clear cache", "Unity's shader compiler will cache the compiled shader, and internally use that." +
                                                                       "\n\nThis may result in seemingly false-positive shader errors. Such as in the case of importing the shader, before the URP shader libraries are." +
                                                                       "\n\nClearing the cache gives the compiler a kick, and makes the shader properly represent the current state of the project/dependencies."), EditorStyles.miniButtonRight, GUILayout.Height(28f)))
                    {
                        importer.ClearCache();
                    }
                }
            }

            EditorGUILayout.Space();

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(template);

            if (template.objectReferenceValue == null) EditorGUILayout.HelpBox("• Template is assumed to be in the contents of the file itself", MessageType.None);
            //EditorGUILayout.LabelField(importer.GetTemplatePath(), EditorStyles.miniLabel);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(shaderName);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(hidden);
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(type);

            if (type.intValue == (int)WaterShaderImporter.WaterShaderSettings.ShaderType.WaterSurface)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Integrations", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(autoIntegration, new GUIContent("Automatic detection", autoIntegration.tooltip));
                if (autoIntegration.boolValue)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField("Fog post-processing", GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
                        EditorGUI.indentLevel--;

                        using (new EditorGUILayout.HorizontalScope(EditorStyles.textField))
                        {
                            GUI.contentColor = Color.green;
                            EditorGUILayout.LabelField(firstIntegration.name);

                            GUI.contentColor = defaultColor;
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField("Curved World 2020", GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
                        EditorGUI.indentLevel--;

                        using (new EditorGUILayout.HorizontalScope(EditorStyles.textField))
                        {
                            if (curvedWorldInstalled)
                            {
                                GUI.contentColor = Color.green;
                                EditorGUILayout.LabelField("Installed");
                            }
                            else
                            {
                                GUI.contentColor = new Color(1f, 0.65f, 0f);
                                EditorGUILayout.LabelField("(Not installed)");
                            }

                            GUI.contentColor = defaultColor;
                        }
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(fogIntegration);
                }
                if (curvedWorldInstalled) EditorGUILayout.HelpBox("Curved World integration must be activated through Window->Amazing Assets->Curved Word (Activator tab)", MessageType.Info);
                
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Functionality support", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(lightCookies);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Extensions", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Underwater Rendering", GUILayout.MaxWidth(EditorGUIUtility.labelWidth));

                using (new EditorGUILayout.HorizontalScope(EditorStyles.textField))
                {
                    if (underwaterRenderingInstalled)
                    {
                        GUI.contentColor = Color.green;
                        EditorGUILayout.LabelField("Installed");
                    }
                    else
                    {
                        GUI.contentColor = new Color(1f, 0.65f, 0f);
                        EditorGUILayout.LabelField("(Not installed)");
                    }

                    GUI.contentColor = defaultColor;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Dynamic Effects", GUILayout.MaxWidth(EditorGUIUtility.labelWidth));

                using (new EditorGUILayout.HorizontalScope(EditorStyles.textField))
                {
                    if (dynamicEffectsInstalled)
                    {
                        GUI.contentColor = Color.green;
                        EditorGUILayout.LabelField("Installed");
                    }
                    else
                    {
                        GUI.contentColor = new Color(1f, 0.65f, 0f);
                        EditorGUILayout.LabelField("(Not installed)");
                    }

                    GUI.contentColor = defaultColor;
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(customIncludeDirectives);
            if (customIncludeDirectives.isExpanded)
            {
                EditorGUILayout.HelpBox("These are defined in a HLSLINCLUDE block and apply to all passes" +
                                        "\nMay be used to insert custom code.", MessageType.Info);
            }

            if (EditorGUI.EndChangeCheck())
            {
                //Force the parameter to a matching value.
                //This way, if the "auto-integration" option is used, the .meta file will be changed when using the shader in a package, spanning different projects.
                //When switching a different project, the file will be seen as changed and will be re-imported, in turn applying the project-specific integration.
                if (autoIntegration.boolValue)
                {
                    fogIntegration.intValue = (int)firstIntegration.asset;
                }

                serializedObject.ApplyModifiedProperties();
            }

            this.ApplyRevertGUI();

            showDependencies = EditorGUILayout.BeginFoldoutHeaderGroup(showDependencies, $"Dependencies ({importer.dependencies.Count})");

            if (showDependencies)
            {
                this.Repaint();

                using (new EditorGUILayout.VerticalScope(EditorStyles.textArea))
                {
                    foreach (string dependency in importer.dependencies)
                    {
                        var rect = EditorGUILayout.BeginHorizontal(EditorStyles.miniLabel);

                        if (rect.Contains(Event.current.mousePosition))
                        {
                            EditorGUIUtility.AddCursorRect(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 27, 27), MouseCursor.Link);
                            EditorGUI.DrawRect(rect, Color.gray * (EditorGUIUtility.isProSkin ? 0.66f : 0.20f));
                        }

                        if (GUILayout.Button(dependency == string.Empty ? new GUIContent(" (Missing)", EditorGUIUtility.IconContent("console.warnicon.sml").image) : new GUIContent(" " + dependency, EditorGUIUtility.IconContent("TextAsset Icon").image),
                                EditorStyles.miniLabel, GUILayout.Height(20f)))
                        {
                            if (dependency != string.Empty)
                            {
                                TextAsset file = AssetDatabase.LoadAssetAtPath<TextAsset>(dependency);

                                EditorGUIUtility.PingObject(file);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUILayout.HelpBox("Should any of these files be modified/moved/deleted, this shader will also re-import", MessageType.Info);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            UI.DrawFooter();

            if (shader)
            {
                UI.DrawNotification(ShaderUtil.ShaderHasError(shader), "Errors may be false-positives due to caching", "Clear cache", () => importer.ClearCache(true), MessageType.Warning);
            }
        }

        void OpenGeneratedCode(bool tessellation)
        {
            importer = (WaterShaderImporter)target;

            string filePath = $"{Application.dataPath.Replace("Assets", string.Empty)}Temp/{importer.settings.shaderName}(Generated Code).shader";

            string code = ShaderConfigurator.TemplateParser.CreateShaderCode(importer.GetTemplatePath(), importer, tessellation);
            File.WriteAllText(filePath, code);

            if (!File.Exists(filePath))
            {
                Debug.LogError(string.Format("Path {0} doesn't exists", filePath));
                return;
            }

            string externalScriptEditor = ScriptEditorUtility.GetExternalScriptEditor();
            if (externalScriptEditor != "internal")
            {
                InternalEditorUtility.OpenFileAtLineExternal(filePath, 0);
            }
            else
            {
                Application.OpenURL("file://" + filePath);
            }
        }
    }
}