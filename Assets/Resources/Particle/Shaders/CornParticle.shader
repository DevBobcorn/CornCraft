Shader "CornShader/Unlit/BlockParticle"
{
	Properties
	{
		[MainTexture] _BaseMap("Base Map (RGB) Smoothness / Alpha (A)", 2DArray) = "white" {}
	}
	SubShader
	{
		Tags { "Queue"="Transparent+1" "IgnoreProjector"="True" "RenderType"="Transparent" }

		Blend SrcAlpha OneMinusSrcAlpha

		LOD 100

		Pass
		{
			Cull off

			CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				// index, 0, 0, 0
				float4 particleInfo : TEXCOORD3;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				// index, 0, 0, 0
				float4 particleInfo : TEXCOORD3;
			};

			sampler2D _MainTex;
			float4 _PosArray[64];
			
			v2f vert (appdata v)
			{
				v2f o;
				
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv.xy;

				// billboard mesh towards camera
				float3 vpos = mul((float3x3) unity_ObjectToWorld, v.vertex.xyz);

				//float4 worldCoord = float4(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23, 1);
				float4 worldCoord = float4(_PosArray[v.particleInfo.x].xyz, 1);

				float4 viewPos = mul(UNITY_MATRIX_V, worldCoord) + float4(vpos, 0);
				float4 outPos = mul(UNITY_MATRIX_P, viewPos);

				o.vertex = outPos;

				o.particleInfo = v.particleInfo;

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return fixed4(i.particleInfo.x / 64, 0, 0, 1);
			}
			ENDCG
		}
	}
}
