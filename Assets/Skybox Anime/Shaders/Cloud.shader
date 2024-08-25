Shader "AnimeSkybox/Cloud"
{
    Properties
    {
        [HDR]_CloudColorD("近太阳云颜色B",color) = (1,1,1,1)
        [HDR]_CloudColorB("近太阳云颜色B",color) = (1,1,1,1)

        [HDR]_CloudColorC("远太阳云颜色A",color) = (1,1,1,1)
        [HDR]_CloudColorA("远太阳云颜色A",color) = (1,1,1,1)

        _CloudMap("CloudMap", 2D) = "white" {}
        _NoiseMap("NoiseMap", 2D) = "white" {}
        _FadeOffset("Fade Offset", Range(-1, 1)) = 0
        _FadeMultiplier("Fade Multiplier", Range(0, 1)) = 0.05
        _FadeSmoothness("Fade Smoothness", Range(0, 1)) = 0.08
        [HDR]_Cloud_edgeColor("云边缘光颜色",color) = (1,1,1,1)

        _SunDirection("_SunDirection", Vector) = (-0.26102,0.12177,-0.95762, 0)
        _MoonDirection("_MoonDirection", Vector) = (-0.33274, -0.11934, 0.93544, 0)
        _SunMoon("唯一日月光切换", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags {
            "Queue" = "Transparent" 
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline" 
        }

        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            #include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise2D.hlsl"

            struct Attributes
            {
                float4 positionOS       : POSITION;
                float2 texUV2           : TEXCOORD1;
                float2 fadeDelay        : TEXCOORD2;
            };
 
            struct Varyings
            {
                float4 positionCS       : SV_POSITION;
                float2 uv               : TEXCOORD0;
                float fogCoord          : TEXCOORD1;
                float2 noiseuv          : TEXCOORD2;
                float3 positionWS       : TEXCOORD3;
                float fadeDelay         : TEXCOORD4;
            };
 
            CBUFFER_START(UnityPerMaterial)

            float4    _CloudColorA;
            float4    _CloudColorB;
            float4    _CloudColorC;
            float4    _CloudColorD;
          
            sampler2D  _CloudMap;
            float4     _CloudMap_ST;

            sampler2D _NoiseMap;
            float4 _NoiseMap_ST;

            float4 _Cloud_edgeColor;
            float3 _SunDirection;
            float3 _MoonDirection;

            float _SunMoon;
            float _FadeOffset;
            float _FadeMultiplier;
            float _FadeSmoothness;

            CBUFFER_END

            half remap(half x, half t1, half t2, half s1, half s2)
            {
                return (x - t1) / (t2 - t1) * (s2 - s1) + s1;
            }

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings) 0;
 
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);

                o.uv      = v.texUV2;
                o.noiseuv = TRANSFORM_TEX(v.texUV2, _NoiseMap);
                o.noiseuv = o.noiseuv * _NoiseMap_ST.xy + _NoiseMap_ST.zw + _Time.x * 0.05;
               
                o.fogCoord = ComputeFogFactor(o.positionCS.z);

                o.fadeDelay = v.fadeDelay.x;
 
                return o;
            }
 
            half4 frag(Varyings i) : SV_Target
            {
            
                float3 LightDirection = lerp(_SunDirection.xyz, _MoonDirection.xyz, _SunMoon);

                float3 SunDirection = clamp((dot(normalize(i.positionWS), LightDirection.xyz)), 0, 1);
                SunDirection = pow(SunDirection, 2);

                float3 CloudColorAB = lerp(_CloudColorA, _CloudColorB, SunDirection.x);
                float3 CloudColorCD = lerp(_CloudColorC, _CloudColorD, SunDirection.x);

                float4 Noise = tex2D(_NoiseMap, i.noiseuv);
            
                //float UVdisturbance = remap(Noise.b, 0, 1, 0, 0.03);
                float4 baseMap = tex2D(_CloudMap, i.uv/* + UVdisturbance*/);

                float cloudFade = SimplexNoise(_Time.x + i.fadeDelay) * _FadeMultiplier + _FadeOffset;

                float smLeft  = max(0, cloudFade - _FadeSmoothness);
                float smRight = smLeft + _FadeSmoothness;
                float fadeAlpha = smoothstep(smLeft, smRight, baseMap.b);

                float3 CloudColor = lerp(CloudColorAB, CloudColorCD, baseMap.r);
                float3 EdgeColor = _Cloud_edgeColor * baseMap.g * SunDirection.x;
                CloudColor = CloudColor + EdgeColor;

                /*
                if (baseMap.a <= 0) {
                    return float4(cloudFade, cloudFade, cloudFade, 1);
                }
                */
              
                return float4(CloudColor, fadeAlpha * baseMap.a);
                //return float4(i.uv, 0, 1);
            }
            ENDHLSL
        }
    }
}