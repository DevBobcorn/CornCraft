using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Enviro 
{  
 
    [Serializable] 
    public class EnviroVolumetricCloudsQuality 
    {
		public bool volumetricClouds = true;
        [Range(1,6)]
        public int downsampling = 4;
        [Range(32,256)]
		public int stepsLayer1 = 128;
        [Range(32,256)]
		public int stepsLayer2 = 64;
        [Range(0f,2f)]
        public float blueNoiseIntensity = 1f;
        [Range(0f,10f)]
		public float reprojectionBlendTime = 10f;
        [Range(0f,1f)]
		public float lodDistance = 0.25f;

    }

    [Serializable] 
    public class EnviroCloudGlobalSettings 
    {
        public bool dualLayer = false;

        public Vector3 floatingPointOriginMod;
        public Gradient sunLightColorGradient;
        public Gradient moonLightColorGradient;
        public Gradient ambientColorGradient;

        public Color sunLightColor;
        public Color moonLightColor;
        public Color ambientColor;

        public bool depthBlending = false;

        public Texture3D noise; 
        public Texture3D detailNoise;
        public Texture2D curlTex;
        public Texture2D blueNoise;

        public float cloudsWorldScale = 5000000f;
        public float atmosphereColorSaturateDistance = 25000f;
        [Range(0.0f, 2.0f)]
        public float ambientLighIntensity = 1f;

        public bool cloudShadows = true;
        [Range(0.0f, 2.0f)]
        public float cloudShadowsIntensity = 1f;

        [Range(0.0f, 1f)]
        public float cloudsTravelSpeed = 0.5f;
    }


    [Serializable] 
    public class EnviroCloudLayerSettings 
    { 
        [Range(-1f,1f)]
        public float cloudsWindDirectionXModifier = 1f;
        [Range(-1f,1f)]
        public float cloudsWindDirectionYModifier = 1f;
        [Range(-0.1f,0.1f)] 
        public float windSpeedModifier = 1f;
        [Range(0f,0.1f)]
        public float windUpwards = 1f;

        [Range(-1f,1f)]
        public float coverage = 1f;
        public float worleyFreq2 = 4f;
        public float worleyFreq1 = 1f;
        [Range(0f,1f)]
        public float dilateCoverage = 0.5f;
        [Range(0f,1f)]
        public float dilateType = 0.5f;
        [Range(0f,1f)]
        public float cloudsTypeModifier = 0.5f;
        public Vector2 locationOffset;

        public float bottomCloudsHeight = 2000f;
        public float topCloudsHeight = 8000f;
 
        [Range(0f,2f)]
        public float density = 0.3f;
        [Range(0f,2f)]
        public float densitySmoothness = 1.0f;
        [Range(0f,2f)]
        public float scatteringIntensity = 1f;
        [Range(0f,1f)] 
        public float silverLiningSpread = 0.8f;
        [Range(0f,1f)]
        public float powderIntensity = 0.5f;
        [Range(0f,1f)]
        public float curlIntensity = 0.25f;
        [Range(0f,0.25f)]
        public float lightStepModifier = 0.05f;

        [Range(0f,2f)]
        public float lightAbsorbtion = 0.5f;  
 
        [Range(0f,1f)]
        public float multiScatteringA = 0.5f;
        [Range(0f,1f)]
        public float multiScatteringB = 0.5f; 
        [Range(0f,1f)]
        public float multiScatteringC = 0.5f;

        public float baseNoiseUV = 15f;
        public float detailNoiseUV = 50f;

        [Range(0.0f, 1.0f)]
        public float baseErosionIntensity = 0f;
        [Range(0.0f, 1.0f)]
        public float detailErosionIntensity = 0.3f;
        [Range(0.0f, 1.0f)]
        public float anvilBias = 0.0f;
    }

    public class EnviroVolumetricCloudRenderer
    { 
        // Clouds Rendering
        public Camera camera;
        public Material raymarchMat;
        public Material reprojectMat;
        public Material depthMat;
        public Material blendAndLightingMat; 
        public Material shadowMat;
        public RenderTexture[] fullBuffer;
        public int fullBufferIndex; 
        public RenderTexture undersampleBuffer;
        public RenderTexture downsampledDepth;
        public Matrix4x4 prevV;
        public int frame = 0;
        public bool firstFrame = true;

#if ENVIRO_HDRP
    public UnityEngine.Rendering.RTHandle[] fullBufferHandles;
    public UnityEngine.Rendering.RTHandle undersampleBufferHandle;
    public UnityEngine.Rendering.RTHandle downsampledDepthHandle;
#endif
    }

    [Serializable]
    [ExecuteInEditMode]
    public class EnviroVolumetricCloudsModule : EnviroModule
    {   
        //Base
        public Enviro.EnviroCloudLayerSettings settingsLayer1;
        public Enviro.EnviroCloudLayerSettings settingsLayer2;
        public Enviro.EnviroCloudGlobalSettings settingsGlobal;
        public Enviro.EnviroVolumetricCloudsQuality settingsQuality;
        public EnviroVolumetricCloudsModule preset;

        //Inspector
        public bool showGlobalControls;
        public bool showLayer1Controls; 
        public bool showLayer2Controls;
        public bool showCoverageControls;
        public bool showLightingControls;
        public bool showDensityControls;
        public bool showTextureControls;
        public bool showWindControls;

        //Wind
        public Vector3 cloudAnimLayer1;
        public Vector3 cloudAnimLayer2;
        public Vector3 cloudAnimNonScaledLayer1;
        public Vector3 cloudAnimNonScaledLayer2;

        //Weather Map
        public RenderTexture weatherMap;
        private Material weatherMapMat;
        private ComputeShader weatherMapCS;

        private Light dirLight;

        //Update Method
        public override void UpdateModule ()
        { 
            if(EnviroManager.instance == null)
               return;

            UpdateWind();
            UnityEngine.Profiling.Profiler.BeginSample("Enviro Clouds Weather Map");
            weatherMap = EnviroManager.instance.VolumetricClouds.RenderWeatherMap();
            UnityEngine.Profiling.Profiler.EndSample(); 
        }
 
        public override void Enable()
        {

        }

        public override void Disable() 
        {
            if(weatherMapMat != null)
               DestroyImmediate(weatherMapMat);

            if(weatherMap != null)
               DestroyImmediate(weatherMap);
        }

        public void RenderCloudsShadows(RenderTexture source, RenderTexture destination, EnviroVolumetricCloudRenderer renderer)
        {
             if(renderer.shadowMat == null)
                renderer.shadowMat = new Material(Shader.Find("Hidden/EnviroApplyShadows"));

                renderer.shadowMat.SetTexture("_CloudsTex",renderer.undersampleBuffer);
                renderer.shadowMat.SetFloat("_Intensity",EnviroManager.instance.VolumetricClouds.settingsGlobal.cloudShadowsIntensity);

                renderer.shadowMat.SetTexture("_MainTex",source);
                Graphics.Blit(source,destination,renderer.shadowMat);
        }
 
#if ENVIRO_URP
        public void RenderCloudsShadowsURP(EnviroURPRenderPass pass,Camera cam, UnityEngine.Rendering.CommandBuffer cmd, RenderTexture source, UnityEngine.Rendering.RenderTargetIdentifier destination, EnviroVolumetricCloudRenderer renderer)
        { 
             if(renderer.shadowMat == null)
                renderer.shadowMat = new Material(Shader.Find("Hidden/EnviroApplyShadows"));

                renderer.shadowMat.SetTexture("_CloudsTex",renderer.undersampleBuffer);
                renderer.shadowMat.SetFloat("_Intensity",EnviroManager.instance.VolumetricClouds.settingsGlobal.cloudShadowsIntensity);

                renderer.shadowMat.EnableKeyword("ENVIROURP");
                pass.CustomBlit(cmd,cam.cameraToWorldMatrix,source,destination,renderer.shadowMat);
        }
#endif

#if ENVIRO_HDRP
        public void RenderCloudsShadowsHDRP(Camera cam, UnityEngine.Rendering.CommandBuffer cmd, UnityEngine.Rendering.RTHandle source, UnityEngine.Rendering.RTHandle destination, EnviroVolumetricCloudRenderer renderer)
        {
             if(renderer.shadowMat == null)
                renderer.shadowMat = new Material(Shader.Find("Hidden/EnviroApplyShadowsHDRP"));

                renderer.shadowMat.SetTexture("_MainTex",source);
                renderer.shadowMat.SetTexture("_CloudsTex",renderer.undersampleBufferHandle);
                renderer.shadowMat.SetVector("_HandleScales", new Vector4(1/renderer.undersampleBufferHandle.rtHandleProperties.rtHandleScale.x,1/renderer.undersampleBufferHandle.rtHandleProperties.rtHandleScale.y,1,1)); 
                renderer.shadowMat.SetFloat("_Intensity",EnviroManager.instance.VolumetricClouds.settingsGlobal.cloudShadowsIntensity);
                cmd.Blit(source,destination,renderer.shadowMat);
        }
#endif
        /// Render Clouds
        public void RenderVolumetricClouds(Camera cam, RenderTexture source, RenderTexture destination, EnviroVolumetricCloudRenderer renderer, EnviroQuality quality)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Enviro Clouds Rendering");

            int downsampling = settingsQuality.downsampling;

            if(quality != null)
               downsampling = quality.volumetricCloudsOverride.downsampling;

            int width = cam.pixelWidth / downsampling;
            int height = cam.pixelHeight / downsampling;

            if(cam.cameraType != CameraType.Reflection)
            {
                if (renderer.fullBuffer == null || renderer.fullBuffer.Length != 2)
                {
                    renderer.fullBuffer = new RenderTexture[2];
                }
                renderer.fullBufferIndex = (renderer.fullBufferIndex + 1) % 2;

                renderer.firstFrame |= CreateRenderTexture(ref renderer.fullBuffer[0], width, height, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear,source.descriptor);
                renderer.firstFrame |= CreateRenderTexture(ref renderer.fullBuffer[1], width, height, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear,source.descriptor);
            }
            renderer.firstFrame |= CreateRenderTexture(ref renderer.undersampleBuffer, width , height, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear,source.descriptor);

            renderer.frame++; 

            if(renderer.frame > 64)
              renderer.frame = 0;   
 
            if(renderer.depthMat == null)
               renderer.depthMat = new Material(Shader.Find("Hidden/EnviroVolumetricCloudsDepth"));
 
            CreateRenderTexture(ref renderer.downsampledDepth, width, height, RenderTextureFormat.RFloat, FilterMode.Point, source.descriptor);
          
            renderer.depthMat.SetTexture("_MainTex", source);
            renderer.depthMat.SetVector("_CameraDepthTexture_TexelSize", new Vector4(1 / source.width, 1 / source.height, source.width, source.height));


            if (downsampling > 1) 
            {
                Graphics.Blit(source, renderer.downsampledDepth, renderer.depthMat, 0);  //Downsample the texture.
            } 
            else 
            {
                Graphics.Blit(source, renderer.downsampledDepth, renderer.depthMat, 1);  //Just copy it.
            }        
  
            //1. Raymarch
            SetRaymarchShader(cam,renderer, quality);
            renderer.raymarchMat.SetTexture("_MainTex", source);
            Graphics.Blit(source,renderer.undersampleBuffer,renderer.raymarchMat);


            //Pass 2: Reprojection
            if(cam.cameraType != CameraType.Reflection)
            {
                if(renderer.reprojectMat == null)
                   renderer.reprojectMat = new Material(Shader.Find("Hidden/EnviroVolumetricCloudsReproject"));
                SetReprojectShader(cam, renderer, quality);
    
                if (renderer.firstFrame) 
                {
                    Graphics.Blit(renderer.undersampleBuffer, renderer.fullBuffer[renderer.fullBufferIndex]);
                }

                renderer.reprojectMat.SetTexture("_MainTex", renderer.fullBuffer[renderer.fullBufferIndex]);
                Graphics.Blit(renderer.fullBuffer[renderer.fullBufferIndex], renderer.fullBuffer[renderer.fullBufferIndex ^ 1], renderer.reprojectMat);
            } 
            //Pass 3: Lighting and Blending
            if(renderer.blendAndLightingMat == null)
               renderer.blendAndLightingMat = new Material(Shader.Find("Hidden/EnviroVolumetricCloudsBlend"));
            SetBlendShader(cam,renderer);
            renderer.blendAndLightingMat.SetTexture("_MainTex", source);
            Graphics.Blit(source, destination, renderer.blendAndLightingMat);
  
            renderer.prevV = cam.worldToCameraMatrix;
            renderer.firstFrame = false;

            UnityEngine.Profiling.Profiler.EndSample();
        }

 #if ENVIRO_URP
        public void RenderVolumetricCloudsURP(UnityEngine.Rendering.Universal.RenderingData renderingData, EnviroURPRenderPass pass, UnityEngine.Rendering.CommandBuffer cmd,  RenderTexture source, UnityEngine.Rendering.RenderTargetIdentifier destination, EnviroVolumetricCloudRenderer renderer, EnviroQuality quality)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Enviro Clouds Rendering");

            int downsampling = settingsQuality.downsampling;

            if(quality != null)
               downsampling = quality.volumetricCloudsOverride.downsampling;

            int width = renderingData.cameraData.camera.pixelWidth / downsampling;
            int height = renderingData.cameraData.camera.pixelHeight / downsampling;

            if(renderingData.cameraData.camera.cameraType != CameraType.Reflection)
            {
                if (renderer.fullBuffer == null || renderer.fullBuffer.Length != 2)
                {
                    renderer.fullBuffer = new RenderTexture[2];
                }
                renderer.fullBufferIndex = (renderer.fullBufferIndex + 1) % 2;
        

            renderer.firstFrame |= CreateRenderTexture(ref renderer.fullBuffer[0], width, height, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear,source.descriptor);
            renderer.firstFrame |= CreateRenderTexture(ref renderer.fullBuffer[1], width, height, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear,source.descriptor);
            }  
            
            renderer.firstFrame |= CreateRenderTexture(ref renderer.undersampleBuffer, width , height, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear,source.descriptor);

            renderer.frame++;
 
            if(renderer.frame > 64)
              renderer.frame = 0;   
 
            if(renderer.depthMat == null)
               renderer.depthMat = new Material(Shader.Find("Hidden/EnviroVolumetricCloudsDepth"));
            
            renderer.depthMat.SetVector("_CameraDepthTexture_TexelSize", new Vector4(1 / renderingData.cameraData.cameraTargetDescriptor.width, 1 / renderingData.cameraData.cameraTargetDescriptor.height, renderingData.cameraData.cameraTargetDescriptor.width, renderingData.cameraData.cameraTargetDescriptor.height));
            
            SetToURP(renderer.depthMat);   
            CreateRenderTexture(ref renderer.downsampledDepth, width, height, RenderTextureFormat.RFloat, FilterMode.Point, source.descriptor);
            
            if (downsampling > 1) 
            {
                pass.CustomBlit(cmd,renderingData.cameraData.camera.cameraToWorldMatrix,source,renderer.downsampledDepth,renderer.depthMat,0);
            } 
            else 
            {
                pass.CustomBlit(cmd,renderingData.cameraData.camera.cameraToWorldMatrix,source,renderer.downsampledDepth,renderer.depthMat,1);
            }        
  
            //1. Raymarch
            
            SetRaymarchShader(renderingData.cameraData.camera,renderer, quality);
           
            SetToURP(renderer.raymarchMat);     
            pass.CustomBlit(cmd,renderingData.cameraData.camera.cameraToWorldMatrix,source,renderer.undersampleBuffer,renderer.raymarchMat);
  

            //Pass 2: Reprojection
            if(renderingData.cameraData.camera.cameraType != CameraType.Reflection)
            {     
                if(renderer.reprojectMat == null)
                   renderer.reprojectMat = new Material(Shader.Find("Hidden/EnviroVolumetricCloudsReproject"));
                SetReprojectShader(renderingData.cameraData.camera, renderer, quality);
                SetToURP(renderer.reprojectMat);
                
                if (renderer.firstFrame) 
                    pass.CustomBlit(cmd,renderingData.cameraData.camera.cameraToWorldMatrix,renderer.undersampleBuffer,renderer.fullBuffer[renderer.fullBufferIndex]);

                pass.CustomBlit(cmd,renderingData.cameraData.camera.cameraToWorldMatrix,renderer.fullBuffer[renderer.fullBufferIndex],renderer.fullBuffer[renderer.fullBufferIndex ^ 1],renderer.reprojectMat);
            }

            //Pass 3: Lighting and Blending       
            if(renderer.blendAndLightingMat == null)
               renderer.blendAndLightingMat = new Material(Shader.Find("Hidden/EnviroVolumetricCloudsBlend"));
            SetBlendShader(renderingData.cameraData.camera,renderer);
            SetToURP(renderer.blendAndLightingMat); 
            pass.CustomBlit(cmd,renderingData.cameraData.camera.cameraToWorldMatrix,source,destination,renderer.blendAndLightingMat);

            renderer.prevV = renderingData.cameraData.camera.worldToCameraMatrix;
            renderer.firstFrame = false;
 
            UnityEngine.Profiling.Profiler.EndSample();
        }
