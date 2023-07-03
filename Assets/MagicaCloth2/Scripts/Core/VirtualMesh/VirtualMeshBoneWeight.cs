// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// VirtualMeshで利用される頂点のボーンウエイト
    /// これはUnity.BoneWeight構造体を再マッピングしたもの
    /// </summary>
    public struct VirtualMeshBoneWeight
    {
        public float4 weights;
        public int4 boneIndices;

        public bool IsValid => weights[0] >= 1e-06f;


        public VirtualMeshBoneWeight(int4 boneIndices, float4 weights)
        {
            this.boneIndices = boneIndices;
            this.weights = weights;
        }

        /// <summary>
        /// 有効なウエイト数
        /// </summary>
        public int Count
        {
            get
            {
                if (weights[3] > 0.0f)
                    return 4;
                if (weights[2] > 0.0f)
                    return 3;
                if (weights[1] > 0.0f)
                    return 2;
                if (weights[0] > 0.0f)
                    return 1;

                return 0;
            }
        }

        public void AddWeight(int boneIndex, float weight)
        {
            if (weight < 1e-06f)
                return;

            // すでに登録済みならばウエイトのみ加算する
            int wcnt = 0;
            for (int i = 0; i < 4; i++)
            {
                float w = weights[i];
                if (w == 0.0f)
                    break;
                if (boneIndices[i] == boneIndex)
                {
                    // ウエイト加算
                    w += weight;
                    weights[i] = w;

                    // ソート
                    for (int j = i; j >= 1; j--)
                    {
                        if (weights[j] > weights[j - 1])
                        {
                            // swap
                            w = weights[j - 1];
                            weights[j - 1] = weights[j];
                            weights[j] = w;

                            int x = boneIndices[j - 1];
                            boneIndices[j - 1] = boneIndices[j];
                            boneIndices[j] = x;
                        }
                    }

                    return;
                }
                wcnt++;
            }


            // すでに登録されているウエイトより大きければ挿入する
            for (int i = 0; i < 4; i++)
            {
                float w = weights[i];
                if (w == 0.0f)
                {
                    weights[i] = weight;
                    boneIndices[i] = boneIndex;
                    return;
                }
                else if (weight > w)
                {
                    // 挿入
                    for (int j = 2; j >= i; j--)
                    {
                        weights[j + 1] = weights[j];
                        boneIndices[j + 1] = boneIndices[j];
                    }
                    weights[i] = weight;
                    boneIndices[i] = boneIndex;
                    return;
                }
            }
        }

        public void AddWeight(in VirtualMeshBoneWeight bw)
        {
            if (bw.IsValid)
            {
                for (int i = 0; i < 4; i++)
                {
                    AddWeight(bw.boneIndices[i], bw.weights[i]);
                }
            }
        }

        /// <summary>
        /// ウエイトを合計１に調整する
        /// </summary>
        public void AdjustWeight()
        {
            if (IsValid == false)
                return;

            float total = math.csum(weights);
            Debug.Assert(total >= 1e-06f);
            float scl = 1.0f / total;
            weights *= scl;
        }

        public override string ToString()
        {
            return $"[{boneIndices}] w({weights})";
        }
    }
}
