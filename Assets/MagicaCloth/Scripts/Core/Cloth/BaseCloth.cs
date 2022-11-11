// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicaCloth
{
    /// <summary>
    /// クロス基本クラス
    /// </summary>
    public abstract partial class BaseCloth : PhysicsTeam
    {
        /// <summary>
        /// パラメータ設定
        /// </summary>
        [SerializeField]
        protected ClothParams clothParams = new ClothParams();

        [SerializeField]
        protected List<int> clothParamDataHashList = new List<int>();

        /// <summary>
        /// クロスデータ
        /// </summary>
        [SerializeField]
        private ClothData clothData = null;

        [SerializeField]
        protected int clothDataHash;
        [SerializeField]
        protected int clothDataVersion;

        /// <summary>
        /// 頂点選択データ
        /// </summary>
        [SerializeField]
        private SelectionData clothSelection = null;

        [SerializeField]
        private int clothSelectionHash;
        [SerializeField]
        private int clothSelectionVersion;

        /// <summary>
        /// カリング用レンダラーリスト
        /// BoneCloth / BoneSpring で使用
        /// </summary>
        [SerializeField]
        private List<Renderer> cullRendererList = new List<Renderer>();

        /// <summary>
        /// ランタイムクロス設定
        /// </summary>
        protected ClothSetup setup = new ClothSetup();


        //=========================================================================================
        private float oldBlendRatio = -1.0f;
        private TeamUpdateMode oldUpdateMode = 0;
        private TeamCullingMode oldCullingMode = 0;
        private bool oldUseAnimatedDistance = false;

        //=========================================================================================
        /// <summary>
        /// データハッシュを求める
        /// </summary>
        /// <returns></returns>
        public override int GetDataHash()
        {
            int hash = base.GetDataHash();
            if (ClothData != null)
                hash += ClothData.GetDataHash();
            if (ClothSelection != null)
                hash += ClothSelection.GetDataHash();

            return hash;
        }

        //=========================================================================================
        public ClothParams Params
        {
            get
            {
                return clothParams;
            }
        }

        public ClothData ClothData
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    return clothData;
                else
                {
                    // unity2019.3で参照がnullとなる不具合の対処（臨時）
                    var so = new SerializedObject(this);
                    return so.FindProperty("clothData").objectReferenceValue as ClothData;
                }
#else
                return clothData;
#endif
            }
            set
            {
                clothData = value;
            }
        }

        public SelectionData ClothSelection
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    return clothSelection;
                else
                {
                    // unity2019.3で参照がnullとなる不具合の対処（臨時）
                    var so = new SerializedObject(this);
                    return so.FindProperty("clothSelection").objectReferenceValue as SelectionData;
                }
#else
                return clothSelection;
