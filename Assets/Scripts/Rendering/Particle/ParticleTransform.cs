using System.Runtime.CompilerServices;
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class ParticleTransform
    {
        public Vector3 Position;
        public Vector3 Scale = Vector3.one;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4x4 GetTransformMatrix4x4(Vector3 cameraPos, Vector3 cameraUp)
        {
            var dir = cameraPos - Position;
            
            return Matrix4x4.Translate(Position) * 
                   Matrix4x4.Scale(Scale) * 
                   Matrix4x4.Rotate(Quaternion.LookRotation(dir, cameraUp));
        }
    }
}