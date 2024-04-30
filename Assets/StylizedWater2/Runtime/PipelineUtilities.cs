//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
#if URP
using UnityEngine.Rendering.Universal;

#if !UNITY_2021_2_OR_NEWER
using UniversalRendererData = UnityEngine.Rendering.Universal.ForwardRendererData;
#endif

using ScriptableRendererFeature = UnityEngine.Rendering.Universal.ScriptableRendererFeature;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StylizedWater2
{
	//Stay awesome Unity, locking everything behind internal UI code just makes things convoluted.
    public static class PipelineUtilities
    {
        private const string renderDataListFieldName = "m_RendererDataList";
        private const string renderFeaturesListFieldName = "m_RendererFeatures";
        private const string defaultRendererIndexFieldName = "m_DefaultRendererIndex";
        
#if URP
        public static ScriptableRendererData[] GetRenderDataList(UniversalRenderPipelineAsset asset)
        {
            FieldInfo renderDataListField = typeof(UniversalRenderPipelineAsset).GetField(renderDataListFieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (renderDataListField != null)
            {
                return (ScriptableRendererData[])renderDataListField.GetValue(asset);
            }

            throw new Exception("Reflection failed on field \"m_RendererDataList\" from class \"UniversalRenderPipelineAsset\". URP API likely changed");
        }
        
        public static void RefreshRendererList()
        {
            if (UniversalRenderPipeline.asset == null)
            {
                Debug.LogError("No pipeline is active, do not display UI that uses this function if it isn't!");
            }
            
            ScriptableRendererData[] m_rendererDataList = GetRenderDataList(UniversalRenderPipeline.asset);
              
            //Display names
            _rendererDisplayList = new GUIContent[m_rendererDataList.Length+1];

            int defaultIndex = GetDefaultRendererIndex(UniversalRenderPipeline.asset);
            _rendererDisplayList[0] = new GUIContent($"Default ({(m_rendererDataList[defaultIndex].name)})");
                    
            for (int i = 1; i < _rendererDisplayList.Length; i++)
            {
                if (m_rendererDataList[i - 1] != null)
                {
                    _rendererDisplayList[i] = new GUIContent($"{(i - 1).ToString()}: {(m_rendererDataList[i - 1]).name}");
                }
                else
                {
                    _rendererDisplayList[i] = new GUIContent("(Missing)");
                }
            }
            
            //Indices
            _rendererIndexList = new int[m_rendererDataList.Length+1];
            for (int i = 0; i < _rendererIndexList.Length; i++)
            {
                _rendererIndexList[i] = i-1;
            }
        }

        private static GUIContent[] _rendererDisplayList;
        public static GUIContent[] rendererDisplayList
        {
            get
            {
                if (_rendererDisplayList == null) RefreshRendererList();
                return _rendererDisplayList;
            }
        }

        private static int[] _rendererIndexList;
        public static int[] rendererIndexList
        {
            get
            {
                if (_rendererIndexList == null) RefreshRendererList();
                return _rendererIndexList;
            }
        }

        /// <summary>
        /// Given a renderer index, validates if there is actually a renderer at the index. Otherwise returns the index of the default renderer.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int ValidateRenderer(int index)
        {
            if (UniversalRenderPipeline.asset)
            {
                int defaultRendererIndex = GetDefaultRendererIndex(UniversalRenderPipeline.asset);
                ScriptableRendererData[] m_rendererDataList = GetRenderDataList(UniversalRenderPipeline.asset);
                
                //-1 is used to indicate the default renderer
                if (index == -1) index = defaultRendererIndex;

                //Check if any renderer exists at the current index
                if (!(index < m_rendererDataList.Length && m_rendererDataList[index] != null))
                {
                    Debug.LogWarning($"Renderer at <b>index {index.ToString()}</b> is missing, falling back to Default Renderer. <b>{m_rendererDataList[defaultRendererIndex].name}</b>", UniversalRenderPipeline.asset);
                    return defaultRendererIndex;
                }
                else
                {
                    //Valid
                    return index;
                }
            }
            else
            {
                Debug.LogError("No Universal Render Pipeline is currently active.");
                return 0;
            }
        }
        
        /// <summary>
        /// Checks if a ForwardRenderer has been assigned to the pipeline asset
        /// </summary>
        /// <param name="renderer"></param>
        public static bool IsRendererAdded(ScriptableRendererData renderer)
        {
            if (UniversalRenderPipeline.asset)
            {
                ScriptableRendererData[] m_rendererDataList = GetRenderDataList(UniversalRenderPipeline.asset);
                bool isPresent = false;

                for (int i = 0; i < m_rendererDataList.Length; i++)
                {
                    if (m_rendererDataList[i] == renderer) isPresent = true;
                }

                return isPresent;
            }
            else
            {
                Debug.LogError("No Universal Render Pipeline is currently active.");
                return false;
            }
        }
        
        /// <summary>
        /// Adds a ForwardRenderer to the pipeline asset in use
        /// </summary>
        /// <param name="renderer"></param>
        private static int AddRendererToPipeline(ScriptableRendererData renderer)
        {
            if (renderer == null) return -1;

            if (UniversalRenderPipeline.asset)
            {
                ScriptableRendererData[] m_rendererDataList = GetRenderDataList(UniversalRenderPipeline.asset);
                List<ScriptableRendererData> rendererDataList = new List<ScriptableRendererData>();

                for (int i = 0; i < m_rendererDataList.Length; i++)
                {
                    rendererDataList.Add(m_rendererDataList[i]);
                }

                rendererDataList.Add(renderer);
                int index = rendererDataList.Count-1;

                typeof(UniversalRenderPipelineAsset).GetField(renderDataListFieldName, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(UniversalRenderPipeline.asset, rendererDataList.ToArray());

#if UNITY_EDITOR
                EditorUtility.SetDirty(UniversalRenderPipeline.asset);
#endif
                
                RefreshRendererList();
                
                return index;
            }
            else
            {
                Debug.LogError("No Universal Render Pipeline is currently active.");
            }

            return -1;
        }

        private static int GetDefaultRendererIndex(UniversalRenderPipelineAsset asset)
        {
            FieldInfo fieldInfo = typeof(UniversalRenderPipelineAsset).GetField(defaultRendererIndexFieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo == null) {throw new Exception($"Reflection failed on the field named \"{defaultRendererIndexFieldName}\". It may have changed in the current Unity version");}

            return (int)fieldInfo.GetValue(asset);
        }

        /// <summary>
        /// Gets the renderer from the current pipeline asset that's marked as default
        /// </summary>
        /// <returns></returns>
        public static ScriptableRendererData GetDefaultRenderer(UniversalRenderPipelineAsset asset = null)
        {
            if (asset == null) asset = UniversalRenderPipeline.asset;
            
            if (asset)
            {
                ScriptableRendererData[] rendererDataList = GetRenderDataList(asset);
                int defaultRendererIndex = GetDefaultRendererIndex(asset);

                return rendererDataList[defaultRendererIndex];
            }

            throw new Exception("No Universal Render Pipeline is currently active.");
        }

        /// <summary>
        /// Editor only! Checks if the given render feature is missing on any renderers. Displays a pop up if that is the case, with the option to add it
        /// </summary>
        /// <param name="name">Descriptive name for the render feature</param>
        /// <typeparam name="T">Render feature type</typeparam>
        [Conditional("UNITY_EDITOR")]
        public static void ValidateRenderFeatureSetup<T>(string name)
        {
            if (Application.isPlaying == false)
            {
                if (RenderFeatureMissing<T>(out ScriptableRendererData[] renderers))
                {
                    #if UNITY_EDITOR
                    string[] rendererNames = new string[renderers.Length];
                    for (int i = 0; i < rendererNames.Length; i++)
                    {
                        rendererNames[i] = "• " + renderers[i].name;
                    }

                    if (EditorUtility.DisplayDialog($"Stylized Water 2", 
                            $"The {name} render feature hasn't been added to the following renderers:\n\n" + 
                            System.String.Join(System.Environment.NewLine, rendererNames) + 
                            $"\n\nThis is required for rendering to take effect", "Setup", "Ignore"))
                    {
                        SetupRenderFeature<T>(name:$"Stylized Water 2: {name}");
                    }
                    #endif
                }
            }
        }

        /// <summary>
        /// Retrieves the given render feature from the default renderer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ScriptableRendererFeature GetRenderFeature<T>(ScriptableRendererData renderer = null)
        {
            if(renderer == null) renderer = GetDefaultRenderer();
            
            foreach (ScriptableRendererFeature feature in renderer.rendererFeatures)
            {
                if (feature && feature.GetType() == typeof(T)) return feature;
            }

            return null;
        }

        /// <summary>
        /// Checks if a ScriptableRendererFeature is added to the default renderer
        /// </summary>
        /// <param name="addIfMissing"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool RenderFeatureAdded<T>(ScriptableRendererData renderer = null)
        {
            if(renderer == null) renderer = GetDefaultRenderer();

            foreach (ScriptableRendererFeature feature in renderer.rendererFeatures)
            {
                if(feature == null) continue;

                if (feature.GetType() == typeof(T))
                {
                    return true;
                }
            }
            
            return false;
        }
		
        /// <summary>
        /// Checks if the given render feature is missing on any configured renderers
        /// </summary>
        /// <param name="renderers"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
		public static bool RenderFeatureMissing<T>(out ScriptableRendererData[] renderers)
		{
			List<ScriptableRendererData> unconfigured = new List<ScriptableRendererData>();
			
			foreach (var asset in GraphicsSettings.allConfiguredRenderPipelines)
            {
                ScriptableRendererData renderer = GetDefaultRenderer((UniversalRenderPipelineAsset)asset);
				
                if(RenderFeatureAdded<T>(renderer) == false) 
                {
                    unconfigured.Add(renderer);
                }
            }
			
			renderers = unconfigured.ToArray();

            return unconfigured.Count > 0;
        }

        /// <summary>
        /// Adds a render feature of a given type to all default renderers
        /// </summary>
        /// <param name="name"></param>
        /// <typeparam name="T"></typeparam>
        public static void SetupRenderFeature<T>(string name = "")
        {
            foreach (var asset in GraphicsSettings.allConfiguredRenderPipelines)
            {
                ScriptableRendererData renderer = GetDefaultRenderer((UniversalRenderPipelineAsset)asset);
				
                if(RenderFeatureAdded<T>(renderer) == false) AddRenderFeature<T>(renderer, name);
            }
        }

        /// <summary>
        /// Adds a ScriptableRendererFeature to the renderer (default is none is supplied)
        /// </summary>
        /// <param name="renderer"></param>
        /// <typeparam name="T"></typeparam>
        public static ScriptableRendererFeature AddRenderFeature<T>(ScriptableRendererData renderer = null, string name = "")
        {
            if (renderer == null) renderer = GetDefaultRenderer();
            
            ScriptableRendererFeature feature = (ScriptableRendererFeature)ScriptableRendererFeature.CreateInstance(typeof(T).ToString());
            feature.name = name == string.Empty ? typeof(T).ToString() : name;
            
            //Add component https://github.com/Unity-Technologies/Graphics/blob/d0473769091ff202422ad13b7b764c7b6a7ef0be/com.unity.render-pipelines.universal/Editor/ScriptableRendererDataEditor.cs#L180
#if UNITY_EDITOR
            AssetDatabase.AddObjectToAsset(feature, renderer);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out var guid, out long localId);
#endif

            //Get feature list
            FieldInfo renderFeaturesInfo = typeof(ScriptableRendererData).GetField(renderFeaturesListFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            List<ScriptableRendererFeature> m_RendererFeatures = (List<ScriptableRendererFeature>)renderFeaturesInfo.GetValue(renderer);

            //Modify and set list
            m_RendererFeatures.Add(feature);
            renderFeaturesInfo.SetValue(renderer, m_RendererFeatures);

            //Onvalidate will call ValidateRendererFeatures and update m_RendererPassMap
            MethodInfo validateInfo = typeof(ScriptableRendererData).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            validateInfo.Invoke(renderer, null);

#if UNITY_EDITOR
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();
#endif
            
            Debug.Log("<b>" + feature.name + "</b> was added to the <i>" + renderer.name + "</i> renderer");

            return feature;
        }

        public static bool IsRenderFeatureEnabled<T>(ScriptableRendererData forwardRenderer = null, bool autoEnable = false)
        {	
			if (!UniversalRenderPipeline.asset) return true;
			
            #if UNITY_2020_1_OR_NEWER //Older version doesn't have the isActive property
            if (forwardRenderer == null) forwardRenderer = GetDefaultRenderer();
            
            FieldInfo renderFeaturesInfo = typeof(ScriptableRendererData).GetField(renderFeaturesListFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            List<ScriptableRendererFeature> m_RendererFeatures = (List<ScriptableRendererFeature>)renderFeaturesInfo.GetValue(forwardRenderer);

            foreach (ScriptableRendererFeature feature in m_RendererFeatures)
            {
                if (feature && feature.GetType() == typeof(T))
                {
                    if (feature.isActive == false && autoEnable)
                    {
                        feature.SetActive(true);
                        
                        #if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(forwardRenderer);
                        #endif
                    }
                    
                    return feature.isActive;
                }
            }
            #endif
            
            //Fallback, if it is not even in the list
            return true;
        }

        public static void ToggleRenderFeature<T>(bool state)
        {
            #if UNITY_2020_1_OR_NEWER
            ScriptableRendererData forwardRenderer = GetDefaultRenderer();
            
            foreach (ScriptableRendererFeature feature in forwardRenderer.rendererFeatures)
            {
                if (feature && feature.GetType() == typeof(T)) feature.SetActive(state);
            }
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(forwardRenderer);
            #endif
            #endif
        }

        public static void CreateAndAssignNewRenderer(out int index, out string path)
        {
            ScriptableRendererData defaultRenderer = GetDefaultRenderer();
            
            path = string.Empty;
            
            #if UNITY_EDITOR
            //Save next to default renderer
            path = AssetDatabase.GetAssetPath(defaultRenderer);
            path = path.Replace(defaultRenderer.name + ".asset", string.Empty);
            #endif
            
            ScriptableRendererData r = CreateEmptyRenderer("Planar Reflections Renderer", path);
            #if UNITY_EDITOR
            path = AssetDatabase.GetAssetPath(r);
            #endif
            
            index = AddRendererToPipeline(r);
            
            //Debug.Log("Created new renderer with index " + index);
        }
        
        /// <summary>
        /// Create an empty renderer, without any render features, but otherwise suitable for camera rendering
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static UniversalRendererData CreateEmptyRenderer(string name = "", string folder = "")
        {
            ScriptableRendererData defaultRenderer = GetDefaultRenderer();
            
            UniversalRendererData rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
  
            #if UNITY_EDITOR
            //Save asset to disk, and load
            if (folder != string.Empty)
            {
                string path = $"{folder}{name}.asset";
                AssetDatabase.CreateAsset(rendererData, path);

                AssetDatabase.ImportAsset(path);
                
                rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            }
            #endif
            
            UniversalRendererData r = (UniversalRendererData)defaultRenderer;
            
            #if UNITY_EDITOR
            //Copy all fields. This should include the shader references, and post processing + XR data. Failing to do so results in nullrefs on these resources when using the renderer.
            EditorUtility.CopySerialized(r, rendererData);
            #endif

            //After copying, apply these unique changes
            rendererData.name = name; //Name must match file name
            rendererData.rendererFeatures.Clear();
            
            /* CopySerialized function accounts for any public fields
            rendererData.shaders = r.shaders;
            rendererData.postProcessData = r.postProcessData;
            
            #if UNITY_2021_2_OR_NEWER
            rendererData.debugShaders = r.debugShaders;
            rendererData.xrSystemData = r.xrSystemData;
            #endif
            */

            return rendererData;
        }
        
        public static void RemoveRendererFromPipeline(ScriptableRendererData renderer)
        {
            if (renderer == null) return;

            if (UniversalRenderPipeline.asset)
            {
                BindingFlags bindings = BindingFlags.NonPublic | BindingFlags.Instance;

                ScriptableRendererData[] m_rendererDataList = GetRenderDataList(UniversalRenderPipeline.asset);
                List<ScriptableRendererData> rendererDataList = new List<ScriptableRendererData>(m_rendererDataList);

                if (rendererDataList.Contains(renderer))
                {
                    rendererDataList.Remove(renderer);
                    
                    typeof(UniversalRenderPipelineAsset).GetField(renderDataListFieldName, bindings).SetValue(UniversalRenderPipeline.asset, rendererDataList.ToArray());

#if UNITY_EDITOR
                    EditorUtility.SetDirty(UniversalRenderPipeline.asset);
                    AssetDatabase.SaveAssets();
#endif
                }
            }
            else
            {
                Debug.LogError("No Universal Render Pipeline is currently active.");
            }
        }

        /// <summary>
        /// Sets the renderer index of the related forward renderer
        /// </summary>
        /// <param name="camData"></param>
        /// <param name="renderer"></param>
        public static void AssignRendererToCamera(UniversalAdditionalCameraData camData, ScriptableRendererData renderer)
        {
            if (UniversalRenderPipeline.asset)
            {
                if (renderer)
                {
                    ScriptableRendererData[] rendererDataList = GetRenderDataList(UniversalRenderPipeline.asset);

                    for (int i = 0; i < rendererDataList.Length; i++)
                    {
                        if (rendererDataList[i] == renderer) camData.SetRenderer(i);
                    }
                }
            }
            else
            {
                Debug.LogError("No Universal Render Pipeline is currently active.");
            }
        }

        public static bool IsDepthTextureOptionDisabledAnywhere()
        {
            bool state = false;

            for (int i = 0; i < GraphicsSettings.allConfiguredRenderPipelines.Length; i++)
            {
                if(GraphicsSettings.allConfiguredRenderPipelines[i].GetType() != typeof(UniversalRenderPipelineAsset)) continue;
                
                UniversalRenderPipelineAsset pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.allConfiguredRenderPipelines[i];

                state |= (pipeline.supportsCameraDepthTexture == false);
            }

            return state;
        }

        public static void SetDepthTextureOnAllAssets(bool state)
        {
            for (int i = 0; i < GraphicsSettings.allConfiguredRenderPipelines.Length; i++)
            {
                if(GraphicsSettings.allConfiguredRenderPipelines[i].GetType() != typeof(UniversalRenderPipelineAsset)) continue;
                
                UniversalRenderPipelineAsset pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.allConfiguredRenderPipelines[i];

                #if UNITY_EDITOR
                if(pipeline.supportsCameraDepthTexture != state) EditorUtility.SetDirty(pipeline);
                #endif
                
                pipeline.supportsCameraDepthTexture = state;
            }
        }
        
        public static bool IsOpaqueTextureOptionDisabledAnywhere()
        {
            bool state = false;

            for (int i = 0; i < GraphicsSettings.allConfiguredRenderPipelines.Length; i++)
            {
                if(GraphicsSettings.allConfiguredRenderPipelines[i].GetType() != typeof(UniversalRenderPipelineAsset)) continue;
                
                UniversalRenderPipelineAsset pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.allConfiguredRenderPipelines[i];

                if (pipeline.supportsCameraOpaqueTexture == false) return true;
            }

            return state;
        }

        public static void SetOpaqueTextureOnAllAssets(bool state)
        {
            for (int i = 0; i < GraphicsSettings.allConfiguredRenderPipelines.Length; i++)
            {
                if(GraphicsSettings.allConfiguredRenderPipelines[i].GetType() != typeof(UniversalRenderPipelineAsset)) continue;
                
                UniversalRenderPipelineAsset pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.allConfiguredRenderPipelines[i];

                #if UNITY_EDITOR
                if(pipeline.supportsCameraOpaqueTexture != state) EditorUtility.SetDirty(pipeline);
                #endif
                
                pipeline.supportsCameraOpaqueTexture = state;
            }
        }
        
        public static bool TransparentShadowsEnabled()
        {
            if (!UniversalRenderPipeline.asset) return false;

            UniversalRendererData main = (UniversalRendererData)GetDefaultRenderer();

            return main ? main.shadowTransparentReceive : false;
        }

        public static bool IsDepthAfterTransparents()
        {
            #if UNITY_2022_2_OR_NEWER
            if (!UniversalRenderPipeline.asset) return false;
            
            UniversalRendererData main = (UniversalRendererData)GetDefaultRenderer();

            return main.copyDepthMode == CopyDepthMode.AfterTransparents;
            #else
            return true;
            #endif
        }
        
        public static bool VREnabled()
        {
            #if UNITY_2023_2_OR_NEWER
            return XRSRPSettings.enabled;
            #else
            return XRGraphics.enabled;
            #endif
        }
#endif
    }
}
