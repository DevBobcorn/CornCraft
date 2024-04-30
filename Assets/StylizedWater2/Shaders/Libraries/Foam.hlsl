//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

#ifndef WATER_FOAM_INCLUDED
#define WATER_FOAM_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_FoamTex);
SAMPLER(sampler_FoamTex);

#define FOAM_CHANNEL 0

float SampleFoamTexture(TEXTURE2D_PARAM(tex, samplerName), float2 uv, float2 tiling, float subTiling, float2 time, float speed, float subSpeed, float slopeMask, float slopeSpeed, float slopeStretch, bool slopeFoamOn)
{
	float4 uvs = PackedUV(uv, tiling, time, speed, subTiling, subSpeed);

	float f1 = SAMPLE_TEXTURE2D(tex, samplerName, uvs.xy)[FOAM_CHANNEL];	
	float f2 = SAMPLE_TEXTURE2D(tex, samplerName, uvs.zw)[FOAM_CHANNEL];

	#if UNITY_COLORSPACE_GAMMA
	f1 = SRGBToLinear(f1);
	f2 = SRGBToLinear(f2);
	#endif

	float foam = saturate(f1 + f2);

	if(slopeFoamOn)
	{
		uvs = PackedUV(uv, tiling, time, speed * slopeSpeed, subTiling, subSpeed * slopeSpeed);
		//Stretch UV vertically on slope
		uvs.yw *= 1-slopeStretch;

		//Cannot reuse the same UV, slope foam needs to be resampled and blended in
		float f3 = SAMPLE_TEXTURE2D(tex, samplerName, uvs.xy)[FOAM_CHANNEL];
		float f4 = SAMPLE_TEXTURE2D(tex, samplerName, uvs.zw)[FOAM_CHANNEL];

		#if UNITY_COLORSPACE_GAMMA
		f3 = SRGBToLinear(f3);
		f4 = SRGBToLinear(f4);
		#endif

		const half slopeFoam = saturate(f3 + f4);
	
		foam = lerp(foam, slopeFoam, slopeMask);
	}

	return foam;
}

float SampleFoamTexture(float2 uv, float2 tiling, float subTiling, float2 time, float speed, float subSpeed, float slopeMask, float slopeSpeed, half slopeStretch, bool slopeFoamOn)
{
	return SampleFoamTexture(TEXTURE2D_ARGS(_FoamTex, sampler_FoamTex), uv, tiling, subTiling, time, speed, subSpeed, slopeMask, slopeSpeed, slopeStretch, slopeFoamOn);
}
#endif