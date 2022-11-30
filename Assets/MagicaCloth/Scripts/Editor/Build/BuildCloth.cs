// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    public static partial class BuildManager
    {
        //=========================================================================================
        // Bone Cloth
        //=========================================================================================
        static Define.Error CreateBoneCloth(CoreComponent core, SerializedObject serializedObject, string savePrefabPath)
        {
            MagicaBoneCloth scr = core as MagicaBoneCloth;
            Define.Error result = Define.Error.None;

            // データチェック
            if (scr.ClothSelection == null)
                return Define.Error.BuildMissingSelection;

            // チームハッシュ設定
            scr.TeamData.ValidateColliderList();

            // メッシュデータ作成
            if ((result = CreateBoneCloth_MeshData(scr, serializedObject, savePrefabPath)) != Define.Error.None)
                return result;

            // クロスデータ作成
            if ((result = CreateBoneCloth_ClothData(scr, serializedObject, savePrefabPath)) != Define.Error.None)
                return result;

            // 検証
            scr.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();
            result = scr.VerifyData();
            return result;
        }

        /// <summary>
        /// メッシュデータ作成
        /// </summary>
        static Define.Error CreateBoneCloth_MeshData(MagicaBoneCloth scr, SerializedObject serializedObject, string savePrefabPath)
        {
            // トランスフォームリスト作成
            var transformList = scr.GetTransformList();
            if (transformList.Count == 0)
                return Define.Error.BuildNoTransformList;

            // 共有データオブジェクト作成
            //string dataname = "BoneClothMeshData_" + scr.name;
            //MeshData mdata = ShareDataObject.CreateShareData<MeshData>(dataname);
            var mdata = CreateShareData<MeshData>("BoneClothMeshData_" + scr.name, savePrefabPath);

            // 頂点作成
            int vcnt = transformList.Count;
            List<Vector3> wposList = new List<Vector3>();
            List<Vector3> wnorList = new List<Vector3>();
            List<Vector4> wtanList = new List<Vector4>();
            List<Vector3> lposList = new List<Vector3>();
            List<Vector3> lnorList = new List<Vector3>();
            List<Vector3> ltanList = new List<Vector3>();
            Transform myt = scr.transform;
            for (int i = 0; i < transformList.Count; i++)
            {
                var t = transformList[i];

                // 頂点追加
                var pos = t.position;
                var lpos = myt.InverseTransformDirection(pos - myt.position);
                var lnor = myt.InverseTransformDirection(t.forward);
                var ltan = myt.InverseTransformDirection(t.up);
                wposList.Add(pos);
                wnorList.Add(t.forward);
                wtanList.Add(t.up);
                lposList.Add(lpos);
                lnorList.Add(lnor);
                ltanList.Add(ltan);
            }
            var vertexInfoList = new List<uint>();
            var vertexWeightList = new List<MeshData.VertexWeight>();
            for (int i = 0; i < lposList.Count; i++)
            {
                // １ウエイトで追加
                uint vinfo = DataUtility.Pack4_28(1, i);
                vertexInfoList.Add(vinfo);
            }
            mdata.vertexInfoList = vertexInfoList.ToArray();
            mdata.vertexWeightList = vertexWeightList.ToArray();
            mdata.vertexCount = lposList.Count;

            // デプスリスト作成
            var sel = scr.ClothSelection.GetSelectionData(null, null);
            List<int> depthList = new List<int>();
            for (int i = 0; i < transformList.Count; i++)
            {
                int depth = 0;
                var t = transformList[i];

                while (t && transformList.Contains(t))
                {
                    int index = transformList.IndexOf(t);

                    if (index < 0 || index >= sel.Count)
                    {
                        // err
                        return Define.Error.BuildInvalidSelection;
                    }

                    if (sel[index] != SelectionData.Move)
                        break;

                    depth++;
                    t = t.parent;
                }

                depthList.Add(depth);
                //Debug.Log($"[{transformList[i].name}] depth:{depth}");
            }


            // トランスフォームの構造ライン（通常は縦）をつなぐ
            HashSet<uint> lineSet = new HashSet<uint>();
            for (int i = 0; i < transformList.Count; i++)
            {
                var t = transformList[i];
                var pt = t.parent;
                if (pt != null && transformList.Contains(pt))
                {
                    int v0 = i;
                    int v1 = transformList.IndexOf(pt);
                    uint pair = DataUtility.PackPair(v0, v1);
                    lineSet.Add(pair);
                }
            }

            // 同じ深さの横ラインをつなぐ
            List<int> triangleList = new List<int>();
            HashSet<uint> triangleLineSet = new HashSet<uint>(lineSet);
            if (scr.ClothTarget.Connection == BoneClothTarget.ConnectionMode.MeshAutomatic)
            {
                // 最寄り頂点を接続する方法（従来）
                // 周りのボーンを調べ一定範囲内のボーンを接続する
                for (int i = 0; i < transformList.Count; i++)
                {
                    var t = transformList[i];
                    int depth = depthList[i];
                    float mindist = 10000.0f;

                    List<int> linkList = new List<int>();
                    List<float> distList = new List<float>();

                    // 同じ深さ（横）を仮接続する
                    for (int j = 0; j < transformList.Count; j++)
                    {
                        if (i == j || depthList[j] != depth)
                            continue;

                        linkList.Add(j);
                        var dist = Vector3.Distance(t.position, transformList[j].position);
                        distList.Add(dist);
                        mindist = Mathf.Min(mindist, dist);
                    }

                    // 最短距離より少し長めの範囲の頂点以外は削除する
                    HashSet<int> removeSet = new HashSet<int>();
                    mindist *= 1.5f;
                    for (int j = 0; j < linkList.Count; j++)
                    {
                        if (distList[j] > mindist)
                            removeSet.Add(j);
                    }

                    // 角度が一定以内ならば最も近い接続以外を削除する
                    for (int j = 0; j < linkList.Count - 1; j++)
                    {
                        for (int k = j + 1; k < linkList.Count; k++)
                        {
                            if (removeSet.Contains(j))
                                continue;
                            if (removeSet.Contains(k))
                                continue;

                            int index0 = linkList[j];
                            int index1 = linkList[k];

                            var ang = Vector3.Angle(transformList[index0].position - t.position, transformList[index1].position - t.position);
                            if (ang <= 45.0f)
                            {
                                removeSet.Add(distList[j] < distList[k] ? k : j);
                            }
                        }
                    }

                    // 登録
                    for (int j = 0; j < linkList.Count; j++)
                    {
                        if (removeSet.Contains(j))
                            continue;
                        // 接続する
                        uint pair = DataUtility.PackPair(i, linkList[j]);
                        triangleLineSet.Add(pair);
                    }
                }

                // トライアングル生成
                if (triangleLineSet.Count > 0)
                {
                    // 一旦各頂点の接続頂点リストを取得
                    List<HashSet<int>> vlink = MeshUtility.GetVertexLinkList(mdata.vertexCount, triangleLineSet);

                    // トライアングル情報作成
                    HashSet<ulong> registTriangleSet = new HashSet<ulong>();
                    for (int i = 0; i < vlink.Count; i++)
                    {
                        HashSet<int> linkset = vlink[i];
                        var t = transformList[i];
                        var move = sel[i] == SelectionData.Move;

                        foreach (var j in linkset)
                        {
                            var t2 = transformList[j];
                            var v = (t2.position - t.position).normalized;
                            var move2 = sel[j] == SelectionData.Move;

                            foreach (var k in linkset)
                            {
                                if (j == k)
                                    continue;

                                // j-kのエッジがtriangleLineSetに含まれていない場合は無効
                                //if (triangleLineSet.Contains(DataUtility.PackPair(j, k)) == false)
                                //    continue;

                                var t3 = transformList[k];
                                var v2 = (t3.position - t.position).normalized;
                                var move3 = sel[k] == SelectionData.Move;

                                // すべて固定頂点なら無効
                                if (move == false && move2 == false && move3 == false)
                                    continue;

                                // 面積が０のトライアングルは除外する
                                var n = Vector3.Cross(t2.position - t.position, t3.position - t.position);
                                var clen = n.magnitude;
                                if (clen < 1e-06f)
                                {
                                    //Debug.Log($"clen == 0 ({i},{j},{k})");
                                    continue;
                                }

                                var ang = Vector3.Angle(v, v2); // deg
                                if (ang <= 100)
                                {
                                    // i - j - k をトライアングルとして登録する
                                    var thash = DataUtility.PackTriple(i, j, k);
                                    if (registTriangleSet.Contains(thash) == false)
                                    {
                                        triangleList.Add(i);
                                        triangleList.Add(j);
                                        triangleList.Add(k);
                                        registTriangleSet.Add(thash);
                                    }
                                }
                            }
                        }
                    }
                }

            }
            else if (scr.ClothTarget.Connection == BoneClothTarget.ConnectionMode.MeshSequentialLoop || scr.ClothTarget.Connection == BoneClothTarget.ConnectionMode.MeshSequentialNoLoop)
            {
                // 登録ライン順に接続する方法
                bool loop = scr.ClothTarget.Connection == BoneClothTarget.ConnectionMode.MeshSequentialLoop;
                int maxLevel;
                List<List<List<Transform>>> grid = scr.GetTransformGrid(out maxLevel);
                if (maxLevel > 0)
                {
                    HashSet<ulong> registTriangleSet = new HashSet<ulong>();

                    for (int x = 0; x < grid.Count; x++)
                    {
                        // このラインの左右ラインインデックス
                        int leftx = loop ? (x + grid.Count - 1) % grid.Count : x - 1;
                        int rightx = loop ? (x + 1) % grid.Count : x + 1;

                        for (int lv = 0; lv < grid[x].Count; lv++)
                        {
                            for (int k = 0; k < grid[x][lv].Count; k++)
                            {
                                var t = grid[x][lv][k];
                                int index = transformList.IndexOf(t);

                                // 自身の左側
                                Transform leftt = null;
                                if (k == 0)
                                {
                                    if (leftx >= 0 && leftx != x)
                                    {
                                        if (lv < grid[leftx].Count)
                                        {
                                            int l = grid[leftx][lv].Count;
                                            leftt = grid[leftx][lv][l - 1];
                                        }
                                    }
                                }
                                else
                                {
                                    leftt = grid[x][lv][k - 1];
                                }
                                int leftIndex = transformList.IndexOf(leftt);

                                // 自身の右側
                                Transform rightt = null;
                                if (k == grid[x][lv].Count - 1)
                                {
                                    if (rightx < grid.Count && rightx != x)
                                    {
                                        if (lv < grid[rightx].Count)
                                        {
                                            rightt = grid[rightx][lv][0];
                                        }
                                    }
                                }
                                else
                                {
                                    rightt = grid[x][lv][k + 1];
                                }
                                int rightIndex = transformList.IndexOf(rightt);

                                // 親
                                Transform pt = t.parent;
                                int parentIndex = transformList.IndexOf(pt);

                                // トライアングル形成
                                // (1)自身-親-左
                                if (parentIndex >= 0 && leftIndex >= 0)
                                {
                                    var thash = DataUtility.PackTriple(index, parentIndex, leftIndex);
                                    if (registTriangleSet.Contains(thash) == false)
                                    {
                                        triangleList.Add(index);
                                        triangleList.Add(parentIndex);
                                        triangleList.Add(leftIndex);
                                        registTriangleSet.Add(thash);
                                    }
                                }

                                // (2)自身-親-右
                                if (parentIndex >= 0 && rightIndex >= 0)
                                {
                                    var thash = DataUtility.PackTriple(index, parentIndex, rightIndex);
                                    if (registTriangleSet.Contains(thash) == false)
                                    {
                                        triangleList.Add(index);
                                        triangleList.Add(parentIndex);
                                        triangleList.Add(rightIndex);
                                        registTriangleSet.Add(thash);
                                    }
                                }

                                //Debug.Log($"[{t.name}] x:{x} lv:{lv} k:{k}");
                            }
                        }
                    }
                }
            }

            // トライアングルの法線を揃える、また不要なトライアングルを除去する
            HashSet<ulong> triangleSet = new HashSet<ulong>();
            if (triangleList.Count > 0)
            {
                // リダクションメッシュを作成する
                // ただ現在は面法線を揃える用途にしか使用しない
                var reductionMesh = new MagicaReductionMesh.ReductionMesh();
                reductionMesh.WeightMode = MagicaReductionMesh.ReductionMesh.ReductionWeightMode.Distance;
                reductionMesh.MeshData.MaxWeightCount = 1;
                reductionMesh.MeshData.WeightPow = 1;
                reductionMesh.MeshData.SameSurfaceAngle = scr.ClothTarget.SameSurfaceAngle; // 80?
                reductionMesh.AddMesh(myt, wposList, wnorList, wtanList, null, triangleList);

                // リダクション（面法線を整えるだけでリダクションは行わない）
                reductionMesh.Reduction(0.0f, 0.0f, 0.0f, false);

                // 最終メッシュデータ取得
                var final = reductionMesh.GetFinalData(myt);
                Debug.Assert(vcnt == final.VertexCount);

                // トライアングルデータ取得
                triangleList = final.triangles;
            }

            // 近接ライン接続
            //if (scr.ClothTarget.LineConnection)
            //{
            //    CreateNearLine(scr, lineSet, wposList, mdata);
            //}

#if false
            // トライアングル接続されているエッジはラインセットから削除する
            for (int i = 0; i < triangleList.Count / 3; i++)
            {
                int v0, v1, v2;
                int index = i * 3;
                v0 = triangleList[index];
                v1 = triangleList[index + 1];
                v2 = triangleList[index + 2];

                var pair0 = DataUtility.PackPair(v0, v1);
                var pair1 = DataUtility.PackPair(v1, v2);
                var pair2 = DataUtility.PackPair(v2, v0);

                lineSet.Remove(pair0);
                lineSet.Remove(pair1);
                lineSet.Remove(pair2);
            }
#endif

            // todo:test
            //lineSet.Clear();

            // ライン格納
            if (lineSet.Count > 0)
            {
                List<int> lineList = new List<int>();
                foreach (var pair in lineSet)
                {
                    int v0, v1;
                    DataUtility.UnpackPair(pair, out v0, out v1);
                    lineList.Add(v0);
                    lineList.Add(v1);
                }
                mdata.lineList = lineList.ToArray();
                mdata.lineCount = lineList.Count / 2;
            }

            // トライアングル格納
            //if (triangleSet.Count > 0)
            //{
            //    List<int> triangleList = new List<int>();
            //    foreach (var tpack in triangleSet)
            //    {
            //        int v0, v1, v2;
            //        DataUtility.UnpackTriple(tpack, out v0, out v1, out v2);
            //        triangleList.Add(v0);
            //        triangleList.Add(v1);
            //        triangleList.Add(v2);
            //    }
            //    mdata.triangleCount = triangleSet.Count;
            //    mdata.triangleList = triangleList.ToArray();
            //}
            if (triangleList.Count > 0)
            {
                mdata.triangleCount = triangleList.Count / 3;
                mdata.triangleList = triangleList.ToArray();
            }

            serializedObject.FindProperty("meshData").objectReferenceValue = mdata;
            serializedObject.ApplyModifiedProperties();

            // 使用トランスフォームシリアライズ
            var property = serializedObject.FindProperty("useTransformList");
            var propertyPos = serializedObject.FindProperty("useTransformPositionList");
            var propertyRot = serializedObject.FindProperty("useTransformRotationList");
            var propertyScl = serializedObject.FindProperty("useTransformScaleList");
            property.arraySize = transformList.Count;
            propertyPos.arraySize = transformList.Count;
            propertyRot.arraySize = transformList.Count;
            propertyScl.arraySize = transformList.Count;
            for (int i = 0; i < transformList.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = transformList[i];
                propertyPos.GetArrayElementAtIndex(i).vector3Value = transformList[i].localPosition;
                propertyRot.GetArrayElementAtIndex(i).quaternionValue = transformList[i].localRotation;
                propertyScl.GetArrayElementAtIndex(i).vector3Value = transformList[i].localScale;
            }
            serializedObject.ApplyModifiedProperties();

            // データ検証とハッシュ
            mdata.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(mdata);

            return Define.Error.None;
        }

        /// <summary>
        /// クロスデータ作成
        /// </summary>
        static Define.Error CreateBoneCloth_ClothData(MagicaBoneCloth scr, SerializedObject serializedObject, string savePrefabPath)
        {
            if (scr.MeshData == null)
                return Define.Error.BuildInvalidMeshData;

            // クロスデータ共有データ作成（既存の場合は選択状態のみコピーする）
            //string dataname = "BoneClothData_" + scr.name;
            //var cloth = ShareDataObject.CreateShareData<ClothData>(dataname);
            var cloth = CreateShareData<ClothData>("BoneClothData_" + scr.name, savePrefabPath);

            // クロスデータ作成
            cloth.CreateData(
                scr,
                scr.Params,
                scr.TeamData,
                scr.MeshData,
                scr,
                scr.ClothSelection.GetSelectionData(scr.MeshData, null)
                );
            serializedObject.FindProperty("clothData").objectReferenceValue = cloth;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(cloth);

            return Define.Error.None;
        }


        //=========================================================================================
        // Bone Spring
        //=========================================================================================
        static Define.Error CreateBoneSpring(CoreComponent core, SerializedObject serializedObject, string savePrefabPath)
        {
            var scr = core as MagicaBoneSpring;
            Define.Error result = Define.Error.None;

            // チームハッシュ設定
            scr.TeamData.ValidateColliderList();

            // メッシュデータ作成
            if ((result = CreateBoneSpring_MeshData(scr, serializedObject, savePrefabPath)) != Define.Error.None)
                return result;

            // クロスデータ作成
            if ((result = CreateBoneSpring_ClothData(scr, serializedObject, savePrefabPath)) != Define.Error.None)
                return result;

            // 検証
            scr.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();
            result = scr.VerifyData();

            return result;
        }

        /// <summary>
        /// メッシュデータ作成
        /// </summary>
        static Define.Error CreateBoneSpring_MeshData(MagicaBoneSpring scr, SerializedObject serializedObject, string savePrefabPath)
        {
            // トランスフォームリスト作成
            var transformList = scr.GetTransformList();
            if (transformList.Count == 0)
                return Define.Error.BuildNoTransformList;

            // 共有データオブジェクト作成
            //string dataname = "BoneSpringMeshData_" + scr.name;
            //MeshData mdata = ShareDataObject.CreateShareData<MeshData>(dataname);
            var mdata = CreateShareData<MeshData>("BoneSpringMeshData_" + scr.name, savePrefabPath);

            // 頂点作成
            List<Vector3> wposList = new List<Vector3>();
            List<Vector3> lposList = new List<Vector3>();
            List<Vector3> lnorList = new List<Vector3>();
            List<Vector3> ltanList = new List<Vector3>();
            Transform myt = scr.transform;
            for (int i = 0; i < transformList.Count; i++)
            {
                var t = transformList[i];

                // 頂点追加
                var pos = t.position;
                var lpos = myt.InverseTransformDirection(pos - myt.position);
                var lnor = myt.InverseTransformDirection(t.forward);
                var ltan = myt.InverseTransformDirection(t.up);
                wposList.Add(pos);
                lposList.Add(lpos);
                lnorList.Add(lnor);
                ltanList.Add(ltan);
            }
            var vertexInfoList = new List<uint>();
            var vertexWeightList = new List<MeshData.VertexWeight>();
            for (int i = 0; i < lposList.Count; i++)
            {
                // １ウエイトで追加
                uint vinfo = DataUtility.Pack4_28(1, i);
                vertexInfoList.Add(vinfo);
            }
            mdata.vertexInfoList = vertexInfoList.ToArray();
            mdata.vertexWeightList = vertexWeightList.ToArray();
            mdata.vertexCount = lposList.Count;

            // ライン作成
            HashSet<uint> lineSet = new HashSet<uint>();

            // 構造ライン
            for (int i = 0; i < transformList.Count; i++)
            {
                var t = transformList[i];
                var pt = t.parent;
                if (pt != null && transformList.Contains(pt))
                {
                    int v0 = i;
                    int v1 = transformList.IndexOf(pt);
                    uint pair = DataUtility.PackPair(v0, v1);
                    lineSet.Add(pair);
                }
            }

            // ライン格納
            List<int> lineList = new List<int>();
            foreach (var pair in lineSet)
            {
                int v0, v1;
                DataUtility.UnpackPair(pair, out v0, out v1);
                lineList.Add(v0);
                lineList.Add(v1);
            }
            mdata.lineList = lineList.ToArray();
            mdata.lineCount = lineList.Count / 2;

            serializedObject.FindProperty("meshData").objectReferenceValue = mdata;
            serializedObject.ApplyModifiedProperties();

            // 使用トランスフォームシリアライズ
            var property = serializedObject.FindProperty("useTransformList");
            var propertyPos = serializedObject.FindProperty("useTransformPositionList");
            var propertyRot = serializedObject.FindProperty("useTransformRotationList");
            var propertyScl = serializedObject.FindProperty("useTransformScaleList");
            property.arraySize = transformList.Count;
            propertyPos.arraySize = transformList.Count;
            propertyRot.arraySize = transformList.Count;
            propertyScl.arraySize = transformList.Count;
            for (int i = 0; i < transformList.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = transformList[i];
                propertyPos.GetArrayElementAtIndex(i).vector3Value = transformList[i].localPosition;
                propertyRot.GetArrayElementAtIndex(i).quaternionValue = transformList[i].localRotation;
                propertyScl.GetArrayElementAtIndex(i).vector3Value = transformList[i].localScale;
            }
            serializedObject.ApplyModifiedProperties();

            // データ検証とハッシュ
            mdata.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(mdata);

            return Define.Error.None;
        }

        /// <summary>
        /// クロスデータ作成
        /// </summary>
        static Define.Error CreateBoneSpring_ClothData(MagicaBoneSpring scr, SerializedObject serializedObject, string savePrefabPath)
        {
            if (scr.MeshData == null)
                return Define.Error.BuildInvalidMeshData;

            // クロスデータ共有データ作成（既存の場合は選択状態のみコピーする）
            //string dataname = "BoneSpringData_" + scr.name;
            //var cloth = ShareDataObject.CreateShareData<ClothData>(dataname);
            var cloth = CreateShareData<ClothData>("BoneSpringData_" + scr.name, savePrefabPath);

            // セレクトデータはすべて「移動」で受け渡す
            List<int> selectList = new List<int>();
            for (int i = 0; i < scr.MeshData.VertexCount; i++)
                selectList.Add(SelectionData.Move);

            // クロスデータ作成
            cloth.CreateData(
                scr,
                scr.Params,
                scr.TeamData,
                scr.MeshData,
                scr,
                selectList
                );
            serializedObject.FindProperty("clothData").objectReferenceValue = cloth;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(cloth);

            return Define.Error.None;
        }

        //=========================================================================================
        // Mesh Cloth
        //=========================================================================================
        static Define.Error CreateMeshCloth(CoreComponent core, SerializedObject serializedObject, string savePrefabPath)
        {
            var scr = core as MagicaMeshCloth;
            Define.Error result = Define.Error.None;

            // データチェック
            if (scr.Deformer == null)
                return Define.Error.BuildMissingDeformer;
            if (scr.ClothSelection == null)
                return Define.Error.BuildMissingSelection;

            // チームハッシュを設定
            scr.TeamData.ValidateColliderList();

            // クロスデータ共有データ作成
            //string dataname = "MeshClothData_" + scr.name;
            //var cloth = ShareDataObject.CreateShareData<ClothData>(dataname);
            var cloth = CreateShareData<ClothData>("MeshClothData_" + scr.name, savePrefabPath);

            // クロスデータ用にセレクションデータを拡張する
            // （１）無効頂点の隣接が移動／固定頂点なら拡張に変更する
            // （２）移動／固定頂点に影響する子頂点に接続する無効頂点は拡張に変更する
            var selection = scr.Deformer.MeshData.ExtendSelection(
                scr.ClothSelection.GetSelectionData(scr.Deformer.MeshData, scr.Deformer.GetRenderDeformerMeshList()),
                true,
                true
                );

            // クロスデータ作成
            cloth.CreateData(
                scr,
                scr.Params,
                scr.TeamData,
                scr.Deformer.MeshData,
                scr.Deformer,
                selection
                );

            // クロスデータを設定
            var cdata = serializedObject.FindProperty("clothData");
            cdata.objectReferenceValue = cloth;
            serializedObject.ApplyModifiedProperties();

            // 検証
            scr.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(cloth);
            result = scr.VerifyData();

            return result;
        }

        //=========================================================================================
        // Mesh Spring
        //=========================================================================================
        static Define.Error CreateMeshSpring(CoreComponent core, SerializedObject serializedObject, string savePrefabPath)
        {
            var scr = core as MagicaMeshSpring;
            Define.Error result = Define.Error.None;

            // データチェック
            if (scr.Deformer == null)
                return Define.Error.BuildMissingDeformer;

            // センタートランスフォーム
            if (scr.CenterTransform == null)
                serializedObject.FindProperty("centerTransform").objectReferenceValue = scr.transform;

            // デフォーマーリスト整理
            //scr.VerifyDeformer();

            // 共有データオブジェクト作成
            //SpringData sdata = ShareDataObject.CreateShareData<SpringData>("SpringData_" + scr.name);
            //serializedObject.ApplyModifiedProperties();
            var sdata = CreateShareData<SpringData>("SpringData_" + scr.name, savePrefabPath);

            CreateMeshSpring_ClothData(scr, sdata, scr.GetDeformer());

            // データ検証
            sdata.CreateVerifyData();

            // 新しいデータを設定
            serializedObject.FindProperty("springData").objectReferenceValue = sdata;
            serializedObject.ApplyModifiedProperties();

            // 仮想デフォーマーのハッシュを設定
            //var property = serializedObject.FindProperty("virtualDeformerHash");
            //property.intValue = scr.VirtualDeformerHash;
            //serializedObject.ApplyModifiedProperties();

            // データ検証
            scr.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(sdata);
            result = scr.VerifyData();

            return result;
        }

        static Define.Error CreateMeshSpring_ClothData(MagicaMeshSpring scr, SpringData sdata, BaseMeshDeformer deformer)
        {
            SpringData.DeformerData data = new SpringData.DeformerData();

            // 中心位置と方向
            var spos = scr.CenterTransform.position;
            var sdir = scr.CenterTransformDirection;
            var srot = scr.CenterTransform.rotation;
            var sscl = scr.Params.SpringRadiusScale;

            // 半径
            float sradius = scr.Params.SpringRadius;

            // マトリックス
            var mat = Matrix4x4.TRS(spos, srot, sscl);
            var imat = mat.inverse;

            // メッシュデータ
            List<Vector3> wposList;
            List<Vector3> wnorList;
            List<Vector3> wtanList;
            int vcnt = deformer.GetEditorPositionNormalTangent(out wposList, out wnorList, out wtanList);

            // 使用頂点とウエイト
            List<int> selectionList = Enumerable.Repeat(SelectionData.Invalid, vcnt).ToList(); // 仮のセレクションデータ
            List<int> useVertexIndexList = new List<int>();
            List<float> weightList = new List<float>();

            for (int i = 0; i < vcnt; i++)
            {
                // 範囲チェック
                var lpos = imat.MultiplyPoint(wposList[i]);
                var dist = lpos.magnitude;
                if (dist <= sradius)
                {
                    // 距離割合
                    var dratio = Mathf.InverseLerp(0.0f, sradius, dist);
                    var dpower = scr.Params.GetSpringDistanceAtten(dratio);

                    // 方向割合
                    var dir = wposList[i] - spos;
                    var ang = Vector3.Angle(sdir, dir);
                    var aratio = Mathf.InverseLerp(0.0f, 180.0f, ang);
                    var apower = scr.Params.GetSpringDirectionAtten(aratio);

                    // ウエイト算出
                    float weight = Mathf.Clamp01(dpower * apower * scr.Params.SpringIntensity);

                    // 登録
                    useVertexIndexList.Add(i);
                    weightList.Add(weight);

                    selectionList[i] = SelectionData.Move;
                }
            }

            // 利用頂点とトライアングル接続する頂点をウエイト０でマークする
            // クロスデータ用にセレクションデータを拡張する
            // （１）無効頂点の隣接が移動／固定頂点なら拡張に変更する
            selectionList = deformer.MeshData.ExtendSelection(selectionList, true, false);
            // 拡張となった頂点を固定としてウエイト０でマークする
            for (int i = 0; i < vcnt; i++)
            {
                if (selectionList[i] == SelectionData.Extend)
                {
                    useVertexIndexList.Add(i);
                    weightList.Add(0.0f);
                }
            }

            // デフォーマーデータ登録
            data.deformerDataHash = deformer.GetDataHash();
            data.vertexCount = deformer.MeshData.VertexCount;
            data.useVertexIndexList = useVertexIndexList.ToArray();
            data.weightList = weightList.ToArray();

            sdata.deformerData = data;

            // 設計時スケール
            Transform influenceTarget = scr.Params.GetInfluenceTarget() ? scr.Params.GetInfluenceTarget() : scr.transform;
            sdata.initScale = influenceTarget.lossyScale;

            return Define.Error.None;
        }

        //=========================================================================================
        // Render Deformer
        //=========================================================================================
        static Define.Error CreateRenderDeformer(CoreComponent core, SerializedObject serializedObject, string savePrefabPath)
        {
            var scr = core as MagicaRenderDeformer;
            var gameObject = scr.gameObject;
            Define.Error result = Define.Error.None;

            // ターゲットオブジェクト
            serializedObject.FindProperty("deformer.targetObject").objectReferenceValue = gameObject;
            serializedObject.FindProperty("deformer.dataHash").intValue = 0;

            // 共有データ作成
            //var meshData = ShareDataObject.CreateShareData<MeshData>("RenderMeshData_" + scr.name);
            var meshData = CreateShareData<MeshData>("RenderMeshData_" + scr.name, savePrefabPath);

            // renderer
            var ren = gameObject.GetComponent<Renderer>();
            if (ren == null)
            {
                //Debug.LogError("Creation failed. Renderer not found.");
                //Debug.LogError(Define.GetErrorMessage(Define.Error.RendererNotFound));
                return Define.Error.RendererNotFound;
            }

            Mesh sharedMesh = null;
            if (ren is SkinnedMeshRenderer)
            {
                meshData.isSkinning = true;
                var sren = ren as SkinnedMeshRenderer;
                sharedMesh = sren.sharedMesh;

                // 設計時スケールはrootBoneから取得する(v1.12.2の変更対応)
                meshData.baseScale = sren.rootBone ? sren.rootBone.lossyScale : gameObject.transform.lossyScale;

            }
            else
            {
                meshData.isSkinning = false;
                var meshFilter = ren.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    //Debug.LogError("Creation failed. MeshFilter not found.");
                    //Debug.LogError(Define.GetErrorMessage(Define.Error.MeshFilterNotFound));
                    return Define.Error.MeshFilterNotFound;
                }
                sharedMesh = meshFilter.sharedMesh;

                // 設計時スケール
                meshData.baseScale = gameObject.transform.lossyScale;
            }
            if (sharedMesh == null)
                return Define.Error.BuildMissingMesh;

            // 頂点
            meshData.vertexCount = sharedMesh.vertexCount;

            // 頂点ハッシュ
            var vlist = sharedMesh.vertices;
            List<ulong> vertexHashList = new List<ulong>();
            for (int i = 0; i < vlist.Length; i++)
            {
                var vhash = DataHashExtensions.GetVectorDataHash(vlist[i]);
                //Debug.Log("[" + i + "] (" + (vlist[i] * 1000) + ") :" + vhash);
                vertexHashList.Add(vhash);
            }
            meshData.vertexHashList = vertexHashList.ToArray();

            // トライアングル
            meshData.triangleCount = sharedMesh.triangles.Length / 3;

            // レンダーデフォーマーのメッシュデータにはローカル座標、法線、接線、UV、トライアングルリストは保存しない
            // 不要なため

            // ボーン
            int boneCount = meshData.isSkinning ? sharedMesh.bindposes.Length : 1;
            meshData.boneCount = boneCount;

            // メッシュデータの検証とハッシュ
            meshData.CreateVerifyData();

            serializedObject.FindProperty("deformer.sharedMesh").objectReferenceValue = sharedMesh;
            serializedObject.FindProperty("deformer.meshData").objectReferenceValue = meshData;
            serializedObject.FindProperty("deformer.meshOptimize").intValue = EditUtility.GetOptimizeMesh(sharedMesh);
            serializedObject.ApplyModifiedProperties();

            // デフォーマーデータの検証とハッシュ
            scr.Deformer.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();

            // コアコンポーネントの検証とハッシュ
            scr.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(meshData);

            // 変更後数
            //Debug.Log("Creation completed. [" + this.name + "]");

            return result;
        }

        //=========================================================================================
        // Virtual Deformer
        //=========================================================================================
        static Define.Error CreateVirtualDeformer(CoreComponent core, SerializedObject serializedObject, string savePrefabPath)
        {
            var scr = core as MagicaVirtualDeformer;
            //var gameObject = scr.gameObject;
            Define.Error result = Define.Error.None;

            // 子メッシュの検証
            if (VerifyChildData(scr.Deformer) == false)
            {
                // error
                Debug.LogError("Setup failed. Invalid RenderDeformer data.");
                return Define.Error.BuildInvalidRenderDeformer;
            }

            serializedObject.FindProperty("deformer.targetObject").objectReferenceValue = scr.gameObject;

            // 新規メッシュデータ
            //var meshData = ShareDataObject.CreateShareData<MeshData>("VirtualMeshData_" + scr.name);
            var meshData = CreateShareData<MeshData>("VirtualMeshData_" + scr.name, savePrefabPath);

            // 設計時スケール
            meshData.baseScale = scr.transform.lossyScale;

            // 仮想メッシュ作成
            var reductionMesh = new MagicaReductionMesh.ReductionMesh();
            //reductionMesh.WeightMode = MagicaReductionMesh.ReductionMesh.ReductionWeightMode.Average; // 平均法(v1.5.2)
            reductionMesh.WeightMode = MagicaReductionMesh.ReductionMesh.ReductionWeightMode.DistanceAverage; // 距離比重改良(v1.8.6)
            reductionMesh.MeshData.MaxWeightCount = scr.Deformer.MaxWeightCount;
            reductionMesh.MeshData.WeightPow = scr.Deformer.WeightPow;
            reductionMesh.MeshData.SameSurfaceAngle = scr.Deformer.SameSurfaceAngle;
            for (int i = 0; i < scr.Deformer.RenderDeformerCount; i++)
            {
                var deformer = scr.Deformer.GetRenderDeformer(i).Deformer;
                if (deformer != null)
                {
                    var sren = deformer.TargetObject.GetComponent<SkinnedMeshRenderer>();
                    List<Transform> boneList = new List<Transform>();
                    if (sren)
                        boneList = new List<Transform>(sren.bones);
                    else
                        boneList.Add(deformer.TargetObject.transform);
                    reductionMesh.AddMesh(
                        deformer.MeshData.isSkinning,
                        deformer.SharedMesh,
                        boneList,
                        deformer.SharedMesh.bindposes,
                        deformer.SharedMesh.boneWeights
                        );
                }
            }

            //reductionMesh.DebugData.DispMeshInfo("リダクション前");

            // リダクション
            reductionMesh.Reduction(
                scr.Deformer.MergeVertexDistance > 0.0f ? 0.0001f : 0.0f,
                scr.Deformer.MergeVertexDistance,
                scr.Deformer.MergeTriangleDistance,
                false
                );

            // （１）ゼロ距離リダクション
            //if (scr.Deformer.MergeVertexDistance > 0.0f)
            //    reductionMesh.ReductionZeroDistance();

            //// （２）頂点距離マージ
            //if (scr.Deformer.MergeVertexDistance > 0.0001f)
            //    reductionMesh.ReductionRadius(scr.Deformer.MergeVertexDistance);

            //// （３）トライアングル接続マージ
            //if (scr.Deformer.MergeTriangleDistance > 0.0f)
            //    reductionMesh.ReductionPolygonLink(scr.Deformer.MergeTriangleDistance);

            //// （４）未使用ボーンの削除
            //reductionMesh.ReductionBone();

            // （５）頂点の最大接続トライアングル数制限
            //reductionMesh.ReductionTriangleConnect(6);

            //reductionMesh.DebugData.DispMeshInfo("リダクション後");

            // 最終メッシュデータ取得
            var final = reductionMesh.GetFinalData(scr.gameObject.transform);

            // メッシュデータシリアライズ
            meshData.isSkinning = final.IsSkinning;
            meshData.vertexCount = final.VertexCount;

            List<uint> vlist;
            List<MeshData.VertexWeight> wlist;
            CreateVertexWeightList(
                final.VertexCount, final.vertices, final.normals, final.tangents, final.boneWeights, final.bindPoses,
                out vlist, out wlist
                );
            meshData.vertexInfoList = vlist.ToArray();
            meshData.vertexWeightList = wlist.ToArray();
            meshData.boneCount = final.BoneCount;

            meshData.uvList = final.uvs.ToArray();
            meshData.lineCount = final.LineCount;
            meshData.lineList = final.lines.ToArray();
            meshData.triangleCount = final.TriangleCount;
            meshData.triangleList = final.triangles.ToArray();

            List<uint> vertexToTriangleInfoList = new List<uint>();
            for (int i = 0; i < final.VertexCount; i++)
            {
                int tcnt = final.vertexToTriangleCountList[i];
                int tstart = final.vertexToTriangleStartList[i];
                vertexToTriangleInfoList.Add(DataUtility.Pack8_24(tcnt, tstart));
            }
            meshData.vertexToTriangleInfoList = vertexToTriangleInfoList.ToArray();
            meshData.vertexToTriangleIndexList = final.vertexToTriangleIndexList.ToArray();

            // 子メッシュ情報
            for (int i = 0; i < final.MeshCount; i++)
            {
                var minfo = final.meshList[i];
                var rdeformer = scr.Deformer.GetRenderDeformer(i).Deformer;
                var mdata = new MeshData.ChildData();

                mdata.childDataHash = rdeformer.GetDataHash();
                mdata.vertexCount = minfo.VertexCount;

                // 頂点ウエイト情報作成
                CreateVertexWeightList(
                    minfo.VertexCount, minfo.vertices, minfo.normals, minfo.tangents, minfo.boneWeights, final.vertexBindPoses,
                    out vlist, out wlist
                    );

                mdata.vertexInfoList = vlist.ToArray();
                mdata.vertexWeightList = wlist.ToArray();

                mdata.parentIndexList = minfo.parents.ToArray();

                meshData.childDataList.Add(mdata);
            }

            // レイヤー情報
            //for (int i = 0; i < final.LayerCount; i++)
            //{
            //    var linfo = new MeshData.LayerInfo();
            //    linfo.triangleList = new List<int>(final.layerList[i].triangleList);
            //    meshData.layerInfoList.Add(linfo);
            //}

            // 検証
            meshData.CreateVerifyData();
            serializedObject.FindProperty("deformer.meshData").objectReferenceValue = meshData;

            // ボーン
            var property = serializedObject.FindProperty("deformer.boneList");
            property.arraySize = final.bones.Count;
            for (int i = 0; i < final.bones.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = final.bones[i];

            serializedObject.ApplyModifiedProperties();

            // デフォーマーデータの検証とハッシュ
            scr.Deformer.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();

            // コアコンポーネントの検証とハッシュ
            scr.CreateVerifyData();
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(meshData);

            //Debug.Log("Setup completed. [" + scr.name + "]");

            return result;
        }

        /// <summary>
        /// 子メッシュに問題がないか検証する
        /// </summary>
        /// <param name="scr"></param>
        /// <returns></returns>
        static bool VerifyChildData(VirtualMeshDeformer scr)
        {
            if (scr.RenderDeformerCount == 0)
                return false;

            for (int i = 0; i < scr.RenderDeformerCount; i++)
            {
                //var deformer = scr.GetRenderDeformer(i).Deformer;
                var deformer = scr.GetRenderDeformer(i);
                if (deformer == null)
                    return false;

                if (deformer.VerifyData() != Define.Error.None)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 頂点ウエイト情報の作成
        /// </summary>
        static void CreateVertexWeightList(
            int vcnt,
            List<Vector3> vertices, List<Vector3> normals, List<Vector4> tangents,
            List<BoneWeight> boneWeights, List<Matrix4x4> bindPoses,
            out List<uint> vlist, out List<MeshData.VertexWeight> wlist
            )
        {
            vlist = new List<uint>();
            wlist = new List<MeshData.VertexWeight>();
            for (int j = 0; j < vcnt; j++)
            {
                var bw = boneWeights[j];

                int wcnt = 0;
                int wstart = wlist.Count;

                // ローカル座標を事前計算する（バインドポーズ方式よりメモリは食うが実行が速い）
                if (bw.weight0 > 0.0f)
                {
                    wcnt++;
                    var vw = new MeshData.VertexWeight();
                    vw.weight = bw.weight0;
                    vw.parentIndex = bw.boneIndex0;
                    vw.localPos = bindPoses[bw.boneIndex0].MultiplyPoint(vertices[j]);
                    vw.localNor = bindPoses[bw.boneIndex0].MultiplyVector(normals[j]).normalized;
                    vw.localTan = bindPoses[bw.boneIndex0].MultiplyVector(tangents[j]).normalized;
                    wlist.Add(vw);
                }
                if (bw.weight1 > 0.0f)
                {
                    wcnt++;
                    var vw = new MeshData.VertexWeight();
                    vw.weight = bw.weight1;
                    vw.parentIndex = bw.boneIndex1;
                    vw.localPos = bindPoses[bw.boneIndex1].MultiplyPoint(vertices[j]);
                    vw.localNor = bindPoses[bw.boneIndex1].MultiplyVector(normals[j]).normalized;
                    vw.localTan = bindPoses[bw.boneIndex1].MultiplyVector(tangents[j]).normalized;
                    wlist.Add(vw);
                }
                if (bw.weight2 > 0.0f)
                {
                    wcnt++;
                    var vw = new MeshData.VertexWeight();
                    vw.weight = bw.weight2;
                    vw.parentIndex = bw.boneIndex2;
                    vw.localPos = bindPoses[bw.boneIndex2].MultiplyPoint(vertices[j]);
                    vw.localNor = bindPoses[bw.boneIndex2].MultiplyVector(normals[j]).normalized;
                    vw.localTan = bindPoses[bw.boneIndex2].MultiplyVector(tangents[j]).normalized;
                    wlist.Add(vw);
                }
                if (bw.weight3 > 0.0f)
                {
                    wcnt++;
                    var vw = new MeshData.VertexWeight();
                    vw.weight = bw.weight3;
                    vw.parentIndex = bw.boneIndex3;
                    vw.localPos = bindPoses[bw.boneIndex3].MultiplyPoint(vertices[j]);
                    vw.localNor = bindPoses[bw.boneIndex3].MultiplyVector(normals[j]).normalized;
                    vw.localTan = bindPoses[bw.boneIndex3].MultiplyVector(tangents[j]).normalized;
                    wlist.Add(vw);
                }

                // 頂点のウエイト情報
                uint pack = DataUtility.Pack4_28(wcnt, wstart);
                vlist.Add(pack);
            }
        }

    }
}
