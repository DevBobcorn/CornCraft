Shader "Hidden/FernRender/PostProcess/FernVolumeUber"
{
    HLSLINCLUDE
        #pragma editor_sync_compilation
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    ENDHLSL
    
    Subshader
    {
        ZTest Always Cull Off ZWrite Off

        // ---- SSAO pass
        Pass
        {

            Name "Fern Volume Uber"
            HLSLPROGRAM

            #pragma shader_feature_local_fragment __ DEBUG_AO DEBUG_COLORBLEEDING DEBUG_NOAO_AO DEBUG_AO_AOONLY DEBUG_NOAO_AOONLY

            #pragma multi_compile _ _HBAO
            #pragma multi_compile _ _DUALKAWASEBLUR
            #pragma multi_compile _ _EDGEDETECION

            
            #pragma vertex VertUber
            #pragma fragment frag


            #include "FernVolumeUber.hlsl"

            ENDHLSL

        }
    }

    Fallback off
}