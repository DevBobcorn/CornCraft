// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// レンダーメッシュワーカー
    /// メッシュの利用頂点のワールド姿勢を求める／書き戻す
    /// </summary>
    public class RenderMeshWorker : PhysicsManagerWorker
    {
        //=========================================================================================
        public override void Create()
        {
        }

        public override void Release()
        {
        }

        public override void RemoveGroup(int group)
        {
        }

        //=========================================================================================
        /// <summary>
        /// ローカル座標変換を頂点ごとに実行するか判定する
        /// </summary>
        /// <returns></returns>
        private bool IsPerformMeshProcessForEachParticle()
        {
            // レンダーメッシュ数と使用できるワーカースレッド数に応じて判定
            return Manager.Mesh.renderMeshInfoList.Count <
                Manager.UpdateTime.WorkerMaximumCount * Define.RenderMesh.WorkerMultiplesOfVertexCollection;
        }

        //=========================================================================================
        /// <summary>
        /// トランスフォームリード中に実行する処理
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public override void Warmup()
        {
            if (Manager.Mesh.renderMeshInfoList.Count == 0)
                return;

#if UNITY_2021_2_OR_NEWER
            // Vertex Buffer Update
            Manager.Mesh.UpdateVertexBuffer();
#endif

            var job = new CalcVertexUseFlagJob()
            {
                updateFlag = PhysicsManagerMeshData.MeshFlag_UpdateUseVertexFront << Manager.Compute.SwapIndex,

                renderMeshInfoList = Manager.Mesh.renderMeshInfoList.ToJobArray(),
                sharedRenderMeshInfoList = Manager.Mesh.sharedRenderMeshInfoList.ToJobArray(),

                //virtualVertexInfoList = Manager.Mesh.virtualVertexInfoList.ToJobArray(),
                virtualVertexUseList = Manager.Mesh.virtualVertexUseList.ToJobArray(),
                virtualVertexFixList = Manager.Mesh.virtualVertexFixList.ToJobArray(),

                sharedChildVertexInfoList = Manager.Mesh.sharedChildVertexInfoList.ToJobArray(),
                sharedChildVertexWeightList = Manager.Mesh.sharedChildWeightList.ToJobArray(),

                sharedRenderVertices = Manager.Mesh.sharedRenderVertices.ToJobArray(),
                sharedRenderNormals = Manager.Mesh.sharedRenderNormals.ToJobArray(),
                sharedRenderTangents = Manager.Mesh.sharedRenderTangents.ToJobArray(),
                sharedBonesPerVertexList = Manager.Mesh.sharedBonesPerVertexList.ToJobArray(),
                sharedBonesPerVertexStartList = Manager.Mesh.sharedBonesPerVertexStartList.ToJobArray(),
                sharedBoneWeightList = Manager.Mesh.sharedBoneWeightList.ToJobArray(),

                renderPosList = Manager.Mesh.renderPosList.ToJobArray(),
                renderNormalList = Manager.Mesh.renderNormalList.ToJobArray(),
                renderTangentList = Manager.Mesh.renderTangentList.ToJobArray(),
                renderBoneWeightList = Manager.Mesh.renderBoneWeightList.ToJobArray(),

                renderVertexFlagList = Manager.Mesh.renderVertexFlagList.ToJobArray(),

#if UNITY_2021_2_OR_NEWER
                renderPosArray = Manager.Mesh.renderPosBuffer.GetNativeArray(),
                renderNormalArray = Manager.Mesh.renderNormalBuffer.GetNativeArray(),
#endif
            };
            Manager.Compute.MasterJob = job.Schedule(Manager.Mesh.renderMeshInfoList.Length, 1, Manager.Compute.MasterJob);
        }

        [BurstCompile]
        private struct CalcVertexUseFlagJob : IJobParallelFor
        {
            public uint updateFlag;

            public NativeArray<PhysicsManagerMeshData.RenderMeshInfo> renderMeshInfoList;
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerMeshData.SharedRenderMeshInfo> sharedRenderMeshInfoList;

            //[Unity.Collections.ReadOnly]
            //public NativeArray<uint> virtualVertexInfoList;
            [Unity.Collections.ReadOnly]
            public NativeArray<byte> virtualVertexUseList;
            [Unity.Collections.ReadOnly]
            public NativeArray<byte> virtualVertexFixList;
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> sharedChildVertexInfoList;
            [Unity.Collections.ReadOnly]
            public NativeArray<MeshData.VertexWeight> sharedChildVertexWeightList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> sharedRenderVertices;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> sharedRenderNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float4> sharedRenderTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<byte> sharedBonesPerVertexList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> sharedBonesPerVertexStartList;
            [Unity.Collections.ReadOnly]
            public NativeArray<BoneWeight1> sharedBoneWeightList;

            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> renderPosList;
            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> renderNormalList;

            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float4> renderTangentList;
            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<BoneWeight1> renderBoneWeightList;

            [NativeDisableParallelForRestriction]
            public NativeArray<uint> renderVertexFlagList;

#if UNITY_2021_2_OR_NEWER
            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> renderPosArray;
            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> renderNormalArray;
#endif


            // レンダーメッシュごと
            public void Execute(int rmindex)
            {
                var r_minfo = renderMeshInfoList[rmindex];
                if (r_minfo.IsUse() == false)
                    return;

                // 更新の必要があるメッシュのみ実行する
                if (r_minfo.IsFlag(updateFlag) == false)
                    return;

                // 高速書き込み判定
                bool isFasterWrite = r_minfo.IsFlag(PhysicsManagerMeshData.MeshFlag_FasterWrite);

                var sr_minfo = sharedRenderMeshInfoList[r_minfo.renderSharedMeshIndex];

                for (int i = 0; i < r_minfo.vertexChunk.dataLength; i++)
                {
                    int index = r_minfo.vertexChunk.startIndex + i;
                    uint flag = renderVertexFlagList[index];

                    // 頂点使用フラグをリセット
                    flag &= 0xffff;

                    int4 data;
                    uint bit = PhysicsManagerMeshData.RenderVertexFlag_Use;
                    for (int l = 0; l < PhysicsManagerMeshData.MaxRenderMeshLinkCount; l++)
                    {
                        if (r_minfo.IsLinkMesh(l))
                        {
                            // data.x = 子共有メッシュの頂点スタートインデックス
                            // data.y = 子共有メッシュのウエイトスタートインデック
                            // data.z = 仮想メッシュの頂点スタートインデックス
                            // data.w = 仮想共有メッシュの頂点スタートインデックス
                            data.x = r_minfo.childMeshVertexStartIndex[l];
                            data.y = r_minfo.childMeshWeightStartIndex[l];
                            data.z = r_minfo.virtualMeshVertexStartIndex[l];
                            data.w = r_minfo.sharedVirtualMeshVertexStartIndex[l];

                            int sc_wstart = data.y;
                            int m_vstart = data.z;
                            int sc_vindex = data.x + i;

                            // ウエイト参照するすべての仮想頂点が利用頂点ならばこのレンダーメッシュ頂点を利用する
                            //int usecnt = 0;
                            //uint pack = sharedChildVertexInfoList[sc_vindex];
                            //int wcnt = DataUtility.Unpack4_28Hi(pack);
                            //int wstart = DataUtility.Unpack4_28Low(pack);
                            //for (int j = 0; j < wcnt; j++)
                            //{
                            //    // ウエイト０はありえない
                            //    var vw = sharedChildVertexWeightList[sc_wstart + wstart + j];
                            //    //if ((virtualVertexInfoList[m_vstart + vw.parentIndex] & 0xffff) > 0)
                            //    //    usecnt++;
                            //    if (virtualVertexUseList[m_vstart + vw.parentIndex] > 0)
                            //        usecnt++;
                            //}
                            //if (wcnt > 0 && wcnt == usecnt)
                            //{
                            //    // 利用する
                            //    flag |= bit;
                            //}

                            // ウエイト参照するすべての仮想頂点が利用頂点ならばこのレンダーメッシュ頂点を利用する
                            uint pack = sharedChildVertexInfoList[sc_vindex];
                            int wcnt = DataUtility.Unpack4_28Hi(pack);
                            int wstart = DataUtility.Unpack4_28Low(pack);
                            int fixcnt = 0;
                            int maxfix = wcnt * 75 / 100; // 許容する固定頂点数(75%まで)
                            int j = 0;
                            for (; j < wcnt; j++)
                            {
                                // ウエイト０はありえない
                                var vw = sharedChildVertexWeightList[sc_wstart + wstart + j];
                                int vindex = m_vstart + vw.parentIndex;

                                // 仮想頂点が１つでも未使用ならば、この頂点は利用しない
                                if (virtualVertexUseList[vindex] == 0)
                                    break;

                                // 仮想頂点の固定数をカウント
                                if (virtualVertexFixList[vindex] > 0)
                                {
                                    fixcnt++;
                                    if (fixcnt > maxfix)
                                        break; // 固定頂点数がしきい値を越えたので、この頂点は使用しない
                                }
                            }
                            if (wcnt == j)
                            {
                                // 利用する
                                flag |= bit;
                            }
                        }
                        bit = bit << 1;
                    }

                    // 頂点フラグを再設定
                    renderVertexFlagList[index] = flag;

                    // 頂点セット
                    int si = r_minfo.sharedRenderMeshVertexStartIndex + i;
                    if ((flag & 0xffff0000) == 0)
                    {
                        // 未使用頂点
                        //float3 pos = sharedRenderVertices[si];
                        float3 pos = isFasterWrite ? 100000f : sharedRenderVertices[si];
                        renderPosList[index] = pos;
                        float3 nor = sharedRenderNormals[si];
                        renderNormalList[index] = nor;
                        renderTangentList[index] = sharedRenderTangents[si];

#if UNITY_2021_2_OR_NEWER
                        if (isFasterWrite)
                        {
                            // ここで書き込む100000fは未使用頂点のフラグでありMeshWriterコンピュートシェーダーで参照される
                            renderPosArray[index] = pos;
                            renderNormalArray[index] = nor;
                        }
#endif
                    }

                    // ボーンウエイト
                    if (sr_minfo.IsSkinning())
                    {
                        int svindex = sr_minfo.bonePerVertexChunk.startIndex + i;
                        int wstart = sharedBonesPerVertexStartList[svindex];
                        int windex = r_minfo.boneWeightsChunk.startIndex + wstart;
                        int swindex = sr_minfo.boneWeightsChunk.startIndex + wstart;
                        int renderBoneIndex = sr_minfo.rendererBoneIndex;

                        int cnt = sharedBonesPerVertexList[svindex];
                        if ((flag & 0xffff0000) == 0)
                        {
                            // 未使用頂点
                            for (int j = 0; j < cnt; j++)
                            {
                                renderBoneWeightList[windex + j] = sharedBoneWeightList[swindex + j];
                            }
                        }
                        else
                        {
                            // 使用頂点
                            for (int j = 0; j < cnt; j++)
                            {
                                BoneWeight1 bw = sharedBoneWeightList[swindex + j];
                                bw.boneIndex = renderBoneIndex;
                                renderBoneWeightList[windex + j] = bw;
                            }
                        }
                    }
                }

                // 情報書き戻し
                r_minfo.SetFlag(updateFlag, false);
                renderMeshInfoList[rmindex] = r_minfo;
            }
        }


        //=========================================================================================
        /// <summary>
        /// 物理更新前処理
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public override JobHandle PreUpdate(JobHandle jobHandle)
        {
            // 何もなし
            return jobHandle;
        }

        //=========================================================================================
        /// <summary>
        /// 物理更新後処理
        /// 仮想メッシュワールド姿勢をレンダーメッシュのローカル姿勢に変換する
        /// またオプションで法線／接線／バウンディングボックスを再計算する
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public override JobHandle PostUpdate(JobHandle jobHandle)
        {
            if (Manager.Mesh.renderMeshInfoList.Count == 0)
                return jobHandle;

            // レンダーメッシュの頂点座標／法線／接線を接続仮想メッシュから収集する
            if (IsPerformMeshProcessForEachParticle())
            {
                // 頂点ごと
                var job = new CollectLocalPositionNormalTangentForEachVertexJob()
                {
                    renderMeshInfoList = Manager.Mesh.renderMeshInfoList.ToJobArray(),

                    transformPosList = Manager.Bone.bonePosList.ToJobArray(),
                    transformRotList = Manager.Bone.boneRotList.ToJobArray(),
                    transformSclList = Manager.Bone.boneSclList.ToJobArray(),

                    sharedChildVertexInfoList = Manager.Mesh.sharedChildVertexInfoList.ToJobArray(),
                    sharedChildVertexWeightList = Manager.Mesh.sharedChildWeightList.ToJobArray(),

                    virtualPosList = Manager.Mesh.virtualPosList.ToJobArray(),
                    virtualRotList = Manager.Mesh.virtualRotList.ToJobArray(),

                    renderVertexFlagList = Manager.Mesh.renderVertexFlagList.ToJobArray(),

                    renderPosList = Manager.Mesh.renderPosList.ToJobArray(),
                    renderNormalList = Manager.Mesh.renderNormalList.ToJobArray(),
                    renderTangentList = Manager.Mesh.renderTangentList.ToJobArray(),
#if UNITY_2021_2_OR_NEWER
                    renderPosArray = Manager.Mesh.renderPosBuffer.GetNativeArray(),
                    renderNormalArray = Manager.Mesh.renderNormalBuffer.GetNativeArray(),
#endif
                };
                jobHandle = job.Schedule(Manager.Mesh.renderPosList.Length, 128, jobHandle);
            }
            else
            {
                //Debug.Log("Group!");
                // レンダーメッシュごと
                // 大量のメッシュの場合はこちらのほうが高速
                var job = new CollectLocalPositionNormalTangentForEachMeshJob()
                {
                    renderMeshInfoList = Manager.Mesh.renderMeshInfoList.ToJobArray(),

                    transformPosList = Manager.Bone.bonePosList.ToJobArray(),
                    transformRotList = Manager.Bone.boneRotList.ToJobArray(),
                    transformSclList = Manager.Bone.boneSclList.ToJobArray(),

                    sharedChildVertexInfoList = Manager.Mesh.sharedChildVertexInfoList.ToJobArray(),
                    sharedChildVertexWeightList = Manager.Mesh.sharedChildWeightList.ToJobArray(),

                    virtualPosList = Manager.Mesh.virtualPosList.ToJobArray(),
                    virtualRotList = Manager.Mesh.virtualRotList.ToJobArray(),

                    renderVertexFlagList = Manager.Mesh.renderVertexFlagList.ToJobArray(),

                    renderPosList = Manager.Mesh.renderPosList.ToJobArray(),
                    renderNormalList = Manager.Mesh.renderNormalList.ToJobArray(),
                    renderTangentList = Manager.Mesh.renderTangentList.ToJobArray(),
#if UNITY_2021_2_OR_NEWER
                    renderPosArray = Manager.Mesh.renderPosBuffer.GetNativeArray(),
                    renderNormalArray = Manager.Mesh.renderNormalBuffer.GetNativeArray(),
#endif
                };
                jobHandle = job.Schedule(Manager.Mesh.renderMeshInfoList.Length, 1, jobHandle);
            }

            return jobHandle;
        }

        /// <summary>
        /// レンダーメッシュごとに収集
        /// </summary>
        [BurstCompile]
        private struct CollectLocalPositionNormalTangentForEachMeshJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerMeshData.RenderMeshInfo> renderMeshInfoList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformSclList;

            [Unity.Collections.ReadOnly]
            public NativeArray<uint> sharedChildVertexInfoList;
            [Unity.Collections.ReadOnly]
            public NativeArray<MeshData.VertexWeight> sharedChildVertexWeightList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> virtualPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> virtualRotList;

            [Unity.Collections.ReadOnly]
            public NativeArray<uint> renderVertexFlagList;

            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> renderPosList;
            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> renderNormalList;

            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float4> renderTangentList;

#if UNITY_2021_2_OR_NEWER
            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> renderPosArray;
            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> renderNormalArray;
#endif

            // レンダーメッシュごと
            public void Execute(int rmindex)
            {
                var r_minfo = renderMeshInfoList[rmindex];
                if (r_minfo.IsUse() == false)
                    return;

                // 書き込み停止判定
                if (r_minfo.IsPause())
                    return;

                // 高速書き込み判定
                bool isFasterWrite = r_minfo.IsFasterWrite();

                // レンダラーのローカル座標系に変換する
                int tindex = r_minfo.transformIndex;
                var tpos = transformPosList[tindex];
                var trot = transformRotList[tindex];
                var tscl = transformSclList[tindex];
                quaternion itrot = math.inverse(trot);

                //int vcnt = r_minfo.vertexChunk.dataLength;
                //int r_vstart = r_minfo.vertexChunk.startIndex;

                bool calcNormal = r_minfo.IsFlag(PhysicsManagerMeshData.Meshflag_CalcNormal);
                bool calcTangent = r_minfo.IsFlag(PhysicsManagerMeshData.Meshflag_CalcTangent);

                // レンダラースケール
                float scaleRatio = r_minfo.baseScale > 0.0f ? math.length(tscl) / r_minfo.baseScale : 1.0f;
                float3 scaleDirection = math.sign(tscl);

                // 頂点ごと
                for (int i = 0; i < r_minfo.vertexChunk.dataLength; i++)
                {
                    int vindex = r_minfo.vertexChunk.startIndex + i;
                    uint flag = renderVertexFlagList[vindex];

                    // 使用頂点のみ
                    if ((flag & 0xffff0000) == 0)
                    {
                        continue;
                    }

                    // ローカル変換
                    CollectionVertex(
                        r_minfo,
                        sharedChildVertexInfoList,
                        sharedChildVertexWeightList,
                        virtualPosList,
                        virtualRotList,
                        tpos,
                        trot,
                        tscl,
                        itrot,
                        scaleRatio,
                        scaleDirection,
                        calcNormal,
                        calcTangent,
                        vindex,
                        i,
                        flag,
                        isFasterWrite,
                        ref renderPosList,
                        ref renderNormalList,
#if UNITY_2021_2_OR_NEWER
                        ref renderPosArray,
                        ref renderNormalArray,
#endif
                        ref renderTangentList
                        );
                }
            }
        }

        /// <summary>
        /// 頂点ごとに収集
        /// </summary>
        [BurstCompile]
        private struct CollectLocalPositionNormalTangentForEachVertexJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<PhysicsManagerMeshData.RenderMeshInfo> renderMeshInfoList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformSclList;

            [Unity.Collections.ReadOnly]
            public NativeArray<uint> sharedChildVertexInfoList;
            [Unity.Collections.ReadOnly]
            public NativeArray<MeshData.VertexWeight> sharedChildVertexWeightList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> virtualPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> virtualRotList;

            [Unity.Collections.ReadOnly]
            public NativeArray<uint> renderVertexFlagList;

            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> renderPosList;
            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> renderNormalList;

            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float4> renderTangentList;

