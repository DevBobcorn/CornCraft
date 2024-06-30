//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

//Double sample depth to avoid depth discrepancies 

#define COLLAPSIBLE_GROUP 1

//Normalize the amount of normal-based distortion between reflection probes and screen-space reflections
#define SCREENSPACE_REFLECTION_DISTORTION_MULTIPLIER 0.25

struct SceneData
{
	float4 positionSS; //Unnormalized
	float2 screenPos; //Normalized and no refraction
	float3 positionWS;
	float3 color;

	#if defined(SCENE_SHADOWMASK)
	float shadowMask;
	#endif
	
	float viewDepth;
	float verticalDepth;
	
	#if RESAMPLE_REFRACTION_DEPTH && _REFRACTION
	float viewDepthRefracted;
	float verticalDepthRefracted;
	#endif
	
	float skyMask;

	//More easy debugging
	half refractionMask;
};

void PopulateSceneData(inout SceneData scene, Varyings input, WaterSurface water)
{	
	scene.positionSS = input.screenPos;
	scene.screenPos = scene.positionSS.xy / scene.positionSS.w;

	//Default for disabled depth texture
	scene.viewDepth = 1;
	scene.verticalDepth = 1;

	scene.refractionMask = 1.0;
	#if !_DISABLE_DEPTH_TEX
	SceneDepth depth = SampleDepth(scene.positionSS);
	scene.positionWS = ReconstructWorldPosition(scene.positionSS, water.viewDelta, depth);
	
	//Invert normal when viewing backfaces
	float normalSign = ceil(dot(water.viewDir, water.waveNormal));
	normalSign = normalSign == 0 ? -1 : 1;

	//Z-distance to opaque surface
	scene.viewDepth = SurfaceDepth(depth, input.positionCS);
	//Distance to opaque geometry in normal direction
	scene.verticalDepth = DepthDistance(water.positionWS, scene.positionWS, water.waveNormal * normalSign);

	//Compare position of water to opaque geometry, in order to filter out pixels in front of the water for refraction
	#if _REFRACTION
		SceneDepth depthRefracted = SampleDepth(scene.positionSS + water.refractionOffset);
		float3 opaqueWorldPosRefracted = ReconstructWorldPosition(scene.positionSS + water.refractionOffset, water.viewDelta, depthRefracted);

		//Reject any offset pixels in front of the water surface
		scene.refractionMask = saturate(SurfaceDepth(depthRefracted, input.positionCS));
		//Lerp to un-refracted screen-position
		water.refractionOffset *= scene.refractionMask;
	
		#if RESAMPLE_REFRACTION_DEPTH
		//With the current screen-space UV known, re-compose the water density
		depthRefracted = SampleDepth(scene.positionSS + water.refractionOffset);
		opaqueWorldPosRefracted = ReconstructWorldPosition(scene.positionSS + water.refractionOffset, water.viewDelta, depthRefracted);

		//Also use the world-position sample as the representation of the underwater geometry (more accurate)
		scene.positionWS = lerp(scene.positionWS, opaqueWorldPosRefracted, scene.refractionMask);
	
		scene.viewDepthRefracted = SurfaceDepth(depthRefracted, input.positionCS);
		scene.verticalDepthRefracted = DepthDistance(water.positionWS, opaqueWorldPosRefracted, water.waveNormal * normalSign);
		#endif
	#endif

	#if defined(SCENE_SHADOWMASK)
		float4 sceneShadowCoords = TransformWorldToShadowCoord(scene.positionWS);
		#if UNITY_VERSION >= 202020
		Light sceneLight = GetMainLight(sceneShadowCoords, scene.positionWS, 1.0);
		#else
		Light sceneLight = GetMainLight(sceneShadowCoords);
		#endif
		
		scene.shadowMask = sceneLight.shadowAttenuation;
	#endif

	#if !_RIVER && _ADVANCED_SHADING
		half VdotN = 1.0 - saturate(dot(water.viewDir, water.waveNormal));
		float grazingTerm = saturate(pow(VdotN, 64));
	
		//Resort to z-depth at surface edges. Otherwise makes intersection/edge fade visible through the water surface
		scene.verticalDepth = lerp(scene.verticalDepth, scene.viewDepth, grazingTerm);

		#if RESAMPLE_REFRACTION_DEPTH && _REFRACTION
		scene.verticalDepthRefracted = lerp(scene.verticalDepthRefracted, scene.viewDepthRefracted, grazingTerm);
		#endif
	#endif
	
	#endif

	#if _REFRACTION || UNDERWATER_ENABLED
	float dispersion = _RefractionChromaticAberration * lerp(1.0, 2.0,  unity_OrthoParams.w);

	#if UNDERWATER_ENABLED
	//Behaviour, pre v1.4.1
	//dispersion *= water.vFace;
	#endif

	scene.color = SampleOpaqueTexture(scene.positionSS, water.refractionOffset.xy, dispersion);
	#endif

	//Skybox mask is used for backface (underwater) reflections, to blend between refraction and reflection probes
	scene.skyMask = 0;
	#ifdef DEPTH_MASK

		#if !_DISABLE_DEPTH_TEX
		float depthSource = depth.linear01;
		
		#if RESAMPLE_REFRACTION_DEPTH && _REFRACTION
		//Use depth resampled with refracted screen UV
		depthSource = depthRefracted.linear01;
		#endif
				
		scene.skyMask = depthSource > 0.99 ? 1 : 0;
		#endif
	#endif
}

