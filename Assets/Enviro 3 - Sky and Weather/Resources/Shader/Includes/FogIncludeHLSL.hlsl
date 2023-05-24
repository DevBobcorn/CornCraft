#include "SkyInclude.cginc"
#pragma multi_compile __ ENVIRO_VOLUMELIGHT
 
TEXTURE2D_X(_EnviroVolumetricFogTex);
SAMPLER(sampler_EnviroVolumetricFogTex);
float4 _EnviroVolumetricFogTex_TexelSize;
float4 _Screen_TexelSize;

uniform float4 _EnviroFogParameters; //x = rayorigin1, y = falloff1, z = density1, w = height1
uniform float4 _EnviroFogParameters2; //x = rayorigin2, y = falloff2, z = density2, w = height2 
uniform float4 _EnviroFogParameters3; //x = maxDensity, y = startDistance, z = , w = sky blend
uniform float4 _EnviroFogColor; //Fog color
uniform float4 _EnviroDirLightColor;

#if defined(SHADER_API_D3D11) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN)
    struct EllipsZones
    {
        float3 pos;
        float radius;
        float3 axis;
        float stretch;
        float density;
        float feather;
        float padding1;
        float padding2;
    };
    StructuredBuffer<EllipsZones> _EllipsZones : register(t1);
    float _EllipsZonesCount; 
#endif

int ihash(int n)
{
	n = (n<<13)^n;
	return (n*(n*n*15731+789221)+1376312589) & 2147483647;
}

float frand(int n)
{
	return ihash(n) / 2147483647.0;
}

float2 cellNoise(int2 p)
{
	int i = p.y*256 + p.x;
	return float2(frand(i), frand(i + 57)) - 0.5;//*2.0-1.0;
} 

float Pow2(float x) 
{ 
    return x * x;
}

// Calculate the line integral of the ray from the camera to the receiver position through the fog density function
// The exponential fog density function is d = GlobalDensity * exp(-HeightFalloff * y)
float CalculateLineIntegralShared(float FogHeightFalloff, float RayDirectionY, float RayOriginTerms)
{
    float Falloff = max(-127.0f, FogHeightFalloff * RayDirectionY);    // if it's lower than -127.0, then exp2() goes crazy in OpenGL's GLSL.
    float LineIntegral = (1.0f - exp2(-Falloff)) / Falloff;
    float LineIntegralTaylor = log(2.0) - (0.5 * Pow2(log(2.0))) * Falloff;		// Taylor expansion around 0

    return RayOriginTerms * (abs(Falloff) > 0.01f ? LineIntegral : LineIntegralTaylor);
}

#if defined(SHADER_API_D3D11) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN)

void FogZones(float3 pos, inout float density)
{
    for (int i = 0; i < _EllipsZonesCount; i++)
    {
        float3 dir = _EllipsZones[i].pos - pos;
        float3 axis = _EllipsZones[i].axis;
        float3 dirAlongAxis = dot(dir, axis) * axis;

        //float scrollNoise = ScrollNoise(dir, _EllipsZones[i].noiseSpeed, _EllipsZones[i].noiseScale, axis, _EllipsZones[i].noiseAmount);

        dir = dir + dirAlongAxis * _EllipsZones[i].stretch;
        float distsq = dot(dir, dir);
        float radius = _EllipsZones[i].radius;
        float feather = _EllipsZones[i].feather;
        // float feather = 0.3;
        feather = (1.0 - smoothstep (radius * feather, radius, distsq));

        float contribution = feather * _EllipsZones[i].density;
        density = density + contribution;
    }
} 
#endif

