using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.FernRenderPipeline
{
    [Serializable]
    public class FernPostProcess : ScriptableRendererFeature
    {
        private FernPostProcessRenderPass m_BeforeOpaquePass, m_AfterOpaqueAndSkyPass, m_BeforePostProcessPass, m_AfterPostProcessPass;

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.postProcessEnabled)
            {
                if (m_BeforeOpaquePass.HasPostProcessRenderers &&
                    m_BeforeOpaquePass.PrepareRenderers(ref renderingData))
                {
                    m_BeforeOpaquePass.Setup(renderer);
                    renderer.EnqueuePass(m_BeforeOpaquePass);
                }
                if (m_AfterOpaqueAndSkyPass.HasPostProcessRenderers &&
                    m_AfterOpaqueAndSkyPass.PrepareRenderers(ref renderingData))
                {
                    m_AfterOpaqueAndSkyPass.Setup(renderer);
                    renderer.EnqueuePass(m_AfterOpaqueAndSkyPass);
                }
                if (m_BeforePostProcessPass.HasPostProcessRenderers &&
                    m_BeforePostProcessPass.PrepareRenderers(ref renderingData))
                {
                    m_BeforePostProcessPass.Setup(renderer);
                    renderer.EnqueuePass(m_BeforePostProcessPass);
                }
            }
        }

        public override void Create()
        {
            List<FernPostProcessRenderer> beforeOpaqueRenderers = new List<FernPostProcessRenderer>();
            List<FernPostProcessRenderer> afterOpaqueAndSkyRenderers = new List<FernPostProcessRenderer>();
            List<FernPostProcessRenderer> beforePostProcessRenderers = new List<FernPostProcessRenderer>();
            List<FernPostProcessRenderer> afterPostProcessRenderers = new List<FernPostProcessRenderer>();
            
            // Sorry, there are some that I can't open source yet
            //beforeOpaqueRenderers.Add(new TrickAreaLightsRender());
            //beforeOpaqueRenderers.Add(new PlanarReflectionRender());
            beforeOpaqueRenderers.Add(new DepthOffsetRender());
            //beforeOpaqueRenderers.Add(new SimpleSSAORenderer());
            beforePostProcessRenderers.Add(new EdgeDetectionEffectRenderer());
            //var hbaoRender = new HBAORenderer();
            //beforeOpaqueRenderers.Add(hbaoRender);
            //beforePostProcessRenderers.Add(hbaoRender);
            //beforePostProcessRenderers.Add(new DualKawaseBlurRender());
            
            m_BeforeOpaquePass = new FernPostProcessRenderPass(FernPostProcessInjectionPoint.BeforeOpaque, beforeOpaqueRenderers);
            m_AfterOpaqueAndSkyPass = new FernPostProcessRenderPass(FernPostProcessInjectionPoint.AfterOpaqueAndSky, afterOpaqueAndSkyRenderers);
            m_BeforePostProcessPass = new FernPostProcessRenderPass(FernPostProcessInjectionPoint.BeforePostProcess, beforePostProcessRenderers);
            m_AfterPostProcessPass = new FernPostProcessRenderPass(FernPostProcessInjectionPoint.AfterPostProcess, afterPostProcessRenderers);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            m_BeforeOpaquePass.Dispose();
            m_AfterOpaqueAndSkyPass.Dispose();
            m_BeforePostProcessPass.Dispose();
            m_AfterPostProcessPass.Dispose();
        }
    }

    /// <summary>
    /// A render pass for executing custom post processing renderers.
    /// </summary>
    public class FernPostProcessRenderPass : ScriptableRenderPass
    {
        private List<ProfilingSampler> m_ProfilingSamplers;
        private string m_PassName;
        private FernPostProcessInjectionPoint injectionPoint;
        private List<FernPostProcessRenderer> m_PostProcessRenderers;
        private List<int> m_ActivePostProcessRenderers;
        private Material uber_Material;
        RenderTextureDescriptor m_Descriptor;

        public class PostProcessRTHandles
        {
            public RTHandle m_Source;
            public RTHandle m_Dest;
            public RTHandle m_cameraDepth;
        }

        public PostProcessRTHandles m_rtHandles = new PostProcessRTHandles();

        /// <summary>
        /// Gets whether this render pass has any post process renderers to execute
        /// </summary>
        public bool HasPostProcessRenderers => m_PostProcessRenderers.Count != 0;

        private ScriptableRenderer m_Render = null;

        /// <summary>
        /// Construct the render pass
        /// </summary>
        /// <param name="injectionPoint">The post processing injection point</param>
        /// <param name="classes">The list of classes for the renderers to be executed by this render pass</param>
        public FernPostProcessRenderPass(FernPostProcessInjectionPoint injectionPoint, List<FernPostProcessRenderer> renderers)
        {
            this.injectionPoint = injectionPoint;
            this.m_ProfilingSamplers = new List<ProfilingSampler>(renderers.Count);
            this.m_PostProcessRenderers = renderers;
            foreach (var renderer in renderers)
            {
                // Get renderer name and add it to the names list
                var attribute = FernPostProcessAttribute.GetAttribute(renderer.GetType());
                m_ProfilingSamplers.Add(new ProfilingSampler(attribute?.Name));
            }

            // Pre-allocate a list for active renderers
            this.m_ActivePostProcessRenderers = new List<int>(renderers.Count);
            // Set render pass event and name based on the injection point.
            switch (injectionPoint)
            {
                case FernPostProcessInjectionPoint.BeforeOpaque:
                    renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
                    m_PassName = "Fern Volume before Opaque";
                    break;
                case FernPostProcessInjectionPoint.AfterOpaqueAndSky:
                    renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
                    m_PassName = "Fern Volume after Opaque & Sky";
                    break;
                case FernPostProcessInjectionPoint.BeforePostProcess:
                    renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing; // TODO： Should After Motion Vector
                    m_PassName = "Fern Volume before PostProcess";
                    break;
                case FernPostProcessInjectionPoint.AfterPostProcess:
                    renderPassEvent = RenderPassEvent.AfterRendering;
                    m_PassName = "Fern Volume after PostProcess";
                    break;
            }
        }

        /// <summary>
        /// Setup Data
        /// </summary>
        public void Setup(ScriptableRenderer renderer)
        {
            m_Render = renderer;
            
        }

        /// <summary>
        /// cameraColorTargetHandle can only be obtained in SRP render
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="renderingData"></param>
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);

            m_Descriptor = renderingData.cameraData.cameraTargetDescriptor;
            m_Descriptor.useMipMap = false;
            m_Descriptor.autoGenerateMips = false;

            m_rtHandles.m_Source = m_Render.cameraColorTargetHandle;
            m_rtHandles.m_cameraDepth = m_Render.cameraDepthTargetHandle;
        }

        /// <summary>
        /// Prepares the renderer for executing on this frame and checks if any of them actually requires rendering
        /// </summary>
        /// <param name="renderingData">Current rendering data</param>
        /// <returns>True if any renderer will be executed for the given camera. False Otherwise.</returns>
        public bool PrepareRenderers(ref RenderingData renderingData)
        {
            // See if current camera is a scene view camera to skip renderers with "visibleInSceneView" = false.
            bool isSceneView = renderingData.cameraData.cameraType == CameraType.SceneView;

            // Here, we will collect the inputs needed by all the custom post processing effects
            ScriptableRenderPassInput passInput = ScriptableRenderPassInput.None;
            
            if (uber_Material == null)
            {
                uber_Material = CoreUtils.CreateEngineMaterial("Hidden/FernRender/PostProcess/FernVolumeUber");
            }

            // Collect the active renderers
            m_ActivePostProcessRenderers.Clear();
            for (int index = 0; index < m_PostProcessRenderers.Count; index++)
            {
                var ppRenderer = m_PostProcessRenderers[index];
                // Skips current renderer if "visibleInSceneView" = false and the current camera is a scene view camera. 
                if (isSceneView && !ppRenderer.visibleInSceneView) continue;
                // Setup the camera for the renderer and if it will render anything, add to active renderers and get its required inputs
                if (ppRenderer.Setup(ref renderingData, injectionPoint, uber_Material))
                {
                    m_ActivePostProcessRenderers.Add(index);
                    passInput |= ppRenderer.input;
                }
            }

            // Configure the pass to tell the renderer what inputs we need
            ConfigureInput(passInput);

            // return if no renderers are active
            return m_ActivePostProcessRenderers.Count != 0;
        }

        /// <summary>
        /// Execute the custom post processing renderers
        /// </summary>
        /// <param name="context">The scriptable render context</param>
        /// <param name="renderingData">Current rendering data</param>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;

            CommandBuffer cmd = CommandBufferPool.Get(m_PassName);
            
            PostProcessUtils.SetSourceSize(cmd, cameraData.cameraTargetDescriptor);
            
            for (int index = 0; index < m_ActivePostProcessRenderers.Count; ++index)
            {
                var rendererIndex = m_ActivePostProcessRenderers[index];
                var fernPostProcessRenderer = m_PostProcessRenderers[rendererIndex];
                if (!fernPostProcessRenderer.Initialized)
                    fernPostProcessRenderer.InitializeInternal();
                using (new ProfilingScope(cmd, m_ProfilingSamplers[rendererIndex]))
                {
                    Render(cmd, context, fernPostProcessRenderer, ref renderingData);
                }
            }

            if (injectionPoint == FernPostProcessInjectionPoint.BeforePostProcess)
            {
                
                m_rtHandles.m_Dest = m_Render.GetCameraColorFrontBuffer(cmd);
                m_Render.SwapColorBuffer(cmd);
                Blitter.BlitCameraTexture(cmd, m_rtHandles.m_Source, m_rtHandles.m_Dest, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, uber_Material, 0);
            }
            
            // Send command buffer for execution, then release it.
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void Render(CommandBuffer cmd, ScriptableRenderContext context, FernPostProcessRenderer fernPostRenderer, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            bool useTemporalAA = cameraData.IsTemporalAAEnabled();
            if (cameraData.antialiasing == AntialiasingMode.TemporalAntiAliasing && !useTemporalAA)
                TemporalAA.ValidateAndWarn(ref cameraData);

            fernPostRenderer.Render(cmd, context, m_rtHandles, ref renderingData, injectionPoint);
        }

        public void Dispose()
        {
            for (int index = 0; index < m_ActivePostProcessRenderers.Count; ++index)
            {
                var rendererIndex = m_ActivePostProcessRenderers[index];
                var fernPostProcessRenderer = m_PostProcessRenderers[rendererIndex];
                fernPostProcessRenderer.Dispose();
            }
            m_rtHandles.m_Source?.Release();
            m_rtHandles.m_Dest?.Release();
            m_rtHandles.m_cameraDepth?.Release();
        }

        public RenderTextureDescriptor GetCompatibleDescriptor()
            => GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, m_Descriptor.graphicsFormat);

        public RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, GraphicsFormat format,
            DepthBits depthBufferBits = DepthBits.None)
            => GetCompatibleDescriptor(m_Descriptor, width, height, format, depthBufferBits);

        internal static RenderTextureDescriptor GetCompatibleDescriptor(RenderTextureDescriptor desc, int width,
            int height, GraphicsFormat format, DepthBits depthBufferBits = DepthBits.None)
        {
            desc.depthBufferBits = (int)depthBufferBits;
            desc.msaaSamples = 1;
            desc.width = width;
            desc.height = height;
            desc.graphicsFormat = format;
            return desc;
        }
    }
}