//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

#if defined(SHADER_API_XBOXONE) || defined(SHADER_API_PSSL)
// AMD recommends this value for GCN http://amd-dev.wpengine.netdna-cdn.com/wordpress/media/2013/05/GCNPerformanceTweets.pdf
#define MAX_TESSELLATION_FACTORS 15.0
#else
#define MAX_TESSELLATION_FACTORS 64.0
#endif

#if defined(SHADER_API_GLES2)
#warning Current graphics API does not support tessellation, falling back to non-tessellated shader automatically.
#else
#define UNITY_CAN_COMPILE_TESSELLATION
#endif

struct TessellationFactors
{
	float edge[3] : SV_TessFactor;
	float inside  : SV_InsideTessFactor;
};

struct VertexControl
{
	float4 positionOS : INTERNALTESSPOS;
	float4 normalOS : NORMAL;
	float4 tangentOS : TANGENT;
	float4 uv : TEXCOORD0;
	float4 color : COLOR;

	#ifdef LIGHTMAP_ON
	float2 staticLightmapUV  : TEXCOORD1;
	#endif
	#ifdef DYNAMICLIGHTMAP_ON
	float2 dynamicLightmapUV  : TEXCOORD2;
	#endif
				
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

VertexControl VertexTessellation(Attributes input)
{
	VertexControl output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);

	output.positionOS = input.positionOS;
	output.normalOS = input.normalOS;
	output.tangentOS = input.tangentOS;
	output.uv.xy = input.uv.xy;
	output.uv.z = _TimeParameters.x;
	output.uv.w = 0;
	output.color = input.color;

	#ifdef LIGHTMAP_ON
	output.staticLightmapUV = input.staticLightmapUV.xy * unity_LightmapST.xy + unity_LightmapST.zw;
	#endif
	#ifdef DYNAMICLIGHTMAP_ON
	output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
	#endif
	
	return output;
}

float CalcDistanceTessFactor(float4 positionOS, float minDist, float maxDist, float tess)
{
	float3 positionWS = TransformObjectToWorld(positionOS.xyz).xyz;
	float dist = distance(positionWS, GetCurrentViewPosition());

	float f = (1.0-saturate((dist - minDist) / (maxDist - minDist)) + 0.001) * tess;
	
	#if DYNAMIC_EFFECTS_ENABLED
	//Doesn't seem to work somehow
	//f += SampleDynamicEffectsDisplacement(positionWS.xyz) * tess;
	#endif
	
	return f;
}

float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
{
	float4 tess;
	tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
	tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
	tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
	tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
	return tess;
}

float4 DistanceBasedTess(float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist)
{
	float3 f;
	f.x = CalcDistanceTessFactor(v0, minDist, maxDist, tess);
	f.y = CalcDistanceTessFactor(v1, minDist, maxDist, tess);
	f.z = CalcDistanceTessFactor(v2, minDist, maxDist, tess);

	//Don't use the Core RP version, creates cracks on edges
	return CalcTriEdgeTessFactors(f);
}

TessellationFactors HullConstant(InputPatch<VertexControl, 3> patch)
{
	TessellationFactors output;

	float4 tf = DistanceBasedTess(patch[0].positionOS, patch[1].positionOS, patch[2].positionOS, _TessValue, _TessMin, _TessMax);

	UNITY_SETUP_INSTANCE_ID(patch[0]);
	
	output.edge[0] = tf.x;
	output.edge[1] = tf.y;
	output.edge[2] = tf.z;
	output.inside = tf.w;
	
	return output;
}

[maxtessfactor(MAX_TESSELLATION_FACTORS)]
[domain("tri")]
[partitioning("fractional_odd")]
[outputtopology("triangle_cw")]
[patchconstantfunc("HullConstant")]
[outputcontrolpoints(3)]
VertexControl Hull(InputPatch<VertexControl, 3> input, uint id : SV_OutputControlPointID)
{
	return input[id];
}

#define TESSELLATION_INTERPOLATE_BARY_URP(name, bary) output.name = input[0].name * bary.x +  input[1].name * bary.y +  input[2].name * bary.z

[domain("tri")]
Varyings Domain(TessellationFactors factors, OutputPatch<VertexControl, 3> input, float3 baryCoords : SV_DomainLocation)
{
	Attributes output = (Attributes)0;

	TESSELLATION_INTERPOLATE_BARY_URP(positionOS, baryCoords);
	TESSELLATION_INTERPOLATE_BARY_URP(uv, baryCoords);
	TESSELLATION_INTERPOLATE_BARY_URP(normalOS, baryCoords);
	TESSELLATION_INTERPOLATE_BARY_URP(tangentOS, baryCoords);
	TESSELLATION_INTERPOLATE_BARY_URP(color, baryCoords);
	
	#if defined(LIGHTMAP_ON)
	TESSELLATION_INTERPOLATE_BARY_URP(staticLightmapUV, baryCoords);
	#endif
	#if defined(DYNAMICLIGHTMAP_ON)
	TESSELLATION_INTERPOLATE_BARY_URP(dynamicLightmapUV, baryCoords);
	#endif

	//Tessellation does not work entirely correct with GPU instancing
	UNITY_TRANSFER_INSTANCE_ID(input[0], output);

	#if !defined(SHADERPASS_DISPLACEMENT) //Displacement will be calculated per pixel
	#if _WAVES && defined(TESSELLATION_ON)
	//Required to repeat these
	VertexNormalInputs normalInput = GetVertexNormalInputs(output.normalOS.xyz, output.tangentOS);
	float3 positionWS = TransformObjectToWorld(output.positionOS.xyz);
	float4 vertexColor = GetVertexColor(output.color.rgba, float4(_IntersectionSource > 0 ? 1 : 0, _VertexColorDepth, _VertexColorWaveFlattening, _VertexColorFoam));
		
	//Returns mesh or world-space UV
	float2 uv = GetSourceUV(output.uv.xy, positionWS.xz, _WorldSpaceUV);

	//Vertex animation
	WaveInfo waves = GetWaveInfo(uv, positionWS, TIME_VERTEX * _WaveSpeed, _WaveHeight, lerp(1, 0, vertexColor.b), _WaveFadeDistance.x, _WaveFadeDistance.y);

	//Offset in direction of normals (only when using mesh uv)
	if(_WorldSpaceUV == 0) waves.position *= normalInput.normalWS.xyz;

	positionWS += waves.position;
	
	output.positionOS.xyz = TransformWorldToObject(positionWS);
	#endif
	#endif
	
	//Wave animation is skipped here
	return LitPassVertex(output);
}