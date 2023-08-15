using Unity.Mathematics;

namespace CraftSharp.Resource
{
    public record EntityModelCube
    {
        public float3 Origin;
        // Size and uv should always be integers so that they can be correctly mapped, See below:
        // https://learn.microsoft.com/en-us/minecraft/creator/documents/entitymodelingandanimation
        public float3 Size;
        public float2 UV;
        public float Inflate;
        // Pivot and rotation for cubes are only available in higher versions of bedrock entity model
        public float3 Pivot;
        public float3 Rotation;
    }
}