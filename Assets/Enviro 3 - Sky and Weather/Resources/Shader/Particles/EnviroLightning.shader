Shader "Enviro3/Lightning" {
Properties {
	_MainTex ("Texture", 2D) = "white" {}
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_Intensity("Brightness", float) = 250
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
	Blend One One
	ColorMask RGB
	Cull Off Lighting Off ZWrite Off

	SubShader {
		Pass {
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile_particles
			#pragma multi_compile_fog
			#pragma exclude_renderers gles 
			#include "UnityCG.cginc"

			sampler2D _MainTex; 
			float4 _TintColor;
			
			float _Intensity;
			
			struct appdata_t 
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float4 _MainTex_ST;
			
			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				return o;
			}

			
			fixed4 frag (v2f i) : SV_Target
			{
				
				half4 col = tex2D(_MainTex, i.texcoord) * _TintColor;
				col.rgb *= _Intensity;
				col.rgb *= col.a;
				return col;
			}
			ENDCG 
		}
	} 
}
}
