// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_2020_1_OR_NEWER
using Unity.Burst;
using Unity.Jobs;
using UnityEngine.Rendering;
#endif

namespace MagicaCloth
{
    /// <summary>
    /// メッシュデータ
    /// </summary>
    public class PhysicsManagerMeshData : PhysicsManagerAccess
    {
        //=========================================================================================
        /// <summary>
        /// 共通メッシュフラグ
        /// </summary>
        public const uint MeshFlag_Active = 0x00000001;
        public const uint MeshFlag_Skinning = 0x00000004;
        public const uint Meshflag_CalcNormal = 0x00000008;
        public const uint Meshflag_CalcTangent = 0x00000010;
        public const uint Meshflag_Pause = 0x00000020; // 一時停止
        // ここからはVirtualMeshInfo用
        // ここからはSharedRenderMeshInfo用
        public const uint MeshFlag_ExistNormals = 0x00010000;   // 法線あり
        public const uint MeshFlag_ExistTangents = 0x00020000;  // 接線あり
        public const uint MeshFlag_ExistWeights = 0x00040000;   // ウエイトあり
        // ここからはRenderMeshInfo用
        public const uint MeshFlag_UpdateUseVertexFront = 0x01000000;   // レンダーメッシュの頂点利用の更新フラグ
        public const uint MeshFlag_UpdateUseVertexBack = 0x02000000;   // レンダーメッシュの頂点利用の更新フラグ
        public const uint MeshFlag_FasterWrite = 0x04000000; // VertexBufferによる高速書き込みフラグ
        public const uint MeshFlag_MeshLink = 0x10000000; // 接続メッシュの有無フラグ（28bit - 31bit)

        //=========================================================================================
        /// <summary>
        /// 仮想メッシュ共有情報
        /// </summary>
        public struct SharedVirtualMeshInfo
        {
            public int uid;

            // 参照カウンタ
            public int useCount;

            public int sharedChildMeshStartIndex;
            public int sharedChildMeshCount;

            // 頂点情報格納先(sharedVirtualUvList/sharedVirtualVertexInfoList)
            public ChunkData vertexChunk;

            // 頂点スキニング情報格納先(sharedVirtualWeightList)
            public ChunkData weightChunk;

            // トライアングル情報格納先(sharedVirtualTriangleList)
            public ChunkData triangleChunk;

            // 頂点が所属するトライアングルリスト(sharedVirtualVertexToTriangleInfoList)
            public ChunkData vertexToTriangleChunk;
        }
        public FixedNativeList<SharedVirtualMeshInfo> sharedVirtualMeshInfoList;
        public Dictionary<int, int> sharedVirtualMeshIdToIndexDict = new Dictionary<int, int>(); // 登録管理

        /// <summary>
        /// 頂点ごとのUV
        /// </summary>
        public FixedChunkNativeArray<float2> sharedVirtualUvList;

        /// <summary>
        /// 頂点ごとのウエイト数とウエイト情報開始インデックス
        /// 上位4bit = ウエイト数
        /// 下位28bit = ウエイトリストの開始インデックス
        /// </summary>
        public FixedChunkNativeArray<uint> sharedVirtualVertexInfoList;

        /// <summary>
        /// ウエイト情報リスト
        /// </summary>
        public FixedChunkNativeArray<MeshData.VertexWeight> sharedVirtualWeightList;

        /// <summary>
        /// トライアングル構成リスト（トライアングルごとに３つの頂点インデックス）
        /// </summary>
        public FixedChunkNativeArray<int> sharedVirtualTriangleList;

        /// <summary>
        /// 頂点が接続するトライアングルインデックスリストへの情報
        /// 上位8bit = 接続トライアングル数
        /// 下位24bit = 接続トライアングルリスト(sharedVirtualVertexToTriangleIndexList)の開始インデックス
        /// </summary>
        public FixedChunkNativeArray<uint> sharedVirtualVertexToTriangleInfoList;

        /// <summary>
        /// 頂点が接続するトライアングルインデックスリスト
        /// </summary>
        public FixedChunkNativeArray<int> sharedVirtualVertexToTriangleIndexList;

        //=========================================================================================
        /// <summary>
        /// 仮想メッシュ頂点フラグ
        /// </summary>
        public const byte VirtualVertexFlag_Use = 0x01; // 使用頂点

        /// <summary>
        /// 仮想メッシュインスタンス情報
        /// </summary>
        public struct VirtualMeshInfo
        {
            public uint flag;
            public int sharedVirtualMeshIndex;
            public int meshUseCount; // 利用カウント
            public int vertexUseCount; // 利用頂点数

            // 頂点情報格納先(useVertexList/posList/normalList/tangentList)
            public ChunkData vertexChunk;

            // ボーン情報格納先(transformIndexList)
            public ChunkData boneChunk;

            // トライアングル情報格納先(triangleNormalList)
            public ChunkData triangleChunk;

            // 仮想メッシュトランスフォーム
            public int transformIndex;

            /// <summary>
            /// フラグ判定
            /// </summary>
            /// <param name="flag"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsFlag(uint flag)
            {
                return (this.flag & flag) != 0;
            }

