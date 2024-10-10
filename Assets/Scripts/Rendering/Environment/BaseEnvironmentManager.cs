#nullable enable
using UnityEngine;

namespace CraftSharp.Rendering
{
    public abstract class BaseEnvironmentManager : MonoBehaviour
    {
        public abstract void SetCamera(Camera mainCamera);

        public abstract void SetTime(long dayTime);

        public abstract void SetRain(bool raining);
        
        public abstract string GetTimeString();
    }
}