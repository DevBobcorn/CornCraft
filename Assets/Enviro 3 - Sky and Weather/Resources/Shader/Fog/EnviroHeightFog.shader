Shader "Hidden/EnviroHeightFog"
{
    Properties
    {
        //_MainTex ("Texture", any) = "white"  {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ ENVIROURP
            #pragma multi_compile __ UNITY_COLORSPACE_GAMMA

            #include "UnityCG.cginc"
            #include "../Includes/FogInclude.cginc"
  
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 position : SV_POSITION;
                float3 ray : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata_img v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v); 
                UNITY_INITIALIZE_OUTPUT(v2f, o); 
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                #if defined(ENVIROURP)
		        o.position = float4(v.vertex.xyz,1.0);
		        #if UNITY_UV_STARTS_AT_TOP
                o.position.y *= -1;
                #endif
                #else
		        o.position = UnityObjectToClipPos(v.vertex);
                #endif 

                o.uv = v.texcoord; 

                return o;
            }

            float4x4 _LeftWorldFromView;
            float4x4 _RightWorldFromView;
            float4x4 _LeftViewFromScreen;
            float4x4 _RightViewFromScreen;

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

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
                    //texcoord.y = 1 - texcoord.y;
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
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float linearDepth = Linear01Depth(depth);

                float viewDistance;
                float3 worldPos, viewDir;
                InverseProjectDepth(depth, i.uv.xy, worldPos, viewDistance, viewDir);
 
                float4 col = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv); 
                float4 fog = GetExponentialHeightFog(worldPos,linearDepth);
                //this is not correct but LinearToGamma does produce even worse results.. 
                #if defined(UNITY_COLORSPACE_GAMMA) 
                fog.rgb *= 1.5;
                #endif

                float3 final = ApplyVolumetricLights(fog,col.rgb, i.uv);

                return float4(final.rgb,1);
            }
            ENDCG
        }
    }
}