float GetWaterDensity(SceneData scene, float mask, float heightScalar, float viewDepthScalar, bool exponential)
{
	//Best default value, otherwise water just turns invisible (infinitely shallow)
	float density = 1.0;
	
	#if !_DISABLE_DEPTH_TEX

	float viewDepth = scene.viewDepth;
	float verticalDepth = scene.verticalDepth;

		#if defined(RESAMPLE_REFRACTION_DEPTH) && _REFRACTION
		viewDepth = scene.viewDepthRefracted;
		verticalDepth = scene.verticalDepthRefracted;
		#endif

	float depthAttenuation = 1.0 - exp(-viewDepth * viewDepthScalar * 0.1);
	float heightAttenuation = saturate(lerp(verticalDepth * heightScalar, 1.0 - exp(-verticalDepth * heightScalar), exponential));
	
	density = max(depthAttenuation, heightAttenuation);
	
	#endif

	#if !_RIVER
	//Use green vertex color channel to subtract density
	density = saturate(density - mask);
	#endif

	return density;
}

float3 GetWaterColor(SceneData scene, float3 scatterColor, float density, float absorption)
{
	float depth = scene.verticalDepth;
	float accumulation = scene.viewDepth;

	#if defined(RESAMPLE_REFRACTION_DEPTH) && _REFRACTION
	depth = scene.verticalDepthRefracted;
	accumulation = scene.viewDepthRefracted;
	#endif
	
	//Color of light ray passing through the water, hitting the sea floor (extinction)
	const float3 underwaterColor = saturate(scene.color * exp(-density * (depth + accumulation)));
	//Energy loss of ray, as it travels deeper and scatters (absorption)
	const float scatterAmount = saturate(exp(-absorption * accumulation));

	return lerp(underwaterColor, scatterColor, scatterAmount);
}

//Note: Throws an error about a BLENDWEIGHTS vertex attribute on GLES when VR is enabled (fixed in URP 10+)
//Possibly related to: https://issuetracker.unity3d.com/issues/oculus-a-non-system-generated-input-signature-parameter-blendindices-cannot-appear-after-a-system-generated-value
#if SHADER_API_GLES3 && defined(STEREO_MULTIVIEW_ON)
#define FRONT_FACE_SEMANTIC_REAL SV_IsFrontFace
#define FRONT_FACE_TYPE_REAL bool
#else
#define FRONT_FACE_SEMANTIC_REAL FRONT_FACE_SEMANTIC
#define FRONT_FACE_TYPE_REAL FRONT_FACE_TYPE
#endif

float4 ForwardPassFragment(Varyings input, FRONT_FACE_TYPE_REAL vertexFace : FRONT_FACE_SEMANTIC_REAL) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
	
	//Initialize with null values. Anything that isn't assigned, shouldn't be used either
	WaterSurface water = (WaterSurface)0;
	SceneData scene = (SceneData)0;

	water.alpha = 1.0;
	water.vFace = IS_FRONT_VFACE(vertexFace, true, false); //0 = back face
	//return float4(lerp(float3(1,0,0), float3(0,1,0), water.vFace), 1.0);
	int faceSign = water.vFace > 0 ? 1 : -1;

	//return float4(ReconstructWorldNormal(input.positionCS), 1.0);
	
	/* ========
	// GEOMETRY DATA
	=========== */
	#if COLLAPSIBLE_GROUP

	float4 vertexColor = input.color; //Mask already applied in vertex shader
	//return float4(vertexColor.rgb, 1);

	//Vertex normal in world-space
	float3 normalWS = normalize(input.normalWS.xyz);
#if _NORMALMAP
	float3 WorldTangent = input.tangent.xyz;
	float3 WorldBiTangent = input.bitangent.xyz;

	//return float4(WorldBiTangent, 1.0);
	float3 positionWS = float3(input.normalWS.w, input.tangent.w, input.bitangent.w);
#else
	float3 positionWS = input.positionWS;
