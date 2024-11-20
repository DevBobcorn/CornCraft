Shader "DeltaValueBar"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" {}

        _BorderColor ("Border Color", Color) = (1, 1, 1, 1)
        _BorderThickness ("Border Thickness", Float) = 5

        _DeltaColor ("Delta Color", Color) = (1, 0.5, 0.5, 1)
        _ValueColor ("Value Color", Color) = (0.2, 0.5, 1, 1)

        _FillAmount ("Fill Amount", Range(0, 1)) = 1
        _DeltaAmount ("Delta Amount", Range(0, 1)) = 0.5

        _BarSize ("Bar Size", Vector) = (700, 42, 0, 0)
        _CornerRadii ("Corner Radii", Vector) = (5, 25, 5, 5)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv * 2.0 - 1.0;
                return output;
            }

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _BorderColor;
            half _BorderThickness;
            half4 _DeltaColor;
            half4 _ValueColor;
            half _FillAmount;
            half _DeltaAmount;
            float2 _BarSize;
            float4 _CornerRadii;
            CBUFFER_END

            // ref: iquilezles.org/articles/distfunctions2d
            // b.x = width
            // b.y = height
            // r.x = roundness top-right  
            // r.y = roundness boottom-right
            // r.z = roundness top-left
            // r.w = roundness bottom-left
            float sdRoundBox(float2 p, float2 b, float4 r)
            {
                r.xy = p.x > 0.0 ? r.xy : r.zw;
                r.x = p.y > 0.0 ? r.x : r.y;
                float2 q = abs(p) - b + r.x;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float borderDist = sdRoundBox(input.uv * _BarSize, _BarSize, _CornerRadii);
                float borderMask = borderDist + _BorderThickness * 2.0;

                // draw border
                half4 color = -_BorderThickness < borderDist && borderDist < 0 ? _BorderColor : _BorderColor * 0;

                // draw delta
                float2 deltaSize = float2(_BarSize.x * _DeltaAmount, _BarSize.y);
                float2 deltaBias = float2(_BarSize.x * (1 - _DeltaAmount) , 0);
                float deltaDist = sdRoundBox(input.uv * _BarSize + deltaBias, deltaSize, _CornerRadii);

                color = borderMask < 0 && deltaDist < 0 ? _DeltaColor : color;

                // draw value
                deltaSize = float2(_BarSize.x * _FillAmount, _BarSize.y);
                deltaBias = float2(_BarSize.x * (1 - _FillAmount) , 0);
                deltaDist = sdRoundBox(input.uv * _BarSize + deltaBias, deltaSize, _CornerRadii);

                color = borderMask < 0 && deltaDist < 0 ? _ValueColor : color;

                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Unlit/Transparent"
}