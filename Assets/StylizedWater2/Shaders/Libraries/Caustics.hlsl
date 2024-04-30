#include "Common.hlsl"

bool _EnableDirectionalCaustics;
float4x4 CausticsProjection;

TEXTURE2D(_CausticsTex);
SAMPLER(sampler_CausticsTex);

float2 GetCausticsProjection(in float4 positionCS, in float3 lightDir, float3 positionWS, inout half attenuation)
{
	#if !_DISABLE_DEPTH_TEX
	if(_EnableDirectionalCaustics)
	{
		#if ADVANCED_SHADING
		const float3 sceneWorldNormal = ReconstructWorldNormal(positionCS);

		const half NdotL = saturate(dot(sceneWorldNormal, lightDir));
		attenuation *= NdotL;
		#endif
		
		//CausticsProjection matrix set up through scripting
		return mul(CausticsProjection, float4(positionWS, 1.0)).xy;
	}
	#endif

	return positionWS.xz;
}

float3 SampleCaustics(float2 uv, float2 time, float tiling, float chromance)
{
	//return SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, uv * tiling).rgb;
	
	float3 caustics1 = SAMPLE_TEXTURE2D_LOD(_CausticsTex, sampler_CausticsTex, uv * tiling + (time.xy), 0).rgb;
	float3 caustics2 = SAMPLE_TEXTURE2D_LOD(_CausticsTex, sampler_CausticsTex, (uv * tiling * 0.8) - (time.xy), 0).rgb;
	
	#if UNITY_COLORSPACE_GAMMA
	caustics1 = SRGBToLinear(caustics1);
	caustics2 = SRGBToLinear(caustics2);
	#endif

	float3 caustics = min(caustics1, caustics2) * 2.0;
	//float3 caustics = (caustics1 +caustics2);

	#if HQ_CAUSTICS
	float3 caustics3 = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, (uv * tiling * 0.6) + (time.xy * 1.2)).rgb * 1;
	float3 caustics4 = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, (uv * tiling * 0.55) - (time.xy * 1.2)).rgb * 1;
	caustics4 = min(caustics3, caustics4);
	
	caustics = min(caustics, caustics4) * 2.0;
	#endif
	
	return lerp(caustics.rrr, caustics.rgb, chromance);
}

//Backwards compatibility for Underwater Rendering
float3 SampleCaustics(float2 uv, float2 time, float tiling)
{
	return SampleCaustics(uv, time, tiling, 1.0);
}