#endif

	#if defined(TESSELLATION_ON)
	//Debug tessellation factor
	//return float4(saturate(CalcDistanceTessFactor(float4(TransformWorldToObject(positionWS.xyz), 1.0), _TessMin, _TessMax, _TessValue)).xxx, 1.0);
	#endif

	water.positionWS = positionWS;
	//Not normalized for depth-pos reconstruction. Normalization required for lighting (otherwise breaks on mobile)
	water.viewDelta = GetCurrentViewPosition() - positionWS;
	//water.viewDir = GetWorldSpaceViewDir(positionWS); //Uses the camera's forward vector for orthographic projection, the result isn't as useful
	
	//Note: SafeNormalize() tends to cause issues on mobile when dealing with large numbers
	water.viewDir = normalize(water.viewDelta);
	//return float4(water.viewDir, 1);
	
	half VdotN = 1.0 - saturate(dot(water.viewDir * faceSign, normalWS));
	
	#if _FLAT_SHADING
	float3 dpdx = ddx(positionWS.xyz);
	float3 dpdy = ddy(positionWS.xyz);
	normalWS = normalize(cross(dpdy, dpdx));
	#endif

	water.vertexNormal = normalWS;
	//return float4(water.vertexNormal, 1.0);
		
	//Returns mesh or world-space UV
	float2 uv = GetSourceUV(input.uv.xy, positionWS.xz, _WorldSpaceUV);;
	#endif

	/* ========
	// WAVES
	=========== */
	#if COLLAPSIBLE_GROUP

	water.waveNormal = normalWS;
	
#if _WAVES
	WaveInfo waves = GetWaveInfo(uv, positionWS, TIME * _WaveSpeed, _WaveHeight,  lerp(1, 0, vertexColor.b), _WaveFadeDistance.x, _WaveFadeDistance.y);
	
	#if !_FLAT_SHADING
	waves.normal = normalize(water.vertexNormal + waves.normal);

	//Flatten by blue vertex color weight
	waves.normal = lerp(waves.normal, normalWS, lerp(0, 1, vertexColor.b));
	
	water.waveNormal = waves.normal;
	#endif
	
	//return float4(water.waveNormal.xyz, 1);

	water.offset.y += waves.position.y;
	//For steep waves the horizontal stretching is too extreme, tone it down here
	water.offset.xz += waves.position.xz;
#endif

	#endif

	#if _WAVES
	//After wave displacement, recalculated world-space UVs
	if(_WorldSpaceUV == 1) uv = GetSourceUV(input.uv.xy, positionWS.xz + water.offset.xz, _WorldSpaceUV);
	//return float4(frac(uv), 0, 1);
	#endif

	#if DYNAMIC_EFFECTS_ENABLED
	float4 dynamicEffectsData = 0;
	if(_ReceiveDynamicEffects)
	{
		dynamicEffectsData = SampleDynamicEffectsData(positionWS.xyz + water.offset.xyz);
		//return float4(BoundsEdgeMask(positionWS.xz).xxx, 1.0);
		//return float4(dynamicEffectsData.rrr, 1.0);
	}
	#endif

	/* ========
	// SHADOWS
	=========== */
	#if COLLAPSIBLE_GROUP

	water.shadowMask = 1.0;
	float4 shadowCoords = float4(0, 0, 0, 0);
	#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	shadowCoords = input.shadowCoord;
	#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
	shadowCoords = TransformWorldToShadowCoord(water.positionWS);
	#endif

	#if UNITY_VERSION >= 202020
	half4 shadowMask = 1.0;
	#if UNITY_VERSION >= 202030
	shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
	#endif
	Light mainLight = GetMainLight(shadowCoords, water.positionWS, shadowMask);
	#else
	Light mainLight = GetMainLight(shadowCoords);
	#endif

	//return float4(shadowMask.xyz, 1.0);

	#if _LIGHT_LAYERS && UNITY_VERSION >= 202220
	uint meshRenderingLayers = GetMeshRenderingLayer();
	if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
	#endif
	{
		water.shadowMask = mainLight.shadowAttenuation;
	}
	
	//return float4(water.shadowMask.xxx,1);
	half backfaceShadows = 1;
	
	#if UNDERWATER_ENABLED
	//Separate so shadows applied by Unity's lighting do not appear on backfaces
	backfaceShadows = water.shadowMask;
	water.shadowMask = lerp(1.0, water.shadowMask, water.vFace);
	#endif
	#endif

	#if _RIVER
	water.slope = CalculateSlopeMask(water.waveNormal, _SlopeAngleThreshold, _SlopeAngleFalloff);
	//return float4(water.slope.xxx, 1);
	#endif
	
	/* ========
	// NORMALS
	=========== */
	#if COLLAPSIBLE_GROUP
	water.tangentNormal = float3(0.5, 0.5, 1);
	water.tangentWorldNormal = water.waveNormal;
	
	#if DYNAMIC_EFFECTS_ENABLED
	if(_ReceiveDynamicEffects && NORMALS_AVAILABLE)
	{
		float4 dynamicNormals = SampleDynamicEffectsNormals(water.positionWS + water.offset);
		//dynamicNormals.xyz = lerp(water.vertexNormal, dynamicNormals.xyz, dynamicEffectsData.a);
  
		//Composite into wave normal. Not using the tangent normal, since this has variable influence on reflection, dynamic effects should denote geometry curvature
		water.waveNormal = BlendNormalWorldspaceRNM(dynamicNormals.xyz, water.waveNormal, float3(0,1,0));
		//return float4(water.waveNormal, 1.0);
	}
    #endif
	
