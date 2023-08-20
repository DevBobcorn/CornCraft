using System.Collections.Generic;
using Unity.Mathematics;

namespace CraftSharp.Resource
{
    public record EntityModelCube
    {
        public float3 Origin;
        // Size and uv should always be integers so that they can be correctly mapped, See below:
        // https://learn.microsoft.com/en-us/minecraft/creator/documents/entitymodelingandanimation
        public float3 Size;
        // Whole box uv mapping
        public float2 UV;
        // Per-face uv mapping (optional), face direction => (x, y, size x, size y)
        public Dictionary<FaceDir, float4> PerFaceUV = null;
        public float Inflate;
        // Pivot and rotation for cubes are only available in higher versions of bedrock entity model
        public float3 Pivot;
        public float3 Rotation;
    }
}