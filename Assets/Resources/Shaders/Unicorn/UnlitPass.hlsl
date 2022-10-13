#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

#include "../Library/Common.hlsl"

float4 UnlitPassVertex (float3 positionOS : POSITION) : SV_POSITION {
    // Position in world space
    float3 positionWS = TransformObjectToWorld(positionOS.xyz);
	return TransformWorldToHClip(positionWS);
}

float4 UnlitPassFragment () : SV_TARGET {
    return float4(1.0, 1.0, 0.0, 1.0);
}

#endif