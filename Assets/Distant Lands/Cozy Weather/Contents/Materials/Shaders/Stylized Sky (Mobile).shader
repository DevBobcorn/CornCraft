// Made with Amplify Shader Editor v1.9.0.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Distant Lands/Cozy/Stylized Sky Mobile"
{
	Properties
	{
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector][HDR]_HorizonColor("Horizon Color", Color) = (0.6399965,0.9474089,0.9622642,0)
		[HideInInspector][HDR]_ZenithColor("Zenith Color", Color) = (0.4000979,0.6638572,0.764151,0)
		[HideInInspector]_Power("Power", Float) = 1
		_SunFlareFalloff("Sun Flare Falloff", Float) = 1
		[HideInInspector]_MoonFlareFalloff("Moon Flare Falloff", Float) = 1
		[HideInInspector][HDR]_SunFlareColor("Sun Flare Color", Color) = (0.355693,0.4595688,0.4802988,1)
		[HideInInspector][HDR]_MoonFlareColor("Moon Flare Color", Color) = (0.355693,0.4595688,0.4802988,1)
		[HideInInspector]_SunSize("Sun Size", Float) = 0
		[HideInInspector][HDR]_SunColor("Sun Color", Color) = (0,0,0,0)
		_GalaxyStars("Galaxy Stars", 2D) = "white" {}
		_StarMap("Star Map", 2D) = "white" {}
		[HDR]_StarColor("Star Color", Color) = (0,0,0,0)
		[ASEEnd]_TextureSample2("Texture Sample 2", 2D) = "white" {}


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
			float4 _HorizonColor;
			float4 _ZenithColor;
			float4 _SunFlareColor;
			float4 _SunColor;
			float4 _MoonFlareColor;
			float4 _StarColor;
			float _Power;
			float _SunFlareFalloff;
			float _SunSize;
			half _MoonFlareFalloff;
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
			sampler2D _StarMap;
			sampler2D _GalaxyStars;
			sampler2D _TextureSample2;


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

				float4 HorizonColor110 = _HorizonColor;
				float4 ZenithColor111 = _ZenithColor;
				float2 texCoord167 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_168_0 = ( texCoord167 - float2( 0.5,0.5 ) );
				float dotResult169 = dot( temp_output_168_0 , temp_output_168_0 );
				float SimpleGradient170 = dotResult169;
				float GradientPos97 = ( 1.0 - saturate( pow( saturate( (0.0 + (SimpleGradient170 - 0.0) * (2.0 - 0.0) / (1.0 - 0.0)) ) , _Power ) ) );
				float4 lerpResult3 = lerp( HorizonColor110 , ZenithColor111 , saturate( GradientPos97 ));
				float4 SimpleSkyGradient68 = lerpResult3;
				float3 normalizeResult26 = normalize( ( WorldPosition - _WorldSpaceCameraPos ) );
				float dotResult27 = dot( normalizeResult26 , CZY_SunDirection );
				float SunDot51 = dotResult27;
				half4 SunFlare35 = abs( ( saturate( pow( abs( (SunDot51*0.5 + 0.5) ) , _SunFlareFalloff ) ) * _SunFlareColor ) );
				float4 SunRender70 = ( _SunColor * ( ( 1.0 - SunDot51 ) > ( pow( _SunSize , 3.0 ) * 0.0007 ) ? 0.0 : 1.0 ) );
				float3 normalizeResult65 = normalize( ( WorldPosition - _WorldSpaceCameraPos ) );
				float dotResult66 = dot( normalizeResult65 , CZY_MoonDirection );
				float MoonDot67 = dotResult66;
				half4 MoonFlare76 = abs( ( saturate( pow( abs( (MoonDot67*0.5 + 0.5) ) , _MoonFlareFalloff ) ) * _MoonFlareColor ) );
				float2 texCoord92 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 Pos83 = texCoord92;
				float mulTime202 = _TimeParameters.x * 0.005;
				float cos203 = cos( mulTime202 );
				float sin203 = sin( mulTime202 );
				float2 rotator203 = mul( Pos83 - float2( 0.5,0.5 ) , float2x2( cos203 , -sin203 , sin203 , cos203 )) + float2( 0.5,0.5 );
				float mulTime205 = _TimeParameters.x * -0.02;
				float simplePerlin2D213 = snoise( (Pos83*5.0 + mulTime205) );
				simplePerlin2D213 = simplePerlin2D213*0.5 + 0.5;
				float StarPlacementPattern217 = saturate( ( min( tex2D( _StarMap, (Pos83*5.0 + mulTime202) ).r , tex2D( _StarMap, (rotator203*2.0 + 0.0) ).r ) * simplePerlin2D213 * (0.2 + (SimpleGradient170 - 0.0) * (0.0 - 0.2) / (1.0 - 0.0)) ) );
				float2 panner321 = ( 1.0 * _Time.y * float2( 0.0007,0 ) + Pos83);
				float mulTime175 = _TimeParameters.x * 0.001;
				float cos176 = cos( 0.01 * _Time.y );
				float sin176 = sin( 0.01 * _Time.y );
				float2 rotator176 = mul( Pos83 - float2( 0.5,0.5 ) , float2x2( cos176 , -sin176 , sin176 , cos176 )) + float2( 0.5,0.5 );
				float4 StarPattern182 = saturate( ( ( ( StarPlacementPattern217 * min( tex2D( _GalaxyStars, (panner321*4.0 + mulTime175) ).r , tex2D( _TextureSample2, (rotator176*0.1 + 0.0) ).r ) ) * ( 1.0 - MoonFlare76.r ) ) * _StarColor ) );
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = ( SimpleSkyGradient68 + SunFlare35 + SunRender70 + MoonFlare76 + StarPattern182 ).rgb;
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
			float4 _HorizonColor;
			float4 _ZenithColor;
			float4 _SunFlareColor;
			float4 _SunColor;
			float4 _MoonFlareColor;
			float4 _StarColor;
			float _Power;
			float _SunFlareFalloff;
			float _SunSize;
			half _MoonFlareFalloff;
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
			float4 _HorizonColor;
			float4 _ZenithColor;
			float4 _SunFlareColor;
			float4 _SunColor;
			float4 _MoonFlareColor;
			float4 _StarColor;
			float _Power;
			float _SunFlareFalloff;
			float _SunSize;
			half _MoonFlareFalloff;
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
6.285715;1085.143;2181.714;596.1429;6466.747;4315.497;7.929173;True;False
Node;AmplifyShaderEditor.CommentaryNode;72;-4728.316,-92.99277;Inherit;False;2040.225;680.2032;;15;58;54;56;60;57;55;76;73;64;61;62;63;65;66;67;Moon Block;0.514151,0.9898598,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;258;-4760.767,751.4372;Inherit;False;2156.234;658.7953;;16;283;280;290;292;288;278;289;291;286;276;285;271;287;275;270;284;Rainbow Block;1,0.9770144,0.5137255,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;71;-4733.934,-925.9814;Inherit;False;2040.225;680.2032;;26;51;27;49;26;24;22;23;35;34;33;31;32;29;53;37;38;47;70;44;41;45;43;46;52;42;331;Sun Block;0.514151,0.9898598,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;91;-4702.299,-2027.029;Inherit;False;1998.663;845.3734;;20;83;92;93;94;14;12;13;97;1;2;110;111;167;168;169;170;335;336;337;338;Variable Declaration;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;173;-17.54585,-2715.89;Inherit;False;2476.533;565.0383;;19;188;253;189;248;178;175;176;321;174;177;219;180;179;254;257;255;218;181;182;Stars;1,0.7345774,0.5254902,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;60;-4677.368,9.632609;Inherit;False;67;MoonDot;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;56;-4313.313,14.24778;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;204;3314.346,-2474.76;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;2;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;83;-4401.376,-1864.431;Inherit;False;Pos;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;205;3203.469,-2200.533;Inherit;False;1;0;FLOAT;-0.02;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;169;-3152,-1936;Inherit;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;203;3138.345,-2474.76;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;206;3243.117,-2287.766;Inherit;False;83;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DotProductOpNode;66;-4094.474,299.3718;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;201;2946.345,-2602.76;Inherit;False;83;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;54;-4508.647,15.1096;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;64;-4331.223,404.3474;Inherit;False;Global;CZY_MoonDirection;CZY_MoonDirection;12;0;Create;True;0;0;0;False;0;False;0,0,0;0,0.2249509,-0.9743701;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.AbsOpNode;75;-3618.212,5.289185;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleTimeNode;202;2946.345,-2522.76;Inherit;False;1;0;FLOAT;0.005;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;167;-3552,-1936;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;180;596.2953,-2410.162;Inherit;True;Property;_TextureSample2;Texture Sample 2;15;0;Create;True;0;0;0;False;0;False;-1;None;59cb97507f14c1d468e967d73ca67a9b;True;0;False;white;Auto;False;Instance;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;63;-4393.336,294.6923;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NormalizeNode;65;-4266.221,293.4446;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;67;-3946.877,291.1702;Inherit;False;MoonDot;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;168;-3344,-1936;Inherit;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos;62;-4642.492,381.5694;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ScaleAndOffsetNode;207;3250.346,-2666.76;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;211;3490.346,-2506.76;Inherit;True;Property;_TextureSample5;Texture Sample 5;12;0;Create;True;0;0;0;False;0;False;-1;None;59cb97507f14c1d468e967d73ca67a9b;True;0;False;white;Auto;False;Instance;209;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;179;596.2953,-2602.162;Inherit;True;Property;_GalaxyStars;Galaxy Stars;11;0;Create;True;0;0;0;False;0;False;-1;None;93557f6a2b0824644b6ddaa6442e6c8e;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMinOpNode;214;3794.346,-2586.76;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;61;-4573.082,232.4811;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;254;1189,-2345;Inherit;False;76;MoonFlare;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;73;-3978.105,94.1355;Inherit;False;Property;_MoonFlareColor;Moon Flare Color;6;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;0.355693,0.4595688,0.4802988,1;0.007843138,0.03137255,0.02745098,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;92;-4608.107,-1858.542;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;55;-4405.444,144.3357;Half;False;Property;_MoonFlareFalloff;Moon Flare Falloff;4;1;[HideInInspector];Create;True;0;0;0;False;0;False;1;24.4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;176;244.2952,-2378.162;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0.01;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;210;3408.117,-2282.766;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;74;-3756.935,6.347229;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleTimeNode;175;44.29524,-2419.162;Inherit;False;1;0;FLOAT;0.001;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;22;-4613.366,-637.3319;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ScaleAndOffsetNode;178;420.2953,-2378.162;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;0.1;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;216;4093.612,-2481.118;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;213;3601.541,-2289.478;Inherit;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;253;1648,-2512;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;58;-4022.834,8.715373;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;189;1648,-2416;Inherit;False;Property;_StarColor;Star Color;13;1;[HDR];Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldSpaceCameraPos;23;-4682.777,-488.2437;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;188;1872,-2496;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;248;2000,-2496;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;182;2128,-2496;Inherit;False;StarPattern;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.Vector3Node;49;-4359.506,-458.4658;Inherit;False;Global;CZY_SunDirection;CZY_SunDirection;12;0;Create;True;0;0;0;False;0;False;0,0,0;0.9855249,-0.03574456,-0.165721;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TFHCRemapNode;212;3677.612,-2145.118;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0.2;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;215;3965.612,-2481.118;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;57;-4177.911,11.49558;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;321;236.2448,-2577.34;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.0007,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;174;52.29524,-2506.162;Inherit;False;83;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;335;-4272,-1536;Inherit;False;170;SimpleGradient;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;177;408.2952,-2575.362;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;4;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;219;948.8347,-2650.82;Inherit;False;217;StarPlacementPattern;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;336;-3792,-1536;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;76;-3487.056,-0.2542725;Half;False;MoonFlare;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.NormalizeNode;26;-4306.505,-576.3683;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;24;-4433.621,-575.1206;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TFHCRemapNode;338;-4064,-1536;Inherit;True;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;217;4237.612,-2481.118;Inherit;False;StarPlacementPattern;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;349;-1104,496;Inherit;False;182;StarPattern;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;78;-1104.976,411.5836;Inherit;False;76;MoonFlare;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;209;3490.346,-2698.76;Inherit;True;Property;_StarMap;Star Map;12;0;Create;True;0;0;0;False;0;False;-1;59cb97507f14c1d468e967d73ca67a9b;59cb97507f14c1d468e967d73ca67a9b;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;170;-2960,-1936;Inherit;False;SimpleGradient;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;68;-750.7037,-660.8309;Inherit;False;SimpleSkyGradient;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;208;3485.612,-2145.118;Inherit;False;170;SimpleGradient;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;44;-3115.263,-521.6712;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;1;-4079.039,-1939.879;Inherit;False;Property;_HorizonColor;Horizon Color;0;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;0.6399965,0.9474089,0.9622642,0;0.3871148,0.678383,0.8611395,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GradientNode;289;-3611.335,853.5206;Inherit;False;0;8;4;1,0,0,0.1205921;1,0.3135593,0,0.2441138;1,0.8774895,0.2216981,0.3529412;0.3030533,1,0.2877358,0.4529488;0.3726415,1,0.9559749,0.5529412;0.4669811,0.7253776,1,0.6470588;0.1561944,0.3586135,0.735849,0.802945;0.2576377,0.08721964,0.5283019,0.9264668;0,0;0,0.08235294;0.6039216,0.8264744;0,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.GetLocalVarNode;53;-4654.877,-837.8683;Inherit;False;51;SunDot;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;14;-3792,-1456;Inherit;False;Property;_Power;Power;2;1;[HideInInspector];Create;True;0;0;0;False;0;False;1;0.638;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;33;-4155.419,-836.0052;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;13;-3488,-1536;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;257;1469.192,-2339.736;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;27;-4134.759,-570.4412;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;12;-3632,-1536;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;70;-2959.761,-526.3458;Inherit;False;SunRender;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;280;-3026.661,1079.623;Inherit;False;4;4;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMinOpNode;181;912,-2496;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;255;1352.066,-2339.02;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.GetLocalVarNode;271;-4440.661,1227.623;Inherit;False;51;SunDot;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;270;-4664.661,1115.623;Inherit;False;Property;_RainbowWidth;Rainbow Width;8;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;24.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;218;1184,-2544;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;97;-3184,-1536;Inherit;False;GradientPos;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;284;-4655.661,1012.623;Inherit;False;Property;_RainbowSize;Rainbow Size;9;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;16.8;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;32;-4290.822,-833.2531;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;337;-3344,-1536;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;287;-4472.661,1115.623;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;276;-4248.661,1227.623;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;29;-4486.156,-832.3912;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;275;-4475.661,1015.623;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;2;-4074.189,-1761.397;Inherit;False;Property;_ZenithColor;Zenith Color;1;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;0.4000979,0.6638572,0.764151,0;0.08784264,0.2475414,0.3962264,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;42;-3872,-496;Inherit;False;Property;_SunSize;Sun Size;7;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;0.78;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;31;-4350.381,-697.744;Float;False;Property;_SunFlareFalloff;Sun Flare Falloff;3;0;Create;False;0;0;0;False;0;False;1;43.7;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;285;-4328.661,1083.623;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;69;-1131.687,172.7539;Inherit;False;68;SimpleSkyGradient;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;288;-3816.494,1090.438;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;110;-3835.76,-1941.606;Inherit;False;HorizonColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.AbsOpNode;47;-3522.625,-840.1691;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.Compare;278;-4024.661,1163.623;Inherit;False;2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Compare;41;-3317.57,-487.7016;Inherit;False;2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;112;-1304.993,-728.6973;Inherit;False;110;HorizonColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;113;-1299.993,-649.6973;Inherit;False;111;ZenithColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;34;-4000.341,-838.7855;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;331;-3664,-496;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;283;-2850.661,1074.623;Inherit;False;RainbowClipping;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;-3506.887,-501.4186;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.0007;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;37;-3878.518,-783.3228;Inherit;False;Property;_SunFlareColor;Sun Flare Color;5;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;0.355693,0.4595688,0.4802988,1;0.2588235,0.3254902,0.3411765,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;45;-3412.185,-673.9927;Inherit;False;Property;_SunColor;Sun Color;10;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;0,0,0,0;10,10,10,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;43;-3557.892,-389.7785;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;115;-1200,-528;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;111;-3836.003,-1761.749;Inherit;False;ZenithColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;38;-3661.348,-839.1111;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;39;-1088.632,251.1212;Inherit;False;35;SunFlare;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;3;-1030.707,-657.8158;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;292;-3346.207,1154.068;Inherit;False;Property;_RainbowIntensity;Rainbow Intensity;14;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GradientSampleNode;290;-3365.049,931.7617;Inherit;True;2;0;OBJECT;;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;77;-1098.976,332.5836;Inherit;False;70;SunRender;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.Compare;286;-4024.661,1003.623;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;51;-3987.163,-578.6428;Inherit;False;SunDot;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;35;-3391.469,-845.7126;Half;False;SunFlare;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCRemapNode;291;-3570.049,973.7616;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;52;-3749.203,-394.6242;Inherit;False;51;SunDot;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;36;-877.3224,316.4634;Inherit;False;5;5;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;94;-4453.894,-1715.211;Inherit;False;Time;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;93;-4622.788,-1712.352;Inherit;False;1;0;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;102;-1404,-531;Inherit;False;97;GradientPos;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;342;104.1543,85.88161;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;False;False;True;1;LightMode=DepthOnly;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;343;104.1543,85.88161;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;339;104.1543,85.88161;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;1;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;0;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;340;-736,320;Float;False;True;-1;2;;0;12;Distant Lands/Cozy/Stylized Sky Mobile;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;8;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;1;False;;False;False;False;False;False;False;False;False;True;True;True;221;False;;255;False;;255;False;;7;False;;2;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Transparent=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;1;1;False;;0;False;;1;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;1;LightMode=UniversalForward;False;False;0;Hidden/InternalErrorShader;0;0;Standard;22;Surface;0;0;  Blend;0;0;Two Sided;2;637952255122392693;Cast Shadows;1;0;  Use Shadow Threshold;0;0;Receive Shadows;1;0;GPU Instancing;1;0;LOD CrossFade;0;0;Built-in Fog;0;0;DOTS Instancing;0;0;Meta Pass;0;0;Extra Pre Pass;0;0;Tessellation;0;0;  Phong;0;0;  Strength;0.5,False,;0;  Type;0;0;  Tess;16,False,;0;  Min;10,False,;0;  Max;25,False,;0;  Edge Length;16,False,;0;  Max Displacement;25,False,;0;Vertex Position,InvertActionOnDeselection;1;0;0;5;False;True;True;True;False;False;;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;341;104.1543,85.88161;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;2;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=ShadowCaster;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
WireConnection;56;0;54;0
WireConnection;204;0;203;0
WireConnection;83;0;92;0
WireConnection;169;0;168;0
WireConnection;169;1;168;0
WireConnection;203;0;201;0
WireConnection;203;2;202;0
WireConnection;66;0;65;0
WireConnection;66;1;64;0
WireConnection;54;0;60;0
WireConnection;75;0;74;0
WireConnection;180;1;178;0
WireConnection;63;0;61;0
WireConnection;63;1;62;0
WireConnection;65;0;63;0
WireConnection;67;0;66;0
WireConnection;168;0;167;0
WireConnection;207;0;201;0
WireConnection;207;2;202;0
WireConnection;211;1;204;0
WireConnection;179;1;177;0
WireConnection;214;0;209;1
WireConnection;214;1;211;1
WireConnection;176;0;174;0
WireConnection;210;0;206;0
WireConnection;210;2;205;0
WireConnection;74;0;58;0
WireConnection;74;1;73;0
WireConnection;178;0;176;0
WireConnection;216;0;215;0
WireConnection;213;0;210;0
WireConnection;253;0;218;0
WireConnection;253;1;257;0
WireConnection;58;0;57;0
WireConnection;188;0;253;0
WireConnection;188;1;189;0
WireConnection;248;0;188;0
WireConnection;182;0;248;0
WireConnection;212;0;208;0
WireConnection;215;0;214;0
WireConnection;215;1;213;0
WireConnection;215;2;212;0
WireConnection;57;0;56;0
WireConnection;57;1;55;0
WireConnection;321;0;174;0
WireConnection;177;0;321;0
WireConnection;177;2;175;0
WireConnection;336;0;338;0
WireConnection;76;0;75;0
WireConnection;26;0;24;0
WireConnection;24;0;22;0
WireConnection;24;1;23;0
WireConnection;338;0;335;0
WireConnection;217;0;216;0
WireConnection;209;1;207;0
WireConnection;170;0;169;0
WireConnection;68;0;3;0
WireConnection;44;0;45;0
WireConnection;44;1;41;0
WireConnection;33;0;32;0
WireConnection;33;1;31;0
WireConnection;13;0;12;0
WireConnection;257;0;255;0
WireConnection;27;0;26;0
WireConnection;27;1;49;0
WireConnection;12;0;336;0
WireConnection;12;1;14;0
WireConnection;70;0;44;0
WireConnection;280;0;290;0
WireConnection;280;1;288;0
WireConnection;280;2;290;4
WireConnection;280;3;292;0
WireConnection;181;0;179;1
WireConnection;181;1;180;1
WireConnection;255;0;254;0
WireConnection;218;0;219;0
WireConnection;218;1;181;0
WireConnection;97;0;337;0
WireConnection;32;0;29;0
WireConnection;337;0;13;0
WireConnection;287;0;270;0
WireConnection;276;0;271;0
WireConnection;29;0;53;0
WireConnection;275;0;284;0
WireConnection;285;0;275;0
WireConnection;285;1;287;0
WireConnection;288;0;286;0
WireConnection;288;1;278;0
WireConnection;110;0;1;0
WireConnection;47;0;38;0
WireConnection;278;0;276;0
WireConnection;278;1;285;0
WireConnection;41;0;43;0
WireConnection;41;1;46;0
WireConnection;34;0;33;0
WireConnection;331;0;42;0
WireConnection;283;0;280;0
WireConnection;46;0;331;0
WireConnection;43;0;52;0
WireConnection;115;0;102;0
WireConnection;111;0;2;0
WireConnection;38;0;34;0
WireConnection;38;1;37;0
WireConnection;3;0;112;0
WireConnection;3;1;113;0
WireConnection;3;2;115;0
WireConnection;290;0;289;0
WireConnection;290;1;291;0
WireConnection;286;0;276;0
WireConnection;286;1;275;0
WireConnection;51;0;27;0
WireConnection;35;0;47;0
WireConnection;291;0;276;0
WireConnection;291;1;275;0
WireConnection;291;2;285;0
WireConnection;36;0;69;0
WireConnection;36;1;39;0
WireConnection;36;2;77;0
WireConnection;36;3;78;0
WireConnection;36;4;349;0
WireConnection;94;0;93;0
WireConnection;340;2;36;0
ASEEND*/
//CHKSM=919DBD18C9274DA8FC2B91AF83A616E138842A90