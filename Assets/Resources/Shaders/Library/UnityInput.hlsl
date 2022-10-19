CBUFFER_START(UnityPerMaterial)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
CBUFFER_END

CBUFFER_START(UnityPerDraw)
    float4x4 unity_MatrixVP;
    float4x4 unity_MatrixV;
    float4x4 glstate_matrix_projection;
    float4 unity_LODFade;
    real4 unity_WorldTransformParams;
CBUFFER_END