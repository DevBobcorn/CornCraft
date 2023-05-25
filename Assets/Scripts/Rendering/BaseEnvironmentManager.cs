#nullable enable
using UnityEngine;

namespace MinecraftClient.Rendering
{
    public abstract class BaseEnvironmentManager : MonoBehaviour
    {
        public abstract void SetTime(long dayTime);

        public abstract void SetRain(bool raining);
        
        public abstract string GetTimeString();
    }
}