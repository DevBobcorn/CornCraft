// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace MagicaReductionMesh
{
    /// <summary>
    /// デバッグ機能
    /// </summary>
    public class DebugData : ReductionMeshAccess
    {
        /// <summary>
        /// メッシュ状態ログ表示
        /// </summary>
        public void DispMeshInfo(string header = "")
        {
            Debug.Log(header + (string.IsNullOrEmpty(header) ? "" : "\n")
                + " Mesh:" + MeshData.MeshCount
                + " VertexCnt:" + MeshData.VertexCount
                + " TriangleCnt:" + MeshData.TriangleCount
                + " LineCnt:" + MeshData.LineCount
                + " TetraCnt:" + MeshData.TetraCount
                );
        }

        //=========================================================================================
        /// <summary>
        /// FinalDataの共有頂点デバッグ表示
        /// (OnDrawGizmos内で呼び出してください）
        /// </summary>
        /// <param name="final"></param>
        /// <param name="drawNormal"></param>
        public static void DebugDrawShared(
            FinalData final,
            bool drawTriangle = true, bool drawLine = true, bool drawTetra = true,
            bool drawVertexNormal = true, bool drawVertexTangent = true, bool drawNumber = false,
            int maxPolygonCount = int.MaxValue, int layer = -1, int tetraIndex = -1, float tetraSize = 1.0f,
            List<int> drawNumberList = null,
            float axisSize = 0.01f
            )
        {
            if (final == null)
                return;

            List<Vector3> wposList;
            List<Vector3> wnorList;
            List<Vector4> wtanList;
            Utility.CalcFinalDataWorldPositionNormalTangent(final, out wposList, out wnorList, out wtanList);

            // triangle
            if (drawTriangle)
            {
                int tcnt = final.TriangleCount;
                for (int i = 0; i < tcnt && i < maxPolygonCount; i++)
                {
                    int index = i * 3;
                    int vi0 = final.triangles[index];
                    int vi1 = final.triangles[index + 1];
                    int vi2 = final.triangles[index + 2];

                    if (drawNumberList != null && drawNumberList.Count > 0)
                    {
                        if (drawNumberList.Contains(vi0) == false && drawNumberList.Contains(vi1) == false && drawNumberList.Contains(vi2) == false)
                            continue;
                    }

                    var v0 = wposList[vi0];
                    var v1 = wposList[vi1];
                    var v2 = wposList[vi2];

                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(v0, v1);
                    Gizmos.DrawLine(v1, v2);
                    Gizmos.DrawLine(v0, v2);
                }
            }

            // line
            if (drawLine)
            {
                Gizmos.color = Color.cyan;
                int lcnt = final.LineCount;
                for (int i = 0; i < lcnt; i++)
                {
                    int index = i * 2;
                    int vi0 = final.lines[index];
                    int vi1 = final.lines[index + 1];

                    Gizmos.DrawLine(wposList[vi0], wposList[vi1]);
                }
            }

            // tetra
            if (drawTetra)
            {
                Gizmos.color = Color.green;
                int tetracnt = final.TetraCount;
                for (int i = 0; i < tetracnt; i++)
                {
                    DrawTetra(final, i, wposList, tetraSize);
                }
            }
            if (tetraIndex >= 0 && tetraIndex < final.TetraCount)
            {
                Gizmos.color = Color.red;
                DrawTetra(final, tetraIndex, wposList, tetraSize);
            }

            // normal
            if (drawVertexNormal)
            {
                Gizmos.color = Color.blue;
                if (drawNumberList != null && drawNumberList.Count > 0)
                {
                    foreach (var i in drawNumberList)
                    {
                        var wpos = wposList[i];
                        Gizmos.DrawLine(wpos, wpos + wnorList[i] * axisSize);
                    }
                }
                else
                {
                    for (int i = 0; i < final.VertexCount; i++)
                    {
                        var wpos = wposList[i];
                        Gizmos.DrawLine(wpos, wpos + wnorList[i] * axisSize);
                    }
                }
            }

            // tangent
            if (drawVertexTangent)
            {
                Gizmos.color = Color.red;
                if (drawNumberList != null && drawNumberList.Count > 0)
                {
                    foreach (var i in drawNumberList)
                    {
                        var wpos = wposList[i];
                        Vector3 wtan = wtanList[i];
                        Gizmos.DrawLine(wpos, wpos + wtan * axisSize);
                    }
                }
                else
                {
                    for (int i = 0; i < final.VertexCount; i++)
                    {
                        var wpos = wposList[i];
                        Vector3 wtan = wtanList[i];
                        Gizmos.DrawLine(wpos, wpos + wtan * axisSize);
                    }
                }
            }

            // number
#if UNITY_EDITOR
            if (drawNumber)
            {
                if (drawNumberList != null && drawNumberList.Count > 0)
                {
                    foreach (var i in drawNumberList)
                    {
                        var wpos = wposList[i];
                        Handles.Label(wpos, "(" + i.ToString() + ")");
                    }
                }
                else
                {
                    for (int i = 0; i < final.VertexCount; i++)
                    {
                        var wpos = wposList[i];
                        Handles.Label(wpos, "(" + i.ToString() + ")");
                    }
                }
            }
#endif
        }

        private static void DrawTetra(FinalData final, int tetraIndex, List<Vector3> wposList, float tetraSize)
        {
            // サイズチェック
            if (final.tetraSizes[tetraIndex] > tetraSize)
                return;

            int index = tetraIndex * 4;
            int vi0 = final.tetras[index];
            int vi1 = final.tetras[index + 1];
            int vi2 = final.tetras[index + 2];
            int vi3 = final.tetras[index + 3];

            var v0 = wposList[vi0];
            var v1 = wposList[vi1];
            var v2 = wposList[vi2];
            var v3 = wposList[vi3];

            Gizmos.DrawLine(v0, v1);
            Gizmos.DrawLine(v0, v2);
            Gizmos.DrawLine(v0, v3);
            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v3);
            Gizmos.DrawLine(v3, v1);
        }

        /// <summary>
        /// FinalDataの子頂点デバッグ表示
        /// (OnDrawGizmos内で呼び出してください）
        /// </summary>
        /// <param name="final"></param>
        /// <param name="drawNormal"></param>
        public static void DebugDrawChild(
            FinalData final, bool drawPosition = false, bool drawNormal = false, bool drawTriangle = false, bool drawNumber = false,
            int maxVertexCount = int.MaxValue,
            float positionSize = 0.001f, float axisSize = 0.01f
            )
        {
            if (final == null)
                return;

            if (drawPosition == false && drawNormal == false && drawTriangle == false && drawNumber == false)
                return;

            List<Vector3> swposList;
            List<Vector3> swnorList;
            List<Vector4> swtanList;
            Utility.CalcFinalDataWorldPositionNormalTangent(final, out swposList, out swnorList, out swtanList);

            for (int mindex = 0; mindex < final.MeshCount; mindex++)
            {
                List<Vector3> wposList;
                List<Vector3> wnorList;
                List<Vector4> wtanList;
                Utility.CalcFinalDataChildWorldPositionNormalTangent(
                    final, mindex, swposList, swnorList, swtanList,
                    out wposList, out wnorList, out wtanList
                    );

                for (int i = 0; i < wposList.Count && i < maxVertexCount; i++)
                {
                    var wpos = wposList[i];

                    // position
                    if (drawPosition)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawSphere(wpos, positionSize);
                    }

                    // normal
                    if (drawNormal)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawLine(wpos, wpos + wnorList[i] * axisSize);
                    }

#if UNITY_EDITOR
                    // number
                    if (drawNumber)
                    {
                        Handles.Label(wpos, i.ToString());
                    }
#endif
                }

                // triangle
                if (drawTriangle)
                {
                    Gizmos.color = Color.magenta;
                    var triangles = final.meshList[mindex].mesh.triangles;
                    for (int i = 0; i < triangles.Length / 3; i++)
                    {
                        int v0 = triangles[i * 3];
                        int v1 = triangles[i * 3 + 1];
                        int v2 = triangles[i * 3 + 2];

                        var wpos0 = wposList[v0];
                        var wpos1 = wposList[v1];
                        var wpos2 = wposList[v2];

                        Gizmos.DrawLine(wpos0, wpos1);
                        Gizmos.DrawLine(wpos1, wpos2);
                        Gizmos.DrawLine(wpos2, wpos0);
                    }
                }
            }
        }
    }
}
