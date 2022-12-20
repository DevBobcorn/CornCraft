// Made with Amplify Shader Editor v1.9.0.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Distant Lands/Cozy/Stylized Clouds Ghibli Mobile"
{
	Properties
	{
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector][HDR][Header(General Cloud Settings)]_CloudColor("Cloud Color", Color) = (0.7264151,0.7264151,0.7264151,0)
		[HideInInspector][HDR]_CloudHighlightColor("Cloud Highlight Color", Color) = (1,1,1,0)
		[HideInInspector]_WindSpeed("Wind Speed", Float) = 0
		[HideInInspector][Header(Cumulus Clouds)]_CumulusCoverageMultiplier("Cumulus Coverage Multiplier", Range( 0 , 2)) = 1
		[HideInInspector]_CloudCohesion("Cloud Cohesion", Range( 0 , 1)) = 0.887
		[HideInInspector]_MainCloudScale("Main Cloud Scale", Float) = 0.8
		[HideInInspector]_Spherize("Spherize", Range( 0 , 1)) = 0.36
		[HideInInspector]_ShadowingDistance("Shadowing Distance", Range( 0 , 0.1)) = 0.07
		[HideInInspector][HDR]_CloudTextureColor("Cloud Texture Color", Color) = (0.6320754,0.6320754,0.6320754,0)
		[HideInInspector]_ClippingThreshold("Clipping Threshold", Range( 0 , 1)) = 0
		[HideInInspector]_MaxCloudCover("Max Cloud Cover", Float) = 0


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

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Transparent" }

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
			
			Blend One Zero, One Zero
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA

			

			HLSLPROGRAM

			#pragma multi_compile_instancing
			#define ASE_SRP_VERSION 110000


			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			
			#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl"


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
			float4 _CloudHighlightColor;
			float4 _CloudColor;
			float4 _CloudTextureColor;
			float _Spherize;
			float _WindSpeed;
			float _CloudCohesion;
			float _CumulusCoverageMultiplier;
			float _MaxCloudCover;
			float _MainCloudScale;
			float _ShadowingDistance;
			float _ClippingThreshold;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			

			
					float2 voronoihash35_g45( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi35_g45( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
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
						 		float2 o = voronoihash35_g45( n + g );
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
			
					float2 voronoihash13_g45( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi13_g45( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
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
						 		float2 o = voronoihash13_g45( n + g );
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
			
					float2 voronoihash11_g45( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi11_g45( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
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
						 		float2 o = voronoihash11_g45( n + g );
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
			
					float2 voronoihash35_g46( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi35_g46( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
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
						 		float2 o = voronoihash35_g46( n + g );
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
			
					float2 voronoihash13_g46( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi13_g46( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
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
						 		float2 o = voronoihash13_g46( n + g );
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
			
					float2 voronoihash11_g46( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi11_g46( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
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
						 		float2 o = voronoihash11_g46( n + g );
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
			
			float4 SampleGradient( Gradient gradient, float time )
			{
				float3 color = gradient.colors[0].rgb;
				UNITY_UNROLL
				for (int c = 1; c < 8; c++)
				{
				float colorPos = saturate((time - gradient.colors[c-1].w) / ( 0.00001 + (gradient.colors[c].w - gradient.colors[c-1].w)) * step(c, gradient.colorsLength-1));
				color = lerp(color, gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), gradient.type));
				}
				#ifndef UNITY_COLORSPACE_GAMMA
				color = SRGBToLinear(color);
				#endif
				float alpha = gradient.alphas[0].x;
				UNITY_UNROLL
				for (int a = 1; a < 8; a++)
				{
				float alphaPos = saturate((time - gradient.alphas[a-1].y) / ( 0.00001 + (gradient.alphas[a].y - gradient.alphas[a-1].y)) * step(a, gradient.alphasLength-1));
				alpha = lerp(alpha, gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), gradient.type));
				}
				return float4(color, alpha);
			}
			
					float2 voronoihash35_g44( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi35_g44( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
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
						 		float2 o = voronoihash35_g44( n + g );
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
			
					float2 voronoihash13_g44( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi13_g44( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
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
						 		float2 o = voronoihash13_g44( n + g );
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
			
					float2 voronoihash11_g44( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi11_g44( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
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
						 		float2 o = voronoihash11_g44( n + g );
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

				float4 CloudHighlightColor334 = _CloudHighlightColor;
				float4 CloudColor332 = _CloudColor;
				Gradient gradient1145 = NewGradient( 0, 2, 2, float4( 0, 0, 0, 0.8676432 ), float4( 1, 1, 1, 0.9294118 ), 0, 0, 0, 0, 0, 0, float2( 1, 0 ), float2( 1, 1 ), 0, 0, 0, 0, 0, 0 );
				float2 texCoord1042 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_1043_0 = ( texCoord1042 - float2( 0.5,0.5 ) );
				float dotResult1045 = dot( temp_output_1043_0 , temp_output_1043_0 );
				float Dot1071 = saturate( (0.85 + (dotResult1045 - 0.0) * (3.0 - 0.85) / (1.0 - 0.0)) );
				float time35_g45 = 0.0;
				float2 voronoiSmoothId35_g45 = 0;
				float2 texCoord955 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 CentralUV998 = ( texCoord955 + float2( -0.5,-0.5 ) );
				float2 temp_output_21_0_g45 = (CentralUV998*1.58 + 0.0);
				float2 break2_g45 = abs( temp_output_21_0_g45 );
				float saferPower4_g45 = abs( break2_g45.x );
				float saferPower3_g45 = abs( break2_g45.y );
				float saferPower6_g45 = abs( ( pow( saferPower4_g45 , 2.0 ) + pow( saferPower3_g45 , 2.0 ) ) );
				float Spherize1078 = _Spherize;
				float Flatness1076 = ( 20.0 * _Spherize );
				float mulTime61 = _TimeParameters.x * ( 0.001 * _WindSpeed );
				float Time152 = mulTime61;
				float2 Wind1035 = ( Time152 * float2( 0.1,0.2 ) );
				float2 temp_output_10_0_g45 = (( ( temp_output_21_0_g45 * ( pow( saferPower6_g45 , Spherize1078 ) * Flatness1076 ) ) + float2( 0.5,0.5 ) )*( 2.0 / 5.0 ) + Wind1035);
				float2 coords35_g45 = temp_output_10_0_g45 * 60.0;
				float2 id35_g45 = 0;
				float2 uv35_g45 = 0;
				float fade35_g45 = 0.5;
				float voroi35_g45 = 0;
				float rest35_g45 = 0;
				for( int it35_g45 = 0; it35_g45 <2; it35_g45++ ){
				voroi35_g45 += fade35_g45 * voronoi35_g45( coords35_g45, time35_g45, id35_g45, uv35_g45, 0,voronoiSmoothId35_g45 );
				rest35_g45 += fade35_g45;
				coords35_g45 *= 2;
				fade35_g45 *= 0.5;
				}//Voronoi35_g45
				voroi35_g45 /= rest35_g45;
				float time13_g45 = 0.0;
				float2 voronoiSmoothId13_g45 = 0;
				float2 coords13_g45 = temp_output_10_0_g45 * 25.0;
				float2 id13_g45 = 0;
				float2 uv13_g45 = 0;
				float fade13_g45 = 0.5;
				float voroi13_g45 = 0;
				float rest13_g45 = 0;
				for( int it13_g45 = 0; it13_g45 <2; it13_g45++ ){
				voroi13_g45 += fade13_g45 * voronoi13_g45( coords13_g45, time13_g45, id13_g45, uv13_g45, 0,voronoiSmoothId13_g45 );
				rest13_g45 += fade13_g45;
				coords13_g45 *= 2;
				fade13_g45 *= 0.5;
				}//Voronoi13_g45
				voroi13_g45 /= rest13_g45;
				float time11_g45 = 17.23;
				float2 voronoiSmoothId11_g45 = 0;
				float2 coords11_g45 = temp_output_10_0_g45 * 9.0;
				float2 id11_g45 = 0;
				float2 uv11_g45 = 0;
				float fade11_g45 = 0.5;
				float voroi11_g45 = 0;
				float rest11_g45 = 0;
				for( int it11_g45 = 0; it11_g45 <2; it11_g45++ ){
				voroi11_g45 += fade11_g45 * voronoi11_g45( coords11_g45, time11_g45, id11_g45, uv11_g45, 0,voronoiSmoothId11_g45 );
				rest11_g45 += fade11_g45;
				coords11_g45 *= 2;
				fade11_g45 *= 0.5;
				}//Voronoi11_g45
				voroi11_g45 /= rest11_g45;
				float2 texCoord1055 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_1056_0 = ( texCoord1055 - float2( 0.5,0.5 ) );
				float dotResult1057 = dot( temp_output_1056_0 , temp_output_1056_0 );
				float ModifiedCohesion1074 = ( _CloudCohesion * 1.0 * ( 1.0 - dotResult1057 ) );
				float lerpResult15_g45 = lerp( saturate( ( voroi35_g45 + voroi13_g45 ) ) , voroi11_g45 , ModifiedCohesion1074);
				float CumulusCoverage376 = ( _CumulusCoverageMultiplier * _MaxCloudCover );
				float lerpResult16_g45 = lerp( lerpResult15_g45 , 1.0 , ( ( 1.0 - CumulusCoverage376 ) + -0.7 ));
				float time35_g46 = 0.0;
				float2 voronoiSmoothId35_g46 = 0;
				float2 temp_output_21_0_g46 = CentralUV998;
				float2 break2_g46 = abs( temp_output_21_0_g46 );
				float saferPower4_g46 = abs( break2_g46.x );
				float saferPower3_g46 = abs( break2_g46.y );
				float saferPower6_g46 = abs( ( pow( saferPower4_g46 , 2.0 ) + pow( saferPower3_g46 , 2.0 ) ) );
				float Scale1080 = ( _MainCloudScale * 0.1 );
				float2 temp_output_10_0_g46 = (( ( temp_output_21_0_g46 * ( pow( saferPower6_g46 , Spherize1078 ) * Flatness1076 ) ) + float2( 0.5,0.5 ) )*( 2.0 / ( Scale1080 * 1.5 ) ) + ( Wind1035 * float2( 0.5,0.5 ) ));
				float2 coords35_g46 = temp_output_10_0_g46 * 60.0;
				float2 id35_g46 = 0;
				float2 uv35_g46 = 0;
				float fade35_g46 = 0.5;
				float voroi35_g46 = 0;
				float rest35_g46 = 0;
				for( int it35_g46 = 0; it35_g46 <2; it35_g46++ ){
				voroi35_g46 += fade35_g46 * voronoi35_g46( coords35_g46, time35_g46, id35_g46, uv35_g46, 0,voronoiSmoothId35_g46 );
				rest35_g46 += fade35_g46;
				coords35_g46 *= 2;
				fade35_g46 *= 0.5;
				}//Voronoi35_g46
				voroi35_g46 /= rest35_g46;
				float time13_g46 = 0.0;
				float2 voronoiSmoothId13_g46 = 0;
				float2 coords13_g46 = temp_output_10_0_g46 * 25.0;
				float2 id13_g46 = 0;
				float2 uv13_g46 = 0;
				float fade13_g46 = 0.5;
				float voroi13_g46 = 0;
				float rest13_g46 = 0;
				for( int it13_g46 = 0; it13_g46 <2; it13_g46++ ){
				voroi13_g46 += fade13_g46 * voronoi13_g46( coords13_g46, time13_g46, id13_g46, uv13_g46, 0,voronoiSmoothId13_g46 );
				rest13_g46 += fade13_g46;
				coords13_g46 *= 2;
				fade13_g46 *= 0.5;
				}//Voronoi13_g46
				voroi13_g46 /= rest13_g46;
				float time11_g46 = 17.23;
				float2 voronoiSmoothId11_g46 = 0;
				float2 coords11_g46 = temp_output_10_0_g46 * 9.0;
				float2 id11_g46 = 0;
				float2 uv11_g46 = 0;
				float fade11_g46 = 0.5;
				float voroi11_g46 = 0;
				float rest11_g46 = 0;
				for( int it11_g46 = 0; it11_g46 <2; it11_g46++ ){
				voroi11_g46 += fade11_g46 * voronoi11_g46( coords11_g46, time11_g46, id11_g46, uv11_g46, 0,voronoiSmoothId11_g46 );
				rest11_g46 += fade11_g46;
				coords11_g46 *= 2;
				fade11_g46 *= 0.5;
				}//Voronoi11_g46
				voroi11_g46 /= rest11_g46;
				float lerpResult15_g46 = lerp( saturate( ( voroi35_g46 + voroi13_g46 ) ) , voroi11_g46 , ( ModifiedCohesion1074 * 1.1 ));
				float lerpResult16_g46 = lerp( lerpResult15_g46 , 1.0 , ( ( 1.0 - CumulusCoverage376 ) + -0.7 ));
				float temp_output_1183_0 = saturate( (0.0 + (( Dot1071 * ( 1.0 - lerpResult16_g46 ) ) - 0.6) * (1.0 - 0.0) / (1.0 - 0.6)) );
				float IT2PreAlpha1184 = temp_output_1183_0;
				float temp_output_1143_0 = (0.0 + (( Dot1071 * ( 1.0 - lerpResult16_g45 ) ) - 0.6) * (IT2PreAlpha1184 - 0.0) / (1.5 - 0.6));
				float clampResult1158 = clamp( temp_output_1143_0 , 0.0 , 0.9 );
				float AdditionalLayer1147 = SampleGradient( gradient1145, clampResult1158 ).r;
				float4 lerpResult1150 = lerp( CloudColor332 , ( CloudColor332 * _CloudTextureColor ) , AdditionalLayer1147);
				float4 ModifiedCloudColor1165 = lerpResult1150;
				Gradient gradient1198 = NewGradient( 0, 2, 2, float4( 0.06119964, 0.06119964, 0.06119964, 0.4411841 ), float4( 1, 1, 1, 0.5794156 ), 0, 0, 0, 0, 0, 0, float2( 1, 0 ), float2( 1, 1 ), 0, 0, 0, 0, 0, 0 );
				float time35_g44 = 0.0;
				float2 voronoiSmoothId35_g44 = 0;
				float2 ShadowUV997 = ( CentralUV998 + ( CentralUV998 * float2( -1,-1 ) * _ShadowingDistance * Dot1071 ) );
				float2 temp_output_21_0_g44 = ShadowUV997;
				float2 break2_g44 = abs( temp_output_21_0_g44 );
				float saferPower4_g44 = abs( break2_g44.x );
				float saferPower3_g44 = abs( break2_g44.y );
				float saferPower6_g44 = abs( ( pow( saferPower4_g44 , 2.0 ) + pow( saferPower3_g44 , 2.0 ) ) );
				float2 temp_output_10_0_g44 = (( ( temp_output_21_0_g44 * ( pow( saferPower6_g44 , Spherize1078 ) * Flatness1076 ) ) + float2( 0.5,0.5 ) )*( 2.0 / ( Scale1080 * 1.5 ) ) + ( Wind1035 * float2( 0.5,0.5 ) ));
				float2 coords35_g44 = temp_output_10_0_g44 * 60.0;
				float2 id35_g44 = 0;
				float2 uv35_g44 = 0;
				float fade35_g44 = 0.5;
				float voroi35_g44 = 0;
				float rest35_g44 = 0;
				for( int it35_g44 = 0; it35_g44 <2; it35_g44++ ){
				voroi35_g44 += fade35_g44 * voronoi35_g44( coords35_g44, time35_g44, id35_g44, uv35_g44, 0,voronoiSmoothId35_g44 );
				rest35_g44 += fade35_g44;
				coords35_g44 *= 2;
				fade35_g44 *= 0.5;
				}//Voronoi35_g44
				voroi35_g44 /= rest35_g44;
				float time13_g44 = 0.0;
				float2 voronoiSmoothId13_g44 = 0;
				float2 coords13_g44 = temp_output_10_0_g44 * 25.0;
				float2 id13_g44 = 0;
				float2 uv13_g44 = 0;
				float fade13_g44 = 0.5;
				float voroi13_g44 = 0;
				float rest13_g44 = 0;
				for( int it13_g44 = 0; it13_g44 <2; it13_g44++ ){
				voroi13_g44 += fade13_g44 * voronoi13_g44( coords13_g44, time13_g44, id13_g44, uv13_g44, 0,voronoiSmoothId13_g44 );
				rest13_g44 += fade13_g44;
				coords13_g44 *= 2;
				fade13_g44 *= 0.5;
				}//Voronoi13_g44
				voroi13_g44 /= rest13_g44;
				float time11_g44 = 17.23;
				float2 voronoiSmoothId11_g44 = 0;
				float2 coords11_g44 = temp_output_10_0_g44 * 9.0;
				float2 id11_g44 = 0;
				float2 uv11_g44 = 0;
				float fade11_g44 = 0.5;
				float voroi11_g44 = 0;
				float rest11_g44 = 0;
				for( int it11_g44 = 0; it11_g44 <2; it11_g44++ ){
				voroi11_g44 += fade11_g44 * voronoi11_g44( coords11_g44, time11_g44, id11_g44, uv11_g44, 0,voronoiSmoothId11_g44 );
				rest11_g44 += fade11_g44;
				coords11_g44 *= 2;
				fade11_g44 *= 0.5;
				}//Voronoi11_g44
				voroi11_g44 /= rest11_g44;
				float lerpResult15_g44 = lerp( saturate( ( voroi35_g44 + voroi13_g44 ) ) , voroi11_g44 , ( ModifiedCohesion1074 * 1.1 ));
				float lerpResult16_g44 = lerp( lerpResult15_g44 , 1.0 , ( ( 1.0 - CumulusCoverage376 ) + -0.7 ));
				float4 lerpResult1206 = lerp( CloudHighlightColor334 , ModifiedCloudColor1165 , saturate( SampleGradient( gradient1198, saturate( (0.0 + (( Dot1071 * ( 1.0 - lerpResult16_g44 ) ) - 0.6) * (1.0 - 0.0) / (1.0 - 0.6)) ) ).r ));
				float4 IT2Color1207 = lerpResult1206;
				Gradient gradient1199 = NewGradient( 0, 2, 2, float4( 0.06119964, 0.06119964, 0.06119964, 0.4617685 ), float4( 1, 1, 1, 0.5117723 ), 0, 0, 0, 0, 0, 0, float2( 1, 0 ), float2( 1, 1 ), 0, 0, 0, 0, 0, 0 );
				float IT2Alpha1202 = SampleGradient( gradient1199, temp_output_1183_0 ).r;
				clip( IT2Alpha1202 - _ClippingThreshold);
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = IT2Color1207.rgb;
				float Alpha = 1;
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

			

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				
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
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _CloudHighlightColor;
			float4 _CloudColor;
			float4 _CloudTextureColor;
			float _Spherize;
			float _WindSpeed;
			float _CloudCohesion;
			float _CumulusCoverageMultiplier;
			float _MaxCloudCover;
			float _MainCloudScale;
			float _ShadowingDistance;
			float _ClippingThreshold;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			

			
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

				

				float Alpha = 1;
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
			#define ASE_SRP_VERSION 110000

			
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				
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
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _CloudHighlightColor;
			float4 _CloudColor;
			float4 _CloudTextureColor;
			float _Spherize;
			float _WindSpeed;
			float _CloudCohesion;
			float _CumulusCoverageMultiplier;
			float _MaxCloudCover;
			float _MainCloudScale;
			float _ShadowingDistance;
			float _ClippingThreshold;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			

			
			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				

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

				

				float Alpha = 1;
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
6.285715;1085.143;2181.714;596.1429;1724.37;629.9566;1;True;False
Node;AmplifyShaderEditor.CommentaryNode;1168;-1275.921,-2528.838;Inherit;False;2636.823;1492.163;;2;1170;1169;Iteration 2;1,0.8737146,0.572549,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;1169;-1211.921,-2464.838;Inherit;False;2070.976;624.3994;Alpha;20;1202;1201;1199;1184;1183;1182;1181;1179;1180;1178;1212;1211;1210;1173;1176;1175;1172;1177;1171;1174;;1,0.8737146,0.572549,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;1167;1522.421,-2232.243;Inherit;False;2326.557;1124.512;;25;1150;1165;1154;1152;1127;1151;1144;1147;1146;1158;1145;1143;1142;1141;1140;1139;1133;1134;1138;1149;1135;1137;1136;1132;1209;Additional Layer;0.7721605,0.4669811,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;372;-4450.156,-2723.177;Inherit;False;2555.466;1283.535;;47;997;1095;985;986;1094;1071;1052;1078;998;1051;1083;1076;1161;1080;1035;1074;947;1045;956;931;1043;925;1036;955;1058;1042;1040;906;1059;1041;1057;1056;1055;1222;376;375;334;52;332;36;152;61;150;70;1225;1226;1229;Variable Declaration;0.6196079,0.9508546,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;1170;-1195.921,-1808.838;Inherit;False;2506.716;730.6439;Color;23;1207;1206;1203;1205;1204;1200;1198;1197;1196;1195;1193;1194;1192;1214;1213;1188;1185;1186;1215;1191;1190;1189;1187;;1,0.8737146,0.572549,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;925;-4408,-1814;Inherit;False;Property;_MainCloudScale;Main Cloud Scale;5;1;[HideInInspector];Create;True;0;0;0;False;0;False;0.8;0.6;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1040;-2688,-2400;Inherit;False;152;Time;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;1057;-3056,-1696;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;152;-3065.96,-2349.724;Inherit;False;Time;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;906;-3104,-1824;Inherit;False;Property;_CloudCohesion;Cloud Cohesion;4;1;[HideInInspector];Create;True;0;0;0;False;0;False;0.887;0.837;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;150;-3367.351,-2349.156;Inherit;False;2;2;0;FLOAT;0.001;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;1059;-2928,-1696;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;1042;-4304,-2064;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;1055;-3408,-1696;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;1171;-1179.921,-2416.838;Inherit;False;1035;Wind;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;1041;-2720,-2288;Inherit;False;Constant;_MainCloudWindDir;Main Cloud Wind Dir;11;0;Create;True;0;0;0;False;0;False;0.1,0.2;0.3,0.1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleTimeNode;61;-3234.401,-2345.619;Inherit;False;1;0;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;1056;-3184,-1696;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;1225;-2705.094,-2501.984;Inherit;False;Property;_MaxCloudCover;Max Cloud Cover;11;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;0.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;956;-2976,-2080;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;-0.5,-0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;998;-2832,-2080;Inherit;False;CentralUV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1036;-2464,-2368;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1226;-4243.582,-1806.441;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;375;-2782.653,-2580.27;Inherit;False;Property;_CumulusCoverageMultiplier;Cumulus Coverage Multiplier;3;2;[HideInInspector];[Header];Create;True;1;Cumulus Clouds;0;0;False;0;False;1;1;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;947;-3936,-1808;Inherit;False;Property;_Spherize;Spherize;6;1;[HideInInspector];Create;True;0;0;0;False;0;False;0.36;0.361;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1058;-2784,-1744;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;955;-3200,-2080;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;1074;-2656,-1744;Inherit;False;ModifiedCohesion;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;1045;-3952,-2064;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1080;-4130,-1813;Inherit;False;Scale;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;1183;148.0785,-2304.838;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;70;-3519.35,-2327.424;Inherit;False;Property;_WindSpeed;Wind Speed;2;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1138;1732.821,-1318.374;Inherit;False;1074;ModifiedCohesion;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1174;-1179.921,-2096.838;Inherit;False;1080;Scale;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1222;-2496,-2576;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;376;-2307,-2580;Inherit;False;CumulusCoverage;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1071;-3472,-2064;Inherit;False;Dot;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1078;-3664,-1808;Inherit;False;Spherize;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1076;-3312,-1824;Inherit;False;Flatness;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;1051;-3824,-2064;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0.85;False;4;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1212;-969.675,-2025.596;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1172;-1179.921,-2256.838;Inherit;False;1076;Flatness;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;1052;-3648,-2064;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1181;-171.9215,-2304.838;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1210;-985.2195,-2413.893;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1211;-972.675,-2119.596;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1173;-1179.921,-1936.838;Inherit;False;376;CumulusCoverage;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1175;-1179.921,-2176.838;Inherit;False;1078;Spherize;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;1179;-363.9215,-2272.838;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;1178;-763.9213,-2272.838;Inherit;True;Ghibli Clouds;-1;;46;bce7362c867d47d49a15818b7e6650d4;0;7;37;FLOAT2;0,0;False;21;FLOAT2;0,0;False;19;FLOAT;1;False;20;FLOAT;1;False;23;FLOAT;1;False;24;FLOAT;0;False;27;FLOAT;0.5;False;2;FLOAT;33;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1177;-1179.921,-2016.838;Inherit;False;1074;ModifiedCohesion;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1180;-363.9215,-2352.838;Inherit;False;1071;Dot;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1132;1572.821,-1702.374;Inherit;False;998;CentralUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;1149;1732.821,-1686.374;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;1.58;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;1133;1732.821,-1238.374;Inherit;False;376;CumulusCoverage;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1136;1732.821,-1766.374;Inherit;False;1035;Wind;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;1094;-2800,-1904;Inherit;False;1071;Dot;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1137;1732.821,-1558.374;Inherit;False;1076;Flatness;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;986;-2912,-1984;Inherit;False;Property;_ShadowingDistance;Shadowing Distance;8;1;[HideInInspector];Create;True;0;0;0;False;0;False;0.07;0.0288;0;0.1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;1182;-27.92151,-2304.838;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0.6;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1135;1732.821,-1478.374;Inherit;False;1078;Spherize;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1035;-2288,-2384;Inherit;False;Wind;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;1176;-1179.921,-2336.838;Inherit;False;998;CentralUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;1140;2420.822,-1718.374;Inherit;False;1071;Dot;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;1043;-4080,-2064;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;931;-4215,-1605;Inherit;False;Property;_Coverage;Coverage;7;1;[HideInInspector];Create;True;0;0;0;False;0;False;0.3574152;0;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1189;-1131.921,-1696.838;Inherit;False;1035;Wind;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1229;-3572.989,-1910.844;Inherit;False;2;2;0;FLOAT;20;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1190;-1131.921,-1376.838;Inherit;False;1080;Scale;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1187;-1131.921,-1296.838;Inherit;False;1074;ModifiedCohesion;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1209;2533.822,-1525.374;Inherit;False;1184;IT2PreAlpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;997;-2192,-2080;Inherit;False;ShadowUV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1214;-891.9211,-1296.838;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1191;-1131.921,-1616.838;Inherit;False;997;ShadowUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1215;-907.9211,-1664.838;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;1127;2036.822,-2150.374;Inherit;False;332;CloudColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;1186;-1131.921,-1536.838;Inherit;False;1078;Spherize;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;1151;1956.822,-2038.374;Inherit;False;Property;_CloudTextureColor;Cloud Texture Color;9;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;0.6320754,0.6320754,0.6320754,0;0.9056604,0.9056604,0.9056604,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1195;-139.9215,-1536.838;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1147;3524.822,-1718.374;Inherit;False;AdditionalLayer;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;1193;-315.9215,-1520.838;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;36;-4018.127,-2611.486;Inherit;False;Property;_CloudColor;Cloud Color;0;3;[HideInInspector];[HDR];[Header];Create;True;1;General Cloud Settings;0;0;False;0;False;0.7264151,0.7264151,0.7264151,0;0.1897503,0.2106195,0.236762,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GradientNode;1145;2980.822,-1750.374;Inherit;False;0;2;2;0,0,0,0.8676432;1,1,1,0.9294118;1,0;1,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.FunctionNode;1192;-667.9213,-1520.838;Inherit;True;Ghibli Clouds;-1;;44;bce7362c867d47d49a15818b7e6650d4;0;7;37;FLOAT2;0,0;False;21;FLOAT2;0,0;False;19;FLOAT;1;False;20;FLOAT;1;False;23;FLOAT;1;False;24;FLOAT;0;False;27;FLOAT;0.5;False;2;FLOAT;33;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1194;-315.9215,-1600.838;Inherit;False;1071;Dot;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;1158;2996.822,-1606.374;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0.9;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;1141;2420.822,-1638.374;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;332;-3793.636,-2612.531;Inherit;False;CloudColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;1188;-1131.921,-1456.838;Inherit;False;1076;Flatness;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GradientSampleNode;1146;3204.822,-1734.374;Inherit;True;2;0;OBJECT;;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;1095;-2304,-2080;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCRemapNode;1143;2804.822,-1686.374;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0.6;False;2;FLOAT;1.5;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1185;-1131.921,-1216.837;Inherit;False;376;CumulusCoverage;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;985;-2560,-2016;Inherit;True;4;4;0;FLOAT2;0,0;False;1;FLOAT2;-1,-1;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1184;308.0785,-2176.838;Inherit;False;IT2PreAlpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1161;-3872,-1648;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1142;2612.822,-1686.374;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1213;-891.9211,-1392.838;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;1196;4.078499,-1536.838;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0.6;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1154;2180.822,-2070.374;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;1152;2086.653,-1862.374;Inherit;False;1147;AdditionalLayer;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;1197;180.0785,-1536.838;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GradientNode;1198;116.0785,-1616.838;Inherit;False;0;2;2;0.06119964,0.06119964,0.06119964,0.4411841;1,1,1,0.5794156;1,0;1,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.ColorNode;52;-4015.761,-2428.211;Inherit;False;Property;_CloudHighlightColor;Cloud Highlight Color;1;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;1,1,1,0;0.6376311,0.5267081,0.4950157,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;1150;2372.822,-2086.374;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;1139;2020.822,-1654.374;Inherit;True;Ghibli Clouds;-1;;45;bce7362c867d47d49a15818b7e6650d4;0;7;37;FLOAT2;0,0;False;21;FLOAT2;0,0;False;19;FLOAT;1;False;20;FLOAT;1;False;23;FLOAT;5;False;24;FLOAT;0;False;27;FLOAT;0.5;False;2;FLOAT;33;FLOAT;0
Node;AmplifyShaderEditor.GradientSampleNode;1200;324.0785,-1584.838;Inherit;True;2;0;OBJECT;;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;334;-3796.106,-2428.746;Inherit;False;CloudHighlightColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;1217;-927,-319;Inherit;False;1202;IT2Alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GradientNode;1199;84.07853,-2384.838;Inherit;False;0;2;2;0.06119964,0.06119964,0.06119964,0.4617685;1,1,1,0.5117723;1,0;1,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.ClipNode;1223;-706.1743,-379.016;Inherit;False;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;1205;644.0785,-1632.838;Inherit;False;1165;ModifiedCloudColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;1134;1732.821,-1398.374;Inherit;False;1080;Scale;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;1144;3012.822,-1686.374;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1083;-3728,-1664;Inherit;False;Coverage;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1165;2644.822,-2086.374;Inherit;False;ModifiedCloudColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;1224;-1024,-224;Inherit;False;Property;_ClippingThreshold;Clipping Threshold;10;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;1204;644.0785,-1712.838;Inherit;False;334;CloudHighlightColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;1219;-924.733,-432.013;Inherit;False;1207;IT2Color;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1207;1092.078,-1584.838;Inherit;False;IT2Color;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;1202;628.0785,-2352.838;Inherit;False;IT2Alpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GradientSampleNode;1201;308.0785,-2368.838;Inherit;True;2;0;OBJECT;;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;1203;724.0786,-1520.838;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;1206;932.0785,-1584.838;Inherit;False;3;0;COLOR;1,1,1,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1233;-406.0242,-441.7039;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;False;False;True;1;LightMode=DepthOnly;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1230;-406.0242,-441.7039;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;1;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;0;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1231;-406.0242,-441.7039;Float;False;True;-1;2;;0;12;Distant Lands/Cozy/Stylized Clouds Ghibli Mobile;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;8;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;1;False;;False;False;False;False;False;False;False;False;True;True;True;221;False;;255;False;;255;False;;7;False;;2;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Transparent=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;1;1;False;;0;False;;1;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;1;LightMode=UniversalForward;False;False;0;Hidden/InternalErrorShader;0;0;Standard;22;Surface;0;0;  Blend;0;0;Two Sided;2;637952267559032767;Cast Shadows;1;0;  Use Shadow Threshold;0;0;Receive Shadows;1;0;GPU Instancing;1;0;LOD CrossFade;0;0;Built-in Fog;0;0;DOTS Instancing;0;0;Meta Pass;0;0;Extra Pre Pass;0;0;Tessellation;0;0;  Phong;0;0;  Strength;0.5,False,;0;  Type;0;0;  Tess;16,False,;0;  Min;10,False,;0;  Max;25,False,;0;  Edge Length;16,False,;0;  Max Displacement;25,False,;0;Vertex Position,InvertActionOnDeselection;1;0;0;5;False;True;True;True;False;False;;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1232;-406.0242,-441.7039;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=ShadowCaster;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1234;-406.0242,-441.7039;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
WireConnection;1057;0;1056;0
WireConnection;1057;1;1056;0
WireConnection;152;0;61;0
WireConnection;150;1;70;0
WireConnection;1059;0;1057;0
WireConnection;61;0;150;0
WireConnection;1056;0;1055;0
WireConnection;956;0;955;0
WireConnection;998;0;956;0
WireConnection;1036;0;1040;0
WireConnection;1036;1;1041;0
WireConnection;1226;0;925;0
WireConnection;1058;0;906;0
WireConnection;1058;2;1059;0
WireConnection;1074;0;1058;0
WireConnection;1045;0;1043;0
WireConnection;1045;1;1043;0
WireConnection;1080;0;1226;0
WireConnection;1183;0;1182;0
WireConnection;1222;0;375;0
WireConnection;1222;1;1225;0
WireConnection;376;0;1222;0
WireConnection;1071;0;1052;0
WireConnection;1078;0;947;0
WireConnection;1076;0;1229;0
WireConnection;1051;0;1045;0
WireConnection;1212;0;1177;0
WireConnection;1052;0;1051;0
WireConnection;1181;0;1180;0
WireConnection;1181;1;1179;0
WireConnection;1210;0;1171;0
WireConnection;1211;0;1174;0
WireConnection;1179;0;1178;33
WireConnection;1178;37;1210;0
WireConnection;1178;21;1176;0
WireConnection;1178;19;1175;0
WireConnection;1178;20;1172;0
WireConnection;1178;23;1211;0
WireConnection;1178;24;1212;0
WireConnection;1178;27;1173;0
WireConnection;1149;0;1132;0
WireConnection;1182;0;1181;0
WireConnection;1035;0;1036;0
WireConnection;1043;0;1042;0
WireConnection;1229;1;947;0
WireConnection;997;0;1095;0
WireConnection;1214;0;1187;0
WireConnection;1215;0;1189;0
WireConnection;1195;0;1194;0
WireConnection;1195;1;1193;0
WireConnection;1147;0;1146;1
WireConnection;1193;0;1192;33
WireConnection;1192;37;1215;0
WireConnection;1192;21;1191;0
WireConnection;1192;19;1186;0
WireConnection;1192;20;1188;0
WireConnection;1192;23;1213;0
WireConnection;1192;24;1214;0
WireConnection;1192;27;1185;0
WireConnection;1158;0;1143;0
WireConnection;1141;0;1139;33
WireConnection;332;0;36;0
WireConnection;1146;0;1145;0
WireConnection;1146;1;1158;0
WireConnection;1095;0;998;0
WireConnection;1095;1;985;0
WireConnection;1143;0;1142;0
WireConnection;1143;4;1209;0
WireConnection;985;0;998;0
WireConnection;985;2;986;0
WireConnection;985;3;1094;0
WireConnection;1184;0;1183;0
WireConnection;1161;0;931;0
WireConnection;1142;0;1140;0
WireConnection;1142;1;1141;0
WireConnection;1213;0;1190;0
WireConnection;1196;0;1195;0
WireConnection;1154;0;1127;0
WireConnection;1154;1;1151;0
WireConnection;1197;0;1196;0
WireConnection;1150;0;1127;0
WireConnection;1150;1;1154;0
WireConnection;1150;2;1152;0
WireConnection;1139;37;1136;0
WireConnection;1139;21;1149;0
WireConnection;1139;19;1135;0
WireConnection;1139;20;1137;0
WireConnection;1139;24;1138;0
WireConnection;1139;27;1133;0
WireConnection;1200;0;1198;0
WireConnection;1200;1;1197;0
WireConnection;334;0;52;0
WireConnection;1223;0;1219;0
WireConnection;1223;1;1217;0
WireConnection;1223;2;1224;0
WireConnection;1144;0;1143;0
WireConnection;1083;0;1161;0
WireConnection;1165;0;1150;0
WireConnection;1207;0;1206;0
WireConnection;1202;0;1201;1
WireConnection;1201;0;1199;0
WireConnection;1201;1;1183;0
WireConnection;1203;0;1200;1
WireConnection;1206;0;1204;0
WireConnection;1206;1;1205;0
WireConnection;1206;2;1203;0
WireConnection;1231;2;1223;0
ASEEND*/
//CHKSM=6B1395EB7E6BA9EEC172C2BC4C6F2CD934E79C69