#if UNITY_2021_2_OR_NEWER
            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> renderPosArray;
            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> renderNormalArray;
#endif
            // 頂点ごと
            public void Execute(int vindex)
            {
                uint flag = renderVertexFlagList[vindex];
                // 使用頂点のみ
                if ((flag & 0xffff0000) == 0)
                {
                    return;
                }

                // レンダーメッシュインデックス
                int rmindex = DataUtility.Unpack16Low(flag) - 1; // (-1)するので注意！
                var r_minfo = renderMeshInfoList[rmindex];
                if (r_minfo.IsUse() == false)
                    return;

                // 書き込み停止判定
                if (r_minfo.IsPause())
                    return;

                // 高速書き込み判定
                bool isFasterWrite = r_minfo.IsFasterWrite();

                bool calcNormal = r_minfo.IsFlag(PhysicsManagerMeshData.Meshflag_CalcNormal);
                bool calcTangent = r_minfo.IsFlag(PhysicsManagerMeshData.Meshflag_CalcTangent);

                // レンダラーのローカル座標系に変換する
                int tindex = r_minfo.transformIndex;
                var tpos = transformPosList[tindex];
                var trot = transformRotList[tindex];
                var tscl = transformSclList[tindex];
                quaternion itrot = math.inverse(trot);

                // レンダラースケール
                float scaleRatio = r_minfo.baseScale > 0.0f ? math.length(tscl) / r_minfo.baseScale : 1.0f;
                float3 scaleDirection = math.sign(tscl);

                // ローカル頂点インデックス
                int i = vindex - r_minfo.vertexChunk.startIndex;

                // ローカル変換
                CollectionVertex(
                    r_minfo,
                    sharedChildVertexInfoList,
                    sharedChildVertexWeightList,
                    virtualPosList,
                    virtualRotList,
                    tpos,
                    trot,
                    tscl,
                    itrot,
                    scaleRatio,
                    scaleDirection,
                    calcNormal,
                    calcTangent,
                    vindex,
                    i,
                    flag,
                    isFasterWrite,
                    ref renderPosList,
                    ref renderNormalList,
#if UNITY_2021_2_OR_NEWER
                    ref renderPosArray,
                    ref renderNormalArray,
#endif
                    ref renderTangentList
                    );
            }
        }

        /// <summary>
        /// １つの頂点のローカル座標変換処理
        /// </summary>
        /// <param name="r_minfo"></param>
        /// <param name="sharedChildVertexInfoList"></param>
        /// <param name="sharedChildVertexWeightList"></param>
        /// <param name="virtualPosList"></param>
        /// <param name="virtualRotList"></param>
        /// <param name="tpos"></param>
        /// <param name="trot"></param>
        /// <param name="tscl"></param>
        /// <param name="itrot"></param>
        /// <param name="scaleRatio"></param>
        /// <param name="scaleDirection"></param>
        /// <param name="calcNormal"></param>
        /// <param name="calcTangent"></param>
        /// <param name="vindex"></param>
        /// <param name="i"></param>
        /// <param name="flag"></param>
        /// <param name="renderPosList"></param>
        /// <param name="renderNormalList"></param>
        /// <param name="renderTangentList"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CollectionVertex(
            in PhysicsManagerMeshData.RenderMeshInfo r_minfo,
            in NativeArray<uint> sharedChildVertexInfoList,
            in NativeArray<MeshData.VertexWeight> sharedChildVertexWeightList,
            in NativeArray<float3> virtualPosList,
            in NativeArray<quaternion> virtualRotList,
            in float3 tpos,
            in quaternion trot,
            in float3 tscl,
            in quaternion itrot,
            float scaleRatio,
            in float3 scaleDirection,
            bool calcNormal,
            bool calcTangent,
            int vindex,
            int i,
            uint flag,
            bool isFasterWrite,
            ref NativeArray<float3> renderPosList,
            ref NativeArray<float3> renderNormalList,
