Shader "Hidden/EnviroApplyShadowsHDRP"
{
    Properties
    {
        //_MainTex ("Texture", any) = "white" {}
        //_CloudsTex ("Texture", any) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"

            struct appdata
            {
                uint vertexID : SV_VertexID;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
                o.uv = GetFullScreenTriangleTexCoord(v.vertexID);
                return o;
            }

            float _Intensity;
            float4 _HandleScales;
            TEXTURE2D_X(_MainTex);
            TEXTURE2D(_CloudsTex);

            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 sceneColor =  SAMPLE_TEXTURE2D_X_LOD(_MainTex,s_trilinear_clamp_sampler, i.uv, 0);
                float cloudShadows =  SAMPLE_TEXTURE2D(_CloudsTex,s_trilinear_clamp_sampler, i.uv * _HandleScales.xy).b * _Intensity;

                float shadowsClouds = clamp((1-cloudShadows),0,1);
                float4 final = float4(sceneColor.rgb * shadowsClouds, sceneColor.a);

                return final;
            }
            ENDHLSL
        }
    }
}
