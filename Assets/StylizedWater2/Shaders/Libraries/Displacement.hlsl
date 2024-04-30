//Functionality to sample the water's displacement pre-pass buffer

uniform bool _WaterDisplacementPrePassAvailable;

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

float SampleDisplacementBuffer(float2 uv)
{
	if(_WaterDisplacementPrePassAvailable == false) return 0;
	const float height = _WaterDisplacementBuffer.SampleLevel(sampler_LinearClamp, uv, 0).r;

	//Need to figure out how to determine if a void is hit
	//if(height == 0.0f) return -1000;
	
	return height;
}

//Main function
float SampleWaterHeight(float3 positionWS)
{
	return SampleDisplacementBuffer(WorldToDisplacementUV(positionWS));
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