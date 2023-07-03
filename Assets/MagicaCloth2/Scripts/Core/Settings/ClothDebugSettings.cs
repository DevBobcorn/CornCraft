// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
#if MC2_DEBUG
using UnityEngine;
#endif

namespace MagicaCloth2
{
    /// <summary>
    /// クロスのデバッグ表示設定
    /// </summary>
    [System.Serializable]
    public class ClothDebugSettings
    {
        public enum DebugAxis
        {
            None,
            Normal,
            All,
        }

        //=====================================================================
        // ■公開するもの
        //=====================================================================
        public bool enable = false;
        public bool ztest = false;
        public bool position = true;
        public DebugAxis axis = DebugAxis.None;
        public bool shape = false;
        public bool baseLine = false;
        public bool depth = false;
        public bool collider = true;
        public bool animatedPosition = false;
        public DebugAxis animatedAxis = DebugAxis.None;
        public bool animatedShape = false;
        //public bool basicPosition = false;
        //public DebugAxis basicAxis = DebugAxis.None;
        //public bool basicShape = false;

        //=====================================================================
        // ■デバッグ用
        //=====================================================================
#if MC2_DEBUG
        //[Space]
        //[Header("[MC2_DEBUG]")]
        [Header("<<< MC2_DEBUG >>>")]
        [Range(0.003f, 0.1f)]
        public float pointSize = 0.01f;
        public bool referOldPos = false;
        public bool radius = true;
        public bool localNumber = false;
        public bool particleNumber = false;
        public bool triangleNumber = false;
        public bool friction = false;
        public bool staticFriction = false;
        public bool attribute = false;
        //public bool verticalDistanceConstraint = false;
        //public bool horizontalDistanceConstraint = false;
        public bool collisionNormal = false;
        public bool cellCube = false;
        public int vertexMinIndex = 0;
        public int vertexMaxIndex = 100000;
        public int triangleMinIndex = 0;
        public int triangleMaxIndex = 100000;
#endif

        //=========================================================================================
        public bool CheckParticleDrawing(int index)
        {
#if MC2_DEBUG
            return index >= vertexMinIndex && index <= vertexMaxIndex;
#else
            return true;
#endif
        }

        public bool CheckTriangleDrawing(int index)
        {
#if MC2_DEBUG
            return index >= triangleMinIndex && index <= triangleMaxIndex;
#else
            return true;
#endif
        }

        public bool CheckRadiusDrawing()
        {
#if MC2_DEBUG
            return radius;
#else
            return true;
#endif
        }

        public float GetPointSize()
        {
#if MC2_DEBUG
            return pointSize;
#else
            return 0.01f;
#endif
        }

        public float GetLineSize()
        {
            return 0.03f; // 固定
        }

        public float GetInertiaCenterRadius()
        {
            return 0.01f; // 固定
        }

        public bool IsReferOldPos()
        {
#if MC2_DEBUG
            return referOldPos;
#else
            return false;
#endif
        }
    }
}
