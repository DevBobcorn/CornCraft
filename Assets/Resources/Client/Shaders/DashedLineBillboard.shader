// https://forum.unity.com/threads/cylindrical-billboard-shader-shader-for-billboard-with-rotation-restricted-to-the-y-axis.498406/
// Source: https://en.wikibooks.org/wiki/Cg_Programming/Unity/Billboards
// Source: http://www.unity3d-france.com/unity/phpBB3/viewtopic.php?t=12304
Shader "Unlit/Dashed Line Billboard"
{
    Properties
    {
        _MainTex ("Texture Image", 2D) = "white" {}
        _Line_Color ("Line Color", Color) = (1, 1, 1, 1)
        _Line_Width ("Line Width", Float) = 0.03
        _Line_Length ("Line Length", Float) = 1.00

        _DashFrequency ("Dash Frequency", Float) = 10
        _DashCapTilt ("Dash Cap Tilt", Float) = 0.05
        _DashOffset ("Dash Offset", Float) = 0

        // 0 for X, 1 for Y, other values for Z
        _BillboardAxis ("Billboard Axis", Integer) = 0
    }
 
    SubShader
    {  
        Cull Off
 
        Pass
        {
            CGPROGRAM
            #include "UnityCG.cginc"
 
            #pragma shader_feature IGNORE_ROTATION_AND_SCALE
            #pragma vertex vert
            #pragma fragment frag
 
            // User-specified uniforms
            uniform sampler2D _MainTex;
            uniform float4 _Line_Color;
            uniform float _Line_Width;
            uniform float _Line_Length;
           
            float4 _MainTex_ST;

            uniform int _BillboardAxis;
 
            struct vertexInput
            {
                float4 vertex : POSITION;
                float4 tex : TEXCOORD0;
            };
 
            struct vertexOutput
            {
                float4 pos : SV_POSITION;
                float2 tex : TEXCOORD0;
            };
            
            vertexOutput vert(vertexInput input)
            {
                // See https://discussions.unity.com/t/strange-unity_objecttoworld-behaviour/769706

                // The world position of the center of the object
                float3 worldPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
 
                // Distance between the camera and the center
                float3 dist = _WorldSpaceCameraPos - worldPos;
 
                // atan2(dist.x, dist.z) = atan (dist.x / dist.z)
                // With atan the tree inverts when the camera has the same z position
                float angle;
                
                if (_BillboardAxis == 0) // Align with X axis
                {
                    angle = atan2(dist.y, dist.z);
                }
                else if (_BillboardAxis == 1) // Align with Y axis
                {
                    angle = atan2(dist.x, dist.z);
                }
                else // Align with Z axis
                {
                    angle = atan2(dist.x, dist.y);
                }
                
 
                float3x3 rotMatrix;
                float cosA = cos(angle);
                float sinA = sin(angle);
       
                // Rotation matrix along axis
                if (_BillboardAxis == 0) // Align with X axis
                {
                    rotMatrix[0].xyz = float3( 0,    1,    0);
                    rotMatrix[1].xyz = float3( cosA, 0, sinA);
                    rotMatrix[2].xyz = float3(-sinA, 0, cosA);
                }
                else if (_BillboardAxis == 1) // Align with Y axis
                {
                    rotMatrix[0].xyz = float3( cosA, 0, sinA);
                    rotMatrix[1].xyz = float3( 0,    1,    0);
                    rotMatrix[2].xyz = float3(-sinA, 0, cosA);
                }
                else // Align with Z axis
                {
                    rotMatrix[0].xyz = float3( cosA, 0, sinA);
                    rotMatrix[1].xyz = float3(-sinA, 0, cosA);
                    rotMatrix[2].xyz = float3( 0,    1,    0);
                }
 
                // The position of the vertex after the rotation
                float4 newPos = float4(mul(rotMatrix, input.vertex * float4(_Line_Width, _Line_Length, 0, 0)), 1);
 
                // The model matrix without the rotation and scale
                float4x4 matrix_M_noRot = unity_ObjectToWorld;
                matrix_M_noRot[0][0] = 1;
                matrix_M_noRot[0][1] = 0;
                matrix_M_noRot[0][2] = 0;
 
                matrix_M_noRot[1][0] = 0;
                matrix_M_noRot[1][1] = 1;
                matrix_M_noRot[1][2] = 0;
 
                matrix_M_noRot[2][0] = 0;
                matrix_M_noRot[2][1] = 0;
                matrix_M_noRot[2][2] = 1;
 
                vertexOutput output;
 
                // The position of the vertex in clip space ignoring the rotation and scale of the object
                #if IGNORE_ROTATION_AND_SCALE
                output.pos = mul(UNITY_MATRIX_VP, mul(matrix_M_noRot, newPos));
                #else
                output.pos = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, newPos));
                #endif
 
                output.tex = TRANSFORM_TEX(input.tex, _MainTex);
 
                return output;
            }

            float _DashFrequency;
            float _DashCapTilt;
            float _DashOffset;

            float4 frag(vertexOutput input) : COLOR
            {
                float4 col = tex2D(_MainTex, input.tex.xy) * _Line_Color;

                // Clip segments
                col.a *= sin((input.tex.y + input.tex.x * _DashCapTilt + _DashOffset) * _DashFrequency * _Line_Length);

                clip(col.a);

                return col;
            }
            ENDCG
        }
    }
}