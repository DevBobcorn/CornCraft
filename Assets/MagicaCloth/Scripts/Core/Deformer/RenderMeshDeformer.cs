// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// レンダラーメッシュデフォーマー
    /// </summary>
    [System.Serializable]
    public class RenderMeshDeformer : BaseMeshDeformer, IBoneReplace
    {
        /// <summary>
        /// データバージョン
        /// </summary>
        private const int DATA_VERSION = 2;

        /// <summary>
        /// 再計算モード
        /// </summary>
        public enum RecalculateMode
        {
            // なし
            None = 0,

            // 法線再計算あり
            UpdateNormalPerFrame = 1,

            // 法線・接線再計算あり
            UpdateNormalAndTangentPerFrame = 2,
        }

        // 法線/接線更新モード
        [SerializeField]
        private RecalculateMode normalAndTangentUpdateMode = RecalculateMode.UpdateNormalPerFrame;

        /// <summary>
        /// バウンディングボックス計算モード
        /// </summary>
        public enum BoundsMode
        {
            None = 0,

            // 初期化時に拡張
            ExpandedAtInitialization = 1,

            // 毎フレーム再計算（重い）
            //RecalculatedPerFrame = 2,
        }
        [SerializeField]
        private BoundsMode boundsUpdateMode = BoundsMode.None;


        [SerializeField]
        private Mesh sharedMesh = null;

        /// <summary>
        /// メッシュの最適化情報
        /// </summary>
        [SerializeField]
        private int meshOptimize = 0;

        // ランタイムデータ //////////////////////////////////////////
        // 書き込み用
        Renderer renderer;
        MeshFilter meshFilter;
        SkinnedMeshRenderer skinMeshRenderer;
        Transform[] originalBones;
        Transform[] boneList;
        Mesh cloneMesh = null;
        GraphicsBuffer vertexBuffer;

        // メッシュ状態変更フラグ
        bool IsChangePosition { get; set; }
        bool IsChangeNormalTangent { get; set; }
        bool IsChangeBoneWeights { get; set; }
        bool oldUse;
        internal bool IsWriteSkip { get; set; }
        internal bool IsFasterWriteUpdate { get; private set; }
        internal bool IsWriteMeshPosition { get; private set; }
        internal bool IsWriteMeshBoneWeight { get; private set; }
        bool IsWriteMeshNormal;
        bool IsWriteMeshTangent;

        //=========================================================================================
        /// <summary>
        /// データを識別するハッシュコードを作成して返す
        /// </summary>
        /// <returns></returns>
        public override int GetDataHash()
        {
            int hash = base.GetDataHash();
            hash += sharedMesh.GetDataHash();
            if (meshOptimize != 0) // 下位互換のため
                hash += meshOptimize.GetDataHash();
            return hash;
        }

        //=========================================================================================
        public Mesh SharedMesh
        {
            get
            {
                return sharedMesh;
            }
        }

        /// <summary>
        /// VertexBufferを利用した高速書き込みの判定フラグ
        /// 実際にはメッシュの頂点構造により利用できない場合があるのでメッシュごとに個別
        /// </summary>
        internal bool IsFasterWrite { get; private set; } = false;


        //=========================================================================================
        public void OnValidate()
        {
            if (Application.isPlaying == false)
                return;

            if (status.IsActive)
            {
                // 法線／接線再計算モード設定
                SetRecalculateNormalAndTangentMode();
            }
        }

        /// <summary>
        /// 初期化
        /// </summary>
        protected override void OnInit()
        {
            base.OnInit();
            if (status.IsInitError)
                return;

            // レンダラーチェック
            if (TargetObject == null)
            {
                status.SetInitError();
                return;
            }
            renderer = TargetObject.GetComponent<Renderer>();
            if (renderer == null)
            {
                status.SetInitError();
                return;
            }

            if (MeshData.VerifyData() != Define.Error.None)
            {
                status.SetInitError();
                return;
            }

            VertexCount = MeshData.VertexCount;
            TriangleCount = MeshData.TriangleCount;

            // 高速書き込み判定
            // メッシュの頂点構成によっては利用できない場合がある
            IsFasterWrite = false;
#if UNITY_2021_2_OR_NEWER
            if (MagicaPhysicsManager.Instance.IsFasterWrite)
            {
                int check = 0;
                int attrCnt = sharedMesh.vertexAttributeCount;
                for (int i = 0; i < attrCnt; i++)
                {
                    var attr = sharedMesh.GetVertexAttribute(i);
                    //Debug.Log($"[{sharedMesh.name}] {attr}");
                    if (attr.attribute == UnityEngine.Rendering.VertexAttribute.Position && attr.format == UnityEngine.Rendering.VertexAttributeFormat.Float32 && attr.dimension == 3 && attr.stream == 0)
                    {
                        //Debug.Log($"[{sharedMesh.name}] Position OK!");
                        check++;
                    }
                    if (attr.attribute == UnityEngine.Rendering.VertexAttribute.Normal && attr.format == UnityEngine.Rendering.VertexAttributeFormat.Float32 && attr.dimension == 3 && attr.stream == 0)
                    {
                        //Debug.Log($"[{sharedMesh.name}] Normal OK!");
                        check++;
                    }
                }
                if (check == 2)
                {
                    // Position,Normalがfloat3であり共にStream=0で並んでいるので利用可能！
                    IsFasterWrite = true;
                    //Debug.Log($"[{sharedMesh.name}] IsHighSpeedWriting ON!");
                }
            }
#endif

            // クローンメッシュ作成
            // ここではメッシュは切り替えない
            cloneMesh = null;
            if (renderer is SkinnedMeshRenderer)
            {
                var sren = renderer as SkinnedMeshRenderer;
                skinMeshRenderer = sren;

                // オリジナルのボーンリスト
                originalBones = sren.bones;

                // srenのボーンリストはここで配列を作成し最後にレンダラーのトランスフォームを追加する
                var blist = new List<Transform>(originalBones);
                blist.Add(sren.rootBone); // (old)renderer.transform
                boneList = blist.ToArray();

                // 高速書き込み時でもスキンレンダラーのクローンメッシュは作成する
                // SkinnedMeshRenderer.GetVertexBuffer()はほとんどのケースでうまく動作するが、キャラクタが描画対象となったフレームのみ
                // カリング処理中に強制スキニング処理が実行されクロスシミュレーションの結果が無効化されてしまう大きな問題がある。
                // この問題の回避方法は今の所無いため、仕方なくクローンメッシュを作成しMesh.GerVertexBuffer()での処理に切り替える。
                // パフォーマンスに関してもMesh.GetVertexBuffer()の方が僅かに遅くなってしまうが致し方なし。
                cloneMesh = GameObject.Instantiate(sharedMesh);

                var bindlist = new List<Matrix4x4>(sharedMesh.bindposes);
                bindlist.Add(Matrix4x4.identity); // レンダラーのバインドポーズを最後に追加
                cloneMesh.bindposes = bindlist.ToArray();
            }
            else
            {
                // メッシュクローン
                cloneMesh = GameObject.Instantiate(sharedMesh);

#if UNITY_2021_2_OR_NEWER
                if (IsFasterWrite)
                {
                    cloneMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                }
#endif

                meshFilter = TargetObject.GetComponent<MeshFilter>();
                Debug.Assert(meshFilter);
            }
            oldUse = false;

#if !UNITY_EDITOR_OSX
            if (IsFasterWrite == false)
            {
                // MacではMetal関連でエラーが発生するので対応（エディタ環境のみ）
                cloneMesh.MarkDynamic();
            }
#endif

            // バウンディングボックスの拡張(v1.11.1)
            if (boundsUpdateMode == BoundsMode.ExpandedAtInitialization)
            {
                var bounds = skinMeshRenderer ? skinMeshRenderer.localBounds : sharedMesh.bounds;
                //Debug.Log($"original bounds:{bounds}");

                // XYZの最大サイズx2に拡張する
                float maxSize = Mathf.Max(Mathf.Max(bounds.extents.x, bounds.extents.y), bounds.extents.z);
                maxSize *= 2.0f;
                bounds.extents = Vector3.one * maxSize;
                //Debug.Log($"new bounds:{bounds}");

                if (skinMeshRenderer)
                    skinMeshRenderer.localBounds = bounds;
                else
                    cloneMesh.bounds = bounds;
            }

            // 共有メッシュのuid
            int uid = sharedMesh.GetInstanceID(); // 共有メッシュのIDを使う
            bool first = MagicaPhysicsManager.Instance.Mesh.IsEmptySharedRenderMesh(uid);

            // メッシュ登録
            MeshIndex = MagicaPhysicsManager.Instance.Mesh.AddRenderMesh(
                uid,
                MeshData.isSkinning,
                IsFasterWrite,
                MeshData.baseScale,
                MeshData.VertexCount,
                IsSkinning ? boneList.Length - 1 : 0, // レンダラーのボーンインデックス
                IsSkinning ? sharedMesh.GetAllBoneWeights().Length : 0
                );

            // レンダーメッシュの共有データを一次元配列にコピーする
            if (first)
            {
                MagicaPhysicsManager.Instance.Mesh.SetRenderSharedMeshData(
                    MeshIndex,
                    IsSkinning,
                    sharedMesh.vertices,
                    sharedMesh.normals,
                    sharedMesh.tangents,
                    sharedMesh.GetBonesPerVertex(),
                    sharedMesh.GetAllBoneWeights()
                    );
            }

            // レンダーメッシュ情報確定
            // すべてのデータが確定してから実行しないと駄目なもの
            MagicaPhysicsManager.Instance.Mesh.UpdateMeshState(MeshIndex);

            // 法線／接線再計算モード設定
            SetRecalculateNormalAndTangentMode();
        }

        /// <summary>
        /// 実行状態に入った場合に呼ばれます
        /// </summary>
        protected override void OnActive()
        {
            base.OnActive();
            if (status.IsInitSuccess)
            {
                MagicaPhysicsManager.Instance.Mesh.SetRenderMeshActive(MeshIndex, true);

                // レンダラートランスフォーム登録
                // スキンレンダラーならスキンレンダラーのルートボーンを指定する
                var meshRootTransform = skinMeshRenderer ? skinMeshRenderer.rootBone : TargetObject.transform; // (old)TargetObject.transform
                MagicaPhysicsManager.Instance.Mesh.AddRenderMeshBone(MeshIndex, meshRootTransform);
            }
        }

        /// <summary>
        /// 実行状態から抜けた場合に呼ばれます
        /// </summary>
        protected override void OnInactive()
        {
            base.OnInactive();
            if (status.IsInitSuccess)
            {
                if (MagicaPhysicsManager.IsInstance())
                {
                    // レンダラートランスフォーム解除
                    MagicaPhysicsManager.Instance.Mesh.RemoveRenderMeshBone(MeshIndex);

                    MagicaPhysicsManager.Instance.Mesh.SetRenderMeshActive(MeshIndex, false);
                }
            }

            // 頂点バッファ解放
            // レンダラーが非表示になった場合はVertexBufferを再取得する必要がある
            if (vertexBuffer != null)
            {
                vertexBuffer.Dispose();
                vertexBuffer = null;
            }
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public override void Dispose()
        {
            if (MagicaPhysicsManager.IsInstance())
            {
                // メッシュ解除
                MagicaPhysicsManager.Instance.Mesh.RemoveRenderMesh(MeshIndex);
            }

            // 頂点バッファ解放
            if (vertexBuffer != null)
                vertexBuffer.Dispose();

            // クローンメッシュ解放
            if (cloneMesh)
                GameObject.Destroy(cloneMesh);

            base.Dispose();
        }

        /// <summary>
        /// 法線／接線再計算モード設定
        /// </summary>
        void SetRecalculateNormalAndTangentMode()
        {
            // ジョブシステムを利用した法線／接線再計算設定
            bool normal = false;
            bool tangent = false;
            if (normalAndTangentUpdateMode == RecalculateMode.UpdateNormalPerFrame)
            {
                normal = true;
            }
            else if (normalAndTangentUpdateMode == RecalculateMode.UpdateNormalAndTangentPerFrame)
            {
                normal = tangent = true;
            }
            MagicaPhysicsManager.Instance.Mesh.SetRenderMeshFlag(MeshIndex, PhysicsManagerMeshData.Meshflag_CalcNormal, normal);
            MagicaPhysicsManager.Instance.Mesh.SetRenderMeshFlag(MeshIndex, PhysicsManagerMeshData.Meshflag_CalcTangent, tangent);
        }

        /// <summary>
        /// UnityPhysicsでの利用を設定する
        /// </summary>
        /// <param name="sw"></param>
        public override void ChangeUseUnityPhysics(bool sw)
        {
            if (status.IsInitSuccess)
            {
                MagicaPhysicsManager.Instance.Mesh.ChangeRenderMeshUseUnityPhysics(MeshIndex, sw);
            }
        }

        public void ChangeCalculation(bool sw)
        {
            if (status.IsInitSuccess)
            {
                MagicaPhysicsManager.Instance.Mesh.SetRenderMeshFlag(MeshIndex, PhysicsManagerMeshData.Meshflag_Pause, !sw);
            }
        }

        //=========================================================================================
        public override bool IsMeshUse()
        {
            if (status.IsInitSuccess)
            {
                return MagicaPhysicsManager.Instance.Mesh.IsUseRenderMesh(MeshIndex);
            }

            return false;
        }

        public override bool IsActiveMesh()
        {
            if (status.IsInitSuccess)
            {
                return MagicaPhysicsManager.Instance.Mesh.IsActiveRenderMesh(MeshIndex);
            }

            return false;
        }

        public override void AddUseMesh(System.Object parent)
        {
            var virtualMeshDeformer = parent as VirtualMeshDeformer;
            Debug.Assert(virtualMeshDeformer != null);

            if (status.IsInitSuccess)
            {
                //Develop.Log($"★AddUseMesh:{this.Parent.name} meshIndex:{MeshIndex}");

                MagicaPhysicsManager.Instance.Mesh.AddUseRenderMesh(MeshIndex);
                IsChangePosition = true;
                IsChangeNormalTangent = true;
                IsChangeBoneWeights = true;

                // 親仮想メッシュと連動させる
                int virtualMeshIndex = virtualMeshDeformer.MeshIndex;
                var virtualMeshInfo = MagicaPhysicsManager.Instance.Mesh.virtualMeshInfoList[virtualMeshIndex];
                var sharedVirtualMeshInfo = MagicaPhysicsManager.Instance.Mesh.sharedVirtualMeshInfoList[virtualMeshInfo.sharedVirtualMeshIndex];
                int index = virtualMeshDeformer.GetRenderMeshDeformerIndex(this);
                long cuid = (long)sharedVirtualMeshInfo.uid << 16 + index;
                int sharedChildMeshIndex = MagicaPhysicsManager.Instance.Mesh.sharedChildMeshIdToSharedVirtualMeshIndexDict[cuid];
                var sharedChildMeshInfo = MagicaPhysicsManager.Instance.Mesh.sharedChildMeshInfoList[sharedChildMeshIndex];

                MagicaPhysicsManager.Instance.Mesh.LinkRenderMesh(
                    MeshIndex,
                    sharedChildMeshInfo.vertexChunk.startIndex,
                    sharedChildMeshInfo.weightChunk.startIndex,
                    virtualMeshInfo.vertexChunk.startIndex,
                    sharedVirtualMeshInfo.vertexChunk.startIndex
                    );

                // 利用頂点更新
                //MagicaPhysicsManager.Instance.Compute.RenderMeshWorker.SetUpdateUseFlag();
            }
        }

        public override void RemoveUseMesh(System.Object parent)
        {
            //base.RemoveUseMesh();

            var virtualMeshDeformer = parent as VirtualMeshDeformer;
            Debug.Assert(virtualMeshDeformer != null);

            if (status.IsInitSuccess)
            {
                // 親仮想メッシュとの連動を解除する
                int virtualMeshIndex = virtualMeshDeformer.MeshIndex;
                var virtualMeshInfo = MagicaPhysicsManager.Instance.Mesh.virtualMeshInfoList[virtualMeshIndex];
                var sharedVirtualMeshInfo = MagicaPhysicsManager.Instance.Mesh.sharedVirtualMeshInfoList[virtualMeshInfo.sharedVirtualMeshIndex];
                int index = virtualMeshDeformer.GetRenderMeshDeformerIndex(this);
                long cuid = (long)sharedVirtualMeshInfo.uid << 16 + index;
                int sharedChildMeshIndex = MagicaPhysicsManager.Instance.Mesh.sharedChildMeshIdToSharedVirtualMeshIndexDict[cuid];
                var sharedChildMeshInfo = MagicaPhysicsManager.Instance.Mesh.sharedChildMeshInfoList[sharedChildMeshIndex];

                MagicaPhysicsManager.Instance.Mesh.UnlinkRenderMesh(
                    MeshIndex,
                    sharedChildMeshInfo.vertexChunk.startIndex,
                    sharedChildMeshInfo.weightChunk.startIndex,
                    virtualMeshInfo.vertexChunk.startIndex,
                    sharedVirtualMeshInfo.vertexChunk.startIndex
                    );


                MagicaPhysicsManager.Instance.Mesh.RemoveUseRenderMesh(MeshIndex);
                IsChangePosition = true;
                IsChangeNormalTangent = true;
                IsChangeBoneWeights = true;

                // 利用頂点更新
                //MagicaPhysicsManager.Instance.Compute.RenderMeshWorker.SetUpdateUseFlag();
            }
        }

        //=========================================================================================
        public bool IsRendererVisible
        {
            get
            {
                return renderer ? renderer.isVisible : false;
            }
        }

        internal bool HasNormal
        {
            get
            {
                return normalAndTangentUpdateMode == RecalculateMode.UpdateNormalPerFrame || normalAndTangentUpdateMode == RecalculateMode.UpdateNormalAndTangentPerFrame;
            }
        }

        //=========================================================================================
        /// <summary>
        /// メッシュの書き込み判定
        /// </summary>
        /// <param name="bufferIndex"></param>
        internal override void MeshCalculation(int bufferIndex)
        {
            IsFasterWriteUpdate = false;
            IsWriteMeshPosition = false;
            IsWriteMeshNormal = false;
            IsWriteMeshTangent = false;
            IsWriteMeshBoneWeight = false;

            bool use = IsMeshUse();

            // 計算状態
            if (Parent.IsCalculate == false && Status.IsActive)
            {
                //Debug.Log($"Finish Skip! :{Parent.name}");
                switch ((Parent as MagicaRenderDeformer)?.cullModeCash)
                {
                    case PhysicsTeam.TeamCullingMode.Pause:
                        // 終了する
                        return;
                    case PhysicsTeam.TeamCullingMode.Reset:
                        // 元のメッシュに戻す
                        use = false;
                        break;
                }
            }

            // 頂点の姿勢／ウエイトの計算状態
            bool vertexCalc = false;
            if (use)
            {
                if (bufferIndex == 1)
                {
                    var state = MagicaPhysicsManager.Instance.Mesh.renderMeshStateDict[MeshIndex];
                    vertexCalc = state.IsFlag(PhysicsManagerMeshData.RenderStateFlag_DelayedCalculated);
                }
                else
                    vertexCalc = true;
            }
            if (vertexCalc == false)
            {
                use = false;
            }

            if (use && IsWriteSkip)
            {
                use = false;
                IsWriteSkip = false;
            }

            //Debug.Log($"Write Mesh. MeshUse:{IsMeshUse()} use:{use} vertexCalc:{vertexCalc} Calc:{Parent.IsCalculate} buffIndex:{bufferIndex} F:{Time.frameCount}");

#if true
            // メッシュ切替
            // 頂点変形が不要な場合は元の共有メッシュに戻す
            if (use != oldUse)
            {
                if (meshFilter)
                {
                    meshFilter.mesh = use ? cloneMesh : sharedMesh;
                }
                else if (skinMeshRenderer)
                {
                    skinMeshRenderer.sharedMesh = use ? cloneMesh : sharedMesh;
                    skinMeshRenderer.bones = use ? boneList : originalBones;
                }
                oldUse = use;

                if (use)
                {
                    IsChangePosition = true;
                    IsChangeNormalTangent = true;
                    IsChangeBoneWeights = true;
                }
                else
                {
                    // 頂点バッファがある場合は解放する
                    if (vertexBuffer != null)
                    {
                        vertexBuffer.Dispose();
                        vertexBuffer = null;
                    }
                }
            }

            // 更新不要ならば抜ける
            if (vertexCalc == false)
                return;

            // 法線／接線の更新状態
            bool normal = normalAndTangentUpdateMode == RecalculateMode.UpdateNormalPerFrame || normalAndTangentUpdateMode == RecalculateMode.UpdateNormalAndTangentPerFrame;
            bool tangent = normalAndTangentUpdateMode == RecalculateMode.UpdateNormalAndTangentPerFrame;

            // すでに法線／接線が不要ならばもとに戻す
            if (IsChangeNormalTangent && normal == false && tangent == false)
            {
                // 元に戻す
                cloneMesh.normals = sharedMesh.normals;
                cloneMesh.tangents = sharedMesh.tangents;
                IsChangeNormalTangent = false;
            }

            // メッシュ書き込み
            if (IsFasterWrite)
            {
                // 高速書き込み
                if (use || IsChangePosition)
                {
                    IsFasterWriteUpdate = true;
                    IsChangePosition = false;
                }
            }
            else
            {
                // 旧来のメッシュ頂点書き換え判定
                if ((use || IsChangePosition))
                {
                    IsWriteMeshPosition = true;
                    if (normal)
                        IsWriteMeshNormal = true;
                    if (tangent)
                        IsWriteMeshTangent = true;
                    IsChangePosition = false;
                }
            }
            if (use && IsSkinning && IsChangeBoneWeights)
            {
                // 頂点ウエイト変更判定
                //Debug.Log("Change Mesh Weights:" + mesh.name + " buff:" + bufferIndex + " frame:" + Time.frameCount);
                IsWriteMeshBoneWeight = true;
                IsChangeBoneWeights = false;
            }
#endif
        }


        /// <summary>
        /// 通常のメッシュ書き込み
        /// </summary>
        internal override void NormalWriting(int bufferIndex)
        {
            if (IsWriteMeshPosition)
            {
                // 旧式のメッシュ書き戻し（重い）
                MagicaPhysicsManager.Instance.Mesh.CopyToRenderMeshLocalPositionData(MeshIndex, cloneMesh, bufferIndex);
                if (IsWriteMeshNormal || IsWriteMeshTangent)
                {
                    MagicaPhysicsManager.Instance.Mesh.CopyToRenderMeshLocalNormalTangentData(MeshIndex, cloneMesh, bufferIndex, IsWriteMeshNormal, IsWriteMeshTangent);
                }
            }

            if (IsWriteMeshBoneWeight)
            {
                //Debug.Log($"BoneWeights Write:{renderer.name} F:{Time.frameCount}");
                // 頂点ウエイト変更
                MagicaPhysicsManager.Instance.Mesh.CopyToRenderMeshBoneWeightData(MeshIndex, cloneMesh, sharedMesh, bufferIndex);

#if UNITY_2021_2_OR_NEWER
                // 旧来のメソッドでメッシュを変更した場合はVertexBufferが無効となるので作り直す必要あり
                vertexBuffer?.Release();
                vertexBuffer?.Dispose();
                vertexBuffer = null;
#endif
            }
        }

        /// <summary>
        /// 高速なメッシュ書き込み
        /// </summary>
        /// <param name="bufferIndex"></param>
        internal bool FasterWriting(int bufferIndex, ComputeShader compute, int kernel, int index, ref int maxVertexCount)
        {
            if (IsFasterWriteUpdate == false)
                return false;

#if UNITY_2021_2_OR_NEWER
            // 頂点バッファの確保
            if (vertexBuffer == null)
                vertexBuffer = cloneMesh.GetVertexBuffer(0);
            if (vertexBuffer == null)
                return false;

            // 法線／接線の更新状態
            bool normal = normalAndTangentUpdateMode == RecalculateMode.UpdateNormalPerFrame || normalAndTangentUpdateMode == RecalculateMode.UpdateNormalAndTangentPerFrame;

            // 書き込み
            //Debug.Log($"FasterWrite:{renderer.name} F:{Time.frameCount}");
            MagicaPhysicsManager.Instance.Mesh.CopyToRenderVertexBuffer(MeshIndex, bufferIndex, vertexBuffer, normal, compute, kernel, index);
            maxVertexCount = Mathf.Max(maxVertexCount, vertexBuffer.count);
#endif

            IsFasterWriteUpdate = false;

            return true;
        }

        /// <summary>
        /// 外部からの法線／接線の計算方法変更対応
        /// </summary>
        public void ChangeNormalTangentUpdateMode()
        {
            // 法線／接線の切り替えを再確認するフラグを立てる
            IsChangeNormalTangent = true;
        }

        //=========================================================================================
        /// <summary>
        /// ボーンを置換する
        /// </summary>
        public void ReplaceBone<T>(Dictionary<T, Transform> boneReplaceDict) where T : class
        {
            if (originalBones != null)
            {
                for (int i = 0; i < originalBones.Length; i++)
                {
                    originalBones[i] = MeshUtility.GetReplaceBone(originalBones[i], boneReplaceDict);
                }
            }

            if (boneList != null)
            {
                for (int i = 0; i < boneList.Length; i++)
                {
                    boneList[i] = MeshUtility.GetReplaceBone(boneList[i], boneReplaceDict);
                }
            }
        }

        /// <summary>
        /// 現在使用しているボーンを格納して返す
        /// </summary>
        /// <returns></returns>
        public HashSet<Transform> GetUsedBones()
        {
            var bonesSet = new HashSet<Transform>();
            if (originalBones != null)
                foreach (var t in originalBones)
                    bonesSet.Add(t);
            if (boneList != null)
                foreach (var t in boneList)
                    bonesSet.Add(t);
            return bonesSet;
        }

        //=========================================================================================
        /// <summary>
        /// メッシュのワールド座標/法線/接線を返す（エディタ設定用）
        /// </summary>
        /// <param name="wposList"></param>
        /// <param name="wnorList"></param>
        /// <param name="wtanList"></param>
        /// <returns>頂点数</returns>
        public override int GetEditorPositionNormalTangent(
            out List<Vector3> wposList,
            out List<Vector3> wnorList,
            out List<Vector3> wtanList
            )
        {
            wposList = new List<Vector3>();
            wnorList = new List<Vector3>();
            wtanList = new List<Vector3>();

            if (Application.isPlaying)
            {
                if (Status.IsDispose)
                    return 0;

                if (IsMeshUse() == false || TargetObject == null)
                    return 0;

                Vector3[] posArray = new Vector3[VertexCount];
                Vector3[] norArray = new Vector3[VertexCount];
                Vector3[] tanArray = new Vector3[VertexCount];
                var meshRootTransform = skinMeshRenderer ? skinMeshRenderer.rootBone : TargetObject.transform; // (old)TargetObject.transform
                MagicaPhysicsManager.Instance.Mesh.CopyToRenderMeshWorldData(MeshIndex, meshRootTransform, posArray, norArray, tanArray);

                wposList = new List<Vector3>(posArray);
                wnorList = new List<Vector3>(norArray);
                wtanList = new List<Vector3>(tanArray);

                return VertexCount;
            }
            else
            {
                if (TargetObject == null)
                {
                    return 0;
                }
                var ren = TargetObject.GetComponent<Renderer>();
                MeshUtility.CalcMeshWorldPositionNormalTangent(ren, sharedMesh, out wposList, out wnorList, out wtanList);

                return wposList.Count;
            }
        }

        /// <summary>
        /// メッシュのトライアングルリストを返す（エディタ設定用）
        /// </summary>
        /// <returns></returns>
        public override List<int> GetEditorTriangleList()
        {
            if (sharedMesh)
            {
                return new List<int>(sharedMesh.triangles);
            }

            return null;
        }

        /// <summary>
        /// メッシュのラインリストを返す（エディタ用）
        /// </summary>
        /// <returns></returns>
        public override List<int> GetEditorLineList()
        {
            // レンダーデフォーマーでは存在しない
            return null;
        }

        /// <summary>
        /// メッシュの使用頂点リストを返す（エディタ用）
        /// </summary>
        /// <returns></returns>
        public List<int> GetEditorUseList()
        {
            if (Application.isPlaying && IsMeshUse())
            {
                return MagicaPhysicsManager.Instance.Mesh.GetVertexUseList(MeshIndex);
            }
            else
                return null;
        }

        //=========================================================================================
        public override int GetVersion()
        {
            return DATA_VERSION;
        }

        /// <summary>
        /// 現在のデータが正常（実行できる状態）か返す
        /// </summary>
        /// <returns></returns>
        public override Define.Error VerifyData()
        {
            var baseError = base.VerifyData();
            if (baseError != Define.Error.None)
                return baseError;

            if (sharedMesh == null)
                return Define.Error.SharedMeshNull;
            if (sharedMesh.isReadable == false)
                return Define.Error.SharedMeshCannotRead;
            var targetMesh = GetTargetSharedMesh();
            if (MeshData != null && targetMesh != null && MeshData.vertexCount != targetMesh.vertexCount)
                return Define.Error.SharedMeshDifferentVertexCount; // 設定頂点数と現在のメッシュの頂点数が異なる

            // 最大頂点数は65535（要望が多いようなら拡張する）
            if (sharedMesh.vertexCount > 65535)
                return Define.Error.MeshVertexCount65535Over;

#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                // メッシュ最適化タイプが異なる場合は頂点順序が変更されているのでNG
                // またモデルインポート設定を参照するので実行時は判定しない
                if (meshOptimize != 0 && meshOptimize != EditUtility.GetOptimizeMesh(sharedMesh))
                    return Define.Error.MeshOptimizeMismatch;

                // KeepQuadsでは動作しない(v1.11.1)
                if (EditUtility.IsKeepQuadsMesh(sharedMesh))
                    return Define.Error.MeshKeepQuads;
            }
#endif

            return Define.Error.None;
        }

        /// <summary>
        /// ターゲットレンダラーの共有メッシュを取得する
        /// </summary>
        /// <returns></returns>
        private Mesh GetTargetSharedMesh()
        {
            if (TargetObject == null)
            {
                return null;
            }
            var ren = TargetObject.GetComponent<Renderer>();
            if (ren == null)
            {
                return null;
            }

            if (ren is SkinnedMeshRenderer)
            {
                var sren = ren as SkinnedMeshRenderer;
                return sren.sharedMesh;
            }
            else
            {
                meshFilter = TargetObject.GetComponent<MeshFilter>();
                return meshFilter.sharedMesh;
            }
        }


        /// <summary>
        /// データ情報
        /// </summary>
        /// <returns></returns>
        public override string GetInformation()
        {
            StaticStringBuilder.Clear();

            var err = VerifyData();
            if (err == Define.Error.None)
            {
                // OK
                StaticStringBuilder.AppendLine("Active: ", Status.IsActive);
                StaticStringBuilder.AppendLine($"Visible: {Parent.IsVisible}");
                StaticStringBuilder.AppendLine($"Calculation:{Parent.IsCalculate}");
                StaticStringBuilder.AppendLine($"Faster Write:{IsFasterWrite}");
                StaticStringBuilder.AppendLine("Skinning: ", MeshData.isSkinning);
                StaticStringBuilder.AppendLine("Vertex: ", MeshData.VertexCount);
                StaticStringBuilder.AppendLine("Triangle: ", MeshData.TriangleCount);
                StaticStringBuilder.Append("Bone: ", MeshData.BoneCount);
            }
            else if (err == Define.Error.EmptyData)
            {
                StaticStringBuilder.Append(Define.GetErrorMessage(err));
            }
            else
            {
                // エラー
                StaticStringBuilder.AppendLine("This mesh data is Invalid!");

                if (Application.isPlaying)
                {
                    StaticStringBuilder.AppendLine("Execution stopped.");
                }
                else
                {
                    StaticStringBuilder.AppendLine("Please create the mesh data.");
                }
                StaticStringBuilder.Append(Define.GetErrorMessage(err));
            }

            return StaticStringBuilder.ToString();
        }
    }
}
