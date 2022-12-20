
#ifndef COZY_SNOW_INCLUDED
#define COZY_SNOW_INCLUDED

uniform float4 CZY_SnowColor;
uniform sampler2D CZY_SnowTexture;
uniform float4 _SnowTexture_ST;
uniform float CZY_SnowScale;
uniform float CZY_SnowAmount;

float3 mod2D289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }

float2 mod2D289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }

float3 permute(float3 x) { return mod2D289(((x * 34.0) + 1.0) * x); }

float snoise(float2 v)
{
	const float4 C = float4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);
	float2 i = floor(v + dot(v, C.yy));
	float2 x0 = v - i + dot(i, C.xx);
	float2 i1;
	i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
	float4 x12 = x0.xyxy + C.xxzz;
	x12.xy -= i1;
	i = mod2D289(i);
	float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0)) + i.x + float3(0.0, i1.x, 1.0));
	float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
	m = m * m;
	m = m * m;
	float3 x = 2.0 * frac(p * C.www) - 1.0;
	float3 h = abs(x) - 0.5;
	float3 ox = floor(x + 0.5);
	float3 a0 = x - ox;
	m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);
	float3 g;
	g.x = a0.x * x0.x + h.x * x0.y;
	g.yz = a0.yz * x12.xz + h.yz * x12.yw;
	return 130.0 * dot(m, g);
}


float2 voronoihash5_g1(float2 p)
{

	p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
	return frac(sin(p) * 43758.5453);
}


float voronoi5_g1(float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId)
{
	float2 n = floor(v);
	float2 f = frac(v);
	float F1 = 8.0;
	float F2 = 8.0; float2 mg = 0;
	for (int j = -1; j <= 1; j++)
	{
		for (int i = -1; i <= 1; i++)
		{
			float2 g = float2(i, j);
			float2 o = voronoihash5_g1(n + g);
			o = (sin(time + o * 6.2831) * 0.5 + 0.5); float2 r = f - g - o;
			float d = 0.5 * dot(r, r);
			if (d < F1) {
				F2 = F1;
				F1 = d; mg = g; mr = r; id = o;
			}
			else if (d < F2) {
				F2 = d;

			}
		}
	}
	return F1;
}


float3 BlendStylizedSnow(float4 i, float3 normal, float3 worldPos)
{
	float2 uv_SnowTexture = _SnowTexture_ST.xy + _SnowTexture_ST.zw;
	float3 ase_worldNormal = normal;
	float3 ase_worldPos = worldPos;
	float2 worldPos_2D = (float2(ase_worldPos.x, ase_worldPos.z));
	float temp_output_6_0_g1 = (1.0 / CZY_SnowScale);
	float simplePerlin2D7_g1 = snoise(worldPos_2D * temp_output_6_0_g1);
	simplePerlin2D7_g1 = simplePerlin2D7_g1 * 0.5 + 0.5;
	float2 coords5_g1 = worldPos_2D * (temp_output_6_0_g1 / 0.1);
	float time5_g2 = 0.0;
	float2 voronoiSmoothId0 = 0;
	float2 id5_g2 = 0;
	float2 uv5_g2 = 0;
	float voroi5_g1 = voronoi5_g1(coords5_g1, time5_g2, id5_g2, uv5_g2, 0, voronoiSmoothId0);
	float4 lerpResult19_g1 = lerp((CZY_SnowColor * tex2D(CZY_SnowTexture, uv_SnowTexture)), i, ((pow((pow(ase_worldNormal.y, 7.0) * (simplePerlin2D7_g1 * (1.0 - voroi5_g1))), 0.5) * 1.0) > (1.0 - CZY_SnowAmount) ? 0.0 : 1.0));
	return lerpResult19_g1.rgb;
}
#endif