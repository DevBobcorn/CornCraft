// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth
{
    public static class MeshUtility
    {
        /// <summary>
        /// SkinnedMeshRendererをMeshRendererに変換する
        /// </summary>
        /// <param name="sren"></param>
        /// <param name="replaceSkinnedMeshRenderer">trueの場合は置き換え、falseの場合は子のオブジェクトを生成して追加する</param>
        /// <returns></returns>
        public static GameObject ReplaceSkinnedMeshRendererToMeshRenderer(SkinnedMeshRenderer sren, bool replaceSkinnedMeshRenderer)
        {
            var obj = sren.gameObject;
            sren.enabled = false;

            GameObject copyobj = obj;
            if (replaceSkinnedMeshRenderer == false)
            {
                // 子のオブジェクトとして追加
                copyobj = new GameObject(obj.name + "[work mesh]");

                // transform
                var t = copyobj.transform;
                t.SetParent(obj.transform);
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
            }

            // mesh filter
            var meshFilter = copyobj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = sren.sharedMesh;

            // mesh renderer
            var mren = copyobj.AddComponent<MeshRenderer>();
            mren.sharedMaterials = sren.sharedMaterials;
            mren.lightProbeUsage = sren.lightProbeUsage;
            mren.probeAnchor = sren.probeAnchor;
            mren.reflectionProbeUsage = sren.reflectionProbeUsage;
            mren.shadowCastingMode = sren.shadowCastingMode;
            mren.receiveShadows = sren.receiveShadows;
            mren.motionVectorGenerationMode = sren.motionVectorGenerationMode;
            mren.allowOcclusionWhenDynamic = sren.allowOcclusionWhenDynamic;

            if (replaceSkinnedMeshRenderer)
            {
                // SkinnedMeshRendererを削除
                GameObject.Destroy(sren);
            }

            return copyobj;
        }

        /// <summary>
        /// メッシュデータから頂点／法線／接線をワールド座標変換して返す
        /// </summary>
        /// <param name="meshData"></param>
        /// <param name="boneList"></param>
        /// <param name="wposList"></param>
        /// <param name="wnorList"></param>
        /// <param name="wtanList"></param>
        /// <returns></returns>
        public static bool CalcMeshWorldPositionNormalTangent(
            MeshData meshData, List<Transform> boneList,
            out List<Vector3> wposList, out List<Vector3> wnorList, out List<Vector3> wtanList
            )
        {
            wposList = new List<Vector3>();
            wnorList = new List<Vector3>();
            wtanList = new List<Vector3>();

            if (meshData == null || boneList == null)
                return false;

            if (meshData.isSkinning == false)
            {
                // 通常メッシュ
                Transform t = boneList[0];
                for (int i = 0; i < meshData.VertexCount; i++)
                {
                    // 頂点スキニング
                    var vw = meshData.vertexWeightList[i];
                    Vector3 wpos = t.TransformPoint(vw.localPos);
                    Vector3 wnor = t.TransformDirection(vw.localNor);
                    Vector3 wtan = t.TransformDirection(vw.localTan);

                    wposList.Add(wpos);
                    wnorList.Add(wnor);
                    wtanList.Add(wtan);
                }
            }
            else
            {
                // スキンメッシュ
                float[] weights = new float[4];
                int[] boneIndexs = new int[4];
                for (int i = 0; i < meshData.VertexCount; i++)
                {
                    Vector3 wpos = Vector3.zero;
                    Vector3 wnor = Vector3.zero;
                    Vector3 wtan = Vector3.zero;

                    // 頂点スキニング
                    uint pack = meshData.vertexInfoList[i];
                    int wcnt = DataUtility.Unpack4_28Hi(pack);
                    int sindex = DataUtility.Unpack4_28Low(pack);
                    for (int j = 0; j < wcnt; j++)
                    {
                        var vw = meshData.vertexWeightList[sindex + j];

                        Transform t = boneList[vw.parentIndex];
                        wpos += t.TransformPoint(vw.localPos) * vw.weight;
                        wnor += t.TransformDirection(vw.localNor) * vw.weight;
                        wtan += t.TransformDirection(vw.localTan) * vw.weight;
                    }
                    wposList.Add(wpos);
                    wnorList.Add(wnor);
                    wtanList.Add(wtan);
                }
            }

            return true;
        }

        /// <summary>
        /// メッシュの頂点／法線／接線をワールド座標変換して返す
        /// </summary>
        /// <param name="ren"></param>
        /// <param name="mesh"></param>
        /// <param name="wposList"></param>
        /// <param name="wnorList"></param>
        /// <param name="wtanList"></param>
        /// <returns></returns>
        public static bool CalcMeshWorldPositionNormalTangent(
            Renderer ren, Mesh mesh, out List<Vector3> wposList, out List<Vector3> wnorList, out List<Vector3> wtanList
            )
        {
            wposList = new List<Vector3>();
            wnorList = new List<Vector3>();
            wtanList = new List<Vector3>();

            if (ren == null || mesh == null)
                return false;

            int vcnt = mesh.vertexCount;
            Vector3[] vlist = mesh.vertices;
            Vector3[] nlist = mesh.normals;
            bool hasNormal = nlist != null && nlist.Length > 0;
            Vector4[] tlist = mesh.tangents;
            bool hasTangent = tlist != null && tlist.Length > 0;

            if (ren is MeshRenderer)
            {
                // 通常メッシュ
                Transform t = ren.transform;
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
                        wtanList.Add(wtan);
                    }
                }
            }
            else if (ren is SkinnedMeshRenderer)
            {
                // スキンメッシュ
                SkinnedMeshRenderer sren = ren as SkinnedMeshRenderer;
                Transform[] boneList = sren.bones;
                Matrix4x4[] bindposeList = mesh.bindposes;
                BoneWeight[] boneWeightList = mesh.boneWeights;
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
                            Transform t = boneList[bindex];

                            // position
                            Vector3 v = bindposeList[bindex].MultiplyPoint3x4(vlist[i]);
                            v = t.TransformPoint(v);
                            v *= w;
                            wpos += v;

                            // normal
                            if (hasNormal)
                            {
                                v = bindposeList[bindex].MultiplyVector(nlist[i]);
                                v = t.TransformVector(v);
                                wnor += v.normalized * w;
                            }

                            // tangent
                            if (hasTangent)
                            {
                                v = bindposeList[bindex].MultiplyVector(tlist[i]);
                                v = t.TransformVector(v);
                                wtan += v.normalized * w;
                            }
                        }
                    }

                    wposList.Add(wpos);
                    if (hasNormal)
                        wnorList.Add(wnor);
                    if (hasTangent)
                        wtanList.Add(wtan);
                }
            }

            return true;
        }

