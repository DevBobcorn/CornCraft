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
        //=========================================================================================
        /// <summary>
        /// 頂点間の平均/最大距離を調べてる（スレッド可）
        /// 結果はaverageVertexDistance/maxVertexDistanceに格納される
        /// </summary>
        internal void CalcAverageAndMaxVertexDistanceRun()
        {
            try
            {
                if (averageVertexDistance.IsCreated == false)
                    averageVertexDistance = new NativeReference<float>(Allocator.Persistent);
                if (maxVertexDistance.IsCreated == false)
                    maxVertexDistance = new NativeReference<float>(Allocator.Persistent);

                averageVertexDistance.Value = 0;
                maxVertexDistance.Value = 0;
                using var averageCount = new NativeReference<int>(Allocator.TempJob);

                // triangle
                if (TriangleCount > 0)
                {
                    var job = new Work_AverageTriangleDistanceJob()
                    {
                        vcnt = VertexCount,
                        tcnt = TriangleCount,
                        localPositions = localPositions.GetNativeArray(),
                        triangles = triangles.GetNativeArray(),
                        averageVertexDistance = averageVertexDistance,
                        averageCount = averageCount,
                        maxVertexDistance = maxVertexDistance,
                    };
                    job.Run();
                }

                // line
                if (LineCount > 0)
                {
                    var job = new Work_AverageLineDistanceJob()
                    {
                        vcnt = VertexCount,
                        lcnt = LineCount,
                        localPositions = localPositions.GetNativeArray(),
                        lines = lines.GetNativeArray(),
                        averageVertexDistance = averageVertexDistance,
                        averageCount = averageCount,
                        maxVertexDistance = maxVertexDistance,
                    };
                    job.Run();
                }

                // 平均化する
                int cnt = averageCount.Value;
                if (cnt > 0)
                {
                    float sqlen = averageVertexDistance.Value;
                    averageVertexDistance.Value = math.sqrt(sqlen / cnt);
                    maxVertexDistance.Value = math.sqrt(maxVertexDistance.Value);
                    //Debug.Log($"max:{maxVertexDistance.Value}");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                result.SetError(Define.Result.Reduction_CalcAverageException);
            }
        }

        [BurstCompile]
        struct Work_AverageTriangleDistanceJob : IJob
        {
            public int vcnt;
            public int tcnt;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;

            public NativeReference<float> averageVertexDistance;
            public NativeReference<int> averageCount;
            public NativeReference<float> maxVertexDistance;

            public void Execute()
            {
                // 調べるトライアングルは最大100まで
                int step = math.max(tcnt / 100, 1);
                float sumsqlen = 0;
                float maxsqlen = 0;
                int cnt = 0;
                for (int tindex = 0; tindex < tcnt; tindex += step)
                {
                    int3 tri = triangles[tindex];
                    var p0 = localPositions[tri.x];
                    var p1 = localPositions[tri.y];
                    var p2 = localPositions[tri.z];
                    float sqlen0 = math.distancesq(p0, p1);
                    float sqlen1 = math.distancesq(p1, p2);
                    float sqlen2 = math.distancesq(p2, p0);
                    sumsqlen += sqlen0;
                    sumsqlen += sqlen1;
                    sumsqlen += sqlen2;
                    cnt += 3;
                    maxsqlen = math.max(maxsqlen, sqlen0);
                    maxsqlen = math.max(maxsqlen, sqlen1);
                    maxsqlen = math.max(maxsqlen, sqlen2);
                }

                averageVertexDistance.Value = averageVertexDistance.Value + sumsqlen;
                averageCount.Value = averageCount.Value + cnt;

                // 最大
                maxVertexDistance.Value = math.max(maxVertexDistance.Value, maxsqlen);
            }
        }

        [BurstCompile]
        struct Work_AverageLineDistanceJob : IJob
        {
            public int vcnt;
            public int lcnt;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int2> lines;

            public NativeReference<float> averageVertexDistance;
            public NativeReference<int> averageCount;
            public NativeReference<float> maxVertexDistance;

            public void Execute()
            {
                // 調べるラインは最大100まで
                int step = math.max(lcnt / 100, 1);
                float sumsqlen = 0;
                int cnt = 0;
                float maxsqlen = 0;
                for (int lindex = 0; lindex < lcnt; lindex += step)
                {
                    int2 line = lines[lindex];
                    var p0 = localPositions[line.x];
                    var p1 = localPositions[line.y];
                    float sqlen = math.distancesq(p0, p1);
                    sumsqlen += sqlen;
                    cnt++;
                    maxsqlen = math.max(maxsqlen, sqlen);
                }

                averageVertexDistance.Value = averageVertexDistance.Value + sumsqlen;
                averageCount.Value = averageCount.Value + cnt;

                // 最大
                maxVertexDistance.Value = math.max(maxVertexDistance.Value, maxsqlen);
            }
        }

        //=========================================================================================
        /// <summary>
        /// 頂点インデックスを格納したグリッドマップを作成して返す
        /// </summary>
        /// <param name="gridSize"></param>
        /// <returns></returns>
        internal GridMap<int> CreateVertexIndexGridMapRun(float gridSize)
        {
            int vcnt = VertexCount;
            var gridMap = new GridMap<int>(vcnt);

            if (vcnt > 0)
            {
                var addJob = new Work_AddVertexIndexGirdMapJob()
                {
                    gridSize = gridSize,
                    vcnt = vcnt,
                    positins = localPositions.GetNativeArray(),
                    gridMap = gridMap.GetMultiHashMap(),
                };
                addJob.Run();
            }

            return gridMap;
        }

        [BurstCompile]
        struct Work_AddVertexIndexGirdMapJob : IJob
        {
            public float gridSize;
            public int vcnt;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positins;
            [Unity.Collections.WriteOnly]
            public NativeParallelMultiHashMap<int3, int> gridMap;

            public void Execute()
            {
                for (int vindex = 0; vindex < vcnt; vindex++)
                {
                    float3 pos = positins[vindex];
                    GridMap<int>.AddGrid(pos, vindex, gridMap, gridSize);
                }
            }
        }

        //=========================================================================================
        public VirtualMeshRaycastHit IntersectRayMesh(float3 rayPos, float3 rayDir, bool doubleSide, float pointRadius)
        {
            // レイをメッシュローカル空間に変換する
            var t = GetCenterTransform();
            float3 localRayPos = t.InverseTransformPoint(rayPos);
            float3 localRayDir = t.InverseTransformDirection(rayDir);
            float3 rayEndPos = rayPos + rayDir * 1000;
            float3 localRayEndPos = t.InverseTransformPoint(rayEndPos);
            float localPointRadius = pointRadius / t.lossyScale.x;

            // ヒットバッファ
            //int buffSize = (VertexCount + TriangleCount) / 10 + 10;
            int buffSize = 100; // おそらく十分な大きさ
            using var hitList = new NativeList<VirtualMeshRaycastHit>(buffSize, Allocator.TempJob);

            JobHandle jobHandle = default;

            // トライアングル交差判定
            var job1 = new Work_IntersectTriangleJob()
            {
                localRayPos = localRayPos,
                localRayDir = localRayDir,
                localRayEndPos = localRayEndPos,
                doubleSide = doubleSide,

                localPositions = localPositions.GetNativeArray(),
                triangles = triangles.GetNativeArray(),

                hitList = hitList.AsParallelWriter(),
            };
            jobHandle = job1.Schedule(TriangleCount, 16, jobHandle);

            // エッジ交差判定
            var job2 = new Work_IntersectEdgeJob()
            {
                localRayPos = localRayPos,
                localRayDir = localRayDir,
                localRayEndPos = localRayEndPos,
                //rayDir = rayDir,
                rayDir = localRayDir,

                localEdgeRadius = localPointRadius,
                localPositions = localPositions.GetNativeArray(),
                edges = edges,
                edgeToTriangles = edgeToTriangles,

                hitList = hitList.AsParallelWriter(),
            };
            jobHandle = job2.Schedule(EdgeCount, 16, jobHandle);

#if false
            // ポイント交差判定
            var job2 = new Work_IntersectPointJob()
            {
                localRayPos = localRayPos,
                localRayDir = localRayDir,
                rayDir = rayDir,

                localPointRadius = localPointRadius,
                localPositions = localPositions.GetNativeArray(),
                vertexToTriangles = vertexToTriangles,

                hitList = hitList.AsParallelWriter(),
            };
            jobHandle = job2.Schedule(VertexCount, 16, jobHandle);
#endif

            // ソート
            var job3 = new Work_IntersetcSortJob()
            {
                hitList = hitList,
            };
            jobHandle = job3.Schedule(jobHandle);

            // 完了待ち
            jobHandle.Complete();

            // 最も近いヒット情報を返す
            var rayhit = hitList.Length > 0 ? hitList[0] : default;

            //Debug.Log($"IntersectMesh: {rayhit}");

            return rayhit;
        }

        [BurstCompile]
        struct Work_IntersectTriangleJob : IJobParallelFor
        {
            public float3 localRayPos;
            public float3 localRayDir;
            public float3 localRayEndPos;
            public bool doubleSide;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;

            // output
            [Unity.Collections.WriteOnly]
            public NativeList<VirtualMeshRaycastHit>.ParallelWriter hitList;


            public void Execute(int tindex)
            {
                int3 tri = triangles[tindex];
                var pos0 = localPositions[tri.x];
                var pos1 = localPositions[tri.y];
                var pos2 = localPositions[tri.z];

                // トライアングルを包む球
                MathUtility.GetTriangleSphere(pos0, pos1, pos2, out float3 sc, out float sr);

                // 球とレイの衝突判定
                float t = 0;
                float3 q = 0;
                if (MathUtility.IntersectRaySphere(localRayPos, localRayDir, sc, sr, ref t, ref q) == false)
                    return;

                // トライアングルとレイの詳細判定
                if (MathUtility.IntersectSegmentTriangle(localRayPos, localRayEndPos, pos0, pos1, pos2, doubleSide, out _, out _, out _, out t) == false)
                    return;

                // 衝突位置
                float3 hitPos = math.lerp(localRayPos, localRayEndPos, t);

                // トライアングル法線
                float3 tn = MathUtility.TriangleNormal(pos0, pos1, pos2);

                // 格納
                //Debug.Log($"Triangle Hit!. tindex:{tindex}, tri:{tri} hitpos:{hitPos}, tn:{tn}");
                var hit = new VirtualMeshRaycastHit();
                hit.type = VirtualMeshPrimitive.Triangle;
                hit.index = tindex;
                hit.position = hitPos;
                hit.distance = t;
                hit.normal = tn;
                hitList.AddNoResize(hit);
            }
        }

        [BurstCompile]
        struct Work_IntersectEdgeJob : IJobParallelFor
        {
            public float3 localRayPos;
            public float3 localRayDir;
            public float3 localRayEndPos;
            public float3 rayDir;

            public float localEdgeRadius;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int2> edges;
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<int2, ushort> edgeToTriangles;

            // output
            [Unity.Collections.WriteOnly]
            public NativeList<VirtualMeshRaycastHit>.ParallelWriter hitList;

            public void Execute(int eindex)
            {
                int2 edge = edges[eindex];

                // エッジがトライアングルに属する場合は無効
                if (edgeToTriangles.ContainsKey(edge))
                    return;

                var pos0 = localPositions[edge.x];
                var pos1 = localPositions[edge.y];

                // ２つの直線の衝突判定
                float distSq = MathUtility.ClosestPtSegmentSegment(pos0, pos1, localRayPos, localRayEndPos, out float s, out float t, out float3 c1, out float3 c2);
                float dist = math.sqrt(distSq);
                if (dist > localEdgeRadius)
                    return;

                // 衝突位置
                float3 hitPos = c2;

                // 格納
                var hit = new VirtualMeshRaycastHit();
                hit.type = VirtualMeshPrimitive.Edge;
                hit.index = eindex;
                hit.position = hitPos;
                hit.distance = t;
                hit.normal = -rayDir;
                hitList.AddNoResize(hit);
            }
        }

        [BurstCompile]
        struct Work_IntersectPointJob : IJobParallelFor
        {
            public float3 localRayPos;
            public float3 localRayDir;
            public float3 rayDir;

            public float localPointRadius;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<FixedList32Bytes<int>> vertexToTriangles;

            // output
            [Unity.Collections.WriteOnly]
            public NativeList<VirtualMeshRaycastHit>.ParallelWriter hitList;

            public void Execute(int vindex)
            {
                // 頂点がトライアングルに接続されている場合は無効
                if (vertexToTriangles[vindex].Length > 0)
                    return;

                var pos = localPositions[vindex];

                // 球とレイの衝突判定
                float t = 0;
                float3 q = 0;
                if (MathUtility.IntersectRaySphere(localRayPos, localRayDir, pos, localPointRadius, ref t, ref q) == false)
                    return;

                // 格納
                var hit = new VirtualMeshRaycastHit();
                hit.type = VirtualMeshPrimitive.Point;
                hit.index = vindex;
                hit.position = pos;
                hit.distance = t;
                hit.normal = -rayDir;
                hitList.AddNoResize(hit);
            }
        }

        [BurstCompile]
        struct Work_IntersetcSortJob : IJob
        {
            public NativeList<VirtualMeshRaycastHit> hitList;

            public void Execute()
            {
                if (hitList.Length > 1)
                    hitList.Sort();
            }
        }
    }
}
