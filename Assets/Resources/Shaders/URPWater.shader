Shader "URP/Water"
{
    Properties
    {
        _ShallowWater ("ShallowColor", Color) = (1.0, 1.0, 1.0, 1.0)
        _DeepWater ("DeepColor", Color) = (1.0, 1.0, 1.0, 1.0)
        _WaterAlpha("WaterAlpha",Range(0,1)) = 0.5
        
        _SurfaceNoise("Surface Noise", 2D) = "white" {}
        _MoveSpeed("MoveSpeed",Range(0,1)) = 0.5
        
        _FoamDistance("Foam Distance",Range(0,10)) = 0.4
        _FoamColor("FoamColor", Color) = (1.0, 1.0, 1.0, 1.0)
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            float4 _ShallowWater;
            float4 _DeepWater;
            float _WaterAlpha;

            sampler2D _SurfaceNoise;
            float4 _SurfaceNoise_ST;
            float _MoveSpeed;
            
            float _FoamDistance;
            float4 _FoamColor;

            // 顶点着色器的输入
            struct a2v
            {
                float3 positionOS : POSITION;
                float4 uv : TEXCOORD0;
                float3 vertexColor : COLOR;
            };

            // 顶点着色器的输出
            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float4 screenPosition : TEXCOORD0;
                float2 noiseUV : TEXCOORD1;
                float2 distortUV : TEXCOORD2;
                float3 vertexColor : COLOR;
            };
            
            v2f vert(a2v v)
            {
                v2f o;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(v.positionOS);
                o.positionCS = positionInputs.positionCS;
                o.noiseUV = TRANSFORM_TEX(v.uv, _SurfaceNoise);
                o.screenPosition = ComputeScreenPos(positionInputs.positionCS);
                o.vertexColor = v.vertexColor;
                return o;
            }
            
            half4 frag(v2f i) : SV_Target
            {
                // 通过深度纹理的采样 计算屏幕深度
                float sceneRawDepth = SampleSceneDepth(i.screenPosition.xy / i.screenPosition.w);
                // 深度纹理的采样结果转换到视图空间下的深度值
                float sceneEyeDepth = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
                // 因为关心的是这个深度值相对于我们的水面有多深，所以需要把视图深度,减去模型顶点的深度
                // 最终得到水的深度
                float waterDepth = sceneEyeDepth - i.screenPosition.w; 
                // 拿到水的颜色
                //float3 waterColor = lerp(i.vertexColor, _DeepWater, clamp(waterDepth * 0.02, 0, 1));
                float3 waterColor = i.vertexColor;
                
                float surfaceNoiseSample = tex2D(_SurfaceNoise, i.noiseUV + _Time.y * _MoveSpeed * 0.1).r;
                
                // 浮沫
                float foam = saturate(waterDepth / _FoamDistance);
                float edge = 1 - sqrt(clamp(waterDepth / 2, 0, 1));
                float surfaceNoise = smoothstep(0, foam, surfaceNoiseSample);
                // 混合水面透明度
                float4 col = float4(waterColor + surfaceNoise * _FoamColor * edge, _WaterAlpha * clamp(waterDepth + edge, 0, 1));
                return col;
            }
            ENDHLSL
        }
    }
}