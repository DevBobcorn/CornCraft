uniform float4 _SunDir;
uniform float4 _MoonDir;

uniform float4 _SunColor;

uniform half4 _FrontColor0;
uniform half4 _FrontColor1;
uniform half4 _FrontColor2;
uniform half4 _FrontColor3;
uniform half4 _FrontColor4;
uniform half4 _FrontColor5;
uniform half4 _FrontColor6;

uniform half4 _BackColor0;
uniform half4 _BackColor1;
uniform half4 _BackColor2;
uniform half4 _BackColor3;
uniform half4 _BackColor4;
uniform half4 _BackColor5;
uniform half4 _BackColor6;

uniform float _frontBackDistribution0;
uniform float _frontBackDistribution1;
uniform float _frontBackDistribution2;
uniform float _frontBackDistribution3;
uniform float _frontBackDistribution4;

uniform float _Intensity;
uniform float _MieScatteringIntensity;


float Mie(float costh, float g)
{
    g = min(g, 0.9381);
    float k = 1.55 * g - 0.55 * g * g * g;

    float kcosth = k * costh;

    return (1 - k * k) / ((4 * 3.14159265f) * (1 - kcosth) * (1 - kcosth));
}

float Remap(float org_val, float org_min, float org_max, float new_min, float new_max)
{
    return new_min + saturate(((org_val - org_min) / (org_max - org_min))*(new_max - new_min));
}


//Cirrus Clouds
uniform sampler2D _CirrusCloudMap;
uniform float _CirrusCloudAlpha;
uniform float _CirrusCloudCoverage;
uniform float _CirrusCloudAltitude;
uniform float4 _CirrusCloudColor;
uniform float _CirrusCloudColorPower;
uniform float2 _CirrusCloudAnimation;

float4 CirrusClouds(float3 uvs)
{
    uvs = normalize(uvs);
 
    float4 uv1;
    float4 uv2;

    uv1.xy = (uvs.xz * 0.2) + _CirrusCloudAnimation;
    uv2.xy = (uvs.xz * 0.6) + _CirrusCloudAnimation;

    float4 clouds1 = tex2D(_CirrusCloudMap, uv1.xy);
    float4 clouds2 = tex2D(_CirrusCloudMap, uv2.xy);

    float color1 = pow(clouds1.g + clouds2.g, 0.1);
    float color2 = pow(clouds2.b * clouds1.r, 0.2);

    float4 finalClouds = lerp(clouds1, clouds2, color1 * color2);
    float cloudExtinction = pow(uvs.y , 2);

    finalClouds.a *= _CirrusCloudAlpha;
    finalClouds.a *= cloudExtinction;

    if (uvs.y < 0) 
        finalClouds.a = 0;

    finalClouds.rgb = finalClouds.a * pow(_CirrusCloudColor,_CirrusCloudColorPower);
    finalClouds.rgb = pow(finalClouds.rgb, saturate(1 - _CirrusCloudCoverage));

    return finalClouds;
}


//2D Clouds
uniform sampler2D _FlatCloudsBaseTexture;
uniform sampler2D _FlatCloudsDetailTexture;
uniform float4 _FlatCloudsAnimation;
uniform float3 _FlatCloudsLightDirection;
uniform float3 _FlatCloudsLightColor;
uniform float3 _FlatCloudsAmbientColor;
uniform float4 _FlatCloudsLightingParams; // x = LightIntensity, y = AmbientIntensity, z = Absorbtion, w = HgPhase
uniform float4 _FlatCloudsParams; // x = Coverage, y = Density, z = Altitude, w = tonemapping
uniform float4 _FlatCloudsTiling; // x = Base, y = Detail

//uniform float _FlatCloudsExposure;

