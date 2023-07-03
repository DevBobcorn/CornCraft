// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace MagicaCloth2
{
    /// <summary>
    /// このAABB構造体はUnity.Mathematics.Geometry.MinMaxAABBを移植したものです。
    /// MinMaxAABBはinternalのため外部から利用することができません。
    /// このコードはすべてMinMaxAABBのコピーとなります。
    /// </summary>
    [System.Serializable]
    public struct AABB : IEquatable<AABB>
    {
        /// <summary>
        /// The minimum point contained by the AABB.
        /// </summary>
        /// <remarks>
        /// If any component of <see cref="Min"/> is greater than <see cref="Max"/> then this AABB is invalid.
        /// </remarks>
        /// <seealso cref="IsValid"/>
        public float3 Min;

        /// <summary>
        /// The maximum point contained by the AABB.
        /// </summary>
        /// <remarks>
        /// If any component of <see cref="Max"/> is less than <see cref="Min"/> then this AABB is invalid.
        /// </remarks>
        /// <seealso cref="IsValid"/>
        public float3 Max;

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
        public AABB(in float3 min, in float3 max)
        {
            Min = min;
            Max = max;
        }

        /// <summary>
        /// Creates the AABB from a center and extents.
        /// </summary>
        /// <remarks>
        /// This function takes full extents. It is the distance between <see cref="Min"/> and <see cref="Max"/>.
        /// If you have half extents, you can call <see cref="CreateFromCenterAndHalfExtents"/>.
        /// </remarks>
        /// <param name="center">Center of AABB.</param>
        /// <param name="extents">Full extents of AABB.</param>
        /// <returns>AABB created from inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AABB CreateFromCenterAndExtents(float3 center, float3 extents)
        {
            return CreateFromCenterAndHalfExtents(center, extents * 0.5f);
        }

        /// <summary>
        /// Creates the AABB from a center and half extents.
        /// </summary>
        /// <remarks>
        /// This function takes half extents. It is half the distance between <see cref="Min"/> and <see cref="Max"/>.
        /// If you have full extents, you can call <see cref="CreateFromCenterAndExtents"/>.
        /// </remarks>
        /// <param name="center">Center of AABB.</param>
        /// <param name="halfExtents">Half extents of AABB.</param>
        /// <returns>AABB created from inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AABB CreateFromCenterAndHalfExtents(float3 center, float3 halfExtents)
        {
            return new AABB(center - halfExtents, center + halfExtents);
        }

        /// <summary>
        /// Computes the extents of the AABB.
        /// </summary>
        /// <remarks>
        /// Extents is the componentwise distance between min and max.
        /// </remarks>
        public float3 Extents => Max - Min;

        /// <summary>
        /// Computes the half extents of the AABB.
        /// </summary>
        /// <remarks>
        /// HalfExtents is half of the componentwise distance between min and max. Subtracting HalfExtents from Center
        /// gives Min and adding HalfExtents to Center gives Max.
        /// </remarks>
        public float3 HalfExtents => (Max - Min) * 0.5f;

        /// <summary>
        /// Computes the center of the AABB.
        /// </summary>
        public float3 Center => (Max + Min) * 0.5f;

        /// <summary>
        /// 最大の辺の長さを返す
        /// </summary>
        public float MaxSideLength
        {
            get
            {
                var ext = Extents;
                return math.max(math.max(ext.x, ext.y), ext.z);
            }
        }

        /// <summary>
        /// Check if the AABB is valid.
        /// </summary>
        /// <remarks>
        /// An AABB is considered valid if <see cref="Min"/> is componentwise less than or equal to <see cref="Max"/>.
        /// </remarks>
        /// <returns>True if <see cref="Min"/> is componentwise less than or equal to <see cref="Max"/>.</returns>
        public bool IsValid => math.all(Min <= Max);

        /// <summary>
        /// Computes the surface area for this axis aligned bounding box.
        /// </summary>
        public float SurfaceArea
        {
            get
            {
                float3 diff = Max - Min;
                return 2 * math.dot(diff, diff.yzx);
            }
        }

        /// <summary>
        /// Tests if the input point is contained by the AABB.
        /// </summary>
        /// <param name="point">Point to test.</param>
        /// <returns>True if AABB contains the input point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(in float3 point) => math.all(point >= Min & point <= Max);

        /// <summary>
        /// Tests if the input AABB is contained entirely by this AABB.
        /// </summary>
        /// <param name="aabb">AABB to test.</param>
        /// <returns>True if input AABB is contained entirely by this AABB.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(in AABB aabb) => math.all((Min <= aabb.Min) & (Max >= aabb.Max));

        /// <summary>
        /// Tests if the input AABB overlaps this AABB.
        /// </summary>
        /// <param name="aabb">AABB to test.</param>
        /// <returns>True if input AABB overlaps with this AABB.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(in AABB aabb)
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
        public void Expand(float signedDistance)
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
        public void Encapsulate(in AABB aabb)
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
        public void Encapsulate(in float3 point)
        {
            Min = math.min(Min, point);
            Max = math.max(Max, point);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(AABB other)
        {
            return Min.Equals(other.Min) && Max.Equals(other.Max);
        }

        /// <summary>
        /// 変換マトリックスにより座標を変換させる
        /// </summary>
        /// <param name="toM"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Transform(in float4x4 toM)
        {
            float3 min = math.transform(toM, Min);
            float3 max = math.transform(toM, Max);
            Min = math.min(min, max);
            Max = math.max(min, max);
        }

        public override string ToString()
        {
            //return string.Format("AABB({0}, {1})", Min, Max);
            return $"AABB Center:{Center}, HalfExtents:{HalfExtents}, Min:{Min}, Max:{Max}";
        }
    }

}
