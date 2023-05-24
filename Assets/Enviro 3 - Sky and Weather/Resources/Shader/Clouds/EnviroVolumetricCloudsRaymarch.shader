Shader "Hidden/EnviroCloudsRaymarch"
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ ENVIRO_DEPTH_BLENDING
            #pragma multi_compile _ ENVIRO_DUAL_LAYER
            #pragma multi_compile _ ENVIRO_CLOUD_SHADOWS
            #pragma multi_compile _ ENVIROURP
            #include "UnityCG.cginc"
            #include "../Includes/VolumetricCloudsInclude.cginc"

            int _Frame;
     
            struct v2f
            {
                float4 position : SV_POSITION;
		        float2 uv : TEXCOORD0;
		        float4 screenPos : TEXCOORD1;
                float eyeIndex : TEXCOORD2; 

                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct appdata 
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert (appdata_img v)
            {
                v2f o; 
                 
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);   
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.eyeIndex = unity_StereoEyeIndex;

                #if defined(ENVIROURP)
		        o.position = float4(v.vertex.xyz,1.0);
		        #if UNITY_UV_STARTS_AT_TOP
                o.position.y *= -1;
                #endif
                #else
		        o.position = UnityObjectToClipPos(v.vertex);
                #endif   

                o.uv = v.texcoord;
                o.screenPos = ComputeScreenPos(o.position);
                
                return o;
            }

            float getRandomRayOffset(float2 uv) // uses blue noise texture to get random ray offset
            {
                float noise = tex2D(_BlueNoise, uv).x;
                noise = mad(noise, 2.0, 1.0);
                return noise;
            }
             
            fixed4 frag (v2f i) : SV_Target
            { 
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 cameraRay =  float4(i.uv * 2.0 - 1.0, 1.0, 1.0);
                float3 EyePosition = _CameraPosition;

                float3 ray = 0; 

                //return lerp(float4(1, 0, 0, 1), float4(0, 1, 0, 1), unity_StereoEyeIndex);
 
               	if (unity_StereoEyeIndex == 0)
	            {
                    cameraRay = mul(_InverseProjection, cameraRay);
                    cameraRay = cameraRay / cameraRay.w;
                    ray = normalize(mul((float3x3)_InverseRotation, cameraRay.xyz));
                }
                else  
                {
                    cameraRay = mul(_InverseProjectionRight, cameraRay);
                    cameraRay = cameraRay / cameraRay.w; 
                    ray = normalize(mul((float3x3)_InverseRotationRight, cameraRay.xyz));
                }
 
                float rayLength = length(ray);
                float sceneDepth = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_DownsampledDepth, UnityStereoTransformScreenSpaceTex(i.uv));
 
                float raymarchEnd = GetRaymarchEndFromSceneDepth(Linear01Depth(sceneDepth), 100000); //* rayLenght
                float raymarchEndShadows = GetRaymarchEndFromSceneDepth(Linear01Depth(sceneDepth), 1000);

                float offset = tex2D(_BlueNoise, squareUV(i.uv + _Randomness.xy)).x * _CloudDensityScale.z;  

                //offset = clamp(offset - 2,-2,2);

                float3 pCent = float3(EyePosition.x, -_CloudsParameter.w, EyePosition.z);


                float bIntensity, bDistance, bAlpha, shadow = 0.0f;
                float3 wpos;
#if ENVIRO_DUAL_LAYER

                //Clouds Layer 1
                RaymarchParameters parametersLayer1;
                InitRaymarchParametersLayer1(parametersLayer1);
                float2 hitDistanceLayer1 = ResolveRay(EyePosition,ray,pCent, raymarchEnd, parametersLayer1);
                float3 layer1Final = Raymarch(EyePosition,ray,hitDistanceLayer1.xy,pCent,parametersLayer1,offset,0);