#endif

#if ENVIRO_HDRP 
        public void RenderVolumetricCloudsHDRP(Camera cam, UnityEngine.Rendering.CommandBuffer cmd, UnityEngine.Rendering.RTHandle source, UnityEngine.Rendering.RTHandle destination, EnviroVolumetricCloudRenderer renderer, EnviroQuality quality)
        { 
            //UnityEngine.Profiling.Profiler.BeginSample("Enviro Clouds Rendering");
 
            int downsampling = settingsQuality.downsampling;

            if(quality != null)
               downsampling = quality.volumetricCloudsOverride.downsampling;

            int width = cam.pixelWidth / downsampling;
            int height = cam.pixelHeight / downsampling;

            RenderTextureDescriptor desc = source.rt.descriptor;
            desc.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
  
            if(cam.cameraType != CameraType.Reflection)
            {
                if (renderer.fullBuffer == null || renderer.fullBuffer.Length != 2)
                {
                    renderer.fullBuffer = new RenderTexture[2];
                    renderer.fullBufferHandles = new UnityEngine.Rendering.RTHandle[2];
                }
                renderer.fullBufferIndex = (renderer.fullBufferIndex + 1) % 2;

                renderer.firstFrame |= CreateRenderTexture(ref renderer.fullBuffer[0], width, height, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear,desc);
                renderer.firstFrame |= CreateRenderTexture(ref renderer.fullBuffer[1], width, height, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear,desc);
                
                renderer.fullBufferHandles[0] = UnityEngine.Rendering.RTHandles.Alloc(renderer.fullBuffer[0]);
                renderer.fullBufferHandles[1] = UnityEngine.Rendering.RTHandles.Alloc(renderer.fullBuffer[1]);     
            }
            renderer.firstFrame |= CreateRenderTexture(ref renderer.undersampleBuffer, width , height, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear,desc);
            renderer.undersampleBufferHandle = UnityEngine.Rendering.RTHandles.Alloc(renderer.undersampleBuffer);


            renderer.frame++; 

            if(renderer.frame > 64)
              renderer.frame = 0;   
 
            if(renderer.depthMat == null)
               renderer.depthMat = new Material(Shader.Find("Hidden/EnviroVolumetricCloudsDepthHDRP"));
 
            CreateRenderTexture(ref renderer.downsampledDepth, width, height, RenderTextureFormat.RFloat, FilterMode.Point, desc);
            renderer.downsampledDepthHandle = UnityEngine.Rendering.RTHandles.Alloc(renderer.downsampledDepth);      
            renderer.depthMat.SetTexture("_MainTex", source);

            if (downsampling > 1) 
            {
                cmd.Blit(source, renderer.downsampledDepthHandle, renderer.depthMat, 0);  //Downsample the texture.
            } 
            else 
            {
                cmd.Blit(source, renderer.downsampledDepthHandle, renderer.depthMat, 1);  //Just copy it.
            }        

           
            //1. Raymarch
            SetRaymarchShader(cam,renderer, quality);
            renderer.raymarchMat.SetTexture("_MainTex", source);
            cmd.Blit(source,renderer.undersampleBufferHandle,renderer.raymarchMat);


            //Pass 2: Reprojection
            if(cam.cameraType != CameraType.Reflection)
            {
                if(renderer.reprojectMat == null)
                   renderer.reprojectMat = new Material(Shader.Find("Hidden/EnviroVolumetricCloudsReproject"));

                SetReprojectShader(cam, renderer, quality);
    
                if (renderer.firstFrame) 
                {
                    cmd.Blit(renderer.undersampleBufferHandle, renderer.fullBufferHandles[renderer.fullBufferIndex]);
                }
 
                renderer.reprojectMat.SetTexture("_MainTex", renderer.fullBufferHandles[renderer.fullBufferIndex]);
                cmd.Blit(renderer.fullBufferHandles[renderer.fullBufferIndex], renderer.fullBufferHandles[renderer.fullBufferIndex ^ 1], renderer.reprojectMat);
            }
            //Pass 3: Lighting and Blending
            if(renderer.blendAndLightingMat == null)
               renderer.blendAndLightingMat = new Material(Shader.Find("Hidden/EnviroVolumetricCloudsBlendHDRP"));

            SetBlendShader(cam,renderer);
            renderer.blendAndLightingMat.SetTexture("_MainTex", source); 
            cmd.Blit(source, destination, renderer.blendAndLightingMat);

            renderer.prevV = cam.worldToCameraMatrix;
            renderer.firstFrame = false;

            //UnityEngine.Profiling.Profiler.EndSample();
        }
