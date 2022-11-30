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
    /// メッシュスプリング
    /// </summary>
    [HelpURL("https://magicasoft.jp/magica-cloth-mesh-spring/")]
    [AddComponentMenu("MagicaCloth/MagicaMeshSpring", 100)]
    public class MagicaMeshSpring : BaseCloth
    {
        /// <summary>
        /// データバージョン
        /// </summary>
        private const int DATA_VERSION = 7;

        /// <summary>
        /// エラーデータバージョン
        /// </summary>
        private const int ERR_DATA_VERSION = 3;

        // 対象仮想メッシュデフォーマー
        [SerializeField]
        private MagicaVirtualDeformer virtualDeformer = null;

        [SerializeField]
        private int virtualDeformerHash;
        [SerializeField]
        private int virtualDeformerVersion;

        // センタートランスフォーム
        [SerializeField]
        private Transform centerTransform = null;

        public enum Axis
        {
            X,
            Y,
            Z,
            InverseX,
            InverseY,
            InverseZ,
        }
        [SerializeField]
        private Axis directionAxis;

        [SerializeField]
        private SpringData springData = null;

        [SerializeField]
        private int springDataHash;
        [SerializeField]
        private int springDataVersion;

        //=========================================================================================
        public override ComponentType GetComponentType()
        {
            return ComponentType.MeshSpring;
        }

        //=========================================================================================
        public override int GetDataHash()
        {
            int hash = base.GetDataHash();
            hash += virtualDeformer.GetDataHash();
            hash += centerTransform.GetDataHash();
            hash += SpringData.GetDataHash();
            return hash;
        }

        //=========================================================================================
        public VirtualMeshDeformer Deformer
        {
            get
            {
                if (virtualDeformer != null)
                    return virtualDeformer.Deformer;
                return null;
            }
        }

        public SpringData SpringData
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    return springData;
                else
                {
                    // unity2019.3で参照がnullとなる不具合の対処（臨時）
                    var so = new SerializedObject(this);
                    return so.FindProperty("springData").objectReferenceValue as SpringData;
                }
#else
                return springData;
#endif
            }
        }

        public int UseVertexCount
        {
            get
            {
                if (SpringData == null)
                    return 0;
                else
                    return SpringData.UseVertexCount;
            }
        }

        public Transform CenterTransform
        {
            get
            {
                return centerTransform;
            }
            set
            {
                centerTransform = value;
            }
        }

        public Axis DirectionAxis
        {
            get
            {
                return directionAxis;
            }
            set
            {
                directionAxis = value;
            }
        }

        public Vector3 CenterTransformDirection
        {
            get
            {
                Vector3 dir = Vector3.forward;
                if (centerTransform)
                {
                    switch (directionAxis)
                    {
                        case Axis.X:
                            dir = centerTransform.right;
                            break;
                        case Axis.Y:
                            dir = centerTransform.up;
                            break;
                        case Axis.Z:
                            dir = centerTransform.forward;
                            break;
                        case Axis.InverseX:
                            dir = -centerTransform.right;
                            break;
                        case Axis.InverseY:
                            dir = -centerTransform.up;
                            break;
                        case Axis.InverseZ:
                            dir = -centerTransform.forward;
                            break;
                    }
                }

                return dir;
            }
        }

        public SpringData.DeformerData GetDeformerData()
        {
            return SpringData.deformerData;
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

        //=========================================================================================
        /// <summary>
        /// クロス初期化
        /// </summary>
        protected override void ClothInit()
        {
            // 中央トランスフォームに移動パーティクルを１つ設定する（これが揺れる）
            // クロスデータはこの場で作成する
            ClothData cdata = ShareDataObject.CreateShareData<ClothData>("ClothData_work");
            cdata.selectionData.Add(SelectionData.Move);
            cdata.vertexFlagLevelList.Add(0);
            cdata.vertexDepthList.Add(0);
            cdata.rootList.Add(0);
            cdata.useVertexList.Add(0);
            cdata.initScale = SpringData.initScale;
            cdata.SaveDataHash = 1;
            cdata.SaveDataVersion = cdata.GetVersion();
            cdata.clampRotationAlgorithm = ClothParams.Algorithm.Algorithm_2;
            cdata.restoreRotationAlgorithm = ClothParams.Algorithm.Algorithm_2;
            cdata.triangleBendAlgorithm = ClothParams.Algorithm.Algorithm_2;
            ClothData = cdata;

            // エラーが出ないように
            clothDataHash = cdata.SaveDataHash;
            clothDataVersion = cdata.SaveDataVersion;

            // クロス初期化
            base.ClothInit();

            // スプリングではClampPositonの速度制限は無視する
            MagicaPhysicsManager.Instance.Team.SetFlag(TeamId, PhysicsManagerTeamData.Flag_IgnoreClampPositionVelocity, true);
        }

        protected override void ClothActive()
        {
            base.ClothActive();
        }

        /// <summary>
        /// 頂点ごとのパーティクルフラグ設定（不要な場合は０）
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected override uint UserFlag(int index)
        {
            uint flag = 0;
            flag |= PhysicsManagerParticleData.Flag_Transform_Read_Base; // トランスフォームをbasePos/baseRotに読み込み
            flag |= PhysicsManagerParticleData.Flag_Transform_Read_Rot; // トランスフォームをrotに読み込む
            return flag;
        }

        /// <summary>
        /// 頂点ごとの連動トランスフォーム設定（不要な場合はnull）
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected override Transform UserTransform(int index)
        {
            return CenterTransform;
        }

        /// <summary>
        /// 頂点ごとの連動トランスフォームのLocalPositionを返す（不要な場合は0）
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected override float3 UserTransformLocalPosition(int vindex)
        {
            return CenterTransform.localPosition;
        }

        /// <summary>
        /// 頂点ごとの連動トランスフォームのLocalRotationを返す（不要な場合はquaternion.identity)
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected override quaternion UserTransformLocalRotation(int vindex)
        {
            return CenterTransform.localRotation;
        }

        /// <summary>
        /// デフォーマーが必須か返す
        /// </summary>
        /// <returns></returns>
        public override bool IsRequiresDeformer()
        {
            return true;
        }

        /// <summary>
        /// デフォーマーを返す
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override BaseMeshDeformer GetDeformer()
        {
            if (virtualDeformer)
            {
                return virtualDeformer.Deformer;
            }
            return null;
        }

        /// <summary>
        /// クロス初期化時に必要なMeshDataを返す（不要ならnull）
        /// </summary>
        /// <returns></returns>
        protected override MeshData GetMeshData()
        {
            // MeshSpringeには不要
            return null;
        }

        /// <summary>
        /// クロス初期化の主にワーカーへの登録
        /// </summary>
        protected override void WorkerInit()
        {
            // センターパーティクル
            int pindex = ParticleChunk.startIndex;

            // デフォーマーごとの設定
            SpringMeshWorker worker = MagicaPhysicsManager.Instance.Compute.SpringMeshWorker;
            {
                // デフォーマー取得
                var deformer = GetDeformer();
                Debug.Assert(deformer != null);
                deformer.Init();

                // スプリングデータ取得
                var data = GetDeformerData();
                Debug.Assert(data != null);

                // スプリングワーカー設定
                var minfo = MagicaPhysicsManager.Instance.Mesh.GetVirtualMeshInfo(deformer.MeshIndex);
                for (int j = 0; j < data.UseVertexCount; j++)
                {
                    int vindex = data.useVertexIndexList[j];
                    worker.Add(TeamId, minfo.vertexChunk.startIndex + vindex, pindex, data.weightList[j]);
                }
            }
        }

        /// <summary>
        /// デフォーマーごとの使用頂点設定
        /// 使用頂点に対して AddUseVertex() / RemoveUseVertex() を実行する
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="deformer"></param>
        protected override void SetDeformerUseVertex(bool sw, BaseMeshDeformer deformer)
        {
            var data = GetDeformerData();

            int vcnt = data.UseVertexCount;
            for (int j = 0; j < vcnt; j++)
            {
                int vindex = data.useVertexIndexList[j];
                if (sw)
                    deformer.AddUseVertex(vindex, false);
                else
                    deformer.RemoveUseVertex(vindex, false);
            }
        }

        /// <summary>
        /// UnityPhyiscsでの更新の変更
        /// 継承クラスは自身の使用するボーンの状態更新などを記述する
        /// </summary>
        /// <param name="sw"></param>
        protected override void ChangeUseUnityPhysics(bool sw)
        {
            base.ChangeUseUnityPhysics(sw);

            // デフォーマに伝達
            virtualDeformer?.SetUseUnityPhysics(sw);
        }

        protected override void OnChangeCalculation()
        {
            base.OnChangeCalculation();

            if (IsCalculate)
            {
                if (MagicaPhysicsManager.Instance.IsDelay)
                {
                    // 読み込みボーンの未来予測をリセットする
                    MagicaPhysicsManager.Instance.Particle.ResetFuturePredictionTransform(particleChunk);
                }
            }
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
            virtualDeformerHash = virtualDeformer.SaveDataHash;
            virtualDeformerVersion = virtualDeformer.SaveDataVersion;
            springDataHash = SpringData.SaveDataHash;
            springDataVersion = SpringData.SaveDataVersion;
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

            if (virtualDeformer == null)
                return Define.Error.DeformerNull;
            var vdeformerError = virtualDeformer.VerifyData();
            if (vdeformerError != Define.Error.None)
                return vdeformerError;
            if (virtualDeformerHash != virtualDeformer.SaveDataHash)
                return Define.Error.DeformerHashMismatch;
            if (virtualDeformerVersion != virtualDeformer.SaveDataVersion)
                return Define.Error.DeformerVersionMismatch;

            if (centerTransform == null)
                return Define.Error.CenterTransformNull;
            var sdata = SpringData;
            if (sdata == null)
                return Define.Error.SpringDataNull;
            var sdataError = sdata.VerifyData();
            if (sdataError != Define.Error.None)
                return sdataError;
            if (springDataHash != sdata.SaveDataHash)
                return Define.Error.SpringDataHashMismatch;
            if (springDataVersion != sdata.SaveDataVersion)
                return Define.Error.SpringDataVersionMismatch;

            return Define.Error.None;
        }

        public override string GetInformation()
        {
            StaticStringBuilder.Clear();

            var err = VerifyData();
            if (err == Define.Error.None)
            {
                // OK
                StaticStringBuilder.AppendLine("Active: ", Status.IsActive);
                StaticStringBuilder.AppendLine($"Visible: {IsVisible}");
                StaticStringBuilder.AppendLine($"Calculation:{IsCalculate}");
                StaticStringBuilder.Append("Use Deformer Vertex: ", UseVertexCount);
            }
            else if (err == Define.Error.EmptyData)
            {
                StaticStringBuilder.Append(Define.GetErrorMessage(err));
            }
            else
            {
                // エラー
                StaticStringBuilder.AppendLine("This mesh spring is in a state error!");
                if (Application.isPlaying)
                {
                    StaticStringBuilder.AppendLine("Execution stopped.");
                }
                else
                {
                    StaticStringBuilder.AppendLine("Please recreate the mesh spring data.");
                }
                StaticStringBuilder.Append(Define.GetErrorMessage(err));
            }

            return StaticStringBuilder.ToString();
        }

        /// <summary>
        /// デフォーマーの検証
        /// </summary>
        public void VerifyDeformer()
        {
        }

        //=========================================================================================
        /// <summary>
        /// ボーンを置換する
        /// </summary>
        /// <param name="boneReplaceDict"></param>
        public override void ReplaceBone<T>(Dictionary<T, Transform> boneReplaceDict)
        {
            if (centerTransform)
            {
                centerTransform = MeshUtility.GetReplaceBone(centerTransform, boneReplaceDict);
            }
        }

        /// <summary>
        /// 現在使用しているボーンを格納して返す
        /// </summary>
        /// <returns></returns>
        public override HashSet<Transform> GetUsedBones()
        {
            var bones = base.GetUsedBones();
            bones.Add(centerTransform);
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

            var t = CenterTransform;
            if (t == null)
                return 0;

            wposList.Add(t.position);
            wnorList.Add(t.forward);
            var up = t.up;
            wtanList.Add(up);

            return 1;
        }

        /// <summary>
        /// メッシュのトライアングルリストを返す（エディタ用）
        /// </summary>
        /// <returns></returns>
        public override List<int> GetEditorTriangleList()
        {
            return null;
        }

        /// <summary>
        /// メッシュのラインリストを返す（エディタ用）
        /// </summary>
        /// <returns></returns>
        public override List<int> GetEditorLineList()
        {
            return null;
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
            sdata.Add(SpringData);
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

            if (SpringData == source)
            {
                //springData = Instantiate(SpringData);
                springData = ShareDataObject.Clone(SpringData);
                return springData;
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
            clothParams.SetMass(1.0f, 1.0f, false);
            clothParams.SetGravity(false, -5.0f, -5.0f);
            clothParams.SetDrag(true, 0.01f, 0.01f);
            clothParams.SetMaxVelocity(true, 3.0f, 3.0f);
            clothParams.SetWorldInfluence(2.0f, 0.5f, 1.0f);
            clothParams.SetTeleport(false);
            clothParams.SetClampDistanceRatio(false);
            clothParams.SetClampPositionLength(true, 0.1f, 0.1f, 1.0f, 1.0f, 1.0f, 0.2f);
            clothParams.SetClampRotationAngle(false);
            clothParams.SetRestoreDistance(1.0f);
            clothParams.SetRestoreRotation(false);
            clothParams.SetSpring(true, 0.02f, 0.14f, 1.0f, 1.0f, 1.0f, 1.0f);
            clothParams.SetSpringDirectionAtten(1.0f, 0.0f, 0.6f);
            clothParams.SetSpringDistanceAtten(1.0f, 0.0f, 0.4f);
            clothParams.SetAdjustRotation(ClothParams.AdjustMode.Fixed, 5.0f);
            clothParams.SetTriangleBend(false);
            clothParams.SetVolume(false);
            clothParams.SetCollision(false, 0.1f);
            clothParams.SetExternalForce(0.2f, 0.0f, 0.0f, 1.0f);
        }
    }
}
