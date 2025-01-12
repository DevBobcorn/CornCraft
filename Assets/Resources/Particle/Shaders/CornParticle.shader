Shader "CornShader/Unlit/BlockParticle"
{
	Properties
	{
		[MainTexture] _BaseMap("Base Map", 2DArray) = "" {}


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

			UNITY_DECLARE_TEX2DARRAY(_MainTex);

			float4 _PosArray[64]; // X, Y, Z, Scale
			float4 _ColArray[64]; // R, G, B, Light
			float4 _TexArray[64]; // U1, V1, U2, V2
			
			v2f vert (appdata v)
			{
				v2f o;

				// Particle index, beware of precision loss
				int index = floor(v.particleInfo.x + 0.5);

				float scale = _PosArray[index].w;
				
				o.vertex = UnityObjectToClipPos(float4(v.vertex.xyz * scale, 1));
				o.uv = v.uv.xy;

				// billboard mesh towards camera
				float3 vpos = mul((float3x3) unity_ObjectToWorld, v.vertex.xyz * scale);

				float4 worldCoord = float4(_PosArray[index].xyz, 1);

				float4 viewPos = mul(UNITY_MATRIX_V, worldCoord) + float4(vpos, 0);
				float4 outPos = mul(UNITY_MATRIX_P, viewPos);

				o.vertex = outPos;

				o.particleInfo = v.particleInfo;

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// Particle index, beware of precision loss
				int index = floor(i.particleInfo.x + 0.5);

				if (_PosArray[index].w < 0.01) discard;

				fixed4 color = _ColArray[index];

				return fixed4(color.xyz, 1);
			}
			ENDCG
		}
	}
}
