Shader "Enviro/HDRP/Sky"
{
    //Properties
    //{
	//	_MoonTex("Moon Tex", 2D) = "black" {}
	//	_MoonGlowTex("Moon Glow Tex", 2D) = "black" {}
	//	_SunTex("Sun Tex", 2D) = "black" {}
	//	_StarsTex ("Stars Tex", Cube) = "black" {}
	//	_GalaxyTex ("Galaxy Tex", Cube) = "black" {}
	//}

	HLSLINCLUDE
	#pragma editor_sync_compilation
	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"
	#include "../Includes/SkyIncludeHLSL.hlsl"

	uniform float4 _SkyMoonParameters;
	uniform float4 _SkySunParameters;
	uniform sampler2D _MoonTex;
	//uniform sampler2D _MoonGlowTex;
	uniform sampler2D _SunTex;
	uniform float4 _MoonColor;
	uniform float _MoonGlowIntensity;
	uniform float _StarIntensity;
	uniform float _GalaxyIntensity;
	uniform float _CirrusClouds;
	uniform float _FlatClouds;
	uniform float _Aurora;
	uniform samplerCUBE _StarsTex;
	uniform samplerCUBE _GalaxyTex;					
	uniform float4x4 _StarsMatrix;
	uniform float4 _AmbientColorTintHDRP;
	uniform float _EnviroSkyIntensity;


	struct VertexInput 
	{
		uint vertexID : SV_VertexID;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};


	struct v2f 
	{
		float4 position : SV_POSITION;
		UNITY_VERTEX_OUTPUT_STEREO
	};

	v2f vert(VertexInput v) 
	{
		v2f o;
		UNITY_SETUP_INSTANCE_ID(v);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

		o.position = GetFullScreenTriangleVertexPosition(v.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
		return o;
	}

	float MoonPhaseFactor(float2 uv, float phase)
	{
		float alpha = 1.0;


		float srefx = uv.x - 0.5;
		float refx = abs(uv.x - 0.5);

		if (phase > 0)
		{
			srefx = (1 - uv.x) - 0.5;
			refx = abs((1 - uv.x) - 0.5);
		}

		phase = abs(_SkyMoonParameters.x);
		float refy = abs(uv.y - 0.5);
		float refxfory = sqrt(0.25 - refy * refy);
		float xmin = -refxfory;
		float xmax = refxfory;
		float xmin1 = (xmax - xmin) * (phase / 2) + xmin;
		float xmin2 = (xmax - xmin) * phase + xmin;

		if (srefx < xmin1)
		{
			alpha = 0;
		}
		else if (srefx < xmin2 && xmin1 != xmin2)
		{
			alpha = (srefx - xmin1) / (xmin2 - xmin1);
		}

		return alpha;
	}

	float3 ScreenSpaceDither(float2 vScreenPos, float3 clr)
	{
		float _DitheringIntensity = 0.25;
		float d = dot(float2(131.0, 312.0), vScreenPos.xy + _Time.y);
		float3 vDither = float3(d, d, d);
		vDither.rgb = frac(vDither.rgb / float3(103.0, 71.0, 97.0)) - float3(0.5, 0.5, 0.5);
		return (vDither.rgb / 15.0) * _DitheringIntensity;
	}   

	float4 frag(v2f i) : SV_Target 
	{			
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

		float3 viewDirWS = GetSkyViewDirWS(i.position.xy);
		float3 dir = -viewDirWS;
		float3 wpos = normalize(mul((float4x4)UNITY_MATRIX_M, dir)).xyz;

		float4 skyColor = float4(0, 0, 0, 1);


		float3 viewDir = normalize(dir);
		//float3 viewDir = normalize(i.texcoord);
		skyColor = GetSkyColor(viewDir, 0.005f);  

		//Stars
		float3 starsUV = mul((float3x3)_StarsMatrix, dir);
		float4 starsTex = texCUBE(_StarsTex, starsUV) * saturate(viewDir.y);
		float4 stars = starsTex * _StarIntensity;
		//skyColor += stars;

		//Galaxy
		float4 galaxyTex = texCUBE(_GalaxyTex, starsUV) * saturate(viewDir.y);
		float4 galaxy = galaxyTex * _GalaxyIntensity;
		//skyColor += galaxy;

		//Sun and Moon UV
		float3 rSun = normalize(cross(_SunDir.xyz, float3(0, -1, 0)));
		float3 uSun = cross(_SunDir.xyz, rSun);
		float2 sunUV = float2(dot(rSun, dir), dot(uSun, dir)) * (21.0 - _SkySunParameters.y) + 0.5;
		float3 rMoon = normalize(cross(_MoonDir.xyz, float3(0, -1, 0)));
		float3 uMoon = cross(_MoonDir.xyz, rMoon);
		float2 moonUV  = float2(dot(rMoon, dir), dot(uMoon, dir)) * (20.7 - _SkyMoonParameters.z) + 0.5;
		
		//Sun
		float4 sun = float4(0,0,0,1);
		float hideBackSun = saturate(dot(_SunDir.xyz, viewDir));
		float4 sunDisk = tex2D(_SunTex, sunUV) * hideBackSun;
		sun = sunDisk * _SunColor * 10;
		skyColor += sun;

		//Moon
		if(_SkyMoonParameters.w > 0.0) 
		{
			float hideBackMoon = saturate(dot(-_MoonDir.xyz, viewDir));
			float4 moon = tex2D(_MoonTex, moonUV) * hideBackMoon;
			float alpha = MoonPhaseFactor(moonUV, _SkyMoonParameters.x);
			float3 moonArea = clamp(moon * 10, 0, 1);
			float starsBehindMoon = 1 - clamp((moonArea * 5), 0, 1);
			moon = lerp(float4(0, 0, 0, 0), moon, alpha);
			moon = moon * _MoonColor;
			//float4 moonGlow = tex2D(_MoonGlowTex, i.moonGlowPos.xy) * hideBackMoon;
			//moonGlow = moonGlow * _MoonColor * _MoonGlowIntensity;
			skyColor += stars * starsBehindMoon;
			skyColor += galaxy * starsBehindMoon;
			skyColor += moon;
		}
		else
		{
			skyColor += stars;
			skyColor += galaxy;
		}
		
		//Aurora 
		if(_Aurora > 0.0)
		{
			float4 aurora = Aurora(wpos); 
			skyColor.rgb += aurora.rgb;
		}

		//Dithering
		//skyColor.rgb += ScreenSpaceDither(i.position.xy,skyColor.rgb);

		float3 cloudsDir = normalize(wpos + float3(0,1,0));

		//Cirrus
		if(_CirrusClouds > 0.0)
		{	
			float3 cirrusUV = wpos;
			cirrusUV.y *= 1 - dot(cloudsDir.y + 10, float3(0,-0.15,0));

			float4 cirrus = CirrusClouds(cirrusUV);
			skyColor.rgb = skyColor.rgb * (1 - cirrus.a) + cirrus.rgb * cirrus.a;
		}

		//2D Clouds
		if(_FlatClouds > 0.0)
		{
			float3 flatCloudsUV = wpos;
			flatCloudsUV.y *= 1 - dot(cloudsDir.y + 200, float3(0,-0.1,0));
			float4 clouds = Clouds2D(flatCloudsUV, wpos); 
			skyColor.rgb = skyColor.rgb * (1 - clouds.a) + clouds.rgb * clouds.a;
		}
	
		return float4(skyColor.rgb * _EnviroSkyIntensity * GetCurrentExposureMultiplier(), 1);
	}

	float4 fragBaking(v2f i) : SV_Target  
	{			
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

		float3 viewDirWS = GetSkyViewDirWS(i.position.xy);
		float3 dir = -viewDirWS;
		float3 wpos = normalize(mul((float4x4)UNITY_MATRIX_M, dir)).xyz;

		float4 skyColor = float4(0, 0, 0, 1);


		float3 viewDir = normalize(dir);
		//float3 viewDir = normalize(i.texcoord);
		skyColor = GetSkyColor(viewDir, 0.005f) * _AmbientColorTintHDRP;  

		//Stars
		float3 starsUV = mul((float3x3)_StarsMatrix, dir);
		float4 starsTex = texCUBE(_StarsTex, starsUV) * saturate(viewDir.y);
		float4 stars = starsTex * _StarIntensity;
		//skyColor += stars;

		//Galaxy
		float4 galaxyTex = texCUBE(_GalaxyTex, starsUV) * saturate(viewDir.y);
		float4 galaxy = galaxyTex * _GalaxyIntensity;
		//skyColor += galaxy;

		//Sun and Moon UV
		float3 rSun = normalize(cross(_SunDir.xyz, float3(0, -1, 0)));
		float3 uSun = cross(_SunDir.xyz, rSun);
		float2 sunUV = float2(dot(rSun, dir), dot(uSun, dir)) * (21.0 - _SkySunParameters.y) + 0.5;
		float3 rMoon = normalize(cross(_MoonDir.xyz, float3(0, -1, 0)));
		float3 uMoon = cross(_MoonDir.xyz, rMoon);
		float2 moonUV  = float2(dot(rMoon, dir), dot(uMoon, dir)) * (20.7 - _SkyMoonParameters.z) + 0.5;
		
		//Sun
		float4 sun = float4(0,0,0,1);
		float hideBackSun = saturate(dot(_SunDir.xyz, viewDir));
		float4 sunDisk = tex2D(_SunTex, sunUV) * hideBackSun;
		sun = sunDisk * _SunColor * 10;
		skyColor += sun;

		//Moon
		if(_SkyMoonParameters.w > 0.0) 
		{
			float hideBackMoon = saturate(dot(-_MoonDir.xyz, viewDir));
			float4 moon = tex2D(_MoonTex, moonUV) * hideBackMoon;
			float alpha = MoonPhaseFactor(moonUV, _SkyMoonParameters.x);
			float3 moonArea = clamp(moon * 10, 0, 1);
			float starsBehindMoon = 1 - clamp((moonArea * 5), 0, 1);
			moon = lerp(float4(0, 0, 0, 0), moon, alpha);
			moon = moon * _MoonColor;
			//float4 moonGlow = tex2D(_MoonGlowTex, i.moonGlowPos.xy) * hideBackMoon;
			//moonGlow = moonGlow * _MoonColor * _MoonGlowIntensity;
			skyColor += stars * starsBehindMoon;
			skyColor += galaxy * starsBehindMoon;
			skyColor += moon;
		}
		else
		{
			skyColor += stars;
			skyColor += galaxy;
		}
		
		//Aurora
		if(_Aurora > 0.0)
		{
			float4 aurora = Aurora(wpos);
			skyColor.rgb += aurora.rgb;
		}

		//Dithering
		//skyColor.rgb += ScreenSpaceDither(i.position.xy,skyColor.rgb);

		float3 cloudsDir = normalize(wpos + float3(0,1,0));

		//Cirrus
		if(_CirrusClouds > 0.0)
		{	
			
			float3 cirrusUV = wpos;
			cirrusUV.y *= 1 - dot(cloudsDir.y + 10, float3(0,-0.15,0));

			float4 cirrus = CirrusClouds(cirrusUV);
			skyColor.rgb = skyColor.rgb * (1 - cirrus.a) + cirrus.rgb * cirrus.a;
		}

		//2D Clouds
		if(_FlatClouds > 0.0)
		{
			float3 flatCloudsUV = wpos;
			flatCloudsUV.y *= 1 - dot(cloudsDir.y + 200, float3(0,-0.1,0));
			float4 clouds = Clouds2D(flatCloudsUV, wpos); 
			skyColor.rgb = skyColor.rgb * (1 - clouds.a) + clouds.rgb * clouds.a;
		} 
		return float4(skyColor.rgb * _EnviroSkyIntensity, 1);
	}

	ENDHLSL

	SubShader
	{
		Tags{ "RenderPipeline" = "HDRenderPipeline" }
		Pass
		{
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment fragBaking
			ENDHLSL
		}

		// For fullscreen Sky
		Pass
		{
			ZWrite Off
			ZTest LEqual
			Blend Off
			Cull Off

			HLSLPROGRAM	
			#pragma vertex vert
			#pragma fragment frag
			ENDHLSL
		}
	}
}
