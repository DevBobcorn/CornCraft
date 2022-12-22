Shader "Hovl/Particles/Blend_Normals"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		_Noise("Noise", 2D) = "white" {}
		_SpeedMainTexUVNoiseZW("Speed MainTex U/V + Noise Z/W", Vector) = (0,0,0,0)
		_Emission("Emission", Float) = 2
		_Color("Color", Color) = (0.5,0.5,0.5,1)
		[MaterialToggle] _Usedepth ("Use depth?", Float ) = 0
		_Depthpower("Depth power", Float) = 1
		[MaterialToggle] _Usecenterglow("Use center glow?", Float) = 0
		_Mask("Mask", 2D) = "white" {}
		_Opacity("Opacity", Range( 0 , 1)) = 1
		_NormalMap("Normal Map", 2D) = "white" {}
		_NormalScale("Normal Scale", Float) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Off
		CGPROGRAM
		#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
		#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
		#endif
		#include "UnityStandardUtils.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		//#pragma target 3.0
		#pragma surface surf Lambert alpha:fade keepalpha noshadow 
		#pragma multi_compile _ SOFTPARTICLES_ON
		struct Input
		{
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
			float4 screenPos;
		};

		uniform float _NormalScale;
		uniform sampler2D _NormalMap;
		uniform float4 _SpeedMainTexUVNoiseZW;
		uniform float4 _NormalMap_ST;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform sampler2D _Noise;
		uniform float4 _Noise_ST;
		uniform float4 _Color;
		uniform sampler2D _Mask;
		uniform float4 _Mask_ST;
		uniform float _Emission;
		uniform sampler2D _CameraDepthTexture;
		uniform float _Depthpower;
		uniform fixed _Usedepth;
		uniform float _Opacity;
		uniform fixed _Usecenterglow;

		void surf( Input i , inout SurfaceOutput o )
		{
			UNITY_SETUP_INSTANCE_ID( i );
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( i );
		
			float2 appendResult21 = (float2(_SpeedMainTexUVNoiseZW.x , _SpeedMainTexUVNoiseZW.y));
			float2 temp_output_24_0 = ( appendResult21 * _Time.y );
			float2 uv_NormalMap = i.uv_texcoord * _NormalMap_ST.xy + _NormalMap_ST.zw;
			o.Normal = UnpackScaleNormal( tex2D( _NormalMap, ( temp_output_24_0 + uv_NormalMap ) ), _NormalScale );
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float4 tex2DNode13 = tex2D( _MainTex, ( temp_output_24_0 + uv_MainTex ) );
			float2 uv_Noise = i.uv_texcoord * _Noise_ST.xy + _Noise_ST.zw;
			float2 appendResult22 = (float2(_SpeedMainTexUVNoiseZW.z , _SpeedMainTexUVNoiseZW.w));
			float4 tex2DNode14 = tex2D( _Noise, ( uv_Noise + ( _Time.y * appendResult22 ) ) );
			float3 temp_output_30_0 = ( (tex2DNode13).rgb * (tex2DNode14).rgb * (_Color).rgb * (i.vertexColor).rgb );
			float2 uv_Mask = i.uv_texcoord * _Mask_ST.xy + _Mask_ST.zw;
			float3 temp_output_58_0 = (tex2D( _Mask, uv_Mask )).rgb;
			float3 temp_cast_0 = ((1.0 + (0.0 - 0.0) * (0.0 - 1.0) / (1.0 - 0.0))).xxx;
			float3 clampResult38 = clamp( ( temp_output_58_0 - temp_cast_0 ) , float3( 0,0,0 ) , float3( 1,1,1 ) );
			float3 clampResult40 = clamp( ( temp_output_58_0 * clampResult38 ) , float3( 0,0,0 ) , float3( 1,1,1 ) );
			float3 staticSwitch46 = lerp(temp_output_30_0, ( temp_output_30_0 * clampResult40 ), _Usecenterglow);
			o.Albedo = staticSwitch46;
			o.Emission = ( staticSwitch46 * _Emission );
			float temp_output_60_0 = ( tex2DNode13.a * tex2DNode14.a * _Color.a * i.vertexColor.a );
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float screenDepth49 = LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture,UNITY_PROJ_COORD( ase_screenPos ))));
			float distanceDepth49 = abs( ( screenDepth49 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( _Depthpower ) );
			float clampResult53 = clamp( distanceDepth49 , 0.0 , 1.0 );		
			float4 staticSwitch47 = temp_output_60_0;
			#ifdef SOFTPARTICLES_ON
				staticSwitch47 *= lerp(1, clampResult53, _Usedepth);
			#endif
			o.Alpha = ( staticSwitch47 * _Opacity );
		}

		ENDCG
	}
}

