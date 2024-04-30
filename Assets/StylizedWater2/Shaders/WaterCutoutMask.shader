Shader "Stylized Water 2/Cutout" 
{
	Properties
	{
		//[CurvedWorldBendSettings] _CurvedWorldBendSettings("0|1|1", Vector) = (0, 0, 0, 0)
	}
	SubShader
	{
		PackageRequirements 
		{ 
			"com.unity.render-pipelines.core": "10.3.2"
			"com.unity.render-pipelines.universal": "10.3.2"
		}
		Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-1" }
		ColorMask 0
		ZWrite On
		
		Pass 
		{
			Name "Depth mask"
			
			HLSLPROGRAM
			#pragma multi_compile_instancing
			
			#include "Libraries/URP.hlsl"
			//#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
			//#define CURVEDWORLD_BEND_ID_1
			//#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
			//#include "Assets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"

			struct Attributes
            {
                float4 positionOS       : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
			
			#pragma vertex vert
            #pragma fragment frag

			Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
				
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				#if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON) 
				CURVEDWORLD_TRANSFORM_VERTEX(input.positionOS)
				#endif
				
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                return output;
            }
			
			half4 frag() : SV_Target { return 0; }
			
			ENDHLSL
		}
	}
}