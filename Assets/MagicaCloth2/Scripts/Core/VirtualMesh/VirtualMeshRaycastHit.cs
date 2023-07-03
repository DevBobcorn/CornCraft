// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Mathematics;

namespace MagicaCloth2
{
    public struct VirtualMeshRaycastHit : IComparable<VirtualMeshRaycastHit>, IValid
    {
        public VirtualMeshPrimitive type;
        public int index;
        public float3 position;
        public float3 normal;
        public float distance;

        public int CompareTo(VirtualMeshRaycastHit other)
        {
            if (distance != other.distance)
                return distance < other.distance ? -1 : 1;
            else
                return 0;
        }

        public bool IsValid()
        {
            return type != VirtualMeshPrimitive.None;
        }

        public override string ToString()
        {
            return $"{type} [{index}] pos:{position}, dist:{distance}, nor:{normal}";
        }
    }
}
