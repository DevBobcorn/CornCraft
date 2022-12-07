using UnityEngine;
using Unity.Mathematics;

namespace MinecraftClient.Resource
{
    public static class VectorUtil
    {
        // [1.2, 1.3] in Json -> float2(1.2, 1.3) in Unity
        public static float2 Json2Float2(Json.JSONData data)
        {
            var numbers = data.DataArray;
            if (numbers.Count == 2)
                return new float2(float.Parse(numbers[0].StringValue), float.Parse(numbers[1].StringValue));
            
            Debug.LogWarning($"Cannot convert to float2: Invalid json array {data.StringValue}");
            return float2.zero;
        }

        // [1.2, 1.3, 1.4] in Json -> float3(1.2, 1.3, 1.4) in Unity
        public static float3 Json2Float3(Json.JSONData data)
        {
            var numbers = data.DataArray;
            if (numbers.Count == 3)
                return new float3(float.Parse(numbers[0].StringValue), float.Parse(numbers[1].StringValue), float.Parse(numbers[2].StringValue));
            
            Debug.LogWarning($"Cannot convert to float3: Invalid json array {data.StringValue}");
            return float3.zero;
        }

        // [1.2, 1.3, 1.4] in Json -> float3(1.4, 1.3, 1.2) in Unity, with x and z values swapped
        // See https://minecraft.fandom.com/wiki/Coordinates
        public static float3 Json2SwappedFloat3(Json.JSONData data)
        {
            var numbers = data.DataArray;
            if (numbers.Count == 3)
                return new float3(float.Parse(numbers[2].StringValue), float.Parse(numbers[1].StringValue), float.Parse(numbers[0].StringValue));
            
            Debug.LogWarning($"Cannot convert to swapped float3: Invalid json array {data.StringValue}");
            return float3.zero;
        }

        // [1.2, 1.3, 1.4, 1.5] in Json -> float4(1.2, 1.3, 1.4, 1.5) in Unity
        public static float4 Json2Float4(Json.JSONData data)
        {
            var numbers = data.DataArray;
            if (numbers.Count == 4)
                return new float4(float.Parse(numbers[0].StringValue), float.Parse(numbers[1].StringValue), float.Parse(numbers[2].StringValue), float.Parse(numbers[3].StringValue));
            
            Debug.LogWarning($"Cannot convert to float4: Invalid json array {data.StringValue}");
            return float4.zero;
        }
    }
}