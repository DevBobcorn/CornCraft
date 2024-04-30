#ifndef WATER_REFLECTIONS_INCLUDED
#define WATER_REFLECTIONS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

#define AIR_RI 1.000293

//Schlick's BRDF fresnel
float ReflectionFresnel(float3 worldNormal, float3 viewDir, float exponent)
{
	float cosTheta = saturate(dot(worldNormal, viewDir));	
	return pow(max(0.0, AIR_RI - cosTheta), exponent);
}

float AttenuateSSR(float2 uv)
{
	float offset = min(1.0 - max(uv.x, uv.y), min(uv.x, uv.y));

	float result = offset / (0.1);
	result = saturate(result);

	return pow(result, 0.5);
}

void RaymarchSSR(float3 origin, float3 direction, uint samples, half stepSize, half thickness, out half2 sampleUV, out half valid, out half outOfBounds)
{
	sampleUV = 0;
	valid = 0;
	outOfBounds = 0;

	direction *= stepSize;
	const half rcpStepCount = rcp(samples);
 
	UNITY_LOOP
	for(uint i = 0; i <= samples; i++)
	{
		origin += direction;
		direction *= 1+stepSize;

		//View-space to screen-space UV
		sampleUV = ComputeNormalizedDeviceCoordinates(origin, GetViewToHClipMatrix());

		if (any(sampleUV.xy < 0) || any(sampleUV.xy > 1))
		{
			outOfBounds = 1;
			valid = 0;
			break;
		}
		
		outOfBounds = AttenuateSSR(sampleUV);

		//Sample Mip0, gradient sampling cannot work with loops
		float deviceDepth = SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, sampleUV, 0).r;
		
		//Calculate view-space position from UV and depth
		//Not using the ComputeViewSpacePosition function, since this negates the Z-component
		float3 samplePos = ComputeWorldSpacePosition(sampleUV, deviceDepth, UNITY_MATRIX_I_P);

		if(distance(samplePos.z, origin.z) > length(direction) * thickness) continue;
		
		if(samplePos.z > origin.z)
		{
			valid = 1;
			return;
		}
	}
}

TEXTURE2D_X(_PlanarReflection);
SAMPLER(sampler_PlanarReflection);

float3 SampleReflectionProbes(float3 reflectionVector, float3 positionWS, float smoothness, float2 screenPos)
{
	float3 probes = float3(0,0,0);
	
	#if UNITY_VERSION >= 202220
	probes = GlossyEnvironmentReflection(reflectionVector, positionWS, smoothness, 1.0, screenPos.xy).rgb;
	#elif UNITY_VERSION >= 202120
	probes = GlossyEnvironmentReflection(reflectionVector, positionWS, smoothness, 1.0).rgb;
	#else
	probes = GlossyEnvironmentReflection(reflectionVector, smoothness, 1.0).rgb;
	#endif

	return probes;
}

bool _WaterSSREnabled;
float4 _WaterSSRParams;
//X: Steps
//Y: Step size
//Z: Thickness

#define SSR_SAMPLES 12
#define SSR_STEPSIZE 0.75
#define SSR_MAX_DISTANCE 100
#define SSR_THICKNESS 1.0

float3 SampleReflections(float3 reflectionVector, float smoothness, float4 screenPos, float3 positionWS, float3 normalWS, float3 viewDir, float2 pixelOffset, bool planarReflectionsEnabled)
{
	#if !_RIVER || UNITY_VERSION >= 202220
	screenPos.xy += pixelOffset.xy * lerp(1.0, 0.1, unity_OrthoParams.w);
	screenPos /= screenPos.w;
	#endif

	const float3 probes = SampleReflectionProbes(reflectionVector, positionWS, smoothness, screenPos.xy);
	
	float3 reflections = probes;

	#if !_DISABLE_DEPTH_TEX
	if(_WaterSSREnabled)
	{
		const float3 positionVS = TransformWorldToView(positionWS);
		const float3 direction = TransformWorldToViewDir(reflectionVector);

		float2 ssrUV = 0;
		half ssrRayMask, ssrEdgeMask = 0;

		RaymarchSSR(positionVS, direction, SSR_SAMPLES, SSR_STEPSIZE, SSR_THICKNESS, ssrUV, ssrRayMask, ssrEdgeMask);

		const float3 reflectionSS = SampleSceneColor(ssrUV);
	
		reflections = lerp(reflections, reflectionSS, ssrRayMask * ssrEdgeMask);
	}
	#endif
		
	#if !_RIVER //Planar reflections are pointless on curved surfaces, skip
	if(planarReflectionsEnabled)
	{
		float4 planarReflections = SAMPLE_TEXTURE2D_X_LOD(_PlanarReflection, sampler_PlanarReflection, screenPos.xy, 0);
		//Terrain add-pass can output negative alpha values. Clamp as a safeguard against this
		planarReflections.a = saturate(planarReflections.a);
	
		reflections = lerp(reflections, planarReflections.rgb, planarReflections.a);
	}
	#endif
	
	return reflections;
}
#endif