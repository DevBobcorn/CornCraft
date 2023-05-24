Shader "Hidden/EnviroVolumetricCloudsDepth"
{
    Properties
    {
        //_MainTex ("Texture", any) = "white" {}
    }
    SubShader 
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        //Pass 1 downsample
       	Pass 
        {
            Cull Off ZWrite Off ZTest Always
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ ENVIROURP
            #include "UnityCG.cginc"


			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
			float4 _CameraDepthTexture_TexelSize;

            struct appdata
            {
                float4 vertex : POSITION;
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
		        UNITY_INITIALIZE_OUTPUT(v2f, o); 
		        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                #if defined(ENVIROURP)
		        o.vertex = float4(v.vertex.xyz,1.0);
		        #if UNITY_UV_STARTS_AT_TOP
                o.vertex.y *= -1;
                #endif
                #else
		        o.vertex = UnityObjectToClipPos(v.vertex);
                #endif    

                o.uv = v.uv;

                o.uv00 = v.uv - 0.5 * _CameraDepthTexture_TexelSize.xy;
                o.uv10 = o.uv00 + float2(_CameraDepthTexture_TexelSize.x, 0.0);
                o.uv01 = o.uv00 + float2(0.0, _CameraDepthTexture_TexelSize.y);
                o.uv11 = o.uv00 + _CameraDepthTexture_TexelSize.xy;

                return o;
            }

            float frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 depth;
                
                depth[0] = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,UnityStereoTransformScreenSpaceTex(i.uv00));
                depth[1] = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,UnityStereoTransformScreenSpaceTex(i.uv10));
                depth[2] = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,UnityStereoTransformScreenSpaceTex(i.uv01));
                depth[3] = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,UnityStereoTransformScreenSpaceTex(i.uv11));
                return max(depth[0], max(depth[1], max(depth[2], depth[3])));
            }
            ENDCG
        }

        //Pass 2 Copy
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ ENVIROURP
            #include "UnityCG.cginc"
            
			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            struct appdata
            {
                float4 vertex : POSITION;
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
		        UNITY_INITIALIZE_OUTPUT(v2f, o); 
		        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                #if defined(ENVIROURP)
		        o.vertex = float4(v.vertex.xyz,1.0);
		        #if UNITY_UV_STARTS_AT_TOP
                o.vertex.y *= -1;
                #endif
                #else
		        o.vertex = UnityObjectToClipPos(v.vertex);
                #endif   
                
                o.uv = v.uv;
                return o;
            }

            float frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,UnityStereoTransformScreenSpaceTex(i.uv));

            }
                ENDCG
		}


    }
}
