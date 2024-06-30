//Set this to value to 1 through Shader.SetGlobalFloat to temporarily disable fog for water
float _WaterFogDisabled;

//Authors of third-party fog solutions can reach out to have their method integrated here

#ifdef SCPostEffects
//Macros normally used for cross-RP compatibility
#define LINEAR_DEPTH(depth) Linear01Depth(depth, _ZBufferParams)

//Legacy (pre v2.2.1)
#define DECLARE_TEX(textureName) TEXTURE2D(textureName);
#define DECLARE_RT(textureName) TEXTURE2D_X(textureName);
#define SAMPLE_TEX(textureName, samplerName, uv) SAMPLE_TEXTURE2D_LOD(textureName, samplerName, uv, 0)
#define SAMPLE_RT_LOD(textureName, samplerName, uv, mip) SAMPLE_TEXTURE2D_X_LOD(textureName, samplerName, uv, mip)
#endif

#ifdef AtmosphericHeightFog
//For versions older than 3.2.0, uncomment this
//bool AHF_Enabled;
#endif

//Fragment stage. Note: Screen position passed here is not normalized (divided by w-component)
void ApplyFog(inout float3 color, float fogFactor, float4 screenPos, float3 positionWS, float vFace) 
{
	float3 foggedColor = color;
	
#ifdef UnityFog
	foggedColor = MixFog(color.rgb, fogFactor);
#endif

#ifdef Colorful
	if(_DensityParams.x > 0) foggedColor.rgb = ApplyFog(color.rgb, fogFactor, positionWS, screenPos.xy / screenPos.w);
#endif
	
#ifdef Enviro
	//Distance/height fog enabled?
	if (_EnviroParams.y > 0 || _EnviroParams.z > 0)
	{
		foggedColor.rgb = TransparentFog(float4(color.rgb, 1.0), positionWS, screenPos.xy / screenPos.w, fogFactor).rgb;
	}
#endif

#ifdef Enviro3
	if(any(_EnviroFogParameters) > 0)
	{
		foggedColor.rgb = ApplyFogAndVolumetricLights(color.rgb, screenPos.xy / screenPos.w, positionWS, 0);
	}
#endif
	
#ifdef Azure
	foggedColor.rgb = ApplyAzureFog(float4(color.rgb, 1.0), positionWS).rgb;
#endif

#ifdef AtmosphericHeightFog
	if (AHF_Enabled)
	{
		float4 fogParams = GetAtmosphericHeightFog(positionWS.xyz);
		foggedColor.rgb = lerp(color.rgb, fogParams.rgb, fogParams.a);
	}
#endif

#ifdef SCPostEffects
	//Distance or height fog enabled
	if(_DistanceParams.z == 1 || _DistanceParams.w == 1)
	{
		ApplyTransparencyFog(positionWS, screenPos.xy / screenPos.w, foggedColor.rgb);
	}
#endif

#ifdef COZY
	foggedColor = BlendStylizedFog(positionWS, float4(color.rgb, 1.0)).rgb;
#endif

#ifdef Buto
	#if defined(BUTO_API_VERSION_2) //Buto 2022
	float3 positionVS = TransformWorldToView(positionWS);
	foggedColor = ButoFogBlend(screenPos.xy / screenPos.w, -positionVS.z, color.rgb);
	#else //Buto 2021
	foggedColor = ButoFogBlend(screenPos.xy / screenPos.w, color.rgb);
	#endif
#endif

	#ifndef UnityFog
	//Allow fog to be disabled for water globally by setting the value through script
	foggedColor = lerp(foggedColor, color, _WaterFogDisabled);
	#endif
	
	//Fog only applies to the front faces, otherwise affects underwater rendering
	color.rgb = lerp(color.rgb, foggedColor.rgb, vFace);
}
