Shader "Hidden/EnviroVolumetricCloudsBlend"
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

            CGPROGRAM
            #pragma vertex vert 
            #pragma fragment frag
            #pragma multi_compile __ ENVIRO_DEPTH_BLENDING
            #pragma multi_compile __ ENVIROURP
            #pragma multi_compile __ UNITY_COLORSPACE_GAMMA
  
            #include "UnityCG.cginc"
            //#include "../Includes/SkyInclude.cginc"
            #include "../Includes/FogInclude.cginc"

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);

            #ifdef STEREO_INSTANCING_ON
                UNITY_DECLARE_TEX2DARRAY (_CloudTex);
            #else
                UNITY_DECLARE_TEX2D (_CloudTex); 
            #endif

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_DownsampledDepth);
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture); 

            float4 _CloudTex_TexelSize;  
            float4 _MainTex_TexelSize; 

            SamplerState Point_Clamp_Sampler;

            float4 _ProjectionExtents;
            float4 _ProjectionExtentsRight;

            float3 _AmbientColor;
            float3 _DirectLightColor;
            float _AtmosphereColorSaturateDistance;

            float4x4 _CamToWorld;
            

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
                float4 screenPos : TEXCOORD1;         
                float2 vsray : TEXCOORD2;
                half3 pos : TEXCOORD3;
//#ifdef ENVIRO_DEPTH_BLENDING
                float2 uv00 : TEXCOORD4; 
                float2 uv10 : TEXCOORD5;
                float2 uv01 : TEXCOORD6;
                float2 uv11 : TEXCOORD7;
//#endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_img v)
            {
                v2f o;
               
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = -v.vertex; 
                
                #if defined(ENVIROURP)
		        o.vertex = float4(v.vertex.xyz,1.0);
		        #if UNITY_UV_STARTS_AT_TOP
                o.vertex.y *= -1;
                #endif
                #else
		        o.vertex = UnityObjectToClipPos(v.vertex);
                #endif    

                o.uv = v.texcoord;

                if(unity_StereoEyeIndex == 0) 
                    o.vsray = (2.0 * v.texcoord - 1.0) * _ProjectionExtents.xy + _ProjectionExtents.zw;
                else
                    o.vsray = (2.0 * v.texcoord - 1.0) * _ProjectionExtentsRight.xy + _ProjectionExtentsRight.zw;

                o.screenPos = ComputeScreenPos(o.vertex);
     
//#ifdef ENVIRO_DEPTH_BLENDING
                o.uv00 = v.texcoord - 0.5 * _CloudTex_TexelSize.xy;
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
                float highResDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,UnityStereoTransformScreenSpaceTex(i.uv)));

                lowResDepth.x = LinearEyeDepth(UNITY_SAMPLE_SCREENSPACE_TEXTURE(_DownsampledDepth,UnityStereoTransformScreenSpaceTex(i.uv00)).r);
                lowResDepth.y = LinearEyeDepth(UNITY_SAMPLE_SCREENSPACE_TEXTURE(_DownsampledDepth,UnityStereoTransformScreenSpaceTex(i.uv10)).r);
                lowResDepth.z = LinearEyeDepth(UNITY_SAMPLE_SCREENSPACE_TEXTURE(_DownsampledDepth,UnityStereoTransformScreenSpaceTex(i.uv01)).r);
                lowResDepth.w = LinearEyeDepth(UNITY_SAMPLE_SCREENSPACE_TEXTURE(_DownsampledDepth,UnityStereoTransformScreenSpaceTex(i.uv11)).r); 

                float4 depthDiff = abs(lowResDepth - highResDepth);
                float accumDiff = dot(depthDiff, float4(1, 1, 1, 1));

                [branch]
                if (accumDiff < 1.5f)
                {
#ifdef STEREO_INSTANCING_ON
                float3 uv = float3(UnityStereoTransformScreenSpaceTex(i.uv),unity_StereoEyeIndex);
                return _CloudTex.Sample(sampler_CloudTex, uv);
#else
                return _CloudTex.Sample(sampler_CloudTex, UnityStereoTransformScreenSpaceTex(i.uv)); 
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
                float3 uv = float3(UnityStereoTransformScreenSpaceTex(nearestUv),unity_StereoEyeIndex);
                return _CloudTex.Sample(Point_Clamp_Sampler, uv);
#else
                return _CloudTex.Sample(Point_Clamp_Sampler, UnityStereoTransformScreenSpaceTex(nearestUv)); 
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

                float4 sourceColor = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex,UnityStereoTransformScreenSpaceTex(i.uv));
                 
//#ifdef ENVIRO_DEPTH_BLENDING
                float4 cloudsColor = Upsample(i);
//#else

 //   #ifdef STEREO_INSTANCING_ON
  //              float3 uv = float3(UnityStereoTransformScreenSpaceTex(i.uv),unity_StereoEyeIndex);
   //             float4 cloudsColor = _CloudTex.Sample(sampler_CloudTex, uv);
  //  #else
 //               float4 cloudsColor = _CloudTex.Sample(sampler_CloudTex, UnityStereoTransformScreenSpaceTex(i.uv)); 
  //  #endif
//#endif             
                float3 sunColor = pow(_DirectLightColor.rgb,2) * 2.0f;
                float3 skyColor = GetSkyColor(viewDir,0.005f);
                float4 finalColor = float4(cloudsColor.r * sunColor + _AmbientColor, cloudsColor.a);

                float atmosphericBlendFactor = saturate(exp(-cloudsColor.g / _AtmosphereColorSaturateDistance));

                //if(_WorldSpaceCameraPos.y <= 2000) 
                   finalColor.rgb = lerp(skyColor, finalColor.rgb, atmosphericBlendFactor); 

                #if defined(UNITY_COLORSPACE_GAMMA)
				finalColor.rgb = LinearToGammaSpace(finalColor.rgb);
			    #endif

#if ENVIRO_DEPTH_BLENDING
                float4 final = float4(sourceColor.rgb * (1 - finalColor.a) + finalColor.rgb * finalColor.a, 1);
                return final;
#else
                float4 final = sourceColor; 

                float sceneDepth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(i.uv)));
                
                if (sceneDepth >= 0.99f) 
                    final = float4(sourceColor.rgb * saturate(1 - finalColor.a) + finalColor.rgb * finalColor.a, 1);
 
                return final;  
#endif
            }
        ENDCG
        }
    }
}