/*ASEBEGIN
Version=16200
663;92;634;655;417.4107;209.4802;1.6;True;False
Node;AmplifyShaderEditor.Vector4Node;15;-3103.802,24.82627;Float;False;Property;_SpeedMainTexUVNoiseZW;Speed MainTex U/V + Noise Z/W;3;0;Create;True;0;0;False;0;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;21;-2742.397,-37.24139;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TimeNode;17;-2772.28,53.38881;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;22;-2739.856,185.038;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;33;-2267.097,648.0693;Float;True;Property;_Mask;Mask;7;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;58;-1941.441,650.6088;Float;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;69;-2339.192,119.1897;Float;False;0;14;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;29;-2305.455,-184.718;Float;False;0;13;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;36;-1923.092,912.1827;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;-2523.281,230.8774;Float;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;24;-2525.724,-35.90024;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;27;-2055.981,216.3253;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;37;-1700.092,897.7924;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;26;-2002.501,-62.12165;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ClampOpNode;38;-1532.594,897.8295;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;1,1,1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.VertexColorNode;32;-1670.612,486.0577;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;13;-1803.192,-66.2159;Float;True;Property;_MainTex;MainTex;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;14;-1804.579,119.2214;Float;True;Property;_Noise;Noise;2;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;31;-1728.612,316.0578;Float;False;Property;_Color;Color;5;0;Create;True;0;0;False;0;0.5,0.5,0.5,1;0.5,0.5,0.5,1;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;50;-992.9719,832.5517;Float;False;Property;_Depthpower;Depth power;6;0;Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;57;-1451.257,454.9211;Float;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DepthFade;49;-781.927,777.129;Float;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;56;-1439.297,239.841;Float;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;39;-1344.594,664.8295;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode;54;-1418.414,-51.20496;Float;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode;55;-1419.297,79.84097;Float;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;60;-636.2719,433.2454;Float;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;53;-514.3105,699.7543;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;40;-1175.594,664.8295;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;1,1,1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;-1137.627,185.1029;Float;False;4;4;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WireNode;74;-2347.178,-236.1942;Float;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;72;-959.345,-129.9445;Float;False;0;70;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;41;-942.8278,447.3033;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;48;-345.5348,557.533;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;71;-385.8843,15.4308;Float;False;Property;_NormalScale;Normal Scale;10;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;73;-631.4565,-249.8404;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;62;-119.2192,570.4644;Float;False;Property;_Opacity;Opacity;8;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;76;-659.4539,161.4998;Float;False;Property;_Usecenterglow;Use center glow?;12;0;Create;True;0;0;False;0;0;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ToggleSwitchNode;77;-118.1879,421.3432;Float;False;Property;_Usedepth;Use depth?;11;0;Create;True;0;0;False;0;0;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;52;-29.05984,321.4998;Float;False;Property;_Emission;Emission;4;0;Create;True;0;0;False;0;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;70;-107.6205,-52.2539;Float;True;Property;_NormalMap;Normal Map;9;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;51;229.2081,258.9761;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;61;262.643,402.9155;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;78;166.1326,677.9241;Float;False;Constant;_Float0;Float 0;13;0;Create;True;0;0;False;0;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;80;462.3044,181.8964;Float;False;True;2;Float;ASEMaterialInspector;0;0;Lambert;Hovl/Particles/Blend_Normals;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Off;2;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;False;0;False;Transparent;;Geometry;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;21;0;15;1
WireConnection;21;1;15;2
WireConnection;22;0;15;3
WireConnection;22;1;15;4
WireConnection;58;0;33;0
WireConnection;23;0;17;2
WireConnection;23;1;22;0
WireConnection;24;0;21;0
WireConnection;24;1;17;2
WireConnection;27;0;69;0
WireConnection;27;1;23;0
WireConnection;37;0;58;0
WireConnection;37;1;36;0
WireConnection;26;0;24;0
WireConnection;26;1;29;0
WireConnection;38;0;37;0
WireConnection;13;1;26;0
WireConnection;14;1;27;0
WireConnection;57;0;32;0
WireConnection;49;0;50;0
WireConnection;56;0;31;0
WireConnection;39;0;58;0
WireConnection;39;1;38;0
WireConnection;54;0;13;0
WireConnection;55;0;14;0
WireConnection;60;0;13;4
WireConnection;60;1;14;4
WireConnection;60;2;31;4
WireConnection;60;3;32;4
WireConnection;53;0;49;0
WireConnection;40;0;39;0
WireConnection;30;0;54;0
WireConnection;30;1;55;0
WireConnection;30;2;56;0
WireConnection;30;3;57;0
WireConnection;74;0;24;0
WireConnection;41;0;30;0
WireConnection;41;1;40;0
WireConnection;48;0;60;0
WireConnection;48;1;53;0
WireConnection;73;0;74;0
WireConnection;73;1;72;0
WireConnection;76;0;30;0
WireConnection;76;1;41;0
WireConnection;77;0;60;0
WireConnection;77;1;48;0
WireConnection;70;1;73;0
WireConnection;70;5;71;0
WireConnection;51;0;76;0
WireConnection;51;1;52;0
WireConnection;61;0;77;0
WireConnection;61;1;62;0
WireConnection;80;0;76;0
WireConnection;80;1;70;0
WireConnection;80;2;51;0
WireConnection;80;9;61;0
ASEEND*/
//CHKSM=3C0B4D58902E448AEF0851062F2614DFF3D378D2