using UnityEngine;

namespace CraftSharp.Rendering
{
    public struct UnityAABB
    {
        public Vector3 Min;
        public Vector3 Max;
        public bool IsLiquid;
        public bool IsTrigger; // For NoCollision blocks

        public UnityAABB(Vector3 min, Vector3 max, bool isLiquid, bool isTrigger)
        {
            Min = min;
            Max = max;
            IsLiquid = isLiquid;
            IsTrigger = isTrigger;
        }
    }
}