// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaReductionMesh
{
    /// <summary>
    /// 仮想メッシュデータ
    /// </summary>
    public class MeshData : ReductionMeshAccess
    {
        /// <summary>
        /// ボーンウエイト情報
        /// </summary>
        public class WeightData
        {
            /// <summary>
            /// ボーンインデックスはオリジナルのインデックスではなく boneList のインデックス
            /// </summary>
            public int boneIndex;
            public float boneWeight;
        }

        /// <summary>
        /// オリジナル頂点情報
        /// </summary>
        public class Vertex
        {
            public int meshIndex;
            public int vertexIndex;
            public Vector3 wpos;
            public Vector3 wnor;
            public Vector3 wtan;
            public float tanw;
            public Vector2 uv;

            /// <summary>
            /// 接続する親頂点インデックス
            /// </summary>
            public int parentIndex;

            /// <summary>
            /// ボーンウエイトリスト
            /// </summary>
            public List<WeightData> boneWeightList = new List<WeightData>();
        }

        /// <summary>
        /// オリジナル頂点リスト（このリストはリダクションにより減少しない）
        /// </summary>
        public List<Vertex> originalVertexList = new List<Vertex>();

        /// <summary>
        /// オリジナルメッシュ情報
        /// </summary>
        public class MeshInfo
        {
            public int index;
            public Mesh mesh;
            public int vertexCount;
            public List<Vertex> vertexList = new List<Vertex>();
        }

        /// <summary>
        /// オリジナルメッシュリスト
        /// </summary>
        public List<MeshInfo> meshInfoList = new List<MeshInfo>();


        /// <summary>
        /// 共有頂点情報
        /// </summary>
        public class ShareVertex
        {
            public int sindex;
            public Vector3 wpos;
            public Vector3 wnor;
            public Vector3 wtan;
            public float tanw;
            public Vector2 uv;

            public Matrix4x4 worldToLocalMatrix;
            public Matrix4x4 bindpose;

            /// <summary>
            /// ボーンウエイトリスト
            /// </summary>
            public List<WeightData> boneWeightList = new List<WeightData>();

            /// <summary>
            /// オリジナル頂点リスト
            /// </summary>
            public List<Vertex> vertexList = new List<Vertex>();

            /// <summary>
            /// 接続するマージメッシュ頂点セット
            /// </summary>
            public HashSet<ShareVertex> linkShareVertexSet = new HashSet<ShareVertex>();

            /// <summary>
            /// 接続するトライアングルセット
            /// </summary>
            public HashSet<Triangle> linkTriangleSet = new HashSet<Triangle>();

            public void AddLink(ShareVertex mv)
            {
                linkShareVertexSet.Add(mv);
            }

            public void ReplaseLink(ShareVertex old, ShareVertex mv)
            {
                if (linkShareVertexSet.Contains(old))
                {
                    linkShareVertexSet.Remove(old);
                    linkShareVertexSet.Add(mv);
                }
            }

            /// <summary>
            /// 座標再計算
            /// </summary>
            public void RecalcCoordinate()
            {
                int cnt = 0;
                wpos = Vector3.zero;
                wnor = Vector3.zero;
                wtan = Vector3.zero;
                uv = Vector2.zero;

                foreach (var vt in vertexList)
                {
                    wpos += vt.wpos;
                    cnt++;
                }

                if (cnt >= 1)
                {
                    // 座標は平均
                    wpos = wpos / cnt;

                    // 法線接線UVは頂点[0]を使用する
                    wnor = vertexList[0].wnor;
                    wtan = vertexList[0].wtan;
                    uv = vertexList[0].uv;
                }

                Debug.Assert(wnor.magnitude >= 0.0001f);
            }

            /// <summary>
            /// 接続するトライアングルから法線／接線を再計算する
            /// </summary>
            public void CalcNormalTangentFromTriangle()
            {
                // ラインではlinkTriangleSet数が０となる
                if (linkTriangleSet.Count > 0)
                {
                    wnor = Vector3.zero;
                    wtan = Vector3.zero;

                    foreach (var tri in linkTriangleSet)
                    {
                        wnor += tri.wnor;
                        wtan += tri.wtan;
                    }

                    wnor.Normalize();
                    wtan.Normalize();

                    //Debug.Assert(wnor.magnitude > 0.001f);
                    //if (wnor.magnitude < 0.001f)
                    if (wnor.sqrMagnitude == 0.0f)
                        Debug.LogAssertion("Calc triangle normal = 0!");
                }
            }

#if false
            /// <summary>
            /// 頂点に接続するトライアングルを最大maxTriangleでカットする
            /// </summary>
            /// <param name="maxTriangle"></param>
            public void ReductionLinkTriangle(int maxTriangle)
            {
                if (linkTriangleSet.Count <= maxTriangle)
                    return;

                // 現在の法線との内積が大きい順でソートする
                var tlist = new List<Triangle>(linkTriangleSet);
                tlist.Sort((a, b) => Vector3.Dot(wnor, a.wnor) > Vector3.Dot(wnor, b.wnor) ? -1 : 1);

                // 最大maxTriangleでカット
                if (tlist.Count > maxTriangle)
                {
                    tlist.RemoveRange(maxTriangle, tlist.Count - maxTriangle);
                }

                // 再設定
                linkTriangleSet = new HashSet<Triangle>(tlist);

                // 法線接線を再計算
                CalcNormalTangentFromTriangle();
            }
#endif

            /// <summary>
            /// ローカル座標を求める
            /// </summary>
            /// <param name="pos"></param>
            /// <returns></returns>
            public Vector3 CalcLocalPos(Vector3 pos)
            {
                Quaternion q = Quaternion.LookRotation(wnor, wtan);
                var iq = Quaternion.Inverse(q);
                var v = pos - wpos;
                return iq * v;
            }

            /// <summary>
            /// ローカル方向を求める
            /// </summary>
            /// <param name="dir"></param>
            /// <returns></returns>
            public Vector3 CalcLocalDir(Vector3 dir)
            {
                Quaternion q = Quaternion.LookRotation(wnor, wtan);
                var iq = Quaternion.Inverse(q);
                return iq * dir;
            }

            /// <summary>
            /// ローカル変換マトリックスを求める
            /// </summary>
            /// <returns></returns>
            public Matrix4x4 CalcWorldToLocalMatrix()
            {
                Quaternion q = Quaternion.LookRotation(wnor, wtan);
                var mat = Matrix4x4.TRS(wpos, q, Vector3.one);
                worldToLocalMatrix = mat.inverse;
                return worldToLocalMatrix;
            }

            /// <summary>
            /// ウエイトを頂点リストから再計算する
            /// </summary>
            public void CalcBoneWeight(ReductionMesh.ReductionWeightMode weightMode, float weightPow)
            {
                switch (weightMode)
                {
                    case ReductionMesh.ReductionWeightMode.Distance:
                        CalcBoneWeight_Distance(weightPow);
                        break;
                    case ReductionMesh.ReductionWeightMode.Average:
                        CalcBoneWeight_Average();
                        break;
                    case ReductionMesh.ReductionWeightMode.DistanceAverage:
                        CalcBoneWeight_DistanceAverage(weightPow);
                        break;
                }
            }

            /// <summary>
            /// 共有頂点に属する頂点の平均ウエイトで計算する方法
            /// </summary>
            /// <param name="weightPow"></param>
            private void CalcBoneWeight_DistanceAverage(float weightPow)
            {
                // 最大距離
                float maxlen = 0;
                float min = 0.001f;
                Vertex minVertex = null;
                foreach (var vtx in vertexList)
                {
                    var dist = Vector3.Distance(wpos, vtx.wpos);
                    if (dist < min)
                    {
                        minVertex = vtx;
                        min = dist;
                    }
                    maxlen = Mathf.Max(maxlen, dist);
                }

                // 使用ボーンとウエイトを収集
                var sumlist = new List<WeightData>();
                if (minVertex == null)
                {
                    foreach (var vtx in vertexList)
                    {
                        // 距離比重
                        float ratio = 1;
                        if (maxlen > 1e-06f)
                        {
                            ratio = Mathf.Clamp01(1.0f - Vector3.Distance(wpos, vtx.wpos) / (maxlen * 2));
                            ratio = Mathf.Pow(ratio, weightPow);
                        }

                        foreach (var w in vtx.boneWeightList)
                        {
                            var wd = sumlist.Find(wdata => wdata.boneIndex == w.boneIndex);
                            if (wd == null)
                            {
                                wd = new WeightData();
                                wd.boneIndex = w.boneIndex;
                                sumlist.Add(wd);
                            }
                            wd.boneWeight = wd.boneWeight + w.boneWeight * ratio; // 距離比重を乗算する
                        }
                    }
                }
                else
                {
                    // ウエイトは最寄りの頂点情報をコピーする
                    //Debug.Log("ウエイト継承" + sindex);
                    foreach (var w in minVertex.boneWeightList)
                    {
                        var wd = new WeightData();
                        wd.boneIndex = w.boneIndex;
                        wd.boneWeight = w.boneWeight;
                        sumlist.Add(wd);
                    }
                }

                if (sumlist.Count > 1)
                {
                    // ウエイトでソート（降順）
                    sumlist.Sort((a, b) => a.boneWeight - b.boneWeight > 0 ? -1 : 1);

                    // 最大４で切り捨て
                    if (sumlist.Count > 4)
                    {
                        sumlist.RemoveRange(4, sumlist.Count - 4);
                    }

                    // ウエイトを合計１に調整
                    AdjustWeight(sumlist);

                    // ウエイトがしきい値以下のものを削除する
                    for (int i = 0; i < sumlist.Count;)
                    {
                        var wd = sumlist[i];
                        if (wd.boneWeight < 0.01f)
                        {
                            //Debug.Log("del weight:" + wd.boneWeight);
                            sumlist.RemoveAt(i);
                            continue;
                        }
                        i++;
                    }

                }

                // ウエイトを合計１に調整
                AdjustWeight(sumlist);

                // 最終値として格納
                boneWeightList = sumlist;
            }

            /// <summary>
            /// 共有頂点に属する頂点の平均ウエイトで計算する方法（新しい実装）
            /// こちらのほうが断然結果が良い！
            /// </summary>
            private void CalcBoneWeight_Average()
            {
                var sumlist = new List<WeightData>();

                foreach (var vtx in vertexList)
                {
                    foreach (var w in vtx.boneWeightList)
                    {
                        var wd = sumlist.Find(wdata => wdata.boneIndex == w.boneIndex);
                        if (wd == null)
                        {
                            wd = new WeightData();
                            wd.boneIndex = w.boneIndex;
                            sumlist.Add(wd);
                        }
                        wd.boneWeight = wd.boneWeight + w.boneWeight;
                    }
                }

                // ウエイトでソート（降順）
                sumlist.Sort((a, b) => a.boneWeight - b.boneWeight > 0 ? -1 : 1);

                // 最大４で切り捨て
                if (sumlist.Count > 4)
                {
                    sumlist.RemoveRange(4, sumlist.Count - 4);
                }

                // ウエイトを合計１に調整
                AdjustWeight(sumlist);

                // ウエイトがしきい値以下のものを削除する
                for (int i = 0; i < sumlist.Count;)
                {
                    var wd = sumlist[i];
                    if (wd.boneWeight < 0.01f) // 1%
                    {
                        //Debug.Log("del weight:" + wd.boneWeight);
                        sumlist.RemoveAt(i);
                        continue;
                    }
                    i++;
                }

                // ウエイトを合計１に調整
                AdjustWeight(sumlist);

                // 最終値として格納
                boneWeightList = sumlist;

            }

            /// <summary>
            /// ウエイトを合計１に調整する
            /// </summary>
            /// <param name="sumlist"></param>
            private void AdjustWeight(List<WeightData> sumlist)
            {
                float total = 0;
                foreach (var wd in sumlist)
                {
                    total += wd.boneWeight;
                }
                float scl = 1.0f / total;
                foreach (var wd in sumlist)
                {
                    wd.boneWeight *= scl;
                }
            }


            /// <summary>
            /// 共有頂点からの距離によりウエイトを計算する方式（リリース時の実装）
            /// </summary>
            /// <param name="weightPow"></param>
            private void CalcBoneWeight_Distance(float weightPow)
            {
                // 最大距離
                float maxlen = 0;
                foreach (var vtx in vertexList)
                {
                    var dist = Vector3.Distance(wpos, vtx.wpos);
                    maxlen = Mathf.Max(maxlen, dist);
                }

                // 最大距離からの係数(t)を元のウエイトに乗算して集計する
                // 同じボーンウエイトは結合する
                var sumlist = new List<WeightData>();
                foreach (var vtx in vertexList)
                {
                    float t = 1.0f;
                    if (maxlen > 0.0f)
                    {
                        var dist = Vector3.Distance(wpos, vtx.wpos);
                        t = Mathf.Clamp01((1.0f - dist / maxlen) + 0.001f);
                        t = Mathf.Pow(t, weightPow); // 3 ?
                    }

                    foreach (var w in vtx.boneWeightList)
                    {
                        var wd = sumlist.Find(wdata => wdata.boneIndex == w.boneIndex);
                        if (wd == null)
                        {
                            wd = new WeightData();
                            wd.boneIndex = w.boneIndex;
                            sumlist.Add(wd);
                        }
                        wd.boneWeight = Mathf.Clamp01(wd.boneWeight + w.boneWeight * t);
                    }
                }

                // ウエイトでソート（降順）
                sumlist.Sort((a, b) => a.boneWeight - b.boneWeight > 0 ? -1 : 1);

                // 最大４で切り捨て
                if (sumlist.Count > 4)
                {
                    sumlist.RemoveRange(4, sumlist.Count - 4);
                }

                // ウエイトを合計１に調整
                AdjustWeight(sumlist);
                //float total = 0;
                //foreach (var wd in sumlist)
                //{
                //    total += wd.boneWeight;
                //}
                //float scl = 1.0f / total;
                //foreach (var wd in sumlist)
                //{
                //    wd.boneWeight *= scl;
                //}

                // 最終値として格納
                boneWeightList = sumlist;
            }

            /// <summary>
            /// ウエイトデータをBoneWeightにして返す
            /// </summary>
            /// <returns></returns>
            public BoneWeight GetBoneWeight()
            {
                var bw = new BoneWeight();

                for (int i = 0; i < boneWeightList.Count; i++)
                {
                    var w = boneWeightList[i];
                    if (i == 0)
                    {
                        bw.boneIndex0 = w.boneIndex;
                        bw.weight0 = w.boneWeight;
                    }
                    if (i == 1)
                    {
                        bw.boneIndex1 = w.boneIndex;
                        bw.weight1 = w.boneWeight;
                    }
                    if (i == 2)
                    {
                        bw.boneIndex2 = w.boneIndex;
                        bw.weight2 = w.boneWeight;
                    }
                    if (i == 3)
                    {
                        bw.boneIndex3 = w.boneIndex;
                        bw.weight3 = w.boneWeight;
                    }
                }

                return bw;
            }
        }

        /// <summary>
        /// 共有頂点リスト
        /// </summary>
        public List<ShareVertex> shareVertexList = new List<ShareVertex>();

        /// <summary>
        /// トライアングル
        /// </summary>
        public class Triangle
        {
            public int tindex;

            public List<ShareVertex> shareVertexList = new List<ShareVertex>();

            /// <summary>
            /// 面法線
            /// </summary>
            public Vector3 wnor;

            /// <summary>
            /// 面接線
            /// </summary>
            public Vector3 wtan;

            /// <summary>
            /// 反転禁止フラグ
            /// </summary>
            public bool flipLock;

            /// <summary>
            /// トライアングルを構成するエッジを返す
            /// </summary>
            /// <param name="edge0"></param>
            /// <param name="edge1"></param>
            /// <param name="edge2"></param>
            public void GetEdge(out uint edge0, out uint edge1, out uint edge2)
            {
                edge0 = Utility.PackPair(shareVertexList[0].sindex, shareVertexList[1].sindex);
                edge1 = Utility.PackPair(shareVertexList[1].sindex, shareVertexList[2].sindex);
                edge2 = Utility.PackPair(shareVertexList[2].sindex, shareVertexList[0].sindex);
            }

            /// <summary>
            /// 面法線を求めて返す
            /// </summary>
            /// <returns></returns>
            public Vector3 CalcTriangleNormal()
            {
                var v0 = shareVertexList[1].wpos - shareVertexList[0].wpos;
                var v1 = shareVertexList[2].wpos - shareVertexList[0].wpos;
                // アンダーフロー防止のため倍数を掛ける
                v0 *= 1000;
                v1 *= 1000;
                wnor = Vector3.Cross(v0, v1).normalized;
                if (wnor.magnitude <= 0.001f)
                {
                    Debug.LogError($"CalcTriangleNormal Invalid! ({shareVertexList[0].sindex},{shareVertexList[1].sindex},{shareVertexList[2].sindex})");
                }
                //Debug.Assert(wnor.magnitude > 0.001f);

                return wnor;
            }

            /// <summary>
            /// 回転方向（面法線）を逆にする
            /// </summary>
            public void Flip()
            {
                var w = shareVertexList[1];
                shareVertexList[1] = shareVertexList[2];
                shareVertexList[2] = w;
                wnor = -wnor;
            }

            /// <summary>
            /// 面接線を求める
            /// </summary>
            /// <returns></returns>
            public Vector3 CalcTriangleTangent()
            {
                // 接線(頂点座標とUVから接線を求める一般的なアルゴリズム)
                var v1 = shareVertexList[0].wpos;
                var v2 = shareVertexList[1].wpos;
                var v3 = shareVertexList[2].wpos;
                var w1 = shareVertexList[0].uv;
                var w2 = shareVertexList[1].uv;
                var w3 = shareVertexList[2].uv;
                Vector3 distBA = v2 - v1;
                Vector3 distCA = v3 - v1;
                Vector2 tdistBA = w2 - w1;
                Vector2 tdistCA = w3 - w1;
                float area = tdistBA.x * tdistCA.y - tdistBA.y * tdistCA.x;
                Vector3 tan = Vector3.zero;
                if (area == 0.0f)
                {
                    // error
                    Debug.LogError("Calc tangent area = 0!");
                }
                else
                {
                    float delta = 1.0f / area;
                    tan = new Vector3(
                        (distBA.x * tdistCA.y) + (distCA.x * -tdistBA.y),
                        (distBA.y * tdistCA.y) + (distCA.y * -tdistBA.y),
                        (distBA.z * tdistCA.y) + (distCA.z * -tdistBA.y)
                        ) * delta;
                    // 左手座標系に合わせる
                    tan = -tan;
                }
                wtan = tan;
                return wtan;
            }

            /// <summary>
            /// 指定エッジからの残り１つの頂点情報を返す
            /// </summary>
            /// <param name="edge"></param>
            /// <returns></returns>
            public ShareVertex GetNonEdgeVertex(int edgev0, int edgev1)
            {
                return shareVertexList.Find(sv => sv.sindex != edgev0 && sv.sindex != edgev1);
            }

            public ulong GetTriangleHash()
            {
                return Utility.PackTriple(shareVertexList[0].sindex, shareVertexList[1].sindex, shareVertexList[2].sindex);
            }

#if false
            /// <summary>
            /// 指定エッジの順番に対してポリゴンの方向(1/-1)を返す
            /// </summary>
            /// <param name="edge0"></param>
            /// <param name="edge1"></param>
            /// <returns></returns>
            public int CheckDirection(int edge0, int edge1)
            {
                //int index0 = shareVertexList.FindIndex(sv => sv.sindex == edge0);
                //int index1 = shareVertexList.FindIndex(sv => sv.sindex == edge1);
                //Debug.Assert(index0 >= 0 && index1 >= 0);
                //if (index0 < 0 || index1 < 0)
                //    return 0;

                //index0 += 3;
                //index1 += 3;
                //return index1 > index0 ? 1 : -1;

                int index2 = shareVertexList.FindIndex(sv => sv.sindex != edge0 && sv.sindex != edge1);
                Debug.Assert(index2 >= 0);
                int next = (index2 + 1) % 3;

                return shareVertexList[next].sindex == edge0 ? 1 : -1;
            }
#endif
            /// <summary>
            /// トライアングルの面積を求めて返す
            /// </summary>
            /// <param name="sv0"></param>
            /// <param name="sv1"></param>
            /// <param name="sv2"></param>
            /// <returns></returns>
            public static float GetTriangleArea(ShareVertex sv0, ShareVertex sv1, ShareVertex sv2)
            {
                float area = Vector3.Cross(sv1.wpos - sv0.wpos, sv2.wpos - sv0.wpos).magnitude;
                return area;
            }


            public override string ToString()
            {
                return $"<{tindex}>({shareVertexList[0].sindex},{shareVertexList[1].sindex},{shareVertexList[2].sindex})";
            }
        }
        Dictionary<ulong, Triangle> triangleDict = new Dictionary<ulong, Triangle>();

        /// <summary>
        /// ライン
        /// </summary>
        private class Line
        {
            public List<ShareVertex> shareVertexList = new List<ShareVertex>();
        }
        Dictionary<uint, Line> lineDict = new Dictionary<uint, Line>();

        /// <summary>
        /// ボーンリスト
        /// </summary>
        public List<Transform> boneList = new List<Transform>();

        /// <summary>
        ///  UV算出モード
        /// </summary>
        public enum UvWrapMode
        {
            None,
            Sphere,
        }

        /// <summary>
        /// トライアングル２つによる四辺形
        /// </summary>
        public class Square
        {
            public ulong shash;

            public List<Triangle> triangleList = new List<Triangle>();

            // なす角（デグリー）
            public float angle;

            public override string ToString()
            {
                return $"[{shash}] {triangleList[0]} - {triangleList[1]} ang:{angle}";
            }
        }

        /// <summary>
        /// テトラ
        /// </summary>
        public class Tetra
        {
            public List<ShareVertex> shareVertexList = new List<ShareVertex>();

            // 外接円
            public Vector3 circumCenter;
            public float circumRadius;

            // 重心と重心からの最大距離
            public Vector3 tetraCenter;
            public float tetraSize;

            public Tetra()
            {
            }

            public Tetra(ShareVertex a, ShareVertex b, ShareVertex c, ShareVertex d)
            {
                shareVertexList.Add(a);
                shareVertexList.Add(b);
                shareVertexList.Add(c);
                shareVertexList.Add(d);

                //CalcCircumcircle();
                CalcSize();
            }

            public ulong GetTetraHash()
            {
                return Utility.PackQuater(shareVertexList[0].sindex, shareVertexList[1].sindex, shareVertexList[2].sindex, shareVertexList[3].sindex);
            }

            /// <summary>
            /// テトラの外接円と半径を求める
            /// https://qiita.com/kkttm530/items/d32bad84a6a7f0d8d7e7
            /// からだけどdeterminantの計算は間違ってるっぽいのでmathの関数を使用する
            /// </summary>
            public void CalcCircumcircle()
            {
                var p1 = shareVertexList[0].wpos;
                var p2 = shareVertexList[1].wpos;
                var p3 = shareVertexList[2].wpos;
                var p4 = shareVertexList[3].wpos;

                float4x4 a = new float4x4(
                    new float4(p1.x, p1.y, p1.z, 1),
                    new float4(p2.x, p2.y, p2.z, 1),
                    new float4(p3.x, p3.y, p3.z, 1),
                    new float4(p4.x, p4.y, p4.z, 1)
                    );

                float s0 = Mathf.Pow(p1.x, 2.0f) + Mathf.Pow(p1.y, 2.0f) + Mathf.Pow(p1.z, 2.0f);
                float s1 = Mathf.Pow(p2.x, 2.0f) + Mathf.Pow(p2.y, 2.0f) + Mathf.Pow(p2.z, 2.0f);
                float s2 = Mathf.Pow(p3.x, 2.0f) + Mathf.Pow(p3.y, 2.0f) + Mathf.Pow(p3.z, 2.0f);
                float s3 = Mathf.Pow(p4.x, 2.0f) + Mathf.Pow(p4.y, 2.0f) + Mathf.Pow(p4.z, 2.0f);

                float4x4 dx = new float4x4(
                    new float4(s0, p1.y, p1.z, 1),
                    new float4(s1, p2.y, p2.z, 1),
                    new float4(s2, p3.y, p3.z, 1),
                    new float4(s3, p4.y, p4.z, 1)
                    );
                float4x4 dy = new float4x4(
                    new float4(s0, p1.x, p1.z, 1),
                    new float4(s1, p2.x, p2.z, 1),
                    new float4(s2, p3.x, p3.z, 1),
                    new float4(s3, p4.x, p4.z, 1)
                    );

                float4x4 dz = new float4x4(
                    new float4(s0, p1.x, p1.y, 1),
                    new float4(s1, p2.x, p2.y, 1),
                    new float4(s2, p3.x, p3.y, 1),
                    new float4(s3, p4.x, p4.y, 1)
                    );

                float4x4 c = new float4x4(
                    new float4(s0, p1.x, p1.y, p1.z),
                    new float4(s1, p2.x, p2.y, p2.z),
                    new float4(s2, p3.x, p3.y, p3.z),
                    new float4(s3, p4.x, p4.y, p4.z)
                    );

                float a0 = math.determinant(a);
                float dx0 = math.determinant(dx);
                float dy0 = -math.determinant(dy);
                float dz0 = math.determinant(dz);
                float c0 = math.determinant(c);

                circumCenter = new Vector3(dx0 / (2 * a0), dy0 / (2 * a0), dz0 / (2 * a0));
                circumRadius = Mathf.Sqrt(dx0 * dx0 + dy0 * dy0 + dz0 * dz0 - 4.0f * a0 * c0) / (2.0f * Mathf.Abs(a0));
            }

            public bool IntersectCircumcircle(Vector3 pos)
            {
                return Vector3.Distance(pos, circumCenter) <= circumRadius;
            }

            public bool CheckSame(Tetra tri)
            {
                return circumCenter == tri.circumCenter && circumRadius == tri.circumRadius;
            }

            public bool ContainsPoint(ShareVertex p1)
            {
                return shareVertexList.Contains(p1);
            }

            public bool ContainsPoint(ShareVertex p1, ShareVertex p2, ShareVertex p3, ShareVertex p4)
            {
                return shareVertexList.Contains(p1) || shareVertexList.Contains(p2) || shareVertexList.Contains(p3) || shareVertexList.Contains(p4);
            }

            /// <summary>
            /// 重心と重心からの最大距離を計算する
            /// </summary>
            public void CalcSize()
            {
                var wpos0 = shareVertexList[0].wpos;
                var wpos1 = shareVertexList[1].wpos;
                var wpos2 = shareVertexList[2].wpos;
                var wpos3 = shareVertexList[3].wpos;
                tetraCenter = (wpos0 + wpos1 + wpos2 + wpos3) / 4.0f;

                float len0 = Vector3.Distance(wpos0, tetraCenter);
                float len1 = Vector3.Distance(wpos1, tetraCenter);
                float len2 = Vector3.Distance(wpos2, tetraCenter);
                float len3 = Vector3.Distance(wpos3, tetraCenter);

                tetraSize = Mathf.Max(Mathf.Max(len0, len1), Mathf.Max(len2, len3));
            }

            /// <summary>
            /// テトラの検証
            /// </summary>
            /// <returns></returns>
            public bool Verification()
            {
                // あまりに平坦なものは弾く
                var wpos0 = shareVertexList[0].wpos;
                var wpos1 = shareVertexList[1].wpos;
                var wpos2 = shareVertexList[2].wpos;
                var wpos3 = shareVertexList[3].wpos;
                var n = Vector3.Cross(wpos0 - wpos1, wpos0 - wpos2);
                if (n.magnitude < 0.00001f)
                    return false;
                n.Normalize();
                var v = wpos3 - wpos0;
                var h = Vector3.Dot(n, v);
                //if (Mathf.Abs(h) < 0.001f)
                if (Mathf.Abs(h) < (tetraSize * 0.2f))
                    return false;

                return true;
            }
        }
        private List<Tetra> tetraList = new List<Tetra>();

        //=========================================================================================
        /// <summary>
        /// 頂点ウエイト距離乗数
        /// </summary>
        private float weightPow = 1.5f;

        /// <summary>
        /// 頂点の最大ウエイト数
        /// </summary>
        private int maxWeightCount = 4;

        /// <summary>
        /// レイヤー構築時に同一とみなす面角度
        /// </summary>
        private float sameSurfaceAngle = 80.0f;

        /// <summary>
        /// 同一四辺形（トリアングルペア）を除去する
        /// </summary>
        private bool removeSameTrianglePair = true;

        /// <summary>
        /// 四辺形（トリアングルペア）を同一とみなす角度
        /// </summary>
        private float removeSameTrianglePairAngle = 10.0f;

        //=========================================================================================
        /// <summary>
        /// 頂点数
        /// </summary>
        public int VertexCount
        {
            get
            {
                return shareVertexList.Count;
            }
        }

        /// <summary>
        /// ライン数
        /// </summary>
        public int LineCount
        {
            get
            {
                return lineDict.Count;
            }
        }

        /// <summary>
        /// トライアングル数
        /// </summary>
        public int TriangleCount
        {
            get
            {
                return triangleDict.Count;
            }
        }

        /// <summary>
        /// テトラ数
        /// </summary>
        public int TetraCount
        {
            get
            {
                return tetraList.Count;
            }
        }

        /// <summary>
        /// メッシュ数
        /// </summary>
        public int MeshCount
        {
            get
            {
                return meshInfoList.Count;
            }
        }

        /// <summary>
        /// ウエイト距離乗数
        /// </summary>
        public float WeightPow
        {
            get
            {
                return weightPow;
            }
            set
            {
                weightPow = value;
            }
        }

        /// <summary>
        /// 最大ウエイト数
        /// </summary>
        public int MaxWeightCount
        {
            get
            {
                return maxWeightCount;
            }
            set
            {
                maxWeightCount = value;
            }
        }

        /// <summary>
        /// レイヤー構築時に同一とみなす面角度
        /// </summary>
        public float SameSurfaceAngle
        {
            get
            {
                return sameSurfaceAngle;
            }
            set
            {
                sameSurfaceAngle = value;
            }
        }

        /// <summary>
        /// 同一四辺形（トリアングルペア）を除去するフラグ
        /// </summary>
        public bool RemoveSameTrianglePair
        {
            get
            {
                return removeSameTrianglePair;
            }
            set
            {
                removeSameTrianglePair = value;
            }
        }

        /// <summary>
        /// 四辺形（トリアングルペア）を同一とみなす角度
        /// </summary>
        public float RemoveSameTrianglePairAngle
        {
            get
            {
                return removeSameTrianglePairAngle;
            }
            set
            {
                removeSameTrianglePairAngle = value;
            }
        }

        //=========================================================================================
        /// <summary>
        /// メッシュを追加する
        /// 登録したメッシュインデックスを返す
        /// </summary>
        /// <param name="isSkinning"></param>
        /// <param name="mesh"></param>
        /// <param name="bones"></param>
        public int AddMesh(bool isSkinning, Mesh mesh, List<Transform> bones, Matrix4x4[] bindPoseList, BoneWeight[] boneWeightList)
        {
            Debug.Assert(mesh);

            // メッシュ情報
            int mindex = meshInfoList.Count();
            var minfo = new MeshInfo();
            minfo.index = mindex;
            minfo.mesh = mesh;
            minfo.vertexCount = mesh.vertexCount;
            meshInfoList.Add(minfo);

            // メッシュのワールド姿勢取得
            List<Vector3> wposList;
            List<Vector3> wnorList;
            List<Vector4> wtanList;
            CalcMeshWorldPositionNormalTangent(isSkinning, mesh, bones, bindPoseList, boneWeightList, out wposList, out wnorList, out wtanList);

            bool hasNormal = wnorList.Count > 0;
            bool hasTangent = wtanList.Count > 0;

            // ボーン登録
            List<int> boneIndexList = new List<int>();
            if (bones != null)
            {
                foreach (var bone in bones)
                {
                    int bindex = boneList.IndexOf(bone);
                    if (bindex < 0)
                    {
                        boneList.Add(bone);
                        bindex = boneList.Count - 1;
                    }
                    boneIndexList.Add(bindex);
                }
            }

            // UV
            var uvs = mesh.uv;
            bool hasUv = uvs != null && uvs.Length == wposList.Count;

            // 頂点登録
            int start = shareVertexList.Count;
            for (int i = 0; i < wposList.Count; i++)
            {
                var vtx = new Vertex();
                vtx.meshIndex = mindex;
                vtx.vertexIndex = i;
                vtx.wpos = wposList[i];
                if (hasNormal)
                    vtx.wnor = wnorList[i];
                if (hasTangent)
                {
                    vtx.wtan = wtanList[i];
                    vtx.tanw = wtanList[i].w;
                }
                if (hasUv)
                    vtx.uv = uvs[i];
                originalVertexList.Add(vtx);

                minfo.vertexList.Add(vtx);

                if (isSkinning)
                {
                    var bw = boneWeightList[i];
                    if (bw.weight0 > 0.0f)
                    {
                        var w = new WeightData()
                        {
                            boneIndex = boneIndexList[bw.boneIndex0],
                            boneWeight = bw.weight0
                        };
                        vtx.boneWeightList.Add(w);
                    }
                    if (bw.weight1 > 0.0f)
                    {
                        var w = new WeightData()
                        {
                            boneIndex = boneIndexList[bw.boneIndex1],
                            boneWeight = bw.weight1
                        };
                        vtx.boneWeightList.Add(w);
                    }
                    if (bw.weight2 > 0.0f)
                    {
                        var w = new WeightData()
                        {
                            boneIndex = boneIndexList[bw.boneIndex2],
                            boneWeight = bw.weight2
                        };
                        vtx.boneWeightList.Add(w);
                    }
                    if (bw.weight3 > 0.0f)
                    {
                        var w = new WeightData()
                        {
                            boneIndex = boneIndexList[bw.boneIndex3],
                            boneWeight = bw.weight3
                        };
                        vtx.boneWeightList.Add(w);
                    }
                }
                else
                {
                    var w = new WeightData()
                    {
                        boneIndex = 0,
                        boneWeight = 1
                    };
                    vtx.boneWeightList.Add(w);
                }

                // 共有頂点登録
                var svtx = new ShareVertex();
                svtx.wpos = vtx.wpos;
                svtx.wnor = vtx.wnor;
                svtx.wtan = vtx.wtan;
                //svtx.tanw = vtx.tanw;
                svtx.tanw = -1.0f; // 接線空間は(-1)DirectX系で統一する
                svtx.uv = vtx.uv;
                svtx.sindex = start + i;
                svtx.vertexList.Add(vtx);
                vtx.parentIndex = svtx.sindex;

                // 共有頂点のウエイト再計算
                svtx.CalcBoneWeight(parent.WeightMode, weightPow);

                shareVertexList.Add(svtx);
            }

            // トライアングルを分解して頂点接続情報を作成
            var triangles = mesh.triangles;
            int tcnt = triangles.Length / 3;
            for (int i = 0; i < tcnt; i++)
            {
                int index = i * 3;
                int vi0 = triangles[index];
                int vi1 = triangles[index + 1];
                int vi2 = triangles[index + 2];

                var svtx0 = shareVertexList[start + vi0];
                var svtx1 = shareVertexList[start + vi1];
                var svtx2 = shareVertexList[start + vi2];

                // トライアングルハッシュ
                ulong thash = Utility.PackTriple(svtx0.sindex, svtx1.sindex, svtx2.sindex);

                // 重複トライアングルはスキップ
                if (triangleDict.ContainsKey(thash))
                {
                    continue;
                }

                // 頂点リンク
                svtx0.AddLink(svtx1);
                svtx0.AddLink(svtx2);
                svtx1.AddLink(svtx0);
                svtx1.AddLink(svtx2);
                svtx2.AddLink(svtx0);
                svtx2.AddLink(svtx1);

                // 登録
                var tri = new Triangle();
                tri.shareVertexList.Add(svtx0);
                tri.shareVertexList.Add(svtx1);
                tri.shareVertexList.Add(svtx2);
                triangleDict.Add(thash, tri);
            }

            return mindex;
        }

        /// <summary>
        /// メッシュを追加する
        /// 登録したメッシュインデックスを返す
        /// </summary>
        /// <param name="root"></param>
        /// <param name="posList"></param>
        /// <param name="norList"></param>
        /// <param name="tanList"></param>
        /// <param name="uvList"></param>
        /// <returns></returns>
        public int AddMesh(Transform root, List<Vector3> posList, List<Vector3> norList = null, List<Vector4> tanList = null, List<Vector2> uvList = null, List<int> triangleList = null)
        {
            Debug.Assert(root != null);
            Debug.Assert(posList != null);
            Debug.Assert(posList.Count > 0);

            // メッシュ情報
            int mindex = meshInfoList.Count();
            var minfo = new MeshInfo();
            minfo.index = mindex;
            minfo.mesh = null;
            minfo.vertexCount = posList.Count;
            meshInfoList.Add(minfo);

            // ボーン登録
            int bindex = boneList.IndexOf(root);
            if (bindex < 0)
            {
                boneList.Add(root);
                bindex = boneList.Count - 1;
            }

            // 頂点登録
            int start = shareVertexList.Count;
            for (int i = 0; i < posList.Count; i++)
            {
                var vtx = new Vertex();
                vtx.meshIndex = mindex;
                vtx.vertexIndex = i;
                vtx.wpos = posList[i];
                vtx.wnor = norList != null ? norList[i] : Vector3.up;
                vtx.wtan = tanList != null ? tanList[i] : new Vector4(1, 0, 0, 1);
                vtx.tanw = tanList != null ? tanList[i].w : -1;
                vtx.uv = uvList != null ? uvList[i] : Vector2.zero;
                originalVertexList.Add(vtx);

                minfo.vertexList.Add(vtx);

                // ウエイト
                var w = new WeightData()
                {
                    boneIndex = 0,
                    boneWeight = 1
                };
                vtx.boneWeightList.Add(w);

                // 共有頂点登録
                var svtx = new ShareVertex();
                svtx.wpos = vtx.wpos;
                svtx.wnor = vtx.wnor;
                svtx.wtan = vtx.wtan;
                svtx.tanw = -1.0f; // 接線空間は(-1)DirectX系で統一する
                svtx.uv = vtx.uv;
                svtx.sindex = start + i;
                svtx.vertexList.Add(vtx);
                vtx.parentIndex = svtx.sindex;

                // 共有頂点のウエイト再計算
                svtx.CalcBoneWeight(parent.WeightMode, weightPow);

                shareVertexList.Add(svtx);
            }

            // トライアングルを分解して頂点接続情報を作成
            if (triangleList != null)
            {
                int tcnt = triangleList.Count / 3;
                for (int i = 0; i < tcnt; i++)
                {
                    int index = i * 3;
                    int vi0 = triangleList[index];
                    int vi1 = triangleList[index + 1];
                    int vi2 = triangleList[index + 2];

                    var svtx0 = shareVertexList[start + vi0];
                    var svtx1 = shareVertexList[start + vi1];
                    var svtx2 = shareVertexList[start + vi2];

                    // トライアングルハッシュ
                    ulong thash = Utility.PackTriple(svtx0.sindex, svtx1.sindex, svtx2.sindex);

                    // 重複トライアングルはスキップ
                    if (triangleDict.ContainsKey(thash))
                    {
                        continue;
                    }

                    // 頂点リンク
                    svtx0.AddLink(svtx1);
                    svtx0.AddLink(svtx2);
                    svtx1.AddLink(svtx0);
                    svtx1.AddLink(svtx2);
                    svtx2.AddLink(svtx0);
                    svtx2.AddLink(svtx1);

                    // 登録
                    var tri = new Triangle();
                    tri.shareVertexList.Add(svtx0);
                    tri.shareVertexList.Add(svtx1);
                    tri.shareVertexList.Add(svtx2);
                    triangleDict.Add(thash, tri);
                }
            }

            return mindex;
        }

        /// <summary>
        /// ２つの頂点を結合する。(sv0にsv1を合成し、sv1を削除する）
        /// </summary>
        /// <param name="sv0"></param>
        /// <param name="sv1"></param>
        public void CombineVertex(ShareVertex sv0, ShareVertex sv1)
        {
            // sv0にsv1を合成する
            sv0.vertexList.AddRange(sv1.vertexList);

            // リンク情報再構築
            sv0.linkShareVertexSet.Remove(sv1);
            foreach (var sv in sv1.linkShareVertexSet)
            {
                if (sv != sv0)
                    sv0.linkShareVertexSet.Add(sv);
            }
            foreach (var sv in sv0.linkShareVertexSet)
            {
                sv.ReplaseLink(sv1, sv0);
            }

            // sv1削除
            shareVertexList.Remove(sv1);

            // sv0の座標を更新する
            sv0.RecalcCoordinate();
        }


        //=========================================================================================
        /// <summary>
        /// 頂点リダクション後のメッシュデータ情報を再設定する
        /// </summary>
        public void UpdateMeshData(bool createTetra)
        {
            // 頂点インデックス設定
            CalcVertexIndex();

            // UV算出
            CalcUV(UvWrapMode.Sphere);

            // トライアングルおよびライン情報形成
            CreateTriangleAndLine();

            // 共有頂点のウエイトを再計算
            CalcShareVertexWeight();

            // トライアングル法線を（できる限り）揃える
            AdjustTriangleNormal();

            // トライアングルに属する頂点の法線接線を再計算
            CalcVertexNormalFromTriangle();

            // テトラメッシュ構築
            if (createTetra)
                CreateTetraMesh();
        }

        /// <summary>
        /// 頂点インデックス設定
        /// </summary>
        private void CalcVertexIndex()
        {
            for (int i = 0; i < shareVertexList.Count; i++)
            {
                var sv = shareVertexList[i];
                sv.sindex = i;

                foreach (var vtx in sv.vertexList)
                    vtx.parentIndex = i;
            }
        }

        /// <summary>
        /// UV値の算出
        /// スフィアラッピング
        /// </summary>
        /// <param name="scr"></param>
        void CalcUV(UvWrapMode wrapMode)
        {
            // バウンディングボックス中心からの簡単なスフィアラッピング
            if (wrapMode == UvWrapMode.Sphere)
            {
                var center = Vector3.zero;
                foreach (var sv in shareVertexList)
                    center += sv.wpos;
                center /= VertexCount;

                float add = 0.0f;

                foreach (var sv in shareVertexList)
                {
                    var lv = sv.wpos - center;
                    var len = lv.magnitude;
                    lv.Normalize();

                    float u = Mathf.Atan2(lv.x, lv.z);
                    u = Mathf.Clamp01(Mathf.InverseLerp(-Mathf.PI, Mathf.PI, u));

                    float v = Vector3.Dot(Vector3.up, lv);
                    v = Mathf.Clamp01(Mathf.InverseLerp(1.0f, -1.0f, v));

                    // 方向ベクトル上に同じUVが生成されてしまうのを避けるためUVに距離を加算してずらす
                    var uv = new Vector2(u + len * 0.01f + add, v + len * 0.01f + add);
                    add += 0.001234f;

                    sv.uv = uv;
                }
            }
        }

        /// <summary>
        /// トライアングルおよびライン情報形成
        /// </summary>
        private void CreateTriangleAndLine()
        {
            triangleDict.Clear();
            lineDict.Clear();

            // すべての頂点接続ペアをライン用に登録する
            HashSet<uint> linePairSet = new HashSet<uint>();
            foreach (var sv0 in shareVertexList)
            {
                foreach (var sv1 in sv0.linkShareVertexSet)
                {
                    uint lhash = Utility.PackPair(sv0.sindex, sv1.sindex);
                    linePairSet.Add(lhash);
                }
            }

            // トライアングル形成
            foreach (var sv0 in shareVertexList)
            {
                var linkList = sv0.linkShareVertexSet.ToArray();

                for (int i = 0; i < (linkList.Length - 1); i++)
                {
                    var sv1 = linkList[i];
                    for (int j = i + 1; j < linkList.Length; j++)
                    {
                        var sv2 = linkList[j];

                        // sv0, sv1, sv2でトライアングルを形成できるか判定
                        if (sv1.linkShareVertexSet.Contains(sv2) && sv2.linkShareVertexSet.Contains(sv1))
                        {
                            //Debug.Log("triangle (" + sv0.sindex + "," + sv1.sindex + "," + sv2.sindex + ")");

                            // トライアングル面積判定
                            var area = Triangle.GetTriangleArea(sv0, sv1, sv2);
                            //Debug.Log("area=" + area);
                            if (area < 1e-06f)
                                continue;


                            // トライアングルハッシュ
                            ulong thash = Utility.PackTriple(sv0.sindex, sv1.sindex, sv2.sindex);

                            // 登録
                            if (triangleDict.ContainsKey(thash) == false)
                            {
                                var tri = new Triangle();
                                tri.shareVertexList.Add(sv0);
                                tri.shareVertexList.Add(sv1);
                                tri.shareVertexList.Add(sv2);

                                triangleDict.Add(thash, tri);

                                // トライアングルで使われたラインはラインペアから削除する
                                var lhash0 = Utility.PackPair(sv0.sindex, sv1.sindex);
                                var lhash1 = Utility.PackPair(sv1.sindex, sv2.sindex);
                                var lhash2 = Utility.PackPair(sv2.sindex, sv0.sindex);
                                linePairSet.Remove(lhash0);
                                linePairSet.Remove(lhash1);
                                linePairSet.Remove(lhash2);
                            }
                        }
                    }
                }
            }

            // 不要なトライアングルを削除する(v1.8.0)
            // 四辺形（トライアングルペア）を調べて、同じ頂点を使用しほぼ同じ平面ならば片方を削除する
            if (RemoveSameTrianglePair)
            {
                var squareDict = GetSquareDict();
                foreach (var kv in squareDict)
                {
                    var slist = kv.Value;
#if false
                Debug.Log($"Before Square list count:{slist.Count}");
                foreach (var s in slist)
                {
                    Debug.Log(s);
                }
#endif
                    // 構成がほぼ同じトライアングルペアは１つを残して削除する
                    HashSet<Square> removeSquareSet = new HashSet<Square>();
                    for (int i = 0; i < slist.Count - 1; i++)
                    {
                        var s0 = slist[i];
                        if (removeSquareSet.Contains(s0))
                            continue;
                        for (int j = i + 1; j < slist.Count; j++)
                        {
                            var s1 = slist[j];
                            if (removeSquareSet.Contains(s1))
                                continue;

                            // １つでもトライアングルが重複する場合は削除しない(v1.10.4)
                            if (s0.triangleList.FindAll(s1.triangleList.Contains).Count > 0)
                            {
                                continue;
                            }

                            var ang = math.abs(s0.angle - s1.angle);

                            // 角度判定
                            if (ang <= RemoveSameTrianglePairAngle)
                            {
                                // 片方を削除する
                                removeSquareSet.Add(s1);
                            }
                        }
                    }
                    foreach (var s in removeSquareSet)
                    {
                        slist.Remove(s);
                        foreach (var tri in s.triangleList)
                        {
                            RemoveTriangle(tri.GetTriangleHash());
                        }
                    }
#if false
                Debug.Log($"After Square list count:{slist.Count}");
                foreach (var s in slist)
                {
                    Debug.Log(s);
                }
#endif
                }
            }

            // ライン形成（残ったラインペア）
            foreach (var lhash in linePairSet)
            {
                if (lineDict.ContainsKey(lhash) == false)
                {
                    int v0, v1;
                    Utility.UnpackPair(lhash, out v0, out v1);
                    var sv0 = shareVertexList[v0];
                    var sv1 = shareVertexList[v1];

                    var line = new Line();
                    line.shareVertexList.Add(sv0);
                    line.shareVertexList.Add(sv1);
                    lineDict.Add(lhash, line);
                }
            }

            // トライアングルインデックス
            int tindex = 0;
            foreach (var tri in triangleDict.Values)
            {
                tri.tindex = tindex;
                tindex++;
            }

            // マージ頂点が接続するトライアングルリスト構築
            foreach (var sv in shareVertexList)
                sv.linkTriangleSet.Clear();
            foreach (var tri in triangleDict.Values)
            {
                foreach (var sv in tri.shareVertexList)
                    sv.linkTriangleSet.Add(tri);
            }
        }

        /// <summary>
        /// 共有頂点のウエイトを再計算
        /// </summary>
        private void CalcShareVertexWeight()
        {
            foreach (var svtx in shareVertexList)
            {
                svtx.CalcBoneWeight(parent.WeightMode, weightPow);
            }
        }

        /// <summary>
        /// トライアングル法線を（できる限り）揃える
        /// </summary>
        private void AdjustTriangleNormal()
        {
            // エッジと接続トライアングル辞書
            var edgeDict = GetEdgeToTriangleDict();

            // 全トライアングルリスト化
            var triHashList = new List<ulong>();
            foreach (var thash in triangleDict.Keys)
            {
                triHashList.Add(thash);
            }

            // 共通トライアングルをレイヤーとして登録していく
            var useTriSet = new HashSet<ulong>();
            var layerTriangleList = new List<List<ulong>>();
            while (triHashList.Count > 0)
            {
                // レイヤー作成
                var triangleList = new List<ulong>();
                int openCount = 0;
                int closeCount = 0;

                // 起点トライアングル
                var tqueue = new Queue<ulong>();
                tqueue.Enqueue(triHashList[0]);
                //Debug.Log("起点:" + triangleDict[triHashList[0]].tindex);

                while (tqueue.Count > 0)
                {
                    // １つ取り出し（基準トライアングル）
                    ulong thash = tqueue.Dequeue();
                    if (useTriSet.Contains(thash))
                        continue;

                    // レイヤートライアングルとして登録
                    triangleList.Add(thash);
                    triHashList.Remove(thash);
                    useTriSet.Add(thash);
                    var tri = triangleDict[thash];

                    // 法線／接線計算
                    var nor1 = tri.CalcTriangleNormal();
                    tri.CalcTriangleTangent();

                    // トライアングル構成エッジ
                    uint edge0, edge1, edge2;
                    tri.GetEdge(out edge0, out edge1, out edge2);

                    // 隣接トライアングル判定
                    uint[] edgeList = new uint[] { edge0, edge1, edge2 };
                    foreach (var edge in edgeList)
                    {
                        var edgeTriList = edgeDict[edge];

                        // トライアングル数が０なら無効
                        if (edgeTriList.Count == 0)
                            continue;

                        foreach (var thash2 in edgeTriList)
                        {
                            // 自身のトライアングルは無視
                            if (thash2 == thash)
                                continue;

                            // すでに処理済みのトライアングルなら無視する
                            if (useTriSet.Contains(thash2))
                                continue;

                            // 基準トライアングルとの面角度を求める
                            var tri2 = triangleDict[thash2];
                            float ang = CalcTwoTriangleAngle(tri, tri2, edge);

                            // 面角度が一定以上ならば不連続としてスキップする
                            if (ang > SameSurfaceAngle)
                                continue;

                            // 面の法線が一定方向を向くように調整する
                            var nor2 = tri2.CalcTriangleNormal();
                            var baseang = Vector3.Angle(nor2, nor1);

                            if (baseang >= 90.0f && tri2.flipLock == false)
                            {
                                // フリップ
                                tri2.Flip();

                                // このトライアングルペアが完全に水平の場合、以降のフリップを禁止する（法線が０となるため）
                                if (baseang == 180.0f)
                                {
                                    tri2.flipLock = true;
                                }
                            }

                            // 隣接トライアングルの法線が開いているか閉じているかのカウント
                            if (CheckTwoTriangleOpen(tri, tri2, edge))
                                openCount++;
                            else
                                closeCount++;

                            // 同一レイヤーとして処理する
                            tqueue.Enqueue(thash2);

                        }
                    }
                }

                // 閉じているトライアングルのほうが多い場合はレイヤー全体の法線をフリップさせる
                //Debug.Log("layer tcnt:" + triangleList.Count + " open:" + openCount + " close:" + closeCount);
                if (closeCount > openCount)
                {
                    foreach (var thash in triangleList)
                    {
                        var tri = triangleDict[thash];
                        tri.Flip();
                        tri.CalcTriangleTangent();
                    }
                }

                // レイヤー登録
                layerTriangleList.Add(triangleList);
            }
        }

        /// <summary>
        /// 共通するエッジをもつ２つのトライアングルが開いているか判定する
        /// </summary>
        /// <param name="tri1"></param>
        /// <param name="tri2"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        private bool CheckTwoTriangleOpen(Triangle tri1, Triangle tri2, uint edge)
        {
            int v0, v1;
            Utility.UnpackPair(edge, out v0, out v1);

            var sv0 = tri2.GetNonEdgeVertex(v0, v1);

            var v = Vector3.Normalize(sv0.wpos - shareVertexList[v0].wpos);

            return Vector3.Dot(tri1.wnor, v) <= 0.0f;
        }

        /// <summary>
        /// 共通するエッジをもつ２つのトライアングルのなす角を求める（デグリー角）
        /// </summary>
        /// <param name="tri1"></param>
        /// <param name="tri2"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        private float CalcTwoTriangleAngle(Triangle tri1, Triangle tri2, uint edge)
        {
            int v0, v1;
            Utility.UnpackPair(edge, out v0, out v1);

            var sv0 = shareVertexList[v0];
            var sv1 = tri1.GetNonEdgeVertex(v0, v1);
            var sv2 = tri2.GetNonEdgeVertex(v0, v1);

            // トライアングル角度
            var va = shareVertexList[v1].wpos - shareVertexList[v0].wpos;
            var vb = sv1.wpos - shareVertexList[v0].wpos;
            var vc = sv2.wpos - shareVertexList[v0].wpos;

            var n0 = Vector3.Cross(va, vb);
            var n1 = Vector3.Cross(vc, va);

            return Vector3.Angle(n0, n1);
        }

        /// <summary>
        /// トライアングルのエッジをキーとした接続トライアングル辞書を作成して返す
        /// </summary>
        /// <returns></returns>
        private Dictionary<uint, List<ulong>> GetEdgeToTriangleDict()
        {
            var edgeDict = new Dictionary<uint, List<ulong>>();

            // トライアングルリスト
            List<uint> edgeList = new List<uint>();
            foreach (var kv in triangleDict)
            {
                ulong thash = kv.Key;
                var tri = kv.Value;

                int sindex0 = tri.shareVertexList[0].sindex;
                int sindex1 = tri.shareVertexList[1].sindex;
                int sindex2 = tri.shareVertexList[2].sindex;

                edgeList.Clear();
                edgeList.Add(Utility.PackPair(sindex0, sindex1));
                edgeList.Add(Utility.PackPair(sindex1, sindex2));
                edgeList.Add(Utility.PackPair(sindex2, sindex0));

                foreach (var edge in edgeList)
                {
                    if (edgeDict.ContainsKey(edge) == false)
                        edgeDict.Add(edge, new List<ulong>());
                    edgeDict[edge].Add(thash);
                }
            }

            return edgeDict;
        }

        /// <summary>
        /// トライアングルに属する頂点法線をトライアングル面法線から算出する
        /// </summary>
        private void CalcVertexNormalFromTriangle()
        {
            foreach (var sv in shareVertexList)
                sv.CalcNormalTangentFromTriangle();
#if false
            Dictionary<int, NormalTangentInfo> sumDict = new Dictionary<int, NormalTangentInfo>();

            // トライアングルごとの法線接線を頂点ごとに集計
            foreach (var tri in triangleDict.Values)
            {
                foreach (var sv in tri.shareVertexList)
                {
                    NormalTangentInfo data = null;
                    if (sumDict.ContainsKey(sv.sindex) == false)
                    {
                        data = new NormalTangentInfo();
                        sumDict.Add(sv.sindex, data);
                    }
                    else
                    {
                        data = sumDict[sv.sindex];
                    }

                    data.normal += tri.wnor;
                    data.tangent += tri.wtan;
                    data.count++;
                }
            }

            // 頂点ごとに集計された法線接線の平均値を設定
            foreach (var kv in sumDict)
            {
                int sindex = kv.Key;

                //if (kv.Value.normal.magnitude < 0.01f)
                //    Debug.LogWarning("法線が短い->" + sindex);

                var nor = kv.Value.normal.normalized;
                var tan = kv.Value.tangent.normalized;


                shareVertexList[sindex].wnor = nor;
                shareVertexList[sindex].wtan = tan;
            }
#endif
        }

        /// <summary>
        /// トライアングルを削除する
        /// </summary>
        /// <param name="thash"></param>
        private void RemoveTriangle(ulong thash)
        {
            if (triangleDict.ContainsKey(thash))
            {
                var tri = triangleDict[thash];
                foreach (var svt in tri.shareVertexList)
                {
                    svt.linkTriangleSet.Remove(tri);
                }

                triangleDict.Remove(thash);
            }
        }

        /// <summary>
        /// エッジを共有する２つのトライアングルの四辺形情報を作成し、
        /// 共通する四辺形をキーとして辞書を返す
        /// </summary>
        /// <returns></returns>
        private Dictionary<ulong, List<Square>> GetSquareDict()
        {
            var squareDict = new Dictionary<ulong, List<Square>>();

            var edgeTriangleDict = GetEdgeToTriangleDict();
            foreach (var kv in edgeTriangleDict)
            {
                // 四辺形情報作成
                int eindex0, eindex1;
                Utility.UnpackPair(kv.Key, out eindex0, out eindex1);
                var tlist = kv.Value;
                for (int i = 0; i < tlist.Count - 1; i++)
                {
                    for (int j = i + 1; j < tlist.Count; j++)
                    {
                        var tri1 = triangleDict[tlist[i]];
                        var tri2 = triangleDict[tlist[j]];

                        var vindex0 = tri1.GetNonEdgeVertex(eindex0, eindex1).sindex;
                        var vindex1 = tri2.GetNonEdgeVertex(eindex0, eindex1).sindex;

                        ulong shash = Utility.PackQuater(vindex0, vindex1, eindex0, eindex1);

                        // なす角
                        var v0 = shareVertexList[vindex0].wpos - shareVertexList[eindex0].wpos;
                        var v1 = shareVertexList[vindex1].wpos - shareVertexList[eindex0].wpos;
                        var ev = shareVertexList[eindex1].wpos - shareVertexList[eindex0].wpos;
                        var n0 = Vector3.Cross(v0, ev);
                        var n1 = Vector3.Cross(v1, ev);
                        var ang = Vector3.Angle(n0, n1);

                        //Debug.Log("ang:" + ang);
                        // なす角が９０度以下は登録しない（これは歪な四辺形になっている）
                        if (ang <= 135)
                            continue;

                        // 登録
                        var square = new Square();
                        square.shash = shash;
                        square.angle = ang;
                        square.triangleList.Add(tri1);
                        square.triangleList.Add(tri2);
                        if (squareDict.ContainsKey(shash) == false)
                        {
                            squareDict.Add(shash, new List<Square>());
                        }
                        squareDict[shash].Add(square);
                    }
                }
            }

            return squareDict;
        }

        /// <summary>
        /// エッジを共有する２つのトライアングルペアを調べ、なす角がほぼ等しく
        /// 同じ４つの頂点を共有するトライアングルを削除する
        /// </summary>
        private void RemoveOverlappingSquareTriangles()
        {
            var squareDict = GetSquareDict();

            foreach (var kv in squareDict)
            {
                // todo:なす角が一定以内の四辺形を見つける
                // todo:１つを残して他を削除する
            }
        }

        /// <summary>
        /// テトラメッシュの構築
        /// </summary>
        private void CreateTetraMesh()
        {
            tetraList.Clear();
            if (VertexCount < 4)
                return;

            // ポイントをすべて内包するテトラポイントを追加する
            var b = CalcBounding();
            float areaRadius = Mathf.Max(Mathf.Max(b.extents.x, b.extents.y), b.extents.z);
            float dist = areaRadius * 100.0f;
            var tempSV0 = new ShareVertex();
            var tempSV1 = new ShareVertex();
            var tempSV2 = new ShareVertex();
            var tempSV3 = new ShareVertex();
            tempSV0.wpos = b.center + new Vector3(0.0f, -dist, 0.0f);
            tempSV1.wpos = b.center + new Vector3(-dist, dist, dist);
            tempSV2.wpos = b.center + new Vector3(dist, dist, dist);
            tempSV3.wpos = b.center + new Vector3(0.0f, dist, -dist);
            int svcnt = shareVertexList.Count;
            tempSV0.sindex = svcnt++;
            tempSV1.sindex = svcnt++;
            tempSV2.sindex = svcnt++;
            tempSV3.sindex = svcnt++;
            shareVertexList.Add(tempSV0);
            shareVertexList.Add(tempSV1);
            shareVertexList.Add(tempSV2);
            shareVertexList.Add(tempSV3);

            // 最初のテトラを分割テトラとして登録
            List<Tetra> divideTetras = new List<Tetra>();
            var tetra0 = new Tetra(tempSV0, tempSV1, tempSV2, tempSV3);
            tetra0.CalcCircumcircle();
            divideTetras.Add(tetra0);

            // 重複チェック用
            Dictionary<ulong, Tetra> useTetraHash = new Dictionary<ulong, Tetra>();
            useTetraHash.Add(tetra0.GetTetraHash(), tetra0);

            // テトラ構築
            for (int k = 0; k < (shareVertexList.Count - 4); k++)
            {
                var point = shareVertexList[k];

                List<Tetra> tempDivTetras = new List<Tetra>();

                for (int i = 0; i < divideTetras.Count;)
                {
                    var tetra = divideTetras[i];
                    if (tetra.ContainsPoint(point) == false)
                    {
                        if (tetra.IntersectCircumcircle(point.wpos))
                        {
                            // 再分割
                            var tetra1 = new Tetra(tetra.shareVertexList[0], tetra.shareVertexList[1], tetra.shareVertexList[2], point);
                            var tetra2 = new Tetra(tetra.shareVertexList[0], tetra.shareVertexList[2], tetra.shareVertexList[3], point);
                            var tetra3 = new Tetra(tetra.shareVertexList[0], tetra.shareVertexList[3], tetra.shareVertexList[1], point);
                            var tetra4 = new Tetra(tetra.shareVertexList[1], tetra.shareVertexList[2], tetra.shareVertexList[3], point);

                            // 検証
                            //bool chk1 = tetra1.Verification();
                            //bool chk2 = tetra2.Verification();
                            //bool chk3 = tetra3.Verification();
                            //bool chk4 = tetra4.Verification();
                            //if (chk1 && chk2 && chk3 && chk4)
                            {

                                tempDivTetras.Add(tetra1);
                                tempDivTetras.Add(tetra2);
                                tempDivTetras.Add(tetra3);
                                tempDivTetras.Add(tetra4);

                                useTetraHash.Remove(tetra.GetTetraHash());
                                divideTetras.RemoveAt(i);
                                continue;
                            }
                        }
                    }

                    i++;
                }

                // 次の候補として追加
                foreach (var tetra in tempDivTetras)
                {
                    ulong thash = tetra.GetTetraHash();
                    if (useTetraHash.ContainsKey(thash) == false)
                    {
                        tetra.CalcCircumcircle();
                        useTetraHash.Add(thash, tetra);
                        divideTetras.Add(tetra);
                    }
                    else
                    {
                        // 衝突
                        // 衝突もとも削除する
                        var deltetra = useTetraHash[thash];
                        useTetraHash.Remove(thash);
                        divideTetras.Remove(deltetra);
                    }
                }
#if false
                // 重複テトラを削除
                List<Tetra> delTetras = new List<Tetra>();
                for (int i = 0; i < (tempDivTetras.Count - 1); i++)
                {
                    var tetra = tempDivTetras[i];
                    for (int j = i + 1; j < tempDivTetras.Count; j++)
                    {
                        var tetra2 = tempDivTetras[j];

                        if (tetra.CheckSame(tetra2))
                        {
                            delTetras.Add(tetra);
                            delTetras.Add(tetra2);
                        }
                    }
                }
                foreach (var tetra in delTetras)
                {
                    tempDivTetras.Remove(tetra);
                }

                // 次の候補として追加
                foreach (var tetra in tempDivTetras)
                {
                    tetra.CalcCircumcircle();
                }
                divideTetras.AddRange(tempDivTetras);
#endif
            }


            // 最初に追加したテトラを削除
            for (int i = 0; i < divideTetras.Count;)
            {
                var tetra = divideTetras[i];
                if (tetra.ContainsPoint(tempSV0, tempSV1, tempSV2, tempSV3))
                {
                    // このテトラは削除する
                    useTetraHash.Remove(tetra.GetTetraHash());
                    divideTetras.RemoveAt(i);
                    continue;
                }

                i++;
            }
            shareVertexList.Remove(tempSV0);
            shareVertexList.Remove(tempSV1);
            shareVertexList.Remove(tempSV2);
            shareVertexList.Remove(tempSV3);

            // テトラの検証
            for (int i = 0; i < divideTetras.Count;)
            {
                var tetra = divideTetras[i];
                if (tetra.Verification() == false)
                {
                    divideTetras.RemoveAt(i);
                    continue;
                }

                // テトラサイズ計算
                //tetra.CalcSize();

                i++;
            }

            // 最終結果を格納
            tetraList = divideTetras;
        }

        private Bounds CalcBounding()
        {
            Bounds b = new Bounds(shareVertexList[0].wpos, Vector3.one * 0.01f);
            foreach (var sv in shareVertexList)
            {
                b.Encapsulate(sv.wpos);
            }

            return b;
        }

        //=========================================================================================
        /// <summary>
        /// 最終メッシュデータを計算して返す
        /// </summary>
        /// <param name="root">メッシュの基準トランスフォーム（この姿勢を元にローカル座標変換される）</param>
        public FinalData GetFinalData(Transform root)
        {
            Debug.Assert(root);

            var final = new FinalData();

            // 頂点座標ローカル変換
            for (int i = 0; i < shareVertexList.Count; i++)
            {
                var svtx = shareVertexList[i];
                var lpos = root.InverseTransformPoint(svtx.wpos);
                var lnor = root.InverseTransformDirection(svtx.wnor).normalized;
                Vector4 ltan = root.InverseTransformDirection(svtx.wtan).normalized;
                ltan.w = svtx.tanw;

                final.vertices.Add(lpos);
                final.normals.Add(lnor);
                final.tangents.Add(ltan);
                final.uvs.Add(svtx.uv);

                // 所属トライアングル
                final.vertexToTriangleCountList.Add(0); // clear
                final.vertexToTriangleStartList.Add(0); // clear

                // 共有頂点のローカル変換マトリックスを求める
                svtx.CalcWorldToLocalMatrix();
            }

            // 頂点ウエイト格納
            foreach (var svtx in shareVertexList)
            {
                final.boneWeights.Add(svtx.GetBoneWeight());
            }

            // ボーンリスト確定
            final.bones = new List<Transform>(boneList);

            // ボーンのバインドポーズを求める
            var rootLocalToWorldMatrix = root.localToWorldMatrix;
            foreach (var bone in final.bones)
            {
                if (bone)
                {
                    Matrix4x4 bindpose = bone.worldToLocalMatrix * rootLocalToWorldMatrix;
                    final.bindPoses.Add(bindpose);
                }
                else
                    final.bindPoses.Add(Matrix4x4.identity);
            }

            // 共有頂点のバインドポーズを求める
            foreach (var sv in shareVertexList)
            {
                Matrix4x4 bindpose = sv.worldToLocalMatrix * rootLocalToWorldMatrix;
                sv.bindpose = bindpose;
                final.vertexBindPoses.Add(bindpose);
            }

            // トライアングルリスト
            foreach (var tri in triangleDict.Values)
            {
                for (int i = 0; i < 3; i++)
                {
                    int sindex = tri.shareVertexList[i].sindex;
                    final.triangles.Add(sindex);
                }

                // トライアングル法線
                //var lnor = root.InverseTransformDirection(tri.wnor).normalized;
                //final.triangleNormals.Add(lnor);
            }

            // 共有頂点所属のトライアングルリスト構築
            for (int i = 0; i < VertexCount; i++)
            {
                var sv = shareVertexList[i];
                if (sv.linkTriangleSet.Count == 0)
                    continue;
                final.vertexToTriangleCountList[i] = sv.linkTriangleSet.Count;
                final.vertexToTriangleStartList[i] = final.vertexToTriangleIndexList.Count;
                foreach (var tri in sv.linkTriangleSet)
                {
                    final.vertexToTriangleIndexList.Add(tri.tindex);
                }
            }

            // ラインリスト
            foreach (var line in lineDict.Values)
            {
                for (int i = 0; i < 2; i++)
                    final.lines.Add(line.shareVertexList[i].sindex);
            }

            // テトラリスト
            foreach (var tetra in tetraList)
            {
                for (int i = 0; i < 4; i++)
                {
                    int sindex = tetra.shareVertexList[i].sindex;
                    final.tetras.Add(sindex);
                }

                // テトラサイズ
                final.tetraSizes.Add(tetra.tetraSize);
            }

            // ライン／トライアングル接続の平均距離を求める
            float sumlen = 0;
            int sumcnt = 0;
            foreach (var tri in triangleDict.Values)
            {
                sumlen += Vector3.Distance(tri.shareVertexList[0].wpos, tri.shareVertexList[1].wpos);
                sumlen += Vector3.Distance(tri.shareVertexList[1].wpos, tri.shareVertexList[2].wpos);
                sumlen += Vector3.Distance(tri.shareVertexList[2].wpos, tri.shareVertexList[0].wpos);
                sumcnt += 3;
            }
            foreach (var line in lineDict.Values)
            {
                sumlen += Vector3.Distance(line.shareVertexList[0].wpos, line.shareVertexList[1].wpos);
                sumcnt += 1;
            }
            sumlen /= (float)sumcnt;
            //Debug.Log("Average length->" + sumlen);

            // マージ頂点の影響リスト初期化
            for (int i = 0; i < VertexCount; i++)
            {
                final.vertexToMeshIndexList.Add(new FinalData.MeshIndexData());
            }

            // オリジナルメッシュ情報作成
            CreateOriginalMeshInfo(final, root, sumlen * 1.5f); // 2.0f?

            return final;
        }

#if false
        /// <summary>
        /// UV値の算出
        /// スフィアラッピング
        /// </summary>
        /// <param name="scr"></param>
        void CalcUV(FinalData final, UvWrapMode wrapMode)
        {
            // バウンディングボックス中心からの簡単なスフィアラッピング
            if (wrapMode == UvWrapMode.Sphere)
            {
                var localCenter = Vector3.zero;
                foreach (var lpos in final.vertices)
                    localCenter += lpos;
                localCenter /= final.VertexCount;

                for (int i = 0; i < final.VertexCount; i++)
                {
                    var lv = final.vertices[i] - localCenter;
                    var len = lv.magnitude;
                    lv.Normalize();

                    float u = Mathf.Atan2(lv.x, lv.z);
                    u = Mathf.Clamp01(Mathf.InverseLerp(-Mathf.PI, Mathf.PI, u));

                    float v = Vector3.Dot(Vector3.up, lv);
                    v = Mathf.Clamp01(Mathf.InverseLerp(1.0f, -1.0f, v));

                    // 方向ベクトル上に同じUVが生成されてしまうのを避けるためUVに距離を加算してずらす
                    var uv = new Vector2(u + len * 0.01f, v + len * 0.01f);

                    final.uvs[i] = uv;
                }
            }
        }
#endif

        /// <summary>
        /// オリジナルメッシュ情報作成
        /// </summary>
        void CreateOriginalMeshInfo(FinalData final, Transform root, float weightLength)
        {
            // メッシュ情報作成
            foreach (var minfo in meshInfoList)
            {
                var mdata = new FinalData.MeshInfo();
                mdata.mesh = minfo.mesh;
                mdata.meshIndex = minfo.index;

                // 子頂点のローカル座標／法線／接線を求める
                foreach (var vtx in minfo.vertexList)
                {
                    var lpos = root.InverseTransformPoint(vtx.wpos);
                    var lnor = root.InverseTransformDirection(vtx.wnor).normalized;
                    Vector4 ltan = root.InverseTransformDirection(vtx.wtan).normalized;
                    ltan.w = vtx.tanw;

                    mdata.vertices.Add(lpos);
                    mdata.normals.Add(lnor);
                    mdata.tangents.Add(ltan);

                    // 元々属していた親マージ頂点を記録
                    mdata.parents.Add(vtx.parentIndex);

                    // ウエイトデータも初期化
                    mdata.boneWeights.Add(new BoneWeight());
                }

                final.meshList.Add(mdata);
            }

            // 子頂点について最も近いmaxWeightCount点のマージ頂点を算出する
            foreach (var vt in originalVertexList)
            {
                var psv = shareVertexList[vt.parentIndex];

                // トライアングル接続ベースの最近点リストを取得
                // ※リストはすでに距離の昇順で並んでいる
                var nearList = SearchNearPointList(vt.wpos, psv, weightLength * 2.0f, 100);
                Debug.Assert(nearList.Count > 0);

                // 検索半径のみ残す
                var nearList2 = nearList.FindAll(sv => Vector3.Distance(vt.wpos, sv.wpos) <= weightLength);
                Debug.Assert(nearList2.Count > 0);

                // 最大ウエイト数でカット
                if (nearList2.Count > maxWeightCount)
                {
                    nearList2.RemoveRange(maxWeightCount, nearList2.Count - maxWeightCount);
                }

                // 最大距離
                float maxlen = weightLength;

                // 最大距離から各ウエイト係数を算出する
                List<float> wList = new List<float>();
                foreach (var sv in nearList2)
                {
                    float t = 1.0f;
                    if (maxlen > 0.0f)
                    {
                        var dist = Vector3.Distance(vt.wpos, sv.wpos);
                        t = Mathf.Clamp01((1.0f - dist / maxlen) + 0.001f);
                        t = Mathf.Pow(t, weightPow); // 2 ? 3 ?
                    }
                    wList.Add(t);
                }

                // ウエイトを合計１に調整
                float total = 0;
                foreach (var w in wList)
                {
                    total += w;
                }
                float scl = 1.0f / total;
                for (int i = 0; i < wList.Count; i++)
                {
                    wList[i] = wList[i] * scl;
                }

                // ウエイトデータを格納する
                var bw = new BoneWeight();
                for (int i = 0; i < nearList2.Count; i++)
                {
                    var sv = nearList2[i];
                    switch (i)
                    {
                        case 0:
                            bw.boneIndex0 = sv.sindex;
                            bw.weight0 = wList[i];
                            break;
                        case 1:
                            bw.boneIndex1 = sv.sindex;
                            bw.weight1 = wList[i];
                            break;
                        case 2:
                            bw.boneIndex2 = sv.sindex;
                            bw.weight2 = wList[i];
                            break;
                        case 3:
                            bw.boneIndex3 = sv.sindex;
                            bw.weight3 = wList[i];
                            break;
                    }

                    // マージ頂点への影響を記録
                    if (i < 4 && wList[i] > 0.0f)
                        AddVertexToMeshIndexData(final, sv.sindex, vt.meshIndex, vt.vertexIndex);
                }
                final.meshList[vt.meshIndex].boneWeights[vt.vertexIndex] = bw;
            }
        }

        /// <summary>
        /// マージ頂点のメッシュへの影響を記録する
        /// </summary>
        /// <param name="sindex"></param>
        /// <param name="meshIndex"></param>
        /// <param name="meshVertexIndex"></param>
        private void AddVertexToMeshIndexData(FinalData final, int sindex, int meshIndex, int meshVertexIndex)
        {
            final.vertexToMeshIndexList[sindex].meshIndexPackList.Add(Utility.Pack16(meshIndex, meshVertexIndex));
        }

        //=========================================================================================
        private class LinkInfo
        {
            public ShareVertex sv;
            public float length;
            public int count;
        }

        private class VertexLengthInfo
        {
            public ShareVertex sv;
            public float length;
        }

        /// <summary>
        /// トライアングル接続情報から最寄りの共有頂点をリストにして返す
        /// </summary>
        /// <param name="sv">検索開始共有頂点</param>
        /// <param name="maxCount">最大検索数</param>
        /// <returns></returns>
        private List<ShareVertex> SearchNearPointList(Vector3 basePos, ShareVertex sv, float weightLength, int maxCount)
        {
            var info = new LinkInfo();
            info.sv = sv;
            info.length = 0.0f;
            info.count = 0;

            Stack<LinkInfo> lstack = new Stack<LinkInfo>();
            lstack.Push(info);

            var checkSet = new HashSet<MeshData.ShareVertex>();
            var vlenList = new List<VertexLengthInfo>();
            while (lstack.Count > 0)
            {
                info = lstack.Pop();

                if (checkSet.Contains(info.sv))
                    continue;

                // 記録
                var vinfo = new VertexLengthInfo();
                vinfo.sv = info.sv;
                vinfo.length = Vector3.Distance(basePos, info.sv.wpos);
                vlenList.Add(vinfo);
                checkSet.Add(info.sv);

                // 接続頂点チェック
                if (info.count < 2) // 3?
                {
                    foreach (var sv2 in info.sv.linkShareVertexSet)
                    {
                        if (checkSet.Contains(sv2))
                            continue;

                        var dist = Vector3.Distance(basePos, sv2.wpos);

                        // 最大検索距離
                        if (dist > weightLength)
                            continue;

                        var info2 = new LinkInfo();
                        info2.sv = sv2;
                        info2.length = dist;
                        info2.count = info.count + 1;
                        lstack.Push(info2);
                    }
                }
            }

            // ソート
            vlenList.Sort((a, b) => a.length < b.length ? -1 : 1);

            // データ作成
            var nearList = new List<ShareVertex>();
            for (int i = 0; i < vlenList.Count && i < maxCount; i++)
                nearList.Add(vlenList[i].sv);

            return nearList;
        }

        //=========================================================================================
        /// <summary>
        /// メッシュの頂点／法線／接線をワールド座標変換して返す
        /// </summary>
        /// <param name="isSkinning"></param>
        /// <param name="mesh"></param>
        /// <param name="bones"></param>
        /// <param name="wposList"></param>
        /// <param name="wnorList"></param>
        /// <param name="wtanList"></param>
        /// <returns></returns>
        public void CalcMeshWorldPositionNormalTangent(
            bool isSkinning,
            Mesh mesh,
            List<Transform> bones,
            Matrix4x4[] bindPoseList,
            BoneWeight[] boneWeightList,
            out List<Vector3> wposList, out List<Vector3> wnorList, out List<Vector4> wtanList
            )
        {
            wposList = new List<Vector3>();
            wnorList = new List<Vector3>();
            wtanList = new List<Vector4>();

            if (mesh == null)
                return;

            int vcnt = mesh.vertexCount;
            Vector3[] vlist = mesh.vertices;
            Vector3[] nlist = mesh.normals;
            Vector4[] tlist = mesh.tangents;

            bool hasNormal = nlist != null && nlist.Length > 0;
            bool hasTangent = tlist != null && tlist.Length > 0;

            if (isSkinning == false)
            {
                // 通常メッシュ
                Transform t = bones[0];
                for (int i = 0; i < vcnt; i++)
                {
                    Vector3 wpos = t.TransformPoint(vlist[i]);
                    wposList.Add(wpos);

                    if (hasNormal)
                    {
                        Vector3 wnor = t.TransformDirection(nlist[i]);
                        wnor.Normalize();
                        wnorList.Add(wnor);
                    }

                    if (hasTangent)
                    {
                        Vector3 wtan = t.TransformDirection(tlist[i]);
                        wtan.Normalize();
                        wtanList.Add(new Vector4(wtan.x, wtan.y, wtan.z, tlist[i].w));
                    }
                }
            }
            else
            {
                // スキンメッシュ
                float[] weights = new float[4];
                int[] boneIndexs = new int[4];
                for (int i = 0; i < vcnt; i++)
                {
                    Vector3 wpos = Vector3.zero;
                    Vector3 wnor = Vector3.zero;
                    Vector3 wtan = Vector3.zero;

                    // 頂点スキニング
                    weights[0] = boneWeightList[i].weight0;
                    weights[1] = boneWeightList[i].weight1;
                    weights[2] = boneWeightList[i].weight2;
                    weights[3] = boneWeightList[i].weight3;
                    boneIndexs[0] = boneWeightList[i].boneIndex0;
                    boneIndexs[1] = boneWeightList[i].boneIndex1;
                    boneIndexs[2] = boneWeightList[i].boneIndex2;
                    boneIndexs[3] = boneWeightList[i].boneIndex3;

                    for (int j = 0; j < 4; j++)
                    {
                        float w = weights[j];
                        if (w > 0.0f)
                        {
                            int bindex = boneIndexs[j];
                            Transform t = bones[bindex];

                            // position
                            Vector3 v = bindPoseList[bindex].MultiplyPoint3x4(vlist[i]);
                            v = t.TransformPoint(v);
                            v *= w;
                            wpos += v;

                            // normal
                            if (hasNormal)
                            {
                                v = bindPoseList[bindex].MultiplyVector(nlist[i]);
                                v = t.TransformVector(v);
                                wnor += v.normalized * w;
                            }

                            // tangent
                            if (hasTangent)
                            {
                                v = bindPoseList[bindex].MultiplyVector(tlist[i]);
                                v = t.TransformVector(v);
                                wtan += v.normalized * w;
                            }
                        }
                    }

                    wposList.Add(wpos);
                    if (hasNormal)
                        wnorList.Add(wnor);
                    if (hasTangent)
                        wtanList.Add(new Vector4(wtan.x, wtan.y, wtan.z, tlist[i].w));
                }
            }
        }
    }
}
