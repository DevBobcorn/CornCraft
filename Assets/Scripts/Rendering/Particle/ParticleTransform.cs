using System.Runtime.CompilerServices;
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class ParticleTransform
    {
        public Vector3 position;
        public Vector3 scale = Vector3.one;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4x4 GetTransformMatrix4x4(Vector3 rootPos, Vector3 cameraPos, Vector3 cameraUp)
        {
            var dir = cameraPos - position;
            
            return Matrix4x4.Translate(position - rootPos) * 
                   Matrix4x4.Scale(scale) * 
                   Matrix4x4.Rotate(Quaternion.LookRotation(dir, cameraUp));
        }
    }
}