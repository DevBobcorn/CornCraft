// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace MagicaCloth2
{
    /// <summary>
    /// int3型のAABB
    /// </summary>
    [System.Serializable]
    public struct IntAABB : IEquatable<IntAABB>
    {
        /// <summary>
        /// The minimum point contained by the AABB.
        /// </summary>
        /// <remarks>
        /// If any component of <see cref="Min"/> is greater than <see cref="Max"/> then this AABB is invalid.
        /// </remarks>
        /// <seealso cref="IsValid"/>
        public int3 Min;

        /// <summary>
        /// The maximum point contained by the AABB.
        /// </summary>
        /// <remarks>
        /// If any component of <see cref="Max"/> is less than <see cref="Min"/> then this AABB is invalid.
        /// </remarks>
        /// <seealso cref="IsValid"/>
        public int3 Max;

        /// <summary>
        /// Constructs the AABB with the given minimum and maximum.
        /// </summary>
        /// <remarks>
        /// If you have a center and extents, you can call <see cref="CreateFromCenterAndExtents"/> or <see cref="CreateFromCenterAndHalfExtents"/>
        /// to create the AABB.
        /// </remarks>
        /// <param name="min">Minimum point inside AABB.</param>
        /// <param name="max">Maximum point inside AABB.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntAABB(int3 min, int3 max)
        {
            Min = min;
            Max = max;
        }

        /// <summary>
        /// Computes the extents of the AABB.
        /// </summary>
        /// <remarks>
        /// Extents is the componentwise distance between min and max.
        /// </remarks>
        public int3 Extents => Max - Min;

        /// <summary>
        /// Computes the center of the AABB.
        /// </summary>
        public int3 Center => (Max + Min) / 2;

        /// <summary>
        /// Check if the AABB is valid.
        /// </summary>
        /// <remarks>
        /// An AABB is considered valid if <see cref="Min"/> is componentwise less than or equal to <see cref="Max"/>.
        /// </remarks>
        /// <returns>True if <see cref="Min"/> is componentwise less than or equal to <see cref="Max"/>.</returns>
        public bool IsValid => math.all(Min <= Max);

        /// <summary>
        /// Tests if the input point is contained by the AABB.
        /// </summary>
        /// <param name="point">Point to test.</param>
        /// <returns>True if AABB contains the input point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int3 point) => math.all(point >= Min & point <= Max);

        /// <summary>
        /// Tests if the input AABB is contained entirely by this AABB.
        /// </summary>
        /// <param name="aabb">AABB to test.</param>
        /// <returns>True if input AABB is contained entirely by this AABB.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(IntAABB aabb) => math.all((Min <= aabb.Min) & (Max >= aabb.Max));

        /// <summary>
        /// Tests if the input AABB overlaps this AABB.
        /// </summary>
        /// <param name="aabb">AABB to test.</param>
        /// <returns>True if input AABB overlaps with this AABB.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(IntAABB aabb)
        {
            return math.all(Max >= aabb.Min & Min <= aabb.Max);
        }

        /// <summary>
        /// Expands the AABB by the given signed distance.
        /// </summary>
        /// <remarks>
        /// Positive distance expands the AABB while negative distance shrinks the AABB.
        /// </remarks>
        /// <param name="signedDistance">Signed distance to expand the AABB with.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Expand(int signedDistance)
        {
            Min -= signedDistance;
            Max += signedDistance;
        }

        /// <summary>
        /// Encapsulates the given AABB.
        /// </summary>
        /// <remarks>
        /// Modifies this AABB so that it contains the given AABB. If the given AABB is already contained by this AABB,
        /// then this AABB doesn't change.
        /// </remarks>
        /// <seealso cref="Contains(Unity.Mathematics.Geometry.MinMaxAABB)"/>
        /// <param name="aabb">AABB to encapsulate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encapsulate(IntAABB aabb)
        {
            Min = math.min(Min, aabb.Min);
            Max = math.max(Max, aabb.Max);
        }

        /// <summary>
        /// Encapsulate the given point.
        /// </summary>
        /// <remarks>
        /// Modifies this AABB so that it contains the given point. If the given point is already contained by this AABB,
        /// then this AABB doesn't change.
        /// </remarks>
        /// <seealso cref="Contains(Unity.Mathematics.float3)"/>
        /// <param name="point">Point to encapsulate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encapsulate(int3 point)
        {
            Min = math.min(Min, point);
            Max = math.max(Max, point);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(IntAABB other)
        {
            return Min.Equals(other.Min) && Max.Equals(other.Max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return string.Format("AABB({0}, {1})", Min, Max);
        }
    }

}
