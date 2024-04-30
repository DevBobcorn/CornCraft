//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

#ifndef PIPELINE_INCLUDED
#define PIPELINE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

#ifdef POST_PROCESSING //These would have the core blit library included
#define UNITY_CORE_SAMPLERS_INCLUDED
#endif

#ifndef UNITY_CORE_SAMPLERS_INCLUDED //Backwards compatibility for <2023.1+
#define UNITY_CORE_SAMPLERS_INCLUDED

SamplerState sampler_LinearClamp;
SamplerState sampler_PointClamp;
SamplerState sampler_PointRepeat;
SamplerState sampler_LinearRepeat;
#endif

#ifndef _DISABLE_DEPTH_TEX
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#endif

#if _REFRACTION || UNDERWATER_ENABLED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#endif

#if UNITY_VERSION < 202120 //Unity 2021.2 (URP 12)
real InitializeInputDataFog(float4 positionWS, real vertFogFactor)
{
	#if defined(SHADER_STAGE_FRAGMENT)
	//Older versions calculate the fog factor on a per-vertex basis
	return vertFogFactor;
	#else
	return ComputeFogFactor(vertFogFactor);
	#endif
}

//Otherwise declared in ShaderVariablesFunctions.hlsl
float LinearDepthToEyeDepth(float rawDepth)
{
	#if UNITY_REVERSED_Z
	return _ProjectionParams.z - (_ProjectionParams.z - _ProjectionParams.y) * rawDepth;
	#else
	return _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * rawDepth;
	#endif
}
#endif

// Deprecated in URP 11+ https://github.com/Unity-Technologies/Graphics/pull/2529. Keep function for backwards compatibility
// Compute Normalized Device Coordinate here (this is normally done in GetVertexPositionInputs, but clip and world-space coords are done manually already)
#if UNITY_VERSION >= 202110 && !defined(UNITY_SHADER_VARIABLES_FUNCTIONS_DEPRECATED_INCLUDED)
float4 ComputeScreenPos(float4 positionCS)
{
	return ComputeNormalizedDeviceCoordinates(positionCS);
}
#endif
#endif

#if UNITY_VERSION <= 202010 //Unity 2019 (URP v7+8)
//Not available in older versions
float3 GetCurrentViewPosition()
{
	return _WorldSpaceCameraPos.xyz;
}

float3 GetWorldSpaceViewDir(float3 positionWS)
{
	return normalize(GetCurrentViewPosition() - positionWS);
}
#endif

#if UNITY_VERSION <= 202110
#define LIGHT_LOOP_BEGIN(lightCount) \
for (uint lightIndex = 0; lightIndex < pixelLightCount; ++lightIndex) { \
if (lightIndex >= (uint)lightCount) break;

#define LIGHT_LOOP_END }
#endif