float HenryGreenstein(float cosTheta, float g)
{
    float k = 3.0 / (8.0 * 3.1415926f) * (1.0 - g * g) / (2.0 + g * g);
    return k * (1.0 + cosTheta * cosTheta) / pow(abs(1.0 + g * g - 2.0 * g * cosTheta), 1.5);
}
half3 tonemapACES(half3 color, float Exposure)
{
    color *= Exposure;

    // See https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/
    const half a = 2.51;
    const half b = 0.03;
    const half c = 2.43;
    const half d = 0.59;
    const half e = 0.14;
    return saturate((color * (a * color + b)) / (color * (c * color + d) + e));
}


float CalculateCloudDensity(float2 posBase, float2 posDetail,float3 worldPos, float coverage)
{
    float4 baseNoise = tex2D(_FlatCloudsBaseTexture, posBase);
    float low_freq_fBm = (baseNoise.g * 0.625) + (baseNoise.b * 0.25) + (baseNoise.a * 0.125);
    float base_cloud = Remap(baseNoise.r, -(1.0 - low_freq_fBm), 1.0, 0.0, 1.0) * coverage;
 
    float4 detailNoise = tex2D(_FlatCloudsDetailTexture, posDetail * 2);
    float high_freq_fBm = (detailNoise.r * 0.625) + (detailNoise.g * 0.25) + (detailNoise.b * 0.125);
    float density = Remap(base_cloud, 1-high_freq_fBm * 0.5, 1.0, 0.0, 1.0);

    density *= pow(high_freq_fBm, 0.4);
    density *= _FlatCloudsParams.y;

    
    return density;
}


float4 Clouds2D (float3 uvs, float3 worldPos)
{
    half4 col = 0;
    uvs = normalize(uvs);
    
    float4 uv1;
    uv1.xy = (uvs.xz * _FlatCloudsTiling.x) + _FlatCloudsAnimation.xy;
    uv1.zw = (uvs.xz * _FlatCloudsTiling.y) + _FlatCloudsAnimation.zw;

    float cloudExtinction = pow(uvs.y, 2);		
    float density = CalculateCloudDensity(uv1.xy, uv1.zw, uvs, _FlatCloudsParams.x);

    //Lighting	
    float absorbtion = exp2(-1 * (density * _FlatCloudsLightingParams.z));
    float3 viewDir = normalize(worldPos - _WorldSpaceCameraPos);
    float inscatterAngle = dot(normalize(_FlatCloudsLightDirection), -viewDir);
    float hg = HenryGreenstein(inscatterAngle, _FlatCloudsLightingParams.w) * 2 * absorbtion;
    float lighting = density * (absorbtion + hg);
    float3 lightColor = pow(_FlatCloudsLightColor, 2) * (_FlatCloudsLightingParams.x );
    col.rgb = lightColor * lighting;
    col.rgb = col.rgb + (_FlatCloudsAmbientColor * _FlatCloudsLightingParams.y);
    
    //Tonemapping
   // if (_FlatCloudsParams.w == 1)		
   //     col.rgb = tonemapACES(col.rgb, _CloudsExposure);

    col.a = saturate(density * cloudExtinction);

    if (uvs.y < 0)
        col.a = 0;

    return col;
}

 
float4 GetSkyColor (float3 viewDir, float mieSize)
{
    float cosTheta = smoothstep(-0.25,1.15,saturate(dot(-_SunDir.xyz, viewDir)));
    half y = -viewDir.y / 0.02;
  
    float3 frontBack0 = lerp(_FrontColor0.rgb,_BackColor0.rgb,cosTheta);
    float3 frontBack1 = lerp(_FrontColor1.rgb,_BackColor1.rgb,cosTheta);
    float3 frontBack2 = lerp(_FrontColor2.rgb,_BackColor2.rgb,cosTheta);
    float3 frontBack3 = lerp(_FrontColor3.rgb,_BackColor3.rgb,cosTheta);
    float3 frontBack4 = lerp(_FrontColor4.rgb,_BackColor4.rgb,cosTheta);
    float3 frontBack5 = lerp(_FrontColor5.rgb,_BackColor5.rgb,cosTheta);

    float heightS1 = Remap(viewDir.y,-0.75,_frontBackDistribution0,0,1);
    float heightS2 = Remap(viewDir.y,_frontBackDistribution0,_frontBackDistribution1,0,1);
    float heightS3 = Remap(viewDir.y,_frontBackDistribution1,_frontBackDistribution2,0,1);
    float heightS4 = Remap(viewDir.y,_frontBackDistribution2,_frontBackDistribution3,0,1);
    float heightS5 = Remap(viewDir.y,_frontBackDistribution3,1,0,1);
 
    float3 sky1 = lerp(frontBack0.rgb,frontBack1.rgb,heightS1);
    float3 sky2 = lerp(sky1.rgb,frontBack2.rgb,heightS2);  
    float3 sky3 = lerp(sky2.rgb,frontBack3.rgb,heightS3); 
    float3 sky4 = lerp(sky3.rgb,frontBack4.rgb,heightS4);
    float3 sky5 = lerp(sky4.rgb,frontBack5.rgb,heightS5);

    float3 skyColor = sky5 * _Intensity;
 
    float eyeCos = dot(_SunDir, viewDir);
	float eyeCos2 = eyeCos * eyeCos;
    float fade = saturate(dot(_SunDir.xyz, viewDir));

    float mie = Mie(eyeCos, 0.7) * _MieScatteringIntensity * fade;
    
	skyColor.rgb += (mie * skyColor) * _SunColor.rgb;

    return float4(skyColor,1);
}

