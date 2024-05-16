// Stylized Water 2
// Staggart Creations (http://staggart.xyz)
// Copyright protected under Unity Asset Store EULA
// Copying or referencing source code for the production of new asset store content is strictly prohibited.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if URP
using UnityEngine.Rendering.Universal;
#endif

namespace StylizedWater2
{
    [ExecuteInEditMode]
    [AddComponentMenu("Stylized Water 2/Planar Reflection Renderer")]
    [HelpURL("https://staggart.xyz/unity/stylized-water-2/sws-2-docs/?section=planar-reflections")]
    public class PlanarReflectionRenderer : MonoBehaviour
    {
#if URP
        public static List<PlanarReflectionRenderer> Instances = new List<PlanarReflectionRenderer>();
        public Dictionary<Camera, Camera> reflectionCameras = new Dictionary<Camera, Camera>();
        
        //Rendering
        [Tooltip("If enabled, the reflection plane will be based on this transform's up vector (green arrow).\n\nOtherwise the world's upwards direction is assumed")]
        public bool rotatable = false;
        [Tooltip("Set the layers that should be rendered into the reflection. The \"Water\" layer is always excluded")]
        public LayerMask cullingMask = -1;
        [Tooltip("The renderer used by the reflection camera. It's recommend to create a separate renderer, so any custom render features aren't executed for the reflection")]
        public int rendererIndex = -1;

        [Min(0f)]
        public float offset = 0.05f;
        [Tooltip("When disabled, the skybox reflection comes from a Reflection Probe. This has the benefit of being omni-directional rather than flat/planar. Enabled this to render the skybox into the planar reflection anyway." +
                 "\n\nNote that enabling this will override Screen Space Reflections completely!")]
        public bool includeSkybox;
        [Tooltip("Render Unity's default scene fog in the reflection. Note that this doesn't strictly work correctly on large triangles, as it is incompatible with oblique camera projections." +
                 "\n\n" +
                 "This does not include to post-processing fog effects!")]
        public bool enableFog;

        //Quality
        public bool renderShadows;
        [Tooltip("Objects beyond this range aren't rendered into the reflection. Note that this may causes popping for large/tall objects.")]
		public float renderRange = 500f;
        [Range(0.25f, 1f)] 
        [Tooltip("A multiplier for the rendering resolution, based on the current screen resolution. The render scale, as configured in the pipeline settings is multiplied over this.")]
		public float renderScale = 0.75f;

        [Range(0, 4)]
        [Tooltip("Do not render LOD objects lower than this value. Example: With a value of 1, LOD0 for LOD Groups will not be used")]
        public int maximumLODLevel = 0;
        
        [SerializeField]
        public List<WaterObject> waterObjects = new List<WaterObject>();
        [Tooltip("If enabled, the center of the rendering bounds (that wraps around the water objects) moves with the Transform position" +
                 "\n\nYou must however ensure you are only moving on the XZ axis")]
        public bool moveWithTransform;
        [HideInInspector]
        public Bounds bounds = new Bounds();

        private float m_renderScale = 1f;
        private float m_renderRange;

        /// <summary>
        /// Reflections will only render if this is true. Value can be set through the static SetQuality function
        /// </summary>
        public static bool AllowReflections { get; private set; } = true;

        private static readonly int _PlanarReflectionsEnabledID = Shader.PropertyToID("_PlanarReflectionsEnabled");
        private static readonly int _PlanarReflectionID = Shader.PropertyToID("_PlanarReflection");
		
        #if UNITY_2023_1_OR_NEWER
        private UniversalRenderPipeline.SingleCameraRequest requestData;
        #endif
        
        [NonSerialized]
        public bool isRendering;

        private Camera m_reflectionCamera;
		private static UniversalAdditionalCameraData m_cameraData;
        
        private void Reset()
        {
            this.gameObject.name = "Planar Reflection Renderer";
        }
        
        private void OnEnable()
        {
            InitializeValues();

            Instances.Add(this);
            EnableReflections();
        }

        private void OnDisable()
        {
            Instances.Remove(this);
            DisableReflections();
        }

        public void InitializeValues()
        {
            m_renderScale = renderScale;
            m_renderRange = renderRange;
        }

        /// <summary>
        /// Assigns all Water Objects in the WaterObject.Instances list and enables reflection for them
        /// </summary>
        public void ApplyToAllWaterInstances()
        {
            waterObjects = new List<WaterObject>(WaterObject.Instances);
            RecalculateBounds();
            EnableMaterialReflectionSampling();
        }

