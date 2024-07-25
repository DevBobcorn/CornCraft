void UnpackExtraVertData_float(float Packed, out float VertLight, out float VertThickness, out float3 VertNormal)
{
    int packedInt = (int) Packed;

    int vertLight       = packedInt & 0xFF;        // Lower 8 bits
    int vertNormalIndex = (packedInt >> 8) & 0x3F; // Middle 6 bits
    int vertThickness   = packedInt >> 14;         // Higher 8 bits

    // Get vertex light value
    VertLight = vertLight / 17.0;

    // Get thickness value
    VertThickness = vertThickness / 255.0;

    // Decode approximate vertex normal
    float3 decoded = float3(0, 0, 0); // float3(1, 1, 1);

    if (      (vertNormalIndex &  0x1) != 0)
    {
        decoded += float3( 1,  0,  0);
    }
    else if ( (vertNormalIndex &  0x2) != 0)
    {
        decoded += float3(-1,  0,  0);
    }

    if (      (vertNormalIndex &  0x4) != 0)
    {
        decoded += float3( 0,  1,  0);
    }
    else if ( (vertNormalIndex &  0x8) != 0)
    {
        decoded += float3(0,  -1,  0);
    }

    if (      (vertNormalIndex & 0x10) != 0)
    {
        decoded += float3( 0,  0,  1);
    }
    else if ( (vertNormalIndex & 0x20) != 0)
    {
        decoded += float3( 0,  0, -1);
    }

    VertNormal = decoded;
}