#if _NORMALMAP
	//Tangent-space
	water.tangentNormal = SampleNormals(uv, _NormalTiling, _NormalSubTiling, positionWS, TIME, _NormalSpeed, _NormalSubSpeed, water.slope, water.vFace);
	//return float4(SRGBToLinear(float3(water.tangentNormal.x * 0.5 + 0.5, water.tangentNormal.y * 0.5 + 0.5, 1)), 1.0);
	
	//Based on wave normal, makes it easier to create blend between the smooth wave normals and high-frequency normal maps
	water.tangentToWorldMatrix = half3x3(WorldTangent, WorldBiTangent, water.waveNormal);

	//World-space
	water.tangentWorldNormal = normalize(TransformTangentToWorld(water.tangentNormal, water.tangentToWorldMatrix));

	//return float4(water.tangentWorldNormal, 1.0);
#endif
	#endif
	
	#if _REFRACTION || UNDERWATER_ENABLED
	float3 refractionViewDir = water.viewDir;

	#if !_RIVER
	//Technically not correct (as opposed to view direction towards the surface world position), but works better for flat water. Value represents the camera's forward vector.
	refractionViewDir = GetWorldToViewMatrix()[2].xyz;
	#endif
	
	water.refractionOffset.xy = RefractionOffset(input.screenPos.xy / input.screenPos.w, refractionViewDir, water.tangentWorldNormal, _RefractionStrength * lerp(1, 0.1,  unity_OrthoParams.w));
	//Float4 so it can simply be added to the un-normalized screen position
	water.refractionOffset.zw = 0;

	//return float4(ScreenEdgeMask(input.screenPos.xy / input.screenPos.w, length(water.refractionOffset.xy)).xxx, 1.0);
	#endif
	
	float2 offsetVector = saturate(water.offset.yy + water.tangentWorldNormal.xz);
	
	//Normals can perturb the screen coordinates, so needs to be calculated first
	PopulateSceneData(scene, input, water);
	
	//return float4(scene.shadowMask.xxx, 1.0);
	//return float4(scene.verticalDepth.xxx, 1.0);
	//return float4(scene.viewDepth.xxx, 1.0);
	//return float4((input.screenPos.xy / input.screenPos.w).xy, 0, 1.0);
	//return float4(frac(scene.positionWS.xyz), 1.0);
	//return float4(frac(water.refractionOffset.xy), 0, 1.0);
	//return float4(scene.refractionMask.xxx, 1.0);
	
	#if UNDERWATER_ENABLED
	ClipSurface(scene.positionSS.xyzw, positionWS, input.positionCS.xyz, water.vFace);
	#endif

	/* =========
	// COLOR + FOG
	============ */
	#if COLLAPSIBLE_GROUP

	water.fog = GetWaterDensity(scene, vertexColor.g, _DepthHorizontal, _DepthVertical, _DepthExp);
	//return float4(water.fog.xxx, 1.0);

	//Albedo
	float4 baseColor = lerp(_ShallowColor, _BaseColor, water.fog);
	//Avoid color bleeding for foam/intersection on clear water (assumes white foam)
	//baseColor = lerp(1.0, baseColor, baseColor.a);
	
	baseColor.rgb += saturate(_WaveTint * water.offset.y);

	water.fog *= baseColor.a;
	water.alpha = baseColor.a;

	#if COLOR_ABSORPTION && _REFRACTION
	if (_ColorAbsorption > 0)
	{
		baseColor.rgb = GetWaterColor(scene, baseColor.rgb, water.fog, _ColorAbsorption * water.vFace);
	}
	#endif
	
	water.albedo.rgb = baseColor.rgb;	
	#endif

	/* ========
	// INTERSECTION FOAM
	=========== */
	#if COLLAPSIBLE_GROUP

	water.intersection = 0;
