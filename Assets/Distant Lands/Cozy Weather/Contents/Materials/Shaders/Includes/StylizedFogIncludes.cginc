
#ifndef COZY_INCLUDED
#define COZY_INCLUDED

uniform float4 CZY_FogColor1;
uniform float4 CZY_FogColor2;
uniform float4 CZY_FogColor3;
uniform float4 CZY_FogColor4;
uniform float4 CZY_FogColor5;

uniform float CZY_FogColorStart1;
uniform float CZY_FogColorStart2;
uniform float CZY_FogColorStart3;
uniform float CZY_FogColorStart4;

uniform half CZY_FogDepthMultiplier;

uniform float4 CZY_LightColor;
uniform float3 CZY_SunDirection;
uniform half CZY_LightIntensity;
uniform half CZY_LightFalloff;

uniform float CZY_FogSmoothness;
uniform float CZY_FogOffset;
uniform float CZY_FogIntensity;

inline float4 ASE_ComputeGrabScreenPos(float4 pos)
{
#if UNITY_UV_STARTS_AT_TOP
	float scale = -1.0;
#else
	float scale = 1.0;
#endif
	float4 o = pos;
	o.y = pos.w * 0.5f;
	o.y = (pos.y - o.y) * _ProjectionParams.x * scale + o.y;
	return o;
}


float3 HSVToRGB(float3 c)
{
	float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
	return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}


float3 RGBToHSV(float3 c)
{
	float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
	float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
	float d = q.x - min(q.w, q.y);
	float e = 1.0e-10;
	return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float4 Debug(float4 input) {

	return CZY_FogColor3;

}

float4 BlendStylizedFog(float3 worldPos, float4 inColor) {



	//float4 ase_screenPos = float4(i.screenPos.xyz, i.screenPos.w + 0.00000000001);
	//float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
	//ase_screenPosNorm.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
	float eyeDepth = distance(worldPos, _WorldSpaceCameraPos);


	float fogDepth = (CZY_FogDepthMultiplier * sqrt(eyeDepth));

	float4 lerpResult28_g2 = lerp(CZY_FogColor1, CZY_FogColor2, saturate((fogDepth / CZY_FogColorStart1)));
	float4 lerpResult41_g2 = lerp(saturate(lerpResult28_g2), CZY_FogColor3, saturate(((CZY_FogColorStart1 - fogDepth) / (CZY_FogColorStart1 - CZY_FogColorStart2))));
	float4 lerpResult35_g2 = lerp(lerpResult41_g2, CZY_FogColor4, saturate(((CZY_FogColorStart2 - fogDepth) / (CZY_FogColorStart2 - CZY_FogColorStart3))));
	float4 lerpResult113_g2 = lerp(lerpResult35_g2, CZY_FogColor5, saturate(((CZY_FogColorStart3 - fogDepth) / (CZY_FogColorStart3 - CZY_FogColorStart4))));

	float4 tempFogColor = lerpResult113_g2;
	float3 hsvTorgb31_g1 = RGBToHSV(CZY_LightColor.rgb);
	float3 hsvTorgb32_g1 = RGBToHSV(tempFogColor.rgb);
	float3 hsvTorgb39_g1 = HSVToRGB(float3(hsvTorgb31_g1.x, hsvTorgb31_g1.y, (hsvTorgb31_g1.z * hsvTorgb32_g1.z)));
	float3 normalizeResult5_g1 = normalize((worldPos - _WorldSpaceCameraPos));
	float dotResult6_g1 = dot(normalizeResult5_g1, CZY_SunDirection);
	half lightMask = saturate(pow(abs(((dotResult6_g1 * 0.5 + 0.5) * CZY_LightIntensity)), CZY_LightFalloff));

	float alpha = saturate(((1.0 - saturate(((((worldPos - _WorldSpaceCameraPos).y * 0.1) * (1.0 / CZY_FogSmoothness)) + (1.0 - CZY_FogOffset)))) * CZY_FogIntensity));
	float finalFogDensity = (tempFogColor.a * saturate(fogDepth)) * alpha;
	float4 lerpResult43_g1 = lerp(tempFogColor, float4(hsvTorgb39_g1, 0.0), saturate((lightMask * (1.5 * finalFogDensity))));
	float4 lerpResult46_g1 = lerp(inColor, lerpResult43_g1, finalFogDensity);


	return lerpResult46_g1;


}


float GetStylizedFogDensity(float3 worldPos) {



	//float4 ase_screenPos = float4(i.screenPos.xyz, i.screenPos.w + 0.00000000001);
	//float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
	//ase_screenPosNorm.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
	float eyeDepth = distance(worldPos, _WorldSpaceCameraPos);


	float fogDepth = (CZY_FogDepthMultiplier * sqrt(eyeDepth));

	float4 lerpResult28_g2 = lerp(CZY_FogColor1, CZY_FogColor2, saturate((fogDepth / CZY_FogColorStart1)));
	float4 lerpResult41_g2 = lerp(saturate(lerpResult28_g2), CZY_FogColor3, saturate(((CZY_FogColorStart1 - fogDepth) / (CZY_FogColorStart1 - CZY_FogColorStart2))));
	float4 lerpResult35_g2 = lerp(lerpResult41_g2, CZY_FogColor4, saturate(((CZY_FogColorStart2 - fogDepth) / (CZY_FogColorStart2 - CZY_FogColorStart3))));
	float4 lerpResult113_g2 = lerp(lerpResult35_g2, CZY_FogColor5, saturate(((CZY_FogColorStart3 - fogDepth) / (CZY_FogColorStart3 - CZY_FogColorStart4))));

	float4 tempFogColor = lerpResult113_g2;
	float3 hsvTorgb31_g1 = RGBToHSV(CZY_LightColor.rgb);
	float3 hsvTorgb32_g1 = RGBToHSV(tempFogColor.rgb);
	float3 hsvTorgb39_g1 = HSVToRGB(float3(hsvTorgb31_g1.x, hsvTorgb31_g1.y, (hsvTorgb31_g1.z * hsvTorgb32_g1.z)));
	float3 normalizeResult5_g1 = normalize((worldPos - _WorldSpaceCameraPos));
	float dotResult6_g1 = dot(normalizeResult5_g1, CZY_SunDirection);
	half lightMask = saturate(pow(abs(((dotResult6_g1 * 0.5 + 0.5) * CZY_LightIntensity)), CZY_LightFalloff));

	float alpha = saturate(((1.0 - saturate(((((worldPos - _WorldSpaceCameraPos).y * 0.1) * (1.0 / CZY_FogSmoothness)) + (1.0 - CZY_FogOffset)))) * CZY_FogIntensity));
	float finalFogDensity = (tempFogColor.a * saturate(fogDepth)) * alpha;



	return finalFogDensity;


}
#endif