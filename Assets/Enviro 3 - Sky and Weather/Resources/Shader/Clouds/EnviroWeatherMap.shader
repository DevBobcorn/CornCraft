Shader "Enviro3/Standard/WeatherTexture"  
{
	Properties 
	{
		_Coverage ("Coverage", Range(0,1)) = 0.5
		_Tiling ("Tiling", Range(1,100)) = 10
	}
	SubShader 
	{  
		Tags { "RenderType"="Opaque" }
		LOD 200
		Pass { 
			CGPROGRAM 
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "../Includes/NoiseInclude.cginc"

			#pragma target 3.0
			#pragma exclude_renderers gles 

			#pragma multi_compile_local __ ENVIRO_DUAL_LAYER 

			sampler2D _MainTex;

		    struct VertexInput 
		    {
  				half4 vertex : POSITION;
 				float2 uv : TEXCOORD0;	
        	};

            struct VertexOutput 
			{
           		float4 position : SV_POSITION;
 				float2 uv : TEXCOORD0;
            }; 
			          
            VertexOutput vert (appdata_img v) 
			{
 			 	VertexOutput o;
 				o.position = UnityObjectToClipPos(v.vertex);				
 				o.uv = v.texcoord;
 				return o; 
            }       
 		     
 			float4x4 world_view_proj;
 
 			float _CoverageLayer1;  
			float _CoverageLayer2;  
			float _CloudsTypeLayer1;
			float _CloudsTypeLayer2;

			float _WorleyFreq1Layer1; 
			float _WorleyFreq1Layer2; 
			float _WorleyFreq2Layer1; 
			float _WorleyFreq2Layer2; 

			float _DilateCoverageLayer1; 
			float _DilateCoverageLayer2; 
			float _DilateTypeLayer1; 
			float _DilateTypeLayer2;

			float _CloudsTypeModifierLayer1; 
			float _CloudsTypeModifierLayer2;

			float4 _LocationOffset;

			float3 _WindDirectionLayer1;
			float3 _WindDirectionLayer2;

			//float _WindSpeed;

		
 			float4 frag(VertexInput input) : SV_Target 
 			{  
				float2 uv = input.uv; 

				float2 windOffsetLayer1 = _WindDirectionLayer1.xy;
				float2 windOffsetLayer2 = _WindDirectionLayer2.xy; 

				float2 fillerUV = uv.xy + windOffsetLayer1 + _LocationOffset.xy;
				float covFiller = worleyFbm2DFiller(fillerUV * 2, 2) * 1.2; 

				///////Layer 1
				//float2 offset_pos_Layer1_1 = windOffsetLayer1 + float2(0.1,0.5) + _LocationOffset.xy;
				//float2 offset_pos_Layer1_2 = windOffsetLayer1 + float2(0.4,-0.5) + _LocationOffset.xy;
				//float2 offset_pos_Layer1_3 = windOffsetLayer1 + float2(-0.2,0.9) + _LocationOffset.xy;

				//float2 sampling_pos_Layer1_1 = float2(uv.xy + offset_pos_Layer1_1) * _TilingLayer1;
				//float2 sampling_pos_Layer1_2 = float2(uv.xy + offset_pos_Layer1_2) * _TilingLayer1;
				//float2 sampling_pos_Layer1_3 = float2(uv.xy + offset_pos_Layer1_3) * _TilingLayer1;

				//Perlin Noises
				//float perlin_Layer1_1 = saturate(CalculatePerlinTileing(sampling_pos_Layer1_1.xy,float2(_TilingLayer1, _TilingLayer1)));
				//float perlin_Layer1_2 = saturate(CalculatePerlinTileing(sampling_pos_Layer1_2.xy,float2(_TilingLayer1, _TilingLayer1)));
				//float perlin_Layer1_3 = saturate(CalculatePerlinTileing(sampling_pos_Layer1_3.xy,float2(_TilingLayer1, _TilingLayer1)));
				

				//Worley Noise
				float worley1Layer1 = worley2(windOffsetLayer1 + _LocationOffset.xy + uv.xy * _WorleyFreq1Layer1, 1);
				float worley2Layer1 = worley2(windOffsetLayer1 + _LocationOffset.xy + uv.xy * _WorleyFreq2Layer1, 1);
 
				//float perlin_Layer1_Combined = saturate(perlin_Layer1_1 + (perlin_Layer1_2 - perlin_Layer1_3) + worleyLayer1);
				float dilateCoverageLayer1 = dilate_perlin_worley(worley1Layer1,worley2Layer1,_DilateCoverageLayer1); 

				//Coverage Layer
				float coverageLayer1 = saturate(dilateCoverageLayer1 + (covFiller * _CoverageLayer1));
			
				float dilateTypeLayer1 = dilate_perlin_worley(worley1Layer1,worley2Layer1,_DilateTypeLayer1);
				float typeLayer1 = saturate(dilateTypeLayer1 * _CloudsTypeModifierLayer1);
				///Layer 1 End
#ifdef ENVIRO_DUAL_LAYER
				///////Layer 2
				//float2 offset_pos_Layer2_1 = float2(0.78,-0.5) + _LocationOffset.zw;
				//float2 offset_pos_Layer2_2 = float2(0.2,0.9) + _LocationOffset.zw;
				//float2 offset_pos_Layer2_3 = float2(-0.5,0.14) + _LocationOffset.zw;

				//float2 sampling_pos_Layer2_1 = float2(uv.xy + offset_pos_Layer2_1) * _TilingLayer2;
				//float2 sampling_pos_Layer2_2 = float2(uv.xy + offset_pos_Layer2_2) * _TilingLayer2;
				//float2 sampling_pos_Layer2_3 = float2(uv.xy + offset_pos_Layer2_3) * _TilingLayer2;

				//Perlin Noises
				//float perlin_Layer2_1 = saturate(CalculatePerlinTileing(sampling_pos_Layer2_1.xy,float2(_TilingLayer2, _TilingLayer2)));
				//float perlin_Layer2_2 = saturate(CalculatePerlinTileing(sampling_pos_Layer2_2.xy,float2(_TilingLayer2, _TilingLayer2)));
				//float perlin_Layer2_3 = saturate(CalculatePerlinTileing(sampling_pos_Layer2_3.xy,float2(_TilingLayer2, _TilingLayer2)));
			

				//Worley Noise
				float worley1Layer2 = worley2(windOffsetLayer2 + _LocationOffset.zw + uv.xy * _WorleyFreq1Layer2, 1);
				float worley2Layer2 = worley2(windOffsetLayer2 + _LocationOffset.zw + uv.xy * _WorleyFreq2Layer2, 1);

				//float perlin_Layer2_Combined = saturate(perlin_Layer2_1 + (perlin_Layer2_2 - perlin_Layer2_3) + worleyLayer2);
				float dilateCoverageLayer2 = dilate_perlin_worley(worley1Layer2,worley2Layer2,_DilateCoverageLayer2); 
				
				//Coverage Layer
				//float covFiller = worleyFbm2DFiller(uv.xy * 4, 4) * 1.2;
				float coverageLayer2 = saturate(dilateCoverageLayer2 + (covFiller * _CoverageLayer2));
			
				float dilateTypeLayer2 = dilate_perlin_worley(worley1Layer2,worley2Layer2,_DilateTypeLayer2);
				float typeLayer2 = saturate(dilateTypeLayer2 * _CloudsTypeModifierLayer2);
				///Layer 2 End

				return float4(coverageLayer1,typeLayer1,coverageLayer2,typeLayer2);
#else
				return float4(coverageLayer1,typeLayer1,0.0f,0.0f);
#endif
			}
		ENDCG
		}
	}
	FallBack "Diffuse"
}
