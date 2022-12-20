// Made with Amplify Shader Editor v1.9.0.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Distant Lands/Cozy/Stylized Clouds Painted"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[HideInInspector][HDR][Header(General Cloud Settings)]_CloudColor("Cloud Color", Color) = (0.7264151,0.7264151,0.7264151,0)
		[HideInInspector][HDR]_MoonColor("Moon Color", Color) = (1,1,1,0)
		[HideInInspector][HDR]_CloudHighlightColor("Cloud Highlight Color", Color) = (1,1,1,0)
		[HideInInspector][HDR]_AltoCloudColor("Alto Cloud Color", Color) = (1,1,1,0)
		[HideInInspector]_MainCloudScale("Main Cloud Scale", Float) = 10
		[HideInInspector]_ClippingThreshold("Clipping Threshold", Range( 0 , 1)) = 0.5
		[HideInInspector]_MoonFlareFalloff("Moon Flare Falloff", Float) = 1
		[HideInInspector]_SunFlareFalloff("Sun Flare Falloff", Float) = 1
		[HideInInspector]_CloudFlareFalloff("Cloud Flare Falloff", Float) = 1
		[HideInInspector]_MaxCloudCover("Max Cloud Cover", Float) = 1
		[HideInInspector]_MinCloudCover("Min Cloud Cover", Float) = 0
		[HideInInspector]_WindSpeed("Wind Speed", Float) = 0
		[HideInInspector][Header(Cumulus Clouds)]_CumulusCoverageMultiplier("Cumulus Coverage Multiplier", Range( 0 , 2)) = 1
		[HideInInspector]_DetailScale("Detail Scale", Float) = 0.5
		[HideInInspector]_DetailAmount("Detail Amount", Float) = 1
		[HideInInspector][Header(Border Clouds)]_BorderHeight("Border Height", Range( 0 , 1)) = 1
		[HideInInspector]_BorderVariation("Border Variation", Range( 0 , 1)) = 1
		[HideInInspector]_BorderEffect("Border Effect", Range( -1 , 1)) = 0
		[HideInInspector][Header(Nimbus Clouds)]_NimbusMultiplier("Nimbus Multiplier", Range( 0 , 2)) = 1
		[HideInInspector]_NimbusVariation("Nimbus Variation", Range( 0 , 1)) = 1
		[HideInInspector]_NimbusHeight("Nimbus Height", Range( 0 , 1)) = 1
		[HideInInspector]_StormDirection("Storm Direction", Vector) = (0,0,0,0)
		[HideInInspector][Header(Altocumulus Clouds)]_AltocumulusMultiplier("Altocumulus Multiplier", Range( 0 , 2)) = 2
		[HideInInspector]_AltocumulusScale("Altocumulus Scale", Float) = 3
		[HideInInspector]_AltocumulusWindSpeed("Altocumulus Wind Speed", Vector) = (1,-2,0,0)
		[HideInInspector][Header(Cirrostratus Clouds)]_CirrostratusMultiplier("Cirrostratus Multiplier", Range( 0 , 2)) = 1
		[HideInInspector]_CirrostratusMoveSpeed("Cirrostratus Move Speed", Float) = 0
		_CirrostratusTexture("Cirrostratus Texture", 2D) = "white" {}
		_CloudTexture("Cloud Texture", 2D) = "white" {}
		[HideInInspector][Header(Cirrus Clouds)]_CirrusMultiplier("Cirrus Multiplier", Range( 0 , 2)) = 1
		[HideInInspector]_CirrusMoveSpeed("Cirrus Move Speed", Float) = 0
		_CirrusTexture("Cirrus Texture", 2D) = "white" {}
		[HideInInspector]_ChemtrailsMultiplier("Chemtrails Multiplier", Range( 0 , 2)) = 1
		[HideInInspector]_CloudThickness("CloudThickness", Range( 0 , 4)) = 1
		[HideInInspector]_ChemtrailsMoveSpeed("Chemtrails Move Speed", Float) = 0
		[ASEEnd]_ChemtrailsTexture("Chemtrails Texture", 2D) = "white" {}
		[HideInInspector]_TextureAmount("Texture Amount", Range( 0 , 3)) = 1
		[HideInInspector][HDR]_CloudTextureColor("Cloud Texture Color", Color) = (1,1,1,0)


		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25
	}

	SubShader
	{
		LOD 0

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+1" }

		Cull Front
		AlphaToMask Off

		Stencil
		{
			Ref 221
			Pass Zero
		}

		HLSLINCLUDE

		#pragma target 3.0

		#pragma prefer_hlslcc gles
		#pragma exclude_renderers d3d11_9x 

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}
		
		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
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

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		#endif //ASE_TESS_FUNCS

		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForward" }
			
			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA

			

			HLSLPROGRAM

			#pragma multi_compile_instancing
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_SRP_VERSION 110000


			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			
			#define ASE_NEEDS_FRAG_WORLD_POSITION


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord : TEXCOORD1;
				#endif
				#ifdef ASE_FOG
					float fogFactor : TEXCOORD2;
				#endif
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _CloudColor;
			float4 _CloudHighlightColor;
			float4 _CloudTextureColor;
			float4 _MoonColor;
			float4 _AltoCloudColor;
			float3 _StormDirection;
			float2 _AltocumulusWindSpeed;
			float _ChemtrailsMultiplier;
			float _CirrusMoveSpeed;
			float _CirrusMultiplier;
			half _CloudFlareFalloff;
			float _AltocumulusScale;
			float _AltocumulusMultiplier;
			float _CirrostratusMoveSpeed;
			float _CirrostratusMultiplier;
			float _ClippingThreshold;
			float _ChemtrailsMoveSpeed;
			float _NimbusVariation;
			float _NimbusHeight;
			float _TextureAmount;
			float _BorderEffect;
			float _BorderVariation;
			float _BorderHeight;
			float _DetailAmount;
			float _DetailScale;
			half _MoonFlareFalloff;
			half _SunFlareFalloff;
			float _CumulusCoverageMultiplier;
			float _MaxCloudCover;
			float _MinCloudCover;
			float _MainCloudScale;
			float _WindSpeed;
			float _NimbusMultiplier;
			float _CloudThickness;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			float3 CZY_SunDirection;
			float3 CZY_MoonDirection;
			sampler2D _ChemtrailsTexture;
			sampler2D _CirrusTexture;
			sampler2D _CloudTexture;
			sampler2D _CirrostratusTexture;
			float4 CZY_LightColor;


			float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
			float snoise( float2 v )
			{
				const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
				float2 i = floor( v + dot( v, C.yy ) );
				float2 x0 = v - i + dot( i, C.xx );
				float2 i1;
				i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
				float4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;
				i = mod2D289( i );
				float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
				float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
				m = m * m;
				m = m * m;
				float3 x = 2.0 * frac( p * C.www ) - 1.0;
				float3 h = abs( x ) - 0.5;
				float3 ox = floor( x + 0.5 );
				float3 a0 = x - ox;
				m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
				float3 g;
				g.x = a0.x * x0.x + h.x * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;
				return 130.0 * dot( m, g );
			}
			
					float2 voronoihash148( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi148( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash148( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return (F2 + F1) * 0.5;
					}
			
					float2 voronoihash234( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi234( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash234( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return (F2 + F1) * 0.5;
					}
			
					float2 voronoihash77( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi77( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash77( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash601( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi601( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash601( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return (F2 + F1) * 0.5;
					}
			
					float2 voronoihash492( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi492( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash492( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash463( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi463( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash463( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			

			VertexOutput VertexFunction ( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.ase_texcoord3.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord3.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float4 positionCS = TransformWorldToHClip( positionWS );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					o.worldPos = positionWS;
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = positionCS;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				#ifdef ASE_FOG
					o.fogFactor = ComputeFogFactor( positionCS.z );
				#endif

				o.clipPos = positionCS;

				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord = v.ase_texcoord;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag ( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 WorldPosition = IN.worldPos;
				#endif

				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float4 CloudColor332 = _CloudColor;
				float4 CloudHighlightColor334 = _CloudHighlightColor;
				float2 texCoord94 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 Pos159 = texCoord94;
				float mulTime61 = _TimeParameters.x * ( 0.001 * _WindSpeed );
				float TIme152 = mulTime61;
				float simplePerlin2D37 = snoise( ( Pos159 + ( TIme152 * float2( 0.2,-0.4 ) ) )*( 100.0 / _MainCloudScale ) );
				simplePerlin2D37 = simplePerlin2D37*0.5 + 0.5;
				float SimpleCloudDensity314 = simplePerlin2D37;
				float time148 = 0.0;
				float2 voronoiSmoothId148 = 0;
				float2 temp_output_66_0 = ( Pos159 + ( TIme152 * float2( 0.3,0.2 ) ) );
				float2 coords148 = temp_output_66_0 * ( 140.0 / _MainCloudScale );
				float2 id148 = 0;
				float2 uv148 = 0;
				float voroi148 = voronoi148( coords148, time148, id148, uv148, 0, voronoiSmoothId148 );
				float time234 = 0.0;
				float2 voronoiSmoothId234 = 0;
				float2 coords234 = temp_output_66_0 * ( 500.0 / _MainCloudScale );
				float2 id234 = 0;
				float2 uv234 = 0;
				float voroi234 = voronoi234( coords234, time234, id234, uv234, 0, voronoiSmoothId234 );
				float2 appendResult312 = (float2(voroi148 , voroi234));
				float2 VoroDetails313 = appendResult312;
				float simplePerlin2D80 = snoise( ( ( float2( 5,20 ) * TIme152 ) + Pos159 )*( 100.0 / 400.0 ) );
				simplePerlin2D80 = simplePerlin2D80*0.5 + 0.5;
				float CurrentCloudCover240 = (_MinCloudCover + (simplePerlin2D80 - 0.0) * (_MaxCloudCover - _MinCloudCover) / (1.0 - 0.0));
				float CumulusCoverage376 = _CumulusCoverageMultiplier;
				float ComplexCloudDensity344 = (0.0 + (min( SimpleCloudDensity314 , ( 1.0 - VoroDetails313.x ) ) - ( 1.0 - ( CurrentCloudCover240 * CumulusCoverage376 ) )) * (1.0 - 0.0) / (1.0 - ( 1.0 - ( CurrentCloudCover240 * CumulusCoverage376 ) )));
				float4 lerpResult53 = lerp( CloudHighlightColor334 , CloudColor332 , saturate( (2.0 + (ComplexCloudDensity344 - 0.0) * (0.7 - 2.0) / (1.0 - 0.0)) ));
				float3 normalizeResult259 = normalize( ( WorldPosition - _WorldSpaceCameraPos ) );
				float dotResult261 = dot( normalizeResult259 , CZY_SunDirection );
				float temp_output_264_0 = abs( (dotResult261*0.5 + 0.5) );
				half LightMask267 = saturate( pow( temp_output_264_0 , _SunFlareFalloff ) );
				float2 appendResult318 = (float2(_MinCloudCover , _MaxCloudCover));
				float2 RequestedCloudCover317 = appendResult318;
				float CloudThicknessDetails329 = ( VoroDetails313.x * saturate( ( ( RequestedCloudCover317 * CumulusCoverage376 ).y - 0.8 ) ) );
				float3 normalizeResult779 = normalize( ( WorldPosition - _WorldSpaceCameraPos ) );
				float dotResult780 = dot( normalizeResult779 , CZY_MoonDirection );
				half MoonlightMask790 = saturate( pow( abs( (dotResult780*0.5 + 0.5) ) , _MoonFlareFalloff ) );
				float4 MoonlightColor797 = _MoonColor;
				float4 lerpResult227 = lerp( ( lerpResult53 + ( LightMask267 * CloudHighlightColor334 * ( 1.0 - CloudThicknessDetails329 ) ) + ( MoonlightMask790 * MoonlightColor797 * ( 1.0 - CloudThicknessDetails329 ) ) ) , ( CloudColor332 * float4( 0.5660378,0.5660378,0.5660378,0 ) ) , CloudThicknessDetails329);
				float time77 = 0.0;
				float2 voronoiSmoothId77 = 0;
				float2 coords77 = ( Pos159 + ( TIme152 * float2( 0.3,0.2 ) ) ) * ( 100.0 / _DetailScale );
				float2 id77 = 0;
				float2 uv77 = 0;
				float fade77 = 0.5;
				float voroi77 = 0;
				float rest77 = 0;
				for( int it77 = 0; it77 <3; it77++ ){
				voroi77 += fade77 * voronoi77( coords77, time77, id77, uv77, 0,voronoiSmoothId77 );
				rest77 += fade77;
				coords77 *= 2;
				fade77 *= 0.5;
				}//Voronoi77
				voroi77 /= rest77;
				float temp_output_47_0 = ( (0.0 + (( 1.0 - voroi77 ) - 0.3) * (0.5 - 0.0) / (1.0 - 0.3)) * 0.1 * _DetailAmount );
				float DetailedClouds347 = saturate( ( ComplexCloudDensity344 + temp_output_47_0 ) );
				float CloudDetail294 = temp_output_47_0;
				float2 texCoord113 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_114_0 = ( texCoord113 - float2( 0.5,0.5 ) );
				float dotResult140 = dot( temp_output_114_0 , temp_output_114_0 );
				float BorderHeight386 = ( 1.0 - _BorderHeight );
				float temp_output_392_0 = ( -2.0 * ( 1.0 - _BorderVariation ) );
				float clampResult420 = clamp( ( ( ( CloudDetail294 + SimpleCloudDensity314 ) * saturate( (( BorderHeight386 * temp_output_392_0 ) + (dotResult140 - 0.0) * (( temp_output_392_0 * -4.0 ) - ( BorderHeight386 * temp_output_392_0 )) / (0.5 - 0.0)) ) ) * 10.0 * _BorderEffect ) , -1.0 , 1.0 );
				float BorderLightTransport418 = clampResult420;
				float3 normalizeResult745 = normalize( ( WorldPosition - _WorldSpaceCameraPos ) );
				float3 normalizeResult773 = normalize( _StormDirection );
				float dotResult743 = dot( normalizeResult745 , normalizeResult773 );
				float2 texCoord721 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_726_0 = ( texCoord721 - float2( 0.5,0.5 ) );
				float dotResult728 = dot( temp_output_726_0 , temp_output_726_0 );
				float temp_output_763_0 = ( -2.0 * ( 1.0 - ( _NimbusVariation * 0.9 ) ) );
				float NimbusLightTransport739 = saturate( ( ( ( CloudDetail294 + SimpleCloudDensity314 ) * saturate( (( ( 1.0 - _NimbusMultiplier ) * temp_output_763_0 ) + (( dotResult743 + ( _NimbusHeight * 4.0 * dotResult728 ) ) - 0.5) * (( temp_output_763_0 * -4.0 ) - ( ( 1.0 - _NimbusMultiplier ) * temp_output_763_0 )) / (7.0 - 0.5)) ) ) * 10.0 ) );
				float mulTime566 = _TimeParameters.x * 0.01;
				float simplePerlin2D563 = snoise( (Pos159*1.0 + mulTime566)*2.0 );
				float mulTime560 = _TimeParameters.x * _ChemtrailsMoveSpeed;
				float cos553 = cos( ( mulTime560 * 0.01 ) );
				float sin553 = sin( ( mulTime560 * 0.01 ) );
				float2 rotator553 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos553 , -sin553 , sin553 , cos553 )) + float2( 0.5,0.5 );
				float cos561 = cos( ( mulTime560 * -0.02 ) );
				float sin561 = sin( ( mulTime560 * -0.02 ) );
				float2 rotator561 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos561 , -sin561 , sin561 , cos561 )) + float2( 0.5,0.5 );
				float mulTime568 = _TimeParameters.x * 0.01;
				float simplePerlin2D570 = snoise( (Pos159*1.0 + mulTime568)*4.0 );
				float4 ChemtrailsPattern576 = ( ( saturate( simplePerlin2D563 ) * tex2D( _ChemtrailsTexture, (rotator553*0.5 + 0.0) ) ) + ( tex2D( _ChemtrailsTexture, rotator561 ) * saturate( simplePerlin2D570 ) ) );
				float2 texCoord583 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_581_0 = ( texCoord583 - float2( 0.5,0.5 ) );
				float dotResult584 = dot( temp_output_581_0 , temp_output_581_0 );
				float4 ChemtrailsFinal590 = ( ChemtrailsPattern576 * saturate( (0.4 + (dotResult584 - 0.0) * (2.0 - 0.4) / (0.1 - 0.0)) ) * ( _ChemtrailsMultiplier * 0.5 ) );
				float mulTime673 = _TimeParameters.x * 0.01;
				float simplePerlin2D681 = snoise( (Pos159*1.0 + mulTime673)*2.0 );
				float mulTime666 = _TimeParameters.x * _CirrusMoveSpeed;
				float cos677 = cos( ( mulTime666 * 0.01 ) );
				float sin677 = sin( ( mulTime666 * 0.01 ) );
				float2 rotator677 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos677 , -sin677 , sin677 , cos677 )) + float2( 0.5,0.5 );
				float cos676 = cos( ( mulTime666 * -0.02 ) );
				float sin676 = sin( ( mulTime666 * -0.02 ) );
				float2 rotator676 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos676 , -sin676 , sin676 , cos676 )) + float2( 0.5,0.5 );
				float mulTime670 = _TimeParameters.x * 0.01;
				float simplePerlin2D682 = snoise( (Pos159*1.0 + mulTime670) );
				simplePerlin2D682 = simplePerlin2D682*0.5 + 0.5;
				float4 CirrusPattern696 = ( ( saturate( simplePerlin2D681 ) * tex2D( _CirrusTexture, (rotator677*1.5 + 0.75) ) ) + ( tex2D( _CirrusTexture, (rotator676*1.0 + 0.0) ) * saturate( simplePerlin2D682 ) ) );
				float2 texCoord685 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_690_0 = ( texCoord685 - float2( 0.5,0.5 ) );
				float dotResult694 = dot( temp_output_690_0 , temp_output_690_0 );
				float4 temp_output_701_0 = ( CirrusPattern696 * saturate( (0.0 + (dotResult694 - 0.0) * (2.0 - 0.0) / (0.2 - 0.0)) ) );
				float CirrusAlpha715 = ( ( temp_output_701_0 * ( _CirrusMultiplier * 10.0 ) ).r * 0.6 );
				float4 SimpleRadiance506 = saturate( ( DetailedClouds347 + BorderLightTransport418 + NimbusLightTransport739 + ChemtrailsFinal590 + CirrusAlpha715 ) );
				float4 lerpResult171 = lerp( CloudColor332 , lerpResult227 , ( 1.0 - SimpleRadiance506 ));
				float temp_output_382_0 = ( CurrentCloudCover240 * CumulusCoverage376 );
				float mulTime874 = _TimeParameters.x * 0.5;
				float2 panner855 = ( ( mulTime874 * 0.004 ) * float2( 0.2,-0.4 ) + Pos159);
				float4 tex2DNode827 = tex2D( _CloudTexture, (panner855*1.0 + 0.75) );
				float cos818 = cos( ( mulTime874 * -0.01 ) );
				float sin818 = sin( ( mulTime874 * -0.01 ) );
				float2 rotator818 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos818 , -sin818 , sin818 , cos818 )) + float2( 0.5,0.5 );
				float4 tex2DNode826 = tex2D( _CloudTexture, (rotator818*3.0 + 0.75) );
				float4 CloudTexture839 = min( tex2DNode827 , tex2DNode826 );
				float clampResult841 = clamp( ( 2.0 * 0.5 ) , 0.0 , 0.98 );
				float CloudTextureFinal847 = ( CloudTexture839 * clampResult841 ).r;
				float CloudLight271 = saturate( pow( temp_output_264_0 , _CloudFlareFalloff ) );
				float4 lerpResult272 = lerp( float4( 0,0,0,0 ) , CloudHighlightColor334 , ( saturate( temp_output_382_0 ) * CloudTextureFinal847 * CloudLight271 ));
				float4 SunThroughClouds273 = ( lerpResult272 * 2.0 );
				float4 CirrusCustomLightColor512 = ( CloudColor332 * _AltoCloudColor );
				float time601 = 0.0;
				float2 voronoiSmoothId601 = 0;
				float mulTime602 = _TimeParameters.x * 0.003;
				float2 coords601 = (Pos159*1.0 + ( float2( 1,-2 ) * mulTime602 )) * 10.0;
				float2 id601 = 0;
				float2 uv601 = 0;
				float voroi601 = voronoi601( coords601, time601, id601, uv601, 0, voronoiSmoothId601 );
				float time492 = ( 10.0 * mulTime602 );
				float2 voronoiSmoothId492 = 0;
				float2 coords492 = IN.ase_texcoord3.xy * 10.0;
				float2 id492 = 0;
				float2 uv492 = 0;
				float voroi492 = voronoi492( coords492, time492, id492, uv492, 0, voronoiSmoothId492 );
				float AltoCumulusPlacement461 = saturate( ( ( ( 1.0 - 0.0 ) - (1.0 + (voroi601 - 0.0) * (-0.5 - 1.0) / (1.0 - 0.0)) ) - voroi492 ) );
				float time463 = 51.2;
				float2 voronoiSmoothId463 = 0;
				float2 coords463 = (Pos159*1.0 + ( _AltocumulusWindSpeed * TIme152 )) * ( 100.0 / _AltocumulusScale );
				float2 id463 = 0;
				float2 uv463 = 0;
				float fade463 = 0.5;
				float voroi463 = 0;
				float rest463 = 0;
				for( int it463 = 0; it463 <2; it463++ ){
				voroi463 += fade463 * voronoi463( coords463, time463, id463, uv463, 0,voronoiSmoothId463 );
				rest463 += fade463;
				coords463 *= 2;
				fade463 *= 0.5;
				}//Voronoi463
				voroi463 /= rest463;
				float AltoCumulusLightTransport447 = saturate( (-1.0 + (( AltoCumulusPlacement461 * ( 0.1 > voroi463 ? (0.5 + (voroi463 - 0.0) * (0.0 - 0.5) / (0.15 - 0.0)) : 0.0 ) * _AltocumulusMultiplier ) - 0.0) * (3.0 - -1.0) / (1.0 - 0.0)) );
				float ACCustomLightsClipping521 = AltoCumulusLightTransport447;
				float mulTime611 = _TimeParameters.x * 0.01;
				float simplePerlin2D620 = snoise( (Pos159*1.0 + mulTime611)*2.0 );
				float mulTime607 = _TimeParameters.x * _CirrostratusMoveSpeed;
				float cos615 = cos( ( mulTime607 * 0.01 ) );
				float sin615 = sin( ( mulTime607 * 0.01 ) );
				float2 rotator615 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos615 , -sin615 , sin615 , cos615 )) + float2( 0.5,0.5 );
				float cos621 = cos( ( mulTime607 * -0.02 ) );
				float sin621 = sin( ( mulTime607 * -0.02 ) );
				float2 rotator621 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos621 , -sin621 , sin621 , cos621 )) + float2( 0.5,0.5 );
				float mulTime612 = _TimeParameters.x * 0.01;
				float simplePerlin2D618 = snoise( (Pos159*10.0 + mulTime612)*4.0 );
				float4 CirrostratPattern634 = ( ( saturate( simplePerlin2D620 ) * tex2D( _CirrostratusTexture, (rotator615*1.5 + 0.75) ) ) + ( tex2D( _CirrostratusTexture, (rotator621*1.5 + 0.75) ) * saturate( simplePerlin2D618 ) ) );
				float2 texCoord625 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_629_0 = ( texCoord625 - float2( 0.5,0.5 ) );
				float dotResult630 = dot( temp_output_629_0 , temp_output_629_0 );
				float4 CirrostratLightTransport641 = ( CirrostratPattern634 * saturate( (0.4 + (dotResult630 - 0.0) * (2.0 - 0.4) / (0.1 - 0.0)) ) * ( _CirrostratusMultiplier * 1.0 ) );
				float Clipping515 = _ClippingThreshold;
				float4 CSCustomLightsClipping648 = ( CirrostratLightTransport641 * ( SimpleRadiance506.r > Clipping515 ? 0.0 : 1.0 ) );
				float4 CustomRadiance657 = saturate( ( ACCustomLightsClipping521 + CSCustomLightsClipping648 ) );
				float4 lerpResult522 = lerp( ( lerpResult171 + SunThroughClouds273 ) , CirrusCustomLightColor512 , CustomRadiance657);
				float4 lerpResult898 = lerp( _CloudTextureColor , CZY_LightColor , float4( 0.5,0.5,0.5,0 ));
				float4 lerpResult891 = lerp( lerpResult522 , ( lerpResult898 * lerpResult522 ) , CloudTextureFinal847);
				float4 FinalCloudColor351 = lerpResult891;
				
				float temp_output_863_0 = saturate( ( DetailedClouds347 + BorderLightTransport418 + NimbusLightTransport739 ) );
				float4 FinalAlpha408 = saturate( ( saturate( ( temp_output_863_0 + ( (-1.0 + (CloudTextureFinal847 - 0.0) * (1.0 - -1.0) / (1.0 - 0.0)) * _TextureAmount * sin( ( temp_output_863_0 * PI ) ) ) ) ) + AltoCumulusLightTransport447 + ChemtrailsFinal590 + CirrostratLightTransport641 + CirrusAlpha715 ) );
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = FinalCloudColor351.rgb;
				float Alpha = saturate( ( FinalAlpha408.r + ( FinalAlpha408.r * 2.0 * _CloudThickness ) ) );
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;

				#ifdef _ALPHATEST_ON
					clip( Alpha - AlphaClipThreshold );
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				#ifdef ASE_FOG
					Color = MixFog( Color, IN.fogFactor );
				#endif

				return half4( Color, Alpha );
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual
			AlphaToMask Off
			ColorMask 0

			HLSLPROGRAM
			
			#pragma multi_compile_instancing
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_SRP_VERSION 110000

			
			#pragma vertex vert
			#pragma fragment frag

			#if ASE_SRP_VERSION >= 110000
				#pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW
			#endif

			#define SHADERPASS SHADERPASS_SHADOWCASTER

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#define ASE_NEEDS_FRAG_WORLD_POSITION


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord : TEXCOORD1;
				#endif
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _CloudColor;
			float4 _CloudHighlightColor;
			float4 _CloudTextureColor;
			float4 _MoonColor;
			float4 _AltoCloudColor;
			float3 _StormDirection;
			float2 _AltocumulusWindSpeed;
			float _ChemtrailsMultiplier;
			float _CirrusMoveSpeed;
			float _CirrusMultiplier;
			half _CloudFlareFalloff;
			float _AltocumulusScale;
			float _AltocumulusMultiplier;
			float _CirrostratusMoveSpeed;
			float _CirrostratusMultiplier;
			float _ClippingThreshold;
			float _ChemtrailsMoveSpeed;
			float _NimbusVariation;
			float _NimbusHeight;
			float _TextureAmount;
			float _BorderEffect;
			float _BorderVariation;
			float _BorderHeight;
			float _DetailAmount;
			float _DetailScale;
			half _MoonFlareFalloff;
			half _SunFlareFalloff;
			float _CumulusCoverageMultiplier;
			float _MaxCloudCover;
			float _MinCloudCover;
			float _MainCloudScale;
			float _WindSpeed;
			float _NimbusMultiplier;
			float _CloudThickness;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _CloudTexture;
			sampler2D _ChemtrailsTexture;
			sampler2D _CirrostratusTexture;
			sampler2D _CirrusTexture;


			float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
			float snoise( float2 v )
			{
				const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
				float2 i = floor( v + dot( v, C.yy ) );
				float2 x0 = v - i + dot( i, C.xx );
				float2 i1;
				i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
				float4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;
				i = mod2D289( i );
				float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
				float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
				m = m * m;
				m = m * m;
				float3 x = 2.0 * frac( p * C.www ) - 1.0;
				float3 h = abs( x ) - 0.5;
				float3 ox = floor( x + 0.5 );
				float3 a0 = x - ox;
				m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
				float3 g;
				g.x = a0.x * x0.x + h.x * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;
				return 130.0 * dot( m, g );
			}
			
					float2 voronoihash148( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi148( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash148( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return (F2 + F1) * 0.5;
					}
			
					float2 voronoihash234( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi234( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash234( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return (F2 + F1) * 0.5;
					}
			
					float2 voronoihash77( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi77( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash77( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash601( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi601( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash601( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return (F2 + F1) * 0.5;
					}
			
					float2 voronoihash492( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi492( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash492( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash463( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi463( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash463( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			

			float3 _LightDirection;
			#if ASE_SRP_VERSION >= 110000 
				float3 _LightPosition;
			#endif

			VertexOutput VertexFunction( VertexInput v )
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					o.worldPos = positionWS;
				#endif

				float3 normalWS = TransformObjectToWorldDir( v.ase_normal );

				#if ASE_SRP_VERSION >= 110000 
				#if _CASTING_PUNCTUAL_LIGHT_SHADOW
					float3 lightDirectionWS = normalize(_LightPosition - positionWS);
				#else
					float3 lightDirectionWS = _LightDirection;
				#endif

				float4 clipPos = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

					#if UNITY_REVERSED_Z
						clipPos.z = min(clipPos.z, UNITY_NEAR_CLIP_VALUE);
					#else
						clipPos.z = max(clipPos.z, UNITY_NEAR_CLIP_VALUE);
					#endif
				#else
						float4 clipPos = TransformWorldToHClip( ApplyShadowBias( positionWS, normalWS, _LightDirection ) );

						#if UNITY_REVERSED_Z
							clipPos.z = min(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
						#else
							clipPos.z = max(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
						#endif
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = clipPos;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				o.clipPos = clipPos;

				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord = v.ase_texcoord;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 WorldPosition = IN.worldPos;
				#endif

				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float2 texCoord94 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 Pos159 = texCoord94;
				float mulTime61 = _TimeParameters.x * ( 0.001 * _WindSpeed );
				float TIme152 = mulTime61;
				float simplePerlin2D37 = snoise( ( Pos159 + ( TIme152 * float2( 0.2,-0.4 ) ) )*( 100.0 / _MainCloudScale ) );
				simplePerlin2D37 = simplePerlin2D37*0.5 + 0.5;
				float SimpleCloudDensity314 = simplePerlin2D37;
				float time148 = 0.0;
				float2 voronoiSmoothId148 = 0;
				float2 temp_output_66_0 = ( Pos159 + ( TIme152 * float2( 0.3,0.2 ) ) );
				float2 coords148 = temp_output_66_0 * ( 140.0 / _MainCloudScale );
				float2 id148 = 0;
				float2 uv148 = 0;
				float voroi148 = voronoi148( coords148, time148, id148, uv148, 0, voronoiSmoothId148 );
				float time234 = 0.0;
				float2 voronoiSmoothId234 = 0;
				float2 coords234 = temp_output_66_0 * ( 500.0 / _MainCloudScale );
				float2 id234 = 0;
				float2 uv234 = 0;
				float voroi234 = voronoi234( coords234, time234, id234, uv234, 0, voronoiSmoothId234 );
				float2 appendResult312 = (float2(voroi148 , voroi234));
				float2 VoroDetails313 = appendResult312;
				float simplePerlin2D80 = snoise( ( ( float2( 5,20 ) * TIme152 ) + Pos159 )*( 100.0 / 400.0 ) );
				simplePerlin2D80 = simplePerlin2D80*0.5 + 0.5;
				float CurrentCloudCover240 = (_MinCloudCover + (simplePerlin2D80 - 0.0) * (_MaxCloudCover - _MinCloudCover) / (1.0 - 0.0));
				float CumulusCoverage376 = _CumulusCoverageMultiplier;
				float ComplexCloudDensity344 = (0.0 + (min( SimpleCloudDensity314 , ( 1.0 - VoroDetails313.x ) ) - ( 1.0 - ( CurrentCloudCover240 * CumulusCoverage376 ) )) * (1.0 - 0.0) / (1.0 - ( 1.0 - ( CurrentCloudCover240 * CumulusCoverage376 ) )));
				float time77 = 0.0;
				float2 voronoiSmoothId77 = 0;
				float2 coords77 = ( Pos159 + ( TIme152 * float2( 0.3,0.2 ) ) ) * ( 100.0 / _DetailScale );
				float2 id77 = 0;
				float2 uv77 = 0;
				float fade77 = 0.5;
				float voroi77 = 0;
				float rest77 = 0;
				for( int it77 = 0; it77 <3; it77++ ){
				voroi77 += fade77 * voronoi77( coords77, time77, id77, uv77, 0,voronoiSmoothId77 );
				rest77 += fade77;
				coords77 *= 2;
				fade77 *= 0.5;
				}//Voronoi77
				voroi77 /= rest77;
				float temp_output_47_0 = ( (0.0 + (( 1.0 - voroi77 ) - 0.3) * (0.5 - 0.0) / (1.0 - 0.3)) * 0.1 * _DetailAmount );
				float DetailedClouds347 = saturate( ( ComplexCloudDensity344 + temp_output_47_0 ) );
				float CloudDetail294 = temp_output_47_0;
				float2 texCoord113 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_114_0 = ( texCoord113 - float2( 0.5,0.5 ) );
				float dotResult140 = dot( temp_output_114_0 , temp_output_114_0 );
				float BorderHeight386 = ( 1.0 - _BorderHeight );
				float temp_output_392_0 = ( -2.0 * ( 1.0 - _BorderVariation ) );
				float clampResult420 = clamp( ( ( ( CloudDetail294 + SimpleCloudDensity314 ) * saturate( (( BorderHeight386 * temp_output_392_0 ) + (dotResult140 - 0.0) * (( temp_output_392_0 * -4.0 ) - ( BorderHeight386 * temp_output_392_0 )) / (0.5 - 0.0)) ) ) * 10.0 * _BorderEffect ) , -1.0 , 1.0 );
				float BorderLightTransport418 = clampResult420;
				float3 normalizeResult745 = normalize( ( WorldPosition - _WorldSpaceCameraPos ) );
				float3 normalizeResult773 = normalize( _StormDirection );
				float dotResult743 = dot( normalizeResult745 , normalizeResult773 );
				float2 texCoord721 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_726_0 = ( texCoord721 - float2( 0.5,0.5 ) );
				float dotResult728 = dot( temp_output_726_0 , temp_output_726_0 );
				float temp_output_763_0 = ( -2.0 * ( 1.0 - ( _NimbusVariation * 0.9 ) ) );
				float NimbusLightTransport739 = saturate( ( ( ( CloudDetail294 + SimpleCloudDensity314 ) * saturate( (( ( 1.0 - _NimbusMultiplier ) * temp_output_763_0 ) + (( dotResult743 + ( _NimbusHeight * 4.0 * dotResult728 ) ) - 0.5) * (( temp_output_763_0 * -4.0 ) - ( ( 1.0 - _NimbusMultiplier ) * temp_output_763_0 )) / (7.0 - 0.5)) ) ) * 10.0 ) );
				float temp_output_863_0 = saturate( ( DetailedClouds347 + BorderLightTransport418 + NimbusLightTransport739 ) );
				float mulTime874 = _TimeParameters.x * 0.5;
				float2 panner855 = ( ( mulTime874 * 0.004 ) * float2( 0.2,-0.4 ) + Pos159);
				float4 tex2DNode827 = tex2D( _CloudTexture, (panner855*1.0 + 0.75) );
				float cos818 = cos( ( mulTime874 * -0.01 ) );
				float sin818 = sin( ( mulTime874 * -0.01 ) );
				float2 rotator818 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos818 , -sin818 , sin818 , cos818 )) + float2( 0.5,0.5 );
				float4 tex2DNode826 = tex2D( _CloudTexture, (rotator818*3.0 + 0.75) );
				float4 CloudTexture839 = min( tex2DNode827 , tex2DNode826 );
				float clampResult841 = clamp( ( 2.0 * 0.5 ) , 0.0 , 0.98 );
				float CloudTextureFinal847 = ( CloudTexture839 * clampResult841 ).r;
				float time601 = 0.0;
				float2 voronoiSmoothId601 = 0;
				float mulTime602 = _TimeParameters.x * 0.003;
				float2 coords601 = (Pos159*1.0 + ( float2( 1,-2 ) * mulTime602 )) * 10.0;
				float2 id601 = 0;
				float2 uv601 = 0;
				float voroi601 = voronoi601( coords601, time601, id601, uv601, 0, voronoiSmoothId601 );
				float time492 = ( 10.0 * mulTime602 );
				float2 voronoiSmoothId492 = 0;
				float2 coords492 = IN.ase_texcoord2.xy * 10.0;
				float2 id492 = 0;
				float2 uv492 = 0;
				float voroi492 = voronoi492( coords492, time492, id492, uv492, 0, voronoiSmoothId492 );
				float AltoCumulusPlacement461 = saturate( ( ( ( 1.0 - 0.0 ) - (1.0 + (voroi601 - 0.0) * (-0.5 - 1.0) / (1.0 - 0.0)) ) - voroi492 ) );
				float time463 = 51.2;
				float2 voronoiSmoothId463 = 0;
				float2 coords463 = (Pos159*1.0 + ( _AltocumulusWindSpeed * TIme152 )) * ( 100.0 / _AltocumulusScale );
				float2 id463 = 0;
				float2 uv463 = 0;
				float fade463 = 0.5;
				float voroi463 = 0;
				float rest463 = 0;
				for( int it463 = 0; it463 <2; it463++ ){
				voroi463 += fade463 * voronoi463( coords463, time463, id463, uv463, 0,voronoiSmoothId463 );
				rest463 += fade463;
				coords463 *= 2;
				fade463 *= 0.5;
				}//Voronoi463
				voroi463 /= rest463;
				float AltoCumulusLightTransport447 = saturate( (-1.0 + (( AltoCumulusPlacement461 * ( 0.1 > voroi463 ? (0.5 + (voroi463 - 0.0) * (0.0 - 0.5) / (0.15 - 0.0)) : 0.0 ) * _AltocumulusMultiplier ) - 0.0) * (3.0 - -1.0) / (1.0 - 0.0)) );
				float mulTime566 = _TimeParameters.x * 0.01;
				float simplePerlin2D563 = snoise( (Pos159*1.0 + mulTime566)*2.0 );
				float mulTime560 = _TimeParameters.x * _ChemtrailsMoveSpeed;
				float cos553 = cos( ( mulTime560 * 0.01 ) );
				float sin553 = sin( ( mulTime560 * 0.01 ) );
				float2 rotator553 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos553 , -sin553 , sin553 , cos553 )) + float2( 0.5,0.5 );
				float cos561 = cos( ( mulTime560 * -0.02 ) );
				float sin561 = sin( ( mulTime560 * -0.02 ) );
				float2 rotator561 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos561 , -sin561 , sin561 , cos561 )) + float2( 0.5,0.5 );
				float mulTime568 = _TimeParameters.x * 0.01;
				float simplePerlin2D570 = snoise( (Pos159*1.0 + mulTime568)*4.0 );
				float4 ChemtrailsPattern576 = ( ( saturate( simplePerlin2D563 ) * tex2D( _ChemtrailsTexture, (rotator553*0.5 + 0.0) ) ) + ( tex2D( _ChemtrailsTexture, rotator561 ) * saturate( simplePerlin2D570 ) ) );
				float2 texCoord583 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_581_0 = ( texCoord583 - float2( 0.5,0.5 ) );
				float dotResult584 = dot( temp_output_581_0 , temp_output_581_0 );
				float4 ChemtrailsFinal590 = ( ChemtrailsPattern576 * saturate( (0.4 + (dotResult584 - 0.0) * (2.0 - 0.4) / (0.1 - 0.0)) ) * ( _ChemtrailsMultiplier * 0.5 ) );
				float mulTime611 = _TimeParameters.x * 0.01;
				float simplePerlin2D620 = snoise( (Pos159*1.0 + mulTime611)*2.0 );
				float mulTime607 = _TimeParameters.x * _CirrostratusMoveSpeed;
				float cos615 = cos( ( mulTime607 * 0.01 ) );
				float sin615 = sin( ( mulTime607 * 0.01 ) );
				float2 rotator615 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos615 , -sin615 , sin615 , cos615 )) + float2( 0.5,0.5 );
				float cos621 = cos( ( mulTime607 * -0.02 ) );
				float sin621 = sin( ( mulTime607 * -0.02 ) );
				float2 rotator621 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos621 , -sin621 , sin621 , cos621 )) + float2( 0.5,0.5 );
				float mulTime612 = _TimeParameters.x * 0.01;
				float simplePerlin2D618 = snoise( (Pos159*10.0 + mulTime612)*4.0 );
				float4 CirrostratPattern634 = ( ( saturate( simplePerlin2D620 ) * tex2D( _CirrostratusTexture, (rotator615*1.5 + 0.75) ) ) + ( tex2D( _CirrostratusTexture, (rotator621*1.5 + 0.75) ) * saturate( simplePerlin2D618 ) ) );
				float2 texCoord625 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_629_0 = ( texCoord625 - float2( 0.5,0.5 ) );
				float dotResult630 = dot( temp_output_629_0 , temp_output_629_0 );
				float4 CirrostratLightTransport641 = ( CirrostratPattern634 * saturate( (0.4 + (dotResult630 - 0.0) * (2.0 - 0.4) / (0.1 - 0.0)) ) * ( _CirrostratusMultiplier * 1.0 ) );
				float mulTime673 = _TimeParameters.x * 0.01;
				float simplePerlin2D681 = snoise( (Pos159*1.0 + mulTime673)*2.0 );
				float mulTime666 = _TimeParameters.x * _CirrusMoveSpeed;
				float cos677 = cos( ( mulTime666 * 0.01 ) );
				float sin677 = sin( ( mulTime666 * 0.01 ) );
				float2 rotator677 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos677 , -sin677 , sin677 , cos677 )) + float2( 0.5,0.5 );
				float cos676 = cos( ( mulTime666 * -0.02 ) );
				float sin676 = sin( ( mulTime666 * -0.02 ) );
				float2 rotator676 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos676 , -sin676 , sin676 , cos676 )) + float2( 0.5,0.5 );
				float mulTime670 = _TimeParameters.x * 0.01;
				float simplePerlin2D682 = snoise( (Pos159*1.0 + mulTime670) );
				simplePerlin2D682 = simplePerlin2D682*0.5 + 0.5;
				float4 CirrusPattern696 = ( ( saturate( simplePerlin2D681 ) * tex2D( _CirrusTexture, (rotator677*1.5 + 0.75) ) ) + ( tex2D( _CirrusTexture, (rotator676*1.0 + 0.0) ) * saturate( simplePerlin2D682 ) ) );
				float2 texCoord685 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_690_0 = ( texCoord685 - float2( 0.5,0.5 ) );
				float dotResult694 = dot( temp_output_690_0 , temp_output_690_0 );
				float4 temp_output_701_0 = ( CirrusPattern696 * saturate( (0.0 + (dotResult694 - 0.0) * (2.0 - 0.0) / (0.2 - 0.0)) ) );
				float CirrusAlpha715 = ( ( temp_output_701_0 * ( _CirrusMultiplier * 10.0 ) ).r * 0.6 );
				float4 FinalAlpha408 = saturate( ( saturate( ( temp_output_863_0 + ( (-1.0 + (CloudTextureFinal847 - 0.0) * (1.0 - -1.0) / (1.0 - 0.0)) * _TextureAmount * sin( ( temp_output_863_0 * PI ) ) ) ) ) + AltoCumulusLightTransport447 + ChemtrailsFinal590 + CirrostratLightTransport641 + CirrusAlpha715 ) );
				

				float Alpha = saturate( ( FinalAlpha408.r + ( FinalAlpha408.r * 2.0 * _CloudThickness ) ) );
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;

				#ifdef _ALPHATEST_ON
					#ifdef _ALPHATEST_SHADOW_ON
						clip(Alpha - AlphaClipThresholdShadow);
					#else
						clip(Alpha - AlphaClipThreshold);
					#endif
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif
				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask 0
			AlphaToMask Off

			HLSLPROGRAM
			
			#pragma multi_compile_instancing
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_SRP_VERSION 110000

			
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#define ASE_NEEDS_FRAG_WORLD_POSITION


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _CloudColor;
			float4 _CloudHighlightColor;
			float4 _CloudTextureColor;
			float4 _MoonColor;
			float4 _AltoCloudColor;
			float3 _StormDirection;
			float2 _AltocumulusWindSpeed;
			float _ChemtrailsMultiplier;
			float _CirrusMoveSpeed;
			float _CirrusMultiplier;
			half _CloudFlareFalloff;
			float _AltocumulusScale;
			float _AltocumulusMultiplier;
			float _CirrostratusMoveSpeed;
			float _CirrostratusMultiplier;
			float _ClippingThreshold;
			float _ChemtrailsMoveSpeed;
			float _NimbusVariation;
			float _NimbusHeight;
			float _TextureAmount;
			float _BorderEffect;
			float _BorderVariation;
			float _BorderHeight;
			float _DetailAmount;
			float _DetailScale;
			half _MoonFlareFalloff;
			half _SunFlareFalloff;
			float _CumulusCoverageMultiplier;
			float _MaxCloudCover;
			float _MinCloudCover;
			float _MainCloudScale;
			float _WindSpeed;
			float _NimbusMultiplier;
			float _CloudThickness;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _CloudTexture;
			sampler2D _ChemtrailsTexture;
			sampler2D _CirrostratusTexture;
			sampler2D _CirrusTexture;


			float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
			float snoise( float2 v )
			{
				const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
				float2 i = floor( v + dot( v, C.yy ) );
				float2 x0 = v - i + dot( i, C.xx );
				float2 i1;
				i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
				float4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;
				i = mod2D289( i );
				float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
				float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
				m = m * m;
				m = m * m;
				float3 x = 2.0 * frac( p * C.www ) - 1.0;
				float3 h = abs( x ) - 0.5;
				float3 ox = floor( x + 0.5 );
				float3 a0 = x - ox;
				m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
				float3 g;
				g.x = a0.x * x0.x + h.x * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;
				return 130.0 * dot( m, g );
			}
			
					float2 voronoihash148( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi148( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash148( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return (F2 + F1) * 0.5;
					}
			
					float2 voronoihash234( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi234( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash234( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return (F2 + F1) * 0.5;
					}
			
					float2 voronoihash77( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi77( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash77( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash601( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi601( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash601( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return (F2 + F1) * 0.5;
					}
			
					float2 voronoihash492( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi492( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash492( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash463( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi463( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash463( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			

			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					o.worldPos = positionWS;
				#endif

				o.clipPos = TransformWorldToHClip( positionWS );
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = o.clipPos;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord = v.ase_texcoord;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 WorldPosition = IN.worldPos;
				#endif

				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float2 texCoord94 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 Pos159 = texCoord94;
				float mulTime61 = _TimeParameters.x * ( 0.001 * _WindSpeed );
				float TIme152 = mulTime61;
				float simplePerlin2D37 = snoise( ( Pos159 + ( TIme152 * float2( 0.2,-0.4 ) ) )*( 100.0 / _MainCloudScale ) );
				simplePerlin2D37 = simplePerlin2D37*0.5 + 0.5;
				float SimpleCloudDensity314 = simplePerlin2D37;
				float time148 = 0.0;
				float2 voronoiSmoothId148 = 0;
				float2 temp_output_66_0 = ( Pos159 + ( TIme152 * float2( 0.3,0.2 ) ) );
				float2 coords148 = temp_output_66_0 * ( 140.0 / _MainCloudScale );
				float2 id148 = 0;
				float2 uv148 = 0;
				float voroi148 = voronoi148( coords148, time148, id148, uv148, 0, voronoiSmoothId148 );
				float time234 = 0.0;
				float2 voronoiSmoothId234 = 0;
				float2 coords234 = temp_output_66_0 * ( 500.0 / _MainCloudScale );
				float2 id234 = 0;
				float2 uv234 = 0;
				float voroi234 = voronoi234( coords234, time234, id234, uv234, 0, voronoiSmoothId234 );
				float2 appendResult312 = (float2(voroi148 , voroi234));
				float2 VoroDetails313 = appendResult312;
				float simplePerlin2D80 = snoise( ( ( float2( 5,20 ) * TIme152 ) + Pos159 )*( 100.0 / 400.0 ) );
				simplePerlin2D80 = simplePerlin2D80*0.5 + 0.5;
				float CurrentCloudCover240 = (_MinCloudCover + (simplePerlin2D80 - 0.0) * (_MaxCloudCover - _MinCloudCover) / (1.0 - 0.0));
				float CumulusCoverage376 = _CumulusCoverageMultiplier;
				float ComplexCloudDensity344 = (0.0 + (min( SimpleCloudDensity314 , ( 1.0 - VoroDetails313.x ) ) - ( 1.0 - ( CurrentCloudCover240 * CumulusCoverage376 ) )) * (1.0 - 0.0) / (1.0 - ( 1.0 - ( CurrentCloudCover240 * CumulusCoverage376 ) )));
				float time77 = 0.0;
				float2 voronoiSmoothId77 = 0;
				float2 coords77 = ( Pos159 + ( TIme152 * float2( 0.3,0.2 ) ) ) * ( 100.0 / _DetailScale );
				float2 id77 = 0;
				float2 uv77 = 0;
				float fade77 = 0.5;
				float voroi77 = 0;
				float rest77 = 0;
				for( int it77 = 0; it77 <3; it77++ ){
				voroi77 += fade77 * voronoi77( coords77, time77, id77, uv77, 0,voronoiSmoothId77 );
				rest77 += fade77;
				coords77 *= 2;
				fade77 *= 0.5;
				}//Voronoi77
				voroi77 /= rest77;
				float temp_output_47_0 = ( (0.0 + (( 1.0 - voroi77 ) - 0.3) * (0.5 - 0.0) / (1.0 - 0.3)) * 0.1 * _DetailAmount );
				float DetailedClouds347 = saturate( ( ComplexCloudDensity344 + temp_output_47_0 ) );
				float CloudDetail294 = temp_output_47_0;
				float2 texCoord113 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_114_0 = ( texCoord113 - float2( 0.5,0.5 ) );
				float dotResult140 = dot( temp_output_114_0 , temp_output_114_0 );
				float BorderHeight386 = ( 1.0 - _BorderHeight );
				float temp_output_392_0 = ( -2.0 * ( 1.0 - _BorderVariation ) );
				float clampResult420 = clamp( ( ( ( CloudDetail294 + SimpleCloudDensity314 ) * saturate( (( BorderHeight386 * temp_output_392_0 ) + (dotResult140 - 0.0) * (( temp_output_392_0 * -4.0 ) - ( BorderHeight386 * temp_output_392_0 )) / (0.5 - 0.0)) ) ) * 10.0 * _BorderEffect ) , -1.0 , 1.0 );
				float BorderLightTransport418 = clampResult420;
				float3 normalizeResult745 = normalize( ( WorldPosition - _WorldSpaceCameraPos ) );
				float3 normalizeResult773 = normalize( _StormDirection );
				float dotResult743 = dot( normalizeResult745 , normalizeResult773 );
				float2 texCoord721 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_726_0 = ( texCoord721 - float2( 0.5,0.5 ) );
				float dotResult728 = dot( temp_output_726_0 , temp_output_726_0 );
				float temp_output_763_0 = ( -2.0 * ( 1.0 - ( _NimbusVariation * 0.9 ) ) );
				float NimbusLightTransport739 = saturate( ( ( ( CloudDetail294 + SimpleCloudDensity314 ) * saturate( (( ( 1.0 - _NimbusMultiplier ) * temp_output_763_0 ) + (( dotResult743 + ( _NimbusHeight * 4.0 * dotResult728 ) ) - 0.5) * (( temp_output_763_0 * -4.0 ) - ( ( 1.0 - _NimbusMultiplier ) * temp_output_763_0 )) / (7.0 - 0.5)) ) ) * 10.0 ) );
				float temp_output_863_0 = saturate( ( DetailedClouds347 + BorderLightTransport418 + NimbusLightTransport739 ) );
				float mulTime874 = _TimeParameters.x * 0.5;
				float2 panner855 = ( ( mulTime874 * 0.004 ) * float2( 0.2,-0.4 ) + Pos159);
				float4 tex2DNode827 = tex2D( _CloudTexture, (panner855*1.0 + 0.75) );
				float cos818 = cos( ( mulTime874 * -0.01 ) );
				float sin818 = sin( ( mulTime874 * -0.01 ) );
				float2 rotator818 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos818 , -sin818 , sin818 , cos818 )) + float2( 0.5,0.5 );
				float4 tex2DNode826 = tex2D( _CloudTexture, (rotator818*3.0 + 0.75) );
				float4 CloudTexture839 = min( tex2DNode827 , tex2DNode826 );
				float clampResult841 = clamp( ( 2.0 * 0.5 ) , 0.0 , 0.98 );
				float CloudTextureFinal847 = ( CloudTexture839 * clampResult841 ).r;
				float time601 = 0.0;
				float2 voronoiSmoothId601 = 0;
				float mulTime602 = _TimeParameters.x * 0.003;
				float2 coords601 = (Pos159*1.0 + ( float2( 1,-2 ) * mulTime602 )) * 10.0;
				float2 id601 = 0;
				float2 uv601 = 0;
				float voroi601 = voronoi601( coords601, time601, id601, uv601, 0, voronoiSmoothId601 );
				float time492 = ( 10.0 * mulTime602 );
				float2 voronoiSmoothId492 = 0;
				float2 coords492 = IN.ase_texcoord2.xy * 10.0;
				float2 id492 = 0;
				float2 uv492 = 0;
				float voroi492 = voronoi492( coords492, time492, id492, uv492, 0, voronoiSmoothId492 );
				float AltoCumulusPlacement461 = saturate( ( ( ( 1.0 - 0.0 ) - (1.0 + (voroi601 - 0.0) * (-0.5 - 1.0) / (1.0 - 0.0)) ) - voroi492 ) );
				float time463 = 51.2;
				float2 voronoiSmoothId463 = 0;
				float2 coords463 = (Pos159*1.0 + ( _AltocumulusWindSpeed * TIme152 )) * ( 100.0 / _AltocumulusScale );
				float2 id463 = 0;
				float2 uv463 = 0;
				float fade463 = 0.5;
				float voroi463 = 0;
				float rest463 = 0;
				for( int it463 = 0; it463 <2; it463++ ){
				voroi463 += fade463 * voronoi463( coords463, time463, id463, uv463, 0,voronoiSmoothId463 );
				rest463 += fade463;
				coords463 *= 2;
				fade463 *= 0.5;
				}//Voronoi463
				voroi463 /= rest463;
				float AltoCumulusLightTransport447 = saturate( (-1.0 + (( AltoCumulusPlacement461 * ( 0.1 > voroi463 ? (0.5 + (voroi463 - 0.0) * (0.0 - 0.5) / (0.15 - 0.0)) : 0.0 ) * _AltocumulusMultiplier ) - 0.0) * (3.0 - -1.0) / (1.0 - 0.0)) );
				float mulTime566 = _TimeParameters.x * 0.01;
				float simplePerlin2D563 = snoise( (Pos159*1.0 + mulTime566)*2.0 );
				float mulTime560 = _TimeParameters.x * _ChemtrailsMoveSpeed;
				float cos553 = cos( ( mulTime560 * 0.01 ) );
				float sin553 = sin( ( mulTime560 * 0.01 ) );
				float2 rotator553 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos553 , -sin553 , sin553 , cos553 )) + float2( 0.5,0.5 );
				float cos561 = cos( ( mulTime560 * -0.02 ) );
				float sin561 = sin( ( mulTime560 * -0.02 ) );
				float2 rotator561 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos561 , -sin561 , sin561 , cos561 )) + float2( 0.5,0.5 );
				float mulTime568 = _TimeParameters.x * 0.01;
				float simplePerlin2D570 = snoise( (Pos159*1.0 + mulTime568)*4.0 );
				float4 ChemtrailsPattern576 = ( ( saturate( simplePerlin2D563 ) * tex2D( _ChemtrailsTexture, (rotator553*0.5 + 0.0) ) ) + ( tex2D( _ChemtrailsTexture, rotator561 ) * saturate( simplePerlin2D570 ) ) );
				float2 texCoord583 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_581_0 = ( texCoord583 - float2( 0.5,0.5 ) );
				float dotResult584 = dot( temp_output_581_0 , temp_output_581_0 );
				float4 ChemtrailsFinal590 = ( ChemtrailsPattern576 * saturate( (0.4 + (dotResult584 - 0.0) * (2.0 - 0.4) / (0.1 - 0.0)) ) * ( _ChemtrailsMultiplier * 0.5 ) );
				float mulTime611 = _TimeParameters.x * 0.01;
				float simplePerlin2D620 = snoise( (Pos159*1.0 + mulTime611)*2.0 );
				float mulTime607 = _TimeParameters.x * _CirrostratusMoveSpeed;
				float cos615 = cos( ( mulTime607 * 0.01 ) );
				float sin615 = sin( ( mulTime607 * 0.01 ) );
				float2 rotator615 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos615 , -sin615 , sin615 , cos615 )) + float2( 0.5,0.5 );
				float cos621 = cos( ( mulTime607 * -0.02 ) );
				float sin621 = sin( ( mulTime607 * -0.02 ) );
				float2 rotator621 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos621 , -sin621 , sin621 , cos621 )) + float2( 0.5,0.5 );
				float mulTime612 = _TimeParameters.x * 0.01;
				float simplePerlin2D618 = snoise( (Pos159*10.0 + mulTime612)*4.0 );
				float4 CirrostratPattern634 = ( ( saturate( simplePerlin2D620 ) * tex2D( _CirrostratusTexture, (rotator615*1.5 + 0.75) ) ) + ( tex2D( _CirrostratusTexture, (rotator621*1.5 + 0.75) ) * saturate( simplePerlin2D618 ) ) );
				float2 texCoord625 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_629_0 = ( texCoord625 - float2( 0.5,0.5 ) );
				float dotResult630 = dot( temp_output_629_0 , temp_output_629_0 );
				float4 CirrostratLightTransport641 = ( CirrostratPattern634 * saturate( (0.4 + (dotResult630 - 0.0) * (2.0 - 0.4) / (0.1 - 0.0)) ) * ( _CirrostratusMultiplier * 1.0 ) );
				float mulTime673 = _TimeParameters.x * 0.01;
				float simplePerlin2D681 = snoise( (Pos159*1.0 + mulTime673)*2.0 );
				float mulTime666 = _TimeParameters.x * _CirrusMoveSpeed;
				float cos677 = cos( ( mulTime666 * 0.01 ) );
				float sin677 = sin( ( mulTime666 * 0.01 ) );
				float2 rotator677 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos677 , -sin677 , sin677 , cos677 )) + float2( 0.5,0.5 );
				float cos676 = cos( ( mulTime666 * -0.02 ) );
				float sin676 = sin( ( mulTime666 * -0.02 ) );
				float2 rotator676 = mul( Pos159 - float2( 0.5,0.5 ) , float2x2( cos676 , -sin676 , sin676 , cos676 )) + float2( 0.5,0.5 );
				float mulTime670 = _TimeParameters.x * 0.01;
				float simplePerlin2D682 = snoise( (Pos159*1.0 + mulTime670) );
				simplePerlin2D682 = simplePerlin2D682*0.5 + 0.5;
				float4 CirrusPattern696 = ( ( saturate( simplePerlin2D681 ) * tex2D( _CirrusTexture, (rotator677*1.5 + 0.75) ) ) + ( tex2D( _CirrusTexture, (rotator676*1.0 + 0.0) ) * saturate( simplePerlin2D682 ) ) );
				float2 texCoord685 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_690_0 = ( texCoord685 - float2( 0.5,0.5 ) );
				float dotResult694 = dot( temp_output_690_0 , temp_output_690_0 );
				float4 temp_output_701_0 = ( CirrusPattern696 * saturate( (0.0 + (dotResult694 - 0.0) * (2.0 - 0.0) / (0.2 - 0.0)) ) );
				float CirrusAlpha715 = ( ( temp_output_701_0 * ( _CirrusMultiplier * 10.0 ) ).r * 0.6 );
				float4 FinalAlpha408 = saturate( ( saturate( ( temp_output_863_0 + ( (-1.0 + (CloudTextureFinal847 - 0.0) * (1.0 - -1.0) / (1.0 - 0.0)) * _TextureAmount * sin( ( temp_output_863_0 * PI ) ) ) ) ) + AltoCumulusLightTransport447 + ChemtrailsFinal590 + CirrostratLightTransport641 + CirrusAlpha715 ) );
				

				float Alpha = saturate( ( FinalAlpha408.r + ( FinalAlpha408.r * 2.0 * _CloudThickness ) ) );
				float AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif
				return 0;
			}
			ENDHLSL
		}

	
	}
	
	
	Fallback "Hidden/InternalErrorShader"
	
}
/*ASEBEGIN
Version=19002
6.285715;1085.143;2181.714;596.1429;1959.284;805.1706;1;True;False
Node;AmplifyShaderEditor.CommentaryNode;372;-4255.697,-4622.015;Inherit;False;2254.259;1199.93;;42;790;789;786;784;783;781;780;779;778;777;776;775;267;266;271;270;265;269;263;264;268;262;261;259;260;258;257;256;332;334;52;36;376;375;159;94;152;61;150;70;796;797;Variable Declaration;0.6196079,0.9508546,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;70;-3324.89,-4226.262;Inherit;False;Property;_WindSpeed;Wind Speed;11;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;150;-3172.891,-4247.994;Inherit;False;2;2;0;FLOAT;0.001;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;423;-250.2423,-1430.506;Inherit;False;2974.933;2000.862;;5;371;396;397;395;373;Cumulus Cloud Block;0.4392157,1,0.7085855,1;0;0
Node;AmplifyShaderEditor.SimpleTimeNode;61;-3039.942,-4244.458;Inherit;False;1;0;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;94;-3079.566,-4417.043;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;152;-2871.501,-4248.563;Inherit;False;TIme;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;373;-203.3957,-786.2642;Inherit;False;1226.633;651.0015;Simple Density;20;149;314;37;42;313;63;312;60;157;148;62;234;65;66;235;68;161;41;67;156;;0.4392157,1,0.7085855,1;0;0
Node;AmplifyShaderEditor.Vector2Node;67;-160.9557,-410.9344;Inherit;False;Constant;_CloudWind2;Cloud Wind 2;14;1;[HideInInspector];Create;True;0;0;0;False;0;False;0.3,0.2;0.1,0.2;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.CommentaryNode;374;-4200.95,-3083.249;Inherit;False;1589.831;729.3493;;19;243;245;246;244;247;240;248;317;80;318;57;74;83;72;71;160;81;82;158;Main Cloud Coverage;0.6196079,0.9508546,1,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;156;-158.4826,-479.7553;Inherit;False;152;TIme;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;159;-2876.129,-4422.287;Inherit;False;Pos;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;82;-4120.487,-2866.749;Inherit;False;Constant;_CloudCoverageMovement;Cloud Coverage Movement;14;1;[HideInInspector];Create;True;0;0;0;False;0;False;5,20;5,20;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.CommentaryNode;395;-191.1863,19.66455;Inherit;False;1980.736;453.4427;Final Detailing;17;347;49;48;294;346;47;46;51;78;77;93;44;43;162;92;155;91;;0.4392157,1,0.7085855,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;158;-4031.467,-2724.653;Inherit;False;152;TIme;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;68;87.06122,-412.8354;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;161;50.75578,-713.4048;Inherit;False;159;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;41;35.16191,-624.1489;Inherit;False;Property;_MainCloudScale;Main Cloud Scale;4;1;[HideInInspector];Create;True;0;0;0;False;0;False;10;20;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;155;-159.114,196.3215;Inherit;False;152;TIme;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;235;293.0141,-270.2905;Inherit;False;2;0;FLOAT;500;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;160;-3828.703,-2718.782;Inherit;False;159;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;91;-154.8089,298.2306;Inherit;False;Constant;_DetailWind;Detail Wind;17;0;Create;True;0;0;0;False;0;False;0.3,0.2;0.3,0.8;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;71;-3728.025,-2637.921;Inherit;False;Constant;_CloudCoverageScale;Cloud Coverage Scale;15;0;Create;True;0;0;0;False;0;False;400;400;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;65;293.5053,-376.8996;Inherit;False;2;0;FLOAT;140;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;66;286.643,-496.0274;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;81;-3804.042,-2822.877;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;92;58.07913,229.1355;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.VoronoiNode;148;480.1892,-443.5733;Inherit;False;0;0;1;3;1;False;1;False;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.GetLocalVarNode;162;19.69116,116.4622;Inherit;False;159;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;62;-154.5866,-604.686;Inherit;False;Constant;_CloudWind1;Cloud Wind 1;13;1;[HideInInspector];Create;True;0;0;0;False;0;False;0.2,-0.4;0.6,-0.8;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleAddOpNode;83;-3658.171,-2819.53;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;43;36.27663,333.869;Inherit;False;Property;_DetailScale;Detail Scale;13;1;[HideInInspector];Create;True;0;0;0;False;0;False;0.5;2.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.VoronoiNode;234;478.0144,-307.2905;Inherit;False;0;0;1;3;1;False;1;False;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.SimpleDivideOpNode;74;-3492.3,-2728.253;Inherit;False;2;0;FLOAT;100;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;157;-157.0807,-680.7291;Inherit;False;152;TIme;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;57;-3313.877,-2517.452;Inherit;False;Property;_MaxCloudCover;Max Cloud Cover;9;1;[HideInInspector];Create;True;0;0;0;False;0;False;1;0.2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;80;-3362,-2822.931;Inherit;True;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;312;667.2,-359.5185;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;60;87.51923,-534.9066;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;93;220.5464,171.2449;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;806;-3574.271,115.5002;Inherit;False;2654.838;1705.478;;5;821;807;857;860;861;Cloud Texture Block;0.345098,0.8386047,1,1;0;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;44;218.4492,268.0113;Inherit;False;2;0;FLOAT;100;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;718;5760,-768;Inherit;False;2713.637;1035.553;;30;769;760;739;750;737;735;733;734;731;730;758;765;766;743;763;761;745;744;746;762;748;747;768;759;728;721;726;770;771;773;Nimbus Block;0.5,0.5,0.5,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;72;-3315.477,-2599.466;Inherit;False;Property;_MinCloudCover;Min Cloud Cover;10;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;248;-3053.39,-2661.413;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos;748;5802.087,-531.1355;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;375;-2588.193,-4479.109;Inherit;False;Property;_CumulusCoverageMultiplier;Cumulus Coverage Multiplier;12;2;[HideInInspector];[Header];Create;True;1;Cumulus Clouds;0;0;False;0;False;1;0.676;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.VoronoiNode;77;353.8299,169.9679;Inherit;True;0;0;1;0;3;False;1;False;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.RangedFloatNode;759;6409.42,110.4931;Inherit;False;Property;_NimbusVariation;Nimbus Variation;19;1;[HideInInspector];Create;True;0;0;0;False;0;False;1;0.903;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;313;808.2874,-365.6784;Inherit;False;VoroDetails;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;422;3408,-752;Inherit;False;2111.501;762.0129;;21;418;420;417;406;399;401;421;398;402;128;389;393;140;392;114;386;113;394;390;387;391;Cloud Border Block;1,0.5882353,0.685091,1;0;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;42;282.7172,-600.2411;Inherit;False;2;0;FLOAT;100;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;63;283.9169,-707.6729;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;397;1082.985,-632.3273;Inherit;False;1154;500;Complex Density;11;380;50;241;344;326;328;69;327;321;381;58;;0.4392157,1,0.7085855,1;0;0
Node;AmplifyShaderEditor.WorldPosInputsNode;747;5861.095,-678.9246;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TextureCoordinatesNode;721;5969.644,-260.1304;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;807;-3532.259,169.0804;Inherit;False;2197.287;953.2202;Pattern;14;839;827;826;824;823;818;816;814;812;808;854;855;874;875;;0.345098,0.8386047,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;391;3488,-128;Inherit;False;Property;_BorderVariation;Border Variation;16;1;[HideInInspector];Create;True;0;0;0;False;0;False;1;0.929;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;326;1081.763,-476.7542;Inherit;False;313;VoroDetails;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;746;6079.842,-607.6136;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;768;6679.782,116.2336;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.9;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;663;6128.124,-2822.822;Inherit;False;2297.557;1709.783;;2;678;664;Cirrus Block;1,0.6554637,0.4588236,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;240;-2878.356,-2665.734;Inherit;False;CurrentCloudCover;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;808;-3478.739,664.8328;Inherit;False;Constant;_CloudTextureChangeSpeed;Cloud Texture Change Speed;28;0;Create;True;0;0;0;False;0;False;0.5;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;726;6193.644,-276.1304;Inherit;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector3Node;744;6069.037,-466.4966;Inherit;False;Property;_StormDirection;Storm Direction;21;1;[HideInInspector];Create;True;0;0;0;False;0;False;0,0,0;0.8920146,0,0.4520065;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.NoiseGeneratorNode;37;466.9412,-696.6409;Inherit;True;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;376;-2284.34,-4479.701;Inherit;False;CumulusCoverage;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;387;3488,-240;Inherit;False;Property;_BorderHeight;Border Height;15;2;[HideInInspector];[Header];Create;True;1;Border Clouds;0;0;False;0;False;1;0.508;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;78;538.305,169.8402;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;46;698.9319,344.7032;Inherit;False;Property;_DetailAmount;Detail Amount;14;1;[HideInInspector];Create;True;0;0;0;False;0;False;1;30;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;113;3552,-480;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;760;6540.42,-4.50688;Inherit;False;Property;_NimbusMultiplier;Nimbus Multiplier;18;2;[HideInInspector];[Header];Create;True;1;Nimbus Clouds;0;0;False;0;False;1;0;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;381;1227.592,-228.8145;Inherit;False;376;CumulusCoverage;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;664;6170.135,-2769.242;Inherit;False;2197.287;953.2202;Pattern;25;696;693;691;688;687;686;684;683;682;681;680;679;677;676;675;674;673;672;671;670;669;668;667;666;665;;1,0.6554637,0.4588236,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;314;699.5449,-698.9639;Inherit;False;SimpleCloudDensity;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;773;6261.161,-476.4007;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.BreakToComponentsNode;327;1266.989,-472.1505;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.CommentaryNode;528;-228.5223,860.652;Inherit;False;3128.028;1619.676;;3;527;496;462;Altocumulus Cloud Block;0.6637449,0.4708971,0.6981132,1;0;0
Node;AmplifyShaderEditor.OneMinusNode;390;3760,-240;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;241;1225.609,-303.6433;Inherit;False;240;CurrentCloudCover;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;762;6810.42,108.4931;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;745;6217.36,-604.9607;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;394;3760,-128;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;769;6375.541,-363.9453;Inherit;False;Property;_NimbusHeight;Nimbus Height;20;1;[HideInInspector];Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;51;695.7791,170.1474;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0.3;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;874;-3206.379,679.0629;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;728;6385.644,-260.1304;Inherit;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;386;3920,-240;Inherit;False;BorderHeight;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;392;3936,-160;Inherit;False;2;2;0;FLOAT;-2;False;1;FLOAT;-0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;328;1382.99,-475.1505;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;814;-2970.22,623.264;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.004;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;743;6442.698,-600.2336;Inherit;True;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;321;1302.474,-566.5957;Inherit;False;314;SimpleCloudDensity;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;462;-162.0468,961.969;Inherit;False;2021.115;830.0204;Placement Noise;18;452;433;435;429;438;455;461;457;458;442;451;454;491;492;494;600;601;602;;0.6637449,0.4708971,0.6981132,1;0;0
Node;AmplifyShaderEditor.OneMinusNode;761;6903.42,-10.50688;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;380;1469.592,-298.8145;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;114;3776,-480;Inherit;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;816;-2965.687,721.3909;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;812;-2975.22,539.264;Inherit;False;159;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;763;6965.42,83.49312;Inherit;False;2;2;0;FLOAT;-2;False;1;FLOAT;-0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;665;6202.655,-2272.49;Inherit;False;Property;_CirrusMoveSpeed;Cirrus Move Speed;30;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;47;882.0814,183.5532;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0.1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;770;6648.459,-360.0035;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;4;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;140;3968,-480;Inherit;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;58;1637.088,-299.8031;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;666;6438.377,-2267.659;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;818;-2792.685,700.3909;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;294;1046.829,82.25906;Inherit;False;CloudDetail;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;602;-39.72644,1648.263;Inherit;False;1;0;FLOAT;0.003;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;457;-36.07951,1389.731;Inherit;False;Constant;_ACMoveSpeed;ACMoveSpeed;14;0;Create;True;0;0;0;False;0;False;1,-2;5,20;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;389;4128,-256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMinOpNode;69;1582.449,-520.7233;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;393;4128,-160;Inherit;False;2;2;0;FLOAT;-4;False;1;FLOAT;-4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;771;6816.106,-441.474;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;765;7122.42,-12.50689;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;855;-2791.458,546.2455;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.2,-0.4;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;603;3180.966,811.0074;Inherit;False;2654.838;1705.478;;3;651;605;604;Cirrostratus Block;0.4588236,0.584294,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;597;3513.774,-2825.563;Inherit;False;2340.552;1688.827;;2;596;595;Chemtrails Block;1,0.9935331,0.4575472,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;766;7122.42,83.49312;Inherit;False;2;2;0;FLOAT;-4;False;1;FLOAT;-4;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;758;7315.526,-79.20961;Inherit;True;5;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;7;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;672;6766.873,-2026.124;Inherit;False;159;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;595;3555.786,-2771.983;Inherit;False;2197.287;953.2202;Pattern;24;576;573;587;588;593;551;556;592;563;561;555;570;553;562;569;565;567;554;559;568;564;566;560;571;;1,0.9935331,0.4575472,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;402;4336,-496;Inherit;False;314;SimpleCloudDensity;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;730;7390.492,-283.6436;Inherit;False;294;CloudDetail;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;667;6686.708,-2214.932;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-0.02;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;458;157.3604,1538.603;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;671;6787.709,-2618.931;Inherit;False;159;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCRemapNode;128;4320,-400;Inherit;True;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0.5;False;3;FLOAT;-2;False;4;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;604;3222.978,864.5875;Inherit;False;2197.287;953.2202;Pattern;25;634;631;628;627;626;624;623;622;621;620;619;618;617;616;615;614;613;612;611;610;609;608;607;606;643;;0.4588236,0.584294,1,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;398;4384,-576;Inherit;False;294;CloudDetail;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;823;-2599.219,539.264;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;1;False;2;FLOAT;0.75;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCRemapNode;50;1817.57,-531.1767;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0.3;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;673;6789.709,-2538.93;Inherit;False;1;0;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;731;7342.492,-194.6436;Inherit;False;314;SimpleCloudDensity;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;455;173.7564,1387.099;Inherit;False;159;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;669;6670.173,-2304.059;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;824;-2595.977,707.991;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;3;False;2;FLOAT;0.75;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;670;6768.874,-1946.124;Inherit;False;1;0;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;668;6727.173,-2399.059;Inherit;False;159;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;674;7037.709,-2599.93;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;606;3255.498,1361.34;Inherit;False;Property;_CirrostratusMoveSpeed;Cirrostratus Move Speed;26;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;344;1993.289,-535.4535;Inherit;False;ComplexCloudDensity;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;675;7016.874,-2007.124;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotatorNode;676;6909.71,-2237.932;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;421;4592,-400;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;677;6907.173,-2394.059;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;571;3588.306,-2275.23;Inherit;False;Property;_ChemtrailsMoveSpeed;Chemtrails Move Speed;34;1;[HideInInspector];Create;False;0;0;0;False;0;False;0;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;734;7590.39,-79.34389;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;827;-2398.256,501.683;Inherit;True;Property;_CloudTexture;Cloud Texture;28;0;Create;True;0;0;0;False;0;False;-1;27248a215d4e5fe449733cb0631f0785;27248a215d4e5fe449733cb0631f0785;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;826;-2398.256,709.6829;Inherit;True;Property;_TextureSample2;Texture Sample 2;28;0;Create;True;0;0;0;False;0;False;-1;None;9b3476b4df9abf8479476bae1bcd8a84;True;0;False;white;Auto;False;Instance;827;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;401;4560,-544;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;733;7580.492,-237.6435;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;454;335.4072,1438.633;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;1;False;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;821;-3516.336,1183.734;Inherit;False;1600.229;583.7008;Final;12;847;843;842;841;840;838;837;836;834;832;828;894;;0.345098,0.8386047,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;406;4608,-288;Inherit;False;Property;_BorderEffect;Border Effect;17;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;1;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;678;6186.058,-1754.588;Inherit;False;1735.998;586.5895;Final;14;715;714;695;689;706;701;698;700;697;694;690;685;899;900;;1,0.6554637,0.4588236,1;0;0
Node;AmplifyShaderEditor.SimpleMinOpNode;875;-2012.488,707.0767;Inherit;False;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;399;4720,-432;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;607;3491.22,1366.17;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;346;776.4108,82.35074;Inherit;False;344;ComplexCloudDensity;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;836;-3112.837,1590.493;Inherit;False;Constant;_CloudTextureMultiplier;Cloud Texture Multiplier;25;0;Create;True;1;Cirrostratus Clouds;0;0;False;0;False;2;2;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;679;7103.175,-2399.059;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;1.5;False;2;FLOAT;0.75;False;1;FLOAT2;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;682;7241.874,-2002.124;Inherit;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;560;3824.028,-2270.4;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;680;7106.417,-2230.332;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;681;7262.709,-2594.93;Inherit;False;Simplex2D;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.VoronoiNode;601;515.2736,1436.263;Inherit;True;0;0;1;3;1;False;1;False;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;10;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.CommentaryNode;496;-150.0306,1829.663;Inherit;False;2200.287;555.4289;Main Noise;16;490;471;499;484;473;463;480;474;477;475;479;478;476;447;800;801;;0.6637449,0.4708971,0.6981132,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;735;7732.492,-141.6436;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;554;4112.825,-2401.799;Inherit;False;159;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;684;7304.139,-2436.64;Inherit;True;Property;_CirrusTexture;Cirrus Texture;31;0;Create;True;0;0;0;False;0;False;-1;None;302629ebb64a0e345948779662fc2cf3;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;686;7304.139,-2228.64;Inherit;True;Property;_TextureSample1;Texture Sample 1;31;0;Create;True;0;0;0;False;0;False;-1;None;9b3476b4df9abf8479476bae1bcd8a84;True;0;False;white;Auto;False;Instance;684;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;451;872.4244,1165.934;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;600;740.4915,1401.016;Inherit;True;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;-0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;48;1220.584,166.7458;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;417;4928,-416;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;10;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;564;4173.361,-2621.671;Inherit;False;159;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;687;7445.01,-2584.845;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;567;4152.524,-2028.865;Inherit;False;159;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;683;7448.496,-1998.098;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;609;3840.552,1014.899;Inherit;False;159;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;610;3819.715,1607.705;Inherit;False;159;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;559;4055.824,-2306.799;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;737;7875.492,-139.6436;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;494;809.8984,1624.261;Inherit;False;2;2;0;FLOAT;10;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;839;-1856.256,609.6829;Inherit;True;CloudTexture;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;838;-2824.837,1591.493;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;476;84.65072,2160.052;Inherit;False;152;TIme;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;612;3821.716,1687.705;Inherit;False;1;0;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;566;4175.361,-2541.671;Inherit;False;1;0;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;685;6238.183,-1544.158;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleTimeNode;568;4154.524,-1948.865;Inherit;False;1;0;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;478;7.86438,2028.4;Inherit;False;Property;_AltocumulusWindSpeed;Altocumulus Wind Speed;24;1;[HideInInspector];Create;True;0;0;0;False;0;False;1,-2;-0.1,0.5;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.GetLocalVarNode;613;3780.016,1234.771;Inherit;False;159;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;617;3739.55,1418.898;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-0.02;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;608;3723.016,1329.771;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;611;3842.552,1094.899;Inherit;False;1;0;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;621;3962.552,1395.898;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;842;-2771.558,1286.597;Inherit;False;839;CloudTexture;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;565;4423.36,-2602.671;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ClampOpNode;420;5072,-400;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;-1;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;691;7672.139,-2452.64;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;690;6444.183,-1545.158;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;614;4090.552,1033.899;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;688;7672.139,-2228.64;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RotatorNode;553;4292.823,-2396.799;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotatorNode;615;3960.015,1239.771;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;479;231.5935,2263.727;Inherit;False;Property;_AltocumulusScale;Altocumulus Scale;23;1;[HideInInspector];Create;True;0;0;0;False;0;False;3;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;452;1018.591,1168.67;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;49;1434.308,165.2476;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;569;4402.524,-2009.865;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;477;276.5755,2089.215;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;616;4069.716,1626.705;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;10;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;475;242.6316,1959.64;Inherit;False;159;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.VoronoiNode;492;1069.963,1399.258;Inherit;True;0;0;1;0;1;False;1;False;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;12.27;False;2;FLOAT;10;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.CommentaryNode;774;-479.6465,-4338.578;Inherit;False;3038.917;2502.995;;4;407;652;500;369;Finalization Block;0.6196079,0.9508546,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;562;4072.359,-2217.672;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-0.02;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;750;8024.268,-128.1811;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;841;-2680.837,1590.493;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0.98;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;693;7848.139,-2340.64;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;418;5296,-428;Inherit;False;BorderLightTransport;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;694;6600.185,-1548.158;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;480;477.5076,2148.297;Inherit;False;2;0;FLOAT;100;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;561;4394.36,-2237.672;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;643;4159.26,1403.498;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;1.5;False;2;FLOAT;0.75;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;347;1585.735,154.9825;Inherit;False;DetailedClouds;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;491;1172.962,1166.258;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;618;4294.716,1631.705;Inherit;False;Simplex2D;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;563;4648.36,-2597.671;Inherit;False;Simplex2D;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;620;4315.552,1038.899;Inherit;False;Simplex2D;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;555;4496.824,-2397.799;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;570;4627.524,-2004.865;Inherit;False;Simplex2D;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;474;420.3206,1963.144;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;1;False;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;739;8191.493,-129.6436;Inherit;True;NimbusLightTransport;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;407;-422.4442,-4222.23;Inherit;False;2843.676;639.4145;Final Alpha;23;883;884;885;408;411;410;644;598;871;497;713;868;869;870;867;863;866;865;862;414;740;409;886;;0.6196079,0.9508546,1,1;0;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;619;4156.017,1234.771;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;1.5;False;2;FLOAT;0.75;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;605;3238.901,1879.241;Inherit;False;1600.229;583.7008;Final;10;641;639;637;636;633;632;630;629;625;658;;0.4588236,0.584294,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;843;-2476.205,1383.366;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;596;3571.709,-1757.329;Inherit;False;1600.229;583.7008;Final;10;590;578;577;586;591;594;583;581;582;584;;1,0.9935331,0.4575472,1;0;0
Node;AmplifyShaderEditor.SaturateNode;626;4501.338,1635.731;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VoronoiNode;463;619.5694,2035.68;Inherit;True;0;0;1;0;2;False;1;False;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;51.2;False;2;FLOAT;3;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.TextureCoordinatesNode;583;3646.834,-1544.899;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;624;4497.852,1048.984;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;697;6738.487,-1547.458;Inherit;True;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0.2;False;3;FLOAT;0;False;4;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;409;-354.5005,-4172.425;Inherit;False;347;DetailedClouds;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;593;4830.659,-2587.586;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;551;4689.789,-2439.38;Inherit;True;Property;_ChemtrailsTexture;Chemtrails Texture;35;0;Create;True;0;0;0;False;0;False;-1;None;9b3476b4df9abf8479476bae1bcd8a84;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;414;-349.1048,-4092.599;Inherit;False;418;BorderLightTransport;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;622;4356.981,1405.19;Inherit;True;Property;_TextureSample0;Texture Sample 0;27;0;Create;True;0;0;0;False;0;False;-1;None;9b3476b4df9abf8479476bae1bcd8a84;True;0;False;white;Auto;False;Instance;623;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;625;3314.026,2091.671;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BreakToComponentsNode;894;-2322.757,1392.667;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RegisterLocalVarNode;696;8069.139,-2343.64;Inherit;False;CirrusPattern;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;556;4689.789,-2231.38;Inherit;True;Property;_ChemtrailsTex2;Chemtrails Tex 2;35;0;Create;True;0;0;0;False;0;False;-1;None;9b3476b4df9abf8479476bae1bcd8a84;True;0;False;white;Auto;False;Instance;551;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;623;4356.981,1197.19;Inherit;True;Property;_CirrostratusTexture;Cirrostratus Texture;27;0;Create;True;0;0;0;False;0;False;-1;None;bf43c8d7b74e204469465f36dfff7d6a;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;442;1330.143,1165.87;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;592;4834.146,-2000.839;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;740;-337.5044,-4008.629;Inherit;False;739;NimbusLightTransport;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;700;6992.107,-1542.236;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;627;4724.981,1181.19;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;847;-2157.138,1388.272;Inherit;False;CloudTextureFinal;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;461;1507.859,1165.986;Inherit;False;AltoCumulusPlacement;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;587;5057.789,-2455.38;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;628;4724.981,1405.19;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;588;5057.789,-2231.38;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;581;3878.834,-1543.899;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;689;6792.185,-1317.058;Inherit;False;Property;_CirrusMultiplier;Cirrus Multiplier;29;2;[HideInInspector];[Header];Create;True;1;Cirrus Clouds;0;0;False;0;False;1;0;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;629;3546.026,2092.671;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;698;6901.737,-1662.425;Inherit;False;696;CirrusPattern;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCRemapNode;473;812.6242,2128.514;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0.15;False;3;FLOAT;0.5;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;862;-41.25244,-4150.762;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;695;7157.684,-1493.445;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;701;7157.289,-1608.256;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;631;4900.981,1293.19;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;484;998.3405,1932.757;Inherit;False;461;AltoCumulusPlacement;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;863;84.64758,-4153.762;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Compare;471;999.6965,2010.744;Inherit;True;2;4;0;FLOAT;0.1;False;1;FLOAT;0.3;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;865;334.5441,-3873.132;Inherit;False;847;CloudTextureFinal;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;630;3722.027,2091.671;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;584;4054.835,-1544.899;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;499;996.4494,2236.662;Inherit;False;Property;_AltocumulusMultiplier;Altocumulus Multiplier;22;2;[HideInInspector];[Header];Create;True;1;Altocumulus Clouds;0;0;False;0;False;2;0;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;573;5233.789,-2343.38;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.PiNode;885;14.45471,-4021.182;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;714;7317.954,-1523.05;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCRemapNode;633;3861.329,2090.371;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0.1;False;3;FLOAT;0.4;False;4;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;577;4070.836,-1363.799;Inherit;False;Property;_ChemtrailsMultiplier;Chemtrails Multiplier;32;1;[HideInInspector];Create;True;1;Chemtrails;0;0;False;0;False;1;0;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;582;4194.137,-1546.199;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0.1;False;3;FLOAT;0.4;False;4;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;490;1243.414,1961.093;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;576;5454.789,-2346.38;Inherit;False;ChemtrailsPattern;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;884;204.4547,-4059.182;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;632;3746,2286;Inherit;False;Property;_CirrostratusMultiplier;Cirrostratus Multiplier;25;2;[HideInInspector];[Header];Create;True;1;Cirrostratus Clouds;0;0;False;0;False;1;0;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;866;578.9585,-3873.718;Inherit;False;FLOAT;1;0;FLOAT;0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RegisterLocalVarNode;634;5121.981,1290.19;Inherit;False;CirrostratPattern;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;637;3974.579,1983.404;Inherit;False;634;CirrostratPattern;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SinOpNode;883;337.4547,-4059.182;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;658;4018,2286;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;636;4044.949,2091.593;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;867;778.0581,-3872.718;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-1;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;899;7448.285,-1523.582;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;578;4358.836,-1363.799;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;870;696.092,-3712.342;Inherit;False;Property;_TextureAmount;Texture Amount;36;1;[HideInInspector];Create;True;0;0;0;False;0;False;1;1;0;3;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;594;4377.758,-1544.977;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;591;4307.387,-1653.166;Inherit;False;576;ChemtrailsPattern;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCRemapNode;800;1384.123,1963.741;Inherit;True;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-1;False;4;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;639;4210.131,2025.573;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;900;7560.285,-1523.582;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.6;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;869;1000.392,-3789.441;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;801;1643.123,1987.741;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;586;4542.938,-1610.997;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;447;1784.112,1978.244;Inherit;False;AltoCumulusLightTransport;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;641;4589.796,2119.112;Inherit;False;CirrostratLightTransport;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;590;4933.604,-1517.458;Inherit;False;ChemtrailsFinal;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;715;7679.589,-1528.182;Inherit;False;CirrusAlpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;868;1468.715,-4139.385;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;598;1603.699,-3848.613;Inherit;False;590;ChemtrailsFinal;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;497;1538.693,-3932.853;Inherit;False;447;AltoCumulusLightTransport;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;713;1623.716,-3686.311;Inherit;False;715;CirrusAlpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;644;1558.727,-3763.764;Inherit;False;641;CirrostratLightTransport;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;871;1600.836,-4138.984;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;410;1855.611,-3974.932;Inherit;False;5;5;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;411;1998.611,-3962.932;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;408;2175.737,-3969.737;Inherit;False;FinalAlpha;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;901;-1452.989,-472.1968;Inherit;False;408;FinalAlpha;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.BreakToComponentsNode;902;-1276.989,-472.1968;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RangedFloatNode;903;-1420.989,-328.1968;Inherit;False;Property;_CloudThickness;CloudThickness;33;1;[HideInInspector];Create;True;0;0;0;False;0;False;1;1;0;4;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;904;-1132.989,-408.1968;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;2;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;369;-414.8885,-2846.547;Inherit;False;2881.345;950.1069;Final Coloring;39;795;794;793;792;791;205;214;340;236;330;351;522;280;513;523;281;171;336;349;227;331;225;216;415;53;339;55;337;338;54;345;515;151;889;891;892;893;897;898;;0.6196079,0.9508546,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;500;-403.2868,-3511.748;Inherit;False;1393.195;555.0131;Simple Radiance;8;501;502;504;505;506;599;712;741;;0.6196079,0.9508546,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;652;1051.16,-3520.311;Inherit;False;1393.195;555.0131;Custom Radiance;5;654;655;656;657;653;;0.6196079,0.9508546,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;527;1904.552,952.911;Inherit;False;939.7803;621.1177;Lighting & Clipping;10;512;510;511;524;521;516;509;519;518;520;;0.6637449,0.4708971,0.6981132,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;396;-213.2237,-1240.208;Inherit;False;1283.597;293.2691;Thickness Details;10;223;378;329;222;379;316;320;315;224;319;;0.4392157,1,0.7085855,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;651;4864.461,1878.709;Inherit;False;916.8853;383.8425;Lighting & Clipping;6;648;650;649;647;646;645;;0.4588236,0.584294,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;371;1099.14,-1213.321;Inherit;False;1576.124;399.0991;Highlights;13;383;278;382;273;283;272;284;274;335;295;282;277;279;;0.4392157,1,0.7085855,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;905;-1004.989,-472.1968;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;340;51.5418,-2382.136;Inherit;False;334;CloudHighlightColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;509;2072.735,1300.613;Inherit;False;447;AltoCumulusLightTransport;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;512;2426.443,1070.978;Inherit;False;CirrusCustomLightColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;205;294.542,-2402.136;Inherit;False;3;3;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;349;855.6476,-2326.646;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;793;66.6481,-2106.958;Inherit;False;797;MoonlightColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;336;823.6486,-2582.646;Inherit;False;332;CloudColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;149;665.326,-444.6863;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;857;-1806.324,1332.86;Inherit;False;847;CloudTextureFinal;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;284;2070.191,-965.471;Inherit;False;Constant;_2;2;15;1;[HideInInspector];Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;656;1713.599,-3268.224;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;795;309.6483,-2126.958;Inherit;False;3;3;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;655;1578.962,-3265.042;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;518;1973.541,1390.363;Inherit;False;506;SimpleRadiance;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;840;-2710.288,1396.086;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;339;472.0735,-2356.739;Inherit;False;332;CloudColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;281;1031.646,-2326.646;Inherit;False;273;SunThroughClouds;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;227;800.0724,-2485.739;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;1,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCRemapNode;837;-2893.907,1394.864;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0.1;False;3;FLOAT;0.4;False;4;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;53;274.542,-2654.136;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;273;2435.788,-1085.51;Inherit;False;SunThroughClouds;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;247;-3270.67,-2987.079;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;435;339.8155,1083.927;Inherit;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;258;-3939.019,-4100.609;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;897;1378.035,-2629.778;Inherit;False;Global;CZY_LightColor;CZY_LightColor;40;0;Create;True;0;0;0;False;0;False;0,0,0,0;1.328976,0.5181817,1.302222,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;523;1307.463,-2231.542;Inherit;False;657;CustomRadiance;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;280;1271.647,-2422.646;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;898;1602.035,-2651.778;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0.5,0.5,0.5,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;171;1031.646,-2454.646;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;516;2382.882,1327.069;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;279;1491.307,-1081.408;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;243;-3766.84,-2978.463;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;510;2266.919,1072.897;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0.7159576,0.8624095,0.8773585,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;505;302.9577,-3317.181;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;828;-3441.211,1396.164;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;272;2069.881,-1094.994;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;506;445.0833,-3320.804;Inherit;False;SimpleRadiance;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;378;72.11449,-1104.554;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DotProductOpNode;245;-3403.879,-2989.757;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;784;-3369.919,-3657.697;Half;False;Property;_MoonFlareFalloff;Moon Flare Falloff;6;1;[HideInInspector];Create;True;0;0;0;False;0;False;1;22.9;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;776;-4118.636,-3837.629;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;515;888.9966,-2030.082;Inherit;False;Clipping;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;786;-3169.658,-3770.558;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;269;-3152.916,-3962.071;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;318;-3046.342,-2498.671;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;278;1132.308,-1087.408;Inherit;False;240;CurrentCloudCover;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;264;-3304.19,-4092.997;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;315;301.7378,-1209.101;Inherit;False;313;VoroDetails;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.BreakToComponentsNode;320;207.6576,-1102.932;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.GetLocalVarNode;379;-161.8844,-1034.554;Inherit;False;376;CumulusCoverage;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;262;-3527.523,-4095.134;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;259;-3811.901,-4101.856;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;504;155.9575,-3319.181;Inherit;False;5;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;COLOR;0,0,0,0;False;4;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.Vector3Node;260;-3865.421,-3964.893;Inherit;False;Global;CZY_SunDirection;CZY_SunDirection;6;1;[HideInInspector];Create;True;0;0;0;False;0;False;0,0,0;0.9855249,-0.03574456,-0.165721;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.NormalizeNode;779;-3811.772,-3776.665;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;854;-2022.233,610.462;Inherit;False;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;263;-3370.048,-3982.888;Half;False;Property;_SunFlareFalloff;Sun Flare Falloff;7;1;[HideInInspector];Create;True;0;0;0;False;0;False;1;14.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;438;657.3745,1155.66;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0.2;False;3;FLOAT;0;False;4;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;861;-1465.324,1339.86;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-1;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;712;-79.14536,-3110.654;Inherit;False;715;CirrusAlpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;52;-3821.302,-4327.05;Inherit;False;Property;_CloudHighlightColor;Cloud Highlight Color;2;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;1,1,1,0;4.158941,4.158941,4.158941,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;317;-2891.143,-2504.371;Inherit;False;RequestedCloudCover;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;777;-3938.89,-3775.418;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DotProductOpNode;780;-3667.028,-3766.739;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;511;2077.743,1014.686;Inherit;False;332;CloudColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;501;-92.01058,-3421.37;Inherit;False;347;DetailedClouds;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;151;569.0351,-2028.101;Inherit;False;Property;_ClippingThreshold;Clipping Threshold;5;1;[HideInInspector];Create;True;0;0;0;False;0;False;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;274;1830.143,-1051.032;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;645;4913.45,2071.161;Inherit;False;506;SimpleRadiance;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;649;4928.082,2154.064;Inherit;False;515;Clipping;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;265;-3169.787,-4095.749;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;319;-180.5387,-1110.532;Inherit;False;317;RequestedCloudCover;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector3Node;778;-3888.292,-3647.702;Inherit;False;Global;CZY_MoonDirection;CZY_MoonDirection;7;1;[HideInInspector];Create;True;0;0;0;False;0;False;0,0,0;0,0.2249509,-0.9743701;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleSubtractOpNode;222;338.5966,-1080.611;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.8;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;654;1283.608,-3214.672;Inherit;False;648;CSCustomLightsClipping;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;266;-3023.711,-4097.528;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;891;1905.628,-2426.822;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;706;7444.954,-1634.717;Inherit;False;CirrusLightTransport;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;513;1272.852,-2307.405;Inherit;False;512;CirrusCustomLightColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;433;141.4556,1073.34;Inherit;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;244;-3546.08,-2977.369;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;906;-876.9893,-472.1968;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;429;-78.02525,1079.905;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;338;50.5418,-2670.136;Inherit;False;332;CloudColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;522;1582.299,-2429.213;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;1,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;330;-108.4593,-2286.136;Inherit;False;329;CloudThicknessDetails;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;382;1352.606,-1081.508;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;790;-2875.409,-3779.444;Half;False;MoonlightMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;334;-3601.647,-4327.585;Inherit;False;CloudHighlightColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;889;1587.72,-2278.208;Inherit;False;847;CloudTextureFinal;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;223;501.6977,-1068.311;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;860;-1590.324,1338.86;Inherit;False;FLOAT;1;0;FLOAT;0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.BreakToComponentsNode;316;509.2098,-1211.688;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RegisterLocalVarNode;271;-2849.713,-3962.671;Inherit;False;CloudLight;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;331;554.0735,-2266.739;Inherit;False;329;CloudThicknessDetails;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;502;-124.6149,-3338.545;Inherit;False;418;BorderLightTransport;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;648;5484.254,1998.892;Inherit;False;CSCustomLightsClipping;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;55;98.54169,-2574.136;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;236;131.5417,-2286.136;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;521;2540.446,1325.394;Inherit;False;ACCustomLightsClipping;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;794;90.64841,-2195.958;Inherit;False;790;MoonlightMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;295;1562.073,-1003.693;Inherit;False;847;CloudTextureFinal;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;261;-3667.157,-4091.93;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;783;-3304.061,-3767.806;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;283;2274.191,-1078.471;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;832;-3209.211,1397.164;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;653;1283.745,-3305.013;Inherit;False;521;ACCustomLightsClipping;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;337;2.541811,-2750.136;Inherit;False;334;CloudHighlightColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;415;595.0764,-2173.415;Inherit;False;506;SimpleRadiance;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;524;2043.342,1109.57;Inherit;False;Property;_AltoCloudColor;Alto Cloud Color;3;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;1,1,1,0;3.149699,3.149699,3.149699,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;225;652.0735,-2372.739;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0.5660378,0.5660378,0.5660378,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;216;473.1443,-2493.871;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;791;-93.353,-2010.958;Inherit;False;329;CloudThicknessDetails;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;224;654.6831,-1165.677;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Compare;646;5137.885,2072.559;Inherit;False;2;4;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;892;1369.995,-2798.411;Inherit;False;Property;_CloudTextureColor;Cloud Texture Color;37;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;1,1,1,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;335;1828.58,-1135.693;Inherit;False;334;CloudHighlightColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos;256;-4188.176,-4013.731;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;214;99.5421,-2478.136;Inherit;False;267;LightMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;781;-3527.394,-3769.943;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;268;-3369.714,-3888.372;Half;False;Property;_CloudFlareFalloff;Cloud Flare Falloff;8;1;[HideInInspector];Create;True;0;0;0;False;0;False;1;10.8;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;282;1609.722,-915.0801;Inherit;False;271;CloudLight;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos;775;-4188.047,-3688.54;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;893;1768.889,-2542.618;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;351;2178.019,-2458.207;Inherit;False;FinalCloudColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;792;146.648,-2010.958;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;520;1986.173,1474.267;Inherit;False;-1;;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;352;-965.2714,-671.483;Inherit;False;351;FinalCloudColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.DotProductOpNode;834;-3033.209,1396.164;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;886;267.8423,-4175.702;Inherit;False;PuffyClouds;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;796;-3354.791,-4527.796;Inherit;False;Property;_MoonColor;Moon Color;1;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;1,1,1,0;0.1100036,0.2264151,0.2252752,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;599;-97.9974,-3188.554;Inherit;False;590;ChemtrailsFinal;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.Compare;519;2195.976,1392.761;Inherit;False;2;4;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;741;-129.5553,-3264.852;Inherit;False;739;NimbusLightTransport;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;789;-3023.582,-3772.337;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;345;-331.1171,-2578.821;Inherit;False;344;ComplexCloudDensity;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;246;-3085.762,-2790.245;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;650;5014.644,1980.411;Inherit;False;641;CirrostratLightTransport;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;329;791.4005,-1176.404;Inherit;True;CloudThicknessDetails;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;657;1874.599,-3274.224;Inherit;False;CustomRadiance;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCRemapNode;54;-93.45968,-2574.136;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;2;False;4;FLOAT;0.7;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;36;-3823.668,-4510.324;Inherit;False;Property;_CloudColor;Cloud Color;0;3;[HideInInspector];[HDR];[Header];Create;True;1;General Cloud Settings;0;0;False;0;False;0.7264151,0.7264151,0.7264151,0;0.573705,0.6316046,0.6538737,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;270;-2991.713,-3956.771;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;383;1134.607,-1011.508;Inherit;False;376;CumulusCoverage;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;257;-4118.765,-4162.82;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SaturateNode;277;1640.36,-1082.697;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;797;-3135.136,-4528.331;Inherit;False;MoonlightColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;332;-3599.176,-4511.369;Inherit;False;CloudColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;647;5324.791,2006.867;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;267;-2875.538,-4104.635;Half;False;LightMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;910;-678.2959,-671.1561;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;False;False;True;1;LightMode=DepthOnly;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;909;-678.2959,-671.1561;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=ShadowCaster;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;911;-678.2959,-671.1561;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;908;-678.2959,-671.1561;Float;False;True;-1;2;;0;12;Distant Lands/Cozy/Stylized Clouds Painted;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;8;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;1;False;;False;False;False;False;False;False;False;False;True;True;True;221;False;;255;False;;255;False;;7;False;;2;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=1;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;1;5;False;;10;False;;1;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;True;2;False;;True;3;False;;True;True;0;False;;0;False;;True;1;LightMode=UniversalForward;False;False;0;Hidden/InternalErrorShader;0;0;Standard;22;Surface;1;637952268182895573;  Blend;0;0;Two Sided;2;637952268204981941;Cast Shadows;1;0;  Use Shadow Threshold;0;0;Receive Shadows;1;0;GPU Instancing;1;0;LOD CrossFade;0;0;Built-in Fog;0;0;DOTS Instancing;0;0;Meta Pass;0;0;Extra Pre Pass;0;0;Tessellation;0;0;  Phong;0;0;  Strength;0.5,False,;0;  Type;0;0;  Tess;16,False,;0;  Min;10,False,;0;  Max;25,False,;0;  Edge Length;16,False,;0;  Max Displacement;25,False,;0;Vertex Position,InvertActionOnDeselection;1;0;0;5;False;True;True;True;False;False;;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;907;-678.2959,-671.1561;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;1;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;0;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
WireConnection;150;1;70;0
WireConnection;61;0;150;0
WireConnection;152;0;61;0
WireConnection;159;0;94;0
WireConnection;68;0;156;0
WireConnection;68;1;67;0
WireConnection;235;1;41;0
WireConnection;65;1;41;0
WireConnection;66;0;161;0
WireConnection;66;1;68;0
WireConnection;81;0;82;0
WireConnection;81;1;158;0
WireConnection;92;0;155;0
WireConnection;92;1;91;0
WireConnection;148;0;66;0
WireConnection;148;2;65;0
WireConnection;83;0;81;0
WireConnection;83;1;160;0
WireConnection;234;0;66;0
WireConnection;234;2;235;0
WireConnection;74;1;71;0
WireConnection;80;0;83;0
WireConnection;80;1;74;0
WireConnection;312;0;148;0
WireConnection;312;1;234;0
WireConnection;60;0;157;0
WireConnection;60;1;62;0
WireConnection;93;0;162;0
WireConnection;93;1;92;0
WireConnection;44;1;43;0
WireConnection;248;0;80;0
WireConnection;248;3;72;0
WireConnection;248;4;57;0
WireConnection;77;0;93;0
WireConnection;77;2;44;0
WireConnection;313;0;312;0
WireConnection;42;1;41;0
WireConnection;63;0;161;0
WireConnection;63;1;60;0
WireConnection;746;0;747;0
WireConnection;746;1;748;0
WireConnection;768;0;759;0
WireConnection;240;0;248;0
WireConnection;726;0;721;0
WireConnection;37;0;63;0
WireConnection;37;1;42;0
WireConnection;376;0;375;0
WireConnection;78;0;77;0
WireConnection;314;0;37;0
WireConnection;773;0;744;0
WireConnection;327;0;326;0
WireConnection;390;0;387;0
WireConnection;762;0;768;0
WireConnection;745;0;746;0
WireConnection;394;0;391;0
WireConnection;51;0;78;0
WireConnection;874;0;808;0
WireConnection;728;0;726;0
WireConnection;728;1;726;0
WireConnection;386;0;390;0
WireConnection;392;1;394;0
WireConnection;328;0;327;0
WireConnection;814;0;874;0
WireConnection;743;0;745;0
WireConnection;743;1;773;0
WireConnection;761;0;760;0
WireConnection;380;0;241;0
WireConnection;380;1;381;0
WireConnection;114;0;113;0
WireConnection;816;0;874;0
WireConnection;763;1;762;0
WireConnection;47;0;51;0
WireConnection;47;2;46;0
WireConnection;770;0;769;0
WireConnection;770;2;728;0
WireConnection;140;0;114;0
WireConnection;140;1;114;0
WireConnection;58;0;380;0
WireConnection;666;0;665;0
WireConnection;818;0;812;0
WireConnection;818;2;816;0
WireConnection;294;0;47;0
WireConnection;389;0;386;0
WireConnection;389;1;392;0
WireConnection;69;0;321;0
WireConnection;69;1;328;0
WireConnection;393;0;392;0
WireConnection;771;0;743;0
WireConnection;771;1;770;0
WireConnection;765;0;761;0
WireConnection;765;1;763;0
WireConnection;855;0;812;0
WireConnection;855;1;814;0
WireConnection;766;0;763;0
WireConnection;758;0;771;0
WireConnection;758;3;765;0
WireConnection;758;4;766;0
WireConnection;667;0;666;0
WireConnection;458;0;457;0
WireConnection;458;1;602;0
WireConnection;128;0;140;0
WireConnection;128;3;389;0
WireConnection;128;4;393;0
WireConnection;823;0;855;0
WireConnection;50;0;69;0
WireConnection;50;1;58;0
WireConnection;669;0;666;0
WireConnection;824;0;818;0
WireConnection;674;0;671;0
WireConnection;674;2;673;0
WireConnection;344;0;50;0
WireConnection;675;0;672;0
WireConnection;675;2;670;0
WireConnection;676;0;668;0
WireConnection;676;2;667;0
WireConnection;421;0;128;0
WireConnection;677;0;668;0
WireConnection;677;2;669;0
WireConnection;734;0;758;0
WireConnection;827;1;823;0
WireConnection;826;1;824;0
WireConnection;401;0;398;0
WireConnection;401;1;402;0
WireConnection;733;0;730;0
WireConnection;733;1;731;0
WireConnection;454;0;455;0
WireConnection;454;2;458;0
WireConnection;875;0;827;0
WireConnection;875;1;826;0
WireConnection;399;0;401;0
WireConnection;399;1;421;0
WireConnection;607;0;606;0
WireConnection;679;0;677;0
WireConnection;682;0;675;0
WireConnection;560;0;571;0
WireConnection;680;0;676;0
WireConnection;681;0;674;0
WireConnection;601;0;454;0
WireConnection;735;0;733;0
WireConnection;735;1;734;0
WireConnection;684;1;679;0
WireConnection;686;1;680;0
WireConnection;600;0;601;0
WireConnection;48;0;346;0
WireConnection;48;1;47;0
WireConnection;417;0;399;0
WireConnection;417;2;406;0
WireConnection;687;0;681;0
WireConnection;683;0;682;0
WireConnection;559;0;560;0
WireConnection;737;0;735;0
WireConnection;494;1;602;0
WireConnection;839;0;875;0
WireConnection;838;0;836;0
WireConnection;617;0;607;0
WireConnection;608;0;607;0
WireConnection;621;0;613;0
WireConnection;621;2;617;0
WireConnection;565;0;564;0
WireConnection;565;2;566;0
WireConnection;420;0;417;0
WireConnection;691;0;687;0
WireConnection;691;1;684;0
WireConnection;690;0;685;0
WireConnection;614;0;609;0
WireConnection;614;2;611;0
WireConnection;688;0;686;0
WireConnection;688;1;683;0
WireConnection;553;0;554;0
WireConnection;553;2;559;0
WireConnection;615;0;613;0
WireConnection;615;2;608;0
WireConnection;452;0;451;0
WireConnection;452;1;600;0
WireConnection;49;0;48;0
WireConnection;569;0;567;0
WireConnection;569;2;568;0
WireConnection;477;0;478;0
WireConnection;477;1;476;0
WireConnection;616;0;610;0
WireConnection;616;2;612;0
WireConnection;492;1;494;0
WireConnection;562;0;560;0
WireConnection;750;0;737;0
WireConnection;841;0;838;0
WireConnection;693;0;691;0
WireConnection;693;1;688;0
WireConnection;418;0;420;0
WireConnection;694;0;690;0
WireConnection;694;1;690;0
WireConnection;480;1;479;0
WireConnection;561;0;554;0
WireConnection;561;2;562;0
WireConnection;643;0;621;0
WireConnection;347;0;49;0
WireConnection;491;0;452;0
WireConnection;491;1;492;0
WireConnection;618;0;616;0
WireConnection;563;0;565;0
WireConnection;620;0;614;0
WireConnection;555;0;553;0
WireConnection;570;0;569;0
WireConnection;474;0;475;0
WireConnection;474;2;477;0
WireConnection;739;0;750;0
WireConnection;619;0;615;0
WireConnection;843;0;842;0
WireConnection;843;1;841;0
WireConnection;626;0;618;0
WireConnection;463;0;474;0
WireConnection;463;2;480;0
WireConnection;624;0;620;0
WireConnection;697;0;694;0
WireConnection;593;0;563;0
WireConnection;551;1;555;0
WireConnection;622;1;643;0
WireConnection;894;0;843;0
WireConnection;696;0;693;0
WireConnection;556;1;561;0
WireConnection;623;1;619;0
WireConnection;442;0;491;0
WireConnection;592;0;570;0
WireConnection;700;0;697;0
WireConnection;627;0;624;0
WireConnection;627;1;623;0
WireConnection;847;0;894;0
WireConnection;461;0;442;0
WireConnection;587;0;593;0
WireConnection;587;1;551;0
WireConnection;628;0;622;0
WireConnection;628;1;626;0
WireConnection;588;0;556;0
WireConnection;588;1;592;0
WireConnection;581;0;583;0
WireConnection;629;0;625;0
WireConnection;473;0;463;0
WireConnection;862;0;409;0
WireConnection;862;1;414;0
WireConnection;862;2;740;0
WireConnection;695;0;689;0
WireConnection;701;0;698;0
WireConnection;701;1;700;0
WireConnection;631;0;627;0
WireConnection;631;1;628;0
WireConnection;863;0;862;0
WireConnection;471;1;463;0
WireConnection;471;2;473;0
WireConnection;630;0;629;0
WireConnection;630;1;629;0
WireConnection;584;0;581;0
WireConnection;584;1;581;0
WireConnection;573;0;587;0
WireConnection;573;1;588;0
WireConnection;714;0;701;0
WireConnection;714;1;695;0
WireConnection;633;0;630;0
WireConnection;582;0;584;0
WireConnection;490;0;484;0
WireConnection;490;1;471;0
WireConnection;490;2;499;0
WireConnection;576;0;573;0
WireConnection;884;0;863;0
WireConnection;884;1;885;0
WireConnection;866;0;865;0
WireConnection;634;0;631;0
WireConnection;883;0;884;0
WireConnection;658;0;632;0
WireConnection;636;0;633;0
WireConnection;867;0;866;0
WireConnection;899;0;714;0
WireConnection;578;0;577;0
WireConnection;594;0;582;0
WireConnection;800;0;490;0
WireConnection;639;0;637;0
WireConnection;639;1;636;0
WireConnection;639;2;658;0
WireConnection;900;0;899;0
WireConnection;869;0;867;0
WireConnection;869;1;870;0
WireConnection;869;2;883;0
WireConnection;801;0;800;0
WireConnection;586;0;591;0
WireConnection;586;1;594;0
WireConnection;586;2;578;0
WireConnection;447;0;801;0
WireConnection;641;0;639;0
WireConnection;590;0;586;0
WireConnection;715;0;900;0
WireConnection;868;0;863;0
WireConnection;868;1;869;0
WireConnection;871;0;868;0
WireConnection;410;0;871;0
WireConnection;410;1;497;0
WireConnection;410;2;598;0
WireConnection;410;3;644;0
WireConnection;410;4;713;0
WireConnection;411;0;410;0
WireConnection;408;0;411;0
WireConnection;902;0;901;0
WireConnection;904;0;902;0
WireConnection;904;2;903;0
WireConnection;905;0;902;0
WireConnection;905;1;904;0
WireConnection;512;0;510;0
WireConnection;205;0;214;0
WireConnection;205;1;340;0
WireConnection;205;2;236;0
WireConnection;349;0;415;0
WireConnection;149;0;148;0
WireConnection;656;0;655;0
WireConnection;795;0;794;0
WireConnection;795;1;793;0
WireConnection;795;2;792;0
WireConnection;655;0;653;0
WireConnection;655;1;654;0
WireConnection;840;0;837;0
WireConnection;227;0;216;0
WireConnection;227;1;225;0
WireConnection;227;2;331;0
WireConnection;837;0;834;0
WireConnection;53;0;337;0
WireConnection;53;1;338;0
WireConnection;53;2;55;0
WireConnection;273;0;283;0
WireConnection;247;0;245;0
WireConnection;435;0;433;0
WireConnection;435;1;433;0
WireConnection;258;0;257;0
WireConnection;258;1;256;0
WireConnection;280;0;171;0
WireConnection;280;1;281;0
WireConnection;898;0;892;0
WireConnection;898;1;897;0
WireConnection;171;0;336;0
WireConnection;171;1;227;0
WireConnection;171;2;349;0
WireConnection;516;0;509;0
WireConnection;516;1;518;0
WireConnection;279;0;382;0
WireConnection;510;0;511;0
WireConnection;510;1;524;0
WireConnection;505;0;504;0
WireConnection;272;1;335;0
WireConnection;272;2;274;0
WireConnection;506;0;505;0
WireConnection;378;0;319;0
WireConnection;378;1;379;0
WireConnection;245;0;244;0
WireConnection;245;1;244;0
WireConnection;515;0;151;0
WireConnection;786;0;783;0
WireConnection;786;1;784;0
WireConnection;269;0;264;0
WireConnection;269;1;268;0
WireConnection;318;0;72;0
WireConnection;318;1;57;0
WireConnection;264;0;262;0
WireConnection;320;0;378;0
WireConnection;262;0;261;0
WireConnection;259;0;258;0
WireConnection;504;0;501;0
WireConnection;504;1;502;0
WireConnection;504;2;741;0
WireConnection;504;3;599;0
WireConnection;504;4;712;0
WireConnection;779;0;777;0
WireConnection;854;0;827;0
WireConnection;854;1;826;0
WireConnection;438;0;435;0
WireConnection;861;0;860;0
WireConnection;317;0;318;0
WireConnection;777;0;776;0
WireConnection;777;1;775;0
WireConnection;780;0;779;0
WireConnection;780;1;778;0
WireConnection;274;0;277;0
WireConnection;274;1;295;0
WireConnection;274;2;282;0
WireConnection;265;0;264;0
WireConnection;265;1;263;0
WireConnection;222;0;320;1
WireConnection;266;0;265;0
WireConnection;891;0;522;0
WireConnection;891;1;893;0
WireConnection;891;2;889;0
WireConnection;706;0;701;0
WireConnection;433;0;429;0
WireConnection;244;0;243;0
WireConnection;906;0;905;0
WireConnection;522;0;280;0
WireConnection;522;1;513;0
WireConnection;522;2;523;0
WireConnection;382;0;278;0
WireConnection;382;1;383;0
WireConnection;790;0;789;0
WireConnection;334;0;52;0
WireConnection;223;0;222;0
WireConnection;860;0;857;0
WireConnection;316;0;315;0
WireConnection;271;0;270;0
WireConnection;648;0;647;0
WireConnection;55;0;54;0
WireConnection;236;0;330;0
WireConnection;521;0;509;0
WireConnection;261;0;259;0
WireConnection;261;1;260;0
WireConnection;783;0;781;0
WireConnection;283;0;272;0
WireConnection;283;1;284;0
WireConnection;832;0;828;0
WireConnection;225;0;339;0
WireConnection;216;0;53;0
WireConnection;216;1;205;0
WireConnection;216;2;795;0
WireConnection;224;0;316;0
WireConnection;224;1;223;0
WireConnection;646;0;645;0
WireConnection;646;1;649;0
WireConnection;781;0;780;0
WireConnection;893;0;898;0
WireConnection;893;1;522;0
WireConnection;351;0;891;0
WireConnection;792;0;791;0
WireConnection;834;0;832;0
WireConnection;834;1;832;0
WireConnection;886;0;863;0
WireConnection;519;0;518;0
WireConnection;519;1;520;0
WireConnection;789;0;786;0
WireConnection;246;0;72;0
WireConnection;246;1;57;0
WireConnection;246;2;247;0
WireConnection;329;0;224;0
WireConnection;657;0;656;0
WireConnection;54;0;345;0
WireConnection;270;0;269;0
WireConnection;277;0;382;0
WireConnection;797;0;796;0
WireConnection;332;0;36;0
WireConnection;647;0;650;0
WireConnection;647;1;646;0
WireConnection;267;0;266;0
WireConnection;908;2;352;0
WireConnection;908;3;906;0
ASEEND*/
//CHKSM=3688B3DDAF8ABEE0985B12BC1D81606DB790D587