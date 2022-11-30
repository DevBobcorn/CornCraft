// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicaCloth
{
    /// <summary>
    /// ベースメッシュデフォーマー
    /// </summary>
    [System.Serializable]
    public abstract class BaseMeshDeformer : IEditorMesh, IDataVerify, IDataHash
    {
        /// <summary>
        /// 仮想メッシュデータ
        /// </summary>
        [SerializeField]
        private MeshData meshData = null;

        /// <summary>
        /// メッシュの計算基準となるオブジェクト(必須)
        /// </summary>
        [SerializeField]
        private GameObject targetObject;

        /// <summary>
        /// データ検証ハッシュ
        /// </summary>
        [SerializeField]
        protected int dataHash;
        [SerializeField]
        protected int dataVersion;

        /// <summary>
        /// 実行状態
        /// </summary>
        protected RuntimeStatus status = new RuntimeStatus();

        //=========================================================================================
        /// <summary>
        /// 親コンポーネント(Unity2019.3の参照切れ対策)
        /// </summary>
        private CoreComponent parent;

        public CoreComponent Parent
        {
            get
            {
                return parent;
            }
            set
            {
                parent = value;
            }
        }

        //=========================================================================================
        public virtual MeshData MeshData
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    return meshData;
                else
                {
                    // unity2019.3で参照がnullとなる不具合の対処（臨時）
                    var so = new SerializedObject(parent);
                    return so.FindProperty("deformer.meshData").objectReferenceValue as MeshData;
                }
#else
                return meshData;
#endif
            }
            set
            {
                meshData = value;
            }
        }

        public GameObject TargetObject
        {
            get
            {
                return targetObject;
            }
            set
            {
                targetObject = value;
            }
        }

        public RuntimeStatus Status
        {
            get
            {
                return status;
            }
        }

        /// <summary>
        /// 登録メッシュインデックス
        /// (-1=無効)
        /// </summary>
        public int MeshIndex { get; protected set; } = -1;

        /// <summary>
        /// 登録頂点数
        /// </summary>
        public int VertexCount { get; protected set; }

        /// <summary>
        /// 登録スキニング頂点数
        /// </summary>
        public int SkinningVertexCount { get; protected set; }

        /// <summary>
        /// 登録トライアングル数
        /// </summary>
        public int TriangleCount { get; protected set; }

        //=========================================================================================
        /// <summary>
        /// 初期化
        /// 通常はStart()で呼ぶ
        /// </summary>
        /// <param name="vcnt"></param>
        public virtual void Init()
        {
            status.UpdateStatusAction = OnUpdateStatus;
            status.OwnerFunc = () => Parent;
            if (status.IsInitComplete || status.IsInitStart)
                return;
            status.SetInitStart();

            OnInit();

            // データチェック
            if (VerifyData() != Define.Error.None)
            {
                // error
                status.SetInitError();
                return;
            }

            status.SetInitComplete();

            // 状態更新
            status.UpdateStatus();
        }

        protected virtual void OnInit()
        {
            // メッシュチャンク無効化
            MeshIndex = -1;

            // マネージャへ登録
            MagicaPhysicsManager.Instance.Mesh.AddMesh(this);
        }

        /// <summary>
        /// 破棄
        /// 通常はOnDestroy()で呼ぶ
        /// </summary>
        public virtual void Dispose()
        {
            // マネージャから削除
            if (MagicaPhysicsManager.IsInstance())
                MagicaPhysicsManager.Instance.Mesh.RemoveMesh(this);

            status.SetDispose();
        }

        public virtual void OnEnable()
        {
            status.SetEnable(true);
            status.UpdateStatus();
        }

        public virtual void OnDisable()
        {
            status.SetEnable(false);
            status.UpdateStatus();
        }

        public virtual void Update()
        {
            // 実行中データ監視
            var error = VerifyData() != Define.Error.None;
            status.SetRuntimeError(error);
            status.UpdateStatus();
        }

        /// <summary>
        /// メッシュの描画判定
        /// </summary>
        /// <param name="bufferIndex"></param>
        internal abstract void MeshCalculation(int bufferIndex);

        /// <summary>
        /// 通常のメッシュ書き込み
        /// </summary>
        /// <param name="bufferIndex"></param>
        internal abstract void NormalWriting(int bufferIndex);

        // 実行状態の更新
        protected void OnUpdateStatus()
        {
            if (status.IsActive)
            {
                // 実行状態に入った
                OnActive();
            }
            else
            {
                // 実行状態から抜けた
                OnInactive();
            }
        }

        /// <summary>
        /// 実行状態に入った場合に呼ばれます
        /// </summary>
        protected virtual void OnActive()
        {
        }

        /// <summary>
        /// 実行状態から抜けた場合に呼ばれます
        /// </summary>
        protected virtual void OnInactive()
        {
        }

        //=========================================================================================
        public virtual bool IsMeshUse()
        {
            return false;
        }

        public virtual bool IsActiveMesh()
        {
            return false;
        }

        public bool IsSkinning
        {
            get
            {
                if (MeshData != null)
                    return MeshData.isSkinning;
                return false;
            }
        }

        public int BoneCount
        {
            get
            {
                if (MeshData != null)
                {
                    if (MeshData.isSkinning)
                        return MeshData.BoneCount;
                    else
                        return 1;
                }
                else
                    return 0;
            }
        }

        //=========================================================================================
        public virtual void AddUseMesh(System.Object parent)
        {
        }

        public virtual void RemoveUseMesh(System.Object parent)
        {
        }

        /// <summary>
        /// 利用頂点登録
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns>新規登録ならtrueを返す</returns>
        public virtual bool AddUseVertex(int vindex, bool fix)
        {
            return false;
        }

        /// <summary>
        /// 利用頂点解除
        /// </summary>
        /// <param name="vindex"></param>
        /// <returns>登録解除ならtrueを返す</returns>
        public virtual bool RemoveUseVertex(int vindex, bool fix)
        {
            return false;
        }

        /// <summary>
        /// 未来予測のリセットを行う
        /// </summary>
        public virtual void ResetFuturePrediction()
        {
        }

        /// <summary>
        /// UnityPhysicsでの利用を設定する
        /// </summary>
        /// <param name="sw"></param>
        public virtual void ChangeUseUnityPhysics(bool sw)
        {
        }

        //=========================================================================================
        /// <summary>
        /// データを識別するハッシュコードを作成して返す
        /// </summary>
        /// <returns></returns>
        public virtual int GetDataHash()
        {
            int hash = 0;
            if (MeshData != null)
                hash += MeshData.GetDataHash();
            if (targetObject)
                hash += targetObject.GetDataHash();

            return hash;
        }

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
        /// <summary>
        /// データバージョンを取得する
        /// </summary>
        /// <returns></returns>
        public abstract int GetVersion();

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
            //if (dataVersion != GetVersion())
            //    return Define.Error.DataVersionMismatch;
            if (MeshData == null)
                return Define.Error.MeshDataNull;
            if (targetObject == null)
                return Define.Error.TargetObjectNull;
            var mdataError = MeshData.VerifyData();
            if (mdataError != Define.Error.None)
                return mdataError;

            return Define.Error.None;
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

        //=========================================================================================
        /// <summary>
        /// メッシュのワールド座標/法線/接線を返す（エディタ設定用）
        /// </summary>
        /// <param name="wposList"></param>
        /// <param name="wnorList"></param>
        /// <param name="wtanList"></param>
        /// <returns>頂点数</returns>
        public abstract int GetEditorPositionNormalTangent(out List<Vector3> wposList, out List<Vector3> wnorList, out List<Vector3> wtanList);

        /// <summary>
        /// メッシュのトライアングルリストを返す（エディタ設定用）
        /// </summary>
        /// <returns></returns>
        public abstract List<int> GetEditorTriangleList();

        /// <summary>
        /// メッシュのラインリストを返す（エディタ用）
        /// </summary>
        /// <returns></returns>
        public abstract List<int> GetEditorLineList();
    }
}
