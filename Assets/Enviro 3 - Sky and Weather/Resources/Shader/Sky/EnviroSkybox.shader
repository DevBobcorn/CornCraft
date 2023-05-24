Shader "Enviro/Skybox"
{
    Properties
    {
		_MoonTex("Moon Tex", 2D) = "black" {}
		_MoonGlowTex("Moon Glow Tex", 2D) = "black" {}
		_SunTex("Sun Tex", 2D) = "black" {}
		_StarsTex ("Stars Tex", Cube) = "black" {}
		_GalaxyTex ("Galaxy Tex", Cube) = "black" {}
	}
	
    SubShader
    {
		Lod 300
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "IgnoreProjector"="True" }
		
        Pass
        {
            Cull Back
            ZWrite Off
		 
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
			#include "../Includes/SkyInclude.cginc"
			#pragma target 3.0 
			#pragma multi_compile __ UNITY_COLORSPACE_GAMMA
			//#pragma multi_compile_fog

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


			struct VertexInput 
             {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0;
				float3 worldPos : TEXCOORD1; 
				UNITY_VERTEX_INPUT_INSTANCE_ID
             };


            struct v2f {
                float4 position : POSITION;
             	float4 sunAndMoonPos : TEXCOORD1;
				float3 starPos : TEXCOORD2;
				//float2 moonGlowPos : TEXCOORD3;
				float3 texcoord : TEXCOORD3;
				float3 cirrusCoords : TEXCOORD4;
				float3 flatCoords : TEXCOORD5;
				float3 worldPos : TEXCOORD6;
				UNITY_VERTEX_OUTPUT_STEREO
            };
 
            v2f vert(VertexInput v) {
                v2f o;
				UNITY_SETUP_INSTANCE_ID(v); 
				UNITY_INITIALIZE_OUTPUT(v2f, o); 
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); 
                o.position = UnityObjectToClipPos(v.vertex);

				float3 rSun = normalize(cross(_SunDir.xyz, float3(0, -1, 0)));
				float3 uSun = cross(_SunDir.xyz, rSun);

				float3 rMoon = normalize(cross(_MoonDir.xyz, float3(0, -1, 0)));
				float3 uMoon = cross(_MoonDir.xyz, rMoon);

				o.sunAndMoonPos.xy = float2(dot(rSun, v.vertex.xyz), dot(uSun, v.vertex.xyz)) * (21.0 - _SkySunParameters.x) + 0.5;
				o.sunAndMoonPos.zw = float2(dot(rMoon, v.vertex.xyz), dot(uMoon, v.vertex.xyz)) * (20.7 - _SkyMoonParameters.z) + 0.5;
				//o.moonGlowPos.xy = float2(dot(rMoon, v.vertex.xyz), dot(uMoon, v.vertex.xyz)) * (21.0 - (_SkyMoonParameters.y)) + 0.5;
				o.starPos = mul((float3x3)_StarsMatrix,v.vertex.xyz);

				o.texcoord = v.texcoord;

				o.worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;

				if(_CirrusClouds > 0.0)
				{
					o.cirrusCoords = normalize(v.vertex).xyz;
					float3 cirrusCoords = normalize(o.cirrusCoords + float3(0,1,0));
					o.cirrusCoords.y *= 1 - dot(cirrusCoords.y + 10, float3(0,-0.15,0));
				}

				if(_FlatClouds > 0.0)
				{
					o.flatCoords = normalize(v.vertex).xyz;
					float3 flatCoords = normalize(o.flatCoords + float3(0,1,0));
					o.flatCoords.y *= 1 - dot(flatCoords.y + 200, float3(0,-0.1,0));
				}

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
				float _DitheringIntensity = 0.05; 
				float d = dot(float2(131.0, 312.0), vScreenPos.xy + _Time.y);
				float3 vDither = float3(d, d, d);
				vDither.rgb = frac(vDither.rgb / float3(103.0, 71.0, 97.0)) - float3(0.5, 0.5, 0.5);
				return (vDither.rgb / 15.0) * _DitheringIntensity;
			}   

            float4 frag(v2f i) : COLOR 
            {			
				float4 skyColor = float4(0, 0, 0, 1);
				float3 viewDir = normalize(i.texcoord);
  
				skyColor = GetSkyColor(viewDir, 0.005f);  

				//Stars
				float4 starsTex = texCUBE(_StarsTex, i.starPos.xyz) * saturate(viewDir.y);
				float4 stars = starsTex * _StarIntensity;
				//skyColor += stars;

				//Galaxy
				float4 galaxyTex = texCUBE(_GalaxyTex, i.starPos.xyz) * saturate(viewDir.y);
				float4 galaxy = galaxyTex * _GalaxyIntensity;
				//skyColor += galaxy;

				//Sun
				float4 sun = float4(0,0,0,1);
				float hideBackSun = saturate(dot(_SunDir.xyz, viewDir));
				float4 sunDisk = tex2D(_SunTex, i.sunAndMoonPos.xy) * hideBackSun;
				sun = sunDisk * _SunColor * 10;
				skyColor += sun;
	  
				//Moon
				if(_SkyMoonParameters.w > 0.0) 
				{
					float hideBackMoon = saturate(dot(-_MoonDir.xyz, viewDir));
					float4 moon = tex2D(_MoonTex, i.sunAndMoonPos.zw) * hideBackMoon;
					float alpha = MoonPhaseFactor(i.sunAndMoonPos.zw, _SkyMoonParameters.x);
					float moonArea = clamp(moon.a * 10, 0, 1); 
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
					float4 aurora = Aurora(i.worldPos);
					skyColor.rgb += aurora.rgb;
				}

				//Dithering
				skyColor.rgb += ScreenSpaceDither(i.position.xy,skyColor.rgb);

				//Cirrus
				if(_CirrusClouds > 0.0)
				{
					float4 cirrus = CirrusClouds(i.cirrusCoords);
					skyColor.rgb = skyColor.rgb * (1 - cirrus.a) + cirrus.rgb * cirrus.a; 
				}
 
				//2D Clouds
				if(_FlatClouds > 0.0)
				{
					float4 clouds = Clouds2D(i.flatCoords, i.worldPos); 
					skyColor.rgb = skyColor.rgb * (1 - clouds.a) + clouds.rgb * clouds.a;
				}
				  
			#if defined(UNITY_COLORSPACE_GAMMA)
				skyColor.rgb = LinearToGammaSpace(skyColor.rgb);
			#endif

                return skyColor;
            }
            ENDCG
        }
	}
    FallBack "None"
}
