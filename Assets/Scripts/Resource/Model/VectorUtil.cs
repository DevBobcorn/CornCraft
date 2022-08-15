using UnityEngine;

namespace MinecraftClient.Resource
{
    public static class VectorUtil
    {
        // [1.2, 1.3] in Json -> Vector2(1.2, 1.3) in Unity
        public static Vector2 Json2Vector2(Json.JSONData data)
        {
            var numbers = data.DataArray;
            if (numbers.Count == 2)
                return new Vector2(float.Parse(numbers[0].StringValue), float.Parse(numbers[1].StringValue));
            
            Debug.LogWarning("Cannot convert to Vector2: Invalid json array");
            return Vector2.zero;
        }

        // [1.2, 1.3, 1.4] in Json -> Vector3(1.2, 1.3, 1.4) in Unity
        public static Vector3 Json2Vector3(Json.JSONData data)
        {
            var numbers = data.DataArray;
            if (numbers.Count == 3)
                return new Vector3(float.Parse(numbers[0].StringValue), float.Parse(numbers[1].StringValue), float.Parse(numbers[2].StringValue));
            
            Debug.LogWarning("Cannot convert to Vector3: Invalid json array");
            return Vector3.zero;
        }

        // [1.2, 1.3, 1.4] in Json -> Vector3(1.4, 1.3, 1.2) in Unity, with x and z values swapped
        // See https://minecraft.fandom.com/wiki/Coordinates
        public static Vector3 Json2SwappedVector3(Json.JSONData data)
        {
            var numbers = data.DataArray;
            if (numbers.Count == 3)
                return new Vector3(float.Parse(numbers[2].StringValue), float.Parse(numbers[1].StringValue), float.Parse(numbers[0].StringValue));
            
            Debug.LogWarning("Cannot convert to Vector3: Invalid json array");
            return Vector3.zero;
        }

        // [1.2, 1.3, 1.4, 1.5] in Json -> Vector4(1.2, 1.3, 1.4, 1.5) in Unity
        public static Vector4 Json2Vector4(Json.JSONData data)
        {
            var numbers = data.DataArray;
            if (numbers.Count == 4)
                return new Vector4(float.Parse(numbers[0].StringValue), float.Parse(numbers[1].StringValue), float.Parse(numbers[2].StringValue), float.Parse(numbers[3].StringValue));
            
            Debug.LogWarning("Cannot convert to Vector4: Invalid json array");
            return Vector4.zero;
        }
    }
}