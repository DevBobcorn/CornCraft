// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// VirtualMeshの共有部分と固有部分を１つにまとめた情報
    /// </summary>
    public class VirtualMeshContainer : IDisposable
    {
        public VirtualMesh shareVirtualMesh;
        public VirtualMesh.UniqueSerializationData uniqueData;

        public VirtualMeshContainer()
        {
        }

        public VirtualMeshContainer(VirtualMesh vmesh)
        {
            shareVirtualMesh = vmesh;
            uniqueData = null;
        }

        public void Dispose()
        {
            shareVirtualMesh?.Dispose();
        }

        //=========================================================================================
        public bool hasUniqueData => uniqueData != null;

        //=========================================================================================
        public int GetTransformCount()
        {
            if (hasUniqueData)
                return uniqueData.transformData.transformArray.Length;
            else
                return shareVirtualMesh.TransformCount;
        }

        public Transform GetTransformFromIndex(int index)
        {
            if (hasUniqueData)
                return uniqueData.transformData.transformArray[index];
            else
                return shareVirtualMesh.transformData.GetTransformFromIndex(index);
        }

        public Transform GetCenterTransform()
        {
            if (hasUniqueData)
                return uniqueData.transformData.transformArray[shareVirtualMesh.centerTransformIndex];
            else
                return shareVirtualMesh.GetCenterTransform();
        }
    }
}
