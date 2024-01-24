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
        _Cloud_SDF_TSb("云大小变化", Range(0.003, 1.5)) = 0.5
        [HDR]_Cloud_edgeColor("云边缘光颜色",color) = (1,1,1,1)

        _SunDirection("_SunDirection", Vector) = (-0.26102,0.12177,-0.95762, 0)
        _MoonDirection("_MoonDirection", Vector) = (-0.33274, -0.11934, 0.93544, 0)
        _SunMoon("唯一日月光切换", Range(0, 1)) = 0
 
    }
    SubShader
    {
        Tags { "Queue"="Transparent" 
               "RenderType" = "Transparent"
               "IgnoreProjector" = "True"
               "RenderPipeline" = "UniversalPipeline" 
             }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        ZTest NotEqual
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
 
   

            struct Attributes
            {
                float4 positionOS       : POSITION;
                float2 texUV1               : TEXCOORD0;
                float2 texUV2              : TEXCOORD1;
            };
 
            struct Varyings
            {
                float4 positionCS       : SV_POSITION;
              
                float2 uv            : TEXCOORD0;
                float fogCoord          : TEXCOORD1;
                float2 noiseuv  : TEXCOORD2;
               float3 positionWS       : TEXCOORD3;
         
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

            float   _Cloud_SDF_TSb;

            float4 _Cloud_edgeColor;
            float3 _SunDirection;
            float3  _MoonDirection;
            float _SunMoon;
       

            CBUFFER_END

            half remap(half x, half t1, half t2, half s1, half s2)
           {
               return (x - t1) / (t2 - t1) * (s2 - s1) + s1;
          }

            
            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
 
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);

          
                o.uv =  v.texUV2;
                o.noiseuv =  TRANSFORM_TEX(v.texUV2, _NoiseMap);
                o.noiseuv = o.noiseuv * _NoiseMap_ST.xy + _NoiseMap_ST.zw + _Time.x*0.05;
               
                o.fogCoord = ComputeFogFactor(o.positionCS.z);
 
                return o;
            }
 
            half4 frag(Varyings i) : SV_Target
            {
            
               float3 LightDirection = lerp(_SunDirection.xyz,_MoonDirection.xyz,_SunMoon);

               float3 SunDirection = clamp((dot(normalize(i.positionWS),LightDirection.xyz)),0,1);
                      SunDirection =  pow(SunDirection,2);
              // float  SunDir = smoothstep(0,1,SunDirection);

               float3  CloudColorAB = lerp(_CloudColorA,_CloudColorB,SunDirection.x) ;
               float3  CloudColorCD = lerp(_CloudColorC,_CloudColorD,SunDirection.x) ;

         // SunDirection =  remap(SunDirection+1,1,2,1,1.15)+_SunColor;

               float4 Noise = tex2D(_NoiseMap,i.noiseuv);
                 
               //float4 baseMap = tex2D(_CloudMap, i.uv);
           
               float UVdisturbance =  remap(Noise.b,0,1,0,0.03);

               float4 baseMap = tex2D(_CloudMap, i.uv + UVdisturbance);

               float baseMapSMstep = smoothstep(clamp((_Cloud_SDF_TSb-0.08),0,1.5),_Cloud_SDF_TSb,baseMap.b);
     
               
               float3  CloudColor = lerp(CloudColorAB,CloudColorCD,baseMap.r) ;
               float3  EdgeColor = _Cloud_edgeColor * baseMap.g * SunDirection.x;
               CloudColor = CloudColor + EdgeColor;

               // c.rgb = MixFog(c.rgb, i.fogCoord);
              
                return float4(  CloudColor,baseMapSMstep*baseMap.a);
               // return float4(SunDirection,1);
              
        
            }
            ENDHLSL
        }
    }
}