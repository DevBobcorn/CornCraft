Shader "Krivodeling/UI/UI Blur"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
        [Toggle] _FlipX("Flip X", float) = 0
        [Toggle] _FlipY("Flip Y", float) = 0
        _Intensity("Intensity", Range(0, 1)) = 0
        _Multiplier("Multiplier", Range(0, 1)) = 0.15
    }

        SubShader
        {
            Tags { "Queue" = "Transparent" }
            GrabPass { }
            Pass
            {
                    CGPROGRAM
                    #pragma debug
                    #pragma vertex vert
                    #pragma fragment frag 

                    #ifndef SHADER_API_D3D11
                    #pragma target 3.0
                    #else
                    #pragma target 4.0
                    #endif

                    sampler2D _GrabTexture : register(s0);
                    sampler2D _MainTex;
                    half4 _Color;
                    float _FlipX;
                    float _FlipY;
                    float _Intensity;
                    float _Multiplier;

                    struct data
                    {
                        float4 vertex : POSITION;
                        float3 normal : NORMAL;
                    };

                    struct v2f
                    {
                        float4 position : POSITION;
                        float4 screenPos : TEXCOORD0;
                    };

                    v2f vert(data i)
                    {
                        v2f o;
                        o.position = UnityObjectToClipPos(i.vertex);
                        o.screenPos = o.position;

                        return o;
                    }

                    half4 frag(v2f i) : COLOR
                    {
                        float2 screenPos = i.screenPos.xy / i.screenPos.w;
                        float depth = _Intensity * _Multiplier / 200;

                        if (_FlipX)
                            screenPos.x = 1 - (screenPos.x + 1) * 0.5;
                        else
                            screenPos.x = (screenPos.x + 1) * 0.5;

                        if (_FlipY)
                            screenPos.y = (screenPos.y + 1) * 0.5;
                        else
                            screenPos.y = 1 - (screenPos.y + 1) * 0.5;

                        half4 sum = half4(0.0h, 0.0h, 0.0h, 0.0h);

                        sum += tex2D(_GrabTexture, float2(screenPos.x - 5.0 * depth, screenPos.y + 5.0 * depth)) * 0.025;
                        sum += tex2D(_GrabTexture, float2(screenPos.x + 5.0 * depth, screenPos.y - 5.0 * depth)) * 0.025;

                        sum += tex2D(_GrabTexture, float2(screenPos.x - 4.0 * depth, screenPos.y + 4.0 * depth)) * 0.05;
                        sum += tex2D(_GrabTexture, float2(screenPos.x + 4.0 * depth, screenPos.y - 4.0 * depth)) * 0.05;

                        sum += tex2D(_GrabTexture, float2(screenPos.x - 3.0 * depth, screenPos.y + 3.0 * depth)) * 0.09;
                        sum += tex2D(_GrabTexture, float2(screenPos.x + 3.0 * depth, screenPos.y - 3.0 * depth)) * 0.09;

                        sum += tex2D(_GrabTexture, float2(screenPos.x - 2.0 * depth, screenPos.y + 2.0 * depth)) * 0.12;
                        sum += tex2D(_GrabTexture, float2(screenPos.x + 2.0 * depth, screenPos.y - 2.0 * depth)) * 0.12;

                        sum += tex2D(_GrabTexture, float2(screenPos.x - 1.0 * depth, screenPos.y + 1.0 * depth)) * 0.15;
                        sum += tex2D(_GrabTexture, float2(screenPos.x + 1.0 * depth, screenPos.y - 1.0 * depth)) * 0.15;

                        sum += tex2D(_GrabTexture, screenPos - 5.0 * depth) * 0.025;
                        sum += tex2D(_GrabTexture, screenPos - 4.0 * depth) * 0.05;
                        sum += tex2D(_GrabTexture, screenPos - 3.0 * depth) * 0.09;
                        sum += tex2D(_GrabTexture, screenPos - 2.0 * depth) * 0.12;
                        sum += tex2D(_GrabTexture, screenPos - 1.0 * depth) * 0.15;
                        sum += tex2D(_GrabTexture, screenPos) * 0.25;
                        sum += tex2D(_GrabTexture, screenPos + 1.0 * depth) * 0.025;
                        sum += tex2D(_GrabTexture, screenPos + 2.0 * depth) * 0.05;
                        sum += tex2D(_GrabTexture, screenPos + 3.0 * depth) * 0.09;
                        sum += tex2D(_GrabTexture, screenPos + 4.0 * depth) * 0.12;
                        sum += tex2D(_GrabTexture, screenPos + 5.0 * depth) * 0.15;

                        return sum / 2 * _Color;
                    }

                ENDCG
                }
        }
            Fallback Off
}
