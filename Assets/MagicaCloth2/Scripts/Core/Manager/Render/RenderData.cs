// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 描画対象の管理情報
    /// レンダラーまたはボーンの描画反映を行う
    /// </summary>
    public class RenderData : IDisposable, ITransform
    {
        /// <summary>
        /// 参照カウント。０になると破棄される
        /// </summary>
        public int ReferenceCount { get; private set; }

        /// <summary>
        /// 利用中のプロセス（＝利用カウント）
        /// </summary>
        HashSet<ClothProcess> useProcessSet = new HashSet<ClothProcess>();

        //=========================================================================================
        // セットアップデータ
        internal RenderSetupData setupData;

        internal string Name => setupData?.name ?? "(empty)";

        internal bool HasSkinnedMesh => setupData?.hasSkinnedMesh ?? false;
        internal bool HasBoneWeight => setupData?.hasBoneWeight ?? false;

        //=========================================================================================
        // カスタムメッシュ情報
        Mesh customMesh;
        NativeArray<Vector3> localPositions;
        NativeArray<Vector3> localNormals;
        NativeArray<BoneWeight> boneWeights;
        BoneWeight centerBoneWeight;

        /// <summary>
        /// カスタムメッシュの使用フラグ
        /// </summary>
        public bool UseCustomMesh { get; private set; }

        /// <summary>
        /// カスタムメッシュの変更フラグ
        /// </summary>
        public bool ChangeCustomMesh { get; private set; }

        public bool ChangePositionNormal { get; private set; }
        public bool ChangeBoneWeight { get; private set; }

        //=========================================================================================
        public void Dispose()
        {
            // オリジナルメッシュに戻す
            SwapOriginalMesh();

            setupData?.Dispose();

            if (localPositions.IsCreated)
                localPositions.Dispose();
            if (localNormals.IsCreated)
                localNormals.Dispose();
            if (boneWeights.IsCreated)
                boneWeights.Dispose();

            if (customMesh)
                GameObject.Destroy(customMesh);
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
            setupData?.GetUsedTransform(transformSet);
        }

        public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
        {
            setupData?.ReplaceTransform(replaceDict);
        }

        /// <summary>
        /// 初期化（メインスレッドのみ）
        /// この処理はスレッド化できないので少し負荷がかかるが即時実行する
        /// </summary>
        /// <param name="ren"></param>
        internal void Initialize(Renderer ren)
        {
            Debug.Assert(ren);

            // セットアップデータ作成
            setupData = new RenderSetupData(ren);

            // センタートランスフォーム用ボーンウエイト
            centerBoneWeight = new BoneWeight();
            centerBoneWeight.boneIndex0 = setupData.renderTransformIndex;
            centerBoneWeight.weight0 = 1.0f;
        }

        internal ResultCode Result => setupData?.result ?? ResultCode.None;

        //=========================================================================================
        internal int AddReferenceCount()
        {
            ReferenceCount++;
            return ReferenceCount;
        }

        internal int RemoveReferenceCount()
        {
            ReferenceCount--;
            return ReferenceCount;
        }

        //=========================================================================================
        void SwapCustomMesh()
        {
            Debug.Assert(setupData != null);

            if (setupData.IsFaild())
                return;
            if (setupData.originalMesh == null)
                return;

            // カスタムメッシュの作成
            if (customMesh == null)
            {
                Debug.Assert(setupData.originalMesh);

                // クローン作成
                customMesh = GameObject.Instantiate(setupData.originalMesh);
                customMesh.MarkDynamic();

                // 作業配列
                int vertexCount = setupData.vertexCount;
                localPositions = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
                localNormals = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
                if (HasBoneWeight)
                    boneWeights = new NativeArray<BoneWeight>(vertexCount, Allocator.Persistent);

                // bind pose
                if (HasBoneWeight)
                {
                    int transformCount = setupData.TransformCount;
                    var bindPoseList = new List<Matrix4x4>(transformCount);
                    bindPoseList.AddRange(setupData.bindPoseList);
                    // rootBone/skinning bones
                    while (bindPoseList.Count < transformCount)
                        bindPoseList.Add(Matrix4x4.identity);
                    customMesh.bindposes = bindPoseList.ToArray();
                }
            }

            // 作業バッファリセット
            ResetCustomMeshWorkData();

            // カスタムメッシュに表示切り替え
            SetMesh(customMesh);

            // スキニング用ボーンを書き換える
            if (HasBoneWeight)
            {
                // このリストにはオリジナルのスキニングボーン＋レンダラーのトランスフォームが含まれている
                setupData.skinRenderer.bones = setupData.transformList.ToArray();
            }

            UseCustomMesh = true;
        }

        void ResetCustomMeshWorkData()
        {
            // オリジナルデータをコピーする
            var meshData = setupData.meshDataArray[0];
            meshData.GetVertices(localPositions);
            meshData.GetNormals(localNormals);
            if (HasBoneWeight)
            {
                setupData.GetBoneWeightsRun(boneWeights);
            }
        }

        /// <summary>
        /// オリジナルメッシュに戻す
        /// </summary>
        void SwapOriginalMesh()
        {
            if (UseCustomMesh && setupData != null)
            {
                SetMesh(setupData.originalMesh);

                if (setupData.skinRenderer != null)
                {
                    setupData.skinRenderer.bones = setupData.transformList.ToArray();
                }
            }

            UseCustomMesh = false;
        }

        /// <summary>
        /// レンダラーにメッシュを設定する
        /// </summary>
        /// <param name="mesh"></param>
        void SetMesh(Mesh mesh)
        {
            if (mesh == null)
                return;

            if (setupData != null)
            {
                if (setupData.meshFilter != null)
                {
                    setupData.meshFilter.mesh = mesh;
                }
                else if (setupData.skinRenderer != null)
                {
                    setupData.skinRenderer.sharedMesh = mesh;
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// 利用の開始
        /// 利用するということはメッシュに頂点を書き込むことを意味する
        /// 通常コンポーネントがEnableになったときに行う
        /// </summary>
        public void StartUse(ClothProcess cprocess)
        {
            UpdateUse(cprocess, 1);
        }

        /// <summary>
        /// 利用の停止
        /// 停止するということはメッシュに頂点を書き込まないことを意味する
        /// 通常コンポーネントがDisableになったときに行う
        /// </summary>
        public void EndUse(ClothProcess cprocess)
        {
            Debug.Assert(useProcessSet.Count > 0);
            UpdateUse(cprocess, -1);
        }

        internal void UpdateUse(ClothProcess cprocess, int add)
        {
            if (add > 0)
            {
                useProcessSet.Add(cprocess);
            }
            else if (add < 0)
            {
                Debug.Assert(useProcessSet.Count > 0);
                useProcessSet.Remove(cprocess);
            }

            // Invisible状態
            bool invisible = useProcessSet.Any(x => x.IsCullingInvisible() && x.IsCullingKeep() == false);

            // 状態変更
            if (invisible || useProcessSet.Count == 0)
            {
                // 利用停止
                // オリジナルメッシュに切り替え
                SwapOriginalMesh();
                ChangeCustomMesh = true;
            }
            else if (useProcessSet.Count == 1)
            {
                // 利用開始
                // カスタムメッシュに切り替え、および作業バッファ作成
                // すでにカスタムメッシュが存在する場合は作業バッファのみ最初期化する
                SwapCustomMesh();
                ChangeCustomMesh = true;
            }
            else if (add != 0)
            {
                // 複数から利用されている状態で１つが停止した。
                // バッファを最初期化する
                ResetCustomMeshWorkData();
                ChangeCustomMesh = true;
            }
        }

        //=========================================================================================
        internal void WriteMesh()
        {
            if (UseCustomMesh == false || useProcessSet.Count == 0)
                return;

            // メッシュに反映
            if (ChangePositionNormal)
            {
                customMesh.SetVertices(localPositions);
                customMesh.SetNormals(localNormals);
            }
            if (ChangeBoneWeight && HasBoneWeight)
            {
                customMesh.boneWeights = boneWeights.ToArray();
            }

            // 完了
            ChangeCustomMesh = false;
            ChangePositionNormal = false;
            ChangeBoneWeight = false;
        }

        //=========================================================================================
        /// <summary>
        /// メッシュの位置法線を更新
        /// </summary>
        /// <param name="mappingChunk"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle UpdatePositionNormal(DataChunk mappingChunk, JobHandle jobHandle = default)
        {
            if (UseCustomMesh == false)
                return jobHandle;

            var vm = MagicaManager.VMesh;

            // 座標・法線の差分書き換え
            var job = new UpdatePositionNormalJob2()
            {
                startIndex = mappingChunk.startIndex,

                meshLocalPositions = localPositions.Reinterpret<float3>(),
                meshLocalNormals = localNormals.Reinterpret<float3>(),

                mappingReferenceIndices = vm.mappingReferenceIndices.GetNativeArray(),
                mappingAttributes = vm.mappingAttributes.GetNativeArray(),
                mappingPositions = vm.mappingPositions.GetNativeArray(),
                mappingNormals = vm.mappingNormals.GetNativeArray(),
            };
            jobHandle = job.Schedule(mappingChunk.dataLength, 32, jobHandle);

            ChangePositionNormal = true;

            return jobHandle;
        }

        [BurstCompile]
        struct UpdatePositionNormalJob2 : IJobParallelFor
        {
            public int startIndex;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> meshLocalPositions;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> meshLocalNormals;

            // mapping mesh
            [Unity.Collections.ReadOnly]
            public NativeArray<int> mappingReferenceIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> mappingAttributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> mappingPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> mappingNormals;

            public void Execute(int index)
            {
                int vindex = index + startIndex;

                // 無効頂点なら書き込まない
                var attr = mappingAttributes[vindex];
                if (attr.IsInvalid())
                    return;

                // 固定も書き込まない(todo:一旦こうする）
                if (attr.IsFixed())
                    return;

                // 書き込む頂点インデックス
                int windex = mappingReferenceIndices[vindex];

                // 座標書き込み
                meshLocalPositions[windex] = mappingPositions[vindex];

                // 法線書き込み
                meshLocalNormals[windex] = mappingNormals[vindex];
            }
        }

        /// <summary>
        /// メッシュのボーンウエイト書き込み
        /// </summary>
        /// <param name="vmesh"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle UpdateBoneWeight(DataChunk mappingChunk, JobHandle jobHandle = default)
        {
            if (UseCustomMesh == false)
                return jobHandle;

            // ボーンウエイトの差分書き換え
            if (HasBoneWeight)
            {
                var vm = MagicaManager.VMesh;

                var job = new UpdateBoneWeightJob2()
                {
                    startIndex = mappingChunk.startIndex,
                    centerBoneWeight = centerBoneWeight,
                    meshBoneWeights = boneWeights,

                    mappingReferenceIndices = vm.mappingReferenceIndices.GetNativeArray(),
                    mappingAttributes = vm.mappingAttributes.GetNativeArray(),
                };
                jobHandle = job.Schedule(mappingChunk.dataLength, 32, jobHandle);

                ChangeBoneWeight = true;
            }

            return jobHandle;
        }

        [BurstCompile]
        struct UpdateBoneWeightJob2 : IJobParallelFor
        {
            public int startIndex;
            public BoneWeight centerBoneWeight;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<BoneWeight> meshBoneWeights;

            // mapping mesh
            [Unity.Collections.ReadOnly]
            public NativeArray<int> mappingReferenceIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> mappingAttributes;

            public void Execute(int index)
            {
                int vindex = index + startIndex;

                // 無効頂点なら書き込まない
                var attr = mappingAttributes[vindex];
                if (attr.IsInvalid())
                    return;

                // 固定も書き込まない(todo:一旦こうする）
                if (attr.IsFixed())
                    return;

                // 書き込む頂点インデックス
                int windex = mappingReferenceIndices[vindex];

                // 使用頂点のウエイトはcenterTransform100%で書き込む
                meshBoneWeights[windex] = centerBoneWeight;
            }
        }

        //=========================================================================================
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($">>> [{Name}] ref:{ReferenceCount}, useProcess:{useProcessSet.Count}, HasSkinnedMesh:{HasSkinnedMesh}, HasBoneWeight:{HasBoneWeight}");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