float4 GetSkyAndCloudsColor (float3 viewDir, float mieSize)
{
    float cosTheta = smoothstep(-0.25,1.15,saturate(dot(-_SunDir.xyz, viewDir)));
    half y = -viewDir.y / 0.02;
  
    float3 frontBack0 = lerp(_FrontColor0.rgb,_BackColor0.rgb,cosTheta);
    float3 frontBack1 = lerp(_FrontColor1.rgb,_BackColor1.rgb,cosTheta);
    float3 frontBack2 = lerp(_FrontColor2.rgb,_BackColor2.rgb,cosTheta);
    float3 frontBack3 = lerp(_FrontColor3.rgb,_BackColor3.rgb,cosTheta);
    float3 frontBack4 = lerp(_FrontColor4.rgb,_BackColor4.rgb,cosTheta);
    float3 frontBack5 = lerp(_FrontColor5.rgb,_BackColor5.rgb,cosTheta);

    float heightS1 = Remap(viewDir.y,-0.75,_frontBackDistribution0,0,1);
    float heightS2 = Remap(viewDir.y,_frontBackDistribution0,_frontBackDistribution1,0,1);
    float heightS3 = Remap(viewDir.y,_frontBackDistribution1,_frontBackDistribution2,0,1);
    float heightS4 = Remap(viewDir.y,_frontBackDistribution2,_frontBackDistribution3,0,1);
    float heightS5 = Remap(viewDir.y,_frontBackDistribution3,1,0,1);
 
    float3 sky1 = lerp(frontBack0.rgb,frontBack1.rgb,heightS1);
    float3 sky2 = lerp(sky1.rgb,frontBack2.rgb,heightS2);  
    float3 sky3 = lerp(sky2.rgb,frontBack3.rgb,heightS3); 
    float3 sky4 = lerp(sky3.rgb,frontBack4.rgb,heightS4);
    float3 sky5 = lerp(sky4.rgb,frontBack5.rgb,heightS5);

    float3 skyColor = sky5 * _Intensity;
 
    float eyeCos = dot(_SunDir, viewDir);
	float eyeCos2 = eyeCos * eyeCos;
    float fade = saturate(dot(_SunDir.xyz, viewDir));

    float mie = Mie(eyeCos, 0.7) * _MieScatteringIntensity * fade;
    
	skyColor.rgb += (mie * skyColor) * _SunColor.rgb;


    float4 cirrus = CirrusClouds(viewDir);
	skyColor.rgb = skyColor.rgb * (1 - cirrus.a) + cirrus.rgb * cirrus.a;

    return float4(skyColor,1);
}