half4 GetExponentialHeightFog(float3 wPos, float linearDepth) 
{
    const half MinFogOpacity = _EnviroFogParameters3.x;
 
    //Receiver
    float3 CameraToReceiver = wPos - _WorldSpaceCameraPos.xyz;
    float3 viewDirection = CameraToReceiver;
    float viewLength = length(viewDirection);
    viewDirection /= viewLength;
    float CameraToReceiverLengthSqr = dot(CameraToReceiver, CameraToReceiver);
    float CameraToReceiverLengthInv = rsqrt(CameraToReceiverLengthSqr);
    float CameraToReceiverLength = CameraToReceiverLengthSqr * CameraToReceiverLengthInv;
    half3 CameraToReceiverNormalized = CameraToReceiver * CameraToReceiverLengthInv;

    float RayOriginTerms = _EnviroFogParameters.x;
    float RayOriginTermsSecond = _EnviroFogParameters2.x;
    float RayLength = CameraToReceiverLength;
    float RayDirectionY = CameraToReceiver.y;

    // Factor in StartDistance
    float ExcludeDistance = _EnviroFogParameters3.y;

    if (ExcludeDistance > 0)
    {
        float ExcludeIntersectionTime = ExcludeDistance * CameraToReceiverLengthInv;
        float CameraToExclusionIntersectionY = ExcludeIntersectionTime * CameraToReceiver.y;
        float ExclusionIntersectionY = _WorldSpaceCameraPos.y + CameraToExclusionIntersectionY;
        float ExclusionIntersectionToReceiverY = CameraToReceiver.y - CameraToExclusionIntersectionY;

        RayLength = (1.0f - ExcludeIntersectionTime) * CameraToReceiverLength;
        RayDirectionY = ExclusionIntersectionToReceiverY;
        
        float Exponent = max(-127.0f, _EnviroFogParameters.y * (ExclusionIntersectionY - _EnviroFogParameters.w));
        RayOriginTerms = _EnviroFogParameters.z * exp2(-Exponent);

        float ExponentSecond = max(-127.0f, _EnviroFogParameters2.y * (ExclusionIntersectionY - _EnviroFogParameters2.w));
        RayOriginTermsSecond = _EnviroFogParameters2.z * exp2(-ExponentSecond);
    }  
 
    // Calculate fog amount of both layers
    float fogAmount = (CalculateLineIntegralShared(_EnviroFogParameters.y, RayDirectionY, RayOriginTerms) + CalculateLineIntegralShared(_EnviroFogParameters2.y, RayDirectionY, RayOriginTermsSecond))* RayLength;
    
    //Fog Zones
#if defined(SHADER_API_D3D11) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN)
    FogZones(wPos,fogAmount);
#endif
    
    // Calculate the amount of light that made it through the fog using the transmission equation
    float fogfactor = max(saturate(exp2(-fogAmount)), MinFogOpacity);

    // Color
    float4 sky = GetSkyColor(viewDirection,0.005f);
    float3 inscatterColor = lerp(_EnviroFogColor.rgb,sky.rgb,_EnviroFogParameters3.w);

    float3 fogColor = inscatterColor * (1 - fogfactor);

    return float4(fogColor, fogfactor);
}

float3 ApplyVolumetricLights(float4 fogColor, float3 sceneColor, float2 uv)
{  
    #if defined(ENVIRO_VOLUMELIGHT) 
    float4 volumeLightsSample = SAMPLE_TEXTURE2D_X(_EnviroVolumetricFogTex, sampler_EnviroVolumetricFogTex, uv);
     //uvs += cellNoise(uvs.xy * _Screen_TexelSize.zw) * _VolumeScatter_TexelSize.xy * 0.8;
    float3 volumeLightsDirectional = volumeLightsSample.a * _EnviroDirLightColor.rgb;
    float3 volumeLights = volumeLightsSample.rgb;  
    return (sceneColor.rgb * fogColor.a + fogColor.rgb * max(volumeLightsDirectional,0.75)) + volumeLights;
    #else
    return sceneColor.rgb * fogColor.a + fogColor.rgb; 
    #endif
}

float3 ApplyFogAndVolumetricLights(float3 sceneColor, float2 uv, float3 wPos, float linearDepth)
{
    float4 fog = GetExponentialHeightFog(wPos,linearDepth);
    return ApplyVolumetricLights(fog,sceneColor,uv);
}