#if UNITY_2021_2_OR_NEWER
            ref NativeArray<float3> renderPosArray,
            ref NativeArray<float3> renderNomalArray,
#endif
            ref NativeArray<float4> renderTangentList
            )
        {
            // レンダーメッシュは複数の仮想メッシュに接続される場合がある
            int4 data;
            float3 sum_pos = 0;
            float3 sum_nor = 0;
            float3 sum_tan = 0;
            float4 sum_tan4 = 0;
            sum_tan4.w = -1;
            int cnt = 0;
            uint bit = PhysicsManagerMeshData.RenderVertexFlag_Use;
            for (int l = 0; l < PhysicsManagerMeshData.MaxRenderMeshLinkCount; l++)
            {
                if (r_minfo.IsLinkMesh(l))
                {
                    // data.x = 子共有メッシュの頂点スタートインデックス
                    // data.y = 子共有メッシュのウエイトスタートインデック
                    // data.z = 仮想メッシュの頂点スタートインデックス
                    // data.w = 仮想共有メッシュの頂点スタートインデックス
                    data.x = r_minfo.childMeshVertexStartIndex[l];
                    data.y = r_minfo.childMeshWeightStartIndex[l];
                    data.z = r_minfo.virtualMeshVertexStartIndex[l];
                    data.w = r_minfo.sharedVirtualMeshVertexStartIndex[l];

                    if ((flag & bit) == 0)
                    {
                        bit = bit << 1;
                        continue;
                    }

                    float3 pos = 0;
                    float3 nor = 0;
                    float3 tan = 0;

                    int sc_vindex = data.x + i;
                    int sc_wstart = data.y;
                    int m_vstart = data.z;

                    // スキニング
                    uint pack = sharedChildVertexInfoList[sc_vindex];
                    int wcnt = DataUtility.Unpack4_28Hi(pack);
                    int wstart = DataUtility.Unpack4_28Low(pack);

                    if (calcTangent)
                    {
                        for (int j = 0; j < wcnt; j++)
                        {
                            var vw = sharedChildVertexWeightList[sc_wstart + wstart + j];

                            // ウエイト０はありえない
                            var mpos = virtualPosList[m_vstart + vw.parentIndex];
                            var mrot = virtualRotList[m_vstart + vw.parentIndex];

                            // position
                            //pos += (mpos + math.mul(mrot, vw.localPos * renderScale)) * vw.weight;
                            pos += (mpos + math.mul(mrot, vw.localPos * scaleDirection * scaleRatio)) * vw.weight;

                            // normal
                            //nor += math.mul(mrot, vw.localNor) * vw.weight;
                            nor += math.mul(mrot, vw.localNor * scaleDirection) * vw.weight;

                            // tangent
                            //tan += math.mul(mrot, vw.localTan) * vw.weight;
                            tan += math.mul(mrot, vw.localTan * scaleDirection) * vw.weight;
                        }

                        // レンダラーのローカル座標系に変換する
                        pos = math.mul(itrot, (pos - tpos)) / tscl;
                        nor = math.mul(itrot, nor);
                        tan = math.mul(itrot, tan);

                        // マイナススケール対応
                        nor *= scaleDirection;
                        tan *= scaleDirection;

                        sum_pos += pos;
                        sum_nor += nor;
                        sum_tan += tan;
                    }
                    else if (calcNormal)
                    {
                        for (int j = 0; j < wcnt; j++)
                        {
                            var vw = sharedChildVertexWeightList[sc_wstart + wstart + j];

                            // ウエイト０はありえない
                            var mpos = virtualPosList[m_vstart + vw.parentIndex];
                            var mrot = virtualRotList[m_vstart + vw.parentIndex];

                            // position
                            //pos += (mpos + math.mul(mrot, vw.localPos * renderScale)) * vw.weight;
                            pos += (mpos + math.mul(mrot, vw.localPos * scaleDirection * scaleRatio)) * vw.weight;

                            // normal
                            //nor += math.mul(mrot, vw.localNor) * vw.weight;
                            nor += math.mul(mrot, vw.localNor * scaleDirection) * vw.weight;
                        }

                        // レンダラーのローカル座標系に変換する
                        pos = math.mul(itrot, (pos - tpos)) / tscl;
                        nor = math.mul(itrot, nor);

                        // マイナススケール対応
                        nor *= scaleDirection;

                        sum_pos += pos;
                        sum_nor += nor;
                    }
                    else
                    {
                        for (int j = 0; j < wcnt; j++)
                        {
                            var vw = sharedChildVertexWeightList[sc_wstart + wstart + j];

                            // ウエイト０はありえない
                            var mpos = virtualPosList[m_vstart + vw.parentIndex];
                            var mrot = virtualRotList[m_vstart + vw.parentIndex];

                            // position
                            //pos += (mpos + math.mul(mrot, vw.localPos * renderScale)) * vw.weight;
                            pos += (mpos + math.mul(mrot, vw.localPos * scaleDirection * scaleRatio)) * vw.weight;
                        }

                        // レンダラーのローカル座標系に変換する
                        pos = math.mul(itrot, (pos - tpos)) / tscl;

                        sum_pos += pos;
                    }
                    cnt++;
                }
                bit = bit << 1;
            }
            if (cnt > 0)
            {
                float3 fpos = sum_pos / cnt;
                renderPosList[vindex] = fpos;

                float3 fnor = 0;

                if (calcTangent)
                {
                    fnor = sum_nor / cnt; ;
                    renderNormalList[vindex] = fnor;
                    sum_tan4.xyz = sum_tan / cnt;
                    renderTangentList[vindex] = sum_tan4;
                }
                else if (calcNormal)
                {
                    fnor = sum_nor / cnt; ;
                    renderNormalList[vindex] = fnor;
                }

#if UNITY_2021_2_OR_NEWER
                // FasterWriteバッファ書き込み
                if (isFasterWrite)
                {
                    renderPosArray[vindex] = fpos; // FasterWriteバッファ
                    renderNomalArray[vindex] = fnor; // FasterWriteバッファ
                }
#endif
            }
        }
    }
}