#if false
        /// <summary>
        /// メッシュの頂点／回転をワールド座標変換して返す
        /// 回転は法線をforwardベクトル,接線をupベクトルとして計算
        /// </summary>
        /// <param name="ren"></param>
        /// <param name="mesh"></param>
        /// <param name="wposList"></param>
        /// <param name="wrotList"></param>
        /// <returns></returns>
        public static bool CalcMeshWorldPositionRotation(
            Renderer ren, Mesh mesh, out List<Vector3> wposList, out List<Quaternion> wrotList
            )
        {
            wrotList = new List<Quaternion>();
            List<Vector3> wnorList;
            List<Vector4> wtanList;
            if (CalcMeshWorldPositionNormalTangent(ren, mesh, out wposList, out wnorList, out wtanList) == false)
                return false;

            for (int i = 0; i < wposList.Count; i++)
            {
                Quaternion rot = Quaternion.LookRotation(wnorList[i], wtanList[i]);
                wrotList.Add(rot);
            }

            return true;
        }
#endif

        /// <summary>
        /// メッシュのローカル法線／接線を座標とUV値から一般的なアルゴリズムで算出して返す
        /// </summary>
        /// <param name="ren"></param>
        /// <param name="mesh"></param>
        /// <param name="wposList"></param>
        /// <param name="wnorList"></param>
        /// <param name="wtanList"></param>
        /// <returns></returns>
        public static bool CalcMeshLocalNormalTangent(
            List<int> selectList,
            Vector3[] vlist, Vector2[] uvlist, int[] triangles,
            out List<Vector3> lnorList, out List<Vector3> ltanList
            )
        {
            lnorList = new List<Vector3>();
            ltanList = new List<Vector3>();

            int vcnt = vlist.Length;
            int tcnt = triangles.Length / 3;

            for (int i = 0; i < vcnt; i++)
            {
                lnorList.Add(Vector3.zero);
                ltanList.Add(Vector3.zero);
            }

            // 法線算出
            for (int i = 0; i < tcnt; i++)
            {
                int index = i * 3;

                int i1 = triangles[index];
                int i2 = triangles[index + 1];
                int i3 = triangles[index + 2];

                var v1 = vlist[i1];
                var v2 = vlist[i2];
                var v3 = vlist[i3];

                var w1 = uvlist[i1];
                var w2 = uvlist[i2];
                var w3 = uvlist[i3];

                // トライアングル頂点がすべて使用されている場合のみ計算する
                if (selectList != null)
                {
                    var use1 = selectList[i1];
                    var use2 = selectList[i2];
                    var use3 = selectList[i3];
                    if (use1 == 0 || use2 == 0 || use3 == 0)
                        continue;
                }

                // 法線
                Vector3 vn1 = v2 - v1;
                Vector3 vn2 = v3 - v1;
                vn1 *= 1000;
                vn2 *= 1000;
                Vector3 nor = Vector3.Cross(vn1, vn2).normalized;

                lnorList[i1] = lnorList[i1] + nor;
                lnorList[i2] = lnorList[i2] + nor;
                lnorList[i3] = lnorList[i3] + nor;

                // 接線
                Vector3 distBA = v2 - v1;
                Vector3 distCA = v3 - v1;
                Vector2 tdistBA = w2 - w1;
                Vector2 tdistCA = w3 - w1;

                float area = tdistBA.x * tdistCA.y - tdistBA.y * tdistCA.x;
                Vector3 tan = Vector3.zero;
                if (area == 0.0f)
                {
                    // error!
                    Debug.LogError("area = 0!");
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

                ltanList[i1] = ltanList[i1] + tan;
                ltanList[i2] = ltanList[i2] + tan;
                ltanList[i3] = ltanList[i3] + tan;

            }

            // 正規化
            for (int i = 0; i < vcnt; i++)
            {
                if (lnorList[i] != Vector3.zero && ltanList[i] != Vector3.zero)
                {
                    ltanList[i] = ltanList[i].normalized;
                    lnorList[i] = lnorList[i].normalized;
                }
                else
                {
                    // この頂点の法線接線は無効なのでデフォルト値
                    ltanList[i] = new Vector3(0, 1, 0);
                    lnorList[i] = new Vector4(1, 0, 0, 1);
                }
            }

            return true;
        }

#if false
        /// <summary>
        /// 本来のメッシュ法線/接線とアルゴリズムで算出した法線/接線を補間する回転を頂点ごとに求めて返す
        /// 回転はアルゴリズム法線/接線のローカル空間での、本来姿勢への回転値。
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="vrotList"></param>
        /// <returns></returns>
        public static bool CalcMeshAdjustVertexRotation(
            Vector3[] vlist, Vector3[] nlist, Vector4[] tlist, Vector2[] uvlist, int[] triangles,
            out Quaternion[] vrotList)
        {
            vrotList = null;

            int vcnt = vlist.Length;

            // アルゴリズム計算のローカル法線／接線を取得する
            List<Vector3> lnorList;
            List<Vector3> ltanList;
            if (CalcMeshLocalNormalTangent(vlist, uvlist, triangles, out lnorList, out ltanList) == false)
                return false;

            // 各頂点においてアルゴリズム計算の法線をオリジナル法線に合わせる回転を求める
            vrotList = new Quaternion[vcnt];
            for (int i = 0; i < vcnt; i++)
            {
                // オリジナル
                var q1 = Quaternion.LookRotation(nlist[i], tlist[i]);

                // アルゴリズム
                var q2 = Quaternion.LookRotation(lnorList[i], ltanList[i]);

                // オリジナル法線/接線をアルゴリズムでの法線/接線空間に投影する
                Quaternion iq2 = Quaternion.Inverse(q2);
                var n = iq2 * (q1 * Vector3.forward);
                var t = iq2 * (q1 * Vector3.up);

                // アルゴリズム空間でのオリジナル姿勢へ戻す回転
                Quaternion q = Quaternion.LookRotation(n, t);

                vrotList[i] = q;
            }

            return true;
        }
#endif

        /// <summary>
        /// ライン／トライアングル情報を分解して各頂点の接続頂点をリストにして返す
        /// </summary>
        /// <param name="vcnt"></param>
        /// <param name="triangleList"></param>
        /// <returns></returns>
        public static List<HashSet<int>> GetTriangleToVertexLinkList(int vcnt, List<int> lineList, List<int> triangleList)
        {
            List<HashSet<int>> vlink = new List<HashSet<int>>();
            for (int i = 0; i < vcnt; i++)
            {
                vlink.Add(new HashSet<int>());
            }

            if (lineList != null && lineList.Count > 0)
            {
                int lcnt = lineList.Count / 2;
                for (int i = 0; i < lcnt; i++)
                {
                    int index = i * 2;

                    int v1 = lineList[index];
                    int v2 = lineList[index + 1];

                    vlink[v1].Add(v2);
                    vlink[v2].Add(v1);
                }
            }

            if (triangleList != null && triangleList.Count > 0)
            {
                int tcnt = triangleList.Count / 3;
                for (int i = 0; i < tcnt; i++)
                {
                    int index = i * 3;

                    int v1 = triangleList[index];
                    int v2 = triangleList[index + 1];
                    int v3 = triangleList[index + 2];

                    vlink[v1].Add(v2);
                    vlink[v1].Add(v3);

                    vlink[v2].Add(v1);
                    vlink[v2].Add(v3);

                    vlink[v3].Add(v1);
                    vlink[v3].Add(v2);
                }
            }

            return vlink;
        }

        /// <summary>
        /// ラインペア情報を分解して各頂点の接続頂点をリストにして返す
        /// </summary>
        /// <param name="vcnt"></param>
        /// <param name="lineSet"></param>
        /// <returns></returns>
        public static List<HashSet<int>> GetVertexLinkList(int vcnt, HashSet<uint> lineSet)
        {
            List<HashSet<int>> vlink = new List<HashSet<int>>();
            for (int i = 0; i < vcnt; i++)
            {
                vlink.Add(new HashSet<int>());
            }

            foreach (var pair in lineSet)
            {
                int v0, v1;
                DataUtility.UnpackPair(pair, out v0, out v1);
                vlink[v0].Add(v1);
                vlink[v1].Add(v0);
            }

            return vlink;
        }

        /// <summary>
        /// 頂点が接続するトライアングルを辞書にして返す
        /// </summary>
        /// <param name="triangleList"></param>
        /// <returns></returns>
        public static Dictionary<int, HashSet<int>> GetVertexToTriangles(List<int> triangleList)
        {
            var vtdict = new Dictionary<int, HashSet<int>>();

            for (int i = 0; i < triangleList.Count; i++)
            {
                int vindex = triangleList[i];
                int tindex = i / 3;

                if (vtdict.ContainsKey(vindex) == false)
                    vtdict.Add(vindex, new HashSet<int>());
                vtdict[vindex].Add(tindex);
            }

            return vtdict;
        }

        /// <summary>
        /// ポリゴン番号とその２つの頂点を与え、残り１つの頂点番号を返す
        /// </summary>
        /// <param name="tindex"></param>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <param name="triangleList"></param>
        /// <returns></returns>
        public static int RestTriangleVertex(int tindex, int v0, int v1, List<int> triangleList)
        {
            int index = tindex * 3;
            for (int i = 0; i < 3; i++)
            {
                int n = triangleList[index + i];
                if (n != v0 && n != v1)
                    return n;
            }

            return 0;
        }

        /// <summary>
        /// トライアングル番頭とその１つの頂点を与え、残りの２つの頂点番号を返す
        /// </summary>
        /// <param name="tindex"></param>
        /// <param name="v0"></param>
        /// <param name="triangleList"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        public static void RestTriangleVertex(int tindex, int v0, List<int> triangleList, out int v1, out int v2)
        {
            int index = tindex * 3;
            int n0 = triangleList[index];
            int n1 = triangleList[index + 1];
            int n2 = triangleList[index + 2];

            if (n0 == v0)
            {
                v1 = n1;
                v2 = n2;
            }
            else if (n1 == v0)
            {
                v1 = n0;
                v2 = n2;
            }
            else if (n2 == v0)
            {
                v1 = n0;
                v2 = n1;
            }
            else
            {
                Debug.LogError("RestTriangleVertex() failed!");
                v1 = -1;
                v2 = -1;
            }
        }

        /// <summary>
        /// ２つのトライアングルが隣接しているか判定する
        /// </summary>
        /// <param name="tindex0"></param>
        /// <param name="tindex1"></param>
        /// <param name="triangleList"></param>
        /// <returns></returns>
        public static bool CheckAdjacentTriangle(int tindex0, int tindex1, List<int> triangleList)
        {
            int index0 = tindex0 * 3;
            int index1 = tindex1 * 3;
            int cnt = 0;
            for (int i = 0; i < 3; i++, index0++)
            {
                int v = triangleList[index0];
                for (int j = 0; j < 3; j++)
                {
                    if (v == triangleList[index1 + j])
                        cnt++;
                }
            }

            return cnt >= 2;
        }

        /// <summary>
        /// エッジをキーとして隣接するトライアングルインデックスを辞書に変換して返す
        /// </summary>
        /// <param name="vcnt"></param>
        /// <param name="triangleList"></param>
        /// <returns></returns>
        public static Dictionary<uint, List<int>> GetTriangleEdgePair(List<int> triangleList)
        {
            Dictionary<uint, List<int>> triangleEdgeDict = new Dictionary<uint, List<int>>();

            if (triangleList != null && triangleList.Count >= 3)
            {

                // すべてのトライアングル
                int tcnt = triangleList.Count / 3;
                for (int i = 0; i < tcnt; i++)
                {
                    int tindex = i * 3;

                    int v0 = triangleList[tindex];
                    int v1 = triangleList[tindex + 1];
                    int v2 = triangleList[tindex + 2];

                    // エッジにトライアングルを追加する
                    AddTriangleEdge(v0, v1, i, triangleEdgeDict);
                    AddTriangleEdge(v0, v2, i, triangleEdgeDict);
                    AddTriangleEdge(v1, v2, i, triangleEdgeDict);
                }
            }

            return triangleEdgeDict;
        }

        // エッジに隣接するトライアングルを追加する
        static void AddTriangleEdge(int v0, int v1, int tindex, Dictionary<uint, List<int>> triangleEdgeDict)
        {
            // 頂点v0/v1をパックする
            uint pack = DataUtility.PackPair(v0, v1);

            List<int> tlist;
            if (triangleEdgeDict.ContainsKey(pack))
            {
                // 既存
                tlist = triangleEdgeDict[pack];
            }
            else
            {
                // 新規
                tlist = new List<int>();
                triangleEdgeDict.Add(pack, tlist);
            }

            // 追加
            tlist.Add(tindex);
        }

        /// <summary>
        /// トライアングルリストをulongにパッキングして返す
        /// </summary>
        /// <param name="triangleList"></param>
        /// <returns></returns>
        public static List<ulong> GetTrianglePackList(List<int> triangleList)
        {
            var packList = new List<ulong>();
            if (triangleList != null && triangleList.Count > 0)
            {
                int cnt = triangleList.Count / 3;
                for (int i = 0; i < cnt; i++)
                {
                    int index = i * 3;
                    int v0 = triangleList[index];
                    int v1 = triangleList[index + 1];
                    int v2 = triangleList[index + 2];
                    ulong pack = DataUtility.PackTriple(v0, v1, v2);
                    packList.Add(pack);
                }
            }

            return packList;
        }

        /// <summary>
        /// 指定ボーンのすべての子へのラインに対して最近接点距離を返す
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="bone"></param>
        /// <param name="lineWidth"></param>
        /// <returns></returns>
        public static float ClosestPtBoneLine(Vector3 pos, Transform bone, float lineWidth, out Vector3 d)
        {
            float mindist = 10000;
            d = bone.position;

            // 子がいない場合はそのまま
            if (bone.childCount == 0)
            {
                mindist = Mathf.Max(Vector3.Distance(pos, bone.position) - lineWidth, 0.0f);
                return mindist;
            }

            // すべての子へのラインに対して判定
            for (int i = 0; i < bone.childCount; i++)
            {
                var child = bone.GetChild(i);

                var spos = bone.position;
                var epos = child.position;

                var w = MathUtility.ClosestPtPointSegment(pos, spos, epos);

                float dist = Mathf.Max(Vector3.Distance(pos, w) - lineWidth, 0.0f);

                //mindist = Mathf.Min(dist, mindist);
                if (dist < mindist)
                {
                    mindist = dist;
                    d = w;
                }
            }

            return mindist;
        }

        /// <summary>
        /// スキンメッシュから実際に利用しているボーントランスフォームのみリスト化して返す
        /// </summary>
        /// <param name="bones"></param>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static List<Transform> GetUseBoneTransformList(Transform[] bones, Mesh mesh)
        {
            List<Transform> boneTransformList = new List<Transform>();

            if (mesh)
            {
                // use bone index
                List<int> boneIndexList = new List<int>();
                HashSet<int> useBoneSet = new HashSet<int>();
                BoneWeight[] bws = mesh.boneWeights;
                foreach (var bw in bws)
                {
                    if (bw.weight0 > 0.0f)
                        useBoneSet.Add(bw.boneIndex0);
                    if (bw.weight1 > 0.0f)
                        useBoneSet.Add(bw.boneIndex1);
                    if (bw.weight2 > 0.0f)
                        useBoneSet.Add(bw.boneIndex2);
                    if (bw.weight3 > 0.0f)
                        useBoneSet.Add(bw.boneIndex3);
                }

                // use transform
                foreach (var index in useBoneSet)
                {
                    boneTransformList.Add(bones[index]);
                }
            }

            return boneTransformList;
        }

        //=========================================================================================
        // テトラメッシュ
        //=========================================================================================
        private class TetraVertex
        {
            public int index;
            public Vector3 pos;

            public TetraVertex() { }
            public TetraVertex(Vector3 pos, int index)
            {
                this.pos = pos;
                this.index = index;
            }
        }

        private class Tetra
        {
            public List<TetraVertex> vertexList = new List<TetraVertex>();

            // 外接円
            public Vector3 circumCenter;
            public float circumRadius;

            // 重心と重心からの最大距離
            public Vector3 tetraCenter;
            public float tetraSize;

            public Tetra()
            {
            }

            public Tetra(TetraVertex a, TetraVertex b, TetraVertex c, TetraVertex d)
            {
                vertexList.Add(a);
                vertexList.Add(b);
                vertexList.Add(c);
                vertexList.Add(d);

                //CalcCircumcircle();
                CalcSize();
            }

            public ulong GetTetraHash()
            {
                return DataUtility.PackQuater(vertexList[0].index, vertexList[1].index, vertexList[2].index, vertexList[3].index);
            }

            /// <summary>
            /// テトラの外接円と半径を求める
            /// https://qiita.com/kkttm530/items/d32bad84a6a7f0d8d7e7
            /// からだけどdeterminantの計算は間違ってるっぽいのでmathの関数を使用する
            /// </summary>
            public void CalcCircumcircle()
            {
                var p1 = vertexList[0].pos;
                var p2 = vertexList[1].pos;
                var p3 = vertexList[2].pos;
                var p4 = vertexList[3].pos;

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

            public bool ContainsPoint(TetraVertex p1)
            {
                return vertexList.Contains(p1);
            }

            public bool ContainsPoint(TetraVertex p1, TetraVertex p2, TetraVertex p3, TetraVertex p4)
            {
                return vertexList.Contains(p1) || vertexList.Contains(p2) || vertexList.Contains(p3) || vertexList.Contains(p4);
            }

            /// <summary>
            /// 重心と重心からの最大距離を計算する
            /// </summary>
            public void CalcSize()
            {
                var wpos0 = vertexList[0].pos;
                var wpos1 = vertexList[1].pos;
                var wpos2 = vertexList[2].pos;
                var wpos3 = vertexList[3].pos;
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
                var wpos0 = vertexList[0].pos;
                var wpos1 = vertexList[1].pos;
                var wpos2 = vertexList[2].pos;
                var wpos3 = vertexList[3].pos;
                var n = Vector3.Cross(wpos0 - wpos1, wpos0 - wpos2);
                if (n.magnitude < 0.00001f)
                    return false;
                n.Normalize();
                var v = wpos3 - wpos0;
                var h = Vector3.Dot(n, v);
                if (Mathf.Abs(h) < (tetraSize * 0.2f))
                    return false;

                return true;
            }
        }

        /// <summary>
        /// テトラメッシュを構築して返す
        /// </summary>
        /// <param name="posList"></param>
        /// <param name="tetraCount">作成されたテトラ数</param>
        /// <param name="tetraIndexList">テトラ数ｘ４のインデックスリスト</param>
        /// <param name="tetraSizeList">テトラの重心からの最大距離リスト</param>
        public static void CalcTetraMesh(List<Vector3> posList, out int tetraCount, out List<int> tetraIndexList, out List<float> tetraSizeList)
        {
            tetraCount = 0;
            tetraIndexList = new List<int>();
            tetraSizeList = new List<float>();

            // 作業用バッファ
            List<TetraVertex> vertexList = new List<TetraVertex>();
            for (int i = 0; i < posList.Count; i++)
            {
                vertexList.Add(new TetraVertex(posList[i], i));
            }

            // 入力頂点のバウンディングボックス
            Bounds b = new Bounds(posList[0], Vector3.one * 0.01f);
            foreach (var pos in posList)
            {
                b.Encapsulate(pos);
            }

            // ポイントをすべて内包するテトラポイントを追加する
            float areaRadius = Mathf.Max(Mathf.Max(b.extents.x, b.extents.y), b.extents.z);
            float dist = areaRadius * 100.0f;
            var tempSV0 = new TetraVertex();
            var tempSV1 = new TetraVertex();
            var tempSV2 = new TetraVertex();
            var tempSV3 = new TetraVertex();
            tempSV0.pos = b.center + new Vector3(0.0f, -dist, 0.0f);
            tempSV1.pos = b.center + new Vector3(-dist, dist, dist);
            tempSV2.pos = b.center + new Vector3(dist, dist, dist);
            tempSV3.pos = b.center + new Vector3(0.0f, dist, -dist);
            int svcnt = vertexList.Count;
            tempSV0.index = svcnt++;
            tempSV1.index = svcnt++;
            tempSV2.index = svcnt++;
            tempSV3.index = svcnt++;
            vertexList.Add(tempSV0);
            vertexList.Add(tempSV1);
            vertexList.Add(tempSV2);
            vertexList.Add(tempSV3);

            // 最初のテトラを分割テトラとして登録
            List<Tetra> divideTetras = new List<Tetra>();
            var tetra0 = new Tetra(tempSV0, tempSV1, tempSV2, tempSV3);
            tetra0.CalcCircumcircle();
            divideTetras.Add(tetra0);

            // 重複チェック用
            Dictionary<ulong, Tetra> useTetraHash = new Dictionary<ulong, Tetra>();
            useTetraHash.Add(tetra0.GetTetraHash(), tetra0);

            // テトラ構築
            for (int k = 0; k < (vertexList.Count - 4); k++)
            {
                var point = vertexList[k];

                List<Tetra> tempDivTetras = new List<Tetra>();

                for (int i = 0; i < divideTetras.Count;)
                {
                    var tetra = divideTetras[i];
                    if (tetra.ContainsPoint(point) == false)
                    {
                        if (tetra.IntersectCircumcircle(point.pos))
                        {
                            // 再分割
                            var tetra1 = new Tetra(tetra.vertexList[0], tetra.vertexList[1], tetra.vertexList[2], point);
                            var tetra2 = new Tetra(tetra.vertexList[0], tetra.vertexList[2], tetra.vertexList[3], point);
                            var tetra3 = new Tetra(tetra.vertexList[0], tetra.vertexList[3], tetra.vertexList[1], point);
                            var tetra4 = new Tetra(tetra.vertexList[1], tetra.vertexList[2], tetra.vertexList[3], point);

                            tempDivTetras.Add(tetra1);
                            tempDivTetras.Add(tetra2);
                            tempDivTetras.Add(tetra3);
                            tempDivTetras.Add(tetra4);

                            useTetraHash.Remove(tetra.GetTetraHash());
                            divideTetras.RemoveAt(i);
                            continue;
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
            vertexList.Remove(tempSV0);
            vertexList.Remove(tempSV1);
            vertexList.Remove(tempSV2);
            vertexList.Remove(tempSV3);

            // テトラの検証
            for (int i = 0; i < divideTetras.Count;)
            {
                var tetra = divideTetras[i];
                if (tetra.Verification() == false)
                {
                    divideTetras.RemoveAt(i);
                    continue;
                }

                i++;
            }

            // 最終結果を格納
            tetraCount = divideTetras.Count;
            foreach (var tetra in divideTetras)
            {
                for (int i = 0; i < 4; i++)
                    tetraIndexList.Add(tetra.vertexList[i].index);

                tetraSizeList.Add(tetra.tetraSize);
            }
        }

        /// <summary>
        /// 現在のボーンを置換辞書から置き換えて返す
        /// 辞書に登録されていない場合は、現在のボーンをそのまま返す
        /// </summary>
        /// <param name="now"></param>
        /// <param name="boneReplaceDict"></param>
        /// <returns></returns>
        public static Transform GetReplaceBone<T>(Transform now, Dictionary<T, Transform> boneReplaceDict) where T : class
        {
            if (typeof(T) == typeof(string))
                return boneReplaceDict.ContainsKey(now.name as T) ? boneReplaceDict[now.name as T] : now;
            else
                return boneReplaceDict.ContainsKey(now as T) ? boneReplaceDict[now as T] : now;
        }
    }
}
