#if ENVIRO_URP
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace Enviro
{
    public class EnviroURPRenderPass : ScriptableRenderPass
    {      
        public ScriptableRenderer scriptableRenderer { get; set; }
        
        private Material blitThroughMat;
        private string passName;

        private List<EnviroVolumetricCloudRenderer> volumetricCloudsRender = new List<EnviroVolumetricCloudRenderer>();

        public EnviroURPRenderPass (string name)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            passName = name;
        } 
  
        public void CustomBlit(CommandBuffer cmd,Matrix4x4 matrix, RenderTargetIdentifier source, RenderTargetIdentifier target, Material mat, int pass)
        {
            
            cmd.SetGlobalTexture("_MainTex", source);
            cmd.SetRenderTarget(target, 0, CubemapFace.Unknown, -1);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, matrix, mat,0, pass);
            
            //Blit(cmd,source,target,mat,pass);
        }
 
        public void CustomBlit(CommandBuffer cmd,Matrix4x4 matrix, RenderTargetIdentifier source, RenderTargetIdentifier target, Material mat)
        {
            
            cmd.SetGlobalTexture("_MainTex", source);
            cmd.SetRenderTarget(target, 0, CubemapFace.Unknown, -1);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, matrix, mat,0);
            
            //Blit(cmd,source,target,mat);
        }

        public void CustomBlit(CommandBuffer cmd,Matrix4x4 matrix, RenderTargetIdentifier source, RenderTargetIdentifier target)
        {
            if(blitThroughMat == null)
               blitThroughMat = new Material(Shader.Find("Hidden/EnviroBlitThrough"));
                
            cmd.SetGlobalTexture("_MainTex", source);
            cmd.SetRenderTarget(target, 0, CubemapFace.Unknown, -1);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, matrix, blitThroughMat);
            
            //Blit(cmd,source,target);
        }
 
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            ConfigureTarget(scriptableRenderer.cameraColorTarget);
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if(GetCloudsRenderer(renderingData.cameraData.camera) == null)
            {
               CreateCloudsRenderer(renderingData.cameraData.camera);
            }
        }
 
        private EnviroVolumetricCloudRenderer CreateCloudsRenderer(Camera cam)
        {
            EnviroVolumetricCloudRenderer r = new EnviroVolumetricCloudRenderer();
            r.camera = cam;
            volumetricCloudsRender.Add(r);
            return r;
        }

        private EnviroVolumetricCloudRenderer GetCloudsRenderer(Camera cam)
        {
            for (int i = 0; i < volumetricCloudsRender.Count; i++)
            {
                if(volumetricCloudsRender[i].camera == cam)
                   return volumetricCloudsRender[i];
            }
            return CreateCloudsRenderer(cam);
        }

        private void SetMatrix(Camera myCam)
        {
            if (UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced) 
            {
                // Both stereo eye inverse view matrices
                Matrix4x4 left_world_from_view = myCam.GetStereoViewMatrix(Camera.StereoscopicEye.Left).inverse;
                Matrix4x4 right_world_from_view = myCam.GetStereoViewMatrix(Camera.StereoscopicEye.Right).inverse;

                // Both stereo eye inverse projection matrices, plumbed through GetGPUProjectionMatrix to compensate for render texture
                Matrix4x4 left_screen_from_view = myCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
                Matrix4x4 right_screen_from_view = myCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
                Matrix4x4 left_view_from_screen = GL.GetGPUProjectionMatrix(left_screen_from_view, true).inverse;
                Matrix4x4 right_view_from_screen = GL.GetGPUProjectionMatrix(right_screen_from_view, true).inverse;

                // Negate [1,1] to reflect Unity's CBuffer state
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3)
                {
                    left_view_from_screen[1, 1] *= -1;
                    right_view_from_screen[1, 1] *= -1;
                }

                Shader.SetGlobalMatrix("_LeftWorldFromView", left_world_from_view);
                Shader.SetGlobalMatrix("_RightWorldFromView", right_world_from_view);
                Shader.SetGlobalMatrix("_LeftViewFromScreen", left_view_from_screen);
                Shader.SetGlobalMatrix("_RightViewFromScreen", right_view_from_screen);
            }
            else
            {
                // Main eye inverse view matrix
                Matrix4x4 left_world_from_view = myCam.cameraToWorldMatrix;

                // Inverse projection matrices, plumbed through GetGPUProjectionMatrix to compensate for render texture
                Matrix4x4 screen_from_view = myCam.projectionMatrix;
                Matrix4x4 left_view_from_screen = GL.GetGPUProjectionMatrix(screen_from_view, true).inverse;

                // Negate [1,1] to reflect Unity's CBuffer state
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3)
                    left_view_from_screen[1, 1] *= -1;

                // Store matrices
                Shader.SetGlobalMatrix("_LeftWorldFromView", left_world_from_view);
                Shader.SetGlobalMatrix("_LeftViewFromScreen", left_view_from_screen);
            } 
        } 

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if(EnviroManager.instance == null)
               return; 
                
            CommandBuffer cmd = CommandBufferPool.Get(passName);
 
            //Set what to render on this camera.
            bool renderVolumetricClouds = false;
            bool renderFog = false;

            if(EnviroManager.instance.VolumetricClouds != null)
                renderVolumetricClouds = EnviroManager.instance.VolumetricClouds.settingsQuality.volumetricClouds;

            if(EnviroManager.instance.Fog != null)
                renderFog = EnviroManager.instance.Fog.Settings.fog;

            //Set some global matrixes used for all the enviro effects.
            SetMatrix(renderingData.cameraData.camera);
 
            //Create temporary texture and blit the camera content.
            RenderTexture sourceTemp = RenderTexture.GetTemporary(renderingData.cameraData.cameraTargetDescriptor);
            CustomBlit(cmd, Matrix4x4.identity,scriptableRenderer.cameraColorTarget, new RenderTargetIdentifier(sourceTemp)); 

            //Render volumetrics mask first
            if(EnviroManager.instance.Fog != null && renderFog)
               EnviroManager.instance.Fog.RenderVolumetricsURP(renderingData.cameraData.camera,this,cmd,sourceTemp);

            if(EnviroManager.instance.Fog != null && EnviroManager.instance.VolumetricClouds != null && renderVolumetricClouds && renderFog)
            { 
                RenderTexture temp1 = RenderTexture.GetTemporary(renderingData.cameraData.cameraTargetDescriptor);

                if(renderingData.cameraData.camera.transform.position.y < EnviroManager.instance.VolumetricClouds.settingsLayer1.bottomCloudsHeight)
                {
                    EnviroVolumetricCloudRenderer renderer = GetCloudsRenderer(renderingData.cameraData.camera);
                    EnviroManager.instance.VolumetricClouds.RenderVolumetricCloudsURP(renderingData,this,cmd, sourceTemp, temp1, renderer, null);

                    if(EnviroManager.instance.VolumetricClouds.settingsGlobal.cloudShadows && renderingData.cameraData.camera.cameraType != CameraType.Reflection)
                    {
                        RenderTexture temp2 = RenderTexture.GetTemporary(renderingData.cameraData.cameraTargetDescriptor);
                        EnviroManager.instance.VolumetricClouds.RenderCloudsShadowsURP(this,renderingData.cameraData.camera,cmd,temp1,temp2,renderer);
                        EnviroManager.instance.Fog.RenderHeightFogURP(renderingData.cameraData.camera,this,cmd,temp2,scriptableRenderer.cameraColorTarget);
                        RenderTexture.ReleaseTemporary(temp2);
                    }
                    else
                    {
                        EnviroManager.instance.Fog.RenderHeightFogURP(renderingData.cameraData.camera,this,cmd,temp1,scriptableRenderer.cameraColorTarget);
                    }
                }
                else
                { 
                    EnviroManager.instance.Fog.RenderHeightFogURP(renderingData.cameraData.camera,this,cmd,sourceTemp,temp1);
                    EnviroVolumetricCloudRenderer renderer = GetCloudsRenderer(renderingData.cameraData.camera);
                    EnviroManager.instance.VolumetricClouds.RenderVolumetricCloudsURP(renderingData,this,cmd, temp1, scriptableRenderer.cameraColorTarget, renderer, null);           
                }

                context.ExecuteCommandBuffer(cmd);
                RenderTexture.ReleaseTemporary(temp1);
            }
            else if(EnviroManager.instance.VolumetricClouds != null && renderVolumetricClouds && !renderFog)
            {
                EnviroVolumetricCloudRenderer renderer = GetCloudsRenderer(renderingData.cameraData.camera);
                  
                if(EnviroManager.instance.VolumetricClouds.settingsGlobal.cloudShadows && renderingData.cameraData.camera.cameraType != CameraType.Reflection)
                {
                    RenderTexture temp1 = RenderTexture.GetTemporary(renderingData.cameraData.cameraTargetDescriptor);
                    EnviroManager.instance.VolumetricClouds.RenderVolumetricCloudsURP(renderingData,this,cmd, sourceTemp, temp1, renderer, null);
                    EnviroManager.instance.VolumetricClouds.RenderCloudsShadowsURP(this,renderingData.cameraData.camera,cmd,temp1,scriptableRenderer.cameraColorTarget,renderer);
                    RenderTexture.ReleaseTemporary(temp1);
                }
                else
                {
                     EnviroManager.instance.VolumetricClouds.RenderVolumetricCloudsURP(renderingData,this,cmd, sourceTemp, scriptableRenderer.cameraColorTarget, renderer, null);
                }
                context.ExecuteCommandBuffer(cmd); 
                
            } 
            else if (Enviro.EnviroManager.instance.Fog != null && renderFog)
            {
                EnviroManager.instance.Fog.RenderHeightFogURP(renderingData.cameraData.camera,this,cmd,sourceTemp,scriptableRenderer.cameraColorTarget);
                context.ExecuteCommandBuffer(cmd);
            }
            else
            {
                //Render Nothing
            }

            //Release source temp render texture
            CommandBufferPool.Release(cmd);
            RenderTexture.ReleaseTemporary(sourceTemp);
        }
    }
}
#endif
