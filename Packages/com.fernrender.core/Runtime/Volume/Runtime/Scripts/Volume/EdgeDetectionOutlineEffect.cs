using UnityEngine.Rendering.FernRenderPipeline;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.FernRenderPipeline
{
    [System.Serializable, VolumeComponentMenu("FernRender/Edge Detection Outline")]
    public class EdgeDetectionOutlineEffect : VolumeComponent
    {
        [Tooltip("Controls the Effect Intensity")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0, 0, 1);
        
        [Tooltip("Controls the edge thickness.")]
        public ClampedFloatParameter thickness = new ClampedFloatParameter(1, 0, 8);
        
        [FormerlySerializedAs("normalThreshold")] [Tooltip("Controls the threshold of the normal difference in degrees.")]
        public ClampedFloatParameter angleThreshold = new ClampedFloatParameter(1, 1, 179.9f);

        [Tooltip("Controls the threshold of the depth difference in world units.")]
        public ClampedFloatParameter depthThreshold = new ClampedFloatParameter(0.01f, 0.001f, 1);

        [Tooltip("Controls the edge color.")]
        public ColorParameter color = new ColorParameter(Color.black, true, false, true);
        
        [Tooltip("Controls Sampler 4 Or 8 times")]
        public BoolParameter LowQuality = new BoolParameter(false);
    }

    [FernPostProcess("Edge Detection", FernPostProcessInjectionPoint.BeforePostProcess)]
    public class EdgeDetectionEffectRenderer : FernPostProcessRenderer
    {
        private EdgeDetectionOutlineEffect m_VolumeComponent;
        private RTHandle edgeDetectionRTHandle;
        private Material m_Material;
        private bool m_SupportsR8RenderTextureFormat =  SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8);

        static class ShaderIDs {
            internal readonly static int Input = Shader.PropertyToID("_BlitTexture");
            internal readonly static int Threshold = Shader.PropertyToID("_Edge_Threshold");
            internal readonly static int Color = Shader.PropertyToID("_Edge_Color");
            internal readonly static string LowQuality = "_LowQuality";
        }
        
        public override bool visibleInSceneView => true;
        
        public override ScriptableRenderPassInput input => ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;

        public override void Initialize()
        {
            m_Material = CoreUtils.CreateEngineMaterial("Hidden/FernNPR/PostProcess/EdgeDetectionOutline");
        }

        public override bool Setup(ref RenderingData renderingData, FernPostProcessInjectionPoint injectionPoint, Material uberMaterial = null)
        {
            var stack = VolumeManager.instance.stack;
            m_VolumeComponent = stack.GetComponent<EdgeDetectionOutlineEffect>();
            if (m_VolumeComponent.intensity.value <= 0)
            {
                Shader.DisableKeyword("_EDGEDETECION");
                return false;
            }
            
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 0;
            descriptor.colorFormat = m_SupportsR8RenderTextureFormat ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;
            
            RenderingUtils.ReAllocateIfNeeded(ref edgeDetectionRTHandle, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_EdgeDetectionTexture");

            return true;
        }

        public override void Render(CommandBuffer cmd, ScriptableRenderContext context, FernPostProcessRenderPass.PostProcessRTHandles rtHandles, ref RenderingData renderingData, FernPostProcessInjectionPoint injectionPoint)
        {
            if(m_Material == null) return;
            CoreUtils.SetKeyword(cmd, "_EDGEDETECION", true);
            float angleThreshold = m_VolumeComponent.angleThreshold.value;
            float depthThreshold = m_VolumeComponent.depthThreshold.value;
            Vector4 threshold = new Vector4(Mathf.Cos(angleThreshold * Mathf.Deg2Rad), m_VolumeComponent.thickness.value, depthThreshold, m_VolumeComponent.intensity.value);
            cmd.SetGlobalVector(ShaderIDs.Threshold, threshold);
            cmd.SetGlobalColor(ShaderIDs.Color, m_VolumeComponent.color.value);
            CoreUtils.SetKeyword(m_Material, ShaderIDs.LowQuality, m_VolumeComponent.LowQuality.value);
            Blitter.BlitCameraTexture(cmd, rtHandles.m_Source, edgeDetectionRTHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_Material, 0);
            cmd.SetGlobalTexture("_EdgeDetectionTexture", edgeDetectionRTHandle.nameID);
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            edgeDetectionRTHandle?.Release();
        }
    }
}