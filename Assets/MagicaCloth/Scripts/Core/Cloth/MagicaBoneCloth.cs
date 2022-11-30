// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicaCloth
{
    /// <summary>
    /// ボーンクロス
    /// </summary>
    [HelpURL("https://magicasoft.jp/magica-cloth-bone-cloth/")]
    [AddComponentMenu("MagicaCloth/MagicaBoneCloth")]
    public class MagicaBoneCloth : BaseCloth
    {
        /// <summary>
        /// データバージョン
        /// </summary>
        private const int DATA_VERSION = 7;

        /// <summary>
        /// エラーデータバージョン
        /// </summary>
        private const int ERR_DATA_VERSION = 3;

        /// <summary>
        /// メッシュデータ
        /// </summary>
        [SerializeField]
        private MeshData meshData = null;

        [SerializeField]
        private int meshDataHash;
        [SerializeField]
        private int meshDataVersion;

        /// <summary>
        /// 使用ルートトランスフォーム情報
        /// </summary>
        [SerializeField]
        private BoneClothTarget clothTarget = new BoneClothTarget();

        /// <summary>
        /// 最終的に使用されるすべてのトランスフォーム情報
        /// </summary>
        [SerializeField]
        private List<Transform> useTransformList = new List<Transform>();
        [SerializeField]
        private List<Vector3> useTransformPositionList = new List<Vector3>();
        [SerializeField]
        private List<Quaternion> useTransformRotationList = new List<Quaternion>();
        [SerializeField]
        private List<Vector3> useTransformScaleList = new List<Vector3>();


        //=========================================================================================
        public override ComponentType GetComponentType()
        {
            return ComponentType.BoneCloth;
        }

        //=========================================================================================
        /// <summary>
        /// データハッシュを求める
        /// </summary>
        /// <returns></returns>
        public override int GetDataHash()
        {
            int hash = base.GetDataHash();
            hash += MeshData.GetDataHash();
            hash += clothTarget.GetDataHash();
            hash += useTransformList.GetDataHash();
            hash += useTransformPositionList.GetDataHash();
            hash += useTransformRotationList.GetDataHash();
            hash += useTransformScaleList.GetDataHash();
            return hash;
        }

        //=========================================================================================
        public BoneClothTarget ClothTarget
        {
            get
            {
                return clothTarget;
            }
        }

        public MeshData MeshData
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    return meshData;
                else
                {
                    // unity2019.3で参照がnullとなる不具合の対処（臨時）
                    var so = new SerializedObject(this);
                    return so.FindProperty("meshData").objectReferenceValue as MeshData;
                }
#else
                return meshData;
#endif
            }
        }

        //=========================================================================================
        protected override void Reset()
        {
            base.Reset();
            ResetParams();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
        }

        protected override void ClothInit()
        {
            // ルートトランスフォームの親をすべて登録する
            ClothTarget.AddParentTransform();

            base.ClothInit();
        }

        protected override void ClothDispose()
        {
            // ルートトランスフォームの親をすべて解除する
            ClothTarget.RemoveParentTransform();

            base.ClothDispose();
        }

        protected override void ClothActive()
        {
            base.ClothActive();
        }

        /// <summary>
        /// 頂点ごとのパーティクルフラグ設定
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected override uint UserFlag(int index)
        {
            uint flag = 0;

            bool isFix = ClothData.IsFixedVertex(index);

            flag |= PhysicsManagerParticleData.Flag_Transform_Restore; // 実行前にlocalPos/localRot復元
            flag |= isFix ? (PhysicsManagerParticleData.Flag_Transform_Read_Pos | PhysicsManagerParticleData.Flag_Transform_Read_Rot) : 0; // トランスフォームをpos/rotに読み込み（固定のみ）
            flag |= PhysicsManagerParticleData.Flag_Transform_Read_Base; // トランスフォームをbasePos/baseRotに読み込み
            flag |= PhysicsManagerParticleData.Flag_Transform_Write; // 最後にトランスフォームへ座標書き込み
            flag |= PhysicsManagerParticleData.Flag_Transform_Parent; // 親トランスフォームを参照する

            return flag;
        }

        /// <summary>
        /// 頂点ごとの連動トランスフォーム設定
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected override Transform UserTransform(int index)
        {
            return GetUseTransform(index);
        }

        /// <summary>
        /// 頂点ごとの連動トランスフォームのLocalPositionを返す（不要な場合は0）
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected override float3 UserTransformLocalPosition(int vindex)
        {
            int index = ClothData.useVertexList[vindex];
            return useTransformPositionList[index];
        }

        /// <summary>
        /// 頂点ごとの連動トランスフォームのLocalRotationを返す（不要な場合はquaternion.identity)
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected override quaternion UserTransformLocalRotation(int vindex)
        {
            int index = ClothData.useVertexList[vindex];
            return useTransformRotationList[index];
        }

        /// <summary>
        /// デフォーマーを返す
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override BaseMeshDeformer GetDeformer()
        {
            // BoneClothには不要
            return null;
        }

        /// <summary>
        /// クロス初期化時に必要なMeshDataを返す（不要ならnull）
        /// </summary>
        /// <returns></returns>
        protected override MeshData GetMeshData()
        {
            return MeshData;
        }

        /// <summary>
        /// クロス初期化の主にワーカーへの登録
        /// </summary>
        protected override void WorkerInit()
        {
            // BoneClothには不要
        }

        /// <summary>
        /// デフォーマーが必須か返す
        /// </summary>
        /// <returns></returns>
        public override bool IsRequiresDeformer()
        {
            // BoneClothには不要
            return false;
        }

        /// <summary>
        /// デフォーマーごとの使用頂点設定
        /// 使用頂点に対して AddUseVertex() / RemoveUseVertex() を実行する
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="deformer"></param>
        protected override void SetDeformerUseVertex(bool sw, BaseMeshDeformer deformer)
        {
            // BoneClothには不要
        }

        /// <summary>
        /// UnityPhyiscsでの更新の変更
        /// 継承クラスは自身の使用するボーンの状態更新などを記述する
        /// </summary>
        /// <param name="sw"></param>
        protected override void ChangeUseUnityPhysics(bool sw)
        {
            base.ChangeUseUnityPhysics(sw);

            if (teamId <= 0)
                return;

            ClothTarget.ChangeUnityPhysicsCount(sw);
        }

        protected override void OnChangeCalculation()
        {
            base.OnChangeCalculation();

            if (IsCalculate)
            {
                // 遅延実行時のみ
                if (MagicaPhysicsManager.Instance.IsDelay)
                {
                    // ルートトランスフォームの親の未来予測をリセットする
                    ClothTarget.ResetFuturePredictionParentTransform();

                    // 読み込みボーンの未来予測をリセットする
                    MagicaPhysicsManager.Instance.Particle.ResetFuturePredictionTransform(particleChunk);
                }
            }

            // 書き込みボーンのフラグ設定
            if (MagicaPhysicsManager.IsInstance())
            {
                //Debug.Log($"ChangeWriteBoneFlag:[{this.name}] CullMode:{CullingMode} Calc:{IsCalculate} F:{Time.frameCount}");
                MagicaPhysicsManager.Instance.Team.ChangeBoneFlag(TeamId, CullingMode, IsCalculate);
            }
        }

        //=========================================================================================
        /// <summary>
        /// 使用するトランスフォームをリストにして返す
        /// </summary>
        /// <returns></returns>
        public List<Transform> GetTransformList()
        {
            HashSet<Transform> tset = new HashSet<Transform>();
            int cnt = clothTarget.RootCount;
            for (int i = 0; i < cnt; i++)
            {
                var root = clothTarget.GetRoot(i);
                if (root != null)
                {
                    var tlist = root.GetComponentsInChildren<Transform>();
                    foreach (var t in tlist)
                        tset.Add(t);
                }
            }
            List<Transform> transformList = new List<Transform>();
            foreach (var t in tset)
            {
                transformList.Add(t);
            }

            return transformList;
        }

        Transform GetUseTransform(int index)
        {
            int vindex = ClothData.useVertexList[index];
            return useTransformList[vindex];
        }

        int UseTransformCount
        {
            get
            {
                return useTransformList.Count;
            }
        }

        /// <summary>
        /// 使用するトランスフォームを登録順にグリッドにして返す
        /// </summary>
        /// <param name="maxLevel"></param>
        /// <returns></returns>
        public List<List<List<Transform>>> GetTransformGrid(out int maxLevel)
        {
            maxLevel = 0;
            List<List<List<Transform>>> grid = new List<List<List<Transform>>>();
            int cnt = clothTarget.RootCount;
            for (int i = 0; i < cnt; i++)
            {
                List<List<Transform>> line = new List<List<Transform>>();

                var root = clothTarget.GetRoot(i);
                if (root != null)
                {
                    var tq = new Queue<Transform>();
                    var lq = new Queue<int>();

                    tq.Enqueue(root);
                    lq.Enqueue(0);

                    while (tq.Count > 0)
                    {
                        var t = tq.Dequeue();
                        int lv = lq.Dequeue();

                        if (line.Count <= lv)
                        {
                            line.Add(new List<Transform>());
                        }
                        line[lv].Add(t);

                        maxLevel = Mathf.Max(maxLevel, lv + 1);

                        // chile
                        for (int j = 0; j < t.childCount; j++)
                        {
                            var c = t.GetChild(j);
                            tq.Enqueue(c);
                            lq.Enqueue(lv + 1);
                        }
                    }
                }

                grid.Add(line);
            }

            return grid;
        }


        //=========================================================================================
        public override int GetVersion()
        {
            return DATA_VERSION;
        }

        /// <summary>
        /// エラーとするデータバージョンを取得する
        /// </summary>
        /// <returns></returns>
        public override int GetErrorVersion()
        {
            return ERR_DATA_VERSION;
        }

        /// <summary>
        /// データを検証して結果を格納する
        /// </summary>
        /// <returns></returns>
        public override void CreateVerifyData()
        {
            base.CreateVerifyData();
            meshDataHash = MeshData.SaveDataHash;
            meshDataVersion = MeshData.SaveDataVersion;
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

            if (ClothData == null)
                return Define.Error.ClothDataNull;
            if (MeshData == null)
                return Define.Error.MeshDataNull;

            var meshDataError = MeshData.VerifyData();
            if (meshDataError != Define.Error.None)
                return meshDataError;
            if (meshDataHash != MeshData.SaveDataHash)
                return Define.Error.MeshDataHashMismatch;
            if (meshDataVersion != MeshData.SaveDataVersion)
                return Define.Error.MeshDataVersionMismatch;

            if (useTransformList.Count == 0)
                return Define.Error.UseTransformCountZero;
            if (UseTransformCount != MeshData.VertexCount)
                return Define.Error.UseTransformCountMismatch;

            foreach (var t in useTransformList)
                if (t == null)
                    return Define.Error.UseTransformNull;

            return Define.Error.None;
        }

        /// <summary>
        /// データ検証の結果テキストを取得する
        /// </summary>
        /// <returns></returns>
        public override string GetInformation()
        {
            StaticStringBuilder.Clear();

            var err = VerifyData();
            if (err == Define.Error.None)
            {
                // OK
                var cdata = ClothData;
                StaticStringBuilder.AppendLine("Active: ", Status.IsActive);
                StaticStringBuilder.AppendLine($"Visible: {IsVisible}");
                StaticStringBuilder.AppendLine($"Calculation:{IsCalculate}");
                StaticStringBuilder.AppendLine("Transform: ", MeshData.VertexCount);
                StaticStringBuilder.AppendLine("Line: ", MeshData.LineCount);
                StaticStringBuilder.AppendLine("Triangle: ", MeshData.TriangleCount);
                StaticStringBuilder.AppendLine("Clamp Distance: ", cdata.ClampDistanceConstraintCount);
                StaticStringBuilder.AppendLine("Clamp Position: ", clothParams.UseClampPositionLength ? cdata.VertexUseCount : 0);
                StaticStringBuilder.AppendLine("Clamp Rotation [", cdata.clampRotationAlgorithm, "] : ", cdata.GetClampRotationCount());
                StaticStringBuilder.AppendLine("Struct Distance: ", cdata.StructDistanceConstraintCount / 2);
                StaticStringBuilder.AppendLine("Bend Distance: ", cdata.BendDistanceConstraintCount / 2);
                StaticStringBuilder.AppendLine("Near Distance: ", cdata.NearDistanceConstraintCount / 2);
                StaticStringBuilder.AppendLine("Restore Rotation [", cdata.restoreRotationAlgorithm, "] : ", cdata.GetRestoreRotationCount());
                StaticStringBuilder.AppendLine("Triangle Bend [", cdata.triangleBendAlgorithm, "] : ", cdata.TriangleBendConstraintCount);
                //StaticStringBuilder.AppendLine("Rotation Interpolation: ", "<Line:", cdata.LineRotationWorkerCount > 0 ? "ON" : "OFF", "> <Triangle:", cdata.TriangleRotationWorkerCount > 0 ? "ON" : "OFF", ">");
                StaticStringBuilder.Append("Rotation Interpolation: ");
                if (cdata.LineRotationWorkerCount > 0)
                    StaticStringBuilder.Append("Line ");
                if (cdata.TriangleRotationWorkerCount > 0)
                    StaticStringBuilder.Append("Triangle ");
                StaticStringBuilder.AppendLine();
                StaticStringBuilder.Append("Collider: ", teamData.ColliderCount);
            }
            else if (err == Define.Error.EmptyData)
            {
                StaticStringBuilder.Append(Define.GetErrorMessage(err));
            }
            else
            {
                // エラー
                StaticStringBuilder.AppendLine("This bone cloth is in a state error!");
                if (Application.isPlaying)
                {
                    StaticStringBuilder.AppendLine("Execution stopped.");
                }
                else
                {
                    StaticStringBuilder.AppendLine("Please recreate the bone cloth data.");
                }
                StaticStringBuilder.Append(Define.GetErrorMessage(err));
            }

            return StaticStringBuilder.ToString();
        }

        //=========================================================================================
        /// <summary>
        /// ボーンを置換する
        /// </summary>
        /// <param name="boneReplaceDict"></param>
        public override void ReplaceBone<T>(Dictionary<T, Transform> boneReplaceDict)
        {
            base.ReplaceBone(boneReplaceDict);

            for (int i = 0; i < useTransformList.Count; i++)
            {
                useTransformList[i] = MeshUtility.GetReplaceBone(useTransformList[i], boneReplaceDict);
            }

            clothTarget.ReplaceBone(boneReplaceDict);
        }

        /// <summary>
        /// 現在使用しているボーンを格納して返す
        /// </summary>
        /// <returns></returns>
        public override HashSet<Transform> GetUsedBones()
        {
            var bones = base.GetUsedBones();
            bones.UnionWith(useTransformList);
            bones.UnionWith(clothTarget.GetUsedBones());
            return bones;
        }

        //=========================================================================================
        /// <summary>
        /// メッシュのワールド座標/法線/接線を返す（エディタ用）
        /// </summary>
        /// <param name="wposList"></param>
        /// <param name="wnorList"></param>
        /// <param name="wtanList"></param>
        /// <returns>頂点数</returns>
        public override int GetEditorPositionNormalTangent(out List<Vector3> wposList, out List<Vector3> wnorList, out List<Vector3> wtanList)
        {
            wposList = new List<Vector3>();
            wnorList = new List<Vector3>();
            wtanList = new List<Vector3>();

            var tlist = GetTransformList();
            foreach (var t in tlist)
            {
                wposList.Add(t.position);
                wnorList.Add(t.TransformDirection(Vector3.forward));
                var up = t.TransformDirection(Vector3.up);
                wtanList.Add(up);
            }

            return wposList.Count;
        }

        /// <summary>
        /// メッシュのトライアングルリストを返す（エディタ用）
        /// </summary>
        /// <returns></returns>
        public override List<int> GetEditorTriangleList()
        {
            List<int> triangles = new List<int>();
            var mdata = MeshData;
            if (mdata != null && mdata.triangleList != null)
                triangles = new List<int>(mdata.triangleList);
            return triangles;
        }

        /// <summary>
        /// メッシュのラインリストを返す（エディタ用）
        /// </summary>
        /// <returns></returns>
        public override List<int> GetEditorLineList()
        {
            List<int> lines = new List<int>();
            var mdata = MeshData;
            if (mdata != null && mdata.lineList != null)
                lines = new List<int>(mdata.lineList);
            return lines;
        }

        //=========================================================================================
        /// <summary>
        /// 頂点の選択状態をリストにして返す（エディタ用）
        /// 選択状態は ClothSelection.Invalid / ClothSelection.Fixed / ClothSelection.Move
        /// すべてがInvalidならばnullを返す
        /// </summary>
        /// <returns></returns>
        public override List<int> GetSelectionList()
        {
            if (ClothSelection != null && MeshData != null)
                return ClothSelection.GetSelectionData(MeshData, null);
            else
                return null;
        }

        /// <summary>
        /// 頂点の使用状態をリストにして返す（エディタ用）
        /// 数値が１以上ならば使用中とみなす
        /// すべて使用状態ならばnullを返す
        /// </summary>
        /// <returns></returns>
        public override List<int> GetUseList()
        {
            // 不要かも
            //if (Application.isPlaying)
            //{
            //    if (ClothData != null)
            //    {
            //        var useList = new List<int>();
            //        foreach (var sel in ClothData.selectionData)
            //            useList.Add(sel != SelectionData.Invalid ? 1 : 0);
            //        return useList;
            //    }
            //}

            return null;
        }

        //=========================================================================================
        /// <summary>
        /// 共有データオブジェクト収集
        /// </summary>
        /// <returns></returns>
        public override List<ShareDataObject> GetAllShareDataObject()
        {
            var sdata = base.GetAllShareDataObject();
            sdata.Add(MeshData);
            return sdata;
        }

        /// <summary>
        /// sourceの共有データを複製して再セットする
        /// 再セットした共有データを返す
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public override ShareDataObject DuplicateShareDataObject(ShareDataObject source)
        {
            var sdata = base.DuplicateShareDataObject(source);
            if (sdata != null)
                return sdata;

            if (MeshData == source)
            {
                //meshData = Instantiate(MeshData);
                meshData = ShareDataObject.Clone(MeshData);
                return meshData;
            }

            return null;
        }

        //=========================================================================================
        /// <summary>
        /// パラメータ初期化
        /// </summary>
        void ResetParams()
        {
            clothParams.AlgorithmType = ClothParams.Algorithm.Algorithm_2;
            clothParams.SetRadius(0.05f, 0.05f);
            clothParams.SetMass(10.0f, 1.0f, true, -0.5f, true);
            clothParams.SetGravity(true, -5.0f, -5.0f);
            clothParams.SetDrag(true, 0.01f, 0.01f);
            clothParams.SetMaxVelocity(true, 3.0f, 3.0f);
            clothParams.SetWorldInfluence(3.0f, 0.5f, 1.0f);
            clothParams.SetTeleport(false);
            clothParams.SetClampDistanceRatio(true, 0.7f, 1.05f, 0.1f);
            clothParams.SetClampPositionLength(false, 0.0f, 0.4f);
            clothParams.SetClampRotationAngle(false, 0.0f, 180.0f, 0.2f);
            clothParams.SetRestoreDistance(1.0f);
            clothParams.SetRestoreRotation(false, 0.03f, 0.005f, 0.3f);
            clothParams.SetSpring(false);
            clothParams.SetAdjustRotation();
            clothParams.SetTriangleBend(true, 0.7f, 0.7f);
            clothParams.SetVolume(false);
            clothParams.SetCollision(false, 0.1f, 0.03f);
            clothParams.SetExternalForce(0.3f, 1.0f, 0.7f, 0.6f);
        }
    }
}