#if _SHARP_INERSECTION || _SMOOTH_INTERSECTION

	float interSecGradient = 0;
	
	#if !_DISABLE_DEPTH_TEX
	float intersectionHeightDelta = scene.verticalDepth;

	#if defined(RESAMPLE_REFRACTION_DEPTH) && _REFRACTION && defined(INTERSECTION_REFRACTION)
	intersectionHeightDelta = scene.verticalDepthRefracted;
	#endif
	
	interSecGradient = 1-saturate(exp(intersectionHeightDelta) / _IntersectionLength);	
	#endif
	
	if (_IntersectionSource == 1) interSecGradient = vertexColor.r;
	if (_IntersectionSource == 2) interSecGradient = saturate(interSecGradient + vertexColor.r);

	#if DYNAMIC_EFFECTS_ENABLED
	if(_ReceiveDynamicEffects)
	{
		//interSecGradient += dynamicEffectsData[DE_ALPHA_CHANNEL];
	}
	#endif
	
	water.intersection = SampleIntersection(uv.xy + (offsetVector * _IntersectionDistortion), _IntersectionTiling, interSecGradient, _IntersectionFalloff, TIME * _IntersectionSpeed) * _IntersectionColor.a;

	#if UNDERWATER_ENABLED
	//Hide on backfaces
	water.intersection *= water.vFace;
	#endif

	#if _WAVES && !_DISABLE_DEPTH_TEX
	//Prevent from peering through waves when camera is at the water level
	if(positionWS.y < scene.positionWS.y) water.intersection = 0;
	#endif

	//water.density += water.intersection;
	
	//Flatten normals on intersection foam
	water.waveNormal = lerp(water.waveNormal, normalWS, water.intersection);
	//return float4(water.intersection.xxx,1);
#endif

	#if _NORMALMAP
	water.tangentWorldNormal = lerp(water.tangentWorldNormal, water.vertexNormal, water.intersection);
	#endif
	#endif

	/* ========
	// SURFACE FOAM
	=========== */
	#if COLLAPSIBLE_GROUP
	water.foam = 0;
	
#if _FOAM
	bool enableSlopeFoam = false;
	#if _RIVER
	enableSlopeFoam = true;
	#endif
	
	#if !_RIVER
	//Composed mask for foam caps, based on wave height
	float crest = saturate(water.offset.y) * _FoamWaveAmount;

	float foamSlopeMask = 0;
	#else
	//Rivers don't have waves
	float crest = 0;
	float foamSlopeMask = saturate((water.slope * _SlopeFoam) + vertexColor.a);
	#endif

	float baseFoam = saturate(_FoamBaseAmount - water.slope + vertexColor.a);
	float foamMask = crest + baseFoam + foamSlopeMask;

	//Parallaxing
	//half2 distortion = (_FoamDistortion * water.viewDir.xz / saturate(dot(water.waveNormal, water.viewDir)));
	half2 foamDistortion = offsetVector * _FoamDistortion.xx;

	#if _RIVER
	//Only distort sideways, makes the effect appear more like foam is moving around obstacles or shallow rocks
	foamDistortion.y = 0;
	#endif
	
	float foamTex = SampleFoamTexture((uv + foamDistortion.xy), _FoamTiling, _FoamSubTiling, TIME, _FoamSpeed, _FoamSubSpeed, foamSlopeMask, _SlopeSpeed, _SlopeStretching, enableSlopeFoam);
	if(_FoamClipping > 0) foamTex = smoothstep(_FoamClipping, 1.0, foamTex);
	
	//Dissolve the foam based on the input gradient
	foamMask = saturate(1.0 - foamMask);
	water.foam = smoothstep(foamMask, foamMask + 1.0, foamTex) * saturate(_FoamColor.a);

	//Dynamic foam (separately sampled)
	#if DYNAMIC_EFFECTS_ENABLED
	if(_ReceiveDynamicEffects)
	{
		foamDistortion = _FoamDistortion * dynamicEffectsData[DE_DISPLACEMENT_CHANNEL].xx;
	
		foamTex = SampleDynamicFoam((uv + foamDistortion.xy), _FoamTilingDynamic, _FoamSubTilingDynamic, TIME, _FoamSpeedDynamic, _FoamSubSpeedDynamic);

		foamMask = dynamicEffectsData[DE_FOAM_CHANNEL];
		foamMask = saturate(1.0 - foamMask);
		water.foam += smoothstep(foamMask, foamMask + 1.0, foamTex);

		water.foam = saturate(water.foam);
	}
	#endif
	
	#if _NORMALMAP
	water.tangentWorldNormal = lerp(water.tangentWorldNormal, water.waveNormal, water.foam);
	#endif
	//return float4(water.foam.xxx, 1);
#endif
	#endif
	
	/* ========
	// EMISSION (Caustics + Specular)
	=========== */
	#if COLLAPSIBLE_GROUP

	#if _CAUSTICS
	float3 causticsCoords = scene.positionWS;
	#if _DISABLE_DEPTH_TEX
	causticsCoords = uv.xyy;
	#endif
	
	float causticsMask = saturate((1-water.fog) - water.intersection - water.foam - scene.skyMask) * water.vFace;
	
	float2 causticsProjection = GetCausticsProjection(input.positionCS, mainLight.direction, causticsCoords, causticsMask);
	
	#ifdef SCENE_SHADOWMASK
	causticsMask *= scene.shadowMask;
	#endif

	float3 causticsDistortion = lerp(water.waveNormal.xyz, water.tangentWorldNormal.xyz, _CausticsDistortion);

	#if _ADVANCED_SHADING
	//causticsDistortion = TransformWorldToViewDir(causticsDistortion);
	//causticsDistortion.xz = causticsDistortion.xy;
	#endif
	
	water.caustics = SampleCaustics(causticsProjection + causticsDistortion.xz, TIME * _CausticsSpeed, _CausticsTiling, _CausticsChromance);
	
	//return float4(causticsMask.xxx, 1.0);
	
	//Note: not masked by surface shadows, this occurs in the lighting function so it also takes point/spot lights into account
	water.caustics *= causticsMask * _CausticsBrightness;
	//return float4(water.caustics.rgb, 1);
	#endif

