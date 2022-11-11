// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace MagicaCloth
{
    /// <summary>
    /// レンダーメッシュデフォーマーのコンポーネント
    /// </summary>
    [HelpURL("https://magicasoft.jp/magica-cloth-render-deformer/")]
    [AddComponentMenu("MagicaCloth/MagicaRenderDeformer")]
    public class MagicaRenderDeformer : CoreComponent
    {
        /// <summary>
        /// データバージョン
        /// </summary>
        private const int DATA_VERSION = 2;

        /// <summary>
        /// エラーデータバージョン
        /// </summary>
        private const int ERR_DATA_VERSION = 0;

        /// <summary>
        /// レンダーメッシュのデフォーマー
        /// </summary>
        [SerializeField]
        private RenderMeshDeformer deformer = new RenderMeshDeformer();

        [SerializeField]
        private int deformerHash;
        [SerializeField]
        private int deformerVersion;

        /// <summary>
        /// カリングモードのキャッシュ(-1=キャッシュ無効)
        /// </summary>
        internal PhysicsTeam.TeamCullingMode cullModeCash { get; private set; } = (PhysicsTeam.TeamCullingMode)(-1);

        //=========================================================================================
        public override ComponentType GetComponentType()
        {
            return ComponentType.RenderDeformer;
        }

        //=========================================================================================
        /// <summary>
        /// データを識別するハッシュコードを作成して返す
        /// </summary>
        /// <returns></returns>
        public override int GetDataHash()
        {
            int hash = 0;
            hash += Deformer.GetDataHash();
            return hash;
        }

        //=========================================================================================
        public RenderMeshDeformer Deformer
        {
            get
            {
                deformer.Parent = this;
                return deformer;
            }
        }

        //=========================================================================================
        void OnValidate()
        {
            Deformer.OnValidate();
        }

        protected override void OnInit()
        {
            Deformer.Init();
        }

        protected override void OnDispose()
        {
            Deformer.Dispose();
        }

        protected override void OnUpdate()
        {
            Deformer.Update();
        }

        protected override void OnActive()
        {
            Deformer.OnEnable();
        }

        protected override void OnInactive()
        {
            Deformer.OnDisable();
        }

        protected void OnBecameVisible()
        {
            //Debug.Log("RD Visible");
            if (MagicaPhysicsManager.IsInstance())
                UpdateCullingMode(this);
        }

        protected void OnBecameInvisible()
        {
            //Debug.Log("RD Invisible");
            if (MagicaPhysicsManager.IsInstance())
                UpdateCullingMode(this);
        }


        //=========================================================================================
        internal override void UpdateCullingMode(CoreComponent caller)
        {
            // カリングモード（VirtualDeformerから収集する)
            cullModeCash = 0;
            foreach (var status in Status.parentStatusSet)
            {
                if (status != null)
                {
                    var owner = status.OwnerFunc() as MagicaVirtualDeformer;
                    if (owner != null)
                    {
                        var ownerCull = owner.cullModeCash;
                        if (ownerCull > cullModeCash)
                            cullModeCash = ownerCull;
                    }
                }
            }

            // 表示状態
            // クロスコンポーネントに非表示での停止が設定されている場合はレンダラーのIsVisibleフラグを参照する
            // それ以外では常に表示状態として可動させる
            var visible = true;
            if (cullModeCash != PhysicsTeam.TeamCullingMode.Off)
                visible = Deformer.IsRendererVisible;
            IsVisible = visible;

            // 計算状態
            bool stopInvisible = cullModeCash != PhysicsTeam.TeamCullingMode.Off;
            bool calc = true;
            if (stopInvisible)
            {
                calc = visible;
            }
            var val = calc ? 1 : 0;
            if (calculateValue != val)
            {
                calculateValue = val;
                OnChangeCalculation();
            }

            // VirtualDeformerへ伝達
            foreach (var status in Status.parentStatusSet)
            {
                var core = status?.OwnerFunc() as CoreComponent;
                if (core && core != caller)
                    core.UpdateCullingMode(this);
            }
        }

        protected override void OnChangeCalculation()
        {
            //Debug.Log($"RD [{this.name}] Visible:{IsVisible} Calc:{IsCalculate} F:{Time.frameCount}");
            Deformer.ChangeCalculation(IsCalculate);

            if (IsCalculate && MagicaPhysicsManager.Instance.IsDelay && cullModeCash == PhysicsTeam.TeamCullingMode.Reset)
            {
                // メッシュ書き込みを１回スキップさせる
                Deformer.IsWriteSkip = true;
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
            deformerHash = Deformer.SaveDataHash;
            deformerVersion = Deformer.SaveDataVersion;
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

            if (Deformer == null)
                return Define.Error.DeformerNull;

            var deformerError = Deformer.VerifyData();
            if (deformerError != Define.Error.None)
                return deformerError;

            if (deformerHash != Deformer.SaveDataHash)
                return Define.Error.DeformerHashMismatch;
            if (deformerVersion != Deformer.SaveDataVersion)
                return Define.Error.DeformerVersionMismatch;

            return Define.Error.None;
        }

        public override string GetInformation()
        {
            if (Deformer != null)
                return Deformer.GetInformation();
            else
                return base.GetInformation();
        }

        //=========================================================================================
        /// <summary>
        /// ボーンを置換する
        /// </summary>
        /// <param name="boneReplaceDict"></param>
        public override void ReplaceBone<T>(Dictionary<T, Transform> boneReplaceDict)
        {
            base.ReplaceBone(boneReplaceDict);

            Deformer.ReplaceBone(boneReplaceDict);
        }

        /// <summary>
        /// 現在使用しているボーンを格納して返す
        /// </summary>
        /// <returns></returns>
        public override HashSet<Transform> GetUsedBones()
        {
            var bones = base.GetUsedBones();
            bones.UnionWith(Deformer.GetUsedBones());
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
            Deformer.ChangeUseUnityPhysics(sw);
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
            return Deformer.GetEditorPositionNormalTangent(out wposList, out wnorList, out wtanList);
        }

        /// <summary>
        /// メッシュのトライアングルリストを返す（エディタ用）
        /// </summary>
        /// <returns></returns>
        public override List<int> GetEditorTriangleList()
        {
            return Deformer.GetEditorTriangleList();
        }

        /// <summary>
        /// メッシュのラインリストを返す（エディタ用）
        /// </summary>
        /// <returns></returns>
        public override List<int> GetEditorLineList()
        {
            return Deformer.GetEditorLineList();
        }

        //=========================================================================================
        /// <summary>
        /// 頂点の使用状態をリストにして返す（エディタ用）
        /// 数値が１以上ならば使用中とみなす
        /// すべて使用状態ならばnullを返す
        /// </summary>
        /// <returns></returns>
        public override List<int> GetUseList()
        {
            return Deformer.GetEditorUseList();
        }

        //=========================================================================================
        /// <summary>
        /// 共有データオブジェクト収集
        /// </summary>
        /// <returns></returns>
        public override List<ShareDataObject> GetAllShareDataObject()
        {
            var slist = base.GetAllShareDataObject();
            slist.Add(Deformer.MeshData);
            return slist;
        }

        /// <summary>
        /// sourceの共有データを複製して再セットする
        /// 再セットした共有データを返す
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public override ShareDataObject DuplicateShareDataObject(ShareDataObject source)
        {
            if (Deformer.MeshData == source)
            {
                //Deformer.MeshData = Instantiate(Deformer.MeshData);
                Deformer.MeshData = ShareDataObject.Clone(Deformer.MeshData);
                return Deformer.MeshData;
            }

            return null;
        }
    }
}
