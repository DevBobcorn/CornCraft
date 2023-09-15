// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
#if MC2_DEBUG
using UnityEngine;
#endif

namespace MagicaCloth2
{
#if MC2_DEBUG
    /// <summary>
    /// VirtualMeshのデバッグ表示設定
    /// </summary>
    [System.Serializable]
    public class VirtualMeshDebugSettings
    {
        public enum DebugAxis
        {
            None,
            Normal,
            All,
        }

        [Header("<<< MC2_DEBUG >>>")]
        public bool enable = false;
        [Range(0.003f, 0.1f)]
        public float pointSize = 0.01f;
        [Range(0.003f, 0.1f)]
        public float lineSize = 0.03f;
        public bool position = true;
        public bool axis = false;
        public bool indexNumber = false;
        public bool boneWeight = false;
        public bool uv = false;
        public bool depth = false;
        public bool rootIndex = false;
        public bool parentIndex = false;
        public bool line = true;
        public bool edgeNumber = false;
        public bool triangle = true;
        public bool triangleNormal = false;
        public bool triangleTangent = false;
        public bool triangleNumber = false;
        public bool baseLine = false;
        public bool boneName = false;
        public int vertexMinIndex = 0;
        public int vertexMaxIndex = 100000;
        public int edgeMinIndex = 0;
        public int edgeMaxIndex = 100000;
        public int triangleMinIndex = 0;
        public int triangleMaxIndex = 100000;
    }
#endif // MC2_DEBUG
}
