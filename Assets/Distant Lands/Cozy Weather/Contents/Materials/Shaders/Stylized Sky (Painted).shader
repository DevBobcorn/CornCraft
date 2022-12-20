// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Distant Lands/Cozy/Stylized Painted Sky"
{
	Properties
	{
		[HideInInspector][HDR]_HorizonColor("Horizon Color", Color) = (0.6399965,0.9474089,0.9622642,0)
		[HideInInspector][HDR]_GalaxyColor3("Galaxy Color 3", Color) = (0.6399965,0.9474089,0.9622642,0)
		[HideInInspector][HDR]_GalaxyColor2("Galaxy Color 2", Color) = (0.6399965,0.9474089,0.9622642,0)
		[HideInInspector][HDR]_GalaxyColor1("Galaxy Color 1", Color) = (0.6399965,0.9474089,0.9622642,0)
		[HideInInspector][HDR]_ZenithColor("Zenith Color", Color) = (0.4000979,0.6638572,0.764151,0)
		[HideInInspector]_Power("Power", Float) = 1
		_SunFlareFalloff("Sun Flare Falloff", Float) = 1
		[HideInInspector]_MoonFlareFalloff("Moon Flare Falloff", Float) = 1
		[HideInInspector][HDR]_SunFlareColor("Sun Flare Color", Color) = (0.355693,0.4595688,0.4802988,1)
		[HideInInspector][HDR]_MoonFlareColor("Moon Flare Color", Color) = (0.355693,0.4595688,0.4802988,1)
		[HideInInspector]_SunSize("Sun Size", Float) = 0
		[HideInInspector]_RainbowWidth("Rainbow Width", Float) = 0
		[HideInInspector]_RainbowSize("Rainbow Size", Float) = 0
		[HideInInspector][HDR]_SunColor("Sun Color", Color) = (0,0,0,0)
		_GalaxyStars("Galaxy Stars", 2D) = "white" {}
		_GalaxyPattern("Galaxy Pattern", 2D) = "white" {}
		_SkyPatchwork("Sky Patchwork", 2D) = "white" {}
		_LightColumns("Light Columns", 2D) = "white" {}
		[HideInInspector][Header(Border Clouds)]_PatchworkHeight("Patchwork Height", Range( 0 , 1)) = 1
		[HideInInspector]_PatchworkVariation("Patchwork Variation", Range( 0 , 1)) = 1
		[HideInInspector]_PatchworkBias("Patchwork Bias", Range( -1 , 1)) = 0
		[HDR]_StarColor("Star Color", Color) = (0,0,0,0)
		[HideInInspector][HDR]_LightColumnColor("Light Column Color", Color) = (0,0,0,0)
		[HideInInspector]_GalaxyMultiplier("Galaxy Multiplier", Range( 0 , 1)) = 0
		[HideInInspector]_RainbowIntensity("Rainbow Intensity", Range( 0 , 1)) = 0
		_ClippingThreshold("Clipping Threshold", Float) = 0.5
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Pass
		{
			ColorMask 0
			ZWrite On
		}

		Tags{ "RenderType" = "Opaque"  "Queue" = "Transparent-100" "IsEmissive" = "true"  }
		Cull Front
		Stencil
		{
			Ref 221
			Comp Always
			Pass Replace
		}
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Unlit keepalpha noshadow noambient novertexlights nolightmap  nodynlightmap nodirlightmap nofog 
		struct Input
		{
			float2 uv_texcoord;
			float3 worldPos;
		};

		uniform float _Power;
		uniform sampler2D _SkyPatchwork;
		uniform float _PatchworkHeight;
		uniform float _PatchworkVariation;
		uniform float _PatchworkBias;
		uniform float _ClippingThreshold;
		uniform float4 _ZenithColor;
		uniform float4 _HorizonColor;
		uniform float3 CZY_SunDirection;
		uniform float _SunFlareFalloff;
		uniform float4 _SunFlareColor;
		uniform float4 _SunColor;
		uniform float _SunSize;
		uniform float3 CZY_MoonDirection;
		uniform half _MoonFlareFalloff;
		uniform float4 _MoonFlareColor;
		uniform sampler2D _GalaxyStars;
		uniform sampler2D _GalaxyPattern;
		uniform float4 _StarColor;
		uniform float4 _GalaxyColor1;
		uniform float4 _GalaxyColor2;
		uniform float4 _GalaxyColor3;
		uniform float _GalaxyMultiplier;
		uniform float _RainbowSize;
		uniform float _RainbowWidth;
		uniform float _RainbowIntensity;
		uniform sampler2D _LightColumns;
		uniform float4 _LightColumnColor;


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


		struct Gradient
		{
			int type;
			int colorsLength;
			int alphasLength;
			float4 colors[8];
			float2 alphas[8];
		};


		Gradient NewGradient(int type, int colorsLength, int alphasLength, 
		float4 colors0, float4 colors1, float4 colors2, float4 colors3, float4 colors4, float4 colors5, float4 colors6, float4 colors7,
		float2 alphas0, float2 alphas1, float2 alphas2, float2 alphas3, float2 alphas4, float2 alphas5, float2 alphas6, float2 alphas7)
		{
			Gradient g;
			g.type = type;
			g.colorsLength = colorsLength;
			g.alphasLength = alphasLength;
			g.colors[ 0 ] = colors0;
			g.colors[ 1 ] = colors1;
			g.colors[ 2 ] = colors2;
			g.colors[ 3 ] = colors3;
			g.colors[ 4 ] = colors4;
			g.colors[ 5 ] = colors5;
			g.colors[ 6 ] = colors6;
			g.colors[ 7 ] = colors7;
			g.alphas[ 0 ] = alphas0;
			g.alphas[ 1 ] = alphas1;
			g.alphas[ 2 ] = alphas2;
			g.alphas[ 3 ] = alphas3;
			g.alphas[ 4 ] = alphas4;
			g.alphas[ 5 ] = alphas5;
			g.alphas[ 6 ] = alphas6;
			g.alphas[ 7 ] = alphas7;
			return g;
		}


		float4 SampleGradient( Gradient gradient, float time )
		{
			float3 color = gradient.colors[0].rgb;
			UNITY_UNROLL
			for (int c = 1; c < 8; c++)
			{
			float colorPos = saturate((time - gradient.colors[c-1].w) / ( 0.00001 + (gradient.colors[c].w - gradient.colors[c-1].w)) * step(c, (float)gradient.colorsLength-1));
			color = lerp(color, gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), gradient.type));
			}
			#ifndef UNITY_COLORSPACE_GAMMA
			color = half3(GammaToLinearSpaceExact(color.r), GammaToLinearSpaceExact(color.g), GammaToLinearSpaceExact(color.b));
			#endif
			float alpha = gradient.alphas[0].x;
			UNITY_UNROLL
			for (int a = 1; a < 8; a++)
			{
			float alphaPos = saturate((time - gradient.alphas[a-1].y) / ( 0.00001 + (gradient.alphas[a].y - gradient.alphas[a-1].y)) * step(a, (float)gradient.alphasLength-1));
			alpha = lerp(alpha, gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), gradient.type));
			}
			return float4(color, alpha);
		}


		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 temp_output_168_0 = ( i.uv_texcoord - float2( 0.5,0.5 ) );
			float dotResult169 = dot( temp_output_168_0 , temp_output_168_0 );
			float SimpleGradient170 = dotResult169;
			float GradientPos97 = ( 1.0 - saturate( pow( saturate( (0.0 + (SimpleGradient170 - 0.0) * (2.0 - 0.0) / (1.0 - 0.0)) ) , _Power ) ) );
			float2 Pos83 = i.uv_texcoord;
			float mulTime89 = _Time.y * 0.001;
			float cos106 = cos( mulTime89 );
			float sin106 = sin( mulTime89 );
			float2 rotator106 = mul( Pos83 - float2( 0.5,0.5 ) , float2x2( cos106 , -sin106 , sin106 , cos106 )) + float2( 0.5,0.5 );
			float PatchworkPattern150 = min( tex2D( _SkyPatchwork, (Pos83*0.6 + mulTime89) ).r , tex2D( _SkyPatchwork, (rotator106*1.0 + 0.0) ).r );
			float2 temp_output_136_0 = ( i.uv_texcoord - float2( 0.5,0.5 ) );
			float dotResult137 = dot( temp_output_136_0 , temp_output_136_0 );
			float temp_output_134_0 = ( -2.0 * ( 1.0 - _PatchworkVariation ) );
			float temp_output_144_0 = saturate( (( ( 1.0 - _PatchworkHeight ) * temp_output_134_0 ) + (dotResult137 - 0.0) * (( temp_output_134_0 * -4.0 ) - ( ( 1.0 - _PatchworkHeight ) * temp_output_134_0 )) / (0.5 - 0.0)) );
			float clampResult148 = clamp( ( ( PatchworkPattern150 * temp_output_144_0 ) * -10.0 * _PatchworkBias ) , -1.0 , 1.0 );
			float PatchworkFinal149 = clampResult148;
			float4 ZenithColor111 = _ZenithColor;
			float4 HorizonColor110 = _HorizonColor;
			float4 SimpleSkyGradient68 = ( saturate( ( GradientPos97 + PatchworkFinal149 ) ) > _ClippingThreshold ? ZenithColor111 : HorizonColor110 );
			float3 ase_worldPos = i.worldPos;
			float3 normalizeResult26 = normalize( ( ase_worldPos - _WorldSpaceCameraPos ) );
			float dotResult27 = dot( normalizeResult26 , CZY_SunDirection );
			float SunDot51 = dotResult27;
			half4 SunFlare35 = abs( ( saturate( pow( abs( (SunDot51*0.5 + 0.5) ) , _SunFlareFalloff ) ) * _SunFlareColor ) );
			float4 SunRender70 = ( _SunColor * ( ( 1.0 - SunDot51 ) > ( pow( _SunSize , 3.0 ) * 0.0007 ) ? 0.0 : 1.0 ) );
			float3 normalizeResult65 = normalize( ( ase_worldPos - _WorldSpaceCameraPos ) );
			float dotResult66 = dot( normalizeResult65 , CZY_MoonDirection );
			float MoonDot67 = dotResult66;
			half4 MoonFlare76 = abs( ( saturate( pow( abs( (MoonDot67*0.5 + 0.5) ) , _MoonFlareFalloff ) ) * _MoonFlareColor ) );
			float mulTime202 = _Time.y * 0.005;
			float cos203 = cos( mulTime202 );
			float sin203 = sin( mulTime202 );
			float2 rotator203 = mul( Pos83 - float2( 0.5,0.5 ) , float2x2( cos203 , -sin203 , sin203 , cos203 )) + float2( 0.5,0.5 );
			float mulTime205 = _Time.y * -0.02;
			float simplePerlin2D213 = snoise( (Pos83*5.0 + mulTime205) );
			simplePerlin2D213 = simplePerlin2D213*0.5 + 0.5;
			float StarPlacementPattern217 = saturate( ( min( tex2D( _SkyPatchwork, (Pos83*5.0 + mulTime202) ).r , tex2D( _SkyPatchwork, (rotator203*2.0 + 0.0) ).r ) * simplePerlin2D213 * (0.2 + (SimpleGradient170 - 0.0) * (0.0 - 0.2) / (1.0 - 0.0)) ) );
			float2 panner321 = ( 1.0 * _Time.y * float2( 0.0007,0 ) + Pos83);
			float mulTime175 = _Time.y * 0.001;
			float cos176 = cos( 0.01 * _Time.y );
			float sin176 = sin( 0.01 * _Time.y );
			float2 rotator176 = mul( Pos83 - float2( 0.5,0.5 ) , float2x2( cos176 , -sin176 , sin176 , cos176 )) + float2( 0.5,0.5 );
			float temp_output_181_0 = min( tex2D( _GalaxyStars, (panner321*4.0 + mulTime175) ).r , tex2D( _GalaxyPattern, (rotator176*0.1 + 0.0) ).r );
			float2 panner318 = ( 1.0 * _Time.y * float2( 0.0007,0 ) + Pos83);
			float mulTime153 = _Time.y * 0.005;
			float2 panner320 = ( 1.0 * _Time.y * float2( 0.001,0 ) + Pos83);
			float mulTime165 = _Time.y * -0.02;
			float simplePerlin2D161 = snoise( (Pos83*15.0 + mulTime165) );
			simplePerlin2D161 = simplePerlin2D161*0.5 + 0.5;
			float GalaxyPattern160 = saturate( ( min( tex2D( _GalaxyPattern, (panner318*10.0 + mulTime153) ).r , tex2D( _GalaxyPattern, (panner320*7.0 + mulTime153) ).r ) * simplePerlin2D161 * (0.2 + (SimpleGradient170 - 0.0) * (0.0 - 0.2) / (0.05 - 0.0)) ) );
			float StarPattern182 = ( ( ( StarPlacementPattern217 * temp_output_181_0 ) + ( temp_output_181_0 * GalaxyPattern160 ) ) * ( 1.0 - MoonFlare76.r ) );
			float cos221 = cos( 0.02 * _Time.y );
			float sin221 = sin( 0.02 * _Time.y );
			float2 rotator221 = mul( Pos83 - float2( 0.5,0.5 ) , float2x2( cos221 , -sin221 , sin221 , cos221 )) + float2( 0.5,0.5 );
			float cos229 = cos( 0.04 * _Time.y );
			float sin229 = sin( 0.04 * _Time.y );
			float2 rotator229 = mul( Pos83 - float2( 0.5,0.5 ) , float2x2( cos229 , -sin229 , sin229 , cos229 )) + float2( 0.5,0.5 );
			float cos235 = cos( 0.01 * _Time.y );
			float sin235 = sin( 0.01 * _Time.y );
			float2 rotator235 = mul( Pos83 - float2( 0.5,0.5 ) , float2x2( cos235 , -sin235 , sin235 , cos235 )) + float2( 0.5,0.5 );
			float4 appendResult227 = (float4(tex2D( _SkyPatchwork, (rotator221*6.0 + 0.0) ).r , tex2D( _SkyPatchwork, (rotator229*5.5 + 2.04) ).r , tex2D( _SkyPatchwork, (rotator235*5.0 + 2.04) ).r , 1.0));
			float4 GalaxyColoring226 = appendResult227;
			float4 break241 = GalaxyColoring226;
			float4 FinalGalaxyColoring236 = ( ( _GalaxyColor1 * break241.r ) + ( _GalaxyColor2 * break241.g ) + ( _GalaxyColor3 * break241.b ) );
			float4 GalaxyFullColor194 = ( saturate( ( StarPattern182 * _StarColor ) ) + ( GalaxyPattern160 * FinalGalaxyColoring236 * _GalaxyMultiplier ) );
			Gradient gradient289 = NewGradient( 0, 8, 4, float4( 1, 0, 0, 0.1205921 ), float4( 1, 0.3135593, 0, 0.2441138 ), float4( 1, 0.8774895, 0.2216981, 0.3529412 ), float4( 0.3030533, 1, 0.2877358, 0.4529488 ), float4( 0.3726415, 1, 0.9559749, 0.5529412 ), float4( 0.4669811, 0.7253776, 1, 0.6470588 ), float4( 0.1561944, 0.3586135, 0.735849, 0.802945 ), float4( 0.2576377, 0.08721964, 0.5283019, 0.9264668 ), float2( 0, 0 ), float2( 0, 0.08235294 ), float2( 0.6039216, 0.8264744 ), float2( 0, 1 ), 0, 0, 0, 0 );
			float temp_output_276_0 = ( 1.0 - SunDot51 );
			float temp_output_275_0 = ( _RainbowSize * 0.01 );
			float temp_output_285_0 = ( temp_output_275_0 + ( _RainbowWidth * 0.01 ) );
			float4 RainbowClipping283 = ( SampleGradient( gradient289, (0.0 + (temp_output_276_0 - temp_output_275_0) * (1.0 - 0.0) / (temp_output_285_0 - temp_output_275_0)) ) * ( ( temp_output_276_0 < temp_output_275_0 ? 0.0 : 1.0 ) * ( temp_output_276_0 > temp_output_285_0 ? 0.0 : 1.0 ) ) * SampleGradient( gradient289, (0.0 + (temp_output_276_0 - temp_output_275_0) * (1.0 - 0.0) / (temp_output_285_0 - temp_output_275_0)) ).a * _RainbowIntensity );
			float cos316 = cos( -0.005 * _Time.y );
			float sin316 = sin( -0.005 * _Time.y );
			float2 rotator316 = mul( Pos83 - float2( 0.5,0.5 ) , float2x2( cos316 , -sin316 , sin316 , cos316 )) + float2( 0.5,0.5 );
			float cos295 = cos( 0.01 * _Time.y );
			float sin295 = sin( 0.01 * _Time.y );
			float2 rotator295 = mul( Pos83 - float2( 0.5,0.5 ) , float2x2( cos295 , -sin295 , sin295 , cos295 )) + float2( 0.5,0.5 );
			float4 transform325 = mul(unity_WorldToObject,float4( ase_worldPos , 0.0 ));
			float saferPower329 = abs( ( ( abs( transform325.y ) * 0.03 ) + 0.0 ) );
			float LightColumnsPattern309 = saturate( ( min( tex2D( _LightColumns, rotator316 ).r , tex2D( _LightColumns, rotator295 ).r ) * saturate( (1.0 + (saturate( pow( saferPower329 , 3.17 ) ) - 0.0) * (0.0 - 1.0) / (1.0 - 0.0)) ) ) );
			float4 LightColumnsColor315 = ( LightColumnsPattern309 * _LightColumnColor );
			o.Emission = ( SimpleSkyGradient68 + SunFlare35 + SunRender70 + MoonFlare76 + GalaxyFullColor194 + RainbowClipping283 + LightColumnsColor315 ).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
}
/*ASEBEGIN
Version=18935
0;1080;2194.286;607.5715;2184.29;3015.576;1;True;False
Node;AmplifyShaderEditor.CommentaryNode;72;-4728.316,-92.99277;Inherit;False;2040.225;680.2032;;15;58;54;56;60;57;55;76;73;64;61;62;63;65;66;67;Moon Block;0.514151,0.9898598,1,1;0;0
Node;AmplifyShaderEditor.WorldPosInputsNode;61;-4573.082,232.4811;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldSpaceCameraPos;62;-4642.492,381.5694;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleSubtractOpNode;63;-4393.336,294.6923;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;91;-4702.299,-2027.029;Inherit;False;1998.663;845.3734;;20;83;92;93;94;14;12;13;97;1;2;110;111;167;168;169;170;335;336;337;338;Variable Declaration;1,1,1,1;0;0
Node;AmplifyShaderEditor.NormalizeNode;65;-4266.221,293.4446;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector3Node;64;-4331.223,404.3474;Inherit;False;Global;CZY_MoonDirection;CZY_MoonDirection;12;0;Create;True;0;0;0;False;0;False;0,0,0;0,-0.7071067,0.7071069;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TextureCoordinatesNode;92;-4608.107,-1858.542;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DotProductOpNode;66;-4094.474,299.3718;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;151;-593.1069,-1979.37;Inherit;False;2830.178;925.2802;;28;155;156;152;153;194;190;248;191;193;246;247;188;185;189;160;198;164;159;161;172;163;158;157;166;165;162;318;320;Galaxy Pattern;1,0.5235849,0.5235849,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;67;-3946.877,291.1702;Inherit;False;MoonDot;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;83;-4401.376,-1864.431;Inherit;False;Pos;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;167;-3552,-1936;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;201;2946.345,-2602.76;Inherit;False;83;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;202;2946.345,-2522.76;Inherit;False;1;0;FLOAT;0.005;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;152;-524.5658,-1769.642;Inherit;False;83;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;168;-3344,-1936;Inherit;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;60;-4677.368,9.632609;Inherit;False;67;MoonDot;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;318;-256.3027,-1854.114;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.0007,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotatorNode;203;3138.345,-2474.76;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;153;-524.5658,-1689.642;Inherit;False;1;0;FLOAT;0.005;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;54;-4508.647,15.1096;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;320;-264.3027,-1655.114;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.001,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DotProductOpNode;169;-3152,-1936;Inherit;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;119;-2277.841,-1985.728;Inherit;False;1445.173;546.2656;;9;150;81;80;79;85;86;106;89;84;Sky Patchwork;1,0.5235849,0.5235849,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;128;-2371.156,-3022.132;Inherit;False;2111.501;762.0129;;19;149;148;147;146;145;144;142;141;139;138;137;136;134;133;132;131;130;129;341;Patchwork Block;1,0.5882353,0.685091,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;84;-2208,-1776;Inherit;False;83;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;205;3203.469,-2200.533;Inherit;False;1;0;FLOAT;-0.02;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;204;3314.346,-2474.76;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;2;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;165;-24.48972,-1373.035;Inherit;False;1;0;FLOAT;-0.02;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;56;-4313.313,14.24778;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;89;-2208,-1696;Inherit;False;1;0;FLOAT;0.001;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;155;-26.35724,-1849.692;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;10;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;207;3250.346,-2666.76;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;206;3243.117,-2287.766;Inherit;False;83;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;55;-4405.444,144.3357;Half;False;Property;_MoonFlareFalloff;Moon Flare Falloff;7;1;[HideInInspector];Create;True;0;0;0;False;0;False;1;24.4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;170;-2960,-1936;Inherit;False;SimpleGradient;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;162;15.1574,-1460.268;Inherit;False;83;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;156;47.34286,-1652.692;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;7;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;208;3485.612,-2145.118;Inherit;False;170;SimpleGradient;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;210;3408.117,-2282.766;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotatorNode;106;-2016,-1648;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;130;-2291.156,-2398.132;Inherit;False;Property;_PatchworkVariation;Patchwork Variation;19;1;[HideInInspector];Create;True;0;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;166;257.6526,-1317.62;Inherit;False;170;SimpleGradient;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;211;3490.346,-2506.76;Inherit;True;Property;_TextureSample5;Texture Sample 5;16;0;Create;True;0;0;0;False;0;False;-1;None;59cb97507f14c1d468e967d73ca67a9b;True;0;False;white;Auto;False;Instance;79;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;173;-17.54585,-2715.89;Inherit;False;2042.533;553.0383;;19;182;200;218;184;219;181;186;180;179;178;177;176;175;174;253;254;255;257;321;Stars;1,0.7345774,0.5254902,1;0;0
Node;AmplifyShaderEditor.SamplerNode;157;262.3864,-1871.262;Inherit;True;Property;_GalaxyPattern;Galaxy Pattern;15;0;Create;True;0;0;0;False;0;False;-1;None;59cb97507f14c1d468e967d73ca67a9b;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldPosInputsNode;324;896,208;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;232;-151.9267,-552.0871;Inherit;False;83;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;158;261.0864,-1679.262;Inherit;True;Property;_TextureSample1;Texture Sample 1;15;0;Create;True;0;0;0;False;0;False;-1;None;59cb97507f14c1d468e967d73ca67a9b;True;0;False;white;Auto;False;Instance;157;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;57;-4177.911,11.49558;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;209;3490.346,-2698.76;Inherit;True;Property;_TextureSample4;Texture Sample 4;16;0;Create;True;0;0;0;False;0;False;-1;None;59cb97507f14c1d468e967d73ca67a9b;True;0;False;white;Auto;False;Instance;79;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScaleAndOffsetNode;163;180.1574,-1455.268;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;15;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;230;-156.8061,-742.4568;Inherit;False;83;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;224;-152,-957;Inherit;False;83;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;161;373.581,-1461.98;Inherit;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;131;-2019.156,-2398.132;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;235;24.07328,-552.0871;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0.01;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;58;-4022.834,8.715373;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;85;-1904,-1840;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;0.6;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCRemapNode;172;449.6527,-1317.62;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0.05;False;3;FLOAT;0.2;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;174;52.29524,-2506.162;Inherit;False;83;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCRemapNode;212;3677.612,-2145.118;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0.2;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMinOpNode;214;3794.346,-2586.76;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;86;-1840,-1648;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMinOpNode;159;566.3865,-1759.262;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;221;24,-957;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0.02;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WorldToObjectTransfNode;325;1056,208;Inherit;False;1;0;FLOAT4;0,0,0,1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.NoiseGeneratorNode;213;3601.541,-2289.478;Inherit;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;129;-2149.156,-2525.132;Inherit;False;Property;_PatchworkHeight;Patchwork Height;18;2;[HideInInspector];[Header];Create;True;1;Border Clouds;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;229;19.19389,-742.4568;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0.04;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;133;-2227.156,-2750.132;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;73;-3978.105,94.1355;Inherit;False;Property;_MoonFlareColor;Moon Flare Color;9;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;0.355693,0.4595688,0.4802988,1;0.008822088,0.03066038,0.02702066,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;71;-4733.934,-925.9814;Inherit;False;2040.225;680.2032;;26;51;27;49;26;24;22;23;35;34;33;31;32;29;53;37;38;47;70;44;41;45;43;46;52;42;331;Sun Block;0.514151,0.9898598,1,1;0;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;136;-2003.156,-2750.132;Inherit;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.AbsOpNode;330;1232,208;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;74;-3756.935,6.347229;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldPosInputsNode;22;-4613.366,-637.3319;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.PannerNode;321;236.2448,-2577.34;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.0007,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;175;44.29524,-2419.162;Inherit;False;1;0;FLOAT;0.001;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;79;-1660,-1893;Inherit;True;Property;_SkyPatchwork;Sky Patchwork;16;0;Create;True;0;0;0;False;0;False;-1;None;59cb97507f14c1d468e967d73ca67a9b;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;134;-1843.156,-2430.132;Inherit;False;2;2;0;FLOAT;-2;False;1;FLOAT;-0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;164;737.6525,-1653.62;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;176;244.2952,-2378.162;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0.01;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;215;3965.612,-2481.118;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos;23;-4682.777,-488.2437;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ScaleAndOffsetNode;228;211.1939,-742.4568;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;5.5;False;2;FLOAT;2.04;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;222;216,-957;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;6;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;234;216.0733,-552.0871;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;5;False;2;FLOAT;2.04;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;132;-1841.156,-2521.132;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;80;-1664,-1680;Inherit;True;Property;_TextureSample0;Texture Sample 0;16;0;Create;True;0;0;0;False;0;False;-1;None;59cb97507f14c1d468e967d73ca67a9b;True;0;False;white;Auto;False;Instance;79;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;231;409.8319,-744.5311;Inherit;True;Property;_TextureSample7;Texture Sample 7;16;0;Create;True;0;0;0;False;0;False;-1;None;59cb97507f14c1d468e967d73ca67a9b;True;0;False;white;Auto;False;Instance;79;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;24;-4433.621,-575.1206;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.AbsOpNode;75;-3618.212,5.289185;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DotProductOpNode;137;-1811.156,-2750.132;Inherit;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;198;865.6525,-1653.62;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMinOpNode;81;-1360,-1760;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;178;420.2953,-2378.162;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;0.1;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;335;-4272,-1536;Inherit;False;170;SimpleGradient;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;139;-1651.156,-2526.132;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;138;-1651.156,-2430.132;Inherit;False;2;2;0;FLOAT;-4;False;1;FLOAT;-4;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;216;4093.612,-2481.118;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;177;408.2952,-2575.362;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;4;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;233;414.7113,-554.1614;Inherit;True;Property;_TextureSample8;Texture Sample 8;16;0;Create;True;0;0;0;False;0;False;-1;None;59cb97507f14c1d468e967d73ca67a9b;True;0;False;white;Auto;False;Instance;79;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;327;1376,208;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.03;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;223;414.638,-959.0742;Inherit;True;Property;_TextureSample6;Texture Sample 6;16;0;Create;True;0;0;0;False;0;False;-1;None;59cb97507f14c1d468e967d73ca67a9b;True;0;False;white;Auto;False;Instance;79;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;227;770.8534,-804.9755;Inherit;False;COLOR;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCRemapNode;141;-1459.156,-2670.132;Inherit;True;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0.5;False;3;FLOAT;-2;False;4;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;179;596.2953,-2602.162;Inherit;True;Property;_GalaxyStars;Galaxy Stars;14;0;Create;True;0;0;0;False;0;False;-1;None;93557f6a2b0824644b6ddaa6442e6c8e;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;217;4237.612,-2481.118;Inherit;False;StarPlacementPattern;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;338;-4064,-1536;Inherit;True;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;49;-4359.506,-458.4658;Inherit;False;Global;CZY_SunDirection;CZY_SunDirection;12;0;Create;True;0;0;0;False;0;False;0,0,0;-0.4940032,0.6833311,0.5376053;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;160;1009.653,-1653.62;Inherit;False;GalaxyPattern;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;328;1520,208;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;180;596.2953,-2410.162;Inherit;True;Property;_TextureSample2;Texture Sample 2;15;0;Create;True;0;0;0;False;0;False;-1;None;59cb97507f14c1d468e967d73ca67a9b;True;0;False;white;Auto;False;Instance;157;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;150;-1221.355,-1764.187;Inherit;False;PatchworkPattern;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;26;-4306.505,-576.3683;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;76;-3487.056,-0.2542725;Half;False;MoonFlare;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;336;-3792,-1536;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;226;925,-810;Inherit;False;GalaxyColoring;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;144;-1187.156,-2670.132;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;294;1515.55,-256.3511;Inherit;False;83;Pos;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMinOpNode;181;912,-2496;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;14;-3792,-1456;Inherit;False;Property;_Power;Power;5;1;[HideInInspector];Create;True;0;0;0;False;0;False;1;0.96;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;329;1664,208;Inherit;False;True;2;0;FLOAT;0;False;1;FLOAT;3.17;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;219;948.8347,-2650.82;Inherit;False;217;StarPlacementPattern;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;27;-4134.759,-570.4412;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;142;-1264.156,-2748.132;Inherit;False;150;PatchworkPattern;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;254;1189,-2345;Inherit;False;76;MoonFlare;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;186;964,-2328;Inherit;False;160;GalaxyPattern;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;255;1352.066,-2339.02;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;184;1184,-2448;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;242;2284.784,-1037.094;Inherit;False;226;GalaxyColoring;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;218;1184,-2544;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;51;-3987.163,-578.6428;Inherit;False;SunDot;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;12;-3632,-1536;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;316;1790.768,-310.1481;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;-0.005;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;145;-1171.156,-2558.132;Inherit;False;Property;_PatchworkBias;Patchwork Bias;20;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;0.067;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;323;1824,208;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;146;-1044.156,-2705.132;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;295;1788.549,-137.3511;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0.01;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;238;2432,-1600;Inherit;False;Property;_GalaxyColor1;Galaxy Color 1;3;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;0.6399965,0.9474089,0.9622642,0;0,0.5019608,1.003922,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;237;2432,-1424;Inherit;False;Property;_GalaxyColor2;Galaxy Color 2;2;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;0.6399965,0.9474089,0.9622642,0;0.1176471,0.454902,0.07843138,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;200;1344,-2512;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;258;-4760.767,751.4372;Inherit;False;2156.234;658.7953;;16;283;280;290;292;288;278;289;291;286;276;285;271;287;275;270;284;Rainbow Block;1,0.9770144,0.5137255,1;0;0
Node;AmplifyShaderEditor.OneMinusNode;257;1469.192,-2339.736;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;239;2432,-1248;Inherit;False;Property;_GalaxyColor3;Galaxy Color 3;1;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;0.6399965,0.9474089,0.9622642,0;0.1647059,0.07843138,0.454902,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;305;2182.115,105.2909;Inherit;True;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;303;2059.549,-160.3511;Inherit;True;Property;_TextureSample9;Texture Sample 9;17;0;Create;True;0;0;0;False;0;False;-1;None;59cb97507f14c1d468e967d73ca67a9b;True;0;False;white;Auto;False;Instance;301;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;53;-4654.877,-837.8683;Inherit;False;51;SunDot;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;147;-851.1564,-2686.132;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;-10;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;241;2522.585,-1078.494;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SaturateNode;13;-3488,-1536;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;301;2060.849,-352.351;Inherit;True;Property;_LightColumns;Light Columns;17;0;Create;True;0;0;0;False;0;False;-1;None;5577fd51123d5de4d93e8555a7bb084e;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;240;2784,-1520;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;29;-4486.156,-832.3912;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;243;2784,-1392;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;337;-3344,-1536;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;244;2784,-1264;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;253;1640.694,-2510.95;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMinOpNode;304;2364.849,-240.3511;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;284;-4655.661,1012.623;Inherit;False;Property;_RainbowSize;Rainbow Size;12;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;78.7;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;317;2451.396,96.1091;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;148;-707.1564,-2670.132;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;-1;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;270;-4664.661,1115.623;Inherit;False;Property;_RainbowWidth;Rainbow Width;11;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;11;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;287;-4472.661,1115.623;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;97;-3184,-1536;Inherit;False;GradientPos;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;271;-4440.661,1227.623;Inherit;False;51;SunDot;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;275;-4475.661,1015.623;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;31;-4350.381,-697.744;Float;False;Property;_SunFlareFalloff;Sun Flare Falloff;6;0;Create;False;0;0;0;False;0;False;1;43.7;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;307;2610.115,-132.7091;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;245;3008,-1408;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.AbsOpNode;32;-4290.822,-833.2531;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;182;1801,-2513;Inherit;False;StarPattern;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;149;-528.0198,-2678.309;Inherit;True;PatchworkFinal;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;42;-3872,-496;Inherit;False;Property;_SunSize;Sun Size;10;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;0.38;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;2;-4074.189,-1761.397;Inherit;False;Property;_ZenithColor;Zenith Color;4;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;0.4000979,0.6638572,0.764151,0;0.06775116,0.1963049,0.3185095,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;33;-4155.419,-836.0052;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;185;1444.961,-1753.405;Inherit;False;182;StarPattern;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;189;1416.468,-1680.191;Inherit;False;Property;_StarColor;Star Color;21;1;[HDR];Create;True;0;0;0;False;0;False;0,0,0,0;0.2633304,0.2633304,0.2633304,0.01785974;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;102;-1738,-539;Inherit;False;97;GradientPos;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;276;-4248.661,1227.623;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;114;-1774,-430;Inherit;False;149;PatchworkFinal;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;1;-4079.039,-1939.879;Inherit;False;Property;_HorizonColor;Horizon Color;0;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;0.6399965,0.9474089,0.9622642,0;0.2872422,0.4742101,0.6315992,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;52;-3749.203,-394.6242;Inherit;False;51;SunDot;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;331;-3664,-496;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;236;3152,-1408;Inherit;False;FinalGalaxyColoring;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;285;-4328.661,1083.623;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;308;2738.115,-132.7091;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;111;-3836.003,-1761.749;Inherit;False;ZenithColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;43;-3557.892,-389.7785;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;-3506.887,-501.4186;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.0007;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;193;1538.878,-1489.766;Inherit;False;160;GalaxyPattern;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;37;-3878.518,-783.3228;Inherit;False;Property;_SunFlareColor;Sun Flare Color;8;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;0.355693,0.4595688,0.4802988,1;0.257921,0.3264449,0.3396226,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;247;1443.316,-1329.237;Inherit;False;Property;_GalaxyMultiplier;Galaxy Multiplier;23;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;309;2882.115,-132.7091;Inherit;False;LightColumnsPattern;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GradientNode;289;-3611.335,853.5206;Inherit;False;0;8;4;1,0,0,0.1205921;1,0.3135593,0,0.2441138;1,0.8774895,0.2216981,0.3529412;0.3030533,1,0.2877358,0.4529488;0.3726415,1,0.9559749,0.5529412;0.4669811,0.7253776,1,0.6470588;0.1561944,0.3586135,0.735849,0.802945;0.2576377,0.08721964,0.5283019,0.9264668;0,0;0,0.08235294;0.6039216,0.8264744;0,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.Compare;278;-4024.661,1163.623;Inherit;False;2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;110;-3835.76,-1941.606;Inherit;False;HorizonColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;188;1625.175,-1732.989;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;103;-1546,-507;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Compare;286;-4024.661,1003.623;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;246;1494.297,-1407.131;Inherit;False;236;FinalGalaxyColoring;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCRemapNode;291;-3570.049,973.7616;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;34;-4000.341,-838.7855;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Compare;41;-3317.57,-487.7016;Inherit;False;2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;113;-1484.993,-625.6973;Inherit;False;111;ZenithColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;292;-3346.207,1154.068;Inherit;False;Property;_RainbowIntensity;Rainbow Intensity;24;1;[HideInInspector];Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;314;1451.55,767.8168;Inherit;False;309;LightColumnsPattern;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;312;1464.469,888.144;Inherit;False;Property;_LightColumnColor;Light Column Color;22;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;0,0,0,0;0.2848729,1.698687,2.015212,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;191;1728.209,-1438.738;Inherit;False;3;3;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;340;-1516.435,-405.2894;Inherit;False;Property;_ClippingThreshold;Clipping Threshold;25;0;Create;True;0;0;0;False;0;False;0.5;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;288;-3816.494,1090.438;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;112;-1539.993,-726.6973;Inherit;False;110;HorizonColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;38;-3661.348,-839.1111;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;248;1761.316,-1733.237;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;45;-3412.185,-673.9927;Inherit;False;Property;_SunColor;Sun Color;13;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;0,0,0,0;10.43295,10.43295,10.43295,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GradientSampleNode;290;-3365.049,931.7617;Inherit;True;2;0;OBJECT;;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;115;-1434,-507;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;190;1893.209,-1583.738;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;44;-3115.263,-521.6712;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;280;-3026.661,1079.623;Inherit;False;4;4;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.Compare;339;-1189.435,-564.2894;Inherit;True;2;4;0;FLOAT;0;False;1;FLOAT;0.5;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.AbsOpNode;47;-3522.625,-840.1691;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;313;1725.55,802.8168;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;70;-2959.761,-526.3458;Inherit;False;SunRender;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;68;-870.7037,-560.8309;Inherit;False;SimpleSkyGradient;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;194;2019.754,-1591.95;Inherit;False;GalaxyFullColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;315;1884.55,797.8168;Inherit;False;LightColumnsColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;35;-3391.469,-845.7126;Half;False;SunFlare;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;283;-2850.661,1074.623;Inherit;False;RainbowClipping;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;311;-1150.603,642.7863;Inherit;False;315;LightColumnsColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;39;-1088.632,251.1212;Inherit;False;35;SunFlare;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;195;-1134.454,492.6038;Inherit;False;194;GalaxyFullColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;77;-1098.976,332.5836;Inherit;False;70;SunRender;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;196;-1147.351,567.3314;Inherit;False;283;RainbowClipping;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;69;-1131.687,172.7539;Inherit;False;68;SimpleSkyGradient;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;78;-1104.976,411.5836;Inherit;False;76;MoonFlare;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;36;-877.3224,316.4634;Inherit;False;7;7;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleTimeNode;93;-4622.788,-1712.352;Inherit;False;1;0;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;341;-1031.29,-2857.576;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;94;-4453.894,-1715.211;Inherit;False;Time;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;104.1543,85.88161;Float;False;True;-1;2;;0;0;Unlit;Distant Lands/Cozy/Stylized Painted Sky;False;False;False;False;True;True;True;True;True;True;False;False;False;False;False;False;False;False;False;False;False;Front;0;False;-1;7;False;-1;False;0;False;-1;0;False;-1;True;0;Translucent;0.5;True;False;-100;False;Opaque;;Transparent;All;18;all;True;True;True;True;0;False;-1;True;221;False;-1;255;False;-1;255;False;-1;7;False;-1;3;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;63;0;61;0
WireConnection;63;1;62;0
WireConnection;65;0;63;0
WireConnection;66;0;65;0
WireConnection;66;1;64;0
WireConnection;67;0;66;0
WireConnection;83;0;92;0
WireConnection;168;0;167;0
WireConnection;318;0;152;0
WireConnection;203;0;201;0
WireConnection;203;2;202;0
WireConnection;54;0;60;0
WireConnection;320;0;152;0
WireConnection;169;0;168;0
WireConnection;169;1;168;0
WireConnection;204;0;203;0
WireConnection;56;0;54;0
WireConnection;155;0;318;0
WireConnection;155;2;153;0
WireConnection;207;0;201;0
WireConnection;207;2;202;0
WireConnection;170;0;169;0
WireConnection;156;0;320;0
WireConnection;156;2;153;0
WireConnection;210;0;206;0
WireConnection;210;2;205;0
WireConnection;106;0;84;0
WireConnection;106;2;89;0
WireConnection;211;1;204;0
WireConnection;157;1;155;0
WireConnection;158;1;156;0
WireConnection;57;0;56;0
WireConnection;57;1;55;0
WireConnection;209;1;207;0
WireConnection;163;0;162;0
WireConnection;163;2;165;0
WireConnection;161;0;163;0
WireConnection;131;0;130;0
WireConnection;235;0;232;0
WireConnection;58;0;57;0
WireConnection;85;0;84;0
WireConnection;85;2;89;0
WireConnection;172;0;166;0
WireConnection;212;0;208;0
WireConnection;214;0;209;1
WireConnection;214;1;211;1
WireConnection;86;0;106;0
WireConnection;159;0;157;1
WireConnection;159;1;158;1
WireConnection;221;0;224;0
WireConnection;325;0;324;0
WireConnection;213;0;210;0
WireConnection;229;0;230;0
WireConnection;136;0;133;0
WireConnection;330;0;325;2
WireConnection;74;0;58;0
WireConnection;74;1;73;0
WireConnection;321;0;174;0
WireConnection;79;1;85;0
WireConnection;134;1;131;0
WireConnection;164;0;159;0
WireConnection;164;1;161;0
WireConnection;164;2;172;0
WireConnection;176;0;174;0
WireConnection;215;0;214;0
WireConnection;215;1;213;0
WireConnection;215;2;212;0
WireConnection;228;0;229;0
WireConnection;222;0;221;0
WireConnection;234;0;235;0
WireConnection;132;0;129;0
WireConnection;80;1;86;0
WireConnection;231;1;228;0
WireConnection;24;0;22;0
WireConnection;24;1;23;0
WireConnection;75;0;74;0
WireConnection;137;0;136;0
WireConnection;137;1;136;0
WireConnection;198;0;164;0
WireConnection;81;0;79;1
WireConnection;81;1;80;1
WireConnection;178;0;176;0
WireConnection;139;0;132;0
WireConnection;139;1;134;0
WireConnection;138;0;134;0
WireConnection;216;0;215;0
WireConnection;177;0;321;0
WireConnection;177;2;175;0
WireConnection;233;1;234;0
WireConnection;327;0;330;0
WireConnection;223;1;222;0
WireConnection;227;0;223;1
WireConnection;227;1;231;1
WireConnection;227;2;233;1
WireConnection;141;0;137;0
WireConnection;141;3;139;0
WireConnection;141;4;138;0
WireConnection;179;1;177;0
WireConnection;217;0;216;0
WireConnection;338;0;335;0
WireConnection;160;0;198;0
WireConnection;328;0;327;0
WireConnection;180;1;178;0
WireConnection;150;0;81;0
WireConnection;26;0;24;0
WireConnection;76;0;75;0
WireConnection;336;0;338;0
WireConnection;226;0;227;0
WireConnection;144;0;141;0
WireConnection;181;0;179;1
WireConnection;181;1;180;1
WireConnection;329;0;328;0
WireConnection;27;0;26;0
WireConnection;27;1;49;0
WireConnection;255;0;254;0
WireConnection;184;0;181;0
WireConnection;184;1;186;0
WireConnection;218;0;219;0
WireConnection;218;1;181;0
WireConnection;51;0;27;0
WireConnection;12;0;336;0
WireConnection;12;1;14;0
WireConnection;316;0;294;0
WireConnection;323;0;329;0
WireConnection;146;0;142;0
WireConnection;146;1;144;0
WireConnection;295;0;294;0
WireConnection;200;0;218;0
WireConnection;200;1;184;0
WireConnection;257;0;255;0
WireConnection;305;0;323;0
WireConnection;303;1;295;0
WireConnection;147;0;146;0
WireConnection;147;2;145;0
WireConnection;241;0;242;0
WireConnection;13;0;12;0
WireConnection;301;1;316;0
WireConnection;240;0;238;0
WireConnection;240;1;241;0
WireConnection;29;0;53;0
WireConnection;243;0;237;0
WireConnection;243;1;241;1
WireConnection;337;0;13;0
WireConnection;244;0;239;0
WireConnection;244;1;241;2
WireConnection;253;0;200;0
WireConnection;253;1;257;0
WireConnection;304;0;301;1
WireConnection;304;1;303;1
WireConnection;317;0;305;0
WireConnection;148;0;147;0
WireConnection;287;0;270;0
WireConnection;97;0;337;0
WireConnection;275;0;284;0
WireConnection;307;0;304;0
WireConnection;307;1;317;0
WireConnection;245;0;240;0
WireConnection;245;1;243;0
WireConnection;245;2;244;0
WireConnection;32;0;29;0
WireConnection;182;0;253;0
WireConnection;149;0;148;0
WireConnection;33;0;32;0
WireConnection;33;1;31;0
WireConnection;276;0;271;0
WireConnection;331;0;42;0
WireConnection;236;0;245;0
WireConnection;285;0;275;0
WireConnection;285;1;287;0
WireConnection;308;0;307;0
WireConnection;111;0;2;0
WireConnection;43;0;52;0
WireConnection;46;0;331;0
WireConnection;309;0;308;0
WireConnection;278;0;276;0
WireConnection;278;1;285;0
WireConnection;110;0;1;0
WireConnection;188;0;185;0
WireConnection;188;1;189;0
WireConnection;103;0;102;0
WireConnection;103;1;114;0
WireConnection;286;0;276;0
WireConnection;286;1;275;0
WireConnection;291;0;276;0
WireConnection;291;1;275;0
WireConnection;291;2;285;0
WireConnection;34;0;33;0
WireConnection;41;0;43;0
WireConnection;41;1;46;0
WireConnection;191;0;193;0
WireConnection;191;1;246;0
WireConnection;191;2;247;0
WireConnection;288;0;286;0
WireConnection;288;1;278;0
WireConnection;38;0;34;0
WireConnection;38;1;37;0
WireConnection;248;0;188;0
WireConnection;290;0;289;0
WireConnection;290;1;291;0
WireConnection;115;0;103;0
WireConnection;190;0;248;0
WireConnection;190;1;191;0
WireConnection;44;0;45;0
WireConnection;44;1;41;0
WireConnection;280;0;290;0
WireConnection;280;1;288;0
WireConnection;280;2;290;4
WireConnection;280;3;292;0
WireConnection;339;0;115;0
WireConnection;339;1;340;0
WireConnection;339;2;113;0
WireConnection;339;3;112;0
WireConnection;47;0;38;0
WireConnection;313;0;314;0
WireConnection;313;1;312;0
WireConnection;70;0;44;0
WireConnection;68;0;339;0
WireConnection;194;0;190;0
WireConnection;315;0;313;0
WireConnection;35;0;47;0
WireConnection;283;0;280;0
WireConnection;36;0;69;0
WireConnection;36;1;39;0
WireConnection;36;2;77;0
WireConnection;36;3;78;0
WireConnection;36;4;195;0
WireConnection;36;5;196;0
WireConnection;36;6;311;0
WireConnection;341;0;142;0
WireConnection;341;1;144;0
WireConnection;94;0;93;0
WireConnection;0;2;36;0
ASEEND*/
//CHKSM=ABAF850ABF5BA180BEE5B44EBA1239D9E6AD44CF