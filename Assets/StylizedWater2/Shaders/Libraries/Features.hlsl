//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

TEXTURE2D(_BumpMapLarge);
TEXTURE2D(_BumpMapSlope);

float3 BlendTangentNormals(float3 a, float3 b)
{
	#if _ADVANCED_SHADING
	return BlendNormalRNM(a, b);
	#else
	return BlendNormal(a, b);
	#endif
}

float3 SampleNormals(float2 uv, float2 tiling, float subTiling, float3 wPos, float2 time, float speed, float subSpeed, float slope, int vFace) 
{
	float4 uvs = PackedUV(uv, tiling, time, speed, subTiling, subSpeed);
	float3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvs.xy));
	float3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvs.zw));

	float3 blendedNormals = BlendTangentNormals(n1, n2);

	#ifdef QUAD_NORMAL_SAMPLES
	uvs = PackedUV(uv, tiling, time.yx, speed, subTiling, subSpeed);
	float3 n4 = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvs.xy * 2.0));
	float3 n5 = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvs.zw * 2.0));

	blendedNormals = BlendTangentNormals(blendedNormals, BlendTangentNormals(n4, n5));
	#endif

#if _DISTANCE_NORMALS
	float pixelDist = length(GetCurrentViewPosition().xyz - wPos.xyz);

	#if UNDERWATER_ENABLED
	//Use vertical distance only for backfaces (underwater). This ensures tiling is reduced when moving deeper into the water, vertically
	pixelDist = lerp(length(GetCurrentViewPosition().xz - wPos.xz), pixelDist, vFace);
	#endif
	
	float fadeFactor = saturate((_DistanceNormalsFadeDist.y - pixelDist) / (_DistanceNormalsFadeDist.y-_DistanceNormalsFadeDist.x));

	float3 largeBlendedNormals;
	
	uvs = PackedUV(uv, _DistanceNormalsTiling.xx, time, speed * 0.5, 0.5, 0.15);
	float3 n1b = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMapLarge, sampler_BumpMap, uvs.xy));
	
	#if _ADVANCED_SHADING //Use 2nd texture sample
	float3 n2b = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMapLarge, sampler_BumpMap, uvs.zw));
	largeBlendedNormals = BlendTangentNormals(n1b, n2b);
	#else
	largeBlendedNormals = n1b;
	#endif
	
	blendedNormals = lerp(largeBlendedNormals, blendedNormals, fadeFactor);
#endif
	
#if _RIVER
	uvs = PackedUV(uv, tiling, time, speed * _SlopeSpeed, subTiling, subSpeed * _SlopeSpeed);
	uvs.xy = uvs.xy * float2(1, 1-_SlopeStretching);
	float3 n3 = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMapSlope, sampler_BumpMap, uvs.xy));

	#if _ADVANCED_SHADING
	n3 = BlendTangentNormals(n3, UnpackNormal(SAMPLE_TEXTURE2D(_BumpMapSlope, sampler_BumpMap, uvs.zw)));
	#endif
	
	blendedNormals = lerp(blendedNormals, n3, slope);
#endif

	#if WAVE_SIMULATION
	BlendWaveSimulation(wPos, blendedNormals);
	#endif
	
	return blendedNormals;
}

float SampleIntersection(float2 uv, float tiling, float gradient, float falloff, float2 time)
{
	float intersection = 0;
	float dist = saturate(gradient / falloff);
	
	float2 nUV = uv * tiling;
	float noise = SAMPLE_TEXTURE2D(_IntersectionNoise, sampler_IntersectionNoise, nUV + time.xy).r;
	
#if _SHARP_INERSECTION
	float sine = sin(time.y * 10.0 - (gradient * _IntersectionRippleDist)) * _IntersectionRippleStrength;

	noise = saturate((noise + sine) * dist + dist);
	intersection = step(_IntersectionClipping, noise);
#elif _SMOOTH_INTERSECTION
	float noise2 = SAMPLE_TEXTURE2D(_IntersectionNoise, sampler_IntersectionNoise, (nUV * 1.5) - (time.xy )).r;

	#if UNITY_COLORSPACE_GAMMA
	noise = SRGBToLinear(noise);
	noise2 = SRGBToLinear(noise2);
	#endif
	
	intersection = saturate(noise + noise2 + dist) * dist;
#endif

	return intersection;
}

float ScreenEdgeMask(float2 screenPos, float length)
{
	float lengthRcp = 1.0f/length;
	float2 t = Remap10(abs(screenPos.xy * 2.0 - 1.0), lengthRcp, lengthRcp);
	return Smoothstep01(t.x) * Smoothstep01(t.y);
}

#define REFRACTION_IOR_RCP 0.7501875 //=1f/1.333f

float2 RefractionOffset(float2 screenPos, float3 viewDir, float3 normalWS, float strength)
{
	//Normalized to match the more accurate method
	float2 offset = normalWS.xz * 0.5;

	#if PHYSICAL_REFRACTION	
	//Light direction as traveling towards the eye, through the water surface
	float3 rayDir = refract(-viewDir, normalWS, REFRACTION_IOR_RCP);
	//Convert to view-space, because the coordinates are used to sample a screen-space texture
	float3 viewSpaceRefraction = TransformWorldToViewDir(rayDir);

	//Prevent streaking at the edges, by lerping to non-screenspace coordinates at the screen edges
	half edgeMask = ScreenEdgeMask(screenPos, length(viewSpaceRefraction.xy));
	//edgeMask = 1.0; //Test, disable
	
	offset.xy = lerp(normalWS.xz * 0.5, viewSpaceRefraction.xy, edgeMask);
	#endif

	return offset * strength;
}

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#define CHROMASHIFT_SIZE 0.05

float3 SampleOpaqueTexture(float4 screenPos, float2 offset, float dispersion)
{
	//Normalize for perspective projection
	screenPos.xy += offset;
	screenPos.xy /= screenPos.w;
	
	float3 sceneColor = SampleSceneColor(screenPos.xy).rgb;
	
	#if PHYSICAL_REFRACTION //Chromatic part
	if(dispersion > 0)
	{
		float chromaShift = (length(offset) * dispersion) / screenPos.w;
		//Note: screen buffer texelsize purposely not used, this way the effect is actually consistent across all resolutions
		float texelOffset = chromaShift * CHROMASHIFT_SIZE;
	
		sceneColor.r = SampleSceneColor(screenPos.xy + float2(texelOffset, 0)).r;
		sceneColor.b = SampleSceneColor(screenPos.xy - float2(texelOffset, 0)).b;
	}
	#endif

	return sceneColor;
}