        /// <summary>
        /// Toggle reflections or set the render scale for all reflection renderers. This can be tied into performance scaling or graphics settings in menus
        /// </summary>
        /// <param name="enableReflections">Toggles rendering of reflections, and toggles it on all the assigned water objects</param>
        /// <param name="renderScale">A multiplier for the current screen resolution. Note that the render scale configured in URP is also taken into account</param>
        /// <param name="renderRange">Objects beyond this range aren't rendered into the reflection</param>
        public static void SetQuality(bool enableReflections, float renderScale = -1f, float renderRange = -1f, int maxLodLevel = -1)
        {
            AllowReflections = enableReflections;
            
            foreach (PlanarReflectionRenderer renderer in Instances)
            {
                if (renderScale > 0) renderer.renderScale = renderScale;
                if (renderRange > 0) renderer.renderRange = renderRange;
                if (maxLodLevel >= 0) renderer.maximumLODLevel = maxLodLevel;
                renderer.InitializeValues();

                if (enableReflections) renderer.EnableReflections();
                if (!enableReflections) renderer.DisableReflections();
            }
        }

        public void EnableReflections()
        {
            if (!AllowReflections || PipelineUtilities.VREnabled()) return;

            RenderPipelineManager.beginCameraRendering += OnWillRenderCamera;
            ToggleMaterialReflectionSampling(true);
        }

