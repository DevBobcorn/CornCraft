// Made with Amplify Shader Editor v1.9.0.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Distant Lands/Cozy/Stepped Fog"
{
	Properties
	{
		[HDR]_FogColor1("Fog Color 1", Color) = (1,0,0.8999224,1)
		[HDR]_FogColor2("Fog Color 2", Color) = (1,0,0,1)
		[HDR]_FogColor3("Fog Color 3", Color) = (1,0,0.7469492,1)
		[HDR]_FogColor4("Fog Color 4", Color) = (0,0.8501792,1,1)
		[HDR]_FogColor5("Fog Color 5", Color) = (0.164721,0,1,1)
		_FogColorStart1("FogColorStart1", Float) = 1
		_FogColorStart2("FogColorStart2", Float) = 2
		_FogColorStart3("FogColorStart3", Float) = 3
		_FogColorStart4("FogColorStart4", Float) = 4
		[HideInInspector]_SunDirection("Sun Direction", Vector) = (0,0,0,0)
		_FlareSquish("Flare Squish", Float) = 1
		_FogDepthMultiplier("Fog Depth Multiplier", Float) = 1
		_LightFalloff("Light Falloff", Float) = 1
		_LightIntensity("Light Intensity", Float) = 0
		[HDR]_LightColor("Light Color", Color) = (0,0,0,0)
		_FogSmoothness("Fog Smoothness", Float) = 0.1
		_FogIntensity("Fog Intensity", Float) = 1
		_FogOffset("Fog Offset", Float) = 1
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Pass
		{
			ColorMask 0
			ZWrite On
		}

		Tags{ "RenderType" = "HeightFog"  "Queue" = "Transparent+1" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Front
		ZWrite Off
		ZTest Always
		Stencil
		{
			Ref 222
			Comp NotEqual
			Pass Replace
		}
		Blend SrcAlpha OneMinusSrcAlpha
		
		GrabPass{ }
		CGPROGRAM
		#include "UnityCG.cginc"
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex);
		#else
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex)
		#endif
		#pragma surface surf Unlit keepalpha noshadow noambient novertexlights nolightmap  nodynlightmap nodirlightmap nofog nometa noforwardadd 
		struct Input
		{
			float4 screenPos;
			float3 worldPos;
		};

		ASE_DECLARE_SCREENSPACE_TEXTURE( _GrabTexture )
		uniform float _FogDepthMultiplier;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform float _FogColorStart4;
		uniform float4 _FogColor5;
		uniform float _FogColorStart3;
		uniform float4 _FogColor4;
		uniform float _FogColorStart2;
		uniform float4 _FogColor3;
		uniform float _FogColorStart1;
		uniform float4 _FogColor2;
		uniform float4 _FogColor1;
		uniform float4 _LightColor;
		uniform float _FlareSquish;
		uniform float3 _SunDirection;
		uniform half _LightIntensity;
		uniform half _LightFalloff;
		uniform float _FogSmoothness;
		uniform float _FogOffset;
		uniform float _FogIntensity;


		inline float4 ASE_ComputeGrabScreenPos( float4 pos )
		{
			#if UNITY_UV_STARTS_AT_TOP
			float scale = -1.0;
			#else
			float scale = 1.0;
			#endif
			float4 o = pos;
			o.y = pos.w * 0.5f;
			o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
			return o;
		}


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

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( ase_screenPos );
			float4 screenColor246 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture,ase_grabScreenPos.xy/ase_grabScreenPos.w);
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float eyeDepth229 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float Distance314 = ( _FogDepthMultiplier * sqrt( eyeDepth229 ) );
			float4 FogColors317 = ( Distance314 > _FogColorStart4 ? _FogColor5 : ( Distance314 > _FogColorStart3 ? _FogColor4 : ( Distance314 > _FogColorStart2 ? _FogColor3 : ( Distance314 > _FogColorStart1 ? _FogColor2 : _FogColor1 ) ) ) );
			float3 hsvTorgb224 = RGBToHSV( _LightColor.rgb );
			float3 hsvTorgb265 = RGBToHSV( FogColors317.rgb );
			float3 hsvTorgb249 = HSVToRGB( float3(hsvTorgb224.x,hsvTorgb224.y,( hsvTorgb224.z * hsvTorgb265.z )) );
			float3 ase_worldPos = i.worldPos;
			float3 appendResult245 = (float3(1.0 , _FlareSquish , 1.0));
			float3 normalizeResult266 = normalize( ( ( ase_worldPos * appendResult245 ) - _WorldSpaceCameraPos ) );
			float dotResult263 = dot( normalizeResult266 , _SunDirection );
			half LightMask250 = saturate( pow( abs( ( (dotResult263*0.5 + 0.5) * _LightIntensity ) ) , _LightFalloff ) );
			float temp_output_275_0 = ( FogColors317.a * saturate( Distance314 ) );
			float4 lerpResult260 = lerp( FogColors317 , float4( hsvTorgb249 , 0.0 ) , saturate( ( LightMask250 * ( 1.5 * temp_output_275_0 ) ) ));
			float4 lerpResult240 = lerp( screenColor246 , lerpResult260 , temp_output_275_0);
			o.Emission = lerpResult240.rgb;
			o.Alpha = saturate( ( ( 1.0 - saturate( ( ( ( ( ase_worldPos - _WorldSpaceCameraPos ).y * 0.1 ) * ( 1.0 / _FogSmoothness ) ) + ( 1.0 - _FogOffset ) ) ) ) * _FogIntensity ) );
		}

		ENDCG
	}
}
/*ASEBEGIN
Version=19002
0;1080.571;2194.286;607.5715;2580.657;275.3208;1;True;False
Node;AmplifyShaderEditor.ScreenDepthNode;229;-2752,-176;Inherit;False;0;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;277;-2778.25,890.0894;Inherit;False;Property;_FlareSquish;Flare Squish;11;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SqrtOpNode;261;-2576,-176;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;268;-2752,-256;Inherit;False;Property;_FogDepthMultiplier;Fog Depth Multiplier;12;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;248;-2553.066,664.5895;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DynamicAppendNode;245;-2484.25,880.0894;Inherit;False;FLOAT3;4;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;219;-2432,-256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;314;-2304,-256;Inherit;False;Distance;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;247;-2222.824,815.7579;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos;255;-2341.629,971.1777;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;315;-608,-2160;Inherit;False;314;Distance;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;304;-608,-2000;Inherit;False;Property;_FogColor2;Fog Color 2;1;1;[HDR];Create;True;0;0;0;False;0;False;1,0,0,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;279;-1981.584,881.3034;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;310;-608,-2080;Inherit;False;Property;_FogColorStart1;FogColorStart1;5;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;303;-608,-1824;Inherit;False;Property;_FogColor1;Fog Color 1;0;1;[HDR];Create;True;0;0;0;False;0;False;1,0,0.8999224,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector3Node;227;-1819.237,997.1949;Inherit;False;Property;_SunDirection;Sun Direction;9;1;[HideInInspector];Create;True;0;0;0;False;0;False;0,0,0;0.6622303,0.6346595,0.3983196;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.NormalizeNode;266;-1789.585,881.3034;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Compare;319;-304,-2034;Inherit;False;2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;283;-304,-1808;Inherit;False;Property;_FogColor3;Fog Color 3;2;1;[HDR];Create;True;0;0;0;False;0;False;1,0,0.7469492,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WireNode;325;-143.8072,-2061.888;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;311;-304,-1888;Inherit;False;Property;_FogColorStart2;FogColorStart2;6;0;Create;True;0;0;0;False;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;263;-1469.583,881.3034;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Compare;320;-48,-2032;Inherit;False;2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;313;-48,-1888;Inherit;False;Property;_FogColorStart3;FogColorStart3;7;0;Create;True;0;0;0;False;0;False;3;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;293;-48,-1808;Inherit;False;Property;_FogColor4;Fog Color 4;3;1;[HDR];Create;True;0;0;0;False;0;False;0,0.8501792,1,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WireNode;324;164.2928,-2052.788;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Compare;321;256,-2032;Inherit;False;2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;291;256,-1808;Inherit;False;Property;_FogColor5;Fog Color 5;4;1;[HDR];Create;True;0;0;0;False;0;False;0.164721,0,1,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;294;256,-1888;Inherit;False;Property;_FogColorStart4;FogColorStart4;8;0;Create;True;0;0;0;False;0;False;4;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;253;-1266.119,871.5823;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;323;380.0928,-2068.388;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;234;-1187.327,1043.645;Half;False;Property;_LightIntensity;Light Intensity;14;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;254;-957.5832,881.3034;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;231;-1888,352;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldSpaceCameraPos;257;-1888,496;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Compare;322;496,-2032;Inherit;False;2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;232;-1632,352;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;317;656,-2032;Inherit;False;FogColors;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;236;-799.2432,955.9188;Half;False;Property;_LightFalloff;Light Falloff;13;0;Create;False;0;0;0;False;0;False;1;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;223;-765.5842,881.3034;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;242;-1568,480;Inherit;False;Property;_FogSmoothness;Fog Smoothness;16;0;Create;True;0;0;0;False;0;False;0.1;100;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;237;-615.6221,890.1;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;316;-1808,112;Inherit;False;314;Distance;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;318;-2000,-256;Inherit;False;317;FogColors;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.BreakToComponentsNode;230;-1504,352;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;270;-1376,352;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;262;-1604.629,-103.2568;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SaturateNode;258;-461.1883,889.8445;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;267;-1376,448;Inherit;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;273;-1637.218,116.753;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;243;-1376,544;Inherit;False;Property;_FogOffset;Fog Offset;18;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;250;-315.8317,884.873;Half;False;LightMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;275;-1468.403,63.76376;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;264;-1184,352;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;272;-2006.491,-736.3435;Inherit;False;Property;_LightColor;Light Color;15;1;[HDR];Create;True;0;0;0;False;0;False;0,0,0,0;1.083397,1.392001,1.382235,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;256;-1184,448;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;235;-1453.592,-194.1233;Inherit;False;250;LightMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;244;-1056,352;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RGBToHSVNode;265;-1643.002,-441.2271;Inherit;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RGBToHSVNode;224;-1654.227,-652.1216;Inherit;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;252;-1329.617,-22.96418;Inherit;False;2;2;0;FLOAT;1.5;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;276;-1218.707,-126.5659;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;238;-1415.906,-483.6272;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;259;-944,352;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;221;-832,432;Inherit;False;Property;_FogIntensity;Fog Intensity;17;0;Create;False;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;220;-1066.722,-158.5626;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.HSVToRGBNode;249;-1263.036,-561.0774;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.OneMinusNode;228;-816,352;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenColorNode;246;-752.5202,-488.0923;Inherit;False;Global;_GrabScreen0;Grab Screen 0;5;0;Create;True;0;0;0;False;0;False;Object;-1;False;False;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;233;-672,352;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;260;-965.6562,-381.7821;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;222;-541,352;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;274;-1795.329,1175.465;Inherit;False;Property;_MoonDirection;Moon Direction;10;1;[HideInInspector];Create;True;0;0;0;False;0;False;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;251;-316.6268,1154.062;Half;False;MoonMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;271;-463.4237,1160.474;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;278;-1470.379,1225.391;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;241;-955.9682,1164.443;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;269;-769.2592,1164.896;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;240;-525.2369,-306.8453;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.PowerNode;225;-622.1776,1159.289;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;239;-1284.198,1168.138;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;;0;0;Unlit;Distant Lands/Cozy/Stepped Fog;False;False;False;False;True;True;True;True;True;True;True;True;False;False;True;False;False;False;False;False;False;Front;2;False;;7;False;;False;0;False;;0;False;;True;0;Custom;0.5;True;False;1;True;Custom;HeightFog;Transparent;All;18;all;True;True;True;True;0;False;;True;222;False;;255;False;;255;False;;6;False;;3;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;2;5;False;;10;False;;0;5;False;;10;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;19;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;261;0;229;0
WireConnection;245;1;277;0
WireConnection;219;0;268;0
WireConnection;219;1;261;0
WireConnection;314;0;219;0
WireConnection;247;0;248;0
WireConnection;247;1;245;0
WireConnection;279;0;247;0
WireConnection;279;1;255;0
WireConnection;266;0;279;0
WireConnection;319;0;315;0
WireConnection;319;1;310;0
WireConnection;319;2;304;0
WireConnection;319;3;303;0
WireConnection;325;0;315;0
WireConnection;263;0;266;0
WireConnection;263;1;227;0
WireConnection;320;0;325;0
WireConnection;320;1;311;0
WireConnection;320;2;283;0
WireConnection;320;3;319;0
WireConnection;324;0;315;0
WireConnection;321;0;324;0
WireConnection;321;1;313;0
WireConnection;321;2;293;0
WireConnection;321;3;320;0
WireConnection;253;0;263;0
WireConnection;323;0;315;0
WireConnection;254;0;253;0
WireConnection;254;1;234;0
WireConnection;322;0;323;0
WireConnection;322;1;294;0
WireConnection;322;2;291;0
WireConnection;322;3;321;0
WireConnection;232;0;231;0
WireConnection;232;1;257;0
WireConnection;317;0;322;0
WireConnection;223;0;254;0
WireConnection;237;0;223;0
WireConnection;237;1;236;0
WireConnection;230;0;232;0
WireConnection;270;0;230;1
WireConnection;262;0;318;0
WireConnection;258;0;237;0
WireConnection;267;1;242;0
WireConnection;273;0;316;0
WireConnection;250;0;258;0
WireConnection;275;0;262;3
WireConnection;275;1;273;0
WireConnection;264;0;270;0
WireConnection;264;1;267;0
WireConnection;256;0;243;0
WireConnection;244;0;264;0
WireConnection;244;1;256;0
WireConnection;265;0;318;0
WireConnection;224;0;272;0
WireConnection;252;1;275;0
WireConnection;276;0;235;0
WireConnection;276;1;252;0
WireConnection;238;0;224;3
WireConnection;238;1;265;3
WireConnection;259;0;244;0
WireConnection;220;0;276;0
WireConnection;249;0;224;1
WireConnection;249;1;224;2
WireConnection;249;2;238;0
WireConnection;228;0;259;0
WireConnection;233;0;228;0
WireConnection;233;1;221;0
WireConnection;260;0;318;0
WireConnection;260;1;249;0
WireConnection;260;2;220;0
WireConnection;222;0;233;0
WireConnection;251;0;271;0
WireConnection;271;0;225;0
WireConnection;278;0;266;0
WireConnection;278;1;274;0
WireConnection;241;0;239;0
WireConnection;241;1;234;0
WireConnection;269;0;241;0
WireConnection;240;0;246;0
WireConnection;240;1;260;0
WireConnection;240;2;275;0
WireConnection;225;0;269;0
WireConnection;225;1;236;0
WireConnection;239;0;278;0
WireConnection;0;2;240;0
WireConnection;0;9;222;0
ASEEND*/
//CHKSM=66921883BCF5FAFC2443233F2CC2FE124BF9513E