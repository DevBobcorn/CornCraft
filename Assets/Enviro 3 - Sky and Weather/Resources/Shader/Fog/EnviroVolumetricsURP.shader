Shader "Hidden/VolumetricsURP"
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

            #pragma multi_compile __ UNITY_2022_2_NEWER

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   
            float4x4 _LeftWorldFromView;
            float4x4 _RightWorldFromView;
            float4x4 _LeftViewFromScreen;
            float4x4 _RightViewFromScreen;

            sampler2D _DitherTexture;

            //UNITY_DECLARE_SCREENSPACE_TEXTURE(_CloudsTex);
            TEXTURE2D_X_FLOAT(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            uniform sampler3D _NoiseTexture; 
 
            uniform int _Steps;
            uniform float3 _CameraPosition;
            uniform float4 _VolumetricLight;
            uniform float4 _HeightFog;
            uniform float4 _HeightParams;
            uniform float4 _NoiseData;
            uniform float3 _WindDirection;
            uniform float4 _MieG;
            uniform float _MaxRayLength;
            uniform float _MaxRayLengthLights;
            uniform float4 _AmbientColor;

            uniform float3 _DirLightDir;

            uniform float4 _Randomness;

            struct PointLight
            {
                float3 pos;
                float range;
                float3 color;
                float padding;
            };
            StructuredBuffer<PointLight> _PointLights;
            float _PointLightsCount;

            struct SpotLight
            {
                float3 pos;
                float range;
                float3 color;
                float3 lightDirection;
                float lightCosHalfAngle;
                //float2 angularFalloffParameters;
                //float2 distanceFalloffParameters;
                float padding;
            };

            StructuredBuffer<SpotLight> _SpotLights;
            float _SpotLightsCount;

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 position : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
 
            struct appdata
            {
                float4 vertex : POSITION;
			    float2 texcoord : TEXCOORD0;
			    UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert (appdata v)
            {
                v2f o = (v2f)0; 
                UNITY_SETUP_INSTANCE_ID(v); 
                //UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                //VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                o.position = float4(v.vertex.xyz,1); 
                #if UNITY_UV_STARTS_AT_TOP
			    o.position.y *= -1;
			    #endif
                //o.position = v.vertex * float4(2,2,1,1) + float4(-1,-1,0,0);
                o.uv = v.texcoord;
                return o;
            }


            float anisotropy(float costheta)
            {
                float g = _MieG;
                float gsq = g*g;
                float denom = 1 + gsq - 2.0 * g * costheta;
                denom = denom * denom * denom;
                denom = sqrt(max(0, denom));
                return (1 - gsq) / denom;
            }
            
            float anisotropyPointSpot(float costheta)
            {
                float g = 0.8;
                float gsq = g*g;
                float denom = 1 + gsq - 2.0 * g * costheta;
                denom = denom * denom * denom;
                denom = sqrt(max(0, denom));
                return (1 - gsq) / denom;
            }

            float Attenuation(float distNorm)
            {
                return 1.0 / (1.0 + 25.0 * distNorm);
            }

            float DirectionalLight(float3 wpos)
		    {
                float atten = 1.0f;

                half cascadeIndex = ComputeCascadeIndex(wpos);
                bool inside = dot(cascadeIndex, 1) < 4;
                float4 coords = mul(_MainLightWorldToShadow[cascadeIndex], float4(wpos, 1.0));
                                         
                ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
                half4 shadowParams = GetMainLightShadowParams();

 #if defined(UNITY_2022_2_NEWER) 
               float shadows = SampleShadowmap(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_LinearClampCompare), coords, shadowSamplingData, shadowParams, false).r;
                 
 #else
                 float shadows = SampleShadowmap(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), coords, shadowSamplingData, shadowParams, false).r;            
 #endif
             
                atten = inside ? shadows : 1.0f; 

                if(shadows > 0.0f)
                    atten = 1.0f;

                return atten; 
            }

            float3 PointLights(float3 pos)
            {
                float3 color = 0;

                for (int i = 0; i < _PointLightsCount; i++)
                {
                    float3 posToLight = _PointLights[i].pos - pos;
                    float distNorm = dot(posToLight, posToLight) * _PointLights[i].range;
                    float att = Attenuation(distNorm);

                    //#if ANISOTROPY
                    float3 cameraToPos = normalize(pos - _WorldSpaceCameraPos.xyz);
                    float costheta = dot(cameraToPos, normalize(posToLight));
                    att *= anisotropyPointSpot(costheta);
                    //#endif

                    color += _PointLights[i].color * att;
                }
                return color;
            }

            float3 SpotLights(float3 pos)
            {
                float3 color = 0;
                for (int i = 0; i < _SpotLightsCount; i++)
                {
                    float3 posToLight = _SpotLights[i].pos - pos;
                    float distNorm = dot(posToLight, posToLight) * _SpotLights[i].range;
                    float att = Attenuation(distNorm);

                    half3 lightVector = normalize(pos - _SpotLights[i].pos);
                    half cosAngle = dot(_SpotLights[i].lightDirection.xyz, lightVector);

                    half angleAttenuation = 1;
                    angleAttenuation = smoothstep(_SpotLights[i].lightCosHalfAngle, lerp(1, _SpotLights[i].lightCosHalfAngle, 0.8f), cosAngle);
                    angleAttenuation = pow(angleAttenuation, 2.0f);
                    att *= angleAttenuation;
                     
                    #if ANISOTROPY
                        float3 cameraToPos = normalize(pos - _CameraPos.xyz);
                        float costheta = dot(cameraToPos, normalize(posToLight));
                        att *= anisotropyPointSpot(costheta);
                    #endif
                    color += _SpotLights[i].color * att;

                }
                return color;
            }

            //-----------------------------------------------------------------------------------------
            // GetDensity
            //-----------------------------------------------------------------------------------------
            float GetDensity(float3 wpos, inout float density, float depth, float3 rayDir)
            {
                density = 1.0f;
 
   // #ifdef NOISE 
            //    float4 noise = tex3D(_NoiseTexture, frac(wpos * _NoiseData.x + float3(_Time.y * _WindDirection.x, 0, _Time.y * _WindDirection.y)));
            //    float noiseFbm = (noise.g * 0.625) + (noise.b * 0.25) + (noise.a * 0.125);
            //    noiseFbm = saturate(noiseFbm - _NoiseData.y);
           //     density *= saturate(noiseFbm);
   // #endif 
                return density;
            }

        

            float2 squareUV(float2 uv) 
            {
                float width = _ScreenParams.x;
                float height =_ScreenParams.y;
                float scale = 1000;
                float x = uv.x * width;
                float y = uv.y * height;
                return float2 (x/scale, y/scale);
            }

                 float4 RayMarch(float2 uv,float2 screenPos, float3 rayStart, float3 rayDir, float rayLength, float rayLengthLights, float linearDepth)
            {            
                float2 interleavedPos = (fmod(floor(screenPos.xy), 8.0)) ;

                #if UNITY_SINGLE_PASS_STEREO
                    float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
                    interleavedPos = (interleavedPos - scaleOffset.zw) / scaleOffset.xy;
                #endif

                float offset = tex2D(_DitherTexture, interleavedPos / 8.0 + float2(0.5 / 8.0, 0.5 / 8.0)).w; //+ _Randomness.xy
  
                int stepCount = _Steps;

                float stepSize = rayLength / stepCount;
                float3 step = rayDir * stepSize;

                float stepSizeLights = rayLengthLights / stepCount;
                float3 stepLights = rayDir * stepSizeLights;
 
                float3 currentPositionDithered = rayStart + step * offset; 
                float3 currentPositionLightsDithered = rayStart + stepLights * offset; 
                float3 currentPosition = rayStart + step;

                float4 color = float4(0.0,0.0,0.0,0); 
                float cosAngle;

                float extinction = 0;
                float transmitance = 0;
                float ambient = 0;
                cosAngle = dot(_DirLightDir.xyz, -rayDir);

                float ani = anisotropy(cosAngle); 
                float4 lightsColor;
                
                [loop]
                for (int i = 0; i < stepCount; i++)
                {
                    float density = GetDensity(currentPosition, density, linearDepth, rayDir);
                    float atten = DirectionalLight(currentPositionDithered) * 0.1; 
                   
                    //Cloud Shadows
                    //float cloudShadows = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CloudsTex,uv).b;
                    //atten *= (1-cloudShadows);

                    float scattering = _VolumetricLight.x *  density;
                    extinction += _VolumetricLight.y * density;

                    transmitance += atten * scattering * exp(-extinction);
                    
                    lightsColor.rgb += PointLights(currentPositionLightsDithered) * stepSizeLights * density;
                    lightsColor.rgb += SpotLights(currentPositionLightsDithered) * stepSizeLights * density;
       
                    currentPosition += step;
                    currentPositionDithered += step;
                    currentPositionLightsDithered += stepLights;
                } 

                //color.rgb = _DirLightColor.rgb * transmitance * ani;
                color.a = transmitance * ani; 
                color.rgb += lightsColor.rgb * 0.1; 

                color = max(0, color);
                
                return color;
            }
      
            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 uv = i.uv.xy;

				float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(uv)).r;

                float linearDepth = Linear01Depth(depth,_ZBufferParams);
				
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
               
                float3 rayStart = _WorldSpaceCameraPos;
				float3 rayDir = wpos - _WorldSpaceCameraPos;	
                //rayDir *= linearDepth;

				float rayLength = length(rayDir);
				rayDir /= rayLength;

				float rayLengthLights = min(rayLength, _MaxRayLengthLights);
                rayLength = min(rayLength, _MaxRayLength);

				float4 color = RayMarch(uv, i.position.xy, rayStart, rayDir, rayLength, rayLengthLights, linearDepth);

				return color;
            }
            ENDHLSL
        }
    }
}
