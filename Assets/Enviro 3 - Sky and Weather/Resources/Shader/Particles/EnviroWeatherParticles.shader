Shader "Enviro3/Particles/WeatherParticles" 
{
Properties 
{
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_MainTex ("Particle Texture", 2D) = "white" {}
	_lightIntensity ("Light Intensity", Range(0.0,1.0)) = 1.0
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Blend SrcAlpha OneMinusSrcAlpha
	ColorMask RGBA
	Cull Off Lighting Off ZWrite Off

	SubShader {
		Pass {
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_particles
			#pragma multi_compile_fog
			#pragma exclude_renderers gles 

			#include "UnityCG.cginc"
			#include "../Includes/ParticlesInclude.cginc"

			sampler2D _MainTex;
			fixed4 _TintColor;
			float4 _EnviroLighting;
			float _lightIntensity;

			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 projPos : TEXCOORD2;
				float3 posWorld : TEXCOORD3;
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			float4 _MainTex_ST;

			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.posWorld = mul(unity_ObjectToWorld, v.vertex);
				o.projPos = ComputeScreenPos (o.vertex);
				COMPUTE_EYEDEPTH(o.projPos.z);
				o.color = v.color;
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
			
			fixed4 frag (v2f i) : SV_Target
			{

				float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));

				#ifdef SOFTPARTICLES_ON
				float partZ = i.projPos.z;
				float fade = saturate (0.5 * (sceneZ-partZ));
				i.color.a *= fade;
				#endif
				
				float4 col = 2.0f * i.color * _TintColor * tex2D(_MainTex, i.texcoord);
				//col *= _EnviroLighting * _lightIntensity;
				UNITY_APPLY_FOG(i.fogCoord, col);

				//float4 fog = TransparentFog(col,i.posWorld,i.projPos.xy,sceneZ);
				float blend = 1.0;
				WeatherEllipsoids(i.posWorld,blend);
				return float4(col.rgb,col.a * blend);
			}  
			ENDCG 
		}
	}	
} 
}
