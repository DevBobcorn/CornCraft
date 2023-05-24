Shader "Hidden/EnviroVolumetricCloudsDepthHDRP"
{
    Properties
    {
       // _CameraDepthTexture ("Texture", any) = "white" {}
    }
    SubShader 
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        //Pass 1 downsample
       	Pass 
        {
            Cull Off ZWrite Off ZTest Always
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

		
			float4 _CameraDepthTexture_TexelSize;


            struct appdata
            {
                uint vertexID : SV_VertexID;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uv00 : TEXCOORD1;
                float2 uv10 : TEXCOORD2;
                float2 uv01 : TEXCOORD3;
                float2 uv11 : TEXCOORD4;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
                o.uv = GetFullScreenTriangleTexCoord(v.vertexID);

                o.uv00 = o.uv - 0.5 * _CameraDepthTexture_TexelSize.xy; //v.uv
                o.uv10 = o.uv00 + float2(_CameraDepthTexture_TexelSize.x, 0.0);
                o.uv01 = o.uv00 + float2(0.0, _CameraDepthTexture_TexelSize.y);
                o.uv11 = o.uv00 + _CameraDepthTexture_TexelSize.xy;

                return o;
            }

            float frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                uint2 positionSS = i.uv * _ScreenSize.xy;

                float4 depth;
                
                depth[0] = LOAD_TEXTURE2D_X_LOD(_CameraDepthTexture,i.uv00 * _ScreenSize.xy,0);
                depth[1] = LOAD_TEXTURE2D_X_LOD(_CameraDepthTexture,i.uv10 * _ScreenSize.xy,0);
                depth[2] = LOAD_TEXTURE2D_X_LOD(_CameraDepthTexture,i.uv01 * _ScreenSize.xy,0);
                depth[3] = LOAD_TEXTURE2D_X_LOD(_CameraDepthTexture,i.uv11 * _ScreenSize.xy,0);
                return max(depth[0], max(depth[1], max(depth[2], depth[3])));
            }
            ENDHLSL
        }

        //Pass 2 Copy
        Pass
        {
            Cull Off ZWrite Off ZTest Always
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


            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v); 
		        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

		        o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
                o.uv = GetFullScreenTriangleTexCoord(v.vertexID);

                return o;
            }

            float frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                 
                return LOAD_TEXTURE2D_X_LOD(_CameraDepthTexture, i.uv * _ScreenSize.xy,0);
  
            }
                ENDHLSL
		}


    }
}
