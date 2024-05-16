using UnityEngine;
using UnityEngine.Rendering;
#if URP
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace StylizedWater2
{
    public class SetupConstants : ScriptableRenderPass
    {
        private static readonly int _EnableDirectionalCaustics = Shader.PropertyToID("_EnableDirectionalCaustics");
        private static readonly int CausticsProjection = Shader.PropertyToID("CausticsProjection");
        private static readonly int _WaterSSREnabled = Shader.PropertyToID("_WaterSSREnabled");
        private static readonly int _WaterDisplacementPrePassAvailable = Shader.PropertyToID("_WaterDisplacementPrePassAvailable");

        private bool m_directionalCaustics;
        
        private static VisibleLight mainLight;
        private Matrix4x4 causticsProjection;

        public SetupConstants()
        {
            //Force a unit scale, otherwise affects the projection tiling of the caustics
            causticsProjection = Matrix4x4.Scale(Vector3.one);
        }

        private StylizedWaterRenderFeature settings;
        
        public void Setup(StylizedWaterRenderFeature renderFeature)
        {
            this.settings = renderFeature;
            m_directionalCaustics = settings.directionalCaustics;
        }

        #if UNITY_2020_2_OR_NEWER
        private ScriptableRenderPassInput requirements;
        #endif

        #if UNITY_6000_0_OR_NEWER //Silence warning spam
        public override void RecordRenderGraph(UnityEngine.Rendering.RenderGraphModule.RenderGraph renderGraph, ContextContainer frameData) { }
        #endif

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            #if UNITY_2020_2_OR_NEWER
            //Inform the render pipeline which pre-passes are required
            requirements = ScriptableRenderPassInput.None;
            
            //Only when using advanced shading, so don't forcibly enable
            //if(m_directionalCaustics) requirements = ScriptableRenderPassInput.Depth;
            
            if (settings.screenSpaceReflectionSettings.enable)
            {
                requirements |= ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth;
            }
            
            if(settings.displacementPrePassSettings.enable) cmd.EnableShaderKeyword(DisplacementPrePass.KEYWORD);
            else cmd.DisableShaderKeyword(DisplacementPrePass.KEYWORD);
            
            cmd.SetGlobalInt(_WaterSSREnabled, settings.screenSpaceReflectionSettings.enable ? 1 : 0);
            cmd.SetGlobalInt(_WaterDisplacementPrePassAvailable, settings.displacementPrePassSettings.enable ? 1 : 0);
            
            ConfigureInput(requirements);
            #endif
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            
            if (m_directionalCaustics)
            {
                //When no lights are visible, main light will be set to -1.
                if (renderingData.lightData.mainLightIndex > -1)
                {
                    mainLight = renderingData.lightData.visibleLights[renderingData.lightData.mainLightIndex];
        
                    if (mainLight.lightType == LightType.Directional)
                    {
                        causticsProjection = Matrix4x4.Rotate(mainLight.light.transform.rotation);
                        
                        cmd.SetGlobalMatrix(CausticsProjection, causticsProjection.inverse);
                    }
                    
                    #if UNITY_2021_2_OR_NEWER
                    //Sets up the required View- -> Clip-space matrices
                    NormalReconstruction.SetupProperties(cmd, renderingData.cameraData);
                    #endif
                }
                else
                {
                    m_directionalCaustics = false;
                }
            }
            
            cmd.SetGlobalInt(_EnableDirectionalCaustics, m_directionalCaustics ? 1 : 0);
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.SetGlobalInt(_EnableDirectionalCaustics, 0);
            cmd.SetGlobalInt(_WaterSSREnabled, 0);
        }

        public void Dispose()
        {
            Shader.SetGlobalInt(_WaterDisplacementPrePassAvailable, 0);
        }
    }
}
#endif