#endif

        void SetRaymarchShader (Camera cam, EnviroVolumetricCloudRenderer renderer, EnviroQuality quality)
        {
            if(renderer.raymarchMat == null)
               renderer.raymarchMat = new Material(Shader.Find("Hidden/EnviroCloudsRaymarch"));
    
            if(dirLight == null)
            {
               dirLight = EnviroHelper.GetDirectionalLight();
            }
            //Check if we use dual lightmode and change the light only in that case. Otherwise keep it to the cached one.
            else if (EnviroManager.instance.Lighting != null && EnviroManager.instance.Lighting.Settings.lightingMode == EnviroLighting.LightingMode.Dual)
            {
               dirLight = EnviroHelper.GetDirectionalLight();
            }

            EnviroCloudLayerSettings layer1 = settingsLayer1;
            EnviroCloudLayerSettings layer2 = settingsLayer2;
            EnviroCloudGlobalSettings global = settingsGlobal;
            
            float blueNoiseIntensity = settingsQuality.blueNoiseIntensity;
            float lodDistance = settingsQuality.lodDistance;
            Vector4 steps = new Vector4(settingsQuality.stepsLayer1,settingsQuality.stepsLayer1,settingsQuality.stepsLayer2,settingsQuality.stepsLayer2);
            int downsample = settingsQuality.downsampling;

            if(quality != null)
            {
                blueNoiseIntensity = quality.volumetricCloudsOverride.blueNoiseIntensity;
                steps = new Vector4(quality.volumetricCloudsOverride.stepsLayer1,quality.volumetricCloudsOverride.stepsLayer1,quality.volumetricCloudsOverride.stepsLayer2,quality.volumetricCloudsOverride.stepsLayer2);
                lodDistance = quality.volumetricCloudsOverride.lodDistance;
                downsample = quality.volumetricCloudsOverride.downsampling;
            }
                   
            //Textures
            renderer.raymarchMat.SetTexture("_Noise", settingsGlobal.noise);
            renderer.raymarchMat.SetTexture("_DetailNoise", settingsGlobal.detailNoise); 
            renderer.raymarchMat.SetTexture("_CurlNoise", settingsGlobal.curlTex);

            if(weatherMap != null)
                renderer.raymarchMat.SetTexture("_WeatherMap",weatherMap);

            //Matrix
            if(UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced)
            {
                renderer.raymarchMat.SetMatrix("_InverseProjection", cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left).inverse);
                renderer.raymarchMat.SetMatrix("_InverseRotation", cam.GetStereoViewMatrix(Camera.StereoscopicEye.Left).inverse);
                renderer.raymarchMat.SetMatrix("_InverseProjectionRight", cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right).inverse);
                renderer.raymarchMat.SetMatrix("_InverseRotationRight", cam.GetStereoViewMatrix(Camera.StereoscopicEye.Right).inverse);
            }
            else
            {
                renderer.raymarchMat.SetMatrix("_InverseProjection", cam.projectionMatrix.inverse);
                renderer.raymarchMat.SetMatrix("_InverseRotation", cam.cameraToWorldMatrix);
            }

            if (EnviroManager.instance.Objects.worldAnchor != null)
                settingsGlobal.floatingPointOriginMod = EnviroManager.instance.Objects.worldAnchor.transform.position;

            renderer.raymarchMat.SetVector("_CameraPosition", cam.transform.position - settingsGlobal.floatingPointOriginMod);
            renderer.raymarchMat.SetVector("_Steps", steps);
            
            if(dirLight != null)
            renderer.raymarchMat.SetVector("_LightDir", -dirLight.transform.forward);  
            else
            renderer.raymarchMat.SetVector("_LightDir", Vector3.zero);  

            renderer.raymarchMat.SetVector("_CloudsNoiseSettings", new Vector4(layer1.baseNoiseUV, layer1.detailNoiseUV, layer2.baseNoiseUV, layer2.detailNoiseUV));

            renderer.raymarchMat.SetVector("_CloudsLighting", new Vector4(layer1.scatteringIntensity, 0f, 0f, layer1.silverLiningSpread));
            renderer.raymarchMat.SetVector("_CloudsLighting2", new Vector4(layer2.scatteringIntensity, 0f, 0f, layer2.silverLiningSpread));
            
            renderer.raymarchMat.SetVector("_CloudsLightingExtended", new Vector4(layer1.powderIntensity, layer1.curlIntensity, layer1.lightStepModifier, layer1.lightAbsorbtion));   
            renderer.raymarchMat.SetVector("_CloudsLightingExtended2", new Vector4(layer2.powderIntensity, layer2.curlIntensity, layer2.lightStepModifier, layer2.lightAbsorbtion));

            renderer.raymarchMat.SetVector("_CloudsMultiScattering", new Vector4(layer1.multiScatteringA, layer1.multiScatteringB, layer1.multiScatteringC, 0));
            renderer.raymarchMat.SetVector("_CloudsMultiScattering2", new Vector4(layer2.multiScatteringA, layer2.multiScatteringB, layer2.multiScatteringC, 0));
            
            renderer.raymarchMat.SetVector("_CloudsParameter", new Vector4(layer1.bottomCloudsHeight, layer1.topCloudsHeight, 1 / (layer1.topCloudsHeight - layer1.bottomCloudsHeight), settingsGlobal.cloudsWorldScale));
            renderer.raymarchMat.SetVector("_CloudsParameter2", new Vector4(layer2.bottomCloudsHeight, layer2.topCloudsHeight, 1 / (layer2.topCloudsHeight - layer2.bottomCloudsHeight), settingsGlobal.cloudsWorldScale));
            renderer.raymarchMat.SetFloat("_BlueNoiseIntensity",blueNoiseIntensity);
            renderer.raymarchMat.SetVector("_CloudDensityScale", new Vector4(layer1.density, layer2.density, layer1.densitySmoothness, layer2.densitySmoothness));
            renderer.raymarchMat.SetVector("_CloudsCoverageSettings", new Vector4(layer1.coverage, 0f, layer1.anvilBias, layer2.anvilBias));
            renderer.raymarchMat.SetVector("_CloudsAnimation", new Vector4(cloudAnimLayer1.x, cloudAnimLayer1.y, cloudAnimLayer1.z, 0f));
            if(EnviroManager.instance.Environment != null)
            { 
                renderer.raymarchMat.SetVector("_CloudsWindDirection", new Vector4(EnviroManager.instance.Environment.Settings.windDirectionX * settingsLayer1.cloudsWindDirectionXModifier, EnviroManager.instance.Environment.Settings.windDirectionY * settingsLayer1.cloudsWindDirectionYModifier, 0f,0f));
            }         
            else
            {
                 renderer.raymarchMat.SetVector("_CloudsWindDirection", new Vector4(settingsLayer1.cloudsWindDirectionXModifier, settingsLayer1.cloudsWindDirectionYModifier, 0f, 0f));
            }

            renderer.raymarchMat.SetVector("_CloudsErosionIntensity", new Vector4(1f - layer1.baseErosionIntensity, layer1.detailErosionIntensity,1f - layer2.baseErosionIntensity, layer2.detailErosionIntensity));
 
            renderer.raymarchMat.SetFloat("_LODDistance", lodDistance);
 
            renderer.raymarchMat.SetTexture("_DownsampledDepth", renderer.downsampledDepth);  
            renderer.raymarchMat.SetInt("_Frame", renderer.frame);
            renderer.raymarchMat.SetTexture("_BlueNoise",settingsGlobal.blueNoise);
            renderer.raymarchMat.SetVector("_Randomness", new Vector4(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value));  
            renderer.raymarchMat.SetVector("_Resolution", new Vector4(renderer.undersampleBuffer.width,renderer.undersampleBuffer.height,downsample,0f));
 
            if(settingsGlobal.dualLayer)
                renderer.raymarchMat.EnableKeyword("ENVIRO_DUAL_LAYER");
            else 
                renderer.raymarchMat.DisableKeyword("ENVIRO_DUAL_LAYER");

            if(settingsGlobal.cloudShadows)
                renderer.raymarchMat.EnableKeyword("ENVIRO_CLOUD_SHADOWS");
            else
                renderer.raymarchMat.DisableKeyword("ENVIRO_CLOUD_SHADOWS"); 

            SetDepthBlending(renderer.raymarchMat);
        } 
 
        void SetReprojectShader (Camera cam, EnviroVolumetricCloudRenderer renderer, EnviroQuality quality)
        {
          
            float reprojectionBlendTime = settingsQuality.reprojectionBlendTime;
                    
            if(quality != null)
            {
                reprojectionBlendTime = quality.volumetricCloudsOverride.reprojectionBlendTime;
            }

            SetDepthBlending(renderer.reprojectMat);

        #if ENVIRO_HDRP
            renderer.reprojectMat.SetTexture("_DownsampledDepth", renderer.downsampledDepthHandle);
            renderer.reprojectMat.SetTexture("_UndersampleCloudTex", renderer.undersampleBufferHandle);
        #else
            renderer.reprojectMat.SetTexture("_DownsampledDepth", renderer.downsampledDepth);
            renderer.reprojectMat.SetTexture("_UndersampleCloudTex", renderer.undersampleBuffer);
        #endif         

            if(UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced) 
            {
                renderer.reprojectMat.SetMatrix("_PrevVP", renderer.prevV);
                renderer.reprojectMat.SetVector("_ProjectionExtents", EnviroHelper.GetProjectionExtents(cam,Camera.StereoscopicEye.Left));
                renderer.reprojectMat.SetVector("_ProjectionExtentsRight", EnviroHelper.GetProjectionExtents(cam,Camera.StereoscopicEye.Right));
            }
            else 
            {
                renderer.reprojectMat.SetMatrix("_PrevVP", renderer.prevV);
                renderer.reprojectMat.SetVector("_ProjectionExtents", EnviroHelper.GetProjectionExtents(cam));
            }
            renderer.reprojectMat.SetFloat("_BlendTime", reprojectionBlendTime);

            Matrix4x4 camtowolrd = Matrix4x4.TRS(cam.transform.position, cam.transform.rotation, Vector3.one);
            renderer.reprojectMat.SetMatrix("_CamToWorld",  camtowolrd);
        }

        void SetBlendShader (Camera cam, EnviroVolumetricCloudRenderer renderer)
        {
            SetDepthBlending(renderer.blendAndLightingMat);

            if(UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced) 
            {
                renderer.blendAndLightingMat.SetVector("_ProjectionExtents", EnviroHelper.GetProjectionExtents(cam,Camera.StereoscopicEye.Left));
                renderer.blendAndLightingMat.SetVector("_ProjectionExtentsRight", EnviroHelper.GetProjectionExtents(cam,Camera.StereoscopicEye.Right));
            }
            else  
            {
                renderer.blendAndLightingMat.SetVector("_ProjectionExtents", EnviroHelper.GetProjectionExtents(cam));
            }
        #if ENVIRO_HDRP
            renderer.blendAndLightingMat.SetTexture("_DownsampledDepth", renderer.downsampledDepthHandle);
            renderer.blendAndLightingMat.SetVector("_DepthHandleScale", new Vector4(1/renderer.downsampledDepthHandle.rtHandleProperties.rtHandleScale.x,1/renderer.downsampledDepthHandle.rtHandleProperties.rtHandleScale.y,1,1));
        #else 
            renderer.blendAndLightingMat.SetTexture("_DownsampledDepth", renderer.downsampledDepth);
        #endif
           
            Matrix4x4 camtowolrd = Matrix4x4.TRS(cam.transform.position, cam.transform.rotation, Vector3.one);
            renderer.blendAndLightingMat.SetMatrix("_CamToWorld",  camtowolrd);

            Color directLightColor;

            if(!EnviroManager.instance.isNight)
            {
                directLightColor = settingsGlobal.sunLightColorGradient.Evaluate(EnviroManager.instance.solarTime);
            }
            else
            { 
                directLightColor = settingsGlobal.moonLightColorGradient.Evaluate(EnviroManager.instance.lunarTime);
            }

            renderer.blendAndLightingMat.SetColor("_DirectLightColor", directLightColor);
            renderer.blendAndLightingMat.SetColor("_AmbientColor", settingsGlobal.ambientColorGradient.Evaluate(EnviroManager.instance.solarTime) * settingsGlobal.ambientLighIntensity);
            renderer.blendAndLightingMat.SetFloat("_AtmosphereColorSaturateDistance",settingsGlobal.atmosphereColorSaturateDistance);
            
            //We don't use reprojection pass for reflections.
        #if ENVIRO_HDRP
            if(cam.cameraType == CameraType.Reflection)
            {
               renderer.blendAndLightingMat.SetTexture("_CloudTex", renderer.undersampleBufferHandle);
               renderer.blendAndLightingMat.SetVector("_HandleScales", new Vector4(1 / renderer.undersampleBufferHandle.rtHandleProperties.rtHandleScale.x, 1 / renderer.undersampleBufferHandle.rtHandleProperties.rtHandleScale.y,1,1));
            }  
            else
            {
               renderer.blendAndLightingMat.SetTexture("_CloudTex", renderer.fullBufferHandles[renderer.fullBufferIndex ^ 1]);
               renderer.blendAndLightingMat.SetVector("_HandleScales", new Vector4(1/ renderer.fullBufferHandles[renderer.fullBufferIndex ^ 1].rtHandleProperties.rtHandleScale.x, 1/ renderer.fullBufferHandles[renderer.fullBufferIndex ^ 1].rtHandleProperties.rtHandleScale.y,1,1));
            }
        #else
            if(cam.cameraType == CameraType.Reflection)
            { 
               renderer.blendAndLightingMat.SetTexture("_CloudTex", renderer.undersampleBuffer);
            }   
            else
            {
              renderer.blendAndLightingMat.SetTexture("_CloudTex", renderer.fullBuffer[renderer.fullBufferIndex ^ 1]);
            } 
        #endif
        }

         private void SetDepthBlending(Material mat)
        {        
            if(settingsGlobal.depthBlending)
                mat.EnableKeyword("ENVIRO_DEPTH_BLENDING");
            else
                mat.DisableKeyword("ENVIRO_DEPTH_BLENDING");
        }

        private void SetToURP(Material mat)
        {        
            mat.EnableKeyword("ENVIROURP");
        }


        public bool CreateRenderTexture(ref RenderTexture texture, int width, int height, RenderTextureFormat format, FilterMode filterMode, RenderTextureDescriptor dsc)
        {
            if(texture != null && (texture.width != width || texture.height != height || texture.vrUsage != dsc.vrUsage))
            {
                DestroyImmediate(texture);
                texture = null;             
            }
            if(texture == null)
            {
                RenderTextureDescriptor d = dsc;
                d.width = width;
                d.height = height;
                d.colorFormat = format;
                d.depthBufferBits = 0;
                texture = new RenderTexture(d);
                texture.antiAliasing = 1;
                texture.useMipMap = false;
                texture.filterMode = filterMode;
                texture.Create();
                return true;
            }
            else
            {
                return false;
            }
        }
  
        public RenderTexture RenderWeatherMap()
        {
            if(weatherMapMat == null)
               weatherMapMat = new Material(Shader.Find("Enviro3/Standard/WeatherTexture"));

            if (weatherMap == null)
            {
                weatherMap = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGBFloat);
                weatherMap.wrapMode = TextureWrapMode.Repeat;                   
            } 
  
            weatherMapMat.SetFloat("_CoverageLayer1",settingsLayer1.coverage);    
            weatherMapMat.SetFloat("_WorleyFreq1Layer1", settingsLayer1.worleyFreq1);
            weatherMapMat.SetFloat("_WorleyFreq2Layer1",settingsLayer1.worleyFreq2);
            weatherMapMat.SetFloat("_DilateCoverageLayer1",settingsLayer1.dilateCoverage);
            weatherMapMat.SetFloat("_DilateTypeLayer1",settingsLayer1.dilateType);
            weatherMapMat.SetFloat("_CloudsTypeModifierLayer1",settingsLayer1.cloudsTypeModifier);

            if(settingsGlobal.dualLayer)
            {
                weatherMapMat.EnableKeyword("ENVIRO_DUAL_LAYER");
                weatherMapMat.SetFloat("_CoverageLayer2",settingsLayer2.coverage);
                weatherMapMat.SetFloat("_WorleyFreq1Layer2",settingsLayer2.worleyFreq1);
                weatherMapMat.SetFloat("_WorleyFreq2Layer2", settingsLayer2.worleyFreq2);
                weatherMapMat.SetFloat("_DilateCoverageLayer2",settingsLayer2.dilateCoverage);
                weatherMapMat.SetFloat("_DilateTypeLayer2",settingsLayer2.dilateType); 
                weatherMapMat.SetFloat("_CloudsTypeModifierLayer2",settingsLayer2.cloudsTypeModifier);
            }
            else
            {
                weatherMapMat.DisableKeyword("ENVIRO_DUAL_LAYER");
            }

            weatherMapMat.SetVector("_LocationOffset",new Vector4(settingsLayer1.locationOffset.x,settingsLayer1.locationOffset.y,settingsLayer2.locationOffset.x,settingsLayer2.locationOffset.y));
            weatherMapMat.SetVector("_WindDirectionLayer1", cloudAnimNonScaledLayer1);
            weatherMapMat.SetVector("_WindDirectionLayer2", cloudAnimNonScaledLayer2);
        
            Graphics.Blit(null,weatherMap,weatherMapMat);
            return weatherMap;
        }

        public RenderTexture RenderWeatherMapCS()
        {
            if(weatherMapMat == null)
               weatherMapMat = new Material(Shader.Find("Enviro3/Standard/WeatherMap"));

            if(weatherMapCS == null)
               weatherMapCS = (ComputeShader)Resources.Load("Shader/Clouds/EnviroWeatherMapCS");

            if (weatherMap == null)
            {
                weatherMap = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGBFloat);
                weatherMap.wrapMode = TextureWrapMode.Repeat;
                weatherMap.enableRandomWrite = true;                    
            } 

            weatherMap.enableRandomWrite = true;   
            weatherMapCS.SetFloat("_CoverageLayer1",settingsLayer1.coverage);    
            weatherMapCS.SetFloat("_WorleyFreq1Layer1", settingsLayer1.worleyFreq1);
            weatherMapCS.SetFloat("_WorleyFreq2Layer1",settingsLayer1.worleyFreq2);
            weatherMapCS.SetFloat("_DilateCoverageLayer1",settingsLayer1.dilateCoverage);
            weatherMapCS.SetFloat("_DilateTypeLayer1",settingsLayer1.dilateType);
            weatherMapCS.SetFloat("_CloudsTypeModifierLayer1",settingsLayer1.cloudsTypeModifier);

            if(settingsGlobal.dualLayer)
            { 
                weatherMapCS.SetFloat("_CoverageLayer2",settingsLayer2.coverage);
                weatherMapCS.SetFloat("_WorleyFreq1Layer2",settingsLayer2.worleyFreq1);
                weatherMapCS.SetFloat("_WorleyFreq2Layer2", settingsLayer2.worleyFreq2);
                weatherMapCS.SetFloat("_DilateCoverageLayer2",settingsLayer2.dilateCoverage);
                weatherMapCS.SetFloat("_DilateTypeLayer2",settingsLayer2.dilateType); 
                weatherMapCS.SetFloat("_CloudsTypeModifierLayer2",settingsLayer2.cloudsTypeModifier);
            }

            weatherMapCS.SetVector("_LocationOffset",new Vector4(settingsLayer1.locationOffset.x,settingsLayer1.locationOffset.y,settingsLayer2.locationOffset.x,settingsLayer2.locationOffset.y));
            
            weatherMapCS.SetTexture(0,"Result",weatherMap);
            weatherMapCS.Dispatch(0, 512/16, 512/16, 1);
            return weatherMap;
        } 

        private void UpdateWind()
        {      
            if(EnviroManager.instance.Environment != null)
            {
                cloudAnimLayer1 += new Vector3(
                (EnviroManager.instance.Environment.Settings.windSpeed * settingsLayer1.windSpeedModifier * EnviroManager.instance.Environment.Settings.windDirectionX * settingsLayer1.cloudsWindDirectionXModifier) * Time.deltaTime,
                (EnviroManager.instance.Environment.Settings.windSpeed * settingsLayer1.windSpeedModifier * EnviroManager.instance.Environment.Settings.windDirectionY * settingsLayer1.cloudsWindDirectionYModifier) * Time.deltaTime,
                (-1f * settingsLayer1.windUpwards * Time.deltaTime));
                            
                cloudAnimLayer1 = EnviroHelper.PingPong(cloudAnimLayer1);

                if(settingsGlobal.dualLayer)
                {       
                    cloudAnimLayer2 += new Vector3(
                    (EnviroManager.instance.Environment.Settings.windSpeed * settingsLayer2.windSpeedModifier * EnviroManager.instance.Environment.Settings.windDirectionX * settingsLayer2.cloudsWindDirectionXModifier) * Time.deltaTime,
                    (EnviroManager.instance.Environment.Settings.windSpeed * settingsLayer2.windSpeedModifier * EnviroManager.instance.Environment.Settings.windDirectionY * settingsLayer2.cloudsWindDirectionYModifier) * Time.deltaTime,
                    (-1f * settingsLayer2.windUpwards * Time.deltaTime));
                                    
                    cloudAnimLayer2 = EnviroHelper.PingPong(cloudAnimLayer2);
                }
                cloudAnimNonScaledLayer1 += new Vector3((settingsLayer1.windSpeedModifier * EnviroManager.instance.Environment.Settings.windSpeed * EnviroManager.instance.Environment.Settings.windDirectionX * settingsLayer1.cloudsWindDirectionXModifier) * Time.deltaTime * 4f, (settingsLayer1.windSpeedModifier * EnviroManager.instance.Environment.Settings.windSpeed * EnviroManager.instance.Environment.Settings.windDirectionY * settingsLayer1.cloudsWindDirectionYModifier) * Time.deltaTime* 4f, -1f * EnviroManager.instance.Environment.Settings.windSpeed * Time.deltaTime ) * settingsGlobal.cloudsTravelSpeed * 0.2f;
                cloudAnimNonScaledLayer2 += new Vector3((settingsLayer2.windSpeedModifier * EnviroManager.instance.Environment.Settings.windSpeed * EnviroManager.instance.Environment.Settings.windDirectionX  * settingsLayer2.cloudsWindDirectionXModifier) * Time.deltaTime* 4f, (settingsLayer2.windSpeedModifier * EnviroManager.instance.Environment.Settings.windSpeed * EnviroManager.instance.Environment.Settings.windDirectionY * settingsLayer2.cloudsWindDirectionYModifier) * Time.deltaTime* 4f, -1f * EnviroManager.instance.Environment.Settings.windSpeed * Time.deltaTime) * settingsGlobal.cloudsTravelSpeed * 0.2f;
            }
            else 
            {
                cloudAnimLayer1 += new Vector3(
                (settingsLayer1.windSpeedModifier * settingsLayer1.cloudsWindDirectionXModifier) * Time.deltaTime,
                (settingsLayer1.windSpeedModifier * settingsLayer1.cloudsWindDirectionYModifier) * Time.deltaTime,
                (-1f * settingsLayer1.windUpwards * Time.deltaTime));
                            
                cloudAnimLayer1 = EnviroHelper.PingPong(cloudAnimLayer1);

                if(settingsGlobal.dualLayer)
                {       
                    cloudAnimLayer2 += new Vector3(
                    (settingsLayer2.windSpeedModifier * settingsLayer2.cloudsWindDirectionXModifier) * Time.deltaTime,
                    (settingsLayer2.windSpeedModifier * settingsLayer2.cloudsWindDirectionYModifier) * Time.deltaTime,
                    (-1f * settingsLayer2.windUpwards * Time.deltaTime));
                                    
                    cloudAnimLayer2 = EnviroHelper.PingPong(cloudAnimLayer2);
                } 

                cloudAnimNonScaledLayer1 += new Vector3((settingsLayer1.windSpeedModifier * settingsLayer1.cloudsWindDirectionXModifier) * Time.deltaTime* 4f, (settingsLayer1.windSpeedModifier * settingsLayer1.cloudsWindDirectionYModifier) * Time.deltaTime* 4f, -1f * settingsLayer1.windUpwards * Time.deltaTime) * settingsGlobal.cloudsTravelSpeed * 0.2f;
                cloudAnimNonScaledLayer2 += new Vector3((settingsLayer2.windSpeedModifier * settingsLayer2.cloudsWindDirectionXModifier) * Time.deltaTime* 4f, (settingsLayer2.windSpeedModifier * settingsLayer2.cloudsWindDirectionYModifier) * Time.deltaTime* 4f, -1f * settingsLayer2.windUpwards * Time.deltaTime) * settingsGlobal.cloudsTravelSpeed * 0.2f;
            }
        
        }

        //Save and Load
        public void LoadModuleValues ()
        {
            if(preset != null)
            { 
                settingsLayer1 = JsonUtility.FromJson<Enviro.EnviroCloudLayerSettings>(JsonUtility.ToJson(preset.settingsLayer1));
                settingsLayer2 = JsonUtility.FromJson<Enviro.EnviroCloudLayerSettings>(JsonUtility.ToJson(preset.settingsLayer2));
                settingsGlobal = JsonUtility.FromJson<Enviro.EnviroCloudGlobalSettings>(JsonUtility.ToJson(preset.settingsGlobal));
            }
            else
            {
                Debug.Log("Please assign a saved module to load from!");
            }
        }

        public void SaveModuleValues ()
        {
#if UNITY_EDITOR
        EnviroVolumetricCloudsModule t =  ScriptableObject.CreateInstance<EnviroVolumetricCloudsModule>();
        t.name = "Cloud Module";
        t.settingsLayer1 = JsonUtility.FromJson<Enviro.EnviroCloudLayerSettings>(JsonUtility.ToJson(settingsLayer1));
        t.settingsLayer2 = JsonUtility.FromJson<Enviro.EnviroCloudLayerSettings>(JsonUtility.ToJson(settingsLayer2));
        t.settingsGlobal = JsonUtility.FromJson<Enviro.EnviroCloudGlobalSettings>(JsonUtility.ToJson(settingsGlobal));

        string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(EnviroHelper.assetPath + "/New " + t.name + ".asset");
        UnityEditor.AssetDatabase.CreateAsset(t, assetPathAndName);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public void SaveModuleValues (EnviroVolumetricCloudsModule module)
        {
            module.settingsLayer1 = JsonUtility.FromJson<Enviro.EnviroCloudLayerSettings>(JsonUtility.ToJson(settingsLayer1));
            module.settingsLayer2 = JsonUtility.FromJson<Enviro.EnviroCloudLayerSettings>(JsonUtility.ToJson(settingsLayer2));
            module.settingsGlobal = JsonUtility.FromJson<Enviro.EnviroCloudGlobalSettings>(JsonUtility.ToJson(settingsGlobal));
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(module);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }
    }
}