using System.Collections.Generic;
using UnityEngine;

namespace MinecraftClient.Resource
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

        private static Dictionary<Axis, Vector3> MakeAxisVectors()
        {
            var vectors = new Dictionary<Axis, Vector3>();
            vectors.Add(Axis.X, Vector3.right);   // 1, 0, 0
            vectors.Add(Axis.Y, Vector3.up);      // 0, 1, 0
            vectors.Add(Axis.Z, Vector3.forward); // 0, 0, 1
            return vectors;
        }

        public static Dictionary<Axis, Vector3> axisVectors = MakeAxisVectors();

        public static void RotateVertices(ref Vector3[] original, Vector3 pivot, Axis axis, float degrees, bool rescale)
        {
            // Set up rotation quaternion...
            Quaternion rot = axis switch
            {
                Axis.X => Quaternion.Euler(degrees, 0F, 0F),
                Axis.Y => Quaternion.Euler(0F, degrees, 0F),
                Axis.Z => Quaternion.Euler(0F, 0F, degrees),
                _      => Quaternion.identity
            };

            // And rotate vertices...
            for (int i = 0;i < original.Length;i++)
            {
                Vector3 offset = original[i] - pivot;
                if (rescale)
                {
                    var scaleFrac = 1F / Mathf.Cos(Mathf.Deg2Rad * degrees);
                    //Debug.Log("Rescaling... axis :" + axis + ", " + scaleFrac);
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
                original[i] = rot * (offset) + pivot;
            }

        }

        private static Vector3 ROTCENTER = Vector3.one * 0.5F;

        public static void RotateWrapper(ref Vector3[] original, Vector2Int zyRot)
        {
            // Set up rotation quaternion...
            Quaternion rot = Quaternion.Euler(0F, zyRot.y * 90F, zyRot.x * 90F);

            // And rotate vertices...
            for (int i = 0;i < original.Length;i++)
            {
                original[i] = rot * (original[i] - ROTCENTER) + ROTCENTER;
            }

        }

    }

}
