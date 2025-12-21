using UnityEngine;

namespace CraftSharp.Rendering
{
    public struct UnityAABB
    {
        public Vector3 Min;
        public Vector3 Max;
        public readonly bool IsTrigger; // For NoCollision blocks

        public UnityAABB(Vector3 min, Vector3 max, bool isTrigger)
        {
            Min = min;
            Max = max;
            IsTrigger = isTrigger;
        }
    }
}