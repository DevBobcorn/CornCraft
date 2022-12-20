
#ifndef COZY_PUDDLES_INCLUDED
#define COZY_PUDDLES_INCLUDED

uniform float CZY_PuddleScale;
uniform float CZY_WetnessAmount;


float2 voronoihash1_g2(float2 p)
{

	p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
	return frac(sin(p) * 43758.5453);
}


float voronoi1_g2(float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId)
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
			float2 o = voronoihash1_g2(n + g);
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
	return (F2 + F1) * 0.5;
}


float2 voronoihash8_g2(float2 p)
{

	p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
	return frac(sin(p) * 43758.5453);
}


float voronoi8_g2(float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId)
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
			float2 o = voronoihash8_g2(n + g);
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


float BlendStylizedPuddles(float3 normal, float3 worldPos)
{
	float3 ase_worldNormal = normal;
	float temp_output_5_0_g2 = (1.0 / CZY_PuddleScale);
	float time1_g2 = 0.0;
	float2 voronoiSmoothId0 = 0;
	float3 ase_worldPos = worldPos;
	float2 appendResult3_g2 = (float2(ase_worldPos.x, ase_worldPos.z));
	float2 coords1_g2 = appendResult3_g2 * temp_output_5_0_g2;
	float2 id1_g2 = 0;
	float2 uv1_g2 = 0;
	float voroi1_g2 = voronoi1_g2(coords1_g2, time1_g2, id1_g2, uv1_g2, 0, voronoiSmoothId0);
	float time8_g2 = 2.16;
	float2 coords8_g2 = (temp_output_5_0_g2 * 3.0);
	float2 id8_g2 = 0;
	float2 uv8_g2 = 0;
	float voroi8_g2 = voronoi8_g2(coords8_g2, time8_g2, id8_g2, uv8_g2, 0, voronoiSmoothId0);
	return ((ase_worldNormal.y * 2.0 * ((1.0 + (voroi1_g2 - 0.0) * (0.0 - 1.0) / (0.4 - 0.0)) + (0.1 + (voroi8_g2 - 0.0) * (-0.3 - 0.1) / (0.21 - 0.0))) * (0.3 + (CZY_WetnessAmount - 0.0) * (1.0 - 0.3) / (1.0 - 0.0))) > (1.0 - (CZY_WetnessAmount * 1.0)) ? 1.0 : 0.0);
	
}
#endif