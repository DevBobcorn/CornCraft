Shader "Hidden/EnviroApplyShadows"
{
    Properties
    {
        //_MainTex ("Texture", any) = "white" {}
        //_CloudsTex ("Texture", any) = "white" {}
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

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
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
                return o;
            }

            float _Intensity;
            //float4x4 _LeftWorldFromView;
            //float4x4 _RightWorldFromView;
            //float4x4 _LeftViewFromScreen;
            //float4x4 _RightViewFromScreen;

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
            UNITY_DECLARE_SCREENSPACE_TEXTURE(_CloudsTex);
            //UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture); 
            //UNITY_DECLARE_SHADOWMAP(_CascadeShadowMapTexture);


            /*float4 GetCascadeWeights_SplitSpheres(float3 wpos)
            {
                float3 fromCenter0 = wpos - unity_ShadowSplitSpheres[0].xyz;
                float3 fromCenter1 = wpos - unity_ShadowSplitSpheres[1].xyz;
                float3 fromCenter2 = wpos - unity_ShadowSplitSpheres[2].xyz;
                float3 fromCenter3 = wpos - unity_ShadowSplitSpheres[3].xyz;
                float4 distances2 = float4(dot(fromCenter0,fromCenter0), dot(fromCenter1,fromCenter1), dot(fromCenter2,fromCenter2), dot(fromCenter3,fromCenter3));
                float4 weights = float4(distances2 >= unity_ShadowSplitSqRadii);
                return weights;
            }

            float4 GetCascadeShadowCoord(float4 pos, float4 cascadeWeights)
            {
                return mul(unity_WorldToShadow[(int)dot(cascadeWeights, float4(1,1,1,1))], pos);
            }*/



            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 sceneColor = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex,i.uv);
                float cloudShadows = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CloudsTex,i.uv).b * _Intensity;
                //float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,i.uv);
                //float linearDepth = LinearEyeDepth(depth); 

                /*
                float4x4 proj, eyeToWorld;
				if (unity_StereoEyeIndex == 0)
				{
					proj = _LeftViewFromScreen;
					eyeToWorld = _LeftWorldFromView;
				}
				else
				{
					proj = _RightViewFromScreen;
					eyeToWorld = _RightWorldFromView;
				}

				//bit of matrix math to take the screen space coord (u,v,depth) and transform to world space
				float2 uvClip = i.uv * 2.0 - 1.0;
				float clipDepth = depth; // Fix for OpenGl Core thanks to Lars Bertram
				clipDepth = (UNITY_NEAR_CLIP_VALUE < 0) ? clipDepth * 2 - 1 : clipDepth;
				float4 clipPos = float4(uvClip, clipDepth, 1.0);
				float4 viewPos = mul(proj, clipPos); // inverse projection by clip position
				viewPos /= viewPos.w; // perspective division
				float3 wpos = mul(eyeToWorld, viewPos).xyz;
 
                float4 cascadeWeights = GetCascadeWeights_SplitSpheres(wpos);
                bool inside = dot(cascadeWeights, float4(1, 1, 1, 1)) < 4;
                float4 samplePos = GetCascadeShadowCoord(float4(wpos, 1), cascadeWeights);
                float sceneShadow = UNITY_SAMPLE_SHADOW(_CascadeShadowMapTexture, samplePos.xyz).r;
                
                float shadowsClouds = clamp((1-cloudShadows),0,1);
                //float shadowMod = clamp(sceneShadow,0,1);
                 
                if(linearDepth > 0.99f)
                {
                   //shadowsClouds = 1.0f;
                   //shadowMod = 1.0f;
                }
                else
                {
                   // shadowsClouds = cloudShadows * shadowMod;
                }

                shadowsClouds = cloudShadows * shadowMod;
                */
                float shadowsClouds = clamp((1-cloudShadows),0,1);
                float4 final = float4(sceneColor.rgb * shadowsClouds, sceneColor.a);
                return final;
            }
            ENDCG
        }
    }
}
