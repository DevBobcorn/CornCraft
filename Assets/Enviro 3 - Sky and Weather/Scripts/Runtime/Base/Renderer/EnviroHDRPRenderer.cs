#if ENVIRO_HDRP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;
using System.Collections.Generic;

namespace Enviro
{
    [Serializable, VolumeComponentMenu("Post-processing/Enviro/Effects Renderer")]
    public class EnviroHDRPRenderer : CustomPostProcessVolumeComponent, IPostProcessComponent
    {
        public bool IsActive() => EnviroManager.instance != null;
        public override CustomPostProcessInjectionPoint injectionPoint => (CustomPostProcessInjectionPoint)0;
        private Material blitTrough;
        private List<EnviroVolumetricCloudRenderer> volumetricCloudsRender = new List<EnviroVolumetricCloudRenderer>();

        public override void Setup()
        {
            if (blitTrough == null)
                blitTrough = new Material(Shader.Find("Hidden/Enviro/BlitTroughHDRP"));
        }

        public override void Cleanup()
        {
            if (blitTrough != null)
                CoreUtils.Destroy(blitTrough);
        }

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {  
            //Do nothing
            if (!EnviroHelper.CanRenderOnCamera(camera.camera) || camera.camera.cameraType == CameraType.Preview)
            {
                blitTrough.SetTexture("_InputTexture", source);
                CoreUtils.DrawFullScreen(cmd, blitTrough);
                return;
            }     

            //Set what to render on this camera.
            bool renderVolumetricClouds = false;
            bool renderFog = false;

            if(EnviroManager.instance.VolumetricClouds != null)
                renderVolumetricClouds = EnviroManager.instance.VolumetricClouds.settingsQuality.volumetricClouds;

            if(EnviroManager.instance.Fog != null)
                renderFog = EnviroManager.instance.Fog.Settings.fog;

            if (renderVolumetricClouds)
            {
                //Create us a volumetric clouds renderer if null.
                if(GetCloudsRenderer(camera.camera) == null)
                {
                   CreateCloudsRenderer(camera.camera);
                }
            }
            //Set some global matrixes used for all the enviro effects.
            SetMatrix(camera.camera);

            //Clouds
            if(EnviroManager.instance.Fog != null && EnviroManager.instance.VolumetricClouds != null && renderVolumetricClouds && renderFog)
            { 
                RenderTexture temp1 = RenderTexture.GetTemporary(source.rt.descriptor);
                RTHandle temp1Handle = RTHandles.Alloc(temp1);
 
                if(camera.camera.transform.position.y < EnviroManager.instance.VolumetricClouds.settingsLayer1.bottomCloudsHeight)
                {
                    EnviroVolumetricCloudRenderer renderer = GetCloudsRenderer(camera.camera);
                    EnviroManager.instance.VolumetricClouds.RenderVolumetricCloudsHDRP(camera.camera,cmd, source, temp1Handle, renderer, null);

                    if(EnviroManager.instance.VolumetricClouds.settingsGlobal.cloudShadows && camera.camera.cameraType != CameraType.Reflection)
                    {
                        RenderTexture temp2 = RenderTexture.GetTemporary(source.rt.descriptor);
                        RTHandle temp2Handle = RTHandles.Alloc(temp2);
                        EnviroManager.instance.VolumetricClouds.RenderCloudsShadowsHDRP(camera.camera,cmd,temp1Handle,temp2Handle,renderer);
                        EnviroManager.instance.Fog.RenderHeightFogHDRP(camera.camera,cmd,temp2Handle,destination);                
                        RenderTexture.ReleaseTemporary(temp2);     
                        //temp2.Release();
                    }
                    else
                    {
                        EnviroManager.instance.Fog.RenderHeightFogHDRP(camera.camera,cmd,temp1Handle,destination);
                    }
                } 
                else
                { 
                    EnviroManager.instance.Fog.RenderHeightFogHDRP(camera.camera,cmd,source,temp1Handle);
                    EnviroVolumetricCloudRenderer renderer = GetCloudsRenderer(camera.camera);
                    EnviroManager.instance.VolumetricClouds.RenderVolumetricCloudsHDRP(camera.camera,cmd, temp1Handle, destination, renderer, null);
                } 
          
                RenderTexture.ReleaseTemporary(temp1);
                //temp1Handle.Release();
            }
            else if(EnviroManager.instance.VolumetricClouds != null && renderVolumetricClouds && !renderFog)
            {
                EnviroVolumetricCloudRenderer renderer = GetCloudsRenderer(camera.camera);
                  
                if(EnviroManager.instance.VolumetricClouds.settingsGlobal.cloudShadows && camera.camera.cameraType != CameraType.Reflection)
                {
                    RenderTexture temp1 = RenderTexture.GetTemporary(source.rt.descriptor);
                    RTHandle temp1Handle = RTHandles.Alloc(temp1);
                    EnviroManager.instance.VolumetricClouds.RenderVolumetricCloudsHDRP(camera.camera,cmd, source, temp1Handle, renderer, null);
                    EnviroManager.instance.VolumetricClouds.RenderCloudsShadowsHDRP(camera.camera,cmd,temp1Handle,destination,renderer);
                    RenderTexture.ReleaseTemporary(temp1);
                    //temp1Handle.Release();
                }
                else
                {
                    EnviroManager.instance.VolumetricClouds.RenderVolumetricCloudsHDRP(camera.camera,cmd, source, destination, renderer, null);
                }
                
            } 
            else if (Enviro.EnviroManager.instance.Fog != null && renderFog)
            {
                EnviroManager.instance.Fog.RenderHeightFogHDRP(camera.camera,cmd,source,destination);
            }
            else
            {
                blitTrough.SetTexture("_InputTexture", source);
                CoreUtils.DrawFullScreen(cmd, blitTrough);
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
    }
} 
#endif