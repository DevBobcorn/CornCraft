// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Transparency Example"
{
	Properties
	{
		_MainColor("Main Color", Color) = (1,1,1,1)

	}
	
	SubShader
	{
		
		
		Tags { "RenderType"="Opaque" }
	LOD 100

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend Off
		AlphaToMask Off
		Cull Back
		ColorMask RGBA
		ZWrite On
		ZTest LEqual
		Offset 0 , 0
		
		
		
		Pass
		{
			Name "Unlit"
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM

			

			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			#include "UnityShaderVariables.cginc"
			#define ASE_NEEDS_FRAG_WORLD_POSITION


			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 worldPos : TEXCOORD0;
				#endif
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform float4 _MainColor;
			uniform float4 CZY_FogColor1;
			uniform float4 CZY_FogColor2;
			uniform float CZY_FogDepthMultiplier;
			uniform float CZY_ColorStart1;
			uniform float4 CZY_FogColor3;
			uniform float CZY_ColorStart2;
			uniform float4 CZY_FogColor4;
			uniform float CZY_ColorStart3;
			uniform float4 CZY_FogColor5;
			uniform float CZY_ColorStart4;
			uniform float4 CZY_LightColor;
			uniform float3 CZY_SunDirection;
			uniform half CZY_LightIntensity;
			uniform half CZY_LightFalloff;
			float3 HSVToRGB( float3 c )
			{
				float4 K = float4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
				float3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
				return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
			}
			
			float3 RGBToHSV(float3 c)
			{
				float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
				float4 p = lerp( float4( c.bg, K.wz ), float4( c.gb, K.xy ), step( c.b, c.g ) );
				float4 q = lerp( float4( p.xyw, c.r ), float4( c.r, p.yzx ), step( p.x, c.r ) );
				float d = q.x - min( q.w, q.y );
				float e = 1.0e-10;
				return float3( abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
			}

			
			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				
				float3 vertexValue = float3(0, 0, 0);
				#if ASE_ABSOLUTE_VERTEX_POS
				vertexValue = v.vertex.xyz;
				#endif
				vertexValue = vertexValue;
				#if ASE_ABSOLUTE_VERTEX_POS
				v.vertex.xyz = vertexValue;
				#else
				v.vertex.xyz += vertexValue;
				#endif
				o.vertex = UnityObjectToClipPos(v.vertex);

				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				#endif
				return o;
			}
			
			fixed4 frag (v2f i ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				fixed4 finalColor;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 WorldPosition = i.worldPos;
				#endif
				float FogDepth101_g1 = ( CZY_FogDepthMultiplier * sqrt( distance( _WorldSpaceCameraPos , WorldPosition ) ) );
				float4 lerpResult77_g1 = lerp( CZY_FogColor1 , CZY_FogColor2 , saturate( ( FogDepth101_g1 / CZY_ColorStart1 ) ));
				float4 lerpResult87_g1 = lerp( saturate( lerpResult77_g1 ) , CZY_FogColor3 , saturate( ( ( CZY_ColorStart1 - FogDepth101_g1 ) / ( CZY_ColorStart1 - CZY_ColorStart2 ) ) ));
				float4 lerpResult68_g1 = lerp( lerpResult87_g1 , CZY_FogColor4 , saturate( ( ( CZY_ColorStart2 - FogDepth101_g1 ) / ( CZY_ColorStart2 - CZY_ColorStart3 ) ) ));
				float4 lerpResult93_g1 = lerp( lerpResult68_g1 , CZY_FogColor5 , saturate( ( ( CZY_ColorStart3 - FogDepth101_g1 ) / ( CZY_ColorStart3 - CZY_ColorStart4 ) ) ));
				float4 MainFogColor103_g1 = lerpResult93_g1;
				float3 hsvTorgb31_g1 = RGBToHSV( CZY_LightColor.rgb );
				float3 hsvTorgb32_g1 = RGBToHSV( MainFogColor103_g1.rgb );
				float3 hsvTorgb39_g1 = HSVToRGB( float3(hsvTorgb31_g1.x,hsvTorgb31_g1.y,( hsvTorgb31_g1.z * hsvTorgb32_g1.z )) );
				float3 normalizeResult5_g1 = normalize( ( WorldPosition - _WorldSpaceCameraPos ) );
				float dotResult6_g1 = dot( normalizeResult5_g1 , CZY_SunDirection );
				half LightMask27_g1 = saturate( pow( abs( ( (dotResult6_g1*0.5 + 0.5) * CZY_LightIntensity ) ) , CZY_LightFalloff ) );
				float temp_output_26_0_g1 = ( MainFogColor103_g1.a * saturate( FogDepth101_g1 ) );
				float4 lerpResult43_g1 = lerp( MainFogColor103_g1 , float4( hsvTorgb39_g1 , 0.0 ) , saturate( ( LightMask27_g1 * ( 1.5 * temp_output_26_0_g1 ) ) ));
				float4 lerpResult46_g1 = lerp( _MainColor , lerpResult43_g1 , temp_output_26_0_g1);
				
				
				finalColor = lerpResult46_g1;
				return finalColor;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=18935
0;1080;2194.286;607.5715;1626;172.4401;1;True;False
Node;AmplifyShaderEditor.ColorNode;1;-886,49;Inherit;False;Property;_MainColor;Main Color;0;0;Create;True;0;0;0;False;0;False;1,1,1,1;1,0.01960784,0,0.6588235;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;11;-509.8571,38.3456;Inherit;False;Stylized Fog Override ASE Function;-1;;1;2c381d05c4c43f644b2063318989ca9e;0;1;57;COLOR;0,0,0,0;False;2;COLOR;63;FLOAT;56
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;10;0,0;Float;False;True;-1;2;ASEMaterialInspector;100;1;Transparency Example;0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;False;True;0;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;True;0;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;RenderType=Opaque=RenderType;True;2;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=ForwardBase;False;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;0;1;True;False;;False;0
WireConnection;11;57;1;0
WireConnection;10;0;11;63
ASEEND*/
//CHKSM=2C6C5A6247A089E039B11DD98908652C56B1204D