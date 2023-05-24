Shader "Hidden/Volumetrics"
{ 
    Properties
    {
        _MainTex ("Texture", any) = "white" {}
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

            #include "UnityCG.cginc"
            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   
            float4x4 _LeftWorldFromView;
            float4x4 _RightWorldFromView;
            float4x4 _LeftViewFromScreen;
            float4x4 _RightViewFromScreen;

            sampler2D _DitherTexture;

            UNITY_DECLARE_SHADOWMAP(_CascadeShadowMapTexture);
            //UNITY_DECLARE_SCREENSPACE_TEXTURE(_CloudsTex);
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

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
            //uniform float4 _DirLightColor;

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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata_img v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v); 
                UNITY_INITIALIZE_OUTPUT(v2f, o); 
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.position = UnityObjectToClipPos(v.vertex);
                //o.position = v.vertex * float4(2,2,1,1) + float4(-1,-1,0,0);
                o.uv = v.texcoord;
                return o;
            }

            float4 GetCascadeWeights_SplitSpheres(float3 wpos)
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

                float4 cascadeWeights = GetCascadeWeights_SplitSpheres(wpos);
                bool inside = dot(cascadeWeights, float4(1, 1, 1, 1)) < 4;

                float4 samplePos = GetCascadeShadowCoord(float4(wpos, 1), cascadeWeights);
                float shadows = UNITY_SAMPLE_SHADOW(_CascadeShadowMapTexture, samplePos.xyz).r;
     
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
                float height = _ScreenParams.y;
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

                float offset = tex2D(_DitherTexture, interleavedPos / 8.0 + float2(0.5 / 8.0, 0.5 / 8.0)).w ; //+ _Randomness.xy

                //float offset = tex2D(_DitherTexture, squareUV(uv)).r;  

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

				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                float linearDepth = Linear01Depth(depth);
				
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
            ENDCG
        }
    }
}