        public void DisableReflections()
        {
            RenderPipelineManager.beginCameraRendering -= OnWillRenderCamera;
            ToggleMaterialReflectionSampling(false);

            //Clear cameras
            foreach (var kvp in reflectionCameras)
            {
                if (kvp.Value == null) continue;

                if (kvp.Value)
                {
                    RenderTexture.ReleaseTemporary(kvp.Value.targetTexture);
                    
                    DestroyImmediate(kvp.Value.gameObject);
                }
            }

            reflectionCameras.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = bounds.size.y > 0.01f ? Color.yellow : Color.white;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        public Bounds CalculateBounds()
        {
            Bounds m_bounds = new Bounds(Vector3.zero, Vector3.zero);
            
            if (waterObjects == null) return m_bounds;
            if (waterObjects.Count == 0) return m_bounds;

            Vector3 minSum = Vector3.one * Mathf.Infinity;
            Vector3 maxSum = Vector3.one * Mathf.NegativeInfinity;
            
            for (int i = 0; i < waterObjects.Count; i++)
            {
                if (!waterObjects[i]) continue;
                    
                minSum = Vector3.Min(waterObjects[i].meshRenderer.bounds.min, minSum);
                maxSum = Vector3.Max(waterObjects[i].meshRenderer.bounds.max, maxSum);
            }
            
            m_bounds.SetMinMax(minSum, maxSum);

            //Flatten to center
            m_bounds.size = new Vector3(m_bounds.size.x, 0f, m_bounds.size.z);

            return m_bounds;
        }

        public void RecalculateBounds()
        {
            bounds = CalculateBounds();
        }

        public static bool InvalidContext(Camera camera)
        {
            #if UNITY_EDITOR
            //Avoid the "Screen position outside of frustrum" error
            if (camera.orthographic && Vector3.Dot(Vector3.up, camera.transform.up) > 0.9999f) return true;
            
            #if UNITY_2021_2_OR_NEWER
            //Causes an internal error in URP's rendering code in the CopyColorPass
            if (camera.cameraType == CameraType.SceneView && UnityEditor.SceneView.lastActiveSceneView && UnityEditor.SceneView.lastActiveSceneView.isUsingSceneFiltering) return true;
            #endif
            #endif
            
            //Skip for any special use camera's (except scene view camera)
            //Note: Scene camera still rendering even if window not focused!
            return (camera.cameraType != CameraType.SceneView && (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview || camera.hideFlags != HideFlags.None));
        }

        private void OnWillRenderCamera(ScriptableRenderContext context, Camera camera)
        {
            if (InvalidContext(camera))
            {
                isRendering = false;
                return;
            }

            isRendering = WaterObjectsVisible(camera);
            
            if (isRendering == false) return;
            
            
            if (moveWithTransform) bounds.center = this.transform.position;

            m_cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (m_cameraData && m_cameraData.renderType == CameraRenderType.Overlay) return;

            reflectionCameras.TryGetValue(camera, out m_reflectionCamera);
            if (m_reflectionCamera == null) CreateReflectionCamera(camera);
            
            //It's possible it is destroyed at this point when disabling reflections
            if (!m_reflectionCamera) return;
            
            UnityEngine.Profiling.Profiler.BeginSample("Planar Water Reflections", camera);

            //Render scale changed
            if (Math.Abs(renderScale - m_renderScale) > 0.001f)
            {
                RenderTexture.ReleaseTemporary(m_reflectionCamera.targetTexture);
                CreateRenderTexture(m_reflectionCamera, camera);

                m_renderScale = renderScale;
            }

            UpdateWaterProperties(m_reflectionCamera);
            
            UpdateCameraProperties(camera, m_reflectionCamera);
            UpdatePerspective(camera, m_reflectionCamera);
            
            bool fogEnabled = RenderSettings.fog && !enableFog;
            //Fog is based on clip-space z-distance and doesn't work with oblique projections
            if (fogEnabled) SetFogState(false);
            int maxLODLevel = QualitySettings.maximumLODLevel;
            QualitySettings.maximumLODLevel = maximumLODLevel;
            GL.invertCulling = true;

            RenderReflection(context, m_reflectionCamera);

            if (fogEnabled) SetFogState(true);
            QualitySettings.maximumLODLevel = maxLODLevel;
            GL.invertCulling = false;

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private void RenderReflection(ScriptableRenderContext context, Camera target)
        {
            /* Uncomment to render NR's vegetation in the reflection
            // Register the reflection camera for Nature Renderer
            var cameraId = VisualDesignCafe.Rendering.Instancing.RendererPool.RegisterCamera(target);

            // Render the instanced objects (details and trees)
            VisualDesignCafe.Rendering.Instancing.RendererPool.GetCamera(cameraId).Render();
            */
            
#pragma warning disable 0618
#if UNITY_2023_1_OR_NEWER
            /*
            requestData = new UniversalRenderPipeline.SingleCameraRequest();
            requestData.destination = target.targetTexture;
            requestData.slice = -1;

            //Throws the 'Recursive rendering is not supported in SRP (are you calling Camera.Render from within a render pipeline?).' error.
            if (RenderPipeline.SupportsRenderRequest(target, requestData))
            {
                RenderPipeline.SubmitRenderRequest(target, requestData);
            }
            */
            
            //Instead, Unity will whine about using an obsolete API.
            UniversalRenderPipeline.RenderSingleCamera(context, target);
            
            //So then what?
#else
            UniversalRenderPipeline.RenderSingleCamera(context, target);
#endif
#pragma warning restore 0618
        }

        private void SetFogState(bool value)
        {
            #if UNITY_EDITOR
            UnityEditor.Unsupported.SetRenderSettingsUseFogNoDirty(value);
            #else
            RenderSettings.fog = value;
            #endif
        }

        private float GetRenderScale()
        {
            return Mathf.Clamp(renderScale * UniversalRenderPipeline.asset.renderScale, 0.25f, 1f);
        }

        /// <summary>
        /// Should the renderer index be changed at runtime, this function must be called to update any reflection cameras
        /// </summary>
        /// <param name="index"></param>
        public void SetRendererIndex(int index)
        {
            index = PipelineUtilities.ValidateRenderer(index);

            foreach (var kvp in reflectionCameras)
            {
                if (kvp.Value == null) continue;
                
                m_cameraData = kvp.Value.GetComponent<UniversalAdditionalCameraData>();
                m_cameraData.SetRenderer(index);
            }
        }

        public void ToggleShadows(bool state)
        {
            foreach (var kvp in reflectionCameras)
            {
                if (kvp.Value == null) continue;
                
                m_cameraData = kvp.Value.GetComponent<UniversalAdditionalCameraData>();
                m_cameraData.renderShadows = state;
            }
        }
        
        /// <summary>
        /// Add the WaterObject, and recalculates the rendering bounds.
        /// </summary>
        /// <param name="waterObject"></param>
        public void AddWaterObject(WaterObject waterObject)
        {
            ToggleMaterialReflectionSampling(waterObject, true);
            waterObjects.Add(waterObject);

            RecalculateBounds();
        }
        
        /// <summary>
        /// Remove the WaterObject, and recalculates the rendering bounds.
        /// </summary>
        /// <param name="waterObject"></param>
        public void RemoveWaterObject(WaterObject waterObject)
        {
            ToggleMaterialReflectionSampling(waterObject, false);
            waterObjects.Remove(waterObject);
            
            RecalculateBounds();
        }
        
        /// <summary>
        /// Enables planar reflections on the MeshRenderers of the assigned water objects
        /// </summary>
        public void EnableMaterialReflectionSampling()
        {
            ToggleMaterialReflectionSampling(AllowReflections);
        }
        
        /// <summary>
        /// Toggles the sampling of the planar reflections texture in the water shader.
        /// </summary>
        /// <param name="state"></param>
        public void ToggleMaterialReflectionSampling(bool state)
        {
            if (waterObjects == null) return;

            for (int i = 0; i < waterObjects.Count; i++)
            {
                if (waterObjects[i] == null) continue;
                
                ToggleMaterialReflectionSampling(waterObjects[i], state);
            }
        }

        private void ToggleMaterialReflectionSampling(WaterObject waterObject, bool state)
        {
            waterObject.props.SetFloat(_PlanarReflectionsEnabledID, state ? 1f : 0f);
            waterObject.ApplyInstancedProperties();
        }

        private void CreateReflectionCamera(Camera source)
        {
            //Object creation
            GameObject go = new GameObject($"{source.name} Planar Reflection");
            go.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
            
            Camera newCamera = go.AddComponent<Camera>();
            newCamera.hideFlags = HideFlags.DontSave;
            //For the scene-view camera this also copies unwanted properties. Such as the camera type and background color!
            newCamera.CopyFrom(source);
            
            //Always exclude water layer
            newCamera.cullingMask = ~(1 << 4) & cullingMask;
            //Must always be set to Game, otherwise shadows render anyway
            newCamera.cameraType = CameraType.Game;
            newCamera.depth = source.depth-1f;
            newCamera.rect = new Rect(0,0,1,1);
            newCamera.enabled = false;
            newCamera.clearFlags = includeSkybox ? CameraClearFlags.Skybox : CameraClearFlags.Depth;
            //Required to maintain the alpha channel for the scene view
            newCamera.backgroundColor = Color.clear;
            newCamera.useOcclusionCulling = false;

            //Component required for the UniversalRenderPipeline.RenderSingleCamera call
            UniversalAdditionalCameraData data = newCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.requiresDepthTexture = false;
            data.requiresColorTexture = false;
            data.renderShadows = renderShadows;
            rendererIndex = PipelineUtilities.ValidateRenderer(rendererIndex);
            data.SetRenderer(rendererIndex);

            CreateRenderTexture(newCamera, source);
            
            reflectionCameras[source] = newCamera;
        }

        private void CreateRenderTexture(Camera targetCamera, Camera source)
        {
            //Note: Do not use RenderTextureFormat.Default or HDR, as these may be without an alpha channel on some platforms
            RenderTextureFormat colorFormat = UniversalRenderPipeline.asset.supportsHDR && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;

            float scale = GetRenderScale();

            RenderTextureDescriptor rtDsc = new RenderTextureDescriptor(
                (int)((float)source.scaledPixelWidth * scale),
                (int)((float)source.scaledPixelHeight * scale), 
                colorFormat);

            rtDsc.depthBufferBits = 16;
            //rtDsc.msaaSamples = UniversalRenderPipeline.asset.msaaSampleCount; //Waste of resources, water distortion makes it virtually unnoticeable.
            
            targetCamera.targetTexture = RenderTexture.GetTemporary(rtDsc);
            targetCamera.targetTexture.filterMode = scale < 1f ? FilterMode.Bilinear : FilterMode.Point;
            targetCamera.targetTexture.name = $"{source.name}_Reflection {rtDsc.width}x{rtDsc.height}";
        }
        
        private static readonly Plane[] frustrumPlanes = new Plane[6];
        
        public bool WaterObjectsVisible(Camera targetCamera)
        {
            GeometryUtility.CalculateFrustumPlanes(targetCamera.projectionMatrix * targetCamera.worldToCameraMatrix, frustrumPlanes);

            return GeometryUtility.TestPlanesAABB(frustrumPlanes, bounds);
        }

        //Assigns the render target of the current reflection camera
        private void UpdateWaterProperties(Camera cam)
        {
            for (int i = 0; i < waterObjects.Count; i++)
            {
                if (waterObjects[i] == null) continue;
                
                waterObjects[i].props.SetTexture(_PlanarReflectionID, cam.targetTexture);
                waterObjects[i].ApplyInstancedProperties();
            }
        }

        private static Vector4 reflectionPlane;
        private static Matrix4x4 reflectionBase;
        private static Vector3 oldCamPos;

        private static Matrix4x4 worldToCamera;
        private static Matrix4x4 viewMatrix;
        private static Matrix4x4 projectionMatrix;
        private static Vector4 clipPlane;
        private static readonly float[] layerCullDistances = new float[32];

        private void UpdateCameraProperties(Camera source, Camera reflectionCam)
        {
            reflectionCam.fieldOfView = source.fieldOfView;
            reflectionCam.orthographic = source.orthographic;
            reflectionCam.orthographicSize = source.orthographicSize;
            reflectionCam.useOcclusionCulling = source.useOcclusionCulling;
        }

        private void UpdatePerspective(Camera source, Camera reflectionCam)
        {
            if (!source || !reflectionCam) return;

            Vector3 normal = rotatable ? this.transform.up : Vector3.up;
            
            Vector3 position = bounds.center + (normal * offset);

            var d = -Vector3.Dot(normal, position);
            reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

            reflectionBase = Matrix4x4.identity;
            reflectionBase *= Matrix4x4.Scale(new Vector3(1, -1, 1));

            // View
            CalculateReflectionMatrix(ref reflectionBase, reflectionPlane);
            oldCamPos = source.transform.position - new Vector3(0, position.y * 2, 0);
            reflectionCam.transform.forward = Vector3.Scale(source.transform.forward, new Vector3(1, -1, 1));

            worldToCamera = source.worldToCameraMatrix;
            viewMatrix = worldToCamera * reflectionBase;

            //Reflect position
            oldCamPos.y = -oldCamPos.y;
            reflectionCam.transform.position = oldCamPos;

            clipPlane = CameraSpacePlane(reflectionCam.worldToCameraMatrix, position - normal * 0.1f, normal, 1.0f);
            projectionMatrix = source.CalculateObliqueMatrix(clipPlane);
            
            //Settings
            reflectionCam.cullingMask = ~(1 << 4) & cullingMask;;
            m_reflectionCamera.clearFlags = includeSkybox ? CameraClearFlags.Skybox : CameraClearFlags.Depth;
            
            #if !UNITY_2023_3_OR_NEWER
            //Only re-apply on value change
            if (m_renderRange != renderRange)
            {
                m_renderRange = renderRange;
                
                for (int i = 0; i < layerCullDistances.Length; i++)
                {
                    layerCullDistances[i] = renderRange;
                }
            }
            reflectionCam.layerCullDistances = layerCullDistances;
            reflectionCam.layerCullSpherical = true;
            #endif

            reflectionCam.projectionMatrix = projectionMatrix;
            reflectionCam.worldToCameraMatrix = viewMatrix;
        }

        // Calculates reflection matrix around the given plane
        private void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
        {
            reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
            reflectionMat.m01 = (-2F * plane[0] * plane[1]);
            reflectionMat.m02 = (-2F * plane[0] * plane[2]);
            reflectionMat.m03 = (-2F * plane[3] * plane[0]);

            reflectionMat.m10 = (-2F * plane[1] * plane[0]);
            reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
            reflectionMat.m12 = (-2F * plane[1] * plane[2]);
            reflectionMat.m13 = (-2F * plane[3] * plane[1]);

            reflectionMat.m20 = (-2F * plane[2] * plane[0]);
            reflectionMat.m21 = (-2F * plane[2] * plane[1]);
            reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
            reflectionMat.m23 = (-2F * plane[3] * plane[2]);

            reflectionMat.m30 = 0F;
            reflectionMat.m31 = 0F;
            reflectionMat.m32 = 0F;
            reflectionMat.m33 = 1F;
        }
        
        // Given position/normal of the plane, calculates plane in camera space.
        private Vector4 CameraSpacePlane(Matrix4x4 worldToCameraMatrix, Vector3 pos, Vector3 normal, float sideSign)
        {
            var offsetPos = pos + normal * offset;
            var cameraPosition = worldToCameraMatrix.MultiplyPoint(offsetPos);
            var cameraNormal = worldToCameraMatrix.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cameraNormal.x, cameraNormal.y, cameraNormal.z,
                -Vector3.Dot(cameraPosition, cameraNormal));
        }

        public RenderTexture TryGetReflectionTexture(Camera targetCamera)
        {
            if (targetCamera)
            {
                reflectionCameras.TryGetValue(targetCamera, out m_reflectionCamera);
                if (m_reflectionCamera)
                {
                    return m_reflectionCamera.targetTexture;
                }
            }

            return null;
        }
#endif
    }
}
