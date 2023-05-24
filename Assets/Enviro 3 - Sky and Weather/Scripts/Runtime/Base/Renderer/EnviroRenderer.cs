using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enviro
{
    [ExecuteInEditMode] 
    [ImageEffectAllowedInSceneView]
    public class EnviroRenderer : MonoBehaviour
    {
        [Tooltip("Assign a quality here if you want to use different settings for this camera. Otherwise it takes settings from Enviro Manager.")]
        public EnviroQuality myQuality;
        private Camera myCam;
        private EnviroVolumetricCloudRenderer volumetricCloudsRender;

        void OnEnable()
        {
            myCam = GetComponent<Camera>();

            //Disable this component in URP and HDRP.
    #if ENVIRO_HDRP || ENVIRO_URP
            this.enabled = false;
    #endif
        }

        void OnDisable ()
        {
             CleanupVolumetricRenderer();
        }

        private void CleanupVolumetricRenderer()
        {
            if(volumetricCloudsRender != null)
            {
                if(volumetricCloudsRender.raymarchMat != null)
                    DestroyImmediate(volumetricCloudsRender.raymarchMat);

                if(volumetricCloudsRender.blendAndLightingMat != null)
                    DestroyImmediate(volumetricCloudsRender.blendAndLightingMat);

                if(volumetricCloudsRender.reprojectMat != null)
                    DestroyImmediate(volumetricCloudsRender.reprojectMat);

                if(volumetricCloudsRender.undersampleBuffer != null)
                    DestroyImmediate(volumetricCloudsRender.undersampleBuffer);
                
                if(volumetricCloudsRender.fullBuffer != null && volumetricCloudsRender.fullBuffer.Length > 0)
                    { 
                        for (int i = 0; i < volumetricCloudsRender.fullBuffer.Length; i++)
                        {
                            if(volumetricCloudsRender.fullBuffer[i] != null)
                                DestroyImmediate(volumetricCloudsRender.fullBuffer[i]);
                        }                     
                    }            
            } 
        }

        private void SetMatrix()
        {
            if (myCam.stereoEnabled)
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

        private Material shadowMat;

        [ImageEffectOpaque]
        private void OnRenderImage(RenderTexture src, RenderTexture dest) 
        {   
            if(EnviroManager.instance == null)
            {
                Graphics.Blit(src,dest);
                return;
            }

            if(myCam == null)
               myCam = GetComponent<Camera>();

            if (myCam.actualRenderingPath == RenderingPath.Forward)
                myCam.depthTextureMode |= DepthTextureMode.Depth;
  
            //Set what to render on this camera.
            bool renderVolumetricClouds = false;
            bool renderFog = false;

            if(EnviroManager.instance.VolumetricClouds != null)
                renderVolumetricClouds = EnviroManager.instance.VolumetricClouds.settingsQuality.volumetricClouds;

            if(EnviroManager.instance.Fog != null)
                renderFog = EnviroManager.instance.Fog.Settings.fog;


            ////////Rendering//////////
            SetMatrix();  

            if(volumetricCloudsRender == null)
               volumetricCloudsRender = new EnviroVolumetricCloudRenderer();

            //Render volumetrics mask first
            if(EnviroManager.instance.Fog != null && renderFog)
               EnviroManager.instance.Fog.RenderVolumetrics(myCam, src);

            if(EnviroManager.instance.Fog != null && EnviroManager.instance.VolumetricClouds != null && renderVolumetricClouds && renderFog)
            { 
                //Change the order of clouds and fog
                RenderTexture temp = RenderTexture.GetTemporary(src.descriptor);
                RenderTexture temp2 = RenderTexture.GetTemporary(src.descriptor);

                if(myCam.transform.position.y < EnviroManager.instance.VolumetricClouds.settingsLayer1.bottomCloudsHeight)
                {
                    EnviroManager.instance.VolumetricClouds.RenderVolumetricClouds(myCam, src, temp, volumetricCloudsRender, myQuality);
                    
                    if(EnviroManager.instance.VolumetricClouds.settingsGlobal.cloudShadows && myCam.cameraType != CameraType.Reflection)
                    {
                        EnviroManager.instance.VolumetricClouds.RenderCloudsShadows(temp,temp2,volumetricCloudsRender);
                        EnviroManager.instance.Fog.RenderHeightFog(myCam,temp2,dest);
                    }
                    else 
                    {
                        EnviroManager.instance.Fog.RenderHeightFog(myCam,temp,dest);
                    }
                }
                else
                {
                    EnviroManager.instance.Fog.RenderHeightFog(myCam,src,temp);
                    EnviroManager.instance.VolumetricClouds.RenderVolumetricClouds(myCam,temp,dest,volumetricCloudsRender,myQuality);    
                }

                RenderTexture.ReleaseTemporary(temp);
                RenderTexture.ReleaseTemporary(temp2);
            }
            else if(EnviroManager.instance.VolumetricClouds != null && renderVolumetricClouds && !renderFog)
            {
                if(EnviroManager.instance.VolumetricClouds.settingsGlobal.cloudShadows && myCam.cameraType != CameraType.Reflection)
                {
                    RenderTexture temp = RenderTexture.GetTemporary(src.descriptor);
                    EnviroManager.instance.VolumetricClouds.RenderVolumetricClouds(myCam,src,temp,volumetricCloudsRender, myQuality);
                    EnviroManager.instance.VolumetricClouds.RenderCloudsShadows(temp,dest,volumetricCloudsRender);
                    RenderTexture.ReleaseTemporary(temp);
                }
                else
                {
                    EnviroManager.instance.VolumetricClouds.RenderVolumetricClouds(myCam,src,dest,volumetricCloudsRender, myQuality);
                }
                
            } 
            else if (Enviro.EnviroManager.instance.Fog != null && renderFog)
            {
                EnviroManager.instance.Fog.RenderHeightFog(myCam,src,dest);
            }
            else
            {
                Graphics.Blit(src,dest);
            }
        }
    }
}
