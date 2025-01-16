using UnityEngine;

namespace CraftSharp.Rendering
{
    public class ParticleStateData<T> where T : ParticleExtraData
    {
        public Vector3 Velocity;
        public float LifeTime;
        public T ExtraData;
    }
}