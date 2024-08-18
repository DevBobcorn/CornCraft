Shader "AnimeSkybox/Skybox"
{
    Properties
    {
        _upPartSunColor("高空近太阳颜色", Color) = (0.00326,0.18243,0.63132,1)
        _upPartSkyColor("高空远太阳颜色", Color) = (0.02948,0.1609,0.27936,1)
        _downPartSunColor("水平线近太阳颜色", Color) = (0.30759,0.346,0.24592,1)
        _downPartSkyColor("水平线远太阳颜色", Color) = (0.04305,0.26222,0.46968,1)
        _IrradianceMapR_maxAngleRange("天空主色垂直变化范围", Range(0, 1)) = 0.44837
        _mainColorSunGatherFactor("近太阳颜色聚集程度", Range(0, 5)) = 0.31277

        _SunAdditionColor("太阳追加点颜色", Color) = (0.90409,0.7345,0.13709, 1)
        _SunAdditionIntensity("太阳追加点颜色强度", Range(0, 3)) = 1.48499
        _IrradianceMapG_maxAngleRange("太阳追加点垂直变化范围", Range(0, 1)) = 0.69804

        _SunRadius("太阳圆盘大小", Range(0, 50)) = 1
        _SunInnerBoundary("太阳内边界", Range(0, 10)) = 1
        _SunOuterBoundary("太阳外边界", Range(0, 10)) = 1
        _sun_disk_power_999("太阳圆盘power", Range(0, 1000)) = 1000
        _SunScattering("散射扩散", Range(0, 2)) = 1
        _sun_color_intensity("太阳圆盘颜色强度", Range(0, 10)) = 1.18529
        _sun_color("太阳圆盘颜色", Color) = (0.90625, 0.43019, 0.11743, 1)
        _sun_color_Scat("日出日落散射颜色", Color) = (0.90625, 0.43019, 0.11743, 1)

        _MoonTex("月亮贴图", 2D) = "white"{}
        _MoonRadius ("月亮大小", Range(0, 10)) = 3
        _MoonMaskRadius("月亮遮罩大小", range(1, 10)) = 5
        _mainColorMoonGatherFactor("近月亮颜色聚集程度", Range(0, 5)) = 0.31277
        _MoonScatteringColor("月亮散射颜色聚集程度", Color) = (1,1,1,1)
        _Moon_color("月亮圆盘颜色", Color) = (0.90625, 0.43019, 0.11743, 1)
        _Moon_color_intensity("月亮颜色强度", Range(0, 10)) = 1.18529

        _IrradianceMap("Irradiance Map",2D)= "while"{}

           _starColorIntensity("星星颜色强度", Range(0, 50)) = 22.7
        _starIntensityLinearDamping("星星遮蔽", Range(0, 1)) = 0.80829

        _NoiseMap("NoiseMap", 2D) = "white" {}
        _StarDotMap("StarDotMap", 2D) = "white" {}
        StarColorLut("StarColorLut", 2D) = "white" {}
        [HideInInspector] _StarColorLut_ST("_NoiseMap_ST", Vector) = (0.5,1,0,0)

        [HideInInspector]_StarDotMap_ST("StarDotMap_ST", Vector) = (10,10,0,0)
        _NoiseSpeed("c_NoiseSpeed", Range( 0 , 1)) = 0.293

        _SunDirection("_SunDirection", Vector) = (-0.26102,0.12177,-0.95762, 0)
        _MoonDirection("_MoonDirection", Vector) = (-0.33274, -0.11934, 0.93544, 0)

        _galaxyTex("银河贴图", 2D) = "white"{}
        _galaxy_INT("银河默认强度", range(0,1)) = 0.2
        _galaxy_intensity("银河强度", range(0,2)) = 1

    }
 
    SubShader
    {
        Tags { "Queue"="Geometry" 
               "RenderType" = "Opaque"
               "IgnoreProjector" = "True" 
               "RenderPipeline" = "UniversalPipeline" 
             }
        LOD 100
 
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
       
         
            #define UNITY_HALF_PI       1.57079632679f   //半圆周率
            #define UNITY_INV_HALF_PI   0.636619772367f  //半圆周率的倒数
            #define UNITY_PI            3.14159265359f   //圆周率
            float4x4 LToW;                               //MoonDir//Matrix4x4 LtoW = moon.transform.localToWorldMatrix; for DirectionToSkybox;
          
            struct appdata
            {
                float4 vertex       : POSITION;
                float4 uv           : TEXCOORD0;
               
            };
 
            struct v2f
            {
                float4 Varying_StarColorUVAndNoise_UV : TEXCOORD0;
                float4 Varying_NoiseUV_large          : TEXCOORD1;
                float4 Varying_WorldPosAndAngle    : TEXCOORD2;
                float4 Varying_IrradianceColor        : TEXCOORD3;
                float3 Test                          : TEXCOORD4;
                float4 UV  : TEXCOORD5; 
                float4 positionWS : TEXCOORD6;
                float4 positionCS : SV_POSITION;


            
            };
 
            CBUFFER_START(UnityPerMaterial)
            float3  _upPartSunColor;
            float3  _upPartSkyColor;
            float3  _downPartSunColor;
            float3  _downPartSkyColor;
            float _IrradianceMapG_maxAngleRange;
            float3 _SunAdditionColor;
            float _SunAdditionIntensity;
            float  _sun_disk_power_999;
            float  _sun_color_intensity;
            float3 _sun_color;
            float _SunInnerBoundary;
            float _SunOuterBoundary;
            float _SunScattering;

            float _IrradianceMapR_maxAngleRange;
            float _mainColorSunGatherFactor;
      
            float _SunRadius;

            sampler2D _IrradianceMap;
            float4 _IrradianceMap_ST;
            sampler2D _MoonTex;
            float4 _MoonTex_ST;

            float _MoonRadius;
            float _MoonMaskRadius;

            float3 _SunDirection;
            float3 _MoonDirection;
            float  _mainColorMoonGatherFactor;
            float3 _MoonScatteringColor;
            float3  _Moon_color;
            float _Moon_color_intensity;
            float3 _sun_color_Scat;

            float _starColorIntensity;
            float _starIntensityLinearDamping;

            sampler2D _StarDotMap;
             float4 _StarDotMap_ST;

            float _NoiseSpeed;

             sampler2D _NoiseMap;
            float4 _NoiseMap_ST;

           sampler2D _StarColorLut;
            float4 _StarColorLut_ST;

            sampler2D _galaxyTex;
            float4 _galaxyTex_ST;
            float _galaxy_INT;
            float  _galaxy_intensity;


            CBUFFER_END

            float FastAcosForAbsCos(float in_abs_cos) //快速反余弦函数
            {
                float _local_tmp = ((in_abs_cos * -0.0187292993068695068359375 + 0.074261002242565155029296875) * in_abs_cos - 0.212114393711090087890625) * in_abs_cos + 1.570728778839111328125;
                return _local_tmp * sqrt(1.0 - in_abs_cos);
            }

             float FastAcos(float in_cos)//快速反余弦函数
            {
                float local_abs_cos = abs(in_cos);
                float local_abs_acos = FastAcosForAbsCos(local_abs_cos);
                return in_cos < 0.0 ?  UNITY_PI - local_abs_acos : local_abs_acos;
            }



       
 
            v2f vert(appdata v)
            {
                v2f o = (v2f)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex);
                o.positionWS.xyz = vertexInput.positionWS;
                float3 _worldPos = mul(UNITY_MATRIX_M, float4(v.vertex.xyz, 1.0)).xyz;
                float3 NormalizeWorldPos = normalize(_worldPos);
         
			        	float4 _clippos  = mul(UNITY_MATRIX_VP, float4(_worldPos, 1.0));


                o.positionCS= _clippos;
                o.UV = v.uv;


                o.Varying_StarColorUVAndNoise_UV.xy = TRANSFORM_TEX(v.uv.xz , _StarDotMap);
                o.Varying_StarColorUVAndNoise_UV.zw = v.uv * 20.0;

                float4 _timeScaleValue = _Time.y * _NoiseSpeed * float4(0.4, 0.2, 0.1, 0.5);
                
                o.Varying_NoiseUV_large.xy = (v.uv.xz * _NoiseMap_ST.xy) + _timeScaleValue.xy;
                o.Varying_NoiseUV_large.zw = (v.uv.xz * _NoiseMap_ST.xy * 2.0) + _timeScaleValue.zw;



              //  Light mainLight = GetMainLight();
                //float3 _viewDir = normalize(_worldPos.xyz );/*_WorldSpaceCameraPos*/
                float3 SunDirection = dot(normalize(vertexInput.positionWS),_SunDirection.xyz);
               // float _WPDotSun = dot(SunDirection, _worldPos.xyz);
                float SunDirectionRemapClamp =clamp((SunDirection * 0.5) + 0.5,0,1.0);
                float _miu = clamp( dot(float3(0,1,0), NormalizeWorldPos), -1, 1 );
                float _angle_up_to_down_1_n1 = (UNITY_HALF_PI - FastAcos(_miu)) * UNITY_INV_HALF_PI;
             
             
                o.Varying_WorldPosAndAngle.xyz = NormalizeWorldPos;
                o.Varying_WorldPosAndAngle.w   = _angle_up_to_down_1_n1;

                  float2 _irradianceMap_G_uv;
                  _irradianceMap_G_uv.x = abs(_angle_up_to_down_1_n1) / max(_IrradianceMapG_maxAngleRange, 0.001f);
                  _irradianceMap_G_uv.y = 0.5;
                  float _irradianceMapG = tex2Dlod(_IrradianceMap, float4( _irradianceMap_G_uv, 0.0, 0.0 )).y;

                  float3 _sunAdditionPartColor = _irradianceMapG * _SunAdditionColor * _SunAdditionIntensity;

                     

                 float _upFactor = smoothstep(0, 1, clamp((abs(_SunDirection.y) - 0.2) * 10/3, 0, 1));
                 float _VDotSunFactor = smoothstep(0, 1, (SunDirectionRemapClamp -1)/0.7 + 1);
                 float _sunAdditionPartFactor = lerp(_VDotSunFactor, 1.0, _upFactor);///
                 float3 _additionPart = _sunAdditionPartColor * _sunAdditionPartFactor;
                 float3 _sumIrradianceRGColor =  _additionPart;

                 o.Varying_IrradianceColor.xyz = _sumIrradianceRGColor;

                 o.Test.xyz = float3(_irradianceMap_G_uv.x,_irradianceMap_G_uv.x,_irradianceMap_G_uv.x);
           

 
                return o;
            }
 
            half4 frag(v2f i) : SV_Target
            {
             
      
                 float sunDist = distance(i.UV.xyz, _SunDirection.xyz);
                 float MoonDist = distance(i.UV.xyz,_MoonDirection);
                 float sunArea = 1 - (sunDist * _SunRadius);
                 float moonArea = 1 - clamp((MoonDist * _MoonMaskRadius),0,1);
                // float moonGalaxyMask = 1 - clamp((MoonDist * 10),0,1);

                 float moonGalaxyMask = step(0.084,MoonDist);

                 float sunArea2 = 1- (sunDist*_SunScattering);//散射扩散
                 float moonArea2 = 1 - (MoonDist*0.5);
                 moonArea2 = smoothstep(0.5,1,moonArea2);
                 float sunArea3 = 1- (sunDist*0.4);
                 sunArea3 = smoothstep(0.05,1.21,sunArea3);
               
                 sunArea = smoothstep(_SunInnerBoundary,_SunOuterBoundary,sunArea);
                
                float3 MoonUV = mul(i.UV.xyz,LToW);
                float2 moonUV = MoonUV.xy * _MoonTex_ST.xy*_MoonRadius + _MoonTex_ST.zw;
               
               

                float  _WorldPosDotUp = dot(i.Varying_WorldPosAndAngle.xyz, float3(0,1,0));
                float  _WorldPosDotUpstep = smoothstep(0,0.1,_WorldPosDotUp);


               float _WorldPosDotUpstep1  = 1-abs(_WorldPosDotUp );
               _WorldPosDotUpstep1 = smoothstep(0.4,1,_WorldPosDotUpstep1 );
            
              
                float _WorldPosDotUpstep2 = clamp(0,1,smoothstep(0,0.01,_WorldPosDotUp)+ smoothstep(0.5,1,_WorldPosDotUpstep1)) ;
       
                float  _WorldPosDotUp_Multi999 = _sun_disk_power_999;

       
           
                  float4 moonTex = tex2D(_MoonTex, moonUV)*moonArea*_WorldPosDotUpstep; 

                 // float3 galaxyUV = mul(i.UV.xyz,galaxyLToW);
                  float4 galaxyTex = tex2D(_galaxyTex,i.UV.xz * _galaxyTex_ST.xy + _galaxyTex_ST.zw);
           
                  sunArea = sunArea *  _WorldPosDotUpstep;

                float3 _sun_disk = dot(min(1, pow(sunArea3 , _WorldPosDotUp_Multi999 * float3(1, 0.1, 0.01))),float3(1, 0.16, 0.03))* _sun_color_intensity * _sun_color;

   
                
                float3 _sun_disk_sunArea = sunArea * _sun_color_intensity * _sun_color ;
                _sun_disk = _sun_disk + _sun_disk_sunArea * 3;
         
                float _LDotDirClampn11_smooth = smoothstep(0, 1, sunArea3);
    

                float2 _irradianceMap_R_uv;
                    _irradianceMap_R_uv.x = abs(i.Varying_WorldPosAndAngle.w) / max(_IrradianceMapR_maxAngleRange,0.001f);
                    _irradianceMap_R_uv.y = 0.5;

                 float _irradianceMapR = tex2Dlod(_IrradianceMap, float4(_irradianceMap_R_uv, 0.0, 0.0)).x;




               

                  float _VDotSunDampingA = max(0, lerp( 1, sunArea2 , _mainColorSunGatherFactor ));
                  float _VDotSunDampingA_pow3 = _VDotSunDampingA * _VDotSunDampingA * _VDotSunDampingA;
              
                    float3 _upPartColor   = lerp(_upPartSkyColor, _upPartSunColor, _VDotSunDampingA_pow3);
                  float3 _downPartColor = lerp(_downPartSkyColor, _downPartSunColor, _VDotSunDampingA_pow3);
                  float3 _mainColor = lerp(_upPartColor, _downPartColor, _irradianceMapR);

                float _VDotMoonDampingA = max(0, lerp( 1, moonArea2 , _mainColorMoonGatherFactor ));
                  float _VDotMoonDampingA_pow3 = _VDotMoonDampingA *_VDotMoonDampingA;

                 float SSS = clamp( _VDotSunDampingA_pow3*_VDotSunDampingA *_VDotSunDampingA  * _WorldPosDotUpstep1 ,0,1);////改进ing
            
                  SSS = smoothstep(0.02,0.5, SSS );

                  SSS = SSS *  _WorldPosDotUpstep2;
      
                 float3 SSSS =  SSS *_sun_color_Scat;
                  
                  float3 FmoonColor =  (moonTex.xyz*_Moon_color*_Moon_color_intensity) + _VDotMoonDampingA_pow3*_MoonScatteringColor;

            

                float3 _day_part_color = (_sun_disk * _LDotDirClampn11_smooth ) + i.Varying_IrradianceColor.xyz + _mainColor+ FmoonColor;



                float _starExistNoise1 = tex2D(_NoiseMap, i.Varying_NoiseUV_large.xy).r;
                float _starExistNoise2 = tex2D(_NoiseMap, i.Varying_NoiseUV_large.zw).r;
                float _starSample = tex2D(_StarDotMap, i.UV.xz*_StarDotMap_ST.xy+_StarDotMap_ST.zw  ).r;
                float _star = _starSample * _starExistNoise2 * _starExistNoise1;
                float _miuResult = i.Varying_WorldPosAndAngle.w * 1.5;
                _miuResult = clamp(_miuResult, 0.0, 1.0);
                float _star_intensity = _star * _miuResult;
                _star_intensity *= 3.0;
                
   
                float _starColorNoise = tex2D(_NoiseMap, i.Varying_StarColorUVAndNoise_UV.zw).r;
                float _starIntensityDamping = (_starColorNoise - _starIntensityLinearDamping) / (1.0 -_starIntensityLinearDamping);
                _starIntensityDamping = clamp(_starIntensityDamping, 0.0, 1.0);
                _star_intensity = _starIntensityDamping * _star_intensity;
                
                float2 _starColorLutUV;
                _starColorLutUV.x = (_starColorNoise * _StarColorLut_ST.x) + _StarColorLut_ST.z;
                _starColorLutUV.y = 0.5;
                float3 _starColorLut = tex2D(_StarColorLut, _starColorLutUV).xyz;
                float3 _starColor = _starColorLut * _starColorIntensity;

                float3 _finalStarColor = _star_intensity * _starColor*moonGalaxyMask;

               galaxyTex.w = pow(galaxyTex.w,10);
              float3 galaxyColor =clamp((galaxyTex.xyz*galaxyTex.w*_WorldPosDotUp *_galaxy_INT*moonGalaxyMask*_galaxy_intensity),0,1);


             return float4(SSSS+_day_part_color+_finalStarColor+galaxyColor,1);
            //return float4(moonGalaxyMask ,moonGalaxyMask ,moonGalaxyMask ,1);
              
            }
            ENDHLSL
        }
    }
}