#if _NORMALMAP
	if(_SparkleIntensity > 0)
	{
		//Can piggyback on the tangent normal
		half3 sparkles = mainLight.color * saturate(step(_SparkleSize, (water.tangentNormal.y))) * _SparkleIntensity;
	
		#if !_UNLIT
		//Fade out the effect as the sun approaches the horizon
		float sunAngle = saturate(dot(water.vertexNormal, mainLight.direction));
		float angleMask = saturate(sunAngle * 10); /* 1.0/0.10 = 10 */
		sparkles *= angleMask;
		#endif
		
		water.specular += sparkles.rgb;
	}
#endif
	
#ifndef _SPECULARHIGHLIGHTS_OFF
	float3 lightReflectionNormal = water.tangentWorldNormal;

	#if _FLAT_SHADING //Use face normals
	lightReflectionNormal = water.waveNormal;
	#endif

	half specularMask = saturate((1-water.foam * 2.0) * (1-water.intersection) * water.shadowMask);
	//return float4(specularMask.xxx, 1.0);

	float3 sunSpecular = SpecularReflection(mainLight, water.viewDir, water.waveNormal, lightReflectionNormal, _SunReflectionDistortion, lerp(8196, 64, _SunReflectionSize), _SunReflectionStrength * specularMask);
	
	water.specular += sunSpecular;
	//return float4(water.specular, 1.0);
#endif
	//return float4(specular, 1.0);

	//Reflection probe/planar
#ifndef _ENVIRONMENTREFLECTIONS_OFF

	//Blend between smooth surface normal and normal map to control the reflection perturbation (probes only!)
	#if !_FLAT_SHADING 
	float3 refWorldNormal = lerp(water.waveNormal, normalize(water.waveNormal + water.tangentWorldNormal), _ReflectionDistortion);
	#else //Skip, not a good fit
	float3 refWorldNormal = water.waveNormal;
	#endif
	
	half3 reflectionViewDir = water.viewDir;

	#if _REFLECTION_PROBE_BOX_PROJECTION
	//Use the camera's forward vector when the camera is orthographic
	if(unity_OrthoParams.w == 1) reflectionViewDir = GetWorldSpaceViewDir(positionWS);
	#endif
	
	half3 reflectionVector = reflect(-reflectionViewDir, refWorldNormal);

	#if !_RIVER
	//Ensure only the top hemisphere of the reflection probe is used
	//reflectionVector.y = max(0, reflectionVector.y);
	#endif
	
	//Pixel offset for planar reflection, sampled in screen-space
	float3 reflectionOffsetVector = lerp(water.vertexNormal, water.tangentWorldNormal, _ReflectionDistortion);
	
	#if _ADVANCED_SHADING
	//reflectionOffsetVector = TransformWorldToViewDir(reflectionOffsetVector);
	//reflectionOffsetVector.xz = reflectionOffsetVector.xy;
	#endif
	
	float2 reflectionPixelOffset = (reflectionOffsetVector.xz * scene.positionSS.w * SCREENSPACE_REFLECTION_DISTORTION_MULTIPLIER).xy;
	
	water.reflections = SampleReflections(reflectionVector, _ReflectionBlur, scene.positionSS.xyzw, positionWS, refWorldNormal, water.viewDir, reflectionPixelOffset, _PlanarReflectionsEnabled);
	//return float4(water.reflections, 1.0);
	
	float reflectionFresnel = ReflectionFresnel(refWorldNormal, water.viewDir * faceSign, _ReflectionFresnel);
	//return float4(reflectionFresnel.xxx, 1.0);
	
	water.reflectionMask = _ReflectionStrength * reflectionFresnel;
	water.reflectionLighting = 1-_ReflectionLighting;

	#if _UNLIT
	//Nullify, otherwise reflections turn black
	water.reflectionLighting = 1.0;
	#endif
