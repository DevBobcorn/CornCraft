using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.FernRenderPipeline
{
    [System.Serializable, VolumeComponentMenu("FernRender/Depth Offset")]
    public class DepthOffsetVolume : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter isEnable = new BoolParameter(false);
        public ClampedIntParameter downSample = new ClampedIntParameter(0, 1, 4);

        public bool IsActive() => isEnable.value;
        public bool IsTileCompatible() => true;
    }

    [FernPostProcess("Depth Offset", FernPostProcessInjectionPoint.BeforeOpaque)]
    public class DepthOffsetRender : FernPostProcessRenderer
    {
        private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("DepthShadowPrepass");

        private static readonly ShaderTagId k_ShaderTagId = new ShaderTagId("DepthShadowOnly");
        private ShaderTagId shaderTagId { get; set; } = k_ShaderTagId;

        private DepthOffsetVolume m_VolumeComponent;
        FilteringSettings m_FilteringSettings;

        public override ScriptableRenderPassInput input => ScriptableRenderPassInput.Depth;

        public override bool visibleInSceneView => true;
        
        RTHandle depthShadowRTHandle;
        

        public override bool Setup(ref RenderingData renderingData, FernPostProcessInjectionPoint injectionPoint, Material uberMaterial = null)
        {
            var stack = VolumeManager.instance.stack;
            m_VolumeComponent = stack.GetComponent<DepthOffsetVolume>();
            if (!m_VolumeComponent.IsActive())
            {
                return false;
            }            
            
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque);

            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthStencilFormat = descriptor.depthStencilFormat;
            descriptor.depthBufferBits = 16;
            descriptor.msaaSamples = 1;
            descriptor.width >>= m_VolumeComponent.downSample.value;
            descriptor.height >>= m_VolumeComponent.downSample.value;
            
            RenderingUtils.ReAllocateIfNeeded(ref depthShadowRTHandle, descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_CameraDepthShadowTexture");
            
            return true;
        }

        public override void Render(CommandBuffer cmd, ScriptableRenderContext context, FernPostProcessRenderPass.PostProcessRTHandles rtHandles, 
            ref RenderingData renderingData, FernPostProcessInjectionPoint injectionPoint)
        {
            
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                CoreUtils.SetRenderTarget(cmd, depthShadowRTHandle);
                CoreUtils.ClearRenderTarget(cmd, ClearFlag.All, Color.clear);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            
                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = RenderingUtils.CreateDrawingSettings(shaderTagId, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None; 
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings);
                cmd.SetGlobalTexture(depthShadowRTHandle.name, depthShadowRTHandle.nameID);
            }
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            depthShadowRTHandle?.Release();
        }
    }
}