#endif
            }
        }

        public ClothSetup Setup
        {
            get
            {
                return setup;
            }
        }

        //=========================================================================================
        protected virtual void Reset()
        {
        }

        protected virtual void OnValidate()
        {
            if (Application.isPlaying == false)
                return;

            // クロスパラメータのラインタイム変更
            setup.ChangeData(this, clothParams, clothData);
        }

        //=========================================================================================
        protected override void OnInit()
        {
            base.OnInit();
            BaseClothInit();
        }

        protected override void OnActive()
        {
            base.OnActive();
            // パーティクル有効化
            EnableParticle(UserTransform, UserTransformLocalPosition, UserTransformLocalRotation);
            // コライダー状態更新
            TeamData.UpdateStatus();
            SetUseMesh(true);
            ClothActive();
        }

        protected override void OnInactive()
        {
            base.OnInactive();
            // パーティクル無効化
            DisableParticle(UserTransform, UserTransformLocalPosition, UserTransformLocalRotation);
            SetUseMesh(false);
            ClothInactive();
        }

        protected override void OnDispose()
        {
            BaseClothDispose();
            base.OnDispose();
        }

        //=========================================================================================
        internal override void UpdateCullingMode(CoreComponent caller)
        {
            //Debug.Log($"UpdateCullingMode [{this.name}]");

            // カリングモード
            bool isBoneCloth = GetComponentType() == ComponentType.BoneCloth || GetComponentType() == ComponentType.BoneSpring;
            if (CullingMode != TeamCullingMode.Off && isBoneCloth && cullRendererList.Count == 0)
            {
                // BoneCloth/BoneSpringだが参照レンダラーが設定されていないので強制的にカリングをOFFに設定する
                CullingMode = TeamCullingMode.Off;
            }

            // deformer
            CoreComponent vd = GetDeformer()?.Parent;

            // 表示状態
            bool visible = false;
            if (CullingMode == TeamCullingMode.Off)
            {
                visible = true;
            }
            else if (IsActive()) // 起動中のみ
            {
                if (isBoneCloth)
                {
                    // カリング用レンダラーリストから判定する
                    if (cullRendererList.Count > 0)
                    {
                        foreach (var ren in cullRendererList)
                        {
                            if (ren && ren.isVisible)
                            {
                                visible = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // 未設定は念の為動作させておく
                        visible = true;
                    }
                }
                else
                {
                    // デフォーマーの表示状態から判定する
                    visible = vd ? vd.IsVisible : false;
                }
            }
            IsVisible = visible;

            // 計算状態
            bool stopInvisible = (CullingMode != TeamCullingMode.Off);
            bool calc = true;
            if (stopInvisible)
            {
                calc = visible;
            }
            int val = calc ? 1 : 0;

            // コンポーネントアクティブ状態
            val = Status.IsActive ? val : 0;

            // 最終判定
            if (calculateValue != val)
            {
                calculateValue = val;
                OnChangeCalculation();
            }

            // デフォーマーへ伝達
            if (vd && vd != caller)
                GetDeformer()?.Parent?.UpdateCullingMode(this);
        }

        protected override void OnChangeCalculation()
        {
            //Debug.Log($"Cloth [{this.name}] Visible:{IsVisible} Calc:{IsCalculate} F:{Time.frameCount}");
            MagicaPhysicsManager.Instance.Team.SetFlag(teamId, PhysicsManagerTeamData.Flag_Pause, !IsCalculate);

            if (IsCalculate)
            {
                // 一時停止再開によるリセット
                if (CullingMode == TeamCullingMode.Reset)
                {
                    //Debug.Log($"Reset cloth! [{this.name}] F:{Time.frameCount}");
                    ResetCloth(ClothParams.TeleportMode.Reset);
                }

                // デフォーマの未来予測をリセットする
                // 遅延実行＋再アクティブ時のみ
                //if (MagicaPhysicsManager.Instance.IsDelay && ActiveCount > 1)
                if (MagicaPhysicsManager.Instance.IsDelay)
                {
                    GetDeformer()?.ResetFuturePrediction();
                }

                // コライダーボーンの未来予測をリセットする
                // 遅延実行＋再アクティブ時のみ
                //if (MagicaPhysicsManager.Instance.IsDelay && ActiveCount > 1)
                if (MagicaPhysicsManager.Instance.IsDelay)
                {
                    MagicaPhysicsManager.Instance.Team.ResetFuturePredictionCollidere(TeamId);
                }
            }
        }

        public int GetCullRenderListCount()
        {
            if (cullRendererList == null)
                return 0;
            return cullRendererList.Count(x => x != null);
        }

        //=========================================================================================
        void BaseClothInit()
        {
            // デフォーマー初期化
            if (IsRequiresDeformer())
            {
                var deformer = GetDeformer();
                if (deformer == null)
                {
                    Status.SetInitError();
                    return;
                }

                // デフォーマーと状態を連動
                var component = deformer.Parent;
                Status.LinkParentStatus(component.Status); // デフォーマーが親、クロスコンポーネントが子

                component.Init();
                if (component.Status.IsInitError)
                {
                    Status.SetInitError();
                    return;
                }
            }

            if (VerifyData() != Define.Error.None)
            {
                Status.SetInitError();
                return;
            }

            // クロス初期化
            ClothInit();

            // クロス初期化後の主にワーカーへの登録
            WorkerInit();

            // 頂点有効化
            SetUseVertex(true);

            // 更新モード記録
            oldUpdateMode = UpdateMode;
            oldCullingMode = CullingMode;
            oldUseAnimatedDistance = UseAnimatedPose;

            // UnityPhysics更新モードによる各種設定
            if (UpdateMode == TeamUpdateMode.UnityPhysics)
                SetUseUnityPhysics(true);
        }

        void BaseClothDispose()
        {
            if (MagicaPhysicsManager.IsInstance() == false)
                return;

            // デフォーマとの状態の連動を解除
            var deformer = GetDeformer();
            if (deformer != null)
            {
                var component = deformer.Parent;
                Status.UnlinkParentStatus(component.Status);
            }

            if (Status.IsInitSuccess)
            {
                // 頂点無効化
                SetUseVertex(false);

                // クロス破棄
                // この中ですべてのコンストレイントとワーカーからチームのデータが自動削除される
                setup.ClothDispose(this);

                ClothDispose();
            }
        }

        /// <summary>
        /// クロス初期化
        /// </summary>
        protected virtual void ClothInit()
        {
            setup.ClothInit(this, GetMeshData(), ClothData, clothParams, UserFlag);
        }

        protected virtual void ClothActive()
        {
            setup.ClothActive(this, clothParams, ClothData);

            // アニメーションされた距離の使用設定
            MagicaPhysicsManager.Instance.Team.SetFlag(TeamId, PhysicsManagerTeamData.Flag_AnimatedPose, UseAnimatedPose);
        }

        protected virtual void ClothInactive()
        {
            setup.ClothInactive(this);
        }

        protected virtual void ClothDispose()
        {
        }

        /// <summary>
        /// 頂点ごとのパーティクルフラグ設定（不要な場合は０）
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected abstract uint UserFlag(int vindex);

        /// <summary>
        /// 頂点ごとの連動トランスフォーム設定（不要な場合はnull）
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected abstract Transform UserTransform(int vindex);

        /// <summary>
        /// 頂点ごとの連動トランスフォームのLocalPositionを返す（不要な場合は0）
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected abstract float3 UserTransformLocalPosition(int vindex);

        /// <summary>
        /// 頂点ごとの連動トランスフォームのLocalRotationを返す（不要な場合はquaternion.identity)
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns></returns>
        protected abstract quaternion UserTransformLocalRotation(int vindex);

        /// <summary>
        /// デフォーマーが必須か返す
        /// </summary>
        /// <returns></returns>
        public abstract bool IsRequiresDeformer();

        /// <summary>
        /// デフォーマーを返す
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public abstract BaseMeshDeformer GetDeformer();

        /// <summary>
        /// クロス初期化時に必要なMeshDataを返す（不要ならnull）
        /// </summary>
        /// <returns></returns>
        protected abstract MeshData GetMeshData();

        /// <summary>
        /// クロス初期化後の主にワーカーへの登録
        /// </summary>
        protected abstract void WorkerInit();


        //=========================================================================================
        /// <summary>
        /// 使用デフォーマー設定
        /// </summary>
        /// <param name="sw"></param>
        void SetUseMesh(bool sw)
        {
            if (MagicaPhysicsManager.IsInstance() == false)
                return;

            if (Status.IsInitSuccess == false)
                return;

            var deformer = GetDeformer();
            if (deformer != null)
            {
                if (sw)
                    deformer.AddUseMesh(this);
                else
                    deformer.RemoveUseMesh(this);
            }
        }

        /// <summary>
        /// 使用頂点設定
        /// </summary>
        /// <param name="sw"></param>
        void SetUseVertex(bool sw)
        {
            if (MagicaPhysicsManager.IsInstance() == false)
                return;

            var deformer = GetDeformer();
            if (deformer != null)
            {
                SetDeformerUseVertex(sw, deformer);
            }
        }

        /// <summary>
        /// デフォーマーの使用頂点設定
        /// 使用頂点に対して AddUseVertex() / RemoveUseVertex() を実行する
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="deformer"></param>
        protected abstract void SetDeformerUseVertex(bool sw, BaseMeshDeformer deformer);

        /// <summary>
        /// デフォーマーに対してアクションを実行する
        /// </summary>
        /// <param name="act"></param>
        internal void DeformerForEach(System.Action<BaseMeshDeformer> act)
        {
            var deformer = GetDeformer();
            if (deformer != null)
            {
                act(deformer);
            }
        }

        //=========================================================================================
        /// <summary>
        /// ブレンド率更新
        /// </summary>
        public void UpdateBlend()
        {
            if (teamId <= 0)
                return;

            // ユーザーブレンド率
            float blend = UserBlendWeight;

            // 距離ブレンド率
            blend *= setup.DistanceBlendRatio;

            // 変更チェック
            blend = Mathf.Clamp01(blend);
            if (blend != oldBlendRatio)
            {
                // チームデータへ反映
                MagicaPhysicsManager.Instance.Team.SetBlendRatio(teamId, blend);

                // コンポーネント有効化判定
                SetUserEnable(blend >= 1e-03f);

                oldBlendRatio = blend;
            }

            // カリングモード変更
            if (CullingMode != oldCullingMode)
            {
                // 反映
                UpdateCullingMode(this);
                oldCullingMode = CullingMode;
            }

            // 更新モード変更
            if (UpdateMode != oldUpdateMode)
            {
                // チームデータへ反映
                //Debug.Log($"Change Update Mode:{UpdateMode}");
                MagicaPhysicsManager.Instance.Team.SetUpdateMode(TeamId, UpdateMode);

                // チームのパーティクルおよびボーンに反映
                SetUseUnityPhysics(UpdateMode == TeamUpdateMode.UnityPhysics);

                oldUpdateMode = UpdateMode;
            }

            // アニメーションされた距離の使用
            if (UseAnimatedPose != oldUseAnimatedDistance)
            {
                // チームデータへ反映
                MagicaPhysicsManager.Instance.Team.SetFlag(TeamId, PhysicsManagerTeamData.Flag_AnimatedPose, UseAnimatedPose);

                oldUseAnimatedDistance = UseAnimatedPose;
            }
        }

        //=========================================================================================
        /// <summary>
        /// ボーンを置換する
        /// </summary>
        /// <param name="boneReplaceDict"></param>
        public override void ReplaceBone<T>(Dictionary<T, Transform> boneReplaceDict)
        {
            base.ReplaceBone(boneReplaceDict);

            // セットアップデータのボーン置換
            setup.ReplaceBone(this, clothParams, boneReplaceDict);
        }

        /// <summary>
        /// 現在使用しているボーンを格納して返す
        /// </summary>
        /// <returns></returns>
        public override HashSet<Transform> GetUsedBones()
        {
            var bones = base.GetUsedBones();

            // セットアップデータのボーン取得
            bones.UnionWith(setup.GetUsedBones(this, clothParams));

            return bones;
        }

        //=========================================================================================
        /// <summary>
        /// UnityPhyiscsでの更新の変更
        /// 継承クラスは自身の使用するボーンの状態更新などを記述する
        /// </summary>
        /// <param name="sw"></param>
        protected override void ChangeUseUnityPhysics(bool sw)
        {
            if (teamId <= 0)
                return;

            setup.ChangeUseUnityPhysics(sw);
            MagicaPhysicsManager.Instance.Team.ChangeUseUnityPhysics(TeamId, sw);
        }

        //=========================================================================================
        /// <summary>
        /// データを検証して結果を格納する
        /// </summary>
        /// <returns></returns>
        public override void CreateVerifyData()
        {
            base.CreateVerifyData();
            clothDataHash = ClothData != null ? ClothData.SaveDataHash : 0;
            clothDataVersion = ClothData != null ? ClothData.SaveDataVersion : 0;
            clothSelectionHash = ClothSelection != null ? ClothSelection.SaveDataHash : 0;
            clothSelectionVersion = ClothSelection != null ? ClothSelection.SaveDataVersion : 0;

            // パラメータハッシュ
            clothParamDataHashList.Clear();
            for (int i = 0; i < (int)ClothParams.ParamType.Max; i++)
            {
                int paramHash = clothParams.GetParamHash(this, (ClothParams.ParamType)i);
                clothParamDataHashList.Add(paramHash);
            }
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

            // clothDataはオプション
            if (ClothData != null)
            {
                var clothDataError = ClothData.VerifyData();
                if (clothDataError != Define.Error.None)
                    return clothDataError;
                if (clothDataHash != ClothData.SaveDataHash)
                    return Define.Error.ClothDataHashMismatch;
                if (clothDataVersion != ClothData.SaveDataVersion)
                    return Define.Error.ClothDataVersionMismatch;
            }

            // clothSelectionはオプション
            if (ClothSelection != null)
            {
                var clothSelectionError = ClothSelection.VerifyData();
                if (clothSelectionError != Define.Error.None)
                    return clothSelectionError;
                if (clothSelectionHash != ClothSelection.SaveDataHash)
                    return Define.Error.ClothSelectionHashMismatch;
                if (clothSelectionVersion != ClothSelection.SaveDataVersion)
                    return Define.Error.ClothSelectionVersionMismatch;
            }

            return Define.Error.None;
        }

        /// <summary>
        /// パラメータに重要な変更が発生したか調べる
        /// 重要な変更はデータを作り直す必要を指している
        /// </summary>
        /// <param name="ptype"></param>
        /// <returns></returns>
        public bool HasChangedParam(ClothParams.ParamType ptype)
        {
            int index = (int)ptype;
            if (clothParamDataHashList.Count == 0)
                return false;
            if (index >= clothParamDataHashList.Count)
            {
                return true;
            }
            int hash = clothParams.GetParamHash(this, ptype);
            if (hash == 0)
                return false;

            return clothParamDataHashList[index] != hash;
        }

        /// <summary>
        /// アルゴリズムバージョンチェック
        /// </summary>
        /// <returns></returns>
        public Define.Error VerifyAlgorithmVersion()
        {
            if (clothData == null)
                return Define.Error.None;

            if (clothData.clampRotationAlgorithm != ClothParams.Algorithm.Algorithm_2)
                return Define.Error.OldAlgorithm;
            if (clothData.restoreRotationAlgorithm != ClothParams.Algorithm.Algorithm_2)
                return Define.Error.OldAlgorithm;
            if (clothData.triangleBendAlgorithm != ClothParams.Algorithm.Algorithm_2)
                return Define.Error.OldAlgorithm;

            return Define.Error.None;
        }

        /// <summary>
        /// データフォーマットを最新に更新する
        /// 主に古いパラメータを最新のパラメータに変換する
        /// </summary>
        /// <returns>true=更新あり, false=更新なし</returns>
        public override bool UpgradeFormat()
        {
            bool change = false;

            // アルゴリズム
            if (clothParams.AlgorithmType == ClothParams.Algorithm.Algorithm_1)
            {
                // アルゴリズム[2]へアップグレード
                clothParams.AlgorithmType = ClothParams.Algorithm.Algorithm_2;
                clothParams.ConvertToLatestAlgorithmParameter();
                change = true;
            }

            return change;
        }

        //=========================================================================================
        /// <summary>
        /// 共有データオブジェクト収集
        /// </summary>
        /// <returns></returns>
        public override List<ShareDataObject> GetAllShareDataObject()
        {
            var sdata = base.GetAllShareDataObject();
            sdata.Add(ClothData);
            sdata.Add(ClothSelection);
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
            if (ClothData == source)
            {
                //clothData = Instantiate(ClothData);
                clothData = ShareDataObject.Clone(ClothData);
                return clothData;
            }

            if (ClothSelection == source)
            {
                //clothSelection = Instantiate(ClothSelection);
                clothSelection = ShareDataObject.Clone(ClothSelection);
                return clothSelection;
            }

            return null;
        }

        //=========================================================================================
        /// <summary>
        /// シミュレーションリセットAPIの内部実装
        /// </summary>
        /// <param name="teleportMode"></param>
        /// <param name="resetStabilizationTime"></param>
        private void ResetClothInternal(ClothParams.TeleportMode teleportMode, float resetStabilizationTime)
        {
            if (IsValid())
            {
                switch (teleportMode)
                {
                    case ClothParams.TeleportMode.Reset:
                        MagicaPhysicsManager.Instance.Team.SetFlag(teamId, PhysicsManagerTeamData.Flag_Reset_WorldInfluence, true);
                        MagicaPhysicsManager.Instance.Team.SetFlag(teamId, PhysicsManagerTeamData.Flag_Reset_Position, true);
                        MagicaPhysicsManager.Instance.Team.SetFlag(teamId, PhysicsManagerTeamData.Flag_Reset_Keep, false);
                        MagicaPhysicsManager.Instance.Team.ResetStabilizationTime(teamId, resetStabilizationTime);
                        break;
                    case ClothParams.TeleportMode.Keep:
                        MagicaPhysicsManager.Instance.Team.SetFlag(teamId, PhysicsManagerTeamData.Flag_Reset_WorldInfluence, false);
                        MagicaPhysicsManager.Instance.Team.SetFlag(teamId, PhysicsManagerTeamData.Flag_Reset_Position, false);
                        MagicaPhysicsManager.Instance.Team.SetFlag(teamId, PhysicsManagerTeamData.Flag_Reset_Keep, true);
                        break;
                }
            }
        }
    }
}