#if ENVIRO_CLOUD_SHADOWS
                //Clouds Shadows Layer1
                wpos = CalculateWorldPosition(i.uv,sceneDepth);
                float2 shadowHitDistanceLayer1 = ResolveRay(EyePosition,ray,pCent, raymarchEnd, parametersLayer1).xy;
                float shadowsLayer1 = RaymarchShadows(EyePosition,wpos,ray, shadowHitDistanceLayer1,pCent,parametersLayer1,offset,sceneDepth,0);
#endif
                //Clouds Layer 2
                RaymarchParameters parametersLayer2;
                InitRaymarchParametersLayer2(parametersLayer2);
                float2 hitDistanceLayer2 = ResolveRay(EyePosition,ray,pCent,raymarchEnd, parametersLayer2);    
                float3 layer2Final = Raymarch(EyePosition,ray,hitDistanceLayer2,pCent,parametersLayer2,offset,1);
#if ENVIRO_CLOUD_SHADOWS
                //Clouds Shadows Layer2
                float2 shadowHitDistanceLayer2 = ResolveRay(EyePosition,ray,pCent, raymarchEnd, parametersLayer2).xy;
                float shadowsLayer2 = RaymarchShadows(EyePosition,wpos,ray, shadowHitDistanceLayer2,pCent,parametersLayer2,offset,sceneDepth,1);
#endif
                if (EyePosition.y < _CloudsParameter2.x) 
                { 
                    if(layer1Final.b >= 1.0)
                       return float4(layer1Final.r,layer1Final.g,shadow,layer1Final.b); 
                    
                    if(layer2Final.z <= 0)
                    {
                        bIntensity = layer1Final.r;
                        bDistance = layer1Final.g;
                        bAlpha = layer1Final.b;
                    }
                    else
                    {
                        bIntensity = layer2Final.x * (1-layer1Final.z) + layer1Final.x;
                        bDistance = layer2Final.y * (1-layer1Final.z) + layer1Final.y * layer1Final.z;
                        bAlpha = layer2Final.z * (1-layer1Final.z) + layer1Final.z;      
                    }
                }
                else
                { 
                    if(layer2Final.b >= 1.0)
                       return float4(layer2Final.r,layer2Final.g,1.0,layer2Final.b); 

                    bIntensity = layer1Final.x * (1-layer2Final.z) + layer2Final.x;
                    bDistance = layer1Final.y * (1-layer2Final.z) + layer2Final.y * layer2Final.z;
                    bAlpha = layer1Final.z * (1-layer2Final.z) + layer2Final.z;
                }
#if ENVIRO_CLOUD_SHADOWS
                //Combine cloud shadows.
                shadow = shadowsLayer1 + shadowsLayer2;
#endif

#else
                    RaymarchParameters parametersLayer1;
                    InitRaymarchParametersLayer1(parametersLayer1);
                    float2 hitDistanceLayer1 = ResolveRay(EyePosition,ray,pCent, raymarchEnd, parametersLayer1);
                    float3 layer1Final = Raymarch(EyePosition,ray,hitDistanceLayer1,pCent,parametersLayer1,offset,0);
#if ENVIRO_CLOUD_SHADOWS
                    //Clouds Shadows
                    float2 shadowHitDistanceLayer1 = ResolveRay(EyePosition,ray,pCent, raymarchEnd, parametersLayer1).xy;
                    wpos = CalculateWorldPosition(i.uv,sceneDepth);
                    shadow = RaymarchShadows(EyePosition,wpos,ray, shadowHitDistanceLayer1,pCent,parametersLayer1,offset,sceneDepth,0);
#endif
                    bIntensity = layer1Final.r;
                    bDistance = layer1Final.g;
                    bAlpha = layer1Final.b;
#endif

                return float4(max(bIntensity,0.0f),max(bDistance,0.0f),clamp(shadow,0.0,0.25),max(bAlpha,0.0f)); 
            }
            ENDCG
        }
    }
}