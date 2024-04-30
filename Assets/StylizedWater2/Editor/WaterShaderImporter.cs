//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace StylizedWater2
{
    [ScriptedImporterAttribute(AssetInfo.SHADER_GENERATOR_VERSION_MAJOR + AssetInfo.SHADER_GENERATOR_MINOR + AssetInfo.SHADER_GENERATOR_PATCH, TARGET_FILE_EXTENSION, 2)]
    public class WaterShaderImporter : ScriptedImporter
    {
        private const string TARGET_FILE_EXTENSION = "watershader";
        private const string ICON_NAME = "water-shader-icon";
        
        [Tooltip("Rather than storing the template in this file, it can be sourced from an external text file" +
                 "\nUse this if you intent to duplicate this asset, and need only minor modifications to its import settings")]
        [SerializeField] public LazyLoadReference<Object> template;
            
        [Space]
        
        public WaterShaderSettings settings;

        /// <summary>
        /// File paths of any file this shader depends on. This list will be populated with any "#include" paths present in the template
        /// Registering these as dependencies is required to trigger the shader to recompile when these files are changed
        /// </summary>
        //[NonSerialized] //Want to keep these serialized. Will differ per-project, which also causes the file to appear as changed for every project when updating the asset (this triggers a re-import)
        public List<string> dependencies = new List<string>();

        private bool HasExternalTemplate()
        {
            #if UNITY_2020_1_OR_NEWER
            return template.isSet;
            #else
            return template.asset;
            #endif
        }
        
        public string GetTemplatePath()
        {
            return HasExternalTemplate() ? AssetDatabase.GetAssetPath(template.asset) : assetPath;
        }

        private void OnValidate()
        {
            if(settings.shaderName == string.Empty) settings.shaderName = $"{Application.productName} ({DateTime.Now.Ticks})";
        }

        public override void OnImportAsset(AssetImportContext context)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(context.assetPath);
            //if (shader != null) ShaderUtil.ClearShaderMessages(shader);
            
            string templatePath = GetTemplatePath();

            if (templatePath == string.Empty)
            {
                Debug.LogError("Failed to import water shader, template file path is null. It possibly hasn't been imported first?", shader);
                return;
            }
            
            #if SWS_DEV
            Stopwatch sw = new Stopwatch();
            sw.Start();
            #endif
            
            string[] lines = File.ReadAllLines(templatePath);

            if (lines.Length == 0)
            {
                Debug.LogError("Failed to generated water shader. Template or file content is empty (or wasn't yet imported)...");
                return;
            }

            dependencies.Clear();
            
            string templateContents = ShaderConfigurator.TemplateParser.CreateShaderCode(context.assetPath, ref lines, this, false);
            
            Shader shaderAsset = ShaderUtil.CreateShaderAsset(templateContents, true);
            ShaderUtil.RegisterShader(shaderAsset);
            
            Texture2D thumbnail = Resources.Load<Texture2D>(ICON_NAME);
            if(!thumbnail) thumbnail = EditorGUIUtility.IconContent("ShaderImporter Icon").image as Texture2D;
            
            context.AddObjectToAsset("MainAsset", shaderAsset, thumbnail);
            context.SetMainObject(shaderAsset);
            
            //Do not attempt to create a tessellation variant for the underwater post-effect shaders
            if (settings.type == WaterShaderSettings.ShaderType.WaterSurface)
            {
                //Re-read the original template again
                lines = File.ReadAllLines(templatePath);
                templateContents = ShaderConfigurator.TemplateParser.CreateShaderCode(context.assetPath, ref lines, this, true);

                Shader tessellation = ShaderUtil.CreateShaderAsset(templateContents, true);
                //ShaderUtil.RegisterShader(tessellation);
                
                context.AddObjectToAsset("Tessellation", (Object)tessellation, thumbnail);
            }
            
            //Set up dependency, so that changes to the template triggers shaders to regenerate
            if (HasExternalTemplate() && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(template, out var guid, out long _))
            {
                //Note: this strictly only works when adding the file path!
                //context.DependsOnArtifact(guid);
                
                dependencies.Insert(0, AssetDatabase.GUIDToAssetPath(guid));
            }

            //Dependencies are populated during the template parsing phase.
            foreach (string dependency in dependencies)
            {
                context.DependsOnSourceAsset(dependency);
            }
            
            #if SWS_DEV
            sw.Stop();
            //Debug.Log($"Imported \"{Path.GetFileNameWithoutExtension(assetPath)}\" water shader in {sw.Elapsed.Milliseconds}ms. With {dependencies.Count} dependencies.", shader);
            #endif
        }

        public void ClearCache(bool recompile = false)
        {
            var objs = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            
            foreach (var obj in objs)
            {
                if (obj is Shader)
                {
                    ShaderUtil.ClearShaderMessages((Shader)obj);
                    ShaderUtil.ClearCachedData((Shader)obj);
                    
                    if(recompile) AssetDatabase.ImportAsset(assetPath);
                    
                    #if SWS_DEV
                    Debug.Log($"Cleared cache for {obj.name}");
                    #endif
                }
            }
        }
        public void RegisterDependency(string dependencyAssetPath)
        {
            if (dependencyAssetPath.StartsWith("Packages/") == false)
            {
                string guid = AssetDatabase.AssetPathToGUID(dependencyAssetPath);

                if (guid == string.Empty)
                {
                    //Also throws an error for things like '#include_library "SurfaceModifiers/SurfaceModifiers.hlsl"', which are wrapped in an #ifdef. That's a false positive
                    //Debug.LogException(new Exception($"Tried to import \"{this.assetPath}\" with an missing dependency, supposedly at path: {dependencyAssetPath}."));
                    return;
                }
            }

            //Tessellation variant pass may run, causing the same dependencies to be registered twice, hence check first
            if(dependencies.Contains(dependencyAssetPath) == false) dependencies.Add(dependencyAssetPath);
        }
        
        //Handles correct behaviour when double-clicking a .watershader asset. Should open in the IDE
        [UnityEditor.Callbacks.OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            Object target = EditorUtility.InstanceIDToObject(instanceID);

            if (target is Shader)
            {
                var path = AssetDatabase.GetAssetPath(instanceID);
                
                if (Path.GetExtension(path) != "." + TARGET_FILE_EXTENSION) return false;

                string externalScriptEditor = ScriptEditorUtility.GetExternalScriptEditor();
                if (externalScriptEditor != "internal")
                {
                    InternalEditorUtility.OpenFileAtLineExternal(path, 0);
                }
                else
                {
                    Application.OpenURL("file://" + path);
                }
                
                return true;
            }
            
            return false;
        }

        [Serializable]
        public class Directive
        {
            public enum Type
            {
                [InspectorName("(no prefix)")]
                custom,
                [InspectorName("#include")]
                include,
                [InspectorName("#pragma")]
                pragma,
                [InspectorName("#define")]
                define
            }
            public bool enabled = true;
            public Type type;
            public string value;

            public Directive(Type _type, string _value)
            {
                this.type = _type;
                this.value = _value;
            }
        }
        
        class WaterShaderAssetPostProcessor : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                RegisterShaders(importedAssets);
            }

            //Register imported water shaders, so they work with Shader.Find() and show up in the shader selection menu
            private static void RegisterShaders(string[] paths)
            {
                foreach (var path in paths)
                {
                    if (!path.EndsWith(WaterShaderImporter.TARGET_FILE_EXTENSION, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    var mainObj = AssetDatabase.LoadMainAssetAtPath(path);
                    if (mainObj is Shader)
                    {
                        if (mainObj.name == string.Empty) return;
                        
                        //ShaderUtil.RegisterShader((Shader)mainObj);
                        
                        #if SWS_DEV
                        //Debug.Log($"Registered water shader \"{mainObj.name}\" on import", mainObj);
                        #endif

                        return;
                    }
                }
            }
        }

        public static string[] FindAllAssets()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath);
            
            FileInfo[] fileInfos = directoryInfo.GetFiles("*." + TARGET_FILE_EXTENSION, SearchOption.AllDirectories);
            
            #if SWS_DEV
            Debug.Log($"{fileInfos.Length} .{TARGET_FILE_EXTENSION} assets found");
            #endif

            string[] filePaths = new string[fileInfos.Length];

            for (int i = 0; i < filePaths.Length; i++)
            {
                filePaths[i] = fileInfos[i].FullName.Replace(@"\", "/").Replace(Application.dataPath, "Assets");
            }

            return filePaths;
        }
        
        #if SWS_DEV
        [MenuItem("SWS/Reimport water shaders")]
        #endif
        public static void ReimportAll()
        {
            string[] filePaths = FindAllAssets();
            foreach (var filePath in filePaths)
            {
                #if SWS_DEV
                Debug.Log($"Reimporting: {filePath}");
                #endif
                AssetDatabase.ImportAsset(filePath);
            }
        }

        [Serializable]
        public class WaterShaderSettings
        {
            [Tooltip("How it will appear in the selection menu")]
            public string shaderName;
            [Tooltip("Hide the shader in the selection menu. Yet still make it findable with Shader.Find()")]
            public bool hidden;
            public enum ShaderType
            {
                WaterSurface,
                PostProcessing
            }
            public ShaderType type;
            
            [Tooltip("Before compiling the shader, check whichever asset is present in the project and activate its integration")]
            public bool autoIntegration = true;
            public ShaderConfigurator.Fog.Assets fogIntegration = ShaderConfigurator.Fog.Assets.UnityFog;

            [Tooltip("Add support for native light cookies. Disabled by default to allow for cookies to act as caustics projectors that ignore the water surface")]
            public bool lightCookies = false;
            
            public List<Directive> customIncludeDirectives = new List<Directive>();
        }
    }
}