            /// <summary>
            /// フラグ設定
            /// </summary>
            /// <param name="flag"></param>
            /// <param name="sw"></param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetFlag(uint flag, bool sw)
            {
                if (sw)
                    this.flag |= flag;
                else
                    this.flag &= ~flag;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsActive()
            {
                return IsFlag(MeshFlag_Active);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsUse()
            {
                return IsFlag(MeshFlag_Active) && meshUseCount > 0 && vertexUseCount > 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsPause()
            {
                return IsFlag(Meshflag_Pause);
            }
        }
        public FixedNativeList<VirtualMeshInfo> virtualMeshInfoList;

        /// <summary>
        /// 頂点情報
        /// 上位16bit = 頂点が参照する仮想メッシュインスタンスインデックス
        ///             ※実際のインデックス＋１が入るので注意！（０＝無効とする）
        /// 下位16bit = 頂点利用数（カウント１以上のものを計算する）
        /// </summary>
        //public FixedChunkNativeArray<uint> virtualVertexInfoList;

        /// <summary>
        /// 頂点が参照する仮想メッシュインスタンスインデックス
        /// ※実際のインデックス＋１が入るので注意！（０＝無効とする）
        /// </summary>
        public FixedChunkNativeArray<short> virtualVertexMeshIndexList;

        /// <summary>
        /// 頂点の利用数（カウント１以上のものを計算する）
        /// </summary>
        public FixedChunkNativeArray<byte> virtualVertexUseList;

        /// <summary>
        /// 頂点の固定フラグカウント（カウント１以上は固定化されている）
        /// </summary>
        public FixedChunkNativeArray<byte> virtualVertexFixList;

        /// <summary>
        /// 頂点フラグ
        /// (VirtualVertexFlag_Use～)
        /// </summary>
        public FixedChunkNativeArray<byte> virtualVertexFlagList;

        /// <summary>
        /// 現在のワールド頂点姿勢リスト
        /// </summary>
        public FixedChunkNativeArray<float3> virtualPosList;
        public FixedChunkNativeArray<quaternion> virtualRotList;

        /// <summary>
        /// 現在のボーン姿勢情報
        /// </summary>
        public FixedChunkNativeArray<int> virtualTransformIndexList;

        /// <summary>
        /// 現在のトライアングル法線接線
        /// </summary>
        public FixedChunkNativeArray<float3> virtualTriangleNormalList;
        public FixedChunkNativeArray<float3> virtualTriangleTangentList;

        /// <summary>
        /// トライアングルが参照する仮想メッシュインスタンスインデックス
        /// ※実際のインデックス＋１が入るので注意！（０＝無効とする）
        /// </summary>
        public FixedChunkNativeArray<ushort> virtualTriangleMeshIndexList;

        //=========================================================================================
        /// <summary>
        /// 仮想メッシュの子メッシュ共有情報
        /// </summary>
        public struct SharedChildMeshInfo
        {
            public long cuid;

            public int sharedVirtualMeshIndex;
            public int virtualMeshIndex;
            public int meshUseCount; // 利用カウント

            // 頂点情報格納先(レンダーメッシュと１：１に対応)
            public ChunkData vertexChunk;

            public ChunkData weightChunk;
        }
        public FixedNativeList<SharedChildMeshInfo> sharedChildMeshInfoList;
        public Dictionary<long, int> sharedChildMeshIdToSharedVirtualMeshIndexDict = new Dictionary<long, int>(); // 登録管理用

        /// <summary>
        /// 頂点ごとのウエイト数とウエイト情報開始インデックス
        /// 上位4bit = ウエイト数
        /// 下位28bit = ウエイトリストの開始インデックス
        /// </summary>
        public FixedChunkNativeArray<uint> sharedChildVertexInfoList;

        /// <summary>
        /// ウエイト情報リスト
        /// </summary>
        public FixedChunkNativeArray<MeshData.VertexWeight> sharedChildWeightList;

        //=========================================================================================
        /// <summary>
        /// レンダーメッシュの共有情報
        /// </summary>
        public struct SharedRenderMeshInfo
        {
            public int uid;

            // 参照カウンタ
            public int useCount;

            public uint flag;

            // 頂点情報格納先(vertices/normals/tangents/)
            public ChunkData vertexChunk;

            // ボーンウエイト格納先
            public ChunkData bonePerVertexChunk;    // (sharedBonesPerVertexList/sharedBonesPerVertexStartList)
            public ChunkData boneWeightsChunk;      // (sharedBoneWeightsList)

            // レンダラートランスフォームのボーンインデックス
            public int rendererBoneIndex;

            /// <summary>
            /// フラグ判定
            /// </summary>
            /// <param name="flag"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsFlag(uint flag)
            {
                return (this.flag & flag) != 0;
            }

            /// <summary>
            /// フラグ設定
            /// </summary>
            /// <param name="flag"></param>
            /// <param name="sw"></param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetFlag(uint flag, bool sw)
            {
                if (sw)
                    this.flag |= flag;
                else
                    this.flag &= ~flag;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsSkinning()
            {
                return IsFlag(MeshFlag_Skinning);
            }
        }
        public FixedNativeList<SharedRenderMeshInfo> sharedRenderMeshInfoList;
        public Dictionary<int, int> sharedRenderMeshIdToIndexDict = new Dictionary<int, int>(); // 登録管理

        public FixedChunkNativeArray<float3> sharedRenderVertices;

        // 法線／接線
        public FixedChunkNativeArray<float3> sharedRenderNormals;
        public FixedChunkNativeArray<float4> sharedRenderTangents; // ここはオリジナルなのでw成分あり

        // ボーンウエイト（存在しない場合があるので注意）
        public FixedChunkNativeArray<byte> sharedBonesPerVertexList;
        public FixedChunkNativeArray<int> sharedBonesPerVertexStartList;
        public FixedChunkNativeArray<BoneWeight1> sharedBoneWeightList;

        //=========================================================================================
        /// <summary>
        /// レンダーメッシュ頂点フラグ
        /// </summary>
        public const uint RenderVertexFlag_Use = 0x00010000; // 使用フラグ(ここから16,17,18,19bitを使用する)

        /// <summary>
        /// レンダーメッシュインスタンスが接続できる仮想メッシュの最大数
        /// </summary>
        public const int MaxRenderMeshLinkCount = 4;

        /// <summary>
        /// レンダーメッシュインスタンス情報
        /// </summary>
        public struct RenderMeshInfo
        {
            public uint flag;

            public int renderSharedMeshIndex;
            public int sharedRenderMeshVertexStartIndex;

            public int meshUseCount; // 利用カウント

            // 接続メッシュ情報（最大４）：接続の有無は flag の 28 - 31bit
            public int4 childMeshVertexStartIndex;          // 子共有メッシュの頂点スタートインデックス
            public int4 childMeshWeightStartIndex;          // 子共有メッシュのウエイトスタートインデック
            public int4 virtualMeshVertexStartIndex;        // 仮想メッシュの頂点スタートインデックス
            public int4 sharedVirtualMeshVertexStartIndex;  // 仮想共有メッシュの頂点スタートインデックス
            public int4 linkMeshCount;

            // 頂点情報格納先(posList/normalList/tangentList)
            public ChunkData vertexChunk;

            // ボーンウエイト格納先(boneWeights)
            public ChunkData boneWeightsChunk;

            // レンダーメッシュトランスフォーム
            public int transformIndex;

            // ベーススケール
            public float baseScale;             // 設計時のスケール

            /// <summary>
            /// フラグ判定
            /// </summary>
            /// <param name="flag"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsFlag(uint flag)
            {
                return (this.flag & flag) != 0;
            }

            /// <summary>
            /// フラグ設定
            /// </summary>
            /// <param name="flag"></param>
            /// <param name="sw"></param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetFlag(uint flag, bool sw)
            {
                if (sw)
                    this.flag |= flag;
                else
                    this.flag &= ~flag;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsActive()
            {
                return IsFlag(MeshFlag_Active);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsUse()
            {
                return IsFlag(MeshFlag_Active) && meshUseCount > 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsLinkMesh(int index)
            {
                return (flag & (MeshFlag_MeshLink << index)) != 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsPause()
            {
                return IsFlag(Meshflag_Pause);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsFasterWrite()
            {
                return IsFlag(MeshFlag_FasterWrite);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsSkinning()
            {
                return IsFlag(MeshFlag_Skinning);
            }

            /// <summary>
            /// 接続メッシュを追加する
            /// </summary>
            /// <param name="childMeshVertexStart"></param>
            /// <param name="childMeshWeightStart"></param>
            /// <param name="virtualMeshVertexStart"></param>
            /// <param name="sharedVirtualMeshVertexStart"></param>
            /// <returns></returns>
            public bool AddLinkMesh(int renderMeshIndex, int childMeshVertexStart, int childMeshWeightStart, int virtualMeshVertexStart, int sharedVirtualMeshVertexStart)
            {
                //Develop.Log($"AddLInkMesh[{renderMeshIndex}] (childMeshVertexStart:{childMeshVertexStart},childMeshWeightStart:{childMeshWeightStart},virtualMeshVertexStart:{virtualMeshVertexStart},sharedVirtualMeshVertexStart:{sharedVirtualMeshVertexStart}");

                for (int i = 0; i < MaxRenderMeshLinkCount; i++)
                {
                    if (IsLinkMesh(i) && childMeshVertexStartIndex[i] == childMeshVertexStart && virtualMeshVertexStartIndex[i] == virtualMeshVertexStart)
                    {
                        // 利用カウントアップ
                        linkMeshCount[i]++;
                        SetFlag(MeshFlag_MeshLink << i, true);
                        return true;
                    }
                }

                for (int i = 0; i < MaxRenderMeshLinkCount; i++)
                {
                    if (IsLinkMesh(i) == false)
                    {
                        childMeshVertexStartIndex[i] = childMeshVertexStart;
                        childMeshWeightStartIndex[i] = childMeshWeightStart;
                        virtualMeshVertexStartIndex[i] = virtualMeshVertexStart;
                        sharedVirtualMeshVertexStartIndex[i] = sharedVirtualMeshVertexStart;
                        linkMeshCount[i] = 1;
                        SetFlag(MeshFlag_MeshLink << i, true);
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// 接続メッシュを削除する
            /// </summary>
            /// <param name="childMeshVertexStart"></param>
            /// <param name="childMeshWeightStart"></param>
            /// <param name="virtualMeshVertexStart"></param>
            /// <param name="sharedVirtualMeshVertexStart"></param>
            /// <returns></returns>
            public bool RemoveLinkMesh(int renderMeshIndex, int childMeshVertexStart, int childMeshWeightStart, int virtualMeshVertexStart, int sharedVirtualMeshVertexStart)
            {
                //Develop.Log($"RemoveLinkMesh[{renderMeshIndex}] (childMeshVertexStart:{childMeshVertexStart},childMeshWeightStart:{childMeshWeightStart},virtualMeshVertexStart:{virtualMeshVertexStart},sharedVirtualMeshVertexStart:{sharedVirtualMeshVertexStart}");

                for (int i = 0; i < MaxRenderMeshLinkCount; i++)
                {
                    if (IsLinkMesh(i) && childMeshVertexStartIndex[i] == childMeshVertexStart && virtualMeshVertexStartIndex[i] == virtualMeshVertexStart)
                    {
                        linkMeshCount[i]--;
                        if (linkMeshCount[i] == 0)
                        {
                            childMeshVertexStartIndex[i] = 0;
                            childMeshWeightStartIndex[i] = 0;
                            virtualMeshVertexStartIndex[i] = 0;
                            sharedVirtualMeshVertexStartIndex[i] = 0;
                            SetFlag(MeshFlag_MeshLink << i, false);
                        }
                        return true;
                    }
                }
                return false;
            }
        }
        public FixedNativeList<RenderMeshInfo> renderMeshInfoList;

        /// <summary>
        /// ジョブに依存しないレンダーメッシュ状態フラグ
        /// </summary>
        public const uint RenderStateFlag_Use = 0x00000001;
        public const uint RenderStateFlag_ExistNormal = 0x00000002;
        public const uint RenderStateFlag_ExistTangent = 0x00000004;
        public const uint RenderStateFlag_FasterWrite = 0x00000008; // VertexBuffferを利用した高速書き込み
        public const uint RenderStateFlag_DelayedCalculated = 0x00000100; // 遅延実行時の計算済みフラグ

        /// <summary>
        /// ジョブに依存しないレンダーメッシュ情報
        /// </summary>
        public class RenderMeshState
        {
            /// <summary>
            /// 状態フラグ(RenderStateFlag_Use～)
            /// </summary>
            public uint flag;

            public int RenderSharedMeshIndex;
            public int RenderSharedMeshId;
            public int VertexChunkStart;
            public int VertexChunkLength;
            public int BoneWeightChunkStart;
            public int BoneWeightChunkLength;

            /// <summary>
            /// フラグ判定
            /// </summary>
            /// <param name="flag"></param>
            /// <returns></returns>
            public bool IsFlag(uint flag)
            {
                return (this.flag & flag) != 0;
            }

            /// <summary>
            /// フラグ設定
            /// </summary>
            /// <param name="flag"></param>
            /// <param name="sw"></param>
            public void SetFlag(uint flag, bool sw)
            {
                if (sw)
                    this.flag |= flag;
                else
                    this.flag &= ~flag;
            }
        }

        /// <summary>
        /// キー：レンダーメッシュインスタンスID
        /// </summary>
        public Dictionary<int, RenderMeshState> renderMeshStateDict = new Dictionary<int, RenderMeshState>();

        /// <summary>
        /// 頂点ごとのフラグ
        /// 上位16bit = フラグ(RenderFlag_Use～)
        /// 下位16bit = 頂点が参照するレンダーメッシュインスタンスインデックス
        ///             ※実際のインデックス＋１が入るので注意！（０＝無効とする）
        /// </summary>
        public FixedChunkNativeArray<uint> renderVertexFlagList;

        /// <summary>
        /// 現在の頂点姿勢リスト
        /// 内部でワールド座標系で計算された後ローカル座標系に書き戻される
        /// </summary>
        public FixedChunkNativeArray<float3> renderPosList;
        public FixedChunkNativeArray<float3> renderNormalList;
        public FixedChunkNativeArray<float4> renderTangentList; // ここはメッシュに合わせるのでw成分あり

        /// <summary>
        /// 現在のボーンウエイトリスト
        /// </summary>
        public FixedChunkNativeArray<BoneWeight1> renderBoneWeightList;


        //=========================================================================================
#if UNITY_2021_2_OR_NEWER
        /// <summary>
        /// コンピュートシェーダーによる高速書き込み用バッファ
        /// </summary>
        internal DoubleComputeBuffer<float3> renderPosBuffer;
        internal DoubleComputeBuffer<float3> renderNormalBuffer;
        internal ComputeBuffer emptyByteAddressBuffer;

        /// <summary>
        /// 高速書き込みのComputeBuffer.BeginWriteの実行確認フラグ
        /// </summary>
        private bool isBeginWrite;
#endif

        /// <summary>
        /// 管理中のレンダーメッシュ
        /// </summary>
        HashSet<BaseMeshDeformer> renderMeshSet = new HashSet<BaseMeshDeformer>();

        // 通常書き込みリスト
        List<RenderMeshDeformer> normalWriteList = new List<RenderMeshDeformer>();

        // 高速書き込みリスト
        List<RenderMeshDeformer> fasterWritePositionList = new List<RenderMeshDeformer>();
        List<RenderMeshDeformer> fasterWritePositionNormalList = new List<RenderMeshDeformer>();


        //=========================================================================================
        /// <summary>
        /// 初期設定
        /// </summary>
        public override void Create()
        {
            // shared virtual mesh
            sharedVirtualMeshInfoList = new FixedNativeList<SharedVirtualMeshInfo>();
            sharedVirtualVertexInfoList = new FixedChunkNativeArray<uint>();
            sharedVirtualWeightList = new FixedChunkNativeArray<MeshData.VertexWeight>();
            sharedVirtualUvList = new FixedChunkNativeArray<float2>();
            sharedVirtualTriangleList = new FixedChunkNativeArray<int>();
            sharedVirtualVertexToTriangleInfoList = new FixedChunkNativeArray<uint>();
            sharedVirtualVertexToTriangleIndexList = new FixedChunkNativeArray<int>();

            // virtual mesh
            virtualMeshInfoList = new FixedNativeList<VirtualMeshInfo>();
            //virtualVertexInfoList = new FixedChunkNativeArray<uint>();
            virtualVertexMeshIndexList = new FixedChunkNativeArray<short>();
            virtualVertexUseList = new FixedChunkNativeArray<byte>();
            virtualVertexFixList = new FixedChunkNativeArray<byte>();
            virtualVertexFlagList = new FixedChunkNativeArray<byte>();
            virtualPosList = new FixedChunkNativeArray<float3>();
            virtualRotList = new FixedChunkNativeArray<quaternion>();
            virtualTransformIndexList = new FixedChunkNativeArray<int>();
            virtualTriangleNormalList = new FixedChunkNativeArray<float3>();
            virtualTriangleTangentList = new FixedChunkNativeArray<float3>();
            virtualTriangleMeshIndexList = new FixedChunkNativeArray<ushort>();

            // shared virtual child mesh
            sharedChildMeshInfoList = new FixedNativeList<SharedChildMeshInfo>();
            sharedChildVertexInfoList = new FixedChunkNativeArray<uint>();
            sharedChildWeightList = new FixedChunkNativeArray<MeshData.VertexWeight>();

            // shared render mesh
            sharedRenderMeshInfoList = new FixedNativeList<SharedRenderMeshInfo>();
            sharedRenderVertices = new FixedChunkNativeArray<float3>();
            sharedRenderNormals = new FixedChunkNativeArray<float3>();
            sharedRenderTangents = new FixedChunkNativeArray<float4>();
            sharedBonesPerVertexList = new FixedChunkNativeArray<byte>();
            sharedBonesPerVertexStartList = new FixedChunkNativeArray<int>();
            sharedBoneWeightList = new FixedChunkNativeArray<BoneWeight1>();

            // render mesh
            renderMeshInfoList = new FixedNativeList<RenderMeshInfo>();
            renderVertexFlagList = new FixedChunkNativeArray<uint>();
            renderPosList = new FixedChunkNativeArray<float3>();
            renderNormalList = new FixedChunkNativeArray<float3>();
            renderTangentList = new FixedChunkNativeArray<float4>();
            renderBoneWeightList = new FixedChunkNativeArray<BoneWeight1>();

#if UNITY_2021_2_OR_NEWER
            // graphics buffer
            renderPosBuffer = new DoubleComputeBuffer<float3>();
            renderNormalBuffer = new DoubleComputeBuffer<float3>();
            emptyByteAddressBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Raw);
#endif
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public override void Dispose()
        {
            if (sharedVirtualMeshInfoList == null)
                return;

#if UNITY_2021_2_OR_NEWER
            // graphics buffer
            renderPosBuffer?.Dispose();
            renderNormalBuffer?.Dispose();
            emptyByteAddressBuffer?.Dispose();
#endif

            // shared virtual mesh
            sharedVirtualMeshInfoList.Dispose();
            sharedVirtualVertexInfoList.Dispose();
            sharedVirtualWeightList.Dispose();
            sharedVirtualUvList.Dispose();
            sharedVirtualTriangleList.Dispose();
            sharedVirtualVertexToTriangleInfoList.Dispose();
            sharedVirtualVertexToTriangleIndexList.Dispose();

            // virtual mesh
            virtualMeshInfoList.Dispose();
            //virtualVertexInfoList.Dispose();
            virtualVertexMeshIndexList.Dispose();
            virtualVertexUseList.Dispose();
            virtualVertexFixList.Dispose();
            virtualVertexFlagList.Dispose();
            virtualPosList.Dispose();
            virtualRotList.Dispose();
            virtualTransformIndexList.Dispose();
            virtualTriangleNormalList.Dispose();
            virtualTriangleTangentList.Dispose();
            virtualTriangleMeshIndexList.Dispose();

            // shared virtual child mesh
            sharedChildMeshInfoList.Dispose();
            sharedChildVertexInfoList.Dispose();
            sharedChildWeightList.Dispose();

            // shared render mesh
            sharedRenderMeshInfoList.Dispose();
            sharedRenderVertices.Dispose();
            sharedRenderNormals.Dispose();
            sharedRenderTangents.Dispose();
            sharedBonesPerVertexList.Dispose();
            sharedBonesPerVertexStartList.Dispose();
            sharedBoneWeightList.Dispose();

            // render mesh
            renderMeshInfoList.Dispose();
            renderVertexFlagList.Dispose();
            renderPosList.Dispose();
            renderNormalList.Dispose();
            renderTangentList.Dispose();
            renderBoneWeightList.Dispose();
        }

        //=========================================================================================
        /// <summary>
        /// メッシュを追加
        /// </summary>
        /// <param name="bmesh"></param>
        public void AddMesh(BaseMeshDeformer bmesh)
        {
            if (bmesh is RenderMeshDeformer)
                renderMeshSet.Add(bmesh);
        }

        /// <summary>
        /// メッシュを削除
        /// </summary>
        /// <param name="bmesh"></param>
        public void RemoveMesh(BaseMeshDeformer bmesh)
        {
            if (renderMeshSet.Contains(bmesh))
                renderMeshSet.Remove(bmesh);
        }

        //=========================================================================================
        /// <summary>
        /// 仮想メッシュの登録
        /// ここでは最低限の情報とデータ領域のみ確保する
        /// </summary>
        /// <param name="id">共有情報ユニークID</param>
        /// <param name="vertexCount"></param>
        /// <param name="boneCount"></param>
        /// <param name="triangleCount"></param>
        /// <returns></returns>
        public int AddVirtualMesh(
            int uid,
            int vertexCount,
            int weightCount,
            int boneCount,
            int triangleCount,
            int vertexToTriangleIndexCount,
            Transform transform
            )
        {
            //Develop.Log($"AddVirtualMesh uid:{uid} vcnt:{vertexCount}");
            // 仮想メッシュ共有情報登録
            int sharedMeshIndex = -1;
            if (uid != 0)
            {
                if (sharedVirtualMeshIdToIndexDict.ContainsKey(uid))
                {
                    // 既存
                    sharedMeshIndex = sharedVirtualMeshIdToIndexDict[uid];
                    var sminfo = sharedVirtualMeshInfoList[sharedMeshIndex];
                    sminfo.useCount++; // 参照カウンタ+
                    sharedVirtualMeshInfoList[sharedMeshIndex] = sminfo;
                }
                else
                {
                    // 新規
                    var sminfo = new SharedVirtualMeshInfo();
                    sminfo.uid = uid;
                    sminfo.useCount = 1;

                    // vertices
                    var oc = sharedVirtualVertexInfoList.AddChunk(vertexCount);
                    sharedVirtualUvList.AddChunk(vertexCount);
                    sharedVirtualVertexToTriangleInfoList.AddChunk(vertexCount);
                    sminfo.vertexChunk = oc;

                    //Develop.Log($"SharedVirtualMeshInfo vchunk start:{oc.startIndex} cnt:{oc.dataLength}");

                    // weight
                    oc = sharedVirtualWeightList.AddChunk(weightCount);
                    sminfo.weightChunk = oc;

                    // triangles
                    if (triangleCount > 0)
                    {
                        oc = sharedVirtualTriangleList.AddChunk(triangleCount * 3);
                        sminfo.triangleChunk = oc;
                    }

                    // vertexToTriangleIndex
                    if (vertexToTriangleIndexCount > 0)
                    {
                        oc = sharedVirtualVertexToTriangleIndexList.AddChunk(vertexToTriangleIndexCount);
                        sminfo.vertexToTriangleChunk = oc;
                    }

                    sharedMeshIndex = sharedVirtualMeshInfoList.Add(sminfo);
                    sharedVirtualMeshIdToIndexDict.Add(uid, sharedMeshIndex);
                }
            }

            // 仮想メッシュインスタンス登録
            var minfo = new VirtualMeshInfo();
            //minfo.SetFlag(MeshFlag_Active, true);
            minfo.sharedVirtualMeshIndex = sharedMeshIndex;

            //var c = virtualVertexInfoList.AddChunk(vertexCount);
            var c = virtualVertexUseList.AddChunk(vertexCount);
            virtualVertexMeshIndexList.AddChunk(vertexCount);
            virtualVertexFixList.AddChunk(vertexCount);
            virtualVertexFlagList.AddChunk(vertexCount);
            virtualPosList.AddChunk(vertexCount);
            virtualRotList.AddChunk(vertexCount);
            minfo.vertexChunk = c;

            //Develop.Log($"VirtualMeshInfo vchunk start:{c.startIndex} cnt:{c.dataLength}");

            Debug.Assert(boneCount > 0);
            //c = virtualTransformIndexList.AddChunk(boneCount);
            c = new ChunkData(); // 空で初期化する(v1.9.4)
            minfo.boneChunk = c;

            if (triangleCount > 0)
            {
                c = virtualTriangleNormalList.AddChunk(triangleCount);
                virtualTriangleTangentList.AddChunk(triangleCount);
                virtualTriangleMeshIndexList.AddChunk(triangleCount);
                minfo.triangleChunk = c;
            }

            // 仮想メッシュボーン
            minfo.transformIndex = Bone.AddBone(transform);

            int index = virtualMeshInfoList.Add(minfo);

            // 頂点／トライアングルの参照仮想メッシュインスタンスIDを設定する
            // （＋１）したものなので注意！
            virtualVertexMeshIndexList.Fill(minfo.vertexChunk, (short)(index + 1)); // (+1)するので注意

            // トライアングルの参照仮想メッシュインスタンスIDを設定する
            if (triangleCount > 0)
                virtualTriangleMeshIndexList.Fill(minfo.triangleChunk, (ushort)(index + 1));

            return index;
        }

        /// <summary>
        /// 仮想共有メッシュが存在するか判定する
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public bool IsEmptySharedVirtualMesh(int uid)
        {
            return sharedVirtualMeshIdToIndexDict.ContainsKey(uid) == false;
        }

        /// <summary>
        /// 仮想メッシュ共有データを設定する
        /// </summary>
        /// <param name="virtualMeshIndex"></param>
        /// <param name="sharedVertices"></param>
        /// <param name="sharedNormals"></param>
        /// <param name="sharedTangents"></param>
        /// <param name="sharedBoneWeights"></param>
        public void SetSharedVirtualMeshData(
            int virtualMeshIndex,
            uint[] sharedVertexInfoList,
            MeshData.VertexWeight[] sharedWeightList,
            Vector2[] sharedUv,
            int[] sharedTriangles,
            uint[] vertexToTriangleInfoList,
            int[] vertexToTriangleIndexList
            )
        {
            var minfo = virtualMeshInfoList[virtualMeshIndex];
            Debug.Assert(minfo.sharedVirtualMeshIndex >= 0);
            var smdata = sharedVirtualMeshInfoList[minfo.sharedVirtualMeshIndex];

            // 共有メッシュデータは新規のみコピーする
            if (smdata.useCount == 1)
            {
                sharedVirtualVertexInfoList.ToJobArray().CopyFromFast(smdata.vertexChunk.startIndex, sharedVertexInfoList);
                sharedVirtualWeightList.ToJobArray().CopyFromFast(smdata.weightChunk.startIndex, sharedWeightList);

                if (sharedUv != null && sharedUv.Length > 0)
                    sharedVirtualUvList.ToJobArray().CopyFromFast(smdata.vertexChunk.startIndex, sharedUv);

                if (vertexToTriangleInfoList != null && vertexToTriangleInfoList.Length > 0)
                    sharedVirtualVertexToTriangleInfoList.ToJobArray().CopyFromFast(smdata.vertexChunk.startIndex, vertexToTriangleInfoList);

                if (vertexToTriangleIndexList != null && vertexToTriangleIndexList.Length > 0)
                    sharedVirtualVertexToTriangleIndexList.ToJobArray().CopyFromFast(smdata.vertexToTriangleChunk.startIndex, vertexToTriangleIndexList);

                if (sharedTriangles != null && sharedTriangles.Length > 0)
                    sharedVirtualTriangleList.ToJobArray().CopyFromFast(smdata.triangleChunk.startIndex, sharedTriangles);
            }
        }

        /// <summary>
        /// 仮想メッシュの解除
        /// </summary>
        /// <param name="virtualMeshIndex"></param>
        public void RemoveVirtualMesh(int virtualMeshIndex)
        {
            if (virtualMeshIndex < 0)
                return;
            if (virtualMeshInfoList.Exists(virtualMeshIndex))
            {
                var minfo = virtualMeshInfoList[virtualMeshIndex];

                // 共有メッシュ解除
                int sharedMeshIndex = minfo.sharedVirtualMeshIndex;
                if (sharedMeshIndex >= 0)
                {
                    var sminfo = sharedVirtualMeshInfoList[sharedMeshIndex];
                    sminfo.useCount--; // 参照カウンタ
                    if (sminfo.useCount == 0)
                    {
                        // 削除
                        sharedVirtualVertexInfoList.RemoveChunk(sminfo.vertexChunk.chunkNo);
                        sharedVirtualWeightList.RemoveChunk(sminfo.weightChunk.chunkNo);
                        sharedVirtualUvList.RemoveChunk(sminfo.vertexChunk.chunkNo);
                        sharedVirtualVertexToTriangleInfoList.RemoveChunk(sminfo.vertexChunk.chunkNo);

                        if (sminfo.triangleChunk.dataLength > 0)
                        {
                            sharedVirtualTriangleList.RemoveChunk(sminfo.triangleChunk.chunkNo);
                        }
                        if (sminfo.vertexToTriangleChunk.dataLength > 0)
                        {
                            sharedVirtualVertexToTriangleIndexList.RemoveChunk(sminfo.vertexToTriangleChunk.chunkNo);
                        }
                        sharedVirtualMeshInfoList.Remove(sharedMeshIndex);
                        sharedVirtualMeshIdToIndexDict.Remove(sminfo.uid);
                    }
                    else
                    {
                        sharedVirtualMeshInfoList[sharedMeshIndex] = sminfo;
                    }
                }

                // インスタンスメッシュ解除
                //virtualVertexInfoList.RemoveChunk(minfo.vertexChunk.chunkNo);
                virtualVertexMeshIndexList.RemoveChunk(minfo.vertexChunk.chunkNo);
                virtualVertexUseList.RemoveChunk(minfo.vertexChunk.chunkNo);
                virtualVertexFixList.RemoveChunk(minfo.vertexChunk.chunkNo);
                virtualVertexFlagList.RemoveChunk(minfo.vertexChunk.chunkNo);
                virtualPosList.RemoveChunk(minfo.vertexChunk.chunkNo);
                virtualRotList.RemoveChunk(minfo.vertexChunk.chunkNo);

                virtualTransformIndexList.RemoveChunk(minfo.boneChunk.chunkNo);

                if (minfo.triangleChunk.dataLength > 0)
                {
                    virtualTriangleNormalList.RemoveChunk(minfo.triangleChunk.chunkNo);
                    virtualTriangleTangentList.RemoveChunk(minfo.triangleChunk.chunkNo);
                    virtualTriangleMeshIndexList.RemoveChunk(minfo.triangleChunk.chunkNo);
                }

                // メッシュトランスフォーム解除
                Bone.RemoveBone(minfo.transformIndex);
                minfo.transformIndex = 0;

                //Debug.Log("Remove Mesh Chunk:" + meshChunkIndex);
                virtualMeshInfoList.Remove(virtualMeshIndex);
            }
        }

        public bool ExistsVirtualMesh(int virtualMeshIndex)
        {
            return virtualMeshInfoList.Exists(virtualMeshIndex);
        }

        public VirtualMeshInfo GetVirtualMeshInfo(int virtualMeshIndex)
        {
            return virtualMeshInfoList[virtualMeshIndex];
        }

        /// <summary>
        /// 仮想メッシュが使用され利用頂点が１以上かか判定する
        /// </summary>
        /// <param name="virtualMeshIndex"></param>
        /// <returns></returns>
        public bool IsUseVirtualMesh(int virtualMeshIndex)
        {
            return virtualMeshInfoList[virtualMeshIndex].IsUse();
        }

        /// <summary>
        /// 仮想メッシュが有効か判定する
        /// </summary>
        /// <param name="virtualMeshIndex"></param>
        /// <returns></returns>
        public bool IsActiveVirtualMesh(int virtualMeshIndex)
        {
            return virtualMeshInfoList[virtualMeshIndex].IsActive();
        }

        /// <summary>
        /// 仮想メッシュの有効状態設定
        /// </summary>
        /// <param name="virtualMeshIndex"></param>
        /// <param name="sw"></param>
        public void SetVirtualMeshActive(int virtualMeshIndex, bool sw)
        {
            if (virtualMeshInfoList.Exists(virtualMeshIndex))
            {
                var minfo = virtualMeshInfoList[virtualMeshIndex];
                minfo.SetFlag(MeshFlag_Active, sw);
                virtualMeshInfoList[virtualMeshIndex] = minfo;
            }
        }

        public void AddUseVirtualMesh(int virtualMeshIndex)
        {
            if (virtualMeshInfoList.Exists(virtualMeshIndex))
            {
                var minfo = virtualMeshInfoList[virtualMeshIndex];
                minfo.meshUseCount++;
                virtualMeshInfoList[virtualMeshIndex] = minfo;
            }
        }

        public void RemoveUseVirtualMesh(int virtualMeshIndex)
        {
            if (virtualMeshInfoList.Exists(virtualMeshIndex))
            {
                var minfo = virtualMeshInfoList[virtualMeshIndex];
                minfo.meshUseCount--;
                Debug.Assert(minfo.meshUseCount >= 0);
                virtualMeshInfoList[virtualMeshIndex] = minfo;
            }
        }

        /// <summary>
        /// 仮想メッシュ使用頂点開始
        /// </summary>
        /// <param name="virtualMeshIndex"></param>
        /// <param name="vindex"></param>
        /// <param name="fix">固定頂点ならばtrue</param>
        /// <returns>新規登録ならばtrueを返す</returns>
        public bool AddUseVirtualVertex(int virtualMeshIndex, int vindex, bool fix)
        {
            if (virtualMeshInfoList.Exists(virtualMeshIndex))
            {
                //Debug.Log("Add:" + meshChunkIndex + "," + vindex);
                var minfo = virtualMeshInfoList[virtualMeshIndex];
                minfo.vertexUseCount++;
                virtualMeshInfoList[virtualMeshIndex] = minfo;

                int index = minfo.vertexChunk.startIndex + vindex;

                //uint value = virtualVertexInfoList[index] + 1;
                //virtualVertexInfoList[index] = value;

                // 使用頂点カウント
                byte value = (byte)(virtualVertexUseList[index] + 1);
                virtualVertexUseList[index] = value;

                // 固定頂点カウント
                if (fix)
                    virtualVertexFixList[index] += 1;

                bool change = (value == 1);
                return change;
            }
            else
                return false;
        }

        /// <summary>
        /// 仮想メッシュ使用頂点解除
        /// </summary>
        /// <param name="virtualMeshIndex"></param>
        /// <param name="vindex"></param>
        /// <returns>登録解除ならtrueを返す</returns>
        public bool RemoveUseVirtualVertex(int virtualMeshIndex, int vindex, bool fix)
        {
            if (virtualMeshInfoList.Exists(virtualMeshIndex))
            {
                //Debug.Log("Rem:" + meshChunkIndex + "," + vindex);
                var minfo = virtualMeshInfoList[virtualMeshIndex];
                minfo.vertexUseCount--;
                virtualMeshInfoList[virtualMeshIndex] = minfo;

                int index = minfo.vertexChunk.startIndex + vindex;

                //uint value = virtualVertexInfoList[index] - 1;
                //virtualVertexInfoList[index] = value;

                // 使用頂点カウント
                byte value = (byte)(virtualVertexUseList[index] - 1);
                virtualVertexUseList[index] = value;

                // 固定頂点カウント
                if (fix)
                    virtualVertexFixList[index] -= 1;

                bool change = (value == 0);

                return change;
            }
            else
                return false;
        }

        /// <summary>
        /// 仮想メッシュ座標計算結果をワールド姿勢で取得する
        /// </summary>
        /// <param name="virtualMeshIndex"></param>
        /// <param name="vertices"></param>
        /// <param name="normals"></param>
        /// <param name="tangents"></param>
        public void CopyToVirtualMeshWorldData(int virtualMeshIndex, Vector3[] vertices, Vector3[] normals, Vector3[] tangents)
        {
            var minfo = virtualMeshInfoList[virtualMeshIndex];
            int start = minfo.vertexChunk.startIndex;
            virtualPosList.ToJobArray().CopyToFast(start, vertices);
            var fw = new float3(0, 0, 1);
            var up = new float3(0, 1, 0);
            for (int i = 0; i < minfo.vertexChunk.dataLength; i++)
            {
                var rot = virtualRotList[start + i];
                normals[i] = math.mul(rot, fw);
                tangents[i] = math.mul(rot, up);
            }
        }

        /// <summary>
        /// 仮想メッシュの利用ボーン登録
        /// </summary>
        /// <param name="virtualMeshIndex"></param>
        /// <param name="boneList"></param>
        public void AddVirtualMeshBone(int virtualMeshIndex, List<Transform> boneList)
        {
            if (virtualMeshInfoList.Exists(virtualMeshIndex))
            {
                var minfo = virtualMeshInfoList[virtualMeshIndex];
                var c = virtualTransformIndexList.AddChunk(boneList.Count);
                minfo.boneChunk = c;

                for (int i = 0; i < boneList.Count; i++)
                {
                    virtualTransformIndexList[minfo.boneChunk.startIndex + i] = Bone.AddBone(boneList[i]);
                }

                virtualMeshInfoList[virtualMeshIndex] = minfo;
            }
        }

        /// <summary>
        /// 仮想メッシュの利用ボーン解除
        /// </summary>
        /// <param name="virtualMeshIndex"></param>
        public void RemoveVirtualMeshBone(int virtualMeshIndex)
        {
            if (virtualMeshIndex >= 0)
            {
                if (virtualMeshInfoList.Exists(virtualMeshIndex))
                {
                    var minfo = virtualMeshInfoList[virtualMeshIndex];

                    for (int i = 0; i < minfo.boneChunk.dataLength; i++)
                    {
                        int tindex = virtualTransformIndexList[minfo.boneChunk.startIndex + i];
                        Bone.RemoveBone(tindex);
                        virtualTransformIndexList[minfo.boneChunk.startIndex + i] = 0;
                    }

                    virtualTransformIndexList.RemoveChunk(minfo.boneChunk.chunkNo);
                    minfo.boneChunk.Clear();
                    virtualMeshInfoList[virtualMeshIndex] = minfo;
                }
            }
        }

        /// <summary>
        /// 仮想メッシュボーンの未来予測をリセットする
        /// </summary>
        /// <param name="virtualMeshIndex"></param>
        public void ResetFuturePredictionVirtualMeshBone(int virtualMeshIndex)
        {
            if (virtualMeshIndex >= 0)
            {
                if (virtualMeshInfoList.Exists(virtualMeshIndex))
                {
                    var minfo = virtualMeshInfoList[virtualMeshIndex];

                    for (int i = 0; i < minfo.boneChunk.dataLength; i++)
                    {
                        int tindex = virtualTransformIndexList[minfo.boneChunk.startIndex + i];
                        Bone.ResetFuturePrediction(tindex);
                    }
                }
            }
        }

        /// <summary>
        /// 仮想メッシュのボーンに対してUnityPhysicsでの利用を設定する
        /// </summary>
        /// <param name="virtualMeshIndex"></param>
        /// <param name="sw"></param>
        public void ChangeVirtualMeshUseUnityPhysics(int virtualMeshIndex, bool sw)
        {
            if (virtualMeshIndex >= 0)
            {
                if (virtualMeshInfoList.Exists(virtualMeshIndex))
                {
                    var minfo = virtualMeshInfoList[virtualMeshIndex];
                    Bone.ChangeUnityPhysicsCount(minfo.transformIndex, sw);

                    for (int i = 0; i < minfo.boneChunk.dataLength; i++)
                    {
                        int tindex = virtualTransformIndexList[minfo.boneChunk.startIndex + i];
                        Bone.ChangeUnityPhysicsCount(tindex, sw);
                    }
                }
            }
        }

        /// <summary>
        /// 仮想メッシュのフラグ設定
        /// </summary>
        /// <param name="virtualMeshIndex"></param>
        /// <param name="flag"></param>
        /// <param name="sw"></param>
        public void SetVirtualMeshFlag(int virtualMeshIndex, uint flag, bool sw)
        {
            if (virtualMeshInfoList.Exists(virtualMeshIndex))
            {
                var minfo = virtualMeshInfoList[virtualMeshIndex];
                minfo.SetFlag(flag, sw);
                virtualMeshInfoList[virtualMeshIndex] = minfo;
            }
        }

        public int SharedVirtualMeshCount
        {
            get
            {
                return sharedVirtualMeshInfoList.Count;
            }
        }

        public int VirtualMeshCount
        {
            get
            {
                return virtualMeshInfoList.Count;
            }
        }

        public int VirtualMeshVertexCount
        {
            get
            {
                int cnt = 0;
                for (int i = 0; i < virtualMeshInfoList.Length; i++)
                    cnt += virtualMeshInfoList[i].vertexChunk.dataLength;
                return cnt;
            }
        }

        public int VirtualMeshTriangleCount
        {
            get
            {
                int cnt = 0;
                for (int i = 0; i < virtualMeshInfoList.Length; i++)
                    cnt += virtualMeshInfoList[i].triangleChunk.dataLength;
                return cnt;
            }
        }

        public int VirtualMeshVertexUseCount
        {
            get
            {
                int cnt = 0;
                for (int i = 0; i < virtualMeshInfoList.Length; i++)
                    if (virtualMeshInfoList[i].IsActive())
                        cnt += virtualMeshInfoList[i].vertexChunk.dataLength;
                return cnt;
            }
        }

        public int VirtualMeshUseCount
        {
            get
            {
                int cnt = 0;
                for (int i = 0; i < virtualMeshInfoList.Length; i++)
                    cnt += virtualMeshInfoList[i].IsUse() ? 1 : 0;
                return cnt;
            }
        }

        public int VirtualMeshPauseCount
        {
            get
            {
                int cnt = 0;
                for (int i = 0; i < virtualMeshInfoList.Length; i++)
                    if (virtualMeshInfoList[i].IsUse() && virtualMeshInfoList[i].IsPause())
                        cnt++;
                return cnt;
            }
        }

        //=========================================================================================
        /// <summary>
        /// 仮想メッシュの子メッシュの登録
        /// ここでは最低限の情報とデータ領域のみ確保する
        /// </summary>
        /// <param name="virtualMeshIndex"></param>
        /// <param name="vertexCount"></param>
        /// <returns></returns>
        public int AddSharedChildMesh(
            long cuid,
            int virtualMeshIndex,
            int vertexCount,
            int weightCount
            )
        {
            var minfo = virtualMeshInfoList[virtualMeshIndex];
            int sharedMergeMeshIndex = minfo.sharedVirtualMeshIndex;

            // 仮想メッシュ共有情報登録
            int sharedChildMeshIndex = -1;
            if (sharedChildMeshIdToSharedVirtualMeshIndexDict.ContainsKey(cuid))
            {
                // 既存
                sharedChildMeshIndex = sharedChildMeshIdToSharedVirtualMeshIndexDict[cuid];
                var sc_minfo = sharedChildMeshInfoList[sharedChildMeshIndex];
                sc_minfo.meshUseCount++; // 参照カウンタ+
                sharedChildMeshInfoList[sharedChildMeshIndex] = sc_minfo;
            }
            else
            {
                // 新規
                var sc_minfo = new SharedChildMeshInfo();
                sc_minfo.cuid = cuid;
                sc_minfo.sharedVirtualMeshIndex = sharedMergeMeshIndex;
                sc_minfo.virtualMeshIndex = virtualMeshIndex;
                sc_minfo.meshUseCount = 1;

                // vertices/normals/triangles/bindpose
                var oc = sharedChildVertexInfoList.AddChunk(vertexCount);
                sc_minfo.vertexChunk = oc;

                // weight
                oc = sharedChildWeightList.AddChunk(weightCount);
                sc_minfo.weightChunk = oc;

                sharedChildMeshIndex = sharedChildMeshInfoList.Add(sc_minfo);

                sharedChildMeshIdToSharedVirtualMeshIndexDict.Add(cuid, sharedChildMeshIndex);
            }

            return sharedChildMeshIndex;
        }

        public bool IsEmptySharedChildMesh(long cuid)
        {
            return sharedChildMeshIdToSharedVirtualMeshIndexDict.ContainsKey(cuid) == false;
        }

        /// <summary>
        /// 仮想メッシュの子メッシュ共有データを設定する
        /// </summary>
        /// <param name="meshChunkIndex"></param>
        /// <param name="sharedVertices"></param>
        /// <param name="sharedNormals"></param>
        /// <param name="sharedTangents"></param>
        /// <param name="sharedBoneWeights"></param>
        public void SetSharedChildMeshData(
            int sharedMeshIndex,
            uint[] sharedVertexInfoList,
            MeshData.VertexWeight[] sharedVertexWeightList
            )
        {
            var smdata = sharedChildMeshInfoList[sharedMeshIndex];

            // 共有メッシュデータは新規のみコピーする
            if (smdata.meshUseCount == 1)
            {
                sharedChildVertexInfoList.ToJobArray().CopyFromFast(smdata.vertexChunk.startIndex, sharedVertexInfoList);
                sharedChildWeightList.ToJobArray().CopyFromFast(smdata.weightChunk.startIndex, sharedVertexWeightList);
            }
        }

        public void RemoveSharedChildMesh(int sharedChildMeshIndex)
        {
            // 共有メッシュ解除
            var sc_minfo = sharedChildMeshInfoList[sharedChildMeshIndex];
            sc_minfo.meshUseCount--; // 参照カウンタ
            if (sc_minfo.meshUseCount == 0)
            {
                // 削除
                sharedChildVertexInfoList.RemoveChunk(sc_minfo.vertexChunk.chunkNo);
                sharedChildWeightList.RemoveChunk(sc_minfo.weightChunk.chunkNo);

                sharedChildMeshInfoList.Remove(sharedChildMeshIndex);

                sharedChildMeshIdToSharedVirtualMeshIndexDict.Remove(sc_minfo.cuid);
            }
            else
            {
                sharedChildMeshInfoList[sharedChildMeshIndex] = sc_minfo;
            }
        }

        public int SharedRenderMeshCount
        {
            get
            {
                return sharedRenderMeshInfoList.Count;
            }
        }

        public int SharedChildMeshCount
        {
            get
            {
                return sharedChildMeshInfoList.Count;
            }
        }


        //=========================================================================================
        /// <summary>
        /// レンダーメッシュの登録
        /// ここでは最低限の情報とデータ領域のみ確保する
        /// </summary>
        /// <param name="vertexCount"></param>
        /// <returns></returns>
        public int AddRenderMesh(
            int uid,
            bool isSkinning,
            bool isFasterWrite,
            Vector3 baseScale,
            int vertexCount,
            int rendererBoneIndex,
            int boneWeightCount
            )
        {
            //Develop.Log($"★AddRenderMesh uid:{uid} vcnt:{vertexCount} rboneindex:{rendererBoneIndex} bonewcnt:{boneWeightCount}, isSkinning:{isSkinning}");
            // レンダーメッシュ共有情報登録
            int sharedMeshIndex = -1;
            if (uid != 0)
            {
                if (sharedRenderMeshIdToIndexDict.ContainsKey(uid))
                {
                    // 既存
                    sharedMeshIndex = sharedRenderMeshIdToIndexDict[uid];
                    var sminfo = sharedRenderMeshInfoList[sharedMeshIndex];
                    sminfo.useCount++; // 参照カウンタ+
                    sharedRenderMeshInfoList[sharedMeshIndex] = sminfo;
                }
                else
                {
                    // 新規
                    var sminfo = new SharedRenderMeshInfo();
                    sminfo.uid = uid;
                    sminfo.useCount = 1;
                    sminfo.rendererBoneIndex = rendererBoneIndex;
                    if (isSkinning)
                        sminfo.SetFlag(MeshFlag_Skinning, true);

                    // vertices/normals/triangles/bindpose
                    var oc = sharedRenderVertices.AddChunk(vertexCount);
                    sharedRenderNormals.AddChunk(vertexCount);
                    sharedRenderTangents.AddChunk(vertexCount);
                    sminfo.vertexChunk = oc;

                    //Develop.Log($"vchunk s:{oc.startIndex} cnt:{oc.dataLength}");

                    // ボーンウエイト
                    if (isSkinning)
                    {
                        var bc = sharedBonesPerVertexList.AddChunk(vertexCount);
                        sharedBonesPerVertexStartList.AddChunk(vertexCount);
                        var wc = sharedBoneWeightList.AddChunk(boneWeightCount);
                        sminfo.bonePerVertexChunk = bc;
                        sminfo.boneWeightsChunk = wc;
                    }

                    sharedMeshIndex = sharedRenderMeshInfoList.Add(sminfo);
                    sharedRenderMeshIdToIndexDict.Add(uid, sharedMeshIndex);
                }
            }

            // レンダーメッシュインスタンス登録
            var minfo = new RenderMeshInfo();
            //minfo.SetFlag(MeshFlag_Active, true);
            minfo.SetFlag(MeshFlag_FasterWrite, isFasterWrite);
            minfo.SetFlag(MeshFlag_Skinning, isSkinning);
            minfo.renderSharedMeshIndex = sharedMeshIndex;
            var sminfo2 = sharedRenderMeshInfoList[sharedMeshIndex];
            minfo.sharedRenderMeshVertexStartIndex = sminfo2.vertexChunk.startIndex;
            var c = renderVertexFlagList.AddChunk(vertexCount);
            renderPosList.AddChunk(vertexCount);
            renderNormalList.AddChunk(vertexCount);
            renderTangentList.AddChunk(vertexCount);
            if (isSkinning)
            {
                minfo.boneWeightsChunk = renderBoneWeightList.AddChunk(boneWeightCount);
            }
            minfo.vertexChunk = c;
            minfo.baseScale = baseScale.magnitude; // 設計時スケール：ベクトル長
            int index = renderMeshInfoList.Add(minfo);

            // ジョブに依存しないレンダーメッシュ情報構築
            var state = new RenderMeshState();
            state.SetFlag(RenderStateFlag_Use, minfo.IsUse());
            state.SetFlag(RenderStateFlag_FasterWrite, isFasterWrite);
            state.RenderSharedMeshIndex = sharedMeshIndex;
            state.RenderSharedMeshId = sminfo2.uid;
            state.VertexChunkStart = c.startIndex;
            state.VertexChunkLength = c.dataLength;
            state.BoneWeightChunkStart = minfo.boneWeightsChunk.startIndex;
            state.BoneWeightChunkLength = minfo.boneWeightsChunk.dataLength;
            renderMeshStateDict.Add(index, state);

            // 頂点の参照マージメッシュインスタンスIDを設定する
            // （＋１）したものなので注意！
            uint flag = (uint)index + 1;
            renderVertexFlagList.Fill(minfo.vertexChunk, flag);

            return index;
        }

        /// <summary>
        /// ジョブに依存しないレンダーメッシュ情報の更新
        /// </summary>
        /// <param name="renderMeshIndex"></param>
        public void UpdateMeshState(int renderMeshIndex)
        {
            var state = renderMeshStateDict[renderMeshIndex];
            var sminfo = sharedRenderMeshInfoList[state.RenderSharedMeshIndex];
            state.SetFlag(RenderStateFlag_ExistNormal, sminfo.IsFlag(MeshFlag_ExistNormals));
            state.SetFlag(RenderStateFlag_ExistTangent, sminfo.IsFlag(MeshFlag_ExistTangents));
        }

        /// <summary>
        /// レンダーメッシュのレンダラートランスフォームを登録する
        /// </summary>
        /// <param name="renderMeshIndex"></param>
        /// <param name="rendererTransform"></param>
        public void AddRenderMeshBone(int renderMeshIndex, Transform rendererTransform)
        {
            var minfo = renderMeshInfoList[renderMeshIndex];

            // レンダーメッシュトランスフォーム登録
            minfo.transformIndex = Bone.AddBone(rendererTransform);

            renderMeshInfoList[renderMeshIndex] = minfo;
        }

        public bool IsEmptySharedRenderMesh(int uid)
        {
            return sharedRenderMeshIdToIndexDict.ContainsKey(uid) == false;
        }

        /// <summary>
        /// レンダーメッシュの共有データを設定する
        /// </summary>
        /// <param name="renderMeshIndex"></param>
        /// <param name="sharedVertices"></param>
        /// <param name="sharedNormals"></param>
        /// <param name="sharedTangents"></param>
        /// <param name="sharedBoneWeights"></param>
        public void SetRenderSharedMeshData(
            int renderMeshIndex,
            bool isSkinning,
            Vector3[] sharedVertices,
            Vector3[] sharedNormals,
            Vector4[] sharedTangents,
            NativeArray<byte> sharedBonesPerVertex,
            NativeArray<BoneWeight1> sharedBoneWeights
            )
        {
            var minfo = renderMeshInfoList[renderMeshIndex];
            Debug.Assert(minfo.renderSharedMeshIndex >= 0);
            var smdata = sharedRenderMeshInfoList[minfo.renderSharedMeshIndex];

            // 共有メッシュデータは新規のみコピーする
            if (smdata.useCount == 1)
            {
                sharedRenderVertices.ToJobArray().CopyFromFast(smdata.vertexChunk.startIndex, sharedVertices);

                // 法線は存在しない場合がある
                if (sharedNormals != null && sharedNormals.Length > 0)
                {
                    sharedRenderNormals.ToJobArray().CopyFromFast(smdata.vertexChunk.startIndex, sharedNormals);
                    smdata.SetFlag(MeshFlag_ExistNormals, true);
                }

                // 接線は存在しない場合がある
                if (sharedTangents != null && sharedTangents.Length > 0)
                {
                    sharedRenderTangents.ToJobArray().CopyFromFast(smdata.vertexChunk.startIndex, sharedTangents);
                    smdata.SetFlag(MeshFlag_ExistTangents, true);
                }

                // ボーンウエイトは存在しない場合がある
                if (isSkinning && sharedBonesPerVertex.Length > 0)
                {
                    int vcnt = sharedBonesPerVertex.Length;

                    // 各頂点のウエイトデータ開始インデックスを算出
                    int[] startIndexList = new int[vcnt];
                    int sindex = 0;
                    for (int i = 0; i < vcnt; i++)
                    {
                        startIndexList[i] = sindex;
                        sindex += sharedBonesPerVertex[i];
                    }

                    sharedBonesPerVertexList.ToJobArray().CopyFromFast(smdata.bonePerVertexChunk.startIndex, sharedBonesPerVertex.ToArray());
                    sharedBonesPerVertexStartList.ToJobArray().CopyFromFast(smdata.bonePerVertexChunk.startIndex, startIndexList);
                    sharedBoneWeightList.ToJobArray().CopyFromFast(smdata.boneWeightsChunk.startIndex, sharedBoneWeights.ToArray());
                }

                sharedRenderMeshInfoList[minfo.renderSharedMeshIndex] = smdata;
            }
        }

        /// <summary>
        /// レンダーメッシュの解除
        /// </summary>
        /// <param name="renderMeshIndex"></param>
        public void RemoveRenderMesh(int renderMeshIndex)
        {
            //Develop.Log($"RemoverRenderMesh index:{renderMeshIndex}");
            if (renderMeshIndex < 0)
                return;
            if (renderMeshInfoList.Exists(renderMeshIndex))
            {
                var minfo = renderMeshInfoList[renderMeshIndex];

                // 共有メッシュ解除
                int sharedMeshIndex = minfo.renderSharedMeshIndex;
                if (sharedMeshIndex >= 0)
                {
                    var sminfo = sharedRenderMeshInfoList[sharedMeshIndex];
                    sminfo.useCount--; // 参照カウンタ
                    if (sminfo.useCount == 0)
                    {
                        // 削除
                        //Develop.Log($"共有メッシュ削除 vchunk s:{sminfo.vertexChunk.startIndex} cnt:{sminfo.vertexChunk.dataLength}");
                        sharedRenderVertices.RemoveChunk(sminfo.vertexChunk.chunkNo);
                        sharedRenderNormals.RemoveChunk(sminfo.vertexChunk.chunkNo);
                        sharedRenderTangents.RemoveChunk(sminfo.vertexChunk.chunkNo);

                        if (sminfo.bonePerVertexChunk.dataLength > 0)
                        {
                            sharedBonesPerVertexList.RemoveChunk(sminfo.bonePerVertexChunk);
                            sharedBonesPerVertexStartList.RemoveChunk(sminfo.bonePerVertexChunk);
                        }
                        if (sminfo.boneWeightsChunk.dataLength > 0)
                        {
                            sharedBoneWeightList.RemoveChunk(sminfo.boneWeightsChunk);
                        }

                        sharedRenderMeshInfoList.Remove(sharedMeshIndex);
                        sharedRenderMeshIdToIndexDict.Remove(sminfo.uid);
                    }
                    else
                    {
                        sharedRenderMeshInfoList[sharedMeshIndex] = sminfo;
                    }
                }

                // インスタンスメッシュ解除
                renderVertexFlagList.RemoveChunk(minfo.vertexChunk.chunkNo);
                renderPosList.RemoveChunk(minfo.vertexChunk.chunkNo);
                renderNormalList.RemoveChunk(minfo.vertexChunk.chunkNo);
                renderTangentList.RemoveChunk(minfo.vertexChunk.chunkNo);

                if (minfo.boneWeightsChunk.dataLength > 0)
                {
                    renderBoneWeightList.RemoveChunk(minfo.boneWeightsChunk);
                }

                //Debug.Log("Remove Mesh Chunk:" + meshChunkIndex);
                renderMeshStateDict.Remove(renderMeshIndex);
                renderMeshInfoList.Remove(renderMeshIndex);
            }
        }

        /// <summary>
        /// レンダーメッシュのレンダラートランスフォームを解除する
        /// </summary>
        /// <param name="renderMeshIndex"></param>
        /// <param name="rendererTransform"></param>
        public void RemoveRenderMeshBone(int renderMeshIndex)
        {
            var minfo = renderMeshInfoList[renderMeshIndex];

            // レンダーメッシュトランスフォーム解除
            Bone.RemoveBone(minfo.transformIndex);
            //minfo.transformIndex = 0;
            minfo.transformIndex = -1; // 削除サイン

            renderMeshInfoList[renderMeshIndex] = minfo;
        }

        /// <summary>
        /// レンダーメッシュのボーンに対してUnityPhysicsでの利用を設定する
        /// </summary>
        /// <param name="renderMeshIndex"></param>
        /// <param name="sw"></param>
        public void ChangeRenderMeshUseUnityPhysics(int renderMeshIndex, bool sw)
        {
            var minfo = renderMeshInfoList[renderMeshIndex];
            if (minfo.transformIndex >= 0)
                Bone.ChangeUnityPhysicsCount(minfo.transformIndex, sw);
        }

        /// <summary>
        /// レンダーメッシュが使用され利用頂点が１以上かか判定する
        /// </summary>
        /// <param name="renderMeshIndex"></param>
        /// <returns></returns>
        public bool IsUseRenderMesh(int renderMeshIndex)
        {
            return renderMeshStateDict[renderMeshIndex].IsFlag(RenderStateFlag_Use);
        }

        /// <summary>
        /// レンダーメッシュが有効か判定する
        /// </summary>
        /// <param name="renderMeshIndex"></param>
        /// <returns></returns>
        public bool IsActiveRenderMesh(int renderMeshIndex)
        {
            return renderMeshInfoList[renderMeshIndex].IsActive();
        }

        /// <summary>
        /// レンダーメッシュのフラグ設定
        /// </summary>
        /// <param name="virtualMeshIndex"></param>
        /// <param name="flag"></param>
        /// <param name="sw"></param>
        public void SetRenderMeshFlag(int renderMeshIndex, uint flag, bool sw)
        {
            if (renderMeshInfoList.Exists(renderMeshIndex))
            {
                var minfo = renderMeshInfoList[renderMeshIndex];
                minfo.SetFlag(flag, sw);
                renderMeshInfoList[renderMeshIndex] = minfo;
            }
        }

        public bool IsRenderMeshFlag(int renderMeshIndex, uint flag)
        {
            if (renderMeshInfoList.Exists(renderMeshIndex))
            {
                var minfo = renderMeshInfoList[renderMeshIndex];
                return minfo.IsFlag(flag);
            }
            return false;
        }

        /// <summary>
        /// レンダーメッシュの有効状態設定
        /// </summary>
        /// <param name="renderMeshIndex"></param>
        /// <param name="sw"></param>
        public void SetRenderMeshActive(int renderMeshIndex, bool sw)
        {
            if (renderMeshInfoList.Exists(renderMeshIndex))
            {
                var minfo = renderMeshInfoList[renderMeshIndex];
                minfo.SetFlag(MeshFlag_Active, sw);
                minfo.SetFlag(MeshFlag_UpdateUseVertexFront, true);
                minfo.SetFlag(MeshFlag_UpdateUseVertexBack, true);
                renderMeshInfoList[renderMeshIndex] = minfo;
                renderMeshStateDict[renderMeshIndex].SetFlag(RenderStateFlag_Use, minfo.IsUse());
                renderMeshStateDict[renderMeshIndex].SetFlag(RenderStateFlag_DelayedCalculated, false);
            }
        }

        public void AddUseRenderMesh(int renderMeshIndex)
        {
            if (renderMeshInfoList.Exists(renderMeshIndex))
            {
                var minfo = renderMeshInfoList[renderMeshIndex];
                minfo.meshUseCount++;
                renderMeshInfoList[renderMeshIndex] = minfo;
                renderMeshStateDict[renderMeshIndex].SetFlag(RenderStateFlag_Use, minfo.IsUse());
            }
        }

        public void RemoveUseRenderMesh(int renderMeshIndex)
        {
            if (renderMeshInfoList.Exists(renderMeshIndex))
            {
                var minfo = renderMeshInfoList[renderMeshIndex];
                minfo.meshUseCount--;
                Debug.Assert(minfo.meshUseCount >= 0);
                renderMeshInfoList[renderMeshIndex] = minfo;
                renderMeshStateDict[renderMeshIndex].SetFlag(RenderStateFlag_Use, minfo.IsUse());
            }
        }

        public void LinkRenderMesh(int renderMeshIndex, int childMeshVertexStart, int childMeshWeightStart, int virtualMeshVertexStart, int sharedVirtualMeshVertexStart)
        {
            // レンダーメッシュにマージ子メッシュを接続する
            var minfo = renderMeshInfoList[renderMeshIndex];
            minfo.AddLinkMesh(renderMeshIndex, childMeshVertexStart, childMeshWeightStart, virtualMeshVertexStart, sharedVirtualMeshVertexStart);
            minfo.SetFlag(MeshFlag_UpdateUseVertexFront, true);
            minfo.SetFlag(MeshFlag_UpdateUseVertexBack, true);
            renderMeshInfoList[renderMeshIndex] = minfo;
            renderMeshStateDict[renderMeshIndex].SetFlag(RenderStateFlag_Use, minfo.IsUse());
            renderMeshStateDict[renderMeshIndex].SetFlag(RenderStateFlag_DelayedCalculated, false);
        }

        public void UnlinkRenderMesh(int renderMeshIndex, int childMeshVertexStart, int childMeshWeightStart, int virtualMeshVertexStart, int sharedVirtualMeshVertexStart)
        {
            // レンダーメッシュからマージ子メッシュの接続を解除する
            var minfo = renderMeshInfoList[renderMeshIndex];
            minfo.RemoveLinkMesh(renderMeshIndex, childMeshVertexStart, childMeshWeightStart, virtualMeshVertexStart, sharedVirtualMeshVertexStart);
            minfo.SetFlag(MeshFlag_UpdateUseVertexFront, true);
            minfo.SetFlag(MeshFlag_UpdateUseVertexBack, true);
            renderMeshInfoList[renderMeshIndex] = minfo;
            if (renderMeshStateDict.ContainsKey(renderMeshIndex))
            {
                renderMeshStateDict[renderMeshIndex].SetFlag(RenderStateFlag_Use, minfo.IsUse());
                renderMeshStateDict[renderMeshIndex].SetFlag(RenderStateFlag_DelayedCalculated, false);
            }
        }

        /// <summary>
        /// レンダーメッシュ座標計算結果をローカル姿勢で取得する
        /// </summary>
        /// <param name="renderMeshIndex"></param>
        /// <param name="vertices"></param>
        /// <param name="normals"></param>
        /// <param name="tangents"></param>
        internal void CopyToRenderMeshLocalPositionData(int renderMeshIndex, Mesh mesh, int bufferIndex)
        {
            var state = renderMeshStateDict[renderMeshIndex];
#if UNITY_2020_1_OR_NEWER
            var flag = MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontValidateIndices;
            //Debug.Log($"bufferIndex:{bufferIndex} array0Length:{renderPosList.ToJobArray().Length} array1Length:{renderPosList.ToJobArray(1).Length}  start:{state.VertexChunkStart} length:{state.VertexChunkLength} F:{Time.frameCount}");
            mesh.SetVertices(renderPosList.ToJobArray(bufferIndex), state.VertexChunkStart, state.VertexChunkLength, flag);
#else
            mesh.SetVertices(renderPosList.ToJobArray(bufferIndex), state.VertexChunkStart, state.VertexChunkLength);
#endif
        }

        /// <summary>
        /// レンダーメッシュ座標計算結果をローカル姿勢で取得する
        /// </summary>
        /// <param name="renderMeshIndex"></param>
        /// <param name="vertices"></param>
        /// <param name="normals"></param>
        /// <param name="tangents"></param>
        internal void CopyToRenderMeshLocalNormalTangentData(int renderMeshIndex, Mesh mesh, int bufferIndex, bool normal, bool tangent)
        {
            var state = renderMeshStateDict[renderMeshIndex];
#if UNITY_2020_1_OR_NEWER
            var flag = MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontValidateIndices;
            if (state.IsFlag(RenderStateFlag_ExistNormal) && normal)
            {
                mesh.SetNormals(renderNormalList.ToJobArray(bufferIndex), state.VertexChunkStart, state.VertexChunkLength, flag);
            }
            if (state.IsFlag(RenderStateFlag_ExistTangent) && tangent)
            {
                mesh.SetTangents(renderTangentList.ToJobArray(bufferIndex), state.VertexChunkStart, state.VertexChunkLength, flag);
            }
#else
            if (state.IsFlag(RenderStateFlag_ExistNormal) && normal)
            {
                mesh.SetNormals(renderNormalList.ToJobArray(bufferIndex), state.VertexChunkStart, state.VertexChunkLength);
            }
            if (state.IsFlag(RenderStateFlag_ExistTangent) && tangent)
            {
                mesh.SetTangents(renderTangentList.ToJobArray(bufferIndex), state.VertexChunkStart, state.VertexChunkLength);
            }
#endif
        }

        /// <summary>
        /// レンダーメッシュのボーンウエイト計算結果を取得する
        /// </summary>
        /// <param name="renderMeshIndex"></param>
        /// <param name="vertices"></param>
        internal void CopyToRenderMeshBoneWeightData(int renderMeshIndex, Mesh mesh, Mesh sharedMesh, int bufferIndex)
        {
            var state = renderMeshStateDict[renderMeshIndex];

            NativeArray<BoneWeight1> weights = new NativeArray<BoneWeight1>(state.BoneWeightChunkLength, Allocator.Temp);
            renderBoneWeightList.ToJobArray(bufferIndex).CopyToFast(state.BoneWeightChunkStart, weights);
            mesh.SetBoneWeights(sharedMesh.GetBonesPerVertex(), weights);
            weights.Dispose();
        }

        /// <summary>
        /// レンダーメッシュ座標計算結果をワールド姿勢で取得する（エディタ用）
        /// </summary>
        /// <param name="renderMeshIndex"></param>
        /// <param name="vertices"></param>
        /// <param name="normals"></param>
        /// <param name="tangents"></param>
        internal void CopyToRenderMeshWorldData(int renderMeshIndex, Transform target, Vector3[] vertices, Vector3[] normals, Vector3[] tangents)
        {
            var minfo = renderMeshInfoList[renderMeshIndex];

            // ローカル座標系データ取得
            renderPosList.ToJobArray().CopyToFast(minfo.vertexChunk.startIndex, vertices);
            renderNormalList.ToJobArray().CopyToFast(minfo.vertexChunk.startIndex, normals);
            Vector4[] tan4array = new Vector4[minfo.vertexChunk.dataLength];
            renderTangentList.ToJobArray().CopyToFast(minfo.vertexChunk.startIndex, tan4array);

            // ワールド座標変換
            for (int i = 0; i < minfo.vertexChunk.dataLength; i++)
            {
                vertices[i] = target.TransformPoint(vertices[i]);
                normals[i] = target.InverseTransformDirection(normals[i]);
                tangents[i] = target.InverseTransformDirection(tan4array[i]);
            }
        }

        /// <summary>
        /// レンダーメッシュの使用頂点リストを返す（エディタ用）
        /// </summary>
        /// <param name="renderMeshIndex"></param>
        /// <returns></returns>
        internal List<int> GetVertexUseList(int renderMeshIndex)
        {
            var minfo = renderMeshInfoList[renderMeshIndex];
            var useList = new List<int>(minfo.vertexChunk.dataLength);
            for (int i = 0; i < minfo.vertexChunk.dataLength; i++)
            {
                uint flag = renderVertexFlagList[minfo.vertexChunk.startIndex + i];
                // 上位16bitが使用メッシュビット
                // 頂点の使用を0/1で返す
                useList.Add((flag & 0xffff0000) != 0 ? 1 : 0);
            }

            return useList;
        }

        public int RenderMeshCount
        {
            get
            {
                return renderMeshInfoList.Count;
            }
        }

        public int RenderMeshVertexCount
        {
            get
            {
                int cnt = 0;
                for (int i = 0; i < renderMeshInfoList.Length; i++)
                    cnt += renderMeshInfoList[i].vertexChunk.dataLength;
                return cnt;
            }
        }

        public int RenderMeshUseCount
        {
            get
            {
                int cnt = 0;
                foreach (var state in renderMeshStateDict.Values)
                    cnt += state.IsFlag(RenderStateFlag_Use) ? 1 : 0;
                return cnt;
            }
        }

        public int RenderMeshVertexUseCount
        {
            get
            {
                int cnt = 0;
                for (int i = 0; i < renderMeshInfoList.Length; i++)
                    if (renderMeshInfoList[i].IsActive())
                        cnt += renderMeshInfoList[i].vertexChunk.dataLength;
                return cnt;
            }
        }

        public int RenderMeshPauseCount
        {
            get
            {
                int cnt = 0;
                for (int i = 0; i < renderMeshInfoList.Length; i++)
                    if (renderMeshInfoList[i].IsUse() && renderMeshInfoList[i].IsPause())
                        cnt++;
                return cnt;
            }
        }

        //=========================================================================================
        /// <summary>
        /// 遅延実行の完了フラグを立てる
        /// </summary>
        internal void SetDelayedCalculatedFlag()
        {
            foreach (var mesh in renderMeshSet)
            {
                if (mesh.Parent.IsCalculate)
                {
                    var state = renderMeshStateDict[mesh.MeshIndex];
                    state.SetFlag(RenderStateFlag_DelayedCalculated, true);
                }
            }
        }


        internal void ClearWritingList()
        {
            normalWriteList.Clear();
            fasterWritePositionList.Clear();
            fasterWritePositionNormalList.Clear();
        }

        /// <summary>
        /// メッシュの書き込み方法の判定と準備
        /// </summary>
        /// <param name="bufferIndex"></param>
        internal void MeshCalculation(int bufferIndex)
        {
            foreach (var bmesh in renderMeshSet)
            {
                if (bmesh != null)
                {
                    var rmesh = bmesh as RenderMeshDeformer;
                    rmesh.MeshCalculation(bufferIndex);

                    // 書き込みリスト構築
                    if (rmesh.IsWriteMeshPosition || rmesh.IsWriteMeshBoneWeight)
                        normalWriteList.Add(rmesh);
                    if (rmesh.IsFasterWriteUpdate)
                    {
                        if (rmesh.HasNormal)
                            fasterWritePositionNormalList.Add(rmesh);
                        else
                            fasterWritePositionList.Add(rmesh);
                    }
                }
            }
        }

        /// <summary>
        /// 物理計算完了後の通常のメッシュ書き込み
        /// </summary>
        internal void NormalWriting(int bufferIndex)
        {
            // 全メッシュ書き込み
            foreach (var rmesh in normalWriteList)
                rmesh.NormalWriting(bufferIndex);
            normalWriteList.Clear();
        }


        //=========================================================================================
#if UNITY_2021_2_OR_NEWER
        /// <summary>
        /// レンダーメッシュ座標計算結果をコンピュートシェーダーのバッファに格納する
        /// </summary>
        /// <param name="renderMeshIndex"></param>
        /// <param name="bufferIndex"></param>
        /// <param name="vertexBuffer"></param>
        /// <param name="normal"></param>
        internal void CopyToRenderVertexBuffer(int renderMeshIndex, int bufferIndex, GraphicsBuffer vertexBuffer, bool normal, ComputeShader compute, int kernel, int index)
        {
            var state = renderMeshStateDict[renderMeshIndex];
            int vcnt = vertexBuffer.count;

            // set
            switch (index)
            {
                case 0:
                    compute.SetInt("VertexCount", vcnt);
                    compute.SetInt("VertexStride", vertexBuffer.stride);
                    compute.SetInt("ChunkStart", state.VertexChunkStart);
                    compute.SetBuffer(kernel, "VertexBuffer", vertexBuffer);
                    break;
                case 1:
                    compute.SetInt("VertexCount2", vcnt);
                    compute.SetInt("VertexStride2", vertexBuffer.stride);
                    compute.SetInt("ChunkStart2", state.VertexChunkStart);
                    compute.SetBuffer(kernel, "VertexBuffer2", vertexBuffer);
                    break;
                default:
                    Debug.LogError($"Invalid write compute shader index! :{index}");
                    break;
            }
        }

        /// <summary>
        /// 高速書き込み用バッファ作成
        /// </summary>
        internal void UpdateVertexBuffer()
        {
            int length = renderPosList.Length;
            if (length > 0)
            {
                //Debug.Log($"F:{Time.frameCount}");
                //bool runCopy = false;

                // バッファの確保／拡張
                int nowSize = renderPosBuffer.Count;
                if (length > nowSize)
                {
                    // バッファ新規作成
                    int newSize = nowSize;
                    while (length > newSize)
                        newSize += 65536; // 65536 * 2?

                    renderPosBuffer.Create(newSize, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
                    renderNormalBuffer.Create(newSize, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);

                    //Debug.Log($"ExpansionBuffer:{newSize} F:{Time.frameCount}");

                    // データコピー
                    //runCopy = true;
                }

                // 高速書き込みバッファの書き込み開始
                renderPosBuffer.BeginWrite(length);
                renderNormalBuffer.BeginWrite(length);
                isBeginWrite = true;
                //Debug.Log($"BeginWrite():renderPosArray:{renderPosArray.Length}, renderPosList:{renderPosList.Length} F:{Time.frameCount}");

                // データコピー
                // 常にデータコピーを行う（こうしないとQuest2などで頂点が崩壊する）
                //if (runCopy)
                {
                    var job = new CopyRenderBuffer()
                    {
                        renderPosList = renderPosList.ToJobArray(),
                        renderNormalList = renderNormalList.ToJobArray(),

                        renderPosArray = renderPosBuffer.GetNativeArray(),
                        renderNormalArray = renderNormalBuffer.GetNativeArray(),
                    };
                    Compute.MasterJob = job.Schedule(length, 64, Compute.MasterJob);
                    //Debug.Log($"Start Buffer Copy. F:{Time.frameCount}");
                }
            }
        }

        /// <summary>
        /// 高速書き込み用NativeArrayに現在のデータをコピー
        /// </summary>
        [BurstCompile]
        struct CopyRenderBuffer : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> renderPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> renderNormalList;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> renderPosArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> renderNormalArray;

            public void Execute(int index)
            {
                renderPosArray[index] = renderPosList[index];
                renderNormalArray[index] = renderNormalList[index];
            }
        }

        /// <summary>
        /// 高速書き込みバッファの作業終了
        /// </summary>
        internal void FinishVertexBuffer()
        {
            if (renderPosBuffer != null && isBeginWrite)
            {
                // 高速書き込み用バッファ書き込み完了
                int length = renderPosList.Length;
                renderPosBuffer.EndWrite(length);
                renderNormalBuffer.EndWrite(length);
                isBeginWrite = false;
                //Debug.Log($"EndWrite(): length:{length} F:{Time.frameCount}");
            }
        }

        /// <summary>
        /// スキニング処理後の高速メッシュ書き込み
        /// </summary>
        /// <param name="bufferIndex"></param>
        internal void FasterWriting(int bufferIndex)
        {
            // Position
            DispatchWriting(0, fasterWritePositionList, bufferIndex);
            // Position+Normal
            DispatchWriting(1, fasterWritePositionNormalList, bufferIndex);

            fasterWritePositionList.Clear();
            fasterWritePositionNormalList.Clear();
        }

        void DispatchWriting(int kernel, List<RenderMeshDeformer> rlist, int bufferIndex)
        {
            if (rlist.Count == 0)
                return;

            var compute = manager.MeshWriterShader;
            if (compute == null)
                return;
            if (compute.IsSupported(kernel) == false)
                return;

            // 頂点数でソートする（効率化のため）
            rlist.Sort((a, b) => a.VertexCount < b.VertexCount ? -1 : 1);

            compute.SetBuffer(kernel, "Positions", renderPosBuffer.GetBuffer(bufferIndex));
            compute.SetBuffer(kernel, "Normals", renderNormalBuffer.GetBuffer(bufferIndex));

            // 最大２つ同時に書き込む
            const int batchCount = 2;
            for (int i = 0; i < rlist.Count;)
            {
                int index = 0; // 書き込みVB数
                int vcnt = 0; // 最大書き込み頂点数
                for (; i < rlist.Count && index < batchCount; i++)
                {
                    var rmesh = rlist[i];
                    if (rmesh.FasterWriting(bufferIndex, compute, kernel, index, ref vcnt))
                    {
                        index++;
                    }
                }
                if (index == 0)
                    continue;

                // 書き込みが１つの場合は空データを指定する
                if (index == 1)
                {
                    compute.SetInt("VertexCount2", 0);
                    compute.SetBuffer(kernel, "VertexBuffer2", emptyByteAddressBuffer);
                }

                // dispatch
                uint x, y, z;
                compute.GetKernelThreadGroupSizes(kernel, out x, out y, out z);
                var groups = (vcnt + (int)x - 1) / (int)x;
                compute.Dispatch(kernel, groups, 1, 1);
            }
        }
#endif
    }
}