///Aurora
sampler2D _Aurora_Layer_1;
sampler2D _Aurora_Layer_2;
sampler2D _Aurora_Colorshift;

float4 _AuroraColor;
float _AuroraIntensity;
float _AuroraBrightness;
float _AuroraContrast;
float _AuroraHeight;
float _AuroraScale;
float _AuroraSpeed;
float _AuroraSteps;

float4 _Aurora_Tiling_Layer1;
float4 _Aurora_Tiling_Layer2;
float4 _Aurora_Tiling_ColorShift;

float randomNoise(float3 co) 
{
	return frac(sin(dot(co.xyz ,float3(17.2486,32.76149, 368.71564))) * 32168.47512);
}

float4 SampleAurora(float3 uv) 
{

    float2 uv_1 = uv.xy * _Aurora_Tiling_Layer1.xy + (_Aurora_Tiling_Layer1.zw * _AuroraSpeed * _Time.y);

    float4 aurora = tex2Dlod(_Aurora_Layer_1, float4(uv_1.xy,0,0));

    float2 uv_2 = uv_1 * _Aurora_Tiling_Layer2.xy + (_Aurora_Tiling_Layer2.zw * _AuroraSpeed * _Time.y);
    float4 aurora2 = tex2Dlod(_Aurora_Layer_2, float4(uv_2.xy,0,0));
    aurora += (aurora2 - 0.5) * 0.5;

    aurora.w = aurora.w * 0.8 + 0.05;

    float3 uv_3 = float3(uv.xy * _Aurora_Tiling_ColorShift.xy + (_Aurora_Tiling_ColorShift.zw * _AuroraSpeed * _Time.y), 0.0);
    float4 cloudColor = tex2Dlod(_Aurora_Colorshift, float4(uv_3.xy,0,0));

    float contrastMask = 1.0 - saturate(aurora.a);
    contrastMask = pow(contrastMask, _AuroraContrast);
    aurora.rgb *= lerp(float3(0,0,0), _AuroraColor.rgb * cloudColor.rgb * _AuroraBrightness, contrastMask);

    float cloudSub = 1.0 - uv.z;
    aurora.a = aurora.a - cloudSub * cloudSub;
    aurora.a = saturate(aurora.a * _AuroraIntensity);
    aurora.rgb *= aurora.a;

    return aurora;
} 

float4 Aurora (float3 wpos)
{     
    if (_AuroraIntensity < 0.05)
	    return float4(0,0,0,0);

	float3 viewDir = normalize(wpos - _WorldSpaceCameraPos);

	float viewFalloff = 1.0 - saturate(dot(viewDir, float3(0,1,0)));

	if (viewDir.y < 0 || viewDir.y > 1)
		return half4(0, 0, 0, 0);

	float3 traceDir = normalize(viewDir + float3(0, viewFalloff * 0.2 ,0));

	float3 worldPos = _WorldSpaceCameraPos + traceDir * ((_AuroraHeight - _WorldSpaceCameraPos.y) / max(traceDir.y, 0.01));
	float3 uv = float3(worldPos.xz * 0.01 * _AuroraScale, 0);

	half3 uvStep = half3(traceDir.xz * -1.0 * (1.0 / traceDir.y), 1.0) * (1.0 / _AuroraSteps);
	uv += uvStep * randomNoise(wpos + _SinTime.w);

	half4 finalColor = half4(0,0,0,0);

	[loop]
	for (int iCount = 0; iCount < _AuroraSteps; iCount++)
	{
		if (finalColor.a > 1)
			break;

		uv += uvStep;
		finalColor += SampleAurora(uv) * (1.0 - finalColor.a);
	}

	finalColor *= viewDir.y;

	return finalColor;
}

