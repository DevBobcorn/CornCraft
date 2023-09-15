// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class VirtualMesh
    {
        /// <summary>
        /// メッシュを最適化する
        /// </summary>
        public void Optimization()
        {
            try
            {
                // 重複トライアングルの削除
                RemoveDuplicateTriangles();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                result.SetError(Define.Result.Optimize_Exception);
            }
        }

        /// <summary>
        /// 重複するトライアングルを除去する
        /// </summary>
        void RemoveDuplicateTriangles()
        {
            if (TriangleCount < 2)
                return;

            using var edgeToTriangleList = new NativeParallelHashMap<int2, FixedList128Bytes<int>>(TriangleCount * 2, Allocator.Persistent);
            using var newTriangles = new NativeList<int3>(TriangleCount, Allocator.Persistent);

            using var useQuadSet = new NativeParallelHashSet<int4>(TriangleCount / 4, Allocator.Persistent); // Unity2023.1.5対応
            using var removeTriangleSet = new NativeParallelHashSet<int3>(TriangleCount / 4, Allocator.Persistent); // Unity2023.1.5対応

            var job1 = new Optimize_EdgeToTrianlgeJob()
            {
                tcnt = TriangleCount,
                triangles = triangles.GetNativeArray(),
                localPositions = localPositions.GetNativeArray(),
                edgeToTriangleList = edgeToTriangleList,
                newTriangles = newTriangles,

                useQuadSet = useQuadSet, // Unity2023.1.5対応
                removeTriangleSet = removeTriangleSet, // Unity2023.1.5対応
            };
            job1.Run();

            // 新しいトライアングルリストに入れ替える
            triangles?.Dispose();
            triangles = new ExSimpleNativeArray<int3>();
            if (newTriangles.Length > 0)
            {
                triangles.AddRange(newTriangles);
            }
        }

        [BurstCompile]
        struct Optimize_EdgeToTrianlgeJob : IJob
        {
            public int tcnt;
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;

            public NativeParallelHashMap<int2, FixedList128Bytes<int>> edgeToTriangleList;
            [Unity.Collections.WriteOnly]
            public NativeList<int3> newTriangles;

            public NativeParallelHashSet<int4> useQuadSet; // Unity2023.1.5対応
            public NativeParallelHashSet<int3> removeTriangleSet; // Unity2023.1.5対応

            public void Execute()
            {
                // エッジに接続するトライアングルリストを作成する
                for (int i = 0; i < tcnt; i++)
                {
                    int3 tri = triangles[i];
                    int2x3 edges = new int2x3(tri.xy, tri.yz, tri.zx);
                    for (int j = 0; j < 3; j++)
                    {
                        int2 edge = DataUtility.PackInt2(edges[j]);
                        if (edgeToTriangleList.ContainsKey(edge))
                        {
                            var tlist = edgeToTriangleList[edge];
                            tlist.Set(i);
                            edgeToTriangleList[edge] = tlist;
                        }
                        else
                        {
                            var tlist = new FixedList128Bytes<int>();
                            tlist.Set(i);
                            edgeToTriangleList.Add(edge, tlist);
                        }
                    }
                }

                // debug
                //foreach (var kv in edgeToTriangleList)
                //{
                //    Debug.Log($"edge[{kv.Key}]");
                //    var data = kv.Value;
                //    for (int i = 0; i < data.Length; i++)
                //        Debug.Log($":{data[i]}");
                //}

                // ほぼ水平なトライアングルペアを登録していき、ペアが重複した場合は１つをの残して削除対象とする
                //var useQuadSet = new NativeParallelHashSet<int4>(tcnt / 4, Allocator.Temp); // Unity2023.1.5対応
                //var removeTriangleSet = new NativeParallelHashSet<int3>(tcnt / 4, Allocator.Temp); // Unity2023.1.5対応
                foreach (var kv in edgeToTriangleList)
                {
                    int2 edge = kv.Key;
                    var px = localPositions[edge.x];
                    var py = localPositions[edge.y];
                    var tlist = kv.Value;
                    int tcnt = tlist.Length;
                    for (int i = 0; i < tcnt - 1; i++)
                    {
                        int3 tri1 = triangles[tlist[i]];
                        int z = DataUtility.RemainingData(tri1, edge);
                        var pz = localPositions[z];
                        for (int j = i + 1; j < tcnt; j++)
                        {
                            int3 tri2 = triangles[tlist[j]];
                            int w = DataUtility.RemainingData(tri2, edge);
                            var pw = localPositions[w];

                            //        z +
                            //         / \
                            // edge.x +---+ edge.y
                            //         \ /
                            //        w +

                            // トライアングルペアのなす角を求める
                            float ang = math.degrees(MathUtility.TriangleAngle(px, py, pz, pw));
                            //Debug.Log($"[{edge.x},{edge.y},{z},{w}] :{ang}");

                            //if (edge.x == 107 || edge.y == 107 || z == 107 || w == 107)
                            //    Debug.Log($"[{edge.x}-{edge.y},{z},{w}], ang:{ang}");

                            // ほぼ水平が対象（歪な形状も弾かれる）
                            if (math.abs(ang) > Define.System.ProxyMeshTrianglePairAngle)
                                continue;

                            // くの字型のトライアングルペアは無視する
                            MathUtility.ClosestPtSegmentSegment(px, py, pz, pw, out var s, out var t, out _, out _);
                            if (s == 0.0f || s == 1.0f || t == 0.0f || t == 1.0f)
                                continue;

                            // この２つのトライアングルペアの四辺形
                            int4 quad = DataUtility.PackInt4(edge.x, edge.y, z, w);

                            // すでにこの四辺形が登録されているならば２つのトライアングルは削除対象に入れる
                            if (useQuadSet.Contains(quad))
                            {
                                removeTriangleSet.Add(tri1);
                                removeTriangleSet.Add(tri2);
                                //Debug.Log($"removeTriangleSet:{tri1}");
                                //Debug.Log($"removeTriangleSet:{tri2}");
                            }
                            else
                            {
                                useQuadSet.Add(quad);
                            }
                        }
                    }
                }

                // 削除トライアングルを除いた新しいトライアングルリストを作成する
                for (int i = 0; i < tcnt; i++)
                {
                    int3 tri = triangles[i];
                    if (removeTriangleSet.Contains(tri))
                        continue;

                    // 登録
                    newTriangles.AddNoResize(tri);
                }
                //foreach (var tri in removeTriangleSet)
                //{
                //    Debug.Log($"Remove Triangle:{tri}");
                //}
            }
        }

        /// <summary>
        /// 共通するエッジをもつ２つのトライアングルが開いているか判定する
        /// </summary>
        /// <param name="tri1"></param>
        /// <param name="tri2"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        bool CheckTwoTriangleOpen(in int3 tri1, in int3 tri2, in int2 edge, in float3 tri1n)
        {
            var sv0 = DataUtility.RemainingData(tri2, edge);
            var v = math.normalize(localPositions[sv0] - localPositions[edge.x]);
            return math.dot(tri1n, v) <= 0.0f;
        }

        /// <summary>
        /// 共通するエッジをもつ２つのトライアングルのなす角を求める（デグリー角）
        /// </summary>
        /// <param name="tri1"></param>
        /// <param name="tri2"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        float CalcTwoTriangleAngle(in int3 tri1, in int3 tri2, in int2 edge)
        {
            var sv1 = DataUtility.RemainingData(tri1, edge);
            var sv2 = DataUtility.RemainingData(tri2, edge);

            // トライアングル角度
            var va = localPositions[edge.y] - localPositions[edge.x];
            var vb = localPositions[sv1] - localPositions[edge.x];
            var vc = localPositions[sv2] - localPositions[edge.x];

            var n0 = math.cross(va, vb);
            var n1 = math.cross(vc, va);

            return math.degrees(MathUtility.Angle(n0, n1));
        }
    }
}
