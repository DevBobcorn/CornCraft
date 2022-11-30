// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaReductionMesh
{
    public class Utility
    {
        /// <summary>
        /// ２つのインデックスを１つのUint型にパッキングする
        /// 上位１６ビット、下位１６ビットにv0/v1番号を結合する
        /// 番号が若いものが上位に来るように配置
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <returns></returns>
        public static uint PackPair(int v0, int v1)
        {
            if (v0 > v1)
            {
                return (uint)v1 << 16 | (uint)v0 & 0xffff;
            }
            else
            {
                return (uint)v0 << 16 | (uint)v1 & 0xffff;
            }
        }

        /// <summary>
        /// パックデータを２つの番号(v0/v1)に分離する
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        public static void UnpackPair(uint pack, out int v0, out int v1)
        {
            // 辺の頂点分解
            v0 = (int)((pack >> 16) & 0xffff);
            v1 = (int)(pack & 0xffff);
        }

        /// <summary>
        /// ３つのインデックスを１つのulong型にパッキングする
        /// 番号が若いものが上位に来るように配置
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static ulong PackTriple(int v0, int v1, int v2)
        {
            List<ulong> indexList = new List<ulong>();
            indexList.Add((ulong)v0);
            indexList.Add((ulong)v1);
            indexList.Add((ulong)v2);
            indexList.Sort();

            ulong hash = (indexList[0] << 32) | (indexList[1] << 16) | (indexList[2]);
            return hash;
        }

        /// <summary>
        /// パックデータを３つの番号に分離する
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        public static void UnpackTriple(ulong pack, out int v0, out int v1, out int v2)
        {
            v0 = (int)((pack >> 32) & 0xffff);
            v1 = (int)((pack >> 16) & 0xffff);
            v2 = (int)(pack & 0xffff);
        }

        /// <summary>
        /// ４つのインデックスを１つのulong型にパッキングする
        /// 番号が若いものが上位に来るように配置
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static ulong PackQuater(int v0, int v1, int v2, int v3)
        {
            List<ulong> indexList = new List<ulong>();
            indexList.Add((ulong)v0);
            indexList.Add((ulong)v1);
            indexList.Add((ulong)v2);
            indexList.Add((ulong)v3);
            indexList.Sort();

            ulong hash = (indexList[0] << 48) | (indexList[1] << 32) | (indexList[2] << 16) | (indexList[3]);
            return hash;
        }

        /// <summary>
        /// パックデータを４つの番号に分離する
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        public static void UnpackQuater(ulong pack, out int v0, out int v1, out int v2, out int v3)
        {
            v0 = (int)((pack >> 48) & 0xffff);
            v1 = (int)((pack >> 32) & 0xffff);
            v2 = (int)((pack >> 16) & 0xffff);
            v3 = (int)(pack & 0xffff);
        }

        /// <summary>
        /// ２つのintを１つのuintにパッキングする
        /// </summary>
        /// <param name="hi"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        public static uint Pack16(int hi, int low)
        {
            return (uint)hi << 16 | (uint)low & 0xffff;
        }

        /// <summary>
        /// uintパックデータから上位16bitをintにして返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        public static int Unpack16Hi(uint pack)
        {
            return (int)((pack >> 16) & 0xffff);
        }

        /// <summary>
        /// uintパックデータから下位16bitをintにして返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        public static int Unpack16Low(uint pack)
        {
            return (int)(pack & 0xffff);
        }

        /// <summary>
        /// ２つのintを１つのulongにパッキングする
        /// </summary>
        /// <param name="hi"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        public static ulong Pack32(int hi, int low)
        {
            return (ulong)hi << 32 | (ulong)low & 0xffffffff;
        }

        /// <summary>
        /// ulongパックデータから上位データを返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        public static int Unpack32Hi(ulong pack)
        {
            return (int)((pack >> 32) & 0xffffffff);
        }

        /// <summary>
        /// ulongパックデータから下位データを返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        public static int Unpack32Low(ulong pack)
        {
            return (int)(pack & 0xffffffff);
        }

        //=========================================================================================
        /// <summary>
        /// FinalDataの共有頂点座標／法線／接線をワールド座標変換して返す
        /// </summary>
        /// <param name="wposList"></param>
        /// <param name="wnorList"></param>
        /// <param name="wtanList"></param>
        /// <returns></returns>
        public static void CalcFinalDataWorldPositionNormalTangent(FinalData final, out List<Vector3> wposList, out List<Vector3> wnorList, out List<Vector4> wtanList)
        {
            wposList = new List<Vector3>();
            wnorList = new List<Vector3>();
            wtanList = new List<Vector4>();

            if (final.VertexCount == 0)
                return;
            if (final.BoneCount == 0)
                return;

            int vcnt = final.VertexCount;

            if (final.IsSkinning == false)
            {
                // 通常メッシュ
                Transform t = final.bones[0];
                for (int i = 0; i < vcnt; i++)
                {
                    Vector3 wpos = t.TransformPoint(final.vertices[i]);
                    wposList.Add(wpos);

                    Vector3 wnor = t.TransformDirection(final.normals[i]);
                    wnor.Normalize();
                    wnorList.Add(wnor);

                    Vector3 wtan = t.TransformDirection(final.tangents[i]);
                    wtan.Normalize();
                    wtanList.Add(new Vector4(wtan.x, wtan.y, wtan.z, final.tangents[i].w));
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
                    weights[0] = final.boneWeights[i].weight0;
                    weights[1] = final.boneWeights[i].weight1;
                    weights[2] = final.boneWeights[i].weight2;
                    weights[3] = final.boneWeights[i].weight3;
                    boneIndexs[0] = final.boneWeights[i].boneIndex0;
                    boneIndexs[1] = final.boneWeights[i].boneIndex1;
                    boneIndexs[2] = final.boneWeights[i].boneIndex2;
                    boneIndexs[3] = final.boneWeights[i].boneIndex3;

                    for (int j = 0; j < 4; j++)
                    {
                        float w = weights[j];
                        if (w > 0.0f)
                        {
                            int bindex = boneIndexs[j];
                            Transform t = final.bones[bindex];

                            // position
                            Vector3 v = final.bindPoses[bindex].MultiplyPoint3x4(final.vertices[i]);
                            v = t.TransformPoint(v);
                            v *= w;
                            wpos += v;

                            // normal
                            v = final.bindPoses[bindex].MultiplyVector(final.normals[i]);
                            v = t.TransformVector(v);
                            wnor += v.normalized * w;

                            // tangent
                            v = final.bindPoses[bindex].MultiplyVector(final.tangents[i]);
                            v = t.TransformVector(v);
                            wtan += v.normalized * w;
                        }
                    }

                    wposList.Add(wpos);
                    wnorList.Add(wnor);
                    wtanList.Add(new Vector4(wtan.x, wtan.y, wtan.z, final.tangents[i].w));
                }
            }
        }

        /// <summary>
        /// FinalDataの子頂点座標／法線／接線をワールド座標変換して返す
        /// </summary>
        /// <param name="wposList"></param>
        /// <param name="wnorList"></param>
        /// <param name="wtanList"></param>
        /// <returns></returns>
        public static void CalcFinalDataChildWorldPositionNormalTangent(
            FinalData final, int meshIndex, List<Vector3> sposList, List<Vector3> snorList, List<Vector4> stanList,
            out List<Vector3> wposList, out List<Vector3> wnorList, out List<Vector4> wtanList
            )
        {
            wposList = new List<Vector3>();
            wnorList = new List<Vector3>();
            wtanList = new List<Vector4>();

            // 回転を求める
            List<Quaternion> quatList = new List<Quaternion>();
            for (int i = 0; i < sposList.Count; i++)
            {
                var q = Quaternion.LookRotation(snorList[i], stanList[i]);
                quatList.Add(q);
            }

            // 共有頂点からさらにスキニングする
            var minfo = final.meshList[meshIndex];
            float[] weights = new float[4];
            int[] boneIndexs = new int[4];
            for (int i = 0; i < minfo.VertexCount; i++)
            {
                Vector3 wpos = Vector3.zero;
                Vector3 wnor = Vector3.zero;
                Vector3 wtan = Vector3.zero;

                // 頂点スキニング
                weights[0] = minfo.boneWeights[i].weight0;
                weights[1] = minfo.boneWeights[i].weight1;
                weights[2] = minfo.boneWeights[i].weight2;
                weights[3] = minfo.boneWeights[i].weight3;
                boneIndexs[0] = minfo.boneWeights[i].boneIndex0;
                boneIndexs[1] = minfo.boneWeights[i].boneIndex1;
                boneIndexs[2] = minfo.boneWeights[i].boneIndex2;
                boneIndexs[3] = minfo.boneWeights[i].boneIndex3;

                for (int j = 0; j < 4; j++)
                {
                    float w = weights[j];
                    if (w > 0.0f)
                    {
                        int bindex = boneIndexs[j];
                        var rot = quatList[bindex];

                        // position
                        Vector3 v = final.vertexBindPoses[bindex].MultiplyPoint3x4(minfo.vertices[i]);
                        v = rot * v + sposList[bindex];
                        v *= w;
                        wpos += v;

                        // normal
                        v = final.vertexBindPoses[bindex].MultiplyVector(minfo.normals[i]);
                        v = rot * v;
                        wnor += v.normalized * w;

                        // tangent
                        v = final.vertexBindPoses[bindex].MultiplyVector(minfo.tangents[i]);
                        v = rot * v;
                        wtan += v.normalized * w;
                    }
                }

                wposList.Add(wpos);
                wnorList.Add(wnor);
                wtanList.Add(new Vector4(wtan.x, wtan.y, wtan.z, -1));
            }
        }

        /// <summary>
        /// 座標／法線／接線をローカル座標変換する
        /// wposList/wnorList/wtanListの中身が書き換わります
        /// </summary>
        /// <param name="root"></param>
        /// <param name="wposList"></param>
        /// <param name="wnorList"></param>
        /// <param name="wtanList"></param>
        public static void CalcLocalPositionNormalTangent(Transform root, List<Vector3> wposList, List<Vector3> wnorList, List<Vector4> wtanList)
        {
            for (int i = 0; i < wposList.Count; i++)
            {
                wposList[i] = root.InverseTransformPoint(wposList[i]);
            }
            for (int i = 0; i < wnorList.Count; i++)
            {
                wnorList[i] = root.InverseTransformDirection(wnorList[i]);
            }
            for (int i = 0; i < wtanList.Count; i++)
            {
                Vector3 v = wtanList[i];
                float w = wtanList[i].w;
                v = root.InverseTransformDirection(v);
                wtanList[i] = new Vector4(v.x, v.y, v.z, w);
            }
        }
    }
}