#endif
	#endif

	/* ========
	// COMPOSITION
	=========== */
	#if COLLAPSIBLE_GROUP
	
	//Foam application on top of everything up to this point
	#if _FOAM
	//Mitigate color bleeding into the foam by scaling it
	water.albedo.rgb = lerp(water.albedo.rgb, _FoamColor.rgb, saturate(water.foam * 2.0));
	#endif

	#if _SHARP_INERSECTION || _SMOOTH_INTERSECTION
	//Layer intersection on top of everything
	water.albedo.rgb = lerp(water.albedo.rgb, _IntersectionColor.rgb, water.intersection);
	#endif

	#if _FOAM || _SHARP_INERSECTION || _SMOOTH_INTERSECTION
	//Sum values to compose alpha
	water.alpha = saturate(water.alpha + water.intersection + water.foam);
	#endif

	#ifndef _ENVIRONMENTREFLECTIONS_OFF
	//Foam complete, use it to mask out the reflection (considering that foam is rough)
	water.reflectionMask = saturate(water.reflectionMask - water.foam - water.intersection) * _ReflectionStrength;
	//return float4(reflectionFresnel.xxx, 1);

	#if !_UNLIT
	//Blend reflection with albedo. Diffuse lighting will affect it
	water.albedo.rgb = lerp(water.albedo, lerp(water.albedo.rgb, water.reflections, water.reflectionMask), _ReflectionLighting);
	//return float4(water.albedo.rgb, 1);
	#endif
	#endif
	//return float4(water.reflections.rgb, 1);

	#if !_UNLIT
	//Blend between smooth geometry normal and normal map for diffuse lighting
	water.diffuseNormal = lerp(water.waveNormal, water.tangentWorldNormal, _NormalStrength);
	#endif

	#if _FLAT_SHADING
	//Moving forward, consider the tangent world normal the same as the flat-shaded normals
	water.tangentWorldNormal = water.waveNormal;
	#endif
	
	//Horizon color (note: not using normals, since they are perturbed by waves)
	float fresnel = saturate(pow(VdotN, _HorizonDistance)) * _HorizonColor.a;
	#if UNDERWATER_ENABLED
	fresnel *= water.vFace;
	#endif
	water.albedo.rgb = lerp(water.albedo.rgb, _HorizonColor.rgb, fresnel);

	#if UNITY_COLORSPACE_GAMMA
	//Gamma-space is likely a choice, enabling this will have the water stand out from non gamma-corrected shaders
	//water.albedo.rgb = LinearToSRGB(water.albedo.rgb);
	#endif
	
	//Final alpha
	water.edgeFade = saturate(scene.verticalDepth / (_EdgeFade * 0.01));

	#if UNDERWATER_ENABLED
	water.edgeFade = lerp(1.0, water.edgeFade, water.vFace);
	#endif

	water.alpha *= water.edgeFade;
	#endif

	/* ========
	// TRANSLUCENCY
	=========== */
	TranslucencyData translucencyData = (TranslucencyData)0;
	#if _TRANSLUCENCY
	float scatteringMask = 1.0;
	scatteringMask = saturate((water.fog + water.edgeFade) - (water.reflectionMask * water.vFace)) * water.shadowMask;
	scatteringMask -= water.foam;

	scatteringMask = saturate(scatteringMask);
	
	//return float4(scatteringMask.xxx, 1);

	translucencyData = PopulateTranslucencyData(_ShallowColor.rgb, mainLight.direction, mainLight.color, water.viewDir, water.waveNormal, water.tangentWorldNormal, scatteringMask, _TranslucencyStrength, _TranslucencyStrengthDirect * water.vFace, _TranslucencyExp, _TranslucencyCurvatureMask * water.vFace, true);
	
	#if UNDERWATER_ENABLED
	//Override the strength of the effect for the backfaces, to match the underwater shading post effect
	translucencyData.strength *= lerp(_UnderwaterFogBrightness * _UnderwaterSubsurfaceStrength, 1, water.vFace);
	#endif
	#endif

	/* ========
	// UNITY SURFACE & INPUT DATA
	=========== */
	#if COLLAPSIBLE_GROUP
	SurfaceData surfaceData = (SurfaceData)0;

	surfaceData.albedo = water.albedo.rgb;
	surfaceData.specular = water.specular.rgb;
	surfaceData.metallic = 0;
	surfaceData.smoothness = 0;
	surfaceData.normalTS = water.tangentNormal;
	surfaceData.emission = 0; //To be populated with translucency+caustics
	surfaceData.occlusion = 1.0;
	surfaceData.alpha = water.alpha;

	//https://github.com/Unity-Technologies/Graphics/blob/31106afc882d7d1d7e3c0a51835df39c6f5e3073/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl#L34
	InputData inputData = (InputData)0;
	inputData.positionWS = positionWS;
	inputData.viewDirectionWS = water.viewDir;
	inputData.shadowCoord = shadowCoords;
	#if UNDERWATER_ENABLED
	//Flatten normals for underwater lighting (distracting, peers through the fog)
	inputData.normalWS = lerp(water.waveNormal, water.tangentWorldNormal, water.vFace);
	#else
	inputData.normalWS = water.tangentWorldNormal;
	#endif
	inputData.fogCoord = InitializeInputDataFog(float4(positionWS, 1.0), input.fogFactorAndVertexLight.x);
	inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;

	inputData.bakedGI = 0;
	
	#if defined(DYNAMICLIGHTMAP_ON) && UNITY_VERSION >= 202120
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV.xy, input.vertexSH, inputData.normalWS);
	#elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
	inputData.bakedGI = SAMPLE_GI(input.vertexSH, GetAbsolutePositionWS(inputData.positionWS), inputData.normalWS, inputData.viewDirectionWS, input.positionCS.xy);
    #else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    #endif
	
	#if UNITY_VERSION >= 202020
	inputData.shadowMask = shadowMask;
	#endif

	//Lightmap(static+dynamic) or SH
	//return float4(inputData.bakedGI, 1.0);
	
	#endif

	//return float4(surfaceData.emission, 1.0);
	/* ========
	// RENDERING DEBUGGER (URP 12+)
	=========== */
	#if COLLAPSIBLE_GROUP
	
	#if UNITY_VERSION >= 202120 && defined(DEBUG_DISPLAY)
	inputData.positionCS = input.positionCS;
	#if _NORMALMAP
	inputData.tangentToWorld = water.tangentToWorldMatrix;
	#else
	inputData.tangentToWorld = 0;
	#endif
	inputData.normalizedScreenSpaceUV = scene.positionSS.xy / scene.positionSS.w;
	inputData.shadowMask = water.shadowMask.xxxx;
	#if defined(DYNAMICLIGHTMAP_ON)
	inputData.dynamicLightmapUV = input.dynamicLightmapUV;
	#endif
	#if defined(LIGHTMAP_ON)
	inputData.staticLightmapUV = input.staticLightmapUV;
	#else
	inputData.vertexSH = input.vertexSH;
	#endif

	surfaceData.emission = water.caustics;
	ApplyTranslucency(translucencyData, surfaceData.emission.rgb);

	inputData.brdfDiffuse = surfaceData.albedo;
	inputData.brdfSpecular = surfaceData.specular;
	inputData.uv = uv;
	inputData.mipCount = 0;
	inputData.texelSize = float4(1/uv.x, 1/uv.y, uv.x, uv.y);
	inputData.mipInfo = 0;
	half4 debugColor;
	
	if (_DebugLightingMode == DEBUGLIGHTINGMODE_REFLECTIONS || _DebugLightingMode == DEBUGLIGHTINGMODE_REFLECTIONS_WITH_SMOOTHNESS)
	{
		return float4(water.reflections * (_DebugLightingMode == DEBUGLIGHTINGMODE_REFLECTIONS_WITH_SMOOTHNESS ? water.reflectionMask : 1), 1.0);
	}
	
	if (CanDebugOverrideOutputColor(inputData, surfaceData, debugColor))
	{
		return debugColor;
	}
	#endif
	#endif

	float4 finalColor = float4(ApplyLighting(surfaceData, scene.color, mainLight, inputData, water, translucencyData, _ShadowStrength, water.vFace), water.alpha);
	
	#if _REFRACTION
	finalColor.rgb = lerp(scene.color.rgb, finalColor.rgb, saturate(water.fog + water.intersection + water.foam));
	//The opaque color texture is now used. The "real" alpha value is solely the edge fade factor
	water.alpha = water.edgeFade;
	#endif

	half fogMask = 1.0;
	#if UNDERWATER_ENABLED
	//Limit to front faces as underwater fog already applies to the bottom
	fogMask = water.vFace;
	#endif
		
	ApplyFog(finalColor.rgb, inputData.fogCoord, scene.positionSS, positionWS, fogMask);
	
	#if UNDERWATER_ENABLED
	float3 underwaterColor = ShadeUnderwaterSurface(surfaceData.albedo.rgb, surfaceData.emission.rgb, surfaceData.specular.rgb, scene.color.rgb, scene.skyMask,
		backfaceShadows, inputData.positionWS, inputData.normalWS, water.tangentWorldNormal, water.viewDir, scene.positionSS.xy,
		_ShallowColor.rgb, _BaseColor.rgb, water.vFace, _UnderwaterSurfaceSmoothness, _UnderwaterRefractionOffset);
	
	finalColor.rgb = lerp(underwaterColor, finalColor.rgb, water.vFace);
	water.alpha = lerp(1.0, water.alpha, water.vFace);
	#endif
	
	#ifdef COZY
	//water.alpha = max(water.alpha, GetStylizedFogDensity(positionWS));
	#endif
	
	finalColor.a = water.alpha;

	#if _RIVER
	//Vertex color green channel controls real alpha in this case (not the color depth gradient)
	finalColor.a = water.alpha * saturate(water.alpha - vertexColor.g);
	#endif

	return finalColor;
}