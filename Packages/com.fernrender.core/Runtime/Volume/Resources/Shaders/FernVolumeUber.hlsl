#ifndef UNIVERSAL_FERNVOLUMEUBER_INCLUDED
#define UNIVERSAL_FERNVOLUMEUBER_INCLUDED

// Includes
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

SAMPLER(sampler_BlitTexture);

#if _EDGEDETECION
	TEXTURE2D(_EdgeDetectionTexture);
	SAMPLER(sampler_EdgeDetectionTexture);
#endif

#if _DUALKAWASEBLUR
	TEXTURE2D(_DualKawaseBlurTex0);
	SAMPLER(sampler_DualKawaseBlurTex0);
	float4 _DualKawaseBlurTex0_TexelSize;
#endif

#if _HBAO
	TEXTURE2D_X(_HBAOTex);
	float4 _TargetScale;
	float4 _HistoryBuffer_RTHandleScale;
	float _HBAOIntensity;
#endif

float4 _Edge_Threshold;
float3 _Edge_Color;
float4 _SourceSize;
half _BlurOffsetX;
half _BlurOffsetY;

#if SHADER_API_GLES
struct AttributesUber
{
	float4 positionOS       : POSITION;
	float2 uv               : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};
#else
struct AttributesUber
{
	uint vertexID : SV_VertexID;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};
#endif


struct VaryingsUber
{
	float4 positionCS : SV_POSITION;
	float2 texcoord   : TEXCOORD0;
	#if _DUALKAWASEBLUR
		float4 uv1 : TEXCOORD1;
		float4 uv2 : TEXCOORD2;
		float4 uv3 : TEXCOORD3;
		float4 uv4 : TEXCOORD4;
	#endif
	UNITY_VERTEX_OUTPUT_STEREO
};

VaryingsUber VertUber(AttributesUber input)
{
	VaryingsUber output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	#if SHADER_API_GLES
		float4 pos = input.positionOS;
		float2 uv  = input.uv;
	#else
		float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
		float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);
	#endif

	output.positionCS = pos;
	output.texcoord   = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;

	#if _DUALKAWASEBLUR
		const float2 halfPixel = _DualKawaseBlurTex0_TexelSize.xy * 0.5;
		const float2 offset = float2(1.0 + _BlurOffsetX, 1.0 + _BlurOffsetY);
		output.uv1.xy = output.texcoord + float2(-halfPixel.x * 2.0, 0.0) * offset;
		output.uv1.zw = output.texcoord + float2(-halfPixel.x, halfPixel.y) * offset;
		output.uv2.xy = output.texcoord + float2(0.0, halfPixel.y * 2.0) * offset;
		output.uv2.zw = output.texcoord + halfPixel * offset;
		output.uv3.xy = output.texcoord + float2(halfPixel.x * 2.0, 0.0) * offset;
		output.uv3.zw = output.texcoord + float2(halfPixel.x, -halfPixel.y) * offset;
		output.uv4.xy = output.texcoord + float2(0.0, -halfPixel.y * 2.0) * offset;
		output.uv4.zw = output.texcoord - halfPixel * offset;
	#endif
	
	return output;
}

#if _HBAO
inline half4 FetchOcclusion(float2 uv) {
	uv *= _HistoryBuffer_RTHandleScale.xy;
	return SAMPLE_TEXTURE2D_X(_HBAOTex, sampler_LinearClamp, uv * _TargetScale.zw);
}
#endif

inline half4 FetchSceneColor(float2 uv) {
	//return LOAD_TEXTURE2D_X(_MainTex, positionSS); // load not supported on GLES2
	return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
}

half4 frag(VaryingsUber input) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

	float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv);

	#if _DUALKAWASEBLUR
		float4 dualBlurSum = SAMPLE_TEXTURE2D(_DualKawaseBlurTex0, sampler_DualKawaseBlurTex0, input.uv1.xy);
		dualBlurSum += SAMPLE_TEXTURE2D(_DualKawaseBlurTex0, sampler_DualKawaseBlurTex0, input.uv1.zw) * 2.0;
		dualBlurSum += SAMPLE_TEXTURE2D(_DualKawaseBlurTex0, sampler_DualKawaseBlurTex0, input.uv2.xy);
		dualBlurSum += SAMPLE_TEXTURE2D(_DualKawaseBlurTex0, sampler_DualKawaseBlurTex0, input.uv2.zw) * 2.0;
		dualBlurSum += SAMPLE_TEXTURE2D(_DualKawaseBlurTex0, sampler_DualKawaseBlurTex0, input.uv3.xy);
		dualBlurSum += SAMPLE_TEXTURE2D(_DualKawaseBlurTex0, sampler_DualKawaseBlurTex0, input.uv3.zw) * 2.0;
		dualBlurSum += SAMPLE_TEXTURE2D(_DualKawaseBlurTex0, sampler_DualKawaseBlurTex0, input.uv4.xy);
		dualBlurSum += SAMPLE_TEXTURE2D(_DualKawaseBlurTex0, sampler_DualKawaseBlurTex0, input.uv4.zw) * 2.0;
		dualBlurSum *= 0.0833;
		color.rgb = dualBlurSum.rgb;
		return color;
	#endif

	#if _EDGEDETECION
		half edgeDetect = SAMPLE_TEXTURE2D(_EdgeDetectionTexture, sampler_EdgeDetectionTexture, uv);
		color.rgb = lerp(color.rgb, _Edge_Color, edgeDetect * _Edge_Threshold.w);
	#endif

	#if _HBAO
		half4 ao = FetchOcclusion(uv);
		ao.b = saturate(pow(abs(ao.b), _HBAOIntensity));
		half3 aoColor = lerp(0, half3(1.0, 1.0, 1.0), ao.b);
		color.rgb *= aoColor;

		#if DEBUG_AO
			color.rgb = aoColor;
		#elif DEBUG_NOAO_AO || DEBUG_AO_AOONLY || DEBUG_NOAO_AOONLY
			if (uv.x <= 0.4985) {
				#if DEBUG_NOAO_AO || DEBUG_NOAO_AOONLY
				color = FetchSceneColor(uv);
				#endif // DEBUG_NOAO_AO || DEBUG_NOAO_AOONLY
				return color;
			}
			if (uv.x > 0.4985 && uv.x < 0.5015) {
				return half4(0, 0, 0, 1);
			}
			#if DEBUG_AO_AOONLY || DEBUG_NOAO_AOONLY
				color.rgb = aoColor;
			#endif // DEBUG_AO_AOONLY) || DEBUG_NOAO_AOONLY
		#endif // DEBUG_AO
	#endif

	return color;
}

#endif
