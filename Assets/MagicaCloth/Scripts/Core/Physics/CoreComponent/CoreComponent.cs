// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// MonoBehaviourを継承するコンポーネント用に各種インターフェースを定義したもの
    /// ・共有データ収集
    /// ・データ検証
    /// ・メッシュ座標取得
    /// ・クロス状態取得
    /// ・データハッシュ作成
    /// ・実行状態管理
    /// </summary>
    public abstract partial class CoreComponent : BaseComponent, IShareDataObject, IDataVerify, IEditorMesh, IEditorCloth, IDataHash, IBoneReplace
    {
        [SerializeField]
        protected int dataHash;
        [SerializeField]
        protected int dataVersion;

        /// <summary>
        /// 実行状態
        /// </summary>
        protected RuntimeStatus status = new RuntimeStatus();

        public RuntimeStatus Status
        {
            get
            {
                return status;
            }
        }

        /// <summary>
        /// アクティブになった回数
        /// </summary>
        //protected int ActiveCount { get; private set; }

        /// <summary>
        /// UnityPhysics更新の使用カウント(v1.9.4)
        /// このカウントが１以上の場合、このコンポーネントはUnityPhysicsの更新タイミングで処理される
        /// </summary>
        private int useUnityPhysicsCount = 0;

        /// <summary>
        /// 現在のUnityPhysics設定状態
        /// </summary>
        private bool nowUseUnityPhysics = false;

        /// <summary>
        /// 現在の表示状態
        /// </summary>
        public bool IsVisible { get; protected set; } = false;

        /// <summary>
        /// 現在の計算状態(0:OFF, 1:ON, -1:invalid)
        /// </summary>
        protected int calculateValue = -1;
        public bool IsCalculate => calculateValue == 1;

        //=========================================================================================
        /// <summary>
        /// データを識別するハッシュコードを作成して返す
        /// </summary>
        /// <returns></returns>
        public abstract int GetDataHash();

        public int SaveDataHash
        {
            get
            {
                return dataHash;
            }
        }

        public int SaveDataVersion
        {
            get
            {
                return dataVersion;
            }
        }

        //=========================================================================================
        protected virtual void Start()
        {
            Init();
        }

        public virtual void OnEnable()
        {
            status.SetEnable(true);
            status.UpdateStatus();
        }

        public virtual void OnDisable()
        {
            //Debug.Log("Core.OnDisable():" + gameObject.name);
            status.SetEnable(false);
            status.UpdateStatus();
        }

        protected virtual void OnDestroy()
        {
            if (Status.IsDispose)
                return;

            //Debug.Log("Core.OnDestroy():" + gameObject.name);
            status.SetDispose();
            OnDispose();

            // 登録削除
            if (MagicaPhysicsManager.IsInstance())
                MagicaPhysicsManager.Instance.Component.RemoveComponent(this);
        }

        // ランタイム時のエラーチェックはパフォーマンスの観点から行わない。
        // 初期化時にはエラーチェックをしているので問題ないはず
        //protected virtual void ManagedUpdate()
        //{
        //    //Debug.Log("ManagedUpdate.");
        //    if (status.IsInitSuccess)
        //    {
        //        var error = VerifyData() != Define.Error.None;
        //        status.SetRuntimeError(error);
        //        UpdateStatus();

        //        if (status.IsActive)
        //            OnUpdate();
        //    }
        //}

        //protected virtual void Update()
        //{
        //    if (status.IsInitSuccess)
        //    {
        //        var error = VerifyData() != Define.Error.None;
        //        status.SetRuntimeError(error);
        //        UpdateStatus();

        //        if (status.IsActive)
        //            OnUpdate();
        //    }
        //}

        //=========================================================================================
        /// <summary>
        /// 初期化
        /// 通常はStart()で呼ぶ
        /// すでに初期化済み、もしくは初期化中ならば何もしない
        /// </summary>
        /// <param name="vcnt"></param>
        public void Init()
        {
            //Develop.Log($"Core.Init():{gameObject.name}");

            status.UpdateStatusAction = OnUpdateStatus;
            status.DisconnectedAction = OnDisconnectedStatus;
            status.OwnerFunc = () => this;
            if (status.IsInitComplete || status.IsInitStart)
                return;
            status.SetInitStart();

            // 登録
            MagicaPhysicsManager.Instance.Component.AddComponent(this);

            if (VerifyData() != Define.Error.None)
            {
                status.SetInitError();
                return;
            }

            OnInit();
            if (status.IsInitError)
                return;

            status.SetInitComplete();

            status.UpdateStatus();
        }

        //=========================================================================================
        /// <summary>
        /// 初期化
        /// </summary>
        protected abstract void OnInit();

        /// <summary>
        /// 破棄
        /// </summary>
        protected abstract void OnDispose();

        /// <summary>
        /// 更新
        /// </summary>
        protected abstract void OnUpdate();

        /// <summary>
        /// 実行状態に入った場合に呼ばれます
        /// </summary>
        protected abstract void OnActive();

        /// <summary>
        /// 実行状態から抜けた場合に呼ばれます
        /// </summary>
        protected abstract void OnInactive();

        /// <summary>
        /// 実行状態が更新された場合に呼び出されます
        /// </summary>
        protected virtual void OnUpdateStatus()
        {
            if (status.IsActive)
            {
                // 実行状態に入った
                //Debug.Log($"[{this.name}] OnActive() F:{Time.frameCount}");
                //ActiveCount++; // アクティブ回数
                OnActive();
                ActiveUseUnityPhysics();
                UpdateCullingMode(this); // 計算ステータス更新
            }
            else
            {
                // 実行状態から抜けた
                //Debug.Log($"[{this.name}] OnInactive() F:{Time.frameCount}");
                InactiveUseUnityPhysics();
                calculateValue = 0; // 計算フラグOFF
                OnInactive();
            }
        }

        /// <summary>
        /// 状態の連動がすべて切断された場合に呼び出されます
        /// </summary>
        protected virtual void OnDisconnectedStatus()
        {
            //Debug.Log("DisconnectStatus:" + gameObject.name);
            // 破棄する
            OnDestroy();
        }


        //=========================================================================================
        /// <summary>
        /// UnityPhysicsの更新設定を行う
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="immediate"></param>
        public void SetUseUnityPhysics(bool sw)
        {
            useUnityPhysicsCount += sw ? 1 : -1;
            Debug.Assert(useUnityPhysicsCount >= 0);

            //Debug.Log($"[{this.name}] SetUseUnityPhysics():{sw} count:{useUnityPhysicsCount}");

            // 即時実行判定
            if (Status.IsActive)
            {
                if (IsUseUnityPhysics() != nowUseUnityPhysics)
                {
                    nowUseUnityPhysics = IsUseUnityPhysics();
                    ChangeUseUnityPhysics(nowUseUnityPhysics);
                }
            }
        }

        private void ActiveUseUnityPhysics()
        {
            if (nowUseUnityPhysics == false && useUnityPhysicsCount > 0)
            {
                //Debug.Log($"[{this.name}] ActiveUseUnityPhysics()");
                nowUseUnityPhysics = true;
                ChangeUseUnityPhysics(nowUseUnityPhysics);
            }
        }

        private void InactiveUseUnityPhysics()
        {
            if (nowUseUnityPhysics == true)
            {
                //Debug.Log($"[{this.name}] InactiveUseUnityPhysics()");
                nowUseUnityPhysics = false;
                ChangeUseUnityPhysics(nowUseUnityPhysics);
            }
        }

        /// <summary>
        /// UnityPhyiscsでの更新の変更
        /// 継承クラスは自身の使用するボーンに対してUnityPhysicsの利用カウントの増減を行う
        /// </summary>
        /// <param name="sw"></param>
        protected virtual void ChangeUseUnityPhysics(bool sw)
        {
            //Debug.Log($"[{this.name}] ChangeUseUnityPhysics():{sw}");
        }

        protected bool IsUseUnityPhysics()
        {
            return useUnityPhysicsCount > 0;
        }

        //=========================================================================================
        /// <summary>
        /// 共有データの収集
        /// </summary>
        /// <returns></returns>
        public virtual List<ShareDataObject> GetAllShareDataObject()
        {
            return new List<ShareDataObject>();
        }

        /// <summary>
        /// sourceの共有データを複製して再セットする
        /// 再セットした共有データを返す
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public abstract ShareDataObject DuplicateShareDataObject(ShareDataObject source);

        //=========================================================================================
        /// <summary>
        /// ユーザー操作による有効フラグの切り替え(v1.2)
        /// </summary>
        /// <param name="sw"></param>
        protected void SetUserEnable(bool sw)
        {
            if (status.SetUserEnable(sw))
            {
                status.UpdateStatus();
            }
        }

        //=========================================================================================
        /// <summary>
        /// カリングモードの更新チェック
        /// </summary>
        /// <param name="Caller"></param>
        internal virtual void UpdateCullingMode(CoreComponent Caller)
        {
            // (1)カリングモード更新
            // (2)表示状態更新
            // (3)計算状態更新
            // (4)伝達
        }

        /// <summary>
        /// カリングモードの変更通知
        /// </summary>
        protected virtual void OnChangeCalculation() { }

        //=========================================================================================
        /// <summary>
        /// データバージョンを取得する
        /// </summary>
        /// <returns></returns>
        public abstract int GetVersion();

        /// <summary>
        /// エラーとするデータバージョンを取得する
        /// これ以下のバージョンのデータは利用することができない
        /// </summary>
        /// <returns></returns>
        public abstract int GetErrorVersion();

        /// <summary>
        /// 現在のデータが正常（実行できる状態）か返す
        /// </summary>
        /// <returns></returns>
        public virtual Define.Error VerifyData()
        {
            if (dataVersion == 0)
                return Define.Error.EmptyData;
            if (dataHash == 0)
                return Define.Error.InvalidDataHash;
            if (dataVersion > 0 && GetErrorVersion() > 0 && dataVersion <= GetErrorVersion())
                return Define.Error.TooOldDataVersion; // データバージョンが古すぎる（動かない）
            if (dataVersion > GetVersion())
                return Define.Error.HigherDataVersion; // データバージョンが新しすぎる（動かさない）
            //if (dataVersion != GetVersion())
            //    return Define.Error.DataVersionMismatch;

            return Define.Error.None;
        }

        /// <summary>
        /// データバージョンチェック
        /// </summary>
        /// <returns></returns>
        public Define.Error VerifyDataVersion()
        {
            if (dataVersion == 0)
                return Define.Error.None;

            return dataVersion == GetVersion() ? Define.Error.None : Define.Error.OldDataVersion;
        }

        /// <summary>
        /// データバージョンが古いかチェックする
        /// </summary>
        /// <returns></returns>
        public bool IsOldDataVertion()
        {
            return dataVersion != GetVersion();
        }

        /// <summary>
        /// データを検証して結果を格納する
        /// </summary>
        /// <returns></returns>
        public virtual void CreateVerifyData()
        {
            dataHash = GetDataHash();
            dataVersion = GetVersion();
        }

        /// <summary>
        /// データ検証の結果テキストを取得する
        /// </summary>
        /// <returns></returns>
        public virtual string GetInformation()
        {
            return "No information.";
        }

        /// <summary>
        /// データフォーマットを最新に更新する
        /// 主に古いパラメータを最新のパラメータに変換する
        /// </summary>
        /// <returns>true=更新あり, false=更新なし</returns>
        public virtual bool UpgradeFormat()
        {
            return false;
        }

        //=========================================================================================
        /// <summary>
        /// アバター変更（着せ替え）
        /// </summary>
        /// <param name="boneReplaceDict"></param>
        public void ChangeAvatar<T>(Dictionary<T, Transform> boneReplaceDict) where T : class
        {
            // 稼働中ならば一旦停止させる
            bool active = status.IsActive;
            if (active)
            {
                status.SetEnable(false);
                status.UpdateStatus();
            }

            // ボーン置換
            ReplaceBone(boneReplaceDict);

            // 稼働中であったならば再び起動する
            if (active)
            {
                status.SetEnable(true);
                status.UpdateStatus();
            }
        }

        //=========================================================================================
        /// <summary>
        /// ボーンを置換する
        /// </summary>
        /// <param name="boneReplaceDict"></param>
        public virtual void ReplaceBone<T>(Dictionary<T, Transform> boneReplaceDict) where T : class
        {
        }

        /// <summary>
        /// 現在使用しているボーンを格納して返す
        /// </summary>
        /// <returns></returns>
        public virtual HashSet<Transform> GetUsedBones()
        {
            //return new HashSet<Transform>();
            var bones = new HashSet<Transform>();
            bones.Add(transform); // コンポーネント自身のTransform
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
        public virtual int GetEditorPositionNormalTangent(out List<Vector3> wposList, out List<Vector3> wnorList, out List<Vector3> wtanList)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// メッシュのトライアングルリストを返す（エディタ用）
        /// </summary>
        /// <returns></returns>
        public virtual List<int> GetEditorTriangleList()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// メッシュのラインリストを返す（エディタ用）
        /// </summary>
        /// <returns></returns>
        public virtual List<int> GetEditorLineList()
        {
            throw new System.NotImplementedException();
        }

        //=========================================================================================
        /// <summary>
        /// 頂点の選択状態をリストにして返す（エディタ用）
        /// 選択状態は ClothSelection.Invalid / ClothSelection.Fixed / ClothSelection.Move
        /// すべてがInvalidならばnullを返す
        /// </summary>
        /// <returns></returns>
        public virtual List<int> GetSelectionList()
        {
            return null;
        }

        /// <summary>
        /// 頂点の使用状態をリストにして返す（エディタ用）
        /// 数値が１以上ならば使用中とみなす
        /// すべて使用状態ならばnullを返す
        /// </summary>
        /// <returns></returns>
        public virtual List<int> GetUseList()
        {
            return null;
        }
    }
}
