//Functionality to sample the water's displacement pre-pass buffer

uniform bool _WaterDisplacementPrePassAvailable;
#define VOID_THRESHOLD -1000 //Same value as in DisplacementPrePass class

uniform float3 _WaterDisplacementCoords;
//XY: Bounds min
//Z: Bounds size
uniform Texture2D _WaterDisplacementBuffer;
#ifndef UNITY_CORE_SAMPLERS_INCLUDED
SamplerState sampler_LinearClamp;
#endif

//Position, relative to rendering bounds (normalized 0-1)
float2 WorldToDisplacementUV(float3 positionWS)
{
	return (positionWS.xz - _WaterDisplacementCoords.xy) / _WaterDisplacementCoords.z;
}

//May be used to validate if the sampled height is actually from a water surface
bool HasHitWaterSurface(float height)
{
	return height > VOID_THRESHOLD;
}

float SampleDisplacementBuffer(float2 uv)
{
	if(_WaterDisplacementPrePassAvailable == false) return 0;
	return _WaterDisplacementBuffer.SampleLevel(sampler_LinearClamp, uv, 0).r;
}

//Main function
float SampleWaterHeight(float3 positionWS)
{
	return SampleDisplacementBuffer(WorldToDisplacementUV(positionWS));
}

//Derive a world-space normal from the height data
float3 SampleWaterNormal(float3 positionWS, float strength)
{
	if(_WaterDisplacementPrePassAvailable == false) return float3(0,1,0);
	
	//Note: not using the buffer's texel size so that the sampled result remains consistent across different resolutions.
	const float radius = 1.0 / _WaterDisplacementCoords.z;

	float2 uv = WorldToDisplacementUV(positionWS);

	const float xLeft = SampleDisplacementBuffer(float2(uv.x - radius, uv.y));
	const float xRight = SampleDisplacementBuffer(float2(uv.x + radius, uv.y));
	
	const float yUp = SampleDisplacementBuffer(float2(uv.x, uv.y + radius));
	const float yDown = SampleDisplacementBuffer(float2(uv.x, uv.y - radius));

	float xDelta = (xLeft - xRight) * strength;
	float zDelta = (yUp - yDown) * strength;

	float3 normal = float3(xDelta, 1.0, zDelta);

	return normalize(normal.xyz);
}

//Shader Graph
void SampleWaterHeight_float(float3 positionWS, out float height)
{
	#if defined(SHADERGRAPH_PREVIEW)
	height = 0;
	#else
	height = SampleWaterHeight(positionWS);
#endif
}

//Shader Graph
void SampleWaterNormal_float(float3 positionWS, float strength, out float3 normal)
{
	#if defined(SHADERGRAPH_PREVIEW)
	normal = float3(0,1,0);
	#else
	normal = SampleWaterNormal(positionWS, strength);
#endif
}