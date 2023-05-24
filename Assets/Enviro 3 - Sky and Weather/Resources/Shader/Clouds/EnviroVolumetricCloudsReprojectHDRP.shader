Shader "Hidden/EnviroVolumetricCloudsReprojectHDRP"
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
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ ENVIRO_DEPTH_BLENDING
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"
            
            TEXTURE2D(_MainTex);
            float4 _MainTex_TexelSize;

            TEXTURE2D(_UndersampleCloudTex);
            float4 _UndersampleCloudTex_TexelSize;

            TEXTURE2D(_DownsampledDepth);

            float4x4 _PrevVP;
            float4x4 _CamToWorld;

            float4 _ProjectionExtents;
            float4 _ProjectionExtentsRight;

            float2 _TexelSize;
            float _BlendTime;
  

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
                float2 ray : TEXCOORD1;
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
                    o.ray = (2.0 * o.uv - 1.0) * _ProjectionExtents.xy + _ProjectionExtents.zw;
                else
                    o.ray = (2.0 * o.uv - 1.0) * _ProjectionExtentsRight.xy + _ProjectionExtentsRight.zw;

                return o;
            } 


            float2 PrevUV(float4 wspos, out half outOfBound) 
            {
                float4x4 prev = mul(UNITY_MATRIX_P,_PrevVP);
                float4 prevUV = mul(prev, wspos);

                prevUV.xy = 0.5 * (prevUV.xy / prevUV.w) + 0.5;

                half oobmax = max(0.0 - prevUV.x, 0.0 - prevUV.y);
                half oobmin = max(prevUV.x - 1.0, prevUV.y - 1.0);

                outOfBound = step(0, max(oobmin, oobmax));
                
                return prevUV;
            }

            float4 ClipAABB(float4 aabbMin, float4 aabbMax, float4 prevSample)
            {
                float4 p_clip = 0.5 * (aabbMax + aabbMin);
                float4 e_clip = 0.5 * (aabbMax - aabbMin);

                float4 v_clip = prevSample - p_clip;
                float4 v_unit = v_clip / e_clip;
                float4 a_unit = abs(v_unit); 
                float ma_unit = max(max(a_unit.x, max(a_unit.y, a_unit.z)), a_unit.w);

                if (ma_unit > 1.0)
                    return p_clip + v_clip / ma_unit;
                else
                    return prevSample;
            }

        
            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float3 vspos = float3(i.ray, 1.0);
       
                float4 raymarchResult = LOAD_TEXTURE2D(_UndersampleCloudTex, i.uv * _ScreenSize.xy);
 
                float distance = raymarchResult.y;		
                float intensity = raymarchResult.x;
                half outOfBound;

                float4 worldPos = mul(_CamToWorld, float4(normalize(vspos) * distance, 1.0));        
                worldPos /= worldPos.w;  

                float2 prevUV = PrevUV(worldPos, outOfBound);          
                {	 
                    float4 prevSample = LOAD_TEXTURE2D(_MainTex, prevUV * _ScreenSize.xy);
 
                    float4 m1 = float4(0.0f,0.0f,0.0f,0.0f); 
                    float4 m2 = float4(0.0f,0.0f,0.0f,0.0f);
                    
                    float sampleCount = 1.0f;

#if ENVIRO_DEPTH_BLENDING
                    float originalPointDepth = LinearEyeDepth(LOAD_TEXTURE2D(_DownsampledDepth, i.uv * _ScreenSize.xy), _ZBufferParams);
#endif
                    [unroll]
                    for (int x = -1; x <= 1; x ++) 
                    {
                        [unroll]
                        for (int y = -1; y <= 1; y ++ ) 
                        {
                            float4 val;
                            if (x == 0 && y == 0) 
                            {
                                val = raymarchResult;
                                m1 += val;
                                m2 += val * val;
                            }
                            else 
                            {
                                float2 uv = i.uv + float2(x * _UndersampleCloudTex_TexelSize.x, y * _UndersampleCloudTex_TexelSize.y);
                                val = LOAD_TEXTURE2D(_UndersampleCloudTex, uv * _ScreenSize.xy);
#if ENVIRO_DEPTH_BLENDING
                                float depth = LinearEyeDepth(LOAD_TEXTURE2D(_DownsampledDepth,uv * _ScreenSize.xy), _ZBufferParams);

                                if (abs(originalPointDepth - depth < 1.5f)) 
                                {
                                    m1 += val;
                                    m2 += val * val;
                                    sampleCount += 1.0f;
                                }
#else
                                m1 += val;
                                m2 += val * val;
                                sampleCount += 1.0f;
#endif
                            }
                        }
                    }

                    float gamma = _BlendTime;
                    float4 mu = m1 / sampleCount;
                    float4 sigma = sqrt(abs(m2 / sampleCount - mu * mu));
                    float4 minc = mu - gamma * sigma;
                    float4 maxc = mu + gamma * sigma;


                    prevSample = ClipAABB(minc, maxc, prevSample);	
  
                    //Blend
                    raymarchResult = lerp(prevSample, raymarchResult, max(0.01f, outOfBound));
                    //raymarchResult = prevSample;
                    
                } 
                return raymarchResult;
            }
            ENDHLSL
		}
    }
}
