using System.Runtime.CompilerServices;
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class ParticleTransform
    {
        public Vector3 Position;
        public float Scale = 0F;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4 GetAsVector4()
        {
            return new Vector4(Position.x, Position.y, Position.z, Scale);
        }
    }
}