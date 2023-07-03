// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    public static class MeshUtility
    {
        /// <summary>
        /// レンダラーからSharedMeshを取得する
        /// </summary>
        /// <param name="ren"></param>
        /// <returns></returns>
        public static Mesh GetSharedMesh(Renderer ren)
        {
            if (ren == null)
                return null;

            if (ren is SkinnedMeshRenderer)
            {
                var sren = ren as SkinnedMeshRenderer;
                return sren.sharedMesh;
            }
            else
            {
                // mesh filter
                var filter = ren.GetComponent<MeshFilter>();
                if (filter == null)
                {
                    Debug.LogError("Not found MeshFilter!");
                    return null;
                }

                return filter.sharedMesh;
            }
        }

        /// <summary>
        /// レンダラーにメッシュを設定する
        /// </summary>
        /// <param name="ren"></param>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static bool SetMesh(Renderer ren, Mesh mesh, Transform[] skinBones = null)
        {
            if (ren is SkinnedMeshRenderer)
            {
                var sren = ren as SkinnedMeshRenderer;
                sren.sharedMesh = mesh;

                if (skinBones != null && skinBones.Length > 0)
                    sren.bones = skinBones;
            }
            else
            {
                // mesh filter
                var filter = ren.GetComponent<MeshFilter>();
                if (filter == null)
                {
                    Debug.LogError("Not found MeshFilter!");
                    return false;
                }

                filter.mesh = mesh;
            }

            return true;
        }

        /// <summary>
        /// このレンダラーが利用しているTransformの数を返す
        /// この数は概算であり正確ではないので注意！
        /// </summary>
        /// <param name="ren"></param>
        /// <returns></returns>
        public static int GetTransformCount(Renderer ren)
        {
            int tcnt = 0;

            if (ren)
            {
                // renderer
                tcnt++;

                if (ren is SkinnedMeshRenderer)
                {
                    var sren = ren as SkinnedMeshRenderer;

                    // root bone
                    tcnt++;

                    // skin bones
                    tcnt += sren.bones?.Length ?? 0;
                }
            }

            return tcnt;
        }
    }
}
