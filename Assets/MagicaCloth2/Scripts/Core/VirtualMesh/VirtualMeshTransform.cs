// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// VirtualMeshで利用されるTransform情報
    /// スレッドからの利用を考えてデータをTransformから分離しておく
    /// </summary>
    public struct VirtualMeshTransform
    {
        /// <summary>
        /// 識別名（最大２９文字）
        /// </summary>
        public FixedString32Bytes name;

        public int index;
        //public float3 localPosition;
        //public quaternion localRotation;
        //public float3 localScale;
        public float4x4 localToWorldMatrix;
        public float4x4 worldToLocalMatrix;

        public int parentIndex;

        //=========================================================================================
        public VirtualMeshTransform(Transform t)
        {
            Debug.Assert(t);
            name = t.name.Substring(0, math.min(t.name.Length, 29));
            index = -1;
            //localPosition = t.localPosition;
            //localRotation = t.localRotation;
            //localScale = t.localScale;
            localToWorldMatrix = t.localToWorldMatrix;
            worldToLocalMatrix = t.worldToLocalMatrix;
            parentIndex = -1;
        }

        public VirtualMeshTransform(Transform t, int index) : this(t)
        {
            this.index = index;
        }

        public VirtualMeshTransform Clone()
        {
            var mt = new VirtualMeshTransform()
            {
                name = name,
                index = index,
                localToWorldMatrix = localToWorldMatrix,
                worldToLocalMatrix = worldToLocalMatrix,
                parentIndex = parentIndex,
            };

            return mt;
        }

        /// <summary>
        /// ワールド座標原点
        /// </summary>
        public static VirtualMeshTransform Origin
        {
            get
            {
                var mt = new VirtualMeshTransform();
                mt.name = "VirtualMesh Origin";
                //mt.localScale = 1;
                mt.localToWorldMatrix = float4x4.identity;
                mt.worldToLocalMatrix = float4x4.identity;
                mt.parentIndex = -1;
                return mt;
            }
        }

        /// <summary>
        /// ハッシュは名前から生成する
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public void Update(Transform t)
        {
            //localPosition = t.localPosition;
            //localRotation = t.localRotation;
            //localScale = t.localScale;
            localToWorldMatrix = t.localToWorldMatrix;
            worldToLocalMatrix = t.worldToLocalMatrix;
        }

        //=========================================================================================
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 TransformPoint(float3 pos)
        {
            return math.transform(localToWorldMatrix, pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 TransformVector(float3 vec)
        {
            return math.mul(localToWorldMatrix, new float4(vec, 0)).xyz;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 TransformDirection(float3 dir)
        {
            float len = math.length(dir);
            if (len > 0.0f)
                return math.normalize(TransformVector(dir)) * len;
            else
                return dir;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 InverseTransformPoint(float3 pos)
        {
            return math.transform(worldToLocalMatrix, pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 InverseTransformVector(float3 vec)
        {
            return math.mul(worldToLocalMatrix, new float4(vec, 0)).xyz;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 InverseTransformDirection(float3 dir)
        {
            float len = math.length(dir);
            if (len > 0.0f)
                return math.normalize(InverseTransformVector(dir)) * len;
            else
                return dir;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion InverseTransformRotation(quaternion rot)
        {
            return math.mul(new quaternion(worldToLocalMatrix), rot);
        }

        /// <summary>
        /// このTransformのローカル座標をtoのローカル座標に変換するTransformを返す
        /// </summary>
        /// <param name="to"></param>
        /// <returns></returns>
        public VirtualMeshTransform Transform(in VirtualMeshTransform to)
        {
            var mt = new VirtualMeshTransform()
            {
                name = "__(temporary)__",
                index = -1,
                localToWorldMatrix = math.mul(to.worldToLocalMatrix, localToWorldMatrix),
                worldToLocalMatrix = math.mul(worldToLocalMatrix, to.localToWorldMatrix),
                parentIndex = -1,
            };
            return mt;
        }
    }
}
