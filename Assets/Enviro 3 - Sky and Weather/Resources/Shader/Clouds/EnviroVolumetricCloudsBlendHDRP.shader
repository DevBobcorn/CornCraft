Shader "Hidden/EnviroVolumetricCloudsBlendHDRP"
{
    Properties 
    {
        //_MainTex ("Texture", any) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

      	Pass
        {
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
            #pragma vertex vert 
            #pragma fragment frag
            #pragma multi_compile __ ENVIRO_DEPTH_BLENDING
  
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"
            //#include "../Includes/SkyIncludeHLSL.hlsl"
            #include "../Includes/FogIncludeHLSL.hlsl"

            TEXTURE2D_X(_MainTex);
            TEXTURE2D(_DownsampledDepth);

            #ifdef STEREO_INSTANCING_ON
                TEXTURE2D_X (_CloudTex);
                SAMPLER(sampler_CloudTex);
            #else
                TEXTURE2D (_CloudTex);
                SAMPLER(sampler_CloudTex);
            #endif 
      
            SamplerState Point_Clamp_Sampler;

            float4 _CloudTex_TexelSize;  
            float4 _MainTex_TexelSize; 
            float4 _HandleScales; 
            float4 _DepthHandleScale;
            float4 _ProjectionExtents;
            float4 _ProjectionExtentsRight;
            float3 _AmbientColor;
            float3 _DirectLightColor;
            float _AtmosphereColorSaturateDistance;
            float4x4 _CamToWorld;
            float3 color;
	        float3 opacity;
            float _EnviroSkyIntensity;
            
            struct appdata
            {
                uint vertexID : SV_VertexID;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;        
                float2 vsray : TEXCOORD1;
//#ifdef ENVIRO_DEPTH_BLENDING
                float2 uv00 : TEXCOORD2; 
                float2 uv10 : TEXCOORD3;
                float2 uv01 : TEXCOORD4;
                float2 uv11 : TEXCOORD5;
//#endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;          
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
                o.uv = GetFullScreenTriangleTexCoord(v.vertexID);

                if(unity_StereoEyeIndex == 0) 
                    o.vsray = (2.0 * o.uv * (1/_RTHandleScale.xy) - 1.0) * _ProjectionExtents.xy + _ProjectionExtents.zw;
                else
                    o.vsray = (2.0 * o.uv * (1/_RTHandleScale.xy) - 1.0) * _ProjectionExtentsRight.xy + _ProjectionExtentsRight.zw;

     
//#ifdef ENVIRO_DEPTH_BLENDING
                o.uv00 = o.uv - 0.5 * _CloudTex_TexelSize.xy;
                o.uv10 = o.uv00 + float2(_CloudTex_TexelSize.x, 0.0);
                o.uv01 = o.uv00 + float2(0.0, _CloudTex_TexelSize.y);
                o.uv11 = o.uv00 + _CloudTex_TexelSize.xy;
//#endif
                return o; 
            }

//#ifdef ENVIRO_DEPTH_BLENDING
            float4 Upsample(v2f i) 
            { 
                float4 lowResDepth = 0.0f;
                float highResDepth = LinearEyeDepth(LOAD_TEXTURE2D_X_LOD(_CameraDepthTexture, i.uv * _ScreenSize.xy * (1/_RTHandleScale.xy), 0), _ZBufferParams);

                lowResDepth.x = LinearEyeDepth(LOAD_TEXTURE2D(_DownsampledDepth,i.uv00 * _ScreenSize.xy * _DepthHandleScale.xy).r, _ZBufferParams);
                lowResDepth.y = LinearEyeDepth(LOAD_TEXTURE2D(_DownsampledDepth,i.uv10 * _ScreenSize.xy * _DepthHandleScale.xy).r, _ZBufferParams);
                lowResDepth.z = LinearEyeDepth(LOAD_TEXTURE2D(_DownsampledDepth,i.uv01 * _ScreenSize.xy * _DepthHandleScale.xy).r, _ZBufferParams);
                lowResDepth.w = LinearEyeDepth(LOAD_TEXTURE2D(_DownsampledDepth,i.uv11 * _ScreenSize.xy * _DepthHandleScale.xy).r, _ZBufferParams); 

                float4 depthDiff = abs(lowResDepth - highResDepth);
                float accumDiff = dot(depthDiff, float4(1, 1, 1, 1));

                [branch]
                if (accumDiff < 1.5f)
                {
#ifdef STEREO_INSTANCING_ON
                float3 uv = float3(i.uv,unity_StereoEyeIndex);
                return _CloudTex.Sample(sampler_CloudTex, uv * _HandleScales.xy);
#else
                return _CloudTex.Sample(sampler_CloudTex, i.uv * _HandleScales.xy);
#endif  
                }
                else
                {
                    float minDepthDiff = depthDiff[0];
                    float2 nearestUv = i.uv00;

                    if (depthDiff[1] < minDepthDiff)
                    { 
                        nearestUv = i.uv10;
                        minDepthDiff = depthDiff[1];
                    }

                    if (depthDiff[2] < minDepthDiff)
                    {
                        nearestUv = i.uv01;
                        minDepthDiff = depthDiff[2];
                    }

                    if (depthDiff[3] < minDepthDiff)
                    {
                        nearestUv = i.uv11;
                        minDepthDiff = depthDiff[3];
                    }

#ifdef STEREO_INSTANCING_ON
                float3 uv = float3(nearestUv,unity_StereoEyeIndex);
                return _CloudTex.Sample(Point_Clamp_Sampler, uv * _HandleScales.xy);
#else
                return _CloudTex.Sample(Point_Clamp_Sampler, nearestUv * _HandleScales.xy); 
#endif
                }
            }
//#endif
  

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                float4 vspos = float4(i.vsray, 1.0, 1.0);

                float4 worldPos = mul(_CamToWorld,vspos); 

                float3 viewDir = normalize(worldPos.xyz - _WorldSpaceCameraPos);

                float4 sourceColor = SAMPLE_TEXTURE2D_X_LOD(_MainTex,s_trilinear_clamp_sampler, i.uv, 0);
                 
//#ifdef ENVIRO_DEPTH_BLENDING
                float4 cloudsColor = Upsample(i);
//#else

 //   #ifdef STEREO_INSTANCING_ON
 //               float3 uv = float3(i.uv * _HandleScales.xy ,unity_StereoEyeIndex);
 //               float4 cloudsColor = _CloudTex.Sample(sampler_CloudTex, uv);
 //   #else
 //               float4 cloudsColor = _CloudTex.Sample(sampler_CloudTex, i.uv * _HandleScales.xy);             
 //   #endif 

//#endif              
                float3 sunColor = pow(_DirectLightColor.rgb,2) * 2.0f; 
                float3 skyColor = GetSkyColor(viewDir,0.005f) * _EnviroSkyIntensity * GetCurrentExposureMultiplier();
                float4 finalColor = float4(cloudsColor.r * sunColor + _AmbientColor, cloudsColor.a);

                float atmosphericBlendFactor = saturate(exp(-cloudsColor.g / _AtmosphereColorSaturateDistance));

                //if(_WorldSpaceCameraPos.y <= 2000) 
                   finalColor.rgb = lerp(skyColor, finalColor.rgb, atmosphericBlendFactor); 
 
                float rawDepth = LOAD_TEXTURE2D_X_LOD(_CameraDepthTexture, i.uv * _ScreenSize.xy * (1/_RTHandleScale.xy), 0);
                float sceneDepth = Linear01Depth(rawDepth, _ZBufferParams);

#if ENVIRO_DEPTH_BLENDING
                float4 final = float4(sourceColor.rgb * (1 - finalColor.a) + finalColor.rgb * finalColor.a, 1);
#else 
                float4 final = sourceColor;

                if (sceneDepth == 1.0f) 
                    final = half4(sourceColor.rgb * saturate(1 - finalColor.a) + finalColor.rgb * finalColor.a, 1); 
#endif       
                // HDRP Fog 
                if (sceneDepth == 1.0f)
                {             
                    PositionInputs posInput = GetPositionInput(i.vertex.xy, _ScreenSize.zw, rawDepth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
                    float3 V = GetSkyViewDirWS(i.uv * _ScreenSize.xy * (1/_RTHandleScale.xy)); 
                    posInput.positionWS = GetCurrentViewPosition() - V * _MaxFogDistance;
                    EvaluateAtmosphericScattering(posInput, V, color, opacity);
                    final.rgb = color + (1 - opacity) * final.rgb;
                }
                return final;

            }
        ENDHLSL
        }
    }
}
