Shader "Hidden/EnviroVolumetricCloudsReproject"
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ ENVIRO_DEPTH_BLENDING
            #pragma multi_compile _ ENVIROURP

            #include "UnityCG.cginc"
            
            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
            float4 _MainTex_TexelSize;

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_UndersampleCloudTex);
            float4 _UndersampleCloudTex_TexelSize;

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_DownsampledDepth);

            float4x4 _PrevVP;
            float4x4 _CamToWorld;

            float4 _ProjectionExtents;
            float4 _ProjectionExtentsRight;

            float2 _TexelSize;
            float _BlendTime;
  

            struct appdata 
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 ray : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
 
            v2f vert(appdata v) 
            { 
                v2f o; 
                UNITY_SETUP_INSTANCE_ID(v); 
		        UNITY_INITIALIZE_OUTPUT(v2f, o); 
		        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);      

                #if defined(ENVIROURP)
		        o.vertex = float4(v.vertex.xyz,1.0);
		        #if UNITY_UV_STARTS_AT_TOP
                o.vertex.y *= -1;
                #endif 
                #else
		        o.vertex = UnityObjectToClipPos(v.vertex);
                #endif   
                o.uv = v.uv;

                if(unity_StereoEyeIndex == 0) 
                    o.ray = (2.0 * v.uv - 1.0) * _ProjectionExtents.xy + _ProjectionExtents.zw;
                else
                    o.ray = (2.0 * v.uv - 1.0) * _ProjectionExtentsRight.xy + _ProjectionExtentsRight.zw;

                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            } 


            float2 PrevUV(float4 wspos, out half outOfBound) 
            {
                float4x4 prev = mul(unity_CameraProjection,_PrevVP);
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
       
                float4 raymarchResult = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_UndersampleCloudTex,UnityStereoTransformScreenSpaceTex(i.uv));

                float distance = raymarchResult.y;		
                float intensity = raymarchResult.x;
                half outOfBound;

                float4 worldPos = mul(_CamToWorld, float4(normalize(vspos) * distance, 1.0));        
                worldPos /= worldPos.w;  

                float2 prevUV = PrevUV(worldPos, outOfBound);          
                {	 
                    float4 prevSample = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, UnityStereoTransformScreenSpaceTex(prevUV));
 
                    float4 m1 = float4(0.0f,0.0f,0.0f,0.0f); 
                    float4 m2 = float4(0.0f,0.0f,0.0f,0.0f);
                    
                    float sampleCount = 1.0f;

                    float originalPointDepth = LinearEyeDepth(UNITY_SAMPLE_SCREENSPACE_TEXTURE(_DownsampledDepth, UnityStereoTransformScreenSpaceTex(i.uv)));
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
                                val = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_UndersampleCloudTex, UnityStereoTransformScreenSpaceTex(uv));
                                float depth = LinearEyeDepth(UNITY_SAMPLE_SCREENSPACE_TEXTURE(_DownsampledDepth,UnityStereoTransformScreenSpaceTex(uv)));

                                if (abs(originalPointDepth - depth < 1.5f)) 
                                {
                                    m1 += val;
                                    m2 += val * val;
                                    sampleCount += 1.0f;
                                }
                            }
                        }
                    }

                    float gamma = _BlendTime;
                    float4 mu = m1 / sampleCount;
                    float4 sigma = sqrt(abs(m2 / sampleCount - mu * mu));
                    float4 minc = mu - gamma * sigma;
                    float4 maxc = mu + gamma * sigma;
                    prevSample = ClipAABB(minc, maxc, prevSample);	
  
                    //Blend.
                    raymarchResult = lerp(prevSample, raymarchResult, max(0.01f, outOfBound));
                   
                }
                return raymarchResult;
            }
            ENDCG
		}
    }
}
