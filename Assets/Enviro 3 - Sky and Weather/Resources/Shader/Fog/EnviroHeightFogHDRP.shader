Shader "Hidden/EnviroHeightFogHDRP"
{
    Properties
    {
        //_MainTex ("Texture", any) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass 
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
 
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"
            //#include "../Includes/SkyIncludeHLSL.hlsl"
            #include "../Includes/FogIncludeHLSL.hlsl"

            struct appdata
            {
                uint vertexID : SV_VertexID;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 position : SV_POSITION;
                float3 ray : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v); 
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.position = GetFullScreenTriangleVertexPosition(v.vertexID);
                o.uv = GetFullScreenTriangleTexCoord(v.vertexID);

                return o;
            }

            float4x4 _LeftWorldFromView;
            float4x4 _RightWorldFromView;
            float4x4 _LeftViewFromScreen;
            float4x4 _RightViewFromScreen;

            float3 color, opacity;

            float _EnviroSkyIntensity;

            TEXTURE2D_X(_MainTex);

            void InverseProjectDepth (float depth, float2 texcoord, out float3 worldPos, out float dist, out float3 viewDir)
            {
                float4x4 proj, eyeToWorld;

                if (unity_StereoEyeIndex == 0)
                {
                    proj 		= _LeftViewFromScreen;
                    eyeToWorld 	= _LeftWorldFromView;
                }
                else
                {
                    proj 		= _RightViewFromScreen;
                    eyeToWorld 	= _RightWorldFromView;
                }

                #if !UNITY_UV_STARTS_AT_TOP
                    texcoord.y = 1 - texcoord.y;
                #endif
                
                float2 uvClip = texcoord * 2.0 - 1.0;
                float clipDepth = depth; // Fix for OpenGl Core thanks to Lars Bertram
				clipDepth = (UNITY_NEAR_CLIP_VALUE < 0) ? clipDepth * 2 - 1 : clipDepth;
                float4 clipPos = float4(uvClip, clipDepth, 1.0);
                float4 viewPos = mul(proj, clipPos); // inverse projection by clip position
                viewPos /= viewPos.w; // perspective division
                worldPos = mul(eyeToWorld, viewPos).xyz;
                viewDir = worldPos - _WorldSpaceCameraPos.xyz;
                dist = length(viewDir);
                viewDir /= dist;
            } 


            float4 frag (v2f i) : SV_Target
            { 
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float depth = LOAD_TEXTURE2D_X_LOD(_CameraDepthTexture, i.uv * _ScreenSize.xy * (1/_RTHandleScale.xy), 0).r;
                float linearDepth = Linear01Depth(depth, _ZBufferParams);

                float viewDistance; 
                float3 worldPos, viewDir; 
                InverseProjectDepth(depth, i.uv.xy * (1/_RTHandleScale.xy), worldPos, viewDistance, viewDir);

                float4 fog = GetExponentialHeightFog(worldPos,linearDepth);

                //HDRP Fog            
                //float3 V = GetSkyViewDirWS(i.uv.xy * _ScreenSize.xy * (1/_RTHandleScale.xy));
                //PositionInputs posInput = GetPositionInput(i.position.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
                //posInput.positionWS = GetCurrentViewPosition() - V * _MaxFogDistance;
                //EvaluateAtmosphericScattering(posInput, V, color, opacity);
                //fog.rgb = color + (1 - opacity) * fog.rgb;
          
                
                float4 col = SAMPLE_TEXTURE2D_X_LOD(_MainTex,s_trilinear_clamp_sampler, i.uv, 0);              

                //float4 volumetrics = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_EnviroVolumetricFogTex, i.uv);  
                //col.rgb = col.rgb * fog.a + fog.rgb * max(volumetrics.rgb,0.75);

                col.rgb = col.rgb * fog.a + fog.rgb;          
                return col;
            }
            ENDHLSL
        }
    }
}
