using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace CraftSharp.Resource
{
    public static class Rotations
    {
        // rotation used in model texture uvs
        public enum UVRot
        {
            UV_0, UV_90, UV_180, UV_270
        }

        // Minecraft Z, Y ,X axis
        // X and Z axis are swapped for Unity use
        public enum Axis
        {
            X, Y, Z
        }

        private static Dictionary<Axis, float3> MakeAxisVectors()
        {
            var vectors = new Dictionary<Axis, float3>
            {
                { Axis.X, Vector3.right },   // 1, 0, 0
                { Axis.Y, Vector3.up },      // 0, 1, 0
                { Axis.Z, Vector3.forward }  // 0, 0, 1
            };
            return vectors;
        }

        // By default unity uses z-x-y order for euler angles, while
        // we use x-y-z order for our bedrock entity models, so we'll
        // do a bit conversion here
        public static Quaternion RotationFromEularsXYZ(float3 eularsXYZ)
        {
            return Quaternion.AngleAxis(eularsXYZ.x, Vector3.right) *
                    Quaternion.AngleAxis(eularsXYZ.y, Vector3.up) *
                    Quaternion.AngleAxis(eularsXYZ.z, Vector3.forward);
        }

        // For Bedrock Edition Entity models
        public static void RotateVertices(ref float3[] original, float3 pivot, float3 eularsXYZ, float downScale = 1F, int startIndex = 0)
        {
            // Set up rotation quaternion...
            Quaternion rot = RotationFromEularsXYZ(eularsXYZ);

            // And rotate vertices...
            for (int i = startIndex;i < original.Length;i++)
            {
                var offset = original[i] - pivot;

                if (downScale == 1F)
                {
                    original[i] = (float3)(rot * offset) + pivot;
                }
                else
                {
                    original[i] = ((float3)(rot * offset) + pivot) / downScale;
                }
            }
        }

        public static Dictionary<Axis, float3> axisVectors = MakeAxisVectors();

        // For Java Edition Block/Item models
        public static void RotateVertices(ref float3[] original, float3 pivot, Axis axis, float degree, bool rescale)
        {
            // Set up rotation quaternion...
            Quaternion rot = axis switch
            {
                Axis.X => Quaternion.Euler(degree, 0F, 0F),
                Axis.Y => Quaternion.Euler(0F, degree, 0F),
                Axis.Z => Quaternion.Euler(0F, 0F, degree),
                _      => Quaternion.identity
            };

            // And rotate vertices...
            for (int i = 0;i < original.Length;i++)
            {
                var offset = original[i] - pivot;
                if (rescale)
                {
                    var scaleFrac = 1F / Mathf.Cos(Mathf.Deg2Rad * degree);
                    switch (axis)
                    {
                        case Axis.X:
                            offset.y *= scaleFrac;
                            offset.z *= scaleFrac;
                            break;
                        case Axis.Y:
                            offset.x *= scaleFrac;
                            offset.z *= scaleFrac;
                            break;
                        case Axis.Z:
                            offset.x *= scaleFrac;
                            offset.y *= scaleFrac;
                            break;
                    }
                }
                original[i] = (float3)(rot * offset) + pivot; // TODO Make this better
            }
        }

        private static float3 ROTCENTER = new(0.5F, 0.5F, 0.5F);

        // For Java Edition Blockstate models
        public static void RotateWrapper(ref float3[] original, int2 zyRot)
        {
            // Set up rotation quaternion...
            Quaternion rot = Quaternion.Euler(0F, zyRot.y * 90F, zyRot.x * 90F);

            // And rotate vertices...
            for (int i = 0;i < original.Length;i++)
            {
                original[i] = (float3)(rot * (original[i] - ROTCENTER)) + ROTCENTER; // TODO Make this better
            }
        }
    }
}
