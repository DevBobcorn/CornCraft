// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Distant Lands/Cozy/Stylized Terrain First Pass"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[HideInInspector]_TerrainHolesTexture("_TerrainHolesTexture", 2D) = "white" {}
		[HideInInspector]_Control("Control", 2D) = "white" {}
		[HideInInspector]_Splat3("Splat3", 2D) = "white" {}
		[HideInInspector]_Splat2("Splat2", 2D) = "white" {}
		[HideInInspector]_Splat1("Splat1", 2D) = "white" {}
		[HideInInspector]_Splat0("Splat0", 2D) = "white" {}
		[HideInInspector]_Normal0("Normal0", 2D) = "white" {}
		[HideInInspector]_Normal1("Normal1", 2D) = "white" {}
		[HideInInspector]_Normal2("Normal2", 2D) = "white" {}
		[HideInInspector]_Normal3("Normal3", 2D) = "white" {}
		[HideInInspector]_Smoothness3("Smoothness3", Range( 0 , 1)) = 1
		[HideInInspector]_Smoothness1("Smoothness1", Range( 0 , 1)) = 1
		[HideInInspector]_Smoothness0("Smoothness0", Range( 0 , 1)) = 1
		[HideInInspector]_Smoothness2("Smoothness2", Range( 0 , 1)) = 1
		[HideInInspector][Gamma]_Metallic0("Metallic0", Range( 0 , 1)) = 0
		[HideInInspector][Gamma]_Metallic2("Metallic2", Range( 0 , 1)) = 0
		[HideInInspector][Gamma]_Metallic3("Metallic3", Range( 0 , 1)) = 0
		[HideInInspector][Gamma]_Metallic1("Metallic1", Range( 0 , 1)) = 0
		[HideInInspector]_Mask2("_Mask2", 2D) = "white" {}
		[HideInInspector]_Mask0("_Mask0", 2D) = "white" {}
		[HideInInspector]_Mask1("_Mask1", 2D) = "white" {}
		[HideInInspector]_Mask3("_Mask3", 2D) = "white" {}
		[HideInInspector]_Specular2("Specular2", Color) = (1,1,1,0)
		[HideInInspector]_Specular1("Specular1", Color) = (1,1,1,0)
		[HideInInspector]_Specular0("Specular0", Color) = (1,1,1,0)
		[HideInInspector]_Specular3("Specular3", Color) = (1,1,1,0)
		[ASEBegin]_RockThreshold("Rock Threshold", Float) = 0
		[ASEEnd]_RockColor("Rock Color", Color) = (0,0,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}

		//_TransmissionShadow( "Transmission Shadow", Range( 0, 1 ) ) = 0.5
		//_TransStrength( "Trans Strength", Range( 0, 50 ) ) = 1
		//_TransNormal( "Trans Normal Distortion", Range( 0, 1 ) ) = 0.5
		//_TransScattering( "Trans Scattering", Range( 1, 50 ) ) = 2
		//_TransDirect( "Trans Direct", Range( 0, 1 ) ) = 0.9
		//_TransAmbient( "Trans Ambient", Range( 0, 1 ) ) = 0.1
		//_TransShadow( "Trans Shadow", Range( 0, 1 ) ) = 0.5
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

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
		Cull Back
		AlphaToMask Off
		
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
			
			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define _NORMALMAP 1
			#define ASE_SRP_VERSION 110000

			
			#pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _ADDITIONAL_OFF
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
			
			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			#pragma multi_compile _ SHADOWS_SHADOWMASK

			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS_FORWARD

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			
			#if ASE_SRP_VERSION <= 70108
			#define REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
			#endif

			#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
			    #define ENABLE_TERRAIN_PERPIXEL_NORMAL
			#endif

			#define ASE_NEEDS_VERT_TANGENT
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#pragma multi_compile_local __ _ALPHATEST_ON
			#pragma shader_feature_local _MASKMAP


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord : TEXCOORD0;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float4 lightmapUVOrVertexSH : TEXCOORD0;
				half4 fogFactorAndVertexLight : TEXCOORD1;
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				float4 shadowCoord : TEXCOORD2;
				#endif
				float4 tSpace0 : TEXCOORD3;
				float4 tSpace1 : TEXCOORD4;
				float4 tSpace2 : TEXCOORD5;
				#if defined(ASE_NEEDS_FRAG_SCREEN_POSITION)
				float4 screenPos : TEXCOORD6;
				#endif
				float4 ase_texcoord7 : TEXCOORD7;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _RockColor;
			float4 _TerrainHolesTexture_ST;
			float4 _Specular3;
			float4 _Splat3_ST;
			float4 _Specular2;
			float4 _Specular1;
			float4 _Splat1_ST;
			float4 _Splat2_ST;
			float4 _Control_ST;
			float4 _Specular0;
			float4 _Splat0_ST;
			float4 CZY_SnowTexture_ST;
			float _Smoothness2;
			float _Metallic3;
			float _Smoothness3;
			float _Smoothness0;
			float _Metallic0;
			float _Metallic1;
			float _Metallic2;
			float _Smoothness1;
			float _RockThreshold;
			#ifdef _TRANSMISSION_ASE
				float _TransmissionShadow;
			#endif
			#ifdef _TRANSLUCENCY_ASE
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _Mask2;
			sampler2D _Mask0;
			sampler2D _Mask1;
			sampler2D _Mask3;
			float4 _MaskMapRemapOffset2;
			float4 _MaskMapRemapScale0;
			float4 _MaskMapRemapScale2;
			float4 _MaskMapRemapOffset1;
			float4 _MaskMapRemapOffset0;
			float4 _MaskMapRemapOffset3;
			float4 _MaskMapRemapScale1;
			float4 _MaskMapRemapScale3;
			float4 CZY_SnowColor;
			sampler2D CZY_SnowTexture;
			sampler2D _Splat0;
			sampler2D _Control;
			sampler2D _Splat1;
			sampler2D _Splat2;
			sampler2D _Splat3;
			sampler2D _TerrainHolesTexture;
			float CZY_SnowScale;
			float _LayerHasMask0;
			float _LayerHasMask1;
			float _LayerHasMask2;
			float _LayerHasMask3;
			float CZY_SnowAmount;
			sampler2D _Normal0;
			sampler2D _Normal1;
			sampler2D _Normal2;
			sampler2D _Normal3;
			float CZY_PuddleScale;
			float CZY_WetnessAmount;


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
			
					float2 voronoihash5_g106( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi5_g106( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
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
						 		float2 o = voronoihash5_g106( n + g );
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
			
					float2 voronoihash1_g105( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi1_g105( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
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
						 		float2 o = voronoihash1_g105( n + g );
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
			
					float2 voronoihash8_g105( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi8_g105( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
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
						 		float2 o = voronoihash8_g105( n + g );
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

				float3 localCalculateTangentsSRP76_g107 = ( ( v.ase_tangent.xyz * v.ase_normal * 0.0 ) );
				{
				v.ase_tangent.xyz = cross ( v.ase_normal, float3( 0, 0, 1 ) );
				v.ase_tangent.w = -1;
				}
				
				o.ase_texcoord7.xy = v.texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord7.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = localCalculateTangentsSRP76_g107;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float3 positionVS = TransformWorldToView( positionWS );
				float4 positionCS = TransformWorldToHClip( positionWS );

				VertexNormalInputs normalInput = GetVertexNormalInputs( v.ase_normal, v.ase_tangent );

				o.tSpace0 = float4( normalInput.normalWS, positionWS.x);
				o.tSpace1 = float4( normalInput.tangentWS, positionWS.y);
				o.tSpace2 = float4( normalInput.bitangentWS, positionWS.z);

				OUTPUT_LIGHTMAP_UV( v.texcoord1, unity_LightmapST, o.lightmapUVOrVertexSH.xy );
				OUTPUT_SH( normalInput.normalWS.xyz, o.lightmapUVOrVertexSH.xyz );

				#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
					o.lightmapUVOrVertexSH.zw = v.texcoord;
					o.lightmapUVOrVertexSH.xy = v.texcoord * unity_LightmapST.xy + unity_LightmapST.zw;
				#endif

				half3 vertexLight = VertexLighting( positionWS, normalInput.normalWS );
				#ifdef ASE_FOG
					half fogFactor = ComputeFogFactor( positionCS.z );
				#else
					half fogFactor = 0;
				#endif
				o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
				
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				VertexPositionInputs vertexInput = (VertexPositionInputs)0;
				vertexInput.positionWS = positionWS;
				vertexInput.positionCS = positionCS;
				o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				
				o.clipPos = positionCS;
				#if defined(ASE_NEEDS_FRAG_SCREEN_POSITION)
				o.screenPos = ComputeScreenPos(positionCS);
				#endif
				return o;
			}
			
			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				
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
				o.ase_tangent = v.ase_tangent;
				o.texcoord = v.texcoord;
				o.texcoord1 = v.texcoord1;
				
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
				o.ase_tangent = patch[0].ase_tangent * bary.x + patch[1].ase_tangent * bary.y + patch[2].ase_tangent * bary.z;
				o.texcoord = patch[0].texcoord * bary.x + patch[1].texcoord * bary.y + patch[2].texcoord * bary.z;
				o.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
				
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

			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE)
				#define ASE_SV_DEPTH SV_DepthLessEqual  
			#else
				#define ASE_SV_DEPTH SV_Depth
			#endif

			half4 frag ( VertexOutput IN 
						#ifdef ASE_DEPTH_WRITE_ON
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
					float2 sampleCoords = (IN.lightmapUVOrVertexSH.zw / _TerrainHeightmapRecipSize.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
					float3 WorldNormal = TransformObjectToWorldNormal(normalize(SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb * 2 - 1));
					float3 WorldTangent = -cross(GetObjectToWorldMatrix()._13_23_33, WorldNormal);
					float3 WorldBiTangent = cross(WorldNormal, -WorldTangent);
				#else
					float3 WorldNormal = normalize( IN.tSpace0.xyz );
					float3 WorldTangent = IN.tSpace1.xyz;
					float3 WorldBiTangent = IN.tSpace2.xyz;
				#endif
				float3 WorldPosition = float3(IN.tSpace0.w,IN.tSpace1.w,IN.tSpace2.w);
				float3 WorldViewDirection = _WorldSpaceCameraPos.xyz  - WorldPosition;
				float4 ShadowCoords = float4( 0, 0, 0, 0 );
				#if defined(ASE_NEEDS_FRAG_SCREEN_POSITION)
				float4 ScreenPos = IN.screenPos;
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					ShadowCoords = IN.shadowCoord;
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
				#endif
	
				WorldViewDirection = SafeNormalize( WorldViewDirection );

				float2 uvCZY_SnowTexture = IN.ase_texcoord7.xy * CZY_SnowTexture_ST.xy + CZY_SnowTexture_ST.zw;
				float4 appendResult33_g107 = (float4(1.0 , 1.0 , 1.0 , _Smoothness0));
				float2 uv_Splat0 = IN.ase_texcoord7.xy * _Splat0_ST.xy + _Splat0_ST.zw;
				float4 tex2DNode4_g107 = tex2D( _Splat0, uv_Splat0 );
				float2 uv_Control = IN.ase_texcoord7.xy * _Control_ST.xy + _Control_ST.zw;
				float4 tex2DNode5_g107 = tex2D( _Control, uv_Control );
				float dotResult20_g107 = dot( tex2DNode5_g107 , float4(1,1,1,1) );
				float SplatWeight22_g107 = dotResult20_g107;
				float localSplatClip74_g107 = ( SplatWeight22_g107 );
				float SplatWeight74_g107 = SplatWeight22_g107;
				{
				#if !defined(SHADER_API_MOBILE) && defined(TERRAIN_SPLAT_ADDPASS)
				clip(SplatWeight74_g107 == 0.0f ? -1 : 1);
				#endif
				}
				float4 SplatControl26_g107 = ( tex2DNode5_g107 / ( localSplatClip74_g107 + 0.001 ) );
				float4 temp_output_59_0_g107 = SplatControl26_g107;
				float4 break20_g108 = temp_output_59_0_g107;
				float4 lerpResult16_g108 = lerp( float4( 0,0,0,0 ) , ( appendResult33_g107 * tex2DNode4_g107 * _Specular0 ) , ( break20_g108.r > max( max( break20_g108.g , break20_g108.b ) , break20_g108.a ) ? 1.0 : 0.0 ));
				float4 appendResult36_g107 = (float4(1.0 , 1.0 , 1.0 , _Smoothness1));
				float2 uv_Splat1 = IN.ase_texcoord7.xy * _Splat1_ST.xy + _Splat1_ST.zw;
				float4 tex2DNode3_g107 = tex2D( _Splat1, uv_Splat1 );
				float4 lerpResult14_g108 = lerp( lerpResult16_g108 , ( appendResult36_g107 * tex2DNode3_g107 * _Specular1 ) , ( break20_g108.g > max( max( break20_g108.r , break20_g108.b ) , break20_g108.a ) ? 1.0 : 0.0 ));
				float4 appendResult39_g107 = (float4(1.0 , 1.0 , 1.0 , _Smoothness2));
				float2 uv_Splat2 = IN.ase_texcoord7.xy * _Splat2_ST.xy + _Splat2_ST.zw;
				float4 tex2DNode6_g107 = tex2D( _Splat2, uv_Splat2 );
				float4 lerpResult8_g108 = lerp( lerpResult14_g108 , ( appendResult39_g107 * tex2DNode6_g107 * _Specular2 ) , ( break20_g108.b > max( max( break20_g108.r , break20_g108.g ) , break20_g108.a ) ? 1.0 : 0.0 ));
				float4 appendResult42_g107 = (float4(1.0 , 1.0 , 1.0 , _Smoothness3));
				float2 uv_Splat3 = IN.ase_texcoord7.xy * _Splat3_ST.xy + _Splat3_ST.zw;
				float4 tex2DNode7_g107 = tex2D( _Splat3, uv_Splat3 );
				float4 lerpResult13_g108 = lerp( lerpResult8_g108 , ( appendResult42_g107 * tex2DNode7_g107 * _Specular3 ) , ( break20_g108.a > max( max( break20_g108.r , break20_g108.g ) , break20_g108.b ) ? 1.0 : 0.0 ));
				float4 MixDiffuse28_g107 = saturate( lerpResult13_g108 );
				float4 temp_output_60_0_g107 = MixDiffuse28_g107;
				float4 localClipHoles100_g107 = ( temp_output_60_0_g107 );
				float2 uv_TerrainHolesTexture = IN.ase_texcoord7.xy * _TerrainHolesTexture_ST.xy + _TerrainHolesTexture_ST.zw;
				float holeClipValue99_g107 = tex2D( _TerrainHolesTexture, uv_TerrainHolesTexture ).r;
				float Hole100_g107 = holeClipValue99_g107;
				{
				#ifdef _ALPHATEST_ON
				clip(Hole100_g107 == 0.0f ? -1 : 1);
				#endif
				}
				float2 appendResult3_g106 = (float2(WorldPosition.x , WorldPosition.z));
				float temp_output_6_0_g106 = ( 1.0 / CZY_SnowScale );
				float simplePerlin2D7_g106 = snoise( appendResult3_g106*temp_output_6_0_g106 );
				simplePerlin2D7_g106 = simplePerlin2D7_g106*0.5 + 0.5;
				float time5_g106 = 0.0;
				float2 voronoiSmoothId5_g106 = 0;
				float2 coords5_g106 = appendResult3_g106 * ( temp_output_6_0_g106 / 0.1 );
				float2 id5_g106 = 0;
				float2 uv5_g106 = 0;
				float voroi5_g106 = voronoi5_g106( coords5_g106, time5_g106, id5_g106, uv5_g106, 0, voronoiSmoothId5_g106 );
				float4 appendResult55_g107 = (float4(_Metallic0 , _Metallic1 , _Metallic2 , _Metallic3));
				float localComputeMasks130_g107 = ( 0.0 );
				float4 masks0130_g107 = float4( 0,0,0,0 );
				float4 masks1130_g107 = float4( 0,0,0,0 );
				float4 masks2130_g107 = float4( 0,0,0,0 );
				float4 masks3130_g107 = float4( 0,0,0,0 );
				float4 appendResult135_g107 = (float4(_LayerHasMask0 , _LayerHasMask1 , _LayerHasMask2 , _LayerHasMask3));
				float4 layerHasMask136_g107 = appendResult135_g107;
				float4 hasMask130_g107 = layerHasMask136_g107;
				float2 uvMask0130_g107 = uv_Splat0;
				float2 uvMask1130_g107 = uv_Splat1;
				float2 uvMask2130_g107 = uv_Splat2;
				float2 uvMask3130_g107 = uv_Splat3;
				{
				masks0130_g107 = 0.5h;
				masks1130_g107 = 0.5h;
				masks2130_g107 = 0.5h;
				masks3130_g107 = 0.5h;
				#ifdef _MASKMAP
				masks0130_g107 = lerp(masks0130_g107, SAMPLE_TEXTURE2D(_Mask0, sampler_Mask0, uvMask0130_g107), hasMask130_g107.x);
				masks1130_g107 = lerp(masks1130_g107, SAMPLE_TEXTURE2D(_Mask1, sampler_Mask0, uvMask1130_g107), hasMask130_g107.y);
				masks2130_g107 = lerp(masks2130_g107, SAMPLE_TEXTURE2D(_Mask2, sampler_Mask0, uvMask2130_g107), hasMask130_g107.z);
				masks3130_g107 = lerp(masks3130_g107, SAMPLE_TEXTURE2D(_Mask3, sampler_Mask0, uvMask3130_g107), hasMask130_g107.w);
				#endif
				masks0130_g107 *= _MaskMapRemapScale0.rgba;
				masks0130_g107 += _MaskMapRemapOffset0.rgba;
				masks1130_g107 *= _MaskMapRemapScale1.rgba;
				masks1130_g107 += _MaskMapRemapOffset1.rgba;
				masks2130_g107 *= _MaskMapRemapScale2.rgba;
				masks2130_g107 += _MaskMapRemapOffset2.rgba;
				masks3130_g107 *= _MaskMapRemapScale3.rgba;
				masks3130_g107 += _MaskMapRemapOffset3.rgba;
				}
				float4 mask0138_g107 = masks0130_g107;
				float4 mask1139_g107 = masks1130_g107;
				float4 mask2140_g107 = masks2130_g107;
				float4 mask3141_g107 = masks3130_g107;
				float4 appendResult168_g107 = (float4((mask0138_g107).x , (mask1139_g107).x , (mask2140_g107).x , (mask3141_g107).x));
				float4 maskMetallic169_g107 = appendResult168_g107;
				float4 lerpResult202_g107 = lerp( appendResult55_g107 , maskMetallic169_g107 , layerHasMask136_g107);
				float dotResult53_g107 = dot( SplatControl26_g107 , lerpResult202_g107 );
				float4 lerpResult19_g106 = lerp( ( CZY_SnowColor * tex2D( CZY_SnowTexture, uvCZY_SnowTexture ) ) , localClipHoles100_g107 , ( ( pow( ( pow( WorldNormal.y , 7.0 ) * ( simplePerlin2D7_g106 * ( 1.0 - voroi5_g106 ) ) ) , 0.5 ) * dotResult53_g107 ) > ( 1.0 - CZY_SnowAmount ) ? 0.0 : 1.0 ));
				float4 lerpResult67 = lerp( _RockColor , lerpResult19_g106 , ( ( 1.0 - WorldNormal.y ) > _RockThreshold ? 0.0 : 1.0 ));
				
				float4 break20_g109 = temp_output_59_0_g107;
				float4 lerpResult16_g109 = lerp( float4( 0,0,0,0 ) , tex2D( _Normal0, uv_Splat0 ) , ( break20_g109.r > max( max( break20_g109.g , break20_g109.b ) , break20_g109.a ) ? 1.0 : 0.0 ));
				float4 lerpResult14_g109 = lerp( lerpResult16_g109 , tex2D( _Normal1, uv_Splat1 ) , ( break20_g109.g > max( max( break20_g109.r , break20_g109.b ) , break20_g109.a ) ? 1.0 : 0.0 ));
				float4 lerpResult8_g109 = lerp( lerpResult14_g109 , tex2D( _Normal2, uv_Splat2 ) , ( break20_g109.b > max( max( break20_g109.r , break20_g109.g ) , break20_g109.a ) ? 1.0 : 0.0 ));
				float4 lerpResult13_g109 = lerp( lerpResult8_g109 , tex2D( _Normal3, uv_Splat3 ) , ( break20_g109.a > max( max( break20_g109.r , break20_g109.g ) , break20_g109.b ) ? 1.0 : 0.0 ));
				float3 temp_output_61_0_g107 = UnpackNormalScale( saturate( lerpResult13_g109 ), 1.0 );
				
				float temp_output_5_0_g105 = ( 1.0 / CZY_PuddleScale );
				float time1_g105 = 0.0;
				float2 voronoiSmoothId1_g105 = 0;
				float2 appendResult3_g105 = (float2(WorldPosition.x , WorldPosition.z));
				float2 coords1_g105 = appendResult3_g105 * temp_output_5_0_g105;
				float2 id1_g105 = 0;
				float2 uv1_g105 = 0;
				float voroi1_g105 = voronoi1_g105( coords1_g105, time1_g105, id1_g105, uv1_g105, 0, voronoiSmoothId1_g105 );
				float time8_g105 = 2.16;
				float2 voronoiSmoothId8_g105 = 0;
				float2 coords8_g105 = IN.ase_texcoord7.xy * ( temp_output_5_0_g105 * 3.0 );
				float2 id8_g105 = 0;
				float2 uv8_g105 = 0;
				float voroi8_g105 = voronoi8_g105( coords8_g105, time8_g105, id8_g105, uv8_g105, 0, voronoiSmoothId8_g105 );
				float4 appendResult205_g107 = (float4(_Smoothness0 , _Smoothness1 , _Smoothness2 , _Smoothness3));
				float4 appendResult206_g107 = (float4(tex2DNode4_g107.a , tex2DNode3_g107.a , tex2DNode6_g107.a , tex2DNode7_g107.a));
				float4 defaultSmoothness210_g107 = ( appendResult205_g107 * appendResult206_g107 );
				float4 appendResult158_g107 = (float4((mask0138_g107).w , (mask1139_g107).w , (mask2140_g107).w , (mask3141_g107).w));
				float4 maskSmoothness149_g107 = appendResult158_g107;
				float4 lerpResult215_g107 = lerp( defaultSmoothness210_g107 , maskSmoothness149_g107 , layerHasMask136_g107);
				float dotResult216_g107 = dot( lerpResult215_g107 , SplatControl26_g107 );
				
				float4 appendResult188_g107 = (float4(_MaskMapRemapOffset0.y , _MaskMapRemapOffset1.y , _MaskMapRemapOffset2.y , _MaskMapRemapOffset3.y));
				float4 appendResult189_g107 = (float4(_MaskMapRemapScale0.y , _MaskMapRemapScale1.y , _MaskMapRemapScale2.y , _MaskMapRemapScale3.y));
				float4 defaultOcclusion191_g107 = ( appendResult188_g107 + appendResult189_g107 );
				float4 appendResult180_g107 = (float4((mask0138_g107).y , (mask1139_g107).y , (mask2140_g107).y , (mask3141_g107).y));
				float4 maskOcclusion181_g107 = appendResult180_g107;
				float4 lerpResult192_g107 = lerp( defaultOcclusion191_g107 , maskOcclusion181_g107 , layerHasMask136_g107);
				float dotResult197_g107 = dot( lerpResult192_g107 , SplatControl26_g107 );
				
				float3 Albedo = lerpResult67.rgb;
				float3 Normal = temp_output_61_0_g107;
				float3 Emission = 0;
				float3 Specular = 0.5;
				float Metallic = 0;
				float Smoothness = ( ( WorldNormal.y * 2.0 * ( (1.0 + (voroi1_g105 - 0.0) * (0.0 - 1.0) / (0.4 - 0.0)) + (0.1 + (voroi8_g105 - 0.0) * (-0.3 - 0.1) / (0.21 - 0.0)) ) * (0.3 + (CZY_WetnessAmount - 0.0) * (1.0 - 0.3) / (1.0 - 0.0)) ) > ( 1.0 - ( CZY_WetnessAmount * dotResult216_g107 ) ) ? 1.0 : 0.0 );
				float Occlusion = dotResult197_g107;
				float Alpha = SplatWeight22_g107;
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;
				float3 BakedGI = 0;
				float3 RefractionColor = 1;
				float RefractionIndex = 1;
				float3 Transmission = 1;
				float3 Translucency = 1;
				#ifdef ASE_DEPTH_WRITE_ON
				float DepthValue = 0;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				InputData inputData;
				inputData.positionWS = WorldPosition;
				inputData.viewDirectionWS = WorldViewDirection;
				inputData.shadowCoord = ShadowCoords;

				#ifdef _NORMALMAP
					#if _NORMAL_DROPOFF_TS
					inputData.normalWS = TransformTangentToWorld(Normal, half3x3( WorldTangent, WorldBiTangent, WorldNormal ));
					#elif _NORMAL_DROPOFF_OS
					inputData.normalWS = TransformObjectToWorldNormal(Normal);
					#elif _NORMAL_DROPOFF_WS
					inputData.normalWS = Normal;
					#endif
					inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
				#else
					inputData.normalWS = WorldNormal;
				#endif

				#ifdef ASE_FOG
					inputData.fogCoord = IN.fogFactorAndVertexLight.x;
				#endif

				inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
				#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
					float3 SH = SampleSH(inputData.normalWS.xyz);
				#else
					float3 SH = IN.lightmapUVOrVertexSH.xyz;
				#endif

				inputData.bakedGI = SAMPLE_GI( IN.lightmapUVOrVertexSH.xy, SH, inputData.normalWS );
				#ifdef _ASE_BAKEDGI
					inputData.bakedGI = BakedGI;
				#endif
				
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.clipPos);
				inputData.shadowMask = SAMPLE_SHADOWMASK(IN.lightmapUVOrVertexSH.xy);

				half4 color = UniversalFragmentPBR(
					inputData, 
					Albedo, 
					Metallic, 
					Specular, 
					Smoothness, 
					Occlusion, 
					Emission, 
					Alpha);

				#ifdef _TRANSMISSION_ASE
				{
					float shadow = _TransmissionShadow;

					Light mainLight = GetMainLight( inputData.shadowCoord );
					float3 mainAtten = mainLight.color * mainLight.distanceAttenuation;
					mainAtten = lerp( mainAtten, mainAtten * mainLight.shadowAttenuation, shadow );
					half3 mainTransmission = max(0 , -dot(inputData.normalWS, mainLight.direction)) * mainAtten * Transmission;
					color.rgb += Albedo * mainTransmission;

					#ifdef _ADDITIONAL_LIGHTS
						int transPixelLightCount = GetAdditionalLightsCount();
						for (int i = 0; i < transPixelLightCount; ++i)
						{
							Light light = GetAdditionalLight(i, inputData.positionWS);
							float3 atten = light.color * light.distanceAttenuation;
							atten = lerp( atten, atten * light.shadowAttenuation, shadow );

							half3 transmission = max(0 , -dot(inputData.normalWS, light.direction)) * atten * Transmission;
							color.rgb += Albedo * transmission;
						}
					#endif
				}
				#endif

				#ifdef _TRANSLUCENCY_ASE
				{
					float shadow = _TransShadow;
					float normal = _TransNormal;
					float scattering = _TransScattering;
					float direct = _TransDirect;
					float ambient = _TransAmbient;
					float strength = _TransStrength;

					Light mainLight = GetMainLight( inputData.shadowCoord );
					float3 mainAtten = mainLight.color * mainLight.distanceAttenuation;
					mainAtten = lerp( mainAtten, mainAtten * mainLight.shadowAttenuation, shadow );

					half3 mainLightDir = mainLight.direction + inputData.normalWS * normal;
					half mainVdotL = pow( saturate( dot( inputData.viewDirectionWS, -mainLightDir ) ), scattering );
					half3 mainTranslucency = mainAtten * ( mainVdotL * direct + inputData.bakedGI * ambient ) * Translucency;
					color.rgb += Albedo * mainTranslucency * strength;

					#ifdef _ADDITIONAL_LIGHTS
						int transPixelLightCount = GetAdditionalLightsCount();
						for (int i = 0; i < transPixelLightCount; ++i)
						{
							Light light = GetAdditionalLight(i, inputData.positionWS);
							float3 atten = light.color * light.distanceAttenuation;
							atten = lerp( atten, atten * light.shadowAttenuation, shadow );

							half3 lightDir = light.direction + inputData.normalWS * normal;
							half VdotL = pow( saturate( dot( inputData.viewDirectionWS, -lightDir ) ), scattering );
							half3 translucency = atten * ( VdotL * direct + inputData.bakedGI * ambient ) * Translucency;
							color.rgb += Albedo * translucency * strength;
						}
					#endif
				}
				#endif

				#ifdef _REFRACTION_ASE
					float4 projScreenPos = ScreenPos / ScreenPos.w;
					float3 refractionOffset = ( RefractionIndex - 1.0 ) * mul( UNITY_MATRIX_V, float4( WorldNormal,0 ) ).xyz * ( 1.0 - dot( WorldNormal, WorldViewDirection ) );
					projScreenPos.xy += refractionOffset.xy;
					float3 refraction = SHADERGRAPH_SAMPLE_SCENE_COLOR( projScreenPos.xy ) * RefractionColor;
					color.rgb = lerp( refraction, color.rgb, color.a );
					color.a = 1;
				#endif

				#ifdef ASE_FINAL_COLOR_ALPHA_MULTIPLY
					color.rgb *= color.a;
				#endif

				#ifdef ASE_FOG
					#ifdef TERRAIN_SPLAT_ADDPASS
						color.rgb = MixFogColor(color.rgb, half3( 0, 0, 0 ), IN.fogFactorAndVertexLight.x );
					#else
						color.rgb = MixFog(color.rgb, IN.fogFactorAndVertexLight.x);
					#endif
				#endif

				#ifdef ASE_DEPTH_WRITE_ON
					outputDepth = DepthValue;
				#endif

				return color;
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
			
			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define _NORMALMAP 1
			#define ASE_SRP_VERSION 110000

			
			#pragma vertex vert
			#pragma fragment frag
#if ASE_SRP_VERSION >= 110000
			#pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW
#endif
			#define SHADERPASS_SHADOWCASTER

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#define ASE_NEEDS_VERT_NORMAL
			#pragma multi_compile_local __ _ALPHATEST_ON
			#pragma shader_feature_local _MASKMAP


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
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
			float4 _RockColor;
			float4 _TerrainHolesTexture_ST;
			float4 _Specular3;
			float4 _Splat3_ST;
			float4 _Specular2;
			float4 _Specular1;
			float4 _Splat1_ST;
			float4 _Splat2_ST;
			float4 _Control_ST;
			float4 _Specular0;
			float4 _Splat0_ST;
			float4 CZY_SnowTexture_ST;
			float _Smoothness2;
			float _Metallic3;
			float _Smoothness3;
			float _Smoothness0;
			float _Metallic0;
			float _Metallic1;
			float _Metallic2;
			float _Smoothness1;
			float _RockThreshold;
			#ifdef _TRANSMISSION_ASE
				float _TransmissionShadow;
			#endif
			#ifdef _TRANSLUCENCY_ASE
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _Mask2;
			sampler2D _Mask0;
			sampler2D _Mask1;
			sampler2D _Mask3;
			float4 _MaskMapRemapOffset2;
			float4 _MaskMapRemapScale0;
			float4 _MaskMapRemapScale2;
			float4 _MaskMapRemapOffset1;
			float4 _MaskMapRemapOffset0;
			float4 _MaskMapRemapOffset3;
			float4 _MaskMapRemapScale1;
			float4 _MaskMapRemapScale3;
			sampler2D _Control;


			
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

				float3 localCalculateTangentsSRP76_g107 = ( ( v.ase_tangent.xyz * v.ase_normal * 0.0 ) );
				{
				v.ase_tangent.xyz = cross ( v.ase_normal, float3( 0, 0, 1 ) );
				v.ase_tangent.w = -1;
				}
				
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = localCalculateTangentsSRP76_g107;
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
				float3 normalWS = TransformObjectToWorldDir(v.ase_normal);

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
				float4 clipPos = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
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
				float4 ase_tangent : TANGENT;
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
				o.ase_tangent = v.ase_tangent;
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
				o.ase_tangent = patch[0].ase_tangent * bary.x + patch[1].ase_tangent * bary.y + patch[2].ase_tangent * bary.z;
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

			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE)
				#define ASE_SV_DEPTH SV_DepthLessEqual  
			#else
				#define ASE_SV_DEPTH SV_Depth
			#endif

			half4 frag(	VertexOutput IN 
						#ifdef ASE_DEPTH_WRITE_ON
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						 ) : SV_TARGET
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

				float2 uv_Control = IN.ase_texcoord2.xy * _Control_ST.xy + _Control_ST.zw;
				float4 tex2DNode5_g107 = tex2D( _Control, uv_Control );
				float dotResult20_g107 = dot( tex2DNode5_g107 , float4(1,1,1,1) );
				float SplatWeight22_g107 = dotResult20_g107;
				
				float Alpha = SplatWeight22_g107;
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;
				#ifdef ASE_DEPTH_WRITE_ON
				float DepthValue = 0;
				#endif

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
				#ifdef ASE_DEPTH_WRITE_ON
					outputDepth = DepthValue;
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
			
			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define _NORMALMAP 1
			#define ASE_SRP_VERSION 110000

			
			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS_DEPTHONLY

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#define ASE_NEEDS_VERT_NORMAL
			#pragma multi_compile_local __ _ALPHATEST_ON
			#pragma shader_feature_local _MASKMAP


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
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
			float4 _RockColor;
			float4 _TerrainHolesTexture_ST;
			float4 _Specular3;
			float4 _Splat3_ST;
			float4 _Specular2;
			float4 _Specular1;
			float4 _Splat1_ST;
			float4 _Splat2_ST;
			float4 _Control_ST;
			float4 _Specular0;
			float4 _Splat0_ST;
			float4 CZY_SnowTexture_ST;
			float _Smoothness2;
			float _Metallic3;
			float _Smoothness3;
			float _Smoothness0;
			float _Metallic0;
			float _Metallic1;
			float _Metallic2;
			float _Smoothness1;
			float _RockThreshold;
			#ifdef _TRANSMISSION_ASE
				float _TransmissionShadow;
			#endif
			#ifdef _TRANSLUCENCY_ASE
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _Mask2;
			sampler2D _Mask0;
			sampler2D _Mask1;
			sampler2D _Mask3;
			float4 _MaskMapRemapOffset2;
			float4 _MaskMapRemapScale0;
			float4 _MaskMapRemapScale2;
			float4 _MaskMapRemapOffset1;
			float4 _MaskMapRemapOffset0;
			float4 _MaskMapRemapOffset3;
			float4 _MaskMapRemapScale1;
			float4 _MaskMapRemapScale3;
			sampler2D _Control;


			
			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 localCalculateTangentsSRP76_g107 = ( ( v.ase_tangent.xyz * v.ase_normal * 0.0 ) );
				{
				v.ase_tangent.xyz = cross ( v.ase_normal, float3( 0, 0, 1 ) );
				v.ase_tangent.w = -1;
				}
				
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = localCalculateTangentsSRP76_g107;
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
				o.clipPos = positionCS;
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
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
				o.ase_tangent = v.ase_tangent;
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
				o.ase_tangent = patch[0].ase_tangent * bary.x + patch[1].ase_tangent * bary.y + patch[2].ase_tangent * bary.z;
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

			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE)
				#define ASE_SV_DEPTH SV_DepthLessEqual  
			#else
				#define ASE_SV_DEPTH SV_Depth
			#endif
			half4 frag(	VertexOutput IN 
						#ifdef ASE_DEPTH_WRITE_ON
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						 ) : SV_TARGET
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

				float2 uv_Control = IN.ase_texcoord2.xy * _Control_ST.xy + _Control_ST.zw;
				float4 tex2DNode5_g107 = tex2D( _Control, uv_Control );
				float dotResult20_g107 = dot( tex2DNode5_g107 , float4(1,1,1,1) );
				float SplatWeight22_g107 = dotResult20_g107;
				
				float Alpha = SplatWeight22_g107;
				float AlphaClipThreshold = 0.5;
				#ifdef ASE_DEPTH_WRITE_ON
				float DepthValue = 0;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif
				#ifdef ASE_DEPTH_WRITE_ON
				outputDepth = DepthValue;
				#endif

				return 0;
			}
			ENDHLSL
		}
		
		
		Pass
		{
			
			Name "Meta"
			Tags { "LightMode"="Meta" }

			Cull Off

			HLSLPROGRAM
			
			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define _NORMALMAP 1
			#define ASE_SRP_VERSION 110000

			
			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS_META

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#pragma multi_compile_local __ _ALPHATEST_ON
			#pragma shader_feature_local _MASKMAP


			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 ase_tangent : TANGENT;
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
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _RockColor;
			float4 _TerrainHolesTexture_ST;
			float4 _Specular3;
			float4 _Splat3_ST;
			float4 _Specular2;
			float4 _Specular1;
			float4 _Splat1_ST;
			float4 _Splat2_ST;
			float4 _Control_ST;
			float4 _Specular0;
			float4 _Splat0_ST;
			float4 CZY_SnowTexture_ST;
			float _Smoothness2;
			float _Metallic3;
			float _Smoothness3;
			float _Smoothness0;
			float _Metallic0;
			float _Metallic1;
			float _Metallic2;
			float _Smoothness1;
			float _RockThreshold;
			#ifdef _TRANSMISSION_ASE
				float _TransmissionShadow;
			#endif
			#ifdef _TRANSLUCENCY_ASE
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _Mask2;
			sampler2D _Mask0;
			sampler2D _Mask1;
			sampler2D _Mask3;
			float4 _MaskMapRemapOffset2;
			float4 _MaskMapRemapScale0;
			float4 _MaskMapRemapScale2;
			float4 _MaskMapRemapOffset1;
			float4 _MaskMapRemapOffset0;
			float4 _MaskMapRemapOffset3;
			float4 _MaskMapRemapScale1;
			float4 _MaskMapRemapScale3;
			float4 CZY_SnowColor;
			sampler2D CZY_SnowTexture;
			sampler2D _Splat0;
			sampler2D _Control;
			sampler2D _Splat1;
			sampler2D _Splat2;
			sampler2D _Splat3;
			sampler2D _TerrainHolesTexture;
			float CZY_SnowScale;
			float _LayerHasMask0;
			float _LayerHasMask1;
			float _LayerHasMask2;
			float _LayerHasMask3;
			float CZY_SnowAmount;


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
			
					float2 voronoihash5_g106( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi5_g106( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
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
						 		float2 o = voronoihash5_g106( n + g );
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

				float3 localCalculateTangentsSRP76_g107 = ( ( v.ase_tangent.xyz * v.ase_normal * 0.0 ) );
				{
				v.ase_tangent.xyz = cross ( v.ase_normal, float3( 0, 0, 1 ) );
				v.ase_tangent.w = -1;
				}
				
				float3 ase_worldNormal = TransformObjectToWorldNormal(v.ase_normal);
				o.ase_texcoord3.xyz = ase_worldNormal;
				
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.zw = 0;
				o.ase_texcoord3.w = 0;
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = localCalculateTangentsSRP76_g107;
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

				o.clipPos = MetaVertexPosition( v.vertex, v.texcoord1.xy, v.texcoord1.xy, unity_LightmapST, unity_DynamicLightmapST );
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
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 ase_tangent : TANGENT;
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
				o.texcoord1 = v.texcoord1;
				o.texcoord2 = v.texcoord2;
				o.ase_tangent = v.ase_tangent;
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
				o.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
				o.texcoord2 = patch[0].texcoord2 * bary.x + patch[1].texcoord2 * bary.y + patch[2].texcoord2 * bary.z;
				o.ase_tangent = patch[0].ase_tangent * bary.x + patch[1].ase_tangent * bary.y + patch[2].ase_tangent * bary.z;
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

				float2 uvCZY_SnowTexture = IN.ase_texcoord2.xy * CZY_SnowTexture_ST.xy + CZY_SnowTexture_ST.zw;
				float4 appendResult33_g107 = (float4(1.0 , 1.0 , 1.0 , _Smoothness0));
				float2 uv_Splat0 = IN.ase_texcoord2.xy * _Splat0_ST.xy + _Splat0_ST.zw;
				float4 tex2DNode4_g107 = tex2D( _Splat0, uv_Splat0 );
				float2 uv_Control = IN.ase_texcoord2.xy * _Control_ST.xy + _Control_ST.zw;
				float4 tex2DNode5_g107 = tex2D( _Control, uv_Control );
				float dotResult20_g107 = dot( tex2DNode5_g107 , float4(1,1,1,1) );
				float SplatWeight22_g107 = dotResult20_g107;
				float localSplatClip74_g107 = ( SplatWeight22_g107 );
				float SplatWeight74_g107 = SplatWeight22_g107;
				{
				#if !defined(SHADER_API_MOBILE) && defined(TERRAIN_SPLAT_ADDPASS)
				clip(SplatWeight74_g107 == 0.0f ? -1 : 1);
				#endif
				}
				float4 SplatControl26_g107 = ( tex2DNode5_g107 / ( localSplatClip74_g107 + 0.001 ) );
				float4 temp_output_59_0_g107 = SplatControl26_g107;
				float4 break20_g108 = temp_output_59_0_g107;
				float4 lerpResult16_g108 = lerp( float4( 0,0,0,0 ) , ( appendResult33_g107 * tex2DNode4_g107 * _Specular0 ) , ( break20_g108.r > max( max( break20_g108.g , break20_g108.b ) , break20_g108.a ) ? 1.0 : 0.0 ));
				float4 appendResult36_g107 = (float4(1.0 , 1.0 , 1.0 , _Smoothness1));
				float2 uv_Splat1 = IN.ase_texcoord2.xy * _Splat1_ST.xy + _Splat1_ST.zw;
				float4 tex2DNode3_g107 = tex2D( _Splat1, uv_Splat1 );
				float4 lerpResult14_g108 = lerp( lerpResult16_g108 , ( appendResult36_g107 * tex2DNode3_g107 * _Specular1 ) , ( break20_g108.g > max( max( break20_g108.r , break20_g108.b ) , break20_g108.a ) ? 1.0 : 0.0 ));
				float4 appendResult39_g107 = (float4(1.0 , 1.0 , 1.0 , _Smoothness2));
				float2 uv_Splat2 = IN.ase_texcoord2.xy * _Splat2_ST.xy + _Splat2_ST.zw;
				float4 tex2DNode6_g107 = tex2D( _Splat2, uv_Splat2 );
				float4 lerpResult8_g108 = lerp( lerpResult14_g108 , ( appendResult39_g107 * tex2DNode6_g107 * _Specular2 ) , ( break20_g108.b > max( max( break20_g108.r , break20_g108.g ) , break20_g108.a ) ? 1.0 : 0.0 ));
				float4 appendResult42_g107 = (float4(1.0 , 1.0 , 1.0 , _Smoothness3));
				float2 uv_Splat3 = IN.ase_texcoord2.xy * _Splat3_ST.xy + _Splat3_ST.zw;
				float4 tex2DNode7_g107 = tex2D( _Splat3, uv_Splat3 );
				float4 lerpResult13_g108 = lerp( lerpResult8_g108 , ( appendResult42_g107 * tex2DNode7_g107 * _Specular3 ) , ( break20_g108.a > max( max( break20_g108.r , break20_g108.g ) , break20_g108.b ) ? 1.0 : 0.0 ));
				float4 MixDiffuse28_g107 = saturate( lerpResult13_g108 );
				float4 temp_output_60_0_g107 = MixDiffuse28_g107;
				float4 localClipHoles100_g107 = ( temp_output_60_0_g107 );
				float2 uv_TerrainHolesTexture = IN.ase_texcoord2.xy * _TerrainHolesTexture_ST.xy + _TerrainHolesTexture_ST.zw;
				float holeClipValue99_g107 = tex2D( _TerrainHolesTexture, uv_TerrainHolesTexture ).r;
				float Hole100_g107 = holeClipValue99_g107;
				{
				#ifdef _ALPHATEST_ON
				clip(Hole100_g107 == 0.0f ? -1 : 1);
				#endif
				}
				float3 ase_worldNormal = IN.ase_texcoord3.xyz;
				float2 appendResult3_g106 = (float2(WorldPosition.x , WorldPosition.z));
				float temp_output_6_0_g106 = ( 1.0 / CZY_SnowScale );
				float simplePerlin2D7_g106 = snoise( appendResult3_g106*temp_output_6_0_g106 );
				simplePerlin2D7_g106 = simplePerlin2D7_g106*0.5 + 0.5;
				float time5_g106 = 0.0;
				float2 voronoiSmoothId5_g106 = 0;
				float2 coords5_g106 = appendResult3_g106 * ( temp_output_6_0_g106 / 0.1 );
				float2 id5_g106 = 0;
				float2 uv5_g106 = 0;
				float voroi5_g106 = voronoi5_g106( coords5_g106, time5_g106, id5_g106, uv5_g106, 0, voronoiSmoothId5_g106 );
				float4 appendResult55_g107 = (float4(_Metallic0 , _Metallic1 , _Metallic2 , _Metallic3));
				float localComputeMasks130_g107 = ( 0.0 );
				float4 masks0130_g107 = float4( 0,0,0,0 );
				float4 masks1130_g107 = float4( 0,0,0,0 );
				float4 masks2130_g107 = float4( 0,0,0,0 );
				float4 masks3130_g107 = float4( 0,0,0,0 );
				float4 appendResult135_g107 = (float4(_LayerHasMask0 , _LayerHasMask1 , _LayerHasMask2 , _LayerHasMask3));
				float4 layerHasMask136_g107 = appendResult135_g107;
				float4 hasMask130_g107 = layerHasMask136_g107;
				float2 uvMask0130_g107 = uv_Splat0;
				float2 uvMask1130_g107 = uv_Splat1;
				float2 uvMask2130_g107 = uv_Splat2;
				float2 uvMask3130_g107 = uv_Splat3;
				{
				masks0130_g107 = 0.5h;
				masks1130_g107 = 0.5h;
				masks2130_g107 = 0.5h;
				masks3130_g107 = 0.5h;
				#ifdef _MASKMAP
				masks0130_g107 = lerp(masks0130_g107, SAMPLE_TEXTURE2D(_Mask0, sampler_Mask0, uvMask0130_g107), hasMask130_g107.x);
				masks1130_g107 = lerp(masks1130_g107, SAMPLE_TEXTURE2D(_Mask1, sampler_Mask0, uvMask1130_g107), hasMask130_g107.y);
				masks2130_g107 = lerp(masks2130_g107, SAMPLE_TEXTURE2D(_Mask2, sampler_Mask0, uvMask2130_g107), hasMask130_g107.z);
				masks3130_g107 = lerp(masks3130_g107, SAMPLE_TEXTURE2D(_Mask3, sampler_Mask0, uvMask3130_g107), hasMask130_g107.w);
				#endif
				masks0130_g107 *= _MaskMapRemapScale0.rgba;
				masks0130_g107 += _MaskMapRemapOffset0.rgba;
				masks1130_g107 *= _MaskMapRemapScale1.rgba;
				masks1130_g107 += _MaskMapRemapOffset1.rgba;
				masks2130_g107 *= _MaskMapRemapScale2.rgba;
				masks2130_g107 += _MaskMapRemapOffset2.rgba;
				masks3130_g107 *= _MaskMapRemapScale3.rgba;
				masks3130_g107 += _MaskMapRemapOffset3.rgba;
				}
				float4 mask0138_g107 = masks0130_g107;
				float4 mask1139_g107 = masks1130_g107;
				float4 mask2140_g107 = masks2130_g107;
				float4 mask3141_g107 = masks3130_g107;
				float4 appendResult168_g107 = (float4((mask0138_g107).x , (mask1139_g107).x , (mask2140_g107).x , (mask3141_g107).x));
				float4 maskMetallic169_g107 = appendResult168_g107;
				float4 lerpResult202_g107 = lerp( appendResult55_g107 , maskMetallic169_g107 , layerHasMask136_g107);
				float dotResult53_g107 = dot( SplatControl26_g107 , lerpResult202_g107 );
				float4 lerpResult19_g106 = lerp( ( CZY_SnowColor * tex2D( CZY_SnowTexture, uvCZY_SnowTexture ) ) , localClipHoles100_g107 , ( ( pow( ( pow( ase_worldNormal.y , 7.0 ) * ( simplePerlin2D7_g106 * ( 1.0 - voroi5_g106 ) ) ) , 0.5 ) * dotResult53_g107 ) > ( 1.0 - CZY_SnowAmount ) ? 0.0 : 1.0 ));
				float4 lerpResult67 = lerp( _RockColor , lerpResult19_g106 , ( ( 1.0 - ase_worldNormal.y ) > _RockThreshold ? 0.0 : 1.0 ));
				
				
				float3 Albedo = lerpResult67.rgb;
				float3 Emission = 0;
				float Alpha = SplatWeight22_g107;
				float AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				MetaInput metaInput = (MetaInput)0;
				metaInput.Albedo = Albedo;
				metaInput.Emission = Emission;
				
				return MetaFragment(metaInput);
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "Universal2D"
			Tags { "LightMode"="Universal2D" }

			Blend One Zero, One Zero
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA

			HLSLPROGRAM
			
			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define _NORMALMAP 1
			#define ASE_SRP_VERSION 110000

			
			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS_2D

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#pragma multi_compile_local __ _ALPHATEST_ON
			#pragma shader_feature_local _MASKMAP


			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
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
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _RockColor;
			float4 _TerrainHolesTexture_ST;
			float4 _Specular3;
			float4 _Splat3_ST;
			float4 _Specular2;
			float4 _Specular1;
			float4 _Splat1_ST;
			float4 _Splat2_ST;
			float4 _Control_ST;
			float4 _Specular0;
			float4 _Splat0_ST;
			float4 CZY_SnowTexture_ST;
			float _Smoothness2;
			float _Metallic3;
			float _Smoothness3;
			float _Smoothness0;
			float _Metallic0;
			float _Metallic1;
			float _Metallic2;
			float _Smoothness1;
			float _RockThreshold;
			#ifdef _TRANSMISSION_ASE
				float _TransmissionShadow;
			#endif
			#ifdef _TRANSLUCENCY_ASE
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _Mask2;
			sampler2D _Mask0;
			sampler2D _Mask1;
			sampler2D _Mask3;
			float4 _MaskMapRemapOffset2;
			float4 _MaskMapRemapScale0;
			float4 _MaskMapRemapScale2;
			float4 _MaskMapRemapOffset1;
			float4 _MaskMapRemapOffset0;
			float4 _MaskMapRemapOffset3;
			float4 _MaskMapRemapScale1;
			float4 _MaskMapRemapScale3;
			float4 CZY_SnowColor;
			sampler2D CZY_SnowTexture;
			sampler2D _Splat0;
			sampler2D _Control;
			sampler2D _Splat1;
			sampler2D _Splat2;
			sampler2D _Splat3;
			sampler2D _TerrainHolesTexture;
			float CZY_SnowScale;
			float _LayerHasMask0;
			float _LayerHasMask1;
			float _LayerHasMask2;
			float _LayerHasMask3;
			float CZY_SnowAmount;


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
			
					float2 voronoihash5_g106( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi5_g106( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
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
						 		float2 o = voronoihash5_g106( n + g );
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
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

				float3 localCalculateTangentsSRP76_g107 = ( ( v.ase_tangent.xyz * v.ase_normal * 0.0 ) );
				{
				v.ase_tangent.xyz = cross ( v.ase_normal, float3( 0, 0, 1 ) );
				v.ase_tangent.w = -1;
				}
				
				float3 ase_worldNormal = TransformObjectToWorldNormal(v.ase_normal);
				o.ase_texcoord3.xyz = ase_worldNormal;
				
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.zw = 0;
				o.ase_texcoord3.w = 0;
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = localCalculateTangentsSRP76_g107;
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

				o.clipPos = positionCS;
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
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
				o.ase_tangent = v.ase_tangent;
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
				o.ase_tangent = patch[0].ase_tangent * bary.x + patch[1].ase_tangent * bary.y + patch[2].ase_tangent * bary.z;
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

				float2 uvCZY_SnowTexture = IN.ase_texcoord2.xy * CZY_SnowTexture_ST.xy + CZY_SnowTexture_ST.zw;
				float4 appendResult33_g107 = (float4(1.0 , 1.0 , 1.0 , _Smoothness0));
				float2 uv_Splat0 = IN.ase_texcoord2.xy * _Splat0_ST.xy + _Splat0_ST.zw;
				float4 tex2DNode4_g107 = tex2D( _Splat0, uv_Splat0 );
				float2 uv_Control = IN.ase_texcoord2.xy * _Control_ST.xy + _Control_ST.zw;
				float4 tex2DNode5_g107 = tex2D( _Control, uv_Control );
				float dotResult20_g107 = dot( tex2DNode5_g107 , float4(1,1,1,1) );
				float SplatWeight22_g107 = dotResult20_g107;
				float localSplatClip74_g107 = ( SplatWeight22_g107 );
				float SplatWeight74_g107 = SplatWeight22_g107;
				{
				#if !defined(SHADER_API_MOBILE) && defined(TERRAIN_SPLAT_ADDPASS)
				clip(SplatWeight74_g107 == 0.0f ? -1 : 1);
				#endif
				}
				float4 SplatControl26_g107 = ( tex2DNode5_g107 / ( localSplatClip74_g107 + 0.001 ) );
				float4 temp_output_59_0_g107 = SplatControl26_g107;
				float4 break20_g108 = temp_output_59_0_g107;
				float4 lerpResult16_g108 = lerp( float4( 0,0,0,0 ) , ( appendResult33_g107 * tex2DNode4_g107 * _Specular0 ) , ( break20_g108.r > max( max( break20_g108.g , break20_g108.b ) , break20_g108.a ) ? 1.0 : 0.0 ));
				float4 appendResult36_g107 = (float4(1.0 , 1.0 , 1.0 , _Smoothness1));
				float2 uv_Splat1 = IN.ase_texcoord2.xy * _Splat1_ST.xy + _Splat1_ST.zw;
				float4 tex2DNode3_g107 = tex2D( _Splat1, uv_Splat1 );
				float4 lerpResult14_g108 = lerp( lerpResult16_g108 , ( appendResult36_g107 * tex2DNode3_g107 * _Specular1 ) , ( break20_g108.g > max( max( break20_g108.r , break20_g108.b ) , break20_g108.a ) ? 1.0 : 0.0 ));
				float4 appendResult39_g107 = (float4(1.0 , 1.0 , 1.0 , _Smoothness2));
				float2 uv_Splat2 = IN.ase_texcoord2.xy * _Splat2_ST.xy + _Splat2_ST.zw;
				float4 tex2DNode6_g107 = tex2D( _Splat2, uv_Splat2 );
				float4 lerpResult8_g108 = lerp( lerpResult14_g108 , ( appendResult39_g107 * tex2DNode6_g107 * _Specular2 ) , ( break20_g108.b > max( max( break20_g108.r , break20_g108.g ) , break20_g108.a ) ? 1.0 : 0.0 ));
				float4 appendResult42_g107 = (float4(1.0 , 1.0 , 1.0 , _Smoothness3));
				float2 uv_Splat3 = IN.ase_texcoord2.xy * _Splat3_ST.xy + _Splat3_ST.zw;
				float4 tex2DNode7_g107 = tex2D( _Splat3, uv_Splat3 );
				float4 lerpResult13_g108 = lerp( lerpResult8_g108 , ( appendResult42_g107 * tex2DNode7_g107 * _Specular3 ) , ( break20_g108.a > max( max( break20_g108.r , break20_g108.g ) , break20_g108.b ) ? 1.0 : 0.0 ));
				float4 MixDiffuse28_g107 = saturate( lerpResult13_g108 );
				float4 temp_output_60_0_g107 = MixDiffuse28_g107;
				float4 localClipHoles100_g107 = ( temp_output_60_0_g107 );
				float2 uv_TerrainHolesTexture = IN.ase_texcoord2.xy * _TerrainHolesTexture_ST.xy + _TerrainHolesTexture_ST.zw;
				float holeClipValue99_g107 = tex2D( _TerrainHolesTexture, uv_TerrainHolesTexture ).r;
				float Hole100_g107 = holeClipValue99_g107;
				{
				#ifdef _ALPHATEST_ON
				clip(Hole100_g107 == 0.0f ? -1 : 1);
				#endif
				}
				float3 ase_worldNormal = IN.ase_texcoord3.xyz;
				float2 appendResult3_g106 = (float2(WorldPosition.x , WorldPosition.z));
				float temp_output_6_0_g106 = ( 1.0 / CZY_SnowScale );
				float simplePerlin2D7_g106 = snoise( appendResult3_g106*temp_output_6_0_g106 );
				simplePerlin2D7_g106 = simplePerlin2D7_g106*0.5 + 0.5;
				float time5_g106 = 0.0;
				float2 voronoiSmoothId5_g106 = 0;
				float2 coords5_g106 = appendResult3_g106 * ( temp_output_6_0_g106 / 0.1 );
				float2 id5_g106 = 0;
				float2 uv5_g106 = 0;
				float voroi5_g106 = voronoi5_g106( coords5_g106, time5_g106, id5_g106, uv5_g106, 0, voronoiSmoothId5_g106 );
				float4 appendResult55_g107 = (float4(_Metallic0 , _Metallic1 , _Metallic2 , _Metallic3));
				float localComputeMasks130_g107 = ( 0.0 );
				float4 masks0130_g107 = float4( 0,0,0,0 );
				float4 masks1130_g107 = float4( 0,0,0,0 );
				float4 masks2130_g107 = float4( 0,0,0,0 );
				float4 masks3130_g107 = float4( 0,0,0,0 );
				float4 appendResult135_g107 = (float4(_LayerHasMask0 , _LayerHasMask1 , _LayerHasMask2 , _LayerHasMask3));
				float4 layerHasMask136_g107 = appendResult135_g107;
				float4 hasMask130_g107 = layerHasMask136_g107;
				float2 uvMask0130_g107 = uv_Splat0;
				float2 uvMask1130_g107 = uv_Splat1;
				float2 uvMask2130_g107 = uv_Splat2;
				float2 uvMask3130_g107 = uv_Splat3;
				{
				masks0130_g107 = 0.5h;
				masks1130_g107 = 0.5h;
				masks2130_g107 = 0.5h;
				masks3130_g107 = 0.5h;
				#ifdef _MASKMAP
				masks0130_g107 = lerp(masks0130_g107, SAMPLE_TEXTURE2D(_Mask0, sampler_Mask0, uvMask0130_g107), hasMask130_g107.x);
				masks1130_g107 = lerp(masks1130_g107, SAMPLE_TEXTURE2D(_Mask1, sampler_Mask0, uvMask1130_g107), hasMask130_g107.y);
				masks2130_g107 = lerp(masks2130_g107, SAMPLE_TEXTURE2D(_Mask2, sampler_Mask0, uvMask2130_g107), hasMask130_g107.z);
				masks3130_g107 = lerp(masks3130_g107, SAMPLE_TEXTURE2D(_Mask3, sampler_Mask0, uvMask3130_g107), hasMask130_g107.w);
				#endif
				masks0130_g107 *= _MaskMapRemapScale0.rgba;
				masks0130_g107 += _MaskMapRemapOffset0.rgba;
				masks1130_g107 *= _MaskMapRemapScale1.rgba;
				masks1130_g107 += _MaskMapRemapOffset1.rgba;
				masks2130_g107 *= _MaskMapRemapScale2.rgba;
				masks2130_g107 += _MaskMapRemapOffset2.rgba;
				masks3130_g107 *= _MaskMapRemapScale3.rgba;
				masks3130_g107 += _MaskMapRemapOffset3.rgba;
				}
				float4 mask0138_g107 = masks0130_g107;
				float4 mask1139_g107 = masks1130_g107;
				float4 mask2140_g107 = masks2130_g107;
				float4 mask3141_g107 = masks3130_g107;
				float4 appendResult168_g107 = (float4((mask0138_g107).x , (mask1139_g107).x , (mask2140_g107).x , (mask3141_g107).x));
				float4 maskMetallic169_g107 = appendResult168_g107;
				float4 lerpResult202_g107 = lerp( appendResult55_g107 , maskMetallic169_g107 , layerHasMask136_g107);
				float dotResult53_g107 = dot( SplatControl26_g107 , lerpResult202_g107 );
				float4 lerpResult19_g106 = lerp( ( CZY_SnowColor * tex2D( CZY_SnowTexture, uvCZY_SnowTexture ) ) , localClipHoles100_g107 , ( ( pow( ( pow( ase_worldNormal.y , 7.0 ) * ( simplePerlin2D7_g106 * ( 1.0 - voroi5_g106 ) ) ) , 0.5 ) * dotResult53_g107 ) > ( 1.0 - CZY_SnowAmount ) ? 0.0 : 1.0 ));
				float4 lerpResult67 = lerp( _RockColor , lerpResult19_g106 , ( ( 1.0 - ase_worldNormal.y ) > _RockThreshold ? 0.0 : 1.0 ));
				
				
				float3 Albedo = lerpResult67.rgb;
				float Alpha = SplatWeight22_g107;
				float AlphaClipThreshold = 0.5;

				half4 color = half4( Albedo, Alpha );

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				return color;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthNormals"
			Tags { "LightMode"="DepthNormals" }

			ZWrite On
			Blend One Zero
            ZTest LEqual
            ZWrite On

			HLSLPROGRAM
			
			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define _NORMALMAP 1
			#define ASE_SRP_VERSION 110000

			
			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS_DEPTHNORMALSONLY

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#define ASE_NEEDS_VERT_NORMAL
			#pragma multi_compile_local __ _ALPHATEST_ON
			#pragma shader_feature_local _MASKMAP


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
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
				float3 worldNormal : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _RockColor;
			float4 _TerrainHolesTexture_ST;
			float4 _Specular3;
			float4 _Splat3_ST;
			float4 _Specular2;
			float4 _Specular1;
			float4 _Splat1_ST;
			float4 _Splat2_ST;
			float4 _Control_ST;
			float4 _Specular0;
			float4 _Splat0_ST;
			float4 CZY_SnowTexture_ST;
			float _Smoothness2;
			float _Metallic3;
			float _Smoothness3;
			float _Smoothness0;
			float _Metallic0;
			float _Metallic1;
			float _Metallic2;
			float _Smoothness1;
			float _RockThreshold;
			#ifdef _TRANSMISSION_ASE
				float _TransmissionShadow;
			#endif
			#ifdef _TRANSLUCENCY_ASE
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _Mask2;
			sampler2D _Mask0;
			sampler2D _Mask1;
			sampler2D _Mask3;
			float4 _MaskMapRemapOffset2;
			float4 _MaskMapRemapScale0;
			float4 _MaskMapRemapScale2;
			float4 _MaskMapRemapOffset1;
			float4 _MaskMapRemapOffset0;
			float4 _MaskMapRemapOffset3;
			float4 _MaskMapRemapScale1;
			float4 _MaskMapRemapScale3;
			sampler2D _Control;


			
			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 localCalculateTangentsSRP76_g107 = ( ( v.ase_tangent.xyz * v.ase_normal * 0.0 ) );
				{
				v.ase_tangent.xyz = cross ( v.ase_normal, float3( 0, 0, 1 ) );
				v.ase_tangent.w = -1;
				}
				
				o.ase_texcoord3.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord3.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = localCalculateTangentsSRP76_g107;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;
				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float3 normalWS = TransformObjectToWorldNormal( v.ase_normal );
				float4 positionCS = TransformWorldToHClip( positionWS );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif

				o.worldNormal = normalWS;

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = positionCS;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				o.clipPos = positionCS;
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
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
				o.ase_tangent = v.ase_tangent;
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
				o.ase_tangent = patch[0].ase_tangent * bary.x + patch[1].ase_tangent * bary.y + patch[2].ase_tangent * bary.z;
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

			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE)
				#define ASE_SV_DEPTH SV_DepthLessEqual  
			#else
				#define ASE_SV_DEPTH SV_Depth
			#endif
			half4 frag(	VertexOutput IN 
						#ifdef ASE_DEPTH_WRITE_ON
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						 ) : SV_TARGET
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

				float2 uv_Control = IN.ase_texcoord3.xy * _Control_ST.xy + _Control_ST.zw;
				float4 tex2DNode5_g107 = tex2D( _Control, uv_Control );
				float dotResult20_g107 = dot( tex2DNode5_g107 , float4(1,1,1,1) );
				float SplatWeight22_g107 = dotResult20_g107;
				
				float Alpha = SplatWeight22_g107;
				float AlphaClipThreshold = 0.5;
				#ifdef ASE_DEPTH_WRITE_ON
				float DepthValue = 0;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif
				
				#ifdef ASE_DEPTH_WRITE_ON
				outputDepth = DepthValue;
				#endif
				
				return float4(PackNormalOctRectEncode(TransformWorldToViewDir(IN.worldNormal, true)), 0.0, 0.0);
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "GBuffer"
			Tags { "LightMode"="UniversalGBuffer" }
			
			Blend One Zero, One Zero
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA
			

			HLSLPROGRAM
			
			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define _NORMALMAP 1
			#define ASE_SRP_VERSION 110000

			
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
			#pragma multi_compile _ _GBUFFER_NORMALS_OCT
			
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS SHADERPASS_GBUFFER

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

			#if ASE_SRP_VERSION <= 70108
			#define REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
			#endif

			#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
			    #define ENABLE_TERRAIN_PERPIXEL_NORMAL
			#endif

			#define ASE_NEEDS_VERT_TANGENT
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#pragma multi_compile_local __ _ALPHATEST_ON
			#pragma shader_feature_local _MASKMAP


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord : TEXCOORD0;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float4 lightmapUVOrVertexSH : TEXCOORD0;
				half4 fogFactorAndVertexLight : TEXCOORD1;
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				float4 shadowCoord : TEXCOORD2;
				#endif
				float4 tSpace0 : TEXCOORD3;
				float4 tSpace1 : TEXCOORD4;
				float4 tSpace2 : TEXCOORD5;
				#if defined(ASE_NEEDS_FRAG_SCREEN_POSITION)
				float4 screenPos : TEXCOORD6;
				#endif
				float4 ase_texcoord7 : TEXCOORD7;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _RockColor;
			float4 _TerrainHolesTexture_ST;
			float4 _Specular3;
			float4 _Splat3_ST;
			float4 _Specular2;
			float4 _Specular1;
			float4 _Splat1_ST;
			float4 _Splat2_ST;
			float4 _Control_ST;
			float4 _Specular0;
			float4 _Splat0_ST;
			float4 CZY_SnowTexture_ST;
			float _Smoothness2;
			float _Metallic3;
			float _Smoothness3;
			float _Smoothness0;
			float _Metallic0;
			float _Metallic1;
			float _Metallic2;
			float _Smoothness1;
			float _RockThreshold;
			#ifdef _TRANSMISSION_ASE
				float _TransmissionShadow;
			#endif
			#ifdef _TRANSLUCENCY_ASE
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _Mask2;
			sampler2D _Mask0;
			sampler2D _Mask1;
			sampler2D _Mask3;
			float4 _MaskMapRemapOffset2;
			float4 _MaskMapRemapScale0;
			float4 _MaskMapRemapScale2;
			float4 _MaskMapRemapOffset1;
			float4 _MaskMapRemapOffset0;
			float4 _MaskMapRemapOffset3;
			float4 _MaskMapRemapScale1;
			float4 _MaskMapRemapScale3;
			float4 CZY_SnowColor;
			sampler2D CZY_SnowTexture;
			sampler2D _Splat0;
			sampler2D _Control;
			sampler2D _Splat1;
			sampler2D _Splat2;
			sampler2D _Splat3;
			sampler2D _TerrainHolesTexture;
			float CZY_SnowScale;
			float _LayerHasMask0;
			float _LayerHasMask1;
			float _LayerHasMask2;
			float _LayerHasMask3;
			float CZY_SnowAmount;
			sampler2D _Normal0;
			sampler2D _Normal1;
			sampler2D _Normal2;
			sampler2D _Normal3;
			float CZY_PuddleScale;
			float CZY_WetnessAmount;


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
			
					float2 voronoihash5_g106( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi5_g106( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
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
						 		float2 o = voronoihash5_g106( n + g );
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
			
					float2 voronoihash1_g105( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi1_g105( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
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
						 		float2 o = voronoihash1_g105( n + g );
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
			
					float2 voronoihash8_g105( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi8_g105( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
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
						 		float2 o = voronoihash8_g105( n + g );
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

				float3 localCalculateTangentsSRP76_g107 = ( ( v.ase_tangent.xyz * v.ase_normal * 0.0 ) );
				{
				v.ase_tangent.xyz = cross ( v.ase_normal, float3( 0, 0, 1 ) );
				v.ase_tangent.w = -1;
				}
				
				o.ase_texcoord7.xy = v.texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord7.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = localCalculateTangentsSRP76_g107;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float3 positionVS = TransformWorldToView( positionWS );
				float4 positionCS = TransformWorldToHClip( positionWS );

				VertexNormalInputs normalInput = GetVertexNormalInputs( v.ase_normal, v.ase_tangent );

				o.tSpace0 = float4( normalInput.normalWS, positionWS.x);
				o.tSpace1 = float4( normalInput.tangentWS, positionWS.y);
				o.tSpace2 = float4( normalInput.bitangentWS, positionWS.z);

				OUTPUT_LIGHTMAP_UV( v.texcoord1, unity_LightmapST, o.lightmapUVOrVertexSH.xy );
				OUTPUT_SH( normalInput.normalWS.xyz, o.lightmapUVOrVertexSH.xyz );

				#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
					o.lightmapUVOrVertexSH.zw = v.texcoord;
					o.lightmapUVOrVertexSH.xy = v.texcoord * unity_LightmapST.xy + unity_LightmapST.zw;
				#endif

				half3 vertexLight = VertexLighting( positionWS, normalInput.normalWS );
				#ifdef ASE_FOG
					half fogFactor = ComputeFogFactor( positionCS.z );
				#else
					half fogFactor = 0;
				#endif
				o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
				
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				VertexPositionInputs vertexInput = (VertexPositionInputs)0;
				vertexInput.positionWS = positionWS;
				vertexInput.positionCS = positionCS;
				o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				
				o.clipPos = positionCS;
				#if defined(ASE_NEEDS_FRAG_SCREEN_POSITION)
				o.screenPos = ComputeScreenPos(positionCS);
				#endif
				return o;
			}
			
			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				
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
				o.ase_tangent = v.ase_tangent;
				o.texcoord = v.texcoord;
				o.texcoord1 = v.texcoord1;
				
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
				o.ase_tangent = patch[0].ase_tangent * bary.x + patch[1].ase_tangent * bary.y + patch[2].ase_tangent * bary.z;
				o.texcoord = patch[0].texcoord * bary.x + patch[1].texcoord * bary.y + patch[2].texcoord * bary.z;
				o.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
				
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

			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE)
				#define ASE_SV_DEPTH SV_DepthLessEqual  
			#else
				#define ASE_SV_DEPTH SV_Depth
			#endif
			FragmentOutput frag ( VertexOutput IN 
								#ifdef ASE_DEPTH_WRITE_ON
								,out float outputDepth : ASE_SV_DEPTH
								#endif
								 )
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
					float2 sampleCoords = (IN.lightmapUVOrVertexSH.zw / _TerrainHeightmapRecipSize.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
					float3 WorldNormal = TransformObjectToWorldNormal(normalize(SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb * 2 - 1));
					float3 WorldTangent = -cross(GetObjectToWorldMatrix()._13_23_33, WorldNormal);
					float3 WorldBiTangent = cross(WorldNormal, -WorldTangent);
				#else
					float3 WorldNormal = normalize( IN.tSpace0.xyz );
					float3 WorldTangent = IN.tSpace1.xyz;
					float3 WorldBiTangent = IN.tSpace2.xyz;
				#endif
				float3 WorldPosition = float3(IN.tSpace0.w,IN.tSpace1.w,IN.tSpace2.w);
				float3 WorldViewDirection = _WorldSpaceCameraPos.xyz  - WorldPosition;
				float4 ShadowCoords = float4( 0, 0, 0, 0 );
				#if defined(ASE_NEEDS_FRAG_SCREEN_POSITION)
				float4 ScreenPos = IN.screenPos;
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					ShadowCoords = IN.shadowCoord;
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
				#endif
	
				WorldViewDirection = SafeNormalize( WorldViewDirection );

				float2 uvCZY_SnowTexture = IN.ase_texcoord7.xy * CZY_SnowTexture_ST.xy + CZY_SnowTexture_ST.zw;
				float4 appendResult33_g107 = (float4(1.0 , 1.0 , 1.0 , _Smoothness0));
				float2 uv_Splat0 = IN.ase_texcoord7.xy * _Splat0_ST.xy + _Splat0_ST.zw;
				float4 tex2DNode4_g107 = tex2D( _Splat0, uv_Splat0 );
				float2 uv_Control = IN.ase_texcoord7.xy * _Control_ST.xy + _Control_ST.zw;
				float4 tex2DNode5_g107 = tex2D( _Control, uv_Control );
				float dotResult20_g107 = dot( tex2DNode5_g107 , float4(1,1,1,1) );
				float SplatWeight22_g107 = dotResult20_g107;
				float localSplatClip74_g107 = ( SplatWeight22_g107 );
				float SplatWeight74_g107 = SplatWeight22_g107;
				{
				#if !defined(SHADER_API_MOBILE) && defined(TERRAIN_SPLAT_ADDPASS)
				clip(SplatWeight74_g107 == 0.0f ? -1 : 1);
				#endif
				}
				float4 SplatControl26_g107 = ( tex2DNode5_g107 / ( localSplatClip74_g107 + 0.001 ) );
				float4 temp_output_59_0_g107 = SplatControl26_g107;
				float4 break20_g108 = temp_output_59_0_g107;
				float4 lerpResult16_g108 = lerp( float4( 0,0,0,0 ) , ( appendResult33_g107 * tex2DNode4_g107 * _Specular0 ) , ( break20_g108.r > max( max( break20_g108.g , break20_g108.b ) , break20_g108.a ) ? 1.0 : 0.0 ));
				float4 appendResult36_g107 = (float4(1.0 , 1.0 , 1.0 , _Smoothness1));
				float2 uv_Splat1 = IN.ase_texcoord7.xy * _Splat1_ST.xy + _Splat1_ST.zw;
				float4 tex2DNode3_g107 = tex2D( _Splat1, uv_Splat1 );
				float4 lerpResult14_g108 = lerp( lerpResult16_g108 , ( appendResult36_g107 * tex2DNode3_g107 * _Specular1 ) , ( break20_g108.g > max( max( break20_g108.r , break20_g108.b ) , break20_g108.a ) ? 1.0 : 0.0 ));
				float4 appendResult39_g107 = (float4(1.0 , 1.0 , 1.0 , _Smoothness2));
				float2 uv_Splat2 = IN.ase_texcoord7.xy * _Splat2_ST.xy + _Splat2_ST.zw;
				float4 tex2DNode6_g107 = tex2D( _Splat2, uv_Splat2 );
				float4 lerpResult8_g108 = lerp( lerpResult14_g108 , ( appendResult39_g107 * tex2DNode6_g107 * _Specular2 ) , ( break20_g108.b > max( max( break20_g108.r , break20_g108.g ) , break20_g108.a ) ? 1.0 : 0.0 ));
				float4 appendResult42_g107 = (float4(1.0 , 1.0 , 1.0 , _Smoothness3));
				float2 uv_Splat3 = IN.ase_texcoord7.xy * _Splat3_ST.xy + _Splat3_ST.zw;
				float4 tex2DNode7_g107 = tex2D( _Splat3, uv_Splat3 );
				float4 lerpResult13_g108 = lerp( lerpResult8_g108 , ( appendResult42_g107 * tex2DNode7_g107 * _Specular3 ) , ( break20_g108.a > max( max( break20_g108.r , break20_g108.g ) , break20_g108.b ) ? 1.0 : 0.0 ));
				float4 MixDiffuse28_g107 = saturate( lerpResult13_g108 );
				float4 temp_output_60_0_g107 = MixDiffuse28_g107;
				float4 localClipHoles100_g107 = ( temp_output_60_0_g107 );
				float2 uv_TerrainHolesTexture = IN.ase_texcoord7.xy * _TerrainHolesTexture_ST.xy + _TerrainHolesTexture_ST.zw;
				float holeClipValue99_g107 = tex2D( _TerrainHolesTexture, uv_TerrainHolesTexture ).r;
				float Hole100_g107 = holeClipValue99_g107;
				{
				#ifdef _ALPHATEST_ON
				clip(Hole100_g107 == 0.0f ? -1 : 1);
				#endif
				}
				float2 appendResult3_g106 = (float2(WorldPosition.x , WorldPosition.z));
				float temp_output_6_0_g106 = ( 1.0 / CZY_SnowScale );
				float simplePerlin2D7_g106 = snoise( appendResult3_g106*temp_output_6_0_g106 );
				simplePerlin2D7_g106 = simplePerlin2D7_g106*0.5 + 0.5;
				float time5_g106 = 0.0;
				float2 voronoiSmoothId5_g106 = 0;
				float2 coords5_g106 = appendResult3_g106 * ( temp_output_6_0_g106 / 0.1 );
				float2 id5_g106 = 0;
				float2 uv5_g106 = 0;
				float voroi5_g106 = voronoi5_g106( coords5_g106, time5_g106, id5_g106, uv5_g106, 0, voronoiSmoothId5_g106 );
				float4 appendResult55_g107 = (float4(_Metallic0 , _Metallic1 , _Metallic2 , _Metallic3));
				float localComputeMasks130_g107 = ( 0.0 );
				float4 masks0130_g107 = float4( 0,0,0,0 );
				float4 masks1130_g107 = float4( 0,0,0,0 );
				float4 masks2130_g107 = float4( 0,0,0,0 );
				float4 masks3130_g107 = float4( 0,0,0,0 );
				float4 appendResult135_g107 = (float4(_LayerHasMask0 , _LayerHasMask1 , _LayerHasMask2 , _LayerHasMask3));
				float4 layerHasMask136_g107 = appendResult135_g107;
				float4 hasMask130_g107 = layerHasMask136_g107;
				float2 uvMask0130_g107 = uv_Splat0;
				float2 uvMask1130_g107 = uv_Splat1;
				float2 uvMask2130_g107 = uv_Splat2;
				float2 uvMask3130_g107 = uv_Splat3;
				{
				masks0130_g107 = 0.5h;
				masks1130_g107 = 0.5h;
				masks2130_g107 = 0.5h;
				masks3130_g107 = 0.5h;
				#ifdef _MASKMAP
				masks0130_g107 = lerp(masks0130_g107, SAMPLE_TEXTURE2D(_Mask0, sampler_Mask0, uvMask0130_g107), hasMask130_g107.x);
				masks1130_g107 = lerp(masks1130_g107, SAMPLE_TEXTURE2D(_Mask1, sampler_Mask0, uvMask1130_g107), hasMask130_g107.y);
				masks2130_g107 = lerp(masks2130_g107, SAMPLE_TEXTURE2D(_Mask2, sampler_Mask0, uvMask2130_g107), hasMask130_g107.z);
				masks3130_g107 = lerp(masks3130_g107, SAMPLE_TEXTURE2D(_Mask3, sampler_Mask0, uvMask3130_g107), hasMask130_g107.w);
				#endif
				masks0130_g107 *= _MaskMapRemapScale0.rgba;
				masks0130_g107 += _MaskMapRemapOffset0.rgba;
				masks1130_g107 *= _MaskMapRemapScale1.rgba;
				masks1130_g107 += _MaskMapRemapOffset1.rgba;
				masks2130_g107 *= _MaskMapRemapScale2.rgba;
				masks2130_g107 += _MaskMapRemapOffset2.rgba;
				masks3130_g107 *= _MaskMapRemapScale3.rgba;
				masks3130_g107 += _MaskMapRemapOffset3.rgba;
				}
				float4 mask0138_g107 = masks0130_g107;
				float4 mask1139_g107 = masks1130_g107;
				float4 mask2140_g107 = masks2130_g107;
				float4 mask3141_g107 = masks3130_g107;
				float4 appendResult168_g107 = (float4((mask0138_g107).x , (mask1139_g107).x , (mask2140_g107).x , (mask3141_g107).x));
				float4 maskMetallic169_g107 = appendResult168_g107;
				float4 lerpResult202_g107 = lerp( appendResult55_g107 , maskMetallic169_g107 , layerHasMask136_g107);
				float dotResult53_g107 = dot( SplatControl26_g107 , lerpResult202_g107 );
				float4 lerpResult19_g106 = lerp( ( CZY_SnowColor * tex2D( CZY_SnowTexture, uvCZY_SnowTexture ) ) , localClipHoles100_g107 , ( ( pow( ( pow( WorldNormal.y , 7.0 ) * ( simplePerlin2D7_g106 * ( 1.0 - voroi5_g106 ) ) ) , 0.5 ) * dotResult53_g107 ) > ( 1.0 - CZY_SnowAmount ) ? 0.0 : 1.0 ));
				float4 lerpResult67 = lerp( _RockColor , lerpResult19_g106 , ( ( 1.0 - WorldNormal.y ) > _RockThreshold ? 0.0 : 1.0 ));
				
				float4 break20_g109 = temp_output_59_0_g107;
				float4 lerpResult16_g109 = lerp( float4( 0,0,0,0 ) , tex2D( _Normal0, uv_Splat0 ) , ( break20_g109.r > max( max( break20_g109.g , break20_g109.b ) , break20_g109.a ) ? 1.0 : 0.0 ));
				float4 lerpResult14_g109 = lerp( lerpResult16_g109 , tex2D( _Normal1, uv_Splat1 ) , ( break20_g109.g > max( max( break20_g109.r , break20_g109.b ) , break20_g109.a ) ? 1.0 : 0.0 ));
				float4 lerpResult8_g109 = lerp( lerpResult14_g109 , tex2D( _Normal2, uv_Splat2 ) , ( break20_g109.b > max( max( break20_g109.r , break20_g109.g ) , break20_g109.a ) ? 1.0 : 0.0 ));
				float4 lerpResult13_g109 = lerp( lerpResult8_g109 , tex2D( _Normal3, uv_Splat3 ) , ( break20_g109.a > max( max( break20_g109.r , break20_g109.g ) , break20_g109.b ) ? 1.0 : 0.0 ));
				float3 temp_output_61_0_g107 = UnpackNormalScale( saturate( lerpResult13_g109 ), 1.0 );
				
				float temp_output_5_0_g105 = ( 1.0 / CZY_PuddleScale );
				float time1_g105 = 0.0;
				float2 voronoiSmoothId1_g105 = 0;
				float2 appendResult3_g105 = (float2(WorldPosition.x , WorldPosition.z));
				float2 coords1_g105 = appendResult3_g105 * temp_output_5_0_g105;
				float2 id1_g105 = 0;
				float2 uv1_g105 = 0;
				float voroi1_g105 = voronoi1_g105( coords1_g105, time1_g105, id1_g105, uv1_g105, 0, voronoiSmoothId1_g105 );
				float time8_g105 = 2.16;
				float2 voronoiSmoothId8_g105 = 0;
				float2 coords8_g105 = IN.ase_texcoord7.xy * ( temp_output_5_0_g105 * 3.0 );
				float2 id8_g105 = 0;
				float2 uv8_g105 = 0;
				float voroi8_g105 = voronoi8_g105( coords8_g105, time8_g105, id8_g105, uv8_g105, 0, voronoiSmoothId8_g105 );
				float4 appendResult205_g107 = (float4(_Smoothness0 , _Smoothness1 , _Smoothness2 , _Smoothness3));
				float4 appendResult206_g107 = (float4(tex2DNode4_g107.a , tex2DNode3_g107.a , tex2DNode6_g107.a , tex2DNode7_g107.a));
				float4 defaultSmoothness210_g107 = ( appendResult205_g107 * appendResult206_g107 );
				float4 appendResult158_g107 = (float4((mask0138_g107).w , (mask1139_g107).w , (mask2140_g107).w , (mask3141_g107).w));
				float4 maskSmoothness149_g107 = appendResult158_g107;
				float4 lerpResult215_g107 = lerp( defaultSmoothness210_g107 , maskSmoothness149_g107 , layerHasMask136_g107);
				float dotResult216_g107 = dot( lerpResult215_g107 , SplatControl26_g107 );
				
				float4 appendResult188_g107 = (float4(_MaskMapRemapOffset0.y , _MaskMapRemapOffset1.y , _MaskMapRemapOffset2.y , _MaskMapRemapOffset3.y));
				float4 appendResult189_g107 = (float4(_MaskMapRemapScale0.y , _MaskMapRemapScale1.y , _MaskMapRemapScale2.y , _MaskMapRemapScale3.y));
				float4 defaultOcclusion191_g107 = ( appendResult188_g107 + appendResult189_g107 );
				float4 appendResult180_g107 = (float4((mask0138_g107).y , (mask1139_g107).y , (mask2140_g107).y , (mask3141_g107).y));
				float4 maskOcclusion181_g107 = appendResult180_g107;
				float4 lerpResult192_g107 = lerp( defaultOcclusion191_g107 , maskOcclusion181_g107 , layerHasMask136_g107);
				float dotResult197_g107 = dot( lerpResult192_g107 , SplatControl26_g107 );
				
				float3 Albedo = lerpResult67.rgb;
				float3 Normal = temp_output_61_0_g107;
				float3 Emission = 0;
				float3 Specular = 0.5;
				float Metallic = 0;
				float Smoothness = ( ( WorldNormal.y * 2.0 * ( (1.0 + (voroi1_g105 - 0.0) * (0.0 - 1.0) / (0.4 - 0.0)) + (0.1 + (voroi8_g105 - 0.0) * (-0.3 - 0.1) / (0.21 - 0.0)) ) * (0.3 + (CZY_WetnessAmount - 0.0) * (1.0 - 0.3) / (1.0 - 0.0)) ) > ( 1.0 - ( CZY_WetnessAmount * dotResult216_g107 ) ) ? 1.0 : 0.0 );
				float Occlusion = dotResult197_g107;
				float Alpha = SplatWeight22_g107;
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;
				float3 BakedGI = 0;
				float3 RefractionColor = 1;
				float RefractionIndex = 1;
				float3 Transmission = 1;
				float3 Translucency = 1;
				#ifdef ASE_DEPTH_WRITE_ON
				float DepthValue = 0;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				InputData inputData;
				inputData.positionWS = WorldPosition;
				inputData.viewDirectionWS = WorldViewDirection;
				inputData.shadowCoord = ShadowCoords;

				#ifdef _NORMALMAP
					#if _NORMAL_DROPOFF_TS
					inputData.normalWS = TransformTangentToWorld(Normal, half3x3( WorldTangent, WorldBiTangent, WorldNormal ));
					#elif _NORMAL_DROPOFF_OS
					inputData.normalWS = TransformObjectToWorldNormal(Normal);
					#elif _NORMAL_DROPOFF_WS
					inputData.normalWS = Normal;
					#endif
					inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
				#else
					inputData.normalWS = WorldNormal;
				#endif

				#ifdef ASE_FOG
					inputData.fogCoord = IN.fogFactorAndVertexLight.x;
				#endif

				inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
				#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
					float3 SH = SampleSH(inputData.normalWS.xyz);
				#else
					float3 SH = IN.lightmapUVOrVertexSH.xyz;
				#endif

				inputData.bakedGI = SAMPLE_GI( IN.lightmapUVOrVertexSH.xy, SH, inputData.normalWS );
				#ifdef _ASE_BAKEDGI
					inputData.bakedGI = BakedGI;
				#endif

				BRDFData brdfData;
				InitializeBRDFData( Albedo, Metallic, Specular, Smoothness, Alpha, brdfData);
				half4 color;
				color.rgb = GlobalIllumination( brdfData, inputData.bakedGI, Occlusion, inputData.normalWS, inputData.viewDirectionWS);
				color.a = Alpha;

				#ifdef _TRANSMISSION_ASE
				{
					float shadow = _TransmissionShadow;
				
					Light mainLight = GetMainLight( inputData.shadowCoord );
					float3 mainAtten = mainLight.color * mainLight.distanceAttenuation;
					mainAtten = lerp( mainAtten, mainAtten * mainLight.shadowAttenuation, shadow );
					half3 mainTransmission = max(0 , -dot(inputData.normalWS, mainLight.direction)) * mainAtten * Transmission;
					color.rgb += Albedo * mainTransmission;
				
					#ifdef _ADDITIONAL_LIGHTS
						int transPixelLightCount = GetAdditionalLightsCount();
						for (int i = 0; i < transPixelLightCount; ++i)
						{
							Light light = GetAdditionalLight(i, inputData.positionWS);
							float3 atten = light.color * light.distanceAttenuation;
							atten = lerp( atten, atten * light.shadowAttenuation, shadow );
				
							half3 transmission = max(0 , -dot(inputData.normalWS, light.direction)) * atten * Transmission;
							color.rgb += Albedo * transmission;
						}
					#endif
				}
				#endif
				
				#ifdef _TRANSLUCENCY_ASE
				{
					float shadow = _TransShadow;
					float normal = _TransNormal;
					float scattering = _TransScattering;
					float direct = _TransDirect;
					float ambient = _TransAmbient;
					float strength = _TransStrength;
				
					Light mainLight = GetMainLight( inputData.shadowCoord );
					float3 mainAtten = mainLight.color * mainLight.distanceAttenuation;
					mainAtten = lerp( mainAtten, mainAtten * mainLight.shadowAttenuation, shadow );
				
					half3 mainLightDir = mainLight.direction + inputData.normalWS * normal;
					half mainVdotL = pow( saturate( dot( inputData.viewDirectionWS, -mainLightDir ) ), scattering );
					half3 mainTranslucency = mainAtten * ( mainVdotL * direct + inputData.bakedGI * ambient ) * Translucency;
					color.rgb += Albedo * mainTranslucency * strength;
				
					#ifdef _ADDITIONAL_LIGHTS
						int transPixelLightCount = GetAdditionalLightsCount();
						for (int i = 0; i < transPixelLightCount; ++i)
						{
							Light light = GetAdditionalLight(i, inputData.positionWS);
							float3 atten = light.color * light.distanceAttenuation;
							atten = lerp( atten, atten * light.shadowAttenuation, shadow );
				
							half3 lightDir = light.direction + inputData.normalWS * normal;
							half VdotL = pow( saturate( dot( inputData.viewDirectionWS, -lightDir ) ), scattering );
							half3 translucency = atten * ( VdotL * direct + inputData.bakedGI * ambient ) * Translucency;
							color.rgb += Albedo * translucency * strength;
						}
					#endif
				}
				#endif
				
				#ifdef _REFRACTION_ASE
					float4 projScreenPos = ScreenPos / ScreenPos.w;
					float3 refractionOffset = ( RefractionIndex - 1.0 ) * mul( UNITY_MATRIX_V, float4( WorldNormal, 0 ) ).xyz * ( 1.0 - dot( WorldNormal, WorldViewDirection ) );
					projScreenPos.xy += refractionOffset.xy;
					float3 refraction = SHADERGRAPH_SAMPLE_SCENE_COLOR( projScreenPos.xy ) * RefractionColor;
					color.rgb = lerp( refraction, color.rgb, color.a );
					color.a = 1;
				#endif
				
				#ifdef ASE_FINAL_COLOR_ALPHA_MULTIPLY
					color.rgb *= color.a;
				#endif
				
				#ifdef ASE_FOG
					#ifdef TERRAIN_SPLAT_ADDPASS
						color.rgb = MixFogColor(color.rgb, half3( 0, 0, 0 ), IN.fogFactorAndVertexLight.x );
					#else
						color.rgb = MixFog(color.rgb, IN.fogFactorAndVertexLight.x);
					#endif
				#endif
				
				#ifdef ASE_DEPTH_WRITE_ON
					outputDepth = DepthValue;
				#endif
				
				return BRDFDataToGbuffer(brdfData, inputData, Smoothness, Emission + color.rgb);
			}

			ENDHLSL
		}
		
	}
	
	CustomEditor "UnityEditor.ShaderGraphLitGUI"
	Fallback "Standard"
	
}
/*ASEBEGIN
Version=18935
0;1080;2194.286;607.5715;1000.619;133.4594;1;True;False
Node;AmplifyShaderEditor.WorldNormalVector;62;-523.6168,-99.02313;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.OneMinusNode;64;-250.0165,-66.12317;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;63;-273.4164,33.97681;Inherit;False;Property;_RockThreshold;Rock Threshold;34;0;Create;True;0;0;0;False;0;False;0;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;162;-490.9626,239.8136;Inherit;False;Stylized Terrain Blend;0;;107;9d4ad2a2d80f7ca4d90e641385d406e3;2,102,1,85,0;7;59;FLOAT4;0,0,0,0;False;60;FLOAT4;0,0,0,0;False;61;FLOAT3;0,0,0;False;57;FLOAT;0;False;58;FLOAT;0;False;201;FLOAT;0;False;62;FLOAT;0;False;7;FLOAT4;0;FLOAT3;14;FLOAT;56;FLOAT;45;FLOAT;200;FLOAT;19;FLOAT3;17
Node;AmplifyShaderEditor.ColorNode;65;-86.21644,-201.3231;Inherit;False;Property;_RockColor;Rock Color;35;0;Create;True;0;0;0;False;0;False;0,0,0,0;0.4433956,0.4433956,0.4433956,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Compare;66;-31.61659,-16.72317;Inherit;False;2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;157;-87.55209,164.4091;Inherit;False;Stylized Snow Blend;32;;106;359b695eb7172584f9df5a0d55bd52e9;0;2;34;FLOAT;0;False;22;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;159;-51.64612,292.8251;Inherit;False;StylizedRainPuddles;-1;;105;ff913d501aadc8b4cb4ec6889f2354ca;0;1;14;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;67;237.3835,64.37683;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;144;657.0565,40.11808;Float;False;False;-1;2;UnityEditor.ShaderGraphLitGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;143;657.0565,40.11808;Float;False;False;-1;2;UnityEditor.ShaderGraphLitGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;True;False;False;False;False;0;False;-1;False;False;False;False;False;False;False;False;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;141;801.0565,222.1181;Float;False;True;-1;2;UnityEditor.ShaderGraphLitGUI;0;12;Distant Lands/Cozy/Stylized Terrain First Pass;94348b07e5e8bab40bd6c8a1e3df54cd;True;Forward;0;1;Forward;18;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;False;0;Standard;0;0;Standard;38;Workflow;1;0;Surface;0;0;  Refraction Model;0;0;  Blend;0;0;Two Sided;1;0;Fragment Normal Space,InvertActionOnDeselection;0;0;Transmission;0;0;  Transmission Shadow;0.5,False,-1;0;Translucency;0;0;  Translucency Strength;1,False,-1;0;  Normal Distortion;0.5,False,-1;0;  Scattering;2,False,-1;0;  Direct;0.9,False,-1;0;  Ambient;0.1,False,-1;0;  Shadow;0.5,False,-1;0;Cast Shadows;1;0;  Use Shadow Threshold;0;0;Receive Shadows;1;0;GPU Instancing;1;0;LOD CrossFade;1;0;Built-in Fog;1;0;_FinalColorxAlpha;0;0;Meta Pass;1;0;Override Baked GI;0;0;Extra Pre Pass;0;0;DOTS Instancing;0;0;Tessellation;0;0;  Phong;0;0;  Strength;0.5,False,-1;0;  Type;0;0;  Tess;16,False,-1;0;  Min;10,False,-1;0;  Max;25,False,-1;0;  Edge Length;16,False,-1;0;  Max Displacement;25,False,-1;0;Write Depth;0;0;  Early Z;0;0;Vertex Position,InvertActionOnDeselection;1;0;0;8;False;True;True;True;True;True;True;True;False;;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;145;657.0565,40.11808;Float;False;False;-1;2;UnityEditor.ShaderGraphLitGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;Universal2D;0;5;Universal2D;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=Universal2D;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;150;632.0565,282.1181;Float;False;False;-1;2;UnityEditor.ShaderGraphLitGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;GBuffer;0;7;GBuffer;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalGBuffer;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;142;657.0565,40.11808;Float;False;False;-1;2;UnityEditor.ShaderGraphLitGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;True;False;False;False;False;0;False;-1;False;False;False;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;140;657.0565,40.11808;Float;False;False;-1;2;UnityEditor.ShaderGraphLitGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;0;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;149;632.0565,282.1181;Float;False;False;-1;2;UnityEditor.ShaderGraphLitGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;DepthNormals;0;6;DepthNormals;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=DepthNormals;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
WireConnection;64;0;62;2
WireConnection;66;0;64;0
WireConnection;66;1;63;0
WireConnection;157;34;162;56
WireConnection;157;22;162;0
WireConnection;159;14;162;45
WireConnection;67;0;65;0
WireConnection;67;1;157;0
WireConnection;67;2;66;0
WireConnection;141;0;67;0
WireConnection;141;1;162;14
WireConnection;141;4;159;0
WireConnection;141;5;162;200
WireConnection;141;6;162;19
WireConnection;141;8;162;17
ASEEND*/
//CHKSM=8A0DBDF18AE5472773BD3DC098108BAD4FD1E09E